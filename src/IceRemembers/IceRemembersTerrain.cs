namespace IceRemembers;

using Engine.Domain.Config;
using Engine.WorldGen;

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
        DomainTerrain(IceRemembersTerrain.Wastes, "Wastes", true, "harsh_land"),
        DomainTerrain(IceRemembersTerrain.Crags, "Crags", true, "harsh_land"),
        DomainTerrain(IceRemembersTerrain.Tundra, "Tundra", true, "frozen_land"),
        DomainTerrain(IceRemembersTerrain.Ice, "Ice", true, "frozen_land"),
        DomainTerrain(IceRemembersTerrain.HotSprings, "Hot Springs", true, "sanctuary"),
        DomainTerrain(IceRemembersTerrain.Shelf, "Shelf", true, "maritime"),
        DomainTerrain(IceRemembersTerrain.Reef, "Reef", true, "maritime"),
        DomainTerrain(IceRemembersTerrain.Ocean, "Ocean", false, "maritime"),
    ];

    public static TerrainCatalog CreateWorldGenCatalog() =>
        new(
        [
            Terrain(IceRemembersTerrain.Tundra, "Tundra", true, 1.1f, 0.75f, "frozen_land", 4f, 6f, 2f, 4f, 3f, 5f),
            Terrain(IceRemembersTerrain.HotSprings, "Hot Springs", true, 1f, 0.95f, "sanctuary", 5f, 7f, 2f, 4f, 4f, 6f),
            Terrain(IceRemembersTerrain.Shelf, "Shelf", true, 1.8f, 0.45f, "maritime", 1f, 3f, 1f, 2f, 1f, 2f),
            Terrain(IceRemembersTerrain.Crags, "Crags", true, 4f, 0.1f, "harsh_land", 0f, 2f, 2f, 5f, 0f, 2f),
            Terrain(IceRemembersTerrain.Wastes, "Wastes", true, 1.4f, 0.2f, "harsh_land", 0f, 1f, 1f, 3f, 0f, 2f),
            Terrain(IceRemembersTerrain.Ocean, "Ocean", false, 0f, 0f, "maritime", 0f, 1f, 0f, 1f, 0f, 1f),
            Terrain(IceRemembersTerrain.Reef, "Reef", true, 1.6f, 0.55f, "maritime", 2f, 5f, 3f, 5f, 2f, 4f),
            Terrain(IceRemembersTerrain.Ice, "Ice", true, 2.5f, 0.3f, "frozen_land", 0f, 2f, 0f, 2f, 0f, 3f),
        ]);

    private static TerrainConfig DomainTerrain(
        IceRemembersTerrain terrain,
        string displayName,
        bool isPassable,
        string boundaryGroup) =>
        new()
        {
            Name = terrain.ToDomainName(),
            DisplayName = displayName,
            IsPassable = isPassable,
            BoundaryGroup = boundaryGroup,
        };

    private static TerrainDefinition Terrain(
        IceRemembersTerrain terrain,
        string displayName,
        bool isPassable,
        float movementCost,
        float habitabilityFactor,
        string boundaryGroup,
        float foodMin,
        float foodMax,
        float wealthMin,
        float wealthMax,
        float populationMin,
        float populationMax) =>
        new(
            terrain.ToDomainName(),
            displayName,
            isPassable,
            movementCost,
            habitabilityFactor,
            boundaryGroup,
            new Dictionary<string, AmbientValueRange>(StringComparer.Ordinal)
            {
                [IceRemembersResource.Food.ToDomainName()] = new(foodMin, foodMax),
                [IceRemembersResource.Wealth.ToDomainName()] = new(wealthMin, wealthMax),
                [IceRemembersResource.Population.ToDomainName()] = new(populationMin, populationMax),
            });
}
