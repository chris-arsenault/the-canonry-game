namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;
using Engine.Kernel.Programs;

public sealed class KernelExecutionContextTests
{
    [Fact]
    public void ActiveExecutionContextStampsMutationEvents()
    {
        const int attributeId = 1;
        const int activeSourceCodeId = 9001;
        var simulation = new Simulation(new SimulationOptions
        {
            EntityCapacity = 4,
            AttributeIdCapacity = 4,
            EntityAttributeCapacity = 4,
            EventCapacity = 64,
        });
        simulation.RegisterAttribute(
            kindId: 1,
            attributeId,
            minimum: 0f,
            maximum: 100f,
            defaultValue: 0f);
        var entityId = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        using (simulation.EnterExecutionContext(
                   activeSourceCodeId,
                   KernelProgramCodes.Effects))
        {
            simulation.SetAttribute(entityId, attributeId, 10f, tick: 0);

            Assert.Equal(
                new KernelExecutionContext(
                    KernelProgramCodes.Effects,
                    activeSourceCodeId),
                simulation.ExecutionContext);
        }

        Assert.True(simulation.Events.TryRead(cursor, out var read));
        Assert.Equal(KernelProgramCodes.Effects, read.Event.ProgramId);
        Assert.Equal(activeSourceCodeId, read.Event.SourceCodeId);
        var changed = read.Event.AsAttributeChanged();
        Assert.Equal(activeSourceCodeId, changed.SourceCodeId);
    }

    [Fact]
    public void ActiveExecutionContextStampsRuntimeCellMutationEvents()
    {
        const int activeSourceCodeId = 9002;
        var simulation = new Simulation(new SimulationOptions
        {
            EntityCapacity = 4,
            MapCellCapacity = 4,
            TerrainIdCapacity = 4,
            EventCapacity = 64,
        });
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        simulation.RegisterTerrain(2, boundaryGroupId: 1);
        simulation.CreateCell(q: 0, r: 0, terrainId: 1, ownerEntityId: 0);
        var ownerEntityId = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        using (simulation.EnterExecutionContext(
                   activeSourceCodeId,
                   KernelProgramCodes.Generators))
        {
            simulation.SetCellState(q: 0, r: 0, terrainId: 2, ownerEntityId, tick: 7);
        }

        Assert.Equal(2, simulation.World.CellTerrainId(0, 0));
        Assert.Equal(ownerEntityId, simulation.World.CoverageOwnerAt(0, 0));
        Assert.True(simulation.Events.TryRead(cursor, out var read));
        Assert.Equal(KernelProgramCodes.Generators, read.Event.ProgramId);
        Assert.Equal(activeSourceCodeId, read.Event.SourceCodeId);
        Assert.Equal(KernelEventType.MapCellStateChanged, read.Event.EventType);
        var changed = read.Event.AsMapCellStateChanged();
        Assert.Equal(7, changed.Tick);
        Assert.Equal(activeSourceCodeId, changed.SourceCodeId);
        Assert.Equal(PlaneCodes.Physical, changed.PlaneCode);
        Assert.Equal(1, changed.PreviousTerrainId);
        Assert.Equal(2, changed.CurrentTerrainId);
        Assert.Equal(0, changed.PreviousOwnerEntityId);
        Assert.Equal(ownerEntityId, changed.CurrentOwnerEntityId);
        Assert.True(simulation.Events.TryRead(cursor, out var legacyRead));
        Assert.Equal(KernelProgramCodes.Generators, legacyRead.Event.ProgramId);
        Assert.Equal(activeSourceCodeId, legacyRead.Event.SourceCodeId);
        Assert.Equal(KernelEventType.CellChanged, legacyRead.Event.EventType);
        var legacyChanged = legacyRead.Event.AsCellChanged();
        Assert.Equal(activeSourceCodeId, legacyChanged.SourceCodeId);
        Assert.False(simulation.Events.TryRead(cursor, out _));
    }

