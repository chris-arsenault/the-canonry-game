namespace WorldGeneratorTool;

using System.Diagnostics;
using System.Text.Json;

using Engine.Constants;
using Engine.Domain.Startup;
using Engine.Projection;

using Godot;

using IceRemembers.Seed;

using SkiaSharp;

using TheCanonryGame.UI;
using TheCanonryGame.UI.Theme;

internal static class WorldCaptureCli
{
    private const int Width = 2560;
    private const int Height = 1440;
    private const float HexSize = 30f;
    private const float DetailZoom = 2f;
    private const int DetailBoundaryMargin = 30;
    private static readonly JsonSerializerOptions ManifestJsonOptions = new() { WriteIndented = true };

    public static int Run(string[] args)
    {
        if (!TryReadOptions(args, out var worldPath))
        {
            PrintUsage();
            return 2;
        }

        var fullWorldPath = Path.GetFullPath(worldPath);
        if (!File.Exists(fullWorldPath))
        {
            Console.Error.WriteLine($"World file '{fullWorldPath}' does not exist.");
            return 2;
        }

        return CaptureWorld(fullWorldPath, ResolveCaptureRoot(fullWorldPath));
    }

    /// <summary>
    /// Captures always land in the repository's capture directory. There is
    /// deliberately no way to redirect them: these screenshots are reviewed by
    /// hand, and a caller free to choose the path is a caller free to write
    /// them somewhere nobody is looking.
    /// </summary>
    private static string ResolveCaptureRoot(string fullWorldPath)
    {
        var directory = new DirectoryInfo(
            Path.GetDirectoryName(fullWorldPath) ??
            throw new InvalidOperationException("World path has no parent directory."));
        while (directory is not null &&
            !File.Exists(Path.Combine(directory.FullName, "TheCanonryGame.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException(
                $"Could not locate the repository root above '{fullWorldPath}'.");
        }

        return Path.Combine(directory.FullName, ".artifacts", "captures");
    }

    private static IceRemembersPregeneratedWorldInfo? FindWorld(string fullWorldPath)
    {
        var worldDirectory = Path.GetDirectoryName(fullWorldPath) ??
            throw new InvalidOperationException("World path has no parent directory.");
        return IceRemembersPregeneratedWorldStore.Discover(worldDirectory)
            .SingleOrDefault(candidate => string.Equals(
                Path.GetFullPath(candidate.Path),
                fullWorldPath,
                StringComparison.Ordinal));
    }

    private static int CaptureWorld(string fullWorldPath, string captureRoot)
    {
        var world = FindWorld(fullWorldPath);
        if (world is null)
        {
            Console.Error.WriteLine(
                $"World file '{fullWorldPath}' is not a valid current Ice Remembers world.");
            return 2;
        }

        var stopwatch = Stopwatch.StartNew();
        var startupSeed = IceRemembersPregeneratedWorldStore.Read(world);
        var geography = PhysicalGeographyProjection.FromBlueprint(startupSeed.Seed.PhysicalGeography);
        if (!geography.HasData)
        {
            Console.Error.WriteLine($"World file '{fullWorldPath}' has no physical geography.");
            return 2;
        }

        // One directory per seed, so the newest capture of a seed always
        // replaces the last and there is a single place to look.
        var fullOutputDirectory = Path.Combine(
            captureRoot,
            $"seed{startupSeed.Identity.BaseSeed}");
        Directory.CreateDirectory(fullOutputDirectory);
        var geometry = new PhysicalMicrogeographyGeometry(HexSize);
        var painter = new PhysicalMicrogeographyPainter(geography, geometry);
        var captures = BuildCaptureDefinitions(geography, geometry);
        var results = new List<CaptureResult>(captures.Length);
        foreach (var capture in captures)
        {
            var fileName = $"{startupSeed.Identity.BaseSeed}-{capture.Name}.png";
            var path = Path.Combine(fullOutputDirectory, fileName);
            Render(path, geography, geometry, painter, capture, startupSeed.Seed.Entities);
            results.Add(new CaptureResult(
                capture.Name,
                capture.View.ToString(),
                capture.Center.Q,
                capture.Center.R,
                capture.Zoom,
                fileName));
            Console.WriteLine($"Wrote {path}");
        }

        var manifest = new CaptureManifest(
            startupSeed.Identity.BaseSeed,
            geography.GenerationEpochCount,
            geography.Radius,
            Path.GetFileName(fullWorldPath),
            Width,
            Height,
            results);
        File.WriteAllText(
            Path.Combine(fullOutputDirectory, "manifest.json"),
            JsonSerializer.Serialize(manifest, ManifestJsonOptions));
        stopwatch.Stop();
        Console.WriteLine(
            $"Captured {results.Count} views from seed {startupSeed.Identity.BaseSeed}/" +
            $"{startupSeed.Identity.Attempt} in {stopwatch.Elapsed.TotalSeconds:0.00}s.");
        return 0;
    }

    private static void Render(
        string path,
        PhysicalGeographyProjection geography,
        PhysicalMicrogeographyGeometry geometry,
        PhysicalMicrogeographyPainter painter,
        in CaptureDefinition capture,
        IReadOnlyList<DomainSeedEntity> entities)
    {
        var imageInfo = new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(imageInfo);
        using var nativeCanvas = new SKCanvas(bitmap);
        nativeCanvas.Clear(ToSkia(UiTokens.Basalt));
        using var canvas = new SkiaPhysicalMicrogeographyCanvas(
            nativeCanvas,
            geometry.HexCenter(capture.Center),
            capture.Zoom,
            Width,
            Height,
            geometry.HexSize);
        var drawMicrotriangles = capture.Zoom >= 1.25f;
        foreach (var cell in geography.Cells)
        {
            var center = geometry.HexCenter(cell.Coord);
            if (!canvas.CellMayBeVisible(center))
            {
                continue;
            }

            canvas.SetCellOrigin(center);
            painter.PaintCell(canvas, cell, capture.View, featuresEnabled: true, drawMicrotriangles);
        }

        DrawEntityOverlay(nativeCanvas, entities, geometry, capture);

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new FileStream(
            path,
            FileMode.Create,
            System.IO.FileAccess.Write,
            FileShare.None);
        encoded.SaveTo(stream);
    }

    // Seed entities drawn as kind-colored dots with a one-letter glyph, so
    // a capture answers "who starts where" at a glance. Regions are skipped:
    // their coordinate is a coverage centroid, not a placement.
    private static void DrawEntityOverlay(
        SKCanvas canvas,
        IReadOnlyList<DomainSeedEntity> entities,
        PhysicalMicrogeographyGeometry geometry,
        in CaptureDefinition capture)
    {
        var cameraCenter = geometry.HexCenter(capture.Center);
        var radius = Math.Max(5f, geometry.HexSize * capture.Zoom * 0.28f);
        using var fill = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        using var outline = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = new SKColor(20, 20, 24),
            StrokeWidth = Math.Max(1.2f, radius * 0.18f),
        };
        using var glyphPaint = new SKPaint { IsAntialias = true, Color = new SKColor(20, 20, 24) };
        using var font = new SKFont(SKTypeface.Default, radius * 1.3f) { Embolden = true };
        foreach (var entity in entities)
        {
            if (!string.Equals(entity.Kind, "region", StringComparison.Ordinal) &&
                TryEntityStyle(entity.Kind, out var color, out var glyph))
            {
                DrawEntityMarker(canvas, geometry, capture, cameraCenter, entity, radius, color, glyph, fill, outline, font, glyphPaint);
            }
        }
    }

