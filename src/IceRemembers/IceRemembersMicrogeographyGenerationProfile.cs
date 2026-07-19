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

    public CountTarget FireCoreCount { get; init; } = new(1, 180);

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
    public int ReefMaximumDepth { get; init; } = 125;

    public int ReefMinimumTemperature { get; init; } = 300;

    public int ReefMinimumGeothermal { get; init; } = 150;

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

    public int ScreeFieldMinimumSlope { get; init; } = 72;

    public int RiverValleyMinimumOrder { get; init; } = 1;

    public int AncestralRiverValleyMinimumErosion { get; init; } = 4;

    public int AncestralRiverValleyMaximumSlope { get; init; } = 68;

    public int MaximumFireCores { get; init; } = 180;

    public int FireCoreDensityThreshold { get; init; } = 590;

    public int FirePropagationBaseCost { get; init; } = 17;

    public int FireConductanceCostDivisor { get; init; } = 30;

    public int FireTemperatureFeedbackPercent { get; init; } = 36;

    public int FireIceMeltDivisor { get; init; } = 2;

    public int AuroraBaseInput { get; init; } = 360;

    public int AuroraLogScale { get; init; } = 66;

    public int StandingPressureRayCount { get; init; } = 720;

    public int CoastalPressureSourceCount { get; init; } = 18;

    public int AncientWakeSourceCount { get; init; } = 20;

    public int PressureCoastalThreshold { get; init; } = 540;

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
        if (FireParametersAreInvalid() || PressureParametersAreInvalid())
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
