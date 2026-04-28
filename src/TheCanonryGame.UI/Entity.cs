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
    private float _glyphSize = 10f;
    private bool _hasTexture;
    private bool _isHistorical;
    private bool _positionInitialized;
    private int _entityId;

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

    public void BindTo(UiEntityProjection entity, IProjectionSymbolCatalog? symbols, bool showName)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _entityId = entity.EntityId;
        _kindStableName = ProjectionDisplay.StableName(symbols, ProjectionSymbolKind.Kind, entity.KindId, $"kind-{entity.KindId}");
        var subtypeStableName = ProjectionDisplay.NullableStableName(symbols, ProjectionSymbolKind.Subtype, entity.SubtypeId);
        _accent = UiTokens.KindAccent(_kindStableName);
        _glyphSize = _kindStableName == "settlement" ? 14f : 10f;

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
        if (!_hasTexture)
        {
            KindGlyph.Draw(this, _kindStableName, Vector2.Zero, _glyphSize, _accent);
        }
    }

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
            var targetDiameter = _glyphSize * 4.8f;
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
        _nameLabel.Visible = showName;
        _nameLabel.Size = new Vector2(160, 20);
        _nameLabel.Position = new Vector2(-80, -_glyphSize - 26f);
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

        circle.Radius = _glyphSize * 2.8f;
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
