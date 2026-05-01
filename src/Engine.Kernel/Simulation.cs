namespace Engine.Kernel;

using Engine.Eventing;
using Engine.Kernel.Programs;

internal readonly record struct CellResourceSeed(int ResourceId, float Value);

internal sealed partial class Simulation : IKernel
{
    private readonly int[] _systemCadences;
    private readonly int[] _systemProgramIds;
    private readonly int[] _systemSourceCodeIds;
    private readonly ISimulationSystem?[] _systems;
    private readonly bool[] _systemActive;
    private readonly KernelProgramRegistry _programs;
    private int _systemCount;
#if DEBUG
    private int _writerThreadId;
#endif
    private RandomSource _random;
    private KernelExecutionContext _executionContext;
    private int _executionContextDepth;

    public Simulation(SimulationOptions? options = null)
    {
        options ??= new SimulationOptions();
        Options = options;
        Events = new KernelEventLog(
            options.EventCapacity,
            maxCursors: options.EventCursorCapacity,
            schemaValidator: options.EventSchemaValidator);
        Commands = new KernelCommandRing(options.CommandCapacity);
        KernelData = new KernelDataSet(
            new KernelEntityData(options.EntityAttributeCapacity, options.AttributeIdCapacity),
            new KernelTerrainData(options.TerrainIdCapacity));
        World = new WorldState(Events, options, KernelData);
        Planners = CreatePlanningRuntime(options);
        LowAltitudeVerbs = CreateLowAltitudeVerbRuntime(options);
        MapActivity = CreateMapActivityRuntime(options);
        MapSignals = CreateMapSignalsRuntime(options);
        Effects = CreateEffectsRuntime(options);
        Gambits = CreateGambitRuntime(options);
        Generators = CreateGeneratorRuntime(options);
        Pressures = CreatePressureRuntime(options);
        Degeneracy = CreateDegeneracyRuntime(options);
        Upkeep = CreateUpkeepRuntime(options);
        Relationships = CreateRelationshipRuntime(options);
        AssignRuntimeCodes(options.RuntimeCodes);
        _executionContext = new KernelExecutionContext(0, StartupSourceCodeId);
        _random = new RandomSource(options.Seed);
        _systemCadences = new int[options.SystemCapacity];
        _systemProgramIds = new int[options.SystemCapacity];
        _systemSourceCodeIds = new int[options.SystemCapacity];
        _systems = new ISimulationSystem[options.SystemCapacity];
        _systemActive = new bool[options.SystemCapacity];
        _programs = new KernelProgramRegistry(options.SystemCapacity);
        RegisterDefaultPrograms();
    }

    private static PlanningProgramRuntime CreatePlanningRuntime(SimulationOptions options)
    {
        var objectiveCapacity = options.GambitObjectiveCapacity;
        return new PlanningProgramRuntime(
            new PlanningProgramData(options.KernelTuning.Programs.Planning.DefaultPlannerKindCode),
            new PlanningProgramState(options.PlanningOverrideCapacity),
            new PlanningProgramScratch(objectiveCapacity, objectiveCapacity + 2),
            options.KernelTuning.Programs);
    }

    private void AssignRuntimeCodes(KernelRuntimeCodes codes)
    {
        GambitSourceCodeId = codes.GambitSourceCodeId;
        UpkeepSourceCodeId = codes.UpkeepSourceCodeId;
        StartupSourceCodeId = codes.StartupSourceCodeId;
        RelationshipSourceCodeId = codes.RelationshipSourceCodeId;
        GeneratorSourceCodeId = codes.GeneratorSourceCodeId;
        PressureSourceCodeId = codes.PressureSourceCodeId;
        DegeneracySourceCodeId = codes.DegeneracySourceCodeId;
        PlanningSourceCodeId = codes.PlanningSourceCodeId;
        MapActivitySourceCodeId = codes.MapActivitySourceCodeId;
        MapSignalsSourceCodeId = codes.MapSignalsSourceCodeId;
        LowAltitudeVerbSourceCodeId = codes.LowAltitudeVerbSourceCodeId;
        EffectsSourceCodeId = codes.EffectsSourceCodeId;
    }

