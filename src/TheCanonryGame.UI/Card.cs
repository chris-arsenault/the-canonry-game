#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using TheCanonryGame.Game.Cards;
using TheCanonryGame.Game.Tableau;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class Card : PanelContainer
{
    private Label? _nameLabel;
    private Label? _statusLabel;
    private Button? _toggleButton;
    private TextureRect? _face;
    private int _runtimeIdValue;

    [Signal]
    public delegate void ToggleRequestedEventHandler(int cardRuntimeIdValue);

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        GetNode<HBoxContainer>("Row").AddThemeConstantOverride("separation", UiTokens.Space3);
        GetNode<VBoxContainer>("Row/NameColumn").AddThemeConstantOverride("separation", 0);

        _face = GetNode<TextureRect>("Row/Face");
        _nameLabel = GetNode<Label>("Row/NameColumn/NameLabel");
        _statusLabel = GetNode<Label>("Row/NameColumn/StatusLabel");
        _toggleButton = GetNode<Button>("Row/ToggleButton");

        _nameLabel.AddThemeFontOverride("font", UiTheme.DisplayFont);
        _nameLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleSmall);
        _nameLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);

        _statusLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _statusLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _statusLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);

        _toggleButton.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _toggleButton.Pressed += () => EmitSignal(SignalName.ToggleRequested, _runtimeIdValue);
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        _ = atPosition;
        return _runtimeIdValue;
    }

    public void BindTo(CardInstance card, GameTableauCardState state)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(state);

        _runtimeIdValue = card.Definition.RuntimeId.Value;
        AddThemeStyleboxOverride("panel", UiTheme.CardStyle(CardAccent(card)));

        if (_nameLabel is not null)
        {
            _nameLabel.Text = card.Definition.Name;
        }

        if (_face is not null)
        {
            _face.Texture = ResolveFace(card);
            _face.Visible = _face.Texture is not null;
        }

        if (_statusLabel is not null)
        {
            _statusLabel.Text = FormatStatus(card, state);
        }

        if (_toggleButton is not null)
        {
            _toggleButton.Disabled = state.GeneratorId <= 0;
            _toggleButton.Text = state.IsEnabled ? "Disable" : "Enable";
            _toggleButton.TooltipText = $"Toggle {card.Definition.Name}";
        }
    }

    public void Flash()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1.4f, 1.4f, 1.4f, 1f), 0.06d);
        tween.TweenProperty(this, "modulate", Colors.White, 0.18d);
    }

    private static string FormatStatus(CardInstance card, GameTableauCardState state)
    {
        var status = state.IsEnabled ? "enabled" : "disabled";
        var known = state.IsKnown ? status : "default";
        var type = card.Definition.Type switch
        {
            CardType.Generator => "generator",
            CardType.System => "system",
            CardType.Action => "action",
            _ => "card",
        };
        return $"{type}  |  {known}  |  fired {state.TotalFired}";
    }

    private static Color CardAccent(CardInstance card) =>
        card.Definition.Type switch
        {
            CardType.Generator => UiTokens.KindDefault,
            CardType.System => UiTokens.Ice,
            CardType.Action => UiTokens.Ember,
            _ => UiTokens.KindDefault,
        };

    private static Texture2D? ResolveFace(CardInstance card)
    {
        foreach (var tag in card.Definition.Tags)
        {
            var texture = AssetLibrary.CardFace(tag);
            if (texture is not null)
            {
                return texture;
            }
        }

        return AssetLibrary.CardBack;
    }
}
