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
}

public static class IceRemembersTerrainExtensions
{
    public static string ToDomainName(this IceRemembersTerrain terrain) => terrain switch
    {
        IceRemembersTerrain.Wastes => "wastes",
        IceRemembersTerrain.Crags => "crags",
        IceRemembersTerrain.Tundra => "tundra",
        IceRemembersTerrain.Ice => "ice",
        IceRemembersTerrain.HotSprings => "hot_springs",
        IceRemembersTerrain.Shelf => "shelf",
        IceRemembersTerrain.Reef => "reef",
        IceRemembersTerrain.Ocean => "ocean",
        _ => throw new ArgumentOutOfRangeException(nameof(terrain), terrain, "Unknown Ice Remembers terrain"),
    };
}

public static class IceRemembersTerrainCatalog
{
    public static IReadOnlyList<TerrainConfig> DomainTerrains { get; } =
    [
        DomainTerrain(IceRemembersTerrain.Wastes, "Wastes", "harsh_land", 1.6f, 0.75f, 1.35f, 0.8f, 0.6f, 1.8f),
        DomainTerrain(IceRemembersTerrain.Crags, "Crags", "harsh_land", 1.8f, 0.8f, 1.5f, 0.75f, 0.4f, 1.5f),
        DomainTerrain(IceRemembersTerrain.Tundra, "Tundra", "frozen_land", 1f, 1f, 1f, 1f, 1f, 1.5f),
        DomainTerrain(IceRemembersTerrain.Ice, "Ice", "frozen_land", 1.25f, 0.9f, 1.1f, 0.95f, 0.8f, 1.4f),
        DomainTerrain(IceRemembersTerrain.HotSprings, "Hot Springs", "sanctuary", 0.9f, 1.35f, 0.75f, 1.25f, 0.7f, 1.2f),
        DomainTerrain(IceRemembersTerrain.Shelf, "Shelf", "maritime", 1.1f, 1f, 1f, 1f, 1.2f, 1.6f),
        DomainTerrain(
            IceRemembersTerrain.Reef,
            "Reef",
            "maritime",
            1.35f,
            0.95f,
            1.1f,
            0.9f,
            0.5f,
            1.3f,
            OrcaWaterProfile(0.8f)),
        DomainTerrain(
            IceRemembersTerrain.Ocean,
            "Ocean",
            "maritime",
            1.6f,
            0.7f,
            1.6f,
            0.5f,
            0.2f,
            1.15f,
            OrcaWaterProfile(0.75f)),
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

    private static TerrainDefinition Terrain(
        IceRemembersTerrain terrain,
        string displayName,
        string boundaryGroup) =>
        new(
            terrain.ToDomainName(),
            displayName,
            boundaryGroup);
}
