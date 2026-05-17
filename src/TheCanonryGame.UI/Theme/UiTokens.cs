using Godot;

namespace TheCanonryGame.UI.Theme;

/// <summary>
/// Central source of truth for all UI design tokens. Every panel, button, card,
/// label, and drawn surface pulls colors, spacing, sizes, and fonts from here.
///
/// Direction: "The Glacial Archive" — editorial archival aesthetic with a cold
/// palette, warm-cream text, one ember accent used rarely. See
/// .interface-design/system.md for full design rationale.
/// </summary>
public static class UiTokens
{
    // Surface (cold, layered). Lower number = deeper / darker / further away.
    public static readonly Color Ink = Hex(0x0B0F14);
    public static readonly Color Basalt = Hex(0x121722);
    public static readonly Color Slate = Hex(0x19202C);
    public static readonly Color Frost = Hex(0x222B3A);
    public static readonly Color Ice = Hex(0x2E3A4F);

    // Text (warm, layered). Warm cream against cold surfaces.
    public static readonly Color Vellum = Hex(0xE7E3D5);
    public static readonly Color Parchment = Hex(0xB8B5A6);
    public static readonly Color Whisper = Hex(0x6F7684);
    public static readonly Color Charcoal = Hex(0x3E4654);

    // Rules and borders — always derived from Vellum at low alpha.
    public static readonly Color Hairline = new(0xE7 / 255f, 0xE3 / 255f, 0xD5 / 255f, 0.08f);
    public static readonly Color HairlineStrong = new(0xE7 / 255f, 0xE3 / 255f, 0xD5 / 255f, 0.20f);

    // Accents. Ember is sacred — used for selected / last-fired / critical only.
    public static readonly Color Ember = Hex(0xD97742);
    public static readonly Color Aurora = Hex(0x78C9B2);
    public static readonly Color Blood = Hex(0xB04646);
    public static readonly Color Gilt = Hex(0xC9A96B);

    // Soft shadow on cards.
    public static readonly Color CardShadow = new(0f, 0f, 0f, 0.28f);

    // Kind accents — muted cold-palette variants used on hex map and card borders.
    public static readonly Color KindPenguin = Hex(0x8AB4C9);
    public static readonly Color KindSettlement = Hex(0xC9A96B);
    public static readonly Color KindFaction = Hex(0xB4707A);
    public static readonly Color KindArtifact = Hex(0xA6C8B8);
    public static readonly Color KindRegion = Hex(0x8896C2);
    public static readonly Color KindOrca = Hex(0x556A7F);
    public static readonly Color KindDefault = Hex(0xA0A498);

    // Culture accents. These are used both on the cultural map roll-up and on
    // entity action rings so cause and territory read as the same signal.
    public static readonly Color CultureAuroraStack = Hex(0x78C9B2);
    public static readonly Color CultureNightshelf = Hex(0x8E7CC3);
    public static readonly Color CultureWakeSingers = Hex(0xC9A96B);
    public static readonly Color CultureNone = Hex(0x6F7684);

    // Terrain — muted realistic palette.
    public static readonly Color TerrainWastes = Hex(0x8C8A80);
    public static readonly Color TerrainCrags = Hex(0x54555A);
    public static readonly Color TerrainTundra = Hex(0xA5B1B8);
    public static readonly Color TerrainIce = Hex(0xC7D2DA);
    public static readonly Color TerrainHotSprings = Hex(0x7BA488);
    public static readonly Color TerrainShelf = Hex(0x6889A1);
    public static readonly Color TerrainReef = Hex(0x3E6078);
    public static readonly Color TerrainOcean = Hex(0x1C2B3F);
    public static readonly Color TerrainLineage = Hex(0xBFA36A);
    public static readonly Color TerrainYoke = Hex(0x7E5D74);
    public static readonly Color TerrainWitness = Hex(0x5AAE9B);
    public static readonly Color TerrainSilence = Hex(0x2F3445);
    public static readonly Color TerrainSeverance = Hex(0xA16A4D);
    public static readonly Color TerrainBreakaway = Hex(0x6C93C8);
    public static readonly Color TerrainBeaconSpire = Hex(0xB8D6D2);
    public static readonly Color TerrainTabooRidge = Hex(0x5A3D55);
    public static readonly Color TerrainCavePocket = Hex(0x4B5966);
    public static readonly Color TerrainOpenField = Hex(0x8FAF8A);

