namespace TheCanonryGame.UI.Tests;

using TheCanonryGame.UI;

public sealed class TerrainKeyTests
{
    [Theory]
    [InlineData("wastes", TerrainKey.Wastes)]
    [InlineData("crags", TerrainKey.Crags)]
    [InlineData("tundra", TerrainKey.Tundra)]
    [InlineData("ice", TerrainKey.Ice)]
    [InlineData("hot_springs", TerrainKey.HotSprings)]
    [InlineData("Hot Springs", TerrainKey.HotSprings)]
    [InlineData("hotsprings", TerrainKey.HotSprings)]
    [InlineData("shelf", TerrainKey.Shelf)]
    [InlineData("reef", TerrainKey.Reef)]
    [InlineData("ocean", TerrainKey.Ocean)]
    public void CanonicalizeMapsDomainTerrainNamesToUiAssetKeys(
        string terrainName,
        string expected)
    {
        Assert.Equal(expected, TerrainKey.Canonicalize(terrainName));
    }

    [Fact]
    public void CanonicalizeKeepsUnknownTerrainName()
    {
        Assert.Equal("volcanic", TerrainKey.Canonicalize("volcanic"));
    }
}
