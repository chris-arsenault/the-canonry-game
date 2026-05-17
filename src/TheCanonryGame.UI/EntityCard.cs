#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000
#pragma warning disable CA2213
using System.Text;

using Engine.Kinds;
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Floating entity info card. Draggable, collapsible, closable.
/// </summary>
public partial class EntityCard : PanelContainer
{
    private const float DefaultWidth = 380f;
    private const float DefaultHeight = 460f;
    private const float MinimumWidth = 320f;
    private const float MinimumHeight = 220f;
    private const double BottomStickThreshold = 160d;

    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private int _entityId;
    private RichTextLabel? _content;
    private VBoxContainer? _body;
    private Label? _titleLabel;
    private ScrollContainer? _scroll;
    private double _pendingScrollValue;
    private double _pendingScrollDistanceFromBottom;
    private bool _pendingScrollStickToBottom;
    private bool _scrollRestoreQueued;
    private bool _collapsed;
    private bool _positionInitialized;
    private bool _dragging;

    [Signal]
    public delegate void CardClosedEventHandler(EntityCard card);

    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    public int EntityId => _entityId;

    public void Initialize(
        UiProjectionState? projection,
        IProjectionSymbolCatalog? symbols,
        int entityId,
        Vector2 position)
    {
        BindTo(projection, symbols, entityId, position);
    }

    public void BindTo(
        UiProjectionState? projection,
        IProjectionSymbolCatalog? symbols,
        int entityId,
        Vector2 initialPosition)
    {
        var changedEntity = _entityId != entityId;
        _projection = projection;
        _symbols = symbols;
        _entityId = entityId;

        if (!_positionInitialized || changedEntity)
        {
            Position = initialPosition;
            _positionInitialized = true;
        }

        Refresh();
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(MinimumWidth, MinimumHeight);
        Size = new Vector2(DefaultWidth, DefaultHeight);
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", UiTheme.FloatingPanelStyle());

        GetNode<VBoxContainer>("Outer").AddThemeConstantOverride("separation", UiTokens.Space2);
        GetNode<HBoxContainer>("Outer/TitleBar").AddThemeConstantOverride("separation", UiTokens.Space2);
        GetNode<ColorRect>("Outer/Hairline").Color = UiTokens.Hairline;

        _titleLabel = GetNode<Label>("Outer/TitleBar/TitleLabel");
        _body = GetNode<VBoxContainer>("Outer/Body");
        _scroll = GetNode<ScrollContainer>("Outer/Body/Scroll");
        _content = GetNode<RichTextLabel>("Outer/Body/Scroll/Content");

        _titleLabel.AddThemeFontOverride("font", UiTheme.DisplayFont);
        _titleLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        _titleLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);

        _content.AddThemeFontOverride("normal_font", UiTheme.BodyFont);
        _content.AddThemeFontOverride("mono_font", UiTheme.MonoFont);
        _content.AddThemeFontSizeOverride("normal_font_size", UiTokens.SizeBodySmall);
        _content.AddThemeFontSizeOverride("mono_font_size", UiTokens.SizeBodySmall);
        _content.AddThemeColorOverride("default_color", UiTokens.Vellum);
        _content.MetaClicked += OnContentMetaClicked;

        WireChromeButton("Outer/TitleBar/CopyButton", OnCopyPressed);
        WireChromeButton("Outer/TitleBar/CollapseButton", OnCollapsePressed);
        WireChromeButton("Outer/TitleBar/CloseButton", OnClosePressed);

