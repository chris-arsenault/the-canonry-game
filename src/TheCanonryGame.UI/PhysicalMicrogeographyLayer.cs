using Engine.Constants;
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

public readonly record struct PhysicalMicrotriangleHit(HexCoord Coord, int TriangleIndex);

public partial class PhysicalMicrogeographyLayer : Node2D
{
    private const int ChunkCoordinateSize = 8;
    private const float MicrotriangleZoomThreshold = 1.25f;

    private readonly List<PhysicalMicrogeographyChunk> _chunks = [];
    private readonly Dictionary<ChunkKey, PhysicalMicrogeographyChunk> _chunksByKey = [];
    private PhysicalGeographyProjection? _geography;
    private HexMapProjection? _map;
    private IProjectionSymbolCatalog? _symbols;
    private PhysicalMicrogeographyGeometry? _geometry;
    private PlaneMode _plane = PlaneMode.Physical;
    private float _cameraZoom = 1.5f;
    private Rect2 _lastCullingRect;
    private bool _lastLayerVisible;
    private bool _hasCullingState;

    public bool InspectionEnabled { get; private set; }

    public bool FeaturesEnabled { get; private set; } = true;

    public PhysicalGeographyView View { get; private set; } = PhysicalGeographyView.Surface;

    internal PhysicalGeographyProjection Geography => _geography!;

    internal PhysicalMicrogeographyGeometry Geometry => _geometry!;

    internal bool DrawMicrotriangles =>
        _plane == PlaneMode.Physical && _cameraZoom >= MicrotriangleZoomThreshold;

    internal bool DrawAggregateHexes =>
        _plane == PlaneMode.Physical && InspectionEnabled && !DrawMicrotriangles;

    public void Bind(
        PhysicalGeographyProjection? geography,
        HexMapProjection? map,
        IProjectionSymbolCatalog? symbols,
        float hexSize)
    {
        _geography = geography;
        _map = map;
        _symbols = symbols;
        _geometry = geography?.HasData == true
            ? new PhysicalMicrogeographyGeometry(hexSize)
            : null;
        RebuildChunks();
    }

    public bool SetPlane(PlaneMode plane)
    {
        if (_plane == plane)
        {
            return false;
        }

        _plane = plane;
        RefreshAllChunks();
        UpdateChunkVisibility(force: true);
        return true;
    }

    public bool SetInspectionEnabled(bool enabled)
    {
        if (InspectionEnabled == enabled)
        {
            return false;
        }

        InspectionEnabled = enabled;
        RefreshAllChunks();
        return true;
    }

    public bool SetFeaturesEnabled(bool enabled)
    {
        if (FeaturesEnabled == enabled)
        {
            return false;
        }

        FeaturesEnabled = enabled;
        RefreshAllChunks();
        return true;
    }

    public bool SetView(PhysicalGeographyView view)
    {
        if (View == view)
        {
            return false;
        }

        View = view;
        RefreshAllChunks();
        return true;
    }

    public bool SetCameraZoom(float cameraZoom)
    {
        var wasMicrotriangleLod = DrawMicrotriangles;
        _cameraZoom = Math.Max(0.01f, cameraZoom);
        if (wasMicrotriangleLod == DrawMicrotriangles)
        {
            return false;
        }

        RefreshAllChunks();
        UpdateChunkVisibility(force: true);
        return true;
    }

    public void RefreshMap() => RefreshAllChunks();

    public void NotifyViewportChanged() => UpdateChunkVisibility(force: true);

    public void RefreshMap(IReadOnlyCollection<HexCoord> dirtyCells)
    {
        ArgumentNullException.ThrowIfNull(dirtyCells);
        var refreshed = new HashSet<ChunkKey>();
        foreach (var coord in dirtyCells)
        {
            var key = ChunkKeyFor(coord);
            if (refreshed.Add(key) && _chunksByKey.TryGetValue(key, out var chunk))
            {
                chunk.Refresh();
            }
        }
    }

    public bool TryGetMicrotriangle(Vector2 worldPosition, out PhysicalMicrotriangleHit hit)
    {
        if (_plane != PlaneMode.Physical ||
            _geometry is null ||
            _geography is null ||
            !DrawMicrotriangles)
        {
            hit = default;
            return false;
        }

        var coord = PixelToHex(worldPosition);
        if (!CellIsRenderable(coord) ||
            !_geometry.TryGetTriangleIndex(worldPosition - _geometry.HexCenter(coord), out var triangleIndex))
        {
            hit = default;
            return false;
        }

        hit = new PhysicalMicrotriangleHit(coord, triangleIndex);
        return true;
    }

    internal bool CellIsRenderable(in PhysicalGeographyCellProjection cell) =>
        CellIsRenderable(cell.Coord, cell.BaseTerrainStableName);

    internal Color TerrainColor(in PhysicalGeographyCellProjection cell) =>
        Theme.UiTokens.TerrainColor(cell.BaseTerrainStableName);

