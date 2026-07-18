namespace Engine.WorldGen;

/// <summary>
/// Domain-neutral parameters for the causal physical generation passes. Values
/// use triangle-world units and fixed scalar ranges rather than the startup
/// heart or gameplay frontier radius.
/// </summary>
public sealed record PhysicalGeographyGenerationProfile
{
    public int Padding { get; init; } = 24;

    public int ElevationScale { get; init; } = 1000;

    public int ContinentalCenterCount { get; init; } = 5;

    public double ContinentalRadius { get; init; } = 92d;

    public double ContinentalWarpScale { get; init; } = 54d;

    public double ContinentalWarpStrength { get; init; } = 18d;

    public int RidgeAxisCount { get; init; } = 7;

    public double RidgeHalfLength { get; init; } = 72d;

    public double RidgeWidth { get; init; } = 11d;

    public double RidgeUplift { get; init; } = 0.62d;

    public double ElevationVariationScale { get; init; } = 25d;

    public double ElevationVariationStrength { get; init; } = 0.12d;

    public int FluvialErosionIterations { get; init; } = 5;

    public double FluvialErosionStrength { get; init; } = 0.045d;

    public double SeaLevel { get; init; }

    public int ShelfDepth { get; init; } = 210;

    public int ShelfBreakDepth { get; init; } = 430;

    public int IslandTriangleThreshold { get; init; } = 720;

    public int LakeDepthThreshold { get; init; } = 22;

    public int RiverAccumulationThreshold { get; init; } = 10500;

    public int TemperatureBaseline { get; init; } = 150;

    public int ElevationTemperatureLapse { get; init; } = 270;

    public int OceanModerationDistance { get; init; } = 70;

    public int OceanModerationStrength { get; init; } = 125;

    public int WarmthPocketCount { get; init; } = 15;

    public double WarmthPocketMinRadius { get; init; } = 10d;

    public double WarmthPocketMaxRadius { get; init; } = 28d;

    public int WarmthPocketMinStrength { get; init; } = 170;

    public int WarmthPocketMaxStrength { get; init; } = 390;

    public double HeartWarmthRadius { get; init; } = 28d;

    public int HeartWarmthStrength { get; init; } = 310;

    public int GeothermalTemperatureStrength { get; init; } = 310;

    public int OceanMoistureDistance { get; init; } = 90;

    public int RidgeElevationThreshold { get; init; } = 145;

    public int RidgeSlopeThreshold { get; init; } = 38;

    public int PeakElevationThreshold { get; init; } = 235;

    public int PeakProminenceThreshold { get; init; } = 16;

    public int CliffSlopeThreshold { get; init; } = 120;

    public int PlainSlopeThreshold { get; init; } = 30;

    public int IceTemperatureThreshold { get; init; } = 285;

    public int PermanentSeaIceTemperature { get; init; } = 205;

    public int IceMarginTemperature { get; init; } = 300;

    public int GlacierThicknessThreshold { get; init; } = 250;

    public int GlacierFluxThreshold { get; init; } = 1200;

    public int GeothermalVentThreshold { get; init; } = 720;

    public int FractureBranchCount { get; init; } = 24;

    public int FractureConductanceFloor { get; init; } = 25;

    public void Validate()
    {
        ValidateCounts();
        ValidateScales();
        ValidateRanges();
    }

    private void ValidateCounts()
    {
        if (Padding <= 0 || ElevationScale <= 0 || ContinentalCenterCount <= 0 ||
            RidgeAxisCount <= 0 || FluvialErosionIterations <= 0)
        {
            throw new InvalidOperationException("Physical worldgen integer parameters are invalid.");
        }
    }

    private void ValidateScales()
    {
        if (ContinentalRadius <= 0d || ContinentalWarpScale <= 0d ||
            RidgeHalfLength <= 0d || RidgeWidth <= 0d || RidgeUplift <= 0d ||
            ElevationVariationScale <= 0d || FluvialErosionStrength <= 0d ||
            WarmthPocketMinRadius <= 0d || HeartWarmthRadius <= 0d)
        {
            throw new InvalidOperationException("Physical worldgen feature scales are invalid.");
        }
    }

    private void ValidateRanges()
    {
        if (ShelfDepth <= 0 || ShelfBreakDepth <= ShelfDepth ||
            RiverAccumulationThreshold <= 0 || OceanModerationDistance <= 0 ||
            OceanMoistureDistance <= 0 || WarmthPocketCount < 1 || FractureBranchCount < 1 ||
            WarmthPocketMaxRadius < WarmthPocketMinRadius)
        {
            throw new InvalidOperationException("Physical worldgen parameter ranges are invalid.");
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
        var field = new PhysicalGeographyField(new PhysicalGeographyGrid(radius, profile.Padding));

        PhysicalTerrainFormation.GenerateUpliftAndErode(field, seed ^ 0x12e5a91, profile);
        PhysicalHydrology.ClassifyLandmassesAndBathymetry(field, profile);
        PhysicalHydrology.SolveDrainage(field, profile);
        PhysicalClimate.Generate(field, seed ^ 0x4a51c73, profile);
        PhysicalGlaciation.Apply(field, profile);
        PhysicalHydrology.ReconcileAfterGlaciation(field, profile);
        PhysicalLandforms.Classify(field, profile);
        PhysicalFractures.Generate(field, seed ^ 0x6f9d2b1, profile);
        PhysicalGeographyValidator.Validate(field, profile);
        return field;
    }
}

public static class PhysicalNoise
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
