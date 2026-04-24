#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA1305 // UI display strings
using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Graph;
using Engine.Kinds;
using Engine.Simulation;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Left sidebar listing all entities. Click an entity to open its floating card.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class EntityBrowser : PanelContainer
{
    private SimulationRunner? _sim;
    private VBoxContainer? _listContainer;
    private readonly Dictionary<EntityId, Button> _entityButtons = [];

    /// <summary>Emitted when the user clicks an entity in the browser.</summary>
    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    public void Initialize(SimulationRunner sim)
    {
        _sim = sim;
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

    private static string? FormatLastActionSubtitle(Entity entity, int currentTick)
    {
        if (entity.LastActionName is null || entity.LastActionTick is null)
        {
            return null;
        }

        var age = System.Math.Max(0, currentTick - entity.LastActionTick.Value);
        var ageText = age == 0 ? "now" : $"{age}t ago";
        return $"    {entity.LastActionName} · {ageText}";
    }

    /// <summary>Rebuild the entity list from current simulation state.</summary>
    public void Refresh()
    {
        if (_sim is null || _listContainer is null) return;

        // Clear existing buttons
        foreach (var child in _listContainer.GetChildren())
        {
            child.QueueFree();
        }

        _entityButtons.Clear();

        // Add entities grouped by kind
        var firstKind = true;
        foreach (var kind in _sim.DomainConfig.Kinds.Keys.OrderBy(kind => kind.Value, System.StringComparer.Ordinal))
        {
            var entities = _sim.Graph.GetEntitiesByKind(kind);
            if (entities.Count == 0) continue;

            var accent = UiTokens.KindAccent(kind.Value);

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
                Text = kind.ToString().ToUpperInvariant(),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            kindLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
            kindLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
            kindLabel.AddThemeColorOverride("font_color", accent);
            kindRow.AddChild(kindLabel);

            var countLabel = new Label
            {
                Text = entities.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            countLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
            countLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
            countLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);
            kindRow.AddChild(countLabel);

            foreach (var entityId in entities)
            {
                var entity = _sim.Graph.GetEntity(entityId);
                if (entity is null) continue;

                var isDead = entity.Status != EntityStatus.Active;

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
                var spriteTex = AssetLibrary.SubtypeMapIcon(kind.Value, entity.Subtype)
                    ?? AssetLibrary.KindSprite(kind.Value);
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
                        KindId = kind.Value,
                        Accent = isDead ? UiTokens.Whisper : accent,
                        CustomMinimumSize = new Vector2(18, 20),
                    };
                }
                entityRow.AddChild(glyph);

                var btn = new Button
                {
                    Text = isDead ? $" {entity.Name}  †" : $" {entity.Name}",
                    Alignment = HorizontalAlignment.Left,
                    Flat = true,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                btn.AddThemeFontOverride("font", UiTheme.BodyFont);
                btn.AddThemeFontSizeOverride("font_size", UiTokens.SizeBody);
                btn.AddThemeColorOverride("font_color", isDead ? UiTokens.Whisper : UiTokens.Vellum);
                btn.AddThemeColorOverride("font_hover_color", UiTokens.Vellum);

                var capturedId = entityId; // Capture for lambda
                btn.Pressed += () => EmitSignal(SignalName.EntitySelected, capturedId.Value);
                entityRow.AddChild(btn);
                _entityButtons[entityId] = btn;

                var subtitleText = FormatLastActionSubtitle(entity, _sim.Clock.CurrentTick);
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
}
