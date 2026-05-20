#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class Entity : Node2D
{
    private Sprite2D? _sprite;
    private Label? _nameLabel;
    private CollisionShape2D? _collisionShape;
    private Tween? _moveTween;
    private Tween? _fadeTween;
    private string _kindStableName = string.Empty;
    private Color _accent = Colors.White;
    private Color _cultureAccent = UiTokens.CultureNone;
    private float _glyphSize = 10f;
    private float _actionIntensity;
    private bool _hasTexture;
    private bool _isHistorical;
    private bool _isHighlighted;
    private bool _positionInitialized;
    private int _entityId;
    private int _cultureSlotCount;
    private int _cultureSlotPolarityCode;

    [Signal]
    public delegate void SelectedEventHandler(int entityIdValue);

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
        _nameLabel = GetNode<Label>("NameLabel");
        _collisionShape = GetNode<CollisionShape2D>("PickArea/CollisionShape");

        GetNode<Area2D>("PickArea").InputEvent += OnPickAreaInputEvent;

        _nameLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _nameLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeCaption);
        _nameLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _nameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    public void BindTo(
        UiEntityProjection entity,
        IProjectionSymbolCatalog? symbols,
        bool showName,
        int currentTick,
        bool isHighlighted = false)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _entityId = entity.EntityId;
        _isHighlighted = isHighlighted;
        _kindStableName = ProjectionDisplay.StableName(symbols, ProjectionSymbolKind.Kind, entity.KindId, $"kind-{entity.KindId}");
        var cultureStableName = ProjectionDisplay.StableName(symbols, ProjectionSymbolKind.Culture, entity.CultureId, "none");
        var subtypeStableName = ProjectionDisplay.NullableStableName(symbols, ProjectionSymbolKind.Subtype, entity.SubtypeId);
        _accent = UiTokens.KindAccent(_kindStableName);
        _cultureAccent = UiTokens.CultureColor(cultureStableName);
        _actionIntensity = RecentActionIntensity(entity, currentTick);
        _glyphSize = _kindStableName == "settlement" ? 14f : 10f;
        _cultureSlotCount = entity.CultureSlotCount;
        _cultureSlotPolarityCode = entity.CultureSlotPolarityCode;

        BindTexture(subtypeStableName);
        BindLabel(ProjectionDisplay.EntityName(symbols, entity), showName);
        BindShape();
        BindHistorical(entity.StatusCode != EntityStatusCodes.Active);
        QueueRedraw();
    }

    public void MoveTo(Vector2 worldPosition)
    {
        if (!_positionInitialized)
        {
            Position = worldPosition;
            _positionInitialized = true;
            return;
        }

        if (Position.DistanceSquaredTo(worldPosition) < 0.01f)
        {
            return;
        }

        _moveTween?.Kill();
        _moveTween = CreateTween();
        _moveTween.TweenProperty(this, "position", worldPosition, 0.25d);
    }

    public override void _Draw()
    {
        var tokenRadius = _glyphSize * (_isHighlighted ? 2.45f : 2.02f);
        DrawCircle(Vector2.Zero, tokenRadius, new Color(UiTokens.Ink.R, UiTokens.Ink.G, UiTokens.Ink.B, _isHighlighted ? 0.56f : 0.34f));
        DrawArc(Vector2.Zero, tokenRadius, 0f, Mathf.Tau, 64, new Color(_cultureAccent, _isHighlighted ? 0.82f : 0.58f), 1.35f, antialiased: true);

        if (_cultureSlotCount > 0)
        {
            var slotColor = SlotPolarityColor(_cultureSlotPolarityCode);
            DrawSlotPips(tokenRadius + (_glyphSize * 0.18f), slotColor);
        }

        if (_actionIntensity > 0f)
        {
            var actionRadius = tokenRadius + (_glyphSize * 0.32f);
            var alpha = 0.16f + (_actionIntensity * 0.34f);
            DrawArc(Vector2.Zero, actionRadius, -Mathf.Pi / 2f, Mathf.Tau * 0.72f, 56, new Color(UiTokens.Gilt, alpha), 1.8f, antialiased: true);
        }

        if (_isHighlighted)
        {
            var radius = tokenRadius + (_glyphSize * 0.58f);
            DrawCircle(Vector2.Zero, radius + 1.5f, new Color(UiTokens.Ink.R, UiTokens.Ink.G, UiTokens.Ink.B, 0.62f));
            DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 72, UiTokens.Gilt, 2.6f, antialiased: true);
        }

        if (!_hasTexture)
        {
            var glyphSize = _isHighlighted ? _glyphSize * 1.28f : _glyphSize;
            KindGlyph.Draw(this, _kindStableName, Vector2.Zero, glyphSize, _accent);
        }

        DrawCultureBadge(tokenRadius);
    }

    private void DrawSlotPips(float slotRadius, Color slotColor)
    {
        var pipCount = Math.Min(_cultureSlotCount, 4);
        for (var i = 0; i < pipCount; i++)
        {
            var angle = (-Mathf.Pi / 2f) + ((i - ((pipCount - 1) * 0.5f)) * 0.24f);
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * slotRadius;
            DrawCircle(offset, 1.95f, UiTokens.Ink);
            DrawCircle(offset, 1.25f, slotColor);
        }
    }

    private void DrawCultureBadge(float tokenRadius)
    {
        var offset = new Vector2(tokenRadius * 0.68f, tokenRadius * 0.70f);
        var badgeRadius = _isHighlighted ? 4.6f : 3.6f;
        DrawCircle(offset, badgeRadius + 1.2f, UiTokens.Ink);
        DrawCircle(offset, badgeRadius, new Color(_cultureAccent, 0.94f));
    }

    private static float RecentActionIntensity(UiEntityProjection entity, int currentTick)
    {
        var lastActionTick = -1;
        if (entity.LastMove is { } move)
        {
            lastActionTick = Math.Max(lastActionTick, move.Tick);
        }

        if (entity.LastGambitExecution is { } gambit)
        {
            lastActionTick = Math.Max(lastActionTick, gambit.Tick);
        }

        if (entity.CreatedByGeneratorTick is { } createdTick)
        {
            lastActionTick = Math.Max(lastActionTick, createdTick);
        }

        if (lastActionTick < 0)
        {
            return 0f;
        }

        var age = Math.Max(0, currentTick - lastActionTick);
        return age > 80 ? 0f : 1f - (age / 80f);
    }

    private static Color SlotPolarityColor(int polarityCode) => polarityCode switch
    {
        RelationshipPolarityCodes.Positive => UiTokens.Aurora,
        RelationshipPolarityCodes.Negative => UiTokens.Blood,
        RelationshipPolarityCodes.Neutral => UiTokens.Gilt,
        _ => UiTokens.Parchment,
    };

    private void BindTexture(string? subtypeStableName)
    {
        if (_sprite is null)
        {
            return;
        }

        var texture = AssetLibrary.SubtypeMapIcon(_kindStableName, subtypeStableName)
            ?? AssetLibrary.KindSprite(_kindStableName);
        _hasTexture = texture is not null;
        _sprite.Visible = _hasTexture;
        _sprite.Texture = texture;

        if (texture is not null)
        {
            var targetDiameter = _glyphSize * (_isHighlighted ? 6.1f : 4.8f);
            var textureSize = texture.GetSize();
            if (textureSize.X > 0f && textureSize.Y > 0f)
            {
                _sprite.Scale = new Vector2(targetDiameter / textureSize.X, targetDiameter / textureSize.Y);
            }
        }
    }

    private void BindLabel(string entityName, bool showName)
    {
        if (_nameLabel is null)
        {
            return;
        }

        _nameLabel.Text = entityName;
        _nameLabel.Visible = showName || _isHighlighted;
        _nameLabel.Size = new Vector2(160, 20);
        _nameLabel.Position = new Vector2(-80, -_glyphSize - (_isHighlighted ? 34f : 26f));
    }

    private void BindShape()
    {
        if (_collisionShape is null)
        {
            return;
        }

        if (_collisionShape.Shape is not CircleShape2D circle)
        {
            circle = new CircleShape2D();
            _collisionShape.Shape = circle;
        }

        circle.Radius = _glyphSize * (_isHighlighted ? 3.6f : 2.8f);
    }

    private void BindHistorical(bool isHistorical)
    {
        if (_isHistorical == isHistorical)
        {
            return;
        }

        _isHistorical = isHistorical;
        var target = UiTokens.WithAlpha(Colors.White, isHistorical ? 0.3f : 1f);
        _fadeTween?.Kill();
        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate", target, 0.4d);
    }

    private void OnPickAreaInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        _ = viewport;
        _ = shapeIdx;

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            EmitSignal(SignalName.Selected, _entityId);
            GetViewport().SetInputAsHandled();
        }
    }
}
