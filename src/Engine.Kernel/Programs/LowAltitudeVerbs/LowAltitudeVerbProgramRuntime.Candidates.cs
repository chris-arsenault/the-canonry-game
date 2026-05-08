namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private readonly struct CandidateBuildResult
    {
        public CandidateBuildResult(VerbTargetCandidate candidate, float sumAffordanceScore)
        {
            ChosenCandidate = candidate;
            SumAffordanceScore = sumAffordanceScore;
            HasChosenCandidate = candidate.RuleSlot != 0;
        }

        public VerbTargetCandidate ChosenCandidate { get; }

        public float SumAffordanceScore { get; }

        public bool HasChosenCandidate { get; }

        public static CandidateBuildResult Empty => default;
    }

    private CandidateBuildResult TryBuildCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick)
    {
        return Data.TargetModeCode(rule) switch
        {
            TargetSelectorCodes.Explore => TryBuildExploreCandidate(simulation, entityId, rule, tick),
            TargetSelectorCodes.EntityKind => TryBuildEntityKindCandidate(simulation, entityId, rule, tick),
            TargetSelectorCodes.RelationshipTarget => TryBuildRelationshipCandidate(simulation, entityId, rule, tick),
            TargetSelectorCodes.Terrain => TryBuildTerrainCandidate(simulation, entityId, rule, tick),
            TargetSelectorCodes.SelfMapValue => TryBuildOwnInfluenceCandidate(simulation, entityId, rule, tick),
            TargetSelectorCodes.OtherMapValue => TryBuildForeignInfluenceCandidate(simulation, entityId, rule, tick),
            _ => CandidateBuildResult.Empty,
        };
    }

    private CandidateBuildResult TryBuildExploreCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick)
    {
        var planeCode = Data.PlaneCode(rule);
        var scalars = EffectiveScalars(simulation.World, entityId, rule);
        var actorQ = CurrentQ(simulation.World, entityId, planeCode);
        var actorR = CurrentR(simulation.World, entityId, planeCode);
        var searchRadius = CellTargetSearchRadius(simulation.World, rule, TargetSelectorCodes.Explore);
        var totalWeight = 0f;
        var sumAffordanceScore = 0f;
        var selected = default(VerbTargetCandidate);
        for (var dq = -searchRadius; dq <= searchRadius; dq++)
        {
            var minDr = global::System.Math.Max(-searchRadius, -dq - searchRadius);
            var maxDr = global::System.Math.Min(searchRadius, -dq + searchRadius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = actorQ + dq;
                var r = actorR + dr;
                if ((q == actorQ && r == actorR) ||
                    !CellTargetCoordinateAllowed(simulation.World, planeCode, q, r) ||
                    !simulation.Movement.CanEnter(simulation, planeCode, entityId, q, r))
                {
                    continue;
                }

                var distance = DistancePref(rule, TargetSelectorCodes.Explore, actorQ, actorR, q, r);
                var saturationDivisor = CellSaturationDivisor(simulation, planeCode, q, r);
                var recencyKey = LowAltitudeVerbProgramState.PackCellTargetKey(planeCode, q, r);
                var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
                var baseWeight = scalars.Weight * distance * saturationDivisor * recency;
                var affordance = default(AffordanceContribution);
                var candidate = Candidate(rule, planeCode, 0, 0, q, r, TargetS(planeCode, q, r), baseWeight, affordance, scalars.OpportunityBonus);
                ConsiderCandidate(simulation, candidate, ref totalWeight, ref selected);
            }
        }

        return new CandidateBuildResult(selected, sumAffordanceScore);
    }

    private CandidateBuildResult TryBuildTerrainCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick)
    {
        var planeCode = Data.PlaneCode(rule);
        var scalars = EffectiveScalars(simulation.World, entityId, rule);
        var actorQ = CurrentQ(simulation.World, entityId, planeCode);
        var actorR = CurrentR(simulation.World, entityId, planeCode);
        var searchRadius = CellTargetSearchRadius(simulation.World, rule, TargetSelectorCodes.Terrain);
        var totalWeight = 0f;
        var sumAffordanceScore = 0f;
        var selected = default(VerbTargetCandidate);
        for (var dq = -searchRadius; dq <= searchRadius; dq++)
        {
            var minDr = global::System.Math.Max(-searchRadius, -dq - searchRadius);
            var maxDr = global::System.Math.Min(searchRadius, -dq + searchRadius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = actorQ + dq;
                var r = actorR + dr;
                if (!CellTargetCoordinateAllowed(simulation.World, planeCode, q, r))
                {
                    continue;
                }

                var terrainId = simulation.World.CellTerrainId(planeCode, q, r);
                if (!Data.TerrainAllowed(rule, terrainId) ||
                    !simulation.Movement.CanEnter(simulation, planeCode, entityId, q, r))
                {
                    continue;
                }

                var distance = DistancePref(rule, TargetSelectorCodes.Terrain, actorQ, actorR, q, r);
                var saturationDivisor = CellSaturationDivisor(simulation, planeCode, q, r);
                var recencyKey = LowAltitudeVerbProgramState.PackCellTargetKey(planeCode, q, r);
                var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
                var baseWeight = scalars.Weight * distance * saturationDivisor * recency;
                var affordance = default(AffordanceContribution);
                var candidate = Candidate(rule, planeCode, 0, 0, q, r, TargetS(planeCode, q, r), baseWeight, affordance, scalars.OpportunityBonus);
                ConsiderCandidate(simulation, candidate, ref totalWeight, ref selected);
            }
        }

        return new CandidateBuildResult(selected, sumAffordanceScore);
    }

    private CandidateBuildResult TryBuildEntityKindCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick)
    {
        var targetKindId = Data.TargetKindId(rule);
        if (targetKindId == 0)
        {
            return CandidateBuildResult.Empty;
        }

        var world = simulation.World;
        var planeCode = Data.PlaneCode(rule);
        var actorKindId = world.EntityKindId(entityId);
        var scalars = EffectiveScalars(world, entityId, rule);
        var actorQ = CurrentQ(world, entityId, planeCode);
        var actorR = CurrentR(world, entityId, planeCode);
        var totalWeight = 0f;
        var sumAffordanceScore = 0f;
        var selected = default(VerbTargetCandidate);

        for (var targetEntityId = 1; targetEntityId <= world.EntityHighWatermark; targetEntityId++)
        {
            if (targetEntityId == entityId ||
                !world.EntityIsActive(targetEntityId) ||
                world.EntityKindId(targetEntityId) != targetKindId)
            {
                continue;
            }

            var targetQ = CurrentQ(world, targetEntityId, planeCode);
            var targetR = CurrentR(world, targetEntityId, planeCode);
            var targetS = planeCode == PlaneCodes.Semantic ? world.EntitySemanticS(targetEntityId) : 0;
            var distance = DistancePref(rule, TargetSelectorCodes.EntityKind, actorQ, actorR, targetQ, targetR);
            var fitness = RelationshipFitness(world, entityId, targetEntityId, rule, scalars);
            var saturationDivisor = CellSaturationDivisor(simulation, planeCode, targetQ, targetR);
            var recencyKey = LowAltitudeVerbProgramState.PackEntityTargetKey(targetEntityId);
            var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
            var baseWeight = scalars.Weight * distance * fitness * saturationDivisor * recency;
            var affordance = CombineAffordancesForEntityTarget(actorKindId, planeCode, targetKindId);
            sumAffordanceScore += affordance.Score;
            var candidate = Candidate(rule, planeCode, targetEntityId, targetKindId, targetQ, targetR, targetS, baseWeight, affordance, scalars.OpportunityBonus);
            ConsiderCandidate(simulation, candidate, ref totalWeight, ref selected);
        }

        return new CandidateBuildResult(selected, sumAffordanceScore);
    }

    private CandidateBuildResult TryBuildRelationshipCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick)
    {
        if (Data.RelationshipKindId(rule) == 0)
        {
            return CandidateBuildResult.Empty;
        }

        var totalWeight = 0f;
        var sumAffordanceScore = 0f;
        var selected = default(VerbTargetCandidate);
        if (Data.RelationshipDirectionCode(rule) is LowAltitudeVerbRelationshipDirectionCodes.Outgoing
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            AddRelationshipCandidates(simulation, entityId, rule, tick, outgoing: true, ref totalWeight, ref sumAffordanceScore, ref selected);
        }

        if (Data.RelationshipDirectionCode(rule) is LowAltitudeVerbRelationshipDirectionCodes.Incoming
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            AddRelationshipCandidates(simulation, entityId, rule, tick, outgoing: false, ref totalWeight, ref sumAffordanceScore, ref selected);
        }

        return new CandidateBuildResult(selected, sumAffordanceScore);
    }

    private void AddRelationshipCandidates(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        bool outgoing,
        ref float totalWeight,
        ref float sumAffordanceScore,
        ref VerbTargetCandidate selected)
    {
        var world = simulation.World;
        var relationshipKindId = Data.RelationshipKindId(rule);
        var planeCode = Data.PlaneCode(rule);
        var actorKindId = world.EntityKindId(entityId);
        var scalars = EffectiveScalars(world, entityId, rule);
        var strengthDelta = Data.RelationshipStrengthDelta(rule);
        var directionCode = outgoing
            ? LowAltitudeVerbRelationshipDirectionCodes.Outgoing
            : LowAltitudeVerbRelationshipDirectionCodes.Incoming;
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

            var targetQ = CurrentQ(world, targetEntityId, planeCode);
            var targetR = CurrentR(world, targetEntityId, planeCode);
            var targetS = planeCode == PlaneCodes.Semantic ? world.EntitySemanticS(targetEntityId) : 0;
            var fitness = RelationshipFitness(world, entityId, targetEntityId, rule, scalars);
            var recencyKey = LowAltitudeVerbProgramState.PackEntityTargetKey(targetEntityId);
            var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
            var baseWeight = scalars.Weight * fitness * recency;
            var targetKindId = world.EntityKindId(targetEntityId);
            var affordance = CombineAffordancesForRelationshipTarget(
                actorKindId,
                targetKindId,
                planeCode,
                relationshipKindId,
                directionCode,
                world.RelationshipStrength(relationshipId),
                strengthDelta);
            sumAffordanceScore += affordance.Score;
            var candidate = Candidate(rule, planeCode, targetEntityId, targetKindId, targetQ, targetR, targetS, baseWeight, affordance, scalars.OpportunityBonus);
            ConsiderCandidate(simulation, candidate, ref totalWeight, ref selected);
        }
    }

    private CandidateBuildResult TryBuildOwnInfluenceCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick)
    {
        var planeCode = Data.PlaneCode(rule);
        var scalars = EffectiveScalars(simulation.World, entityId, rule);
        var actorQ = CurrentQ(simulation.World, entityId, planeCode);
        var actorR = CurrentR(simulation.World, entityId, planeCode);
        var searchRadius = CellTargetSearchRadius(simulation.World, rule, TargetSelectorCodes.SelfMapValue);
        var totalWeight = 0f;
        var sumAffordanceScore = 0f;
        var selected = default(VerbTargetCandidate);
        for (var dq = -searchRadius; dq <= searchRadius; dq++)
        {
            var minDr = global::System.Math.Max(-searchRadius, -dq - searchRadius);
            var maxDr = global::System.Math.Min(searchRadius, -dq + searchRadius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = actorQ + dq;
                var r = actorR + dr;
                if (!CellTargetCoordinateAllowed(simulation.World, planeCode, q, r))
                {
                    continue;
                }

                var influence = simulation.World.GetCellInfluence(entityId, planeCode, q, r);
                if (influence <= 0f)
                {
                    continue;
                }

                var candidate = ScoreOwnInfluenceCell(simulation, entityId, rule, tick, planeCode, q, r, TargetS(planeCode, q, r), influence, scalars, out var affordanceScore);
                sumAffordanceScore += affordanceScore;
                ConsiderCandidate(simulation, candidate, ref totalWeight, ref selected);
            }
        }

        return new CandidateBuildResult(selected, sumAffordanceScore);
    }

    private VerbTargetCandidate ScoreOwnInfluenceCell(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int q,
        int r,
        int s,
        float influence,
        EffectiveVerbScalars scalars,
        out float affordanceScore)
    {
        var actorKindId = simulation.World.EntityKindId(entityId);
        var actorQ = CurrentQ(simulation.World, entityId, planeCode);
        var actorR = CurrentR(simulation.World, entityId, planeCode);
        var shortfallBias = Tuning.LowAltitudeVerbs.InfluenceShortfallSamplingFactor *
            global::System.MathF.Max(0f, (100f - influence) / 100f);
        var distance = DistancePref(rule, TargetSelectorCodes.SelfMapValue, actorQ, actorR, q, r);
        var saturationDivisor = CellSaturationDivisor(simulation, planeCode, q, r);
        var recencyKey = LowAltitudeVerbProgramState.PackCellTargetKey(planeCode, q, r);
        var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
        var baseWeight = scalars.Weight * (0.25f + shortfallBias) * distance * saturationDivisor * recency;
        var affordance = CombineAffordancesForInfluenceTarget(
            actorKindId,
            planeCode,
            Data.OperationCode(rule),
            actorKindId,
            AffordancePolarityCodes.Increase,
            influence);
        affordanceScore = affordance.Score;
        return Candidate(rule, planeCode, 0, 0, q, r, s, baseWeight, affordance, scalars.OpportunityBonus);
    }

    private CandidateBuildResult TryBuildForeignInfluenceCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick)
    {
        var world = simulation.World;
        var planeCode = Data.PlaneCode(rule);
        var actorKindId = world.EntityKindId(entityId);
        var scalars = EffectiveScalars(world, entityId, rule);
        var actorQ = CurrentQ(world, entityId, planeCode);
        var actorR = CurrentR(world, entityId, planeCode);
        var searchRadius = CellTargetSearchRadius(world, rule, TargetSelectorCodes.OtherMapValue);
        var filter = BuildForeignInfluenceFilter(world, entityId, rule);
        var scratchWidth = (searchRadius * 2) + 1;
        if ((long)scratchWidth * scratchWidth <= ForeignInfluenceScratchCapacity)
        {
            return TryBuildForeignInfluenceCandidateFromInfluenceCells(
                simulation,
                entityId,
                rule,
                tick,
                planeCode,
                actorKindId,
                scalars,
                actorQ,
                actorR,
                searchRadius,
                scratchWidth,
                filter);
        }

        return TryBuildForeignInfluenceCandidateFromSearchCells(
            simulation,
            entityId,
            rule,
            tick,
            planeCode,
            actorKindId,
            scalars,
            actorQ,
            actorR,
            searchRadius,
            filter);
    }

    private CandidateBuildResult TryBuildForeignInfluenceCandidateFromSearchCells(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int actorKindId,
        EffectiveVerbScalars scalars,
        int actorQ,
        int actorR,
        int searchRadius,
        ForeignInfluenceFilter filter)
    {
        var totalWeight = 0f;
        var sumAffordanceScore = 0f;
        var selected = default(VerbTargetCandidate);
        for (var dq = -searchRadius; dq <= searchRadius; dq++)
        {
            var minDr = global::System.Math.Max(-searchRadius, -dq - searchRadius);
            var maxDr = global::System.Math.Min(searchRadius, -dq + searchRadius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = actorQ + dq;
                var r = actorR + dr;
                if (!CellTargetCoordinateAllowed(simulation.World, planeCode, q, r))
                {
                    continue;
                }

                var influencerId = StrongestAllowedOtherInfluencerAt(
                    simulation,
                    entityId,
                    rule,
                    filter,
                    planeCode,
                    q,
                    r,
                    out var influence);
                if (influencerId == 0)
                {
                    continue;
                }

                var influencerKindId = simulation.World.EntityKindId(influencerId);
                var presenceBias = Tuning.LowAltitudeVerbs.ForeignInfluencePresenceSamplingFactor * (influence / 100f);
                var distance = DistancePref(rule, TargetSelectorCodes.OtherMapValue, actorQ, actorR, q, r);
                var saturationDivisor = CellSaturationDivisor(simulation, planeCode, q, r);
                var recencyKey = LowAltitudeVerbProgramState.PackEntityTargetKey(influencerId);
                var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
                var baseWeight = scalars.Weight * (0.25f + presenceBias) * distance * saturationDivisor * recency;
                var affordance = CombineAffordancesForInfluenceTarget(
                    actorKindId,
                    planeCode,
                    Data.OperationCode(rule),
                    influencerKindId,
                    AffordancePolarityCodes.Reduce,
                    influence);
                sumAffordanceScore += affordance.Score;
                var candidate = Candidate(rule, planeCode, influencerId, influencerKindId, q, r, TargetS(planeCode, q, r), baseWeight, affordance, scalars.OpportunityBonus);
                ConsiderCandidate(simulation, candidate, ref totalWeight, ref selected);
            }
        }

        return new CandidateBuildResult(selected, sumAffordanceScore);
    }

    private CandidateBuildResult TryBuildForeignInfluenceCandidateFromInfluenceCells(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int actorKindId,
        EffectiveVerbScalars scalars,
        int actorQ,
        int actorR,
        int searchRadius,
        int scratchWidth,
        ForeignInfluenceFilter filter)
    {
        var epoch = BeginForeignInfluenceScratchEpoch();
        var touchedCount = PopulateForeignInfluenceScratch(
            simulation.World,
            entityId,
            rule,
            filter,
            planeCode,
            actorQ,
            actorR,
            searchRadius,
            scratchWidth,
            epoch);
        return ScoreForeignInfluenceScratch(
            simulation,
            entityId,
            rule,
            tick,
            planeCode,
            actorKindId,
            scalars,
            actorQ,
            actorR,
            searchRadius,
            scratchWidth,
            epoch,
            touchedCount);
    }

    private int BeginForeignInfluenceScratchEpoch()
    {
        if (_foreignInfluenceScratchEpoch == int.MaxValue)
        {
            global::System.Array.Clear(_foreignInfluenceEpochScratch);
            _foreignInfluenceScratchEpoch = 0;
        }

        _foreignInfluenceScratchEpoch++;
        return _foreignInfluenceScratchEpoch;
    }

    private int PopulateForeignInfluenceScratch(
        WorldState world,
        int entityId,
        int rule,
        ForeignInfluenceFilter filter,
        int planeCode,
        int actorQ,
        int actorR,
        int searchRadius,
        int scratchWidth,
        int epoch)
    {
        var touchedCount = 0;
        for (var otherEntityId = 1; otherEntityId <= world.EntityHighWatermark; otherEntityId++)
        {
            if (otherEntityId == entityId ||
                !world.EntityIsActive(otherEntityId) ||
                world.InfluenceCellCount(otherEntityId, planeCode) == 0)
            {
                continue;
            }

            AddForeignInfluenceCellsForEntity(
                world,
                otherEntityId,
                filter,
                planeCode,
                actorQ,
                actorR,
                searchRadius,
                scratchWidth,
                epoch,
                ref touchedCount);
        }

        return touchedCount;
    }

    private void AddForeignInfluenceCellsForEntity(
        WorldState world,
        int otherEntityId,
        ForeignInfluenceFilter filter,
        int planeCode,
        int actorQ,
        int actorR,
        int searchRadius,
        int scratchWidth,
        int epoch,
        ref int touchedCount)
    {
        var cursor = world.InfluenceCells(otherEntityId, planeCode);
        while (world.MoveNextInfluenceCell(ref cursor, out var q, out var r, out var candidateInfluence))
        {
            if (!ForeignInfluenceScratchCellAllowed(world, planeCode, actorQ, actorR, q, r, searchRadius))
            {
                continue;
            }

            var slot = ForeignInfluenceScratchSlot(actorQ, actorR, q, r, searchRadius, scratchWidth);
            var slotTouched = _foreignInfluenceEpochScratch[slot] == epoch;
            if (slotTouched && candidateInfluence <= _foreignInfluenceValueScratch[slot])
            {
                continue;
            }

            if (!ForeignInfluenceTargetAllowed(otherEntityId, filter, candidateInfluence))
            {
                continue;
            }

            if (!slotTouched)
            {
                _foreignInfluenceTouchedSlotScratch[touchedCount] = slot;
                touchedCount++;
            }

            _foreignInfluenceEpochScratch[slot] = epoch;
            _foreignInfluenceEntityScratch[slot] = otherEntityId;
            _foreignInfluenceValueScratch[slot] = candidateInfluence;
        }
    }

    private bool ForeignInfluenceScratchCellAllowed(
        WorldState world,
        int planeCode,
        int actorQ,
        int actorR,
        int q,
        int r,
        int searchRadius)
    {
        var dq = q - actorQ;
        if (dq < -searchRadius || dq > searchRadius)
        {
            return false;
        }

        var dr = r - actorR;
        if (dr < -searchRadius || dr > searchRadius)
        {
            return false;
        }

        var ds = -dq - dr;
        return ds >= -searchRadius &&
            ds <= searchRadius &&
            CellTargetCoordinateAllowed(world, planeCode, q, r);
    }

    private static int ForeignInfluenceScratchSlot(
        int actorQ,
        int actorR,
        int q,
        int r,
        int searchRadius,
        int scratchWidth)
    {
        var scratchQ = q - actorQ + searchRadius;
        var scratchR = r - actorR + searchRadius;
        return (scratchQ * scratchWidth) + scratchR;
    }

    private CandidateBuildResult ScoreForeignInfluenceScratch(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int actorKindId,
        EffectiveVerbScalars scalars,
        int actorQ,
        int actorR,
        int searchRadius,
        int scratchWidth,
        int epoch,
        int touchedCount)
    {
        var totalWeight = 0f;
        var sumAffordanceScore = 0f;
        var selected = default(VerbTargetCandidate);
        for (var i = 0; i < touchedCount; i++)
        {
            AddForeignInfluenceScratchCandidate(
                simulation,
                entityId,
                rule,
                tick,
                planeCode,
                actorKindId,
                scalars,
                actorQ,
                actorR,
                _foreignInfluenceTouchedSlotScratch[i],
                searchRadius,
                scratchWidth,
                epoch,
                ref totalWeight,
                ref sumAffordanceScore,
                ref selected);
        }

        return new CandidateBuildResult(selected, sumAffordanceScore);
    }

    private void AddForeignInfluenceScratchCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int actorKindId,
        EffectiveVerbScalars scalars,
        int actorQ,
        int actorR,
        int slot,
        int searchRadius,
        int scratchWidth,
        int epoch,
        ref float totalWeight,
        ref float sumAffordanceScore,
        ref VerbTargetCandidate selected)
    {
        if (_foreignInfluenceEpochScratch[slot] != epoch)
        {
            return;
        }

        var scratchQ = slot / scratchWidth;
        var scratchR = slot - (scratchQ * scratchWidth);
        var q = actorQ + scratchQ - searchRadius;
        var r = actorR + scratchR - searchRadius;
        var influencerId = _foreignInfluenceEntityScratch[slot];
        var influence = _foreignInfluenceValueScratch[slot];
        var influencerKindId = simulation.World.EntityKindId(influencerId);
        var presenceBias = Tuning.LowAltitudeVerbs.ForeignInfluencePresenceSamplingFactor * (influence / 100f);
        var distance = DistancePref(rule, TargetSelectorCodes.OtherMapValue, actorQ, actorR, q, r);
        var saturationDivisor = CellSaturationDivisor(simulation, planeCode, q, r);
        var recencyKey = LowAltitudeVerbProgramState.PackEntityTargetKey(influencerId);
        var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
        var baseWeight = scalars.Weight * (0.25f + presenceBias) * distance * saturationDivisor * recency;
        var affordance = CombineAffordancesForInfluenceTarget(
            actorKindId,
            planeCode,
            Data.OperationCode(rule),
            influencerKindId,
            AffordancePolarityCodes.Reduce,
            influence);
        sumAffordanceScore += affordance.Score;
        var candidate = Candidate(rule, planeCode, influencerId, influencerKindId, q, r, TargetS(planeCode, q, r), baseWeight, affordance, scalars.OpportunityBonus);
        ConsiderCandidate(simulation, candidate, ref totalWeight, ref selected);
    }

    private static void ConsiderCandidate(
        Simulation simulation,
        VerbTargetCandidate candidate,
        ref float totalWeight,
        ref VerbTargetCandidate selected)
    {
        totalWeight += candidate.Weight;
        if (totalWeight > 0f && simulation.NextFloat() * totalWeight <= candidate.Weight)
        {
            selected = candidate;
        }
    }
}
