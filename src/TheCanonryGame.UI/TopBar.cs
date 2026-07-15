#pragma warning disable CA1303 // UI copy is local to the game shell.
#pragma warning disable CA1305
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class TopBar : PanelContainer
{
    private Label? _titleLabel;
    private Label? _subtitleLabel;
    private Label? _tickLabel;

    [Signal]
    public delegate void PauseRequestedEventHandler();

    [Signal]
    public delegate void StepRequestedEventHandler();

    [Signal]
    public delegate void SpeedRequestedEventHandler(double ticksPerSecond);

    [Signal]
    public delegate void ResetRequestedEventHandler();

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 48);
        AddThemeStyleboxOverride("panel", UiTheme.ChromeBarStyle());

        _titleLabel = GetNode<Label>("Row/TitleColumn/TitleLabel");
        _subtitleLabel = GetNode<Label>("Row/TitleColumn/SubtitleLabel");
        _tickLabel = GetNode<Label>("Row/TickLabel");

        GetNode<HBoxContainer>("Row").AddThemeConstantOverride("separation", UiTokens.Space4);
        GetNode<VBoxContainer>("Row/TitleColumn").AddThemeConstantOverride("separation", 0);
        GetNode<HBoxContainer>("Row/Controls").AddThemeConstantOverride("separation", UiTokens.Space2);

        _titleLabel.AddThemeFontOverride("font", UiTheme.DisplayFont);
        _titleLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleSmall);
        _titleLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);

        _subtitleLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _subtitleLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        _subtitleLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);

        _tickLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
        _tickLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBody);
        _tickLabel.AddThemeColorOverride("font_color", UiTokens.Parchment);

        WireButton("Row/Controls/PauseButton", () => EmitSignal(SignalName.PauseRequested));
        WireButton("Row/Controls/StepButton", () => EmitSignal(SignalName.StepRequested));
        WireButton("Row/Controls/NormalSpeedButton", () => EmitSignal(SignalName.SpeedRequested, 1d));
        WireButton("Row/Controls/FastSpeedButton", () => EmitSignal(SignalName.SpeedRequested, 5d));
        WireButton("Row/Controls/VeryFastSpeedButton", () => EmitSignal(SignalName.SpeedRequested, 10d));
        WireButton("Row/Controls/ResetButton", () => EmitSignal(SignalName.ResetRequested));
    }

    public void SetStatus(
        int tick,
        string timeState,
        int entityCount,
        int generatorFireCount,
        int seed,
        int seedAttempt,
        string? agesLabel = null)
    {
        if (_tickLabel is null)
        {
            return;
        }

        var ages = string.IsNullOrEmpty(agesLabel) ? string.Empty : $"  |  {agesLabel}";
        _tickLabel.Text =
            $"t{tick,-5}  |  {timeState,-8}  |  {entityCount,3} entities  |  {generatorFireCount,3} fires  |  seed {seed}/{seedAttempt}{ages}";
    }

    private void WireButton(string path, Action onPressed)
    {
        var button = GetNode<Button>(path);
        button.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        button.Pressed += onPressed;
    }
}
