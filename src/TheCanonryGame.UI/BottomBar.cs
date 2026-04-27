#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class BottomBar : PanelContainer
{
    private Button? _tableauToggleButton;
    private Button? _readingsToggleButton;
    private Label? _hintLabel;

    [Signal]
    public delegate void TableauToggleRequestedEventHandler();

    [Signal]
    public delegate void ReadingsToggleRequestedEventHandler();

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 40);
        AddThemeStyleboxOverride("panel", UiTheme.ChromeBarStyle(bottom: true));

        _tableauToggleButton = GetNode<Button>("Row/TableauToggleButton");
        _readingsToggleButton = GetNode<Button>("Row/ReadingsToggleButton");
        _hintLabel = GetNode<Label>("Row/HintLabel");

        GetNode<HBoxContainer>("Row").AddThemeConstantOverride("separation", UiTokens.Space2);
        StyleButton(_tableauToggleButton);
        StyleButton(_readingsToggleButton);

        _tableauToggleButton.Pressed += () => EmitSignal(SignalName.TableauToggleRequested);
        _readingsToggleButton.Pressed += () => EmitSignal(SignalName.ReadingsToggleRequested);

        _hintLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _hintLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        _hintLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);
    }

    public void SetTableauVisible(bool visible)
    {
        if (_tableauToggleButton is not null)
        {
            _tableauToggleButton.Text = visible ? "Hide Tableau" : "Tableau";
        }
    }

    public void SetReadingsVisible(bool visible)
    {
        if (_readingsToggleButton is not null)
        {
            _readingsToggleButton.Text = visible ? "Hide Readings" : "Readings";
        }
    }

    private static void StyleButton(Button button)
    {
        button.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
    }
}
