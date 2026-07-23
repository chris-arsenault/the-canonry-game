using Engine.Constants;
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

public readonly record struct PhysicalMap3DVertexKey(int X, int Z);

public readonly record struct PhysicalMap3DTriangleSample(
    PhysicalMap3DVertexKey A,
    PhysicalMap3DVertexKey B,
    PhysicalMap3DVertexKey C,
    float Elevation);

public readonly record struct PhysicalMap3DFace(
    int CellIndex,
    HexCoord Coord,
    int TriangleIndex,
    Vector3 A,
    Vector3 B,
    Vector3 C)
{
    public Vector3 Center => (A + B + C) / 3f;
}

public sealed class PhysicalMicrogeographyChunkMeshData
{
    private readonly Vector3[] _groundVertices;
    private readonly Vector3[] _groundNormals;
    private readonly Vector2[] _groundUvs;
    private readonly PhysicalMap3DFace[] _faces;
    private readonly Vector3[] _waterVertices;
    private readonly Color[] _waterColors;
    private readonly Vector3[] _structuralLineVertices;
    private readonly Color[] _structuralLineColors;
    private readonly Vector3[] _featureLineVertices;
    private readonly Color[] _featureLineColors;

    internal PhysicalMicrogeographyChunkMeshData(
        int chunkQ,
        int chunkR,
        Vector3[] groundVertices,
        Vector3[] groundNormals,
        Vector2[] groundUvs,
        PhysicalMap3DFace[] faces,
        Vector3[] waterVertices,
        Color[] waterColors,
        Vector3[] structuralLineVertices,
        Color[] structuralLineColors,
        Vector3[] featureLineVertices,
        Color[] featureLineColors)
    {
        ChunkQ = chunkQ;
        ChunkR = chunkR;
        _groundVertices = groundVertices;
        _groundNormals = groundNormals;
        _groundUvs = groundUvs;
        _faces = faces;
        _waterVertices = waterVertices;
        _waterColors = waterColors;
        _structuralLineVertices = structuralLineVertices;
        _structuralLineColors = structuralLineColors;
        _featureLineVertices = featureLineVertices;
        _featureLineColors = featureLineColors;
    }

    public int ChunkQ { get; }

    public int ChunkR { get; }

    internal Vector3[] GroundVertices() => _groundVertices;

    internal Vector3[] GroundNormals() => _groundNormals;

    internal Vector2[] GroundUvs() => _groundUvs;

    internal PhysicalMap3DFace[] Faces() => _faces;

    internal Vector3[] WaterVertices() => _waterVertices;

    internal Color[] WaterColors() => _waterColors;

    internal Vector3[] StructuralLineVertices() => _structuralLineVertices;

    internal Color[] StructuralLineColors() => _structuralLineColors;

    internal Vector3[] FeatureLineVertices() => _featureLineVertices;

    internal Color[] FeatureLineColors() => _featureLineColors;
}

public sealed class PhysicalMicrogeographyMeshData
{
    private readonly Dictionary<HexCoord, PhysicalMap3DFace[]> _facesByCoord;
    private readonly Dictionary<PhysicalMap3DVertexKey, float> _cornerHeights;

    internal PhysicalMicrogeographyMeshData(
        IReadOnlyList<PhysicalMicrogeographyChunkMeshData> chunks,
        Dictionary<HexCoord, PhysicalMap3DFace[]> facesByCoord,
        Dictionary<PhysicalMap3DVertexKey, float> cornerHeights,
        float maximumAbsoluteHeight)
    {
        Chunks = chunks;
        _facesByCoord = facesByCoord;
        _cornerHeights = cornerHeights;
        MaximumAbsoluteHeight = maximumAbsoluteHeight;
    }

    public IReadOnlyList<PhysicalMicrogeographyChunkMeshData> Chunks { get; }

    public float MaximumAbsoluteHeight { get; }

    public bool TryGetFaces(HexCoord coord, out PhysicalMap3DFace[] faces) =>
        _facesByCoord.TryGetValue(coord, out faces!);

    public bool TryGetCornerHeight(PhysicalMap3DVertexKey key, out float height) =>
        _cornerHeights.TryGetValue(key, out height);
}

