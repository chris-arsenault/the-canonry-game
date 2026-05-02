namespace Engine.Domain.Tests;

using Engine.WorldGen;

internal static class TestTerrainCatalog
{
    public const string DefaultTerrain = "wastes";

    public static TerrainCatalog Create() => new(
    [
        new TerrainDefinition(DefaultTerrain, "Wastes", "land"),
        new TerrainDefinition("tundra", "Tundra", "land"),
        new TerrainDefinition("ice", "Ice", "frozen"),
        new TerrainDefinition("ocean", "Ocean", "water"),
    ]);
}
