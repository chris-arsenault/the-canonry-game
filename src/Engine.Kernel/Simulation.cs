namespace Engine.Kernel;

using Engine.Eventing;
using Engine.Kernel.Programs;

internal sealed partial class Simulation : IKernel
{
    private readonly int[] _systemCadences;
    private readonly int[] _systemProgramIds;
    private readonly int[] _systemSourceCodeIds;
    private readonly ISimulationSystem?[] _systems;
    private readonly bool[] _systemActive;
    private readonly int[] _dirtyCultureScratch;
    private readonly KernelProgramRegistry _programs;
    private int _systemCount;
#if DEBUG
    private int _writerThreadId;
    private int _writerActive;
#endif
    private RandomSource _random;
    private KernelExecutionContext _executionContext;
    private int _executionContextDepth;

    public Simulation(SimulationOptions? options = null)
    {
        options ??= new SimulationOptions();
        if (options.KernelTuning.Programs.MinorActorMainTickRatio <= 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(options));
        }

        ValidateCultureTuning(options);
        Options = options;
        Events = new KernelEventLog(
            options.EventCapacity,
            maxCursors: options.EventCursorCapacity,
            schemaValidator: options.EventSchemaValidator);
        Commands = new KernelCommandRing(options.CommandCapacity);
        KernelData = new KernelDataSet(
            new KernelEntityData(options.EntityAttributeCapacity, options.AttributeIdCapacity, options.EntityTraitCapacity),
            new KernelTerrainData(options.TerrainIdCapacity),
            new KernelCultureData(options.CultureIdCapacity));
        World = new WorldState(Events, options, KernelData);
        CultureBoundaries = CreateCultureBoundaryRuntime(options);
        World.AttachCultureBoundaries(CultureBoundaries);
        LowAltitudeVerbs = CreateLowAltitudeVerbRuntime(options);
        Movement = CreateMovementRuntime(options);
        MapActivity = CreateMapActivityRuntime(options);
        MapSignals = CreateMapSignalsRuntime(options);
        Effects = CreateEffectsRuntime(options);
        CultureSlots = CreateCultureSlotRuntime(options);
        Gambits = CreateGambitRuntime(options);
        Generators = CreateGeneratorRuntime(options);
        Upkeep = CreateUpkeepRuntime(options);
        Relationships = CreateRelationshipRuntime(options);
        MinorActors = CreateMinorActorRuntime(options);
        CultureFieldDecay = CreateCultureFieldDecayRuntime(options);
        Chronicles = new ChronicleProgramRuntime(options);
        AssignRuntimeCodes(options.RuntimeCodes);
        _executionContext = new KernelExecutionContext(0, StartupSourceCodeId);
        _random = new RandomSource(options.Seed);
        _systemCadences = new int[options.SystemCapacity];
        _systemProgramIds = new int[options.SystemCapacity];
        _systemSourceCodeIds = new int[options.SystemCapacity];
        _systems = new ISimulationSystem[options.SystemCapacity];
        _systemActive = new bool[options.SystemCapacity];
        _dirtyCultureScratch = new int[options.CultureIdCapacity + 1];
        _programs = new KernelProgramRegistry(options.SystemCapacity);
        RegisterDefaultPrograms();
    }

    private void AssignRuntimeCodes(KernelRuntimeCodes codes)
    {
        GambitSourceCodeId = codes.GambitSourceCodeId;
        UpkeepSourceCodeId = codes.UpkeepSourceCodeId;
        StartupSourceCodeId = codes.StartupSourceCodeId;
        RelationshipSourceCodeId = codes.RelationshipSourceCodeId;
        GeneratorSourceCodeId = codes.GeneratorSourceCodeId;
        MapActivitySourceCodeId = codes.MapActivitySourceCodeId;
        MapSignalsSourceCodeId = codes.MapSignalsSourceCodeId;
        LowAltitudeVerbSourceCodeId = codes.LowAltitudeVerbSourceCodeId;
        EffectsSourceCodeId = codes.EffectsSourceCodeId;
        PlayerSourceCodeId = codes.PlayerSourceCodeId;
        ChronicleSourceCodeId = codes.ChronicleSourceCodeId;
    }

    private static CultureBoundaryProgramRuntime CreateCultureBoundaryRuntime(SimulationOptions options) =>
        new(
            new CultureBoundaryProgramData(),
            new CultureBoundaryProgramState(),
            options.MapInfluenceCapacity,
            options.CultureIdCapacity);

    private static LowAltitudeVerbProgramRuntime CreateLowAltitudeVerbRuntime(SimulationOptions options) =>
        new(
            new LowAltitudeVerbProgramData(options.LowAltitudeVerbRuleCapacity, options.LowAltitudeVerbItemCapacity),
            new LowAltitudeVerbProgramState(
                options.EntityCapacity,
                options.KernelTuning.Programs.LowAltitudeVerbs.RecentTargetRingSize),
            options.KernelTuning.Programs);

    private static MovementProgramRuntime CreateMovementRuntime(SimulationOptions options) =>
        new(
            new MovementProgramData(options.MovementProfileRuleCapacity),
            new MovementProgramState(options.MovementCellCapacity),
            new MovementProgramScratch(options.MapCellCapacity),
            options.KernelTuning.Programs);

    private static MapActivityProgramRuntime CreateMapActivityRuntime(SimulationOptions options) =>
        new(
            new MapActivityProgramData(),
            new MapActivityProgramState(options.MapActivityCellCapacity),
            options.KernelTuning.Programs);

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

    private static CultureSlotProgramRuntime CreateCultureSlotRuntime(SimulationOptions options) =>
        new(
            new CultureSlotProgramData(
                options.CultureSlotCapacity,
                options.CultureSlotAllowedKindCapacity,
                options.CultureSlotScoreRuleCapacity),
            new CultureSlotProgramState(options.CultureSlotCapacity),
            options.KernelTuning.Programs);

    private static MinorActorProgramRuntime CreateMinorActorRuntime(SimulationOptions options) =>
        new(
            new MinorActorProgramData(options.MinorActorRuleCapacity),
            new MinorActorProgramState(
                options.MinorActorCapacity,
                options.MinorActorSourceRuleCapacity,
                options.EntityCapacity));

    private static GambitProgramRuntime CreateGambitRuntime(SimulationOptions options) =>
        new(
            new GambitProgramData(
                options.GambitCapacity,
                options.GambitStepCapacity,
                options.GambitProviderSelectorCapacity),
            new GambitProgramState(
                options.GambitCapacity,
                options.GambitCooldownCapacity,
                options.EntityCapacity),
            new GambitProgramScratch(options.EntityCapacity),
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

    private static CultureFieldDecayProgramRuntime CreateCultureFieldDecayRuntime(SimulationOptions options) =>
        new(
            options.KernelTuning.Programs.CultureField,
            options.MapInfluenceCapacity);

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
        var mainCadence = MainTickRatio;
        RegisterProgram(KernelProgramCodes.Movement, mainCadence, Movement);
        RegisterProgram(KernelProgramCodes.LowAltitudeVerbs, mainCadence, LowAltitudeVerbs);
        RegisterProgram(KernelProgramCodes.MapActivity, mainCadence, MapActivity);
        RegisterProgram(KernelProgramCodes.MapSignals, mainCadence, MapSignals);
        RegisterProgram(
            KernelProgramCodes.CultureFieldDecay,
            Options.KernelTuning.Programs.CultureField.DecayCadenceMainTicks * mainCadence,
            CultureFieldDecay);
        RegisterProgram(KernelProgramCodes.CultureBoundaries, mainCadence, CultureBoundaries);
        RegisterProgram(KernelProgramCodes.Relationships, mainCadence, Relationships);
        RegisterProgram(KernelProgramCodes.Effects, mainCadence, Effects);
        RegisterProgram(KernelProgramCodes.Upkeep, mainCadence, Upkeep);
        RegisterProgram(KernelProgramCodes.Gambits, mainCadence, Gambits);
        RegisterProgram(KernelProgramCodes.ChronicleDirector, mainCadence, Chronicles);
        RegisterProgram(KernelProgramCodes.Generators, mainCadence, Generators);
        RegisterProgram(KernelProgramCodes.CultureSlots, 10 * mainCadence, CultureSlots);
        RegisterProgram(
            KernelProgramCodes.MinorActors,
            1,
            MinorActors);
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
                    ProgramDependencyChannelCodes.MovementSurface,
                    ProgramDependencyLag.PreviousTick),
                new ProgramReadDependency(
                    ProgramDependencyChannelCodes.MapActivitySignals,
                    ProgramDependencyLag.PreviousTick),
                new ProgramReadDependency(
                    ProgramDependencyChannelCodes.MapSignals,
                    ProgramDependencyLag.PreviousTick),
            ],
            []),
        new(
            KernelProgramCodes.Movement,
            [],
            [
                new ProgramWriteDependency(
                    ProgramDependencyChannelCodes.MovementSurface,
                    ProgramDependencyLag.SameTick),
            ]),
        new(
            KernelProgramCodes.MapActivity,
            [],
            [
                new ProgramWriteDependency(
                    ProgramDependencyChannelCodes.MapActivitySignals,
                    ProgramDependencyLag.SameTick),
            ]),
        new(
            KernelProgramCodes.MapSignals,
            [
                new ProgramReadDependency(
                    ProgramDependencyChannelCodes.MapActivitySignals,
                    ProgramDependencyLag.SameTick),
            ],
            [
                new ProgramWriteDependency(
                    ProgramDependencyChannelCodes.MapSignals,
                    ProgramDependencyLag.SameTick),
            ]),
        new(
            KernelProgramCodes.Effects,
            [
                new ProgramReadDependency(
                    ProgramDependencyChannelCodes.MapSignals,
                    ProgramDependencyLag.SameTick),
            ],
            []),
        new(
            KernelProgramCodes.Gambits,
            [
                new ProgramReadDependency(
                    ProgramDependencyChannelCodes.MovementSurface,
                    ProgramDependencyLag.PreviousTick),
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
                if (write.ChannelCode != read.ChannelCode)
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
                read.ChannelCode);
            _ = violation;
            throw new global::System.InvalidOperationException();
        }
    }

    internal WorldState World { get; }
    internal KernelDataSet KernelData { get; }
    internal KernelEventLog Events { get; }
    internal KernelCommandRing Commands { get; }
    internal LowAltitudeVerbProgramRuntime LowAltitudeVerbs { get; }
    internal MovementProgramRuntime Movement { get; }
    internal MapActivityProgramRuntime MapActivity { get; }
    internal MapSignalsProgramRuntime MapSignals { get; }
    internal CultureBoundaryProgramRuntime CultureBoundaries { get; }
    internal EffectsProgramRuntime Effects { get; }
    internal CultureSlotProgramRuntime CultureSlots { get; }
    internal GambitProgramRuntime Gambits { get; }
    internal GeneratorProgramRuntime Generators { get; }

    internal ChronicleProgramRuntime Chronicles { get; }
    internal UpkeepProgramRuntime Upkeep { get; }
    internal RelationshipProgramRuntime Relationships { get; }
    internal MinorActorProgramRuntime MinorActors { get; }
    internal CultureFieldDecayProgramRuntime CultureFieldDecay { get; }
    internal int GambitSourceCodeId { get; private set; }
    internal int UpkeepSourceCodeId { get; private set; }
    internal int StartupSourceCodeId { get; private set; }
    internal int RelationshipSourceCodeId { get; private set; }
    internal int GeneratorSourceCodeId { get; private set; }

    internal int ChronicleSourceCodeId { get; private set; }
    internal int MapActivitySourceCodeId { get; private set; }
    internal int MapSignalsSourceCodeId { get; private set; }
    internal int LowAltitudeVerbSourceCodeId { get; private set; }
    internal int EffectsSourceCodeId { get; private set; }
    internal int PlayerSourceCodeId { get; private set; }
    internal KernelExecutionContext ExecutionContext => _executionContext;
    public int CurrentTick { get; private set; }
    internal int MainTickRatio => Options.KernelTuning.Programs.MinorActorMainTickRatio;
    public long CurrentVersion => WorldVersion;
    public long WorldVersion => World.WorldVersion;
    internal SimulationOptions Options { get; }

    internal LowAltitudeSignalSnapshot ReadLowAltitudeSignalSnapshot(
        int planeCode,
        int q,
        int r)
    {
        return new LowAltitudeSignalSnapshot
        {
            CurrentOccupancy = MapActivity.CurrentOccupancy(planeCode, q, r),
            CrowdingLoad = MapSignals.CrowdingLoadAt(planeCode, q, r),
        };
    }

    internal int ReadCellOccupancyOneRingSum(int planeCode, int q, int r)
    {
        var sum = MapActivity.CurrentOccupancy(planeCode, q, r);
        for (var direction = 0; direction < 6; direction++)
        {
            var (dq, dr) = HexDirectionOffset(direction);
            sum += MapActivity.CurrentOccupancy(planeCode, q + dq, r + dr);
        }

        return sum;
    }

    private static (int Dq, int Dr) HexDirectionOffset(int direction) => direction switch
    {
        0 => (1, 0),
        1 => (1, -1),
        2 => (0, -1),
        3 => (-1, 0),
        4 => (-1, 1),
        _ => (0, 1),
    };

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

    internal int RegisterTrait(int kindId, int traitId, float assignmentChance)
    {
        return KernelData.Entities.RegisterTrait(kindId, traitId, assignmentChance);
    }

    internal void RegisterCultureProduction(int cultureId, float physicalMultiplier, float semanticMultiplier)
    {
        KernelData.Cultures.RegisterCultureProduction(cultureId, physicalMultiplier, semanticMultiplier);
    }

    internal int RegisterTerrain(int terrainId, int boundaryGroupId)
    {
        KernelData.Terrain.RegisterTerrain(terrainId, boundaryGroupId);
        return terrainId;
    }

    internal int RegisterTerrain(
        int terrainId,
        int boundaryGroupId,
        float movementCost,
        float influenceMultiplier,
        float claimCostMultiplier,
        float claimChanceMultiplier,
        float roadBuildMultiplier,
        float roadSpeedMultiplier)
    {
        KernelData.Terrain.RegisterTerrain(
            terrainId,
            boundaryGroupId,
            movementCost,
            influenceMultiplier,
            claimCostMultiplier,
            claimChanceMultiplier,
            roadBuildMultiplier,
            roadSpeedMultiplier);
        return terrainId;
    }

    internal void RegisterTerrainMovementProfile(
        int terrainId,
        int movementProfileId,
        float movementCost)
    {
        KernelData.Terrain.RegisterTerrainMovementProfile(terrainId, movementProfileId, movementCost);
    }

    internal void RegisterTerrainKindProfile(
        int terrainId,
        int kindId,
        int fieldFlags,
        float influenceMultiplier,
        float claimCostMultiplier,
        float claimChanceMultiplier)
    {
        KernelData.Terrain.RegisterTerrainKindProfile(
            terrainId,
            kindId,
            fieldFlags,
            influenceMultiplier,
            claimCostMultiplier,
            claimChanceMultiplier);
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
            tick,
            _executionContext);
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

    internal void SeedRelationship(
        int relationshipKindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        float strength)
    {
        AssertStartupSeedTick();
        _ = CreateRelationship(
            relationshipKindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            CurrentTick);
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

    internal void SetEntityStatus(int entityId, int statusCode, int tick)
    {
        World.SetEntityStatus(entityId, statusCode, tick, _executionContext);
    }

    internal void SetEntityCulture(int entityId, int cultureId, int tick)
    {
        World.SetEntityCulture(entityId, cultureId, tick, _executionContext);
    }

    internal void MoveEntityPhysical(int entityId, int q, int r, int tick)
    {
        _ = Movement.Place(this, entityId, PlaneCodes.Physical, q, r, tick, MovementCauseCodes.Forced);
    }

    internal MovementMoveResult MoveEntityPhysical(int entityId, int q, int r, int tick, int causeCode)
    {
        if (causeCode == MovementCauseCodes.Travel)
        {
            return Movement.Navigate(this, entityId, PlaneCodes.Physical, q, r, tick);
        }

        return Movement.Place(this, entityId, PlaneCodes.Physical, q, r, tick, causeCode);
    }

    internal void MoveEntityPhysicalUnchecked(int entityId, int q, int r, int tick)
    {
        World.MoveEntityPhysical(entityId, q, r, tick, _executionContext);
    }

    internal void MoveEntityPhysicalUnchecked(int entityId, int q, int r, int z, int tick)
    {
        World.MoveEntityPhysical(entityId, q, r, z, tick, _executionContext);
    }

    internal void MoveEntitySemantic(int entityId, int q, int r, int s, int tick)
    {
        _ = Movement.Place(this, entityId, PlaneCodes.Semantic, q, r, tick, MovementCauseCodes.Forced);
        _ = s;
    }

    internal MovementMoveResult MoveEntitySemantic(int entityId, int q, int r, int s, int tick, int causeCode)
    {
        _ = s;
        if (causeCode == MovementCauseCodes.Travel)
        {
            return Movement.Navigate(this, entityId, PlaneCodes.Semantic, q, r, tick);
        }

        return Movement.Place(this, entityId, PlaneCodes.Semantic, q, r, tick, causeCode);
    }

    internal void MoveEntitySemanticUnchecked(int entityId, int q, int r, int s, int tick)
    {
        World.MoveEntitySemantic(entityId, q, r, s, tick, _executionContext);
    }

    internal int CreateRelationship(
        int relationshipKindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        float strength,
        int tick)
    {
        var relationshipId = World.CreateRelationship(
            relationshipKindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            tick,
            _executionContext);
        if (relationshipId > 0 && Relationships.KindIsCultureAnchor(relationshipKindId))
        {
            World.SetEntityCultureAnchorToEntity(sourceEntityId, targetEntityId);
        }

        return relationshipId;
    }

    internal void SetRelationshipStrength(int relationshipId, float strength, int tick)
    {
        World.SetRelationshipStrength(relationshipId, strength, tick, _executionContext);
    }

    internal void CullRelationship(int relationshipId, int tick)
    {
        World.CullRelationship(relationshipId, tick, _executionContext);
    }

    internal void CullIncomingRelationshipsOfKind(int targetEntityId, int kindId, int tick)
    {
        World.CullIncomingRelationshipsOfKind(targetEntityId, kindId, tick, _executionContext);
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

    internal int RegisterProgram(
        int programId,
        int cadence,
        ISimulationSystem system)
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

    internal KernelProvenanceScope EnterProvenanceInvocation(
        int producerKindCode,
        int producerId,
        int primaryEntityId,
        int secondaryEntityId = 0,
        int triggerKindCode = ProvenanceTriggerKindCodes.None,
        int triggerId = 0,
        int parentInvocationId = -1)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfLessThan(parentInvocationId, -1);

        var previous = _executionContext;
        var previousDepth = _executionContextDepth;
        var resolvedParentInvocationId = parentInvocationId >= 0
            ? parentInvocationId
            : previous.ProvenanceInvocationId;
        var invocationId = World.BeginProvenanceInvocation(
            CurrentTick,
            producerKindCode,
            producerId,
            primaryEntityId,
            secondaryEntityId,
            triggerKindCode,
            triggerId,
            resolvedParentInvocationId,
            previous);
        _executionContext = previous with { ProvenanceInvocationId = invocationId };
        _executionContextDepth = previousDepth + 1;
        return new KernelProvenanceScope(this, previous, previousDepth, invocationId);
    }

    internal void CompleteProvenanceInvocation(int invocationId)
    {
        World.CompleteProvenanceInvocation(invocationId);
        EmitProvenanceInvocationRecorded(
            CurrentTick,
            invocationId,
            World.ProvenanceInvocationProducerKindCode(invocationId),
            World.ProvenanceInvocationProducerId(invocationId),
            World.ProvenanceInvocationPrimaryEntityId(invocationId),
            World.ProvenanceInvocationSecondaryEntityId(invocationId),
            World.ProvenanceInvocationParentInvocationId(invocationId),
            World.ProvenanceInvocationTriggerKindCode(invocationId),
            World.ProvenanceInvocationTriggerId(invocationId),
            World.ProvenanceInvocationOutputCount(invocationId));
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
        EnterWriterThread();
        try
        {
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
        finally
        {
            ExitWriterThread();
        }
    }

    private void AdvanceOneTick()
    {
        CurrentTick++;
        World.BeginTick(CurrentTick);
        World.GrowStorageIfNeeded(CurrentTick, _executionContext);
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

    private void EnterWriterThread()
    {
#if DEBUG
        var currentThreadId = global::System.Environment.CurrentManagedThreadId;
        var wasActive = global::System.Threading.Interlocked.CompareExchange(ref _writerActive, 1, 0);
        var observedThreadId = global::System.Threading.Volatile.Read(ref _writerThreadId);
        global::System.Diagnostics.Debug.Assert(wasActive == 0 || observedThreadId == currentThreadId);
        global::System.Threading.Volatile.Write(ref _writerThreadId, currentThreadId);
#endif
    }

    private void ExitWriterThread()
    {
#if DEBUG
        global::System.Threading.Volatile.Write(ref _writerThreadId, 0);
        global::System.Threading.Volatile.Write(ref _writerActive, 0);
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

    private static void ValidateCultureTuning(SimulationOptions options)
    {
        if (!IsFiniteNonNegative(options.KernelTuning.Programs.CultureInfluenceActionScale))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(options));
        }

        var cultureField = options.KernelTuning.Programs.CultureField;
        if (!IsFiniteNonNegative(cultureField.DecayPerPulse) ||
            !IsFiniteNonNegative(cultureField.MinimumDecayPerPulse) ||
            !IsFiniteNonNegative(cultureField.CullFloorRawMass) ||
            cultureField.DecayCadenceMainTicks <= 0 ||
            !IsFiniteNonNegative(cultureField.RemittanceShare) ||
            cultureField.RemittanceShare > 1f ||
            cultureField.RemittanceMinimumDistance < 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(options));
        }
    }

    private static bool IsFiniteNonNegative(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;

    private int SourceCodeIdForProgram(int programId)
    {
        var sourceCodeId = SourceCodeIdForCoreProgram(programId);
        if (sourceCodeId > 0)
        {
            return sourceCodeId;
        }

        sourceCodeId = SourceCodeIdForCultureProgram(programId);
        return sourceCodeId > 0 ? sourceCodeId : SourceCodeIdForLowAltitudeProgram(programId);
    }

    private int SourceCodeIdForCoreProgram(int programId) => programId switch
    {
        KernelProgramCodes.Gambits => GambitSourceCodeId,
        KernelProgramCodes.Generators => GeneratorSourceCodeId,
        KernelProgramCodes.ChronicleDirector => ChronicleSourceCodeId,
        KernelProgramCodes.Movement => MapActivitySourceCodeId,
        KernelProgramCodes.Effects => EffectsSourceCodeId,
        KernelProgramCodes.MinorActors => MapActivitySourceCodeId,
        _ => 0,
    };

    private int SourceCodeIdForCultureProgram(int programId) => programId switch
    {
        KernelProgramCodes.MapSignals => MapSignalsSourceCodeId,
        KernelProgramCodes.CultureBoundaries => MapSignalsSourceCodeId,
        KernelProgramCodes.CultureFieldDecay => MapSignalsSourceCodeId,
        KernelProgramCodes.CultureSlots => MapSignalsSourceCodeId,
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

}