/// <summary>
/// Reconstructs a watertight display surface from one elevation sample per
/// physical microtriangle. This is immutable presentation geometry; generated
/// triangle values remain authoritative and are not changed by reconstruction.
/// </summary>
public sealed class PhysicalMicrogeographyMeshBuilder
{
    private const int ChunkCoordinateSize = 8;
    private const float ElevationDisplayScale = 0.14f;
    private const float FineElevationSubunitsPerUnit = 1024f;
    private const float VertexKeyScale = 64f;
    private const float WaterSurfaceOffset = 0.08f;
    private const float OverlayHeightOffset = 0.12f;

    private static readonly Color RiverLine = new(0.28f, 0.72f, 0.94f, 0.96f);
    private static readonly Color OceanOutline = new(0.12f, 0.36f, 0.60f, 0.90f);
    private static readonly Color LakeOutline = new(0.18f, 0.60f, 0.72f, 0.90f);
    private static readonly Color FloodplainOutline = new(0.31f, 0.57f, 0.43f, 0.82f);
    private static readonly Color PeakFeature = new(0.92f, 0.94f, 0.96f, 1f);
    private static readonly Color WaterfallFeature = new(0.80f, 0.94f, 0.99f, 1f);
    private static readonly Color SignificantFeature = new(0.92f, 0.72f, 0.28f, 1f);

    private static readonly Vector2[] TriangleUvs =
    [
        new Vector2(0f, 0f),
        new Vector2(0f, 1f),
        new Vector2(1f, 0f),
    ];

    private readonly PhysicalGeographyProjection _geography;
    private readonly PhysicalMicrogeographyGeometry _geometry;
    private readonly bool _hasFineElevation;

