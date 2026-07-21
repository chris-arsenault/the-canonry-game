namespace IceRemembers;

using Engine.WorldGen;

public sealed record ProportionRangeTarget
{
    public ProportionRangeTarget(double minimum, double maximum)
    {
        if (minimum is < 0d or > 1d || maximum is < 0d or > 1d || maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(minimum));
        }

        Minimum = minimum;
        Maximum = maximum;
    }

    public double Minimum { get; }

    public double Maximum { get; }

    public bool Contains(double value) => value >= Minimum && value <= Maximum;
}

public sealed record IceRemembersMicrogeographyQualityTargets
{
    public int MinimumEvaluationRadius { get; init; } = 32;

    public ProportionRangeTarget LargestNaturalSurfaceCoverage { get; init; } = new(0d, 0.58d);

    public ProportionRangeTarget LargestFinalSurfaceCoverage { get; init; } = new(0d, 0.65d);

    public ProportionRangeTarget MagicOverrideCoverage { get; init; } = new(0.002d, 0.28d);

    public ProportionRangeTarget MeltLineCoverage { get; init; } = new(0d, 0.05d);

    public CountTarget FireCoreCount { get; init; } = new(1, 1000);

    public CountTarget WarmHabitatComponentCount { get; init; } = new(8, int.MaxValue);

    internal void Validate()
    {
        if (MinimumEvaluationRadius < 0 || LargestNaturalSurfaceCoverage is null ||
            LargestFinalSurfaceCoverage is null || MagicOverrideCoverage is null ||
            MeltLineCoverage is null || FireCoreCount is null || WarmHabitatComponentCount is null)
        {
            throw new InvalidOperationException("Ice Remembers microgeography quality targets are invalid.");
        }
    }
}

public sealed record IceRemembersMicrogeographyGenerationProfile
{
    public int ReefMaximumDepth { get; init; } = 160;

    public int ReefMinimumTemperature { get; init; } = 260;

    public int ReefMinimumGeothermal { get; init; } = 60;

    /// <summary>
    /// Warm ocean-current heat that lets reefs grow past the temperature
    /// gate, so warm-current coasts carry reef arcs in a cold world.
    /// </summary>
    public int ReefWarmCurrentHeat { get; init; } = 420;

    /// <summary>
    /// Minimum ocean-current strength that reads as a visible current
    /// channel on the surface map.
    /// </summary>
    public int CurrentChannelMinimumStrength { get; init; } = 560;

    /// <summary>
    /// Minimum tectonic strain for deep-ocean plate boundaries to surface as
    /// trench terrain.
    /// </summary>
    public int TrenchMinimumStrain { get; init; } = 430;

    public int PackIceMinimumThickness { get; init; } = 50;

    public int SeaIceMaximumTemperature { get; init; } = 315;

    public int IceShelfMinimumThickness { get; init; } = 75;

    public int GlacierMinimumThickness { get; init; } = 300;

    public int SnowfieldMinimumThickness { get; init; } = 250;

    public int OpenFieldMinimumTemperature { get; init; } = 335;

    public int OpenFieldMinimumMoisture { get; init; } = 260;

    public int WetTundraMinimumMoisture { get; init; } = 620;

    public int ColdSteppeMinimumTemperature { get; init; } = 235;

    public int ColdSteppeMinimumMoisture { get; init; } = 150;

    public int ColdSteppeMaximumMoisture { get; init; } = 520;

    public int ColdSteppeMaximumSlope { get; init; } = 42;

    public int PolarBarrensMaximumTemperature { get; init; } = 255;

    public int PolarBarrensMaximumMoisture { get; init; } = 210;

    /// <summary>
    /// How much fully fire-worked ground hardens, in per-mille resistance.
    /// Baked and vitrified rock is durable, so fire country keeps its ridges.
    /// </summary>
    public double FireRockHardening { get; init; } = 900d;

    /// <summary>
    /// How much fully aurora-soaked ground weakens. Aurora ground is friable
    /// and washes out into hollows rather than standing up.
    /// </summary>
    public double AuroraRockWeakening { get; init; } = 520d;

    public int ScreeFieldMinimumSlope { get; init; } = 72;

    public int RiverValleyMinimumOrder { get; init; } = 1;

    public int AncestralRiverValleyMinimumErosion { get; init; } = 4;

    public int AncestralRiverValleyMaximumSlope { get; init; } = 68;

    public int MaximumFireCores { get; init; } = 900;

    public int FireCoreDensityThreshold { get; init; } = 300;

    /// <summary>
    /// Fire spreads as a conduit-limited flow along fractures rather than as a
    /// halo around heat. A low base cost with a strong conductance term makes
    /// firecore reach anisotropic — it follows lineations — which is both more
    /// legible and stops fire intensity reading as a recolouring of the
    /// geothermal field it shares a source with.
    /// </summary>
    public int FirePropagationBaseCost { get; init; } = 4;

    public int FireConductanceCostDivisor { get; init; } = 26;

    public int FireTemperatureFeedbackPercent { get; init; } = 36;

    public int FireIceMeltDivisor { get; init; } = 2;

    public int AuroraBaseInput { get; init; } = 360;

    public int AuroraLogScale { get; init; } = 66;

    public int StandingPressureRayCount { get; init; } = 1200;

    public int CoastalPressureSourceCount { get; init; } = 200;

    public int AncientWakeSourceCount { get; init; } = 260;

    public int MagicFeatureHysteresis { get; init; } = 90;

    public int PressureCoastalThreshold { get; init; } = 140;

    public int PressureCoastalWaveAmplification { get; init; } = 360;

    public int PressureCoastalCurrentAmplification { get; init; } = 180;

    public IceRemembersMicrogeographyQualityTargets QualityTargets { get; init; } = new();

    public void Validate()
    {
        ValidateNaturalSurfaceParameters();
        ValidateMagicParameters();
        QualityTargets.Validate();
    }

    private void ValidateNaturalSurfaceParameters()
    {
        if (ReefMaximumDepth <= 0 || PackIceMinimumThickness <= 0 ||
            IceShelfMinimumThickness <= 0 || GlacierMinimumThickness < SnowfieldMinimumThickness ||
            ColdSteppeMaximumMoisture < ColdSteppeMinimumMoisture ||
            AncestralRiverValleyMinimumErosion <= 0 || AncestralRiverValleyMaximumSlope < 0)
        {
            throw new InvalidOperationException("Ice Remembers natural-surface parameters are invalid.");
        }
    }

    private void ValidateMagicParameters()
    {
        if (FireParametersAreInvalid() || PressureParametersAreInvalid() ||
            MagicFeatureHysteresis is < 1 or > 300)
        {
            throw new InvalidOperationException("Ice Remembers magic-generation parameters are invalid.");
        }
    }

    private bool FireParametersAreInvalid() =>
        MaximumFireCores <= 0 || FireCoreDensityThreshold is < 0 or > 1000 ||
        FirePropagationBaseCost <= 0 || FireConductanceCostDivisor <= 0 || FireIceMeltDivisor <= 0;

    private bool PressureParametersAreInvalid() =>
        StandingPressureRayCount <= 0 || CoastalPressureSourceCount <= 0 ||
        AncientWakeSourceCount <= 0 || PressureCoastalThreshold is < 0 or > 1000 ||
        PressureCoastalWaveAmplification is < 0 or > 1000 ||
        PressureCoastalCurrentAmplification is < 0 or > 1000;
}
