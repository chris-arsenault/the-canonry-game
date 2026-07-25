namespace Engine.Kernel;

internal sealed partial class GeneratorProgramRuntime
{
    private const int PlacementPreferredBaseScore = 1000;
    private const int PlacementPreferredRankStep = 140;
    private const int PlacementFallbackTerrainScore = 350;
    private const int PlacementDefaultTerrainScore = 120;

    // Avoided terrain is the land/water partition, priced above any
    // realistic taste swing so no authored weight strands a land kind at
    // sea or a swimmer ashore.
    private const int PlacementAvoidedTerrainScore = -1200;
    private const int PlacementNearPreferredNeighborScore = 60;
    private const int PlacementDistantPreferredNeighborScore = 15;
    private const int PlacementOriginBonusBase = 260;
    private const int PlacementOriginBonusStep = 22;
    private const int PlacementCultureDominantScore = 980;
    private const int PlacementCulturePresentScore = 620;
    private const int PlacementCultureTraceScore = 0;
    private const int PlacementCultureAdjacentScore = 260;
    private const int PlacementForeignDominantPenalty = 350;
    private const int PlacementCultureInteriorBonus = 420;
    private const int PlacementCultureFrontierBonus = 360;
    private const int PlacementCultureRouteBonus = 280;
    private const int PlacementWildernessBaseScore = 320;
    private const int PlacementPreferredSiteScore = 860;

    // Full-strength affinity over a maxed fact (255) matches the old
    // three-band peak of 360.
    private static readonly FixedScalar PlacementFactAffinityScale = FixedScalar.FromRatio(360, 255);
    private static readonly FixedScalar PlacementCultureTraceRawInfluence = WorldState.CultureTraceRawMass;
    private static readonly FixedScalar PlacementCultureSupportedRawInfluence = WorldState.CultureControlledRawMass;
    private static readonly FixedScalar PlacementCultureDominantRawInfluence = WorldState.CultureEntrenchedRawMass;
    private static readonly FixedScalar PlacementCultureConnectedBonusRawInfluence = WorldState.CultureNexusRawMass;

    private static readonly int[] PlacementFrontierNeighborDQ = [1, 1, 0, -1, -1, 0];
    private static readonly int[] PlacementFrontierNeighborDR = [0, -1, -1, 0, 1, 1];

    // Places at the leading edge of the produced entity's culture: a known, free,
    // terrain-allowed cell adjacent to ground the culture holds (own raw mass at
    // or above trace) but does not yet hold itself, preferring the outermost such
    // cell. The entity becomes a new anchor there and its emission pushes the
    // frontier out - this is how a culture expands across a plane. Works on both
    // planes; iterates the culture's raw-influence cells (bounded).
    private bool FindTerrainAwareHexAtCultureFrontier(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int movementProfileId,
        int producedCultureId,
        out int q,
        out int r)
    {
        q = 0;
        r = 0;
        if (producedCultureId <= 0)
        {
            return false;
        }

        var world = simulation.World;
        var found = false;
        var bestRing = -1;
        var selectedQ = 0;
        var selectedR = 0;
        var cursor = world.RawCultureInfluenceCellsOf(producedCultureId, planeCode);
        while (world.MoveNextRawCultureInfluenceCell(ref cursor, out var cq, out var cr, out var raw))
        {
            if (raw < WorldState.CultureTraceRawMass)
            {
                continue;
            }

            for (var direction = 0; direction < 6; direction++)
            {
                var nq = cq + PlacementFrontierNeighborDQ[direction];
                var nr = cr + PlacementFrontierNeighborDR[direction];
                var ring = (global::System.Math.Abs(nq) + global::System.Math.Abs(nr) + global::System.Math.Abs(nq + nr)) / 2;
                if (ring <= bestRing ||
                    !FrontierPlacementCellAllowed(simulation, planeCode, producedSlot, movementProfileId, producedCultureId, nq, nr))
                {
                    continue;
                }

                bestRing = ring;
                selectedQ = nq;
                selectedR = nr;
                found = true;
            }
        }

        q = selectedQ;
        r = selectedR;
        return found;
    }

