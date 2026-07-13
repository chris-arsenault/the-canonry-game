namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramRuntime :
    IKernelProgramRuntime<MinorActorProgramData, MinorActorProgramState>,
    IKernelParallelJob
{
    private const int LocalVerbStepRadius = 1;
    private const int VerbTargetCapacity = 7;
    private const float CultureTraceRawInfluence = WorldState.CultureTraceRawMass;
    private const float CultureSupportedRawInfluence = WorldState.CultureControlledRawMass;
    private const float CultureStrongRawInfluence = WorldState.CultureEntrenchedRawMass;
    private const float CultureFieldRawInfluence = WorldState.CultureFullRawMass;
    private const float CultureForeignTraceToleranceShare = 10f;
    private const float CultureForeignContestScaleShare = 45f;
    private const float LifecycleMaintenanceCoverageScale = 0.03f;
    private const float LifecycleMaintenanceEarlyNeedScale = 0.20f;
    private const float LifecycleMaintenanceScoreScale = 1.35f;
    private const float NexusSourceCultureMultiplier = 1.5f;
    private static readonly int[] NeighborDQ = [1, 1, 0, -1, -1, 0];
    private static readonly int[] NeighborDR = [0, -1, -1, 0, 1, 1];
    private readonly MinorActorCellTarget[] _verbTargetCells;
    private readonly MinorActorDecision[] _parallelDecisions;
    private Simulation? _parallelSimulation;
    private int _parallelPulse;
    private int _parallelTick;

    public MinorActorProgramRuntime(MinorActorProgramData data, MinorActorProgramState state)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        Data = data;
        State = state;
        _verbTargetCells = new MinorActorCellTarget[
            SimulationOptions.MaximumParallelWorkerCount * VerbTargetCapacity];
        _parallelDecisions = new MinorActorDecision[state.ActorCapacity + 1];
    }

    public MinorActorProgramData Data { get; }

    public MinorActorProgramState State { get; }

    public int RegisterEmitterRule(
        int sourceKindId,
        int sourceSubtypeId,
        int actorKindCode,
        int actionCode,
        int planeCode,
        int ttlPulses,
        int emitCadencePulses,
        int maxActivePerSource,
        int spawnRadius,
        int moveChancePermille,
        int actionCadencePulses,
        float workDelta) =>
        RegisterEmitterRule(
            sourceKindId,
            sourceSubtypeId,
            MinorActorPurposeCodes.CulturalLabor,
            actorKindCode,
            actionCode,
            planeCode,
            ttlPulses,
            emitCadencePulses,
            maxActivePerSource,
            spawnRadius,
            moveChancePermille,
            actionCadencePulses,
            workDelta);

    public int RegisterEmitterRule(
        int sourceKindId,
        int sourceSubtypeId,
        int purposeCode,
        int actorKindCode,
        int actionCode,
        int planeCode,
        int ttlPulses,
        int emitCadencePulses,
        int maxActivePerSource,
        int spawnRadius,
        int moveChancePermille,
        int actionCadencePulses,
        float workDelta) =>
        Data.RegisterEmitterRule(
            sourceKindId,
            sourceSubtypeId,
            purposeCode,
            actorKindCode,
            actionCode,
            planeCode,
            ttlPulses,
            emitCadencePulses,
            maxActivePerSource,
            spawnRadius,
            moveChancePermille,
            actionCadencePulses,
            workDelta);

    public int RegisterVerb(
        int sourceRuleId,
        int verbId,
        int actionCode,
        int targetingCode,
        int planeCode,
        int radius,
        int sampleCount,
        float weight,
        float workDeltaMultiplier) =>
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
        int actionCode,
        int planeCode,
        int q,
        int r,
        int ttlPulses,
        int moveChancePermille,
        int actionCadencePulses,
        float workDelta,
        int tick,
        int purposeCode = MinorActorPurposeCodes.CulturalLabor) =>
        State.Spawn(
            cultureId,
            sourceEntityId,
            sourceRuleId,
            actorKindCode,
            actionCode,
            planeCode,
            q,
            r,
            ttlPulses,
            moveChancePermille,
            actionCadencePulses,
            workDelta,
            tick,
            State.Pulse,
            purposeCode);

    public int WorkTier(int sourceEntityId) => State.WorkTier(sourceEntityId);

    public int IncreaseWorkTier(int sourceEntityId, int maxTier) =>
        State.IncreaseWorkTier(sourceEntityId, maxTier);

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var pulse = State.AdvancePulse();
        EmitFromSources(simulation, tick, pulse);
        TickActors(simulation, tick, pulse);
    }

    private void EmitFromSources(Simulation simulation, int tick, int pulse)
    {
        if (Data.RuleCount == 0)
        {
            return;
        }

        var world = simulation.World;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!world.EntityIsActive(entityId))
            {
                continue;
            }

            for (var rule = 1; rule <= Data.RuleCount; rule++)
            {
                TryEmitFromSource(simulation, world, entityId, rule, tick, pulse);
            }
        }
    }

    private void TryEmitFromSource(
        Simulation simulation,
        WorldState world,
        int sourceEntityId,
        int rule,
        int tick,
        int pulse)
    {
        var slotMultiplier = simulation.CultureSlots.CausalSpendLimitMultiplier(sourceEntityId);
        var workTier = State.WorkTier(sourceEntityId);
        if (!Data.RuleMatches(world, sourceEntityId, rule) ||
            State.SourceActiveCount(sourceEntityId, rule) >= EffectiveMaxActivePerSource(rule, slotMultiplier, workTier) ||
            pulse < State.NextEmissionPulse(sourceEntityId, rule))
        {
            return;
        }

        State.SetNextEmissionPulse(sourceEntityId, rule, pulse + EffectiveEmitCadence(rule, slotMultiplier));
        if (!TryResolveSourcePosition(world, sourceEntityId, Data.PlaneCodes[rule], out var sourceQ, out var sourceR) ||
            !TrySelectSpawnCell(
                simulation,
                world.EntityCultureId(sourceEntityId),
                Data.PlaneCodes[rule],
                sourceQ,
                sourceR,
                Data.SpawnRadii[rule],
                out var q,
                out var r))
        {
            return;
        }

        _ = State.Spawn(
            world.EntityCultureId(sourceEntityId),
            sourceEntityId,
            rule,
            Data.ActorKindCodes[rule],
            Data.ActionCodes[rule],
            Data.PlaneCodes[rule],
            q,
            r,
            Data.TtlPulses[rule],
            Data.MoveChancePermille[rule],
            Data.ActionCadencePulses[rule],
            Data.WorkDeltas[rule],
            tick,
            pulse,
            Data.PurposeCodes[rule]);
    }

    private int EffectiveMaxActivePerSource(int rule, int slotMultiplier, int workTier) =>
        (Data.MaxActivePerSource[rule] * global::System.Math.Max(1, slotMultiplier)) +
        global::System.Math.Max(0, workTier);

    private int EffectiveEmitCadence(int rule, int slotMultiplier) =>
        global::System.Math.Max(1, Data.EmitCadencePulses[rule] / global::System.Math.Max(1, slotMultiplier));

    private void TickActors(Simulation simulation, int tick, int pulse)
    {
        if (State.HighWatermark == 0)
        {
            return;
        }

        var parallelReadsReady = simulation.CultureBoundaries.PrepareParallelReads(simulation);
        _parallelSimulation = simulation;
        _parallelTick = tick;
        _parallelPulse = pulse;
        var scheduler = parallelReadsReady
            ? simulation.WorkScheduler
            : InlineKernelWorkScheduler.Instance;
        scheduler.Run(
            this,
            startInclusive: 1,
            endExclusive: State.HighWatermark + 1,
            simulation.Options.ParallelMinimumChunkSize);
        _parallelSimulation = null;

        for (var actorId = 1; actorId <= State.HighWatermark; actorId++)
        {
            ApplyActorDecision(simulation, actorId, tick, _parallelDecisions[actorId]);
        }
    }

    void IKernelParallelJob.Execute(int workerIndex, int startInclusive, int endExclusive)
    {
        var simulation = _parallelSimulation ?? throw new global::System.InvalidOperationException();
        for (var actorId = startInclusive; actorId < endExclusive; actorId++)
        {
            var decision = EvaluateActorDecision(
                simulation,
                actorId,
                workerIndex,
                _parallelTick,
                _parallelPulse);
            _parallelDecisions[actorId] = decision;
            ApplyActorOwnedDecision(actorId, _parallelPulse, decision);
        }
    }

    private void ApplyActorOwnedDecision(int actorId, int pulse, in MinorActorDecision decision)
    {
        if (!decision.IsActive)
        {
            return;
        }

        if (decision.ActionDue)
        {
            State.SetNextActionPulse(actorId, pulse + State.ActionCadencePulses(actorId));
            if (decision.HasVerbLayer && decision.HasSelectedVerb)
            {
                State.SetPosition(actorId, decision.Selected.Q, decision.Selected.R);
            }
        }

        if (decision.Move)
        {
            State.SetPosition(actorId, decision.MoveQ, decision.MoveR);
        }

        State.SetRemainingPulses(actorId, global::System.Math.Max(0, decision.RemainingPulses));
    }

    private MinorActorDecision EvaluateActorDecision(
        Simulation simulation,
        int actorId,
        int workerIndex,
        int tick,
        int pulse)
    {
        if (!State.IsActive(actorId))
        {
            return default;
        }

        var random = simulation.CreateRandomCursor(
            KernelPhase.ActorDecision,
            KernelProgramCodes.MinorActors,
            actorId,
            purposeId: 1);
        var sourceRule = State.SourceRuleId(actorId);
        var hasVerbLayer = HasVerbLayer(sourceRule);
        var actionDue = pulse >= State.NextActionPulse(actorId);
        var selected = default(MinorActorVerbCandidate);
        var hasSelectedVerb = actionDue && hasVerbLayer &&
            TrySelectVerbCandidate(simulation, actorId, tick, workerIndex, ref random, out selected);
        var moveQ = 0;
        var moveR = 0;
        var move = !hasVerbLayer && TryPlanRandomMove(simulation, actorId, ref random, out moveQ, out moveR);
        return new MinorActorDecision(
            true,
            actionDue,
            hasVerbLayer,
            hasSelectedVerb,
            selected,
            move,
            moveQ,
            moveR,
            State.RemainingPulses(actorId) - 1);
    }

    private void ApplyActorDecision(
        Simulation simulation,
        int actorId,
        int tick,
        in MinorActorDecision decision)
    {
        if (!decision.IsActive)
        {
            return;
        }

        if (decision.ActionDue)
        {
            if (decision.HasVerbLayer)
            {
                ApplySelectedVerb(simulation, actorId, tick, decision.HasSelectedVerb, decision.Selected);
            }
            else
            {
                ApplyLegacyWork(simulation, actorId, tick);
            }
        }

        if (decision.RemainingPulses <= 0)
        {
            _ = State.Deactivate(actorId);
        }
    }

    private bool HasVerbLayer(int sourceRule)
        => (uint)(sourceRule - 1) < (uint)Data.RuleCapacity && Data.RuleHasVerbs[sourceRule];

    private void ApplySelectedVerb(
        Simulation simulation,
        int actorId,
        int tick,
        bool hasSelected,
        in MinorActorVerbCandidate selected)
    {
        if (!hasSelected)
        {
            EmitMinorActorVerb(simulation, actorId, tick, default, TraceStatusCodes.NoTarget, delta: 0f);
            return;
        }

        var delta = State.WorkDelta(actorId) *
            selected.WorkDeltaMultiplier *
            SourceSlotWorkMultiplier(simulation, State.SourceEntityId(actorId)) *
            NexusSourceMultiplier(simulation, actorId, selected.PlaneCode);
        if (delta <= 0f)
        {
            EmitMinorActorVerb(simulation, actorId, tick, selected, TraceStatusCodes.NoReward, delta);
            return;
        }

        var appliedDelta = ApplySelectedVerbAction(simulation, actorId, selected, delta, tick);

        if (appliedDelta <= 0f)
        {
            EmitMinorActorVerb(simulation, actorId, tick, selected, TraceStatusCodes.NoReward, appliedDelta);
            return;
        }

        EmitMinorActorVerb(simulation, actorId, tick, selected, TraceStatusCodes.Selected, appliedDelta);
    }

    private float ApplySelectedVerbAction(
        Simulation simulation,
        int actorId,
        in MinorActorVerbCandidate selected,
        float delta,
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
                return 0f;
        }
    }

    private static float SourceSlotWorkMultiplier(Simulation simulation, int sourceEntityId)
    {
        var slotMultiplier = simulation.CultureSlots.CausalSpendLimitMultiplier(sourceEntityId);
        return 1f + ((global::System.Math.Max(1, slotMultiplier) - 1) * 0.5f);
    }

    private float NexusSourceMultiplier(Simulation simulation, int actorId, int planeCode)
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
            return 1f;
        }

        var sourceRaw = simulation.World.GetCellCultureInfluenceRaw(cultureId, planeCode, sourceQ, sourceR);
        return sourceRaw >= WorldState.CultureNexusRawMass
            ? NexusSourceCultureMultiplier
            : 1f;
    }

    private bool TrySelectVerbCandidate(
        Simulation simulation,
        int actorId,
        int tick,
        int workerIndex,
        ref KernelRandomCursor random,
        out MinorActorVerbCandidate selected)
    {
        selected = default;
        var totalScore = 0f;
        var sourceRule = State.SourceRuleId(actorId);
        var processedPlaneMask = 0;
        for (var verb = 1; verb <= Data.VerbCount; verb++)
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
                    workerIndex,
                    ref random,
                    out var candidate))
            {
                continue;
            }

            if (TrySelectWeightedCandidate(ref random, candidate, ref totalScore))
            {
                selected = candidate;
            }
        }

        return totalScore > 0f;
    }

    private static int PlaneBit(int planeCode) =>
        planeCode == PlaneCodes.Physical ? 1 : 2;

    private bool TrySelectVerbCandidateForPlane(
        Simulation simulation,
        int actorId,
        int sourceRule,
        int planeCode,
        int tick,
        int workerIndex,
        ref KernelRandomCursor random,
        out MinorActorVerbCandidate selected)
    {
        selected = default;
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

        var sourceWorkTier = State.WorkTier(State.SourceEntityId(actorId));
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
            workerIndex * VerbTargetCapacity,
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
            workerIndex * VerbTargetCapacity,
            ref random,
            out selected);
    }

    private bool TryScoreVerbTargetsForPlane(
        Simulation simulation,
        int actorId,
        int sourceRule,
        int planeCode,
        int sourceWorkTier,
        int targetCount,
        int tick,
        int targetOffset,
        ref KernelRandomCursor random,
        out MinorActorVerbCandidate selected)
    {
        selected = default;
        var totalScore = 0f;
        for (var verb = 1; verb <= Data.VerbCount; verb++)
        {
            if (!VerbSelectableForRule(verb, sourceRule) ||
                Data.VerbPlaneCodes[verb] != planeCode)
            {
                continue;
            }

            var operatingRadius = Data.VerbRadii[verb] + sourceWorkTier;
            for (var i = 0; i < targetCount; i++)
            {
                var target = _verbTargetCells[targetOffset + i];
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
                    tick);
                if (TrySelectWeightedCandidate(ref random, candidate, ref totalScore))
                {
                    selected = candidate;
                }
            }
        }

        return totalScore > 0f;
    }

    private static bool TrySelectWeightedCandidate(
        ref KernelRandomCursor random,
        in MinorActorVerbCandidate candidate,
        ref float totalScore)
    {
        if (candidate.Score <= 0f)
        {
            return false;
        }

        totalScore += candidate.Score;
        return random.NextFloat() * totalScore <= candidate.Score;
    }

    private bool VerbSelectableForRule(int verb, int sourceRule) =>
        Data.VerbSourceRuleIds[verb] == sourceRule && Data.VerbWeights[verb] > 0f;

    private int MaxVerbOperatingRadius(int sourceRule, int planeCode, int sourceWorkTier)
    {
        var radius = 0;
        for (var verb = 1; verb <= Data.VerbCount; verb++)
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
        int targetOffset,
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

                targets[targetOffset + count] = BuildVerbTargetCell(
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
        var boundary = simulation.CultureBoundaries.CellAt(simulation.World, cultureId, planeCode, q, r);
        var cultureField = new MinorActorCultureField(
            boundary.RawMass,
            boundary.LocalRawMass,
            boundary.ConnectedRawMass,
            boundary.SupportedAdjacentCount,
            boundary.SupportedRingTwoCount);
        var attachment = new MinorActorCultureAttachment(
            boundary.AttachedComponentScoreMass,
            boundary.SupportedAdjacentCount,
            boundary.AttachedComponentCount,
            boundary.IsLargestComponent ? 1f : 0f);
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
        int tick)
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
            tick);

        if (score <= 0f)
        {
            return default;
        }

        var distanceBias = 1f / (1f + (0.1f * target.DistanceFromActor));
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

    private void ApplyLegacyWork(Simulation simulation, int actorId, int tick)
    {
        var workDelta = State.WorkDelta(actorId);
        if (workDelta <= 0f)
        {
            return;
        }

        var planeCode = State.PlaneCode(actorId);
        var q = State.Q(actorId);
        var r = State.R(actorId);
        if (!simulation.World.CellIsKnown(planeCode, q, r))
        {
            return;
        }

        switch (State.ActionCode(actorId))
        {
            case MinorActorActionCodes.CultureExpansion:
                _ = ApplyCultureExpansion(
                    simulation,
                    actorId,
                    planeCode,
                    q,
                    r,
                    CultureInfluenceActionDelta(simulation, workDelta),
                    tick);
                break;
            case MinorActorActionCodes.RoadMaintenance:
                _ = simulation.Movement.ReinforceRoad(simulation, planeCode, q, r, workDelta, tick);
                break;
            case MinorActorActionCodes.CultureCleanup:
                _ = ReduceOtherCultureSources(
                    simulation,
                    State.CultureId(actorId),
                    planeCode,
                    q,
                    r,
                    CultureInfluenceActionDelta(simulation, workDelta),
                    tick);
                break;
        }
    }

    private static float CultureInfluenceActionDelta(Simulation simulation, float delta) =>
        delta * simulation.Options.KernelTuning.Programs.CultureInfluenceActionScale;

    private float ApplyCultureExpansion(
        Simulation simulation,
        int actorId,
        int planeCode,
        int q,
        int r,
        float delta,
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

    private static float ReduceOtherCultureSources(
        Simulation simulation,
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        float delta,
        int tick)
    {
        return simulation.ReduceOtherCultureSourcesAtCell(ownCultureId, planeCode, q, r, delta, tick);
    }

    private static float ApplyLifecycleMaintenance(
        Simulation simulation,
        int sourceEntityId,
        float coverage,
        int tick)
    {
        if (coverage <= 0f ||
            !simulation.Upkeep.TryGetLifecycleMaintenanceNeed(
                simulation,
                sourceEntityId,
                tick,
                out var attributeId,
                out var ageDrainComponent,
                out _))
        {
            return 0f;
        }

        var delta = ageDrainComponent * coverage * LifecycleMaintenanceCoverageScale;
        if (delta <= 0f)
        {
            return 0f;
        }

        var previous = simulation.World.GetAttribute(sourceEntityId, attributeId);
        simulation.ModifyAttribute(sourceEntityId, attributeId, delta, tick);
        return simulation.World.GetAttribute(sourceEntityId, attributeId) - previous;
    }

    private bool TryPlanRandomMove(
        Simulation simulation,
        int actorId,
        ref KernelRandomCursor random,
        out int q,
        out int r)
    {
        q = State.Q(actorId);
        r = State.R(actorId);
        var moveChancePermille = State.MoveChancePermille(actorId);
        if (moveChancePermille <= 0 || random.NextInt(1000) >= moveChancePermille)
        {
            return false;
        }

        var direction = random.NextInt(6);
        q += NeighborDQ[direction];
        r += NeighborDR[direction];
        return simulation.World.CellIsKnown(State.PlaneCode(actorId), q, r);
    }

    private void EmitMinorActorVerb(
        Simulation simulation,
        int actorId,
        int tick,
        in MinorActorVerbCandidate selected,
        int statusCode,
        float delta)
    {
        var sourceEntityId = State.SourceEntityId(actorId);
        var sourceWorkTier = State.WorkTier(sourceEntityId);
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
        var bestScore = simulation.World.CellIsKnown(planeCode, q, r) ? 0.01f : 0f;
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

                var field = simulation.CultureBoundaries.CellAt(
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

        return bestScore > 0f;
    }

}
