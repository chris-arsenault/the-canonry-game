using Engine.Eventing;

namespace Engine.Kernel;

internal sealed record SimulationOptions
{
    public int EntityCapacity { get; init; } = 16384;
    // Relationship slots contain active edges only and are reused after cull.
    public int RelationshipCapacity { get; init; } = 65536;
    public int AttributeCapacity { get; init; } = 65536;
    public int TraitCapacity { get; init; } = 65536;
    public int MapCellCapacity { get; init; } = 131072;
    public int MapGenerationRadiusCapacity { get; init; }
    public int MapInfluenceCapacity { get; init; } = 1048576;
    public int SystemCapacity { get; init; } = 32;
    public int EventCapacity { get; init; } = 65536;
    public int EventCursorCapacity { get; init; } = 32;
    public IKernelEventSchemaValidator? EventSchemaValidator { get; init; }
    public int CommandCapacity { get; init; } = 1024;
    public int GambitCapacity { get; init; } = 256;
    public int GambitStepCapacity { get; init; } = 2048;
    public int GambitProviderSelectorCapacity { get; init; } = 512;
    public int GeneratorCapacity { get; init; } = 256;

    /// <summary>Maximum distinct physical superregions the provincial fire-window
    /// system can register over a run.</summary>
    public int GeneratorProvinceCapacity { get; init; } = 64;

    /// <summary>Maximum concurrently active artifact districts.</summary>
    public int DistrictCapacity { get; init; } = 1024;
    public int GeneratorItemCapacity { get; init; } = 4096;
    public int LifecycleRuleCapacity { get; init; } = 256;
    public int LowAltitudeVerbRuleCapacity { get; init; } = 256;
    public int LowAltitudeVerbItemCapacity { get; init; } = 1024;
    public int MovementCellCapacity { get; init; } = 131072;
    public int MovementProfileRuleCapacity { get; init; } = 256;
    public int EffectRuleCapacity { get; init; } = 512;
    public int CultureSlotCapacity { get; init; } = 64;
    public int CultureSlotAllowedKindCapacity { get; init; } = 256;
    public int CultureSlotScoreRuleCapacity { get; init; } = 512;
    public int MinorActorCapacity { get; init; } = 4096;
    public int ChronicleArchetypeCapacity { get; init; } = 16;
    public int ChronicleItemCapacity { get; init; } = 256;
    // The global chronicle concurrency cap: the ice tells at most this many
    // tales at once. Scarcity is the point - chronicles are player focus
    // objects, not ambient churn.
    public int ChronicleCapacity { get; init; } = 7;
    public int WorldBeatAgeCapacity { get; init; } = 24;
    public int WorldBeatItemCapacity { get; init; } = 256;
    public int MapFrontierThawRingCapacity { get; init; } = 65536;
    public int GeologicalFeatureCapacity { get; init; } = 1024;
    public int MapFrontierCandidateCapacity { get; init; } = 128;
    public int KnowledgeSiteCapacity { get; init; } = 256;
    public int KnowledgeGrantCapacity { get; init; } = 2048;
    public int KnowledgeModifierCapacity { get; init; } = 8192;
    public int MinorActorRuleCapacity { get; init; } = 64;
    public int EntityAttributeCapacity { get; init; } = 512;
    public int EntityTraitCapacity { get; init; } = 512;
    public int AttributeIdCapacity { get; init; } = 1024;
    public int TraitIdCapacity { get; init; } = 1024;
    public int KindIdCapacity { get; init; } = 256;
    public int CultureIdCapacity { get; init; } = 256;
    public int TerrainIdCapacity { get; init; } = 128;
    public int RelationshipRuleCapacity { get; init; } = 256;
    public int RelationshipKindIdCapacity { get; init; } = 256;
    public int RelationshipGraphDeltaMergeThreshold { get; init; } = 1024;
    public KernelRuntimeCodes RuntimeCodes { get; init; } = KernelRuntimeCodes.Default;
    public KernelTuningOptions KernelTuning { get; init; } = new();
    public KernelExecutionOptions Execution { get; init; } = new();
    public bool TraceEnabled { get; init; }
    public int Seed { get; init; } = 1;
}

internal sealed record KernelExecutionOptions
{
    public bool ReverseEvaluationOrder { get; init; }
    public bool ReverseDeltaInsertionOrder { get; init; }
    public int LogicalShardCount { get; init; } = 1;
}

internal sealed record KernelTuningOptions
{
    public KernelProgramTuningOptions Programs { get; init; } = new();
}

internal sealed record KernelProgramTuningOptions
{
    public int MainProgramCadenceTicks { get; init; } = 5;
    public FixedScalar CultureInfluenceActionScale { get; init; } = FixedScalar.FromUnits(150000L);
    public ProgramEnabledTuningOptions Enabled { get; init; } = new();
    public LifecycleTuningOptions Lifecycle { get; init; } = new();
    public GeneratorProgramTuningOptions Generators { get; init; } = new();
    public LowAltitudeVerbTuningOptions LowAltitudeVerbs { get; init; } = new();
    public CultureFieldTuningOptions CultureField { get; init; } = new();
    public WorldBeatsTuningOptions WorldBeats { get; init; } = new();
    public MapFrontierTuningOptions MapFrontier { get; init; } = new();

