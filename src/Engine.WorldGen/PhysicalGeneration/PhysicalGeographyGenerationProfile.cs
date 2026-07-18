namespace Engine.WorldGen;

public sealed record CoverageTarget
{
    public CoverageTarget(double target, double tolerance)
    {
        if (target is < 0d or > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(target));
        }

        if (tolerance is < 0d or > 1d || target - tolerance < 0d || target + tolerance > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance));
        }

        Target = target;
        Tolerance = tolerance;
    }

    public double Target { get; }

    public double Tolerance { get; }

    public double Minimum => Target - Tolerance;

    public double Maximum => Target + Tolerance;

    public bool Contains(double value) => value >= Minimum && value <= Maximum;
}

public sealed record CountTarget
{
    public CountTarget(int minimum, int maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimum);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximum, minimum);

        Minimum = minimum;
        Maximum = maximum;
    }

    public int Minimum { get; }

    public int Maximum { get; }

    public bool Contains(int value) => value >= Minimum && value <= Maximum;
}

public sealed record PhysicalGeographyQualityTargets
{
    public int MinimumEvaluationRadius { get; init; } = 32;

    public CoverageTarget OceanCoverage { get; init; } = new(0.52d, 0.14d);

    public CoverageTarget LandIceCoverage { get; init; } = new(0.14d, 0.10d);

    public CoverageTarget SeaIceCoverage { get; init; } = new(0.42d, 0.32d);

    public CountTarget MajorLandmassCount { get; init; } = new(2, 6);

    public CountTarget InlandLakeBodyCount { get; init; } = new(2, 80);

    internal void Validate()
    {
        if (MinimumEvaluationRadius < 0)
        {
            throw new InvalidOperationException("Minimum quality evaluation radius cannot be negative.");
        }

        if (OceanCoverage is null || LandIceCoverage is null || SeaIceCoverage is null ||
            MajorLandmassCount is null || InlandLakeBodyCount is null)
        {
            throw new InvalidOperationException("Physical geography quality targets cannot be null.");
        }
    }
}

/// <summary>
/// Domain-neutral configuration for the causal physical generation layers.
/// All distribution goals are configurable targets. Structural truths such as
/// acyclic flow remain invariants.
/// </summary>
public sealed record PhysicalGeographyGenerationProfile
{
    public int Padding { get; init; } = 24;

    public int ElevationScale { get; init; } = 1000;

    public int ContinentalCenterCount { get; init; } = 5;

    public int OceanBasinCenterCount { get; init; } = 7;

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

    public int LakeDepthThreshold { get; init; } = 18;

    public int MinimumLakeTriangles { get; init; } = 6;

    public double EndorheicLakeFraction { get; init; } = 0.06d;

    public int RiverAccumulationThreshold { get; init; } = 10500;

    public int MinimumRiverReachTriangles { get; init; } = 7;

    public int TemperatureBaseline { get; init; } = 285;

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

    public int MinimumIceBalance { get; init; } = 80;

    public int GeothermalVentThreshold { get; init; } = 720;

    public int FractureBranchCount { get; init; } = 24;

    public int FractureConductanceFloor { get; init; } = 25;

    public PhysicalGeographyQualityTargets QualityTargets { get; init; } = new();

    public void Validate()
    {
        if (QualityTargets is null)
        {
            throw new InvalidOperationException("Physical geography quality targets cannot be null.");
        }

        QualityTargets.Validate();
        ValidateCounts();
        ValidateScales();
        ValidateRanges();
    }

    private void ValidateCounts()
    {
        if (Padding <= 0 || ElevationScale <= 0 || ContinentalCenterCount <= 0 ||
            OceanBasinCenterCount <= 0 || RidgeAxisCount <= 0 || FluvialErosionIterations <= 0 ||
            MinimumLakeTriangles <= 0 || MinimumRiverReachTriangles <= 0)
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
        ValidateHydrologyRanges();
        ValidateClimateRanges();
    }

    private void ValidateHydrologyRanges()
    {
        if (ShelfDepth <= 0 || ShelfBreakDepth <= ShelfDepth || RiverAccumulationThreshold <= 0)
        {
            throw new InvalidOperationException("Physical worldgen hydrology ranges are invalid.");
        }

        if (EndorheicLakeFraction is < 0d or > 1d)
        {
            throw new InvalidOperationException("Endorheic lake fraction is invalid.");
        }
    }

    private void ValidateClimateRanges()
    {
        if (OceanModerationDistance <= 0 || OceanMoistureDistance <= 0 ||
            WarmthPocketCount < 1 || FractureBranchCount < 1 ||
            WarmthPocketMaxRadius < WarmthPocketMinRadius)
        {
            throw new InvalidOperationException("Physical worldgen climate ranges are invalid.");
        }
    }
}
