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

    private PhysicalMicrogeographyLayer? _owner;
    private PhysicalGeographyCellProjection[] _cells = [];

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
        var color = _owner!.View switch
        {
            PhysicalGeographyView.Terrain => _owner.TerrainColor(cell),
            PhysicalGeographyView.Material => UiTokens.PhysicalMaterialColor(
                _owner.Geography.MaterialName(cell.DominantMaterialIndex)),
            PhysicalGeographyView.Elevation => ElevationColor(cell.MeanElevation),
            PhysicalGeographyView.Temperature => TemperatureColor(cell.MeanTemperature),
            PhysicalGeographyView.Moisture => MoistureColor(cell.MeanMoisture),
            PhysicalGeographyView.Geothermal => GeothermalColor(cell.MeanGeothermal),
            PhysicalGeographyView.Hydrology => AggregateHydrologyColor(cell),
            _ => _owner.TerrainColor(cell),
        };
        DrawColoredPolygon(_owner.Geometry.HexPoints, color);
        DrawPolyline(_owner.Geometry.ClosedHexPoints, UiTokens.Hairline, 0.75f);
        if (_owner.FeaturesEnabled)
        {
            DrawHexFeatures(cell);
        }
    }

    private void DrawMicrotriangles(in PhysicalGeographyCellProjection cell)
    {
        var triangles = _owner!.Geography.MicrotrianglesFor(cell);
        for (var index = 0; index < triangles.Length; index++)
        {
            var triangle = triangles[index];
            DrawColoredPolygon(
                _owner.Geometry.TrianglePoints(index),
                TriangleColor(cell, triangle));
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
        in PhysicalMicrotriangleProjection triangle)
    {
        var color = BaseTriangleColor(cell, triangle);
        return _owner!.View == PhysicalGeographyView.Hydrology
            ? color
            : ApplyWaterTint(color, triangle.WaterKindCode, triangle.WaterDepth);
    }

    private Color BaseTriangleColor(
        in PhysicalGeographyCellProjection cell,
        in PhysicalMicrotriangleProjection triangle) =>
        _owner!.View switch
        {
            PhysicalGeographyView.Terrain => ElevationShade(
                _owner.TerrainColor(cell),
                triangle.Elevation),
            PhysicalGeographyView.Material => UiTokens.PhysicalMaterialColor(
                _owner.Geography.MaterialName(triangle.MaterialIndex)),
            PhysicalGeographyView.Elevation => ElevationColor(triangle.Elevation),
            PhysicalGeographyView.Temperature => TemperatureColor(triangle.Temperature),
            PhysicalGeographyView.Moisture => MoistureColor(triangle.Moisture),
            PhysicalGeographyView.Geothermal => GeothermalColor(triangle.Geothermal),
            PhysicalGeographyView.Landform => LandformColor(triangle.LandformCode),
            PhysicalGeographyView.Hydrology => HydrologyColor(triangle),
            _ => UiTokens.Whisper,
        };

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
        }
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

    private static Color LandformColor(int landformCode) => landformCode switch
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

    private static Color ElevationShade(Color color, int elevation)
    {
        var factor = 0.72f + (0.34f * Math.Clamp((elevation + 1000f) / 2000f, 0f, 1f));
        return new Color(color.R * factor, color.G * factor, color.B * factor, color.A);
    }
}