    private static void DrawEntityMarker(
        SKCanvas canvas,
        PhysicalMicrogeographyGeometry geometry,
        in CaptureDefinition capture,
        Vector2 cameraCenter,
        DomainSeedEntity entity,
        float radius,
        SKColor color,
        string glyph,
        SKPaint fill,
        SKPaint outline,
        SKFont font,
        SKPaint glyphPaint)
    {
        var world = geometry.HexCenter(new HexCoord(entity.PhysicalQ, entity.PhysicalR));
        var x = ((world.X - cameraCenter.X) * capture.Zoom) + (Width * 0.5f);
        var y = ((world.Y - cameraCenter.Y) * capture.Zoom) + (Height * 0.5f);
        if (x < -radius || x > Width + radius || y < -radius || y > Height + radius)
        {
            return;
        }

        fill.Color = color;
        canvas.DrawCircle(x, y, radius, fill);
        canvas.DrawCircle(x, y, radius, outline);
        canvas.DrawText(glyph, x, y + (radius * 0.45f), SKTextAlign.Center, font, glyphPaint);
    }

    private static readonly Dictionary<string, (SKColor Color, string Glyph)> EntityStyles =
        new(StringComparer.Ordinal)
        {
            ["settlement"] = (new SKColor(255, 255, 255), "S"),
            ["penguin"] = (new SKColor(255, 214, 64), "P"),
            ["faction"] = (new SKColor(255, 140, 40), "F"),
            ["artifact"] = (new SKColor(226, 92, 240), "A"),
            ["orca"] = (new SKColor(64, 220, 240), "O"),
            ["monster"] = (new SKColor(235, 64, 64), "M"),
            ["point_of_interest"] = (new SKColor(96, 230, 120), "I"),
            ["transport"] = (new SKColor(90, 140, 255), "T"),
            ["ability"] = (new SKColor(180, 180, 190), "B"),
            ["occurrence"] = (new SKColor(150, 150, 160), "X"),
            ["canon"] = (new SKColor(120, 165, 160), "C"),
        };

