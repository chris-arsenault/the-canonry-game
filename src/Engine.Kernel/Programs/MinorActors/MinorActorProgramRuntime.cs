namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramRuntime :
    IKernelProgramRuntime<MinorActorProgramData, MinorActorProgramState>
{
    internal void NoteEntityRetired(int entityId)
    {
        State.DeactivateSource(entityId);
    }
    private const int LocalVerbStepRadius = 1;
    private static readonly FixedScalar CultureTraceRawInfluence = WorldState.CultureTraceRawMass;
    private static readonly FixedScalar CultureSupportedRawInfluence = WorldState.CultureControlledRawMass;
    private static readonly FixedScalar CultureStrongRawInfluence = WorldState.CultureEntrenchedRawMass;
    private static readonly FixedScalar CultureFieldRawInfluence = WorldState.CultureFullRawMass;
    private static readonly FixedScalar CultureForeignTraceToleranceShare = 10;
    private static readonly FixedScalar CultureForeignContestScaleShare = 45;
    private static readonly FixedScalar LifecycleMaintenanceCoverageScale = FixedScalar.FromUnits(30000L);
    private static readonly FixedScalar LifecycleMaintenanceEarlyNeedScale = FixedScalar.FromUnits(200000L);
    private static readonly FixedScalar LifecycleMaintenanceScoreScale = FixedScalar.FromUnits(1350000L);
    private static readonly FixedScalar NexusSourceCultureMultiplier = FixedScalar.FromUnits(1500000L);
    private readonly MinorActorCellTarget[] _verbTargetCells = new MinorActorCellTarget[7];

    public MinorActorProgramRuntime(MinorActorProgramData data, MinorActorProgramState state)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        Data = data;
        State = state;
    }

    public MinorActorProgramData Data { get; }

    public MinorActorProgramState State { get; }

    public int RegisterEmitterRule(
        int sourceKindId,
        int sourceSubtypeId,
        int purposeCode,
        int actorKindCode,
        int planeCode,
        int ttlTicks,
        int emitCadenceTicks,
        int maxActivePerSource,
        int spawnRadius,
        int actionCadenceTicks,
        FixedScalar workDelta) =>
        Data.RegisterEmitterRule(
            sourceKindId,
            sourceSubtypeId,
            purposeCode,
            actorKindCode,
            planeCode,
            ttlTicks,
            emitCadenceTicks,
            maxActivePerSource,
            spawnRadius,
            actionCadenceTicks,
            workDelta);

    public int RegisterVerb(
        int sourceRuleId,
        int verbId,
        int actionCode,
        int targetingCode,
        int planeCode,
        int radius,
        int sampleCount,
        FixedScalar weight,
        FixedScalar workDeltaMultiplier) =>
        Data.RegisterVerb(
            sourceRuleId,
            verbId,
            actionCode,
            targetingCode,
            planeCode,
            radius,
            sampleCount,
            weight,
            workDeltaMultiplier);

    public int Spawn(
        int cultureId,
        int sourceEntityId,
        int sourceRuleId,
        int actorKindCode,
        int planeCode,
        int q,
        int r,
        int ttlTicks,
        int actionCadenceTicks,
        FixedScalar workDelta,
        int tick,
        int purposeCode = MinorActorPurposeCodes.CulturalLabor)
    {
        if (!Data.RuleHasVerbs(sourceRuleId))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(sourceRuleId));
        }

        return State.Spawn(
            cultureId,
            sourceEntityId,
            sourceRuleId,
            actorKindCode,
            planeCode,
            q,
            r,
            ttlTicks,
            actionCadenceTicks,
            workDelta,
            tick,
            purposeCode);
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        EmitFromSources(simulation, tick);
        TickActors(simulation, tick);
    }

    private void EmitFromSources(Simulation simulation, int tick)
    {
        if (Data.RuleCount == 0)
        {
            return;
        }

        var world = simulation.World;
        for (var index = 0; index < simulation.FrozenEntityCount; index++)
        {
            var entityId = simulation.FrozenEntityAt(index);
            var sourceKindId = world.EntityKindId(entityId);
            for (var rule = Data.FirstRuleForSourceKind(sourceKindId);
                 rule > 0;
                 rule = Data.NextRuleForSourceKind(rule))
            {
                TryEmitFromSource(simulation, world, entityId, rule, tick);
            }
        }
    }

    private void TryEmitFromSource(
        Simulation simulation,
        WorldState world,
        int sourceEntityId,
        int rule,
        int tick)
    {
        var slotMultiplier = simulation.CultureSlots.CausalSpendLimitMultiplier(sourceEntityId);
        var workTier = world.EntityWorkTier(sourceEntityId);
        if (!Data.RuleHasVerbs(rule) ||
            !Data.RuleMatches(world, sourceEntityId, rule) ||
            State.SourceActiveCount(sourceEntityId, rule) >= EffectiveMaxActivePerSource(world, sourceEntityId, rule, slotMultiplier, workTier) ||
            tick < State.NextEmissionTick(sourceEntityId, rule))
        {
            return;
        }

        var anchorEntityId = sourceEntityId;
        if (Data.PurposeCodes[rule] == MinorActorPurposeCodes.CampaignLabor &&
            !TryResolveCampaignTarget(world, sourceEntityId, out anchorEntityId))
        {
            return;
        }

        State.SetNextEmissionTick(sourceEntityId, rule, tick + EffectiveEmitCadence(rule, slotMultiplier));
        if (!TryResolveSpawnPosition(simulation, world, sourceEntityId, anchorEntityId, rule, out var q, out var r))
        {
            return;
        }

        _ = State.Spawn(
            world.EntityCultureId(sourceEntityId),
            sourceEntityId,
            rule,
            Data.ActorKindCodes[rule],
            Data.PlaneCodes[rule],
            q,
            r,
            Data.TtlTicks[rule],
            Data.ActionCadenceTicks[rule],
            Data.WorkDeltas[rule],
            tick,
            Data.PurposeCodes[rule]);
    }



    // Catalog kinds draw their bonus workers from the stage table (steps at
    // promotions); legacy kinds keep the raw additive tier.
    private int EffectiveMaxActivePerSource(WorldState world, int sourceEntityId, int rule, int slotMultiplier, int workTier)
    {
        var kindId = world.EntityKindId(sourceEntityId);
        var progressionBonus = world.KindHasStages(kindId)
            ? world.StageWorkerBonus(world.StageRuleFor(kindId, workTier))
            : workTier;
        return (Data.MaxActivePerSource[rule] * global::System.Math.Max(1, slotMultiplier)) +
            global::System.Math.Max(0, progressionBonus);
    }

    private int EffectiveEmitCadence(int rule, int slotMultiplier) =>
        global::System.Math.Max(1, Data.EmitCadenceTicks[rule] / global::System.Math.Max(1, slotMultiplier));

    private void TickActors(Simulation simulation, int tick)
    {
        while (State.TryTakeDueActor(tick, out var actorId))
        {
            TickActor(simulation, actorId, tick);
        }
    }

    private void TickActor(Simulation simulation, int actorId, int tick)
    {
        if (!simulation.World.EntityIsActive(State.SourceEntityId(actorId)))
        {
            _ = State.Deactivate(actorId);
            return;
        }

        if (tick >= State.NextActionTick(actorId))
        {
            ApplySelectedVerb(simulation, actorId, tick);
            State.SetNextActionTick(actorId, tick + State.ActionCadenceTicks(actorId));
        }

        if (tick >= State.ExpiresAtTick(actorId))
        {
            _ = State.Deactivate(actorId);
        }
        else
        {
            State.Schedule(actorId);
        }
    }

    private void ApplySelectedVerb(Simulation simulation, int actorId, int tick)
    {
        if (!TrySelectVerbCandidate(simulation, actorId, tick, out var selected))
        {
            EmitMinorActorVerb(simulation, actorId, tick, default, TraceStatusCodes.NoTarget, delta: 0);
            return;
        }

        State.SetPosition(actorId, selected.Q, selected.R);
        var cultureId = State.CultureId(actorId);
        var delta = State.WorkDelta(actorId) *
            selected.WorkDeltaMultiplier *
            SourceSlotWorkMultiplier(simulation, State.SourceEntityId(actorId)) *
            NexusSourceMultiplier(simulation, actorId, selected.PlaneCode) *
            simulation.Knowledge.Modifier(
                cultureId,
                KnowledgeGrantDomainCodes.MinorActorWork,
                State.PurposeCode(actorId));
        if (selected.ActionCode == MinorActorActionCodes.RoadMaintenance)
        {
            delta *= simulation.Knowledge.Modifier(cultureId, KnowledgeGrantDomainCodes.RoadWork);
        }
        if (delta <= 0)
        {
            EmitMinorActorVerb(simulation, actorId, tick, selected, TraceStatusCodes.NoReward, delta);
            return;
        }

        var appliedDelta = ApplySelectedVerbAction(simulation, actorId, selected, delta, tick);

        if (appliedDelta <= 0)
        {
            EmitMinorActorVerb(simulation, actorId, tick, selected, TraceStatusCodes.NoReward, appliedDelta);
            return;
        }

        EmitMinorActorVerb(simulation, actorId, tick, selected, TraceStatusCodes.Selected, appliedDelta);
    }

    private FixedScalar ApplySelectedVerbAction(
        Simulation simulation,
        int actorId,
        in MinorActorVerbCandidate selected,
        FixedScalar delta,
        int tick)
    {
        switch (selected.ActionCode)
        {
            case MinorActorActionCodes.CultureExpansion:
                return ApplyCultureExpansion(
                    simulation,
                    actorId,
                    selected.PlaneCode,
                    selected.Q,
                    selected.R,
                    CultureInfluenceActionDelta(simulation, delta),
                    tick);
            case MinorActorActionCodes.RoadMaintenance:
                _ = simulation.Movement.ReinforceRoad(
                    simulation,
                    selected.PlaneCode,
                    selected.Q,
                    selected.R,
                    delta,
                    tick);
                return delta;
            case MinorActorActionCodes.CultureCleanup:
                return ReduceOtherCultureSources(
                    simulation,
                    State.CultureId(actorId),
                    selected.PlaneCode,
                    selected.Q,
                    selected.R,
                    CultureInfluenceActionDelta(simulation, delta),
                    tick);
            case MinorActorActionCodes.LifecycleMaintenance:
                return ApplyLifecycleMaintenance(
                    simulation,
                    State.SourceEntityId(actorId),
                    delta,
                    tick);
            default:
                return 0;
        }
    }

    private static FixedScalar SourceSlotWorkMultiplier(Simulation simulation, int sourceEntityId)
    {
        var slotMultiplier = simulation.CultureSlots.CausalSpendLimitMultiplier(sourceEntityId);
        return 1 + ((global::System.Math.Max(1, slotMultiplier) - 1) * FixedScalar.FromUnits(500000L));
    }

    private FixedScalar NexusSourceMultiplier(Simulation simulation, int actorId, int planeCode)
    {
        var cultureId = State.CultureId(actorId);
        if (cultureId <= 0 ||
            !TryResolveSourcePosition(
                simulation.World,
                State.SourceEntityId(actorId),
                planeCode,
                out var sourceQ,
                out var sourceR))
        {
            return 1;
        }

        var sourceRaw = simulation.World.GetCellCultureInfluenceRaw(cultureId, planeCode, sourceQ, sourceR);
        return sourceRaw >= WorldState.CultureNexusRawMass
            ? NexusSourceCultureMultiplier
            : 1;
    }

    private bool TrySelectVerbCandidate(
        Simulation simulation,
        int actorId,
        int tick,
        out MinorActorVerbCandidate selected)
    {
        selected = default;
        var totalScore = FixedScalar.Zero;
        var selectedPriority = FixedScalar.Zero;
        var sourceRule = State.SourceRuleId(actorId);
        var processedPlaneMask = 0;
        var lifecycleNeed = new MinorActorLifecycleNeed();
        for (var verb = Data.FirstVerbForRule(sourceRule);
             verb > 0;
             verb = Data.NextVerbForRule(verb))
        {
            if (!VerbSelectableForRule(verb, sourceRule))
            {
                continue;
            }

            var planeCode = Data.VerbPlaneCodes[verb];
            var planeBit = PlaneBit(planeCode);
            if ((processedPlaneMask & planeBit) != 0)
            {
                continue;
            }

            processedPlaneMask |= planeBit;
            if (!TrySelectVerbCandidateForPlane(
                    simulation,
                    actorId,
                    sourceRule,
                    planeCode,
                    tick,
                    ref lifecycleNeed,
                    out var candidate,
                    out var candidatePriority))
            {
                continue;
            }

            if (TrySelectPrioritizedCandidate(
                    candidate,
                    candidatePriority,
                    ref totalScore,
                    ref selectedPriority,
                    selected))
            {
                selected = candidate;
            }
        }

        return totalScore > 0;
    }

    private static int PlaneBit(int planeCode) =>
        planeCode == PlaneCodes.Physical ? 1 : 2;

    private bool TrySelectVerbCandidateForPlane(
        Simulation simulation,
        int actorId,
        int sourceRule,
        int planeCode,
        int tick,
        ref MinorActorLifecycleNeed lifecycleNeed,
        out MinorActorVerbCandidate selected,
        out FixedScalar selectedPriority)
    {
        selected = default;
        selectedPriority = 0;
        if (!TryResolveActorPosition(actorId, planeCode, out var actorQ, out var actorR))
        {
            return false;
        }

        var sourceQ = actorQ;
        var sourceR = actorR;
        if (!TryResolveSourcePosition(simulation.World, State.SourceEntityId(actorId), planeCode, out sourceQ, out sourceR))
        {
            sourceQ = actorQ;
            sourceR = actorR;
        }

        var sourceWorkTier = simulation.World.EntityWorkTier(State.SourceEntityId(actorId));
        var maxOperatingRadius = MaxVerbOperatingRadius(sourceRule, planeCode, sourceWorkTier);
        var cultureId = State.CultureId(actorId);
        var targetCount = BuildVerbTargetCells(
            simulation,
            cultureId,
            planeCode,
            actorQ,
            actorR,
            sourceQ,
            sourceR,
            maxOperatingRadius,
            _verbTargetCells);
        if (targetCount == 0)
        {
            return false;
        }

        return TryScoreVerbTargetsForPlane(
            simulation,
            actorId,
            sourceRule,
            planeCode,
            sourceWorkTier,
            targetCount,
            tick,
            ref lifecycleNeed,
            out selected,
            out selectedPriority);
    }

    private bool TryScoreVerbTargetsForPlane(
        Simulation simulation,
        int actorId,
        int sourceRule,
        int planeCode,
        int sourceWorkTier,
        int targetCount,
        int tick,
        ref MinorActorLifecycleNeed lifecycleNeed,
        out MinorActorVerbCandidate selected,
        out FixedScalar selectedPriority)
    {
        selected = default;
        selectedPriority = 0;
        var totalScore = FixedScalar.Zero;
        for (var verb = Data.FirstVerbForRule(sourceRule);
             verb > 0;
             verb = Data.NextVerbForRule(verb))
        {
            if (!VerbSelectableForRule(verb, sourceRule) ||
                Data.VerbPlaneCodes[verb] != planeCode)
            {
                continue;
            }

            var operatingRadius = Data.VerbRadii[verb] + sourceWorkTier;
            for (var i = 0; i < targetCount; i++)
            {
                var target = _verbTargetCells[i];
                if (target.DistanceFromSource > operatingRadius)
                {
                    continue;
                }

                var candidate = ScoreVerbTarget(
                    simulation,
                    actorId,
                    verb,
                    planeCode,
                    target,
                    operatingRadius,
                    tick,
                    ref lifecycleNeed);
                if (TrySelectWeightedCandidate(
                        simulation,
                        actorId,
                        candidate,
                        ref totalScore,
                        ref selectedPriority,
                        selected))
                {
                    selected = candidate;
                }
            }
        }

        return totalScore > 0;
    }

    private static bool TrySelectWeightedCandidate(
        Simulation simulation,
        int actorId,
        in MinorActorVerbCandidate candidate,
        ref FixedScalar totalScore,
        ref FixedScalar selectedPriority,
        in MinorActorVerbCandidate selected)
    {
        if (candidate.Score <= 0)
        {
            return false;
        }

        totalScore += candidate.Score;
        var candidateKey = MinorActorCandidateKey(candidate);
        var candidatePriority = simulation.RandomScalar(
            actorId,
            candidate.VerbId,
            KernelRandomPurposeCodes.MinorActorVerbSelection,
            candidateKey) * candidate.Score;
        return TrySelectPrioritizedCandidate(
            candidate,
            candidatePriority,
            ref totalScore,
            ref selectedPriority,
            selected,
            scoreAlreadyAdded: true);
    }

    private static bool TrySelectPrioritizedCandidate(
        in MinorActorVerbCandidate candidate,
        FixedScalar candidatePriority,
        ref FixedScalar totalScore,
        ref FixedScalar selectedPriority,
        in MinorActorVerbCandidate selected,
        bool scoreAlreadyAdded = false)
    {
        if (candidate.Score <= 0)
        {
            return false;
        }

        if (!scoreAlreadyAdded)
        {
            totalScore += candidate.Score;
        }

        if (selected.Score <= 0)
        {
            selectedPriority = candidatePriority;
            return true;
        }

        var candidateKey = MinorActorCandidateKey(candidate);
        var selectedKey = MinorActorCandidateKey(selected);
        if (candidatePriority <= selectedPriority &&
            (candidatePriority != selectedPriority || candidateKey >= selectedKey))
        {
            return false;
        }

        selectedPriority = candidatePriority;
        return true;
    }

    private static int MinorActorCandidateKey(in MinorActorVerbCandidate candidate)
    {
        unchecked
        {
            var hash = (uint)candidate.Q * 2654435761u;
            hash ^= (uint)candidate.R * 2246822519u;
            hash ^= (uint)candidate.VerbId * 3266489917u;
            return (int)(hash ^ (hash >> 16));
        }
    }

    private bool VerbSelectableForRule(int verb, int sourceRule) =>
        Data.VerbSourceRuleIds[verb] == sourceRule && Data.VerbWeights[verb] > 0;

    private int MaxVerbOperatingRadius(int sourceRule, int planeCode, int sourceWorkTier)
    {
        var radius = 0;
        for (var verb = Data.FirstVerbForRule(sourceRule);
             verb > 0;
             verb = Data.NextVerbForRule(verb))
        {
            if (VerbSelectableForRule(verb, sourceRule) &&
                Data.VerbPlaneCodes[verb] == planeCode)
            {
                radius = global::System.Math.Max(radius, Data.VerbRadii[verb] + sourceWorkTier);
            }
        }

        return radius;
    }

    private int BuildVerbTargetCells(
        Simulation simulation,
        int cultureId,
        int planeCode,
        int actorQ,
        int actorR,
        int sourceQ,
        int sourceR,
        int maxOperatingRadius,
        MinorActorCellTarget[] targets)
    {
        var radius = maxOperatingRadius <= 0 ? 0 : LocalVerbStepRadius;
        var count = 0;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = actorQ + dq;
                var r = actorR + dr;
                if (!simulation.World.CellIsKnown(planeCode, q, r))
                {
                    continue;
                }

                var distanceFromSource = HexDistance(sourceQ, sourceR, q, r);
                if (distanceFromSource > maxOperatingRadius)
                {
                    continue;
                }

                targets[count] = BuildVerbTargetCell(
                    simulation,
                    cultureId,
                    planeCode,
                    q,
                    r,
                    HexDistance(actorQ, actorR, q, r),
                    distanceFromSource);
                count++;
            }
        }

        return count;
    }

    private bool TryResolveActorPosition(int actorId, int planeCode, out int q, out int r)
    {
        q = State.Q(actorId);
        r = State.R(actorId);
        return State.PlaneCode(actorId) == planeCode;
    }

    private MinorActorCellTarget BuildVerbTargetCell(
        Simulation simulation,
        int cultureId,
        int planeCode,
        int q,
        int r,
        int distanceFromActor,
        int distanceFromSource)
    {
        var boundary = simulation.CultureLocality.CellAt(simulation.World, cultureId, planeCode, q, r);
        var cultureField = new MinorActorCultureField(
            boundary.RawMass,
            boundary.LocalRawMass,
            boundary.ConnectedRawMass,
            boundary.SupportedAdjacentCount,
            boundary.SupportedRingTwoCount);
        var attachment = new MinorActorCultureAttachment(
            boundary.ConnectedRawMass,
            boundary.SupportedAdjacentCount,
            boundary.RawMass >= WorldState.CultureControlledRawMass ? 1 : 0);
        return new MinorActorCellTarget(
            q,
            r,
            distanceFromActor,
            distanceFromSource,
            boundary.Share,
            boundary.RawMass,
            boundary.ForeignShare,
            boundary.ForeignRawMass,
            cultureField,
            attachment,
            boundary.CellKindCode);
    }

    private MinorActorVerbCandidate ScoreVerbTarget(
        Simulation simulation,
        int actorId,
        int verb,
        int planeCode,
        in MinorActorCellTarget target,
        int effectiveRadius,
        int tick,
        ref MinorActorLifecycleNeed lifecycleNeed)
    {
        var score = MinorVerbScore(
            simulation,
            actorId,
            verb,
            planeCode,
            target.Q,
            target.R,
            target.OwnShare,
            target.OwnRaw,
            target.ForeignShare,
            target.ForeignRaw,
            target.CultureField,
            target.Attachment,
            tick,
            ref lifecycleNeed);

        if (score <= 0)
        {
            return default;
        }

        var distanceBias = 1 / (1 + (FixedScalar.FromUnits(100000L) * target.DistanceFromActor));
        score *= Data.VerbWeights[verb] * distanceBias;
        return new MinorActorVerbCandidate(
            Data.VerbIds[verb],
            Data.VerbActionCodes[verb],
            Data.VerbTargetingCodes[verb],
            planeCode,
            target.Q,
            target.R,
            target.CellKindCode,
            effectiveRadius,
            target.CultureField.LocalRaw,
            target.CultureField.ConnectedRaw,
            score,
            Data.VerbWorkDeltaMultipliers[verb]);
    }

    private static FixedScalar CultureInfluenceActionDelta(Simulation simulation, FixedScalar delta) =>
        delta * simulation.Options.KernelTuning.Programs.CultureInfluenceActionScale;

    private FixedScalar ApplyCultureExpansion(
        Simulation simulation,
        int actorId,
        int planeCode,
        int q,
        int r,
        FixedScalar delta,
        int tick)
    {
        var sourceEntityId = State.SourceEntityId(actorId);
        if (sourceEntityId > 0)
        {
            simulation.ModifyCellCultureInfluenceRawFromEntity(sourceEntityId, planeCode, q, r, delta, tick);
            return delta;
        }

        return simulation.ModifyCellCultureInfluenceRaw(
            State.CultureId(actorId),
            planeCode,
            q,
            r,
            simulation.CultureProductionScaled(State.CultureId(actorId), planeCode, delta),
            tick);
    }

    private static FixedScalar ReduceOtherCultureSources(
        Simulation simulation,
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        FixedScalar delta,
        int tick)
    {
        return simulation.ReduceOtherCultureSourcesAtCell(ownCultureId, planeCode, q, r, delta, tick);
    }

    private static FixedScalar ApplyLifecycleMaintenance(
        Simulation simulation,
        int sourceEntityId,
        FixedScalar coverage,
        int tick)
    {
        if (coverage <= 0 ||
            !simulation.Effects.TryGetLifecycleMaintenanceNeed(
                simulation,
                sourceEntityId,
                tick,
                out var attributeId,
                out var ageDrainComponent,
                out _))
        {
            return 0;
        }

        var delta = ageDrainComponent * coverage * LifecycleMaintenanceCoverageScale;
        if (delta <= 0)
        {
            return 0;
        }

        simulation.ModifyAttribute(sourceEntityId, attributeId, delta, tick);
        return delta;
    }

    private void EmitMinorActorVerb(
        Simulation simulation,
        int actorId,
        int tick,
        in MinorActorVerbCandidate selected,
        int statusCode,
        FixedScalar delta)
    {
        var sourceEntityId = State.SourceEntityId(actorId);
        var sourceWorkTier = simulation.World.EntityWorkTier(sourceEntityId);
        simulation.EmitMinorActorVerbExecuted(
            tick,
            actorId,
            sourceEntityId,
            State.SourceRuleId(actorId),
            sourceWorkTier,
            EffectiveActorRadius(actorId, sourceWorkTier, selected),
            State.CultureId(actorId),
            selected.VerbId,
            State.PurposeCode(actorId),
            selected.ActionCode,
            selected.TargetingCode,
            selected.PlaneCode == 0 ? State.PlaneCode(actorId) : selected.PlaneCode,
            selected.Q,
            selected.R,
            statusCode,
            selected.CellKindCode,
            selected.LocalRawMass,
            selected.ConnectedRawMass,
            selected.Score,
            delta);
    }

    private int EffectiveActorRadius(int actorId, int sourceWorkTier, in MinorActorVerbCandidate selected)
    {
        if (selected.EffectiveRadius > 0)
        {
            return selected.EffectiveRadius;
        }

        var sourceRule = State.SourceRuleId(actorId);
        return (uint)(sourceRule - 1) < (uint)Data.RuleCapacity
            ? Data.RuleMaxVerbRadii[sourceRule] + sourceWorkTier
            : 0;
    }

    private static bool TryResolveSourcePosition(
        WorldState world,
        int sourceEntityId,
        int planeCode,
        out int q,
        out int r)
    {
        if (sourceEntityId <= 0 || !world.EntityIsActive(sourceEntityId))
        {
            q = 0;
            r = 0;
            return false;
        }

        if (planeCode == PlaneCodes.Physical)
        {
            q = world.EntityPhysicalQ(sourceEntityId);
            r = world.EntityPhysicalR(sourceEntityId);
            return true;
        }

        if (planeCode == PlaneCodes.Semantic)
        {
            q = world.EntitySemanticQ(sourceEntityId);
            r = world.EntitySemanticR(sourceEntityId);
            return true;
        }

        q = 0;
        r = 0;
        return false;
    }

    private bool TryResolveSpawnPosition(
        Simulation simulation,
        WorldState world,
        int sourceEntityId,
        int anchorEntityId,
        int rule,
        out int q,
        out int r)
    {
        q = 0;
        r = 0;
        if (!TryResolveSourcePosition(world, anchorEntityId, Data.PlaneCodes[rule], out var sourceQ, out var sourceR))
        {
            return false;
        }

        if (anchorEntityId != sourceEntityId)
        {
            AdvanceAnchorToFrontier(world, sourceEntityId, Data.PlaneCodes[rule], ref sourceQ, ref sourceR);
        }

        return Data.PurposeCodes[rule] == MinorActorPurposeCodes.CampaignLabor
            ? TrySelectCampaignSpawnCell(
                simulation,
                world.EntityCultureId(sourceEntityId),
                Data.PlaneCodes[rule],
                sourceQ,
                sourceR,
                Data.SpawnRadii[rule],
                out q,
                out r)
            : TrySelectSpawnCell(
                simulation,
                world.EntityCultureId(sourceEntityId),
                Data.PlaneCodes[rule],
                sourceQ,
                sourceR,
                Data.SpawnRadii[rule],
                out q,
                out r);
    }

    private bool TrySelectSpawnCell(
        Simulation simulation,
        int cultureId,
        int planeCode,
        int sourceQ,
        int sourceR,
        int radius,
        out int q,
        out int r)
    {
        if (radius <= 0 || cultureId <= 0)
        {
            q = sourceQ;
            r = sourceR;
            return simulation.World.CellIsKnown(planeCode, q, r);
        }

        q = sourceQ;
        r = sourceR;
        var bestScore = simulation.World.CellIsKnown(planeCode, q, r) ? FixedScalar.FromUnits(10000L) : 0;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var candidateQ = sourceQ + dq;
                var candidateR = sourceR + dr;
                if (!simulation.World.CellIsKnown(planeCode, candidateQ, candidateR))
                {
                    continue;
                }

                var field = simulation.CultureLocality.CellAt(
                    simulation.World,
                    cultureId,
                    planeCode,
                    candidateQ,
                    candidateR);
                var score = field.ConnectedRawMass;
                if (score > bestScore)
                {
                    q = candidateQ;
                    r = candidateR;
                    bestScore = score;
                }
            }
        }

        return bestScore > 0;
    }

}
