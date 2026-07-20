namespace IceRemembers;

using Engine.Constants;
using Engine.Domain.Config;
using Engine.WorldGen;

using K = IceRemembers.IceRemembersKinds;
using Profile = IceRemembers.IceRemembersMovementProfile;

public enum IceRemembersTerrain
{
    Wastes,
    Crags,
    Tundra,
    Ice,
    HotSprings,
    Shelf,
    Reef,
    Ocean,
    Verdant,
    Floodplain,
    Fjord,
    Lake,
    AuroraField,
    Basalt,
    SeaIce,
    Lineage,
    Yoke,
    Witness,
    Silence,
    Severance,
    Breakaway,
    BeaconSpire,
    TabooRidge,
    CavePocket,
    OpenField,
    SpireTier,
    TunnelGallery,
    DeepIce,
}

public static class IceRemembersTerrainExtensions
{
    private static readonly string[] DomainNames =
    [
        "wastes",
        "crags",
        "tundra",
        "ice",
        "hot_springs",
        "shelf",
        "reef",
        "ocean",
        "verdant",
        "floodplain",
        "fjord",
        "lake",
        "aurora_field",
        "basalt_flats",
        "sea_ice",
        "lineage",
        "yoke",
        "witness",
        "silence",
        "severance",
        "breakaway",
        "beacon_spire",
        "taboo_ridge",
        "cave_pocket",
        "open_field",
        "spire_tier",
        "tunnel_gallery",
        "deep_ice",
    ];

    public static string ToDomainName(this IceRemembersTerrain terrain)
    {
        var index = (int)terrain;
        return (uint)index < (uint)DomainNames.Length
            ? DomainNames[index]
            : throw new ArgumentOutOfRangeException(nameof(terrain), terrain, "Unknown Ice Remembers terrain");
    }
}

