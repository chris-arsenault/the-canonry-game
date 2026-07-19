#pragma warning disable CA1303
using System.Text.Json;

using Engine.Projection;

using Godot;

using TheCanonryGame.UI;

namespace TheCanonryGame.App;

internal sealed record WorldgenCaptureOptions(string WorldPath, string OutputDirectory)
{
    private const string WorldPrefix = "--worldgen-capture=";
    private const string OutputPrefix = "--capture-output=";

    public static WorldgenCaptureOptions? Parse(IReadOnlyList<string> arguments)
    {
        var world = Option(arguments, WorldPrefix);
        if (world is null)
        {
            return null;
        }

        var fullWorldPath = Path.GetFullPath(world);
        if (!File.Exists(fullWorldPath))
        {
            throw new InvalidOperationException($"Capture world '{fullWorldPath}' does not exist.");
        }

        var output = Option(arguments, OutputPrefix) ?? Path.Combine(
            Path.GetDirectoryName(fullWorldPath)!,
            "captures",
            Path.GetFileNameWithoutExtension(fullWorldPath));
        return new WorldgenCaptureOptions(fullWorldPath, Path.GetFullPath(output));
    }

    private static string? Option(IReadOnlyList<string> arguments, string prefix)
    {
        foreach (var argument in arguments)
        {
            if (!argument.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var value = argument[prefix.Length..];
            return value.Length > 0
                ? value
                : throw new InvalidOperationException($"Capture option '{prefix}' requires a value.");
        }

        return null;
    }
}

internal sealed class WorldgenCaptureController
{
    private const int SettleFrameCount = 3;
    private static readonly JsonSerializerOptions ManifestJsonOptions = new() { WriteIndented = true };
    private readonly WorldgenCaptureOptions _options;
    private readonly SceneTree _tree;
    private readonly Viewport _viewport;
    private readonly Camera2D _camera;
    private readonly HexMapRenderer _renderer;
    private readonly PhysicalGeographyProjection _geography;
    private readonly int _seed;
    private readonly CaptureDefinition[] _captures;
    private readonly List<CaptureResult> _results = [];
    private int _captureIndex;
    private int _settleFrames;
    private bool _configured;

    public WorldgenCaptureController(
        WorldgenCaptureOptions options,
        SceneTree tree,
        Viewport viewport,
        Camera2D camera,
        HexMapRenderer renderer,
        Control uiRoot,
        Node2D entityLayer,
        PhysicalGeographyProjection geography,
        int seed)
    {
        _options = options;
        _tree = tree;
        _viewport = viewport;
        _camera = camera;
        _renderer = renderer;
        _geography = geography;
        _seed = seed;
        Directory.CreateDirectory(options.OutputDirectory);
        uiRoot.Visible = false;
        entityLayer.Visible = false;
        renderer.CurrentPlane = PlaneMode.Physical;
        _ = renderer.SetWorldgenInspectionEnabled(true);
        _ = renderer.SetGeographyFeaturesEnabled(true);
        _captures = BuildCaptureDefinitions();
        GD.Print($"Worldgen capture will write {_captures.Length} images to {options.OutputDirectory}.");
    }

    public void ProcessFrame()
    {
        if (_captureIndex >= _captures.Length)
        {
            return;
        }

        if (!_configured)
        {
            Configure(_captures[_captureIndex]);
            return;
        }

        if (_settleFrames-- > 0)
        {
            RenderingServer.ForceDraw(false);
            return;
        }

        Save(_captures[_captureIndex]);
        _captureIndex++;
        _configured = false;
        if (_captureIndex == _captures.Length)
        {
            SaveManifest();
            GD.Print($"Worldgen capture complete: {_options.OutputDirectory}");
            _tree.Quit();
        }
    }

    private void Configure(CaptureDefinition capture)
    {
        _camera.Position = _renderer.GetHexCenter(capture.Center);
        _camera.Zoom = new Vector2(capture.Zoom, capture.Zoom);
        _ = _renderer.SetGeographyView(capture.View);
        _ = _renderer.SetCameraZoom(capture.Zoom);
        _renderer.NotifyViewportChanged();
        _settleFrames = SettleFrameCount;
        _configured = true;
    }

