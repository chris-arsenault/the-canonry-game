namespace Engine.Kernel;

using Engine.Eventing;
using Engine.Kernel.Programs;

public readonly record struct CellResourceSeed(int ResourceId, float Value);

public sealed class Simulation : IKernel
{
    private readonly int[] _systemPhases;
    private readonly int[] _systemCadences;
    private readonly ISimulationSystem?[] _systems;
    private readonly bool[] _systemActive;
    private readonly KernelProgramRegistry _programs;
    private readonly int _maxPhase;
    private int _systemCount;
    private int _writerThreadId;
    private RandomSource _random;

    public Simulation(SimulationOptions? options = null)
    {
        options ??= new SimulationOptions();
        _maxPhase = options.MaxPhase;
        Events = new KernelEventLog(
            options.EventCapacity,
            maxCursors: options.EventCursorCapacity,
            schemaValidator: options.EventSchemaValidator);
        Commands = new KernelCommandRing(options.CommandCapacity);
        World = new WorldState(Events, options);
        Processes = new ProcessProgramRuntime(
            options.ProcessCapacity,
            options.ProcessStepCapacity,
            options.ProcessCooldownCapacity,
            options.MapCellCapacity,
            options.EntityCapacity,
            options.ProcessObjectiveCapacity,
            options.ProcessProviderSelectorCapacity,
            options.ProcessSupportRuleCapacity,
            options.ProcessCommitmentCapacity);
        Generators = new GeneratorProgramRuntime(options.GeneratorCapacity, options.GeneratorItemCapacity);
        Pressures = new PressureProgramRuntime(options.PressureCapacity, options.PressureValueCapacity);
        Degeneracy = new DegeneracyProgramRuntime(options.DegeneracyDetectorCapacity, options.DegeneracyStateCapacity);
        Upkeep = new UpkeepProgramRuntime(options.AttributeRuleCapacity, options.LifecycleRuleCapacity);
        Relationships = new RelationshipProgramRuntime(options.RelationshipRuleCapacity);
        ProcessSourceCodeId = options.RuntimeCodes.ProcessSourceCodeId;
        CardSourceCodeId = options.RuntimeCodes.CardSourceCodeId;
        UpkeepSourceCodeId = options.RuntimeCodes.UpkeepSourceCodeId;
        _random = new RandomSource(options.Seed);
        _systemPhases = new int[options.SystemCapacity];
        _systemCadences = new int[options.SystemCapacity];
        _systems = new ISimulationSystem[options.SystemCapacity];
        _systemActive = new bool[options.SystemCapacity];
        _programs = new KernelProgramRegistry(options.SystemCapacity);
        RegisterProgram(KernelProgramCodes.Upkeep, TickPhaseCodes.Low, 1, Upkeep);
        RegisterProgram(KernelProgramCodes.Relationships, TickPhaseCodes.Low, 1, Relationships);
        RegisterProgram(KernelProgramCodes.Processes, TickPhaseCodes.Process, 1, Processes);
        RegisterProgram(KernelProgramCodes.Pressures, TickPhaseCodes.Pressure, 5, Pressures);
        RegisterProgram(KernelProgramCodes.Generators, TickPhaseCodes.Generator, 1, Generators);
        RegisterProgram(KernelProgramCodes.Degeneracy, TickPhaseCodes.Event, 10, Degeneracy);
    }

    public WorldState World { get; }
    public KernelEventLog Events { get; }
    public KernelCommandRing Commands { get; }
    public ProcessProgramRuntime Processes { get; }
    public GeneratorProgramRuntime Generators { get; }
    public PressureProgramRuntime Pressures { get; }
    public DegeneracyProgramRuntime Degeneracy { get; }
    public UpkeepProgramRuntime Upkeep { get; }
    public RelationshipProgramRuntime Relationships { get; }
    public int ProcessSourceCodeId { get; }
    public int CardSourceCodeId { get; }
    public int UpkeepSourceCodeId { get; }
    public int CurrentTick { get; private set; }
    public long CurrentVersion => WorldVersion;
    public long WorldVersion => World.WorldVersion;

    public int RegisterAttribute(
        int kindId,
        int attributeId,
        float minimum,
        float maximum,
        float defaultValue,
        float defaultVariance = 0f,
        float driftRate = 0f,
        int locationResourceId = 0,
        float locationResourceDriftPerUnit = 0f,
        float locationResourceDriftMinOffset = 0f,
        float locationResourceDriftMaxOffset = 0f,
        int criticalTargetAttributeId = 0,
        float criticalThreshold = 15f,
        float criticalDelta = 0f)
    {
        return Upkeep.RegisterAttribute(
            World,
            kindId,
            attributeId,
            minimum,
            maximum,
            defaultValue,
            defaultVariance,
            driftRate,
            locationResourceId,
            locationResourceDriftPerUnit,
            locationResourceDriftMinOffset,
            locationResourceDriftMaxOffset,
            criticalTargetAttributeId,
            criticalThreshold,
            criticalDelta);
    }

