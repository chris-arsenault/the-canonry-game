#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA1305 // UI display strings
using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Projection;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Left sidebar listing all entities. Click an entity to open its floating card.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class EntityBrowser : PanelContainer
{
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private VBoxContainer? _listContainer;
    private readonly Dictionary<int, Button> _entityButtons = [];

    /// <summary>Emitted when the user clicks an entity in the browser.</summary>
    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    public void Initialize(UiProjectionState? projection, IProjectionSymbolCatalog? symbols)
    {
        _projection = projection;
        _symbols = symbols;
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(240, 0);
        SizeFlagsVertical = SizeFlags.ExpandFill;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", UiTokens.Space2);
        AddChild(vbox);

        var title = new Label
        {
            Text = "Registry",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontOverride("font", UiTheme.DisplayFont);
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        title.AddThemeColorOverride("font_color", UiTokens.Vellum);
        vbox.AddChild(title);

        var subtitle = new Label
        {
            Text = "ALL KNOWN ENTITIES",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        subtitle.AddThemeFontOverride("font", UiTheme.BodyFont);
        subtitle.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        subtitle.AddThemeColorOverride("font_color", UiTokens.Whisper);
        vbox.AddChild(subtitle);

        vbox.AddChild(new ColorRect
        {
            Color = UiTokens.Hairline,
            CustomMinimumSize = new Vector2(0, 1),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        vbox.AddChild(scroll);

        _listContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _listContainer.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(_listContainer);
    }

    private string? FormatLastActionSubtitle(UiEntityProjection entity, int currentTick)
    {
        var last = entity.LastProcessExecution;
        if (last is null)
        {
            return null;
        }

        var processName = ResolveDisplayName(
            ProjectionSymbolKind.Process,
            last.ProcessId,
            $"Process {last.ProcessId}");
        var age = System.Math.Max(0, currentTick - last.Tick);
        var ageText = age == 0 ? "now" : $"{age}t ago";
        return $"    {processName} · {ageText}";
    }

    /// <summary>Rebuild the entity list from current simulation state.</summary>
    public void Refresh()
    {
        if (_projection is null || _listContainer is null) return;

        // Clear existing buttons
        foreach (var child in _listContainer.GetChildren())
        {
            child.QueueFree();
        }

        _entityButtons.Clear();

        var firstKind = true;
        foreach (var group in _projection.Entities.Values
            .OrderBy(entity => ResolveStableName(ProjectionSymbolKind.Kind, entity.KindId, $"kind-{entity.KindId}"), System.StringComparer.Ordinal)
            .ThenBy(entity => ResolveEntityName(entity), System.StringComparer.Ordinal)
            .GroupBy(entity => entity.KindId))
        {
            var entities = group.ToArray();
            if (entities.Length == 0) continue;

            var kindStableName = ResolveStableName(ProjectionSymbolKind.Kind, group.Key, $"kind-{group.Key}");
            var accent = UiTokens.KindAccent(kindStableName);

            // Gap before each section (except the first)
            if (!firstKind)
            {
                _listContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, UiTokens.Space3) });
            }

            firstKind = false;

            // Kind header — small-caps caption, accent-colored, with trailing count
            var kindRow = new HBoxContainer();
            kindRow.AddThemeConstantOverride("separation", UiTokens.Space2);
            _listContainer.AddChild(kindRow);

            var kindLabel = new Label
            {
                Text = ResolveDisplayName(ProjectionSymbolKind.Kind, group.Key, kindStableName).ToUpperInvariant(),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            kindLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
            kindLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
            kindLabel.AddThemeColorOverride("font_color", accent);
            kindRow.AddChild(kindLabel);

            var countLabel = new Label
            {
                Text = entities.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            countLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
            countLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
            countLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);
            kindRow.AddChild(countLabel);

            foreach (var entity in entities)
            {
                var isDead = entity.StatusCode != UiProjectionStatusCodes.Active;
                var subtypeStableName = ResolveNullableStableName(ProjectionSymbolKind.Subtype, entity.SubtypeId);
                var entityName = ResolveEntityName(entity);

                // Row: [glyph] [name button expand] — the glyph is a tiny sprite so
                // the registry reads as a specimen list rather than a flat names column.
                var entityRow = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                entityRow.AddThemeConstantOverride("separation", UiTokens.Space1);
                _listContainer.AddChild(entityRow);

                // Small sidebar row — map icon preferred (reads at tiny size).
                // Portrait sprite is too noisy at 22px; polygon is the
                // final fallback.
                Control glyph;
                var spriteTex = AssetLibrary.SubtypeMapIcon(kindStableName, subtypeStableName)
                    ?? AssetLibrary.KindSprite(kindStableName);
                if (spriteTex is not null)
                {
                    glyph = new TextureRect
                    {
                        Texture = spriteTex,
                        CustomMinimumSize = new Vector2(22, 24),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                        Modulate = isDead ? UiTokens.WithAlpha(Colors.White, 0.45f) : Colors.White,
                        MouseFilter = Control.MouseFilterEnum.Ignore,
                    };
                }
                else
                {
                    glyph = new KindGlyphRect
                    {
                        KindId = kindStableName,
                        Accent = isDead ? UiTokens.Whisper : accent,
                        CustomMinimumSize = new Vector2(18, 20),
                    };
                }
                entityRow.AddChild(glyph);

                var btn = new Button
                {
                    Text = isDead ? $" {entityName}  †" : $" {entityName}",
                    Alignment = HorizontalAlignment.Left,
                    Flat = true,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                btn.AddThemeFontOverride("font", UiTheme.BodyFont);
                btn.AddThemeFontSizeOverride("font_size", UiTokens.SizeBody);
                btn.AddThemeColorOverride("font_color", isDead ? UiTokens.Whisper : UiTokens.Vellum);
                btn.AddThemeColorOverride("font_hover_color", UiTokens.Vellum);

                var capturedId = entity.EntityId;
                btn.Pressed += () => EmitSignal(SignalName.EntitySelected, capturedId);
                entityRow.AddChild(btn);
                _entityButtons[entity.EntityId] = btn;

                var subtitleText = FormatLastActionSubtitle(entity, _projection.CurrentTick);
                if (subtitleText is not null)
                {
                    var subtitle = new Label
                    {
                        Text = subtitleText,
                    };
                    subtitle.AddThemeFontOverride("font", UiTheme.BodyFont);
                    subtitle.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
                    subtitle.AddThemeColorOverride("font_color", UiTokens.Whisper);
                    _listContainer.AddChild(subtitle);
                }
            }
        }
    }

    private string ResolveEntityName(UiEntityProjection entity) =>
        _symbols?.ResolveEntityName(
            new ProjectionEntityNameKey(
                entity.EntityId,
                entity.KindId,
                entity.SubtypeId,
                entity.CultureId,
                entity.NameSeed),
            $"E{entity.EntityId}") ?? $"E{entity.EntityId}";

    private string ResolveDisplayName(ProjectionSymbolKind kind, int id, string fallback) =>
        _symbols?.ResolveDisplayName(kind, id, fallback) ?? fallback;

    private string ResolveStableName(ProjectionSymbolKind kind, int id, string fallback)
    {
        if (_symbols?.TryResolve(kind, id, out var symbol) == true)
        {
            return symbol.StableName;
        }

        return fallback;
    }

    private string? ResolveNullableStableName(ProjectionSymbolKind kind, int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return _symbols?.TryResolve(kind, id, out var symbol) == true
            ? symbol.StableName
            : null;
    }
}