    private void Save(CaptureDefinition capture)
    {
        var fileName = $"{_seed}-{capture.Name}.png";
        var path = Path.Combine(_options.OutputDirectory, fileName);
        var error = _viewport.GetTexture().GetImage().SavePng(path);
        if (error != Error.Ok)
        {
            throw new IOException($"Godot failed to save capture '{path}': {error}.");
        }

        _results.Add(new CaptureResult(
            capture.Name,
            capture.View.ToString(),
            capture.Center.Q,
            capture.Center.R,
            capture.Zoom,
            fileName));
    }

    private CaptureDefinition[] BuildCaptureDefinitions()
    {
        var wholeZoom = WholeWorldZoom();
        var warm = _geography.Cells.MaxBy(static cell => cell.MeanTemperature).Coord;
        var cold = _geography.Cells.MinBy(static cell => cell.MeanTemperature).Coord;
        var river = MajorRiverCell();
        return
        [
            new("whole-surface", PhysicalGeographyView.Surface, default, wholeZoom),
            new("whole-elevation", PhysicalGeographyView.Elevation, default, wholeZoom),
            new("whole-hydrology", PhysicalGeographyView.Hydrology, default, wholeZoom),
            new("whole-rivers", PhysicalGeographyView.RiverGeometry, default, wholeZoom),
            new("warm-surface", PhysicalGeographyView.Surface, warm, 2f),
            new("cold-surface", PhysicalGeographyView.Surface, cold, 2f),
            new("river-detail", PhysicalGeographyView.RiverGeometry, river, 2f),
        ];
    }

    private float WholeWorldZoom()
    {
        var radius = _geography.Radius;
        var extentCoords = new[]
        {
            new HexCoord(radius, 0),
            new HexCoord(0, radius),
            new HexCoord(-radius, radius),
            new HexCoord(-radius, 0),
            new HexCoord(0, -radius),
            new HexCoord(radius, -radius),
        };
        var positions = extentCoords.Select(_renderer.GetHexCenter).ToArray();
        var width = positions.Max(static position => position.X) -
            positions.Min(static position => position.X) + (_renderer.HexSize * 3f);
        var height = positions.Max(static position => position.Y) -
            positions.Min(static position => position.Y) + (_renderer.HexSize * 3f);
        var viewportSize = _viewport.GetVisibleRect().Size;
        return Math.Clamp(Math.Min(viewportSize.X / width, viewportSize.Y / height) * 0.94f, 0.01f, 5f);
    }

    private HexCoord MajorRiverCell()
    {
        var best = _geography.Cells[0].Coord;
        var bestOrder = -1;
        var bestAccumulation = -1;
        foreach (var cell in _geography.Cells)
        {
            foreach (var triangle in _geography.MicrotrianglesFor(cell))
            {
                if (triangle.RiverOrder > bestOrder ||
                    triangle.RiverOrder == bestOrder && triangle.FlowAccumulation > bestAccumulation)
                {
                    best = cell.Coord;
                    bestOrder = triangle.RiverOrder;
                    bestAccumulation = triangle.FlowAccumulation;
                }
            }
        }

        return best;
    }

    private void SaveManifest()
    {
        var manifest = new CaptureManifest(
            _seed,
            _geography.GenerationEpochCount,
            _geography.Radius,
            Path.GetFileName(_options.WorldPath),
            _viewport.GetVisibleRect().Size.X,
            _viewport.GetVisibleRect().Size.Y,
            _results);
        var json = JsonSerializer.Serialize(manifest, ManifestJsonOptions);
        File.WriteAllText(Path.Combine(_options.OutputDirectory, "manifest.json"), json);
    }

    private readonly record struct CaptureDefinition(
        string Name,
        PhysicalGeographyView View,
        HexCoord Center,
        float Zoom);

    private sealed record CaptureResult(
        string Name,
        string View,
        int Q,
        int R,
        float Zoom,
        string File);

    private sealed record CaptureManifest(
        int Seed,
        int GenerationEpochCount,
        int Radius,
        string WorldFile,
        float ViewportWidth,
        float ViewportHeight,
        IReadOnlyList<CaptureResult> Captures);
}
