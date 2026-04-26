#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA1305 // UI display strings
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Godot;

using Engine.Projection;
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

    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private int _entityId;
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

    public int EntityId => _entityId;

    public void Initialize(
        UiProjectionState? projection,
        IProjectionSymbolCatalog? symbols,
        int entityId,
        Vector2 position)
    {
        _projection = projection;
        _symbols = symbols;
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
        if (_projection is null || _content is null || _titleLabel is null) return;

        if (!_projection.Entities.TryGetValue(_entityId, out var entity))
        {
            _titleLabel.Text = "Entity not found";
            _content.Text = "";
            return;
        }

        var entityName = ResolveEntityName(entity);
        var kindName = ResolveDisplayName(ProjectionSymbolKind.Kind, entity.KindId, $"Kind {entity.KindId}");
        _titleLabel.Text = $"{entityName}  ·  {kindName}";
        if (_collapsed) return; // Don't rebuild body when collapsed

        CaptureScrollAnchor();

        var sb = new StringBuilder();

        // Archival header — subtype/culture/status in small-caps parchment,
        // coordinates and age in mono for tabular legibility.
        var secondary = ColorTag(UiTokens.Parchment);
        var whisper = ColorTag(UiTokens.Whisper);
        var subtype = ResolveDisplayName(ProjectionSymbolKind.Subtype, entity.SubtypeId, $"Subtype {entity.SubtypeId}");
        var culture = ResolveDisplayName(ProjectionSymbolKind.Culture, entity.CultureId, $"Culture {entity.CultureId}");
        var status = entity.StatusCode == UiProjectionStatusCodes.Active ? "Active" : "Historical";
        sb.AppendLine($"[color={secondary}]{subtype}  ·  {culture}  ·  {status}[/color]");
        var currentTick = _projection?.CurrentTick ?? 0;
        var createdTick = entity.CreatedTick ?? 0;
        sb.AppendLine($"[code][color={whisper}]({entity.PhysicalQ}, {entity.PhysicalR})  ·  age {currentTick - createdTick}t[/color][/code]");

        sb.AppendLine();

        sb.AppendLine(SectionHeader("Seed State"));
        AppendSeedMetric(sb, "population", entity.SeedPopulation, whisper);
        AppendSeedMetric(sb, "integrity", entity.SeedIntegrity, whisper);
        AppendSeedMetric(sb, "projection", entity.SeedProjection, whisper);
        AppendSeedMetric(sb, "cohesion", entity.SeedCohesion, whisper);
        AppendSeedMetric(sb, "reach", entity.SeedReach, whisper);
        AppendSeedMetric(sb, "provisions", entity.SeedProvisions, whisper);
        AppendSeedMetric(sb, "vitality", entity.SeedVitality, whisper);
        AppendSeedMetric(sb, "agency", entity.SeedAgency, whisper);
        AppendSeedMetric(sb, "memory", entity.SeedMemory, whisper);
        AppendSeedMetric(sb, "potency", entity.SeedPotency, whisper);
        AppendSeedMetric(sb, "notoriety", entity.SeedNotoriety, whisper);

        sb.AppendLine();

        // Attributes
        sb.AppendLine(SectionHeader("Attributes"));
        if (entity.Attributes.Count == 0)
        {
            sb.AppendLine($"  [color={whisper}]— no attribute events —[/color]");
        }
        else
        {
            foreach (var attr in entity.Attributes.Values.OrderBy(attr => attr.AttributeId))
            {
                var name = ResolveDisplayName(
                    ProjectionSymbolKind.Attribute,
                    attr.AttributeId,
                    $"Attribute {attr.AttributeId}");
                sb.AppendLine($"  {name}: [b]{attr.NewValue:F1}[/b] [color={whisper}]from {attr.OldValue:F1}[/color]");
            }
        }

        sb.AppendLine();

        // Relationships
        sb.AppendLine(SectionHeader("Relationships"));
        var outgoing = _projection.Relationships.Values
            .Where(rel => rel.Key.SourceEntityId == _entityId && rel.IsActive)
            .OrderBy(rel => rel.Key.TargetEntityId)
            .ToArray();
        var incoming = _projection.Relationships.Values
            .Where(rel => rel.Key.TargetEntityId == _entityId && rel.IsActive)
            .OrderBy(rel => rel.Key.SourceEntityId)
            .ToArray();

        if (outgoing.Count == 0 && incoming.Count == 0)
        {
            sb.AppendLine($"  [color={whisper}]— none —[/color]");
        }

        foreach (var rel in outgoing)
        {
            var targetName = ResolveEntityName(rel.Key.TargetEntityId);
            var relationshipName = ResolveDisplayName(
                ProjectionSymbolKind.RelationshipKind,
                rel.Key.RelationshipKindId,
                $"Relationship {rel.Key.RelationshipKindId}");
            sb.AppendLine($"  → {relationshipName} → {targetName} [color={whisper}]({rel.NewStrength:F2})[/color]");
        }

        foreach (var rel in incoming)
        {
            var sourceName = ResolveEntityName(rel.Key.SourceEntityId);
            var relationshipName = ResolveDisplayName(
                ProjectionSymbolKind.RelationshipKind,
                rel.Key.RelationshipKindId,
                $"Relationship {rel.Key.RelationshipKindId}");
            sb.AppendLine($"  ← {relationshipName} ← {sourceName} [color={whisper}]({rel.NewStrength:F2})[/color]");
        }

        sb.AppendLine(SectionHeader("Processes"));
        if (entity.LastProcessEvaluation is null &&
            entity.LastProcessExecution is null &&
            entity.LastGoalGeneration is null &&
            entity.LastDecisionExecution is null)
        {
            sb.AppendLine($"  [color={whisper}]— no process events —[/color]");
        }
        else
        {
            if (entity.LastGoalGeneration is { } goals)
            {
                sb.AppendLine($"  goals generated: {goals.GoalCount} [color={whisper}]actionable {goals.ActionableGoalCount} @ t{goals.Tick}[/color]");
            }

            if (entity.LastProcessEvaluation is { } evaluation)
            {
                var selectedGoal = ResolveDisplayName(ProjectionSymbolKind.Goal, evaluation.SelectedGoalId, $"Goal {evaluation.SelectedGoalId}");
                var process = ResolveDisplayName(ProjectionSymbolKind.Process, evaluation.ExecutedProcessId, $"Process {evaluation.ExecutedProcessId}");
                sb.AppendLine($"  evaluated: {selectedGoal} / {process} [color={whisper}]candidates {evaluation.CandidateCount} @ t{evaluation.Tick}[/color]");
            }

            if (entity.LastProcessExecution is { } execution)
            {
                var process = ResolveDisplayName(ProjectionSymbolKind.Process, execution.ProcessId, $"Process {execution.ProcessId}");
                sb.AppendLine($"  executed: {process} [color={whisper}]affected {execution.AffectedEntityCount} @ t{execution.Tick}[/color]");
            }

            if (entity.LastDecisionExecution is { } decision)
            {
                var decisionName = ResolveDisplayName(ProjectionSymbolKind.Decision, decision.DecisionId, $"Decision {decision.DecisionId}");
                sb.AppendLine($"  decision: {decisionName} [color={whisper}]card {decision.CardId} @ t{decision.Tick}[/color]");
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

    private static void AppendSeedMetric(StringBuilder sb, string label, float value, string whisper)
    {
        if (System.Math.Abs(value) <= 0.0001f)
        {
            return;
        }

        sb.AppendLine($"  {label}: [b]{value:F2}[/b] [color={whisper}]seed[/color]");
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

    private string ResolveEntityName(int entityId)
    {
        if (_projection?.Entities.TryGetValue(entityId, out var entity) == true)
        {
            return ResolveEntityName(entity);
        }

        return $"E{entityId}";
    }

    private string ResolveDisplayName(ProjectionSymbolKind kind, int id, string fallback) =>
        id > 0
            ? _symbols?.ResolveDisplayName(kind, id, fallback) ?? fallback
            : "none";

    private static string ColorTag(Color color) => Main.ColorHex(color);

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
        if (_content is null)
        {
            return;
        }

        DisplayServer.ClipboardSet(_content.Text);
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
