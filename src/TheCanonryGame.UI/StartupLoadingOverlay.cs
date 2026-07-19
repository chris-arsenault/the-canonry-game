#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
namespace TheCanonryGame.UI;

using Godot;

using TheCanonryGame.UI.Theme;

public partial class StartupLoadingOverlay : Control
{
    private static readonly string[] ActivityFrames = ["·", "··", "···", "····"];

    private Label? _statusLabel;
    private Label? _elapsedLabel;
    private string? _displayedStatus;
    private int _displayedActivityFrame = -1;
    private int _displayedSecond = -1;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var backdrop = new ColorRect
        {
            Color = UiTokens.WithAlpha(UiTokens.Basalt, 0.97f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        center.AddChild(BuildLoadingPanel());
    }

    private PanelContainer BuildLoadingPanel()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(520, 220),
        };
        panel.AddThemeStyleboxOverride("panel", UiTheme.FloatingPanelStyle());

        var content = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        content.AddThemeConstantOverride("separation", UiTokens.Space3);
        panel.AddChild(content);

        AddHeadline(content);
        AddProgressLabels(content);
        return panel;
    }

    private static void AddHeadline(VBoxContainer content)
    {
        var title = new Label
        {
            Text = "THE ICE REMEMBERS",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontOverride("font", UiTheme.DisplayFont);
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        title.AddThemeColorOverride("font_color", UiTokens.Vellum);
        content.AddChild(title);

        var subtitle = new Label
        {
            Text = "assembling a world beneath the thaw",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        subtitle.AddThemeFontOverride("font", UiTheme.BodyFont);
        subtitle.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        subtitle.AddThemeColorOverride("font_color", UiTokens.Whisper);
        content.AddChild(subtitle);
    }

    private void AddProgressLabels(VBoxContainer content)
    {
        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _statusLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _statusLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBody);
        _statusLabel.AddThemeColorOverride("font_color", UiTokens.Aurora);
        content.AddChild(_statusLabel);

        _elapsedLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _elapsedLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
        _elapsedLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeCaption);
        _elapsedLabel.AddThemeColorOverride("font_color", UiTokens.Parchment);
        content.AddChild(_elapsedLabel);
    }

    public void ShowLoading(string status, double elapsedSeconds = 0d)
    {
        Visible = true;
        MouseFilter = MouseFilterEnum.Stop;
        UpdateStatus(status, elapsedSeconds);
    }

    public void UpdateStatus(string status, double elapsedSeconds)
    {
        if (_statusLabel is null || _elapsedLabel is null)
        {
            return;
        }

        var elapsedSecond = Math.Max(0, (int)elapsedSeconds);
        var frame = elapsedSecond % ActivityFrames.Length;
        if (!string.Equals(_displayedStatus, status, StringComparison.Ordinal) ||
            _displayedActivityFrame != frame)
        {
            _statusLabel.Text = $"{status} {ActivityFrames[frame]}";
            _displayedStatus = status;
            _displayedActivityFrame = frame;
        }

        if (_displayedSecond != elapsedSecond)
        {
            _elapsedLabel.Text = $"{elapsedSecond}s elapsed";
            _displayedSecond = elapsedSecond;
        }
    }

    public void ShowFailure(string message, double elapsedSeconds)
    {
        Visible = true;
        MouseFilter = MouseFilterEnum.Stop;
        if (_statusLabel is not null)
        {
            _statusLabel.Text = "World startup failed";
            _statusLabel.AddThemeColorOverride("font_color", UiTokens.Blood);
        }

        if (_elapsedLabel is not null)
        {
            _elapsedLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _elapsedLabel.Text = $"{message}\n{elapsedSeconds:0.0}s elapsed — see console for details";
        }
    }

    public void HideLoading()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
    }
}