    private bool FrontierPlacementCellAllowed(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int movementProfileId,
        int producedCultureId,
        int q,
        int r)
    {
        var world = simulation.World;
        return world.CellIsKnown(planeCode, q, r) &&
            world.GetCellCultureInfluenceRaw(producedCultureId, planeCode, q, r) < WorldState.CultureTraceRawMass &&
            PlacementCellIsFree(world, planeCode, q, r) &&
            PlacementCellAllowsCulture(simulation, planeCode, producedSlot, producedCultureId, q, r) &&
            PlacementTerrainIsAllowed(simulation, planeCode, producedSlot, movementProfileId, q, r);
    }

    private bool FindTerrainAwareHex(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int movementProfileId,
        int producedCultureId,
        int originQ,
        int originR,
        int minRadius,
        int maxRadius,
        out int q,
        out int r)
    {
        var world = simulation.World;
        var effectiveMaxRadius = EffectivePlacementMaxRadius(world, planeCode, maxRadius);
        var found = false;
        var bestScore = int.MinValue;
        var equalBestCount = 0;
        var selectedQ = originQ;
        var selectedR = originR;
        for (var radius = minRadius; radius <= effectiveMaxRadius; radius++)
        {
            found = TryFindTerrainAwareHexAtRadius(
                simulation,
                planeCode,
                producedSlot,
                movementProfileId,
                producedCultureId,
                originQ,
                originR,
                radius,
                minRadius,
                effectiveMaxRadius,
                ref bestScore,
                ref equalBestCount,
                ref selectedQ,
                ref selectedR) || found;
        }

        q = selectedQ;
        r = selectedR;
        return found;
    }

    private bool TryFindTerrainAwareHexAtRadius(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int movementProfileId,
        int producedCultureId,
        int originQ,
        int originR,
        int radius,
        int minRadius,
        int maxRadius,
        ref int bestScore,
        ref int equalBestCount,
        ref int selectedQ,
        ref int selectedR)
    {
        var found = false;
        for (var dq = -radius; dq <= radius; dq++)
        {
            found = TryFindTerrainAwareHexColumn(
                simulation,
                planeCode,
                producedSlot,
                movementProfileId,
                producedCultureId,
                originQ,
                originR,
                radius,
                dq,
                minRadius,
                maxRadius,
                ref bestScore,
                ref equalBestCount,
                ref selectedQ,
                ref selectedR) || found;
        }

        return found;
    }

    private bool TryFindTerrainAwareHexColumn(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int movementProfileId,
        int producedCultureId,
        int originQ,
        int originR,
        int radius,
        int dq,
        int minRadius,
        int maxRadius,
        ref int bestScore,
        ref int equalBestCount,
        ref int selectedQ,
        ref int selectedR)
    {
        var found = false;
        var minDr = global::System.Math.Max(-radius, -dq - radius);
        var maxDr = global::System.Math.Min(radius, -dq + radius);
        for (var dr = minDr; dr <= maxDr; dr++)
        {
            var candidateQ = originQ + dq;
            var candidateR = originR + dr;
            found = SelectPlacementCandidateIfMatch(
                simulation,
                planeCode,
                producedSlot,
                movementProfileId,
                producedCultureId,
                candidateQ,
                candidateR,
                originQ,
                originR,
                radius,
                minRadius,
                maxRadius,
                ref bestScore,
                ref equalBestCount,
                ref selectedQ,
                ref selectedR) || found;
        }

        return found;
    }

    private bool FindTerrainAwareFreeHexInCoverage(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int movementProfileId,
        int producedCultureId,
        int coverageOwnerId,
        int originQ,
        int originR,
        int minRadius,
        int maxRadius,
        out int q,
        out int r)
    {
        var world = simulation.World;
        var effectiveMaxRadius = EffectivePlacementMaxRadius(world, planeCode, maxRadius);
        var found = false;
        var bestScore = int.MinValue;
        var equalBestCount = 0;
        var selectedQ = originQ;
        var selectedR = originR;
        if (IsPendingRef(coverageOwnerId))
        {
            return FindFreeHexInPendingCoverage(
                simulation,
                planeCode,
                producedSlot,
                movementProfileId,
                producedCultureId,
                coverageOwnerId,
                originQ,
                originR,
                minRadius,
                effectiveMaxRadius,
                out q,
                out r);
        }

        KernelQuery.ForEachCoverageCell(
            world,
            coverageOwnerId,
            planeCode,
            (cq, cr) =>
            {
                found = SelectPlacementCandidateIfMatch(
                    simulation,
                    planeCode,
                    producedSlot,
                    movementProfileId,
                    producedCultureId,
                    cq,
                    cr,
                    originQ,
                    originR,
                    KernelQuery.HexDistance(originQ, originR, cq, cr),
                    minRadius,
                    effectiveMaxRadius,
                    ref bestScore,
                    ref equalBestCount,
                    ref selectedQ,
                    ref selectedR) || found;
                return true;
            });

        q = selectedQ;
        r = selectedR;
        return found;
    }

