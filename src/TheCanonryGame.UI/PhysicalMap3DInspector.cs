#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

/// <summary>
/// Read-only, full-field 3D presentation of immutable generated geography.
/// It deliberately owns no simulation nodes or runtime mutation paths.
/// </summary>
public partial class PhysicalMap3DInspector : Node3D
{
    private readonly List<PhysicalMicrogeographyMeshChunk3D> _chunks = [];
    private Node3D? _terrainRoot;
    private Camera3D? _camera;
    private PhysicalMap3DCameraController? _cameraController;
    private PhysicalGeographyProjection? _geography;
    private PhysicalMicrogeographyGeometry? _geometry;
    private PhysicalMicrogeographyMeshData? _meshData;
    private PhysicalMap3DPicker? _picker;
    private PhysicalMicrogeographyStyle? _style;
    private ShaderMaterial? _groundMaterial;
    private StandardMaterial3D? _waterMaterial;
    private StandardMaterial3D? _overlayMaterial;
    private PhysicalGeographyView _view = PhysicalGeographyView.Surface;
    private float _verticalExaggeration = 1f;
    private bool _wireframeVisible;
    private bool _waterVisible = true;
    private bool _featuresVisible = true;

    public bool Active { get; private set; }

    public float VerticalExaggeration => _verticalExaggeration;

    public override void _Ready()
    {
        _terrainRoot = GetNode<Node3D>("TerrainRoot");
        _camera = GetNode<Camera3D>("Camera3D");
        _cameraController = new PhysicalMap3DCameraController(_camera);
        _groundMaterial = CreateGroundMaterial();
        _waterMaterial = CreateWaterMaterial();
        _overlayMaterial = CreateOverlayMaterial();
        Visible = false;
        SetProcess(false);
    }

    public void Bind(PhysicalGeographyProjection? geography, float hexSize)
    {
        if (ReferenceEquals(_geography, geography) &&
            _geometry is not null &&
            Mathf.IsEqualApprox(_geometry.HexSize, hexSize))
        {
            return;
        }

        _geography = geography;
        _geometry = geography?.HasData == true
            ? new PhysicalMicrogeographyGeometry(hexSize)
            : null;
        ClearGeometry();
        if (Active)
        {
            EnsureGeometry();
        }
    }

    public bool SetActive(bool active)
    {
        if (Active == active)
        {
            return false;
        }

        Active = active;
        Visible = active;
        SetProcess(active);
        if (active)
        {
            EnsureGeometry();
            _cameraController?.MakeCurrent();
        }

        return true;
    }

    public void SetView(PhysicalGeographyView view)
    {
        if (_view == view)
        {
            return;
        }

        _view = view;
        foreach (var chunk in _chunks)
        {
            chunk.RefreshView(view);
        }
    }

    public void SetVerticalExaggeration(float exaggeration)
    {
        _verticalExaggeration = Math.Clamp(exaggeration, 0.25f, 4f);
        if (_terrainRoot is not null)
        {
            _terrainRoot.Scale = new Vector3(1f, _verticalExaggeration, 1f);
        }
    }

    public void SetWireframeVisible(bool visible)
    {
        _wireframeVisible = visible;
        _groundMaterial?.SetShaderParameter("show_wireframe", visible);
    }

    public void SetWaterVisible(bool visible)
    {
        _waterVisible = visible;
        foreach (var chunk in _chunks)
        {
            chunk.SetWaterVisible(visible);
        }
    }

    public void SetFeaturesVisible(bool visible)
    {
        _featuresVisible = visible;
        foreach (var chunk in _chunks)
        {
            chunk.SetFeaturesVisible(visible);
        }
    }

    public bool HandlePointerInput(InputEvent input) =>
        Active && _cameraController?.HandlePointerInput(input) == true;

    public bool Pan(Vector2 direction, float delta) =>
        Active && _cameraController?.Pan(direction, delta) == true;

    public bool TryPick(Vector2 screenPosition, out PhysicalMicrotriangleHit hit)
    {
        if (!Active || _picker is null || _camera is null)
        {
            hit = default;
            return false;
        }

        return _picker.TryPick(
            _camera,
            screenPosition,
            _verticalExaggeration,
            out hit);
    }

    private void EnsureGeometry()
    {
        if (!CanBuildGeometry())
        {
            return;
        }

        var geography = _geography!;
        var geometry = _geometry!;
        var terrainRoot = _terrainRoot!;
        var groundMaterial = _groundMaterial!;
        var waterMaterial = _waterMaterial!;
        var overlayMaterial = _overlayMaterial!;
        _style = new PhysicalMicrogeographyStyle(geography, geometry);
        _meshData = new PhysicalMicrogeographyMeshBuilder(geography, geometry).Build();
        foreach (var data in _meshData.Chunks)
        {
            var chunk = new PhysicalMicrogeographyMeshChunk3D();
            terrainRoot.AddChild(chunk);
            chunk.Bind(
                data,
                geography,
                _style,
                groundMaterial,
                waterMaterial,
                overlayMaterial,
                _view);
            chunk.SetWaterVisible(_waterVisible);
            chunk.SetFeaturesVisible(_featuresVisible);
            _chunks.Add(chunk);
        }

        terrainRoot.Scale = new Vector3(1f, _verticalExaggeration, 1f);
        _picker = new PhysicalMap3DPicker(geometry, _meshData);
        _cameraController?.Reset(
            geography.Radius,
            geometry.HexSize,
            _meshData.MaximumAbsoluteHeight * _verticalExaggeration);
        groundMaterial.SetShaderParameter("show_wireframe", _wireframeVisible);
    }

    private bool CanBuildGeometry() =>
        _meshData is null &&
        _geography?.HasData == true &&
        _geometry is not null &&
        _terrainRoot is not null &&
        _groundMaterial is not null &&
        _waterMaterial is not null &&
        _overlayMaterial is not null;

    private void ClearGeometry()
    {
        foreach (var chunk in _chunks)
        {
            chunk.QueueFree();
        }

        _chunks.Clear();
        _meshData = null;
        _picker = null;
        _style = null;
    }

    private static ShaderMaterial CreateGroundMaterial()
    {
        var shader = GD.Load<Shader>("res://shaders/physical_microgeography.gdshader") ??
            throw new InvalidOperationException("The physical microgeography shader is missing.");
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("show_wireframe", false);
        material.SetShaderParameter("wire_color", new Color(0.92f, 0.95f, 0.98f, 0.38f));
        return material;
    }

    private static StandardMaterial3D CreateWaterMaterial() => new()
    {
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        VertexColorUseAsAlbedo = true,
        Roughness = 0.28f,
        Metallic = 0.06f,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
    };

    private static StandardMaterial3D CreateOverlayMaterial() => new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        VertexColorUseAsAlbedo = true,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
    };
}