    public int ScaleMainProgramDuration(int duration)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(duration);
        NumericGuard.ThrowIfNegativeOrZero(MainProgramCadenceTicks);
        return duration == 0 ? 0 : checked(duration * MainProgramCadenceTicks);
    }
}

internal sealed record ProgramEnabledTuningOptions
{
    public int Movement { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int LowAltitudeVerbs { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int Effects { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int CultureSlots { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int Gambits { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int Relationships { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int Generators { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int MinorActors { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
}

internal sealed record LifecycleTuningOptions
{
    public int AgingModelCode { get; init; } = LifecycleAgingModelCodes.Linear;
}

internal sealed record CultureFieldTuningOptions
{
    // Eased from 0.005 with the stage rework: the worker treadmill that
    // paid this decay bill is retired, and interior holding now comes from
    // stage emission and districts (docs/design/entity-stages.md).
    public FixedScalar DecayPerRun { get; init; } = FixedScalar.FromUnits(4000L);
    public FixedScalar MinimumDecayPerRun { get; init; } = FixedScalar.FromUnits(600000L);
    public FixedScalar CullFloorRawMass { get; init; } = 1;
    public int DecayCadenceTicks { get; init; } = 25;
    public FixedScalar RemittanceShare { get; init; } = FixedScalar.FromUnits(500000L);
    public int RemittanceMinimumDistance { get; init; } = 6;

    /// <summary>
    /// The standing tide: in contested cells the locally weaker culture
    /// decays faster, so fronts form where cultures touch and boundaries
    /// can visibly move. 1 disables contest.
    /// </summary>
    public FixedScalar ContestDecayMultiplier { get; init; } = 6;

    /// <summary>A rival needs at least this raw mass (and more than the local side) for a cell to count as contested.</summary>
    public FixedScalar ContestFloorRawMass { get; init; } = 4;
}

internal sealed record WorldBeatsTuningOptions
{
    public int CadenceTicks { get; init; } = 125;
}

internal sealed record MapFrontierTuningOptions
{
    public int CadenceTicks { get; init; } = 125;
}

internal sealed record GeneratorProgramTuningOptions
{
    public bool RetryFireTargets { get; init; } = true;
    public int FireWindowTicks { get; init; } = GeneratorProgramConstants.DefaultFireWindowTicks;
    public int FiresPerWindow { get; init; } = GeneratorProgramConstants.DefaultFiresPerWindow;

    // Provincial fire windows: each physical-map superregion has a tempo set
    // by its settledness band, so throughput grows as
    // territory settles and each place generates its own stories. Bands are
    // derived from covered cells inside the superregion; multipliers stretch
    // FireWindowTicks for less settled ground. Multipliers of 1 collapse the
    // system back to uniform provincial tempo.
    public int FrontierFireWindowMultiplier { get; init; } = 8;
    public int SettledFireWindowMultiplier { get; init; } = 3;
    public int SettledCoveredCellThreshold { get; init; } = 15;
    public int HeartlandCoveredCellThreshold { get; init; } = 100;
    public int ProvinceBandRefreshTicks { get; init; } = 200;
}

internal sealed record LowAltitudeVerbTuningOptions
{
    public FixedScalar OpportunityRate { get; init; } = FixedScalar.FromUnits(400000L);
    public FixedScalar OpportunityBonus { get; init; } = 1;
    public int RecentTargetCooldownTicks { get; init; } = 90;
    public int RecentTargetRingSize { get; init; } = 3;
    // Sized so a one-ring stack of ~10 cuts a destination's weight to ~1/7:
    // occupied ground genuinely repels new arrivals instead of mildly
    // discounting them, which is what keeps resting entities from
    // converging into piles at the few most attractive targets.
    public FixedScalar CellSaturationFactor { get; init; } = FixedScalar.FromUnits(600000L);
    public FixedScalar ExploreDistanceScale { get; init; } = 6;
    public FixedScalar TerrainDistanceScale { get; init; } = 6;
    public FixedScalar EntityKindDistanceScale { get; init; } = 6;
    public FixedScalar RelationshipTargetDistanceScale { get; init; } = 8;
    public FixedScalar OwnInfluenceDistanceScale { get; init; } = 6;
    public FixedScalar ForeignInfluenceDistanceScale { get; init; } = 6;
    public FixedScalar InfluenceShortfallSamplingFactor { get; init; } = 1;
    public FixedScalar ForeignInfluencePresenceSamplingFactor { get; init; } = 1;

    /// <summary>
    /// The soft leash: physical movement targets outside the entity's own
    /// culture support keep only this share of their weight. Fog-adjacent
    /// exploration verbs ignore the leash - ranging is their job.
    /// </summary>
    public FixedScalar UnsupportedTargetMultiplier { get; init; } = FixedScalar.FromUnits(350000L);

    /// <summary>Own-culture raw mass at or above this counts as supported ground.</summary>
    public FixedScalar SupportThresholdRawMass { get; init; } = 12;
}