    [Fact]
    public void ExecutionScopeRestoresPreviousContextAfterDispose()
    {
        var simulation = new Simulation();
        var initial = simulation.ExecutionContext;
        var outer = new KernelExecutionContext(
            KernelProgramCodes.Gambits,
            SourceCodeId: 101);
        var inner = new KernelExecutionContext(
            KernelProgramCodes.Effects,
            SourceCodeId: 202);

        using (simulation.EnterExecutionContext(
                   outer.SourceCodeId,
                   outer.ProgramId))
        {
            Assert.Equal(outer, simulation.ExecutionContext);

            using (simulation.EnterExecutionContext(
                       inner.SourceCodeId,
                       inner.ProgramId))
            {
                Assert.Equal(inner, simulation.ExecutionContext);
            }

            Assert.Equal(outer, simulation.ExecutionContext);
        }

        Assert.Equal(initial, simulation.ExecutionContext);
    }

    [Fact]
    public void RegisteredProgramStartsEmitEventsWithTheirExecutionContextAndRestoreCallerContext()
    {
        const int kindId = 1;
        const int firstAttributeId = 100;
        var simulation = new Simulation(new SimulationOptions
        {
            EntityCapacity = 4,
            AttributeIdCapacity = 256,
            EntityAttributeCapacity = 128,
            EventCapacity = 256,
            SystemCapacity = 32,
        });
        var programIds = AllProgramIds();
        RegisterAttributesForPrograms(simulation, kindId, firstAttributeId, programIds.Length);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var systems = RegisterRecordingSystems(simulation, programIds, entityId, firstAttributeId);
        var initial = simulation.ExecutionContext;
        var caller = new KernelExecutionContext(
            KernelProgramCodes.Effects,
            SourceCodeId: 9000);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        using (simulation.EnterExecutionContext(
                   caller.SourceCodeId,
                   caller.ProgramId))
        {
            simulation.AdvanceTicks(1);

            Assert.Equal(caller, simulation.ExecutionContext);
        }

        Assert.Equal(initial, simulation.ExecutionContext);
        AssertSystemsObservedContext(systems);
        var observedContextsByAttribute = ReadAttributeContexts(simulation, cursor, firstAttributeId);
        AssertSystemEvents(systems, observedContextsByAttribute);
    }

    [Fact]
    public void RuntimeEventBoundaryStampsEventsWithoutPayloadSourceCode()
    {
        const int sourceCodeId = 7001;
        var simulation = new Simulation(new SimulationOptions
        {
            EntityCapacity = 4,
            EventCapacity = 64,
        });
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        using (simulation.EnterExecutionContext(
                   sourceCodeId,
                   KernelProgramCodes.Gambits))
        {
            simulation.EmitGambitEvaluated(
                tick: 0,
                entityId: 1,
                kindId: 1,
                gambitId: 3,
                candidateCount: 4,
                state: KernelEventState.Succeeded);
        }

        Assert.True(simulation.Events.TryRead(cursor, out var read));
        Assert.Equal(KernelEventType.GambitEvaluated, read.Event.EventType);
        Assert.Equal(KernelProgramCodes.Gambits, read.Event.ProgramId);
        Assert.Equal(sourceCodeId, read.Event.SourceCodeId);
    }

    [Fact]
    public void AnonymousSystemUsesAmbientExecutionContext()
    {
        const int kindId = 1;
        const int attributeId = 1;
        const int sourceCodeId = 5001;
        var simulation = new Simulation(new SimulationOptions
        {
            EntityCapacity = 4,
            AttributeIdCapacity = 4,
            EntityAttributeCapacity = 4,
            EventCapacity = 64,
        });
        simulation.RegisterAttribute(
            kindId,
            attributeId,
            minimum: 0f,
            maximum: 100f,
            defaultValue: 0f);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((sim, tick) => sim.SetAttribute(entityId, attributeId, 10f, tick)));
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        using (simulation.EnterExecutionContext(
                   sourceCodeId,
                   KernelProgramCodes.LowAltitudeVerbs))
        {
            simulation.AdvanceTicks(1);
        }