    private static readonly global::System.Collections.Generic.Dictionary<string, Color> TerrainColors = new(global::System.StringComparer.Ordinal)
    {
        [TerrainKey.Wastes] = TerrainWastes,
        [TerrainKey.Crags] = TerrainCrags,
        [TerrainKey.Tundra] = TerrainTundra,
        [TerrainKey.Ice] = TerrainIce,
        [TerrainKey.HotSprings] = TerrainHotSprings,
        [TerrainKey.Shelf] = TerrainShelf,
        [TerrainKey.Reef] = TerrainReef,
        [TerrainKey.Ocean] = TerrainOcean,
        [TerrainKey.Lineage] = TerrainLineage,
        [TerrainKey.Yoke] = TerrainYoke,
        [TerrainKey.Witness] = TerrainWitness,
        [TerrainKey.Silence] = TerrainSilence,
        [TerrainKey.Severance] = TerrainSeverance,
        [TerrainKey.Breakaway] = TerrainBreakaway,
        [TerrainKey.BeaconSpire] = TerrainBeaconSpire,
        [TerrainKey.TabooRidge] = TerrainTabooRidge,
        [TerrainKey.CavePocket] = TerrainCavePocket,
        [TerrainKey.OpenField] = TerrainOpenField,
    };

    // Faction palette on the map — cold, distinguishable.
    public static readonly Color[] FactionPalette =
    [
        Hex(0xB4707A), // dusty rose
        Hex(0x7A8CB4), // periwinkle
        Hex(0x78C9B2), // aurora
        Hex(0xC9A96B), // gilt
        Hex(0x8896C2), // cold iris
        Hex(0xA6C8B8), // sea-glass
    ];

    // Spacing scale. No ad-hoc values — always pick one of these.
    public const int Space1 = 4;
    public const int Space2 = 8;
    public const int Space3 = 12;
    public const int Space4 = 16;
    public const int Space5 = 24;
    public const int Space6 = 32;
    public const int Space7 = 48;

    // Radius scale.
    public const int RadiusSharp = 2;
    public const int RadiusSmall = 6;
    public const int RadiusMedium = 10;
    public const int RadiusLarge = 14;

    // Font sizes.
    public const int SizeTitleLarge = 22;
    public const int SizeTitleMedium = 18;
    public const int SizeTitleSmall = 15;
    public const int SizeBody = 13;
    public const int SizeBodySmall = 11;
    public const int SizeCaption = 10;
    public const int SizeMicro = 9;
    public const int SizeNumeric = 13;

    // Font fallback chains — system fonts with graceful degradation.
    public static readonly string[] DisplayFontNames =
    [
        "EB Garamond",
        "Cormorant Garamond",
        "Iowan Old Style",
        "Georgia",
        "DejaVu Serif",
        "Liberation Serif",
        "Noto Serif",
    ];

    public static readonly string[] BodyFontNames =
    [
        "IBM Plex Sans",
        "Inter Tight",
        "Source Sans Pro",
        "DejaVu Sans",
        "Liberation Sans",
        "Noto Sans",
    ];

    public static readonly string[] MonoFontNames =
    [
        "IBM Plex Mono",
        "JetBrains Mono",
        "Fira Code",
        "DejaVu Sans Mono",
        "Liberation Mono",
        "Noto Sans Mono",
    ];

    // Kind accent lookup. Accepts the string KindId value (e.g. "penguin", "settlement").
    public static Color KindAccent(string kindId) => kindId switch
    {
        "penguin" => KindPenguin,
        "settlement" => KindSettlement,
        "faction" => KindFaction,
        "artifact" => KindArtifact,
        "region" => KindRegion,
        "orca" => KindOrca,
        _ => KindDefault,
    };

    public static Color CultureColor(string cultureId) => cultureId switch
    {
        "aurora_stack" => CultureAuroraStack,
        "nightshelf" => CultureNightshelf,
        "wake_singers" => CultureWakeSingers,
        "none" => CultureNone,
        _ => FactionPalette[StablePaletteIndex(cultureId)],
    };

    public static Color TerrainColor(string stableName) =>
        TerrainColors.TryGetValue(TerrainKey.Canonicalize(stableName), out var color) ? color : Whisper;

    public static Color WithAlpha(Color color, float alpha) =>
        new(color.R, color.G, color.B, alpha);

    private static Color Hex(uint rgb) =>
        new(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f);

    private static int StablePaletteIndex(string value)
    {
        unchecked
        {
            var hash = 17;
            for (var i = 0; i < value.Length; i++)
            {
                hash = (hash * 31) + value[i];
            }

            return (hash & 0x7FFFFFFF) % FactionPalette.Length;
        }
    }
}
