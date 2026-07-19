using Engine.Constants;
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public interface IPhysicalMicrogeographyCanvas
{
    void FillPolygon(Vector2[] points, Color color);

    void StrokePolyline(Vector2[] points, Color color, float width, bool antialiased);

    void StrokeLine(Vector2 from, Vector2 destination, Color color, float width, bool antialiased);

    void FillCircle(Vector2 center, float radius, Color color);
}

public sealed partial class PhysicalMicrogeographyPainter
{
    private const float FineElevationSubunitsPerUnit = 1024f;
    private static readonly Color OceanWater = new(0.08f, 0.20f, 0.34f);
    private static readonly Color LakeWater = new(0.16f, 0.38f, 0.48f);
    private static readonly Color RiverWater = new(0.26f, 0.68f, 0.88f, 0.92f);
    private static readonly Color CoastLine = new(0.55f, 0.82f, 0.91f, 0.92f);
    private static readonly Color RidgeLine = new(0.82f, 0.78f, 0.66f, 0.78f);
    private static readonly Color CliffLine = new(0.20f, 0.18f, 0.20f, 0.95f);
    private static readonly Color PassLine = new(0.92f, 0.64f, 0.30f, 0.95f);
    private static readonly Color FractureLine = new(0.94f, 0.42f, 0.24f, 0.92f);
    private static readonly string[] FirstMagicEpochFields =
    [
        "fire_first_epoch",
        "aurora_first_epoch",
        "pressure_first_epoch",
        "pressure_coastal_first_epoch",
    ];

    private readonly RenderContext _owner;
    private readonly bool _hasFineElevation;
    private readonly float _minimumElevation;
    private readonly float _maximumElevation;
    private IPhysicalMicrogeographyCanvas? _canvas;