    // A pending owner's coverage exists only as planned physical cells.
    private bool FindFreeHexInPendingCoverage(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int movementProfileId,
        int producedCultureId,
        int coverageOwnerId,
        int originQ,
        int originR,
        int minRadius,
        int effectiveMaxRadius,
        out int q,
        out int r)
    {
        var found = false;
        var bestScore = int.MinValue;
        var equalBestCount = 0;
        var selectedQ = originQ;
        var selectedR = originR;
        if (planeCode == PlaneCodes.Physical)
        {
            var planIndex = PendingPlanIndex(coverageOwnerId);
            var start = _plannedCoverageStarts[planIndex];
            var end = start + _plannedCoverageCounts[planIndex];
            for (var cell = start; cell < end; cell++)
            {
                var cq = _plannedCoverageCellQ[cell];
                var cr = _plannedCoverageCellR[cell];
                found = SelectPlacementCandidateIfMatch(
                    simulation,
                    planeCode,
                    producedSlot,
                    movementProfileId,
                    producedCultureId,
                    cq,
                    cr,
                    originQ,
                    originR,
                    KernelQuery.HexDistance(originQ, originR, cq, cr),
                    minRadius,
                    effectiveMaxRadius,
                    ref bestScore,
                    ref equalBestCount,
                    ref selectedQ,
                    ref selectedR) || found;
            }
        }

        q = selectedQ;
        r = selectedR;
        return found;
    }

    private bool SelectPlacementCandidateIfMatch(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int movementProfileId,
        int producedCultureId,
        int candidateQ,
        int candidateR,
        int originQ,
        int originR,
        int searchRadius,
        int minRadius,
        int maxRadius,
        ref int bestScore,
        ref int equalBestCount,
        ref int selectedQ,
        ref int selectedR)
    {
        if (!PlacementCandidateMatches(
                simulation,
                planeCode,
                producedSlot,
                movementProfileId,
                producedCultureId,
                candidateQ,
                candidateR,
                originQ,
                originR,
                searchRadius,
                minRadius,
                maxRadius))
        {
            return false;
        }

        var found = true;
        var score = ScorePlacementCell(
            simulation,
            planeCode,
            producedSlot,
            producedCultureId,
            candidateQ,
            candidateR,
            originQ,
            originR);
        SelectPlacementCandidate(
            simulation,
            candidateQ,
            candidateR,
            score,
            ref bestScore,
            ref equalBestCount,
            ref selectedQ,
            ref selectedR,
            ref found);
        return true;
    }

    private bool PlacementCandidateMatches(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int movementProfileId,
        int producedCultureId,
        int q,
        int r,
        int originQ,
        int originR,
        int searchRadius,
        int minRadius,
        int maxRadius)
    {
        var distance = KernelQuery.HexDistance(originQ, originR, q, r);
        return distance == searchRadius &&
            distance >= minRadius &&
            distance <= maxRadius &&
            PlacementCellIsFree(simulation.World, planeCode, q, r) &&
            PlacementCellAllowsCulture(simulation, planeCode, producedSlot, producedCultureId, q, r) &&
            PlacementTerrainIsAllowed(simulation, planeCode, producedSlot, movementProfileId, q, r);
    }

    private bool PlacementCellAllowsCulture(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int producedCultureId,
        int q,
        int r)
    {
        return _producedCulturePlacementCodes[producedSlot] switch
        {
            GeneratorCulturePlacementCodes.HostileIntrusion => true,
            GeneratorCulturePlacementCodes.CultureRoute or GeneratorCulturePlacementCodes.CultureInterior =>
                PlacementCellAllowsNonHostileCulture(simulation, planeCode, producedCultureId, q, r) &&
                PlacementCellHasOwnCultureSupport(simulation, planeCode, producedCultureId, q, r),
            GeneratorCulturePlacementCodes.PreferInheritedCulture or GeneratorCulturePlacementCodes.Ignore =>
                PlacementCellAllowsNonHostileCulture(simulation, planeCode, producedCultureId, q, r),
            _ => PlacementCellAllowsNonHostileCulture(simulation, planeCode, producedCultureId, q, r),
        };
    }

