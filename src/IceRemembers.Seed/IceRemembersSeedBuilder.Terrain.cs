namespace IceRemembers.Seed;

using Engine.WorldGen;

using IceRemembers;

internal static partial class IceRemembersSeedBuilder
{
    private readonly record struct TerrainSeedSet(
        List<HexCoord> Ocean,
        List<HexCoord> Ice,
        List<HexCoord> Geothermal,
        List<HexCoord> Waste,
        List<HexCoord> Crag);

    private readonly record struct TerrainSalts(
        int Ocean,
        int Cold,
        int Geothermal,
        int Waste,
        int Crag,
        int Maritime);

    private readonly record struct TerrainInfluence(
        float Ocean,
        float Cold,
        float Geothermal,
        float Arid,
        float Rugged);

    private static void PaintTerrain(
        HexGrid grid,
        Random random,
        MapSubdivisionMap subdivisionMap)
    {
        ArgumentNullException.ThrowIfNull(subdivisionMap);
        var seeds = PickTerrainSeeds(grid, random);
        var salts = NextTerrainSalts(random);
        var coldness = PaintBaseTerrain(grid, seeds, salts, subdivisionMap);
        ApplyMaritimeTerrain(grid, coldness, salts);
    }

    private static TerrainSeedSet PickTerrainSeeds(HexGrid grid, Random random) =>
        new(
            PickSeedCoords(
                grid,
                random,
                count: 5,
                minDistance: grid.Radius / 3,
                coord => HexCoord.Distance(coord, new HexCoord(0, 0)) >= grid.Radius / 3),
            PickSeedCoords(
                grid,
                random,
                count: 4,
                minDistance: grid.Radius / 3,
                coord => HexCoord.Distance(coord, new HexCoord(0, 0)) >= grid.Radius / 3),
            PickSeedCoords(
                grid,
                random,
                count: 4,
                minDistance: grid.Radius / 4,
                coord => HexCoord.Distance(coord, new HexCoord(0, 0)) <= (grid.Radius * 5) / 6),
            PickSeedCoords(
                grid,
                random,
                count: 4,
                minDistance: grid.Radius / 4,
                coord =>
                {
                    var distance = HexCoord.Distance(coord, new HexCoord(0, 0));
                    return distance >= grid.Radius / 5 && distance <= (grid.Radius * 9) / 10;
                }),
            PickSeedCoords(
                grid,
                random,
                count: 5,
                minDistance: grid.Radius / 4,
                coord =>
                {
                    var distance = HexCoord.Distance(coord, new HexCoord(0, 0));
                    return distance >= grid.Radius / 4 && distance <= (grid.Radius * 9) / 10;
                }));

    private static TerrainSalts NextTerrainSalts(Random random) =>
        new(
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next());

    private static Dictionary<HexCoord, float> PaintBaseTerrain(
        HexGrid grid,
        TerrainSeedSet seeds,
        TerrainSalts salts,
        MapSubdivisionMap subdivisionMap)
    {
        var coldness = new Dictionary<HexCoord, float>();
        foreach (var cell in grid.AllCells)
        {
            var influence = CalculateTerrainInfluence(grid, cell.Coord, seeds, salts, subdivisionMap);
            coldness[cell.Coord] = influence.Cold;

            var terrain = DetermineBaseTerrain(influence);
            SetTerrain(grid, cell.Coord, terrain);
        }

        return coldness;
    }

    private static TerrainInfluence CalculateTerrainInfluence(
        HexGrid grid,
        HexCoord coord,
        TerrainSeedSet seeds,
        TerrainSalts salts,
        MapSubdivisionMap subdivisionMap)
    {
        var gridRadius = grid.Radius;
        var subdivision = subdivisionMap.GetCell(coord);
        var boundaryWater = ContinentBoundaryWater(grid, subdivisionMap, coord, salts.Ocean ^ 4093);
        var bias = SuperregionTerrainBias(subdivision.SuperregionId);
        var radiusBias = HexCoord.Distance(coord, new HexCoord(0, 0)) / (float)gridRadius;
        var ocean = ComputeInfluence(coord, seeds.Ocean, radius: gridRadius * 0.55f)
            + (radiusBias * 0.08f)
            + (boundaryWater * 0.9f)
            + bias.Ocean
            + (CellNoise(coord, salts.Ocean) * 0.12f);
        var cold = ComputeInfluence(coord, seeds.Ice, radius: gridRadius * 0.5f)
            + (radiusBias * 0.12f)
            + bias.Cold
            + (CellNoise(coord, salts.Cold) * 0.1f);
        var geothermal = ComputeInfluence(coord, seeds.Geothermal, radius: gridRadius * 0.24f)
            + bias.Geothermal
            + (CellNoise(coord, salts.Geothermal) * 0.11f);
        var arid = ComputeInfluence(coord, seeds.Waste, radius: gridRadius * 0.3f)
            + bias.Arid
            + (CellNoise(coord, salts.Waste) * 0.13f);
        var rugged = ComputeInfluence(coord, seeds.Crag, radius: gridRadius * 0.22f)
            + bias.Rugged
            + (CellNoise(coord, salts.Crag) * 0.11f);

        return new TerrainInfluence(ocean, cold, geothermal, arid, rugged);
    }

