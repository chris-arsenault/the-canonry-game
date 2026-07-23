namespace Engine.Domain.Tests.Spatial;

using Engine.Domain.Spatial;

internal static class SpatialTestData
{
    public const string Wastes = "wastes";
    public const string Tundra = "tundra";
    public const string Crags = "crags";
    public const string HotSprings = "hot_springs";
    public const string Shelf = "shelf";
    public const string Ocean = "ocean";

    public static TerrainCatalog TerrainCatalog() => new(
    [
        new TerrainDefinition(Wastes, "Wastes", "harsh_land"),
        new TerrainDefinition(Tundra, "Tundra", "frozen_land"),
        new TerrainDefinition(Crags, "Crags", "harsh_land"),
        new TerrainDefinition(HotSprings, "Hot Springs", "sanctuary"),
        new TerrainDefinition(Shelf, "Shelf", "maritime"),
        new TerrainDefinition(Ocean, "Ocean", "maritime"),
    ]);

    public static HexGrid Grid(int radius) => new(radius, TerrainCatalog(), Wastes);

    public static HexCell Cell(string terrainName) =>
        new(new HexCoord(0, 0), TerrainCatalog().Resolve(terrainName));
}
