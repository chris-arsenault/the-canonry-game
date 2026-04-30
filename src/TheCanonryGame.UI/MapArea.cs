#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class MapArea : Control
{
    private Button? _planeToggleButton;
    private Button? _influenceToggleButton;
    private Control? _legendPanel;

    [Signal]
    public delegate void PlaneToggleRequestedEventHandler();

    [Signal]
    public delegate void InfluenceToggleRequestedEventHandler();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        _planeToggleButton = GetNode<Button>("PlaneToggleButton");
        _influenceToggleButton = GetNode<Button>("InfluenceToggleButton");
        _legendPanel = GetNode<Control>("LegendPanel");

        _planeToggleButton.CustomMinimumSize = new Vector2(150, 30);
        _planeToggleButton.TooltipText = "Toggle physical / semantic plane";
        _planeToggleButton.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _planeToggleButton.Pressed += () => EmitSignal(SignalName.PlaneToggleRequested);

        _influenceToggleButton.CustomMinimumSize = new Vector2(150, 30);
        _influenceToggleButton.TooltipText = "Toggle influence overlay";
        _influenceToggleButton.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _influenceToggleButton.Pressed += () => EmitSignal(SignalName.InfluenceToggleRequested);

        PositionOverlays();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            PositionOverlays();
        }
    }

    public void SetPlane(PlaneMode plane)
    {
        if (_planeToggleButton is null)
        {
            return;
        }

        _planeToggleButton.Text = plane == PlaneMode.Physical
            ? "Physical"
            : "Semantic";
    }

    public void SetInfluenceVisible(bool visible)
    {
        if (_influenceToggleButton is null)
        {
            return;
        }

        _influenceToggleButton.Text = visible
            ? "Influence On"
            : "Influence Off";
    }

    private void PositionOverlays()
    {
        if (_planeToggleButton is not null)
        {
            _planeToggleButton.Position = new Vector2(UiTokens.Space4, Size.Y - UiTokens.Space6 - UiTokens.Space2);
        }

        if (_influenceToggleButton is not null)
        {
            _influenceToggleButton.Position = new Vector2(
                UiTokens.Space4,
                Size.Y - UiTokens.Space6 - UiTokens.Space2 - 34f);
        }

        if (_legendPanel is not null)
        {
            var legendSize = _legendPanel.GetCombinedMinimumSize();
            _legendPanel.Position = new Vector2(
                Size.X - legendSize.X - UiTokens.Space4,
                Size.Y - legendSize.Y - UiTokens.Space6 - UiTokens.Space2);
        }
    }
}
