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
/// Floating entity info card. Draggable, collapsible, closable.
/// Shows entity attributes, current goal, last action, relationships.
/// Multiple cards can be open simultaneously.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class EntityCard : PanelContainer
{
    private SimulationRunner? _sim;
    private EntityId _entityId;
    private RichTextLabel? _content;
    private VBoxContainer? _body;
    private Label? _titleLabel;
    private bool _dragging;
    private Vector2 _dragOffset;
    private bool _collapsed;

    /// <summary>Emitted when the user closes this card.</summary>
    [Signal]
    public delegate void CardClosedEventHandler(EntityCard card);

    public EntityId EntityId => _entityId;

    public void Initialize(SimulationRunner sim, EntityId entityId, Vector2 position)
    {
        _sim = sim;
        _entityId = entityId;
        Position = position;
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(320, 0);
        MouseFilter = MouseFilterEnum.Stop;

        var outerVBox = new VBoxContainer();
        AddChild(outerVBox);

        // Title bar (draggable)
        var titleBar = new HBoxContainer();
        outerVBox.AddChild(titleBar);

        _titleLabel = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Text = "Entity",
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 14);
        titleBar.AddChild(_titleLabel);

        // Collapse button
        var collapseBtn = new Button { Text = "_", CustomMinimumSize = new Vector2(28, 28) };
        collapseBtn.Pressed += OnCollapsePressed;
        titleBar.AddChild(collapseBtn);

        // Close button
        var closeBtn = new Button { Text = "X", CustomMinimumSize = new Vector2(28, 28) };
        closeBtn.Pressed += OnClosePressed;
        titleBar.AddChild(closeBtn);

        // Separator
        outerVBox.AddChild(new HSeparator());

        // Body (collapsible)
        _body = new VBoxContainer();
        outerVBox.AddChild(_body);

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 200),
        };
        _body.AddChild(scroll);

        _content = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ScrollActive = false,
        };
        scroll.AddChild(_content);

        Refresh();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    // Start drag if clicking in title area (top 32px)
                    if (mb.Position.Y < 32)
                    {
                        _dragging = true;
                        _dragOffset = mb.Position;
                        GetViewport().SetInputAsHandled();
                    }
                }
                else
                {
                    _dragging = false;
                }
            }
        }
        else if (@event is InputEventMouseMotion mm && _dragging)
        {
            Position += mm.Relative;
            GetViewport().SetInputAsHandled();
        }
    }

    public void Refresh()
    {
        if (_sim is null || _content is null || _titleLabel is null) return;

        var entity = _sim.Graph.GetEntity(_entityId);
        if (entity is null)
        {
            _titleLabel.Text = "Entity not found";
            _content.Text = "";
            return;
        }

        _titleLabel.Text = $"{entity.Name} ({entity.Kind})";
        if (_collapsed) return; // Don't rebuild body when collapsed

        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"[b]Subtype:[/b] {entity.Subtype}  |  [b]Culture:[/b] {entity.Culture}  |  [b]Status:[/b] {entity.Status}");
        if (entity.Coordinates is not null)
        {
            sb.AppendLine($"[b]Position:[/b] ({entity.Coordinates.Value.Q}, {entity.Coordinates.Value.R})  |  [b]Age:[/b] {_sim.Clock.CurrentTick - entity.CreatedAt}");
        }

        sb.AppendLine();

        // Current Goal
        sb.AppendLine("[b][u]Current Goal[/u][/b]");
        if (entity.CurrentGoal is not null)
        {
            var targetName = _sim.Graph.GetEntity(entity.CurrentGoal.TargetEntityId)?.Name ?? "unknown";
            sb.AppendLine($"  Process: [b]{entity.CurrentGoal.ProcessId}[/b]");
            sb.AppendLine($"  Target: {targetName} at ({entity.CurrentGoal.TargetCoordinates.Q}, {entity.CurrentGoal.TargetCoordinates.R})");
            sb.AppendLine($"  Score: {entity.CurrentGoal.Score:F3}");
        }
        else
        {
            sb.AppendLine("  [i]No active goal[/i]");
        }

        sb.AppendLine();

        // Last Action
        sb.AppendLine("[b][u]Last Action[/u][/b]");
        if (entity.LastActionName is not null)
        {
            sb.AppendLine($"  {entity.LastActionName} [color=gray](tick {entity.LastActionTick})[/color]");
        }
        else
        {
            sb.AppendLine("  [i]None[/i]");
        }

        sb.AppendLine();

        // Attributes
        sb.AppendLine("[b][u]Attributes[/u][/b]");
        var attrNames = _sim.Attributes.GetAttributeNames(_entityId).OrderBy(n => n, System.StringComparer.Ordinal);
        foreach (var attrName in attrNames)
        {
            var value = _sim.Attributes.Get(_entityId, attrName);
            var effectMod = _sim.Effects.ComputeEffectModifier(_entityId, attrName);
            var trend = _sim.Attributes.GetTrend(_entityId, attrName);
            var trendArrow = trend > 0.01f ? "[color=green]▲[/color]" : trend < -0.01f ? "[color=red]▼[/color]" : "—";

            if (System.Math.Abs(effectMod) > 0.01f)
            {
                sb.AppendLine($"  {attrName}: [b]{value:F1}[/b] (+{effectMod:F1}) {trendArrow}");
            }
            else
            {
                sb.AppendLine($"  {attrName}: [b]{value:F1}[/b] {trendArrow}");
            }
        }

        sb.AppendLine();

        // Relationships
        sb.AppendLine("[b][u]Relationships[/u][/b]");
        var outgoing = _sim.Graph.OutgoingRelationships(_entityId).ToList();
        var incoming = _sim.Graph.IncomingRelationships(_entityId).ToList();

        if (outgoing.Count == 0 && incoming.Count == 0)
        {
            sb.AppendLine("  [i](none)[/i]");
        }

        foreach (var rel in outgoing)
        {
            var targetName = _sim.Graph.GetEntity(rel.TargetId)?.Name ?? rel.TargetId.ToString();
            sb.AppendLine($"  → {rel.Kind} → {targetName} ({rel.Strength:F2})");
        }

        foreach (var rel in incoming)
        {
            var sourceName = _sim.Graph.GetEntity(rel.SourceId)?.Name ?? rel.SourceId.ToString();
            sb.AppendLine($"  ← {rel.Kind} ← {sourceName} ({rel.Strength:F2})");
        }

        _content.Text = sb.ToString();
    }

    private void OnCollapsePressed()
    {
        _collapsed = !_collapsed;
        if (_body is not null) _body.Visible = !_collapsed;
    }

    private void OnClosePressed()
    {
        EmitSignal(SignalName.CardClosed, this);
        QueueFree();
    }
}
