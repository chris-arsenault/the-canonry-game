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
    private bool _resizing;
    private ScrollContainer? _scroll;

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
        CustomMinimumSize = new Vector2(340, 400);
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

        // Copy button
        var copyBtn = new Button { Text = "📋", CustomMinimumSize = new Vector2(28, 28) };
        copyBtn.Pressed += OnCopyPressed;
        titleBar.AddChild(copyBtn);

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

        _scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 300),
        };
        _body.AddChild(_scroll);
        var scroll = _scroll;

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
                    // Resize handle: bottom-right 20x20 corner
                    if (mb.Position.X > Size.X - 20 && mb.Position.Y > Size.Y - 20)
                    {
                        _resizing = true;
                        GetViewport().SetInputAsHandled();
                    }
                    // Drag if clicking in title area (top 32px)
                    else if (mb.Position.Y < 32)
                    {
                        _dragging = true;
                        _dragOffset = mb.Position;
                        GetViewport().SetInputAsHandled();
                    }
                }
                else
                {
                    _dragging = false;
                    _resizing = false;
                }
            }
        }
        else if (@event is InputEventMouseMotion mm)
        {
            if (_dragging)
            {
                Position += mm.Relative;
                GetViewport().SetInputAsHandled();
            }
            else if (_resizing && _scroll is not null)
            {
                var newWidth = Mathf.Max(280, CustomMinimumSize.X + mm.Relative.X);
                var newScrollHeight = Mathf.Max(100, _scroll.CustomMinimumSize.Y + mm.Relative.Y);
                CustomMinimumSize = new Vector2(newWidth, CustomMinimumSize.Y + mm.Relative.Y);
                _scroll.CustomMinimumSize = new Vector2(0, newScrollHeight);
                GetViewport().SetInputAsHandled();
            }
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
        sb.AppendLine($"{entity.Subtype}  |  {entity.Culture}  |  {entity.Status}");
        if (entity.Coordinates is not null)
        {
            sb.AppendLine($"({entity.Coordinates.Value.Q}, {entity.Coordinates.Value.R})  |  age {_sim.Clock.CurrentTick - entity.CreatedAt}");
        }

        sb.AppendLine();

        // Goals
        sb.AppendLine("[b][u]Goals[/u][/b]");
        if (entity.Goals.Count == 0)
        {
            sb.AppendLine("  [i]No goals[/i]");
        }
        else
        {
            foreach (var goal in entity.Goals)
            {
                var providerName = goal.TargetEntityId == entity.Id ? "self" :
                    (_sim.Graph.GetEntity(goal.TargetEntityId)?.Name ?? goal.TargetEntityId.ToString());
                var prefix = goal.IsSubGoal ? "  ↳ " : "  ";
                var state = goal.IsActionable ? "next" : "blocked";
                var attr = goal.IsSubGoal
                    ? $"unblock {DescribeGoalEffect(goal.DesiredEffect)}"
                    : DescribeGoalEffect(goal.DesiredEffect);
                sb.AppendLine($"{prefix}[b]{attr}[/b] via {providerName} [{state}] (urgency {goal.Urgency:F2})");
            }
        }

        sb.AppendLine();

        // Action History (frequency)
        sb.AppendLine("[b][u]Recent Actions[/u][/b]");
        if (entity.ActionHistory.Count > 0)
        {
            var counts = new Dictionary<string, int>();
            foreach (var (_, name) in entity.ActionHistory)
            {
                counts.TryGetValue(name, out var c);
                counts[name] = c + 1;
            }

            foreach (var (name, count) in counts.OrderByDescending(kv => kv.Value))
            {
                var bar = new string('█', System.Math.Min(count, 20));
                sb.AppendLine($"  {name}: {count} [color=gray]{bar}[/color]");
            }

            if (entity.LastActionName is not null)
            {
                sb.AppendLine($"  [i]Last: {entity.LastActionName} (tick {entity.LastActionTick})[/i]");
            }
        }
        else
        {
            sb.AppendLine("  [i]None[/i]");
        }

        sb.AppendLine();

        // Attributes
        sb.AppendLine("[b][u]Attributes[/u][/b]");
        foreach (var attr in entity.Attributes.All.OrderBy(a => a.Name, System.StringComparer.Ordinal))
        {
            if (attr is Engine.Attributes.ScalarValue scalar)
            {
                var effectInfo = "";
                if (scalar.TickEffects.Count > 0)
                {
                    var tickDelta = scalar.TickEffects.Where(e => !e.IsMultiplier).Sum(e => e.Value);
                    if (System.Math.Abs(tickDelta) > 0.001f)
                        effectInfo = $" [color=gray]({tickDelta:+0.##;-0.##}/tick)[/color]";
                }

                sb.AppendLine($"  {scalar.Name}: [b]{scalar.Value:F1}[/b]{effectInfo}");
            }
            else if (attr is Engine.Attributes.SemanticPlane plane)
            {
                sb.AppendLine($"  {plane.Name}: [b]({plane.X:F2}, {plane.Y:F2})[/b] [{plane.Label}]");
            }
        }

        sb.AppendLine();

        // Active Effects (tick + static, per attribute)
        var hasEffects = false;
        foreach (var attr in entity.Attributes.All.OrderBy(a => a.Name, System.StringComparer.Ordinal))
        {
            void ShowEffects(string label, IReadOnlyList<Engine.Attributes.AttributeEffect> effects, string suffix)
            {
                foreach (var effect in effects)
                {
                    if (!hasEffects) { sb.AppendLine("[b][u]Active Effects[/u][/b]"); hasEffects = true; }
                    var sign = effect.Value >= 0 ? "+" : "";
                    var kind = effect.IsMultiplier ? "×" : "";
                    sb.AppendLine($"  {label}: {kind}{sign}{effect.Value:F3}{suffix} [color=gray]{effect.Description}[/color]");
                }
            }

            if (attr is Engine.Attributes.ScalarValue scalar)
            {
                ShowEffects(scalar.Name, scalar.TickEffects, "/tick");
                ShowEffects(scalar.Name, scalar.StaticEffects, "");
            }
            else if (attr is Engine.Attributes.SemanticPlane plane)
            {
                ShowEffects($"{plane.Name}.x", plane.TickEffectsX, "/tick");
                ShowEffects($"{plane.Name}.y", plane.TickEffectsY, "/tick");
                ShowEffects($"{plane.Name}.x", plane.StaticEffectsX, "");
                ShowEffects($"{plane.Name}.y", plane.StaticEffectsY, "");
            }
        }

        if (!hasEffects)
        {
            sb.AppendLine("[b][u]Active Effects[/u][/b]");
            sb.AppendLine("  [i](none)[/i]");
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

        // Evaluation Report (engine-produced, read-only)
        sb.AppendLine("[b][u]Processes[/u][/b]");
        var report = entity.LastEvaluation;
        if (report is null)
        {
            sb.AppendLine("  [i](no evaluation data)[/i]");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(report.StatusNote) && (report.IsStale || report.Candidates.Count == 0))
            {
                sb.AppendLine($"  [i][color=gray]{FormatReportStatus(report, _sim.Clock.CurrentTick)}[/color][/i]");
            }

            if (report.Candidates.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(report.StatusNote))
                {
                    sb.AppendLine("  [i](no evaluation data)[/i]");
                }
            }

            foreach (var candidate in report.Candidates)
            {
                var statusColor = candidate.WasSelected ? "cyan"
                    : candidate.PreconditionPassed ? "green" : "red";
                var statusIcon = candidate.WasSelected ? "★"
                    : candidate.PreconditionPassed ? "✓" : "✗";
                sb.AppendLine($"  [color={statusColor}]{statusIcon}[/color] {candidate.ProcessName} → {candidate.ProviderName}  score: {candidate.Score:F3}");
                if (!candidate.PreconditionPassed)
                {
                    sb.AppendLine($"    [color=gray]{candidate.PreconditionDiagnostic}[/color]");
                }
            }
        }

        _content.Text = sb.ToString();
    }

    private static string DescribeGoalEffect(Engine.Wiring.AttributeRef effect)
    {
        return effect.Scope switch
        {
            Engine.Wiring.RefScope.Self => effect.Name,
            Engine.Wiring.RefScope.Target => $"target.{effect.Name}",
            _ => effect.ToString(),
        };
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

    private void OnCopyPressed()
    {
        if (_sim is null || _content is null)
        {
            return;
        }

        var entity = _sim.Graph.GetEntity(_entityId);
        if (entity is null)
        {
            return;
        }

        DisplayServer.ClipboardSet(BuildClipboardText(entity, _content.Text));
    }

    private static string BuildClipboardText(Entity entity, string visibleText)
    {
        var sb = new StringBuilder(visibleText);
        AppendDiagnosticSection(sb, "Goal Generation Trace", entity.GoalGenerationTrace);
        AppendDiagnosticSection(sb, "Evaluation Trace", entity.EvaluationTrace);
        return sb.ToString();
    }

    private static void AppendDiagnosticSection(StringBuilder sb, string title, string? trace)
    {
        if (string.IsNullOrWhiteSpace(trace))
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine($"--- {title} ---");
        sb.AppendLine(trace.TrimEnd());
    }

    private static string FormatReportStatus(EvaluationReport report, int currentTick)
    {
        if (!report.IsStale)
        {
            return report.StatusNote ?? string.Empty;
        }

        var age = System.Math.Max(0, currentTick - report.SourceTick);
        var prefix = age == 1 ? "stale by 1 tick" : $"stale by {age} ticks";
        return string.IsNullOrWhiteSpace(report.StatusNote)
            ? prefix
            : $"{prefix}: {report.StatusNote}";
    }
}
