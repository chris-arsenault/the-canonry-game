namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime :
    IKernelProgramRuntime<LowAltitudeVerbProgramData, LowAltitudeVerbProgramState>
{
    private readonly VerbTargetCandidate[] _verbCandidateScratch;
    private readonly FixedScalar[] _verbAffordanceSumScratch;

    public LowAltitudeVerbProgramRuntime(LowAltitudeVerbProgramData data, LowAltitudeVerbProgramState state)
        : this(data, state, new KernelProgramTuningOptions())
    {
    }

    public LowAltitudeVerbProgramRuntime(
        LowAltitudeVerbProgramData data,
        LowAltitudeVerbProgramState state,
        KernelProgramTuningOptions tuning)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        Data = data;
        State = state;
        Tuning = tuning;
        _verbCandidateScratch = new VerbTargetCandidate[data.RuleCapacity];
        _verbAffordanceSumScratch = new FixedScalar[data.RuleCapacity];
    }

    public LowAltitudeVerbProgramData Data { get; }

    public void ResetAgeWeightMultipliers() => Data.ResetAgeWeightMultipliers();

    public void ApplyAgeWeightMultiplier(int verbId, FixedScalar multiplier) => Data.ApplyAgeWeightMultiplier(verbId, multiplier);

    public LowAltitudeVerbProgramState State { get; }

    public KernelProgramTuningOptions Tuning { get; }

    public int RegisterVerb(
        int kindId,
        int verbId,
        int planeCode,
        int operationCode,
        int targetModeCode,
        int targetKindId,
        int relationshipKindId,
        int relationshipDirectionCode,
        int minCommitmentTicks,
        int maxCommitmentTicks,
        int dwellTicks,
        int targetRadius,
        int candidateSampleCount,
        FixedScalar weight,
        FixedScalar opportunityBonusOverride = default,
        FixedScalar fitnessWeight = default,
        FixedScalar distanceScaleOverride = default,
        int fitnessRelationshipKindId = 0,
        int fitnessRelationshipDirectionCode = LowAltitudeVerbRelationshipDirectionCodes.Either,
        int fitnessPolarityCode = AffordancePolarityCodes.None,
        FixedScalar fitnessThreshold = default,
        FixedScalar influenceDelta = default,
        int influenceRadius = 0,
        FixedScalar coverageGrowthChance = default,
        FixedScalar headquartersMoveChance = default,
        FixedScalar requiredInfluence = default,
        int foreignInfluenceFilterEnabled = 0,
        int foreignInfluenceBlockPositiveRelationships = 0,
        int foreignInfluenceAllowNegativeRelationships = 0,
        FixedScalar foreignInfluenceMinimumRelationshipStrength = default,
        FixedScalar? foreignInfluenceAllowUnrelatedInfluenceAtOrAbove = null,
        FixedScalar relationshipStrengthDelta = default,
        int influenceTargetingCode = LowAltitudeInfluenceTargetingCodes.None) =>
        Data.RegisterVerb(new LowAltitudeVerbRegistration
        {
            KindId = kindId,
            VerbId = verbId,
            PlaneCode = planeCode,
            OperationCode = operationCode,
            InfluenceTargetingCode = influenceTargetingCode,
            TargetModeCode = targetModeCode,
            TargetKindId = targetKindId,
            RelationshipKindId = relationshipKindId,
            RelationshipDirectionCode = relationshipDirectionCode,
            MinTicks = minCommitmentTicks,
            MaxTicks = maxCommitmentTicks,
            DwellTicks = dwellTicks,
            TargetRadius = targetRadius,
            CandidateSampleCount = candidateSampleCount,
            Weight = weight,
            OpportunityBonusOverride = opportunityBonusOverride,
            FitnessWeight = fitnessWeight,
            DistanceScaleOverride = distanceScaleOverride,
            ForeignInfluenceFilterEnabled = foreignInfluenceFilterEnabled,
            ForeignInfluenceBlockPositiveRelationships = foreignInfluenceBlockPositiveRelationships,
            ForeignInfluenceAllowNegativeRelationships = foreignInfluenceAllowNegativeRelationships,
            ForeignInfluenceMinimumRelationshipStrength = foreignInfluenceMinimumRelationshipStrength,
            ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove =
                foreignInfluenceAllowUnrelatedInfluenceAtOrAbove ?? -FixedScalar.One,
            FitnessRelationshipKindId = fitnessRelationshipKindId,
            FitnessRelationshipDirectionCode = fitnessRelationshipDirectionCode,
            FitnessPolarityCode = fitnessPolarityCode,
            FitnessThreshold = fitnessThreshold,
            InfluenceDelta = influenceDelta,
            InfluenceRadius = influenceRadius,
            CoverageChance = coverageGrowthChance,
            RelocationChance = headquartersMoveChance,
            RequiredMapValue = requiredInfluence,
            RelationshipStrengthDelta = relationshipStrengthDelta,
        });

    public void AddTerrainFilter(int verbSlot, int terrainId) =>
        Data.AddTerrainFilter(verbSlot, terrainId);

    public void SetGeographyFilter(int verbSlot, int minTemperature, int siteFlags, int fogAdjacent = 0) =>
        Data.SetGeographyFilter(verbSlot, minTemperature, siteFlags, fogAdjacent);

    public void SetRestAtDestination(int verbSlot) =>
        Data.SetRestAtDestination(verbSlot);

    public int RegisterInitialInfluence(int kindId, int planeCode, FixedScalar influenceValue) =>
        Data.RegisterInitialInfluence(kindId, planeCode, influenceValue);

    public void AddPrimitiveStep(int rule, int opcode, int intArgument, FixedScalar scalarArgument) =>
        Data.AddPrimitiveStep(rule, opcode, intArgument, scalarArgument);

    public int RegisterAffordance(
        int sourceKindCode,
        int sourceId,
        int shapeCode,
        int actorKindId,
        int planeCode,
        int candidateKindId,
        int relationshipKindId,
        int relationshipDirectionCode,
        int operationCode,
        int polarityCode,
        FixedScalar threshold,
        FixedScalar weight) =>
        Data.RegisterAffordance(
            sourceKindCode,
            sourceId,
            shapeCode,
            actorKindId,
            planeCode,
            candidateKindId,
            relationshipKindId,
            relationshipDirectionCode,
            operationCode,
            polarityCode,
            threshold,
            weight);

    public int RegisterTraitWeightModifier(
        int kindId,
        int traitId,
        int verbId,
        FixedScalar weightDelta,
        FixedScalar opportunityBonusDelta,
        FixedScalar fitnessWeightDelta) =>
        Data.RegisterTraitWeightModifier(
            kindId,
            traitId,
            verbId,
            weightDelta,
            opportunityBonusDelta,
            fitnessWeightDelta);

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        for (var entityId = world.FirstActiveEntity();
             entityId > 0;
             entityId = world.NextActiveEntity(entityId))
        {
            ApplyRelationshipFollowMovementIntent(simulation, entityId, tick);

            if (ApplyInitialInfluenceIfNeeded(simulation, entityId, tick))
            {
                continue;
            }

            if (ShouldChooseIntent(simulation, entityId, tick))
            {
                ChooseIntent(simulation, entityId, tick);
            }

            var slot = State.ActiveVerbSlot(entityId);
            if (slot != 0)
            {
                ApplyIntent(simulation, entityId, slot, tick);
            }
        }
    }

    private bool ApplyInitialInfluenceIfNeeded(Simulation simulation, int entityId, int tick)
    {
        if (State.InitialInfluenceApplied(entityId))
        {
            return false;
        }

        var applied = false;
        var kindId = simulation.World.EntityKindId(entityId);
        for (var rule = Data.FirstInitialInfluenceForKind(kindId);
             rule > 0;
             rule = Data.NextInitialInfluenceForKind(rule))
        {
            if (ApplyInitialInfluenceRule(simulation, entityId, rule, tick))
            {
                applied = true;
            }
        }

        State.MarkInitialInfluenceApplied(entityId);
        return applied;
    }

    private bool ApplyInitialInfluenceRule(Simulation simulation, int entityId, int rule, int tick)
    {
        var planeCode = Data.InitialInfluencePlaneCode(rule);
        var q = CurrentQ(simulation.World, entityId, planeCode);
        var r = CurrentR(simulation.World, entityId, planeCode);
        if (!simulation.World.CellIsKnown(planeCode, q, r))
        {
            return false;
        }

        var influenceValue = Data.InitialInfluenceValue(rule);
        simulation.ModifyCellCultureInfluenceRawFromEntity(entityId, planeCode, q, r, influenceValue, tick);
        return true;
    }

    private bool ShouldChooseIntent(Simulation simulation, int entityId, int tick)
    {
        var slot = State.ActiveVerbSlot(entityId);
        if (slot == 0)
        {
            return true;
        }

        if (IntentInvalidated(simulation, entityId, slot, tick))
        {
            ClearIntentAndPushRecent(entityId, tick);
            return true;
        }

        var reachedTick = State.ReachedTick(entityId);
        if (reachedTick == 0)
        {
            return false;
        }

        if (tick >= State.DwellUntilTick(entityId))
        {
            ClearIntentAndPushRecent(entityId, tick);
            return true;
        }

        return false;
    }

    private bool IntentInvalidated(Simulation simulation, int entityId, int slot, int tick)
    {
        var targetEntityId = State.TargetEntityId(entityId);
        if (targetEntityId != 0 && !simulation.World.EntityIsActive(targetEntityId))
        {
            return true;
        }

        if (PhysicalMovementSuspendedByCarry(simulation, entityId, slot))
        {
            return true;
        }

        if (targetEntityId != 0 &&
            Data.TargetModeCode(slot) == TargetSelectorCodes.RelationshipTarget &&
            !HasTargetRelationship(simulation.World, entityId, targetEntityId, slot))
        {
            return true;
        }

        return tick >= State.ExpiresTick(entityId);
    }

    private void ClearIntentAndPushRecent(int entityId, int tick)
    {
        var key = ResolveActiveTargetKey(entityId);
        if (key != 0L)
        {
            State.PushRecentTarget(entityId, key, tick);
        }

        State.ClearIntent(entityId);
    }

    private long ResolveActiveTargetKey(int entityId)
    {
        var slot = State.ActiveVerbSlot(entityId);
        if (slot == 0)
        {
            return 0L;
        }

        var targetEntityId = State.TargetEntityId(entityId);
        if (targetEntityId != 0)
        {
            return LowAltitudeVerbProgramState.PackEntityTargetKey(targetEntityId);
        }

        var planeCode = State.TargetPlaneCode(entityId);
        return LowAltitudeVerbProgramState.PackCellTargetKey(planeCode, State.TargetQ(entityId), State.TargetR(entityId));
    }

    // A carried entity's position belongs to its carrier: while an incoming
    // follow relationship governs it, its own physical movement will is
    // suspended so the two programs cannot fight over the same coordinate
    // tick by tick.
    private bool PhysicalMovementSuspendedByCarry(Simulation simulation, int entityId, int rule) =>
        Data.OperationCode(rule) == ProgramOpcodeCodes.MoveActor &&
        Data.PlaneCode(rule) == PlaneCodes.Physical &&
        simulation.Relationships.TryResolveFollowSource(simulation.World, entityId, out _);

    private static void ApplyRelationshipFollowMovementIntent(Simulation simulation, int entityId, int tick)
    {
        var world = simulation.World;
        if (!simulation.Relationships.TryResolveFollowSource(world, entityId, out var sourceId))
        {
            return;
        }

        var sourceQ = world.EntityPhysicalQ(sourceId);
        var sourceR = world.EntityPhysicalR(sourceId);
        if (world.EntityPhysicalQ(entityId) != sourceQ || world.EntityPhysicalR(entityId) != sourceR)
        {
            simulation.Movement.Place(
                simulation,
                entityId,
                PlaneCodes.Physical,
                sourceQ,
                sourceR,
                tick,
                MovementCauseCodes.Follow);
        }
    }

    private void ChooseIntent(Simulation simulation, int entityId, int tick)
    {
        var bucket = BuildVerbBucket(simulation, entityId, tick);
        if (bucket.VerbCount == 0)
        {
            return;
        }

        var selectedIndex = PickVerbFromBucket(simulation, bucket, out var selectionBucketCode);
        if (selectedIndex < 0)
        {
            return;
        }

        CommitSelectedIntent(simulation, entityId, tick, _verbCandidateScratch[selectedIndex], selectionBucketCode);
    }

    private VerbBucket BuildVerbBucket(Simulation simulation, int entityId, int tick)
    {
        var bucket = default(VerbBucket);
        var candidates = _verbCandidateScratch;
        var sumAffordanceScores = _verbAffordanceSumScratch;
        var kindId = simulation.World.EntityKindId(entityId);
        var traceIntentId = State.NextIntentId();
        for (var rule = Data.FirstRuleForKind(kindId);
             rule > 0;
             rule = Data.NextRuleForKind(rule))
        {
            if (PhysicalMovementSuspendedByCarry(simulation, entityId, rule))
            {
                continue;
            }

            var result = TryBuildCandidate(simulation, entityId, rule, tick);
            if (!result.HasChosenCandidate)
            {
                EmitIntentTrace(simulation, tick, entityId, rule, traceIntentId, TraceStatusCodes.NoTarget, default);
                continue;
            }

            EmitIntentTrace(simulation, tick, entityId, rule, traceIntentId, TraceStatusCodes.Offered, result.ChosenCandidate);
            candidates[bucket.VerbCount] = result.ChosenCandidate;
            sumAffordanceScores[bucket.VerbCount] = result.SumAffordanceScore;
            bucket.TotalRoutineWeight += result.ChosenCandidate.Weight;
            if (result.SumAffordanceScore > 0)
            {
                bucket.HasOpportunity = true;
                bucket.TotalOpportunityWeight += result.SumAffordanceScore;
            }

            bucket.VerbCount++;
        }

        return bucket;
    }

    private int PickVerbFromBucket(Simulation simulation, VerbBucket bucket, out int selectionBucketCode)
    {
        var opportunityRate = Tuning.LowAltitudeVerbs.OpportunityRate;
        var pickFromOpportunity = bucket.HasOpportunity &&
            opportunityRate > 0 &&
            simulation.NextScalar() < opportunityRate;
        selectionBucketCode = pickFromOpportunity
            ? LowAltitudeVerbSelectionBucketCodes.Opportunity
            : LowAltitudeVerbSelectionBucketCodes.Routine;
        return pickFromOpportunity
            ? PickOpportunityIndex(simulation, _verbAffordanceSumScratch, bucket.TotalOpportunityWeight, bucket.VerbCount)
            : PickRoutineIndex(simulation, _verbCandidateScratch, bucket.TotalRoutineWeight, bucket.VerbCount);
    }

    private void CommitSelectedIntent(
        Simulation simulation,
        int entityId,
        int tick,
        VerbTargetCandidate selected,
        int selectionBucketCode)
    {
        var intentIdSeed = State.NextIntentId();
        selected = ScatterDestination(simulation, entityId, selected, tick);
        var duration = simulation.NextInclusive(
            Data.MinCommitmentTicks(selected.RuleSlot),
            Data.MaxCommitmentTicks(selected.RuleSlot));
        var intentId = State.SetIntent(
            entityId,
            selected.RuleSlot,
            selected.PlaneCode,
            selected.TargetEntityId,
            selected.Q,
            selected.R,
            selected.S,
            tick,
            tick + duration,
            IntentDwellTicks(entityId, intentIdSeed, selected.RuleSlot),
            IntentStepIntervalTicks(entityId, intentIdSeed, selected.RuleSlot),
            IntentPathLoosenessPermille(entityId, intentIdSeed, selected.RuleSlot));
        EmitIntentTrace(simulation, tick, entityId, selected.RuleSlot, intentId, TraceStatusCodes.Selected, selected, selectionBucketCode);
    }

    private struct VerbBucket
    {
        public int VerbCount;
        public FixedScalar TotalRoutineWeight;
        public FixedScalar TotalOpportunityWeight;
        public bool HasOpportunity;
    }

    private static int PickRoutineIndex(
        Simulation simulation,
        VerbTargetCandidate[] candidates,
        FixedScalar totalWeight,
        int count)
    {
        if (count == 0 || totalWeight <= 0)
        {
            return -1;
        }

        var threshold = simulation.NextScalar() * totalWeight;
        var running = FixedScalar.Zero;
        for (var i = 0; i < count; i++)
        {
            running += candidates[i].Weight;
            if (threshold <= running)
            {
                return i;
            }
        }

        return count - 1;
    }

    private static int PickOpportunityIndex(
        Simulation simulation,
        FixedScalar[] opportunityWeights,
        FixedScalar totalWeight,
        int count)
    {
        if (count == 0 || totalWeight <= 0)
        {
            return -1;
        }

        var threshold = simulation.NextScalar() * totalWeight;
        var running = FixedScalar.Zero;
        for (var i = 0; i < count; i++)
        {
            if (opportunityWeights[i] <= 0)
            {
                continue;
            }

            running += opportunityWeights[i];
            if (threshold <= running)
            {
                return i;
            }
        }

        for (var i = count - 1; i >= 0; i--)
        {
            if (opportunityWeights[i] > 0)
            {
                return i;
            }
        }

        return -1;
    }

    private void ApplyIntent(Simulation simulation, int entityId, int rule, int tick)
    {
        using var actionScope = simulation.EnterActionExecutionContext(Data.VerbId(rule));
        var targetQ = State.TargetQ(entityId);
        var targetR = State.TargetR(entityId);
        var planeCode = Data.PlaneCode(rule);
        if (!simulation.World.CellIsKnown(planeCode, targetQ, targetR))
        {
            State.MarkReached(entityId, tick, tick + State.IntentDwellTicks(entityId));
            return;
        }

        var stepStart = Data.StepStart(rule);
        var stepCount = Data.StepCount(rule);
        if (Data.OperationCode(rule) == ProgramOpcodeCodes.MoveActor)
        {
            ApplyStep(simulation, entityId, rule, stepStart, targetQ, targetR, tick);
            return;
        }

        if (!State.StepIsReady(entityId, tick))
        {
            return;
        }

        for (var offset = 0; offset < stepCount; offset++)
        {
            ApplyStep(simulation, entityId, rule, stepStart + offset, targetQ, targetR, tick);
        }

        State.MarkStepped(entityId, tick);
    }

    private void ApplyStep(
        Simulation simulation,
        int entityId,
        int rule,
        int step,
        int targetQ,
        int targetR,
        int tick)
    {
        var planeCode = Data.PlaneCode(rule);
        switch (Data.StepOpcode(step))
        {
            case ProgramOpcodeCodes.MoveActor:
                MoveTowardIntent(simulation, entityId, rule, tick);
                break;
            case ProgramOpcodeCodes.AddOwnedCellScalar:
                ApplyInfluenceAround(
                    simulation,
                    entityId,
                    planeCode,
                    targetQ,
                    targetR,
                    Data.InfluenceRadius(rule),
                    EffectiveInfluenceDelta(simulation, entityId, rule, reducingForeign: false),
                    tick);
                break;
            case ProgramOpcodeCodes.SubtractOtherCellScalar:
                ReduceForeignInfluenceAround(
                    simulation,
                    entityId,
                    rule,
                    planeCode,
                    targetQ,
                    targetR,
                    Data.InfluenceRadius(rule),
                    EffectiveInfluenceDelta(simulation, entityId, rule, reducingForeign: true),
                    tick);
                break;
            case ProgramOpcodeCodes.TryClaimCell:
                TryClaimCellFromProgramStep(simulation, entityId, rule, targetQ, targetR, tick);
                break;
            case ProgramOpcodeCodes.TryRelocateEntity:
                TryRelocateEntityFromProgramStep(simulation, entityId, rule, targetQ, targetR, tick);
                break;
            case ProgramOpcodeCodes.AdjustRelationshipStrength:
                AdjustTargetRelationshipStrength(simulation, entityId, rule, tick);
                break;
        }
    }

    private FixedScalar EffectiveInfluenceDelta(
        Simulation simulation,
        int entityId,
        int rule,
        bool reducingForeign) =>
        Data.InfluenceDelta(rule) *
        CultureSlotInfluenceEffectivenessMultiplier(simulation, entityId, rule, reducingForeign) *
        Tuning.CultureInfluenceActionScale;

    private void ApplyInfluenceAround(
        Simulation simulation,
        int entityId,
        int planeCode,
        int centerQ,
        int centerR,
        int radius,
        FixedScalar delta,
        int tick)
    {
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = centerQ + dq;
                var r = centerR + dr;
                if (simulation.World.CellIsKnown(planeCode, q, r))
                {
                    simulation.ModifyCellCultureInfluenceRawFromEntity(
                        entityId,
                        planeCode,
                        q,
                        r,
                        TerrainAdjustedInfluenceDelta(simulation, entityId, planeCode, q, r, delta),
                        tick);
                }
            }
        }
    }

    private void ReduceForeignInfluenceAround(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int centerQ,
        int centerR,
        int radius,
        FixedScalar delta,
        int tick)
    {
        _ = rule;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = centerQ + dq;
                var r = centerR + dr;
                if (!simulation.World.CellIsKnown(planeCode, q, r))
                {
                    continue;
                }

                ReduceAllowedOtherSourcesAt(
                    simulation,
                    entityId,
                    planeCode,
                    q,
                    r,
                    TerrainAdjustedInfluenceDelta(simulation, entityId, planeCode, q, r, delta),
                    tick);
            }
        }
    }

    private void ReduceAllowedOtherSourcesAt(
        Simulation simulation,
        int entityId,
        int planeCode,
        int q,
        int r,
        FixedScalar delta,
        int tick)
    {
        if (delta <= 0)
        {
            return;
        }

        var world = simulation.World;
        var actorCultureId = world.EntityCultureId(entityId);
        if (actorCultureId <= 0)
        {
            return;
        }

        _ = world;
        _ = simulation.ReduceOtherCultureSourcesAtCell(actorCultureId, planeCode, q, r, delta, tick);
    }

    private void TryClaimCellFromProgramStep(
        Simulation simulation,
        int entityId,
        int rule,
        int q,
        int r,
        int tick)
    {
        var planeCode = Data.PlaneCode(rule);
        var actorKindId = simulation.World.EntityKindId(entityId);
        var claimChance = Data.CoverageGrowthChance(rule) * simulation.TerrainClaimChanceMultiplier(planeCode, actorKindId, q, r);
        var requiredInfluence = Data.RequiredInfluence(rule) * simulation.TerrainClaimCostMultiplier(planeCode, actorKindId, q, r);
        if (simulation.NextScalar() > claimChance ||
            OwnCultureRawAt(simulation.World, entityId, planeCode, q, r) < requiredInfluence ||
            !CellCanBeClaimedFromEntity(simulation.World, planeCode, entityId, q, r) ||
            simulation.World.CoverageOwnerAt(planeCode, q, r) == entityId)
        {
            return;
        }

        KernelEffectExecutor.SetCellState(
            simulation,
            planeCode,
            q,
            r,
            simulation.World.CellTerrainId(planeCode, q, r),
            entityId);
    }

    // Physical ground quality now scales at the deposit choke point via the
    // culture growth multiplier; only semantic terrain keeps its authored
    // influence table here.
    private static FixedScalar TerrainAdjustedInfluenceDelta(
        Simulation simulation,
        int entityId,
        int planeCode,
        int q,
        int r,
        FixedScalar delta)
    {
        return planeCode == PlaneCodes.Physical
            ? delta
            : delta * simulation.TerrainInfluenceMultiplier(planeCode, simulation.World.EntityKindId(entityId), q, r);
    }

    private static FixedScalar OwnCultureRawAt(WorldState world, int entityId, int planeCode, int q, int r)
    {
        var cultureId = world.EntityCultureId(entityId);
        return cultureId <= 0 ? 0 : world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
    }

    private void TryRelocateEntityFromProgramStep(
        Simulation simulation,
        int entityId,
        int rule,
        int q,
        int r,
        int tick)
    {
        if (simulation.NextScalar() > Data.HeadquartersMoveChance(rule) ||
            OwnCultureRawAt(simulation.World, entityId, PlaneCodes.Physical, q, r) < Data.RequiredInfluence(rule))
        {
            return;
        }

        simulation.Movement.Place(simulation, entityId, PlaneCodes.Physical, q, r, tick, MovementCauseCodes.Relocation);
    }

    private bool CellCanBeClaimedFromEntity(WorldState world, int planeCode, int entityId, int q, int r)
    {
        if (HexDistance(CurrentQ(world, entityId, planeCode), CurrentR(world, entityId, planeCode), q, r) <= 1)
        {
            return true;
        }

        if (!world.CoverageOwnerHasCells(entityId, planeCode))
        {
            return false;
        }

        var cursor = world.CoverageCells(entityId, planeCode);
        while (world.MoveNextCoverageCell(ref cursor, out var ownedQ, out var ownedR))
        {
            if (HexDistance(ownedQ, ownedR, q, r) <= 1)
            {
                return true;
            }
        }

        return false;
    }

}
