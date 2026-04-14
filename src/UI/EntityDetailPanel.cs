#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA1305 // UI display strings
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Godot;

using Engine.Graph;
using Engine.Simulation;

namespace TheCanonryGame.UI;

/// <summary>
/// Side panel showing details for a selected entity: attributes, relationships, effects.
/// Updates live as the simulation ticks.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class EntityDetailPanel : PanelContainer
{
    private SimulationRunner? _sim;
    private EntityId? _selectedEntityId;
    private RichTextLabel? _content;
    private Label? _titleLabel;

    public override void _Ready()
    {
        // Panel setup
        CustomMinimumSize = new Vector2(300, 0);
        SizeFlagsVertical = SizeFlags.ExpandFill;

        var vbox = new VBoxContainer();
        AddChild(vbox);

        // Title
        _titleLabel = new Label
        {
            Text = "Select an entity",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(_titleLabel);

        // Separator
        vbox.AddChild(new HSeparator());

        // Scrollable content
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        vbox.AddChild(scroll);

        _content = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ScrollActive = false,
        };
        scroll.AddChild(_content);
    }

    public void Initialize(SimulationRunner sim)
    {
        _sim = sim;
    }

    public void SelectEntity(EntityId entityId)
    {
        _selectedEntityId = entityId;
        Refresh();
    }

    public void ClearSelection()
    {
        _selectedEntityId = null;
        if (_titleLabel is not null) _titleLabel.Text = "Select an entity";
        if (_content is not null) _content.Text = "";
    }

    /// <summary>Refresh the display with current simulation state.</summary>
    public void Refresh()
    {
        if (_sim is null || _selectedEntityId is null || _content is null || _titleLabel is null)
        {
            return;
        }

        var entity = _sim.Graph.GetEntity(_selectedEntityId.Value);
        if (entity is null)
        {
            _titleLabel.Text = "Entity not found";
            _content.Text = "";
            return;
        }

        _titleLabel.Text = $"{entity.Name} ({entity.Kind})";

        var sb = new StringBuilder();

        // Header info
        sb.AppendLine($"[b]Kind:[/b] {entity.Kind}");
        sb.AppendLine($"[b]Subtype:[/b] {entity.Subtype}");
        sb.AppendLine($"[b]Status:[/b] {entity.Status}");
        sb.AppendLine($"[b]Culture:[/b] {entity.Culture}");
        sb.AppendLine($"[b]Age:[/b] {_sim.Clock.CurrentTick - entity.CreatedAt} ticks");
        if (entity.Coordinates is not null)
        {
            sb.AppendLine($"[b]Position:[/b] ({entity.Coordinates.Value.Q}, {entity.Coordinates.Value.R})");
        }

        sb.AppendLine();

        // Attributes
        sb.AppendLine("[b][u]Attributes[/u][/b]");
        var attrNames = entity.Attributes.Names.OrderBy(n => n, System.StringComparer.Ordinal);
        foreach (var attrName in attrNames)
        {
            var baseValue = entity.Attributes.GetValue(attrName);
            var effectMod = _sim.Effects.ComputeEffectModifier(_selectedEntityId.Value, attrName);
            var trend = entity.Attributes[attrName]?.Trend ?? 0f;
            var trendArrow = trend > 0.01f ? "[color=green]▲[/color]" : trend < -0.01f ? "[color=red]▼[/color]" : "—";

            if (System.Math.Abs(effectMod) > 0.01f)
            {
                sb.AppendLine($"  {attrName}: [b]{baseValue:F1}[/b] (+{effectMod:F1} effects) {trendArrow}");
            }
            else
            {
                sb.AppendLine($"  {attrName}: [b]{baseValue:F1}[/b] {trendArrow}");
            }
        }

        sb.AppendLine();

        // Active Relationships
        sb.AppendLine("[b][u]Relationships[/u][/b]");
        var outgoing = _sim.Graph.OutgoingRelationships(_selectedEntityId.Value).ToList();
        var incoming = _sim.Graph.IncomingRelationships(_selectedEntityId.Value).ToList();

        if (outgoing.Count == 0 && incoming.Count == 0)
        {
            sb.AppendLine("  (none)");
        }

        foreach (var rel in outgoing)
        {
            var targetName = _sim.Graph.GetEntity(rel.TargetId)?.Name ?? rel.TargetId.ToString();
            sb.AppendLine($"  → {rel.Kind} → {targetName} (str: {rel.Strength:F2})");
        }

        foreach (var rel in incoming)
        {
            var sourceName = _sim.Graph.GetEntity(rel.SourceId)?.Name ?? rel.SourceId.ToString();
            sb.AppendLine($"  ← {rel.Kind} ← {sourceName} (str: {rel.Strength:F2})");
        }

        sb.AppendLine();

        // Active Effects
        var effects = _sim.Effects.GetAllEffects(_selectedEntityId.Value);
        if (effects.Count > 0)
        {
            sb.AppendLine("[b][u]Active Effects[/u][/b]");
            foreach (var effect in effects)
            {
                var sign = effect.Value >= 0 ? "+" : "";
                sb.AppendLine($"  {effect.AttributeName}: {sign}{effect.Value:F1} [{effect.EffectType}] from {effect.SourceDescription}");
            }
        }

        _content.Text = sb.ToString();
    }
}