    private static LowAltitudeVerbProgramRuntime CreateLowAltitudeVerbRuntime(SimulationOptions options) =>
        new(
            new LowAltitudeVerbProgramData(options.LowAltitudeVerbRuleCapacity, options.LowAltitudeVerbItemCapacity),
            new LowAltitudeVerbProgramState(options.EntityCapacity),
            options.KernelTuning.Programs);

    private static MapActivityProgramRuntime CreateMapActivityRuntime(SimulationOptions options) =>
        new(
            new MapActivityProgramData(),
            new MapActivityProgramState(options.MapActivityCellCapacity));

    private static MapSignalsProgramRuntime CreateMapSignalsRuntime(SimulationOptions options) =>
        new(
            new MapSignalsProgramData(
                options.TerrainIdCapacity,
                options.KindIdCapacity,
                options.RelationshipKindIdCapacity),
            new MapSignalsProgramState(options.MapSignalCellCapacity, options.EntityCapacity));

    private static EffectsProgramRuntime CreateEffectsRuntime(SimulationOptions options) =>
        new(
            new EffectsProgramData(options.EffectRuleCapacity),
            new EffectsProgramState());

    private static GambitProgramRuntime CreateGambitRuntime(SimulationOptions options) =>
        new(
            new GambitProgramData(
                options.GambitCapacity,
                options.GambitStepCapacity,
                options.GambitObjectiveCapacity,
                options.GambitProviderSelectorCapacity,
                options.GambitSupportRuleCapacity),
            new GambitProgramState(
                options.GambitCapacity,
                options.GambitCooldownCapacity,
                options.EntityCapacity,
                options.GambitCommitmentCapacity),
            new GambitProgramScratch(
                options.MapCellCapacity,
                options.EntityCapacity,
                options.GambitObjectiveCapacity),
            options.KernelTuning.Programs);

    private static GeneratorProgramRuntime CreateGeneratorRuntime(SimulationOptions options)
    {
        var data = new GeneratorProgramData(options.GeneratorCapacity, options.GeneratorItemCapacity);
        return new GeneratorProgramRuntime(
            data,
            new GeneratorProgramState(options.GeneratorCapacity),
            new GeneratorProgramScratch(
                options.GeneratorCapacity,
                options.MapCellCapacity,
                options.GeneratorItemCapacity,
                options.EntityCapacity),
            options.KernelTuning.Programs);
    }

    private static PressureProgramRuntime CreatePressureRuntime(SimulationOptions options) =>
        new(
            new PressureProgramData(options.PressureCapacity),
            new PressureProgramState(options.PressureCapacity, options.PressureValueCapacity),
            options.KernelTuning.Programs);

    private static DegeneracyProgramRuntime CreateDegeneracyRuntime(SimulationOptions options) =>
        new(
            new DegeneracyProgramData(options.DegeneracyDetectorCapacity),
            new DegeneracyProgramState(options.DegeneracyDetectorCapacity, options.DegeneracyStateCapacity),
            options.KernelTuning.Programs);

    private static UpkeepProgramRuntime CreateUpkeepRuntime(SimulationOptions options) =>
        new(
            new UpkeepProgramData(options.LifecycleRuleCapacity),
            new UpkeepProgramState(),
            options.KernelTuning.Programs);

    private static RelationshipProgramRuntime CreateRelationshipRuntime(SimulationOptions options) =>
        new(
            new RelationshipProgramData(options.RelationshipRuleCapacity),
            new RelationshipProgramState(options.RelationshipCapacity),
            options.KernelTuning.Programs);

    private void RegisterDefaultPrograms()
    {
        RegisterProgram(KernelProgramCodes.LowAltitudeVerbs, 1, LowAltitudeVerbs);
        RegisterProgram(KernelProgramCodes.MapActivity, 1, MapActivity);
        RegisterProgram(KernelProgramCodes.MapSignals, 1, MapSignals);
        RegisterProgram(KernelProgramCodes.Relationships, 1, Relationships);
        RegisterProgram(KernelProgramCodes.Effects, 1, Effects);
        RegisterProgram(KernelProgramCodes.Upkeep, 1, Upkeep);
        RegisterProgram(KernelProgramCodes.Gambits, 1, Gambits);
        RegisterProgram(KernelProgramCodes.Pressures, 5, Pressures);
        RegisterProgram(KernelProgramCodes.Generators, 1, Generators);
        RegisterProgram(KernelProgramCodes.Degeneracy, 10, Degeneracy);
        ValidateProgramDependencies();
    }

