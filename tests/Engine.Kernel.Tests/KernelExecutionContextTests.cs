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
                   KernelProgramCodes.Planning))
        {
            simulation.SetAttribute(entityId, attributeId, 10f, tick: 0);

            Assert.Equal(
                new KernelExecutionContext(
                    KernelProgramCodes.Planning,
                    activeSourceCodeId),
                simulation.ExecutionContext);
        }

        Assert.True(simulation.Events.TryRead(cursor, out var read));
        Assert.Equal(KernelProgramCodes.Planning, read.Event.ProgramId);
        Assert.Equal(activeSourceCodeId, read.Event.SourceCodeId);
        var changed = read.Event.AsAttributeChanged();
        Assert.Equal(activeSourceCodeId, changed.SourceCodeId);
    }

    [Fact]
    public void ExecutionScopeRestoresPreviousContextAfterDispose()
    {
        var simulation = new Simulation();
        var initial = simulation.ExecutionContext;
        var outer = new KernelExecutionContext(
            KernelProgramCodes.Processes,
            SourceCodeId: 101);
        var inner = new KernelExecutionContext(
            KernelProgramCodes.Planning,
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
        for (var i = 0; i < programIds.Length; i++)
        {
            simulation.RegisterAttribute(
                kindId,
                firstAttributeId + i,
                minimum: 0f,
                maximum: 100f,
                defaultValue: 0f);
        }

        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
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

        var initial = simulation.ExecutionContext;
        var caller = new KernelExecutionContext(
            KernelProgramCodes.Planning,
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

        foreach (var system in systems)
        {
            Assert.Equal(1, system.TickCount);
            Assert.Equal(system.ProgramId, system.ObservedContext.ProgramId);
            Assert.True(
                system.ObservedContext.SourceCodeId > 0,
                $"Program {system.ProgramId} entered execution with no source context.");
        }

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

        foreach (var system in systems)
        {
            Assert.True(
                observedContextsByAttribute.TryGetValue(system.AttributeId, out var emittedContext),
                $"Program {system.ProgramId} did not emit its attribute event.");
            Assert.Equal(system.ObservedContext, emittedContext);
        }
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
                   KernelProgramCodes.Processes))
        {
            simulation.EmitProcessEvaluated(
                tick: 0,
                entityId: 1,
                kindId: 1,
                selectedGoalId: 2,
                processId: 3,
                candidateCount: 4,
                state: KernelEventState.Succeeded);
        }

        Assert.True(simulation.Events.TryRead(cursor, out var read));
        Assert.Equal(KernelEventType.ProcessEvaluated, read.Event.EventType);
        Assert.Equal(KernelProgramCodes.Processes, read.Event.ProgramId);
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
                   KernelProgramCodes.Planning))
        {
            simulation.AdvanceTicks(1);
        }

        Assert.True(simulation.Events.TryRead(cursor, out var read));
        Assert.Equal(KernelProgramCodes.Planning, read.Event.ProgramId);
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
            KernelProgramCodes.Planning,
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
            "src/Engine.Kernel/Simulation.cs",
            "src/Engine.Kernel/WorldState.cs",
        ], rawWriteOwners);
    }

    [Fact]
    public void KernelMutationApisDoNotExposeSourceCodeOverloads()
    {
        var forbidden = typeof(Simulation).Assembly
            .GetTypes()
            .Where(type => type == typeof(Simulation) || type == typeof(WorldState))
            .SelectMany(type => type.GetMethods())
            .Where(method => method.Name is
                "SeedEntity" or
                "SeedAttribute" or
                "SeedResource" or
                "SetAttribute" or
                "ModifyAttribute" or
                "SetResource" or
                "ModifyResource")
            .Where(method =>
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
        KernelProgramCodes.Processes,
        KernelProgramCodes.Relationships,
        KernelProgramCodes.Pressures,
        KernelProgramCodes.Generators,
        KernelProgramCodes.Degeneracy,
        KernelProgramCodes.Gambits,
        KernelProgramCodes.Planning,
    ];

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
