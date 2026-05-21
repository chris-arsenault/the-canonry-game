namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramRuntime :
    IKernelProgramRuntime<MinorActorProgramData, MinorActorProgramState>
{
    private const int LocalVerbStepRadius = 1;
    private const float CultureTraceRawInfluence = 2.5f;
    private const float CultureSupportedRawInfluence = 12f;
    private const float CultureStrongRawInfluence = 30f;
    private const float CultureFieldRawInfluence = 180f;
    private const float CultureRingTwoConnectionWeight = 7f / 20f;
    private const float CultureForeignTraceToleranceShare = 10f;
    private const float CultureForeignContestScaleShare = 45f;
    private const float CultureFieldForeignContestShare = 25f;
    private const float CultureFieldDominantOwnShare = 65f;
    private const float CultureFieldMajorForeignContestShare = 35f;
    private const float LifecycleMaintenanceCoverageScale = 0.03f;
    private const int CultureAdjacentNeighborCount = 6;
    private static readonly int[] NeighborDQ = [1, 1, 0, -1, -1, 0];
    private static readonly int[] NeighborDR = [0, -1, -1, 0, 1, 1];
    private static readonly int[] CultureNeighborDQ =
    [
        1, 1, 0, -1, -1, 0,
        2, 2, 2, 1, 0, -1, -2, -2, -2, -1, 0, 1,
    ];

    private static readonly int[] CultureNeighborDR =
    [
        0, -1, -1, 0, 1, 1,
        0, -1, -2, -2, -2, -1, 0, 1, 2, 2, 2, 1,
    ];

    private readonly CultureInfluenceComponentScratch _cultureInfluenceComponents = new();

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
        for (var actorId = 1; actorId <= State.HighWatermark; actorId++)
        {
            if (State.IsActive(actorId))
            {
                TickActor(simulation, actorId, tick, pulse);
            }
        }
    }

    private void TickActor(Simulation simulation, int actorId, int tick, int pulse)
    {
        var sourceRule = State.SourceRuleId(actorId);
        var hasVerbLayer = HasVerbLayer(sourceRule);
        if (pulse >= State.NextActionPulse(actorId))
        {
            if (hasVerbLayer)
            {
                ApplySelectedVerb(simulation, actorId, tick);
            }
            else
            {
                ApplyLegacyWork(simulation, actorId, tick);
            }

            State.SetNextActionPulse(actorId, pulse + State.ActionCadencePulses(actorId));
        }

        if (!hasVerbLayer)
        {
            TryMoveRandomly(simulation, actorId);
        }

        var remaining = State.RemainingPulses(actorId) - 1;
        if (remaining <= 0)
        {
            _ = State.Deactivate(actorId);
            return;
        }

        State.SetRemainingPulses(actorId, remaining);
    }

    private bool HasVerbLayer(int sourceRule)
    {
        if (sourceRule <= 0)
        {
            return false;
        }

        for (var verb = 1; verb <= Data.VerbCount; verb++)
        {
            if (Data.VerbSourceRuleIds[verb] == sourceRule)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplySelectedVerb(Simulation simulation, int actorId, int tick)
    {
        if (!TrySelectVerbCandidate(simulation, actorId, tick, out var selected))
        {
            EmitMinorActorVerb(simulation, actorId, tick, default, TraceStatusCodes.NoTarget, delta: 0f);
            return;
        }

        State.SetPosition(actorId, selected.Q, selected.R);
        var delta = State.WorkDelta(actorId) *
            selected.WorkDeltaMultiplier *
            SourceSlotWorkMultiplier(simulation, State.SourceEntityId(actorId));
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
                return ApplyCultureExpansion(simulation, actorId, selected.PlaneCode, selected.Q, selected.R, delta, tick);
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
                    delta,
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

    private bool TrySelectVerbCandidate(
        Simulation simulation,
        int actorId,
        int tick,
        out MinorActorVerbCandidate selected)
    {
        selected = default;
        var bestScore = 0f;
        var sourceRule = State.SourceRuleId(actorId);
        for (var verb = 1; verb <= Data.VerbCount; verb++)
        {
            if (Data.VerbSourceRuleIds[verb] != sourceRule ||
                Data.VerbWeights[verb] <= 0f)
            {
                continue;
            }

            if (!TrySelectVerbCandidate(simulation, actorId, verb, tick, out var candidate))
            {
                continue;
            }

            if (candidate.Score > bestScore)
            {
                selected = candidate;
                bestScore = candidate.Score;
            }
        }

        return bestScore > 0f;
    }

    private bool TrySelectVerbCandidate(
        Simulation simulation,
        int actorId,
        int verb,
        int tick,
        out MinorActorVerbCandidate selected)
    {
        selected = default;
        var planeCode = Data.VerbPlaneCodes[verb];
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

        _cultureInfluenceComponents.Build(simulation.World, State.CultureId(actorId), planeCode);
        var operatingRadius = Data.VerbRadii[verb] + State.WorkTier(State.SourceEntityId(actorId));
        var radius = operatingRadius <= 0 ? 0 : LocalVerbStepRadius;
        var bestScore = 0f;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = actorQ + dq;
                var r = actorR + dr;
                if (!simulation.World.CellIsKnown(planeCode, q, r) ||
                    HexDistance(sourceQ, sourceR, q, r) > operatingRadius)
                {
                    continue;
                }

                var candidate = ScoreVerbTarget(
                    simulation,
                    actorId,
                    verb,
                    planeCode,
                    q,
                    r,
                    actorQ,
                    actorR,
                    operatingRadius,
                    tick);
                if (candidate.Score > bestScore)
                {
                    selected = candidate;
                    bestScore = candidate.Score;
                }
            }
        }

        return bestScore > 0f;
    }

    private bool TryResolveActorPosition(int actorId, int planeCode, out int q, out int r)
    {
        q = State.Q(actorId);
        r = State.R(actorId);
        return State.PlaneCode(actorId) == planeCode;
    }

    private MinorActorVerbCandidate ScoreVerbTarget(
        Simulation simulation,
        int actorId,
        int verb,
        int planeCode,
        int q,
        int r,
        int centerQ,
        int centerR,
        int effectiveRadius,
        int tick)
    {
        var world = simulation.World;
        var cultureId = State.CultureId(actorId);
        var ownShare = world.GetCellCultureInfluence(cultureId, planeCode, q, r);
        var ownRaw = world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
        var neighborhood = CultureNeighborhood(world, cultureId, planeCode, q, r);
        var attachment = _cultureInfluenceComponents.AttachmentAt(q, r);
        var foreignShare = StrongestOtherCultureInfluence(world, cultureId, planeCode, q, r, out var foreignCultureId);
        var foreignRaw = foreignCultureId > 0
            ? world.GetCellCultureInfluenceRaw(foreignCultureId, planeCode, q, r)
            : StrongestOtherCultureRaw(world, cultureId, planeCode, q, r, out _);
        var cellKindCode = CultureFieldCellKind(ownRaw, ownShare, foreignShare, neighborhood.SupportedAdjacentCount, neighborhood.ConnectedRaw);
        var score = MinorVerbScore(
            simulation,
            actorId,
            verb,
            planeCode,
            q,
            r,
            ownShare,
            ownRaw,
            foreignShare,
            foreignRaw,
            neighborhood,
            attachment,
            tick);

        if (score <= 0f)
        {
            return default;
        }

        var distance = HexDistance(centerQ, centerR, q, r);
        var distanceBias = 1f / (1f + (0.1f * distance));
        score *= Data.VerbWeights[verb] * distanceBias;
        return new MinorActorVerbCandidate(
            Data.VerbIds[verb],
            Data.VerbActionCodes[verb],
            Data.VerbTargetingCodes[verb],
            planeCode,
            q,
            r,
            cellKindCode,
            effectiveRadius,
            neighborhood.LocalRaw,
            neighborhood.ConnectedRaw,
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
                _ = ApplyCultureExpansion(simulation, actorId, planeCode, q, r, workDelta, tick);
                break;
            case MinorActorActionCodes.RoadMaintenance:
                _ = simulation.Movement.ReinforceRoad(simulation, planeCode, q, r, workDelta, tick);
                break;
            case MinorActorActionCodes.CultureCleanup:
                _ = ReduceOtherCultureSources(simulation, State.CultureId(actorId), planeCode, q, r, workDelta, tick);
                break;
        }
    }

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

        return simulation.World.ModifyCellCultureInfluenceRaw(
            State.CultureId(actorId),
            planeCode,
            q,
            r,
            delta,
            tick,
            simulation.ExecutionContext);
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

    private void TryMoveRandomly(Simulation simulation, int actorId)
    {
        var moveChancePermille = State.MoveChancePermille(actorId);
        if (moveChancePermille <= 0 || simulation.NextInt(1000) >= moveChancePermille)
        {
            return;
        }

        var direction = simulation.NextInt(6);
        var q = State.Q(actorId) + NeighborDQ[direction];
        var r = State.R(actorId) + NeighborDR[direction];
        if (simulation.World.CellIsKnown(State.PlaneCode(actorId), q, r))
        {
            State.SetPosition(actorId, q, r);
        }
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
        var maxRadius = 0;
        for (var verb = 1; verb <= Data.VerbCount; verb++)
        {
            if (Data.VerbSourceRuleIds[verb] == sourceRule)
            {
                maxRadius = global::System.Math.Max(maxRadius, Data.VerbRadii[verb] + sourceWorkTier);
            }
        }

        return maxRadius;
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

                var raw = simulation.World.GetCellCultureInfluenceRaw(cultureId, planeCode, candidateQ, candidateR);
                var neighborhood = CultureNeighborhood(simulation.World, cultureId, planeCode, candidateQ, candidateR);
                var score = raw + neighborhood.AdjacentRawTotal + (neighborhood.RingTwoRawTotal * CultureRingTwoConnectionWeight);
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

    private static CultureInfluenceNeighborhood CultureNeighborhood(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r)
    {
        var centerRaw = world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
        var adjacentRawTotal = 0f;
        var supportedAdjacentCount = 0;
        var ringTwoRawTotal = 0f;
        for (var i = 0; i < CultureNeighborDQ.Length; i++)
        {
            var raw = world.GetCellCultureInfluenceRaw(
                cultureId,
                planeCode,
                q + CultureNeighborDQ[i],
                r + CultureNeighborDR[i]);
            if (i < CultureAdjacentNeighborCount)
            {
                adjacentRawTotal += raw;
                supportedAdjacentCount += raw >= CultureSupportedRawInfluence ? 1 : 0;
                continue;
            }

            ringTwoRawTotal += raw;
        }

        return new CultureInfluenceNeighborhood(centerRaw, adjacentRawTotal, supportedAdjacentCount, ringTwoRawTotal);
    }

    private static int CultureFieldCellKind(
        float raw,
        float share,
        float foreignShare,
        int supportedAdjacentCount,
        float connectedRaw)
    {
        if (CultureFieldIsEmpty(raw, share, connectedRaw))
        {
            return CultureFieldCellKindCodes.None;
        }

        if (CultureFieldIsContested(raw, share, foreignShare))
        {
            return CultureFieldCellKindCodes.Contested;
        }

        if (CultureFieldIsInterior(raw, supportedAdjacentCount))
        {
            return CultureFieldCellKindCodes.Interior;
        }

        if (CultureFieldIsIsolated(raw, supportedAdjacentCount))
        {
            return CultureFieldCellKindCodes.Isolated;
        }

        return CultureFieldIsBorder(raw, supportedAdjacentCount)
            ? CultureFieldCellKindCodes.Border
            : CultureFieldCellKindCodes.None;
    }

    private static bool CultureFieldIsEmpty(float raw, float share, float connectedRaw) =>
        raw < CultureTraceRawInfluence &&
        share <= 0f &&
        connectedRaw < CultureSupportedRawInfluence;

    private static bool CultureFieldIsContested(float raw, float share, float foreignShare) =>
        raw >= CultureTraceRawInfluence &&
        foreignShare >= CultureFieldForeignContestShare &&
        (share < CultureFieldDominantOwnShare || foreignShare >= CultureFieldMajorForeignContestShare);

    private static bool CultureFieldIsInterior(float raw, int supportedAdjacentCount) =>
        raw >= CultureStrongRawInfluence && supportedAdjacentCount >= 5;

    private static bool CultureFieldIsIsolated(float raw, int supportedAdjacentCount) =>
        raw >= CultureTraceRawInfluence && supportedAdjacentCount == 0;

    private static bool CultureFieldIsBorder(float raw, int supportedAdjacentCount) =>
        raw >= CultureTraceRawInfluence || supportedAdjacentCount > 0;

    private static float StrongestOtherCultureInfluence(
        WorldState world,
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        out int cultureId)
    {
        cultureId = 0;
        var influence = 0f;
        for (var otherCultureId = 1; otherCultureId <= world.CultureHighWatermark; otherCultureId++)
        {
            if (otherCultureId == ownCultureId)
            {
                continue;
            }

            var candidateInfluence = world.GetCellCultureInfluence(otherCultureId, planeCode, q, r);
            if (candidateInfluence <= influence)
            {
                continue;
            }

            influence = candidateInfluence;
            cultureId = otherCultureId;
        }

        return influence;
    }

    private static float StrongestOtherCultureRaw(
        WorldState world,
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        out int cultureId)
    {
        cultureId = 0;
        var raw = 0f;
        for (var otherCultureId = 1; otherCultureId <= world.CultureHighWatermark; otherCultureId++)
        {
            if (otherCultureId == ownCultureId)
            {
                continue;
            }

            var candidateRaw = world.GetCellCultureInfluenceRaw(otherCultureId, planeCode, q, r);
            if (candidateRaw <= raw)
            {
                continue;
            }

            raw = candidateRaw;
            cultureId = otherCultureId;
        }

        return raw;
    }

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        return (global::System.Math.Abs(aq - bq) +
            global::System.Math.Abs(ar - br) +
            global::System.Math.Abs(asCoord - bsCoord)) / 2;
    }

    private static float Saturating(float value, float scale) =>
        value <= 0f ? 0f : 1f - global::System.MathF.Exp(-value / scale);

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private readonly record struct CultureInfluenceNeighborhood(
        float CenterRaw,
        float AdjacentRawTotal,
        int SupportedAdjacentCount,
        float RingTwoRawTotal)
    {
        public float LocalRaw => CenterRaw + AdjacentRawTotal;

        public float ConnectedRaw => LocalRaw + (RingTwoRawTotal * CultureRingTwoConnectionWeight);
    }

    private readonly record struct MinorActorVerbCandidate(
        int VerbId,
        int ActionCode,
        int TargetingCode,
        int PlaneCode,
        int Q,
        int R,
        int CellKindCode,
        int EffectiveRadius,
        float LocalRawMass,
        float ConnectedRawMass,
        float Score,
        float WorkDeltaMultiplier);
}