    public PhysicalMicrogeographyPainter(
        PhysicalGeographyProjection geography,
        PhysicalMicrogeographyGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geography);
        ArgumentNullException.ThrowIfNull(geometry);
        _owner = new RenderContext(geography, geometry);
        _hasFineElevation = geography.ScalarFieldNames.Contains(
            "fine_elevation",
            StringComparer.Ordinal);
        _minimumElevation = geography.Cells.Count == 0
            ? -1f
            : Math.Min(-1f, geography.Cells.Min(static cell => cell.MinElevation));
        _maximumElevation = geography.Cells.Count == 0
            ? 1f
            : Math.Max(1f, geography.Cells.Max(static cell => cell.MaxElevation));
    }

    public void PaintCell(
        IPhysicalMicrogeographyCanvas canvas,
        in PhysicalGeographyCellProjection cell,
        PhysicalGeographyView view,
        bool featuresEnabled,
        bool drawMicrotriangles)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        if (_canvas is not null)
        {
            throw new InvalidOperationException("The physical-geography painter is already painting.");
        }

        _canvas = canvas;
        _owner.View = view;
        _owner.FeaturesEnabled = featuresEnabled;
        _owner.DrawMicrotriangles = drawMicrotriangles;
        _owner.DrawAggregateHexes = !drawMicrotriangles;
        try
        {
            if (drawMicrotriangles)
            {
                DrawMicrotriangles(cell);
            }
            else
            {
                DrawAggregate(cell);
            }
        }
        finally
        {
            _canvas = null;
        }
    }

    private void DrawAggregate(in PhysicalGeographyCellProjection cell)
    {
        var color = AggregateColor(cell);
        DrawColoredPolygon(_owner!.Geometry.HexPoints(), color);
        DrawPolyline(_owner.Geometry.ClosedHexPoints(), UiTokens.Hairline, 0.75f);
        if (FeatureDetail() == PhysicalMicrogeographyFeatureDetail.Aggregate)
        {
            DrawHexFeatures(cell);
        }

        if (_owner.View == PhysicalGeographyView.OceanCurrents)
        {
            DrawAggregateCurrent(cell);
        }
    }

    private Color AggregateColor(in PhysicalGeographyCellProjection cell)
    {
        var view = _owner!.View;
        if (view is PhysicalGeographyView.Fire or PhysicalGeographyView.Aurora or
            PhysicalGeographyView.Pressure or PhysicalGeographyView.MagicComposite or
            PhysicalGeographyView.MagicHistory)
        {
            return AggregateMagicColor(cell, view);
        }

        var color = view <= PhysicalGeographyView.Material
            ? AggregateIdentityColor(cell, view)
            : AggregatePhysicalFieldColor(cell, view);
        return PhysicalMicrogeographyRenderDecisions.UsesStructuralRiverOverlay(view)
            ? ApplyAggregateStructuralWater(color, cell)
            : color;
    }

    private Color AggregateIdentityColor(
        in PhysicalGeographyCellProjection cell,
        PhysicalGeographyView view)
    {
        var color = view switch
        {
            PhysicalGeographyView.Terrain => _owner!.TerrainColor(cell),
            PhysicalGeographyView.Surface => UiTokens.PhysicalSurfaceColor(
                _owner!.Geography.SurfaceName(cell.PrimarySurfaceIndex)),
            PhysicalGeographyView.NaturalSurface => DominantNaturalSurfaceColor(cell),
            PhysicalGeographyView.Material => UiTokens.PhysicalMaterialColor(
                _owner!.Geography.MaterialName(cell.DominantMaterialIndex)),
            _ => UiTokens.Whisper,
        };
        return PhysicalMicrogeographyRenderDecisions.UsesReliefShading(view)
            ? ReliefShade(color, cell.MeanElevation, cell.Relief, cell.Roughness)
            : color;
    }

    private Color AggregatePhysicalFieldColor(
        in PhysicalGeographyCellProjection cell,
        PhysicalGeographyView view)
    {
        if (IsTectonicView(view))
        {
            return AggregateTectonicColor(cell, view);
        }

        if (IsHistoryView(view))
        {
            return AggregateHistoryColor(cell, view);
        }

        return view <= PhysicalGeographyView.Geothermal
            ? AggregateClimateColor(cell, view)
            : AggregateProcessColor(cell, view);
    }

    private Color AggregateClimateColor(
        in PhysicalGeographyCellProjection cell,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Elevation => ElevationColor(
                AggregateElevation(cell),
                _minimumElevation,
                _maximumElevation),
            PhysicalGeographyView.Temperature => TemperatureColor(cell.MeanTemperature),
            PhysicalGeographyView.Moisture => MoistureColor(cell.MeanMoisture),
            PhysicalGeographyView.Geothermal => GeothermalColor(cell.MeanGeothermal),
            _ => UiTokens.Whisper,
        };

    private Color AggregateProcessColor(
        in PhysicalGeographyCellProjection cell,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.OceanCurrents => OceanCurrentColor(
                AggregateScalarValue(cell, "ocean_current_strength"),
                AggregateScalarValue(cell, "ocean_current_heat")),
            PhysicalGeographyView.WaveExposure =>
                WaveExposureColor(AggregateScalarValue(cell, "wave_exposure")),
            PhysicalGeographyView.Precipitation => PrecipitationColor(
                AggregateScalarValue(cell, "rainfall"),
                AggregateScalarValue(cell, "snowfall")),
            PhysicalGeographyView.SeaIceSuitability =>
                SeaIceSuitabilityColor(AggregateScalarValue(cell, "sea_ice_suitability")),
            PhysicalGeographyView.Hydrology => AggregateHydrologyColor(cell),
            PhysicalGeographyView.RiverGeometry => RiverGeometryColor(
                cell.RiverCoverage,
                cell.FloodplainCoverage,
                cell.DeltaCoverage),
            PhysicalGeographyView.Ice => IceColor(cell.MeanIceThickness),
            PhysicalGeographyView.Fractures => FractureColor(
                System.Numerics.BitOperations.PopCount((uint)cell.FractureEdgeMask) * 1000 / 6),
            _ => UiTokens.Whisper,
        };

    private Color AggregateHistoryColor(
        in PhysicalGeographyCellProjection cell,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Sediment => SedimentColor(
                AggregateScalarValue(cell, "sediment_thickness"),
                AggregateScalarValue(cell, "deposition_history")),
            PhysicalGeographyView.ErosionHistory => ErosionHistoryColor(
                AggregateScalarValue(cell, "fluvial_erosion_history"),
                AggregateScalarValue(cell, "glacial_erosion_history"),
                AggregateScalarValue(cell, "coastal_erosion_history")),
            PhysicalGeographyView.MaximumGlaciation =>
                MaximumGlaciationColor(AggregateScalarValue(cell, "maximum_ice_thickness")),
            _ => UiTokens.Whisper,
        };

    private Color AggregateTectonicColor(
        in PhysicalGeographyCellProjection cell,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.TectonicProvinces =>
                ScalarColor(cell, 0, "crust_province", TectonicProvinceColor),
            PhysicalGeographyView.CrustType => ScalarColor(cell, 0, "crust_type", CrustTypeColor),
            PhysicalGeographyView.TectonicBoundaries => AggregateBoundaryColor(cell),
            PhysicalGeographyView.TectonicStrain =>
                AggregateScalarColor(cell, "tectonic_strain", TectonicStrainColor),
            _ => UiTokens.Whisper,
        };

    private Color AggregateMagicColor(
        in PhysicalGeographyCellProjection cell,
        PhysicalGeographyView view)
    {
        if (view == PhysicalGeographyView.MagicHistory)
        {
            return AggregateMagicHistoryColor(cell);
        }

        var magic = view switch
        {
            PhysicalGeographyView.Fire => FireColor(cell.MeanFireIntensity),
            PhysicalGeographyView.Aurora => AuroraColor(cell.MeanAuroraIntensity),
            PhysicalGeographyView.Pressure => PressureColor(cell.MeanPressureIntensity),
            _ => MagicCompositeColor(
                cell.MeanFireIntensity,
                cell.MeanAuroraIntensity,
                cell.MeanPressureIntensity),
        };
        var intensity = view switch
        {
            PhysicalGeographyView.Fire => cell.MeanFireIntensity,
            PhysicalGeographyView.Aurora => cell.MeanAuroraIntensity,
            PhysicalGeographyView.Pressure => cell.MeanPressureIntensity,
            _ => Math.Max(
                cell.MeanFireIntensity,
                Math.Max(cell.MeanAuroraIntensity, cell.MeanPressureIntensity)),
        };
        var geography = ReliefShade(
            DominantNaturalSurfaceColor(cell),
            cell.MeanElevation,
            cell.Relief,
            cell.Roughness);
        return CompositeMagicOverGeography(geography, magic, intensity);
    }

    private void DrawMicrotriangles(in PhysicalGeographyCellProjection cell)
    {
        var triangles = _owner!.Geography.MicrotrianglesFor(cell);
        for (var index = 0; index < triangles.Length; index++)
        {
            var triangle = triangles[index];
            DrawColoredPolygon(
                _owner.Geometry.TrianglePoints(index),
                TriangleColor(cell, index, triangle));
            DrawPolyline(
                _owner.Geometry.ClosedTrianglePoints(index),
                new Color(UiTokens.Vellum, 0.055f),
                0.45f);
        }

        if (PhysicalMicrogeographyRenderDecisions.UsesStructuralRiverOverlay(_owner.View))
        {
            DrawRiverSpines(triangles);
        }

        DrawPolyline(_owner.Geometry.ClosedHexPoints(), UiTokens.HairlineStrong, 0.9f);
        if (FeatureDetail() == PhysicalMicrogeographyFeatureDetail.Microtriangle)
        {
            DrawTriangleFeatures(cell, triangles);
        }

        if (_owner.View == PhysicalGeographyView.OceanCurrents)
        {
            DrawTriangleCurrents(cell, triangles);
        }
    }

    private Color TriangleColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle)
    {
        var color = BaseTriangleColor(cell, triangleIndex, triangle);
        color = ViewUsesWaterTint(_owner!.View)
            ? ApplyWaterTint(color, triangle.WaterKindCode, triangle.WaterDepth)
            : color;
        return PhysicalMicrogeographyRenderDecisions.UsesStructuralRiverOverlay(_owner.View)
            ? ApplyRiverGeometryTint(color, triangle)
            : color;
    }

    private Color BaseTriangleColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle)
    {
        var view = _owner!.View;
        if (view is PhysicalGeographyView.Fire or PhysicalGeographyView.Aurora or
            PhysicalGeographyView.Pressure or PhysicalGeographyView.MagicComposite or
            PhysicalGeographyView.MagicHistory)
        {
            return TriangleMagicColor(cell, triangleIndex, triangle, view);
        }

        return view <= PhysicalGeographyView.Material
            ? TriangleIdentityColor(cell, triangle, view)
            : TrianglePhysicalFieldColor(cell, triangleIndex, triangle, view);
    }

    private Color TriangleIdentityColor(
        in PhysicalGeographyCellProjection cell,
        in PhysicalMicrotriangleProjection triangle,
        PhysicalGeographyView view)
    {
        var color = view switch
        {
            PhysicalGeographyView.Terrain => _owner!.TerrainColor(cell),
            PhysicalGeographyView.Surface => UiTokens.PhysicalSurfaceColor(
                _owner!.Geography.SurfaceName(triangle.SurfaceIndex)),
            PhysicalGeographyView.NaturalSurface => UiTokens.PhysicalSurfaceColor(
                _owner!.Geography.SurfaceName(triangle.BaseSurfaceIndex)),
            PhysicalGeographyView.Material => UiTokens.PhysicalMaterialColor(
                _owner!.Geography.MaterialName(triangle.MaterialIndex)),
            _ => UiTokens.Whisper,
        };
        return PhysicalMicrogeographyRenderDecisions.UsesReliefShading(view)
            ? ReliefShade(color, triangle.Elevation, triangle.Slope, triangle.Slope)
            : color;
    }

    private Color TrianglePhysicalFieldColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle,
        PhysicalGeographyView view)
    {
        if (IsTectonicView(view))
        {
            return TriangleTectonicColor(cell, triangleIndex, view);
        }

        if (IsHistoryView(view))
        {
            return TriangleHistoryColor(cell, triangleIndex, view);
        }

        return view <= PhysicalGeographyView.Geothermal
            ? TriangleClimateColor(cell, triangleIndex, triangle, view)
            : TriangleProcessColor(cell, triangleIndex, triangle, view);
    }

    private Color TriangleClimateColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Elevation => ElevationColor(
                TriangleElevation(cell, triangleIndex, triangle),
                _minimumElevation,
                _maximumElevation),
            PhysicalGeographyView.Temperature => TemperatureColor(triangle.Temperature),
            PhysicalGeographyView.Moisture => MoistureColor(triangle.Moisture),
            PhysicalGeographyView.Geothermal => GeothermalColor(triangle.Geothermal),
            _ => UiTokens.Whisper,
        };

    private float AggregateElevation(in PhysicalGeographyCellProjection cell) =>
        _hasFineElevation
            ? AggregateScalarValue(cell, "fine_elevation") / FineElevationSubunitsPerUnit
            : cell.MeanElevation;

    private float TriangleElevation(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle) =>
        _hasFineElevation
            ? ScalarValue(cell, triangleIndex, "fine_elevation") / FineElevationSubunitsPerUnit
            : triangle.Elevation;

    private Color TriangleProcessColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle,
        PhysicalGeographyView view)
    => view <= PhysicalGeographyView.SeaIceSuitability
        ? TriangleAtmosphericProcessColor(cell, triangleIndex, view)
        : TriangleLandscapeProcessColor(triangle, view);

    private Color TriangleAtmosphericProcessColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.OceanCurrents => OceanCurrentColor(
                ScalarValue(cell, triangleIndex, "ocean_current_strength"),
                ScalarValue(cell, triangleIndex, "ocean_current_heat")),
            PhysicalGeographyView.WaveExposure =>
                WaveExposureColor(ScalarValue(cell, triangleIndex, "wave_exposure")),
            PhysicalGeographyView.Precipitation => PrecipitationColor(
                ScalarValue(cell, triangleIndex, "rainfall"),
                ScalarValue(cell, triangleIndex, "snowfall")),
            PhysicalGeographyView.SeaIceSuitability => SeaIceSuitabilityColor(
                ScalarValue(cell, triangleIndex, "sea_ice_suitability")),
            _ => UiTokens.Whisper,
        };

    private static Color TriangleLandscapeProcessColor(
        in PhysicalMicrotriangleProjection triangle,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Landform => LandformColor(triangle.LandformCode),
            PhysicalGeographyView.Hydrology => HydrologyColor(triangle),
            PhysicalGeographyView.RiverGeometry => RiverGeometryColor(
                triangle.RiverChannelStrength,
                triangle.FloodplainIntensity,
                triangle.DeltaIntensity),
            PhysicalGeographyView.Ice => IceColor(triangle.IceThickness),
            PhysicalGeographyView.Fractures => FractureColor(triangle.FractureConductance),
            _ => UiTokens.Whisper,
        };

    private Color TriangleHistoryColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Sediment => SedimentColor(
                ScalarValue(cell, triangleIndex, "sediment_thickness"),
                ScalarValue(cell, triangleIndex, "deposition_history")),
            PhysicalGeographyView.ErosionHistory => ErosionHistoryColor(
                ScalarValue(cell, triangleIndex, "fluvial_erosion_history"),
                ScalarValue(cell, triangleIndex, "glacial_erosion_history"),
                ScalarValue(cell, triangleIndex, "coastal_erosion_history")),
            PhysicalGeographyView.MaximumGlaciation => MaximumGlaciationColor(
                ScalarValue(cell, triangleIndex, "maximum_ice_thickness")),
            _ => UiTokens.Whisper,
        };

    private static bool IsTectonicView(PhysicalGeographyView view) =>
        view is >= PhysicalGeographyView.TectonicProvinces and <= PhysicalGeographyView.TectonicStrain;

    private static bool IsHistoryView(PhysicalGeographyView view) =>
        view is >= PhysicalGeographyView.Sediment and <= PhysicalGeographyView.MaximumGlaciation;

    private Color TriangleTectonicColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.TectonicProvinces =>
                ScalarColor(cell, triangleIndex, "crust_province", TectonicProvinceColor),
            PhysicalGeographyView.CrustType =>
                ScalarColor(cell, triangleIndex, "crust_type", CrustTypeColor),
            PhysicalGeographyView.TectonicBoundaries =>
                ScalarColor(cell, triangleIndex, "tectonic_boundary", TectonicBoundaryColor),
            PhysicalGeographyView.TectonicStrain =>
                ScalarColor(cell, triangleIndex, "tectonic_strain", TectonicStrainColor),
            _ => UiTokens.Whisper,
        };

    private Color TriangleMagicColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle,
        PhysicalGeographyView view)
    {
        if (view == PhysicalGeographyView.MagicHistory)
        {
            return MagicHistoryTriangleColor(cell, triangleIndex);
        }

        var fire = ScalarValue(cell, triangleIndex, "fire_intensity");
        var aurora = ScalarValue(cell, triangleIndex, "aurora_intensity");
        var pressure = ScalarValue(cell, triangleIndex, "pressure_intensity");
        var magic = view switch
        {
            PhysicalGeographyView.Fire => FireColor(fire),
            PhysicalGeographyView.Aurora => AuroraColor(aurora),
            PhysicalGeographyView.Pressure => PressureColor(pressure),
            _ => MagicCompositeColor(fire, aurora, pressure),
        };
        var intensity = view switch
        {
            PhysicalGeographyView.Fire => fire,
            PhysicalGeographyView.Aurora => aurora,
            PhysicalGeographyView.Pressure => pressure,
            _ => Math.Max(fire, Math.Max(aurora, pressure)),
        };
        var geography = ReliefShade(
            UiTokens.PhysicalSurfaceColor(_owner!.Geography.SurfaceName(triangle.BaseSurfaceIndex)),
            triangle.Elevation,
            triangle.Slope,
            triangle.Slope);
        return CompositeMagicOverGeography(geography, magic, intensity);
    }

    private Color AggregateMagicHistoryColor(in PhysicalGeographyCellProjection cell)
    {
        var epochCount = Math.Max(1, _owner!.Geography.GenerationEpochCount);
        return MagicHistoryColor(
            AggregateScalarValue(cell, "fire_history") / epochCount,
            AggregateScalarValue(cell, "aurora_history") / epochCount,
            AggregateScalarValue(cell, "pressure_history") / epochCount,
            AggregateScalarValue(cell, "pressure_coastal_history") / epochCount,
            AggregateFirstMagicEpoch(cell),
            epochCount);
    }

    private Color MagicHistoryTriangleColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex)
    {
        var epochCount = Math.Max(1, _owner!.Geography.GenerationEpochCount);
        return MagicHistoryColor(
            ScalarValue(cell, triangleIndex, "fire_history") / epochCount,
            ScalarValue(cell, triangleIndex, "aurora_history") / epochCount,
            ScalarValue(cell, triangleIndex, "pressure_history") / epochCount,
            ScalarValue(cell, triangleIndex, "pressure_coastal_history") / epochCount,
            FirstMagicEpoch(cell, triangleIndex),
            epochCount);
    }

    private int AggregateFirstMagicEpoch(in PhysicalGeographyCellProjection cell)
    {
        var first = int.MaxValue;
        for (var triangleIndex = 0;
            triangleIndex < PhysicalGeographyCodes.MicrotrianglesPerHex;
            triangleIndex++)
        {
            var candidate = FirstMagicEpoch(cell, triangleIndex);
            if (candidate >= 0)
            {
                first = Math.Min(first, candidate);
            }
        }

        return first == int.MaxValue ? -1 : first;
    }

    private int FirstMagicEpoch(in PhysicalGeographyCellProjection cell, int triangleIndex)
    {
        var first = int.MaxValue;
        foreach (var stableName in FirstMagicEpochFields)
        {
            var candidate = ScalarValue(cell, triangleIndex, stableName);
            if (candidate >= 0)
            {
                first = Math.Min(first, candidate);
            }
        }

        return first == int.MaxValue ? -1 : first;
    }

    private Color ScalarColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        string stableName,
        Func<int, Color> color)
    {
        _ = _owner!.Geography.TryGetScalarValue(
            stableName,
            cell.MicrotriangleOffset + triangleIndex,
            out var value);
        return color(value);
    }

    private Color AggregateScalarColor(
        in PhysicalGeographyCellProjection cell,
        string stableName,
        Func<int, Color> color)
    {
        var total = 0;
        for (var triangleIndex = 0;
            triangleIndex < PhysicalGeographyCodes.MicrotrianglesPerHex;
            triangleIndex++)
        {
            _ = _owner!.Geography.TryGetScalarValue(
                stableName,
                cell.MicrotriangleOffset + triangleIndex,
                out var value);
            total += value;
        }

        return color(total / PhysicalGeographyCodes.MicrotrianglesPerHex);
    }

    private int AggregateScalarValue(
        in PhysicalGeographyCellProjection cell,
        string stableName)
    {
        var total = 0;
        for (var triangleIndex = 0;
            triangleIndex < PhysicalGeographyCodes.MicrotrianglesPerHex;
            triangleIndex++)
        {
            total += ScalarValue(cell, triangleIndex, stableName);
        }

        return total / PhysicalGeographyCodes.MicrotrianglesPerHex;
    }

    private int ScalarValue(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        string stableName)
    {
        _ = _owner!.Geography.TryGetScalarValue(
            stableName,
            cell.MicrotriangleOffset + triangleIndex,
            out var value);
        return value;
    }

    private Color AggregateBoundaryColor(in PhysicalGeographyCellProjection cell)
    {
        var strongestBoundary = 0;
        var strongestStrain = 0;
        for (var triangleIndex = 0;
            triangleIndex < PhysicalGeographyCodes.MicrotrianglesPerHex;
            triangleIndex++)
        {
            _ = _owner!.Geography.TryGetScalarValue(
                "tectonic_strain",
                cell.MicrotriangleOffset + triangleIndex,
                out var strain);
            if (strain <= strongestStrain)
            {
                continue;
            }

            strongestStrain = strain;
            _ = _owner.Geography.TryGetScalarValue(
                "tectonic_boundary",
                cell.MicrotriangleOffset + triangleIndex,
                out strongestBoundary);
        }

        return TectonicBoundaryColor(strongestBoundary).Lerp(
            UiTokens.Ink,
            1f - Math.Clamp(strongestStrain / 1000f, 0f, 1f));
    }

    private void DrawTriangleFeatures(
        in PhysicalGeographyCellProjection cell,
        ReadOnlySpan<PhysicalMicrotriangleProjection> triangles)
    {
        for (var index = 0; index < triangles.Length; index++)
        {
            var triangle = triangles[index];
            if (triangle.LandformCode == PhysicalLandformCodes.Peak)
            {
                DrawCircle(
                    _owner!.Geometry.TriangleCentroid(index),
                    1.4f,
                    UiTokens.Vellum);
            }

            if (triangle.SignificantFeatureFlags != 0)
            {
                DrawCircle(
                    _owner!.Geometry.TriangleCentroid(index),
                    1.05f,
                    UiTokens.Gilt);
            }
        }
    }

    private void DrawHexFeatures(in PhysicalGeographyCellProjection cell)
    {
        for (var sector = 0; sector < PhysicalGeographyCodes.HexSectorCount; sector++)
        {
            var neighborExists = _owner!.Geography.TryGetCell(cell.Coord.Neighbor(sector), out _);
            if (!PhysicalMicrogeographyRenderDecisions.OwnsSharedHexEdge(
                    cell.Coord.Q,
                    cell.Coord.R,
                    sector,
                    neighborExists))
            {
                continue;
            }

            var bit = 1 << sector;
            var a = _owner.Geometry.HexPoints()[sector];
            var b = _owner.Geometry.HexPoints()[(sector + 1) % PhysicalGeographyCodes.HexSectorCount];
            if ((cell.RidgeEdgeMask & bit) != 0)
            {
                DrawLine(a, b, RidgeLine, 1.5f, true);
            }

            if ((cell.CliffEdgeMask & bit) != 0)
            {
                DrawLine(a, b, CliffLine, 2.2f, true);
            }

            if ((cell.CoastEdgeMask & bit) != 0)
            {
                DrawLine(a, b, CoastLine, 2.4f, true);
            }

            if ((cell.PassEdgeMask & bit) != 0)
            {
                DrawLine(a.Lerp(b, 0.28f), a.Lerp(b, 0.72f), PassLine, 2.8f, true);
            }

            if ((cell.FractureEdgeMask & bit) != 0)
            {
                DrawLine(a.Lerp(b, 0.12f), a.Lerp(b, 0.88f), FractureLine, 1.2f, true);
            }
        }
    }

    private Color DominantNaturalSurfaceColor(in PhysicalGeographyCellProjection cell)
    {
        Span<int> counts = stackalloc int[64];
        var dominant = 0;
        foreach (var triangle in _owner!.Geography.MicrotrianglesFor(cell))
        {
            if ((uint)triangle.BaseSurfaceIndex >= (uint)counts.Length)
            {
                continue;
            }

            counts[triangle.BaseSurfaceIndex]++;
            if (counts[triangle.BaseSurfaceIndex] > counts[dominant])
            {
                dominant = triangle.BaseSurfaceIndex;
            }
        }

        return UiTokens.PhysicalSurfaceColor(_owner.Geography.SurfaceName(dominant));
    }

    private static Color AggregateHydrologyColor(in PhysicalGeographyCellProjection cell)
    {
        var waterShare = Math.Clamp(cell.WaterCoverage / 24f, 0f, 1f);
        var water = cell.OceanCoverage >= cell.LakeCoverage ? OceanWater : LakeWater;
        return UiTokens.TerrainTundra.Lerp(water, waterShare);
    }

    private static Color HydrologyColor(in PhysicalMicrotriangleProjection triangle) =>
        triangle.WaterKindCode switch
        {
            PhysicalWaterKindCodes.Ocean => OceanWater.Lerp(
                new Color(0.02f, 0.07f, 0.15f),
                Math.Clamp(triangle.WaterDepth / 700f, 0f, 1f)),
            PhysicalWaterKindCodes.Lake => LakeWater,
            _ when triangle.DeltaIntensity > 0 => new Color(0.20f, 0.56f, 0.54f),
            _ when UsesFilledRiverGeometry(triangle) => RiverWater,
            _ when triangle.FloodplainIntensity > 0 => new Color(0.34f, 0.53f, 0.42f),
            _ => new Color(0.38f, 0.42f, 0.38f),
        };

    private static Color RiverGeometryColor(int channel, int floodplain, int delta)
    {
        if (delta > 0)
        {
            return new Color(0.20f, 0.62f, 0.56f).Lerp(RiverWater, Math.Clamp(delta / 1000f, 0f, 0.45f));
        }

        if (channel > 0)
        {
            return new Color(0.16f, 0.36f, 0.42f).Lerp(
                RiverWater,
                Math.Clamp(channel / 1000f, 0.25f, 1f));
        }

        return floodplain > 0
            ? new Color(0.27f, 0.55f, 0.39f).Lerp(
                new Color(0.48f, 0.62f, 0.45f),
                Math.Clamp(floodplain / 1000f, 0f, 1f))
            : new Color(0.31f, 0.34f, 0.32f);
    }

    private static Color ApplyRiverGeometryTint(
        Color color,
        in PhysicalMicrotriangleProjection triangle)
    {
        if (triangle.DeltaIntensity > 0)
        {
            return color.Lerp(new Color(0.20f, 0.62f, 0.56f), 0.72f);
        }

        if (UsesFilledRiverGeometry(triangle))
        {
            return color.Lerp(RiverWater, 0.58f + (0.28f * triangle.RiverChannelStrength / 1000f));
        }

        return triangle.FloodplainIntensity > 0
            ? color.Lerp(new Color(0.35f, 0.55f, 0.40f), 0.22f)
            : color;
    }

    private void DrawRiverSpines(ReadOnlySpan<PhysicalMicrotriangleProjection> triangles)
    {
        for (var triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex++)
        {
            var triangle = triangles[triangleIndex];
            if (triangle.RiverSpineEdgeMask == 0)
            {
                continue;
            }

            var center = _owner!.Geometry.TriangleCentroid(triangleIndex);
            var width = 0.9f + Math.Min(
                2.8f,
                (triangle.RiverChannelStrength / 420f) + (triangle.RiverChannelOrder * 0.22f));
            for (var edge = 0; edge < 3; edge++)
            {
                if ((triangle.RiverSpineEdgeMask & (1 << edge)) != 0)
                {
                    DrawLine(
                        center,
                        _owner.Geometry.TriangleEdgeMidpoint(triangleIndex, edge),
                        RiverWater,
                        width,
                        true);
                }
            }
        }
    }

    private static bool UsesFilledRiverGeometry(in PhysicalMicrotriangleProjection triangle) =>
        triangle.RiverChannelStrength > 0 &&
        (triangle.RiverSpineEdgeMask == 0 ||
            triangle.RiverChannelOrder >= 2 ||
            triangle.RiverChannelStrength >= 640);

    private static Color ApplyAggregateStructuralWater(
        Color color,
        in PhysicalGeographyCellProjection cell)
    {
        if (cell.WaterCoverage > 0)
        {
            var water = cell.OceanCoverage >= cell.LakeCoverage ? OceanWater : LakeWater;
            color = color.Lerp(water, 0.34f + (0.42f * cell.WaterCoverage / 24f));
        }

        if (cell.DeltaCoverage > 0)
        {
            return color.Lerp(
                new Color(0.20f, 0.62f, 0.56f),
                0.34f + (0.42f * cell.DeltaCoverage / 24f));
        }

        return cell.RiverCoverage > 0
            ? color.Lerp(RiverWater, 0.30f + (0.48f * cell.RiverCoverage / 24f))
            : color;
    }

    private static Color ApplyWaterTint(Color color, int waterKindCode, int waterDepth) =>
        waterKindCode switch
        {
            PhysicalWaterKindCodes.Ocean => color.Lerp(
                OceanWater,
                0.48f + (0.22f * Math.Clamp(waterDepth / 700f, 0f, 1f))),
            PhysicalWaterKindCodes.Lake => color.Lerp(LakeWater, 0.58f),
            _ => color,
        };

    private void DrawTriangleCurrents(
        in PhysicalGeographyCellProjection cell,
        ReadOnlySpan<PhysicalMicrotriangleProjection> triangles)
    {
        for (var triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex++)
        {
            if (triangles[triangleIndex].WaterKindCode != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            DrawCurrentVector(
                _owner!.Geometry.TriangleCentroid(triangleIndex),
                ScalarValue(cell, triangleIndex, "ocean_current_x"),
                ScalarValue(cell, triangleIndex, "ocean_current_y"),
                ScalarValue(cell, triangleIndex, "ocean_current_strength"),
                3.2f);
        }
    }

    private void DrawAggregateCurrent(in PhysicalGeographyCellProjection cell) =>
        DrawCurrentVector(
            Vector2.Zero,
            AggregateScalarValue(cell, "ocean_current_x"),
            AggregateScalarValue(cell, "ocean_current_y"),
            AggregateScalarValue(cell, "ocean_current_strength"),
            _owner!.Geometry.HexSize * 0.52f);

    private void DrawCurrentVector(
        Vector2 start,
        int currentX,
        int currentY,
        int strength,
        float maximumLength)
    {
        var direction = new Vector2(currentX, currentY);
        if (strength <= 0 || direction.LengthSquared() < 1f)
        {
            return;
        }

        direction = direction.Normalized();
        var end = start + (direction * maximumLength * Math.Clamp(strength / 1000f, 0.2f, 1f));
        DrawLine(start, end, new Color(0.80f, 0.94f, 1f, 0.82f), 0.8f, antialiased: true);
        DrawCircle(end, 0.55f, new Color(0.94f, 0.98f, 1f, 0.88f));
    }

    private PhysicalMicrogeographyFeatureDetail FeatureDetail() =>
        PhysicalMicrogeographyRenderDecisions.FeatureDetail(
            _owner!.FeaturesEnabled,
            _owner.DrawMicrotriangles,
            _owner.DrawAggregateHexes);

    private static bool ViewUsesWaterTint(PhysicalGeographyView view) =>
        PhysicalMicrogeographyRenderDecisions.UsesWaterUnderlay(view);

    private static Color ReliefShade(
        Color color,
        int elevation,
        int relief,
        int roughness)
    {
        var altitude = Math.Clamp((elevation + 700f) / 1400f, 0f, 1f);
        var structure = Math.Clamp(Math.Max(relief, roughness) / 180f, 0f, 1f);
        var factor = 0.80f + (0.22f * altitude) + (0.16f * structure);
        return new Color(color.R * factor, color.G * factor, color.B * factor, color.A);
    }

    private static Color CompositeMagicOverGeography(Color geography, Color magic, int intensity) =>
        geography.Lerp(magic, 0.18f + (0.62f * Math.Clamp(intensity / 1000f, 0f, 1f)));

    private void DrawColoredPolygon(Vector2[] points, Color color) =>
        _canvas!.FillPolygon(points, color);

    private void DrawPolyline(
        Vector2[] points,
        Color color,
        float width,
        bool antialiased = false) =>
        _canvas!.StrokePolyline(points, color, width, antialiased);

    private void DrawLine(
        Vector2 from,
        Vector2 to,
        Color color,
        float width,
        bool antialiased = false) =>
        _canvas!.StrokeLine(from, to, color, width, antialiased);

    private void DrawCircle(Vector2 center, float radius, Color color) =>
        _canvas!.FillCircle(center, radius, color);

    private sealed class RenderContext(
        PhysicalGeographyProjection geography,
        PhysicalMicrogeographyGeometry geometry)
    {
        public PhysicalGeographyProjection Geography { get; } = geography;

        public PhysicalMicrogeographyGeometry Geometry { get; } = geometry;

        public PhysicalGeographyView View { get; set; }

        public bool FeaturesEnabled { get; set; }

        public bool DrawMicrotriangles { get; set; }

        public bool DrawAggregateHexes { get; set; }

        public Color TerrainColor(in PhysicalGeographyCellProjection cell) =>
            UiTokens.TerrainColor(cell.BaseTerrainStableName);
    }
}
