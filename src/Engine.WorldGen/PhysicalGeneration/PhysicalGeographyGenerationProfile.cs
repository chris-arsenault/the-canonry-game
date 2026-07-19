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

    public double MajorLandmassMinimumLandCoverage { get; init; } = 0.02d;

    public CountTarget InlandLakeBodyCount { get; init; } = new(2, 80);

    public CoverageTarget LargestLandmassLandCoverage { get; init; } = new(0.38d, 0.30d);

    public CoverageTarget IslandLandCoverage { get; init; } = new(0.10d, 0.10d);

    public CoverageTarget CoastEdgeDensity { get; init; } = new(0.20d, 0.20d);

    public CoverageTarget MajorLandmassSpacing { get; init; } = new(0.30d, 0.30d);

    public CountTarget HotspotLandmassCount { get; init; } = new(1, 8);

    public CoverageTarget RiverChannelCoverage { get; init; } = new(0.05d, 0.05d);

    public CoverageTarget FloodplainCoverage { get; init; } = new(0.02d, 0.02d);

    public CoverageTarget DeltaCoverage { get; init; } = new(0.003d, 0.003d);

    public CoverageTarget TributarySpineCoverage { get; init; } = new(0.075d, 0.075d);

    public CoverageTarget WideRiverCoverage { get; init; } = new(0.015d, 0.015d);

    public CountTarget HigherOrderRiverTriangleCount { get; init; } = new(1, int.MaxValue);

    public CountTarget DeltaMouthCount { get; init; } = new(1, 1000);

    public CountTarget ColdLandformTypeCount { get; init; } = new(3, 15);

    public int MinimumLandformTypesPerMajorLandmass { get; init; } = 3;

    public CountTarget VariedMajorLandmassCount { get; init; } = new(1, 8);

    public CountTarget CoastalMajorLandmassCount { get; init; } = new(1, 8);

    public CountTarget BranchingDrainageBasinCount { get; init; } = new(1, int.MaxValue);

    public CoverageTarget DominantRiverDirectionShare { get; init; } = new(0.25d, 0.25d);

    internal void Validate()
    {
        if (MinimumEvaluationRadius < 0)
        {
            throw new InvalidOperationException("Minimum quality evaluation radius cannot be negative.");
        }

        if (CoverageTargetIsNull() || CountTargetIsNull())
        {
            throw new InvalidOperationException("Physical geography quality targets cannot be null.");
        }

        if (MajorLandmassMinimumLandCoverage is <= 0d or > 1d)
        {
            throw new InvalidOperationException("Major-landmass minimum coverage is invalid.");
        }

        if (MinimumLandformTypesPerMajorLandmass <= 0)
        {
            throw new InvalidOperationException("Major-landmass variety target is invalid.");
        }
    }

    private bool CoverageTargetIsNull() =>
        OceanCoverage is null || LandIceCoverage is null || SeaIceCoverage is null ||
        LargestLandmassLandCoverage is null || IslandLandCoverage is null ||
        CoastEdgeDensity is null || MajorLandmassSpacing is null || RiverCoverageTargetIsNull();

    private bool RiverCoverageTargetIsNull() =>
        RiverChannelCoverage is null || FloodplainCoverage is null || DeltaCoverage is null ||
        TributarySpineCoverage is null || WideRiverCoverage is null ||
        DominantRiverDirectionShare is null;

    private bool CountTargetIsNull() =>
        MajorLandmassCount is null || InlandLakeBodyCount is null || HotspotLandmassCount is null ||
        HigherOrderRiverTriangleCount is null || DeltaMouthCount is null ||
        ColdLandformTypeCount is null || VariedMajorLandmassCount is null ||
        CoastalMajorLandmassCount is null || BranchingDrainageBasinCount is null;
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

    public int TectonicProvinceCount { get; init; } = 14;

    public int TectonicSiteCandidateMultiplier { get; init; } = 24;

    public int ContinentalAssemblyCount { get; init; } = 4;

    public double ContinentalCrustFraction { get; init; } = 0.48d;

    public double ContinentalTransitionWidth { get; init; } = 26d;

    public double ContinentalAssemblySeparationDepth { get; init; } = 0.38d;

    public double ContinentalCrustPotentialFloor { get; init; } = 0.62d;

    public double OceanicCrustPotentialCeiling { get; init; } = 0.38d;

    public double ContinentalWarpScale { get; init; } = 54d;

    public double ContinentalWarpStrength { get; init; } = 18d;

    public double RidgeWidth { get; init; } = 11d;

    public double RidgeUplift { get; init; } = 0.62d;

    public double RiftDepth { get; init; } = 0.28d;

    public double TectonicMotionMinimum { get; init; } = 0.22d;

    public double TectonicMotionMaximum { get; init; } = 0.72d;

    public double TectonicConvergenceThreshold { get; init; } = 0.12d;

    public double TectonicEpochVariationScale { get; init; } = 34d;

    public int TectonicBoundaryPersistence { get; init; } = 420;

    public double ElevationVariationScale { get; init; } = 25d;

    public double ElevationVariationStrength { get; init; } = 0.12d;

    public int EpochTectonicElevationStep { get; init; } = 150;

    public int FluvialErosionIterations { get; init; } = 5;

    public double FluvialErosionStrength { get; init; } = 0.045d;

    public int SedimentTransportFraction { get; init; } = 760;

    public int SedimentDepositionSlopeThreshold { get; init; } = 34;

    public int MaximumSedimentDepositionPerPass { get; init; } = 18;

    public int ImpactCraterCount { get; init; } = 6;

    public double ImpactMinimumRadius { get; init; } = 5d;

    public double ImpactMaximumRadius { get; init; } = 18d;

    public double ImpactMinimumSpacing { get; init; } = 22d;

    public int ImpactDepthRatio { get; init; } = 280;

    public int ImpactRimHeightRatio { get; init; } = 90;

    public int ImpactEjectaFraction { get; init; } = 520;

    public int ImpactFractureStrength { get; init; } = 760;

    public double ImpactThermalRadiusThreshold { get; init; } = 12d;

    public int IceAccumulationStep { get; init; } = 420;

    public int IceAblationStep { get; init; } = 260;

    public int FrozenLakeIceAdvanceDecay { get; init; } = 40;

    public int GlacierGroundingFraction { get; init; } = 900;

    public int GlacialMeltTemperatureFloor { get; init; } = 170;

    public int GlacialAblationRunoffStrength { get; init; } = 460;

    public int BasalMeltRunoffStrength { get; init; } = 180;

    public int GlacialRetreatRunoffStrength { get; init; } = 260;

    public int MaximumGlacialErosionPerPass { get; init; } = 18;

    public int MaximumGlacialDepositionPerPass { get; init; } = 20;

    public int AncestralValleyMinimumErosion { get; init; } = 5;

    public int OutwashMinimumGlacialDeposition { get; init; } = 6;

    public int ImpactLandformMinimumExcavation { get; init; } = 18;

    public int ImpactLandformMinimumRimUplift { get; init; } = 8;

    public int InheritedRidgeActivityThreshold { get; init; } = 60;

    public int CoastalErosionWaveThreshold { get; init; } = 420;

    public int CoastalErosionStrength { get; init; } = 180;

    public int MaximumCoastalErosionPerPass { get; init; } = 12;

    public int CoastalDepositionFraction { get; init; } = 580;

    public int CoastalSeaLevelChange { get; init; }

    public double SeaLevel { get; init; }

    public int ShelfDepth { get; init; } = 210;

    public int ShelfBreakDepth { get; init; } = 430;

    public int IslandTriangleThreshold { get; init; } = 720;

    public int LakeDepthThreshold { get; init; } = 18;

    public int MinimumLakeTriangles { get; init; } = 6;

    public double EndorheicLakeFraction { get; init; } = 0.06d;

    public int RiverAccumulationThreshold { get; init; } = 10500;

    public int RiverMinimumAccumulationThreshold { get; init; } = 2400;

    public int RiverBasinHeadwaterFraction { get; init; } = 80;

    public int MinimumRiverReachTriangles { get; init; } = 7;

    public int RiverChannelBaseStrength { get; init; } = 260;

    public int RiverChannelOrderStrength { get; init; } = 145;

    public int RiverChannelDischargeStrength { get; init; } = 55;

    public int RiverChannelWidthDecay { get; init; } = 330;

    public int RiverChannelWideningOrder { get; init; } = 2;

    public int RiverChannelWideningDischargeMultiplier { get; init; } = 5;

    public int RiverChannelMaximumExpansionSteps { get; init; } = 2;

    public int RiverChannelMaximumLongitudinalAlignment { get; init; } = 560;

    public int RiverChannelMaximumBankRelief { get; init; } = 70;

    public int FloodplainMinimumRiverOrder { get; init; } = 2;

    public int FloodplainMaximumSlope { get; init; } = 42;

    public int FloodplainMaximumRelief { get; init; } = 34;

    public int FloodplainMaximumLongitudinalAlignment { get; init; } = 720;

    public int FloodplainIntensityDecay { get; init; } = 280;

    public int FloodplainMaximumExpansionSteps { get; init; } = 2;

    public int DeltaMinimumRiverOrder { get; init; } = 2;

    public int DeltaDischargeMultiplier { get; init; } = 3;

    public int DeltaMinimumSedimentDelivery { get; init; } = 1;

    public int DeltaMaximumWaveEnergy { get; init; } = 760;

    public int DeltaMaximumCurrentStrength { get; init; } = 900;

    public int DeltaIntensityDecay { get; init; } = 240;

    public int DeltaMinimumForwardAlignment { get; init; } = 80;

    public int DeltaMaximumExpansionSteps { get; init; } = 3;

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

    public int OceanCurrentSmoothingIterations { get; init; } = 6;

    public int OceanCurrentHeatIterations { get; init; } = 10;

    public int OceanCurrentFetchSteps { get; init; } = 18;

    public int OceanCurrentMinimumStrength { get; init; } = 110;

    public int OceanCurrentTemperatureStrength { get; init; } = 150;

    public int OceanCurrentWarmthStrength { get; init; } = 120;

    public int CurrentEvaporationStrength { get; init; } = 180;

    public int PrecipitationBaseline { get; init; } = 40;

    public int OrographicPrecipitationStrength { get; init; } = 220;

    public int RainShadowStrength { get; init; } = 260;

    public int SnowTemperatureThreshold { get; init; } = 300;

    public int SnowTemperatureTransition { get; init; } = 100;

    public int SeaIceCurrentResistance { get; init; } = 220;

    public int SeaIceWaveResistance { get; init; } = 180;

    public int SeaIceSuitabilityVariation { get; init; } = 150;

    public double SeaIceVariationScale { get; init; } = 26d;

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

    public int MantlePlumeCount { get; init; } = 3;

    public double MantlePlumeRadius { get; init; } = 11d;

    public double MantlePlumeMinimumSpacing { get; init; } = 36d;

    public int MantlePlumeStrength { get; init; } = 940;

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
        if (TectonicCountsAreInvalid() || LandscapeCountsAreInvalid())
        {
            throw new InvalidOperationException("Physical worldgen integer parameters are invalid.");
        }
    }

    private bool TectonicCountsAreInvalid() =>
        TectonicProvinceCount < 4 || TectonicSiteCandidateMultiplier < 2 ||
        MantlePlumeCount < 0 || ContinentalAssemblyCount < 2 ||
        ContinentalAssemblyCount > TectonicProvinceCount - 2;

    private bool LandscapeCountsAreInvalid() =>
        BaseLandscapeCountsAreInvalid() || EvolutionCountsAreInvalid();

    private bool BaseLandscapeCountsAreInvalid() =>
        Padding <= 0 || ElevationScale <= 0 || FluvialErosionIterations <= 0 ||
        ImpactCraterCount < 0 ||
        MinimumLakeTriangles <= 0 || MinimumRiverReachTriangles <= 0 ||
        OceanCurrentSmoothingIterations <= 0 || OceanCurrentHeatIterations <= 0 ||
        OceanCurrentFetchSteps <= 0 || RiverGeometryCountsAreInvalid();

    private bool RiverGeometryCountsAreInvalid() =>
        RiverChannelWideningOrder <= 0 ||
        RiverChannelWideningDischargeMultiplier <= 0 || RiverChannelMaximumExpansionSteps < 0 ||
        FloodplainMinimumRiverOrder <= 0 || FloodplainMaximumExpansionSteps < 0 ||
        DeltaMinimumRiverOrder <= 0 || DeltaDischargeMultiplier <= 0 ||
        DeltaMinimumSedimentDelivery < 0 || DeltaMaximumExpansionSteps < 0;

    private bool EvolutionCountsAreInvalid() =>
        EpochTectonicElevationStep <= 0 || IceAccumulationStep <= 0 || IceAblationStep <= 0 ||
        FrozenLakeIceAdvanceDecay <= 0 ||
        MaximumGlacialErosionPerPass <= 0 || MaximumGlacialDepositionPerPass <= 0 ||
        LandformHistoryCountsAreInvalid() || MaximumCoastalErosionPerPass <= 0 ||
        MaximumSedimentDepositionPerPass <= 0;

    private bool LandformHistoryCountsAreInvalid() =>
        AncestralValleyMinimumErosion <= 0 || OutwashMinimumGlacialDeposition <= 0 ||
        ImpactLandformMinimumExcavation <= 0 || ImpactLandformMinimumRimUplift <= 0 ||
        InheritedRidgeActivityThreshold <= 0;

    private void ValidateScales()
    {
        if (TectonicScaleIsInvalid() || LandscapeScaleIsInvalid())
        {
            throw new InvalidOperationException("Physical worldgen feature scales are invalid.");
        }
    }

    private bool TectonicScaleIsInvalid() =>
        ContinentalTransitionWidth <= 0d || ContinentalWarpScale <= 0d ||
        TectonicEpochVariationScale <= 0d ||
        RidgeWidth <= 0d || RidgeUplift <= 0d || RiftDepth <= 0d ||
        ContinentalAssemblySeparationDepth <= 0d ||
        MantlePlumeRadius <= 0d || MantlePlumeMinimumSpacing <= 0d;

    private bool LandscapeScaleIsInvalid() =>
        ElevationVariationScale <= 0d || FluvialErosionStrength <= 0d ||
        ImpactMinimumRadius <= 0d || ImpactMaximumRadius < ImpactMinimumRadius ||
        ImpactMinimumSpacing <= 0d || ImpactThermalRadiusThreshold <= 0d ||
        WarmthPocketMinRadius <= 0d || HeartWarmthRadius <= 0d ||
        SeaIceVariationScale <= 0d;

    private void ValidateRanges()
    {
        if (TectonicRangesAreInvalid())
        {
            throw new InvalidOperationException("Physical worldgen tectonic ranges are invalid.");
        }

        ValidateHydrologyRanges();
        ValidateClimateRanges();
    }

    private bool TectonicRangesAreInvalid() =>
        ContinentalCrustFraction is <= 0d or >= 1d ||
        ContinentalCrustPotentialFloor is <= 0d or > 1d ||
        OceanicCrustPotentialCeiling is < 0d or >= 1d ||
        OceanicCrustPotentialCeiling >= ContinentalCrustPotentialFloor ||
        TectonicMotionRangesAreInvalid();

    private bool TectonicMotionRangesAreInvalid() =>
        TectonicMotionMinimum <= 0d ||
        TectonicMotionMaximum < TectonicMotionMinimum ||
        TectonicConvergenceThreshold < 0d ||
        !IsUnitScalar(TectonicBoundaryPersistence) ||
        MantlePlumeStrength is < 1 or > 1000;

    private void ValidateHydrologyRanges()
    {
        if (ShelfDepth <= 0 || ShelfBreakDepth <= ShelfDepth || RiverThresholdsAreInvalid())
        {
            throw new InvalidOperationException("Physical worldgen hydrology ranges are invalid.");
        }

        if (EndorheicLakeFraction is < 0d or > 1d)
        {
            throw new InvalidOperationException("Endorheic lake fraction is invalid.");
        }

        if (LandscapeEvolutionRangesAreInvalid())
        {
            throw new InvalidOperationException("Physical worldgen landscape-evolution ranges are invalid.");
        }
    }

    private bool RiverThresholdsAreInvalid() =>
        RiverAccumulationThreshold <= 0 || RiverMinimumAccumulationThreshold <= 0 ||
        RiverMinimumAccumulationThreshold > RiverAccumulationThreshold ||
        RiverBasinHeadwaterFraction is < 1 or > 1000;

    private bool LandscapeEvolutionRangesAreInvalid() =>
        !IsUnitScalar(SedimentTransportFraction) ||
        ImpactRangesAreInvalid() ||
        GlacialRunoffRangesAreInvalid() ||
        SedimentDepositionSlopeThreshold < 0 ||
        !IsUnitScalar(CoastalErosionWaveThreshold) ||
        !IsUnitScalar(CoastalErosionStrength) ||
        !IsUnitScalar(CoastalDepositionFraction) ||
        CoastalSeaLevelChange is < -1000 or > 1000 || RiverGeometryRangesAreInvalid();

    private bool ImpactRangesAreInvalid() =>
        !IsUnitScalar(ImpactDepthRatio) || !IsUnitScalar(ImpactRimHeightRatio) ||
        !IsUnitScalar(ImpactEjectaFraction) || !IsUnitScalar(ImpactFractureStrength);

    private bool GlacialRunoffRangesAreInvalid() =>
        GlacialMeltTemperatureFloor is < 0 or > 1000 ||
        !IsUnitScalar(GlacialAblationRunoffStrength) ||
        !IsUnitScalar(BasalMeltRunoffStrength) ||
        !IsUnitScalar(GlacialRetreatRunoffStrength) ||
        GlacierGroundingFraction is < 1 or > 1000;

    private bool RiverGeometryRangesAreInvalid() =>
        RiverChannelRangesAreInvalid() || FloodplainRangesAreInvalid() || DeltaRangesAreInvalid();

    private bool RiverChannelRangesAreInvalid() =>
        !IsUnitScalar(RiverChannelBaseStrength) || !IsUnitScalar(RiverChannelOrderStrength) ||
        !IsUnitScalar(RiverChannelDischargeStrength) || RiverChannelWidthDecay <= 0 ||
        !IsUnitScalar(RiverChannelMaximumLongitudinalAlignment) ||
        RiverChannelMaximumBankRelief < 0;

    private bool FloodplainRangesAreInvalid() =>
        FloodplainMaximumSlope < 0 || FloodplainMaximumRelief < 0 ||
        !IsUnitScalar(FloodplainMaximumLongitudinalAlignment) || FloodplainIntensityDecay <= 0;

    private bool DeltaRangesAreInvalid() =>
        !IsUnitScalar(DeltaMaximumWaveEnergy) ||
        !IsUnitScalar(DeltaMaximumCurrentStrength) ||
        !IsUnitScalar(DeltaMinimumForwardAlignment) || DeltaIntensityDecay <= 0;

    private static bool IsUnitScalar(int value) => value is >= 0 and <= 1000;

    private void ValidateClimateRanges()
    {
        if (BaseClimateRangesAreInvalid() || CurrentRangesAreInvalid() ||
            PrecipitationRangesAreInvalid() || SeaIceRangesAreInvalid())
        {
            throw new InvalidOperationException("Physical worldgen climate ranges are invalid.");
        }
    }

    private bool BaseClimateRangesAreInvalid() =>
        OceanModerationDistance <= 0 || OceanMoistureDistance <= 0 ||
        WarmthPocketCount < 1 || FractureBranchCount < 1 ||
        WarmthPocketMaxRadius < WarmthPocketMinRadius;

    private bool CurrentRangesAreInvalid() =>
        OceanCurrentMinimumStrength is < 0 or > 1000 ||
        OceanCurrentTemperatureStrength is < 0 or > 1000 ||
        OceanCurrentWarmthStrength is < 0 or > 1000 ||
        CurrentEvaporationStrength is < 0 or > 1000;

    private bool PrecipitationRangesAreInvalid() =>
        PrecipitationBaseline is < 0 or > 1000 ||
        OrographicPrecipitationStrength is < 0 or > 1000 ||
        RainShadowStrength is < 0 or > 1000 ||
        SnowTemperatureThreshold is < 0 or > 1000 || SnowTemperatureTransition <= 0;

    private bool SeaIceRangesAreInvalid() =>
        SeaIceCurrentResistance is < 0 or > 1000 ||
        SeaIceWaveResistance is < 0 or > 1000 ||
        SeaIceSuitabilityVariation is < 0 or > 1000;
}