    public override void _Ready()
    {
        GetViewport().SizeChanged += NotifyViewportChanged;
    }

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= NotifyViewportChanged;
        base._ExitTree();
    }

    private void RebuildChunks()
    {
        foreach (var chunk in _chunks)
        {
            chunk.QueueFree();
        }

        _chunks.Clear();
        _chunksByKey.Clear();
        _hasCullingState = false;
        if (_geography?.HasData != true || _geometry is null)
        {
            return;
        }

        var cellsByChunk = new Dictionary<ChunkKey, List<PhysicalGeographyCellProjection>>();
        foreach (var cell in _geography.Cells)
        {
            var key = new ChunkKey(
                FloorDivide(cell.Coord.Q, ChunkCoordinateSize),
                FloorDivide(cell.Coord.R, ChunkCoordinateSize));
            if (!cellsByChunk.TryGetValue(key, out var cells))
            {
                cells = [];
                cellsByChunk.Add(key, cells);
            }

            cells.Add(cell);
        }

        foreach (var (key, cells) in cellsByChunk)
        {
            var chunk = new PhysicalMicrogeographyChunk();
            AddChild(chunk);
            chunk.Bind(this, cells.ToArray(), BoundsFor(cells));
            _chunks.Add(chunk);
            _chunksByKey.Add(key, chunk);
        }

        UpdateChunkVisibility(force: true);
    }

    private Rect2 BoundsFor(List<PhysicalGeographyCellProjection> cells)
    {
        var first = _geometry!.HexCenter(cells[0].Coord);
        var minimum = first;
        var maximum = first;
        for (var index = 1; index < cells.Count; index++)
        {
            var center = _geometry.HexCenter(cells[index].Coord);
            minimum = new Vector2(Math.Min(minimum.X, center.X), Math.Min(minimum.Y, center.Y));
            maximum = new Vector2(Math.Max(maximum.X, center.X), Math.Max(maximum.Y, center.Y));
        }

        return new Rect2(minimum, maximum - minimum).Grow(_geometry.HexSize * 1.1f);
    }

    private void UpdateChunkVisibility(bool force = false)
    {
        var layerVisible = _plane == PlaneMode.Physical && _geometry is not null;
        var visibleRect = layerVisible ? VisibleLocalRect() : default;
        if (!force &&
            _hasCullingState &&
            _lastLayerVisible == layerVisible &&
            (!layerVisible || _lastCullingRect == visibleRect))
        {
            return;
        }

        _hasCullingState = true;
        _lastLayerVisible = layerVisible;
        _lastCullingRect = visibleRect;
        foreach (var chunk in _chunks)
        {
            var shouldBeVisible = layerVisible && visibleRect.Intersects(chunk.Bounds);
            chunk.SetCullingVisible(shouldBeVisible);
        }
    }

    private Rect2 VisibleLocalRect()
    {
        var viewport = GetViewport().GetVisibleRect();
        var inverse = GetCanvasTransform().AffineInverse();
        var topLeft = inverse * viewport.Position;
        var topRight = inverse * new Vector2(viewport.End.X, viewport.Position.Y);
        var bottomLeft = inverse * new Vector2(viewport.Position.X, viewport.End.Y);
        var bottomRight = inverse * viewport.End;
        var minimum = new Vector2(
            Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X)),
            Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y)));
        var maximum = new Vector2(
            Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X)),
            Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y)));
        return new Rect2(minimum, maximum - minimum).Grow(_geometry!.HexSize * 2f);
    }

    private bool CellIsRenderable(HexCoord coord, string? generatedTerrain = null)
    {
        if (_geography?.TryGetCell(coord, out var geographyCell) != true)
        {
            return false;
        }

        if (InspectionEnabled)
        {
            return true;
        }

        if (_map?.TryGetHex(PlaneCodes.Physical, coord, out var liveCell) != true)
        {
            return false;
        }

        var stableName = ProjectionDisplay.StableName(
            _symbols,
            ProjectionSymbolKind.Terrain,
            liveCell.TerrainId,
            string.Empty);
        return string.Equals(
            stableName,
            generatedTerrain ?? geographyCell.BaseTerrainStableName,
            StringComparison.Ordinal);
    }

    private HexCoord PixelToHex(Vector2 pixel)
    {
        var q = ((Mathf.Sqrt(3f) / 3f * pixel.X) - (pixel.Y / 3f)) / _geometry!.HexSize;
        var r = (2f / 3f * pixel.Y) / _geometry.HexSize;
        return RoundAxial(q, r);
    }

    private static HexCoord RoundAxial(float q, float r)
    {
        var s = -q - r;
        var roundedQ = (int)MathF.Round(q, MidpointRounding.AwayFromZero);
        var roundedR = (int)MathF.Round(r, MidpointRounding.AwayFromZero);
        var roundedS = (int)MathF.Round(s, MidpointRounding.AwayFromZero);
        var qDifference = MathF.Abs(roundedQ - q);
        var rDifference = MathF.Abs(roundedR - r);
        var sDifference = MathF.Abs(roundedS - s);
        if (qDifference > rDifference && qDifference > sDifference)
        {
            roundedQ = -roundedR - roundedS;
        }
        else if (rDifference > sDifference)
        {
            roundedR = -roundedQ - roundedS;
        }

        return new HexCoord(roundedQ, roundedR);
    }

    private void RefreshAllChunks()
    {
        foreach (var chunk in _chunks)
        {
            chunk.Refresh();
        }
    }

    private static ChunkKey ChunkKeyFor(HexCoord coord) => new(
        FloorDivide(coord.Q, ChunkCoordinateSize),
        FloorDivide(coord.R, ChunkCoordinateSize));

    private static int FloorDivide(int value, int divisor) =>
        value >= 0 ? value / divisor : -(((-value) + divisor - 1) / divisor);

    private readonly record struct ChunkKey(int Q, int R);
}
