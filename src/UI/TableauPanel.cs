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
    private GridContainer? _rows;
    private readonly Queue<string> _recentFires = [];
    private readonly Dictionary<string, CheckBox> _checkBoxes = [];
    private readonly Dictionary<string, GeneratorRowRefs> _rowRefs = [];
    private string? _lastFiredGeneratorId;
    private bool _suppressSignals;

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
            if (!_rowRefs.TryGetValue(generatorId, out var row) || !_checkBoxes.TryGetValue(generatorId, out var checkBox))
            {
                continue;
            }

            var isActive = HasActiveGeneratorCard(generatorId);
            SetCheckBoxState(checkBox, isActive);
            var instance = _sim.WorldState.Generators.ById[generatorId];
            var isLastFired = string.Equals(generatorId, _lastFiredGeneratorId, StringComparison.Ordinal);
            var accent = UiTokens.KindAccent(generator.Target.Kind.Value);
            var chipTone = GetChipTone(instance, isLastFired);
            row.StatusLabel.Text = BuildStatusText(generatorId, instance);
            row.StatsLabel.Text = BuildStatsText(instance);
            row.OutputsLabel.Text = BuildOutputs(generator);
            row.PreconditionsLabel.Text = BuildPreconditions(generator);
            row.NameLabel.AddThemeColorOverride("font_color", isActive ? UiTokens.Vellum : UiTokens.Parchment);
            row.ActivePip.Color = isActive ? ActivePip : InactivePip;
            row.CardPanel.AddThemeStyleboxOverride("panel", UiTheme.CardStyle(accent, selected: isActive, highlighted: isLastFired));
            row.HeaderPanel.AddThemeStyleboxOverride("panel", UiTheme.HeaderStripStyle(accent));
            row.KindBadgePanel.AddThemeStyleboxOverride("panel", UiTheme.BadgeStyle(accent));
            row.StatusPanel.AddThemeStyleboxOverride("panel", UiTheme.ChipStyle(chipTone));
            row.TypeLineLabel.AddThemeColorOverride("font_color", UiTokens.Parchment);
            row.KindBadgeLabel.AddThemeColorOverride("font_color", accent);
            row.StatusLabel.AddThemeColorOverride("font_color", UiTheme.ChipTextColor(chipTone));
        }
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(680, 440);
        AddThemeStyleboxOverride("panel", UiTheme.FloatingPanelStyle());

        // Layered root: backdrop illustration behind the cards, VBox content
        // on top. PanelContainer manages one child, so the Control here holds
        // both via FullRect anchors.
        var layers = new Control
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(layers);

        if (AssetLibrary.TableauBackdrop is { } backdrop)
        {
            var bg = new TextureRect
            {
                Texture = backdrop,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                Modulate = new Color(1f, 1f, 1f, 0.12f),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            bg.SetAnchorsPreset(LayoutPreset.FullRect);
            layers.AddChild(bg);
        }

        var outer = new VBoxContainer();
        outer.SetAnchorsPreset(LayoutPreset.FullRect);
        outer.AddThemeConstantOverride("separation", UiTokens.Space3);
        layers.AddChild(outer);

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
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(0, 280),
        };
        outer.AddChild(scroll);

        _rows = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _rows.AddThemeConstantOverride("h_separation", UiTokens.Space3);
        _rows.AddThemeConstantOverride("v_separation", UiTokens.Space3);
        scroll.AddChild(_rows);

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

        _checkBoxes.Clear();
        _rowRefs.Clear();
        _suppressSignals = true;

        foreach (var entry in _sim.DefinitionGraph.Generators.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var generatorId = entry.Key;
            var generator = entry.Value;
            var accent = UiTokens.KindAccent(generator.Target.Kind.Value);
            var card = new PanelContainer
            {
                CustomMinimumSize = new Vector2(308, 260),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            card.AddThemeStyleboxOverride("panel", UiTheme.CardStyle(accent, selected: HasActiveGeneratorCard(generatorId)));

            // Layered root so the illustrated card-face can sit behind the
            // content. PanelContainer manages ONE child, so we stack a
            // Control here and put both the background and the content
            // inside it as FullRect overlays.
            var layers = new Control
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            card.AddChild(layers);

            var cardFace = AssetLibrary.CardFace(generator.Target.Kind.Value);
            if (cardFace is not null)
            {
                var bg = new TextureRect
                {
                    Texture = cardFace,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                    // Dimmed so overlaid text stays readable on the dark card.
                    Modulate = new Color(1f, 1f, 1f, 0.28f),
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                bg.SetAnchorsPreset(LayoutPreset.FullRect);
                layers.AddChild(bg);
            }

            var row = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            row.SetAnchorsPreset(LayoutPreset.FullRect);
            row.AddThemeConstantOverride("separation", UiTokens.Space2);
            layers.AddChild(row);

            var headerPanel = new PanelContainer();
            headerPanel.AddThemeStyleboxOverride("panel", UiTheme.HeaderStripStyle(accent));
            row.AddChild(headerPanel);

            var headerColumn = new VBoxContainer();
            headerColumn.AddThemeConstantOverride("separation", UiTokens.Space1);
            headerPanel.AddChild(headerColumn);

            var banner = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            headerColumn.AddChild(banner);

            var typeLine = new Label
            {
                Text = BuildTypeLine(generator),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            typeLine.AddThemeFontOverride("font", UiTheme.BodyFont);
            typeLine.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
            typeLine.AddThemeColorOverride("font_color", UiTokens.Parchment);
            banner.AddChild(typeLine);

            var kindBadgePanel = new PanelContainer();
            kindBadgePanel.AddThemeStyleboxOverride("panel", UiTheme.BadgeStyle(accent));
            banner.AddChild(kindBadgePanel);

            var kindBadgeLabel = new Label
            {
                Text = generator.Target.Kind.Value.ToUpperInvariant(),
            };
            kindBadgeLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
            kindBadgeLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
            kindBadgeLabel.AddThemeColorOverride("font_color", accent);
            kindBadgePanel.AddChild(kindBadgeLabel);

            var head = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            head.AddThemeConstantOverride("separation", UiTokens.Space2);
            headerColumn.AddChild(head);

            var activePip = new ColorRect
            {
                CustomMinimumSize = new Vector2(8, 8),
                Color = HasActiveGeneratorCard(generatorId) ? ActivePip : InactivePip,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            head.AddChild(activePip);

            var checkBox = new CheckBox
            {
                ButtonPressed = HasActiveGeneratorCard(generatorId),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                Text = string.Empty,
            };
            checkBox.Toggled += enabled => OnGeneratorToggled(generatorId, enabled);
            head.AddChild(checkBox);

            var nameLabel = new Label
            {
                Text = generator.Name,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            nameLabel.AddThemeFontOverride("font", UiTheme.DisplayFont);
            nameLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleSmall);
            nameLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);
            head.AddChild(nameLabel);

            var initialInstance = _sim.WorldState.Generators.ById[generatorId];
            var initialTone = GetChipTone(initialInstance, isLastFired: false);
            var statusPanel = new PanelContainer();
            statusPanel.AddThemeStyleboxOverride("panel", UiTheme.ChipStyle(initialTone));
            head.AddChild(statusPanel);

            var statusLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            statusLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
            statusLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
            statusLabel.AddThemeColorOverride("font_color", UiTheme.ChipTextColor(initialTone));
            statusPanel.AddChild(statusLabel);

            var descriptionLabel = CreateBodyLabel(fontSize: UiTokens.SizeBodySmall);
            descriptionLabel.Text = string.IsNullOrWhiteSpace(generator.Description)
                ? generator.Id
                : generator.Description!;
            descriptionLabel.AddThemeColorOverride("font_color", UiTokens.Parchment);
            row.AddChild(descriptionLabel);

            var preconditionsPanel = CreateSectionPanel();
            var preconditionsLabel = CreateBodyLabel();
            preconditionsPanel.AddChild(WrapSection("Preconditions", preconditionsLabel));
            row.AddChild(preconditionsPanel);

            var outputsPanel = CreateSectionPanel();
            var outputsLabel = CreateBodyLabel();
            outputsPanel.AddChild(WrapSection("Outputs", outputsLabel));
            row.AddChild(outputsPanel);

            var statsPanel = CreateSectionPanel();
            var statsLabel = CreateBodyLabel();
            statsPanel.AddChild(WrapSection("Tracking", statsLabel));
            row.AddChild(statsPanel);

            var tooltip = BuildTooltip(generator);
            card.TooltipText = tooltip;
            checkBox.TooltipText = tooltip;
            nameLabel.TooltipText = tooltip;
            descriptionLabel.TooltipText = tooltip;
            preconditionsLabel.TooltipText = tooltip;
            outputsLabel.TooltipText = tooltip;
            statsLabel.TooltipText = tooltip;

            _rows.AddChild(card);
            _checkBoxes[generatorId] = checkBox;
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

        _suppressSignals = false;
        Refresh();
    }

    private void OnGeneratorToggled(string generatorId, bool enabled)
    {
        if (_suppressSignals || _sim is null || !_checkBoxes.TryGetValue(generatorId, out var checkBox))
        {
            return;
        }

        if (enabled)
        {
            if (HasActiveGeneratorCard(generatorId))
            {
                return;
            }

            if (!_sim.SpawnCards.TryGetValue(generatorId, out var cardDefinition) ||
                _sim.Tableau.PlaceCard(cardDefinition, _sim.Clock.CurrentTick) is null)
            {
                SetCheckBoxState(checkBox, false);
            }

            return;
        }

        var activeCard = _sim.Tableau.ActiveCards.FirstOrDefault(card => card.Definition.GeneratorId == generatorId);
        if (activeCard is null)
        {
            return;
        }

        _sim.Tableau.RemoveCard(activeCard);
    }

    private bool HasActiveGeneratorCard(string generatorId) =>
        _sim is not null && _sim.Tableau.ActiveCards.Any(card => card.Definition.GeneratorId == generatorId);

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

    private void SetCheckBoxState(CheckBox checkBox, bool pressed)
    {
        _suppressSignals = true;
        checkBox.ButtonPressed = pressed;
        _suppressSignals = false;
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
