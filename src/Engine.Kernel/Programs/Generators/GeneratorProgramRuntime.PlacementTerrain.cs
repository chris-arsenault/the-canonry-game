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

    private bool FindTerrainAwareHex(
        Simulation simulation,
        int planeCode,
        int producedSlot,
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
                candidateQ,
                candidateR,
                originQ,
                originR,
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
                    cq,
                    cr,
                    originQ,
                    originR,
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
        int candidateQ,
        int candidateR,
        int originQ,
        int originR,
        int minRadius,
        int maxRadius,
        ref int bestScore,
        ref int equalBestCount,
        ref int selectedQ,
        ref int selectedR)
    {
        var kindId = _producedKindIds[producedSlot];
        if (!PlacementCandidateMatches(
                simulation,
                planeCode,
                producedSlot,
                kindId,
                candidateQ,
                candidateR,
                originQ,
                originR,
                minRadius,
                maxRadius))
        {
            return false;
        }

        var found = true;
        var score = ScorePlacementCell(simulation, planeCode, producedSlot, candidateQ, candidateR, originQ, originR);
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
        int kindId,
        int q,
        int r,
        int originQ,
        int originR,
        int minRadius,
        int maxRadius)
    {
        var distance = KernelQuery.HexDistance(originQ, originR, q, r);
        return distance >= minRadius &&
            distance <= maxRadius &&
            PlacementCellIsFree(simulation.World, planeCode, q, r) &&
            PlacementTerrainIsAllowed(simulation, planeCode, producedSlot, kindId, q, r);
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
        int kindId,
        int q,
        int r)
    {
        var terrainId = simulation.World.CellTerrainId(planeCode, q, r);
        var allowedStart = PlacementAllowedTerrainStart(planeCode, producedSlot);
        var allowedCount = PlacementAllowedTerrainCount(planeCode, producedSlot);
        return TerrainInFilter(terrainId, allowedStart, allowedCount, emptyPasses: true) &&
            simulation.KernelData.Terrain.CanEnter(terrainId, kindId);
    }

    private int ScorePlacementCell(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int q,
        int r,
        int originQ,
        int originR)
    {
        var terrainId = simulation.World.CellTerrainId(planeCode, q, r);
        var score = TerrainBasePlacementScore(planeCode, producedSlot, terrainId);
        score += PreferredNeighborPlacementScore(simulation.World, planeCode, producedSlot, q, r);
        score -= KernelQuery.HexDistance(0, 0, q, r);
        score += global::System.Math.Max(0, PlacementOriginBonusBase - (KernelQuery.HexDistance(originQ, originR, q, r) * PlacementOriginBonusStep));
        return score;
    }

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
