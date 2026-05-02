namespace IceRemembers.Seed;

using Engine.Graph;
using Engine.WorldGen;

using IceRemembers;

using K = IceRemembers.IceRemembersKinds;

internal static partial class IceRemembersSeedBuilder
{
    private static void CreateSupportingSettlements(
        SeedAssembly seed,
        HexGrid grid,
        MapCoverageMap coverageMap,
        Dictionary<string, MapCoverage> regions,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        List<SeedEntity> allSettlements,
        SeedEntity factionA,
        SeedEntity factionB,
        SeedEntity factionC)
    {
        var count = 3 + random.Next(3);
        var usedNames = allSettlements.Select(settlement => settlement.Name).ToHashSet(StringComparer.Ordinal);

        for (var i = 0; i < count; i++)
        {
            var plan = CreateSupportingSettlementPlan(random, i, factionA, factionB, factionC);
            var coverage = regions[plan.CoverageKey];
            var coord = ChooseAnchor(
                grid,
                random,
                takenPhysical,
                minimumDistance: 4,
                cell => SupportingSettlementScore(grid, coverageMap, coverage, cell, plan.Coastal));
            var name = UniqueName(Pick(plan.NamePool, random), usedNames);

            var settlement = CreateEntity(
                seed,
                K.Settlement,
                plan.Subtype,
                name,
                C(plan.Culture),
                coordinates: (coord.Q, coord.R));
            takenPhysical.Add(coord);
            SetSemanticProjected(settlement, coord, takenSemantic, RandomSemanticBias(random));
            allSettlements.Add(settlement);
            CreateRelationship(
                seed,
                "controlled_by",
                RelationshipPolarity.Neutral,
                settlement.SeedEntityId,
                plan.ControlledBy.SeedEntityId,
                0.65f);
        }
    }

    private static SupportingSettlementPlan CreateSupportingSettlementPlan(
        Random random,
        int index,
        SeedEntity factionA,
        SeedEntity factionB,
        SeedEntity factionC)
    {
        var far = random.NextSingle() < 0.22f;
        var coastal = far || (index == 0 ? random.NextSingle() < 0.65f : random.NextSingle() < 0.35f);
        var culture = Pick(SupportingSettlementCultures(far, coastal), random);
        var subtype = Pick(coastal ? CoastalSettlementSubtypes : InteriorSettlementSubtypes, random);
        var coverageKey = SupportingSettlementCoverageKey(random, far, coastal);
        var controlledBy = SupportingSettlementController(coverageKey, culture, factionA, factionB, factionC);
        var namePool = SupportingSettlementNamePool(far, coastal);

        return new SupportingSettlementPlan(coastal, culture, subtype, coverageKey, controlledBy, namePool);
    }

    private static IceRemembersCulture[] SupportingSettlementCultures(bool far, bool coastal)
    {
        if (far)
        {
            return FarPenguinCultures;
        }

        if (coastal)
        {
            return CoastalCultures;
        }

        return InteriorCultures;
    }

    private static string SupportingSettlementCoverageKey(Random random, bool far, bool coastal)
    {
        if (far)
        {
            return random.NextSingle() < 0.55f ? "farwake" : "blackwater";
        }

        if (coastal)
        {
            return "coastlands";
        }

        return Pick(InteriorCoverageKeys, random);
    }

    private static string[] SupportingSettlementNamePool(bool far, bool coastal)
    {
        if (far)
        {
            return FarSettlementNames;
        }

        if (coastal)
        {
            return CoastalSettlementNames;
        }

        return InteriorSettlementNames;
    }

    private static SeedEntity SupportingSettlementController(
        string coverageKey,
        IceRemembersCulture culture,
        SeedEntity factionA,
        SeedEntity factionB,
        SeedEntity factionC)
    {
        if (coverageKey is "farwake" or "blackwater" || culture == IceRemembersCulture.WakeSingers)
        {
            return factionC;
        }

        if (coverageKey == "coastlands" || culture == IceRemembersCulture.Nightshelf)
        {
            return factionB;
        }

        return factionA;
    }

    private static IEnumerable<(string Subtype, string Name, string Culture, int Faction, float Loyalty)>
        CreateSupportingPenguinPlans(Random random, SeedEntity factionA, SeedEntity factionB)
    {
        var count = 2 + random.Next(3);
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "High-Beak Auditor Selka",
            "Shadow-Broker Nyla",
            "Captain Greyfeather",
            "Trader Shellback",
            "Elder Deepcurrent",
            "Keeper Hollows",
        };

        for (var i = 0; i < count; i++)
        {
            var factionRoll = random.NextSingle();
            var faction = factionRoll < 0.55f ? factionA : factionB;
            var culture = Pick(PenguinCulturesForFaction(faction, factionA), random);
            var name = UniqueName(
                $"{Pick(PenguinGivenNames, random)} {Pick(PenguinByNames, random)}",
                names);

            yield return (
                Pick(PenguinSubtypes, random),
                name,
                C(culture),
                faction.SeedEntityId,
                0.45f + ((float)random.NextDouble() * 0.45f));
        }
    }

    private static IceRemembersCulture[] PenguinCulturesForFaction(
        SeedEntity faction,
        SeedEntity factionA) =>
        faction == factionA ? NorthernPenguinCultures : SouthernPenguinCultures;

    private static float SupportingSettlementScore(
        HexGrid grid,
        MapCoverageMap coverageMap,
        MapCoverage coverage,
        HexCell cell,
        bool coastal)
    {
        var affinity = CoverageAffinity(coverageMap, coverage, cell.Coord, 24f, 10f);
        var terrain = coastal
            ? TerrainScore(cell.Terrain, IceRemembersTerrain.Reef, 24f) +
              TerrainScore(cell.Terrain, IceRemembersTerrain.Shelf, 22f) +
              TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 8f) +
              (CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf) * 8f)
            : TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 20f) +
              TerrainScore(cell.Terrain, IceRemembersTerrain.HotSprings, 14f) +
              TerrainScore(cell.Terrain, IceRemembersTerrain.Ice, 10f) +
              TerrainScore(cell.Terrain, IceRemembersTerrain.Wastes, 8f);

        return affinity + terrain;
    }
}
