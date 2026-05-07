namespace IceRemembers;

using Engine.Domain.Config;
using Engine.WorldGen;

using K = IceRemembers.IceRemembersKinds;

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
        DomainTerrain(IceRemembersTerrain.Wastes, "Wastes", "harsh_land", 5.2f, 0.75f, 1.35f, 0.8f, 0.6f, 4.6f),
        DomainTerrain(IceRemembersTerrain.Crags, "Crags", "harsh_land", 6f, 0.8f, 1.5f, 0.75f, 0.4f, 4.2f),
        DomainTerrain(IceRemembersTerrain.Tundra, "Tundra", "frozen_land", 3f, 1f, 1f, 1f, 1f, 3.8f),
        DomainTerrain(IceRemembersTerrain.Ice, "Ice", "frozen_land", 4f, 0.9f, 1.1f, 0.95f, 0.8f, 3.6f),
        DomainTerrain(IceRemembersTerrain.HotSprings, "Hot Springs", "sanctuary", 2.5f, 1.35f, 0.75f, 1.25f, 0.7f, 3.2f),
        DomainTerrain(IceRemembersTerrain.Shelf, "Shelf", "maritime", 3.3f, 1f, 1f, 1f, 1.2f, 4f),
        DomainTerrain(
            IceRemembersTerrain.Reef,
            "Reef",
            "maritime",
            4.4f,
            0.95f,
            1.1f,
            0.9f,
            0.5f,
            3.5f,
            OrcaWaterProfile(2.2f)),
        DomainTerrain(
            IceRemembersTerrain.Ocean,
            "Ocean",
            "maritime",
            5.3f,
            0.7f,
            1.6f,
            0.5f,
            0.2f,
            3f,
            OrcaWaterProfile(2f)),
        DomainTerrain(IceRemembersTerrain.Lineage, "Lineage", "semantic_memory", 2.6f, 1.15f, 0.85f, 1.25f, 1.1f, 3.8f),
        DomainTerrain(IceRemembersTerrain.Yoke, "Yoke", "semantic_authority", 4.1f, 1.05f, 0.75f, 1.35f, 0.8f, 3.4f, YokeProfiles()),
        DomainTerrain(IceRemembersTerrain.Witness, "Witness", "semantic_memory", 2.8f, 1.4f, 0.9f, 1.15f, 1.35f, 4.2f),
        DomainTerrain(IceRemembersTerrain.Silence, "Silence", "semantic_shadow", 4.8f, 0.65f, 1.2f, 0.9f, 0.7f, 3.2f, HiddenSemanticProfiles()),
        DomainTerrain(IceRemembersTerrain.Severance, "Severance", "semantic_rupture", 5.4f, 0.7f, 1.5f, 0.65f, 0.55f, 3f),
        DomainTerrain(IceRemembersTerrain.Breakaway, "Breakaway", "semantic_rupture", 2.4f, 1.05f, 1.25f, 0.75f, 1.3f, 4.3f),
        DomainTerrain(IceRemembersTerrain.BeaconSpire, "Beacon Spire", "semantic_memory", 3.4f, 1.55f, 0.8f, 1.2f, 1.45f, 4.8f),
        DomainTerrain(IceRemembersTerrain.TabooRidge, "Taboo Ridge", "semantic_shadow", 6.2f, 0.5f, 1.7f, 0.55f, 0.35f, 2.8f),
        DomainTerrain(IceRemembersTerrain.CavePocket, "Cave Pocket", "semantic_shadow", 4.4f, 0.6f, 0.9f, 1.15f, 0.6f, 3.1f, HiddenSemanticProfiles()),
        DomainTerrain(IceRemembersTerrain.OpenField, "Open Field", "semantic_open", 3f, 1f, 1f, 1f, 1f, 3.6f),
    ];

    public static TerrainCatalog CreateWorldGenCatalog() =>
        new(
        [
            Terrain(IceRemembersTerrain.Tundra, "Tundra", "frozen_land"),
            Terrain(IceRemembersTerrain.HotSprings, "Hot Springs", "sanctuary"),
            Terrain(IceRemembersTerrain.Shelf, "Shelf", "maritime"),
            Terrain(IceRemembersTerrain.Crags, "Crags", "harsh_land"),
            Terrain(IceRemembersTerrain.Wastes, "Wastes", "harsh_land"),
            Terrain(IceRemembersTerrain.Ocean, "Ocean", "maritime"),
            Terrain(IceRemembersTerrain.Reef, "Reef", "maritime"),
            Terrain(IceRemembersTerrain.Ice, "Ice", "frozen_land"),
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
        IReadOnlyList<TerrainKindProfileConfig>? kindProfiles = null) =>
        new()
        {
            Name = terrain.ToDomainName(),
            DisplayName = displayName,
            BoundaryGroup = boundaryGroup,
            MovementCost = movementCost,
            InfluenceMultiplier = influenceMultiplier,
            ClaimCostMultiplier = claimCostMultiplier,
            ClaimChanceMultiplier = claimChanceMultiplier,
            RoadBuildMultiplier = roadBuildMultiplier,
            RoadSpeedMultiplier = roadSpeedMultiplier,
            KindProfiles = kindProfiles ?? [],
        };

    private static IReadOnlyList<TerrainKindProfileConfig> OrcaWaterProfile(float movementCost) =>
    [
        new()
        {
            Kind = K.Orca,
            MovementCost = movementCost,
            InfluenceMultiplier = 1.2f,
            ClaimCostMultiplier = 0.8f,
            ClaimChanceMultiplier = 1.2f,
        },
    ];

    private static IReadOnlyList<TerrainKindProfileConfig> YokeProfiles() =>
    [
        new()
        {
            Kind = K.Faction,
            MovementCost = 3f,
            ClaimCostMultiplier = 0.65f,
            ClaimChanceMultiplier = 1.45f,
        },
        new()
        {
            Kind = K.Settlement,
            MovementCost = 3.2f,
            ClaimCostMultiplier = 0.8f,
            ClaimChanceMultiplier = 1.25f,
        },
    ];

    private static IReadOnlyList<TerrainKindProfileConfig> HiddenSemanticProfiles() =>
    [
        new()
        {
            Kind = K.Artifact,
            MovementCost = 2.5f,
            InfluenceMultiplier = 0.8f,
            ClaimCostMultiplier = 0.8f,
            ClaimChanceMultiplier = 1.2f,
        },
        new()
        {
            Kind = K.Monster,
            MovementCost = 2.2f,
            InfluenceMultiplier = 1.15f,
            ClaimCostMultiplier = 0.65f,
            ClaimChanceMultiplier = 1.35f,
        },
        new()
        {
            Kind = K.Occurrence,
            MovementCost = 2.7f,
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
