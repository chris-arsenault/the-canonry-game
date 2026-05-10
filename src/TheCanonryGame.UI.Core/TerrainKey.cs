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
    public const string Lineage = "Lineage";
    public const string Yoke = "Yoke";
    public const string Witness = "Witness";
    public const string Silence = "Silence";
    public const string Severance = "Severance";
    public const string Breakaway = "Breakaway";
    public const string BeaconSpire = "BeaconSpire";
    public const string TabooRidge = "TabooRidge";
    public const string CavePocket = "CavePocket";
    public const string OpenField = "OpenField";

    private static readonly global::System.Collections.Generic.Dictionary<string, string> CanonicalNames = new(global::System.StringComparer.Ordinal)
    {
        ["WASTES"] = Wastes,
        ["CRAGS"] = Crags,
        ["TUNDRA"] = Tundra,
        ["ICE"] = Ice,
        ["HOTSPRINGS"] = HotSprings,
        ["SHELF"] = Shelf,
        ["REEF"] = Reef,
        ["OCEAN"] = Ocean,
        ["LINEAGE"] = Lineage,
        ["YOKE"] = Yoke,
        ["WITNESS"] = Witness,
        ["SILENCE"] = Silence,
        ["SEVERANCE"] = Severance,
        ["BREAKAWAY"] = Breakaway,
        ["BEACONSPIRE"] = BeaconSpire,
        ["TABOORIDGE"] = TabooRidge,
        ["CAVEPOCKET"] = CavePocket,
        ["OPENFIELD"] = OpenField,
    };

    public static string Canonicalize(string terrainName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(terrainName);

        var normalized = terrainName
            .Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        return CanonicalNames.TryGetValue(normalized, out var canonical) ? canonical : terrainName;
    }
}
