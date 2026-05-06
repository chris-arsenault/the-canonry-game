#pragma warning disable CA2000 // Godot's Image/ImageTexture lifecycle is owned by the engine/GC; explicit dispose conflicts with GC-based semantics.
using System;
using System.Collections.Generic;

using Godot;

namespace TheCanonryGame.UI.Theme;

/// <summary>
/// Runtime registry of illustration assets served to Godot from
/// res://assets/game/. That folder is a curated, human-vetted subset — the
/// illuminator pipeline writes candidates to assets/output/processed/ (which
/// has a .gdignore so Godot never scans it), and approved assets are copied
/// into assets/game/ by hand. This keeps the Godot import tree stable so
/// regens do not trigger multi-minute reimports of unchanged assets.
///
/// Textures are decoded from PNG bytes at startup using Godot's Image API —
/// no dependence on the editor import pipeline, so the game runs without a
/// prior editor session.
///
/// Call <see cref="Load"/> once from Main._Ready() before any UI code that
/// references these assets. Individual accessors return null if Load() wasn't
/// called or the file is missing — callers fall back to polygonal glyphs
/// (KindGlyph) when a sprite isn't available.
/// </summary>
public static class AssetLibrary
{
    private const string ProcessedRoot = "res://assets/game";

    private static readonly Dictionary<string, Texture2D> _byPath = new(StringComparer.Ordinal);

    public static Texture2D? Penguin { get; private set; }
    public static Texture2D? Settlement { get; private set; }
    public static Texture2D? Orca { get; private set; }
    public static Texture2D? FactionBanner { get; private set; }
    public static Texture2D? Artifact { get; private set; }
    public static Texture2D? RegionCairn { get; private set; }

    public static Texture2D? CardPenguin { get; private set; }
    public static Texture2D? CardSettlement { get; private set; }
    public static Texture2D? CardFaction { get; private set; }
    public static Texture2D? CardArtifact { get; private set; }
    public static Texture2D? CardOrca { get; private set; }
    public static Texture2D? CardRegion { get; private set; }
    public static Texture2D? CardBack { get; private set; }

    public static Texture2D? TitleArt { get; private set; }
    public static Texture2D? TableauBackdrop { get; private set; }
    public static Texture2D? AuroraBand { get; private set; }

    public static Texture2D? CompassRose { get; private set; }
    public static Texture2D? CornerOrnament { get; private set; }
    public static Texture2D? DividerFlourish { get; private set; }
    public static Texture2D? ScaleBar { get; private set; }
    public static Texture2D? TextureParchment { get; private set; }
    public static Texture2D? TextureSlate { get; private set; }

    // Semantic-plane stained-glass panes. Six images, one per hex wedge on
    // the Semantic plane's origin rose-window. Displayed clipped to the
    // triangular wedge in HexMapRenderer.
    public static Texture2D? PaneBreakaway { get; private set; }
    public static Texture2D? PaneYoke { get; private set; }
    public static Texture2D? PaneWitness { get; private set; }
    public static Texture2D? PaneSilence { get; private set; }
    public static Texture2D? PaneLineage { get; private set; }
    public static Texture2D? PaneSeverance { get; private set; }

    public static Texture2D? RoadLightOverlay { get; private set; }
    public static Texture2D? RoadHeavyOverlay { get; private set; }
    public static Texture2D? SemanticCellOverlay { get; private set; }

    private static readonly Dictionary<string, Texture2D?> _terrain = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Texture2D?> _kindSprites = [];
    private static readonly Dictionary<string, Texture2D?> _cardFaces = [];
    private static readonly Dictionary<string, Texture2D?> _mapIcons = [];
    private static readonly Dictionary<string, Texture2D?> _subtypeIcons = [];
    private static readonly Dictionary<string, Texture2D?> _attributeIcons = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Texture2D?> _eventIcons = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether Load() has completed successfully. Accessors return null until then.</summary>
    public static bool Loaded { get; private set; }

    /// <summary>Load all illustration assets into memory. Idempotent.</summary>
    public static void Load()
    {
        if (Loaded) return;

        LoadEntitySprites();
        LoadMapIconSprites();
        LoadSubtypeIconSprites();
        LoadInlineIcons();
        LoadCardFaces();
        LoadBackgroundsAndChrome();
        LoadTerrainTextures();
        LoadMapOverlays();

        Loaded = true;
        GD.Print($"[AssetLibrary] Loaded {_byPath.Count} illustration textures");
    }

