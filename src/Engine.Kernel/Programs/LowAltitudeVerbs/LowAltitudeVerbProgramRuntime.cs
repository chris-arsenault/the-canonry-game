namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime :
    IKernelProgramRuntime<LowAltitudeVerbProgramData, LowAltitudeVerbProgramState>
{
    private const int DirectionCount = 6;
    private const int VerbBucketCapacity = 64;

    private readonly VerbTargetCandidate[] _verbCandidateScratch = new VerbTargetCandidate[VerbBucketCapacity];
    private readonly float[] _verbAffordanceSumScratch = new float[VerbBucketCapacity];

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
    }

    public LowAltitudeVerbProgramData Data { get; }

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
        float weight,
        float opportunityBonusOverride = 0f,
        float fitnessWeight = 0f,
        float distanceScaleOverride = 0f,
        int fitnessRelationshipKindId = 0,
        int fitnessRelationshipDirectionCode = LowAltitudeVerbRelationshipDirectionCodes.Either,
        int fitnessPolarityCode = AffordancePolarityCodes.None,
        float fitnessThreshold = 0f,
        float influenceDelta = 0f,
        int influenceRadius = 0,
        float coverageGrowthChance = 0f,
        float headquartersMoveChance = 0f,
        float requiredInfluence = 0f,
        int foreignInfluenceFilterEnabled = 0,
        int foreignInfluenceBlockPositiveRelationships = 0,
        int foreignInfluenceAllowNegativeRelationships = 0,
        float foreignInfluenceMinimumRelationshipStrength = 0f,
        float foreignInfluenceAllowUnrelatedInfluenceAtOrAbove = -1f,
        float relationshipStrengthDelta = 0f) =>
        Data.RegisterVerb(new LowAltitudeVerbRegistration
        {
            KindId = kindId,
            VerbId = verbId,
            PlaneCode = planeCode,
            OperationCode = operationCode,
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
            ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove = foreignInfluenceAllowUnrelatedInfluenceAtOrAbove,
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

    public int RegisterInitialInfluence(int kindId, int planeCode, float influenceValue) =>
        Data.RegisterInitialInfluence(kindId, planeCode, influenceValue);

    public void ConfigureSemanticBounds(int minQ, int maxQ, int minR, int maxR) =>
        Data.ConfigureSemanticBounds(minQ, maxQ, minR, maxR);

    public void AddPrimitiveStep(int rule, int opcode, int intArgument, float floatArgument) =>
        Data.AddPrimitiveStep(rule, opcode, intArgument, floatArgument);

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
        int terrainId,
        int polarityCode,
        float threshold,
        float weight) =>
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
            terrainId,
            polarityCode,
            threshold,
            weight);

    public int RegisterTraitWeightModifier(
        int kindId,
        int traitId,
        int verbId,
        float weightDelta,
        float opportunityBonusDelta,
        float fitnessWeightDelta) =>
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
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!world.EntityIsActive(entityId))
            {
                continue;
            }

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
        for (var rule = 1; rule <= Data.InitialInfluenceRuleCount; rule++)
        {
            if (Data.InitialInfluenceKindId(rule) == kindId &&
                ApplyInitialInfluenceRule(simulation, entityId, rule, tick))
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
        if (simulation.World.GetCellInfluence(entityId, planeCode, q, r) < influenceValue)
        {
            simulation.SetCellInfluence(entityId, planeCode, q, r, influenceValue, tick);
            return true;
        }

        return false;
    }

    private bool ShouldChooseIntent(Simulation simulation, int entityId, int tick)
    {
        var slot = State.ActiveVerbSlot(entityId);
        if (slot == 0)
        {
            return true;
        }

        var targetEntityId = State.TargetEntityId(entityId);
        if (targetEntityId != 0 && !simulation.World.EntityIsActive(targetEntityId))
        {
            ClearIntentAndPushRecent(entityId, tick);
            return true;
        }

        if (targetEntityId != 0 &&
            Data.TargetModeCode(slot) == TargetSelectorCodes.RelationshipTarget &&
            !HasTargetRelationship(simulation.World, entityId, targetEntityId, slot))
        {
            ClearIntentAndPushRecent(entityId, tick);
            return true;
        }

        if (tick >= State.ExpiresTick(entityId))
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
        for (var rule = 1; rule <= Data.RuleCount && bucket.VerbCount < VerbBucketCapacity; rule++)
        {
            if (Data.KindId(rule) != kindId)
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
            if (result.SumAffordanceScore > 0f)
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
            opportunityRate > 0f &&
            simulation.NextFloat() < opportunityRate;
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
        public float TotalRoutineWeight;
        public float TotalOpportunityWeight;
        public bool HasOpportunity;
    }

    private static int PickRoutineIndex(
        Simulation simulation,
        VerbTargetCandidate[] candidates,
        float totalWeight,
        int count)
    {
        if (count == 0 || totalWeight <= 0f)
        {
            return -1;
        }

        var threshold = simulation.NextFloat() * totalWeight;
        var running = 0f;
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
        float[] opportunityWeights,
        float totalWeight,
        int count)
    {
        if (count == 0 || totalWeight <= 0f)
        {
            return -1;
        }

        var threshold = simulation.NextFloat() * totalWeight;
        var running = 0f;
        for (var i = 0; i < count; i++)
        {
            if (opportunityWeights[i] <= 0f)
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
            if (opportunityWeights[i] > 0f)
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
                ApplyInfluenceAround(simulation, entityId, planeCode, targetQ, targetR, Data.InfluenceRadius(rule), Data.InfluenceDelta(rule), tick);
                break;
            case ProgramOpcodeCodes.SubtractOtherCellScalar:
                ReduceForeignInfluenceAround(simulation, entityId, rule, planeCode, targetQ, targetR, Data.InfluenceRadius(rule), Data.InfluenceDelta(rule), tick);
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

    private void ApplyInfluenceAround(
        Simulation simulation,
        int entityId,
        int planeCode,
        int centerQ,
        int centerR,
        int radius,
        float delta,
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
                    simulation.ModifyCellInfluence(
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
        float delta,
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
                if (!simulation.World.CellIsKnown(planeCode, q, r))
                {
                    continue;
                }

                var targetEntityId = StrongestAllowedOtherInfluencerAt(simulation, entityId, rule, planeCode, q, r, out _);
                if (targetEntityId != 0)
                {
                    simulation.ModifyCellInfluence(
                        targetEntityId,
                        planeCode,
                        q,
                        r,
                        -TerrainAdjustedInfluenceDelta(simulation, entityId, planeCode, q, r, delta),
                        tick);
                }
            }
        }
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
        if (simulation.NextFloat() > claimChance ||
            simulation.World.GetCellInfluence(entityId, planeCode, q, r) < requiredInfluence ||
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

    private static float TerrainAdjustedInfluenceDelta(
        Simulation simulation,
        int entityId,
        int planeCode,
        int q,
        int r,
        float delta)
    {
        return delta * simulation.TerrainInfluenceMultiplier(planeCode, simulation.World.EntityKindId(entityId), q, r);
    }

    private void TryRelocateEntityFromProgramStep(
        Simulation simulation,
        int entityId,
        int rule,
        int q,
        int r,
        int tick)
    {
        if (simulation.NextFloat() > Data.HeadquartersMoveChance(rule) ||
            simulation.World.GetCellInfluence(entityId, PlaneCodes.Physical, q, r) < Data.RequiredInfluence(rule))
        {
            return;
        }

        simulation.Movement.TryMovePhysical(simulation, entityId, q, r, tick, MovementCauseCodes.Relocation);
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

    private int StrongestAllowedOtherInfluencerAt(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int q,
        int r,
        out float influence)
    {
        var selectedEntityId = 0;
        influence = 0f;
        var world = simulation.World;
        for (var otherEntityId = 1; otherEntityId <= world.EntityHighWatermark; otherEntityId++)
        {
            if (otherEntityId == entityId || !world.EntityIsActive(otherEntityId))
            {
                continue;
            }

            var candidateInfluence = world.GetCellInfluence(otherEntityId, planeCode, q, r);
            if (candidateInfluence <= influence ||
                !ForeignInfluenceTargetAllowed(world, entityId, otherEntityId, rule, candidateInfluence))
            {
                continue;
            }

            influence = candidateInfluence;
            selectedEntityId = otherEntityId;
        }

        return selectedEntityId;
    }

    private bool ForeignInfluenceTargetAllowed(
        WorldState world,
        int entityId,
        int targetEntityId,
        int rule,
        float influence)
    {
        if (!Data.ForeignInfluenceFilterEnabled(rule))
        {
            return true;
        }

        var minimumStrength = Data.ForeignInfluenceMinimumRelationshipStrength(rule);
        if (Data.ForeignInfluenceBlocksPositiveRelationships(rule) &&
            HasRelationshipPolarityBetween(world, entityId, targetEntityId, RelationshipPolarityCodes.Positive, minimumStrength))
        {
            return false;
        }

        if (Data.ForeignInfluenceAllowsNegativeRelationships(rule) &&
            HasRelationshipPolarityBetween(world, entityId, targetEntityId, RelationshipPolarityCodes.Negative, minimumStrength))
        {
            return true;
        }

        var threshold = Data.ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove(rule);
        return threshold >= 0f && influence >= threshold;
    }

    private static bool HasRelationshipPolarityBetween(
        WorldState world,
        int firstEntityId,
        int secondEntityId,
        int polarityCode,
        float minimumStrength)
    {
        for (var relationshipId = 1; relationshipId <= world.RelationshipHighWatermark; relationshipId++)
        {
            if (!world.RelationshipIsActive(relationshipId) ||
                world.RelationshipPolarityCode(relationshipId) != polarityCode ||
                world.RelationshipStrength(relationshipId) < minimumStrength)
            {
                continue;
            }

            var sourceId = world.RelationshipSourceId(relationshipId);
            var targetId = world.RelationshipTargetId(relationshipId);
            if ((sourceId == firstEntityId && targetId == secondEntityId) ||
                (sourceId == secondEntityId && targetId == firstEntityId))
            {
                return true;
            }
        }

        return false;
    }

}
