using Engine.Eventing;

namespace Engine.Kernel;

internal sealed record SimulationOptions
{
    public int EntityCapacity { get; init; } = 16384;
    // Relationship slots are lifetime totals: culled edges stay allocated as
    // the historical record, so capacity must cover every relationship ever
    // created in a run. Sized for 100k-tick evidence horizons; the full-game
    // horizon (~2.5M ticks) needs a history-eviction design, not bigger arrays.
    public int RelationshipCapacity { get; init; } = 65536;
    public int AttributeCapacity { get; init; } = 65536;
    public int TraitCapacity { get; init; } = 65536;
    public int ProvenanceInvocationCapacity { get; init; } = 8192;
    public int ProvenanceOutputCapacity { get; init; } = 65536;
    public int GrowableStorageModeCode { get; init; } = KernelGrowableStorageModeCodes.Enabled;
    public int GrowableStorageCheckCadenceTicks { get; init; } = 100;
    public int GrowableStorageThresholdPermille { get; init; } = 900;
    public int GrowableStorageGrowthFactor { get; init; } = 2;
    public int MapCellCapacity { get; init; } = 16384;
    public int MapGenerationRadiusCapacity { get; init; }
    public int MapInfluenceCapacity { get; init; } = 65536;
    public int SystemCapacity { get; init; } = 32;
    public int EventCapacity { get; init; } = 65536;
    public int EventCursorCapacity { get; init; } = 32;
    public IKernelEventSchemaValidator? EventSchemaValidator { get; init; }
    public int CommandCapacity { get; init; } = 1024;
    public int GambitCapacity { get; init; } = 256;
    public int GambitStepCapacity { get; init; } = 2048;
    public int GambitCooldownCapacity { get; init; } = 8192;
    public int GambitProviderSelectorCapacity { get; init; } = 512;
    public int GeneratorCapacity { get; init; } = 256;
    public int GeneratorItemCapacity { get; init; } = 4096;
    public int LifecycleRuleCapacity { get; init; } = 256;
    public int LowAltitudeVerbRuleCapacity { get; init; } = 256;
    public int LowAltitudeVerbItemCapacity { get; init; } = 1024;
    public int MovementCellCapacity { get; init; } = 32768;
    public int MovementProfileRuleCapacity { get; init; } = 256;
    public int MapActivityCellCapacity { get; init; } = 32768;
    public int MapSignalCellCapacity { get; init; } = 32768;
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
    public int MapFrontierThawRingCapacity { get; init; } = 8192;
    public int KnowledgeSiteCapacity { get; init; } = 256;
    public int KnowledgeGrantCapacity { get; init; } = 2048;
    public int KnowledgeModifierCapacity { get; init; } = 8192;
    public int MinorActorRuleCapacity { get; init; } = 64;
    public int MinorActorSourceRuleCapacity { get; init; } = 16384;
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
    public bool TraceEnabled { get; init; }
    public int Seed { get; init; } = 1;
}

internal sealed record KernelTuningOptions
{
    public KernelProgramTuningOptions Programs { get; init; } = new();
}

internal sealed record KernelProgramTuningOptions
{
    public int MinorActorMainTickRatio { get; init; } = 5;
    public float CultureInfluenceActionScale { get; init; } = 0.15f;
    public ProgramEnabledTuningOptions Enabled { get; init; } = new();
    public UpkeepProgramTuningOptions Upkeep { get; init; } = new();
    public GeneratorProgramTuningOptions Generators { get; init; } = new();
    public LowAltitudeVerbTuningOptions LowAltitudeVerbs { get; init; } = new();
    public CultureFieldTuningOptions CultureField { get; init; } = new();
    public WorldBeatsTuningOptions WorldBeats { get; init; } = new();
    public MapFrontierTuningOptions MapFrontier { get; init; } = new();

    public int ScaleMainTicks(int ticks)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        if (MinorActorMainTickRatio <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        return ticks == 0 ? 0 : checked(ticks * MinorActorMainTickRatio);
    }

    public int MainTickAge(int startedTick, int currentTick)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(startedTick);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(currentTick);
        if (MinorActorMainTickRatio <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        return currentTick <= startedTick ? 0 : (currentTick - startedTick) / MinorActorMainTickRatio;
    }
}

internal sealed record ProgramEnabledTuningOptions
{
    public int Movement { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int LowAltitudeVerbs { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int MapActivity { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int MapSignals { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int Effects { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int CultureSlots { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int Upkeep { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int Gambits { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int Relationships { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int Generators { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
    public int MinorActors { get; init; } = ProgramEnabledOverrideCodes.Unspecified;
}

internal sealed record UpkeepProgramTuningOptions
{
    public int AgingModelCode { get; init; } = UpkeepAgingModelCodes.Linear;
}

internal sealed record CultureFieldTuningOptions
{
    public float DecayPerPulse { get; init; } = 0.005f;
    public float MinimumDecayPerPulse { get; init; } = 0.6f;
    public float CullFloorRawMass { get; init; } = 1f;
    public int DecayCadenceMainTicks { get; init; } = 5;
    public float RemittanceShare { get; init; } = 0.5f;
    public int RemittanceMinimumDistance { get; init; } = 6;

    /// <summary>
    /// The standing tide: in contested cells the locally weaker culture
    /// decays faster, so fronts form where cultures touch and boundaries
    /// can visibly move. 1 disables contest.
    /// </summary>
    public float ContestDecayMultiplier { get; init; } = 6f;

    /// <summary>A rival needs at least this raw mass (and more than the local side) for a cell to count as contested.</summary>
    public float ContestFloorRawMass { get; init; } = 4f;
}

internal sealed record WorldBeatsTuningOptions
{
    /// <summary>Age recognition cadence in main ticks (raw ticks = this x MinorActorMainTickRatio).</summary>
    public int CadenceMainTicks { get; init; } = 25;
}

internal sealed record MapFrontierTuningOptions
{
    /// <summary>Thaw/reclaim cadence in main ticks (raw ticks = this x MinorActorMainTickRatio).</summary>
    public int CadenceMainTicks { get; init; } = 25;
}

internal sealed record GeneratorProgramTuningOptions
{
    public bool RetryFireTargets { get; init; } = true;
    public int PulseWidthTicks { get; init; } = GeneratorProgramConstants.DefaultPulseWidthTicks;
    public int FiresPerPulse { get; init; } = GeneratorProgramConstants.DefaultFiresPerPulse;
}

internal sealed record LowAltitudeVerbTuningOptions
{
    public float OpportunityRate { get; init; } = 0.4f;
    public float OpportunityBonus { get; init; } = 1f;
    public int RecentTargetCooldownTicks { get; init; } = 90;
    public int RecentTargetRingSize { get; init; } = 3;
    public float CellSaturationFactor { get; init; } = 0.25f;
    public float ExploreDistanceScale { get; init; } = 6f;
    public float TerrainDistanceScale { get; init; } = 6f;
    public float EntityKindDistanceScale { get; init; } = 6f;
    public float RelationshipTargetDistanceScale { get; init; } = 8f;
    public float OwnInfluenceDistanceScale { get; init; } = 6f;
    public float ForeignInfluenceDistanceScale { get; init; } = 6f;
    public float InfluenceShortfallSamplingFactor { get; init; } = 1f;
    public float ForeignInfluencePresenceSamplingFactor { get; init; } = 1f;
}
