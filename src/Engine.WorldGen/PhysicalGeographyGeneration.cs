namespace Engine.WorldGen;

using Engine.Constants;

public sealed record PhysicalGeographyGenerationProfile
{
    public int Padding { get; init; } = 24;

    public double PlateSpacing { get; init; } = 64d;

    public double PlateBoundaryWidth { get; init; } = 13d;

    public double SeaLevel { get; init; }

    public int ElevationScale { get; init; } = 1000;

    public double MacroNoiseScale { get; init; } = 52d;

    public double MediumNoiseScale { get; init; } = 17d;

    public double DetailNoiseScale { get; init; } = 4.5d;

    public double HeartBiasRadius { get; init; } = 34d;

    public double HeartLandBias { get; init; } = 0.25d;

    public double PaddingOceanBias { get; init; } = 0.72d;

    public double TemperatureBaseline { get; init; } = 0.34d;

    public double TemperatureNoiseStrength { get; init; } = 0.15d;

    public double ElevationTemperatureLapse { get; init; } = 0.32d;

    public int OceanMoistureDistance { get; init; } = 90;

    public double MoistureNoiseStrength { get; init; } = 0.28d;

    public double OrographicDrying { get; init; } = 0.24d;

    public double PrevailingWindX { get; init; } = 1d;

    public double PrevailingWindY { get; init; }

    public int LakeDepthThreshold { get; init; } = 24;

    public int RiverAccumulationThreshold { get; init; } = 11000;

    public int RidgeElevationThreshold { get; init; } = 150;

    public int RidgeSlopeThreshold { get; init; } = 42;

    public int PeakElevationThreshold { get; init; } = 245;

    public int PeakProminenceThreshold { get; init; } = 18;

    public int CliffSlopeThreshold { get; init; } = 125;

    public int PlainSlopeThreshold { get; init; } = 34;

    public int PermanentSeaIceTemperature { get; init; } = 210;

    public int IceMarginTemperature { get; init; } = 300;

    public int GeothermalVentThreshold { get; init; } = 720;

    public void Validate()
    {
        ValidateIntegerScales();
        ValidateFeatureScales();
        ValidateThresholds();
    }

    private void ValidateIntegerScales()
    {
        if (Padding <= 0 || ElevationScale <= 0 || OceanMoistureDistance <= 0)
        {
            throw new InvalidOperationException("Physical worldgen integer scales must be positive.");
        }
    }

    private void ValidateFeatureScales()
    {
        if (PlateSpacing <= 0d || PlateBoundaryWidth <= 0d ||
            MacroNoiseScale <= 0d || MediumNoiseScale <= 0d || DetailNoiseScale <= 0d ||
            Math.Abs(PrevailingWindX) + Math.Abs(PrevailingWindY) <= double.Epsilon)
        {
            throw new InvalidOperationException("Physical worldgen feature scales must be positive.");
        }
    }

    private void ValidateThresholds()
    {
        if (LakeDepthThreshold < 0 || RiverAccumulationThreshold <= 0 ||
            PeakProminenceThreshold < 0 || PlainSlopeThreshold < 0)
        {
            throw new InvalidOperationException("Physical worldgen thresholds are invalid.");
        }
    }
}

public static class PhysicalGeographyGenerator
{
    public static PhysicalGeographyField Generate(
        int radius,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.Validate();
        var grid = new PhysicalGeographyGrid(radius, profile.Padding);
        var field = new PhysicalGeographyField(grid);
        var sites = CreateMacroSites(grid, seed, profile);
        GenerateElevation(field, sites, seed, profile);
        PhysicalHydrology.ClassifyOcean(field, profile);
        GenerateClimate(field, seed, profile);
        PhysicalHydrology.SolveDrainage(field, profile);
        PhysicalLandforms.Classify(field, profile);
        PhysicalGeographyValidator.Validate(field, profile);
        return field;
    }

