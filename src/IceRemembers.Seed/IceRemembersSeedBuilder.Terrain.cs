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
        int Fertility,
        int Maritime);

    private readonly record struct TerrainInfluence(
        float Ocean,
        float Cold,
        float Geothermal,
        float Arid,
        float Rugged,
        float Fertility);

    private static void PaintTerrain(HexGrid grid, Random random)
    {
        var seeds = PickTerrainSeeds(grid, random);
        var salts = NextTerrainSalts(random);
        var coldness = PaintBaseTerrain(grid, random, seeds, salts);
        ApplyMaritimeTerrain(grid, random, coldness, salts);
    }

    private static TerrainSeedSet PickTerrainSeeds(HexGrid grid, Random random) =>
        new(
            PickSeedCoords(
                grid,
                random,
                count: 3,
                minDistance: grid.Radius / 2,
                coord => HexCoord.Distance(coord, new HexCoord(0, 0)) >= (grid.Radius * 3) / 4),
            PickSeedCoords(
                grid,
                random,
                count: 3,
                minDistance: grid.Radius / 3,
                coord => HexCoord.Distance(coord, new HexCoord(0, 0)) >= grid.Radius / 2),
            PickSeedCoords(
                grid,
                random,
                count: 3,
                minDistance: grid.Radius / 4,
                coord => HexCoord.Distance(coord, new HexCoord(0, 0)) <= (grid.Radius * 3) / 4),
            PickSeedCoords(
                grid,
                random,
                count: 3,
                minDistance: grid.Radius / 4,
                coord => HexCoord.Distance(coord, new HexCoord(0, 0)) is >= 4 and <= 16),
            PickSeedCoords(
                grid,
                random,
                count: 4,
                minDistance: grid.Radius / 4,
                coord => HexCoord.Distance(coord, new HexCoord(0, 0)) is >= 5 and <= 18));

    private static TerrainSalts NextTerrainSalts(Random random) =>
        new(
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next());

    private static Dictionary<HexCoord, float> PaintBaseTerrain(
        HexGrid grid,
        Random random,
        TerrainSeedSet seeds,
        TerrainSalts salts)
    {
        var coldness = new Dictionary<HexCoord, float>();
        foreach (var cell in grid.AllCells)
        {
            var influence = CalculateTerrainInfluence(cell.Coord, grid.Radius, seeds, salts);
            coldness[cell.Coord] = influence.Cold;

            var terrain = DetermineBaseTerrain(influence);
            var (elevation, fertility) = TerrainProfileFor(terrain, influence);
            SetTerrainAndAmbientResources(grid, cell.Coord, terrain, elevation, fertility, random);
        }

        return coldness;
    }

    private static TerrainInfluence CalculateTerrainInfluence(
        HexCoord coord,
        int gridRadius,
        TerrainSeedSet seeds,
        TerrainSalts salts)
    {
        var radiusBias = HexCoord.Distance(coord, new HexCoord(0, 0)) / (float)gridRadius;
        var ocean = ComputeInfluence(coord, seeds.Ocean, radius: gridRadius * 0.55f)
            + (radiusBias * 0.18f)
            + (CellNoise(coord, salts.Ocean) * 0.14f);
        var cold = ComputeInfluence(coord, seeds.Ice, radius: gridRadius * 0.5f)
            + (radiusBias * 0.2f)
            + (CellNoise(coord, salts.Cold) * 0.12f);
        var geothermal = ComputeInfluence(coord, seeds.Geothermal, radius: gridRadius * 0.24f)
            + (CellNoise(coord, salts.Geothermal) * 0.12f);
        var arid = ComputeInfluence(coord, seeds.Waste, radius: gridRadius * 0.3f)
            + (CellNoise(coord, salts.Waste) * 0.15f);
        var rugged = ComputeInfluence(coord, seeds.Crag, radius: gridRadius * 0.22f)
            + (CellNoise(coord, salts.Crag) * 0.12f);

        return new TerrainInfluence(ocean, cold, geothermal, arid, rugged, CellNoise(coord, salts.Fertility));
    }

    private static void ApplyMaritimeTerrain(
        HexGrid grid,
        Random random,
        Dictionary<HexCoord, float> coldness,
        TerrainSalts salts)
    {
        foreach (var (coord, terrain) in FindMaritimeTransforms(grid, coldness, salts.Maritime))
        {
            var (elevation, fertility) = MaritimeTerrainProfile(terrain, coord, salts.Fertility);
            SetTerrainAndAmbientResources(grid, coord, terrain, elevation, fertility, random);
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

    private static (float Elevation, float Fertility) MaritimeTerrainProfile(
        IceRemembersTerrain terrain,
        HexCoord coord,
        int fertilitySalt) =>
        terrain switch
        {
            IceRemembersTerrain.Shelf => (10f, 0.35f + (CellNoise(coord, fertilitySalt ^ 17) * 0.1f)),
            IceRemembersTerrain.Reef => (5f, 0.45f + (CellNoise(coord, fertilitySalt ^ 29) * 0.2f)),
            _ => (20f, 0.5f),
        };

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
        influence.Ocean >= 0.78f;

    private static bool IsGeothermalTerrain(TerrainInfluence influence) =>
        influence.Geothermal >= 0.76f && influence.Ocean < 0.62f;

    private static bool IsRuggedTerrain(TerrainInfluence influence) =>
        influence.Rugged >= 0.74f && influence.Ocean < 0.65f;

    private static bool IsIceTerrain(TerrainInfluence influence) =>
        influence.Cold >= 0.72f && influence.Ocean < 0.66f;

    private static bool IsWasteTerrain(TerrainInfluence influence) =>
        influence.Arid >= 0.72f && influence.Geothermal < 0.68f && influence.Ocean < 0.64f;

    private static (float Elevation, float Fertility) TerrainProfileFor(
        IceRemembersTerrain terrain,
        TerrainInfluence influence) =>
        terrain switch
        {
            IceRemembersTerrain.Ocean => (0f, 0f),
            IceRemembersTerrain.Reef => (5f, 0.45f + (influence.Fertility * 0.2f)),
            IceRemembersTerrain.Shelf => (10f, 0.3f + (influence.Fertility * 0.15f)),
            IceRemembersTerrain.HotSprings => (12f + (influence.Geothermal * 12f), 0.82f + (influence.Fertility * 0.14f)),
            IceRemembersTerrain.Crags => (180f + (influence.Rugged * 220f), 0.08f + (influence.Fertility * 0.12f)),
            IceRemembersTerrain.Ice => (70f + (influence.Cold * 60f), 0.1f + (influence.Fertility * 0.12f)),
            IceRemembersTerrain.Wastes => (18f + (influence.Ocean * 10f), 0.12f + (influence.Fertility * 0.16f)),
            _ => (18f + ((1f - influence.Ocean) * 8f), 0.48f + (influence.Fertility * 0.26f)),
        };

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

    private static void SetTerrainAndAmbientResources(
        HexGrid grid,
        HexCoord coord,
        IceRemembersTerrain terrain,
        float elevation,
        float fertility,
        Random random)
    {
        grid.SetCell(coord, terrain.ToDomainName(), elevation, fertility);
        var cell = grid.GetCell(coord)!;
        TerrainAmbientResources.Seed(cell, random);
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