    private static void LoadEntitySprites()
    {
        Penguin = LoadTexture("sprites/penguin.png");
        Settlement = LoadTexture("sprites/settlement.png");
        Orca = LoadTexture("sprites/orca.png");
        FactionBanner = LoadTexture("sprites/faction.png");
        Artifact = LoadTexture("sprites/artifact.png");
        RegionCairn = LoadTexture("sprites/region.png");

        RegisterKindSpriteFaces();
    }

    private static void RegisterKindSpriteFaces()
    {
        _kindSprites["penguin"] = Penguin;
        _kindSprites["settlement"] = Settlement;
        _kindSprites["orca"] = Orca;
        _kindSprites["faction"] = FactionBanner;
        _kindSprites["artifact"] = Artifact;
        _kindSprites["region"] = RegionCairn;
    }

    private static void LoadMapIconSprites()
    {
        _mapIcons["penguin"] = LoadTexture("map-icons/penguin@128.png");
        _mapIcons["settlement"] = LoadTexture("map-icons/settlement@128.png");
        _mapIcons["orca"] = LoadTexture("map-icons/orca@128.png");
        _mapIcons["faction"] = LoadTexture("map-icons/faction@128.png");
        _mapIcons["artifact"] = LoadTexture("map-icons/artifact@128.png");
        _mapIcons["region"] = LoadTexture("map-icons/region@128.png");
    }

    private static void LoadSubtypeIconSprites()
    {
        LoadSubtypeIcons("penguin", "warden", "broker", "witness", "exile");
        LoadSubtypeIcons("orca", "shamen", "war-leader", "cripple");
        LoadSubtypeIcons("settlement", "rookery", "colony", "holdfast", "sanctuary", "harbor", "reefhold");
        LoadSubtypeIcons("faction", "order", "compact", "cell", "choir");
        LoadSubtypeIcons("artifact", "relic", "instrument", "weapon", "tome", "seal");
        LoadSubtypeIcons("region", "face", "shelf", "depths", "reach", "sea", "scar");
    }

    private static void LoadInlineIcons()
    {
        foreach (var attr in new[] { "dominion", "echo", "food", "holding", "population", "power", "provisions", "warmth", "wealth" })
        {
            _attributeIcons[attr] = LoadTexture($"icons/attr-{attr}@32.png");
        }

        foreach (var ev in new[] { "card", "creation", "lifecycle", "notice", "threshold" })
        {
            _eventIcons[ev] = LoadTexture($"icons/{ev}@32.png");
        }
    }

    private static void LoadCardFaces()
    {
        CardPenguin = LoadTexture("cards/card-penguin.png");
        CardSettlement = LoadTexture("cards/card-settlement.png");
        CardFaction = LoadTexture("cards/card-faction.png");
        CardArtifact = LoadTexture("cards/card-artifact.png");
        CardOrca = LoadTexture("cards/card-orca.png");
        CardRegion = LoadTexture("cards/card-region.png");
        CardBack = LoadTexture("cards/card-back.png");

        _cardFaces["penguin"] = CardPenguin;
        _cardFaces["settlement"] = CardSettlement;
        _cardFaces["faction"] = CardFaction;
        _cardFaces["artifact"] = CardArtifact;
        _cardFaces["orca"] = CardOrca;
        _cardFaces["region"] = CardRegion;
    }

    private static void LoadBackgroundsAndChrome()
    {
        TitleArt = LoadTexture("backgrounds/title.png");
        TableauBackdrop = LoadTexture("backgrounds/tableau.png");
        AuroraBand = LoadTexture("backgrounds/aurora.png");

        PaneBreakaway = LoadTexture("semantic-plane/breakaway.png");
        PaneYoke = LoadTexture("semantic-plane/yoke.png");
        PaneWitness = LoadTexture("semantic-plane/witness.png");
        PaneSilence = LoadTexture("semantic-plane/silence.png");
        PaneLineage = LoadTexture("semantic-plane/lineage.png");
        PaneSeverance = LoadTexture("semantic-plane/severance.png");

        CompassRose = LoadTexture("chrome/compass-rose.png");
        CornerOrnament = LoadTexture("chrome/corner-ornament.png");
        DividerFlourish = LoadTexture("chrome/divider-flourish.png");
        ScaleBar = LoadTexture("chrome/scale-bar.png");
        TextureParchment = LoadTexture("chrome/texture-parchment.png");
        TextureSlate = LoadTexture("chrome/texture-slate.png");
    }

