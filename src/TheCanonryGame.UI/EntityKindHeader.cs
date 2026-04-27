#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class EntityKindHeader : VBoxContainer
{
    private Control? _topGap;
    private Label? _nameLabel;
    private Label? _countLabel;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 0);
        _topGap = GetNode<Control>("TopGap");
        GetNode<HBoxContainer>("Row").AddThemeConstantOverride("separation", UiTokens.Space2);

        _nameLabel = GetNode<Label>("Row/NameLabel");
        _countLabel = GetNode<Label>("Row/CountLabel");

        _nameLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _nameLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);

        _countLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
        _countLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        _countLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);
    }

    public void BindTo(string displayName, Color accent, int count, bool hasLeadingGap)
    {
        ArgumentNullException.ThrowIfNull(displayName);

        if (_topGap is not null)
        {
            _topGap.CustomMinimumSize = new Vector2(0, hasLeadingGap ? UiTokens.Space3 : 0);
        }

        if (_nameLabel is not null)
        {
            _nameLabel.Text = displayName.ToUpperInvariant();
            _nameLabel.AddThemeColorOverride("font_color", accent);
        }

        if (_countLabel is not null)
        {
            _countLabel.Text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
