namespace IceRemembers.Seed;

using Engine.Graph;
using Engine.WorldGen;

using IceRemembers;

using K = IceRemembers.IceRemembersKinds;

internal static partial class IceRemembersSeedBuilder
{
    private static readonly (string Name, string Subtype)[] OrcaPlans =
    [
        ("Grandmother Deep", "matriarch"),
        ("Krathund Gnurl", "matriarch"),
        ("Grandfather Blood", "elder"),
        ("Tide-Splitter", "scout"),
        ("Tidescream", "scout"),
    ];

    private static readonly (string Name, string Subtype, IceRemembersTerrain Terrain)[] PointOfInterestPlans =
    [
        ("The Veil Vents", "vent", IceRemembersTerrain.HotSprings),
        ("The Glow-Fissure", "vent", IceRemembersTerrain.Crags),
        ("The Forgotten Shore", "shoal", IceRemembersTerrain.Shelf),
        ("The Corpse Current", "bloom", IceRemembersTerrain.Ocean),
    ];

    private readonly record struct AbilityPlan(
        string Name,
        string Subtype,
        IceRemembersCulture Culture,
        string AnchorRegionKey,
        string PrimaryHolderName,
        string SecondaryHolderName);

    private static readonly AbilityPlan[] AbilityPlans =
    [
        new("Ice-Reading", "craft", IceRemembersCulture.AuroraStack, "heartlands", "Captain Greyfeather", "Elder Deepcurrent"),
        new("Fire-Core Craft", "craft", IceRemembersCulture.Nightshelf, "coastlands", "Trader Shellback", "Captain Greyfeather"),
        new("Wake-Song", "rite", IceRemembersCulture.WakeSingers, "farwake", "Singer Underwake", "Elder Deepcurrent"),
    ];

    private static void CreateOrcas(
        SeedAssembly seed,
        HexGrid grid,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        SeedEntity farwakeFaction)
    {
        foreach (var (name, subtype) in OrcaPlans)
        {
            var coord = ChooseAnchor(
                grid,
                random,
                takenPhysical,
                minimumDistance: 4,
                cell => OrcaScore(grid, cell));
            ReservePhysical(coord, takenPhysical, name);

            var orca = CreateEntity(
                seed,
                K.Orca,
                subtype,
                name,
                C(IceRemembersCulture.WakeSingers),
                coordinates: (coord.Q, coord.R));
            SetSemanticProjected(orca, coord, takenSemantic, RandomSemanticBias(random));

            CreateRelationship(
                seed,
                "member_of",
                RelationshipPolarity.Neutral,
                orca.SeedEntityId,
                farwakeFaction.SeedEntityId,
                0.85f);
        }
    }

