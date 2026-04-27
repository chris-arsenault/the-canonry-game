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
        writer.Write(options.EntityCapacity);
        writer.Write(options.RelationshipCapacity);
        writer.Write(options.AttributeCapacity);
        writer.Write(options.ResourceCapacity);
        writer.Write(options.MapCellCapacity);
        writer.Write(options.SystemCapacity);
        writer.Write(options.EventCapacity);
        writer.Write(options.EventCursorCapacity);
        writer.Write(options.CommandCapacity);
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
        writer.Write(options.RuntimeCodes.RelationshipCreatedCodeId);
        writer.Write(options.RuntimeCodes.RelationshipCulledCodeId);
        writer.Write(options.RuntimeCodes.RelationshipStrengthChangedCodeId);
        writer.Write(options.RuntimeCodes.ProcessSourceCodeId);
        writer.Write(options.RuntimeCodes.GambitSourceCodeId);
        writer.Write(options.RuntimeCodes.UpkeepSourceCodeId);
        writer.Write(options.RuntimeCodes.StartupSourceCodeId);
        writer.Write(options.RuntimeCodes.RelationshipSourceCodeId);
        writer.Write(options.RuntimeCodes.GeneratorSourceCodeId);
        writer.Write(options.RuntimeCodes.PressureSourceCodeId);
        writer.Write(options.RuntimeCodes.DegeneracySourceCodeId);
        writer.Write(options.RuntimeCodes.PlanningSourceCodeId);
        writer.Write(options.Seed);
        writer.Write(eventCount);
    }

    public static SimulationOptions Read(
        global::System.IO.BinaryReader reader,
        SimulationOptions? options)
    {
        var entityCapacity = reader.ReadInt32();
        var relationshipCapacity = reader.ReadInt32();
        var attributeCapacity = reader.ReadInt32();
        var resourceCapacity = reader.ReadInt32();
        var mapCellCapacity = reader.ReadInt32();
        var systemCapacity = reader.ReadInt32();
        var eventCapacity = reader.ReadInt32();
        var eventCursorCapacity = reader.ReadInt32();
        var commandCapacity = reader.ReadInt32();
        var processCapacity = reader.ReadInt32();
        var processStepCapacity = reader.ReadInt32();
        var processCooldownCapacity = reader.ReadInt32();
        var processObjectiveCapacity = reader.ReadInt32();
        var processProviderSelectorCapacity = reader.ReadInt32();
        var processSupportRuleCapacity = reader.ReadInt32();
        var processCommitmentCapacity = reader.ReadInt32();
        var gambitCapacity = reader.ReadInt32();
        var gambitStepCapacity = reader.ReadInt32();
        var gambitCooldownCapacity = reader.ReadInt32();
        var gambitObjectiveCapacity = reader.ReadInt32();
        var gambitProviderSelectorCapacity = reader.ReadInt32();
        var gambitSupportRuleCapacity = reader.ReadInt32();
        var gambitCommitmentCapacity = reader.ReadInt32();
        var generatorCapacity = reader.ReadInt32();
        var generatorItemCapacity = reader.ReadInt32();
        var pressureCapacity = reader.ReadInt32();
        var pressureValueCapacity = reader.ReadInt32();
        var degeneracyDetectorCapacity = reader.ReadInt32();
        var degeneracyStateCapacity = reader.ReadInt32();
        var lifecycleRuleCapacity = reader.ReadInt32();
        var entityAttributeCapacity = reader.ReadInt32();
        var upkeepRuleCapacity = reader.ReadInt32();
        var attributeIdCapacity = reader.ReadInt32();
        var kindIdCapacity = reader.ReadInt32();
        var terrainIdCapacity = reader.ReadInt32();
        var relationshipRuleCapacity = reader.ReadInt32();
        var relationshipKindIdCapacity = reader.ReadInt32();
        var relationshipGraphDeltaMergeThreshold = reader.ReadInt32();
        var relationshipCreatedCodeId = reader.ReadInt32();
        var relationshipCulledCodeId = reader.ReadInt32();
        var relationshipStrengthChangedCodeId = reader.ReadInt32();
        var processSourceCodeId = reader.ReadInt32();
        var gambitSourceCodeId = reader.ReadInt32();
        var upkeepSourceCodeId = reader.ReadInt32();
        var startupSourceCodeId = reader.ReadInt32();
        var relationshipSourceCodeId = reader.ReadInt32();
        var generatorSourceCodeId = reader.ReadInt32();
        var pressureSourceCodeId = reader.ReadInt32();
        var degeneracySourceCodeId = reader.ReadInt32();
        var planningSourceCodeId = reader.ReadInt32();
        var seed = reader.ReadInt32();
        var eventCount = reader.ReadInt32();
        if (eventCapacity <= eventCount)
        {
            eventCapacity = eventCount + 1;
        }

        if (options is not null)
        {
            if (options.EventCapacity > eventCapacity)
            {
                eventCapacity = options.EventCapacity;
            }

            if (options.EventCursorCapacity > eventCursorCapacity)
            {
                eventCursorCapacity = options.EventCursorCapacity;
            }

            if (options.CommandCapacity > commandCapacity)
            {
                commandCapacity = options.CommandCapacity;
            }
        }

        return new SimulationOptions
        {
            EntityCapacity = entityCapacity,
            RelationshipCapacity = relationshipCapacity,
            AttributeCapacity = attributeCapacity,
            ResourceCapacity = resourceCapacity,
            MapCellCapacity = mapCellCapacity,
            SystemCapacity = systemCapacity,
            EventCapacity = eventCapacity,
            EventCursorCapacity = eventCursorCapacity,
            EventSchemaValidator = options?.EventSchemaValidator,
            CommandCapacity = commandCapacity,
            ProcessCapacity = processCapacity,
            ProcessStepCapacity = processStepCapacity,
            ProcessCooldownCapacity = processCooldownCapacity,
            ProcessObjectiveCapacity = processObjectiveCapacity,
            ProcessProviderSelectorCapacity = processProviderSelectorCapacity,
            ProcessSupportRuleCapacity = processSupportRuleCapacity,
            ProcessCommitmentCapacity = processCommitmentCapacity,
            GambitCapacity = gambitCapacity,
            GambitStepCapacity = gambitStepCapacity,
            GambitCooldownCapacity = gambitCooldownCapacity,
            GambitObjectiveCapacity = gambitObjectiveCapacity,
            GambitProviderSelectorCapacity = gambitProviderSelectorCapacity,
            GambitSupportRuleCapacity = gambitSupportRuleCapacity,
            GambitCommitmentCapacity = gambitCommitmentCapacity,
            GeneratorCapacity = generatorCapacity,
            GeneratorItemCapacity = generatorItemCapacity,
            PressureCapacity = pressureCapacity,
            PressureValueCapacity = pressureValueCapacity,
            DegeneracyDetectorCapacity = degeneracyDetectorCapacity,
            DegeneracyStateCapacity = degeneracyStateCapacity,
            LifecycleRuleCapacity = lifecycleRuleCapacity,
            EntityAttributeCapacity = entityAttributeCapacity,
            UpkeepRuleCapacity = upkeepRuleCapacity,
            AttributeIdCapacity = attributeIdCapacity,
            KindIdCapacity = kindIdCapacity,
            TerrainIdCapacity = terrainIdCapacity,
            RelationshipRuleCapacity = relationshipRuleCapacity,
            RelationshipKindIdCapacity = relationshipKindIdCapacity,
            RelationshipGraphDeltaMergeThreshold = relationshipGraphDeltaMergeThreshold,
            RuntimeCodes = new KernelRuntimeCodes(
                relationshipCreatedCodeId,
                relationshipCulledCodeId,
                relationshipStrengthChangedCodeId,
                processSourceCodeId,
                gambitSourceCodeId,
                upkeepSourceCodeId,
                startupSourceCodeId,
                relationshipSourceCodeId,
                generatorSourceCodeId,
                pressureSourceCodeId,
                degeneracySourceCodeId,
                planningSourceCodeId),
            Seed = seed,
        };
    }
}
