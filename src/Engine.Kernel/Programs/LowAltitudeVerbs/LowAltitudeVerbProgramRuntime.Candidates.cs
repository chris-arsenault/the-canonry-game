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
        var scalars = EffectiveScalars(simulation, entityId, rule);
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
                    !simulation.Movement.CanEnter(simulation, planeCode, entityId, q, r) ||
                    !MovementTargetReachable(simulation, entityId, rule, planeCode, q, r, tick))
                {
                    continue;
                }

                var distance = DistancePref(rule, TargetSelectorCodes.Explore, actorQ, actorR, q, r);
                var saturationDivisor = CellSaturationDivisor(simulation, planeCode, q, r);
                var recencyKey = LowAltitudeVerbProgramState.PackCellTargetKey(planeCode, q, r);
                var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
                var routeDrive = TravelCultureRouteMultiplier(simulation.World, entityId, rule, planeCode, q, r);
                var baseWeight = scalars.Weight * distance * saturationDivisor * recency * routeDrive;
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
        var scalars = EffectiveScalars(simulation, entityId, rule);
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
                    !simulation.Movement.CanEnter(simulation, planeCode, entityId, q, r) ||
                    !MovementTargetReachable(simulation, entityId, rule, planeCode, q, r, tick))
                {
                    continue;
                }

                var distance = DistancePref(rule, TargetSelectorCodes.Terrain, actorQ, actorR, q, r);
                var saturationDivisor = CellSaturationDivisor(simulation, planeCode, q, r);
                var recencyKey = LowAltitudeVerbProgramState.PackCellTargetKey(planeCode, q, r);
                var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
                var routeDrive = TravelCultureRouteMultiplier(simulation.World, entityId, rule, planeCode, q, r);
                var baseWeight = scalars.Weight * distance * saturationDivisor * recency * routeDrive;
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
        var scalars = EffectiveScalars(simulation, entityId, rule);
        var actorQ = CurrentQ(world, entityId, planeCode);
        var actorR = CurrentR(world, entityId, planeCode);
        var totalWeight = 0f;
        var sumAffordanceScore = 0f;
        var selected = default(VerbTargetCandidate);
        if (!TryPrepareCultureInfluenceComponentsForRule(world, entityId, rule, planeCode))
        {
            return CandidateBuildResult.Empty;
        }

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
            if (!MovementTargetReachable(simulation, entityId, rule, planeCode, targetQ, targetR, tick))
            {
                continue;
            }

            AddEntityKindCandidate(
                simulation,
                entityId,
                rule,
                tick,
                planeCode,
                actorKindId,
                targetKindId,
                scalars,
                actorQ,
                actorR,
                targetEntityId,
                targetQ,
                targetR,
                ref totalWeight,
                ref sumAffordanceScore,
                ref selected);
        }

        return new CandidateBuildResult(selected, sumAffordanceScore);
    }

    private bool TryPrepareCultureInfluenceComponentsForRule(WorldState world, int entityId, int rule, int planeCode)
    {
        if (!OperationBuildsOwnedInfluence(Data.OperationCode(rule)))
        {
            return true;
        }

        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return false;
        }

        _cultureInfluenceComponents.Build(world, cultureId, planeCode);
        return true;
    }

    private void AddEntityKindCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int actorKindId,
        int targetKindId,
        EffectiveVerbScalars scalars,
        int actorQ,
        int actorR,
        int targetEntityId,
        int targetQ,
        int targetR,
        ref float totalWeight,
        ref float sumAffordanceScore,
        ref VerbTargetCandidate selected)
    {
        var world = simulation.World;
        var targetS = planeCode == PlaneCodes.Semantic ? world.EntitySemanticS(targetEntityId) : 0;
        var distance = DistancePref(rule, TargetSelectorCodes.EntityKind, actorQ, actorR, targetQ, targetR);
        var fitness = RelationshipFitness(world, entityId, targetEntityId, rule, scalars);
        var saturationDivisor = CellSaturationDivisor(simulation, planeCode, targetQ, targetR);
        var recencyKey = LowAltitudeVerbProgramState.PackEntityTargetKey(targetEntityId);
        var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
        var routeDrive = TravelCultureRouteMultiplier(world, entityId, rule, planeCode, targetQ, targetR);
        var baseWeight = scalars.Weight * distance * fitness * saturationDivisor * recency * routeDrive;
        if (!TryApplyEntityKindCultureExpansionDrive(world, entityId, rule, planeCode, targetQ, targetR, ref baseWeight))
        {
            return;
        }

        var affordance = CombineAffordancesForEntityTarget(actorKindId, planeCode, targetKindId);
        sumAffordanceScore += affordance.Score;
        var candidate = Candidate(rule, planeCode, targetEntityId, targetKindId, targetQ, targetR, targetS, baseWeight, affordance, scalars.OpportunityBonus);
        ConsiderCandidate(simulation, candidate, ref totalWeight, ref selected);
    }

    private bool TryApplyEntityKindCultureExpansionDrive(
        WorldState world,
        int entityId,
        int rule,
        int planeCode,
        int targetQ,
        int targetR,
        ref float baseWeight)
    {
        if (!OperationBuildsOwnedInfluence(Data.OperationCode(rule)))
        {
            return true;
        }

        var expansionDrive = OwnCultureExpansionDriveAround(
            world,
            entityId,
            rule,
            planeCode,
            targetQ,
            targetR,
            Data.InfluenceRadius(rule),
            _cultureInfluenceComponents);
        if (expansionDrive <= 0f)
        {
            return false;
        }

        baseWeight *= expansionDrive;
        return true;
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
        var scalars = EffectiveScalars(simulation, entityId, rule);
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
            if (!MovementTargetReachable(simulation, entityId, rule, planeCode, targetQ, targetR, tick))
            {
                continue;
            }

            var targetS = planeCode == PlaneCodes.Semantic ? world.EntitySemanticS(targetEntityId) : 0;
            var fitness = RelationshipFitness(world, entityId, targetEntityId, rule, scalars);
            var recencyKey = LowAltitudeVerbProgramState.PackEntityTargetKey(targetEntityId);
            var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
            var routeDrive = TravelCultureRouteMultiplier(world, entityId, rule, planeCode, targetQ, targetR);
            var baseWeight = scalars.Weight * fitness * recency * routeDrive;
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
        var world = simulation.World;
        var planeCode = Data.PlaneCode(rule);
        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return CandidateBuildResult.Empty;
        }

        var scalars = EffectiveScalars(simulation, entityId, rule);
        var actorQ = CurrentQ(world, entityId, planeCode);
        var actorR = CurrentR(world, entityId, planeCode);
        var searchRadius = CellTargetSearchRadius(world, rule, TargetSelectorCodes.SelfMapValue);
        _cultureInfluenceComponents.Build(world, cultureId, planeCode);
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
                if (!CellTargetCoordinateAllowed(world, planeCode, q, r))
                {
                    continue;
                }

                if (!MovementTargetReachable(simulation, entityId, rule, planeCode, q, r, tick))
                {
                    continue;
                }

                AddOwnInfluenceCandidate(
                    simulation,
                    entityId,
                    rule,
                    tick,
                    planeCode,
                    cultureId,
                    q,
                    r,
                    scalars,
                    ref totalWeight,
                    ref sumAffordanceScore,
                    ref selected);
            }
        }

        return new CandidateBuildResult(selected, sumAffordanceScore);
    }

    private void AddOwnInfluenceCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int cultureId,
        int q,
        int r,
        EffectiveVerbScalars scalars,
        ref float totalWeight,
        ref float sumAffordanceScore,
        ref VerbTargetCandidate selected)
    {
        var world = simulation.World;
        var entityInfluence = world.GetCellInfluence(entityId, planeCode, q, r);
        var ownCultureInfluence = world.GetCellCultureInfluence(cultureId, planeCode, q, r);
        var neighborhood = CultureNeighborhood(world, cultureId, planeCode, q, r);
        var targetingCode = EffectiveInfluenceTargetingCode(rule);
        if (!OwnCultureExpansionTargetAllowed(targetingCode, ownCultureInfluence, neighborhood.CenterRaw, entityInfluence, neighborhood))
        {
            return;
        }

        var foreignCultureInfluence = StrongestOtherCultureInfluence(world, cultureId, planeCode, q, r, out _);
        var candidate = ScoreOwnInfluenceCell(
            simulation,
            entityId,
            rule,
            tick,
            planeCode,
            q,
            r,
            TargetS(planeCode, q, r),
            ownCultureInfluence,
            foreignCultureInfluence,
            neighborhood.CenterRaw,
            entityInfluence,
            neighborhood,
            _cultureInfluenceComponents.AttachmentAt(q, r),
            scalars,
            out var affordanceScore);
        if (candidate.RuleSlot == 0)
        {
            return;
        }

        sumAffordanceScore += affordanceScore;
        ConsiderCandidate(simulation, candidate, ref totalWeight, ref selected);
    }

    private bool MovementTargetReachable(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int q,
        int r,
        int tick) =>
        Data.OperationCode(rule) != ProgramOpcodeCodes.MoveActor ||
        simulation.Movement.CanReach(simulation, entityId, planeCode, q, r, tick);

    private VerbTargetCandidate ScoreOwnInfluenceCell(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int q,
        int r,
        int s,
        float ownCultureInfluence,
        float foreignCultureInfluence,
        float ownCultureRaw,
        float entityInfluence,
        CultureInfluenceNeighborhood neighborhood,
        CultureInfluenceAttachment attachment,
        EffectiveVerbScalars scalars,
        out float affordanceScore)
    {
        var actorKindId = simulation.World.EntityKindId(entityId);
        var actorQ = CurrentQ(simulation.World, entityId, planeCode);
        var actorR = CurrentR(simulation.World, entityId, planeCode);
        var targetingCode = EffectiveInfluenceTargetingCode(rule);
        var culturalExpansionDrive = OwnCultureExpansionDrive(
            targetingCode,
            ownCultureInfluence,
            foreignCultureInfluence,
            ownCultureRaw,
            entityInfluence,
            neighborhood,
            attachment);
        if (culturalExpansionDrive <= 0f)
        {
            affordanceScore = 0f;
            return default;
        }

        var distance = DistancePref(rule, TargetSelectorCodes.SelfMapValue, actorQ, actorR, q, r);
        var saturationDivisor = InfluenceTargetSaturationDivisor(simulation, targetingCode, planeCode, q, r);
        var recencyKey = LowAltitudeVerbProgramState.PackCellTargetKey(planeCode, q, r);
        var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
        var baseWeight = scalars.Weight * culturalExpansionDrive * distance * saturationDivisor * recency;
        var affordance = CombineAffordancesForInfluenceTarget(
            actorKindId,
            planeCode,
            Data.OperationCode(rule),
            actorKindId,
            AffordancePolarityCodes.Increase,
            ownCultureInfluence);
        affordanceScore = affordance.Score + OwnCultureFieldOpportunity(culturalExpansionDrive);
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
        var actorCultureId = world.EntityCultureId(entityId);
        var scalars = EffectiveScalars(simulation, entityId, rule);
        var actorQ = CurrentQ(world, entityId, planeCode);
        var actorR = CurrentR(world, entityId, planeCode);
        var searchRadius = CellTargetSearchRadius(world, rule, TargetSelectorCodes.OtherMapValue);
        var filter = BuildForeignInfluenceFilter(world, entityId, rule);
        if (actorCultureId > 0)
        {
            _cultureInfluenceComponents.Build(world, actorCultureId, planeCode);
        }

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
                actorCultureId,
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
            actorCultureId,
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
        int actorCultureId,
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

                if (!MovementTargetReachable(simulation, entityId, rule, planeCode, q, r, tick))
                {
                    continue;
                }

                AddForeignInfluenceSearchCandidate(
                    simulation,
                    entityId,
                    rule,
                    tick,
                    filter,
                    planeCode,
                    actorKindId,
                    actorCultureId,
                    scalars,
                    actorQ,
                    actorR,
                    q,
                    r,
                    ref totalWeight,
                    ref sumAffordanceScore,
                    ref selected);
            }
        }

        return new CandidateBuildResult(selected, sumAffordanceScore);
    }

    private void AddForeignInfluenceSearchCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        ForeignInfluenceFilter filter,
        int planeCode,
        int actorKindId,
        int actorCultureId,
        EffectiveVerbScalars scalars,
        int actorQ,
        int actorR,
        int q,
        int r,
        ref float totalWeight,
        ref float sumAffordanceScore,
        ref VerbTargetCandidate selected)
    {
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
            return;
        }

        AddForeignInfluenceCandidate(
            simulation,
            entityId,
            rule,
            planeCode,
            actorKindId,
            actorCultureId,
            scalars,
            actorQ,
            actorR,
            q,
            r,
            influencerId,
            influence,
            tick,
            ref totalWeight,
            ref sumAffordanceScore,
            ref selected);
    }

    private CandidateBuildResult TryBuildForeignInfluenceCandidateFromInfluenceCells(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int actorKindId,
        int actorCultureId,
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
            actorCultureId,
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
            actorCultureId,
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
        int actorCultureId,
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
                world.EntityCultureId(otherEntityId) == actorCultureId ||
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
        int actorCultureId,
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
                actorCultureId,
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
        int actorCultureId,
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
        if (!MovementTargetReachable(simulation, entityId, rule, planeCode, q, r, tick))
        {
            return;
        }

        AddForeignInfluenceCandidate(
            simulation,
            entityId,
            rule,
            planeCode,
            actorKindId,
            actorCultureId,
            scalars,
            actorQ,
            actorR,
            q,
            r,
            _foreignInfluenceEntityScratch[slot],
            _foreignInfluenceValueScratch[slot],
            tick,
            ref totalWeight,
            ref sumAffordanceScore,
            ref selected);
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
