namespace TheCanonryGame.UI;

internal static class TerrainKey
{
    public const string Wastes = "Wastes";
    public const string Crags = "Crags";
    public const string Tundra = "Tundra";
    public const string Ice = "Ice";
    public const string HotSprings = "HotSprings";
    public const string Shelf = "Shelf";
    public const string Reef = "Reef";
    public const string Ocean = "Ocean";

    public static string Canonicalize(string terrainName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(terrainName);

        var normalized = terrainName
            .Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        return normalized switch
        {
            "WASTES" => Wastes,
            "CRAGS" => Crags,
            "TUNDRA" => Tundra,
            "ICE" => Ice,
            "HOTSPRINGS" => HotSprings,
            "SHELF" => Shelf,
            "REEF" => Reef,
            "OCEAN" => Ocean,
            _ => terrainName,
        };
    }
}
