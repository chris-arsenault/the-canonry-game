namespace Engine.Kernel;

internal static class KernelSnapshot
{
    private const int FormatCode = 1263747121;
    private const int FormatVersion = 56;

    public static void SaveKernelState(Simulation simulation, global::System.IO.Stream destination)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        global::System.ArgumentNullException.ThrowIfNull(destination);
        if (!simulation.IsConvergedForSnapshot)
        {
            throw new global::System.InvalidOperationException();
        }
        using var writer = new global::System.IO.BinaryWriter(
            destination,
            global::System.Text.Encoding.UTF8,
            leaveOpen: true);
        writer.Write(FormatCode);
        writer.Write(FormatVersion);
        KernelSnapshotOptions.Write(
            writer,
            simulation.Options,
            simulation.SnapshotEventCount());
        simulation.WriteSnapshot(writer);
        writer.Flush();
    }

    public static Simulation LoadKernelState(
        global::System.IO.Stream source,
        SimulationOptions? options = null)
    {
        global::System.ArgumentNullException.ThrowIfNull(source);
        using var reader = new global::System.IO.BinaryReader(
            source,
            global::System.Text.Encoding.UTF8,
            leaveOpen: true);
        if (reader.ReadInt32() != FormatCode || reader.ReadInt32() != FormatVersion)
        {
            throw new global::System.InvalidOperationException();
        }

        var snapshotOptions = KernelSnapshotOptions.Read(reader, options);
        var simulation = new Simulation(snapshotOptions);
        simulation.ReadSnapshot(reader);
        simulation.EmitSnapshotEvents();
        return simulation;
    }
}

internal static class KernelSnapshotOptions
{
    public static void Write(
        global::System.IO.BinaryWriter writer,
        SimulationOptions options,
        int eventCount)
    {
        WriteStorageCapacities(writer, options);
        WriteProgramCapacities(writer, options);
        WriteWorldCapacities(writer, options);
        WriteRuntimeCodes(writer, options.RuntimeCodes);
        WriteKernelTuning(writer, options.KernelTuning);
        writer.Write(options.Execution.ReverseEvaluationOrder);
        writer.Write(options.Seed);
        writer.Write(eventCount);
    }

    private static void WriteStorageCapacities(
        global::System.IO.BinaryWriter writer,
        SimulationOptions options)
    {
        writer.Write(options.EntityCapacity);
        writer.Write(options.RelationshipCapacity);
        writer.Write(options.AttributeCapacity);
        writer.Write(options.TraitCapacity);
        writer.Write(options.MapCellCapacity);
        writer.Write(options.MapGenerationRadiusCapacity);
        writer.Write(options.MapInfluenceCapacity);
        writer.Write(options.SystemCapacity);
        writer.Write(options.EventCapacity);
        writer.Write(options.EventCursorCapacity);
        writer.Write(options.CommandCapacity);
    }

    private static void WriteProgramCapacities(
        global::System.IO.BinaryWriter writer,
        SimulationOptions options)
    {
        writer.Write(options.GambitCapacity);
        writer.Write(options.GambitStepCapacity);
        writer.Write(options.GambitProviderSelectorCapacity);
        writer.Write(options.GeneratorCapacity);
        writer.Write(options.GeneratorItemCapacity);
    }

    private static void WriteWorldCapacities(
        global::System.IO.BinaryWriter writer,
        SimulationOptions options)
    {
        writer.Write(options.LifecycleRuleCapacity);
        writer.Write(options.LowAltitudeVerbRuleCapacity);
        writer.Write(options.LowAltitudeVerbItemCapacity);
        writer.Write(options.MovementCellCapacity);
        writer.Write(options.MovementProfileRuleCapacity);
        writer.Write(options.EffectRuleCapacity);
        writer.Write(options.CultureSlotCapacity);
        writer.Write(options.CultureSlotAllowedKindCapacity);
        writer.Write(options.CultureSlotScoreRuleCapacity);
        writer.Write(options.MinorActorCapacity);
        writer.Write(options.MinorActorRuleCapacity);
        writer.Write(options.EntityAttributeCapacity);
        writer.Write(options.EntityTraitCapacity);
        writer.Write(options.AttributeIdCapacity);
        writer.Write(options.TraitIdCapacity);
        writer.Write(options.KindIdCapacity);
        writer.Write(options.CultureIdCapacity);
        writer.Write(options.TerrainIdCapacity);
        writer.Write(options.RelationshipRuleCapacity);
        writer.Write(options.RelationshipKindIdCapacity);
        writer.Write(options.RelationshipGraphDeltaMergeThreshold);
        writer.Write(options.MapFrontierThawRingCapacity);
        writer.Write(options.MapFrontierCandidateCapacity);
    }

