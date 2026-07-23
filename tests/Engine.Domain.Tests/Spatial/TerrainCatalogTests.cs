namespace Engine.Domain.Tests.Spatial;

using Engine.Domain.Spatial;

public sealed class TerrainCatalogTests
{
    [Fact]
    public void Resolve_RequiresExactStableNameCasing()
    {
        var catalog = new TerrainCatalog(
        [
            new TerrainDefinition("tundra", "Tundra", "frozen_land"),
        ]);

        Assert.Equal("Tundra", catalog.Resolve("tundra").DisplayName);
        Assert.Throws<KeyNotFoundException>(() => catalog.Resolve("Tundra"));
    }

    [Fact]
    public void Constructor_RejectsDuplicateStableNames()
    {
        Assert.Throws<InvalidOperationException>(() => new TerrainCatalog(
        [
            new TerrainDefinition("wastes", "Wastes", "harsh_land"),
            new TerrainDefinition("wastes", "Other Wastes", "other_land"),
        ]));
    }

    [Theory]
    [InlineData("", "Wastes", "harsh_land")]
    [InlineData("wastes", "", "harsh_land")]
    [InlineData("wastes", "Wastes", "")]
    [InlineData(" ", "Wastes", "harsh_land")]
    [InlineData("wastes", " ", "harsh_land")]
    [InlineData("wastes", "Wastes", " ")]
    public void Constructor_RejectsBlankTerrainIdentityFields(
        string stableName,
        string displayName,
        string boundaryGroup)
    {
        Assert.Throws<ArgumentException>(() => new TerrainCatalog(
        [
            new TerrainDefinition(stableName, displayName, boundaryGroup),
        ]));
    }

    [Fact]
    public void Resolve_RejectsUnknownOrBlankTerrainNames()
    {
        var catalog = SpatialTestData.TerrainCatalog();

        Assert.Throws<ArgumentException>(() => catalog.Resolve(""));
        Assert.Throws<KeyNotFoundException>(() => catalog.Resolve("glacier"));
    }
}
