namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private readonly struct CandidateBuildResult
    {
        public CandidateBuildResult(VerbTargetCandidate candidate, FixedScalar sumAffordanceScore)
        {
            ChosenCandidate = candidate;
            SumAffordanceScore = sumAffordanceScore;
            HasChosenCandidate = candidate.RuleSlot != 0;
        }

        public VerbTargetCandidate ChosenCandidate { get; }

        public FixedScalar SumAffordanceScore { get; }

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
            TargetSelectorCodes.ChroniclePursuit => TryBuildChroniclePursuitCandidate(simulation, entityId, rule, tick),
            TargetSelectorCodes.AttractorTrek => TryBuildAttractorTrekCandidate(simulation, entityId, rule, tick),
            _ => CandidateBuildResult.Empty,
        };
    }

    private const int TrekMinDistance = 8;

    /// <summary>
    /// Journeys toward the pull of the world: sample distant cells and weigh
    /// them by current local attractor mass, so entities trek to where
    /// life gathers - settlements, works, and the cast of hot chronicles.
    /// </summary>
    private CandidateBuildResult TryBuildAttractorTrekCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick)
    {
        var world = simulation.World;
        var planeCode = Data.PlaneCode(rule);
        var scalars = EffectiveScalars(simulation, entityId, rule);
        var actorQ = CurrentQ(world, entityId, planeCode);
        var actorR = CurrentR(world, entityId, planeCode);
        var searchRadius = global::System.Math.Max(TrekMinDistance + 2, CellTargetSearchRadius(world, rule, TargetSelectorCodes.AttractorTrek));
        var samples = global::System.Math.Max(4, Data.CandidateSampleCount(rule));
        var selected = default(VerbTargetCandidate);
        for (var i = 0; i < samples; i++)
        {
            var logicalKey = world.EntityOccurrenceHandle(entityId);
            var dq = simulation.RandomInclusive(
                -searchRadius,
                searchRadius,
                logicalKey,
                rule,
                KernelRandomPurposeCodes.VerbAttractorQ,
                i);
            var dr = simulation.RandomInclusive(
                global::System.Math.Max(-searchRadius, -dq - searchRadius),
                global::System.Math.Min(searchRadius, -dq + searchRadius),
                logicalKey,
                rule,
                KernelRandomPurposeCodes.VerbAttractorR,
                i);
            var q = actorQ + dq;
            var r = actorR + dr;
            var distance = (global::System.Math.Abs(dq) + global::System.Math.Abs(dr) + global::System.Math.Abs(dq + dr)) / 2;
            if (distance < TrekMinDistance ||
                !CellTargetCoordinateAllowed(world, planeCode, q, r) ||
                !MovementTargetReachable(simulation, entityId, rule, planeCode, q, r, tick))
            {
                continue;
            }

            var pull = simulation.ReadAttractorMass(planeCode, q, r);
            if (pull <= 0)
            {
                continue;
            }

            var weight = scalars.Weight * pull * DistancePref(rule, TargetSelectorCodes.AttractorTrek, actorQ, actorR, q, r);
            var candidate = Candidate(rule, planeCode, 0, world.EntityKindId(entityId), q, r, TargetS(planeCode, q, r), weight, default, scalars.OpportunityBonus);
            ConsiderCandidate(simulation, entityId, candidate, ref selected);
        }

        return new CandidateBuildResult(selected, 0);
    }

    private const int PursuitMinDistance = 5;

    /// <summary>
    /// The tale draws its cast together: a journey toward the entity's
    /// chronicle costar, offered only while the story is hot and the costar
    /// is genuinely far - close costars need no pursuit.
    /// </summary>
    private CandidateBuildResult TryBuildChroniclePursuitCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick)
    {
        var costarId = simulation.Chronicles.PursuitTargetFor(entityId, out var heat);
        if (costarId <= 0 || !simulation.World.EntityIsActive(costarId))
        {
            return CandidateBuildResult.Empty;
        }

        var world = simulation.World;
        var planeCode = Data.PlaneCode(rule);
        var actorQ = CurrentQ(world, entityId, planeCode);
        var actorR = CurrentR(world, entityId, planeCode);
        var targetQ = CurrentQ(world, costarId, planeCode);
        var targetR = CurrentR(world, costarId, planeCode);
        var distance = (global::System.Math.Abs(actorQ - targetQ) +
            global::System.Math.Abs(actorR - targetR) +
            global::System.Math.Abs((actorQ + actorR) - (targetQ + targetR))) / 2;
        if (distance < PursuitMinDistance ||
            !MovementTargetReachable(simulation, entityId, rule, planeCode, targetQ, targetR, tick))
        {
            return CandidateBuildResult.Empty;
        }

        var scalars = EffectiveScalars(simulation, entityId, rule);
        var targetS = planeCode == PlaneCodes.Semantic ? world.EntitySemanticS(costarId) : 0;
        var weight = scalars.Weight * (1 + (heat * FixedScalar.FromUnits(150000L)));
        var candidate = new VerbTargetCandidate(
            RuleSlot: rule,
            PlaneCode: planeCode,
            TargetEntityId: costarId,
            TargetKindId: world.EntityKindId(costarId),
            Q: targetQ,
            R: targetR,
            S: targetS,
            BaseWeight: weight,
            AffordanceScore: 0,
            AffordanceBonus: 0,
            AffordanceSourceKindCode: 0,
            AffordanceSourceId: 0,
            AffordanceShapeCode: 0,
            AffordanceSourceKindMask: 0,
            AffordanceShapeMask: 0,
            AffordanceContributionCount: 0,
            GeneratorAffordanceCount: 0,
            GambitAffordanceCount: 0,
            HiddenAffordanceCount: 0,
            Weight: weight);
        return new CandidateBuildResult(candidate, 0);
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
        var sumAffordanceScore = FixedScalar.Zero;
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
                var routeDrive = TravelCultureRouteMultiplier(simulation, entityId, rule, planeCode, q, r);
                var leash = SupportLeashMultiplier(simulation, entityId, rule, planeCode, q, r);
                var baseWeight = scalars.Weight * distance * saturationDivisor * recency * routeDrive * leash;
                var affordance = default(AffordanceContribution);
                var candidate = Candidate(rule, planeCode, 0, 0, q, r, TargetS(planeCode, q, r), baseWeight, affordance, scalars.OpportunityBonus);
                ConsiderCandidate(simulation, entityId, candidate, ref selected);
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
        var sumAffordanceScore = FixedScalar.Zero;
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
                    !GeographyAllowed(simulation, rule, planeCode, q, r) ||
                    !simulation.Movement.CanEnter(simulation, planeCode, entityId, q, r) ||
                    !MovementTargetReachable(simulation, entityId, rule, planeCode, q, r, tick))
                {
                    continue;
                }

                var distance = DistancePref(rule, TargetSelectorCodes.Terrain, actorQ, actorR, q, r);
                var saturationDivisor = CellSaturationDivisor(simulation, planeCode, q, r);
                var recencyKey = LowAltitudeVerbProgramState.PackCellTargetKey(planeCode, q, r);
                var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
                var routeDrive = TravelCultureRouteMultiplier(simulation, entityId, rule, planeCode, q, r);
                var leash = SupportLeashMultiplier(simulation, entityId, rule, planeCode, q, r);
                var baseWeight = scalars.Weight * distance * saturationDivisor * recency * routeDrive * leash;
                var affordance = default(AffordanceContribution);
                var candidate = Candidate(rule, planeCode, 0, 0, q, r, TargetS(planeCode, q, r), baseWeight, affordance, scalars.OpportunityBonus);
                ConsiderCandidate(simulation, entityId, candidate, ref selected);
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
        var sumAffordanceScore = FixedScalar.Zero;
        var selected = default(VerbTargetCandidate);
        if (!ActorCanBuildOwnedInfluence(world, entityId, rule))
        {
            return CandidateBuildResult.Empty;
        }

        for (var targetEntityId = world.FirstEntityOfKind(targetKindId);
             targetEntityId > 0;
             targetEntityId = world.NextEntityOfKind(targetEntityId))
        {
            if (targetEntityId == entityId)
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
                ref sumAffordanceScore,
                ref selected);
        }

        return new CandidateBuildResult(selected, sumAffordanceScore);
    }

    private bool ActorCanBuildOwnedInfluence(WorldState world, int entityId, int rule)
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

        return cultureId > 0;
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
        ref FixedScalar sumAffordanceScore,
        ref VerbTargetCandidate selected)
    {
        var world = simulation.World;
        var targetS = planeCode == PlaneCodes.Semantic ? world.EntitySemanticS(targetEntityId) : 0;
        var distance = DistancePref(rule, TargetSelectorCodes.EntityKind, actorQ, actorR, targetQ, targetR);
        var fitness = RelationshipFitness(world, entityId, targetEntityId, rule, scalars);
        var saturationDivisor = CellSaturationDivisor(simulation, planeCode, targetQ, targetR);
        var recencyKey = LowAltitudeVerbProgramState.PackEntityTargetKey(
            world.EntityOccurrenceHandle(targetEntityId));
        var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
        var routeDrive = TravelCultureRouteMultiplier(simulation, entityId, rule, planeCode, targetQ, targetR);
        var baseWeight = scalars.Weight * distance * fitness * saturationDivisor * recency * routeDrive;
        if (!TryApplyEntityKindCultureExpansionDrive(simulation, entityId, rule, planeCode, targetQ, targetR, ref baseWeight))
        {
            return;
        }

        var affordance = CombineAffordancesForEntityTarget(actorKindId, planeCode, targetKindId);
        sumAffordanceScore += affordance.Score;
        var candidate = Candidate(rule, planeCode, targetEntityId, targetKindId, targetQ, targetR, targetS, baseWeight, affordance, scalars.OpportunityBonus);
        ConsiderCandidate(simulation, entityId, candidate, ref selected);
    }

    private bool TryApplyEntityKindCultureExpansionDrive(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int targetQ,
        int targetR,
        ref FixedScalar baseWeight)
    {
        if (!OperationBuildsOwnedInfluence(Data.OperationCode(rule)))
        {
            return true;
        }

        var expansionDrive = OwnCultureExpansionDriveAround(
            simulation,
            entityId,
            rule,
            planeCode,
            targetQ,
            targetR,
            Data.InfluenceRadius(rule));
        if (expansionDrive <= 0)
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

        var sumAffordanceScore = FixedScalar.Zero;
        var selected = default(VerbTargetCandidate);
        if (Data.RelationshipDirectionCode(rule) is LowAltitudeVerbRelationshipDirectionCodes.Outgoing
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            AddRelationshipCandidates(simulation, entityId, rule, tick, outgoing: true, ref sumAffordanceScore, ref selected);
        }

        if (Data.RelationshipDirectionCode(rule) is LowAltitudeVerbRelationshipDirectionCodes.Incoming
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            AddRelationshipCandidates(simulation, entityId, rule, tick, outgoing: false, ref sumAffordanceScore, ref selected);
        }

        return new CandidateBuildResult(selected, sumAffordanceScore);
    }

    private void AddRelationshipCandidates(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        bool outgoing,
        ref FixedScalar sumAffordanceScore,
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
            var recencyKey = LowAltitudeVerbProgramState.PackEntityTargetKey(
                world.EntityOccurrenceHandle(targetEntityId));
            var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
            var routeDrive = TravelCultureRouteMultiplier(simulation, entityId, rule, planeCode, targetQ, targetR);
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
            ConsiderCandidate(simulation, entityId, candidate, ref selected);
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
        var sumAffordanceScore = FixedScalar.Zero;
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
        ref FixedScalar sumAffordanceScore,
        ref VerbTargetCandidate selected)
    {
        var boundary = simulation.CultureLocality.CellAt(simulation.World, cultureId, planeCode, q, r);
        var neighborhood = CultureNeighborhood(boundary);
        var targetingCode = EffectiveInfluenceTargetingCode(rule);
        if (!OwnCultureExpansionTargetAllowed(targetingCode, boundary.RawMass, neighborhood))
        {
            return;
        }

        var candidate = ScoreOwnInfluenceCell(
            simulation,
            entityId,
            rule,
            tick,
            planeCode,
            q,
            r,
            TargetS(planeCode, q, r),
            boundary.Share,
            boundary.ForeignShare,
            boundary.ForeignRawMass,
            boundary.RawMass,
            neighborhood,
            CultureAttachment(boundary),
            scalars,
            out var affordanceScore);
        if (candidate.RuleSlot == 0)
        {
            return;
        }

        sumAffordanceScore += affordanceScore;
        ConsiderCandidate(simulation, entityId, candidate, ref selected);
    }

    private bool MovementTargetReachable(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int q,
        int r,
        int tick)
    {
        if (Data.OperationCode(rule) != ProgramOpcodeCodes.MoveActor)
        {
            return true;
        }

        var targetMode = Data.TargetModeCode(rule);
        if (targetMode == TargetSelectorCodes.Explore ||
            targetMode == TargetSelectorCodes.Terrain)
        {
            return true;
        }

        return simulation.Movement.CanReach(simulation, entityId, planeCode, q, r, tick);
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
        FixedScalar ownCultureInfluence,
        FixedScalar foreignCultureInfluence,
        FixedScalar foreignCultureRaw,
        FixedScalar ownCultureRaw,
        CultureInfluenceNeighborhood neighborhood,
        CultureInfluenceAttachment attachment,
        EffectiveVerbScalars scalars,
        out FixedScalar affordanceScore)
    {
        var actorKindId = simulation.World.EntityKindId(entityId);
        var actorQ = CurrentQ(simulation.World, entityId, planeCode);
        var actorR = CurrentR(simulation.World, entityId, planeCode);
        var targetingCode = EffectiveInfluenceTargetingCode(rule);
        var culturalExpansionDrive = OwnCultureExpansionDrive(
            targetingCode,
            ownCultureInfluence,
            foreignCultureInfluence,
            foreignCultureRaw,
            ownCultureRaw,
            neighborhood,
            attachment);
        if (culturalExpansionDrive <= 0)
        {
            affordanceScore = 0;
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
            searchRadius);
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
        int searchRadius)
    {
        var sumAffordanceScore = FixedScalar.Zero;
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
                    planeCode,
                    actorKindId,
                    actorCultureId,
                    scalars,
                    actorQ,
                    actorR,
                    q,
                    r,
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
        int planeCode,
        int actorKindId,
        int actorCultureId,
        EffectiveVerbScalars scalars,
        int actorQ,
        int actorR,
        int q,
        int r,
        ref FixedScalar sumAffordanceScore,
        ref VerbTargetCandidate selected)
    {
        _ = rule;
        var influence = ForeignCultureInfluenceAt(
            simulation,
            actorCultureId,
            planeCode,
            q,
            r,
            out var foreignCultureId);
        if (foreignCultureId == 0)
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
            0,
            influence,
            tick,
            ref sumAffordanceScore,
            ref selected);
    }

    private static void ConsiderCandidate(
        Simulation simulation,
        int entityId,
        VerbTargetCandidate candidate,
        ref VerbTargetCandidate selected)
    {
        if (candidate.Weight <= 0)
        {
            return;
        }

        var logicalKey = simulation.World.EntityOccurrenceHandle(entityId);
        var candidateKey = CandidateRandomKey(candidate);
        var candidatePriority = simulation.RandomScalar(
            logicalKey,
            candidate.RuleSlot,
            KernelRandomPurposeCodes.VerbCandidateSelection,
            candidateKey) * candidate.Weight;
        var selectedPriority = selected.Weight <= 0
            ? FixedScalar.Zero
            : simulation.RandomScalar(
                logicalKey,
                selected.RuleSlot,
                KernelRandomPurposeCodes.VerbCandidateSelection,
                CandidateRandomKey(selected)) * selected.Weight;
        if (selected.Weight <= 0 ||
            candidatePriority > selectedPriority ||
            (candidatePriority == selectedPriority && candidateKey < CandidateRandomKey(selected)))
        {
            selected = candidate;
        }
    }

    private static int CandidateRandomKey(in VerbTargetCandidate candidate)
    {
        unchecked
        {
            var hash = (uint)candidate.TargetEntityId * 2654435761u;
            hash ^= (uint)candidate.PlaneCode * 2246822519u;
            hash ^= (uint)candidate.Q * 3266489917u;
            hash ^= (uint)candidate.R * 668265263u;
            hash ^= (uint)candidate.S * 374761393u;
            return (int)(hash ^ (hash >> 16));
        }
    }

    // A verb can want warm ground, a significant site, or the edge of the
    // known world rather than an exact terrain list. Filters apply on the
    // physical plane only and cost one field-word fetch when configured,
    // nothing when not.
    private bool GeographyAllowed(Simulation simulation, int rule, int planeCode, int q, int r)
    {
        var minTemperature = Data.MinTemperature(rule);
        var siteFlags = Data.SiteFlagsFilter(rule);
        var fogAdjacent = Data.RequiresFogAdjacent(rule);
        if ((minTemperature <= 0 && siteFlags == 0 && !fogAdjacent) || planeCode != PlaneCodes.Physical)
        {
            return true;
        }

        var geography = simulation.KernelData.PhysicalGeography;
        if (geography.Fact(q, r, PhysicalHexFactCodes.Temperature) < minTemperature)
        {
            return false;
        }

        if (fogAdjacent && !CellBordersFog(simulation, q, r))
        {
            return false;
        }

        return siteFlags == 0 || (geography.FieldWord(q, r) & siteFlags) != 0;
    }

    // The edge of the known world: a known cell with at least one latent
    // unrevealed neighbour. Exploration verbs hunt these; presence reveal
    // does the opening once the explorer arrives.
    // The soft leash: ground without own-culture support keeps only a
    // fraction of its target weight, so ordinary life orbits the strongholds.
    // Fog-adjacent exploration verbs are exempt - ranging is their job - and
    // cultureless entities answer to no hearth.
    private FixedScalar SupportLeashMultiplier(Simulation simulation, int entityId, int rule, int planeCode, int q, int r)
    {
        if (planeCode != PlaneCodes.Physical || Data.RequiresFogAdjacent(rule))
        {
            return 1;
        }

        var world = simulation.World;
        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return 1;
        }

        var tuning = simulation.Options.KernelTuning.Programs.LowAltitudeVerbs;
        var threshold = tuning.SupportThresholdRawMass * simulation.Knowledge.Modifier(
            cultureId,
            KnowledgeGrantDomainCodes.FrontierThawThreshold);
        if (threshold <= 0 ||
            world.CultureFieldAt(cultureId, PlaneCodes.Physical, q, r).TotalRawMass >= threshold)
        {
            return 1;
        }

        return tuning.UnsupportedTargetMultiplier;
    }

    private static readonly int[] FogNeighborDQ = [1, 1, 0, -1, -1, 0];
    private static readonly int[] FogNeighborDR = [0, -1, -1, 0, 1, 1];

    private static bool CellBordersFog(Simulation simulation, int q, int r)
    {
        var world = simulation.World;
        var geography = simulation.KernelData.PhysicalGeography;
        for (var direction = 0; direction < 6; direction++)
        {
            var nq = q + FogNeighborDQ[direction];
            var nr = r + FogNeighborDR[direction];
            if (!world.CellIsKnown(PlaneCodes.Physical, nq, nr) &&
                geography.TryGetCell(nq, nr, out _, out _, out _))
            {
                return true;
            }
        }

        return false;
    }
}