    private static bool TryEntityStyle(string kind, out SKColor color, out string glyph)
    {
        if (EntityStyles.TryGetValue(kind, out var style))
        {
            (color, glyph) = style;
            return true;
        }

        (color, glyph) = (SKColor.Empty, string.Empty);
        return false;
    }

    private static CaptureDefinition[] BuildCaptureDefinitions(
        PhysicalGeographyProjection geography,
        PhysicalMicrogeographyGeometry geometry)
    {
        var interiorLand = geography.Cells
            .Where(cell =>
                cell.Coord.DistanceFromOrigin <= geography.Radius - DetailBoundaryMargin &&
                cell.LandCoverage > cell.WaterCoverage)
            .ToArray();
        var warm = interiorLand.MaxBy(static cell => cell.MeanTemperature).Coord;
        var cold = interiorLand
            .Where(static cell =>
                Math.Max(
                    cell.MeanFireIntensity,
                    Math.Max(cell.MeanAuroraIntensity, cell.MeanPressureIntensity)) < 300)
            .MinBy(static cell => cell.MeanTemperature)
            .Coord;
        var river = MajorRiverCell(geography, geography.Radius - DetailBoundaryMargin);
        var wholeZoom = WholeWorldZoom(geography.Radius, geometry);
        return
        [
            new("whole-terrain", PhysicalGeographyView.Terrain, default, wholeZoom),
            new("whole-surface", PhysicalGeographyView.Surface, default, wholeZoom),
            new("whole-elevation", PhysicalGeographyView.Elevation, default, wholeZoom),
            new("whole-hydrology", PhysicalGeographyView.Hydrology, default, wholeZoom),
            new("whole-rivers", PhysicalGeographyView.RiverGeometry, default, wholeZoom),
            new("warm-terrain", PhysicalGeographyView.Terrain, warm, DetailZoom),
            new("warm-surface", PhysicalGeographyView.Surface, warm, DetailZoom),
            new("cold-terrain", PhysicalGeographyView.Terrain, cold, DetailZoom),
            new("cold-surface", PhysicalGeographyView.Surface, cold, DetailZoom),
            new("cold-elevation", PhysicalGeographyView.Elevation, cold, DetailZoom),
            new("river-surface", PhysicalGeographyView.Surface, river, DetailZoom),
            new("river-hydrology", PhysicalGeographyView.Hydrology, river, DetailZoom),
            new("river-geometry", PhysicalGeographyView.RiverGeometry, river, DetailZoom),
        ];
    }

    private static float WholeWorldZoom(int radius, PhysicalMicrogeographyGeometry geometry)
    {
        var extentCoords = new[]
        {
            new HexCoord(radius, 0),
            new HexCoord(0, radius),
            new HexCoord(-radius, radius),
            new HexCoord(-radius, 0),
            new HexCoord(0, -radius),
            new HexCoord(radius, -radius),
        };
        var positions = extentCoords.Select(geometry.HexCenter).ToArray();
        var width = positions.Max(static position => position.X) -
            positions.Min(static position => position.X) + (geometry.HexSize * 3f);
        var height = positions.Max(static position => position.Y) -
            positions.Min(static position => position.Y) + (geometry.HexSize * 3f);
        return Math.Clamp(Math.Min(Width / width, Height / height) * 0.94f, 0.01f, 5f);
    }

