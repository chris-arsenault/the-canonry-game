namespace Engine.Domain.Tests;

using Engine.WorldGen;

internal static class TestTerrainCatalog
{
    public const string DefaultTerrain = "wastes";

    public static TerrainCatalog Create() => new(
    [
        new TerrainDefinition(DefaultTerrain, "Wastes", 1f, 0.2f, "land"),
        new TerrainDefinition("tundra", "Tundra", 1f, 0.6f, "land"),
        new TerrainDefinition("ice", "Ice", 2f, 0.1f, "frozen"),
        new TerrainDefinition("ocean", "Ocean", float.PositiveInfinity, 0f, "water"),
    ]);
}
