namespace IceRemembers.Seed;

using Engine.Domain.Spatial;
using Engine.Domain.Startup;
using Engine.Graph;
using Engine.Kinds;

using IceRemembers;

internal static partial class IceRemembersSeedBuilder
{
    private const float SeedPlacementPreferredBaseScore = 100f;
    private const float SeedPlacementPreferredRankStep = 14f;
    private const float SeedPlacementFallbackTerrainScore = 35f;
    private const float SeedPlacementDefaultTerrainScore = 12f;

    // The avoided set is the land/water partition, so it must outweigh any
    // authored taste swing: a water-loving culture prices coasts up, it
    // does not put a rookery in the open sea.
    private const float SeedPlacementAvoidedTerrainScore = -120f;
    private const float SeedPlacementNearPreferredNeighborScore = 6f;
    private const float SeedPlacementDistantPreferredNeighborScore = 1.5f;
    private const float SeedPlacementOriginBonusBase = 26f;
    private const float SeedPlacementOriginBonusStep = 2.2f;
    private const float SeedPlacementCenterPenaltyStep = 0.08f;

    // Physical picks draw from a wider slice of the ranked candidates than
    // semantic ones: taste differentiates real geography, and the wider
    // draw keeps kind after kind from stacking onto the same best hex.
    private const int SeedPlacementPhysicalPickWidth = 8;

    private static HexCoord ChoosePhysicalCoordinate(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> taken,
        EntityKind kind,
        string subtype,
        string culture,
        int minimumDistance,
        HexCoord? origin,
        int searchRadius,
        Func<HexCell, float>? contextScorer = null,
        Func<HexCell, bool>? candidateFilter = null) =>
        ChoosePhysicalCoordinate(
            seed,
            random,
            taken,
            PhysicalTerrainProfile(kind, subtype, culture),
            minimumDistance,
            origin,
            searchRadius,
            WithTasteScorer(seed, kind, culture, contextScorer),
            candidateFilter);

    /// <summary>
    /// Folds the kind and culture placement taste into the context score,
    /// so seed placement prices hexes with the same taste the kernel uses.
    /// </summary>
    private static Func<HexCell, float> WithTasteScorer(
        SeedAssembly seed,
        EntityKind kind,
        string? culture,
        Func<HexCell, float>? contextScorer) =>
        contextScorer is null
            ? cell => seed.Taste.Score(kind, culture, cell.Coord)
            : cell => contextScorer(cell) + seed.Taste.Score(kind, culture, cell.Coord);

    private static HexCoord ChoosePhysicalCoordinate(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> taken,
        IceRemembersPlacementTerrainProfile profile,
        int minimumDistance,
        HexCoord? origin,
        int searchRadius,
        Func<HexCell, float>? contextScorer = null,
        Func<HexCell, bool>? candidateFilter = null)
    {
        var candidates = RankedPhysicalCandidates(
            seed,
            taken,
            profile,
            minimumDistance,
            origin,
            searchRadius,
            contextScorer,
            candidateFilter);
        if (candidates.Count == 0 && origin.HasValue)
        {
            // Legal ground may simply be outside the ring - a harbor's
            // penguins still belong near the harbor, so widen before
            // abandoning the anchor entirely.
            candidates = RankedPhysicalCandidates(
                seed,
                taken,
                profile,
                minimumDistance,
                origin,
                searchRadius * 3,
                contextScorer,
                candidateFilter);
        }

        if (candidates.Count == 0 && origin.HasValue)
        {
            candidates = RankedPhysicalCandidates(
                seed,
                taken,
                profile,
                minimumDistance,
                origin: null,
                searchRadius: 0,
                contextScorer: contextScorer,
                candidateFilter: candidateFilter);
        }

        if (candidates.Count == 0 && minimumDistance > 0)
        {
            candidates = RankedPhysicalCandidates(
                seed,
                taken,
                profile,
                minimumDistance: 0,
                origin: origin,
                searchRadius: searchRadius,
                contextScorer: contextScorer,
                candidateFilter: candidateFilter);
        }

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("Could not find a free physical coordinate for seed placement.");
        }