    // PreviousTick reads are previous-tick dependencies: they intentionally consume materialized state from
    // the prior tick so low-altitude decisions are not order-coupled to same-tick signal production.
    private static readonly ProgramMetadata[] ProgramDependencyMetadata =
    [
        new(
            KernelProgramCodes.LowAltitudeVerbs,
            [
                new ProgramReadDependency(
                    ProgramDependencyResourceCodes.MapActivitySignals,
                    ProgramDependencyLag.PreviousTick),
                new ProgramReadDependency(
                    ProgramDependencyResourceCodes.MapSignals,
                    ProgramDependencyLag.PreviousTick),
            ],
            []),
        new(
            KernelProgramCodes.MapActivity,
            [],
            [
                new ProgramWriteDependency(
                    ProgramDependencyResourceCodes.MapActivitySignals,
                    ProgramDependencyLag.SameTick),
            ]),
        new(
            KernelProgramCodes.MapSignals,
            [
                new ProgramReadDependency(
                    ProgramDependencyResourceCodes.MapActivitySignals,
                    ProgramDependencyLag.SameTick),
            ],
            [
                new ProgramWriteDependency(
                    ProgramDependencyResourceCodes.MapSignals,
                    ProgramDependencyLag.SameTick),
            ]),
        new(
            KernelProgramCodes.Effects,
            [
                new ProgramReadDependency(
                    ProgramDependencyResourceCodes.MapSignals,
                    ProgramDependencyLag.SameTick),
            ],
            []),
    ];

    private void ValidateProgramDependencies()
    {
        for (var metadataIndex = 0; metadataIndex < ProgramDependencyMetadata.Length; metadataIndex++)
        {
            var metadata = ProgramDependencyMetadata[metadataIndex];
            for (var readIndex = 0; readIndex < metadata.Reads.Length; readIndex++)
            {
                ValidateProgramReadDependency(metadata.ProgramId, metadata.Reads[readIndex]);
            }
        }
    }

    private void ValidateProgramReadDependency(int readerProgramId, ProgramReadDependency read)
    {
        if (read.DependencyLag == ProgramDependencyLag.PreviousTick)
        {
            return;
        }

        var readerHandle = _programs.SystemHandleOf(readerProgramId);
        for (var metadataIndex = 0; metadataIndex < ProgramDependencyMetadata.Length; metadataIndex++)
        {
            var writerMetadata = ProgramDependencyMetadata[metadataIndex];
            for (var writeIndex = 0; writeIndex < writerMetadata.Writes.Length; writeIndex++)
            {
                var write = writerMetadata.Writes[writeIndex];
                if (write.ResourceCode != read.ResourceCode)
                {
                    continue;
                }

                ValidateProgramWritePrecedesRead(
                    readerProgramId,
                    readerHandle,
                    writerMetadata.ProgramId,
                    read,
                    write);
            }
        }
    }

    private void ValidateProgramWritePrecedesRead(
        int readerProgramId,
        int readerHandle,
        int writerProgramId,
        ProgramReadDependency read,
        ProgramWriteDependency write)
    {
        var writerHandle = _programs.SystemHandleOf(writerProgramId);
        if (write.DependencyLag == ProgramDependencyLag.SameTick &&
            writerHandle > readerHandle)
        {
            var violation = new ProgramDependencyViolation(
                readerProgramId,
                writerProgramId,
                read.DependencyLag,
                read.ResourceCode);
            _ = violation;
            throw new global::System.InvalidOperationException();
        }
    }