    public int CreateEntity(
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS,
        int tick)
    {
        return MaterializeEntity(
            0,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            tick);
    }

    public void SeedCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId)
    {
        AssertStartupSeedTick();
        CreateCell(q, r, terrainId, ownerEntityId);
    }

    public void SeedCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        global::System.ReadOnlySpan<CellResourceSeed> resources)
    {
        AssertStartupSeedTick();
        CreateCell(q, r, terrainId, ownerEntityId);
        for (var i = 0; i < resources.Length; i++)
        {
            var resource = resources[i];
            SetResource(resource.ResourceId, q, r, ProcessSourceCodeId, resource.Value, CurrentTick);
        }
    }

    public void CreateCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId)
    {
        World.MaterializeCell(q, r, terrainId, ownerEntityId);
        WriteKernelEvent(KernelEvent.From(new CellCreatedKernelEvent(
            CurrentTick,
            WorldVersion,
            q,
            r,
            terrainId,
            ownerEntityId)));
    }

    public void SeedEntity(
        int entityId,
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS)
    {
        AssertStartupSeedTick();
        _ = MaterializeEntity(
            entityId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            CurrentTick);
    }

    private int MaterializeEntity(
        int entityId,
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS,
        int tick)
    {
        var createdEntityId = World.MaterializeEntity(
            entityId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            tick);
        WriteKernelEvent(KernelEvent.From(new EntityCreatedKernelEvent(
            tick,
            WorldVersion,
            createdEntityId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS)));
        Upkeep.InitializeEntity(this, createdEntityId, tick);
        return createdEntityId;
    }

    public void SeedAttribute(
        int entityId,
        int attributeId,
        int sourceCodeId,
        float value)
    {
        AssertStartupSeedTick();
        SetAttribute(entityId, attributeId, sourceCodeId, value, CurrentTick);
    }

    public void SeedResource(
        int resourceId,
        int q,
        int r,
        int sourceCodeId,
        float value)
    {
        AssertStartupSeedTick();
        SetResource(resourceId, q, r, sourceCodeId, value, CurrentTick);
    }

    public void SeedRelationship(
        int relationshipKindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        int factId0,
        int factId1,
        int factId2,
        int factId3,
        int factCount,
        float strength)
    {
        AssertStartupSeedTick();
        _ = CreateRelationship(
            relationshipKindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            CurrentTick,
            factId0,
            factId1,
            factId2,
            factId3,
            factCount);
    }

    public void SetAttribute(
        int entityId,
        int attributeId,
        int sourceCodeId,
        float value,
        int tick)
    {
        World.SetAttribute(entityId, attributeId, sourceCodeId, value, tick);
    }

    public void SetResource(
        int resourceId,
        int q,
        int r,
        int sourceCodeId,
        float value,
        int tick)
    {
        World.SetResource(resourceId, q, r, sourceCodeId, value, tick);
    }

    public int CreateRelationship(
        int relationshipKindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        float strength,
        int tick,
        int factId0 = 0,
        int factId1 = 0,
        int factId2 = 0,
        int factId3 = 0,
        int factCount = 0)
    {
        return World.CreateRelationship(
            relationshipKindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            tick,
            factId0,
            factId1,
            factId2,
            factId3,
            factCount);
    }

    public int RegisterSystem(int phase, int cadence, ISimulationSystem system)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(phase);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThan(phase, _maxPhase);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cadence);
        global::System.ArgumentNullException.ThrowIfNull(system);

        var slot = FindSystemSlot();
        _systemPhases[slot] = phase;
        _systemCadences[slot] = cadence;
        _systems[slot] = system;
        _systemActive[slot] = true;
        if (slot >= _systemCount)
        {
            _systemCount = slot + 1;
        }

        return slot;
    }

    public int RegisterProgram(int programId, int phase, int cadence, ISimulationSystem system)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(programId);
        var handle = RegisterSystem(phase, cadence, system);
        _programs.Register(programId, handle);
        return handle;
    }

    public void SetSystemActive(int handle, bool active)
    {
        if ((uint)handle >= (uint)_systemActive.Length || _systems[handle] is null)
        {
            throw new global::System.InvalidOperationException();
        }

        _systemActive[handle] = active;
    }

    public void SetProgramEnabled(int programId, bool enabled)
    {
        _programs.SetEnabled(this, programId, enabled);
    }

    public bool ProgramIsEnabled(int programId)
    {
        return _programs.IsEnabled(programId);
    }

    public void UnregisterSystem(int handle)
    {
        if ((uint)handle >= (uint)_systemActive.Length)
        {
            return;
        }

        _systemActive[handle] = false;
        _systems[handle] = null;
    }

    public int AdvanceTicks(int count, in AdvanceOptions options = default)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(count);
        AssertWriterThread();

        var advanced = 0;
        while (advanced < count)
        {
            if (options.Cancel.IsCancellationRequested)
            {
                break;
            }

            var stopContext = new KernelStopContext(CurrentTick, CurrentVersion, advanced);
            if (options.StopAt is not null && options.StopAt(in stopContext))
            {
                break;
            }

            AdvanceOneTick();
            advanced++;
        }

        return advanced;
    }

    private void AdvanceOneTick()
    {
        CurrentTick++;
        World.BeginTick(CurrentTick);
        DrainCommands();
        for (var phase = 0; phase <= _maxPhase; phase++)
        {
            TickPhase(phase);
        }

        WriteTickCompleted();
    }

    public int NextInt(int exclusiveMaximum)
    {
        return _random.NextInt(exclusiveMaximum);
    }

    public int NextInclusive(int minimum, int maximum)
    {
        return _random.NextInclusive(minimum, maximum);
    }

    public float NextFloat()
    {
        return _random.NextFloat();
    }

    private int FindSystemSlot()
    {
        for (var i = 0; i < _systemActive.Length; i++)
        {
            if (!_systemActive[i])
            {
                return i;
            }
        }

        throw new global::System.InvalidOperationException();
    }

    private void DrainCommands()
    {
        while (Commands.TryRead(out var command))
        {
            if (command.IssueVersion != CurrentVersion && !CommandIsVersionTolerant(command.CommandType))
            {
                WriteCommandRejected(command, KernelCommandRejectReasonCodes.StaleVersion);
                continue;
            }

            var result = ApplyCommand(in command);
            if (result.Applied)
            {
                WriteCommandApplied(command);
            }
            else
            {
                WriteCommandRejected(command, result.ReasonCode);
            }
        }
    }

    private KernelCommandApplyResult ApplyCommand(in KernelCommand command)
    {
        if (command.CommandType == KernelCommandTypeCodes.NoOp)
        {
            return KernelCommandApplyResult.Success;
        }

        if (command.CommandType == KernelCommandTypeCodes.SetGeneratorEnabled)
        {
            var payload = command.AsSetGeneratorEnabled();
            if (payload.GeneratorId <= 0 || (uint)payload.Enabled > 1u)
            {
                return new KernelCommandApplyResult(false, KernelCommandRejectReasonCodes.InvalidCommand);
            }

            if (!Generators.TrySetGeneratorEnabledById(payload.GeneratorId, payload.Enabled != 0))
            {
                return new KernelCommandApplyResult(false, KernelCommandRejectReasonCodes.InvalidCommand);
            }

            WriteGeneratorEnabledChanged(payload.GeneratorId, payload.Enabled);
            return KernelCommandApplyResult.Success;
        }

        return new KernelCommandApplyResult(false, KernelCommandRejectReasonCodes.UnsupportedCommand);
    }

    private static bool CommandIsVersionTolerant(int commandType)
    {
        return commandType == KernelCommandTypeCodes.SetGeneratorEnabled;
    }

    private void AssertStartupSeedTick()
    {
        if (CurrentTick != 0)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void WriteKernelEvent(in KernelEvent kernelEvent)
    {
        if (!Events.TryWrite(kernelEvent))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    public void WriteGeneratorEnabledChanged(int generatorId, int enabled)
    {
        _ = Events.TryWrite(KernelEvent.From(new GeneratorEnabledChangedKernelEvent(
            CurrentTick,
            WorldVersion,
            generatorId,
            enabled)));
    }

    private void WriteTickCompleted()
    {
        _ = Events.TryWrite(KernelEvent.From(new TickCompletedKernelEvent(
            CurrentTick,
            WorldVersion)));
    }

    private void WriteCommandApplied(in KernelCommand command)
    {
        _ = Events.TryWrite(KernelEvent.From(new CommandAppliedKernelEvent(
            CurrentTick,
            WorldVersion,
            command.CommandId,
            command.CommandType,
            command.IssueVersion)));
    }

    private void WriteCommandRejected(in KernelCommand command, int reasonCodeId)
    {
        _ = Events.TryWrite(KernelEvent.From(new CommandRejectedKernelEvent(
            CurrentTick,
            WorldVersion,
            command.CommandId,
            command.CommandType,
            command.IssueVersion,
            reasonCodeId)));
    }

    private void AssertWriterThread()
    {
#if DEBUG
        var currentThreadId = global::System.Environment.CurrentManagedThreadId;
        var observedThreadId = global::System.Threading.Interlocked.CompareExchange(ref _writerThreadId, currentThreadId, 0);
        global::System.Diagnostics.Debug.Assert(observedThreadId == 0 || observedThreadId == currentThreadId);
#endif
    }

    private void TickPhase(int phase)
    {
        for (var i = 0; i < _systemCount; i++)
        {
            if (!_systemActive[i] || _systemPhases[i] != phase || CurrentTick % _systemCadences[i] != 0)
            {
                continue;
            }

            var system = _systems[i];
            if (system is null)
            {
                throw new global::System.InvalidOperationException();
            }

            system.Tick(this, CurrentTick);
        }
    }

    private readonly record struct KernelCommandApplyResult(bool Applied, int ReasonCode)
    {
        public static KernelCommandApplyResult Success => new(true, KernelCommandRejectReasonCodes.None);
    }
}