    public PhysicalMicrogeographyMeshBuilder(
        PhysicalGeographyProjection geography,
        PhysicalMicrogeographyGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geography);
        ArgumentNullException.ThrowIfNull(geometry);
        _geography = geography;
        _geometry = geometry;
        _hasFineElevation = geography.ScalarFieldNames.Contains(
            "fine_elevation",
            StringComparer.Ordinal);
    }

    public PhysicalMicrogeographyMeshData Build()
    {
        var cornerHeights = ReconstructCornerHeights(BuildTriangleSamples());
        var cornerNormals = BuildCornerNormals(cornerHeights);
        var waterHeights = BuildWaterBodyHeights();
        var cellsByChunk = _geography.Cells.GroupBy(static cell => new ChunkKey(
            FloorDivide(cell.Coord.Q, ChunkCoordinateSize),
            FloorDivide(cell.Coord.R, ChunkCoordinateSize)));
        var chunks = new List<PhysicalMicrogeographyChunkMeshData>();
        var facesByCoord = new Dictionary<HexCoord, PhysicalMap3DFace[]>(_geography.Cells.Count);
        var maximumAbsoluteHeight = 0f;

        foreach (var chunkCells in cellsByChunk)
        {
            var chunk = BuildChunk(
                chunkCells.Key,
                chunkCells,
                cornerHeights,
                cornerNormals,
                waterHeights,
                facesByCoord,
                ref maximumAbsoluteHeight);
            chunks.Add(chunk);
        }

        return new PhysicalMicrogeographyMeshData(
            chunks,
            facesByCoord,
            cornerHeights,
            maximumAbsoluteHeight);
    }

    public static Dictionary<PhysicalMap3DVertexKey, float> ReconstructCornerHeights(
        IEnumerable<PhysicalMap3DTriangleSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        var accumulators = new Dictionary<PhysicalMap3DVertexKey, HeightAccumulator>();
        foreach (var sample in samples)
        {
            Accumulate(sample.A, sample.Elevation, accumulators);
            Accumulate(sample.B, sample.Elevation, accumulators);
            Accumulate(sample.C, sample.Elevation, accumulators);
        }

        return accumulators.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.Total / pair.Value.Count);
    }

    public static PhysicalMap3DVertexKey VertexKey(Vector2 point) => new(
        checked((int)MathF.Round(point.X * VertexKeyScale, MidpointRounding.AwayFromZero)),
        checked((int)MathF.Round(point.Y * VertexKeyScale, MidpointRounding.AwayFromZero)));

    private List<PhysicalMap3DTriangleSample> BuildTriangleSamples()
    {
        var samples = new List<PhysicalMap3DTriangleSample>(
            checked(_geography.Cells.Count * PhysicalGeographyCodes.MicrotrianglesPerHex));
        foreach (var cell in _geography.Cells)
        {
            var center = _geometry.HexCenter(cell.Coord);
            var triangles = _geography.MicrotrianglesFor(cell);
            for (var triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex++)
            {
                var points = _geometry.TrianglePoints(triangleIndex);
                var elevation = TriangleElevation(cell, triangleIndex, triangles[triangleIndex]) *
                    ElevationDisplayScale;
                samples.Add(new PhysicalMap3DTriangleSample(
                    VertexKey(center + points[0]),
                    VertexKey(center + points[1]),
                    VertexKey(center + points[2]),
                    elevation));
            }
        }

        return samples;
    }

    private Dictionary<PhysicalMap3DVertexKey, Vector3> BuildCornerNormals(
        Dictionary<PhysicalMap3DVertexKey, float> cornerHeights)
    {
        var normalSums = new Dictionary<PhysicalMap3DVertexKey, Vector3>(cornerHeights.Count);
        foreach (var cell in _geography.Cells)
        {
            var center = _geometry.HexCenter(cell.Coord);
            for (var triangleIndex = 0;
                triangleIndex < PhysicalGeographyCodes.MicrotrianglesPerHex;
                triangleIndex++)
            {
                var face = TopFacingFace(center, triangleIndex, cornerHeights);
                var normal = UpwardNormal(face.A, face.B, face.C);
                AddNormal(face.KeyA, normal, normalSums);
                AddNormal(face.KeyB, normal, normalSums);
                AddNormal(face.KeyC, normal, normalSums);
            }
        }

        foreach (var key in normalSums.Keys.ToArray())
        {
            normalSums[key] = normalSums[key].Normalized();
        }

        return normalSums;
    }

    private Dictionary<int, float> BuildWaterBodyHeights()
    {
        var samplesByBody = new Dictionary<int, List<float>>();
        foreach (var cell in _geography.Cells)
        {
            var triangles = _geography.MicrotrianglesFor(cell);
            for (var triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex++)
            {
                var triangle = triangles[triangleIndex];
                if (triangle.WaterKindCode == PhysicalWaterKindCodes.None ||
                    triangle.WaterBodyId <= 0 ||
                    triangle.WaterDepth <= 0)
                {
                    continue;
                }

                if (!samplesByBody.TryGetValue(triangle.WaterBodyId, out var samples))
                {
                    samples = [];
                    samplesByBody.Add(triangle.WaterBodyId, samples);
                }

                samples.Add(TriangleElevation(cell, triangleIndex, triangle) + triangle.WaterDepth);
            }
        }

        var heights = new Dictionary<int, float>(samplesByBody.Count);
        foreach (var (bodyId, samples) in samplesByBody)
        {
            samples.Sort();
            heights.Add(
                bodyId,
                (samples[samples.Count / 2] * ElevationDisplayScale) + WaterSurfaceOffset);
        }

        return heights;
    }

    private PhysicalMicrogeographyChunkMeshData BuildChunk(
        ChunkKey key,
        IEnumerable<PhysicalGeographyCellProjection> sourceCells,
        Dictionary<PhysicalMap3DVertexKey, float> cornerHeights,
        Dictionary<PhysicalMap3DVertexKey, Vector3> cornerNormals,
        Dictionary<int, float> waterHeights,
        Dictionary<HexCoord, PhysicalMap3DFace[]> facesByCoord,
        ref float maximumAbsoluteHeight)
    {
        var cells = sourceCells.ToArray();
        var buffers = new ChunkBuildBuffers(cells.Length);
        foreach (var cell in cells)
        {
            BuildCell(
                cell,
                cornerHeights,
                cornerNormals,
                waterHeights,
                buffers,
                facesByCoord,
                ref maximumAbsoluteHeight);
        }

        return buffers.Build(key.Q, key.R);
    }

    private void BuildCell(
        in PhysicalGeographyCellProjection cell,
        Dictionary<PhysicalMap3DVertexKey, float> cornerHeights,
        Dictionary<PhysicalMap3DVertexKey, Vector3> cornerNormals,
        Dictionary<int, float> waterHeights,
        ChunkBuildBuffers buffers,
        Dictionary<HexCoord, PhysicalMap3DFace[]> facesByCoord,
        ref float maximumAbsoluteHeight)
    {
        var cellFaces = new List<PhysicalMap3DFace>(
            PhysicalGeographyCodes.MicrotrianglesPerHex * 3);
        var center = _geometry.HexCenter(cell.Coord);
        var triangles = _geography.MicrotrianglesFor(cell);
        for (var triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex++)
        {
            var rendered = AddGroundFaces(
                cell,
                triangleIndex,
                triangles[triangleIndex],
                center,
                cornerHeights,
                cornerNormals,
                buffers,
                ref maximumAbsoluteHeight);
            cellFaces.AddRange(rendered.Faces);
            AddOverlays(
                center,
                triangleIndex,
                triangles[triangleIndex],
                rendered,
                waterHeights,
                buffers);
        }

        facesByCoord.Add(cell.Coord, cellFaces.ToArray());
    }

    private RenderedMicrotriangle AddGroundFaces(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle,
        Vector2 center,
        Dictionary<PhysicalMap3DVertexKey, float> cornerHeights,
        Dictionary<PhysicalMap3DVertexKey, Vector3> cornerNormals,
        ChunkBuildBuffers buffers,
        ref float maximumAbsoluteHeight)
    {
        var boundary = TopFacingFace(center, triangleIndex, cornerHeights);
        var triangleCenter = center + _geometry.TriangleCentroid(triangleIndex);
        var centerPoint = new Vector3(
            triangleCenter.X,
            TriangleElevation(cell, triangleIndex, triangle) * ElevationDisplayScale,
            triangleCenter.Y);
        var fan = BuildTopFacingFan(centerPoint, boundary.A, boundary.B, boundary.C);
        var centerNormal = fan
            .Select(static face => UpwardNormal(face.A, face.B, face.C))
            .Aggregate(Vector3.Zero, static (sum, normal) => sum + normal)
            .Normalized();
        var faces = new PhysicalMap3DFace[fan.Length];
        for (var index = 0; index < fan.Length; index++)
        {
            var face = fan[index];
            buffers.AddGround(
                face.A,
                face.B,
                face.C,
                centerNormal,
                BoundaryNormal(face.B, boundary, cornerNormals),
                BoundaryNormal(face.C, boundary, cornerNormals));
            faces[index] = new PhysicalMap3DFace(
                cell.CellIndex,
                cell.Coord,
                triangleIndex,
                face.A,
                face.B,
                face.C);
            buffers.Faces.Add(faces[index]);
        }

        maximumAbsoluteHeight = Math.Max(
            maximumAbsoluteHeight,
            Math.Max(
                Math.Abs(centerPoint.Y),
                Math.Max(
                    Math.Abs(boundary.A.Y),
                    Math.Max(Math.Abs(boundary.B.Y), Math.Abs(boundary.C.Y)))));
        return new RenderedMicrotriangle(boundary, faces);
    }

    private void AddOverlays(
        Vector2 center,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle,
        in RenderedMicrotriangle rendered,
        Dictionary<int, float> waterHeights,
        ChunkBuildBuffers buffers)
    {
        AddWaterFace(
            triangle,
            rendered.Boundary.A,
            rendered.Boundary.B,
            rendered.Boundary.C,
            waterHeights,
            buffers.WaterVertices,
            buffers.WaterColors);
        AddStructuralLines(
            center,
            triangleIndex,
            triangle,
            rendered.Faces,
            buffers.StructuralLineVertices,
            buffers.StructuralLineColors);
        AddFeatureLines(
            center,
            triangleIndex,
            triangle,
            rendered.Faces,
            buffers.FeatureLineVertices,
            buffers.FeatureLineColors);
    }

    private void AddStructuralLines(
        Vector2 center,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle,
        IReadOnlyList<PhysicalMap3DFace> terrainFaces,
        List<Vector3> vertices,
        List<Color> colors)
    {
        AddAreaOutlineLines(center, triangleIndex, triangle, terrainFaces, vertices, colors);

        var wide = UsesFilledRiverGeometry(triangle) ||
            triangle.WaterKindCode == PhysicalWaterKindCodes.Lake;
        var mask = wide ? triangle.ThalwegEdgeMask : triangle.RiverSpineEdgeMask;
        if (mask == 0 || (wide && !triangle.IsRiverThalweg))
        {
            return;
        }

        var triangleCenter = center + _geometry.TriangleCentroid(triangleIndex);
        for (var edge = 0; edge < 3; edge++)
        {
            if ((mask & (1 << edge)) == 0)
            {
                continue;
            }

            var edgeCenter = center + _geometry.TriangleEdgeMidpoint(triangleIndex, edge);
            AddTerrainLine(triangleCenter, edgeCenter, terrainFaces, RiverLine, vertices, colors);
        }
    }

    private void AddAreaOutlineLines(
        Vector2 center,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle,
        IReadOnlyList<PhysicalMap3DFace> terrainFaces,
        List<Vector3> vertices,
        List<Color> colors)
    {
        if (triangle.AreaOutlineEdgeMask == 0)
        {
            return;
        }

        var color = triangle.WaterKindCode switch
        {
            PhysicalWaterKindCodes.Ocean => OceanOutline,
            PhysicalWaterKindCodes.Lake => LakeOutline,
            _ => FloodplainOutline,
        };
        for (var edge = 0; edge < 3; edge++)
        {
            if ((triangle.AreaOutlineEdgeMask & (1 << edge)) == 0)
            {
                continue;
            }

            _geometry.TriangleEdgeEndpoints(triangleIndex, edge, out var first, out var second);
            AddTerrainLine(
                center + first,
                center + second,
                terrainFaces,
                color,
                vertices,
                colors);
        }
    }

    private void AddFeatureLines(
        Vector2 center,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle,
        IReadOnlyList<PhysicalMap3DFace> terrainFaces,
        List<Vector3> vertices,
        List<Color> colors)
    {
        var color = triangle.SignificantFeatureFlags != 0
            ? SignificantFeature
            : triangle.LandformCode switch
            {
                PhysicalLandformCodes.Peak => PeakFeature,
                PhysicalLandformCodes.Waterfall => WaterfallFeature,
                _ => default,
            };
        if (color.A <= 0f)
        {
            return;
        }

        var point = center + _geometry.TriangleCentroid(triangleIndex);
        var radius = Math.Max(0.8f, _geometry.HexSize * 0.025f);
        AddTerrainLine(
            point + new Vector2(-radius, 0f),
            point + new Vector2(radius, 0f),
            terrainFaces,
            color,
            vertices,
            colors);
        AddTerrainLine(
            point + new Vector2(0f, -radius),
            point + new Vector2(0f, radius),
            terrainFaces,
            color,
            vertices,
            colors);
    }

    private static void AddTerrainLine(
        Vector2 first,
        Vector2 second,
        IReadOnlyList<PhysicalMap3DFace> terrainFaces,
        Color color,
        List<Vector3> vertices,
        List<Color> colors)
    {
        vertices.Add(TerrainPoint(first, terrainFaces));
        vertices.Add(TerrainPoint(second, terrainFaces));
        colors.Add(color);
        colors.Add(color);
    }

    private static Vector3 TerrainPoint(
        Vector2 point,
        IReadOnlyList<PhysicalMap3DFace> terrainFaces) => new(
        point.X,
        InterpolatedHeight(point, terrainFaces) + OverlayHeightOffset,
        point.Y);

    private static float InterpolatedHeight(
        Vector2 point,
        IReadOnlyList<PhysicalMap3DFace> terrainFaces)
    {
        foreach (var face in terrainFaces)
        {
            if (TryInterpolatedHeight(point, face, out var height))
            {
                return height;
            }
        }

        return terrainFaces[0].Center.Y;
    }

    private static bool TryInterpolatedHeight(
        Vector2 point,
        in PhysicalMap3DFace face,
        out float height)
    {
        var a = new Vector2(face.A.X, face.A.Z);
        var b = new Vector2(face.B.X, face.B.Z);
        var c = new Vector2(face.C.X, face.C.Z);
        var denominator = ((b.Y - c.Y) * (a.X - c.X)) + ((c.X - b.X) * (a.Y - c.Y));
        if (Math.Abs(denominator) < 0.000001f)
        {
            height = face.Center.Y;
            return false;
        }

        var first = (((b.Y - c.Y) * (point.X - c.X)) +
            ((c.X - b.X) * (point.Y - c.Y))) / denominator;
        var second = (((c.Y - a.Y) * (point.X - c.X)) +
            ((a.X - c.X) * (point.Y - c.Y))) / denominator;
        var third = 1f - first - second;
        height = (first * face.A.Y) + (second * face.B.Y) +
            ((1f - first - second) * face.C.Y);
        const float tolerance = -0.0001f;
        return first >= tolerance && second >= tolerance && third >= tolerance;
    }

    private static bool UsesFilledRiverGeometry(in PhysicalMicrotriangleProjection triangle) =>
        triangle.RiverChannelStrength > 0 &&
        (triangle.RiverSpineEdgeMask == 0 ||
            triangle.RiverChannelOrder >= 2 ||
            triangle.RiverChannelStrength >= 640);

    private static void AddWaterFace(
        in PhysicalMicrotriangleProjection triangle,
        Vector3 groundA,
        Vector3 groundB,
        Vector3 groundC,
        Dictionary<int, float> waterHeights,
        List<Vector3> vertices,
        List<Color> colors)
    {
        if (triangle.WaterKindCode == PhysicalWaterKindCodes.None ||
            !waterHeights.TryGetValue(triangle.WaterBodyId, out var height))
        {
            return;
        }

        vertices.Add(new Vector3(groundA.X, height, groundA.Z));
        vertices.Add(new Vector3(groundB.X, height, groundB.Z));
        vertices.Add(new Vector3(groundC.X, height, groundC.Z));
        var color = triangle.WaterKindCode == PhysicalWaterKindCodes.Ocean
            ? new Color(0.06f, 0.24f, 0.42f, 0.52f)
            : new Color(0.10f, 0.42f, 0.52f, 0.50f);
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    private PhysicalMap3DVertexKey[] TriangleKeys(Vector2 center, int triangleIndex)
    {
        var points = _geometry.TrianglePoints(triangleIndex);
        return
        [
            VertexKey(center + points[0]),
            VertexKey(center + points[1]),
            VertexKey(center + points[2]),
        ];
    }

    private TopFacingGroundFace TopFacingFace(
        Vector2 center,
        int triangleIndex,
        Dictionary<PhysicalMap3DVertexKey, float> cornerHeights)
    {
        var keys = TriangleKeys(center, triangleIndex);
        var keyA = keys[0];
        var keyB = keys[1];
        var keyC = keys[2];
        var a = Position(keyA, cornerHeights[keyA]);
        var b = Position(keyB, cornerHeights[keyB]);
        var c = Position(keyC, cornerHeights[keyC]);
        var oriented = OrientTopFacing(a, b, c);
        if (oriented.Swapped)
        {
            (keyB, keyC) = (keyC, keyB);
        }

        return new TopFacingGroundFace(
            keyA,
            keyB,
            keyC,
            a,
            oriented.B,
            oriented.C);
    }

    // Godot treats clockwise triangle winding as the front face. In the XZ
    // terrain plane that means its geometric cross product points downward
    // when the visible face points toward a camera above the map.
    internal static (Vector3 B, Vector3 C, bool Swapped) OrientTopFacing(
        Vector3 a,
        Vector3 b,
        Vector3 c) =>
        (b - a).Cross(c - a).Y <= 0f
            ? (b, c, false)
            : (c, b, true);

    internal static (Vector3 A, Vector3 B, Vector3 C)[] BuildTopFacingFan(
        Vector3 center,
        Vector3 a,
        Vector3 b,
        Vector3 c) =>
    [
        TopFacingTriangle(center, a, b),
        TopFacingTriangle(center, b, c),
        TopFacingTriangle(center, c, a),
    ];

    // Lighting normals remain upward even though Godot's clockwise front-face
    // convention makes the ordered face's geometric cross point downward.
    internal static Vector3 UpwardNormal(Vector3 a, Vector3 b, Vector3 c) =>
        -((b - a).Cross(c - a).Normalized());

    private static (Vector3 A, Vector3 B, Vector3 C) TopFacingTriangle(
        Vector3 first,
        Vector3 second,
        Vector3 third)
    {
        var oriented = OrientTopFacing(first, second, third);
        return (first, oriented.B, oriented.C);
    }

    private static Vector3 BoundaryNormal(
        Vector3 point,
        in TopFacingGroundFace boundary,
        Dictionary<PhysicalMap3DVertexKey, Vector3> cornerNormals)
    {
        if (point == boundary.A)
        {
            return cornerNormals[boundary.KeyA];
        }

        if (point == boundary.B)
        {
            return cornerNormals[boundary.KeyB];
        }

        return cornerNormals[boundary.KeyC];
    }

    private float TriangleElevation(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle)
    {
        if (_hasFineElevation &&
            _geography.TryGetScalarValue(
                "fine_elevation",
                cell.MicrotriangleOffset + triangleIndex,
                out var fineElevation))
        {
            return fineElevation / FineElevationSubunitsPerUnit;
        }

        return triangle.Elevation;
    }

    private static Vector3 Position(PhysicalMap3DVertexKey key, float height) => new(
        key.X / VertexKeyScale,
        height,
        key.Z / VertexKeyScale);

    private static void Accumulate(
        PhysicalMap3DVertexKey key,
        float height,
        Dictionary<PhysicalMap3DVertexKey, HeightAccumulator> accumulators)
    {
        if (accumulators.TryGetValue(key, out var accumulator))
        {
            accumulators[key] = new HeightAccumulator(
                accumulator.Total + height,
                accumulator.Count + 1);
        }
        else
        {
            accumulators.Add(key, new HeightAccumulator(height, 1));
        }
    }

    private static void AddNormal(
        PhysicalMap3DVertexKey key,
        Vector3 normal,
        Dictionary<PhysicalMap3DVertexKey, Vector3> normalSums)
    {
        normalSums[key] = normalSums.TryGetValue(key, out var sum)
            ? sum + normal
            : normal;
    }

    private static int FloorDivide(int value, int divisor) =>
        value >= 0 ? value / divisor : -(((-value) + divisor - 1) / divisor);

    private sealed class ChunkBuildBuffers
    {
        public ChunkBuildBuffers(int cellCount)
        {
            var vertexCapacity = checked(
                cellCount * PhysicalGeographyCodes.MicrotrianglesPerHex * 9);
            GroundVertices = new List<Vector3>(vertexCapacity);
            GroundNormals = new List<Vector3>(vertexCapacity);
            GroundUvs = new List<Vector2>(vertexCapacity);
            Faces = new List<PhysicalMap3DFace>(
                cellCount * PhysicalGeographyCodes.MicrotrianglesPerHex * 3);
        }

        public List<Vector3> GroundVertices { get; }

        public List<Vector3> GroundNormals { get; }

        public List<Vector2> GroundUvs { get; }

        public List<PhysicalMap3DFace> Faces { get; }

        public List<Vector3> WaterVertices { get; } = [];

        public List<Color> WaterColors { get; } = [];

        public List<Vector3> StructuralLineVertices { get; } = [];

        public List<Color> StructuralLineColors { get; } = [];

        public List<Vector3> FeatureLineVertices { get; } = [];

        public List<Color> FeatureLineColors { get; } = [];

        public void AddGround(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 normalA,
            Vector3 normalB,
            Vector3 normalC)
        {
            GroundVertices.Add(a);
            GroundVertices.Add(b);
            GroundVertices.Add(c);
            GroundNormals.Add(normalA);
            GroundNormals.Add(normalB);
            GroundNormals.Add(normalC);
            GroundUvs.AddRange(TriangleUvs);
        }

        public PhysicalMicrogeographyChunkMeshData Build(int chunkQ, int chunkR) => new(
            chunkQ,
            chunkR,
            GroundVertices.ToArray(),
            GroundNormals.ToArray(),
            GroundUvs.ToArray(),
            Faces.ToArray(),
            WaterVertices.ToArray(),
            WaterColors.ToArray(),
            StructuralLineVertices.ToArray(),
            StructuralLineColors.ToArray(),
            FeatureLineVertices.ToArray(),
            FeatureLineColors.ToArray());
    }

    private readonly record struct HeightAccumulator(float Total, int Count);

    private readonly record struct ChunkKey(int Q, int R);

    private readonly record struct TopFacingGroundFace(
        PhysicalMap3DVertexKey KeyA,
        PhysicalMap3DVertexKey KeyB,
        PhysicalMap3DVertexKey KeyC,
        Vector3 A,
        Vector3 B,
        Vector3 C);

    private readonly record struct RenderedMicrotriangle(
        TopFacingGroundFace Boundary,
        PhysicalMap3DFace[] Faces);
}