    private static MacroSite[] CreateMacroSites(
        PhysicalGeographyGrid grid,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        var extentX = (Math.Sqrt(3d) * grid.WorkRadius) + profile.PlateSpacing;
        var extentY = (1.5d * grid.WorkRadius) + profile.PlateSpacing;
        var rowHeight = profile.PlateSpacing * 0.82d;
        var rows = (int)Math.Ceiling((extentY * 2d) / rowHeight) + 1;
        var columns = (int)Math.Ceiling((extentX * 2d) / profile.PlateSpacing) + 1;
        var sites = new List<MacroSite>(checked(rows * columns));
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                sites.Add(CreateMacroSite(seed, profile, extentX, extentY, rowHeight, row, column));
            }
        }

        return sites.ToArray();
    }

    private static MacroSite CreateMacroSite(
        int seed,
        PhysicalGeographyGenerationProfile profile,
        double extentX,
        double extentY,
        double rowHeight,
        int row,
        int column)
    {
        var offset = (row & 1) == 0 ? 0d : profile.PlateSpacing * 0.5d;
        var jitterX = PhysicalNoise.HashSigned(seed ^ 0x1371, row, column) * profile.PlateSpacing * 0.22d;
        var jitterY = PhysicalNoise.HashSigned(seed ^ 0x286d, row, column) * rowHeight * 0.22d;
        var x = -extentX + (column * profile.PlateSpacing) + offset + jitterX;
        var y = -extentY + (row * rowHeight) + jitterY;
        var continentalTendency = PhysicalNoise.HashSigned(seed ^ 0x45a1, row, column) * 0.39d;
        var age = PhysicalNoise.HashUnit(seed ^ 0x5bb3, row, column);
        var angle = PhysicalNoise.HashUnit(seed ^ 0x7229, row, column) * Math.PI * 2d;
        return new MacroSite(x, y, continentalTendency, age, Math.Cos(angle), Math.Sin(angle));
    }

    private static void GenerateElevation(
        PhysicalGeographyField field,
        MacroSite[] sites,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var x = field.Grid.TriangleX(triangleIndex);
            var y = field.Grid.TriangleY(triangleIndex);
            FindNearestSites(x, y, sites, out var nearest, out var second, out var nearestDistance, out var secondDistance);
            var boundary = Math.Clamp(
                1d - ((Math.Sqrt(secondDistance) - Math.Sqrt(nearestDistance)) / profile.PlateBoundaryWidth),
                0d,
                1d);
            var convergence = BoundaryConvergence(sites[nearest], sites[second]);
            var uplift = Math.Max(0d, convergence) * boundary * 0.46d;
            var rift = Math.Max(0d, -convergence) * boundary * 0.31d;
            var elevation = BaseElevation(x, y, sites[nearest], seed, profile) + uplift - rift;
            elevation += HeartBias(x, y, profile);
            elevation -= PaddingBias(field.Grid, triangleIndex, profile);
            field.Elevation[triangleIndex] = ScaleSigned(elevation, profile.ElevationScale);
            field.PlateBoundaryInfluence[triangleIndex] = (float)boundary;
            field.Uplift[triangleIndex] = (float)uplift;
            field.Geothermal[triangleIndex] = GenerateGeothermal(x, y, boundary, convergence, seed);
        }
    }

    private static double BaseElevation(
        double x,
        double y,
        MacroSite site,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        var warpScale = profile.MediumNoiseScale * 1.7d;
        var warpStrength = profile.MediumNoiseScale * 0.42d;
        var warpedX = x +
            (PhysicalNoise.FractalNoise(x, y, seed ^ 0x3d29, warpScale, 2) * warpStrength);
        var warpedY = y +
            (PhysicalNoise.FractalNoise(x, y, seed ^ 0x6a43, warpScale, 2) * warpStrength);
        var youngRoughness = 0.85d + ((1d - site.Age) * 0.3d);
        return site.ContinentalTendency +
            ((site.Age - 0.5d) * 0.08d) +
            (PhysicalNoise.FractalNoise(warpedX, warpedY, seed ^ 0x19f1, profile.MacroNoiseScale, 3) * 0.30d) +
            (PhysicalNoise.FractalNoise(warpedX, warpedY, seed ^ 0x4b37, profile.MediumNoiseScale, 3) *
                0.13d * youngRoughness) +
            (PhysicalNoise.FractalNoise(warpedX, warpedY, seed ^ 0x7c15, profile.DetailNoiseScale, 2) *
                0.045d * youngRoughness);
    }

    private static double HeartBias(double x, double y, PhysicalGeographyGenerationProfile profile)
    {
        var distance = Math.Sqrt((x * x) + (y * y));
        var amount = Math.Clamp(1d - (distance / profile.HeartBiasRadius), 0d, 1d);
        return amount * amount * profile.HeartLandBias;
    }

    private static double PaddingBias(
        PhysicalGeographyGrid grid,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile)
    {
        var distance = grid.HexDistanceAt(grid.TriangleHexIndex(triangleIndex));
        if (distance <= grid.Radius)
        {
            return 0d;
        }

        var amount = (distance - grid.Radius) / (double)grid.Padding;
        return amount * amount * profile.PaddingOceanBias;
    }

    private static int GenerateGeothermal(
        double x,
        double y,
        double boundary,
        double convergence,
        int seed)
    {
        var divergent = Math.Max(0d, -convergence) * boundary * 0.68d;
        var broad = PhysicalNoise.UnitNoise(x, y, seed ^ 0x5ac3, 42d, 3) * 0.24d;
        var hotspotNoise = PhysicalNoise.UnitNoise(x, y, seed ^ 0x6fd1, 24d, 2);
        var hotspot = Math.Max(0d, hotspotNoise - 0.79d) * 2.5d;
        return ScaleUnit(Math.Clamp(divergent + broad + hotspot, 0d, 1d));
    }

    private static void GenerateClimate(
        PhysicalGeographyField field,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        var oceanDistance = PhysicalHydrology.MeasureOceanDistance(field);
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var x = field.Grid.TriangleX(triangleIndex);
            var y = field.Grid.TriangleY(triangleIndex);
            var positiveElevation = Math.Max(0d, field.Elevation[triangleIndex] / (double)profile.ElevationScale);
            var temperatureNoise = PhysicalNoise.FractalNoise(x, y, seed ^ 0x2f31, 58d, 3);
            var geothermal = field.Geothermal[triangleIndex] / 1000d;
            var temperature = profile.TemperatureBaseline +
                (temperatureNoise * profile.TemperatureNoiseStrength) -
                (positiveElevation * profile.ElevationTemperatureLapse) +
                (geothermal * 0.16d);
            field.Temperature[triangleIndex] = ScaleUnit(temperature);
            GenerateMoisture(field, oceanDistance, triangleIndex, x, y, seed, profile);
            SetClimateFeatures(field, triangleIndex, profile);
        }
    }

    private static void GenerateMoisture(
        PhysicalGeographyField field,
        int[] oceanDistance,
        int triangleIndex,
        double x,
        double y,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        var distance = oceanDistance[triangleIndex];
        var oceanExposure = distance < 0
            ? 0d
            : Math.Clamp(1d - (distance / (double)profile.OceanMoistureDistance), 0d, 1d);
        var noise = PhysicalNoise.UnitNoise(x, y, seed ^ 0x6bb1, 31d, 3);
        var drying = (field.Uplift[triangleIndex] * (float)profile.OrographicDrying) +
            OrographicShadow(field, triangleIndex, profile);
        var moisture = 0.12d + (oceanExposure * 0.55d) +
            (noise * profile.MoistureNoiseStrength) - drying;
        if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
        {
            moisture = 1d;
        }

        field.Moisture[triangleIndex] = ScaleUnit(moisture);
        var meltFactor = 0.55d + (field.Temperature[triangleIndex] / 1000d * 0.45d);
        field.Runoff[triangleIndex] = Math.Max(
            1,
            (int)Math.Round(
                field.Moisture[triangleIndex] * meltFactor,
                MidpointRounding.AwayFromZero));
    }

    private static double OrographicShadow(
        PhysicalGeographyField field,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile)
    {
        var upwindNeighbor = -1;
        var upwindProjection = double.MaxValue;
        var x = field.Grid.TriangleX(triangleIndex);
        var y = field.Grid.TriangleY(triangleIndex);
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor < 0)
            {
                continue;
            }

            var projection = ((field.Grid.TriangleX(neighbor) - x) * profile.PrevailingWindX) +
                ((field.Grid.TriangleY(neighbor) - y) * profile.PrevailingWindY);
            if (projection < upwindProjection)
            {
                upwindNeighbor = neighbor;
                upwindProjection = projection;
            }
        }

        return upwindNeighbor < 0
            ? 0d
            : Math.Max(
                0d,
                (field.Elevation[upwindNeighbor] - field.Elevation[triangleIndex]) /
                    (double)profile.ElevationScale) * profile.OrographicDrying;
    }

    private static void SetClimateFeatures(
        PhysicalGeographyField field,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile)
    {
        var flags = 0;
        if (field.Geothermal[triangleIndex] >= profile.GeothermalVentThreshold)
        {
            flags |= PhysicalGeographyFeatureCodes.GeothermalVent;
        }

        if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean &&
            field.Temperature[triangleIndex] <= profile.PermanentSeaIceTemperature)
        {
            flags |= PhysicalGeographyFeatureCodes.PermanentSeaIce;
        }

        if (field.Temperature[triangleIndex] > profile.PermanentSeaIceTemperature &&
            field.Temperature[triangleIndex] <= profile.IceMarginTemperature)
        {
            flags |= PhysicalGeographyFeatureCodes.PermanentIceMargin;
        }

        field.FeatureFlags[triangleIndex] = flags;
    }

    private static void FindNearestSites(
        double x,
        double y,
        MacroSite[] sites,
        out int nearest,
        out int second,
        out double nearestDistance,
        out double secondDistance)
    {
        nearest = 0;
        second = 0;
        nearestDistance = double.MaxValue;
        secondDistance = double.MaxValue;
        for (var siteIndex = 0; siteIndex < sites.Length; siteIndex++)
        {
            var dx = x - sites[siteIndex].X;
            var dy = y - sites[siteIndex].Y;
            var distance = (dx * dx) + (dy * dy);
            if (distance < nearestDistance)
            {
                second = nearest;
                secondDistance = nearestDistance;
                nearest = siteIndex;
                nearestDistance = distance;
            }
            else if (distance < secondDistance)
            {
                second = siteIndex;
                secondDistance = distance;
            }
        }
    }

    private static double BoundaryConvergence(MacroSite first, MacroSite second)
    {
        var dx = second.X - first.X;
        var dy = second.Y - first.Y;
        var length = Math.Sqrt((dx * dx) + (dy * dy));
        if (length <= double.Epsilon)
        {
            return 0d;
        }

        var normalX = dx / length;
        var normalY = dy / length;
        return ((first.VelocityX - second.VelocityX) * normalX) +
            ((first.VelocityY - second.VelocityY) * normalY);
    }

    private static int ScaleSigned(double value, int scale) =>
        (int)Math.Round(Math.Clamp(value, -2d, 2d) * scale, MidpointRounding.AwayFromZero);

    private static int ScaleUnit(double value) =>
        (int)Math.Round(Math.Clamp(value, 0d, 1d) * 1000d, MidpointRounding.AwayFromZero);

    private readonly record struct MacroSite(
        double X,
        double Y,
        double ContinentalTendency,
        double Age,
        double VelocityX,
        double VelocityY);
}

