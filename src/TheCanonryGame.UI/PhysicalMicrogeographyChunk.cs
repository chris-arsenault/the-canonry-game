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
        if (_owner.FeaturesEnabled)
        {
            DrawHexFeatures(cell);
        }
    }

    private Color AggregateColor(in PhysicalGeographyCellProjection cell)
    {
        var view = _owner!.View;
        if (view is PhysicalGeographyView.Fire or PhysicalGeographyView.Aurora or
            PhysicalGeographyView.Pressure or PhysicalGeographyView.MagicComposite)
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

    private static Color AggregatePhysicalFieldColor(
        in PhysicalGeographyCellProjection cell,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Elevation => ElevationColor(cell.MeanElevation),
            PhysicalGeographyView.Temperature => TemperatureColor(cell.MeanTemperature),
            PhysicalGeographyView.Moisture => MoistureColor(cell.MeanMoisture),
            PhysicalGeographyView.Geothermal => GeothermalColor(cell.MeanGeothermal),
            PhysicalGeographyView.Hydrology => AggregateHydrologyColor(cell),
            PhysicalGeographyView.Ice => IceColor(cell.MeanIceThickness),
            PhysicalGeographyView.Fractures => FractureColor(
                System.Numerics.BitOperations.PopCount((uint)cell.FractureEdgeMask) * 1000 / 6),
            _ => UiTokens.Whisper,
        };

    private static Color AggregateMagicColor(
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
        if (_owner.FeaturesEnabled)
        {
            DrawTriangleFeatures(cell, triangles);
            DrawHexFeatures(cell);
        }
    }

    private Color TriangleColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle)
    {
        var color = BaseTriangleColor(cell, triangleIndex, triangle);
        return ViewUsesWaterTint(_owner!.View)
            ? ApplyWaterTint(color, triangle.WaterKindCode, triangle.WaterDepth)
            : color;
    }

    private Color BaseTriangleColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle)
    {
        var view = _owner!.View;
        if (view is PhysicalGeographyView.Fire or PhysicalGeographyView.Aurora or
            PhysicalGeographyView.Pressure or PhysicalGeographyView.MagicComposite)
        {
            return TriangleMagicColor(cell, triangleIndex, view);
        }

        return view <= PhysicalGeographyView.Material
            ? TriangleIdentityColor(cell, triangle, view)
            : TrianglePhysicalFieldColor(triangle, view);
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

    private static Color TrianglePhysicalFieldColor(
        in PhysicalMicrotriangleProjection triangle,
        PhysicalGeographyView view) => view switch
        {
            PhysicalGeographyView.Elevation => ElevationColor(triangle.Elevation),
            PhysicalGeographyView.Temperature => TemperatureColor(triangle.Temperature),
            PhysicalGeographyView.Moisture => MoistureColor(triangle.Moisture),
            PhysicalGeographyView.Geothermal => GeothermalColor(triangle.Geothermal),
            PhysicalGeographyView.Landform => LandformColor(triangle.LandformCode),
            PhysicalGeographyView.Hydrology => HydrologyColor(triangle),
            PhysicalGeographyView.Ice => IceColor(triangle.IceThickness),
            PhysicalGeographyView.Fractures => FractureColor(triangle.FractureConductance),
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
            _ => UiTokens.Whisper,
        };

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
            if (triangle.RiverOrder > 0 && triangle.FlowEdge >= 0)
            {
                DrawRiver(cell.Coord, index, triangle);
            }

            if (triangle.LandformCode == PhysicalLandformCodes.Peak)
            {
                DrawCircle(
                    _owner!.Geometry.TriangleCentroid(index),
                    1.4f + (0.55f * triangle.RiverOrder),
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

    private void DrawRiver(
        HexCoord coord,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle)
    {
        PhysicalMicrotriangleTopology.ResolveNeighbor(
            coord.Q,
            coord.R,
            triangleIndex,
            triangle.FlowEdge,
            out var neighborQ,
            out var neighborR,
            out var neighborTriangleIndex);
        var neighborCoord = new HexCoord(neighborQ, neighborR);
        if (!_owner!.Geography.TryGetMicrotriangle(neighborCoord, neighborTriangleIndex, out _))
        {
            return;
        }

        var start = _owner.Geometry.TriangleCentroid(triangleIndex);
        var target = _owner.Geometry.HexCenter(neighborCoord) -
            _owner.Geometry.HexCenter(coord) +
            _owner.Geometry.TriangleCentroid(neighborTriangleIndex);
        DrawLine(start, target, RiverWater, 0.8f + (triangle.RiverOrder * 0.55f), true);
    }

    private void DrawHexFeatures(in PhysicalGeographyCellProjection cell)
    {
        for (var sector = 0; sector < PhysicalGeographyCodes.HexSectorCount; sector++)
        {
            var bit = 1 << sector;
            var a = _owner!.Geometry.HexPoints[sector];
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
            _ when triangle.RiverOrder > 0 => new Color(0.20f, 0.48f, 0.50f),
            _ => new Color(0.38f, 0.42f, 0.38f),
        };

    private static Color ApplyWaterTint(Color color, int waterKindCode, int waterDepth) =>
        waterKindCode switch
        {
            PhysicalWaterKindCodes.Ocean => color.Lerp(
                OceanWater,
                0.48f + (0.22f * Math.Clamp(waterDepth / 700f, 0f, 1f))),
            PhysicalWaterKindCodes.Lake => color.Lerp(LakeWater, 0.58f),
            _ => color,
        };

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

    private static Color IceColor(int iceThickness) =>
        new Color(0.13f, 0.18f, 0.24f).Lerp(
            new Color(0.82f, 0.94f, 0.98f),
            Math.Clamp(iceThickness / 1000f, 0f, 1f));

    private static Color FractureColor(int conductance) =>
        new Color(0.10f, 0.10f, 0.14f).Lerp(
            new Color(0.96f, 0.30f, 0.12f),
            Math.Clamp(conductance / 1000f, 0f, 1f));

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

    private static bool ViewUsesWaterTint(PhysicalGeographyView view) =>
        view == PhysicalGeographyView.Terrain ||
        view is >= PhysicalGeographyView.Material and <= PhysicalGeographyView.Landform;

    private static Color ElevationShade(Color color, int elevation)
    {
        var factor = 0.72f + (0.34f * Math.Clamp((elevation + 1000f) / 2000f, 0f, 1f));
        return new Color(color.R * factor, color.G * factor, color.B * factor, color.A);
    }
}
