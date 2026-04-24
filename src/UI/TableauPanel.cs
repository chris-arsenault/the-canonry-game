#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA1305 // UI display strings
using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Generators;
using Engine.Kinds;
using Engine.Messages;
using Engine.Simulation;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Player-facing list of generator cards currently available on the tableau.
/// Toggling a checkbox removes or re-adds the corresponding zero-cost generator card.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class TableauPanel : PanelContainer
{
    private static readonly Color ActivePip   = UiTokens.Aurora;
    private static readonly Color InactivePip = UiTokens.Charcoal;

    private SimulationRunner? _sim;
    private Label? _summaryLabel;
    private Label? _recentLabel;
    private TextureRect? _detailArt;
    private Label? _detailPlaceholder;
    private Label? _detailNameLabel;
    private Label? _detailTypeLabel;
    private Label? _detailDescLabel;
    private VBoxContainer? _detailSections;
    private Control? _detailContent;
    private GridContainer? _rows;
    private readonly Queue<string> _recentFires = [];
    private readonly Dictionary<string, GeneratorRowRefs> _rowRefs = [];
    private string? _lastFiredGeneratorId;
    private string? _hoveredGeneratorId;

    public void Initialize(SimulationRunner sim)
    {
        _sim = sim;
        _recentFires.Clear();
        _lastFiredGeneratorId = null;
        _sim.MessageBus.Subscribe(OnMessageReceived);
        if (_rows is not null)
        {
            RebuildRows();
        }
    }

    public void Refresh()
    {
        if (_sim is null || _rows is null || _summaryLabel is null || _recentLabel is null)
        {
            return;
        }

        var activeCount = _sim.Tableau.ActiveCards.Count(card => !string.IsNullOrWhiteSpace(card.Definition.GeneratorId));
        var totalCount = _sim.DefinitionGraph.Generators.Count;
        var lastFiredText = _lastFiredGeneratorId is null
            ? "none"
            : $"{_sim.DefinitionGraph.Generators[_lastFiredGeneratorId].Name} @ {_sim.WorldState.Generators.ById[_lastFiredGeneratorId].LastFiredTick}";
        _summaryLabel.Text = $"Active {activeCount}/{totalCount}  |  Fired {_sim.GeneratorSystem.TotalFired}  |  Last {lastFiredText}";
        _recentLabel.Text = _recentFires.Count == 0
            ? "Recent: none"
            : "Recent:\n" + string.Join('\n', _recentFires);

        foreach (var (generatorId, generator) in _sim.DefinitionGraph.Generators.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!_rowRefs.TryGetValue(generatorId, out var row))
            {
                continue;
            }

            var isActive = HasActiveGeneratorCard(generatorId);
            var instance = _sim.WorldState.Generators.ById[generatorId];
            var isLastFired = string.Equals(generatorId, _lastFiredGeneratorId, StringComparison.Ordinal);
            var accent = UiTokens.KindAccent(generator.Target.Kind.Value);
            var chipTone = GetChipTone(instance, isLastFired);
            row.StatusLabel.Text = BuildStatusText(generatorId, instance);
            row.NameLabel.AddThemeColorOverride("font_color", isActive ? UiTokens.Vellum : UiTokens.Parchment);
            row.ActivePip.Color = isActive ? ActivePip : InactivePip;
            row.CardPanel.AddThemeStyleboxOverride("panel", UiTheme.CardStyle(accent, selected: isActive, highlighted: isLastFired));
            row.HeaderPanel.AddThemeStyleboxOverride("panel", UiTheme.HeaderStripStyle(accent));
            row.KindBadgePanel.AddThemeStyleboxOverride("panel", UiTheme.BadgeStyle(accent));
            row.StatusPanel.AddThemeStyleboxOverride("panel", UiTheme.ChipStyle(chipTone));
            row.TypeLineLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);
            row.KindBadgeLabel.AddThemeColorOverride("font_color", accent);
            row.StatusLabel.AddThemeColorOverride("font_color", UiTheme.ChipTextColor(chipTone));
        }
    }

    public override void _Ready()
    {
        // Floor on the panel's min size — 4 cards @ 232 wide + gutters
        // + 16px padding margins + ~24 stylebox = ~1000. Height fits one
        // full row of 340-tall cards plus title/detail/footer bands.
        // PositionFloatingHud in Main.cs bumps this up at runtime.
        CustomMinimumSize = new Vector2(1000, 640);

        // Stylebox with 0 content margins so the backdrop TextureRect can
        // reach the panel's visible edge. Visual padding is restored below
        // via a MarginContainer around the content VBox.
        var panelStyle = UiTheme.FloatingPanelStyle();
        panelStyle.ContentMarginTop = 0;
        panelStyle.ContentMarginBottom = 0;
        panelStyle.ContentMarginLeft = 0;
        panelStyle.ContentMarginRight = 0;
        AddThemeStyleboxOverride("panel", panelStyle);

        // Stop mouse/scroll events so they don't leak through to the map
        // underneath when the cursor is over the panel (including empty
        // gaps between cards).
        MouseFilter = MouseFilterEnum.Stop;
        // Clip any child bleed (backdrop TextureRect in particular) so the
        // panel's visible edge IS its edge, regardless of ambient size.
        ClipContents = true;

        // Layered root: backdrop illustration behind the cards, VBox content
        // on top. With 0 stylebox margins, layers fills the full panel rect.
        var layers = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(layers);

        if (AssetLibrary.TableauBackdrop is { } backdrop)
        {
            var bg = new TextureRect
            {
                Texture = backdrop,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                Modulate = new Color(1f, 1f, 1f, 0.18f),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            layers.AddChild(bg);
        }

        // Margins push the content VBox inward while the backdrop behind
        // continues edge-to-edge.
        var padding = new MarginContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        padding.AddThemeConstantOverride("margin_top", UiTokens.Space4);
        padding.AddThemeConstantOverride("margin_bottom", UiTokens.Space4);
        padding.AddThemeConstantOverride("margin_left", UiTokens.Space4);
        padding.AddThemeConstantOverride("margin_right", UiTokens.Space4);
        padding.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        layers.AddChild(padding);

        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", UiTokens.Space3);
        padding.AddChild(outer);

        var title = new Label
        {
            Text = "Tableau",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontOverride("font", UiTheme.DisplayFont);
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleLarge);
        title.AddThemeColorOverride("font_color", UiTokens.Vellum);
        outer.AddChild(title);

        var subtitle = new Label
        {
            Text = "CARDS THAT DEFINE THE WORLD'S NATURE",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        subtitle.AddThemeFontOverride("font", UiTheme.BodyFont);
        subtitle.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        subtitle.AddThemeColorOverride("font_color", UiTokens.Whisper);
        outer.AddChild(subtitle);

        outer.AddChild(BuildHairline());

        _summaryLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _summaryLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
        _summaryLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _summaryLabel.AddThemeColorOverride("font_color", UiTokens.Parchment);
        outer.AddChild(_summaryLabel);

        outer.AddChild(BuildHairline());

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        outer.AddChild(scroll);

        _rows = new GridContainer
        {
            Columns = 4,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _rows.AddThemeConstantOverride("h_separation", UiTokens.Space2);
        _rows.AddThemeConstantOverride("v_separation", UiTokens.Space2);
        scroll.AddChild(_rows);

        outer.AddChild(BuildHairline());

        // Detail blowup — when hovering a card, show an enlarged version:
        // big card-face image on the left + full details on the right.
        // Hidden placeholder text when nothing is hovered.
        var detailPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 300),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        detailPanel.AddThemeStyleboxOverride("panel", UiTheme.InsetStyle());
        outer.AddChild(detailPanel);

        // Swap-view root: shows either a placeholder or the content hbox.
        _detailContent = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        detailPanel.AddChild(_detailContent);

        _detailPlaceholder = new Label
        {
            Text = "HOVER A CARD FOR DETAIL",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _detailPlaceholder.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _detailPlaceholder.AddThemeFontOverride("font", UiTheme.BodyFont);
        _detailPlaceholder.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _detailPlaceholder.AddThemeColorOverride("font_color", UiTokens.Whisper);
        _detailContent.AddChild(_detailPlaceholder);

        var detailRow = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        detailRow.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        detailRow.AddThemeConstantOverride("separation", UiTokens.Space3);
        _detailContent.AddChild(detailRow);

        // Left: enlarged card-face art (2:3 aspect, like the card source).
        var artFrame = new Control
        {
            CustomMinimumSize = new Vector2(200, 280),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            MouseFilter = MouseFilterEnum.Ignore,
            ClipContents = true,
        };
        detailRow.AddChild(artFrame);

        _detailArt = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _detailArt.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        artFrame.AddChild(_detailArt);

        // Right: full textual breakdown — name, type line, description,
        // then the mono preconditions/outputs/tracking block.
        var textColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        textColumn.AddThemeConstantOverride("separation", UiTokens.Space1);
        detailRow.AddChild(textColumn);

        _detailNameLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _detailNameLabel.AddThemeFontOverride("font", UiTheme.DisplayFont);
        _detailNameLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        _detailNameLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);
        textColumn.AddChild(_detailNameLabel);

        _detailTypeLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _detailTypeLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _detailTypeLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _detailTypeLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);
        textColumn.AddChild(_detailTypeLabel);

        _detailDescLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _detailDescLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _detailDescLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _detailDescLabel.AddThemeColorOverride("font_color", UiTokens.Parchment);
        textColumn.AddChild(_detailDescLabel);

        // Iconographic sections — cleared and rebuilt per hovered card.
        _detailSections = new VBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _detailSections.AddThemeConstantOverride("separation", UiTokens.Space2);
        textColumn.AddChild(_detailSections);

        outer.AddChild(BuildHairline());
        _recentLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _recentLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
        _recentLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        _recentLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);
        outer.AddChild(_recentLabel);

        if (_sim is not null)
        {
            RebuildRows();
        }
    }

    private static ColorRect BuildHairline()
    {
        return new ColorRect
        {
            Color = UiTokens.Hairline,
            CustomMinimumSize = new Vector2(0, 1),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
    }

    private void RebuildRows()
    {
        if (_sim is null || _rows is null)
        {
            return;
        }

        foreach (var child in _rows.GetChildren())
        {
            child.QueueFree();
        }

        _rowRefs.Clear();

        foreach (var entry in _sim.DefinitionGraph.Generators.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var generatorId = entry.Key;
            var generator = entry.Value;
            var accent = UiTokens.KindAccent(generator.Target.Kind.Value);

            // Card: portrait MTG-style at ~30% larger than the earlier pass,
            // with fixed internal heights so every card is the same size
            // regardless of description length. MouseFilter Stop lets the
            // PanelContainer receive GuiInput; children Ignore so clicks
            // land here. MouseEntered/Exited drive the detail footer.
            var card = new PanelContainer
            {
                CustomMinimumSize = new Vector2(232, 340),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Stop,
                ClipContents = true,
            };
            card.AddThemeStyleboxOverride("panel", UiTheme.CardStyle(accent, selected: HasActiveGeneratorCard(generatorId)));
            var localGeneratorId = generatorId;
            card.GuiInput += evt =>
            {
                if (evt is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                {
                    OnGeneratorToggled(localGeneratorId, !HasActiveGeneratorCard(localGeneratorId));
                }
            };
            card.MouseEntered += () => ShowCardDetail(localGeneratorId);
            card.MouseExited += () => ClearCardDetailIf(localGeneratorId);

            // Root VBox stacks the MTG bands top-down. No FullRect layered
            // Control — straightforward sized children, so the panel's
            // sizing is predictable and nothing leaks past its bounds.
            var stack = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            stack.AddThemeConstantOverride("separation", 0);
            card.AddChild(stack);

            // 1. Name banner — accent-colored strip, top band.
            var headerPanel = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            headerPanel.AddThemeStyleboxOverride("panel", UiTheme.HeaderStripStyle(accent));
            stack.AddChild(headerPanel);

            var headerRow = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            headerRow.AddThemeConstantOverride("separation", UiTokens.Space1);
            headerPanel.AddChild(headerRow);

            var activePip = new ColorRect
            {
                CustomMinimumSize = new Vector2(8, 8),
                Color = HasActiveGeneratorCard(generatorId) ? ActivePip : InactivePip,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            headerRow.AddChild(activePip);

            var nameLabel = new Label
            {
                Text = generator.Name,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipText = true,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            nameLabel.AddThemeFontOverride("font", UiTheme.DisplayFont);
            nameLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleSmall);
            nameLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);
            headerRow.AddChild(nameLabel);

            // 2. Card-face art — full-alpha, wide, prominent. Taller frame
            //    than the earlier pass so less of the 1024×1536 source is
            //    cropped away (was showing ~37% of source height, now ~60%).
            var artFrame = new Control
            {
                CustomMinimumSize = new Vector2(0, 200),
                MouseFilter = MouseFilterEnum.Ignore,
                ClipContents = true,
            };
            stack.AddChild(artFrame);

            var cardFace = AssetLibrary.CardFace(generator.Target.Kind.Value);
            if (cardFace is not null)
            {
                var art = new TextureRect
                {
                    Texture = cardFace,
                    // IgnoreSize: without this, TextureRect's default
                    // ExpandMode = KeepSize makes its minimum = the
                    // texture's native size (1776×2368). The anchored
                    // FullRect can't shrink it, so the rect ends up huge
                    // and ClipContents shows only the top-left corner.
                    // IgnoreSize lets the anchors actually size the rect.
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                art.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                artFrame.AddChild(art);
            }

            // 3. Type line + kind badge — below the art, before the rules.
            var typeRow = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            typeRow.AddThemeConstantOverride("separation", UiTokens.Space1);
            stack.AddChild(typeRow);

            var typeLine = new Label
            {
                Text = BuildTypeLine(generator),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipText = true,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            typeLine.AddThemeFontOverride("font", UiTheme.BodyFont);
            typeLine.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
            typeLine.AddThemeColorOverride("font_color", UiTokens.Whisper);
            typeRow.AddChild(typeLine);

            var kindBadgePanel = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            kindBadgePanel.AddThemeStyleboxOverride("panel", UiTheme.BadgeStyle(accent));
            typeRow.AddChild(kindBadgePanel);

            var kindBadgeLabel = new Label
            {
                Text = generator.Target.Kind.Value.ToUpperInvariant(),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            kindBadgeLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
            kindBadgeLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
            kindBadgeLabel.AddThemeColorOverride("font_color", accent);
            kindBadgePanel.AddChild(kindBadgeLabel);

            // 4. Rules text box — fixed height so every card is the same
            //    total height regardless of description length.
            var rulesPanel = new PanelContainer
            {
                CustomMinimumSize = new Vector2(0, 56),
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
                ClipContents = true,
            };
            rulesPanel.AddThemeStyleboxOverride("panel", UiTheme.InsetStyle());
            stack.AddChild(rulesPanel);

            var descriptionLabel = CreateBodyLabel(fontSize: UiTokens.SizeCaption);
            descriptionLabel.Text = string.IsNullOrWhiteSpace(generator.Description)
                ? generator.Id
                : generator.Description!;
            descriptionLabel.AddThemeColorOverride("font_color", UiTokens.Parchment);
            descriptionLabel.MouseFilter = MouseFilterEnum.Ignore;
            rulesPanel.AddChild(descriptionLabel);

            // 5. Status chip — bottom-right, like MTG's power/toughness.
            var statusRow = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            stack.AddChild(statusRow);

            var statusSpacer = new Control
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            statusRow.AddChild(statusSpacer);

            var initialInstance = _sim.WorldState.Generators.ById[generatorId];
            var initialTone = GetChipTone(initialInstance, isLastFired: false);
            var statusPanel = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            statusPanel.AddThemeStyleboxOverride("panel", UiTheme.ChipStyle(initialTone));
            statusRow.AddChild(statusPanel);

            var statusLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            statusLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
            statusLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
            statusLabel.AddThemeColorOverride("font_color", UiTheme.ChipTextColor(initialTone));
            statusPanel.AddChild(statusLabel);

            // Detailed preconditions/outputs/tracking live in the tooltip
            // and in the bottom detail bar (updated on hover). Unused
            // labels remain in GeneratorRowRefs for now.
            var preconditionsLabel = CreateBodyLabel();
            var outputsLabel = CreateBodyLabel();
            var statsLabel = CreateBodyLabel();

            var tooltip = BuildTooltip(generator);
            card.TooltipText = tooltip;

            _rows.AddChild(card);
            _rowRefs[generatorId] = new GeneratorRowRefs(
                card,
                headerPanel,
                kindBadgePanel,
                statusPanel,
                nameLabel,
                typeLine,
                kindBadgeLabel,
                statusLabel,
                preconditionsLabel,
                outputsLabel,
                statsLabel,
                activePip);
        }

        Refresh();
    }

    private void OnGeneratorToggled(string generatorId, bool enabled)
    {
        if (_sim is null)
        {
            return;
        }

        if (enabled)
        {
            if (HasActiveGeneratorCard(generatorId))
            {
                return;
            }

            if (_sim.SpawnCards.TryGetValue(generatorId, out var cardDefinition))
            {
                _sim.Tableau.PlaceCard(cardDefinition, _sim.Clock.CurrentTick);
            }

            Refresh();
            return;
        }

        var activeCard = _sim.Tableau.ActiveCards.FirstOrDefault(card => card.Definition.GeneratorId == generatorId);
        if (activeCard is not null)
        {
            _sim.Tableau.RemoveCard(activeCard);
        }

        Refresh();
    }

    private bool HasActiveGeneratorCard(string generatorId) =>
        _sim is not null && _sim.Tableau.ActiveCards.Any(card => card.Definition.GeneratorId == generatorId);

    private void ShowCardDetail(string generatorId)
    {
        if (_sim is null || _detailArt is null || _detailNameLabel is null ||
            _detailTypeLabel is null || _detailDescLabel is null ||
            _detailSections is null || _detailPlaceholder is null ||
            _detailContent is null)
        {
            return;
        }

        if (!_sim.DefinitionGraph.Generators.TryGetValue(generatorId, out var generator))
        {
            return;
        }

        _hoveredGeneratorId = generatorId;
        var instance = _sim.WorldState.Generators.ById[generatorId];
        _detailArt.Texture = AssetLibrary.CardFace(generator.Target.Kind.Value);
        _detailNameLabel.Text = generator.Name;
        _detailTypeLabel.Text = BuildTypeLine(generator);
        _detailDescLabel.Text = string.IsNullOrWhiteSpace(generator.Description)
            ? generator.Id
            : generator.Description!;

        RebuildDetailSections(generator, instance);

        _detailPlaceholder.Visible = false;
        if (_detailContent.GetChildCount() >= 2 && _detailContent.GetChild(1) is Control row)
        {
            row.Visible = true;
        }
    }

    private void ClearCardDetailIf(string generatorId)
    {
        if (_detailPlaceholder is null || _detailContent is null) return;
        if (!string.Equals(_hoveredGeneratorId, generatorId, StringComparison.Ordinal)) return;
        _hoveredGeneratorId = null;
        _detailPlaceholder.Visible = true;
        if (_detailContent.GetChildCount() >= 2 && _detailContent.GetChild(1) is Control row)
        {
            row.Visible = false;
        }
    }

    private void RebuildDetailSections(DefinitionGeneratorNode generator, GeneratorInstance instance)
    {
        if (_detailSections is null) return;

        foreach (var child in _detailSections.GetChildren())
        {
            child.QueueFree();
        }

        // WHEN — what makes the card fire. Attribute-name + direction glyph,
        // human-readable. No "any" fallback row; if there's no trigger just
        // omit the section.
        if (generator.Triggers.Count > 0)
        {
            var triggerRows = generator.Triggers
                .Where(t => t.AttributeId is not null)
                .Select<DefinitionGeneratorTrigger, (Texture2D?, string)>(trigger =>
                {
                    var attrName = trigger.AttributeId!.Value.Value;
                    var icon = AssetLibrary.AttributeIcon(attrName) ?? AssetLibrary.EventIcon("notice");
                    var arrow = trigger.Direction switch
                    {
                        TriggerDirection.High => "↑",
                        TriggerDirection.Low => "↓",
                        TriggerDirection.Critical => "⚠",
                        _ => string.Empty,
                    };
                    return (icon, $"{Capitalize(attrName)} {arrow}".TrimEnd());
                })
                .ToArray();
            if (triggerRows.Length > 0)
            {
                AddDetailSection("WHEN", triggerRows);
            }
        }

        // CREATES — what the card makes. Just "Kind Subtype" with an
        // optional chance suffix. No refs, no resolution keywords.
        if (generator.Produces.Count > 0)
        {
            var rows = generator.Produces.Select<DefinitionProducedEntity, (Texture2D?, string)>(produced =>
            {
                var subtypeDisplay = !string.IsNullOrWhiteSpace(produced.Subtype)
                    ? Capitalize(produced.Subtype)
                    : produced.Subtypes is { Count: > 0 }
                        ? string.Join(" or ", produced.Subtypes.OrderBy(v => v, StringComparer.Ordinal).Select(Capitalize))
                        : string.Empty;
                var icon = AssetLibrary.SubtypeMapIcon(produced.Kind.Value, produced.Subtype)
                    ?? AssetLibrary.KindMapIcon(produced.Kind.Value);
                var label = string.IsNullOrEmpty(subtypeDisplay)
                    ? Capitalize(produced.Kind.Value)
                    : $"{Capitalize(produced.Kind.Value)} {subtypeDisplay}";
                if (produced.CreateChance < 1f)
                {
                    label += $"  ({Mathf.RoundToInt(produced.CreateChance * 100f)}%)";
                }
                return (icon, label);
            }).ToArray();
            AddDetailSection("CREATES", rows);
        }

        // HISTORY — runtime stats.
        var trackingRows = new List<(Texture2D?, string)>
        {
            (AssetLibrary.EventIcon("creation"), $"{instance.TotalRuns} fires"),
        };
        if (instance.FailedRuns > 0)
        {
            trackingRows.Add((AssetLibrary.EventIcon("threshold"), $"{instance.FailedRuns} blocked"));
        }
        if (instance.LastFiredTick is { } run)
        {
            trackingRows.Add((AssetLibrary.EventIcon("card"), $"Last at tick {run}"));
        }
        AddDetailSection("HISTORY", trackingRows.ToArray());
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (char.IsUpper(value[0])) return value;
        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    private void AddDetailSection(string title, params (Texture2D? Icon, string Text)[] rows)
    {
        if (_detailSections is null) return;

        var section = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        section.AddThemeConstantOverride("separation", 2);
        _detailSections.AddChild(section);

        var header = new Label
        {
            Text = title,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        header.AddThemeFontOverride("font", UiTheme.BodyFont);
        header.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        header.AddThemeColorOverride("font_color", UiTokens.Ember);
        section.AddChild(header);

        foreach (var (icon, text) in rows)
        {
            var row = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", UiTokens.Space1);
            section.AddChild(row);

            var iconFrame = new Control
            {
                CustomMinimumSize = new Vector2(20, 20),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            row.AddChild(iconFrame);

            if (icon is not null)
            {
                var iconRect = new TextureRect
                {
                    Texture = icon,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                iconRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                iconFrame.AddChild(iconRect);
            }

            var label = new Label
            {
                Text = text,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            label.AddThemeFontOverride("font", UiTheme.BodyFont);
            label.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
            label.AddThemeColorOverride("font_color", UiTokens.Vellum);
            row.AddChild(label);
        }
    }

    private void OnMessageReceived(SimulationMessage message)
    {
        if (message is not GeneratorFiredMessage fired || _sim is null)
        {
            return;
        }

        _lastFiredGeneratorId = fired.GeneratorId;
        var targetName = fired.TargetEntityId is { } targetId
            ? _sim.Graph.GetEntity(targetId)?.Name ?? targetId.ToString()
            : "unknown";
        _recentFires.Enqueue($"[{fired.Tick}] {_sim.DefinitionGraph.Generators[fired.GeneratorId].Name} -> {targetName}");
        while (_recentFires.Count > 6)
        {
            _recentFires.Dequeue();
        }
    }

    private static string BuildTooltip(DefinitionGeneratorNode generator)
    {
        var triggerSummary = string.Join(
            " or ",
            generator.Triggers.Select(BuildTriggerSummary));
        var description = string.IsNullOrWhiteSpace(generator.Description)
            ? generator.Id
            : generator.Description!;
        return $"{description}\nTargets {generator.Target.Kind} when {triggerSummary}";
    }

    private static string BuildTriggerSummary(DefinitionGeneratorTrigger trigger)
    {
        if (trigger.AttributeId is null || trigger.Direction == TriggerDirection.Any)
        {
            return "its trigger passes";
        }

        var band = trigger.Direction switch
        {
            TriggerDirection.High => "is high",
            TriggerDirection.Low => "is low",
            TriggerDirection.Critical => "is critical",
            _ => "changes",
        };
        return $"{trigger.AttributeId.Value.Value} {band}";
    }

    private static Label CreateBodyLabel(int fontSize = UiTokens.SizeCaption)
    {
        var label = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.AddThemeFontOverride("font", UiTheme.BodyFont);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", UiTokens.Parchment);
        return label;
    }

    private static PanelContainer CreateSectionPanel()
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.InsetStyle());
        return panel;
    }

    private static VBoxContainer WrapSection(string title, Label body)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", UiTokens.Space1);

        var titleLabel = new Label
        {
            Text = title.ToUpperInvariant(),
        };
        titleLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        titleLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        titleLabel.AddThemeColorOverride("font_color", UiTokens.Parchment);
        box.AddChild(titleLabel);
        box.AddChild(body);
        return box;
    }

    private static UiTheme.ChipTone GetChipTone(GeneratorInstance instance, bool isLastFired)
    {
        if (isLastFired) return UiTheme.ChipTone.Warning;        // Ember — rare, draws the eye
        if (instance.LastFiredTick is not null) return UiTheme.ChipTone.Active;
        if (instance.FailedRuns > 0) return UiTheme.ChipTone.Critical;
        return UiTheme.ChipTone.Neutral;
    }

    private static string BuildTypeLine(DefinitionGeneratorNode generator)
    {
        var target = generator.Target.Subtypes is { Count: > 0 }
            ? $"{generator.Target.Kind} [{string.Join("|", generator.Target.Subtypes.OrderBy(value => value, StringComparer.Ordinal))}]"
            : generator.Target.Kind.ToString();
        return $"GENERATOR  •  TARGET {target.ToUpperInvariant()}";
    }

    private static string BuildPreconditions(DefinitionGeneratorNode generator)
    {
        var targetSubtypes = generator.Target.Subtypes is { Count: > 0 }
            ? $"[{string.Join("|", generator.Target.Subtypes.OrderBy(value => value, StringComparer.Ordinal))}]"
            : string.Empty;
        var target = $"{generator.Target.Kind}{targetSubtypes}";
        var triggers = generator.Triggers.Count == 0
            ? "any"
            : string.Join(", ", generator.Triggers.Select(BuildTriggerSummary));
        var secondaries = generator.SecondaryTargets.Count == 0
            ? "none"
            : string.Join("; ", generator.SecondaryTargets.Select(BuildSecondaryTarget));
        return $"Preconditions: target {target}; trigger {triggers}; secondary {secondaries}";
    }

    private static string BuildOutputs(DefinitionGeneratorNode generator)
    {
        var produces = string.Join(", ", generator.Produces.Select(BuildProducedEntity));
        var relationships = string.Join(
            ", ",
            generator.Relationships.Select(rel => $"{rel.SourceRef} -{rel.RelationshipId.Value}-> {rel.DestinationRef}"));
        return $"Outputs: {produces}" + (string.IsNullOrWhiteSpace(relationships) ? string.Empty : $" | edges {relationships}");
    }

    private static string BuildSecondaryTarget(DefinitionGeneratorBindingTarget target)
    {
        var conditions = target.Query?.AttributeConditions.Count > 0
            ? " where " + string.Join(", ", target.Query.AttributeConditions.Select(BuildAttributeCondition))
            : string.Empty;
        return $"{target.Ref}:{target.Kind}{conditions}";
    }

    private static string BuildProducedEntity(DefinitionProducedEntity produced)
    {
        var subtype = !string.IsNullOrWhiteSpace(produced.Subtype)
            ? produced.Subtype
            : produced.Subtypes is { Count: > 0 }
                ? string.Join("|", produced.Subtypes.OrderBy(value => value, StringComparer.Ordinal))
                : "any";
        var chance = produced.CreateChance < 1f ? $" @{Mathf.RoundToInt(produced.CreateChance * 100f)}%" : string.Empty;
        var resolution = produced.Query is null
            ? "create"
            : produced.CreateIfMissing
                ? "query/create"
                : "query-only";
        return $"{produced.Ref}:{produced.Kind}[{subtype}] {resolution}{chance}";
    }

    private static string BuildAttributeCondition(DefinitionQueryAttributeCondition condition)
    {
        var qualifier = condition.Direction switch
        {
            TriggerDirection.High => "high",
            TriggerDirection.Low => "low",
            TriggerDirection.Critical => "critical",
            _ => "any",
        };
        return $"{condition.AttributeId.Value} {qualifier}";
    }

    private static string BuildStatusText(string generatorId, GeneratorInstance instance)
    {
        if (instance.TotalRuns == 0 && instance.FailedRuns == 0)
        {
            return "Dormant";
        }

        if (instance.LastFiredTick is { } lastRun)
        {
            return $"Fired {lastRun}";
        }

        return $"Blocked x{instance.FailedRuns}";
    }

    private static string BuildStatsText(GeneratorInstance instance)
    {
        var lastFailure = instance.LastFailedTick is { } tick ? tick.ToString() : "never";
        return $"Executed {instance.TotalRuns}  |  Failed {instance.FailedRuns}  |  Last run {instance.LastFiredTick?.ToString() ?? "never"}  |  Last fail {lastFailure}";
    }

    private sealed record GeneratorRowRefs(
        PanelContainer CardPanel,
        PanelContainer HeaderPanel,
        PanelContainer KindBadgePanel,
        PanelContainer StatusPanel,
        Label NameLabel,
        Label TypeLineLabel,
        Label KindBadgeLabel,
        Label StatusLabel,
        Label PreconditionsLabel,
        Label OutputsLabel,
        Label StatsLabel,
        ColorRect ActivePip);
}