internal static class PhysicalNoise
{
    public static double UnitNoise(double x, double y, int salt, double scale, int octaves) =>
        (FractalNoise(x, y, salt, scale, octaves) + 1d) * 0.5d;

    public static double FractalNoise(double x, double y, int salt, double scale, int octaves)
    {
        var amplitude = 1d;
        var amplitudeTotal = 0d;
        var value = 0d;
        for (var octave = 0; octave < octaves; octave++)
        {
            value += ValueNoise(x / scale, y / scale, salt + (octave * 7919)) * amplitude;
            amplitudeTotal += amplitude;
            amplitude *= 0.5d;
            scale *= 0.5d;
        }

        return value / amplitudeTotal;
    }

    public static double HashSigned(int salt, int x, int y) => (HashUnit(salt, x, y) * 2d) - 1d;

    public static double HashUnit(int salt, int x, int y)
    {
        unchecked
        {
            var hash = salt;
            hash = (hash * 397) ^ x;
            hash = (hash * 397) ^ y;
            hash ^= hash >> 13;
            hash *= 16777619;
            return (hash & 0x7fffffff) / (double)int.MaxValue;
        }
    }

    private static double ValueNoise(double x, double y, int salt)
    {
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var tx = Smooth(x - x0);
        var ty = Smooth(y - y0);
        var a = Lerp(HashSigned(salt, x0, y0), HashSigned(salt, x0 + 1, y0), tx);
        var b = Lerp(HashSigned(salt, x0, y0 + 1), HashSigned(salt, x0 + 1, y0 + 1), tx);
        return Lerp(a, b, ty);
    }

    private static double Smooth(double value) => value * value * (3d - (2d * value));

    private static double Lerp(double a, double b, double amount) => a + ((b - a) * amount);
}