    private static float ContinentBoundaryWater(
        HexGrid grid,
        MapSubdivisionMap subdivisionMap,
        HexCoord coord,
        int salt)
    {
        var continentId = subdivisionMap.GetCell(coord).ContinentId;
        for (var radius = 1; radius <= 3; radius++)
        {
            foreach (var candidate in HexCoord.CoordsInRadius(coord, radius))
            {
                if (!grid.ContainsCoord(candidate))
                {
                    continue;
                }

                if (subdivisionMap.GetCell(candidate).ContinentId != continentId)
                {
                    var width = radius switch
                    {
                        1 => 0.95f,
                        2 => 0.7f,
                        _ => 0.45f,
                    };
                    return Math.Clamp(width + (CellNoise(coord, salt) * 0.1f), 0f, 1f);
                }
            }
        }

        return 0f;
    }

    private static (
        float Ocean,
        float Cold,
        float Geothermal,
        float Arid,
        float Rugged) SuperregionTerrainBias(int superregionId) =>
        superregionId switch
        {
            NorthlandsSuperregionId => (0.04f, 0.22f, 0.02f, 0.02f, 0.06f),
            HeartlandsSuperregionId => (0.02f, 0.08f, 0.2f, 0.08f, 0.02f),
            CoastlandsSuperregionId => (0.2f, 0.08f, 0.03f, 0.03f, 0.02f),
            FarwakeSuperregionId => (0.28f, 0.08f, 0.02f, 0.02f, 0.02f),
            BlackwaterSuperregionId => (0.32f, 0.04f, 0.02f, 0.04f, 0.08f),
            WhiteEmptySuperregionId => (0.02f, 0.18f, 0.02f, 0.18f, 0.05f),
            DeepEmptySuperregionId => (0.04f, 0.12f, 0.01f, 0.24f, 0.16f),
            _ => (0f, 0f, 0f, 0f, 0f),
        };

    private static void ApplyMaritimeTerrain(
        HexGrid grid,
        Dictionary<HexCoord, float> coldness,
        TerrainSalts salts)
    {
        foreach (var (coord, terrain) in FindMaritimeTransforms(grid, coldness, salts.Maritime))
        {
            SetTerrain(grid, coord, terrain);
        }
    }

    private static List<(HexCoord Coord, IceRemembersTerrain Terrain)> FindMaritimeTransforms(
        HexGrid grid,
        Dictionary<HexCoord, float> coldness,
        int maritimeSalt)
    {
        var transforms = new List<(HexCoord Coord, IceRemembersTerrain Terrain)>();
        foreach (var cell in grid.AllCells)
        {
            if (TryGetMaritimeTransform(grid, cell, coldness[cell.Coord], maritimeSalt, out var terrain))
            {
                transforms.Add((cell.Coord, terrain));
            }
        }

        return transforms;
    }

