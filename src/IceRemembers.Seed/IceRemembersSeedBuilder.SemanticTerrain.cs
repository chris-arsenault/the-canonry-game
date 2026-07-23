namespace IceRemembers.Seed;

using Engine.Domain.Spatial;

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
                count: 18,
                minDistance: SemanticFeatureScale / 3,
                coord => coord.S >= SemanticFeatureScale / 5),
            PickSemanticSeedCoords(
                random,
                count: 18,
                minDistance: SemanticFeatureScale / 3,
                coord => coord.Q <= -SemanticFeatureScale / 5),
            PickSemanticSeedCoords(
                random,
                count: 18,
                minDistance: SemanticFeatureScale / 3,
                coord => coord.R >= SemanticFeatureScale / 5),
            PickSemanticSeedCoords(
                random,
                count: 18,
                minDistance: SemanticFeatureScale / 3,
                coord => coord.R <= -SemanticFeatureScale / 5),
            PickSemanticSeedCoords(
                random,
                count: 18,
                minDistance: SemanticFeatureScale / 3,
                coord => coord.S <= -SemanticFeatureScale / 5 && SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) >= SemanticFeatureScale / 4),
            PickSemanticSeedCoords(
                random,
                count: 18,
                minDistance: SemanticFeatureScale / 3,
                coord => coord.Q >= SemanticFeatureScale / 5),
            PickSemanticSeedCoords(
                random,
                count: 24,
                minDistance: SemanticFeatureScale / 5,
                coord => SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) is >= 8 and <= 32),
            PickSemanticSeedCoords(
                random,
                count: 28,
                minDistance: SemanticFeatureScale / 5,
                coord => SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) >= SemanticFeatureScale / 2),
            PickSemanticSeedCoords(
                random,
                count: 24,
                minDistance: SemanticFeatureScale / 5,
                coord => coord.R <= -SemanticFeatureScale / 6 && SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) >= SemanticFeatureScale / 3));

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
        var blueprintCells = new List<Engine.Domain.Startup.DomainSemanticTerrainCell>(
            (3 * GenerationRadius * GenerationRadius) + (3 * GenerationRadius) + 1);
        foreach (var coord in SemanticHexCoord.CoordsInRadius(SemanticHexCoord.Origin, GenerationRadius))
        {
            var influence = CalculateSemanticTerrainInfluence(coord, seeds, salts);
            var terrain = DetermineSemanticTerrain(influence);
            var subdivision = SemanticSubdivisionForTerrain(terrain);
            blueprintCells.Add(new Engine.Domain.Startup.DomainSemanticTerrainCell(
                coord.Q,
                coord.R,
                terrain.ToDomainName(),
                subdivision.SuperregionId,
                subdivision.ContinentId));
            if (SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) <= SemanticHeartRadius)
            {
                SetSemanticTerrain(seed, coord, terrain);
            }
        }

        var blueprint = new Engine.Domain.Startup.DomainSemanticTerrainBlueprint(
            GenerationRadius,
            blueprintCells.ToArray());
        blueprint.Validate();
        seed.SetSemanticTerrainBlueprint(blueprint);
    }

    private static SemanticTerrainInfluence CalculateSemanticTerrainInfluence(
        SemanticHexCoord coord,
        SemanticTerrainSeedSet seeds,
        SemanticTerrainSalts salts)
    {
        var radial = SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) / (float)SemanticFeatureScale;
        var q = coord.Q / (float)SemanticFeatureScale;
        var r = coord.R / (float)SemanticFeatureScale;
        var s = coord.S / (float)SemanticFeatureScale;

        var open = 0.44f - (radial * 0.2f) + SemanticNoise(coord, salts.Open, 0.08f);
        var lineage = ComputeSemanticInfluence(coord, seeds.Lineage, SemanticFeatureScale * 0.34f)
            + (Positive(s) * 0.36f)
            + SemanticNoise(coord, salts.Lineage, 0.09f);
        var yoke = ComputeSemanticInfluence(coord, seeds.Yoke, SemanticFeatureScale * 0.32f)
            + (Positive(-q) * 0.34f)
            + SemanticNoise(coord, salts.Yoke, 0.09f);
        var witness = ComputeSemanticInfluence(coord, seeds.Witness, SemanticFeatureScale * 0.32f)
            + (Positive(r) * 0.36f)
            + SemanticNoise(coord, salts.Witness, 0.09f);
        var silence = ComputeSemanticInfluence(coord, seeds.Silence, SemanticFeatureScale * 0.32f)
            + (Positive(-r) * 0.36f)
            + SemanticNoise(coord, salts.Silence, 0.09f);
        var severance = ComputeSemanticInfluence(coord, seeds.Severance, SemanticFeatureScale * 0.3f)
            + (Positive(-s) * 0.34f)
            + (radial * 0.08f)
            + SemanticNoise(coord, salts.Severance, 0.09f);
        var breakaway = ComputeSemanticInfluence(coord, seeds.Breakaway, SemanticFeatureScale * 0.32f)
            + (Positive(q) * 0.34f)
            + SemanticNoise(coord, salts.Breakaway, 0.09f);
        var beacon = ComputeSemanticInfluence(coord, seeds.Beacon, SemanticFeatureScale * 0.11f)
            + SemanticNoise(coord, salts.Beacon, 0.04f);
        var taboo = ComputeSemanticInfluence(coord, seeds.Taboo, SemanticFeatureScale * 0.14f)
            + (radial * 0.08f)
            + SemanticNoise(coord, salts.Taboo, 0.04f);
        var cave = ComputeSemanticInfluence(coord, seeds.Cave, SemanticFeatureScale * 0.13f)
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
        var candidates = SemanticHexCoord.CoordsInRadius(SemanticHexCoord.Origin, GenerationRadius)
            .Where(predicate)
            .ToArray();
        var innerCandidates = candidates
            .Where(coord => SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) <= SemanticFeatureScale)
            .OrderBy(_ => random.Next())
            .ToArray();
        var outerCandidates = candidates
            .Where(coord => SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord) > SemanticFeatureScale)
            .OrderBy(_ => random.Next())
            .ToArray();
        var chosen = new List<SemanticHexCoord>();
        AddSemanticSeeds(chosen, innerCandidates, Math.Max(1, count / 3), minDistance);
        AddSemanticSeeds(chosen, outerCandidates, count, minDistance);
        AddSemanticSeeds(chosen, candidates, count, minDistance);

        if (chosen.Count == 0)
        {
            chosen.Add(SemanticHexCoord.Origin);
        }

        return chosen;
    }

    private static void AddSemanticSeeds(
        List<SemanticHexCoord> chosen,
        IEnumerable<SemanticHexCoord> candidates,
        int targetCount,
        int minDistance)
    {
        foreach (var candidate in candidates)
        {
            if (chosen.TrueForAll(existing => SemanticHexCoord.Distance(existing, candidate) >= minDistance))
            {
                chosen.Add(candidate);
            }

            if (chosen.Count >= targetCount)
            {
                return;
            }
        }
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
