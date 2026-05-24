namespace Engine.Kernel;

internal sealed partial class GeneratorProgramRuntime
{
    private const int PlacementPreferredBaseScore = 1000;
    private const int PlacementPreferredRankStep = 140;
    private const int PlacementFallbackTerrainScore = 350;
    private const int PlacementDefaultTerrainScore = 120;
    private const int PlacementAvoidedTerrainScore = -250;
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
    private const float PlacementCultureTraceRawInfluence = WorldState.CultureTraceRawMass;
    private const float PlacementCultureSupportedRawInfluence = WorldState.CultureControlledRawMass;
    private const float PlacementCultureDominantRawInfluence = WorldState.CultureEntrenchedRawMass;
    private const float PlacementCultureConnectedBonusRawInfluence = WorldState.CultureNexusRawMass;

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
            GeneratorCulturePlacementCodes.PreferInheritedCulture or GeneratorCulturePlacementCodes.Ignore =>
                PlacementCellAllowsNonHostileCulture(simulation, planeCode, producedCultureId, q, r),
            _ => PlacementCellAllowsNonHostileCulture(simulation, planeCode, producedCultureId, q, r),
        };
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
            var boundary = simulation.CultureBoundaries.CellAt(simulation.World, producedCultureId, planeCode, q, r);
            return boundary.ForeignRawMass < WorldState.CultureEntrenchedRawMass;
        }

        for (var cultureId = 1; cultureId <= simulation.World.CultureHighWatermark; cultureId++)
        {
            var boundary = simulation.CultureBoundaries.CellAt(simulation.World, cultureId, planeCode, q, r);
            if (boundary.RawMass >= WorldState.CultureEntrenchedRawMass)
            {
                return false;
            }
        }

        return true;
    }

    private bool PlacementCellIsFree(WorldState world, int planeCode, int q, int r)
    {
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
            simulation.KernelData.Terrain.CanEnter(terrainId, movementProfileId);
    }

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
        score -= KernelQuery.HexDistance(0, 0, q, r);
        score += global::System.Math.Max(0, PlacementOriginBonusBase - (KernelQuery.HexDistance(originQ, originR, q, r) * PlacementOriginBonusStep));
        return score;
    }

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

        var boundary = simulation.CultureBoundaries.CellAt(simulation.World, producedCultureId, planeCode, q, r);
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

    private static int PreferInheritedCulturePlacementScore(CultureBoundaryCell boundary)
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

        return boundary.ForeignShare >= 50f ? -PlacementForeignDominantPenalty : 0;
    }

    private static int CultureInteriorPlacementScore(CultureBoundaryCell boundary)
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

        if (boundary.IsLargestComponent)
        {
            score += PlacementCultureInteriorBonus / 2;
        }

        return score;
    }

    private static int CultureFrontierPlacementScore(CultureBoundaryCell boundary)
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

    private static int CultureRoutePlacementScore(CultureBoundaryCell boundary)
    {
        var score = PreferInheritedCulturePlacementScore(boundary);
        if (boundary.RawMass >= PlacementCultureSupportedRawInfluence ||
            CulturePlacementHasSupportedAdjacency(boundary))
        {
            score += PlacementCultureRouteBonus;
        }

        return score;
    }

    private static int WildernessPlacementScore(CultureBoundaryCell boundary)
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

    private static float CulturePlacementConnectedMass(CultureBoundaryCell boundary) =>
        global::System.MathF.Max(boundary.ConnectedRawMass, boundary.AttachedComponentScoreMass);

    private static int CulturePlacementConnectedBonus(float connectedRawInfluence) =>
        (int)(global::System.MathF.Min(1f, connectedRawInfluence / PlacementCultureConnectedBonusRawInfluence) * 240f);

    private static bool CulturePlacementIsDominant(
        float ownInfluence,
        float ownRawInfluence,
        float foreignInfluence) =>
        ownInfluence >= foreignInfluence &&
        ownRawInfluence >= PlacementCultureDominantRawInfluence;

    private static bool CulturePlacementIsPresent(
        float ownInfluence,
        float ownRawInfluence,
        float foreignInfluence) =>
        ownRawInfluence >= PlacementCultureSupportedRawInfluence &&
        ownInfluence >= foreignInfluence * 0.75f;

    private static bool CulturePlacementHasSupportedAdjacency(CultureBoundaryCell boundary) =>
        boundary.StrongestAdjacentRawMass >= PlacementCultureDominantRawInfluence ||
        boundary.LocalRawMass - boundary.RawMass >= PlacementCultureDominantRawInfluence ||
        boundary.AttachedComponentScoreMass >= PlacementCultureDominantRawInfluence;

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
