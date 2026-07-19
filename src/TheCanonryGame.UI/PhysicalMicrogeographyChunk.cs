#pragma warning disable CA2213
using Engine.Constants;
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

internal partial class PhysicalMicrogeographyChunk : Node2D
{
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

    private PhysicalMicrogeographyLayer? _owner;
    private PhysicalGeographyCellProjection[] _cells = [];
    private bool _cullingVisible = true;

    public Rect2 Bounds { get; private set; }

    public void Bind(
        PhysicalMicrogeographyLayer owner,
        PhysicalGeographyCellProjection[] cells,
        Rect2 bounds)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(cells);
        _owner = owner;
        _cells = cells;
        Bounds = bounds;
        QueueRedraw();
    }

    public void SetCullingVisible(bool visible)
    {
        if (_cullingVisible == visible)
        {
            return;
        }

        _cullingVisible = visible;
        Visible = visible;
        if (visible)
        {
            QueueRedraw();
        }
    }

    public void Refresh()
    {
        if (_cullingVisible)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_owner is null || (!_owner.DrawMicrotriangles && !_owner.DrawAggregateHexes))
        {
            return;
        }

        foreach (var cell in _cells)
        {
            if (!_owner.CellIsRenderable(cell))
            {
                continue;
            }

            DrawSetTransform(_owner.Geometry.HexCenter(cell.Coord));
            if (_owner.DrawMicrotriangles)
            {
                DrawMicrotriangles(cell);
            }
            else
            {
                DrawAggregate(cell);
            }
        }

        DrawSetTransform(Vector2.Zero);
    }

    private void DrawAggregate(in PhysicalGeographyCellProjection cell)
    {
        var color = AggregateColor(cell);
        DrawColoredPolygon(_owner!.Geometry.HexPoints, color);
        DrawPolyline(_owner.Geometry.ClosedHexPoints, UiTokens.Hairline, 0.75f);
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

        return view <= PhysicalGeographyView.Material
            ? AggregateIdentityColor(cell, view)
            : AggregatePhysicalFieldColor(cell, view);
    }

    private Color AggregateIdentityColor(
        in PhysicalGeographyCellProjection cell,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Terrain => _owner!.TerrainColor(cell),
            PhysicalGeographyView.Surface => UiTokens.PhysicalSurfaceColor(
                _owner!.Geography.SurfaceName(cell.PrimarySurfaceIndex)),
            PhysicalGeographyView.NaturalSurface => DominantNaturalSurfaceColor(cell),
            PhysicalGeographyView.Material => UiTokens.PhysicalMaterialColor(
                _owner!.Geography.MaterialName(cell.DominantMaterialIndex)),
            _ => UiTokens.Whisper,
        };

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

    private static Color AggregateClimateColor(
        in PhysicalGeographyCellProjection cell,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Elevation => ElevationColor(cell.MeanElevation),
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
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Fire => FireColor(cell.MeanFireIntensity),
            PhysicalGeographyView.Aurora => AuroraColor(cell.MeanAuroraIntensity),
            PhysicalGeographyView.Pressure => PressureColor(cell.MeanPressureIntensity),
            PhysicalGeographyView.MagicComposite => MagicCompositeColor(
                cell.MeanFireIntensity,
                cell.MeanAuroraIntensity,
                cell.MeanPressureIntensity),
            PhysicalGeographyView.MagicHistory => AggregateMagicHistoryColor(cell),
            _ => UiTokens.Whisper,
        };

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

        DrawPolyline(_owner.Geometry.ClosedHexPoints, UiTokens.HairlineStrong, 0.9f);
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
        return _owner.View == PhysicalGeographyView.Terrain
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
            return TriangleMagicColor(cell, triangleIndex, view);
        }

        return view <= PhysicalGeographyView.Material
            ? TriangleIdentityColor(cell, triangle, view)
            : TrianglePhysicalFieldColor(cell, triangleIndex, triangle, view);
    }

    private Color TriangleIdentityColor(
        in PhysicalGeographyCellProjection cell,
        in PhysicalMicrotriangleProjection triangle,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Terrain => ElevationShade(
                _owner!.TerrainColor(cell),
                triangle.Elevation),
            PhysicalGeographyView.Surface => UiTokens.PhysicalSurfaceColor(
                _owner!.Geography.SurfaceName(triangle.SurfaceIndex)),
            PhysicalGeographyView.NaturalSurface => UiTokens.PhysicalSurfaceColor(
                _owner!.Geography.SurfaceName(triangle.BaseSurfaceIndex)),
            PhysicalGeographyView.Material => UiTokens.PhysicalMaterialColor(
                _owner!.Geography.MaterialName(triangle.MaterialIndex)),
            _ => UiTokens.Whisper,
        };

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
            ? TriangleClimateColor(triangle, view)
            : TriangleProcessColor(cell, triangleIndex, triangle, view);
    }

    private static Color TriangleClimateColor(
        in PhysicalMicrotriangleProjection triangle,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Elevation => ElevationColor(triangle.Elevation),
            PhysicalGeographyView.Temperature => TemperatureColor(triangle.Temperature),
            PhysicalGeographyView.Moisture => MoistureColor(triangle.Moisture),
            PhysicalGeographyView.Geothermal => GeothermalColor(triangle.Geothermal),
            _ => UiTokens.Whisper,
        };

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
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Fire => ScalarColor(cell, triangleIndex, "fire_intensity", FireColor),
            PhysicalGeographyView.Aurora => ScalarColor(cell, triangleIndex, "aurora_intensity", AuroraColor),
            PhysicalGeographyView.Pressure => ScalarColor(cell, triangleIndex, "pressure_intensity", PressureColor),
            PhysicalGeographyView.MagicComposite => MagicCompositeTriangleColor(cell, triangleIndex),
            PhysicalGeographyView.MagicHistory => MagicHistoryTriangleColor(cell, triangleIndex),
            _ => UiTokens.Whisper,
        };

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

    private Color MagicCompositeTriangleColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex)
    {
        _ = _owner!.Geography.TryGetScalarValue(
            "fire_intensity",
            cell.MicrotriangleOffset + triangleIndex,
            out var fire);
        _ = _owner.Geography.TryGetScalarValue(
            "aurora_intensity",
            cell.MicrotriangleOffset + triangleIndex,
            out var aurora);
        _ = _owner.Geography.TryGetScalarValue(
            "pressure_intensity",
            cell.MicrotriangleOffset + triangleIndex,
            out var pressure);
        return MagicCompositeColor(fire, aurora, pressure);
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
            var a = _owner.Geometry.HexPoints[sector];
            var b = _owner.Geometry.HexPoints[(sector + 1) % PhysicalGeographyCodes.HexSectorCount];
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
            _ when triangle.RiverChannelStrength > 0 => RiverWater,
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

        if (triangle.RiverChannelStrength > 0)
        {
            return color.Lerp(RiverWater, 0.58f + (0.28f * triangle.RiverChannelStrength / 1000f));
        }

        return triangle.FloodplainIntensity > 0
            ? color.Lerp(new Color(0.35f, 0.55f, 0.40f), 0.22f)
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

    private static Color ElevationColor(int elevation)
    {
        var normalized = Math.Clamp((elevation + 1000f) / 2000f, 0f, 1f);
        if (normalized < 0.5f)
        {
            return new Color(0.05f, 0.16f, 0.28f).Lerp(
                new Color(0.36f, 0.48f, 0.42f),
                normalized * 2f);
        }

        return new Color(0.36f, 0.48f, 0.42f).Lerp(
            new Color(0.92f, 0.91f, 0.86f),
            (normalized - 0.5f) * 2f);
    }

    private static Color TemperatureColor(int temperature) =>
        new Color(0.18f, 0.38f, 0.68f).Lerp(
            new Color(0.88f, 0.38f, 0.18f),
            Math.Clamp(temperature / 1000f, 0f, 1f));

    private static Color MoistureColor(int moisture) =>
        new Color(0.54f, 0.42f, 0.25f).Lerp(
            new Color(0.18f, 0.62f, 0.68f),
            Math.Clamp(moisture / 1000f, 0f, 1f));

    private static Color GeothermalColor(int geothermal) =>
        new Color(0.17f, 0.18f, 0.23f).Lerp(
            new Color(0.92f, 0.30f, 0.12f),
            Math.Clamp(geothermal / 1000f, 0f, 1f));

    private static Color OceanCurrentColor(int strength, int heat)
    {
        var thermal = new Color(0.08f, 0.20f, 0.38f).Lerp(
            new Color(0.20f, 0.74f, 0.66f),
            Math.Clamp(heat / 1000f, 0f, 1f));
        return new Color(0.06f, 0.09f, 0.14f).Lerp(
            thermal,
            Math.Clamp(strength / 1000f, 0f, 1f));
    }

    private static Color WaveExposureColor(int exposure) =>
        new Color(0.05f, 0.13f, 0.22f).Lerp(
            new Color(0.68f, 0.90f, 0.96f),
            Math.Clamp(exposure / 1000f, 0f, 1f));

    private static Color PrecipitationColor(int rainfall, int snowfall)
    {
        var total = Math.Clamp((rainfall + snowfall) / 1000f, 0f, 1f);
        var snowShare = snowfall / (float)Math.Max(1, rainfall + snowfall);
        var wet = new Color(0.15f, 0.42f, 0.72f).Lerp(
            new Color(0.88f, 0.95f, 0.98f),
            snowShare);
        return new Color(0.18f, 0.14f, 0.12f).Lerp(wet, total);
    }

    private static Color SeaIceSuitabilityColor(int suitability) =>
        new Color(0.08f, 0.16f, 0.28f).Lerp(
            new Color(0.88f, 0.97f, 1f),
            Math.Clamp(suitability / 1000f, 0f, 1f));

    private static Color SedimentColor(int thickness, int deposition)
    {
        var amount = Math.Clamp((thickness + (deposition / 2f)) / 180f, 0f, 1f);
        return new Color(0.13f, 0.14f, 0.16f).Lerp(
            new Color(0.73f, 0.58f, 0.35f),
            amount);
    }

    private static Color ErosionHistoryColor(int fluvial, int glacial, int coastal)
    {
        var maximum = Math.Max(1f, Math.Max(fluvial, Math.Max(glacial, coastal)));
        var intensity = Math.Clamp((fluvial + glacial + coastal) / 240f, 0f, 1f);
        var process = new Color(
            0.18f + (0.22f * coastal / maximum),
            0.16f + (0.55f * fluvial / maximum),
            0.20f + (0.66f * glacial / maximum));
        return new Color(0.08f, 0.09f, 0.11f).Lerp(process, intensity);
    }

    private static Color MaximumGlaciationColor(int thickness) =>
        new Color(0.16f, 0.12f, 0.18f).Lerp(
            new Color(0.78f, 0.94f, 1f),
            Math.Clamp(thickness / 1000f, 0f, 1f));

    private static Color IceColor(int iceThickness) =>
        new Color(0.13f, 0.18f, 0.24f).Lerp(
            new Color(0.82f, 0.94f, 0.98f),
            Math.Clamp(iceThickness / 1000f, 0f, 1f));

    private static Color FractureColor(int conductance) =>
        new Color(0.10f, 0.10f, 0.14f).Lerp(
            new Color(0.96f, 0.30f, 0.12f),
            Math.Clamp(conductance / 1000f, 0f, 1f));

    private static Color TectonicProvinceColor(int provinceId) => provinceId <= 0
        ? UiTokens.Ink
        : Color.FromHsv((provinceId * 0.618034f) % 1f, 0.58f, 0.84f);

    private static Color CrustTypeColor(int crustType) => crustType switch
    {
        1 => new Color(0.12f, 0.30f, 0.48f),
        2 => new Color(0.66f, 0.51f, 0.30f),
        _ => UiTokens.Ink,
    };

    private static Color TectonicBoundaryColor(int boundaryType) => boundaryType switch
    {
        1 => new Color(0.95f, 0.30f, 0.16f),
        2 => new Color(0.20f, 0.65f, 0.92f),
        3 => new Color(0.94f, 0.70f, 0.22f),
        _ => UiTokens.Ink,
    };

    private static Color TectonicStrainColor(int strain) =>
        new Color(0.08f, 0.10f, 0.16f).Lerp(
            new Color(0.94f, 0.36f, 0.18f),
            Math.Clamp(strain / 1000f, 0f, 1f));

    private static Color FireColor(int intensity) =>
        new Color(0.10f, 0.08f, 0.10f).Lerp(
            new Color(1.00f, 0.28f, 0.05f),
            Math.Clamp(intensity / 1000f, 0f, 1f));

    private static Color AuroraColor(int intensity) =>
        new Color(0.08f, 0.10f, 0.17f).Lerp(
            new Color(0.26f, 0.96f, 0.73f),
            Math.Clamp(intensity / 1000f, 0f, 1f));

    private static Color PressureColor(int intensity) =>
        new Color(0.07f, 0.09f, 0.15f).Lerp(
            new Color(0.70f, 0.20f, 0.78f),
            Math.Clamp(intensity / 1000f, 0f, 1f));

    private static Color MagicCompositeColor(int fire, int aurora, int pressure)
    {
        var fireStrength = Math.Clamp(fire / 1000f, 0f, 1f);
        var auroraStrength = Math.Clamp(aurora / 1000f, 0f, 1f);
        var pressureStrength = Math.Clamp(pressure / 1000f, 0f, 1f);
        return new Color(
            Math.Clamp(0.07f + (0.82f * fireStrength) + (0.38f * pressureStrength), 0f, 1f),
            Math.Clamp(0.08f + (0.76f * auroraStrength) + (0.12f * fireStrength), 0f, 1f),
            Math.Clamp(0.13f + (0.56f * auroraStrength) + (0.68f * pressureStrength), 0f, 1f));
    }

    private static Color MagicHistoryColor(
        int fire,
        int aurora,
        int pressure,
        int coastalPressure,
        int firstEpoch,
        int epochCount)
    {
        if (firstEpoch < 0)
        {
            return new Color(0.07f, 0.08f, 0.11f);
        }

        var history = MagicCompositeColor(fire, aurora, pressure);
        history = history.Lerp(
            new Color(0.70f, 0.48f, 0.84f),
            Math.Clamp(coastalPressure / 1000f, 0f, 1f) * 0.42f);
        var ancient = 1f - Math.Clamp(firstEpoch / (float)Math.Max(1, epochCount - 1), 0f, 1f);
        return history.Lerp(new Color(0.94f, 0.78f, 0.42f), ancient * 0.24f);
    }

    private static Color LandformColor(int landformCode) =>
        landformCode >= PhysicalLandformCodes.UValley
            ? GlacialLandformColor(landformCode)
            : StandardLandformColor(landformCode);

    private static Color StandardLandformColor(int landformCode) => landformCode switch
    {
        PhysicalLandformCodes.Plain => new Color(0.42f, 0.52f, 0.42f),
        PhysicalLandformCodes.Slope => new Color(0.52f, 0.49f, 0.40f),
        PhysicalLandformCodes.Valley => new Color(0.28f, 0.48f, 0.42f),
        PhysicalLandformCodes.Basin => new Color(0.30f, 0.36f, 0.44f),
        PhysicalLandformCodes.Ridge => new Color(0.67f, 0.62f, 0.54f),
        PhysicalLandformCodes.Peak => new Color(0.90f, 0.88f, 0.82f),
        PhysicalLandformCodes.Pass => new Color(0.82f, 0.57f, 0.28f),
        PhysicalLandformCodes.Seabed => new Color(0.12f, 0.23f, 0.34f),
        _ => UiTokens.Whisper,
    };

    private static Color GlacialLandformColor(int landformCode) => landformCode switch
    {
        PhysicalLandformCodes.UValley => new Color(0.32f, 0.52f, 0.50f),
        PhysicalLandformCodes.Cirque => new Color(0.58f, 0.72f, 0.76f),
        PhysicalLandformCodes.Fjord => new Color(0.10f, 0.31f, 0.43f),
        PhysicalLandformCodes.Moraine => new Color(0.44f, 0.39f, 0.33f),
        _ => UiTokens.Whisper,
    };

    private PhysicalMicrogeographyFeatureDetail FeatureDetail() =>
        PhysicalMicrogeographyRenderDecisions.FeatureDetail(
            _owner!.FeaturesEnabled,
            _owner.DrawMicrotriangles,
            _owner.DrawAggregateHexes);

    private static bool ViewUsesWaterTint(PhysicalGeographyView view) =>
        PhysicalMicrogeographyRenderDecisions.UsesWaterUnderlay(view);

    private static Color ElevationShade(Color color, int elevation)
    {
        var factor = 0.72f + (0.34f * Math.Clamp((elevation + 1000f) / 2000f, 0f, 1f));
        return new Color(color.R * factor, color.G * factor, color.B * factor, color.A);
    }
}