    private static bool TryGetMaritimeTransform(
        HexGrid grid,
        HexCell cell,
        float coldness,
        int maritimeSalt,
        out IceRemembersTerrain terrain)
    {
        terrain = default;
        if (IsTerrain(cell, IceRemembersTerrain.Ocean))
        {
            return false;
        }

        var adjacentOcean = CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Ocean);
        return TryGetCoastalTerrain(cell, adjacentOcean, coldness, maritimeSalt, out terrain);
    }

    private static bool TryGetCoastalTerrain(
        HexCell cell,
        int adjacentOcean,
        float coldness,
        int maritimeSalt,
        out IceRemembersTerrain terrain)
    {
        terrain = default;
        if (adjacentOcean == 0)
        {
            return false;
        }

        if (ShouldBecomeShelf(coldness, adjacentOcean))
        {
            terrain = IceRemembersTerrain.Shelf;
            return true;
        }

        if (ShouldBecomeReef(cell, adjacentOcean, maritimeSalt))
        {
            terrain = IceRemembersTerrain.Reef;
            return true;
        }

        return false;
    }

    private static bool ShouldBecomeShelf(float coldness, int adjacentOcean) =>
        coldness >= 0.58f || (adjacentOcean >= 3 && coldness >= 0.44f);

    private static bool ShouldBecomeReef(HexCell cell, int adjacentOcean, int maritimeSalt) =>
        !IsTerrain(cell, IceRemembersTerrain.Crags) &&
        !IsTerrain(cell, IceRemembersTerrain.HotSprings) &&
        (adjacentOcean >= 2 || CellNoise(cell.Coord, maritimeSalt) >= 0.66f);

    private static IceRemembersTerrain DetermineBaseTerrain(TerrainInfluence influence)
    {
        if (IsOceanTerrain(influence))
        {
            return IceRemembersTerrain.Ocean;
        }

        if (IsGeothermalTerrain(influence))
        {
            return IceRemembersTerrain.HotSprings;
        }

        if (IsRuggedTerrain(influence))
        {
            return IceRemembersTerrain.Crags;
        }

        if (IsIceTerrain(influence))
        {
            return IceRemembersTerrain.Ice;
        }

        if (IsWasteTerrain(influence))
        {
            return IceRemembersTerrain.Wastes;
        }

        return IceRemembersTerrain.Tundra;
    }

    private static bool IsOceanTerrain(TerrainInfluence influence) =>
        influence.Ocean >= 0.72f;

    private static bool IsGeothermalTerrain(TerrainInfluence influence) =>
        influence.Geothermal >= 0.68f && influence.Ocean < 0.66f;

    private static bool IsRuggedTerrain(TerrainInfluence influence) =>
        influence.Rugged >= 0.66f && influence.Ocean < 0.67f;

    private static bool IsIceTerrain(TerrainInfluence influence) =>
        influence.Cold >= 0.66f && influence.Ocean < 0.7f;

    private static bool IsWasteTerrain(TerrainInfluence influence) =>
        influence.Arid >= 0.66f && influence.Geothermal < 0.68f && influence.Ocean < 0.64f;

    private static List<HexCoord> PickSeedCoords(
        HexGrid grid,
        Random random,
        int count,
        int minDistance,
        Func<HexCoord, bool> predicate)
    {
        var candidates = grid.AllCells
            .Select(cell => cell.Coord)
            .Where(predicate)
            .OrderBy(_ => random.Next())
            .ToList();

        var chosen = new List<HexCoord>();
        foreach (var candidate in candidates)
        {
            if (chosen.TrueForAll(existing => HexCoord.Distance(existing, candidate) >= minDistance))
            {
                chosen.Add(candidate);
            }

            if (chosen.Count == count)
            {
                break;
            }
        }

        if (chosen.Count == 0)
        {
            chosen.Add(new HexCoord(0, 0));
        }

        return chosen;
    }

    private static float ComputeInfluence(HexCoord coord, List<HexCoord> seeds, float radius)
    {
        if (seeds.Count == 0 || radius <= 0f)
        {
            return 0f;
        }

        var strongest = 0f;
        foreach (var seed in seeds)
        {
            var influence = 1f - (HexCoord.Distance(coord, seed) / radius);
            if (influence > strongest)
            {
                strongest = influence;
            }
        }

        return Math.Clamp(strongest, 0f, 1f);
    }

    private static float CellNoise(HexCoord coord, int salt)
    {
        unchecked
        {
            var hash = salt;
            hash = (hash * 397) ^ coord.Q;
            hash = (hash * 397) ^ coord.R;
            hash ^= hash >> 13;
            hash *= 16777619;
            return (hash & 0x7fffffff) / (float)int.MaxValue;
        }
    }

    private static void SetTerrain(
        HexGrid grid,
        HexCoord coord,
        IceRemembersTerrain terrain)
    {
        grid.SetCell(coord, terrain.ToDomainName());
    }

    private static float TerrainScore(TerrainDefinition actual, IceRemembersTerrain desired, float value) =>
        string.Equals(actual.StableName, desired.ToDomainName(), StringComparison.Ordinal) ? value : 0f;

    private static bool IsTerrain(HexCell cell, IceRemembersTerrain terrain) =>
        string.Equals(cell.Terrain.StableName, terrain.ToDomainName(), StringComparison.Ordinal);

    private static HashSet<string> TerrainSet(params IceRemembersTerrain[] terrains) =>
        terrains.Select(terrain => terrain.ToDomainName()).ToHashSet(StringComparer.Ordinal);

    private static int CountTerrainsInRadius(
        HexGrid grid,
        HexCoord center,
        int radius,
        params IceRemembersTerrain[] terrains)
    {
        var set = TerrainSet(terrains);
        return HexCoord.CoordsInRadius(center, radius)
            .Where(grid.ContainsCoord)
            .Select(grid.GetCell)
            .Count(cell => cell is not null && set.Contains(cell.Terrain.StableName));
    }

    private static int CountAdjacentTerrains(
        HexGrid grid,
        HexCoord center,
        params IceRemembersTerrain[] terrains)
    {
        var set = TerrainSet(terrains);
        return center.Neighbors()
            .Where(grid.ContainsCoord)
            .Select(grid.GetCell)
            .Count(cell => cell is not null && set.Contains(cell.Terrain.StableName));
    }
}