        Assert.True(simulation.Events.TryRead(cursor, out var read));
        Assert.Equal(KernelProgramCodes.LowAltitudeVerbs, read.Event.ProgramId);
        Assert.Equal(sourceCodeId, read.Event.SourceCodeId);
        var changed = read.Event.AsAttributeChanged();
        Assert.Equal(sourceCodeId, changed.SourceCodeId);
    }

    [Fact]
    public void ProgramExecutionRestoresPreviousContextWhenSystemThrows()
    {
        var simulation = new Simulation(new SimulationOptions { SystemCapacity = 16 });
        var initial = simulation.ExecutionContext;
        var caller = new KernelExecutionContext(
            KernelProgramCodes.Relationships,
            SourceCodeId: 7000);
        simulation.RegisterProgram(
            KernelProgramCodes.Effects,
            cadence: 1,
            new ThrowingSystem());

        using (simulation.EnterExecutionContext(
                   caller.SourceCodeId,
                   caller.ProgramId))
        {
            Assert.Throws<InvalidOperationException>(() => simulation.AdvanceTicks(1));
            Assert.Equal(caller, simulation.ExecutionContext);
        }

        Assert.Equal(initial, simulation.ExecutionContext);
    }

    [Fact]
    public void KernelProgramRuntimesDoNotWriteEventLogDirectly()
    {
        var root = FindRepoRoot();
        var programDirectory = Path.Combine(root.FullName, "src", "Engine.Kernel", "Programs");
        var violations = Directory
            .GetFiles(programDirectory, "*.cs", SearchOption.AllDirectories)
            .Select(file => (File: file, Text: File.ReadAllText(file)))
            .Where(candidate => candidate.Text.Contains("simulation.Events.TryWrite", StringComparison.Ordinal))
            .Select(candidate => Path.GetRelativePath(root.FullName, candidate.File).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void KernelSourcesOnlyWriteRawEventsThroughContextualBoundaries()
    {
        var root = FindRepoRoot();
        var kernelDirectory = Path.Combine(root.FullName, "src", "Engine.Kernel");
        var rawWriteOwners = Directory
            .GetFiles(kernelDirectory, "*.cs", SearchOption.AllDirectories)
            .Select(file => (File: file, Text: File.ReadAllText(file)))
            .Where(candidate =>
                candidate.Text.Contains("Events.TryWrite", StringComparison.Ordinal) ||
                candidate.Text.Contains("_events.TryWrite", StringComparison.Ordinal) ||
                candidate.Text.Contains("TryWrite(kernelEvent", StringComparison.Ordinal))
            .Select(candidate => Path.GetRelativePath(root.FullName, candidate.File).Replace('\\', '/'))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
        [
            "src/Engine.Kernel/Simulation.Events.cs",
            "src/Engine.Kernel/WorldState.RelationshipIndexes.cs",
        ], rawWriteOwners);
    }

    [Fact]
    public void KernelMutationApisDoNotExposeSourceCodeOverloads()
    {
        var forbidden = typeof(Simulation).Assembly
            .GetTypes()
            .SelectMany(type => type.GetMethods())
            .Where(method =>
                (method.DeclaringType == typeof(Simulation) || method.DeclaringType == typeof(WorldState)) &&
                method.Name is (
                    "SeedEntity" or
                    "SeedAttribute" or
                    "SetAttribute" or
                    "ModifyAttribute") &&
                method.GetParameters().Any(parameter =>
                    string.Equals(parameter.Name, "sourceCodeId", StringComparison.Ordinal) ||
                    string.Equals(parameter.Name, "attributeSourceCodeId", StringComparison.Ordinal)))
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(forbidden);
    }

    private static int[] AllProgramIds() =>
    [
        KernelProgramCodes.Upkeep,
        KernelProgramCodes.Relationships,
        KernelProgramCodes.Generators,
        KernelProgramCodes.Degeneracy,
        KernelProgramCodes.Gambits,
        KernelProgramCodes.Movement,
        KernelProgramCodes.MapActivity,
        KernelProgramCodes.LowAltitudeVerbs,
        KernelProgramCodes.Effects,
        KernelProgramCodes.MapSignals,
    ];

    private static void RegisterAttributesForPrograms(
        Simulation simulation,
        int kindId,
        int firstAttributeId,
        int programCount)
    {
        for (var i = 0; i < programCount; i++)
        {
            simulation.RegisterAttribute(
                kindId,
                firstAttributeId + i,
                minimum: 0f,
                maximum: 100f,
                defaultValue: 0f);
        }
    }

    private static RecordingEventSystem[] RegisterRecordingSystems(
        Simulation simulation,
        int[] programIds,
        int entityId,
        int firstAttributeId)
    {
        var systems = new RecordingEventSystem[programIds.Length];
        for (var i = 0; i < programIds.Length; i++)
        {
            systems[i] = new RecordingEventSystem(
                programIds[i],
                entityId,
                firstAttributeId + i,
                i + 1);
            simulation.RegisterProgram(programIds[i], cadence: 1, systems[i]);
        }

        return systems;
    }

    private static void AssertSystemsObservedContext(RecordingEventSystem[] systems)
    {
        foreach (var system in systems)
        {
            Assert.Equal(1, system.TickCount);
            Assert.Equal(system.ProgramId, system.ObservedContext.ProgramId);
            Assert.True(
                system.ObservedContext.SourceCodeId > 0,
                $"Program {system.ProgramId} entered execution with no source context.");
        }
    }

    private static Dictionary<int, KernelExecutionContext> ReadAttributeContexts(
        Simulation simulation,
        KernelEventCursor cursor,
        int firstAttributeId)
    {
        var observedContextsByAttribute = new Dictionary<int, KernelExecutionContext>();
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType != KernelEventType.AttributeChanged)
            {
                continue;
            }

            var changed = read.Event.AsAttributeChanged();
            if (changed.AttributeId >= firstAttributeId)
            {
                observedContextsByAttribute[changed.AttributeId] = new KernelExecutionContext(
                    read.Event.ProgramId,
                    read.Event.SourceCodeId);
            }
        }

        return observedContextsByAttribute;
    }

    private static void AssertSystemEvents(
        RecordingEventSystem[] systems,
        Dictionary<int, KernelExecutionContext> observedContextsByAttribute)
    {
        foreach (var system in systems)
        {
            Assert.True(
                observedContextsByAttribute.TryGetValue(system.AttributeId, out var emittedContext),
                $"Program {system.ProgramId} did not emit its attribute event.");
            Assert.Equal(system.ObservedContext, emittedContext);
        }
    }

    private sealed class RecordingEventSystem : ISimulationSystem
    {
        private readonly int _entityId;
        private readonly float _value;

        public RecordingEventSystem(
            int programId,
            int entityId,
            int attributeId,
            float value)
        {
            ProgramId = programId;
            _entityId = entityId;
            AttributeId = attributeId;
            _value = value;
        }

        public int ProgramId { get; }
        public int AttributeId { get; }
        public int TickCount { get; private set; }
        public KernelExecutionContext ObservedContext { get; private set; }

        public void Tick(Simulation simulation, int tick)
        {
            TickCount++;
            ObservedContext = simulation.ExecutionContext;
            simulation.SetAttribute(_entityId, AttributeId, _value, tick);
        }
    }

    private sealed class ThrowingSystem : ISimulationSystem
    {
        public void Tick(Simulation simulation, int tick)
        {
            throw new InvalidOperationException("test execution stop");
        }
    }

    private sealed class RecordingSystem(Action<Simulation, int> onTick) : ISimulationSystem
    {
        public void Tick(Simulation simulation, int tick) => onTick(simulation, tick);
    }

    private static DirectoryInfo FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TheCanonryGame.csproj")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("Could not find repository root.");
    }
}