public static class IceRemembersTerrainCatalog
{
    public static IReadOnlyList<TerrainConfig> DomainTerrains { get; } =
    [
        DomainTerrain(IceRemembersTerrain.Wastes, "Wastes", "harsh_land", 5.2f, 0.75f, 1.35f, 0.8f, 0.6f, 4.6f,
            movementProfiles: PhysicalMovement(trail: 5.2f, shore: 6f, wake: 0f)),
        DomainTerrain(IceRemembersTerrain.Crags, "Crags", "harsh_land", 6f, 0.8f, 1.5f, 0.75f, 0.4f, 4.2f,
            movementProfiles: PhysicalMovement(trail: 6f, shore: 6.5f, wake: 0f)),
        DomainTerrain(IceRemembersTerrain.Tundra, "Tundra", "frozen_land", 3f, 1f, 1f, 1f, 1f, 3.8f,
            movementProfiles: PhysicalMovement(trail: 3f, shore: 3.2f, wake: 6f)),
        DomainTerrain(IceRemembersTerrain.Ice, "Ice", "frozen_land", 4f, 0.9f, 1.1f, 0.95f, 0.8f, 3.6f,
            movementProfiles: PhysicalMovement(trail: 4f, shore: 4.2f, wake: 5f)),
        DomainTerrain(IceRemembersTerrain.HotSprings, "Hot Springs", "warm_land", 2.5f, 1.35f, 0.75f, 1.25f, 0.7f, 3.2f,
            movementProfiles: PhysicalMovement(trail: 2.5f, shore: 2.6f, wake: 0f)),
        DomainTerrain(IceRemembersTerrain.Shelf, "Shelf", "maritime", 3.3f, 1f, 1f, 1f, 1.2f, 4f,
            movementProfiles: PhysicalMovement(trail: 3.3f, shore: 2.8f, wake: 2.4f),
            isWater: true),
        DomainTerrain(
            IceRemembersTerrain.Reef,
            "Reef",
            "maritime",
            4.4f,
            0.95f,
            1.1f,
            0.9f,
            0.5f,
            1f,
            movementProfiles: PhysicalMovement(trail: 0f, shore: 4.8f, wake: 2.2f),
            isWater: true),
        DomainTerrain(
            IceRemembersTerrain.Ocean,
            "Ocean",
            "maritime",
            5.3f,
            0.7f,
            1.6f,
            0.5f,
            0.2f,
            1f,
            movementProfiles: PhysicalMovement(trail: 0f, shore: 6f, wake: 2f),
            isWater: true),
        // The good land: scarce warm steppe. Highest land capacity and
        // culture pull - the ground fronts fight over.
        DomainTerrain(IceRemembersTerrain.Verdant, "Verdant", "warm_land", 2.2f, 1.25f, 0.8f, 1.2f, 1.3f, 4.4f,
            movementProfiles: PhysicalMovement(trail: 2.2f, shore: 2.4f, wake: 0f)),
        // River lands: valleys, floodplains, deltas. Fertile corridor ground
        // - cheap to walk, road-friendly, worth settling.
        DomainTerrain(IceRemembersTerrain.Floodplain, "Floodplain", "warm_land", 2.6f, 1.15f, 0.85f, 1.15f, 1.2f, 4.2f,
            movementProfiles: PhysicalMovement(trail: 2.6f, shore: 2.4f, wake: 0f)),
        // The natural harbor: sheltered glacial-carved water.
        DomainTerrain(IceRemembersTerrain.Fjord, "Fjord", "maritime", 3f, 1f, 1.1f, 0.95f, 0.6f, 1f,
            movementProfiles: PhysicalMovement(trail: 0f, shore: 3f, wake: 2.6f),
            isWater: true),
        // Inland freshwater: no sea connectivity for Wake movers beyond it.
        DomainTerrain(IceRemembersTerrain.Lake, "Lake", "maritime", 3f, 0.95f, 1.2f, 0.85f, 0.4f, 1f,
            movementProfiles: PhysicalMovement(trail: 0f, shore: 3f, wake: 2.8f),
            isWater: true),
        // Aurora-charged ice provinces: slow, hard, thinly habitable land
        // that pulls aurora- and memory-flavored content.
        DomainTerrain(IceRemembersTerrain.AuroraField, "Aurora Field", "frozen_land", 4.5f, 0.85f, 1.3f, 0.8f, 0.5f, 3.4f,
            movementProfiles: PhysicalMovement(trail: 4.5f, shore: 4.8f, wake: 0f)),
        // Volcanic fields: hazard-and-power ground.
        DomainTerrain(IceRemembersTerrain.Basalt, "Basalt Flats", "harsh_land", 4.8f, 0.8f, 1.25f, 0.8f, 0.7f, 3.8f,
            movementProfiles: PhysicalMovement(trail: 4.8f, shore: 5.2f, wake: 0f)),
        // Frozen sea: transient cover over water, the Deep Ice mechanism with
        // inverse polarity - walkers cross slowly, swimmers are shut out, and
        // the water underneath keeps its identity for the thaw.
        DomainTerrain(IceRemembersTerrain.SeaIce, "Sea Ice", "frozen_land", 5.5f, 0.6f, 1.5f, 0.6f, 0.2f, 3f,
            movementProfiles: PhysicalMovement(trail: 5.5f, shore: 5f, wake: 0f)),
        DomainTerrain(IceRemembersTerrain.Lineage, "Lineage", "semantic_memory", 2.6f, 1.15f, 0.85f, 1.25f, 1.1f, 3.8f,
            movementProfiles: SemanticMovement(commons: 2.4f, mandate: 2.8f, veil: 4f, rift: 5f)),
        DomainTerrain(IceRemembersTerrain.Yoke, "Yoke", "semantic_authority", 4.1f, 1.05f, 0.75f, 1.35f, 0.8f, 3.4f,
            movementProfiles: SemanticMovement(commons: 4.3f, mandate: 2.8f, veil: 6f, rift: 6f),
            kindProfiles: YokeProfiles()),
        DomainTerrain(IceRemembersTerrain.Witness, "Witness", "semantic_memory", 2.8f, 1.4f, 0.9f, 1.15f, 1.35f, 4.2f,
            movementProfiles: SemanticMovement(commons: 2.6f, mandate: 3f, veil: 4.5f, rift: 5f)),
        DomainTerrain(IceRemembersTerrain.Silence, "Silence", "semantic_shadow", 4.8f, 0.65f, 1.2f, 0.9f, 0.7f, 3.2f,
            movementProfiles: SemanticMovement(commons: 6f, mandate: 6f, veil: 2.4f, rift: 4f),
            kindProfiles: HiddenSemanticProfiles()),
        DomainTerrain(IceRemembersTerrain.Severance, "Severance", "semantic_rupture", 5.4f, 0.7f, 1.5f, 0.65f, 0.55f, 3f,
            movementProfiles: SemanticMovement(commons: 6f, mandate: 6f, veil: 4f, rift: 3f)),
        DomainTerrain(IceRemembersTerrain.Breakaway, "Breakaway", "semantic_rupture", 2.4f, 1.05f, 1.25f, 0.75f, 1.3f, 4.3f,
            movementProfiles: SemanticMovement(commons: 3.2f, mandate: 4f, veil: 3f, rift: 2.2f)),
        DomainTerrain(IceRemembersTerrain.BeaconSpire, "Beacon Spire", "semantic_memory", 3.4f, 1.55f, 0.8f, 1.2f, 1.45f, 4.8f,
            movementProfiles: SemanticMovement(commons: 3f, mandate: 3.2f, veil: 6f, rift: 5.5f)),
        DomainTerrain(IceRemembersTerrain.TabooRidge, "Taboo Ridge", "semantic_shadow", 6.2f, 0.5f, 1.7f, 0.55f, 0.35f, 2.8f,
            movementProfiles: SemanticMovement(commons: 0f, mandate: 5f, veil: 3.5f, rift: 3.8f)),
        DomainTerrain(IceRemembersTerrain.CavePocket, "Cave Pocket", "semantic_shadow", 4.4f, 0.6f, 0.9f, 1.15f, 0.6f, 3.1f,
            movementProfiles: SemanticMovement(commons: 5.5f, mandate: 6f, veil: 2.5f, rift: 4.2f),
            kindProfiles: HiddenSemanticProfiles()),
        DomainTerrain(IceRemembersTerrain.OpenField, "Open Field", "semantic_open", 3f, 1f, 1f, 1f, 1f, 3.6f,
            movementProfiles: SemanticMovement(commons: 2.8f, mandate: 3f, veil: 3.3f, rift: 3.2f)),
        DomainTerrain(IceRemembersTerrain.SpireTier, "Spire Tier", "forged_height", 2.8f, 1.5f, 1.2f, 0.9f, 0f, 1f,
            movementProfiles: PhysicalMovement(trail: 2.8f, shore: 0f, wake: 0f)),
        DomainTerrain(IceRemembersTerrain.TunnelGallery, "Tunnel Gallery", "forged_depth", 3.2f, 1.2f, 1.1f, 0.9f, 0f, 1f,
            movementProfiles: PhysicalMovement(trail: 3.2f, shore: 0f, wake: 0f)),
        // The un-thawed and the reclaimed: ground the ice holds shut. Fully
        // impassable (every profile blocked), uninhabitable, uncultured -
        // the physical world's frontier and the Long Dark's reach.
        DomainTerrain(IceRemembersTerrain.DeepIce, "Deep Ice", "frozen_land", 9f, 0.2f, 3f, 0.05f, 0f, 1f,
            movementProfiles: PhysicalMovement(trail: 0f, shore: 0f, wake: 0f)),
    ];

