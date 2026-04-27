#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class ChroniclePanel : PanelContainer
{
    private RichTextLabel? _eventLogLabel;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(340, 0);
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeStyleboxOverride("panel", UiTheme.RailPanelStyle(onLeft: false));

        GetNode<VBoxContainer>("Outer").AddThemeConstantOverride("separation", UiTokens.Space2);

        var title = GetNode<Label>("Outer/TitleLabel");
        title.AddThemeFontOverride("font", UiTheme.DisplayFont);
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        title.AddThemeColorOverride("font_color", UiTokens.Vellum);

        var subtitle = GetNode<Label>("Outer/SubtitleLabel");
        subtitle.AddThemeFontOverride("font", UiTheme.BodyFont);
        subtitle.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        subtitle.AddThemeColorOverride("font_color", UiTokens.Whisper);

        GetNode<ColorRect>("Outer/Hairline").Color = UiTokens.Hairline;

        _eventLogLabel = GetNode<RichTextLabel>("Outer/Scroll/EventLogLabel");
        _eventLogLabel.AddThemeFontOverride("normal_font", UiTheme.BodyFont);
        _eventLogLabel.AddThemeFontOverride("mono_font", UiTheme.MonoFont);
        _eventLogLabel.AddThemeFontSizeOverride("normal_font_size", UiTokens.SizeBodySmall);
        _eventLogLabel.AddThemeFontSizeOverride("mono_font_size", UiTokens.SizeBodySmall);
        _eventLogLabel.AddThemeColorOverride("default_color", UiTokens.Vellum);
    }

    public void SetLines(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (_eventLogLabel is not null)
        {
            _eventLogLabel.Text = string.Join('\n', lines);
        }
    }

    public void Clear()
    {
        if (_eventLogLabel is not null)
        {
            _eventLogLabel.Text = string.Empty;
        }
    }
}
