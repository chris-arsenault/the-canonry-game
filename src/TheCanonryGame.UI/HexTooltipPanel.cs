#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Constants;
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class HexTooltipPanel : PanelContainer
{
    private const long TooltipDelayMs = 500;
    private static readonly string[] MagicFirstEpochFields =
    [
        "fire_first_epoch",
        "aurora_first_epoch",
        "pressure_first_epoch",
        "pressure_coastal_first_epoch",
    ];

    private readonly List<Control> _passThroughControls = [];
    private HexMapProjection? _map;
    private PhysicalGeographyProjection? _physicalGeography;
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private HexMapRenderer? _renderer;
    private PhysicalMap3DInspector? _physicalMap3DInspector;
    private Node2D? _worldCanvas;
    private Label? _label;
    private HexCoord? _hoveredHex;
    private int _hoveredTriangleIndex = -1;
    private Vector2 _hoverScreenPosition;
    private long _hoverStartedAtMs;
    private TooltipTextState? _lastTextState;
    private TooltipPositionState? _lastPositionState;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride("panel", UiTheme.OverlayPanelStyle());

        _label = GetNode<Label>("TooltipLabel");
        _label.MouseFilter = MouseFilterEnum.Ignore;
        _label.AutowrapMode = TextServer.AutowrapMode.Off;
        _label.AddThemeFontOverride("font", UiTheme.MonoFont);
        _label.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _label.AddThemeColorOverride("font_color", UiTokens.Vellum);
    }

    public void Initialize(
        HexMapProjection? map,
        PhysicalGeographyProjection? physicalGeography,
        UiProjectionState? projection,
        IProjectionSymbolCatalog? symbols,
        HexMapRenderer? renderer,
        PhysicalMap3DInspector? physicalMap3DInspector,
        Node2D worldCanvas,
        IEnumerable<Control?> passThroughControls)
    {
        ArgumentNullException.ThrowIfNull(worldCanvas);
        ArgumentNullException.ThrowIfNull(passThroughControls);

        _map = map;
        _physicalGeography = physicalGeography;
        _projection = projection;
        _symbols = symbols;
        _renderer = renderer;
        _physicalMap3DInspector = physicalMap3DInspector;
        _worldCanvas = worldCanvas;
        _passThroughControls.Clear();
        _passThroughControls.AddRange(passThroughControls.OfType<Control>());
        ClearTooltip();
    }

    public bool UpdateTooltip()
    {
        if (!TryResolvePointedLocation(out var screenPosition, out var pointedHex, out var triangleIndex))
        {
            ClearTooltip();
            return false;
        }

        var now = System.Environment.TickCount64;
        _hoverScreenPosition = screenPosition;
        if (_hoveredHex is null || _hoveredHex.Value != pointedHex)
        {
            _hoveredHex = pointedHex;
            _hoveredTriangleIndex = triangleIndex;
            _hoverStartedAtMs = now;
            _lastTextState = null;
            _lastPositionState = null;
            Visible = false;
            return true;
        }

        if (_hoveredTriangleIndex != triangleIndex)
        {
            _hoveredTriangleIndex = triangleIndex;
            _lastTextState = null;
            _lastPositionState = null;
        }

        if (!Visible && now - _hoverStartedAtMs < TooltipDelayMs)
        {
            return true;
        }

        PresentTooltipText(pointedHex);
        Visible = true;
        PositionTooltip(_hoverScreenPosition);
        return true;
    }

    private bool TryResolvePointedLocation(
        out Vector2 screenPosition,
        out HexCoord pointedHex,
        out int triangleIndex)
    {
        screenPosition = default;
        pointedHex = default;
        triangleIndex = -1;
        if (_renderer is null || _label is null || _worldCanvas is null ||
            ShouldSuppressHexTooltip(GetViewport().GuiGetHoveredControl()))
        {
            return false;
        }

        screenPosition = GetViewport().GetMousePosition();
        return _physicalMap3DInspector?.Active == true
            ? TryResolvePhysical3DLocation(screenPosition, out pointedHex, out triangleIndex)
            : TryResolve2DLocation(screenPosition, out pointedHex, out triangleIndex);
    }

    private bool TryResolvePhysical3DLocation(
        Vector2 screenPosition,
        out HexCoord pointedHex,
        out int triangleIndex)
    {
        if (_physicalMap3DInspector?.TryPick(screenPosition, out var hit) != true)
        {
            pointedHex = default;
            triangleIndex = -1;
            return false;
        }

        pointedHex = hit.Coord;
        triangleIndex = hit.TriangleIndex;
        return true;
    }

    private bool TryResolve2DLocation(
        Vector2 screenPosition,
        out HexCoord pointedHex,
        out int triangleIndex)
    {
        var worldPosition = _worldCanvas!.GetCanvasTransform().AffineInverse() * screenPosition;
        var resolvedHex = _renderer!.GetHexAtWorldPosition(worldPosition);
        if (resolvedHex is null)
        {
            pointedHex = default;
            triangleIndex = -1;
            return false;
        }

        pointedHex = resolvedHex.Value;
        triangleIndex = _renderer.GetPhysicalMicrotriangleAtWorldPosition(worldPosition)?
            .TriangleIndex ?? -1;
        return true;
    }

    public void ClearTooltip()
    {
        _hoveredHex = null;
        _hoveredTriangleIndex = -1;
        _lastTextState = null;
        _lastPositionState = null;
        Visible = false;
    }

    private bool ShouldSuppressHexTooltip(Control? hoveredControl)
    {
        if (hoveredControl is null)
        {
            return false;
        }

        return !_passThroughControls.Contains(hoveredControl);
    }

    private string FormatHexTooltip(HexCoord coord, int triangleIndex)
    {
        if (_physicalMap3DInspector?.Active == true)
        {
            return FormatPhysicalInspectionTooltip(coord, triangleIndex);
        }

        if (_map is null || _projection is null)
        {
            return string.Empty;
        }

        return _renderer?.CurrentPlane == PlaneMode.Semantic
            ? FormatSemanticHexTooltip(coord)
            : FormatPhysicalHexTooltip(coord, triangleIndex);
    }

    private void PresentTooltipText(HexCoord coord)
    {
        if (_label is null || _renderer is null)
        {
            return;
        }

        var plane = TooltipPlane();
        var textState = new TooltipTextState(
            coord,
            _hoveredTriangleIndex,
            plane,
            _map?.CellVersion ?? -1,
            plane == PlaneMode.Semantic ? _projection?.EntityMapVersion ?? -1 : -1);
        if (_lastTextState == textState)
        {
            return;
        }

        _label.Text = FormatHexTooltip(coord, _hoveredTriangleIndex);
        _lastTextState = textState;
        _lastPositionState = null;
    }

    private PlaneMode TooltipPlane() => _physicalMap3DInspector?.Active == true
        ? PlaneMode.Physical
        : _renderer!.CurrentPlane;

    private string FormatPhysicalInspectionTooltip(HexCoord coord, int triangleIndex)
    {
        if (_physicalGeography?.TryGetCell(coord, out var geographyCell) != true)
        {
            return $"{coord.Q,3} , {coord.R,3}   outside generated geography";
        }

        return $"{FormatLocation(coord, triangleIndex)}\n" +
            FormatPhysicalGeography(geographyCell, triangleIndex);
    }

    private string FormatSemanticHexTooltip(HexCoord coord)
    {
        var s = -coord.Q - coord.R;
        var occupantsLine = FormatOccupants(PlaneCodes.Semantic, coord);
        if (!_map!.TryGetHex(PlaneCodes.Semantic, coord, out var cell))
        {
            return $"q {coord.Q,3}    r {coord.R,3}    s {s,3}\n{occupantsLine}";
        }

        var terrain = FormatTerrain(cell);
        var owner = FormatOwner(cell);
        var continent = FormatSubdivision(ProjectionSymbolKind.Continent, cell.ContinentId);
        var superregion = FormatSubdivision(ProjectionSymbolKind.Superregion, cell.SuperregionId);
        return
            $"q {coord.Q,3}    r {coord.R,3}    s {s,3}\n" +
            $"{terrain,-10}   {owner}   {FormatRoad(cell)}\n" +
            $"{continent}   {superregion}\n" +
            $"{FormatCultureInfluence(cell)}\n" +
            $"{FormatInfluence(cell)}\n" +
            occupantsLine;
    }

    private string FormatPhysicalHexTooltip(HexCoord coord, int triangleIndex)
    {
        var hasLiveCell = _map!.TryGetHex(PlaneCodes.Physical, coord, out var cell);
        var geographyCell = default(PhysicalGeographyCellProjection);
        var hasGeography = _physicalGeography is not null &&
            _physicalGeography.TryGetCell(coord, out geographyCell);
        if (!hasLiveCell &&
            (!hasGeography || _renderer?.WorldgenInspectionEnabled != true))
        {
            return $"{coord.Q,3} , {coord.R,3}   unknown";
        }

        var location = FormatLocation(coord, triangleIndex);
        var geography = hasGeography
            ? FormatPhysicalGeography(geographyCell, triangleIndex)
            : string.Empty;
        if (!hasLiveCell)
        {
            return $"{location}\n{geography}";
        }

        var terrain = FormatTerrain(cell);
        var owner = FormatOwner(cell);
        var continent = FormatSubdivision(ProjectionSymbolKind.Continent, cell.ContinentId);
        var superregion = FormatSubdivision(ProjectionSymbolKind.Superregion, cell.SuperregionId);
        var occupantsLine = FormatOccupants(PlaneCodes.Physical, coord);
        var forgedLine = FormatForgedColumn(cell);
        return
            $"{location}   {terrain,-10}   {owner}   {FormatSurface(cell)}\n" +
            (forgedLine.Length > 0 ? forgedLine + "\n" : string.Empty) +
            $"{continent}   {superregion}\n" +
            (geography.Length > 0 ? geography + "\n" : string.Empty) +
            $"{FormatCultureInfluence(cell)}\n" +
            $"{FormatInfluence(cell)}\n" +
            occupantsLine;
    }

    private static string FormatLocation(HexCoord coord, int triangleIndex) =>
        triangleIndex >= 0
            ? $"({coord.Q},{coord.R},t{triangleIndex})"
            : $"({coord.Q},{coord.R})";

    private string FormatPhysicalGeography(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex)
    {
        var terrain = Humanize(cell.BaseTerrainStableName);
        var material = Humanize(_physicalGeography!.MaterialName(cell.DominantMaterialIndex));
        var surface = Humanize(_physicalGeography.SurfaceName(cell.PrimarySurfaceIndex));
        var anomalies = SignificantFeatureNames(cell.SignificantFeatureFlags);
        var subdivisions =
            $"{FormatSubdivision(ProjectionSymbolKind.Continent, cell.ContinentId)}   " +
            FormatSubdivision(ProjectionSymbolKind.Superregion, cell.SuperregionId);
        var summary =
            $"generated {terrain}   surface {surface}   material {material}\n" +
            $"anomalies {anomalies}\n" +
            $"elevation {cell.MeanElevation} [{cell.MinElevation},{cell.MaxElevation}]   " +
            $"relief {cell.Relief}   rough {cell.Roughness}\n" +
            $"temperature {cell.MeanTemperature}   moisture {cell.MeanMoisture}   " +
            $"geothermal {cell.MeanGeothermal}\n" +
            $"coverage land {cell.LandCoverage}/24 water {cell.WaterCoverage}/24 " +
            $"ocean {cell.OceanCoverage}/24 lake {cell.LakeCoverage}/24 " +
            $"ice {cell.PermanentIceCoverage}/24 river {cell.RiverCoverage}/24 " +
            $"floodplain {cell.FloodplainCoverage}/24 delta {cell.DeltaCoverage}/24 " +
            $"wide {cell.WideRiverCoverage}/24\n" +
            $"ice thickness {cell.MeanIceThickness}/{cell.MaxIceThickness}   " +
            $"magic fire {cell.MeanFireIntensity}/{cell.MaxFireIntensity} " +
            $"aurora {cell.MeanAuroraIntensity}/{cell.MaxAuroraIntensity} " +
            $"pressure {cell.MeanPressureIntensity}/{cell.MaxPressureIntensity} " +
            $"epochs {_physicalGeography.GenerationEpochCount}\n" +
            $"peaks {cell.PeakCount}   edges river {cell.RiverEdgeMask:X2} " +
            $"coast {cell.CoastEdgeMask:X2} ridge {cell.RidgeEdgeMask:X2} " +
            $"cliff {cell.CliffEdgeMask:X2} pass {cell.PassEdgeMask:X2} " +
            $"fracture {cell.FractureEdgeMask:X2}\n" +
            subdivisions;
        return triangleIndex >= 0 &&
            _physicalGeography.TryGetMicrotriangle(cell.Coord, triangleIndex, out var triangle)
                ? summary + "\n" + FormatPhysicalMicrotriangle(
                    cell.MicrotriangleOffset + triangleIndex,
                    triangleIndex,
                    triangle)
                : summary;
    }

    private string FormatPhysicalMicrotriangle(
        int globalTriangleIndex,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle)
    {
        var material = Humanize(_physicalGeography!.MaterialName(triangle.MaterialIndex));
        var features = PhysicalFeatureNames(triangle.FeatureFlags);
        var surface = Humanize(_physicalGeography.SurfaceName(triangle.SurfaceIndex));
        var naturalSurface = Humanize(_physicalGeography.SurfaceName(triangle.BaseSurfaceIndex));
        var anomalies = SignificantFeatureNames(triangle.SignificantFeatureFlags);
        _ = _physicalGeography.TryGetScalarValue("fire_intensity", globalTriangleIndex, out var fire);
        _ = _physicalGeography.TryGetScalarValue("aurora_intensity", globalTriangleIndex, out var aurora);
        _ = _physicalGeography.TryGetScalarValue("pressure_intensity", globalTriangleIndex, out var pressure);
        return
            $"triangle {triangleIndex:00}   {surface} (natural {naturalSurface})\n" +
            $"{material}   {LandformName(triangle.LandformCode)}   anomalies {anomalies}\n" +
            $"elev {triangle.Elevation} slope {triangle.Slope}   temp {triangle.Temperature}   " +
            $"moist {triangle.Moisture}   geo {triangle.Geothermal}\n" +
            $"water {WaterKindName(triangle.WaterKindCode)} depth {triangle.WaterDepth} " +
            $"body {triangle.WaterBodyId} basin {triangle.DrainageBasinId}\n" +
            $"flow edge {triangle.FlowEdge} accumulation {triangle.FlowAccumulation} " +
            $"river order {triangle.RiverOrder} channel {triangle.RiverChannelStrength}/" +
            $"{triangle.RiverChannelOrder} floodplain {triangle.FloodplainIntensity} " +
            $"delta {triangle.DeltaIntensity}   {features}\n" +
            $"landmass {triangle.LandmassId}/{LandmassClassName(triangle.LandmassClassCode)} " +
            $"continental group {triangle.ContinentalGroupId} bathymetry {BathymetryName(triangle.BathymetricZoneCode)}\n" +
            $"ice {triangle.IceThickness} flux {triangle.IceFlux}   fracture {triangle.FractureConductance} " +
            $"heat source {triangle.HeatSourceDensity}\n" +
            $"magic fire {fire} aurora {aurora} pressure {pressure}\n" +
            FormatMagicHistory(globalTriangleIndex);
    }

    private string FormatMagicHistory(int globalTriangleIndex)
    {
        _ = _physicalGeography!.TryGetScalarValue("fire_history", globalTriangleIndex, out var fire);
        _ = _physicalGeography.TryGetScalarValue("aurora_history", globalTriangleIndex, out var aurora);
        _ = _physicalGeography.TryGetScalarValue("pressure_history", globalTriangleIndex, out var pressure);
        _ = _physicalGeography.TryGetScalarValue(
            "pressure_coastal_history",
            globalTriangleIndex,
            out var coast);
        var first = FirstMagicEpoch(globalTriangleIndex);
        var age = first < 0 ? "none" : $"{first + 1}/{_physicalGeography.GenerationEpochCount}";
        return $"history fire {fire} aurora {aurora} pressure {pressure} coast {coast} first {age}";
    }

    private int FirstMagicEpoch(int globalTriangleIndex)
    {
        var first = int.MaxValue;
        foreach (var stableName in MagicFirstEpochFields)
        {
            _ = _physicalGeography!.TryGetScalarValue(stableName, globalTriangleIndex, out var epoch);
            if (epoch >= 0)
            {
                first = Math.Min(first, epoch);
            }
        }

        return first == int.MaxValue ? -1 : first;
    }

    private static string LandformName(int code) =>
        code >= PhysicalLandformCodes.UValley
            ? GlacialLandformName(code)
            : StandardLandformName(code);

    private static string StandardLandformName(int code) => code switch
    {
        PhysicalLandformCodes.Plain => "plain",
        PhysicalLandformCodes.Slope => "slope",
        PhysicalLandformCodes.Valley => "valley",
        PhysicalLandformCodes.Basin => "basin",
        PhysicalLandformCodes.Ridge => "ridge",
        PhysicalLandformCodes.Peak => "peak",
        PhysicalLandformCodes.Pass => "pass",
        PhysicalLandformCodes.Seabed => "seabed",
        _ => $"landform-{code}",
    };

    private static string GlacialLandformName(int code) => code switch
    {
        PhysicalLandformCodes.UValley => "u-valley",
        PhysicalLandformCodes.Cirque => "cirque",
        PhysicalLandformCodes.Fjord => "fjord",
        PhysicalLandformCodes.Moraine => "moraine",
        _ => HistoricalLandformName(code),
    };

    private static string HistoricalLandformName(int code) => code switch
    {
        PhysicalLandformCodes.OutwashPlain => "outwash plain",
        PhysicalLandformCodes.ImpactBasin => "impact basin",
        PhysicalLandformCodes.ImpactRim => "impact rim",
        PhysicalLandformCodes.Gorge => "gorge",
        PhysicalLandformCodes.Waterfall => "waterfall",
        _ => $"landform-{code}",
    };

    private static string LandmassClassName(int code) => code switch
    {
        PhysicalLandmassClassCodes.Island => "island",
        PhysicalLandmassClassCodes.Continent => "continent",
        _ => "none",
    };

    private static string BathymetryName(int code) => code switch
    {
        PhysicalBathymetricZoneCodes.ExposedShelf => "exposed shelf",
        PhysicalBathymetricZoneCodes.Shelf => "shelf",
        PhysicalBathymetricZoneCodes.ShelfBreak => "shelf break",
        PhysicalBathymetricZoneCodes.DeepOcean => "deep ocean",
        _ => "land",
    };

    private static string WaterKindName(int code) => code switch
    {
        PhysicalWaterKindCodes.Ocean => "ocean",
        PhysicalWaterKindCodes.Lake => "lake",
        _ => "none",
    };

    private static string PhysicalFeatureNames(int flags)
    {
        var features = new List<string>(3);
        if ((flags & PhysicalGeographyFeatureCodes.GeothermalVent) != 0)
        {
            features.Add("vent");
        }

        if ((flags & PhysicalGeographyFeatureCodes.PermanentSeaIce) != 0)
        {
            features.Add("sea ice");
        }

        if ((flags & PhysicalGeographyFeatureCodes.PermanentIceMargin) != 0)
        {
            features.Add("ice margin");
        }

        if ((flags & PhysicalGeographyFeatureCodes.FireCore) != 0)
        {
            features.Add("fire core");
        }

        if ((flags & PhysicalGeographyFeatureCodes.MagmaChamber) != 0)
        {
            features.Add("magma chamber");
        }

        if ((flags & PhysicalGeographyFeatureCodes.DeadGodLocus) != 0)
        {
            features.Add("pressure locus");
        }

        AddVolcanicFeatureNames(features, flags);

        return features.Count == 0 ? "features none" : $"features {string.Join(", ", features)}";
    }

    private static void AddVolcanicFeatureNames(List<string> features, int flags)
    {
        if ((flags & PhysicalGeographyFeatureCodes.MantlePlume) != 0)
        {
            features.Add("active mantle plume");
        }

        if ((flags & PhysicalGeographyFeatureCodes.VolcanicCone) != 0)
        {
            features.Add("volcanic cone");
        }

        if ((flags & PhysicalGeographyFeatureCodes.VolcanicCaldera) != 0)
        {
            features.Add("caldera");
        }

        if ((flags & PhysicalGeographyFeatureCodes.LavaPlain) != 0)
        {
            features.Add("lava plain");
        }
    }

    private string SignificantFeatureNames(ulong flags)
    {
        var names = _physicalGeography!.SignificantFeatureNames(flags)
            .Select(Humanize)
            .ToArray();
        return names.Length == 0 ? "none" : string.Join(", ", names);
    }

    private static string Humanize(string stableName) =>
        stableName.Replace('_', ' ');

    private static string FormatForgedColumn(HexProjection cell)
    {
        if (cell.ForgedSpireTiers <= 0 && cell.ForgedTunnelDepth <= 0)
        {
            return string.Empty;
        }

        var spires = cell.ForgedSpireTiers > 0 ? $"spire tiers \u25b2{cell.ForgedSpireTiers}" : string.Empty;
        var tunnels = cell.ForgedTunnelDepth > 0 ? $"tunnels \u25bc{cell.ForgedTunnelDepth}" : string.Empty;
        return spires.Length > 0 && tunnels.Length > 0 ? $"{spires}   {tunnels}" : spires + tunnels;
    }

    private string FormatTerrain(HexProjection cell) =>
        ProjectionDisplay.DisplayName(
            _symbols,
            ProjectionSymbolKind.Terrain,
            cell.TerrainId,
            $"Terrain {cell.TerrainId}");

    private string FormatSubdivision(ProjectionSymbolKind kind, int id) =>
        ProjectionDisplay.DisplayName(_symbols, kind, id, $"{kind} {id}");

    private string FormatOwner(HexProjection cell)
    {
        if (cell.OwnerEntityId <= 0 ||
            !_projection!.Entities.TryGetValue(cell.OwnerEntityId, out var ownerEntity))
        {
            return "unclaimed";
        }

        return ProjectionDisplay.EntityName(_symbols, ownerEntity);
    }

    private string FormatRoad(HexProjection cell)
    {
        var roadQuality = Mathf.Clamp(cell.RoadQuality / 100f, 0f, 1f);
        return roadQuality switch
        {
            > 0.75f => $"heavy road {roadQuality:0.00}",
            > 0.25f => $"light road {roadQuality:0.00}",
            _ => "no road",
        };
    }

    private string FormatSurface(HexProjection cell)
    {
        var road = FormatRoad(cell);
        if (cell.TravelStructureId <= 0)
        {
            return road;
        }

        var structure = ProjectionDisplay.DisplayName(
            _symbols,
            ProjectionSymbolKind.TravelStructure,
            cell.TravelStructureId,
            $"Structure {cell.TravelStructureId}");
        return $"{structure}; {road}";
    }

    private string FormatInfluence(HexProjection cell)
    {
        var influences = cell.Influences
            .Where(influence => influence.Value > 0f)
            .Take(3)
            .Select(influence => $"{FormatEntityName(influence.EntityId)} {influence.Value:0}")
            .ToArray();
        return influences.Length == 0
            ? "influence none"
            : $"influence {string.Join(" | ", influences)}";
    }

    private string FormatCultureInfluence(HexProjection cell)
    {
        if (cell.CultureInfluences.Count == 0)
        {
            return "culture none";
        }

        var cultures = cell.CultureInfluences
            .Take(3)
            .Select(FormatCultureBoundary)
            .ToArray();
        var dominantCultureId = cell.CultureInfluences[0].CultureId;
        var ownerCultureId = cell.OwnerEntityId > 0 &&
            _projection!.Entities.TryGetValue(cell.OwnerEntityId, out var owner)
                ? owner.CultureId
                : 0;
        var mismatch = ownerCultureId > 0 && ownerCultureId != dominantCultureId
            ? "  control mismatch"
            : string.Empty;
        var runnerUp = FormatRunnerUpCulture(cell);
        return $"culture {string.Join(" | ", cultures)}{runnerUp}{mismatch}";
    }

    private string FormatCultureBoundary(HexCultureInfluenceProjection influence)
    {
        var cultureName = ProjectionDisplay.DisplayName(
            _symbols,
            ProjectionSymbolKind.Culture,
            influence.CultureId,
            $"Culture {influence.CultureId}");
        var kind = CultureCellKindName(influence.CellKindCode);
        var band = CultureStrengthBandName(influence.StrengthBandCode);
        var component = influence.ComponentId > 0
            ? $" c{influence.ComponentId}/{influence.ComponentCellCount}"
            : string.Empty;
        return $"{cultureName} {influence.Share:0}% raw {influence.RawMass:0} {kind}/{band}{component}";
    }

    private string FormatRunnerUpCulture(HexProjection cell)
    {
        if (cell.CultureInfluences.Count <= 1)
        {
            return string.Empty;
        }

        var runnerUp = cell.CultureInfluences[1];
        var cultureName = ProjectionDisplay.DisplayName(
            _symbols,
            ProjectionSymbolKind.Culture,
            runnerUp.CultureId,
            $"Culture {runnerUp.CultureId}");
        return $"  runner-up {cultureName} {runnerUp.Share:0}% raw {runnerUp.RawMass:0}";
    }

    private static string CultureCellKindName(int code) => code switch
    {
        CultureFieldCellKindCodes.Interior => "interior",
        CultureFieldCellKindCodes.Border => "border",
        CultureFieldCellKindCodes.Isolated => "isolated",
        CultureFieldCellKindCodes.Contested => "contested",
        _ => "none",
    };

    private static string CultureStrengthBandName(int code) => code switch
    {
        CultureFieldStrengthBandCodes.Nexus => "nexus",
        CultureFieldStrengthBandCodes.Entrenched => "entrenched",
        CultureFieldStrengthBandCodes.Controlled => "controlled",
        CultureFieldStrengthBandCodes.Claimed => "claimed",
        CultureFieldStrengthBandCodes.Trace => "trace",
        _ => "none",
    };

    private string FormatEntityName(int entityId)
    {
        return _projection!.Entities.TryGetValue(entityId, out var entity)
            ? ProjectionDisplay.EntityName(_symbols, entity)
            : $"Entity {entityId}";
    }

    private string FormatProjectedEntityLine(UiEntityProjection entity)
    {
        var name = ProjectionDisplay.EntityName(_symbols, entity);
        var kind = ProjectionDisplay.DisplayName(
            _symbols,
            ProjectionSymbolKind.Kind,
            entity.KindId,
            $"Kind {entity.KindId}");
        return $"{name}  {kind}";
    }

    private string FormatOccupants(int planeCode, HexCoord coord)
    {
        var occupants = _projection!.ActiveOccupants(planeCode, coord)
            .Order()
            .Select(entityId => _projection.Entities.TryGetValue(entityId, out var entity) ? entity : null)
            .OfType<UiEntityProjection>()
            .Select(FormatProjectedEntityLine)
            .ToList();
        return occupants.Count == 0 ? "uninhabited" : string.Join('\n', occupants);
    }

    private void PositionTooltip(Vector2 screenPos)
    {
        var viewportSize = GetViewportRect().Size;
        var panelSize = GetCombinedMinimumSize();
        var state = new TooltipPositionState(screenPos, viewportSize, panelSize);
        if (_lastPositionState == state)
        {
            return;
        }

        var pos = screenPos + new Vector2(16f, 18f);
        pos.X = Mathf.Min(pos.X, viewportSize.X - panelSize.X - 8f);
        pos.Y = Mathf.Min(pos.Y, viewportSize.Y - panelSize.Y - 8f);
        pos.X = Mathf.Max(pos.X, 8f);
        pos.Y = Mathf.Max(pos.Y, 8f);
        Position = pos;
        _lastPositionState = state;
    }

    private readonly record struct TooltipTextState(
        HexCoord Coord,
        int TriangleIndex,
        PlaneMode Plane,
        long CellVersion,
        long EntityMapVersion);

    private readonly record struct TooltipPositionState(
        Vector2 ScreenPosition,
        Vector2 ViewportSize,
        Vector2 PanelSize);
}