        return candidates[random.Next(Math.Min(SeedPlacementPhysicalPickWidth, candidates.Count))].Coord;
    }

    private static void SetPhysical(SeedEntity entity, HexCoord coord, HashSet<HexCoord> taken)
    {
        ReservePhysical(coord, taken, entity.Name);
        entity.Coordinates = coord;
    }

    private static void SetPhysicalNaturalNear(
        SeedAssembly seed,
        SeedEntity entity,
        HexCoord origin,
        Random random,
        HashSet<HexCoord> taken,
        int searchRadius,
        int minimumDistance = 0,
        Func<HexCell, float>? contextScorer = null)
    {
        var coord = ChoosePhysicalCoordinate(
            seed,
            random,
            taken,
            entity.Kind,
            entity.Subtype,
            entity.Culture,
            minimumDistance,
            origin,
            searchRadius,
            contextScorer);
        SetPhysical(entity, coord, taken);
    }

    private static void ReservePhysical(HexCoord coord, HashSet<HexCoord> taken, string label)
    {
        if (!taken.Add(coord))
        {
            throw new InvalidOperationException(
                $"Physical position ({coord.Q},{coord.R}) already taken while placing {label}; duplicate starting coords.");
        }
    }

    private static void SetSemantic(SeedEntity entity, SemanticHexCoord coord, HashSet<SemanticHexCoord> taken)
    {
        if (!taken.Add(coord))
        {
            throw new InvalidOperationException(
                $"Semantic position ({coord.Q},{coord.R}) already taken; duplicate starting coords.");
        }

        entity.SemanticPosition = coord;
    }

    private static void SetSemanticNatural(
        SeedAssembly seed,
        SeedEntity entity,
        Random random,
        HashSet<SemanticHexCoord> taken,
        int minimumDistance = 3)
    {
        var coord = ChooseSemanticCoordinate(
            seed,
            random,
            taken,
            SemanticTerrainProfile(entity),
            minimumDistance,
            origin: null,
            searchRadius: 0);
        SetSemantic(entity, coord, taken);
    }

    private static void SetSemanticNaturalNear(
        SeedAssembly seed,
        SeedEntity entity,
        SeedEntity anchor,
        Random random,
        HashSet<SemanticHexCoord> taken,
        int searchRadius,
        int minimumDistance = 3)
    {
        var coord = ChooseSemanticCoordinate(
            seed,
            random,
            taken,
            SemanticTerrainProfile(entity),
            minimumDistance,
            anchor.SemanticPosition,
            searchRadius);
        SetSemantic(entity, coord, taken);
    }

    private static SemanticHexCoord ChooseSemanticCoordinate(
        SeedAssembly seed,
        Random random,
        HashSet<SemanticHexCoord> taken,
        IceRemembersPlacementTerrainProfile profile,
        int minimumDistance,
        SemanticHexCoord? origin,
        int searchRadius)
    {
        var candidates = RankedSemanticCandidates(
            seed,
            taken,
            profile,
            minimumDistance,
            origin,
            searchRadius);
        if (candidates.Count == 0 && origin.HasValue)
        {
            candidates = RankedSemanticCandidates(
                seed,
                taken,
                profile,
                minimumDistance,
                origin: null,
                searchRadius: 0);
        }

        if (candidates.Count == 0 && minimumDistance > 0)
        {
            candidates = RankedSemanticCandidates(
                seed,
                taken,
                profile,
                minimumDistance: 0,
                origin: null,
                searchRadius: 0);
        }

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("Could not find a free semantic coordinate for seed placement.");
        }

        return candidates[random.Next(Math.Min(4, candidates.Count))].Coord;
    }

    private static List<(SemanticHexCoord Coord, float Score)> RankedSemanticCandidates(
        SeedAssembly seed,
        HashSet<SemanticHexCoord> taken,
        IceRemembersPlacementTerrainProfile profile,
        int minimumDistance,
        SemanticHexCoord? origin,
        int searchRadius) =>
        seed.SemanticCells.Values
            .Select(cell => (Coord: new SemanticHexCoord(cell.Q, cell.R), Cell: cell))
            .Where(candidate =>
                !taken.Contains(candidate.Coord) &&
                SemanticCandidateIsInsideSearch(candidate.Coord, origin, searchRadius) &&
                SemanticCandidateHasMinimumDistance(candidate.Coord, taken, minimumDistance))
            .Select(candidate => (
                candidate.Coord,
                Score: SemanticPlacementScore(seed, candidate.Cell, candidate.Coord, profile, origin)))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Coord.Q)
            .ThenBy(candidate => candidate.Coord.R)
            .Take(24)
            .ToList();

    private static List<(HexCoord Coord, float Score)> RankedPhysicalCandidates(
        SeedAssembly seed,
        HashSet<HexCoord> taken,
        IceRemembersPlacementTerrainProfile profile,
        int minimumDistance,
        HexCoord? origin,
        int searchRadius,
        Func<HexCell, float>? contextScorer,
        Func<HexCell, bool>? candidateFilter) =>
        seed.Grid.AllCells
            .Where(cell =>
                !taken.Contains(cell.Coord) &&
                // The land/water partition is a hard rule, not a preference:
                // an avoided cell never enters candidacy, and an exhausted
                // pool widens the search instead of settling for the sea.
                !profile.AvoidedTerrains.Contains(cell.Terrain.StableName) &&
                (candidateFilter is null || candidateFilter(cell)) &&
                PhysicalCandidateIsInsideSearch(cell.Coord, origin, searchRadius) &&
                PhysicalCandidateHasMinimumDistance(cell.Coord, taken, minimumDistance))
            .Select(cell => (
                cell.Coord,
                Score: PhysicalPlacementScore(seed, cell, profile, origin, contextScorer)))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Coord.Q)
            .ThenBy(candidate => candidate.Coord.R)
            .Take(24)
            .ToList();

    private static bool PhysicalCandidateIsInsideSearch(
        HexCoord coord,
        HexCoord? origin,
        int searchRadius) =>
        !origin.HasValue ||
        HexCoord.Distance(origin.Value, coord) <= searchRadius;

    private static bool PhysicalCandidateHasMinimumDistance(
        HexCoord coord,
        HashSet<HexCoord> taken,
        int minimumDistance) =>
        minimumDistance <= 0 ||
        taken.All(existing => HexCoord.Distance(existing, coord) >= minimumDistance);

    private static bool IsMaritimeSettlementCell(HexGrid grid, HexCell cell)
    {
        var terrain = cell.Terrain.StableName;
        if (string.Equals(terrain, IceRemembersTerrain.Reef.ToDomainName(), StringComparison.Ordinal) ||
            string.Equals(terrain, IceRemembersTerrain.Shelf.ToDomainName(), StringComparison.Ordinal))
        {
            return true;
        }

        return cell.Coord.Neighbors()
            .Select(grid.GetCell)
            .Any(neighbor => neighbor is not null &&
                (string.Equals(neighbor.Terrain.StableName, IceRemembersTerrain.Ocean.ToDomainName(), StringComparison.Ordinal) ||
                 string.Equals(neighbor.Terrain.StableName, IceRemembersTerrain.Reef.ToDomainName(), StringComparison.Ordinal) ||
                 string.Equals(neighbor.Terrain.StableName, IceRemembersTerrain.Shelf.ToDomainName(), StringComparison.Ordinal)));
    }

    private static bool SemanticCandidateIsInsideSearch(
        SemanticHexCoord coord,
        SemanticHexCoord? origin,
        int searchRadius) =>
        !origin.HasValue ||
        SemanticHexCoord.Distance(origin.Value, coord) <= searchRadius;

    private static bool SemanticCandidateHasMinimumDistance(
        SemanticHexCoord coord,
        HashSet<SemanticHexCoord> taken,
        int minimumDistance) =>
        minimumDistance <= 0 ||
        taken.All(existing => SemanticHexCoord.Distance(existing, coord) >= minimumDistance);

    private static float SemanticPlacementScore(
        SeedAssembly seed,
        DomainSeedCell cell,
        SemanticHexCoord coord,
        IceRemembersPlacementTerrainProfile profile,
        SemanticHexCoord? origin)
    {
        var score = TerrainProfilePlacementScore(cell.Terrain, profile);
        score += CountPreferredSemanticNeighbors(seed, coord, profile, radius: 1) * SeedPlacementNearPreferredNeighborScore;
        score += CountPreferredSemanticNeighbors(seed, coord, profile, radius: 2) * SeedPlacementDistantPreferredNeighborScore;
        if (origin.HasValue)
        {
            score += Math.Max(0f, SeedPlacementOriginBonusBase - (SemanticHexCoord.Distance(origin.Value, coord) * SeedPlacementOriginBonusStep));
        }

        return score;
    }

    private static float PhysicalPlacementScore(
        SeedAssembly seed,
        HexCell cell,
        IceRemembersPlacementTerrainProfile profile,
        HexCoord? origin,
        Func<HexCell, float>? contextScorer)
    {
        var score = TerrainProfilePlacementScore(cell.Terrain.StableName, profile);
        score += CountPreferredPhysicalNeighbors(seed.Grid, cell.Coord, profile, radius: 1) * SeedPlacementNearPreferredNeighborScore;
        score += CountPreferredPhysicalNeighbors(seed.Grid, cell.Coord, profile, radius: 2) * SeedPlacementDistantPreferredNeighborScore;
        score -= HexCoord.Distance(new HexCoord(0, 0), cell.Coord) * SeedPlacementCenterPenaltyStep;
        if (origin.HasValue)
        {
            score += Math.Max(0f, SeedPlacementOriginBonusBase - (HexCoord.Distance(origin.Value, cell.Coord) * SeedPlacementOriginBonusStep));
        }

        if (contextScorer is not null)
        {
            score += contextScorer(cell);
        }

        return score;
    }

    private static float TerrainProfilePlacementScore(
        string terrainName,
        IceRemembersPlacementTerrainProfile profile)
    {
        for (var i = 0; i < profile.PreferredTerrainOrder.Count; i++)
        {
            if (string.Equals(terrainName, profile.PreferredTerrainOrder[i], StringComparison.Ordinal))
            {
                return SeedPlacementPreferredBaseScore - (i * SeedPlacementPreferredRankStep);
            }
        }

        if (profile.FallbackTerrain is not null &&
            string.Equals(terrainName, profile.FallbackTerrain, StringComparison.Ordinal))
        {
            return SeedPlacementFallbackTerrainScore;
        }

        return profile.AvoidedTerrains.Contains(terrainName)
            ? SeedPlacementAvoidedTerrainScore
            : SeedPlacementDefaultTerrainScore;
    }

    private static int CountPreferredSemanticNeighbors(
        SeedAssembly seed,
        SemanticHexCoord center,
        IceRemembersPlacementTerrainProfile profile,
        int radius)
    {
        return SemanticHexCoord.CoordsInRadius(center, radius)
            .Count(coord =>
                SemanticHexCoord.Distance(center, coord) > 0 &&
                seed.SemanticCells.TryGetValue(coord, out var cell) &&
                profile.PreferredTerrains.Contains(cell.Terrain));
    }

    private static int CountPreferredPhysicalNeighbors(
        HexGrid grid,
        HexCoord center,
        IceRemembersPlacementTerrainProfile profile,
        int radius) =>
        HexCoord.CoordsInRadius(center, radius)
            .Count(coord =>
                HexCoord.Distance(center, coord) > 0 &&
                grid.GetCell(coord) is { } cell &&
                profile.PreferredTerrains.Contains(cell.Terrain.StableName));

    private static IceRemembersPlacementTerrainProfile PhysicalTerrainProfile(
        EntityKind kind,
        string subtype,
        string culture) =>
        IceRemembersPlacementTerrainProfiles.Physical(kind, subtype, culture);

    private static IceRemembersPlacementTerrainProfile SemanticTerrainProfile(SeedEntity entity) =>
        IceRemembersPlacementTerrainProfiles.Semantic(entity.Kind, entity.Subtype, entity.Culture);

    private static MapCoverage CreateSeedCoverage(
        HexGrid grid,
        MapCoverageMap coverageMap,
        SeedAssembly assembly,
        MapCoverage seed,
        HexCoord origin,
        int desiredCellCount,
        int minimumCellCount,
        IReadOnlySet<string> preferredTerrains,
        HashSet<HexCoord> takenPhysical)
    {
        var result = coverageMap.CreateCoverage(
            grid,
            new MapCoveragePlacementRequest
            {
                Name = seed.Name,
                OwnerEntityId = seed.OwnerEntityId,
                Origin = origin,
                Mode = CoveragePlacementMode.CreateNew,
                DesiredCellCount = desiredCellCount,
                MinimumCellCount = minimumCellCount,
                PreferredTerrains = preferredTerrains,
                RespectTerrainBoundaries = false,
            });

        // The entity moves to the coverage anchor, so its reservation must
        // move with it - a stale reservation left later placements free to
        // stack onto the anchor hex.
        var entity = assembly.Entity(seed.OwnerEntityId);
        takenPhysical.Remove(entity.Coordinates);
        var anchor = takenPhysical.Contains(result.Anchor)
            ? result.Coverage.Cells
                .Where(cell => !takenPhysical.Contains(cell))
                .OrderBy(cell => HexCoord.Distance(cell, result.Anchor))
                .ThenBy(static cell => cell.Q)
                .ThenBy(static cell => cell.R)
                .First()
            : result.Anchor;
        takenPhysical.Add(anchor);
        entity.Coordinates = anchor;
        return result.Coverage;
    }
}