    /// <summary>
    /// What each movement profile pays to cross a directional edge class.
    /// Rivers surcharge walkers but roads negate them (fords become
    /// bridges); ridge and cliff surcharges are road-immune - mountains are
    /// crossed at passes, which worldgen already folded to open edges. Wake
    /// ignores land edges entirely (no rows).
    /// </summary>
    public static IReadOnlyList<CrossingProfileConfig> CrossingProfiles { get; } =
    [
        Crossing(PhysicalCrossingCodes.River, Profile.Trail, 1.6f, roadNegated: true),
        Crossing(PhysicalCrossingCodes.Ridge, Profile.Trail, 2f),
        Crossing(PhysicalCrossingCodes.Ridge, Profile.Shore, 2f),
        Crossing(PhysicalCrossingCodes.Cliff, Profile.Trail, 4f),
        Crossing(PhysicalCrossingCodes.Cliff, Profile.Shore, 4f),
    ];

    private static CrossingProfileConfig Crossing(
        int crossingClass,
        Profile profile,
        float factor,
        bool roadNegated = false) =>
        new()
        {
            CrossingClass = crossingClass,
            Profile = profile.ToDomainName(),
            Factor = factor,
            RoadNegated = roadNegated,
        };

    public static TerrainCatalog CreateWorldGenCatalog() =>
        new(
        [
            Terrain(IceRemembersTerrain.Tundra, "Tundra", "frozen_land"),
            Terrain(IceRemembersTerrain.HotSprings, "Hot Springs", "warm_land"),
            Terrain(IceRemembersTerrain.Shelf, "Shelf", "maritime"),
            Terrain(IceRemembersTerrain.Crags, "Crags", "harsh_land"),
            Terrain(IceRemembersTerrain.Wastes, "Wastes", "harsh_land"),
            Terrain(IceRemembersTerrain.Ocean, "Ocean", "maritime"),
            Terrain(IceRemembersTerrain.Reef, "Reef", "maritime"),
            Terrain(IceRemembersTerrain.Ice, "Ice", "frozen_land"),
            Terrain(IceRemembersTerrain.Verdant, "Verdant", "warm_land"),
            Terrain(IceRemembersTerrain.Floodplain, "Floodplain", "warm_land"),
            Terrain(IceRemembersTerrain.Fjord, "Fjord", "maritime"),
            Terrain(IceRemembersTerrain.Lake, "Lake", "maritime"),
            Terrain(IceRemembersTerrain.AuroraField, "Aurora Field", "frozen_land"),
            Terrain(IceRemembersTerrain.Basalt, "Basalt Flats", "harsh_land"),
        ]);

