#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

public partial class PhysicalMicrogeographyMeshChunk3D : Node3D
{
    private MeshInstance3D? _ground;
    private MeshInstance3D? _water;
    private MeshInstance3D? _structuralLines;
    private MeshInstance3D? _featureLines;
    private PhysicalMicrogeographyChunkMeshData? _data;
    private PhysicalGeographyProjection? _geography;
    private PhysicalMicrogeographyStyle? _style;
    private Material? _groundMaterial;

    public void Bind(
        PhysicalMicrogeographyChunkMeshData data,
        PhysicalGeographyProjection geography,
        PhysicalMicrogeographyStyle style,
        Material groundMaterial,
        Material waterMaterial,
        Material overlayMaterial,
        PhysicalGeographyView view)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(geography);
        ArgumentNullException.ThrowIfNull(style);
        ArgumentNullException.ThrowIfNull(groundMaterial);
        ArgumentNullException.ThrowIfNull(waterMaterial);
        ArgumentNullException.ThrowIfNull(overlayMaterial);
        _data = data;
        _geography = geography;
        _style = style;
        _groundMaterial = groundMaterial;
        Name = $"Chunk_{data.ChunkQ}_{data.ChunkR}";
        _ground = new MeshInstance3D { Name = "Ground" };
        _water = new MeshInstance3D { Name = "Water" };
        _structuralLines = new MeshInstance3D { Name = "StructuralLines" };
        _featureLines = new MeshInstance3D { Name = "FeatureLines" };
        AddChild(_ground);
        AddChild(_water);
        AddChild(_structuralLines);
        AddChild(_featureLines);
        RefreshView(view);
        _water.Mesh = BuildWaterMesh(data);
        _water.MaterialOverride = waterMaterial;
        _structuralLines.Mesh = BuildLineMesh(
            data.StructuralLineVertices(),
            data.StructuralLineColors());
        _structuralLines.MaterialOverride = overlayMaterial;
        _featureLines.Mesh = BuildLineMesh(
            data.FeatureLineVertices(),
            data.FeatureLineColors());
        _featureLines.MaterialOverride = overlayMaterial;
    }

    public void RefreshView(PhysicalGeographyView view)
    {
        if (_data is null || _geography is null || _style is null || _ground is null)
        {
            return;
        }

        var vertices = _data.GroundVertices();
        var faces = _data.Faces();
        var colors = new Color[vertices.Length];
        for (var faceIndex = 0; faceIndex < faces.Length; faceIndex++)
        {
            var face = faces[faceIndex];
            var cell = _geography.Cells[face.CellIndex];
            var triangle = _geography.MicrotrianglesFor(cell)[face.TriangleIndex];
            var color = _style.TriangleColor(cell, face.TriangleIndex, triangle, view);
            var vertexOffset = faceIndex * 3;
            colors[vertexOffset] = color;
            colors[vertexOffset + 1] = color;
            colors[vertexOffset + 2] = color;
        }

        _ground.Mesh = BuildGroundMesh(_data, colors);
        _ground.MaterialOverride = _groundMaterial;
        if (_structuralLines is not null)
        {
            _structuralLines.Visible =
                PhysicalMicrogeographyRenderDecisions.UsesStructuralRiverOverlay(view);
        }
    }

    public void SetWaterVisible(bool visible)
    {
        if (_water is not null)
        {
            _water.Visible = visible;
        }
    }

    public void SetFeaturesVisible(bool visible)
    {
        if (_featureLines is not null)
        {
            _featureLines.Visible = visible;
        }
    }

    private static ArrayMesh BuildGroundMesh(
        PhysicalMicrogeographyChunkMeshData data,
        Color[] colors)
    {
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = data.GroundVertices();
        arrays[(int)Mesh.ArrayType.Normal] = data.GroundNormals();
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.TexUV] = data.GroundUvs();
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    private static ArrayMesh? BuildWaterMesh(PhysicalMicrogeographyChunkMeshData data)
    {
        var vertices = data.WaterVertices();
        if (vertices.Length == 0)
        {
            return null;
        }

        var normals = Enumerable.Repeat(Vector3.Up, vertices.Length).ToArray();
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Color] = data.WaterColors();
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    private static ArrayMesh? BuildLineMesh(Vector3[] vertices, Color[] colors)
    {
        if (vertices.Length == 0)
        {
            return null;
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
        return mesh;
    }
}