    internal WorldState World { get; }
    internal KernelDataSet KernelData { get; }
    internal KernelEventLog Events { get; }
    internal KernelCommandRing Commands { get; }
    internal PlanningProgramRuntime Planners { get; }
    internal LowAltitudeVerbProgramRuntime LowAltitudeVerbs { get; }
    internal MapActivityProgramRuntime MapActivity { get; }
    internal MapSignalsProgramRuntime MapSignals { get; }
    internal EffectsProgramRuntime Effects { get; }
    internal GambitProgramRuntime Gambits { get; }
    internal GeneratorProgramRuntime Generators { get; }
    internal PressureProgramRuntime Pressures { get; }
    internal DegeneracyProgramRuntime Degeneracy { get; }
    internal UpkeepProgramRuntime Upkeep { get; }
    internal RelationshipProgramRuntime Relationships { get; }
    internal int GambitSourceCodeId { get; private set; }
    internal int UpkeepSourceCodeId { get; private set; }
    internal int StartupSourceCodeId { get; private set; }
    internal int RelationshipSourceCodeId { get; private set; }
    internal int GeneratorSourceCodeId { get; private set; }
    internal int PressureSourceCodeId { get; private set; }
    internal int DegeneracySourceCodeId { get; private set; }
    internal int PlanningSourceCodeId { get; private set; }
    internal int MapActivitySourceCodeId { get; private set; }
    internal int MapSignalsSourceCodeId { get; private set; }
    internal int LowAltitudeVerbSourceCodeId { get; private set; }
    internal int EffectsSourceCodeId { get; private set; }
    internal KernelExecutionContext ExecutionContext => _executionContext;
    public int CurrentTick { get; private set; }
    public long CurrentVersion => WorldVersion;
    public long WorldVersion => World.WorldVersion;
    internal SimulationOptions Options { get; }

    internal LowAltitudeSignalSnapshot ReadLowAltitudeSignalSnapshot(
        int planeCode,
        int q,
        int r,
        int tick,
        int targetEntityId)
    {
        var attractorMass = MapSignals.AttractorMassAt(planeCode, q, r);
        if (targetEntityId > 0)
        {
            attractorMass += MapSignals.EntityAttractorMass(targetEntityId, planeCode);
        }

        return new LowAltitudeSignalSnapshot
        {
            VisitCount = MapActivity.VisitCount(planeCode, q, r),
            LastVisitedTick = MapActivity.LastVisitedTick(planeCode, q, r),
            Heat = MapActivity.Heat(planeCode, q, r, tick),
            CurrentOccupancy = MapActivity.CurrentOccupancy(planeCode, q, r),
            CrowdingPressure = MapSignals.CrowdingPressureAt(planeCode, q, r),
            AttractorMass = attractorMass,
        };
    }

    internal int RegisterAttribute(
        int kindId,
        int attributeId,
        float minimum,
        float maximum,
        float defaultValue,
        float defaultVariance = 0f)
    {
        return KernelData.Entities.RegisterAttribute(
            kindId,
            attributeId,
            minimum,
            maximum,
            defaultValue,
            defaultVariance);
    }

    internal int RegisterTerrain(int terrainId, int boundaryGroupId)
    {
        KernelData.Terrain.RegisterTerrain(terrainId, boundaryGroupId);
        return terrainId;
    }

    internal int CreateEntity(
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

    internal void SeedCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId = 0,
        int continentId = 0)
    {
        AssertStartupSeedTick();
        CreateCell(q, r, terrainId, ownerEntityId, superregionId, continentId);
    }