        Modulate = UiTokens.WithAlpha(Colors.White, 0f);
        CreateTween().TweenProperty(this, "modulate", Colors.White, 0.12d);
        Refresh();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton)
        {
            _dragging = mouseButton.Pressed && mouseButton.Position.Y < 34f;
            if (_dragging || !mouseButton.Pressed)
            {
                GetViewport().SetInputAsHandled();
            }
        }
        else if (_dragging && @event is InputEventMouseMotion motion)
        {
            Position += motion.Relative;
            GetViewport().SetInputAsHandled();
        }
    }

    public void FadeOutAndQueueFree()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        var target = UiTokens.WithAlpha(Modulate, 0f);
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", target, 0.12d);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    public void Refresh()
    {
        if (_projection is null || _content is null || _titleLabel is null)
        {
            return;
        }

        if (!_projection.Entities.TryGetValue(_entityId, out var entity))
        {
            _titleLabel.Text = "Entity not found";
            _content.Text = string.Empty;
            return;
        }

        var entityName = ProjectionDisplay.EntityName(_symbols, entity);
        var kindName = ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Kind, entity.KindId, $"Kind {entity.KindId}");
        _titleLabel.Text = $"{entityName}  |  {kindName}";
        if (_collapsed)
        {
            return;
        }

        CaptureScrollAnchor();

        var sb = new StringBuilder();
        var secondary = ColorTag(UiTokens.Parchment);
        var whisper = ColorTag(UiTokens.Whisper);
        var subtype = ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Subtype, entity.SubtypeId, $"Subtype {entity.SubtypeId}");
        var culture = ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Culture, entity.CultureId, $"Culture {entity.CultureId}");
        var status = entity.StatusCode == EntityStatusCodes.Active ? "Active" : "Historical";
        sb.AppendLine($"[color={secondary}]{subtype}  |  {culture}  |  {status}[/color]");
        var currentTick = _projection.CurrentTick;
        var createdTick = entity.CreatedTick ?? 0;
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"[code][color={whisper}]({entity.PhysicalQ}, {entity.PhysicalR})  |  age {currentTick - createdTick}t[/color][/code]");

        sb.AppendLine();
        AppendHierarchy(sb, entity, whisper);
        sb.AppendLine();
        AppendAttributes(sb, entity, whisper);
        sb.AppendLine();
        AppendTraits(sb, entity, whisper);
        sb.AppendLine();
        AppendRelationships(sb, whisper);
        AppendActions(sb, entity, whisper);

        _content.Text = sb.ToString();
        QueueScrollRestore();
    }

    private void AppendAttributes(StringBuilder sb, UiEntityProjection entity, string whisper)
    {
        sb.AppendLine(SectionHeader("Attributes"));
        if (entity.Attributes.Count == 0)
        {
            sb.AppendLine($"  [color={whisper}]none[/color]");
            return;
        }

        foreach (var attr in entity.Attributes.Values.OrderBy(attr => attr.AttributeId))
        {
            var name = ProjectionDisplay.DisplayName(
                _symbols,
                ProjectionSymbolKind.Attribute,
                attr.AttributeId,
                $"Attribute {attr.AttributeId}");
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  {name}: [b]{attr.NewValue:F1}[/b] [color={whisper}]from {attr.OldValue:F1}[/color]");
        }
    }

    private void AppendTraits(StringBuilder sb, UiEntityProjection entity, string whisper)
    {
        sb.AppendLine(SectionHeader("Traits"));
        if (entity.Traits.Count == 0)
        {
            sb.AppendLine($"  [color={whisper}]none[/color]");
            return;
        }

        foreach (var traitId in entity.Traits.Order())
        {
            var name = ProjectionDisplay.DisplayName(
                _symbols,
                ProjectionSymbolKind.Trait,
                traitId,
                $"Trait {traitId}");
            sb.AppendLine($"  {name}");
        }
    }

    private void AppendHierarchy(StringBuilder sb, UiEntityProjection entity, string whisper)
    {
        _ = entity;
        sb.AppendLine(SectionHeader("Hierarchy"));

        var tier = _projection!.EntityTierOf(_entityId);
        if (tier == EntityTier.None)
        {
            sb.AppendLine($"  [color={whisper}](untiered)[/color]");
            return;
        }

        sb.AppendLine($"  [color={whisper}]Tier:[/color] {tier}");

        var (outgoing, incoming) = CollectVisibleRelationships();
        var affiliationsUp = outgoing.Where(rel => _projection!.IsAffiliationRelationship(rel.Key.RelationshipKindId)).ToList();
        var affiliationsDown = incoming.Where(rel => _projection!.IsAffiliationRelationship(rel.Key.RelationshipKindId)).ToList();
        var siblings = CollectSiblings(affiliationsUp);

        if (affiliationsUp.Count == 0 && affiliationsDown.Count == 0 && siblings.Count == 0)
        {
            sb.AppendLine($"  [color={whisper}]no affiliations[/color]");
            return;
        }

        AppendAffiliationGroup(sb, "Belongs to", affiliationsUp, whisper, outgoing: true);
        AppendSiblingsGroup(sb, siblings, whisper);
        AppendAffiliationGroup(sb, "Contains", affiliationsDown, whisper, outgoing: false);
    }

    private void AppendAffiliationGroup(
        StringBuilder sb,
        string label,
        List<UiRelationshipProjection> relationships,
        string whisper,
        bool outgoing)
    {
        if (relationships.Count == 0)
        {
            return;
        }

        sb.AppendLine($"  [color={whisper}]{label}:[/color]");
        foreach (var rel in relationships)
        {
            var otherId = outgoing ? rel.Key.TargetEntityId : rel.Key.SourceEntityId;
            var otherName = ProjectionDisplay.EntityName(_symbols, _projection, otherId);
            var relationshipName = ProjectionDisplay.DisplayName(
                _symbols,
                ProjectionSymbolKind.RelationshipKind,
                rel.Key.RelationshipKindId,
                $"Relationship {rel.Key.RelationshipKindId}");
            sb.AppendLine(
                System.Globalization.CultureInfo.InvariantCulture,
                $"    {relationshipName} -> [url={otherId}]{otherName}[/url] [color={whisper}]({rel.NewStrength:F2})[/color]");
        }
    }

    private void AppendSiblingsGroup(
        StringBuilder sb,
        IReadOnlyList<SiblingEntry> siblings,
        string whisper)
    {
        if (siblings.Count == 0)
        {
            return;
        }

        sb.AppendLine($"  [color={whisper}]Siblings:[/color]");
        foreach (var sibling in siblings)
        {
            var name = ProjectionDisplay.EntityName(_symbols, _projection, sibling.EntityId);
            sb.AppendLine(
                System.Globalization.CultureInfo.InvariantCulture,
                $"    via {sibling.SharedParentName} -> [url={sibling.EntityId}]{name}[/url]");
        }
    }

    private readonly record struct SiblingEntry(int EntityId, string SharedParentName);

    private List<SiblingEntry> CollectSiblings(List<UiRelationshipProjection> affiliationsUp)
    {
        var siblings = new List<SiblingEntry>();
        if (_projection is null)
        {
            return siblings;
        }

        var seen = new HashSet<int> { _entityId };
        foreach (var parentEdge in affiliationsUp)
        {
            var parentId = parentEdge.Key.TargetEntityId;
            var parentName = ProjectionDisplay.EntityName(_symbols, _projection, parentId);
            var parentInbound = _projection.ActiveIncomingRelationships(parentId);
            foreach (var inbound in parentInbound)
            {
                if (inbound.Key.RelationshipKindId != parentEdge.Key.RelationshipKindId)
                {
                    continue;
                }

                var siblingId = inbound.Key.SourceEntityId;
                if (!seen.Add(siblingId))
                {
                    continue;
                }

                siblings.Add(new SiblingEntry(siblingId, parentName));
                if (siblings.Count >= 6)
                {
                    return siblings;
                }
            }
        }

        return siblings;
    }

    private void OnContentMetaClicked(Variant meta)
    {
        var raw = meta.AsString();
        if (int.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var entityId))
        {
            EmitSignal(SignalName.EntitySelected, entityId);
        }
    }

    private void AppendRelationships(StringBuilder sb, string whisper)
    {
        sb.AppendLine(SectionHeader("Relationships"));

        var (outgoing, incoming) = CollectVisibleRelationships();
        if (outgoing.Count == 0 && incoming.Count == 0)
        {
            sb.AppendLine($"  [color={whisper}]none[/color]");
        }

        AppendOutgoingRelationships(sb, outgoing, whisper);
        AppendIncomingRelationships(sb, incoming, whisper);
    }

    private (List<UiRelationshipProjection> Outgoing, List<UiRelationshipProjection> Incoming) CollectVisibleRelationships()
    {
        if (EntityIsHistorical(_entityId))
        {
            return ([], []);
        }

        var outgoing = _projection!.ActiveOutgoingRelationships(_entityId).ToList();
        var incoming = _projection.ActiveIncomingRelationships(_entityId).ToList();
        outgoing.Sort(static (left, right) => left.Key.TargetEntityId.CompareTo(right.Key.TargetEntityId));
        incoming.Sort(static (left, right) => left.Key.SourceEntityId.CompareTo(right.Key.SourceEntityId));
        return (outgoing, incoming);
    }

    private void AppendOutgoingRelationships(
        StringBuilder sb,
        IEnumerable<UiRelationshipProjection> outgoing,
        string whisper)
    {
        foreach (var rel in outgoing)
        {
            var targetName = ProjectionDisplay.EntityName(_symbols, _projection, rel.Key.TargetEntityId);
            var relationshipName = ProjectionDisplay.DisplayName(
                _symbols,
                ProjectionSymbolKind.RelationshipKind,
                rel.Key.RelationshipKindId,
                $"Relationship {rel.Key.RelationshipKindId}");
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  -> {relationshipName} -> {targetName} [color={whisper}]({rel.NewStrength:F2})[/color]");
        }
    }

    private void AppendIncomingRelationships(
        StringBuilder sb,
        IEnumerable<UiRelationshipProjection> incoming,
        string whisper)
    {
        foreach (var rel in incoming)
        {
            var sourceName = ProjectionDisplay.EntityName(_symbols, _projection, rel.Key.SourceEntityId);
            var relationshipName = ProjectionDisplay.DisplayName(
                _symbols,
                ProjectionSymbolKind.RelationshipKind,
                rel.Key.RelationshipKindId,
                $"Relationship {rel.Key.RelationshipKindId}");
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  <- {relationshipName} <- {sourceName} [color={whisper}]({rel.NewStrength:F2})[/color]");
        }
    }

    private bool EntityIsHistorical(int entityId) =>
        !_projection!.Entities.TryGetValue(entityId, out var entity) ||
        entity.StatusCode != EntityStatusCodes.Active;

    private void AppendActions(StringBuilder sb, UiEntityProjection entity, string whisper)
    {
        sb.AppendLine(SectionHeader("Actions"));
        if (entity.LastGoalGeneration is null && entity.LastGambitExecution is null)
        {
            sb.AppendLine($"  [color={whisper}]none[/color]");
            return;
        }

        if (entity.LastGoalGeneration is { } goals)
        {
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  goals generated: {goals.GoalCount} [color={whisper}]actionable {goals.ActionableGoalCount} @ t{goals.Tick}[/color]");
        }

        if (entity.LastGambitExecution is { } gambit)
        {
            var gambitName = ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Gambit, gambit.GambitId, $"Gambit {gambit.GambitId}");
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  gambit: {gambitName} [color={whisper}]affected {gambit.AffectedEntityCount} @ t{gambit.Tick}[/color]");
        }
    }

    private static string SectionHeader(string title)
    {
        var caption = ColorTag(UiTokens.Parchment);
        return $"\n[color={caption}]{title.ToUpperInvariant()}[/color]";
    }

    private static string ColorTag(Color color) => UiFormat.ColorHex(color);

    private void OnCollapsePressed()
    {
        _collapsed = !_collapsed;
        if (_body is not null)
        {
            _body.Visible = !_collapsed;
        }
    }

    private void OnClosePressed() => EmitSignal(SignalName.CardClosed, this);

    private void OnCopyPressed()
    {
        if (_content is not null)
        {
            DisplayServer.ClipboardSet(_content.Text);
        }
    }

    private void CaptureScrollAnchor()
    {
        var bar = _scroll?.GetVScrollBar();
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
        var bar = _scroll?.GetVScrollBar();
        if (bar is null)
        {
            return;
        }

        var maxScroll = GetMaxScroll(bar);
        var target = _pendingScrollStickToBottom
            ? System.Math.Max(0d, maxScroll - _pendingScrollDistanceFromBottom)
            : System.Math.Min(_pendingScrollValue, maxScroll);
        _scroll!.ScrollVertical = (int)System.Math.Round(target, MidpointRounding.ToEven);
    }

    private void WireChromeButton(string path, Action onPressed)
    {
        var button = GetNode<Button>(path);
        button.AddThemeFontOverride("font", UiTheme.BodyFont);
        button.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        button.Pressed += onPressed;
    }

    private static double GetMaxScroll(VScrollBar bar) =>
        System.Math.Max(0d, bar.MaxValue - bar.Page);
}
