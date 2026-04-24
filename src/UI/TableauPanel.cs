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

namespace TheCanonryGame.UI;

/// <summary>
/// Player-facing list of generator cards currently available on the tableau.
/// Toggling a checkbox removes or re-adds the corresponding zero-cost generator card.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class TableauPanel : PanelContainer
{
    private static readonly Color PanelBackground = new(0.05f, 0.08f, 0.11f, 0.84f);
    private static readonly Color PanelBorder = new(0.70f, 0.83f, 0.89f, 0.45f);
    private static readonly Color CardBackground = new(0.11f, 0.11f, 0.13f, 0.95f);
    private static readonly Color CardInset = new(0.17f, 0.17f, 0.18f, 0.82f);
    private static readonly Color CardBorder = new(0.79f, 0.69f, 0.48f, 0.62f);
    private static readonly Color CardInnerBorder = new(0.94f, 0.88f, 0.70f, 0.10f);
    private static readonly Color CardHeader = new(0.18f, 0.18f, 0.19f, 0.94f);
    private static readonly Color CardFrameShadow = new(0f, 0f, 0f, 0.28f);
    private static readonly Color ActivePip = new(0.29f, 0.66f, 0.51f, 1.0f);
    private static readonly Color InactivePip = new(0.41f, 0.18f, 0.18f, 1.0f);
    private static readonly Color TextPrimary = new(0.97f, 0.95f, 0.92f);
    private static readonly Color TextMuted = new(0.75f, 0.76f, 0.78f);
    private static readonly Color TextFlavor = new(0.82f, 0.82f, 0.78f);
    private static readonly Color TextRuleHeader = new(0.95f, 0.87f, 0.63f);
    private static readonly Color StatusChipText = new(0.97f, 0.96f, 0.92f);

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
            var accent = GetKindAccent(generator.Target.Kind);
            row.StatusLabel.Text = BuildStatusText(generatorId, instance);
            row.StatsLabel.Text = BuildStatsText(instance);
            row.OutputsLabel.Text = BuildOutputs(generator);
            row.PreconditionsLabel.Text = BuildPreconditions(generator);
            row.NameLabel.Modulate = isActive ? TextPrimary : new Color(0.72f, 0.72f, 0.72f);
            row.ActivePip.Color = isActive ? ActivePip : InactivePip;
            row.CardPanel.AddThemeStyleboxOverride("panel", BuildGeneratorCardStyle(accent, isActive, isLastFired));
            row.HeaderPanel.AddThemeStyleboxOverride("panel", BuildHeaderStyle(accent));
            row.KindBadgePanel.AddThemeStyleboxOverride("panel", BuildBadgeStyle(accent, 0.24f));
            row.StatusPanel.AddThemeStyleboxOverride("panel", BuildStatusStyle(instance, isLastFired));
            row.TypeLineLabel.AddThemeColorOverride("font_color", accent.Lightened(0.40f));
            row.KindBadgeLabel.AddThemeColorOverride("font_color", accent.Lightened(0.55f));
            row.StatusLabel.AddThemeColorOverride("font_color", StatusChipText);
        }
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(640, 420);
        AddThemeStyleboxOverride("panel", BuildPanelStyle());

        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", 8);
        AddChild(outer);

        var title = new Label
        {
            Text = "Tableau",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 16);
        outer.AddChild(title);

        _summaryLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _summaryLabel.AddThemeFontSizeOverride("font_size", 11);
        outer.AddChild(_summaryLabel);
        outer.AddChild(new HSeparator());

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
        _rows.AddThemeConstantOverride("h_separation", 12);
        _rows.AddThemeConstantOverride("v_separation", 12);
        scroll.AddChild(_rows);

        outer.AddChild(new HSeparator());
        _recentLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _recentLabel.AddThemeFontSizeOverride("font_size", 10);
        _recentLabel.AddThemeColorOverride("font_color", new Color(0.78f, 0.78f, 0.78f));
        outer.AddChild(_recentLabel);

        if (_sim is not null)
        {
            RebuildRows();
        }
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
            var accent = GetKindAccent(generator.Target.Kind);
            var card = new PanelContainer
            {
                CustomMinimumSize = new Vector2(296, 250),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            card.AddThemeStyleboxOverride("panel", BuildGeneratorCardStyle(accent, HasActiveGeneratorCard(generatorId), false));

            var row = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            row.AddThemeConstantOverride("separation", 4);
            card.AddChild(row);

            var headerPanel = new PanelContainer();
            headerPanel.AddThemeStyleboxOverride("panel", BuildHeaderStyle(accent));
            row.AddChild(headerPanel);

            var headerColumn = new VBoxContainer();
            headerColumn.AddThemeConstantOverride("separation", 4);
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
            typeLine.AddThemeFontSizeOverride("font_size", 9);
            typeLine.AddThemeColorOverride("font_color", accent.Lightened(0.40f));
            banner.AddChild(typeLine);

            var kindBadgePanel = new PanelContainer();
            kindBadgePanel.AddThemeStyleboxOverride("panel", BuildBadgeStyle(accent, 0.24f));
            banner.AddChild(kindBadgePanel);

            var kindBadgeLabel = new Label
            {
                Text = generator.Target.Kind.Value.ToUpperInvariant(),
            };
            kindBadgeLabel.AddThemeFontSizeOverride("font_size", 8);
            kindBadgeLabel.AddThemeColorOverride("font_color", accent.Lightened(0.55f));
            kindBadgePanel.AddChild(kindBadgeLabel);

            var head = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            headerColumn.AddChild(head);

            var activePip = new ColorRect
            {
                CustomMinimumSize = new Vector2(10, 10),
                Color = HasActiveGeneratorCard(generatorId) ? ActivePip : InactivePip,
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
            nameLabel.AddThemeFontSizeOverride("font_size", 15);
            nameLabel.AddThemeColorOverride("font_color", TextPrimary);
            head.AddChild(nameLabel);

            var statusPanel = new PanelContainer();
            statusPanel.AddThemeStyleboxOverride("panel", BuildStatusStyle(_sim.WorldState.Generators.ById[generatorId], false));
            head.AddChild(statusPanel);

            var statusLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            statusLabel.AddThemeFontSizeOverride("font_size", 9);
            statusLabel.AddThemeColorOverride("font_color", StatusChipText);
            statusPanel.AddChild(statusLabel);

            var descriptionLabel = CreateBodyLabel(fontSize: 11);
            descriptionLabel.Text = string.IsNullOrWhiteSpace(generator.Description)
                ? generator.Id
                : generator.Description!;
            descriptionLabel.AddThemeColorOverride("font_color", TextFlavor);
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

    private static Label CreateBodyLabel(int fontSize = 10)
    {
        var label = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", TextMuted);
        return label;
    }

    private static PanelContainer CreateSectionPanel()
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", BuildSectionStyle());
        return panel;
    }

    private static VBoxContainer WrapSection(string title, Label body)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 2);

        var titleLabel = new Label
        {
            Text = title.ToUpperInvariant(),
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 9);
        titleLabel.AddThemeColorOverride("font_color", TextRuleHeader);
        box.AddChild(titleLabel);
        box.AddChild(body);
        return box;
    }

    private static StyleBoxFlat BuildPanelStyle()
    {
        var style = new StyleBoxFlat
        {
            BgColor = PanelBackground,
            BorderColor = PanelBorder,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            ShadowColor = new Color(0f, 0f, 0f, 0.24f),
            ShadowSize = 8,
        };
        style.ContentMarginTop = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginBottom = 12;
        style.ContentMarginLeft = 12;
        return style;
    }

    private static StyleBoxFlat BuildGeneratorCardStyle(Color accent, bool isActive, bool isHighlighted)
    {
        var border = isHighlighted
            ? accent.Lightened(0.42f)
            : isActive
                ? accent.Lightened(0.18f)
                : CardBorder.Lerp(accent, 0.35f);
        var style = new StyleBoxFlat
        {
            BgColor = CardBackground,
            BorderColor = border,
            CornerRadiusTopLeft = 18,
            CornerRadiusTopRight = 18,
            CornerRadiusBottomRight = 18,
            CornerRadiusBottomLeft = 18,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            ShadowColor = CardFrameShadow,
            ShadowSize = isHighlighted ? 14 : 10,
        };
        style.ContentMarginTop = 10;
        style.ContentMarginRight = 10;
        style.ContentMarginBottom = 10;
        style.ContentMarginLeft = 10;
        return style;
    }

    private static StyleBoxFlat BuildHeaderStyle(Color accent)
    {
        var style = new StyleBoxFlat
        {
            BgColor = CardHeader.Lerp(accent.Darkened(0.70f), 0.30f),
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            BorderWidthBottom = 1,
            BorderColor = WithAlpha(accent.Lightened(0.15f), 0.45f),
        };
        style.ContentMarginTop = 8;
        style.ContentMarginRight = 8;
        style.ContentMarginBottom = 8;
        style.ContentMarginLeft = 8;
        return style;
    }

    private static StyleBoxFlat BuildBadgeStyle(Color accent, float alpha)
    {
        var style = new StyleBoxFlat
        {
            BgColor = WithAlpha(accent, alpha),
            BorderColor = WithAlpha(accent.Lightened(0.10f), 0.55f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusBottomLeft = 10,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
        };
        style.ContentMarginTop = 4;
        style.ContentMarginRight = 7;
        style.ContentMarginBottom = 4;
        style.ContentMarginLeft = 7;
        return style;
    }

    private static StyleBoxFlat BuildStatusStyle(GeneratorInstance instance, bool isLastFired)
    {
        var color = isLastFired
            ? new Color(0.74f, 0.56f, 0.20f, 0.95f)
            : instance.LastFiredTick is not null
                ? new Color(0.24f, 0.48f, 0.30f, 0.92f)
                : instance.FailedRuns > 0
                    ? new Color(0.46f, 0.20f, 0.20f, 0.92f)
                    : new Color(0.23f, 0.25f, 0.29f, 0.92f);
        var style = new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusBottomLeft = 10,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderColor = color.Lightened(0.16f),
        };
        style.ContentMarginTop = 4;
        style.ContentMarginRight = 6;
        style.ContentMarginBottom = 4;
        style.ContentMarginLeft = 6;
        return style;
    }

    private static StyleBoxFlat BuildSectionStyle()
    {
        var style = new StyleBoxFlat
        {
            BgColor = CardInset,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderColor = CardInnerBorder,
        };
        style.ContentMarginTop = 6;
        style.ContentMarginRight = 6;
        style.ContentMarginBottom = 6;
        style.ContentMarginLeft = 6;
        return style;
    }

    private static Color GetKindAccent(KindId kind) => kind.Value switch
    {
        "penguin" => new Color(0.45f, 0.73f, 0.88f),
        "settlement" => new Color(0.74f, 0.61f, 0.35f),
        "faction" => new Color(0.69f, 0.35f, 0.35f),
        "artifact" => new Color(0.60f, 0.80f, 0.67f),
        "region" => new Color(0.59f, 0.66f, 0.91f),
        "orca" => new Color(0.55f, 0.71f, 0.84f),
        _ => new Color(0.72f, 0.68f, 0.56f),
    };

    private static Color WithAlpha(Color color, float alpha) =>
        new(color.R, color.G, color.B, alpha);

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
