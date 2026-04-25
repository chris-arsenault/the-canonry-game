#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA1305 // UI display strings
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Godot;

using Engine.Graph;
using Engine.Simulation;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Floating entity info card. Draggable, collapsible, closable.
/// Shows entity attributes, current goal, last action, relationships.
/// Multiple cards can be open simultaneously.
/// </summary>
#pragma warning disable CA2213 // Godot nodes managed by scene tree
public partial class EntityCard : PanelContainer
{
    private const float DefaultWidth = 380f;
    private const float DefaultHeight = 460f;
    private const float MinimumWidth = 320f;
    private const float MinimumHeight = 220f;
    private const float ResizeGripSize = 24f;
    private const double BottomStickThreshold = 160d;

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
    private double _pendingScrollValue;
    private double _pendingScrollDistanceFromBottom;
    private bool _pendingScrollStickToBottom;
    private bool _scrollRestoreQueued;

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
        CustomMinimumSize = new Vector2(MinimumWidth, MinimumHeight);
        Size = new Vector2(DefaultWidth, DefaultHeight);
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", UiTheme.FloatingPanelStyle());

        var outerVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        outerVBox.AddThemeConstantOverride("separation", UiTokens.Space2);
        AddChild(outerVBox);

        // Title bar (draggable)
        var titleBar = new HBoxContainer();
        titleBar.AddThemeConstantOverride("separation", UiTokens.Space2);
        outerVBox.AddChild(titleBar);

