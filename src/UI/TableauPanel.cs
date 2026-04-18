#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA1305 // UI display strings
using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Simulation;

namespace TheCanonryGame.UI;

/// <summary>
/// Player-facing list of generator cards currently available on the tableau.
/// Toggling a checkbox removes or re-adds the corresponding zero-cost generator card.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class TableauPanel : PanelContainer
{
    private SimulationRunner? _sim;
    private VBoxContainer? _rows;
    private readonly Dictionary<string, CheckBox> _checkBoxes = [];
    private bool _suppressSignals;

    public void Initialize(SimulationRunner sim)
    {
        _sim = sim;
        if (_rows is not null)
        {
            RebuildRows();
        }
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(280, 220);

        var outer = new VBoxContainer();
        AddChild(outer);

        var title = new Label
        {
            Text = "Generators",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 14);
        outer.AddChild(title);
        outer.AddChild(new HSeparator());

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(0, 180),
        };
        outer.AddChild(scroll);

        _rows = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        scroll.AddChild(_rows);

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
        _suppressSignals = true;

        foreach (var entry in _sim.DefinitionGraph.Generators.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var generatorId = entry.Key;
            var generator = entry.Value;
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };

            var checkBox = new CheckBox
            {
                ButtonPressed = HasActiveGeneratorCard(generatorId),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            checkBox.Toggled += enabled => OnGeneratorToggled(generatorId, enabled);
            row.AddChild(checkBox);

            var textColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            row.AddChild(textColumn);

            var nameLabel = new Label
            {
                Text = generator.Name,
            };
            textColumn.AddChild(nameLabel);

            var subtitle = new Label
            {
                Text = BuildSubtitle(generator),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            subtitle.AddThemeFontSizeOverride("font_size", 10);
            subtitle.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
            textColumn.AddChild(subtitle);

            var tooltip = BuildTooltip(generator);
            row.TooltipText = tooltip;
            checkBox.TooltipText = tooltip;
            nameLabel.TooltipText = tooltip;
            subtitle.TooltipText = tooltip;

            _rows.AddChild(row);
            _checkBoxes[generatorId] = checkBox;
        }

        _suppressSignals = false;
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

    private static string BuildSubtitle(DefinitionGeneratorNode generator)
    {
        var subtitle = string.IsNullOrWhiteSpace(generator.Description)
            ? generator.Id
            : generator.Description!;
        return subtitle.Length <= 40 ? subtitle : subtitle[..37] + "...";
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
}