    private static void WriteRuntimeCodes(
        global::System.IO.BinaryWriter writer,
        KernelRuntimeCodes runtimeCodes)
    {
        writer.Write(runtimeCodes.RelationshipCreatedCodeId);
        writer.Write(runtimeCodes.RelationshipCulledCodeId);
        writer.Write(runtimeCodes.RelationshipStrengthChangedCodeId);
        writer.Write(runtimeCodes.GambitSourceCodeId);
        writer.Write(runtimeCodes.StartupSourceCodeId);
        writer.Write(runtimeCodes.RelationshipSourceCodeId);
        writer.Write(runtimeCodes.GeneratorSourceCodeId);
        writer.Write(runtimeCodes.MapSourceCodeId);
        writer.Write(runtimeCodes.LowAltitudeVerbSourceCodeId);
        writer.Write(runtimeCodes.EffectsSourceCodeId);
        writer.Write(runtimeCodes.PlayerSourceCodeId);
        writer.Write(runtimeCodes.ChronicleSourceCodeId);
    }

    private static void WriteKernelTuning(
        global::System.IO.BinaryWriter writer,
        KernelTuningOptions kernelTuning)
    {
        WriteProgramEnabledOverrides(writer, kernelTuning.Programs.Enabled);
        writer.Write(kernelTuning.Programs.CultureInfluenceActionScale.Units);
        writer.Write(kernelTuning.Programs.Lifecycle.AgingModelCode);
        writer.Write(kernelTuning.Programs.Generators.RetryFireTargets);
        writer.Write(kernelTuning.Programs.Generators.FireWindowTicks);
        writer.Write(kernelTuning.Programs.Generators.FiresPerWindow);
        writer.Write(kernelTuning.Programs.Generators.FrontierFireWindowMultiplier);
        writer.Write(kernelTuning.Programs.Generators.SettledFireWindowMultiplier);
        writer.Write(kernelTuning.Programs.Generators.SettledCoveredCellThreshold);
        writer.Write(kernelTuning.Programs.Generators.HeartlandCoveredCellThreshold);
        writer.Write(kernelTuning.Programs.Generators.ProvinceBandRefreshTicks);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.OpportunityRate.Units);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.OpportunityBonus.Units);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.RecentTargetCooldownTicks);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.RecentTargetRingSize);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.CellSaturationFactor.Units);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.ExploreDistanceScale.Units);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.TerrainDistanceScale.Units);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.EntityKindDistanceScale.Units);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.RelationshipTargetDistanceScale.Units);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.OwnInfluenceDistanceScale.Units);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.ForeignInfluenceDistanceScale.Units);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.InfluenceShortfallSamplingFactor.Units);
        writer.Write(kernelTuning.Programs.LowAltitudeVerbs.ForeignInfluencePresenceSamplingFactor.Units);
        writer.Write(kernelTuning.Programs.CultureField.DecayPerRun.Units);
        writer.Write(kernelTuning.Programs.CultureField.MinimumDecayPerRun.Units);
        writer.Write(kernelTuning.Programs.CultureField.CullFloorRawMass.Units);
        writer.Write(kernelTuning.Programs.CultureField.DecayCadenceTicks);
        writer.Write(kernelTuning.Programs.CultureField.RemittanceShare.Units);
        writer.Write(kernelTuning.Programs.CultureField.RemittanceMinimumDistance);
        writer.Write(kernelTuning.Programs.CultureField.ContestDecayMultiplier.Units);
        writer.Write(kernelTuning.Programs.CultureField.ContestFloorRawMass.Units);
    }

    private static void WriteProgramEnabledOverrides(
        global::System.IO.BinaryWriter writer,
        ProgramEnabledTuningOptions enabled)
    {
        writer.Write(enabled.Movement);
        writer.Write(enabled.LowAltitudeVerbs);
        writer.Write(enabled.Effects);
        writer.Write(enabled.CultureSlots);
        writer.Write(enabled.Gambits);
        writer.Write(enabled.Relationships);
        writer.Write(enabled.Generators);
        writer.Write(enabled.MinorActors);
    }

    public static SimulationOptions Read(
        global::System.IO.BinaryReader reader,
        SimulationOptions? options)
    {
        var values = ReadValues(reader);
        var eventCount = reader.ReadInt32();
        ApplyEventCapacityFloor(ref values, eventCount);
        ApplyOptionOverrides(ref values, options);
        return CreateOptions(values, options);
    }

    private static SnapshotOptionValues ReadValues(global::System.IO.BinaryReader reader)
    {
        var values = new SnapshotOptionValues
        {
            EntityCapacity = reader.ReadInt32(),
            RelationshipCapacity = reader.ReadInt32(),
            AttributeCapacity = reader.ReadInt32(),
            TraitCapacity = reader.ReadInt32(),
            MapCellCapacity = reader.ReadInt32(),
            MapGenerationRadiusCapacity = reader.ReadInt32(),
            MapInfluenceCapacity = reader.ReadInt32(),
            SystemCapacity = reader.ReadInt32(),
            EventCapacity = reader.ReadInt32(),
            EventCursorCapacity = reader.ReadInt32(),
            CommandCapacity = reader.ReadInt32(),
        };

        ReadProgramValues(reader, ref values);
        ReadWorldValues(reader, ref values);
        ReadRuntimeCodeValues(reader, ref values);
        ReadKernelTuningValues(reader, ref values);
        values.ReverseEvaluationOrder = reader.ReadBoolean();
        values.Seed = reader.ReadInt32();
        return values;
    }

    private static void ReadProgramValues(
        global::System.IO.BinaryReader reader,
        ref SnapshotOptionValues values)
    {
        values.GambitCapacity = reader.ReadInt32();
        values.GambitStepCapacity = reader.ReadInt32();
        values.GambitProviderSelectorCapacity = reader.ReadInt32();
        values.GeneratorCapacity = reader.ReadInt32();
        values.GeneratorItemCapacity = reader.ReadInt32();
    }

    private static void ReadWorldValues(
        global::System.IO.BinaryReader reader,
        ref SnapshotOptionValues values)
    {
        values.LifecycleRuleCapacity = reader.ReadInt32();
        values.LowAltitudeVerbRuleCapacity = reader.ReadInt32();
        values.LowAltitudeVerbItemCapacity = reader.ReadInt32();
        values.MovementCellCapacity = reader.ReadInt32();
        values.MovementProfileRuleCapacity = reader.ReadInt32();
        values.EffectRuleCapacity = reader.ReadInt32();
        values.CultureSlotCapacity = reader.ReadInt32();
        values.CultureSlotAllowedKindCapacity = reader.ReadInt32();
        values.CultureSlotScoreRuleCapacity = reader.ReadInt32();
        values.MinorActorCapacity = reader.ReadInt32();
        values.MinorActorRuleCapacity = reader.ReadInt32();
        values.EntityAttributeCapacity = reader.ReadInt32();
        values.EntityTraitCapacity = reader.ReadInt32();
        values.AttributeIdCapacity = reader.ReadInt32();
        values.TraitIdCapacity = reader.ReadInt32();
        values.KindIdCapacity = reader.ReadInt32();
        values.CultureIdCapacity = reader.ReadInt32();
        values.TerrainIdCapacity = reader.ReadInt32();
        values.RelationshipRuleCapacity = reader.ReadInt32();
        values.RelationshipKindIdCapacity = reader.ReadInt32();
        values.RelationshipGraphDeltaMergeThreshold = reader.ReadInt32();
        values.MapFrontierThawRingCapacity = reader.ReadInt32();
        values.MapFrontierCandidateCapacity = reader.ReadInt32();
    }

    private static void ReadRuntimeCodeValues(
        global::System.IO.BinaryReader reader,
        ref SnapshotOptionValues values)
    {
        values.RelationshipCreatedCodeId = reader.ReadInt32();
        values.RelationshipCulledCodeId = reader.ReadInt32();
        values.RelationshipStrengthChangedCodeId = reader.ReadInt32();
        values.GambitSourceCodeId = reader.ReadInt32();
        values.StartupSourceCodeId = reader.ReadInt32();
        values.RelationshipSourceCodeId = reader.ReadInt32();
        values.GeneratorSourceCodeId = reader.ReadInt32();
        values.MapSourceCodeId = reader.ReadInt32();
        values.LowAltitudeVerbSourceCodeId = reader.ReadInt32();
        values.EffectsSourceCodeId = reader.ReadInt32();
        values.PlayerSourceCodeId = reader.ReadInt32();
        values.ChronicleSourceCodeId = reader.ReadInt32();
    }

    private static void ReadKernelTuningValues(
        global::System.IO.BinaryReader reader,
        ref SnapshotOptionValues values)
    {
        values.MovementProgramEnabled = reader.ReadInt32();
        values.LowAltitudeVerbProgramEnabled = reader.ReadInt32();
        values.EffectsProgramEnabled = reader.ReadInt32();
        values.CultureSlotsProgramEnabled = reader.ReadInt32();
        values.GambitsProgramEnabled = reader.ReadInt32();
        values.RelationshipsProgramEnabled = reader.ReadInt32();
        values.GeneratorsProgramEnabled = reader.ReadInt32();
        values.MinorActorsProgramEnabled = reader.ReadInt32();
        values.CultureInfluenceActionScale = FixedScalar.FromUnits(reader.ReadInt64());
        values.LifecycleAgingModelCode = reader.ReadInt32();
        ReadGeneratorTuningValues(reader, ref values);
        values.LowAltitudeOpportunityRate = FixedScalar.FromUnits(reader.ReadInt64());
        values.LowAltitudeOpportunityBonus = FixedScalar.FromUnits(reader.ReadInt64());
        values.LowAltitudeRecentTargetCooldownTicks = reader.ReadInt32();
        values.LowAltitudeRecentTargetRingSize = reader.ReadInt32();
        values.LowAltitudeCellSaturationFactor = FixedScalar.FromUnits(reader.ReadInt64());
        values.LowAltitudeExploreDistanceScale = FixedScalar.FromUnits(reader.ReadInt64());
        values.LowAltitudeTerrainDistanceScale = FixedScalar.FromUnits(reader.ReadInt64());
        values.LowAltitudeEntityKindDistanceScale = FixedScalar.FromUnits(reader.ReadInt64());
        values.LowAltitudeRelationshipTargetDistanceScale = FixedScalar.FromUnits(reader.ReadInt64());
        values.LowAltitudeOwnInfluenceDistanceScale = FixedScalar.FromUnits(reader.ReadInt64());
        values.LowAltitudeForeignInfluenceDistanceScale = FixedScalar.FromUnits(reader.ReadInt64());
        values.LowAltitudeInfluenceShortfallSamplingFactor = FixedScalar.FromUnits(reader.ReadInt64());
        values.LowAltitudeForeignInfluencePresenceSamplingFactor = FixedScalar.FromUnits(reader.ReadInt64());
        values.CultureFieldDecayPerRun = FixedScalar.FromUnits(reader.ReadInt64());
        values.CultureFieldMinimumDecayPerRun = FixedScalar.FromUnits(reader.ReadInt64());
        values.CultureFieldCullFloorRawMass = FixedScalar.FromUnits(reader.ReadInt64());
        values.CultureFieldDecayCadenceTicks = reader.ReadInt32();
        values.CultureFieldRemittanceShare = FixedScalar.FromUnits(reader.ReadInt64());
        values.CultureFieldRemittanceMinimumDistance = reader.ReadInt32();
        values.CultureFieldContestDecayMultiplier = FixedScalar.FromUnits(reader.ReadInt64());
        values.CultureFieldContestFloorRawMass = FixedScalar.FromUnits(reader.ReadInt64());
    }

    private static void ApplyEventCapacityFloor(ref SnapshotOptionValues values, int eventCount)
    {
        if (values.EventCapacity <= eventCount)
        {
            values.EventCapacity = eventCount + 1;
        }
    }

    private static void ApplyOptionOverrides(
        ref SnapshotOptionValues values,
        SimulationOptions? options)
    {
        if (options is not null)
        {
            if (options.EventCapacity > values.EventCapacity)
            {
                values.EventCapacity = options.EventCapacity;
            }

            if (options.EventCursorCapacity > values.EventCursorCapacity)
            {
                values.EventCursorCapacity = options.EventCursorCapacity;
            }

            if (options.CommandCapacity > values.CommandCapacity)
            {
                values.CommandCapacity = options.CommandCapacity;
            }
        }
    }

    private static SimulationOptions CreateOptions(
        SnapshotOptionValues values,
        SimulationOptions? options)
    {
        return new SimulationOptions
        {
            EntityCapacity = values.EntityCapacity,
            RelationshipCapacity = values.RelationshipCapacity,
            AttributeCapacity = values.AttributeCapacity,
            TraitCapacity = values.TraitCapacity,
            MapCellCapacity = values.MapCellCapacity,
            MapGenerationRadiusCapacity = values.MapGenerationRadiusCapacity,
            MapInfluenceCapacity = values.MapInfluenceCapacity,
            SystemCapacity = values.SystemCapacity,
            EventCapacity = values.EventCapacity,
            EventCursorCapacity = values.EventCursorCapacity,
            EventSchemaValidator = options?.EventSchemaValidator,
            CommandCapacity = values.CommandCapacity,
            GambitCapacity = values.GambitCapacity,
            GambitStepCapacity = values.GambitStepCapacity,
            GambitProviderSelectorCapacity = values.GambitProviderSelectorCapacity,
            GeneratorCapacity = values.GeneratorCapacity,
            GeneratorItemCapacity = values.GeneratorItemCapacity,
            LifecycleRuleCapacity = values.LifecycleRuleCapacity,
            LowAltitudeVerbRuleCapacity = values.LowAltitudeVerbRuleCapacity,
            LowAltitudeVerbItemCapacity = values.LowAltitudeVerbItemCapacity,
            MovementCellCapacity = values.MovementCellCapacity,
            MovementProfileRuleCapacity = values.MovementProfileRuleCapacity,
            EffectRuleCapacity = values.EffectRuleCapacity,
            CultureSlotCapacity = values.CultureSlotCapacity,
            CultureSlotAllowedKindCapacity = values.CultureSlotAllowedKindCapacity,
            CultureSlotScoreRuleCapacity = values.CultureSlotScoreRuleCapacity,
            MinorActorCapacity = values.MinorActorCapacity,
            MinorActorRuleCapacity = values.MinorActorRuleCapacity,
            EntityAttributeCapacity = values.EntityAttributeCapacity,
            EntityTraitCapacity = values.EntityTraitCapacity,
            AttributeIdCapacity = values.AttributeIdCapacity,
            TraitIdCapacity = values.TraitIdCapacity,
            KindIdCapacity = values.KindIdCapacity,
            CultureIdCapacity = values.CultureIdCapacity,
            TerrainIdCapacity = values.TerrainIdCapacity,
            RelationshipRuleCapacity = values.RelationshipRuleCapacity,
            RelationshipKindIdCapacity = values.RelationshipKindIdCapacity,
            RelationshipGraphDeltaMergeThreshold = values.RelationshipGraphDeltaMergeThreshold,
            MapFrontierThawRingCapacity = values.MapFrontierThawRingCapacity,
            MapFrontierCandidateCapacity = values.MapFrontierCandidateCapacity,
            RuntimeCodes = CreateRuntimeCodes(values),
            KernelTuning = CreateKernelTuning(values),
            Execution = new KernelExecutionOptions
            {
                WorkerCount = options?.Execution.WorkerCount ?? 1,
                ReverseEvaluationOrder = values.ReverseEvaluationOrder,
                OwnerPartitionCount =
                    options?.Execution.OwnerPartitionCount ??
                    KernelOwnerTopology.DefaultPartitionCount,
                OwnerLeadTicks =
                    options?.Execution.OwnerLeadTicks ??
                    KernelExecutionOptions.DefaultOwnerLeadTicks,
            },
            Seed = values.Seed,
        };
    }

    private static KernelRuntimeCodes CreateRuntimeCodes(SnapshotOptionValues values) =>
        new(
            values.RelationshipCreatedCodeId,
            values.RelationshipCulledCodeId,
            values.RelationshipStrengthChangedCodeId,
            values.GambitSourceCodeId,
            values.StartupSourceCodeId,
            values.RelationshipSourceCodeId,
            values.GeneratorSourceCodeId,
            values.MapSourceCodeId,
            values.LowAltitudeVerbSourceCodeId,
            values.EffectsSourceCodeId,
            values.PlayerSourceCodeId,
            values.ChronicleSourceCodeId);

    private static void ReadGeneratorTuningValues(
        global::System.IO.BinaryReader reader,
        ref SnapshotOptionValues values)
    {
        values.GeneratorRetryFireTargets = reader.ReadBoolean();
        values.GeneratorFireWindowTicks = reader.ReadInt32();
        values.GeneratorFiresPerWindow = reader.ReadInt32();
        values.GeneratorFrontierFireWindowMultiplier = reader.ReadInt32();
        values.GeneratorSettledFireWindowMultiplier = reader.ReadInt32();
        values.GeneratorSettledCoveredCellThreshold = reader.ReadInt32();
        values.GeneratorHeartlandCoveredCellThreshold = reader.ReadInt32();
        values.GeneratorProvinceBandRefreshTicks = reader.ReadInt32();
    }

    private static GeneratorProgramTuningOptions CreateGeneratorTuning(SnapshotOptionValues values) =>
        new()
        {
            RetryFireTargets = values.GeneratorRetryFireTargets,
            FireWindowTicks = values.GeneratorFireWindowTicks,
            FiresPerWindow = values.GeneratorFiresPerWindow,
            FrontierFireWindowMultiplier = values.GeneratorFrontierFireWindowMultiplier,
            SettledFireWindowMultiplier = values.GeneratorSettledFireWindowMultiplier,
            SettledCoveredCellThreshold = values.GeneratorSettledCoveredCellThreshold,
            HeartlandCoveredCellThreshold = values.GeneratorHeartlandCoveredCellThreshold,
            ProvinceBandRefreshTicks = values.GeneratorProvinceBandRefreshTicks,
        };

    private static KernelTuningOptions CreateKernelTuning(SnapshotOptionValues values) =>
        new()
        {
            Programs = new KernelProgramTuningOptions
            {
                Enabled = new ProgramEnabledTuningOptions
                {
                    Movement = values.MovementProgramEnabled,
                    LowAltitudeVerbs = values.LowAltitudeVerbProgramEnabled,
                    Effects = values.EffectsProgramEnabled,
                    CultureSlots = values.CultureSlotsProgramEnabled,
                    Gambits = values.GambitsProgramEnabled,
                    Relationships = values.RelationshipsProgramEnabled,
                    Generators = values.GeneratorsProgramEnabled,
                    MinorActors = values.MinorActorsProgramEnabled,
                },
                CultureInfluenceActionScale = values.CultureInfluenceActionScale,
                Lifecycle = new LifecycleTuningOptions
                {
                    AgingModelCode = values.LifecycleAgingModelCode,
                },
                Generators = CreateGeneratorTuning(values),
                LowAltitudeVerbs = new LowAltitudeVerbTuningOptions
                {
                    OpportunityRate = values.LowAltitudeOpportunityRate,
                    OpportunityBonus = values.LowAltitudeOpportunityBonus,
                    RecentTargetCooldownTicks = values.LowAltitudeRecentTargetCooldownTicks,
                    RecentTargetRingSize = values.LowAltitudeRecentTargetRingSize,
                    CellSaturationFactor = values.LowAltitudeCellSaturationFactor,
                    ExploreDistanceScale = values.LowAltitudeExploreDistanceScale,
                    TerrainDistanceScale = values.LowAltitudeTerrainDistanceScale,
                    EntityKindDistanceScale = values.LowAltitudeEntityKindDistanceScale,
                    RelationshipTargetDistanceScale = values.LowAltitudeRelationshipTargetDistanceScale,
                    OwnInfluenceDistanceScale = values.LowAltitudeOwnInfluenceDistanceScale,
                    ForeignInfluenceDistanceScale = values.LowAltitudeForeignInfluenceDistanceScale,
                    InfluenceShortfallSamplingFactor = values.LowAltitudeInfluenceShortfallSamplingFactor,
                    ForeignInfluencePresenceSamplingFactor = values.LowAltitudeForeignInfluencePresenceSamplingFactor,
                },
                CultureField = new CultureFieldTuningOptions
                {
                    DecayPerRun = values.CultureFieldDecayPerRun,
                    ContestDecayMultiplier = values.CultureFieldContestDecayMultiplier,
                    ContestFloorRawMass = values.CultureFieldContestFloorRawMass,
                    MinimumDecayPerRun = values.CultureFieldMinimumDecayPerRun,
                    CullFloorRawMass = values.CultureFieldCullFloorRawMass,
                    DecayCadenceTicks = values.CultureFieldDecayCadenceTicks,
                    RemittanceShare = values.CultureFieldRemittanceShare,
                    RemittanceMinimumDistance = values.CultureFieldRemittanceMinimumDistance,
                },
            },
        };

    private struct SnapshotOptionValues
    {
        public int EntityCapacity;
        public int RelationshipCapacity;
        public int AttributeCapacity;
        public int TraitCapacity;
        public int MapCellCapacity;
        public int MapGenerationRadiusCapacity;
        public int MapInfluenceCapacity;
        public int SystemCapacity;
        public int EventCapacity;
        public int EventCursorCapacity;
        public int CommandCapacity;
        public int GambitCapacity;
        public int GambitStepCapacity;
        public int GambitProviderSelectorCapacity;
        public int GeneratorCapacity;
        public int GeneratorItemCapacity;
        public int LifecycleRuleCapacity;
        public int LowAltitudeVerbRuleCapacity;
        public int LowAltitudeVerbItemCapacity;
        public int MovementCellCapacity;
        public int MovementProfileRuleCapacity;
        public int EffectRuleCapacity;
        public int CultureSlotCapacity;
        public int CultureSlotAllowedKindCapacity;
        public int CultureSlotScoreRuleCapacity;
        public int MinorActorCapacity;
        public int MinorActorRuleCapacity;
        public int EntityAttributeCapacity;
        public int EntityTraitCapacity;
        public int AttributeIdCapacity;
        public int TraitIdCapacity;
        public int KindIdCapacity;
        public int CultureIdCapacity;
        public int TerrainIdCapacity;
        public int RelationshipRuleCapacity;
        public int RelationshipKindIdCapacity;
        public int RelationshipGraphDeltaMergeThreshold;
        public int MapFrontierThawRingCapacity;
        public int MapFrontierCandidateCapacity;
        public int RelationshipCreatedCodeId;
        public int RelationshipCulledCodeId;
        public int RelationshipStrengthChangedCodeId;
        public int GambitSourceCodeId;
        public int StartupSourceCodeId;
        public int RelationshipSourceCodeId;
        public int GeneratorSourceCodeId;
        public int MapSourceCodeId;
        public int LowAltitudeVerbSourceCodeId;
        public int EffectsSourceCodeId;
        public int PlayerSourceCodeId;
        public int ChronicleSourceCodeId;
        public int MovementProgramEnabled;
        public int LowAltitudeVerbProgramEnabled;
        public int EffectsProgramEnabled;
        public int CultureSlotsProgramEnabled;
        public int GambitsProgramEnabled;
        public int RelationshipsProgramEnabled;
        public int GeneratorsProgramEnabled;
        public int MinorActorsProgramEnabled;
        public FixedScalar CultureInfluenceActionScale;
        public int LifecycleAgingModelCode;
        public bool GeneratorRetryFireTargets;
        public int GeneratorFireWindowTicks;
        public int GeneratorFiresPerWindow;
        public int GeneratorFrontierFireWindowMultiplier;
        public int GeneratorSettledFireWindowMultiplier;
        public int GeneratorSettledCoveredCellThreshold;
        public int GeneratorHeartlandCoveredCellThreshold;
        public int GeneratorProvinceBandRefreshTicks;
        public FixedScalar LowAltitudeOpportunityRate;
        public FixedScalar LowAltitudeOpportunityBonus;
        public int LowAltitudeRecentTargetCooldownTicks;
        public int LowAltitudeRecentTargetRingSize;
        public FixedScalar LowAltitudeCellSaturationFactor;
        public FixedScalar LowAltitudeExploreDistanceScale;
        public FixedScalar LowAltitudeTerrainDistanceScale;
        public FixedScalar LowAltitudeEntityKindDistanceScale;
        public FixedScalar LowAltitudeRelationshipTargetDistanceScale;
        public FixedScalar LowAltitudeOwnInfluenceDistanceScale;
        public FixedScalar LowAltitudeForeignInfluenceDistanceScale;
        public FixedScalar LowAltitudeInfluenceShortfallSamplingFactor;
        public FixedScalar LowAltitudeForeignInfluencePresenceSamplingFactor;
        public FixedScalar CultureFieldDecayPerRun;
        public FixedScalar CultureFieldContestDecayMultiplier;
        public FixedScalar CultureFieldContestFloorRawMass;
        public FixedScalar CultureFieldMinimumDecayPerRun;
        public FixedScalar CultureFieldCullFloorRawMass;
        public int CultureFieldDecayCadenceTicks;
        public FixedScalar CultureFieldRemittanceShare;
        public int CultureFieldRemittanceMinimumDistance;
        public bool ReverseEvaluationOrder;
        public int Seed;
    }
}