    private static HexCoord MajorRiverCell(PhysicalGeographyProjection geography, int maximumRadius)
    {
        var best = geography.Cells[0].Coord;
        var bestOrder = -1;
        var bestAccumulation = -1;
        foreach (var cell in geography.Cells)
        {
            if (cell.Coord.DistanceFromOrigin > maximumRadius)
            {
                continue;
            }

            foreach (var triangle in geography.MicrotrianglesFor(cell))
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

    private static bool TryReadOptions(string[] args, out string worldPath)
    {
        const string worldPrefix = "--world=";
        worldPath = string.Empty;
        foreach (var argument in args)
        {
            if (!argument.StartsWith(worldPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            worldPath = argument[worldPrefix.Length..];
        }

        return args.Length == 1 &&
            !string.IsNullOrWhiteSpace(worldPath) &&
            string.Equals(Path.GetExtension(worldPath), ".world", StringComparison.OrdinalIgnoreCase);
    }

    private static void PrintUsage() =>
        Console.Error.WriteLine(
            "Usage: dotnet run --project tools/WorldGenerator -c Release -- capture " +
            "--world=<path.world>\n" +
            "Captures are written to .artifacts/captures/seed<seed>/ and cannot be redirected.");

    private static SKColor ToSkia(Color color) => new(
        ToByte(color.R),
        ToByte(color.G),
        ToByte(color.B),
        ToByte(color.A));

    private static byte ToByte(float value) =>
        (byte)Math.Round(
            Math.Clamp(value, 0f, 1f) * byte.MaxValue,
            MidpointRounding.AwayFromZero);

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
        int ViewportWidth,
        int ViewportHeight,
        IReadOnlyList<CaptureResult> Captures);

    private sealed class SkiaPhysicalMicrogeographyCanvas : IPhysicalMicrogeographyCanvas, IDisposable
    {
        private readonly SKCanvas _canvas;
        private readonly Vector2 _cameraCenter;
        private readonly float _zoom;
        private readonly int _width;
        private readonly int _height;
        private readonly float _visibleMargin;
        private readonly SKPaint _fill = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
        private readonly SKPaint _stroke = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };
        private Vector2 _cellOrigin;

        public SkiaPhysicalMicrogeographyCanvas(
            SKCanvas canvas,
            Vector2 cameraCenter,
            float zoom,
            int width,
            int height,
            float hexSize)
        {
            _canvas = canvas;
            _cameraCenter = cameraCenter;
            _zoom = zoom;
            _width = width;
            _height = height;
            _visibleMargin = hexSize * zoom * 1.2f;
        }

        public void SetCellOrigin(Vector2 origin) => _cellOrigin = origin;

        public bool CellMayBeVisible(Vector2 center)
        {
            var point = TransformWorld(center);
            return point.X >= -_visibleMargin && point.X <= _width + _visibleMargin &&
                point.Y >= -_visibleMargin && point.Y <= _height + _visibleMargin;
        }

        public void FillPolygon(Vector2[] points, Color color)
        {
            using var path = BuildPath(points);
            _fill.Color = ToSkia(color);
            _canvas.DrawPath(path, _fill);
        }

        public void StrokePolyline(Vector2[] points, Color color, float width, bool antialiased)
        {
            using var path = BuildPath(points);
            ConfigureStroke(color, width, antialiased);
            _canvas.DrawPath(path, _stroke);
        }

        public void StrokeLine(
            Vector2 from,
            Vector2 destination,
            Color color,
            float width,
            bool antialiased)
        {
            ConfigureStroke(color, width, antialiased);
            var start = TransformLocal(from);
            var end = TransformLocal(destination);
            _canvas.DrawLine(start.X, start.Y, end.X, end.Y, _stroke);
        }

        public void FillCircle(Vector2 center, float radius, Color color)
        {
            var point = TransformLocal(center);
            _fill.Color = ToSkia(color);
            _canvas.DrawCircle(point.X, point.Y, Math.Max(0.35f, radius * _zoom), _fill);
        }

        public void Dispose()
        {
            _fill.Dispose();
            _stroke.Dispose();
        }

        private SKPath BuildPath(Vector2[] points)
        {
            using var builder = new SKPathBuilder();
            if (points.Length == 0)
            {
                return builder.Detach();
            }

            var first = TransformLocal(points[0]);
            builder.MoveTo(first.X, first.Y);
            for (var index = 1; index < points.Length; index++)
            {
                var point = TransformLocal(points[index]);
                builder.LineTo(point.X, point.Y);
            }

            return builder.Detach();
        }

        private void ConfigureStroke(Color color, float width, bool antialiased)
        {
            _stroke.Color = ToSkia(color);
            _stroke.StrokeWidth = Math.Max(0.35f, width * _zoom);
            _stroke.IsAntialias = antialiased;
        }

        private Vector2 TransformLocal(Vector2 local) => TransformWorld(_cellOrigin + local);

        private Vector2 TransformWorld(Vector2 world) => new(
            ((world.X - _cameraCenter.X) * _zoom) + (_width * 0.5f),
            ((world.Y - _cameraCenter.Y) * _zoom) + (_height * 0.5f));
    }
}
