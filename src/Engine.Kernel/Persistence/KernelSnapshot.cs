namespace Engine.Kernel;

internal static class KernelSnapshot
{
    private const int FormatCode = 1263747121;
    private const int FormatVersion = 4;

    public static void SaveKernelState(Simulation simulation, global::System.IO.Stream destination)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        global::System.ArgumentNullException.ThrowIfNull(destination);
        using var writer = new global::System.IO.BinaryWriter(
            destination,
            global::System.Text.Encoding.UTF8,
            leaveOpen: true);
        writer.Write(FormatCode);
        writer.Write(FormatVersion);
        KernelSnapshotOptions.Write(writer, simulation.Options, simulation.World.SnapshotEventCount());
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
        writer.Write(options.ResourceCapacity);
        writer.Write(options.MapCellCapacity);
        writer.Write(options.SystemCapacity);
        writer.Write(options.EventCapacity);
        writer.Write(options.EventCursorCapacity);
        writer.Write(options.CommandCapacity);
    }

    private static void WriteProgramCapacities(
        global::System.IO.BinaryWriter writer,
        SimulationOptions options)
    {
        writer.Write(options.ProcessCapacity);
        writer.Write(options.ProcessStepCapacity);
        writer.Write(options.ProcessCooldownCapacity);
        writer.Write(options.ProcessObjectiveCapacity);
        writer.Write(options.ProcessProviderSelectorCapacity);
        writer.Write(options.ProcessSupportRuleCapacity);
        writer.Write(options.ProcessCommitmentCapacity);
        writer.Write(options.GambitCapacity);
        writer.Write(options.GambitStepCapacity);
        writer.Write(options.GambitCooldownCapacity);
        writer.Write(options.GambitObjectiveCapacity);
        writer.Write(options.GambitProviderSelectorCapacity);
        writer.Write(options.GambitSupportRuleCapacity);
        writer.Write(options.GambitCommitmentCapacity);
        writer.Write(options.GeneratorCapacity);
        writer.Write(options.GeneratorItemCapacity);
        writer.Write(options.PressureCapacity);
        writer.Write(options.PressureValueCapacity);
    }

    private static void WriteWorldCapacities(
        global::System.IO.BinaryWriter writer,
        SimulationOptions options)
    {
        writer.Write(options.DegeneracyDetectorCapacity);
        writer.Write(options.DegeneracyStateCapacity);
        writer.Write(options.LifecycleRuleCapacity);
        writer.Write(options.EntityAttributeCapacity);
        writer.Write(options.UpkeepRuleCapacity);
        writer.Write(options.AttributeIdCapacity);
        writer.Write(options.KindIdCapacity);
        writer.Write(options.TerrainIdCapacity);
        writer.Write(options.RelationshipRuleCapacity);
        writer.Write(options.RelationshipKindIdCapacity);
        writer.Write(options.RelationshipGraphDeltaMergeThreshold);
    }

    private static void WriteRuntimeCodes(
        global::System.IO.BinaryWriter writer,
        KernelRuntimeCodes runtimeCodes)
    {
        writer.Write(runtimeCodes.RelationshipCreatedCodeId);
        writer.Write(runtimeCodes.RelationshipCulledCodeId);
        writer.Write(runtimeCodes.RelationshipStrengthChangedCodeId);
        writer.Write(runtimeCodes.ProcessSourceCodeId);
        writer.Write(runtimeCodes.GambitSourceCodeId);
        writer.Write(runtimeCodes.UpkeepSourceCodeId);
        writer.Write(runtimeCodes.StartupSourceCodeId);
        writer.Write(runtimeCodes.RelationshipSourceCodeId);
        writer.Write(runtimeCodes.GeneratorSourceCodeId);
        writer.Write(runtimeCodes.PressureSourceCodeId);
        writer.Write(runtimeCodes.DegeneracySourceCodeId);
        writer.Write(runtimeCodes.PlanningSourceCodeId);
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
            ResourceCapacity = reader.ReadInt32(),
            MapCellCapacity = reader.ReadInt32(),
            SystemCapacity = reader.ReadInt32(),
            EventCapacity = reader.ReadInt32(),
            EventCursorCapacity = reader.ReadInt32(),
            CommandCapacity = reader.ReadInt32(),
        };

        ReadProgramValues(reader, ref values);
        ReadWorldValues(reader, ref values);
        ReadRuntimeCodeValues(reader, ref values);
        values.Seed = reader.ReadInt32();
        return values;
    }

    private static void ReadProgramValues(
        global::System.IO.BinaryReader reader,
        ref SnapshotOptionValues values)
    {
        values.ProcessCapacity = reader.ReadInt32();
        values.ProcessStepCapacity = reader.ReadInt32();
        values.ProcessCooldownCapacity = reader.ReadInt32();
        values.ProcessObjectiveCapacity = reader.ReadInt32();
        values.ProcessProviderSelectorCapacity = reader.ReadInt32();
        values.ProcessSupportRuleCapacity = reader.ReadInt32();
        values.ProcessCommitmentCapacity = reader.ReadInt32();
        values.GambitCapacity = reader.ReadInt32();
        values.GambitStepCapacity = reader.ReadInt32();
        values.GambitCooldownCapacity = reader.ReadInt32();
        values.GambitObjectiveCapacity = reader.ReadInt32();
        values.GambitProviderSelectorCapacity = reader.ReadInt32();
        values.GambitSupportRuleCapacity = reader.ReadInt32();
        values.GambitCommitmentCapacity = reader.ReadInt32();
        values.GeneratorCapacity = reader.ReadInt32();
        values.GeneratorItemCapacity = reader.ReadInt32();
        values.PressureCapacity = reader.ReadInt32();
        values.PressureValueCapacity = reader.ReadInt32();
    }

    private static void ReadWorldValues(
        global::System.IO.BinaryReader reader,
        ref SnapshotOptionValues values)
    {
        values.DegeneracyDetectorCapacity = reader.ReadInt32();
        values.DegeneracyStateCapacity = reader.ReadInt32();
        values.LifecycleRuleCapacity = reader.ReadInt32();
        values.EntityAttributeCapacity = reader.ReadInt32();
        values.UpkeepRuleCapacity = reader.ReadInt32();
        values.AttributeIdCapacity = reader.ReadInt32();
        values.KindIdCapacity = reader.ReadInt32();
        values.TerrainIdCapacity = reader.ReadInt32();
        values.RelationshipRuleCapacity = reader.ReadInt32();
        values.RelationshipKindIdCapacity = reader.ReadInt32();
        values.RelationshipGraphDeltaMergeThreshold = reader.ReadInt32();
    }

    private static void ReadRuntimeCodeValues(
        global::System.IO.BinaryReader reader,
        ref SnapshotOptionValues values)
    {
        values.RelationshipCreatedCodeId = reader.ReadInt32();
        values.RelationshipCulledCodeId = reader.ReadInt32();
        values.RelationshipStrengthChangedCodeId = reader.ReadInt32();
        values.ProcessSourceCodeId = reader.ReadInt32();
        values.GambitSourceCodeId = reader.ReadInt32();
        values.UpkeepSourceCodeId = reader.ReadInt32();
        values.StartupSourceCodeId = reader.ReadInt32();
        values.RelationshipSourceCodeId = reader.ReadInt32();
        values.GeneratorSourceCodeId = reader.ReadInt32();
        values.PressureSourceCodeId = reader.ReadInt32();
        values.DegeneracySourceCodeId = reader.ReadInt32();
        values.PlanningSourceCodeId = reader.ReadInt32();
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
            ResourceCapacity = values.ResourceCapacity,
            MapCellCapacity = values.MapCellCapacity,
            SystemCapacity = values.SystemCapacity,
            EventCapacity = values.EventCapacity,
            EventCursorCapacity = values.EventCursorCapacity,
            EventSchemaValidator = options?.EventSchemaValidator,
            CommandCapacity = values.CommandCapacity,
            ProcessCapacity = values.ProcessCapacity,
            ProcessStepCapacity = values.ProcessStepCapacity,
            ProcessCooldownCapacity = values.ProcessCooldownCapacity,
            ProcessObjectiveCapacity = values.ProcessObjectiveCapacity,
            ProcessProviderSelectorCapacity = values.ProcessProviderSelectorCapacity,
            ProcessSupportRuleCapacity = values.ProcessSupportRuleCapacity,
            ProcessCommitmentCapacity = values.ProcessCommitmentCapacity,
            GambitCapacity = values.GambitCapacity,
            GambitStepCapacity = values.GambitStepCapacity,
            GambitCooldownCapacity = values.GambitCooldownCapacity,
            GambitObjectiveCapacity = values.GambitObjectiveCapacity,
            GambitProviderSelectorCapacity = values.GambitProviderSelectorCapacity,
            GambitSupportRuleCapacity = values.GambitSupportRuleCapacity,
            GambitCommitmentCapacity = values.GambitCommitmentCapacity,
            GeneratorCapacity = values.GeneratorCapacity,
            GeneratorItemCapacity = values.GeneratorItemCapacity,
            PressureCapacity = values.PressureCapacity,
            PressureValueCapacity = values.PressureValueCapacity,
            DegeneracyDetectorCapacity = values.DegeneracyDetectorCapacity,
            DegeneracyStateCapacity = values.DegeneracyStateCapacity,
            LifecycleRuleCapacity = values.LifecycleRuleCapacity,
            EntityAttributeCapacity = values.EntityAttributeCapacity,
            UpkeepRuleCapacity = values.UpkeepRuleCapacity,
            AttributeIdCapacity = values.AttributeIdCapacity,
            KindIdCapacity = values.KindIdCapacity,
            TerrainIdCapacity = values.TerrainIdCapacity,
            RelationshipRuleCapacity = values.RelationshipRuleCapacity,
            RelationshipKindIdCapacity = values.RelationshipKindIdCapacity,
            RelationshipGraphDeltaMergeThreshold = values.RelationshipGraphDeltaMergeThreshold,
            RuntimeCodes = new KernelRuntimeCodes(
                values.RelationshipCreatedCodeId,
                values.RelationshipCulledCodeId,
                values.RelationshipStrengthChangedCodeId,
                values.ProcessSourceCodeId,
                values.GambitSourceCodeId,
                values.UpkeepSourceCodeId,
                values.StartupSourceCodeId,
                values.RelationshipSourceCodeId,
                values.GeneratorSourceCodeId,
                values.PressureSourceCodeId,
                values.DegeneracySourceCodeId,
                values.PlanningSourceCodeId),
            Seed = values.Seed,
        };
    }

    private struct SnapshotOptionValues
    {
        public int EntityCapacity;
        public int RelationshipCapacity;
        public int AttributeCapacity;
        public int ResourceCapacity;
        public int MapCellCapacity;
        public int SystemCapacity;
        public int EventCapacity;
        public int EventCursorCapacity;
        public int CommandCapacity;
        public int ProcessCapacity;
        public int ProcessStepCapacity;
        public int ProcessCooldownCapacity;
        public int ProcessObjectiveCapacity;
        public int ProcessProviderSelectorCapacity;
        public int ProcessSupportRuleCapacity;
        public int ProcessCommitmentCapacity;
        public int GambitCapacity;
        public int GambitStepCapacity;
        public int GambitCooldownCapacity;
        public int GambitObjectiveCapacity;
        public int GambitProviderSelectorCapacity;
        public int GambitSupportRuleCapacity;
        public int GambitCommitmentCapacity;
        public int GeneratorCapacity;
        public int GeneratorItemCapacity;
        public int PressureCapacity;
        public int PressureValueCapacity;
        public int DegeneracyDetectorCapacity;
        public int DegeneracyStateCapacity;
        public int LifecycleRuleCapacity;
        public int EntityAttributeCapacity;
        public int UpkeepRuleCapacity;
        public int AttributeIdCapacity;
        public int KindIdCapacity;
        public int TerrainIdCapacity;
        public int RelationshipRuleCapacity;
        public int RelationshipKindIdCapacity;
        public int RelationshipGraphDeltaMergeThreshold;
        public int RelationshipCreatedCodeId;
        public int RelationshipCulledCodeId;
        public int RelationshipStrengthChangedCodeId;
        public int ProcessSourceCodeId;
        public int GambitSourceCodeId;
        public int UpkeepSourceCodeId;
        public int StartupSourceCodeId;
        public int RelationshipSourceCodeId;
        public int GeneratorSourceCodeId;
        public int PressureSourceCodeId;
        public int DegeneracySourceCodeId;
        public int PlanningSourceCodeId;
        public int Seed;
    }
}
