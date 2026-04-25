namespace Engine.Kernel;

using Engine.Eventing;

public sealed class Simulation
{
    private readonly int[] _systemPhases;
    private readonly int[] _systemCadences;
    private readonly ISimulationSystem?[] _systems;
    private readonly bool[] _systemActive;
    private readonly int _maxPhase;
    private int _systemCount;
    private RandomSource _random;

    public Simulation(SimulationOptions? options = null)
    {
        options ??= new SimulationOptions();
        _maxPhase = options.MaxPhase;
        Events = new KernelEventLog(options.EventCapacity, maxCursors: options.EventCursorCapacity);
        World = new WorldState(Events, options);
        Processes = new ProcessRuntime(
            options.ProcessCapacity,
            options.ProcessStepCapacity,
            options.ProcessCooldownCapacity,
            options.MapCellCapacity,
            options.EntityCapacity,
            options.PlannerDemandCapacity,
            options.PlannerProviderSelectorCapacity,
            options.PlannerSupportRuleCapacity,
            options.PlannerCommitmentCapacity);
        Generators = new GeneratorRuntime(options.GeneratorCapacity, options.GeneratorItemCapacity);
        Pressures = new PressureRuntime(options.PressureCapacity, options.PressureValueCapacity);
        Degeneracy = new DegeneracyRuntime(options.DegeneracyDetectorCapacity, options.DegeneracyStateCapacity);
        Lifecycle = new LifecycleRuntime(options.LifecycleRuleCapacity);
        Attributes = new AttributeRuntime(options.AttributeRuleCapacity, options.LifecycleRuleCapacity);
        Relationships = new RelationshipRuntime(options.RelationshipRuleCapacity);
        ProcessSourceCodeId = options.ProcessSourceCodeId;
        CardSourceCodeId = options.CardSourceCodeId;
        _random = new RandomSource(options.Seed);
        _systemPhases = new int[options.SystemCapacity];
        _systemCadences = new int[options.SystemCapacity];
        _systems = new ISimulationSystem[options.SystemCapacity];
        _systemActive = new bool[options.SystemCapacity];
        RegisterSystem(TickPhaseCodes.Low, 1, Attributes);
        RegisterSystem(TickPhaseCodes.Low, 1, Relationships);
        RegisterSystem(TickPhaseCodes.Process, 1, Processes);
        RegisterSystem(TickPhaseCodes.Pressure, 5, Pressures);
        RegisterSystem(TickPhaseCodes.Generator, 1, Generators);
        RegisterSystem(TickPhaseCodes.Event, 10, Degeneracy);
    }

    public WorldState World { get; }
    public KernelEventLog Events { get; }
    public ProcessRuntime Processes { get; }
    public GeneratorRuntime Generators { get; }
    public PressureRuntime Pressures { get; }
    public DegeneracyRuntime Degeneracy { get; }
    public LifecycleRuntime Lifecycle { get; }
    public AttributeRuntime Attributes { get; }
    public RelationshipRuntime Relationships { get; }
    public int ProcessSourceCodeId { get; }
    public int CardSourceCodeId { get; }
    public int CurrentTick { get; private set; }
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
        return Attributes.RegisterAttribute(
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
        var entityId = World.CreateEntity(
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            tick);
        Attributes.InitializeEntity(this, entityId, tick);
        return entityId;
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

    public void UnregisterSystem(int handle)
    {
        if ((uint)handle >= (uint)_systemActive.Length)
        {
            return;
        }

        _systemActive[handle] = false;
        _systems[handle] = null;
    }

    public bool Tick()
    {
        CurrentTick++;
        World.BeginTick(CurrentTick);
        for (var phase = 0; phase <= _maxPhase; phase++)
        {
            TickPhase(phase);
        }

        return true;
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
}