    // Interior and route outputs (settlements, factions, harbors, reefholds)
    // may only be founded where their culture already has presence, adjacency,
    // or connected mass. Without this gate one distant or mobile target could
    // seed a permanent same-culture satellite in unsupported void, fragmenting
    // the culture's territory.
    private bool PlacementCellHasOwnCultureSupport(
        Simulation simulation,
        int planeCode,
        int producedCultureId,
        int q,
        int r)
    {
        if (producedCultureId <= 0)
        {
            return true;
        }

        var boundary = simulation.CultureLocality.CellAt(simulation.World, producedCultureId, planeCode, q, r);
        return boundary.RawMass >= WorldState.CultureTraceRawMass ||
            boundary.StrongestAdjacentRawMass >= WorldState.CultureClaimedRawMass ||
            boundary.ConnectedRawMass >= WorldState.CultureControlledRawMass;
    }

    private bool PlacementCellAllowsNonHostileCulture(
        Simulation simulation,
        int planeCode,
        int producedCultureId,
        int q,
        int r)
    {
        if (producedCultureId > 0)
        {
            var boundary = simulation.CultureLocality.CellAt(simulation.World, producedCultureId, planeCode, q, r);
            return boundary.ForeignRawMass < WorldState.CultureEntrenchedRawMass;
        }

        for (var cultureId = 1; cultureId <= simulation.World.CultureHighWatermark; cultureId++)
        {
            var boundary = simulation.CultureLocality.CellAt(simulation.World, cultureId, planeCode, q, r);
            if (boundary.RawMass >= WorldState.CultureEntrenchedRawMass)
            {
                return false;
            }
        }

        return true;
    }

    private bool PlacementCellIsFree(WorldState world, int planeCode, int q, int r)
    {
        if (PendingPlacementOccupies(planeCode, q, r))
        {
            return false;
        }

        return planeCode == PlaneCodes.Physical
            ? IsFreePhysical(world, q, r)
            : SemanticCoordinateAllowed(world, q, r) && !KernelQuery.SemanticCellIsOccupied(world, q, r);
    }

    private bool PlacementTerrainIsAllowed(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int movementProfileId,
        int q,
        int r)
    {
        var terrainId = simulation.World.CellTerrainId(planeCode, q, r);
        var allowedStart = PlacementAllowedTerrainStart(planeCode, producedSlot);
        var allowedCount = PlacementAllowedTerrainCount(planeCode, producedSlot);
        return TerrainInFilter(terrainId, allowedStart, allowedCount, emptyPasses: true) &&
            simulation.KernelData.Terrain.CanEnter(terrainId, movementProfileId) &&
            PhysicalPlacementRespectsPartition(planeCode, producedSlot, terrainId);
    }

    // On the physical plane the avoided set is the land/water partition: a
    // hard rule, never outscored. The radius walk widens past an exhausted
    // ring on its own. Semantic avoidance stays a soft score - hostile idea
    // ground repels, it does not forbid.
    private bool PhysicalPlacementRespectsPartition(int planeCode, int producedSlot, int terrainId) =>
        planeCode != PlaneCodes.Physical ||
        !TerrainInFilter(
            terrainId,
            PlacementAvoidedTerrainStart(planeCode, producedSlot),
            PlacementAvoidedTerrainCount(planeCode, producedSlot),
            emptyPasses: false);

    private int ScorePlacementCell(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int producedCultureId,
        int q,
        int r,
        int originQ,
        int originR)
    {
        var terrainId = simulation.World.CellTerrainId(planeCode, q, r);
        var score = TerrainBasePlacementScore(planeCode, producedSlot, terrainId);
        score += PreferredNeighborPlacementScore(simulation.World, planeCode, producedSlot, q, r);
        score += CulturePlacementScore(simulation, planeCode, producedSlot, producedCultureId, q, r);
        score += GeographyPlacementScore(simulation, planeCode, producedSlot, q, r);
        if (planeCode == PlaneCodes.Physical)
        {
            score += (int)simulation.PlacementTasteScore(
                Data.ProducedKindIds[producedSlot],
                producedCultureId,
                q,
                r);
        }

        score -= KernelQuery.HexDistance(0, 0, q, r);
        score += global::System.Math.Max(0, PlacementOriginBonusBase - (KernelQuery.HexDistance(originQ, originR, q, r) * PlacementOriginBonusStep));
        return score;
    }

