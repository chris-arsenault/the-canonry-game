namespace IceRemembers.Seed;

using Engine.WorldGen;

using IceRemembers;

using Terrain = IceRemembers.IceRemembersTerrain;

internal static partial class IceRemembersSeedBuilder
{
    private readonly record struct SemanticTerrainSeedSet(
        List<SemanticHexCoord> Lineage,
        List<SemanticHexCoord> Yoke,
        List<SemanticHexCoord> Witness,
        List<SemanticHexCoord> Silence,
        List<SemanticHexCoord> Severance,
        List<SemanticHexCoord> Breakaway,
        List<SemanticHexCoord> Beacon,
        List<SemanticHexCoord> Taboo,
        List<SemanticHexCoord> Cave);

    private readonly record struct SemanticTerrainSalts(
        int Open,
        int Lineage,
        int Yoke,
        int Witness,
        int Silence,
        int Severance,
        int Breakaway,
        int Beacon,
        int Taboo,
        int Cave);

    private readonly record struct SemanticTerrainInfluence(
        float Open,
        float Lineage,
        float Yoke,
        float Witness,
        float Silence,
        float Severance,
        float Breakaway,
        float Beacon,
        float Taboo,
        float Cave);

    private static readonly Dictionary<Terrain, (int SuperregionId, int ContinentId)> SemanticTerrainSubdivisions =
        new()
        {
            [Terrain.Lineage] = (SemanticLineageSuperregionId, SemanticMemoryContinentId),
            [Terrain.Yoke] = (SemanticYokeSuperregionId, SemanticAuthorityContinentId),
            [Terrain.Witness] = (SemanticWitnessSuperregionId, SemanticMemoryContinentId),
            [Terrain.Silence] = (SemanticSilenceSuperregionId, SemanticShadowContinentId),
            [Terrain.Severance] = (SemanticSeveranceSuperregionId, SemanticRuptureContinentId),
            [Terrain.Breakaway] = (SemanticBreakawaySuperregionId, SemanticRuptureContinentId),
            [Terrain.BeaconSpire] = (SemanticBeaconSpireSuperregionId, SemanticMemoryContinentId),
            [Terrain.TabooRidge] = (SemanticTabooRidgeSuperregionId, SemanticShadowContinentId),
            [Terrain.CavePocket] = (SemanticCavePocketSuperregionId, SemanticShadowContinentId),
            [Terrain.OpenField] = (SemanticOpenFieldSuperregionId, SemanticMemoryContinentId),
        };

    private static void CreateSemanticMap(SeedAssembly seed, int salt)
    {
        var random = new Random(salt);
        var seeds = PickSemanticTerrainSeeds(random);
        var salts = NextSemanticTerrainSalts(random);
        PaintSemanticTerrain(seed, seeds, salts);
    }

