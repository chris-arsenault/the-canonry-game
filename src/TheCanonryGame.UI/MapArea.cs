#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class MapArea : Control
{
    public const string LayerRoads = "roads";
    public const string LayerRoutes = "routes";
    public const string LayerBorders = "borders";
    public const string LayerLabels = "labels";
    public const string LayerTerritory = "territory";
    public const string LayerInfluence = "influence";
    public const string LayerCultureInfluence = "culture_influence";
    public const string LayerLabor = "labor";
    public const string LayerWorldgenInspection = "worldgen_inspection";
    public const string LayerGeographyFeatures = "geography_features";
    public const string GeographyViewTerrain = "terrain";
    public const string GeographyViewSurface = "surface";
    public const string GeographyViewNaturalSurface = "natural_surface";
    public const string GeographyViewMaterial = "material";
    public const string GeographyViewElevation = "elevation";
    public const string GeographyViewTemperature = "temperature";
    public const string GeographyViewMoisture = "moisture";
    public const string GeographyViewGeothermal = "geothermal";
    public const string GeographyViewOceanCurrents = "ocean_currents";
    public const string GeographyViewWaveExposure = "wave_exposure";
    public const string GeographyViewPrecipitation = "precipitation";
    public const string GeographyViewSeaIceSuitability = "sea_ice_suitability";
    public const string GeographyViewLandform = "landform";
    public const string GeographyViewHydrology = "hydrology";
    public const string GeographyViewIce = "ice";
    public const string GeographyViewFractures = "fractures";
    public const string GeographyViewTectonicProvinces = "tectonic_provinces";
    public const string GeographyViewCrustType = "crust_type";
    public const string GeographyViewTectonicBoundaries = "tectonic_boundaries";
    public const string GeographyViewTectonicStrain = "tectonic_strain";
    public const string GeographyViewFire = "fire";
    public const string GeographyViewAurora = "aurora";
    public const string GeographyViewPressure = "pressure";
    public const string GeographyViewMagicComposite = "magic_composite";
    public const string LensDefault = "default";
    public const string LensCulture = "culture";
    public const string LensControl = "control";
    public const string LensInfluence = "influence";
    public const string LensCustom = "custom";

    private readonly Dictionary<string, Button> _lensButtons = [];
    private readonly Dictionary<string, CheckBox> _layerCheckBoxes = [];
    private Button? _planeToggleButton;
    private Button? _influenceToggleButton;
    private PanelContainer? _layersPanel;
    private LegendPanel? _legendPanel;
    private Control? _geographyControls;
    private OptionButton? _geographyViewOptions;
    private OverlayLayoutState? _lastOverlayLayout;
    private bool _updatingLayerControls;
    private bool _updatingLensControls;

    [Signal]
    public delegate void PlaneToggleRequestedEventHandler();

    [Signal]
    public delegate void MapLayerVisibilityChangedEventHandler(string layerKey, bool visible);

    [Signal]
    public delegate void MapLensRequestedEventHandler(string lensKey);

    [Signal]
    public delegate void GeographyViewRequestedEventHandler(string viewKey);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        _planeToggleButton = GetNode<Button>("PlaneToggleButton");
        _influenceToggleButton = GetNodeOrNull<Button>("InfluenceToggleButton");
        _legendPanel = GetNode<LegendPanel>("LegendPanel");

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
        if (_geographyControls is not null)
        {
            _geographyControls.Visible = plane == PlaneMode.Physical;
            _lastOverlayLayout = null;
            PositionOverlays();
        }
    }

    public void SetLayerVisible(string layerKey, bool visible)
    {
        if (!_layerCheckBoxes.TryGetValue(layerKey, out var checkbox))
        {
            return;
        }

        _updatingLayerControls = true;
        checkbox.ButtonPressed = visible;
        _updatingLayerControls = false;
    }

    public void SetLensKey(string lensKey)
    {
        _updatingLensControls = true;
        foreach (var (key, button) in _lensButtons)
        {
            button.ButtonPressed = string.Equals(key, lensKey, StringComparison.Ordinal);
        }

        _updatingLensControls = false;
    }

    public void SetGeographyViewKey(string viewKey)
    {
        if (_geographyViewOptions is null)
        {
            return;
        }

        for (var index = 0; index < _geographyViewOptions.ItemCount; index++)
        {
            if (string.Equals(
                _geographyViewOptions.GetItemMetadata(index).AsString(),
                viewKey,
                StringComparison.Ordinal))
            {
                _geographyViewOptions.Select(index);
                return;
            }
        }
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

        var lensLabel = new Label
        {
            Text = "Map Lens",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        lensLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        lensLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        lensLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);
        box.AddChild(lensLabel);

        var lensGrid = new GridContainer
        {
            Columns = 2,
        };
        lensGrid.AddThemeConstantOverride("h_separation", UiTokens.Space1);
        lensGrid.AddThemeConstantOverride("v_separation", UiTokens.Space1);
        box.AddChild(lensGrid);
        AddLensButton(lensGrid, "Default", LensDefault);
        AddLensButton(lensGrid, "Culture", LensCulture);
        AddLensButton(lensGrid, "Control", LensControl);
        AddLensButton(lensGrid, "Influence", LensInfluence);

        AddSeparator(box);

        var label = new Label
        {
            Text = "Map Layers",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        label.AddThemeFontOverride("font", UiTheme.BodyFont);
        label.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        label.AddThemeColorOverride("font_color", UiTokens.Whisper);
        box.AddChild(label);

        _ = AddLayerCheckBox(box, "Roads", LayerRoads);
        _ = AddLayerCheckBox(box, "Routes", LayerRoutes);
        _ = AddLayerCheckBox(box, "Borders", LayerBorders);
        _ = AddLayerCheckBox(box, "Labels", LayerLabels);
        _ = AddLayerCheckBox(box, "Control", LayerTerritory);
        _ = AddLayerCheckBox(box, "Culture", LayerCultureInfluence);
        _ = AddLayerCheckBox(box, "Influence", LayerInfluence);
        _ = AddLayerCheckBox(box, "Labor", LayerLabor);
        AddGeographyControls(box);
        AddChild(_layersPanel);
    }

    private static void AddSeparator(VBoxContainer box) =>
        box.AddChild(new ColorRect
        {
            Color = UiTokens.Hairline,
            CustomMinimumSize = new Vector2(0, 1),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        });

    private void AddGeographyControls(VBoxContainer box)
    {
        var controls = new VBoxContainer();
        controls.AddThemeConstantOverride("separation", UiTokens.Space1);
        AddSeparator(controls);
        var label = new Label
        {
            Text = "Physical Geography",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        label.AddThemeFontOverride("font", UiTheme.BodyFont);
        label.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        label.AddThemeColorOverride("font_color", UiTokens.Whisper);
        controls.AddChild(label);

        _geographyViewOptions = new OptionButton
        {
            TooltipText = "Choose the generated physical field to inspect",
        };
        AddGeographyViewOption("Terrain", GeographyViewTerrain);
        AddGeographyViewOption("Final surfaces", GeographyViewSurface);
        AddGeographyViewOption("Natural surfaces", GeographyViewNaturalSurface);
        AddGeographyViewOption("Materials", GeographyViewMaterial);
        AddGeographyViewOption("Elevation", GeographyViewElevation);
        AddGeographyViewOption("Temperature", GeographyViewTemperature);
        AddGeographyViewOption("Moisture", GeographyViewMoisture);
        AddGeographyViewOption("Geothermal", GeographyViewGeothermal);
        AddGeneratedProcessViewOptions();
        AddGeographyViewOption("Landforms", GeographyViewLandform);
        AddGeographyViewOption("Hydrology", GeographyViewHydrology);
        AddGeographyViewOption("Ice", GeographyViewIce);
        AddGeographyViewOption("Fractures", GeographyViewFractures);
        AddGeographyViewOption("Tectonic provinces", GeographyViewTectonicProvinces);
        AddGeographyViewOption("Crust type", GeographyViewCrustType);
        AddGeographyViewOption("Tectonic boundaries", GeographyViewTectonicBoundaries);
        AddGeographyViewOption("Tectonic strain", GeographyViewTectonicStrain);
        AddGeographyViewOption("Fire", GeographyViewFire);
        AddGeographyViewOption("Aurora", GeographyViewAurora);
        AddGeographyViewOption("Pressure", GeographyViewPressure);
        AddGeographyViewOption("Magic composite", GeographyViewMagicComposite);
        _geographyViewOptions.Select(1);
        _geographyViewOptions.ItemSelected += OnGeographyViewSelected;
        controls.AddChild(_geographyViewOptions);

        var inspection = AddLayerCheckBox(
            controls,
            "Reveal latent field",
            LayerWorldgenInspection);
        inspection.ButtonPressed = false;
        _ = AddLayerCheckBox(controls, "Geography features", LayerGeographyFeatures);
        _geographyControls = controls;
        box.AddChild(controls);
    }

    private void AddGeneratedProcessViewOptions()
    {
        AddGeographyViewOption("Ocean currents", GeographyViewOceanCurrents);
        AddGeographyViewOption("Wave exposure", GeographyViewWaveExposure);
        AddGeographyViewOption("Rain and snow", GeographyViewPrecipitation);
        AddGeographyViewOption("Sea-ice suitability", GeographyViewSeaIceSuitability);
    }

    private void AddGeographyViewOption(string label, string viewKey)
    {
        var index = _geographyViewOptions!.ItemCount;
        _geographyViewOptions.AddItem(label);
        _geographyViewOptions.SetItemMetadata(index, viewKey);
    }

    public void BindCultureOverview(
        Engine.Projection.HexMapProjection? map,
        Engine.Projection.UiProjectionState? projection,
        Engine.Projection.IProjectionSymbolCatalog? symbols,
        PlaneMode plane,
        IReadOnlyList<Engine.Projection.CulturePowerSummary>? culturePowers = null)
    {
        if (_legendPanel?.BindCultureOverview(map, projection, symbols, plane, culturePowers) == true)
        {
            PositionOverlays();
        }
    }

    private void AddLensButton(GridContainer grid, string text, string lensKey)
    {
        var button = new Button
        {
            Text = text,
            ToggleMode = true,
            CustomMinimumSize = new Vector2(70, 26),
            TooltipText = $"{text} lens",
        };
        button.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        button.Pressed += () => OnLensPressed(lensKey);
        _lensButtons[lensKey] = button;
        grid.AddChild(button);
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
        _layerCheckBoxes[layerKey] = checkbox;
        box.AddChild(checkbox);
        return checkbox;
    }

    private void OnLensPressed(string lensKey)
    {
        if (_updatingLensControls)
        {
            return;
        }

        EmitSignal(SignalName.MapLensRequested, lensKey);
    }

    private void OnLayerToggled(string layerKey, bool visible)
    {
        if (_updatingLayerControls)
        {
            return;
        }

        EmitSignal(SignalName.MapLayerVisibilityChanged, layerKey, visible);
    }

    private void OnGeographyViewSelected(long index)
    {
        if (_geographyViewOptions is null || (ulong)index >= (ulong)_geographyViewOptions.ItemCount)
        {
            return;
        }

        EmitSignal(
            SignalName.GeographyViewRequested,
            _geographyViewOptions.GetItemMetadata((int)index).AsString());
    }

    private void PositionOverlays()
    {
        var areaSize = Size;
        var layerPanelSize = _layersPanel?.GetCombinedMinimumSize() ?? Vector2.Zero;
        var legendSize = _legendPanel?.GetCombinedMinimumSize() ?? Vector2.Zero;
        var state = new OverlayLayoutState(
            areaSize,
            layerPanelSize,
            legendSize,
            _planeToggleButton is not null,
            _layersPanel is not null,
            _legendPanel is not null);
        if (_lastOverlayLayout == state)
        {
            return;
        }

        if (_planeToggleButton is not null)
        {
            _planeToggleButton.Position = new Vector2(UiTokens.Space4, areaSize.Y - UiTokens.Space6 - UiTokens.Space2);
        }

        if (_layersPanel is not null)
        {
            _layersPanel.Position = new Vector2(
                UiTokens.Space4,
                areaSize.Y - UiTokens.Space6 - UiTokens.Space2 - 34f - layerPanelSize.Y - UiTokens.Space2);
        }

        if (_legendPanel is not null)
        {
            _legendPanel.Position = new Vector2(
                areaSize.X - legendSize.X - UiTokens.Space4,
                areaSize.Y - legendSize.Y - UiTokens.Space6 - UiTokens.Space2);
        }

        _lastOverlayLayout = state;
    }

    private readonly record struct OverlayLayoutState(
        Vector2 AreaSize,
        Vector2 LayerPanelSize,
        Vector2 LegendSize,
        bool HasPlaneToggle,
        bool HasLayersPanel,
        bool HasLegendPanel);
}