        _titleLabel = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Text = "Entity",
        };
        _titleLabel.AddThemeFontOverride("font", UiTheme.DisplayFont);
        _titleLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        _titleLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);
        titleBar.AddChild(_titleLabel);

        // Tiny chrome buttons — body font, minimal.
        var copyBtn = BuildChromeButton("copy", "Copy details");
        copyBtn.Pressed += OnCopyPressed;
        titleBar.AddChild(copyBtn);

        var collapseBtn = BuildChromeButton("–", "Collapse");
        collapseBtn.Pressed += OnCollapsePressed;
        titleBar.AddChild(collapseBtn);

        var closeBtn = BuildChromeButton("×", "Close");
        closeBtn.Pressed += OnClosePressed;
        titleBar.AddChild(closeBtn);

        // Hairline rule
        outerVBox.AddChild(new ColorRect
        {
            Color = UiTokens.Hairline,
            CustomMinimumSize = new Vector2(0, 1),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });

        // Body (collapsible)
        _body = new VBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        outerVBox.AddChild(_body);

        _scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
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
        _content.AddThemeFontOverride("normal_font", UiTheme.BodyFont);
        _content.AddThemeFontOverride("mono_font", UiTheme.MonoFont);
        _content.AddThemeFontSizeOverride("normal_font_size", UiTokens.SizeBodySmall);
        _content.AddThemeFontSizeOverride("mono_font_size", UiTokens.SizeBodySmall);
        _content.AddThemeColorOverride("default_color", UiTokens.Vellum);
        scroll.AddChild(_content);

        Refresh();
    }

    private static Button BuildChromeButton(string text, string tooltip)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(26, 26),
            TooltipText = tooltip,
        };
        btn.AddThemeFontOverride("font", UiTheme.BodyFont);
        btn.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        return btn;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    // Resize handle: bottom-right corner
                    if (mb.Position.X > Size.X - ResizeGripSize && mb.Position.Y > Size.Y - ResizeGripSize)
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
                var newSize = Size + mm.Relative;
                newSize.X = Mathf.Max(MinimumWidth, newSize.X);
                newSize.Y = Mathf.Max(MinimumHeight, newSize.Y);
                Size = newSize;
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

        _titleLabel.Text = $"{entity.Name}  ·  {entity.Kind}";
        if (_collapsed) return; // Don't rebuild body when collapsed

        CaptureScrollAnchor();

        var sb = new StringBuilder();

        // Archival header — subtype/culture/status in small-caps parchment,
        // coordinates and age in mono for tabular legibility.
        var secondary = ColorTag(UiTokens.Parchment);
        var whisper = ColorTag(UiTokens.Whisper);
        sb.AppendLine($"[color={secondary}]{entity.Subtype}  ·  {entity.Culture}  ·  {entity.Status}[/color]");
        sb.AppendLine($"[code][color={whisper}]({entity.Coordinates.Q}, {entity.Coordinates.R})  ·  age {_sim.Clock.CurrentTick - entity.CreatedAt}t[/color][/code]");

        sb.AppendLine();

        // Goals
        sb.AppendLine(SectionHeader("Goals"));
        if (entity.Goals.Count == 0)
        {
            sb.AppendLine($"  [color={whisper}]— no goals —[/color]");
        }
        else
        {
            foreach (var goal in entity.Goals)
            {
                var providerName = goal.TargetEntityId == entity.Id ? "self" :
                    (_sim.Graph.GetEntity(goal.TargetEntityId)?.Name ?? goal.TargetEntityId.ToString());
                var prefix = goal.IsSubGoal ? "  ↳ " : "  ";
                var stateColor = goal.IsActionable ? ColorTag(UiTokens.Aurora) : ColorTag(UiTokens.Ember);
                var state = goal.IsActionable ? "next" : "blocked";
                var attr = goal.IsSubGoal
                    ? $"unblock {DescribeGoalEffect(goal.DesiredEffect)}"
                    : DescribeGoalEffect(goal.DesiredEffect);
                sb.AppendLine($"{prefix}[b]{attr}[/b] via {providerName}  [color={stateColor}]{state}[/color]  [color={whisper}]u {goal.Urgency:F2}[/color]");
            }
        }

        sb.AppendLine();

        // Action History (frequency)
        sb.AppendLine(SectionHeader("Recent Actions"));
        if (entity.ActionHistory.Count > 0)
        {
            var counts = new Dictionary<Engine.Processes.ProcessRuntimeId, int>();
            foreach (var (_, processId) in entity.ActionHistory)
            {
                counts.TryGetValue(processId, out var c);
                counts[processId] = c + 1;
            }

            foreach (var (processId, count) in counts.OrderByDescending(kv => kv.Value))
            {
                var name = ProcessName(processId);
                var bar = new string('▌', System.Math.Min(count, 20));
                sb.AppendLine($"  {name}: {count} [color={ColorTag(UiTokens.Whisper)}]{bar}[/color]");
            }

            if (!entity.LastActionProcessId.IsNone)
            {
                sb.AppendLine($"  [i][color={whisper}]Last: {ProcessName(entity.LastActionProcessId)} @ t{entity.LastActionTick}[/color][/i]");
            }
        }
        else
        {
            sb.AppendLine($"  [color={whisper}]— none —[/color]");
        }

        sb.AppendLine();

        // Attributes
        sb.AppendLine(SectionHeader("Attributes"));
        foreach (var attr in entity.Attributes.All.OrderBy(a => a.Name, System.StringComparer.Ordinal))
        {
            if (attr is Engine.Attributes.ScalarValue scalar)
            {
                var effectInfo = "";
                if (scalar.TickEffects.Count > 0)
                {
                    var tickDelta = scalar.TickEffects.Where(e => !e.IsMultiplier).Sum(e => e.Value);
                    if (System.Math.Abs(tickDelta) > 0.001f)
                        effectInfo = $" [color={whisper}]({tickDelta:+0.##;-0.##}/tick)[/color]";
                }

                sb.AppendLine($"  {scalar.Name}: [b]{scalar.Value:F1}[/b]{effectInfo}");
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
                    if (!hasEffects) { sb.AppendLine(SectionHeader("Active Effects")); hasEffects = true; }
                    var sign = effect.Value >= 0 ? "+" : "";
                    var kind = effect.IsMultiplier ? "×" : "";
                    sb.AppendLine($"  {label}: {kind}{sign}{effect.Value:F3}{suffix} [color={whisper}]{effect.Description}[/color]");
                }
            }

            if (attr is Engine.Attributes.ScalarValue scalar)
            {
                ShowEffects(scalar.Name, scalar.TickEffects, "/tick");
                ShowEffects(scalar.Name, scalar.StaticEffects, "");
            }
        }

        if (!hasEffects)
        {
            sb.AppendLine(SectionHeader("Active Effects"));
            sb.AppendLine($"  [color={whisper}]— none —[/color]");
        }

        sb.AppendLine();

        // Relationships
        sb.AppendLine(SectionHeader("Relationships"));
        var outgoing = _sim.Graph.OutgoingRelationships(_entityId).ToList();
        var incoming = _sim.Graph.IncomingRelationships(_entityId).ToList();

        if (outgoing.Count == 0 && incoming.Count == 0)
        {
            sb.AppendLine($"  [color={whisper}]— none —[/color]");
        }

        foreach (var rel in outgoing)
        {
            var targetName = _sim.Graph.GetEntity(rel.TargetId)?.Name ?? rel.TargetId.ToString();
            sb.AppendLine($"  → {rel.Kind} → {targetName} [color={whisper}]({rel.Strength:F2})[/color]");
        }

        foreach (var rel in incoming)
        {
            var sourceName = _sim.Graph.GetEntity(rel.SourceId)?.Name ?? rel.SourceId.ToString();
            sb.AppendLine($"  ← {rel.Kind} ← {sourceName} [color={whisper}]({rel.Strength:F2})[/color]");
        }

        // Evaluation Report (engine-produced, read-only)
        sb.AppendLine(SectionHeader("Processes"));
        var report = entity.LastEvaluation;
        if (report is null)
        {
            sb.AppendLine($"  [color={whisper}]— no evaluation data —[/color]");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(report.StatusNote) && (report.IsStale || report.Candidates.Count == 0))
            {
                sb.AppendLine($"  [i][color={whisper}]{FormatReportStatus(report, _sim.Clock.CurrentTick)}[/color][/i]");
            }

            if (report.Candidates.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(report.StatusNote))
                {
                    sb.AppendLine($"  [color={whisper}]— no evaluation data —[/color]");
                }
            }

            foreach (var candidate in report.Candidates)
            {
                var statusColor = candidate.WasSelected
                    ? ColorTag(UiTokens.Ember)
                    : candidate.PreconditionPassed
                        ? ColorTag(UiTokens.Aurora)
                        : ColorTag(UiTokens.Blood);
                var statusIcon = candidate.WasSelected ? "★"
                    : candidate.PreconditionPassed ? "✓" : "✗";
                var providerName = _sim.Graph.GetEntity(candidate.ProviderId)?.Name ?? candidate.ProviderId.ToString();
                sb.AppendLine($"  [color={statusColor}]{statusIcon}[/color] {ProcessName(candidate.ProcessId)} → {providerName}  [color={whisper}]score {candidate.Score:F3}[/color]");
                if (!candidate.PreconditionPassed)
                {
                    sb.AppendLine($"    [color={whisper}]{candidate.PreconditionDiagnostic}[/color]");
                }
            }
        }

        _content.Text = sb.ToString();
        QueueScrollRestore();
    }

    private static string SectionHeader(string title)
    {
        var caption = ColorTag(UiTokens.Parchment);
        return $"\n[color={caption}]{title.ToUpperInvariant()}[/color]";
    }

    private string ProcessName(Engine.Processes.ProcessRuntimeId processId)
    {
        if (_sim is null || processId.IsNone)
        {
            return string.Empty;
        }

        return _sim.BehaviorDefinitions.TryGetDefinition(processId, out var definition) &&
               definition is not null
            ? definition.Name
            : processId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string ColorTag(Color color) => Main.ColorHex(color);

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
        _ = entity;
        return visibleText;
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

    private void CaptureScrollAnchor()
    {
        if (_scroll is null)
        {
            return;
        }

        var bar = _scroll.GetVScrollBar();
        if (bar is null)
        {
            return;
        }

        var maxScroll = GetMaxScroll(bar);
        _pendingScrollValue = bar.Value;
        _pendingScrollDistanceFromBottom = System.Math.Max(0d, maxScroll - bar.Value);
        _pendingScrollStickToBottom = _pendingScrollDistanceFromBottom <= BottomStickThreshold;
    }

    private void QueueScrollRestore()
    {
        if (_scroll is null || _scrollRestoreQueued)
        {
            return;
        }

        _scrollRestoreQueued = true;
        CallDeferred(nameof(DeferScrollRestore));
    }

    private void DeferScrollRestore()
    {
        CallDeferred(nameof(ApplyScrollRestore));
    }

    private void ApplyScrollRestore()
    {
        _scrollRestoreQueued = false;

        if (_scroll is null)
        {
            return;
        }

        var bar = _scroll.GetVScrollBar();
        if (bar is null)
        {
            return;
        }

        var maxScroll = GetMaxScroll(bar);
        var target = _pendingScrollStickToBottom
            ? System.Math.Max(0d, maxScroll - _pendingScrollDistanceFromBottom)
            : System.Math.Min(_pendingScrollValue, maxScroll);
        _scroll.ScrollVertical = (int)System.Math.Round(target);
    }

    private static double GetMaxScroll(VScrollBar bar)
    {
        return System.Math.Max(0d, bar.MaxValue - bar.Page);
    }
}
