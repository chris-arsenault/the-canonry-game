namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime :
    IKernelProgramRuntime<LowAltitudeVerbProgramData, LowAltitudeVerbProgramState>
{
    private const int DirectionCount = 6;

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
        float exploreWeight,
        float stalenessWeight,
        float hotspotWeight,
        float crowdingWeight,
        float generatorAffordanceWeight,
        float influenceDelta,
        int influenceRadius,
        float coverageGrowthChance,
        float headquartersMoveChance,
        float requiredInfluence,
        float relationshipStrengthDelta = 0f) =>
        RegisterVerb(
            kindId,
            verbId,
            planeCode,
            operationCode,
            targetModeCode,
            targetKindId,
            relationshipKindId,
            relationshipDirectionCode,
            minCommitmentTicks,
            maxCommitmentTicks,
            dwellTicks,
            targetRadius,
            candidateSampleCount,
            weight,
            exploreWeight,
            stalenessWeight,
            hotspotWeight,
            crowdingWeight,
            generatorAffordanceWeight,
            affinityRelationshipKindId: 0,
            affinityRelationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            relationshipAffinityWeight: 0f,
            influenceDelta: influenceDelta,
            influenceRadius: influenceRadius,
            coverageGrowthChance: coverageGrowthChance,
            headquartersMoveChance: headquartersMoveChance,
            requiredInfluence: requiredInfluence,
            relationshipStrengthDelta: relationshipStrengthDelta,
            foreignInfluenceFilterEnabled: 0,
            foreignInfluenceBlockPositiveRelationships: 0,
            foreignInfluenceAllowNegativeRelationships: 0,
            foreignInfluenceMinimumRelationshipStrength: 0f,
            foreignInfluenceAllowUnrelatedInfluenceAtOrAbove: -1f);

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
        float exploreWeight,
        float stalenessWeight,
        float hotspotWeight,
        float crowdingWeight,
        float generatorAffordanceWeight,
        int affinityRelationshipKindId,
        int affinityRelationshipDirectionCode,
        float relationshipAffinityWeight,
        float influenceDelta,
        int influenceRadius,
        float coverageGrowthChance,
        float headquartersMoveChance,
        float requiredInfluence,
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
            ExploreWeight = exploreWeight,
            StalenessWeight = stalenessWeight,
            HotspotWeight = hotspotWeight,
            CrowdingWeight = crowdingWeight,
            GeneratorAffordanceWeight = generatorAffordanceWeight,
            ForeignInfluenceFilterEnabled = foreignInfluenceFilterEnabled,
            ForeignInfluenceBlockPositiveRelationships = foreignInfluenceBlockPositiveRelationships,
            ForeignInfluenceAllowNegativeRelationships = foreignInfluenceAllowNegativeRelationships,
            ForeignInfluenceMinimumRelationshipStrength = foreignInfluenceMinimumRelationshipStrength,
            ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove = foreignInfluenceAllowUnrelatedInfluenceAtOrAbove,
            AffinityRelationshipKindId = affinityRelationshipKindId,
            AffinityRelationshipDirectionCode = affinityRelationshipDirectionCode,
            RelationshipAffinityWeight = relationshipAffinityWeight,
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

    public int RegisterGeneratorAffordance(
        int actorKindId,
        int candidateKindId,
        int bindingKindId,
        float affordanceScore) =>
        Data.RegisterGeneratorAffordance(actorKindId, candidateKindId, bindingKindId, affordanceScore);

    public int RegisterTraitWeightModifier(
        int kindId,
        int traitId,
        int verbId,
        float weightDelta,
        float exploreWeightDelta,
        float stalenessWeightDelta,
        float hotspotWeightDelta,
        float crowdingWeightDelta,
        float generatorAffordanceWeightDelta,
        float relationshipAffinityWeightDelta) =>
        Data.RegisterTraitWeightModifier(
            kindId,
            traitId,
            verbId,
            weightDelta,
            exploreWeightDelta,
            stalenessWeightDelta,
            hotspotWeightDelta,
            crowdingWeightDelta,
            generatorAffordanceWeightDelta,
            relationshipAffinityWeightDelta);

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
            State.ClearIntent(entityId);
            return true;
        }

        if (targetEntityId != 0 &&
            Data.TargetModeCode(slot) == TargetSelectorCodes.RelationshipTarget &&
            !HasTargetRelationship(simulation.World, entityId, targetEntityId, slot))
        {
            State.ClearIntent(entityId);
            return true;
        }

        if (tick >= State.ExpiresTick(entityId))
        {
            State.ClearIntent(entityId);
            return true;
        }

        var reachedTick = State.ReachedTick(entityId);
        if (reachedTick == 0)
        {
            return false;
        }

        if (tick >= State.DwellUntilTick(entityId))
        {
            State.ClearIntent(entityId);
            return true;
        }

        return false;
    }

    private void ChooseIntent(Simulation simulation, int entityId, int tick)
    {
        var world = simulation.World;
        var kindId = world.EntityKindId(entityId);
        var totalWeight = 0f;
        var selected = default(VerbTargetCandidate);
        var planId = State.NextIntentId();

        for (var rule = 1; rule <= Data.RuleCount; rule++)
        {
            if (Data.KindId(rule) != kindId)
            {
                continue;
            }

            if (!TryBuildCandidate(simulation, entityId, rule, tick, out var candidate))
            {
                EmitIntentTrace(simulation, tick, entityId, rule, planId, TraceStatusCodes.NoTarget, default);
                continue;
            }

            EmitIntentTrace(simulation, tick, entityId, rule, planId, TraceStatusCodes.Offered, candidate);
            totalWeight += candidate.Weight;
            if (simulation.NextFloat() * totalWeight <= candidate.Weight)
            {
                selected = candidate;
            }
        }

        if (selected.RuleSlot == 0)
        {
            return;
        }

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
        EmitIntentTrace(simulation, tick, entityId, selected.RuleSlot, intentId, TraceStatusCodes.Selected, selected);
    }

    private bool TryBuildCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        return Data.TargetModeCode(rule) switch
        {
            TargetSelectorCodes.Explore => TryBuildExploreCandidate(simulation, entityId, rule, tick, out candidate),
            TargetSelectorCodes.EntityKind => TryBuildEntityKindCandidate(simulation, entityId, rule, tick, out candidate),
            TargetSelectorCodes.RelationshipTarget => TryBuildRelationshipCandidate(simulation, entityId, rule, tick, out candidate),
            TargetSelectorCodes.Terrain => TryBuildTerrainCandidate(simulation, entityId, rule, tick, out candidate),
            TargetSelectorCodes.SelfMapValue => TryBuildOwnInfluenceCandidate(simulation, entityId, rule, tick, out candidate),
            TargetSelectorCodes.OtherMapValue => TryBuildForeignInfluenceCandidate(simulation, entityId, rule, tick, out candidate),
            _ => NoCandidate(out candidate),
        };
    }

    private bool TryBuildExploreCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        var totalWeight = 0f;
        var selectedQ = CurrentQ(simulation.World, entityId, Data.PlaneCode(rule));
        var selectedR = CurrentR(simulation.World, entityId, Data.PlaneCode(rule));
        var selectedS = -selectedQ - selectedR;
        var sampleCount = Data.CandidateSampleCount(rule);
        for (var i = 0; i < sampleCount; i++)
        {
            if (!TryRandomCoordinate(simulation, Data.PlaneCode(rule), out var q, out var r, out var s))
            {
                continue;
            }

            var weight = LocationWeight(simulation, entityId, rule, Data.PlaneCode(rule), q, r, tick, 0);
            totalWeight += weight;
            if (simulation.NextFloat() * totalWeight <= weight)
            {
                selectedQ = q;
                selectedR = r;
                selectedS = s;
            }
        }

        if (totalWeight <= 0f)
        {
            return NoCandidate(out candidate);
        }

        candidate = Candidate(rule, Data.PlaneCode(rule), 0, 0, selectedQ, selectedR, selectedS, totalWeight / sampleCount);
        return true;
    }

    private bool TryBuildEntityKindCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        var targetKindId = Data.TargetKindId(rule);
        if (targetKindId == 0)
        {
            return NoCandidate(out candidate);
        }

        return TryChooseTargetEntity(simulation, entityId, rule, tick, targetKindId, out candidate);
    }

    private bool TryBuildRelationshipCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        var relationshipKindId = Data.RelationshipKindId(rule);
        if (relationshipKindId == 0)
        {
            return NoCandidate(out candidate);
        }

        var totalWeight = 0f;
        var selected = default(VerbTargetCandidate);
        if (Data.RelationshipDirectionCode(rule) is LowAltitudeVerbRelationshipDirectionCodes.Outgoing
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            AddRelationshipCandidates(simulation, entityId, rule, tick, outgoing: true, ref totalWeight, ref selected);
        }

        if (Data.RelationshipDirectionCode(rule) is LowAltitudeVerbRelationshipDirectionCodes.Incoming
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            AddRelationshipCandidates(simulation, entityId, rule, tick, outgoing: false, ref totalWeight, ref selected);
        }

        if (selected.RuleSlot == 0)
        {
            return NoCandidate(out candidate);
        }

        candidate = selected;
        return true;
    }

    private bool TryBuildTerrainCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        var planeCode = Data.PlaneCode(rule);
        var totalWeight = 0f;
        var selectedQ = CurrentQ(simulation.World, entityId, planeCode);
        var selectedR = CurrentR(simulation.World, entityId, planeCode);
        var selectedS = planeCode == PlaneCodes.Semantic ? -selectedQ - selectedR : 0;
        var sampleCount = Data.CandidateSampleCount(rule);
        var actorKindId = simulation.World.EntityKindId(entityId);
        for (var i = 0; i < sampleCount * 3; i++)
        {
            if (!TryRandomCoordinate(simulation, planeCode, out var q, out var r, out var s))
            {
                continue;
            }

            if (!Data.TerrainAllowed(rule, simulation.World.CellTerrainId(planeCode, q, r)))
            {
                continue;
            }

            if (!simulation.Movement.CanEnter(simulation, planeCode, actorKindId, q, r))
            {
                continue;
            }

            var weight = LocationWeight(simulation, entityId, rule, planeCode, q, r, tick, 0);
            totalWeight += weight;
            if (simulation.NextFloat() * totalWeight <= weight)
            {
                selectedQ = q;
                selectedR = r;
                selectedS = s;
            }
        }

        if (totalWeight <= 0f)
        {
            return NoCandidate(out candidate);
        }

        candidate = Candidate(rule, planeCode, 0, 0, selectedQ, selectedR, selectedS, totalWeight / sampleCount);
        return true;
    }

    private bool TryBuildOwnInfluenceCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        var totalWeight = 0f;
        var planeCode = Data.PlaneCode(rule);
        var actorKindId = simulation.World.EntityKindId(entityId);
        var generatorAffordance = GeneratorAffordanceScore(actorKindId, actorKindId, actorKindId);
        var selectedQ = CurrentQ(simulation.World, entityId, planeCode);
        var selectedR = CurrentR(simulation.World, entityId, planeCode);
        var selectedS = planeCode == PlaneCodes.Semantic ? -selectedQ - selectedR : 0;
        var sampleCount = Data.CandidateSampleCount(rule);
        for (var i = 0; i < sampleCount * 4; i++)
        {
            if (!TryRandomCoordinate(simulation, planeCode, out var q, out var r, out var s))
            {
                continue;
            }

            var influence = simulation.World.GetCellInfluence(entityId, planeCode, q, r);
            if (influence <= 0f)
            {
                continue;
            }

            var weight = LocationWeight(simulation, entityId, rule, planeCode, q, r, tick, generatorAffordance) +
                ((100f - influence) / 100f) * 2f;
            totalWeight += weight;
            if (simulation.NextFloat() * totalWeight <= weight)
            {
                selectedQ = q;
                selectedR = r;
                selectedS = s;
            }
        }

        if (totalWeight <= 0f &&
            simulation.World.GetCellInfluence(entityId, planeCode, selectedQ, selectedR) <= 0f)
        {
            return NoCandidate(out candidate);
        }

        var candidateWeight = totalWeight <= 0f ? Data.Weight(rule) : totalWeight / sampleCount;
        candidate = Candidate(rule, planeCode, 0, 0, selectedQ, selectedR, selectedS, candidateWeight);
        return true;
    }

    private bool TryBuildForeignInfluenceCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        var totalWeight = 0f;
        var planeCode = Data.PlaneCode(rule);
        var actorKindId = simulation.World.EntityKindId(entityId);
        var selectedQ = 0;
        var selectedR = 0;
        var selectedS = 0;
        var selectedEntityId = 0;
        var selectedKindId = 0;
        var sampleCount = Data.CandidateSampleCount(rule);
        for (var i = 0; i < sampleCount * 4; i++)
        {
            if (!TryRandomCoordinate(simulation, planeCode, out var q, out var r, out var s))
            {
                continue;
            }

            var influencerId = StrongestAllowedOtherInfluencerAt(simulation, entityId, rule, planeCode, q, r, out var influence);
            if (influencerId == 0)
            {
                continue;
            }

            var influencerKindId = simulation.World.EntityKindId(influencerId);
            var generatorAffordance = GeneratorAffordanceScore(actorKindId, influencerKindId, influencerKindId);
            var weight = LocationWeight(simulation, entityId, rule, planeCode, q, r, tick, generatorAffordance) +
                (influence / 100f) * 3f;
            totalWeight += weight;
            if (simulation.NextFloat() * totalWeight <= weight)
            {
                selectedQ = q;
                selectedR = r;
                selectedS = s;
                selectedEntityId = influencerId;
                selectedKindId = influencerKindId;
            }
        }

        if (selectedEntityId == 0)
        {
            return NoCandidate(out candidate);
        }

        candidate = Candidate(rule, planeCode, selectedEntityId, selectedKindId, selectedQ, selectedR, selectedS, totalWeight / sampleCount);
        return true;
    }

    private bool TryChooseTargetEntity(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int targetKindId,
        out VerbTargetCandidate candidate)
    {
        var world = simulation.World;
        var totalWeight = 0f;
        var selected = default(VerbTargetCandidate);
        for (var targetEntityId = 1; targetEntityId <= world.EntityHighWatermark; targetEntityId++)
        {
            if (targetEntityId == entityId ||
                !world.EntityIsActive(targetEntityId) ||
                world.EntityKindId(targetEntityId) != targetKindId)
            {
                continue;
            }

            AddEntityCandidate(simulation, entityId, rule, tick, targetEntityId, ref totalWeight, ref selected);
        }

        if (selected.RuleSlot == 0)
        {
            return NoCandidate(out candidate);
        }

        candidate = selected;
        return true;
    }

    private void AddRelationshipCandidates(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        bool outgoing,
        ref float totalWeight,
        ref VerbTargetCandidate selected)
    {
        var world = simulation.World;
        var relationshipKindId = Data.RelationshipKindId(rule);
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKind(entityId, relationshipKindId)
            : world.IncomingRelationshipsOfKind(entityId, relationshipKindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var targetEntityId = outgoing
                ? world.RelationshipTargetId(relationshipId)
                : world.RelationshipSourceId(relationshipId);
            if (!world.EntityIsActive(targetEntityId))
            {
                continue;
            }

            AddEntityCandidate(simulation, entityId, rule, tick, targetEntityId, ref totalWeight, ref selected);
        }
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

    private bool TryRandomCoordinate(Simulation simulation, int planeCode, out int q, out int r, out int s)
    {
        if (planeCode == PlaneCodes.Semantic)
        {
            return TryRandomSemanticCoordinate(simulation, out q, out r, out s);
        }

        var radius = global::System.Math.Max(1, simulation.World.MapRadius);
        for (var attempt = 0; attempt < 16; attempt++)
        {
            q = simulation.NextInclusive(-radius, radius);
            r = simulation.NextInclusive(-radius, radius);
            s = -q - r;
            if (MaxAbs(q, r, s) > radius)
            {
                continue;
            }

            if (simulation.World.CellIsKnown(q, r))
            {
                return true;
            }
        }

        q = 0;
        r = 0;
        s = 0;
        return simulation.World.CellIsKnown(0, 0);
    }

}