    private static SemanticTerrainSeedSet PickSemanticTerrainSeeds(Random random) =>
        new(
            PickSemanticSeedCoords(
                random,
                count: 5,
                minDistance: StartupMapRadius / 3,
                coord => coord.S >= StartupMapRadius / 5),
            PickSemanticSeedCoords(
                random,
                count: 5,
                minDistance: StartupMapRadius / 3,
                coord => coord.Q <= -StartupMapRadius / 5),
            PickSemanticSeedCoords(
                random,
                count: 5,
                minDistance: StartupMapRadius / 3,
                coord => coord.R >= StartupMapRadius / 5),
            PickSemanticSeedCoords(
                random,
                count: 5,
                minDistance: StartupMapRadius / 3,
                coord => coord.R <= -StartupMapRadius / 5),
            PickSemanticSeedCoords(
                random,
                count: 5,
                minDistance: StartupMapRadius / 3,
                coord => coord.S <= -StartupMapRadius / 5 && SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) >= StartupMapRadius / 4),
            PickSemanticSeedCoords(
                random,
                count: 5,
                minDistance: StartupMapRadius / 3,
                coord => coord.Q >= StartupMapRadius / 5),
            PickSemanticSeedCoords(
                random,
                count: 5,
                minDistance: StartupMapRadius / 5,
                coord => SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) is >= 8 and <= 32),
            PickSemanticSeedCoords(
                random,
                count: 7,
                minDistance: StartupMapRadius / 5,
                coord => SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) >= StartupMapRadius / 2),
            PickSemanticSeedCoords(
                random,
                count: 6,
                minDistance: StartupMapRadius / 5,
                coord => coord.R <= -StartupMapRadius / 6 && SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) >= StartupMapRadius / 3));

    private static SemanticTerrainSalts NextSemanticTerrainSalts(Random random) =>
        new(
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next(),
            random.Next());

    private static void PaintSemanticTerrain(
        SeedAssembly seed,
        SemanticTerrainSeedSet seeds,
        SemanticTerrainSalts salts)
    {
        foreach (var coord in SemanticHexCoord.CoordsInRadius(SemanticHexCoord.Origin, StartupMapRadius))
        {
            var influence = CalculateSemanticTerrainInfluence(coord, seeds, salts);
            SetSemanticTerrain(seed, coord, DetermineSemanticTerrain(influence));
        }
    }

    private static SemanticTerrainInfluence CalculateSemanticTerrainInfluence(
        SemanticHexCoord coord,
        SemanticTerrainSeedSet seeds,
        SemanticTerrainSalts salts)
    {
        var radial = SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) / (float)StartupMapRadius;
        var q = coord.Q / (float)StartupMapRadius;
        var r = coord.R / (float)StartupMapRadius;
        var s = coord.S / (float)StartupMapRadius;

        var open = 0.44f - (radial * 0.2f) + SemanticNoise(coord, salts.Open, 0.08f);
        var lineage = ComputeSemanticInfluence(coord, seeds.Lineage, StartupMapRadius * 0.34f)
            + (Positive(s) * 0.36f)
            + SemanticNoise(coord, salts.Lineage, 0.09f);
        var yoke = ComputeSemanticInfluence(coord, seeds.Yoke, StartupMapRadius * 0.32f)
            + (Positive(-q) * 0.34f)
            + SemanticNoise(coord, salts.Yoke, 0.09f);
        var witness = ComputeSemanticInfluence(coord, seeds.Witness, StartupMapRadius * 0.32f)
            + (Positive(r) * 0.36f)
            + SemanticNoise(coord, salts.Witness, 0.09f);
        var silence = ComputeSemanticInfluence(coord, seeds.Silence, StartupMapRadius * 0.32f)
            + (Positive(-r) * 0.36f)
            + SemanticNoise(coord, salts.Silence, 0.09f);
        var severance = ComputeSemanticInfluence(coord, seeds.Severance, StartupMapRadius * 0.3f)
            + (Positive(-s) * 0.34f)
            + (radial * 0.08f)
            + SemanticNoise(coord, salts.Severance, 0.09f);
        var breakaway = ComputeSemanticInfluence(coord, seeds.Breakaway, StartupMapRadius * 0.32f)
            + (Positive(q) * 0.34f)
            + SemanticNoise(coord, salts.Breakaway, 0.09f);
        var beacon = ComputeSemanticInfluence(coord, seeds.Beacon, StartupMapRadius * 0.11f)
            + SemanticNoise(coord, salts.Beacon, 0.04f);
        var taboo = ComputeSemanticInfluence(coord, seeds.Taboo, StartupMapRadius * 0.14f)
            + (radial * 0.08f)
            + SemanticNoise(coord, salts.Taboo, 0.04f);
        var cave = ComputeSemanticInfluence(coord, seeds.Cave, StartupMapRadius * 0.13f)
            + (Positive(-r) * 0.08f)
            + SemanticNoise(coord, salts.Cave, 0.04f);

        return new SemanticTerrainInfluence(
            open,
            lineage,
            yoke,
            witness,
            silence,
            severance,
            breakaway,
            beacon,
            taboo,
            cave);
    }

    private static Terrain DetermineSemanticTerrain(SemanticTerrainInfluence influence)
    {
        if (influence.Beacon >= 0.78f)
        {
            return Terrain.BeaconSpire;
        }

        if (influence.Taboo >= 0.78f)
        {
            return Terrain.TabooRidge;
        }

        if (influence.Cave >= 0.78f)
        {
            return Terrain.CavePocket;
        }

        var terrain = Terrain.OpenField;
        var score = influence.Open;
        ChooseIfBetter(Terrain.Lineage, influence.Lineage, ref terrain, ref score);
        ChooseIfBetter(Terrain.Yoke, influence.Yoke, ref terrain, ref score);
        ChooseIfBetter(Terrain.Witness, influence.Witness, ref terrain, ref score);
        ChooseIfBetter(Terrain.Silence, influence.Silence, ref terrain, ref score);
        ChooseIfBetter(Terrain.Severance, influence.Severance, ref terrain, ref score);
        ChooseIfBetter(Terrain.Breakaway, influence.Breakaway, ref terrain, ref score);
        return terrain;
    }

    private static List<SemanticHexCoord> PickSemanticSeedCoords(
        Random random,
        int count,
        int minDistance,
        Func<SemanticHexCoord, bool> predicate)
    {
        var candidates = SemanticHexCoord.CoordsInRadius(SemanticHexCoord.Origin, StartupMapRadius)
            .Where(predicate)
            .OrderBy(_ => random.Next())
            .ToList();

        var chosen = new List<SemanticHexCoord>();
        foreach (var candidate in candidates)
        {
            if (chosen.TrueForAll(existing => SemanticHexCoord.Distance(existing, candidate) >= minDistance))
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
            chosen.Add(SemanticHexCoord.Origin);
        }

        return chosen;
    }

    private static float ComputeSemanticInfluence(
        SemanticHexCoord coord,
        List<SemanticHexCoord> seeds,
        float radius)
    {
        if (seeds.Count == 0 || radius <= 0f)
        {
            return 0f;
        }

        var strongest = 0f;
        foreach (var seed in seeds)
        {
            var influence = 1f - (SemanticHexCoord.Distance(coord, seed) / radius);
            if (influence > strongest)
            {
                strongest = influence;
            }
        }

        return Math.Clamp(strongest, 0f, 1f);
    }

    private static void SetSemanticTerrain(
        SeedAssembly seed,
        SemanticHexCoord coord,
        Terrain terrain)
    {
        var subdivision = SemanticSubdivisionForTerrain(terrain);
        seed.SetSemanticCell(
            coord,
            terrain.ToDomainName(),
            ownerSeedEntityId: null,
            subdivision.SuperregionId,
            subdivision.ContinentId);
    }

    private static (int SuperregionId, int ContinentId) SemanticSubdivisionForTerrain(Terrain terrain) =>
        SemanticTerrainSubdivisions.TryGetValue(terrain, out var subdivision)
            ? subdivision
            : (SemanticOpenFieldSuperregionId, SemanticMemoryContinentId);

    private static void ChooseIfBetter(
        Terrain terrain,
        float candidateScore,
        ref Terrain bestTerrain,
        ref float bestScore)
    {
        if (candidateScore > bestScore)
        {
            bestTerrain = terrain;
            bestScore = candidateScore;
        }
    }

    private static float SemanticNoise(SemanticHexCoord coord, int salt, float scale) =>
        CellNoise(new HexCoord(coord.Q, coord.R), salt) * scale;

    private static float Positive(float value) => Math.Max(0f, value);
}