    internal void SeedCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        global::System.ReadOnlySpan<CellResourceSeed> resources,
        int superregionId = 0,
        int continentId = 0)
    {
        AssertStartupSeedTick();
        CreateCell(q, r, terrainId, ownerEntityId, superregionId, continentId);
        for (var i = 0; i < resources.Length; i++)
        {
            var resource = resources[i];
            SetResource(resource.ResourceId, q, r, resource.Value, CurrentTick);
        }
    }

    internal void CreateCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId = 0,
        int continentId = 0)
    {
        AssertExecutionSource();
        World.MaterializeCell(q, r, terrainId, ownerEntityId, superregionId, continentId);
        WriteKernelEvent(KernelEvent.From(new CellCreatedKernelEvent(
            CurrentTick,
            WorldVersion,
            q,
            r,
            _executionContext.SourceCodeId,
            terrainId,
            ownerEntityId,
            superregionId,
            continentId)));
    }

    internal void SeedEntity(
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
        AssertExecutionSource();
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
            _executionContext.SourceCodeId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS)));
        KernelData.Entities.InitializeEntity(this, createdEntityId, tick);
        return createdEntityId;
    }

    internal void SeedAttribute(
        int entityId,
        int attributeId,
        float value)
    {
        AssertStartupSeedTick();
        SetAttribute(entityId, attributeId, value, CurrentTick);
    }

    internal void SeedResource(
        int resourceId,
        int q,
        int r,
        float value)
    {
        AssertStartupSeedTick();
        SetResource(resourceId, q, r, value, CurrentTick);
    }

    internal void SeedRelationship(
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

    internal void SetAttribute(
        int entityId,
        int attributeId,
        float value,
        int tick)
    {
        World.SetAttribute(entityId, attributeId, value, tick, _executionContext);
    }

    internal void ModifyAttribute(
        int entityId,
        int attributeId,
        float delta,
        int tick)
    {
        World.ModifyAttribute(entityId, attributeId, delta, tick, _executionContext);
    }

    internal void SetResource(
        int resourceId,
        int q,
        int r,
        float value,
        int tick)
    {
        World.SetResource(resourceId, q, r, value, tick, _executionContext);
    }

    internal void ModifyResource(
        int resourceId,
        int q,
        int r,
        float delta,
        int tick)
    {
        World.ModifyResource(resourceId, q, r, delta, tick, _executionContext);
    }

    internal void SetEntityStatus(int entityId, int statusCode, int tick)
    {
        World.SetEntityStatus(entityId, statusCode, tick, _executionContext);
    }

    internal void MoveEntityPhysical(int entityId, int q, int r, int tick)
    {
        World.MoveEntityPhysical(entityId, q, r, tick, _executionContext);
    }

    internal void MoveEntitySemantic(int entityId, int q, int r, int s, int tick)
    {
        World.MoveEntitySemantic(entityId, q, r, s, tick, _executionContext);
    }

    internal void SetCellState(int q, int r, int terrainId, int ownerEntityId, int tick)
    {
        World.SetCellState(q, r, terrainId, ownerEntityId, tick, _executionContext);
    }

    internal void SetCellInfluence(int entityId, int planeCode, int q, int r, float value, int tick)
    {
        World.SetCellInfluence(entityId, planeCode, q, r, value, tick, _executionContext);
    }

    internal void ModifyCellInfluence(int entityId, int planeCode, int q, int r, float delta, int tick)
    {
        World.ModifyCellInfluence(entityId, planeCode, q, r, delta, tick, _executionContext);
    }

    internal void SetCoverageOwner(int q, int r, int ownerEntityId, int tick)
    {
        World.SetCoverageOwner(q, r, ownerEntityId, tick, _executionContext);
    }

    internal void SetCellTerrain(int q, int r, int terrainId, int tick)
    {
        World.SetCellTerrain(q, r, terrainId, tick, _executionContext);
    }

    internal int CreateRelationship(
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
            factCount,
            _executionContext);
    }

    internal void SetRelationshipStrength(int relationshipId, float strength, int tick)
    {
        World.SetRelationshipStrength(relationshipId, strength, tick, _executionContext);
    }

    internal void CullRelationship(int relationshipId, int tick)
    {
        World.CullRelationship(relationshipId, tick, _executionContext);
    }

    internal void CullOutgoingRelationshipsOfKind(int sourceEntityId, int kindId, int tick)
    {
        World.CullOutgoingRelationshipsOfKind(sourceEntityId, kindId, tick, _executionContext);
    }

    internal int RegisterSystem(int cadence, ISimulationSystem system)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cadence);
        global::System.ArgumentNullException.ThrowIfNull(system);

        var slot = FindSystemSlot();
        _systemCadences[slot] = cadence;
        _systemProgramIds[slot] = 0;
        _systemSourceCodeIds[slot] = 0;
        _systems[slot] = system;
        _systemActive[slot] = true;
        if (slot >= _systemCount)
        {
            _systemCount = slot + 1;
        }

        return slot;
    }

    internal int RegisterProgram(int programId, int cadence, ISimulationSystem system)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(programId);
        var sourceCodeId = SourceCodeIdForProgram(programId);
        if (sourceCodeId <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        var handle = RegisterSystem(cadence, system);
        _systemProgramIds[handle] = programId;
        _systemSourceCodeIds[handle] = sourceCodeId;
        _programs.Register(programId, handle);
        return handle;
    }

    internal KernelExecutionScope EnterExecutionContext(
        int sourceCodeId,
        int programId,
        int actionId = 0)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(sourceCodeId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(programId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(actionId);
        var previous = _executionContext;
        var previousDepth = _executionContextDepth;
        _executionContext = new KernelExecutionContext(programId, sourceCodeId, actionId);
        _executionContextDepth = previousDepth + 1;
        return new KernelExecutionScope(this, previous, previousDepth);
    }

    internal KernelExecutionScope EnterActionExecutionContext(int actionId)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actionId);
        var previous = _executionContext;
        var previousDepth = _executionContextDepth;
        _executionContext = previous with { ActionId = actionId };
        _executionContextDepth = previousDepth + 1;
        return new KernelExecutionScope(this, previous, previousDepth);
    }

    internal void RestoreExecutionContext(in KernelExecutionContext context, int depth)
    {
        _executionContext = context;
        _executionContextDepth = depth;
    }

    internal void SetSystemActive(int handle, bool active)
    {
        if ((uint)handle >= (uint)_systemActive.Length || _systems[handle] is null)
        {
            throw new global::System.InvalidOperationException();
        }

        _systemActive[handle] = active;
    }

    internal void SetProgramEnabled(int programId, bool enabled)
    {
        _programs.SetEnabled(this, programId, enabled);
    }

    internal bool ProgramIsEnabled(int programId)
    {
        return _programs.IsEnabled(programId);
    }

    internal void UnregisterSystem(int handle)
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
        TickSystems();

        WriteTickCompleted();
    }

    internal int NextInt(int exclusiveMaximum)
    {
        return _random.NextInt(exclusiveMaximum);
    }

    internal int NextInclusive(int minimum, int maximum)
    {
        return _random.NextInclusive(minimum, maximum);
    }

    internal float NextFloat()
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

    private void AssertExecutionSource()
    {
        if (_executionContext.SourceCodeId <= 0)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void AssertWriterThread()
    {
#if DEBUG
        var currentThreadId = global::System.Environment.CurrentManagedThreadId;
        var observedThreadId = global::System.Threading.Interlocked.CompareExchange(ref _writerThreadId, currentThreadId, 0);
        global::System.Diagnostics.Debug.Assert(observedThreadId == 0 || observedThreadId == currentThreadId);
#endif
    }

    private void TickSystems()
    {
        for (var i = 0; i < _systemCount; i++)
        {
            if (!_systemActive[i] || CurrentTick % _systemCadences[i] != 0)
            {
                continue;
            }

            var system = _systems[i];
            if (system is null)
            {
                throw new global::System.InvalidOperationException();
            }

            if (_systemSourceCodeIds[i] > 0)
            {
                using var scope = EnterExecutionContext(
                    _systemSourceCodeIds[i],
                    _systemProgramIds[i]);
                system.Tick(this, CurrentTick);
            }
            else
            {
                system.Tick(this, CurrentTick);
            }
        }
    }

    private int SourceCodeIdForProgram(int programId)
    {
        var sourceCodeId = SourceCodeIdForCoreProgram(programId);
        return sourceCodeId > 0 ? sourceCodeId : SourceCodeIdForLowAltitudeProgram(programId);
    }

    private int SourceCodeIdForCoreProgram(int programId) => programId switch
    {
        KernelProgramCodes.Gambits => GambitSourceCodeId,
        KernelProgramCodes.Pressures => PressureSourceCodeId,
        KernelProgramCodes.Generators => GeneratorSourceCodeId,
        KernelProgramCodes.Degeneracy => DegeneracySourceCodeId,
        KernelProgramCodes.Planning => PlanningSourceCodeId,
        KernelProgramCodes.MapSignals => MapSignalsSourceCodeId,
        KernelProgramCodes.Effects => EffectsSourceCodeId,
        _ => 0,
    };

    private int SourceCodeIdForLowAltitudeProgram(int programId) => programId switch
    {
        KernelProgramCodes.LowAltitudeVerbs => LowAltitudeVerbSourceCodeId,
        KernelProgramCodes.MapActivity => MapActivitySourceCodeId,
        KernelProgramCodes.Upkeep => UpkeepSourceCodeId,
        KernelProgramCodes.Relationships => RelationshipSourceCodeId,
        _ => 0,
    };

    private readonly record struct KernelCommandApplyResult(bool Applied, int ReasonCode)
    {
        public static KernelCommandApplyResult Success => new(true, KernelCommandRejectReasonCodes.None);
    }
}
