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
        DomainTerrain(IceRemembersTerrain.Wastes, "Wastes", "harsh_land"),
        DomainTerrain(IceRemembersTerrain.Crags, "Crags", "harsh_land"),
        DomainTerrain(IceRemembersTerrain.Tundra, "Tundra", "frozen_land"),
        DomainTerrain(IceRemembersTerrain.Ice, "Ice", "frozen_land"),
        DomainTerrain(IceRemembersTerrain.HotSprings, "Hot Springs", "sanctuary"),
        DomainTerrain(IceRemembersTerrain.Shelf, "Shelf", "maritime"),
        DomainTerrain(IceRemembersTerrain.Reef, "Reef", "maritime"),
        DomainTerrain(IceRemembersTerrain.Ocean, "Ocean", "maritime"),
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
        string boundaryGroup) =>
        new()
        {
            Name = terrain.ToDomainName(),
            DisplayName = displayName,
            BoundaryGroup = boundaryGroup,
        };

    private static TerrainDefinition Terrain(
        IceRemembersTerrain terrain,
        string displayName,
        string boundaryGroup) =>
        new(
            terrain.ToDomainName(),
            displayName,
            boundaryGroup);
}
