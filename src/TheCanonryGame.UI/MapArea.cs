#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class MapArea : Control
{
    public const string LayerRoads = "roads";
    public const string LayerMapFeatures = "map_features";
    public const string LayerInfluence = "influence";

    private Button? _planeToggleButton;
    private Button? _influenceToggleButton;
    private PanelContainer? _layersPanel;
    private CheckBox? _roadsLayerCheckBox;
    private CheckBox? _featuresLayerCheckBox;
    private CheckBox? _influenceLayerCheckBox;
    private Control? _legendPanel;
    private bool _updatingLayerControls;

    [Signal]
    public delegate void PlaneToggleRequestedEventHandler();

    [Signal]
    public delegate void MapLayerVisibilityChangedEventHandler(string layerKey, bool visible);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        _planeToggleButton = GetNode<Button>("PlaneToggleButton");
        _influenceToggleButton = GetNodeOrNull<Button>("InfluenceToggleButton");
        _legendPanel = GetNode<Control>("LegendPanel");

        _planeToggleButton.CustomMinimumSize = new Vector2(150, 30);
        _planeToggleButton.TooltipText = "Toggle physical / semantic plane";
        _planeToggleButton.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _planeToggleButton.Pressed += () => EmitSignal(SignalName.PlaneToggleRequested);

        _influenceToggleButton?.QueueFree();
        _influenceToggleButton = null;
        AddLayerPanel();

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

    public void SetLayerVisible(string layerKey, bool visible)
    {
        var checkbox = layerKey switch
        {
            LayerRoads => _roadsLayerCheckBox,
            LayerMapFeatures => _featuresLayerCheckBox,
            LayerInfluence => _influenceLayerCheckBox,
            _ => null,
        };

        if (checkbox is null)
        {
            return;
        }

        _updatingLayerControls = true;
        checkbox.ButtonPressed = visible;
        _updatingLayerControls = false;
    }

    private void AddLayerPanel()
    {
        _layersPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(160, 0),
        };
        _layersPanel.AddThemeStyleboxOverride("panel", UiTheme.RailPanelStyle(onLeft: false));

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", UiTokens.Space1);
        _layersPanel.AddChild(box);

        var label = new Label
        {
            Text = "Map Layers",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        label.AddThemeFontOverride("font", UiTheme.BodyFont);
        label.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        label.AddThemeColorOverride("font_color", UiTokens.Whisper);
        box.AddChild(label);

        _roadsLayerCheckBox = AddLayerCheckBox(box, "Roads", LayerRoads);
        _featuresLayerCheckBox = AddLayerCheckBox(box, "Features", LayerMapFeatures);
        _influenceLayerCheckBox = AddLayerCheckBox(box, "Influence", LayerInfluence);
        AddChild(_layersPanel);
    }

    private CheckBox AddLayerCheckBox(VBoxContainer box, string text, string layerKey)
    {
        var checkbox = new CheckBox
        {
            Text = text,
            ButtonPressed = true,
            TooltipText = $"Toggle {text} layer",
        };
        checkbox.Toggled += visible => OnLayerToggled(layerKey, visible);
        box.AddChild(checkbox);
        return checkbox;
    }

    private void OnLayerToggled(string layerKey, bool visible)
    {
        if (_updatingLayerControls)
        {
            return;
        }

        EmitSignal(SignalName.MapLayerVisibilityChanged, layerKey, visible);
    }

    private void PositionOverlays()
    {
        if (_planeToggleButton is not null)
        {
            _planeToggleButton.Position = new Vector2(UiTokens.Space4, Size.Y - UiTokens.Space6 - UiTokens.Space2);
        }

        if (_layersPanel is not null)
        {
            var panelSize = _layersPanel.GetCombinedMinimumSize();
            _layersPanel.Position = new Vector2(
                UiTokens.Space4,
                Size.Y - UiTokens.Space6 - UiTokens.Space2 - 34f - panelSize.Y - UiTokens.Space2);
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
