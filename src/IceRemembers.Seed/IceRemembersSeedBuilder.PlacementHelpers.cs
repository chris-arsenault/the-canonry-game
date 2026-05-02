namespace IceRemembers.Seed;

using Engine.Domain.Startup;
using Engine.Graph;
using Engine.WorldGen;

internal static partial class IceRemembersSeedBuilder
{
    private static HexCoord? ChooseNearbyFreeCell(HexGrid grid, HashSet<HexCoord> taken, HexCoord origin, int searchRadius)
    {
        for (var radius = 0; radius <= searchRadius; radius++)
        {
            var candidates = HexCoord.CoordsInRadius(origin, radius)
                .Where(coord => grid.ContainsCoord(coord) && !taken.Contains(coord))
                .OrderBy(coord => coord.Q)
                .ThenBy(coord => coord.R)
                .ToList();

            if (candidates.Count > 0)
            {
                return candidates[0];
            }
        }

        return null;
    }

    private static void SetPhysical(SeedEntity entity, HexCoord coord, HashSet<HexCoord> taken)
    {
        ReservePhysical(coord, taken, entity.Name);
        entity.Coordinates = coord;
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

    private static void SetSemanticProjected(
        SeedEntity entity,
        HexCoord physicalCoord,
        HashSet<SemanticHexCoord> taken,
        SemanticHexCoord bias)
    {
        var projected = new SemanticHexCoord(
            (int)Math.Round(physicalCoord.Q / 2f, MidpointRounding.ToEven) + bias.Q,
            (int)Math.Round(physicalCoord.R / 2f, MidpointRounding.ToEven) + bias.R);

        var resolved = FindNearestFreeSemantic(projected, taken);
        SetSemantic(entity, resolved, taken);
    }

    private static SemanticHexCoord FindNearestFreeSemantic(
        SemanticHexCoord origin,
        HashSet<SemanticHexCoord> taken)
    {
        if (!taken.Contains(origin))
        {
            return origin;
        }

        for (var radius = 1; radius <= 24; radius++)
        {
            foreach (var coord in SemanticHexCoord.CoordsInRadius(origin, radius)
                         .Where(candidate => SemanticHexCoord.Distance(candidate, origin) == radius))
            {
                if (!taken.Contains(coord))
                {
                    return coord;
                }
            }
        }

        throw new InvalidOperationException("Could not find a free semantic coordinate for seed placement.");
    }

    private static MapCoverage CreateSeedCoverage(
        HexGrid grid,
        MapCoverageMap coverageMap,
        SeedAssembly assembly,
        MapCoverage seed,
        HexCoord origin,
        int desiredCellCount,
        int minimumCellCount,
        IReadOnlySet<string> preferredTerrains)
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

        assembly.Entity(seed.OwnerEntityId).Coordinates = result.Anchor;
        return result.Coverage;
    }
}
