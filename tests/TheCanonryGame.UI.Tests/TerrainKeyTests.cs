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
    [InlineData("lineage", TerrainKey.Lineage)]
    [InlineData("yoke", TerrainKey.Yoke)]
    [InlineData("witness", TerrainKey.Witness)]
    [InlineData("silence", TerrainKey.Silence)]
    [InlineData("severance", TerrainKey.Severance)]
    [InlineData("breakaway", TerrainKey.Breakaway)]
    [InlineData("beacon_spire", TerrainKey.BeaconSpire)]
    [InlineData("Beacon Spire", TerrainKey.BeaconSpire)]
    [InlineData("taboo-ridge", TerrainKey.TabooRidge)]
    [InlineData("cave pocket", TerrainKey.CavePocket)]
    [InlineData("open_field", TerrainKey.OpenField)]
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