    private static float OrcaScore(HexGrid grid, HexCell cell)
    {
        var rimBias = HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 1.4f;
        return TerrainScore(cell.Terrain, IceRemembersTerrain.Ocean, 30f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Reef, 18f)
            + (CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef) * 6f)
            + rimBias;
    }

    private static void CreatePointsOfInterest(
        SeedAssembly seed,
        HexGrid grid,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic)
    {
        foreach (var (name, subtype, terrain) in PointOfInterestPlans)
        {
            var coord = ChooseAnchor(
                grid,
                random,
                takenPhysical,
                minimumDistance: 3,
                cell => PointOfInterestScore(cell, terrain));
            ReservePhysical(coord, takenPhysical, name);

            var poi = CreateEntity(
                seed,
                K.PointOfInterest,
                subtype,
                name,
                "none",
                coordinates: (coord.Q, coord.R));
            SetSemanticProjected(poi, coord, takenSemantic, RandomSemanticBias(random));
        }
    }

    private static float PointOfInterestScore(HexCell cell, IceRemembersTerrain preferredTerrain) =>
        TerrainScore(cell.Terrain, preferredTerrain, 60f);

    private static void CreateAbilities(
        SeedAssembly seed,
        HexGrid grid,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        Dictionary<string, MapCoverage> regions)
    {
        foreach (var plan in AbilityPlans)
        {
            var anchorRegion = regions[plan.AnchorRegionKey];
            var anchorCoord = seed.Entity(anchorRegion.OwnerEntityId).Coordinates;
            var coord = ChooseNearbyFreeCell(grid, takenPhysical, anchorCoord, searchRadius: 6)
                ?? throw new InvalidOperationException(
                    $"Could not place ability '{plan.Name}' near {plan.AnchorRegionKey}.");
            ReservePhysical(coord, takenPhysical, plan.Name);

            var ability = CreateEntity(
                seed,
                K.Ability,
                plan.Subtype,
                plan.Name,
                C(plan.Culture),
                coordinates: (coord.Q, coord.R));
            SetSemanticProjected(ability, coord, takenSemantic, RandomSemanticBias(random));

            CreateRelationship(
                seed,
                "anchored",
                RelationshipPolarity.Neutral,
                ability.SeedEntityId,
                anchorRegion.OwnerEntityId,
                0.8f);

            CreateAbilityPractitioner(seed, ability, plan.PrimaryHolderName, 0.85f, taughtBy: false);
            CreateAbilityPractitioner(seed, ability, plan.SecondaryHolderName, 0.7f, taughtBy: true);
        }
    }

    private static void CreateAbilityPractitioner(
        SeedAssembly seed,
        SeedEntity ability,
        string holderName,
        float strength,
        bool taughtBy)
    {
        var holder = seed.Entities.FirstOrDefault(entity =>
            string.Equals(entity.Name, holderName, StringComparison.Ordinal));
        if (holder is null)
        {
            throw new InvalidOperationException(
                $"Ability '{ability.Name}' references unknown holder '{holderName}'.");
        }

        var facts = taughtBy
            ? new[] { IceRemembersRelationshipFact.TaughtBy.ToDomainName() }
            : null;

        seed.CreateRelationship(
            IceRemembersRelationship.PracticedBy.ToDomainName(),
            RelationshipPolarity.Positive,
            ability.SeedEntityId,
            holder.SeedEntityId,
            strength,
            facts);
    }

    private static void CreateTransports(
        SeedAssembly seed,
        HexGrid grid,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        Dictionary<string, MapCoverage> regions,
        SeedEntity northernCompact)
    {
        var anchorCoord = seed.Entity(regions["heartlands"].OwnerEntityId).Coordinates;
        var coord = ChooseNearbyFreeCell(grid, takenPhysical, anchorCoord, searchRadius: 8)
            ?? throw new InvalidOperationException("Could not place Aurora Charter near Heartlands.");
        ReservePhysical(coord, takenPhysical, "Aurora Charter");

        var transport = CreateEntity(
            seed,
            K.Transport,
            "battleship",
            "Aurora Charter",
            C(IceRemembersCulture.AuroraStack),
            coordinates: (coord.Q, coord.R));
        SetSemanticProjected(transport, coord, takenSemantic, RandomSemanticBias(random));

        CreateRelationship(
            seed,
            "holds",
            RelationshipPolarity.Neutral,
            northernCompact.SeedEntityId,
            transport.SeedEntityId,
            0.9f);
    }

    private static void CreateOccurrences(
        SeedAssembly seed,
        HexGrid grid,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        SeedEntity frosthaven)
    {
        var coord = ChooseNearbyFreeCell(grid, takenPhysical, frosthaven.Coordinates, searchRadius: 6)
            ?? throw new InvalidOperationException("Could not place Lantern Vigil near Aurora Stack.");
        ReservePhysical(coord, takenPhysical, "Lantern Vigil");

        var occurrence = CreateEntity(
            seed,
            K.Occurrence,
            "festival",
            "Lantern Vigil",
            C(IceRemembersCulture.AuroraStack),
            coordinates: (coord.Q, coord.R));
        SetSemanticProjected(occurrence, coord, takenSemantic, RandomSemanticBias(random));

        CreateRelationship(
            seed,
            "home_base",
            RelationshipPolarity.Neutral,
            occurrence.SeedEntityId,
            frosthaven.SeedEntityId,
            1f);
    }
}
