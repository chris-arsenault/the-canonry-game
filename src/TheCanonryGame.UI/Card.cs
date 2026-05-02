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
    private Label? _typeBadgeLabel;
    private Label? _nameLabel;
    private TextureRect? _art;
    private Label? _subtitleLabel;
    private Label? _descriptionLabel;
    private Label? _statusLabel;
    private Button? _toggleButton;
    private PanelContainer? _typeBadge;
    private PanelContainer? _artFrame;
    private ColorRect? _hRule;
    private int _runtimeIdValue;

    [Signal]
    public delegate void ToggleRequestedEventHandler(int cardRuntimeIdValue);

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        SizeFlagsVertical = SizeFlags.ShrinkBegin;

        var body = GetNode<VBoxContainer>("Body");
        body.AddThemeConstantOverride("separation", UiTokens.Space2);

        var headerRow = GetNode<HBoxContainer>("Body/HeaderRow");
        headerRow.AddThemeConstantOverride("separation", UiTokens.Space2);

        _typeBadge = GetNode<PanelContainer>("Body/HeaderRow/TypeBadge");
        _typeBadgeLabel = GetNode<Label>("Body/HeaderRow/TypeBadge/TypeBadgeLabel");
        _typeBadgeLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _typeBadgeLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);

        _nameLabel = GetNode<Label>("Body/NameLabel");
        _nameLabel.AddThemeFontOverride("font", UiTheme.DisplayFont);
        _nameLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleSmall);
        _nameLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);

        _artFrame = GetNode<PanelContainer>("Body/ArtFrame");
        _artFrame.AddThemeStyleboxOverride("panel", UiTheme.InsetStyle());
        _art = GetNode<TextureRect>("Body/ArtFrame/Art");

        _subtitleLabel = GetNode<Label>("Body/SubtitleLabel");
        _subtitleLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _subtitleLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        _subtitleLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);

        _descriptionLabel = GetNode<Label>("Body/DescriptionLabel");
        _descriptionLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _descriptionLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _descriptionLabel.AddThemeColorOverride("font_color", UiTokens.Parchment);

        _hRule = GetNode<ColorRect>("Body/HRule");
        _hRule.Color = UiTokens.Hairline;

        var footerRow = GetNode<HBoxContainer>("Body/FooterRow");
        footerRow.AddThemeConstantOverride("separation", UiTokens.Space2);

        _statusLabel = GetNode<Label>("Body/FooterRow/StatusLabel");
        _statusLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
        _statusLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        _statusLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);

        _toggleButton = GetNode<Button>("Body/FooterRow/ToggleButton");
        _toggleButton.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _toggleButton.Pressed += () => EmitSignal(SignalName.ToggleRequested, _runtimeIdValue);
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        _ = atPosition;
        return _runtimeIdValue;
    }

    internal void BindTo(CardInstance card, GameTableauCardState state)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(state);

        _runtimeIdValue = card.Definition.RuntimeId.Value;
        var accent = CardAccent(card);
        AddThemeStyleboxOverride("panel", UiTheme.CardStyle(accent));

        BindHeader(card, accent);
        BindBody(card, state);
        BindFooter(card, state);
    }

    public void Flash()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1.4f, 1.4f, 1.4f, 1f), 0.06d);
        tween.TweenProperty(this, "modulate", Colors.White, 0.18d);
    }

    private void BindHeader(CardInstance card, Color accent)
    {
        if (_typeBadge is not null)
        {
            _typeBadge.AddThemeStyleboxOverride("panel", UiTheme.BadgeStyle(accent));
        }

        if (_typeBadgeLabel is not null)
        {
            _typeBadgeLabel.Text = TypeLabelText(card.Definition.Type);
            _typeBadgeLabel.AddThemeColorOverride("font_color", accent);
        }
    }

    private void BindBody(CardInstance card, GameTableauCardState state)
    {
        _ = state;
        if (_nameLabel is not null)
        {
            _nameLabel.Text = card.Definition.Name;
        }

        if (_art is not null)
        {
            _art.Texture = ResolveFace(card);
        }

        if (_subtitleLabel is not null)
        {
            _subtitleLabel.Text = SubtitleText(card);
            _subtitleLabel.Visible = !string.IsNullOrEmpty(_subtitleLabel.Text);
        }

        if (_descriptionLabel is not null)
        {
            _descriptionLabel.Text = DescriptionText(card);
        }
    }

    private void BindFooter(CardInstance card, GameTableauCardState state)
    {
        if (_statusLabel is not null)
        {
            _statusLabel.Text = StatusFooter(state);
        }

        if (_toggleButton is not null)
        {
            var toggleable = state.GeneratorId > 0;
            _toggleButton.Visible = toggleable;
            _toggleButton.Disabled = !toggleable;
            _toggleButton.Text = state.IsEnabled ? "Disable" : "Enable";
            _toggleButton.TooltipText = $"Toggle {card.Definition.Name}";
        }
    }

    private static string TypeLabelText(CardType type) => type switch
    {
        CardType.Generator => "GENERATOR",
        CardType.Action => "GAMBIT",
        CardType.System => "SYSTEM",
        _ => "CARD",
    };

    private static string SubtitleText(CardInstance card)
    {
        var kindHint = ResolveKindHint(card);
        var typeWord = card.Definition.Type == CardType.Generator ? "generator" : "gambit";
        return string.IsNullOrEmpty(kindHint)
            ? typeWord
            : $"{kindHint} · {typeWord}";
    }

    private static string DescriptionText(CardInstance card)
    {
        var description = card.Definition.Description?.Trim();
        return string.IsNullOrEmpty(description)
            ? "(no description recorded)"
            : description;
    }

    private static string StatusFooter(GameTableauCardState state)
    {
        var fired = state.TotalFired;
        if (state.LastFireTick is { } tick)
        {
            return $"fired {fired} · t{tick}";
        }

        return $"fired {fired}";
    }

    private static Color CardAccent(CardInstance card) =>
        card.Definition.Type switch
        {
            CardType.Generator => UiTokens.Aurora,
            CardType.Action => UiTokens.Ember,
            CardType.System => UiTokens.Ice,
            _ => UiTokens.KindDefault,
        };

    private static Texture2D? ResolveFace(CardInstance card)
    {
        var kindHint = ResolveKindHint(card);
        if (!string.IsNullOrEmpty(kindHint))
        {
            var byKind = AssetLibrary.CardFace(kindHint);
            if (byKind is not null)
            {
                return byKind;
            }
        }

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

    private static string? ResolveKindHint(CardInstance card)
    {
        foreach (var tag in card.Definition.Tags)
        {
            if (tag.StartsWith("kind:", StringComparison.Ordinal))
            {
                return tag["kind:".Length..];
            }
        }

        return null;
    }
}