    private static void LoadTerrainTextures()
    {
        _terrain["Wastes"] = LoadTexture("terrain/wastes.png");
        _terrain["Crags"] = LoadTexture("terrain/crags.png");
        _terrain["Tundra"] = LoadTexture("terrain/tundra.png");
        _terrain["Ice"] = LoadTexture("terrain/ice.png");
        _terrain["HotSprings"] = LoadTexture("terrain/hotsprings.png");
        _terrain["Shelf"] = LoadTexture("terrain/shelf.png");
        _terrain["Reef"] = LoadTexture("terrain/reef.png");
        _terrain["Ocean"] = LoadTexture("terrain/ocean.png");
    }

    private static void LoadMapOverlays()
    {
        RoadLightOverlay = LoadTexture("map-overlays/road-light@128.png");
        RoadHeavyOverlay = LoadTexture("map-overlays/road-heavy@128.png");
        SemanticCellOverlay = LoadTexture("map-overlays/semantic-cell-overlay@128.png");
    }

    /// <summary>Get a per-kind sprite texture, or null if not available.</summary>
    public static Texture2D? KindSprite(string kindId) =>
        _kindSprites.TryGetValue(kindId, out var tex) ? tex : null;

    /// <summary>
    /// Get a per-kind map-icon texture (flat heraldic stamp for the hex map),
    /// or null if not available. Callers on the map should prefer this over
    /// <see cref="KindSprite"/> — the portrait-register sprites are for card
    /// faces and entity inspectors, not for small map markers.
    /// </summary>
    public static Texture2D? KindMapIcon(string kindId) =>
        _mapIcons.TryGetValue(kindId, out var tex) ? tex : null;

    /// <summary>
    /// Get a subtype-specific map icon (e.g. penguin-warden, settlement-holdfast),
    /// falling back to the generic <see cref="KindMapIcon"/> when no subtype
    /// asset is loaded. Call sites on the hex map and entity browser should
    /// prefer this over <see cref="KindMapIcon"/> so icons discriminate by
    /// role where the domain distinguishes.
    /// </summary>
    public static Texture2D? SubtypeMapIcon(string kindId, string? subtype)
    {
        if (!string.IsNullOrEmpty(subtype))
        {
            var key = $"{kindId}-{subtype}";
            if (_subtypeIcons.TryGetValue(key, out var tex) && tex is not null)
                return tex;
        }
        return KindMapIcon(kindId);
    }

    /// <summary>Get a per-kind card-face texture, or null if not available.</summary>
    public static Texture2D? CardFace(string kindId) =>
        _cardFaces.TryGetValue(kindId, out var tex) ? tex : null;

    /// <summary>Get a terrain texture, or null if not available.</summary>
    public static Texture2D? Terrain(string stableName)
    {
        ArgumentNullException.ThrowIfNull(stableName);
        var key = TerrainKey.Canonicalize(stableName);
        return _terrain.TryGetValue(key, out var tex) ? tex : null;
    }

    /// <summary>Get a per-attribute glyph icon (dominion, echo, food, etc.).</summary>
    public static Texture2D? AttributeIcon(string attributeName)
    {
        ArgumentNullException.ThrowIfNull(attributeName);
        return _attributeIcons.TryGetValue(attributeName, out var tex) ? tex : null;
    }

    /// <summary>Get an event-type icon (creation, lifecycle, threshold, ...).</summary>
    public static Texture2D? EventIcon(string eventName)
    {
        ArgumentNullException.ThrowIfNull(eventName);
        return _eventIcons.TryGetValue(eventName, out var tex) ? tex : null;
    }

    private static void LoadSubtypeIcons(string kindId, params string[] subtypes)
    {
        foreach (var subtype in subtypes)
        {
            var key = $"{kindId}-{subtype}";
            _subtypeIcons[key] = LoadTexture($"map-icons/{key}@128.png");
        }
    }

    private static Texture2D? LoadTexture(string relativePath)
    {
        var resPath = $"{ProcessedRoot}/{relativePath}";
        if (_byPath.TryGetValue(resPath, out var cached)) return cached;

        if (!Godot.FileAccess.FileExists(resPath))
        {
            GD.PushWarning($"[AssetLibrary] Missing asset: {resPath}");
            return null;
        }

        using var file = Godot.FileAccess.Open(resPath, Godot.FileAccess.ModeFlags.Read);
        if (file is null)
        {
            GD.PushWarning($"[AssetLibrary] Cannot open asset: {resPath}");
            return null;
        }

        var bytes = file.GetBuffer((long)file.GetLength());
        var img = new Image();
        var err = img.LoadPngFromBuffer(bytes);
        if (err != Error.Ok)
        {
            GD.PushWarning($"[AssetLibrary] Failed to decode PNG ({err}): {resPath}");
            return null;
        }

        var texture = ImageTexture.CreateFromImage(img);
        _byPath[resPath] = texture;
        return texture;
    }
}