    // Geography pulls content toward the hexes that suit it: a preferred
    // site flag scores like a top terrain preference, and each signed
    // affinity adds its real quantized fact times a fixed step. Producers
    // with no authored geography pay one array read and no fact fetches.
    private int GeographyPlacementScore(Simulation simulation, int planeCode, int producedSlot, int q, int r)
    {
        if (planeCode != PlaneCodes.Physical)
        {
            return 0;
        }

        var siteFlags = Data.ProducedPlacementSiteFlags[producedSlot];
        var baseIndex = producedSlot * GeneratorProgramData.PlacementBandAffinityCount;
        var affinities = Data.ProducedPlacementBandAffinities;
        var warmth = affinities[baseIndex + GeneratorProgramData.PlacementWarmthAffinityOffset];
        var fire = affinities[baseIndex + GeneratorProgramData.PlacementFireAffinityOffset];
        var aurora = affinities[baseIndex + GeneratorProgramData.PlacementAuroraAffinityOffset];
        var pressure = affinities[baseIndex + GeneratorProgramData.PlacementPressureAffinityOffset];
        if (siteFlags == 0 && warmth == 0 && fire == 0 && aurora == 0 && pressure == 0)
        {
            return 0;
        }

        var geography = simulation.KernelData.PhysicalGeography;
        var score = 0;
        if (siteFlags != 0 && (geography.FieldWord(q, r) & siteFlags) != 0)
        {
            score += PlacementPreferredSiteScore;
        }

        score += FactAffinityScore(geography, warmth, q, r, PhysicalHexFactCodes.Temperature);
        score += FactAffinityScore(geography, fire, q, r, PhysicalHexFactCodes.FireMean);
        score += FactAffinityScore(geography, aurora, q, r, PhysicalHexFactCodes.AuroraMean);
        score += FactAffinityScore(geography, pressure, q, r, PhysicalHexFactCodes.PressureMean);
        return score;
    }

    private static int FactAffinityScore(
        KernelPhysicalGeographyData geography,
        FixedScalar affinity,
        int q,
        int r,
        int factCode) =>
        affinity == 0
            ? 0
            : (int)(affinity * geography.Fact(q, r, factCode) * PlacementFactAffinityScale);

    private int CulturePlacementScore(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int producedCultureId,
        int q,
        int r)
    {
        var placementCode = _producedCulturePlacementCodes[producedSlot];
        if (placementCode == GeneratorCulturePlacementCodes.Ignore ||
            placementCode == GeneratorCulturePlacementCodes.HostileIntrusion ||
            producedCultureId <= 0)
        {
            return 0;
        }

        var boundary = simulation.CultureLocality.CellAt(simulation.World, producedCultureId, planeCode, q, r);
        if (placementCode == GeneratorCulturePlacementCodes.CultureInterior)
        {
            return CultureInteriorPlacementScore(boundary);
        }

        if (placementCode == GeneratorCulturePlacementCodes.CultureFrontier)
        {
            return CultureFrontierPlacementScore(boundary);
        }

        if (placementCode == GeneratorCulturePlacementCodes.CultureRoute)
        {
            return CultureRoutePlacementScore(boundary);
        }

        if (placementCode == GeneratorCulturePlacementCodes.Wilderness)
        {
            return WildernessPlacementScore(boundary);
        }

        return PreferInheritedCulturePlacementScore(boundary);
    }

    private static int PreferInheritedCulturePlacementScore(CultureLocalityCell boundary)
    {
        var connectedRawInfluence = CulturePlacementConnectedMass(boundary);
        if (CulturePlacementIsDominant(boundary.Share, boundary.RawMass, boundary.ForeignShare))
        {
            return PlacementCultureDominantScore + CulturePlacementConnectedBonus(connectedRawInfluence);
        }

        if (CulturePlacementIsPresent(boundary.Share, boundary.RawMass, boundary.ForeignShare))
        {
            return PlacementCulturePresentScore + CulturePlacementConnectedBonus(connectedRawInfluence);
        }

        if (boundary.RawMass >= PlacementCultureTraceRawInfluence)
        {
            return PlacementCultureTraceScore;
        }

        if (CulturePlacementHasSupportedAdjacency(boundary))
        {
            return PlacementCultureAdjacentScore + CulturePlacementConnectedBonus(connectedRawInfluence);
        }

        return boundary.ForeignShare >= 50 ? -PlacementForeignDominantPenalty : 0;
    }

