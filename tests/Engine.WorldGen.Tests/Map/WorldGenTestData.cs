namespace Engine.WorldGen.Tests.Map;

using Engine.WorldGen;

public static class WorldGenTestData
{
    public const string Wastes = "wastes";
    public const string Tundra = "tundra";
    public const string Crags = "crags";
    public const string HotSprings = "hot_springs";
    public const string Shelf = "shelf";
    public const string Ocean = "ocean";

    public static TerrainCatalog TerrainCatalog() => new(
    [
        new TerrainDefinition(Wastes, "Wastes", 1.4f, 0.2f, "harsh_land"),
        new TerrainDefinition(Tundra, "Tundra", 1.1f, 0.75f, "frozen_land"),
        new TerrainDefinition(Crags, "Crags", 4f, 0.1f, "harsh_land"),
        new TerrainDefinition(HotSprings, "Hot Springs", 1f, 0.95f, "sanctuary"),
        new TerrainDefinition(Shelf, "Shelf", 1.8f, 0.45f, "maritime"),
        new TerrainDefinition(Ocean, "Ocean", float.PositiveInfinity, 0f, "maritime"),
    ]);

    public static HexGrid Grid(int radius) => new(radius, TerrainCatalog(), Wastes);

    public static HexCell Cell(string terrainName, float fertility = 0.5f) =>
        new(new HexCoord(0, 0), TerrainCatalog().Resolve(terrainName), fertility: fertility);
}