    private static TerrainConfig DomainTerrain(
        IceRemembersTerrain terrain,
        string displayName,
        string boundaryGroup,
        float movementCost,
        float influenceMultiplier,
        float claimCostMultiplier,
        float claimChanceMultiplier,
        float roadBuildMultiplier,
        float roadSpeedMultiplier,
        IReadOnlyList<TerrainMovementProfileConfig>? movementProfiles = null,
        IReadOnlyList<TerrainKindProfileConfig>? kindProfiles = null,
        bool isWater = false) =>
        new()
        {
            Name = terrain.ToDomainName(),
            DisplayName = displayName,
            BoundaryGroup = boundaryGroup,
            IsWater = isWater,
            MovementCost = movementCost,
            InfluenceMultiplier = influenceMultiplier,
            ClaimCostMultiplier = claimCostMultiplier,
            ClaimChanceMultiplier = claimChanceMultiplier,
            RoadBuildMultiplier = roadBuildMultiplier,
            RoadSpeedMultiplier = roadSpeedMultiplier,
            MovementProfiles = movementProfiles ?? [],
            KindProfiles = kindProfiles ?? [],
        };

    private static IReadOnlyList<TerrainMovementProfileConfig> PhysicalMovement(
        float trail,
        float shore,
        float wake) =>
    [
        Move(Profile.Trail, trail),
        Move(Profile.Shore, shore),
        Move(Profile.Wake, wake),
    ];

    private static IReadOnlyList<TerrainMovementProfileConfig> SemanticMovement(
        float commons,
        float mandate,
        float veil,
        float rift) =>
    [
        Move(Profile.Commons, commons),
        Move(Profile.Mandate, mandate),
        Move(Profile.Veil, veil),
        Move(Profile.Rift, rift),
    ];

    private static TerrainMovementProfileConfig Move(Profile profile, float movementCost) =>
        new()
        {
            Profile = profile.ToDomainName(),
            MovementCost = movementCost,
        };

    private static IReadOnlyList<TerrainKindProfileConfig> YokeProfiles() =>
    [
        new()
        {
            Kind = K.Faction,
            ClaimCostMultiplier = 0.65f,
            ClaimChanceMultiplier = 1.45f,
        },
        new()
        {
            Kind = K.Settlement,
            ClaimCostMultiplier = 0.8f,
            ClaimChanceMultiplier = 1.25f,
        },
    ];

    private static IReadOnlyList<TerrainKindProfileConfig> HiddenSemanticProfiles() =>
    [
        new()
        {
            Kind = K.Artifact,
            InfluenceMultiplier = 0.8f,
            ClaimCostMultiplier = 0.8f,
            ClaimChanceMultiplier = 1.2f,
        },
        new()
        {
            Kind = K.Monster,
            InfluenceMultiplier = 1.15f,
            ClaimCostMultiplier = 0.65f,
            ClaimChanceMultiplier = 1.35f,
        },
        new()
        {
            Kind = K.Occurrence,
            InfluenceMultiplier = 1.05f,
            ClaimCostMultiplier = 0.8f,
            ClaimChanceMultiplier = 1.2f,
        },
    ];

    private static TerrainDefinition Terrain(
        IceRemembersTerrain terrain,
        string displayName,
        string boundaryGroup) =>
        new(
            terrain.ToDomainName(),
            displayName,
            boundaryGroup);
}