    private static int CultureInteriorPlacementScore(CultureLocalityCell boundary)
    {
        var score = PreferInheritedCulturePlacementScore(boundary);
        if (boundary.RawMass < PlacementCultureSupportedRawInfluence)
        {
            return score - PlacementForeignDominantPenalty;
        }

        if (boundary.CellKindCode == CultureFieldCellKindCodes.Interior)
        {
            score += PlacementCultureInteriorBonus;
        }

        if (boundary.RawMass >= WorldState.CultureControlledRawMass)
        {
            score += PlacementCultureInteriorBonus / 2;
        }

        return score;
    }

    private static int CultureFrontierPlacementScore(CultureLocalityCell boundary)
    {
        var score = PreferInheritedCulturePlacementScore(boundary);
        if (boundary.CellKindCode == CultureFieldCellKindCodes.Border ||
            boundary.CellKindCode == CultureFieldCellKindCodes.Contested)
        {
            score += PlacementCultureFrontierBonus;
        }

        if (boundary.RawMass < PlacementCultureDominantRawInfluence &&
            CulturePlacementHasSupportedAdjacency(boundary))
        {
            score += PlacementCultureFrontierBonus / 2;
        }

        return score;
    }

    private static int CultureRoutePlacementScore(CultureLocalityCell boundary)
    {
        var score = PreferInheritedCulturePlacementScore(boundary);
        if (boundary.RawMass >= PlacementCultureSupportedRawInfluence ||
            CulturePlacementHasSupportedAdjacency(boundary))
        {
            score += PlacementCultureRouteBonus;
        }

        return score;
    }

    private static int WildernessPlacementScore(CultureLocalityCell boundary)
    {
        if (boundary.TotalRawMass <= PlacementCultureTraceRawInfluence)
        {
            return PlacementWildernessBaseScore;
        }

        if (boundary.RawMass >= PlacementCultureSupportedRawInfluence ||
            boundary.ForeignRawMass >= PlacementCultureSupportedRawInfluence)
        {
            return -PlacementForeignDominantPenalty;
        }

        return 0;
    }

    private static FixedScalar CulturePlacementConnectedMass(CultureLocalityCell boundary) =>
        boundary.ConnectedRawMass;

    private static int CulturePlacementConnectedBonus(FixedScalar connectedRawInfluence) =>
        (int)(FixedScalar.Min(1, connectedRawInfluence / PlacementCultureConnectedBonusRawInfluence) * 240);

    private static bool CulturePlacementIsDominant(
        FixedScalar ownInfluence,
        FixedScalar ownRawInfluence,
        FixedScalar foreignInfluence) =>
        ownInfluence >= foreignInfluence &&
        ownRawInfluence >= PlacementCultureDominantRawInfluence;

    private static bool CulturePlacementIsPresent(
        FixedScalar ownInfluence,
        FixedScalar ownRawInfluence,
        FixedScalar foreignInfluence) =>
        ownRawInfluence >= PlacementCultureSupportedRawInfluence &&
        ownInfluence >= foreignInfluence * FixedScalar.FromUnits(750000L);

    private static bool CulturePlacementHasSupportedAdjacency(CultureLocalityCell boundary) =>
        boundary.StrongestAdjacentRawMass >= PlacementCultureDominantRawInfluence ||
        boundary.LocalRawMass - boundary.RawMass >= PlacementCultureDominantRawInfluence;

    private int TerrainBasePlacementScore(int planeCode, int producedSlot, int terrainId)
    {
        var preferredRank = PreferredTerrainRank(planeCode, producedSlot, terrainId);
        if (preferredRank >= 0)
        {
            return PlacementPreferredBaseScore - (preferredRank * PlacementPreferredRankStep);
        }

        if (_producedPlacementFallbackTerrainIds[PlacementTerrainSlot(producedSlot, planeCode)] == terrainId)
        {
            return PlacementFallbackTerrainScore;
        }

        if (TerrainInFilter(
                terrainId,
                PlacementAvoidedTerrainStart(planeCode, producedSlot),
                PlacementAvoidedTerrainCount(planeCode, producedSlot),
                emptyPasses: false))
        {
            return PlacementAvoidedTerrainScore;
        }

        return PlacementDefaultTerrainScore;
    }

    private int PreferredNeighborPlacementScore(WorldState world, int planeCode, int producedSlot, int q, int r)
    {
        if (PlacementPreferredTerrainCount(planeCode, producedSlot) == 0)
        {
            return 0;
        }

        var score = 0;
        for (var direction = 0; direction < 6; direction++)
        {
            var neighborQ = q + DirectionQ(direction);
            var neighborR = r + DirectionR(direction);
            if (PlacementCellHasPreferredTerrain(world, planeCode, producedSlot, neighborQ, neighborR))
            {
                score += PlacementNearPreferredNeighborScore;
            }
        }

        for (var dq = -2; dq <= 2; dq++)
        {
            var minDr = global::System.Math.Max(-2, -dq - 2);
            var maxDr = global::System.Math.Min(2, -dq + 2);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                if (KernelQuery.HexDistance(0, 0, dq, dr) != 2)
                {
                    continue;
                }

                if (PlacementCellHasPreferredTerrain(world, planeCode, producedSlot, q + dq, r + dr))
                {
                    score += PlacementDistantPreferredNeighborScore;
                }
            }
        }

        return score;
    }

    private bool PlacementCellHasPreferredTerrain(WorldState world, int planeCode, int producedSlot, int q, int r)
    {
        if (!PlacementCoordinateHasTerrain(world, planeCode, q, r))
        {
            return false;
        }

        return PreferredTerrainRank(planeCode, producedSlot, world.CellTerrainId(planeCode, q, r)) >= 0;
    }

    private bool PlacementCoordinateHasTerrain(WorldState world, int planeCode, int q, int r)
    {
        return planeCode == PlaneCodes.Physical
            ? world.CellIsKnown(q, r)
            : SemanticCoordinateAllowed(world, q, r);
    }

    private int PreferredTerrainRank(int planeCode, int producedSlot, int terrainId)
    {
        var start = PlacementPreferredTerrainStart(planeCode, producedSlot);
        var end = start + PlacementPreferredTerrainCount(planeCode, producedSlot);
        for (var i = start; i < end; i++)
        {
            if (_terrainFilterIds[i] == terrainId)
            {
                return i - start;
            }
        }

        return -1;
    }

    private static void SelectPlacementCandidate(
        Simulation simulation,
        int candidateQ,
        int candidateR,
        int score,
        ref int bestScore,
        ref int equalBestCount,
        ref int selectedQ,
        ref int selectedR,
        ref bool found)
    {
        if (score > bestScore)
        {
            bestScore = score;
            equalBestCount = 1;
            selectedQ = candidateQ;
            selectedR = candidateR;
            found = true;
            return;
        }

        if (score != bestScore)
        {
            return;
        }

        equalBestCount++;
        if (simulation.NextInt(equalBestCount) == 0)
        {
            selectedQ = candidateQ;
            selectedR = candidateR;
            found = true;
        }
    }

    private int PlacementPreferredTerrainStart(int planeCode, int producedSlot) =>
        _producedPlacementPreferredTerrainStarts[PlacementTerrainSlot(producedSlot, planeCode)];

    private int PlacementPreferredTerrainCount(int planeCode, int producedSlot) =>
        _producedPlacementPreferredTerrainCounts[PlacementTerrainSlot(producedSlot, planeCode)];

    private int PlacementAllowedTerrainStart(int planeCode, int producedSlot) =>
        _producedPlacementAllowedTerrainStarts[PlacementTerrainSlot(producedSlot, planeCode)];

    private int PlacementAllowedTerrainCount(int planeCode, int producedSlot) =>
        _producedPlacementAllowedTerrainCounts[PlacementTerrainSlot(producedSlot, planeCode)];

    private int PlacementAvoidedTerrainStart(int planeCode, int producedSlot) =>
        _producedPlacementAvoidedTerrainStarts[PlacementTerrainSlot(producedSlot, planeCode)];

    private int PlacementAvoidedTerrainCount(int planeCode, int producedSlot) =>
        _producedPlacementAvoidedTerrainCounts[PlacementTerrainSlot(producedSlot, planeCode)];

    private static int PlacementTerrainSlot(int producedSlot, int planeCode)
    {
        var planeIndex = planeCode == PlaneCodes.Physical ? 0 : 1;
        return ((producedSlot - 1) * 2) + planeIndex + 1;
    }
}
