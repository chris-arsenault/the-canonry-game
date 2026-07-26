namespace Engine.Kernel;

using Engine.Eventing;
using Engine.Kernel.Programs;

internal sealed partial class Simulation : IKernel
{
    private readonly int[] _systemCadences;
    private readonly int[] _systemProgramIds;
    private readonly int[] _systemSourceCodeIds;
    private readonly int[] _systemPhaseCodes;
    private readonly ISimulationSystem?[] _systems;
    private readonly bool[] _systemActive;
    private readonly int[] _dirtyCultureScratch;
    private readonly KernelProgramRegistry _programs;
    private int _systemCount;
#if DEBUG
    private int _writerThreadId;
    private int _writerActive;
#endif
    private readonly KeyedRandom _random;
    private readonly FrozenTickView _frozenTick;
    private readonly TickDeltaBuffer _tickDeltas;
    private readonly KernelEventPublisher _eventPublisher;
    private KernelExecutionContext _executionContext;
    private int _executionContextDepth;
    private int _currentPhaseCode;
    private bool _collectingDeltas;
    private long _phaseRevision;

    public Simulation(SimulationOptions? options = null)
    {
        options ??= new SimulationOptions();
        ValidateOptions(options);
        Options = options;
        Events = CreateEventLog(options);
        _eventPublisher = new KernelEventPublisher(Events, options.EventCapacity);
        Commands = new KernelCommandRing(options.CommandCapacity);
        KernelData = CreateKernelData(options);
        World = new WorldState(_eventPublisher, options, KernelData);
        CultureLocality = new CultureLocality(
            options.MapInfluenceCapacity,
            options.MapCellCapacity);
        World.AttachCultureLocality(CultureLocality);
        LowAltitudeVerbs = CreateLowAltitudeVerbRuntime(options);
        Movement = CreateMovementRuntime(options);
        Locality = new LocalityQuery(
            new LocalityData(
                options.TerrainIdCapacity,
                options.KindIdCapacity,
                options.RelationshipKindIdCapacity));
        Effects = CreateEffectsRuntime(options);
        CultureSlots = CreateCultureSlotRuntime(options);
        Gambits = CreateGambitRuntime(options);
        Generators = CreateGeneratorRuntime(options);
        Relationships = CreateRelationshipRuntime(options);
        MinorActors = CreateMinorActorRuntime(options);
        CultureFieldDecay = CreateCultureFieldDecayRuntime(options);
        Chronicles = new ChronicleProgramRuntime(options);
        WorldBeats = new WorldBeatsProgramRuntime(options);
        MapFrontier = new MapFrontierProgramRuntime(options);
        Knowledge = new KnowledgeProgramRuntime(
            options.KnowledgeSiteCapacity,
            options.KnowledgeGrantCapacity,
            options.KnowledgeModifierCapacity,
            options.CultureIdCapacity);
        AssignRuntimeCodes(options.RuntimeCodes);
        _executionContext = new KernelExecutionContext(0, StartupSourceCodeId);
        _random = new KeyedRandom(options.Seed);
        _frozenTick = new FrozenTickView(options.EntityCapacity, options.RelationshipCapacity);
        _tickDeltas = new TickDeltaBuffer(options);
        _systemCadences = new int[options.SystemCapacity];
        _systemProgramIds = new int[options.SystemCapacity];
        _systemSourceCodeIds = new int[options.SystemCapacity];
        _systemPhaseCodes = new int[options.SystemCapacity];
        _systems = new ISimulationSystem[options.SystemCapacity];
        _systemActive = new bool[options.SystemCapacity];
        _dirtyCultureScratch = new int[options.CultureIdCapacity + 1];
        _programs = new KernelProgramRegistry(options.SystemCapacity);
        RegisterDefaultPrograms();
    }

    private static KernelEventLog CreateEventLog(SimulationOptions options) =>
        new(
            options.EventCapacity,
            maxCursors: options.EventCursorCapacity,
            schemaValidator: options.EventSchemaValidator);

    private static KernelDataSet CreateKernelData(SimulationOptions options) =>
        new(
            new KernelEntityData(
                options.EntityAttributeCapacity,
                options.AttributeIdCapacity,
                options.EntityTraitCapacity),
            new KernelTerrainData(options.TerrainIdCapacity),
            new KernelCultureData(options.CultureIdCapacity),
            new KernelPhysicalGeographyData(options.MapGenerationRadiusCapacity),
            new KernelPlacementTasteData(options.KindIdCapacity, options.CultureIdCapacity),
            new KernelGeologicalFeatureData(options.GeologicalFeatureCapacity));

    private static void ValidateOptions(SimulationOptions options)
    {
        if (options.KernelTuning.Programs.MainProgramCadenceTicks <= 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(options));
        }

        ValidateCultureTuning(options);
        ValidateMapCapacities(options);
        ValidateExecutionOptions(options.Execution);
    }

    private void AssignRuntimeCodes(KernelRuntimeCodes codes)
    {
        GambitSourceCodeId = codes.GambitSourceCodeId;
        StartupSourceCodeId = codes.StartupSourceCodeId;
        RelationshipSourceCodeId = codes.RelationshipSourceCodeId;
        GeneratorSourceCodeId = codes.GeneratorSourceCodeId;
        MapSourceCodeId = codes.MapSourceCodeId;
        LowAltitudeVerbSourceCodeId = codes.LowAltitudeVerbSourceCodeId;
        EffectsSourceCodeId = codes.EffectsSourceCodeId;
        PlayerSourceCodeId = codes.PlayerSourceCodeId;
        ChronicleSourceCodeId = codes.ChronicleSourceCodeId;
    }

    private static LowAltitudeVerbProgramRuntime CreateLowAltitudeVerbRuntime(SimulationOptions options) =>
        new(
            new LowAltitudeVerbProgramData(
                options.LowAltitudeVerbRuleCapacity,
                options.LowAltitudeVerbItemCapacity,
                options.KindIdCapacity),
            new LowAltitudeVerbProgramState(
                options.EntityCapacity,
                options.KernelTuning.Programs.LowAltitudeVerbs.RecentTargetRingSize),
            options.KernelTuning.Programs);

    private MovementProgramRuntime CreateMovementRuntime(SimulationOptions options) =>
        new(
            new MovementProgramData(
                options.MovementProfileRuleCapacity,
                kindCapacity: options.KindIdCapacity),
            new MovementProgramState(options.MovementCellCapacity),
            new MovementProgramScratch(options.MapCellCapacity),
            World,
            options.KernelTuning.Programs);

    private static EffectsProgramRuntime CreateEffectsRuntime(SimulationOptions options) =>
        new(
            new EffectsProgramData(
                options.EffectRuleCapacity,
                options.LifecycleRuleCapacity,
                options.KindIdCapacity),
            new EffectsProgramState(),
            options.KernelTuning.Programs);

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
            new MinorActorProgramData(options.MinorActorRuleCapacity, options.KindIdCapacity),
            new MinorActorProgramState(
                options.MinorActorCapacity,
                options.MinorActorRuleCapacity,
                options.EntityCapacity));

    private static GambitProgramRuntime CreateGambitRuntime(SimulationOptions options) =>
        new(
            new GambitProgramData(
                options.GambitCapacity,
                options.GambitStepCapacity,
                options.GambitProviderSelectorCapacity),
            new GambitProgramState(
                options.GambitCapacity,
                options.EntityCapacity),
            new GambitProgramScratch(
                options.EntityCapacity,
                options.GambitCapacity,
                options.GambitStepCapacity),
            options.KernelTuning.Programs);

    private static GeneratorProgramRuntime CreateGeneratorRuntime(SimulationOptions options)
    {
        var data = new GeneratorProgramData(options.GeneratorCapacity, options.GeneratorItemCapacity);
        return new GeneratorProgramRuntime(
            data,
            new GeneratorProgramState(options.GeneratorCapacity, options.GeneratorProvinceCapacity),
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

    private static RelationshipProgramRuntime CreateRelationshipRuntime(SimulationOptions options) =>
        new(
            new RelationshipProgramData(options.RelationshipRuleCapacity, options.RelationshipKindIdCapacity),
            new RelationshipProgramState(options.RelationshipCapacity, options.EntityCapacity),
            options.KernelTuning.Programs);

    private void RegisterDefaultPrograms()
    {
        var mainCadence = Options.KernelTuning.Programs.MainProgramCadenceTicks;
        RegisterProgram(KernelProgramCodes.Movement, mainCadence, Movement);
        RegisterProgram(KernelProgramCodes.LowAltitudeVerbs, mainCadence, LowAltitudeVerbs);
        RegisterProgram(
            KernelProgramCodes.CultureFieldDecay,
            Options.KernelTuning.Programs.CultureField.DecayCadenceTicks,
            CultureFieldDecay);
        RegisterProgram(KernelProgramCodes.Relationships, mainCadence, Relationships);
        RegisterProgram(KernelProgramCodes.Effects, mainCadence, Effects);
        RegisterProgram(KernelProgramCodes.Gambits, mainCadence, Gambits);
        RegisterProgram(KernelProgramCodes.ChronicleDirector, mainCadence, Chronicles);
        RegisterProgram(KernelProgramCodes.Generators, mainCadence, Generators);
        RegisterProgram(
            KernelProgramCodes.WorldBeats,
            Options.KernelTuning.Programs.WorldBeats.CadenceTicks,
            WorldBeats);
        RegisterProgram(
            KernelProgramCodes.MapFrontier,
            Options.KernelTuning.Programs.MapFrontier.CadenceTicks,
            MapFrontier);
        RegisterProgram(KernelProgramCodes.CultureSlots, 10 * mainCadence, CultureSlots);
        // Claims change slowly; recompute knowledge modifiers on the same slow
        // cadence as culture slots rather than every main-program boundary.
        RegisterProgram(KernelProgramCodes.Knowledge, 10 * mainCadence, Knowledge);
        RegisterProgram(KernelProgramCodes.MinorActors, 1, MinorActors);
        ValidateProgramDependencies();
    }

    private static readonly ProgramMetadata[] ProgramDependencyMetadata =
    [
        new(
            KernelProgramCodes.LowAltitudeVerbs,
            [
                new ProgramReadDependency(
                    ProgramDependencyChannelCodes.MovementSurface,
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
    internal LocalityQuery Locality { get; }
    internal CultureLocality CultureLocality { get; }
    internal EffectsProgramRuntime Effects { get; }
    internal CultureSlotProgramRuntime CultureSlots { get; }
    internal GambitProgramRuntime Gambits { get; }
    internal GeneratorProgramRuntime Generators { get; }

    internal ChronicleProgramRuntime Chronicles { get; }
    internal WorldBeatsProgramRuntime WorldBeats { get; }
    internal MapFrontierProgramRuntime MapFrontier { get; }
    internal KnowledgeProgramRuntime Knowledge { get; }
    internal RelationshipProgramRuntime Relationships { get; }
    internal MinorActorProgramRuntime MinorActors { get; }
    internal CultureFieldDecayProgramRuntime CultureFieldDecay { get; }
    internal int GambitSourceCodeId { get; private set; }
    internal int StartupSourceCodeId { get; private set; }
    internal int RelationshipSourceCodeId { get; private set; }
    internal int GeneratorSourceCodeId { get; private set; }

    internal int ChronicleSourceCodeId { get; private set; }
    internal int MapSourceCodeId { get; private set; }
    internal int LowAltitudeVerbSourceCodeId { get; private set; }
    internal int EffectsSourceCodeId { get; private set; }
    internal int PlayerSourceCodeId { get; private set; }
    internal KernelExecutionContext ExecutionContext => _executionContext;
    public int CurrentTick { get; private set; }
    public long CurrentVersion => WorldVersion;
    public long WorldVersion => World.WorldVersion;
    internal SimulationOptions Options { get; }

    internal FixedScalar ReadAttractorMass(int planeCode, int q, int r) =>
        Locality.AttractorMassAt(this, planeCode, q, r);

    internal int ReadCellOccupancyOneRingSum(int planeCode, int q, int r)
    {
        var sum = Locality.OccupancyAt(World, planeCode, q, r);
        for (var direction = 0; direction < 6; direction++)
        {
            var (dq, dr) = HexDirectionOffset(direction);
            sum += Locality.OccupancyAt(World, planeCode, q + dq, r + dr);
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
        FixedScalar minimum,
        FixedScalar maximum,
        FixedScalar defaultValue,
        FixedScalar defaultVariance = default)
    {
        return KernelData.Entities.RegisterAttribute(
            kindId,
            attributeId,
            minimum,
            maximum,
            defaultValue,
            defaultVariance);
    }

    internal int RegisterTrait(int kindId, int traitId, FixedScalar assignmentChance)
    {
        return KernelData.Entities.RegisterTrait(kindId, traitId, assignmentChance);
    }

    internal void RegisterCultureProduction(int cultureId, FixedScalar physicalMultiplier, FixedScalar semanticMultiplier)
    {
        KernelData.Cultures.RegisterCultureProduction(cultureId, physicalMultiplier, semanticMultiplier);
    }

    internal int RegisterKnowledgeSite(int q, int r, int tier) =>
        Knowledge.RegisterSite(q, r, tier);

    internal void RegisterKnowledgeGrant(int site, int domain, int target, FixedScalar multiplier) =>
        Knowledge.RegisterGrant(site, domain, target, multiplier);

    internal int RegisterTerrain(int terrainId, int boundaryGroupId)
    {
        KernelData.Terrain.RegisterTerrain(terrainId, boundaryGroupId);
        return terrainId;
    }

    internal int RegisterTerrain(
        int terrainId,
        int boundaryGroupId,
        FixedScalar movementCost,
        FixedScalar influenceMultiplier,
        FixedScalar claimCostMultiplier,
        FixedScalar claimChanceMultiplier,
        FixedScalar roadBuildMultiplier,
        FixedScalar roadSpeedMultiplier)
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
        FixedScalar movementCost)
    {
        KernelData.Terrain.RegisterTerrainMovementProfile(terrainId, movementProfileId, movementCost);
    }

    internal void RegisterCrossingProfile(
        int crossingClass,
        int movementProfileId,
        FixedScalar factor,
        bool roadNegated)
    {
        KernelData.Terrain.RegisterCrossingProfile(crossingClass, movementProfileId, factor, roadNegated);
    }


    internal void RegisterTerrainKindProfile(
        int terrainId,
        int kindId,
        int fieldFlags,
        FixedScalar influenceMultiplier,
        FixedScalar claimCostMultiplier,
        FixedScalar claimChanceMultiplier)
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
            tick,
            revealOnArrival: true);
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
            CurrentTick,
            revealOnArrival: false);
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
        int tick,
        bool revealOnArrival)
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
        var physicalLocality = CultureLocality.CellAt(
            World,
            cultureId,
            PlaneCodes.Physical,
            physicalQ,
            physicalR);
        var semanticLocality = CultureLocality.CellAt(
            World,
            cultureId,
            PlaneCodes.Semantic,
            semanticQ,
            semanticR);
        WriteKernelEvent(KernelEvent.From(new EntityCreatedKernelEvent(
            tick,
            WorldVersion,
            World.EntityOccurrenceHandle(createdEntityId),
            _executionContext.SourceCodeId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            World.EntityOccurrenceHandle(createdEntityId),
            physicalLocality.StrengthBandCode,
            physicalLocality.CellKindCode,
            semanticLocality.StrengthBandCode,
            semanticLocality.CellKindCode,
            physicalLocality.LocalRawMass.Units,
            physicalLocality.ConnectedRawMass.Units,
            semanticLocality.LocalRawMass.Units,
            semanticLocality.ConnectedRawMass.Units)));
        KernelData.Entities.InitializeEntity(this, createdEntityId, tick);
        if (revealOnArrival)
        {
            // An entity coming into the running world lights the ground around
            // it the same way a traveller's arrival does. Startup cells are
            // installed separately and define the initial live map exactly.
            MapFrontier.RevealOnArrival(this, createdEntityId, tick);
        }

        return createdEntityId;
    }

    internal void SeedAttribute(
        int entityId,
        int attributeId,
        FixedScalar value)
    {
        AssertStartupSeedTick();
        SetAttribute(entityId, attributeId, value, CurrentTick);
    }

    internal void SeedRelationship(
        int relationshipKindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        FixedScalar strength)
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
        FixedScalar value,
        int tick)
    {
        World.SetAttribute(entityId, attributeId, value, tick, _executionContext);
    }

    internal void ModifyAttribute(
        int entityId,
        int attributeId,
        FixedScalar delta,
        int tick)
    {
        if (_collectingDeltas)
        {
            var occurrence = World.EntityOccurrenceHandle(entityId);
            _tickDeltas.AddAttribute(new AttributeDelta(
                occurrence,
                attributeId,
                KernelFixedPoint.AttributeUnits(delta),
                DeltaSourceKey(occurrence, attributeId),
                _executionContext));
            return;
        }

        World.ModifyAttribute(entityId, attributeId, delta, tick, _executionContext);
    }

    internal void ModifyAttributeUnits(
        int entityId,
        int attributeId,
        int deltaUnits,
        int tick)
    {
        if (_collectingDeltas)
        {
            var occurrence = World.EntityOccurrenceHandle(entityId);
            _tickDeltas.AddAttribute(new AttributeDelta(
                occurrence,
                attributeId,
                deltaUnits,
                DeltaSourceKey(occurrence, attributeId),
                _executionContext));
            return;
        }

        World.ModifyAttributeUnits(entityId, attributeId, deltaUnits, tick, _executionContext);
    }

    internal void SetEntityStatus(int entityId, int statusCode, int tick)
    {
        if (statusCode == EntityStatusCodes.Historical)
        {
            if (_collectingDeltas)
            {
                QueueLifecycleRetirement(entityId);
                return;
            }

            RetireEntity(entityId, tick);
            return;
        }

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


    internal void MoveEntitySemanticUnchecked(int entityId, int q, int r, int s, int tick)
    {
        World.MoveEntitySemantic(entityId, q, r, s, tick, _executionContext);
    }

    internal int CreateRelationship(
        int relationshipKindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        FixedScalar strength,
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
        if (relationshipId > 0)
        {
            Relationships.NoteRelationshipCreated(World, relationshipId);
        }

        if (relationshipId > 0 && Relationships.KindIsCultureAnchor(relationshipKindId))
        {
            World.SetEntityCultureAnchorToEntity(sourceEntityId, targetEntityId);
        }

        if (relationshipId > 0)
        {
            WorldBeats.NoteRelationshipCreated(relationshipKindId);
        }

        return relationshipId;
    }

    internal void SetRelationshipStrength(int relationshipId, FixedScalar strength, int tick)
    {
        World.SetRelationshipStrength(relationshipId, strength, tick, _executionContext);
    }

    internal void ModifyRelationshipStrengthUnits(int relationshipId, int deltaUnits, int tick)
    {
        if (_collectingDeltas)
        {
            var occurrence = World.RelationshipOccurrenceHandle(relationshipId);
            _tickDeltas.AddRelationship(new RelationshipDelta(
                occurrence,
                deltaUnits,
                DeltaSourceKey(occurrence, 0),
                _executionContext));
            return;
        }

        World.ModifyRelationshipStrengthUnits(relationshipId, deltaUnits, tick, _executionContext);
    }

    internal void CullRelationship(int relationshipId, int tick)
    {
        if (!World.RelationshipIsActive(relationshipId))
        {
            return;
        }

        if (_collectingDeltas)
        {
            var occurrence = World.RelationshipOccurrenceHandle(relationshipId);
            _tickDeltas.AddRelationshipEnd(new RelationshipEndDelta(
                occurrence,
                0,
                Unconditional: true,
                DeltaSourceKey(occurrence, 0),
                _executionContext));
            return;
        }

        var relationshipKindId = World.RelationshipKindId(relationshipId);
        Relationships.NoteRelationshipCulled(World, relationshipId);
        World.CullRelationship(relationshipId, tick, _executionContext);
        WorldBeats.NoteRelationshipEnded(relationshipKindId);
    }

    internal void CullIncomingRelationshipsOfKind(int targetEntityId, int kindId, int tick)
    {
        while (true)
        {
            var cursor = World.IncomingRelationshipsOfKind(targetEntityId, kindId);
            if (!World.MoveNextRelationship(ref cursor, out var relationshipId))
            {
                return;
            }

            CullRelationship(relationshipId, tick);
        }
    }

    internal void CullOutgoingRelationshipsOfKind(int sourceEntityId, int kindId, int tick)
    {
        while (true)
        {
            var cursor = World.OutgoingRelationshipsOfKind(sourceEntityId, kindId);
            if (!World.MoveNextRelationship(ref cursor, out var relationshipId))
            {
                return;
            }

            CullRelationship(relationshipId, tick);
        }
    }

    public int AdvanceTicks(int count, in AdvanceOptions options = default)
    {
        NumericGuard.ThrowIfNegative(count);
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

    private static void ValidateCultureTuning(SimulationOptions options)
    {
        if (!IsFiniteNonNegative(options.KernelTuning.Programs.CultureInfluenceActionScale))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(options));
        }

        var cultureField = options.KernelTuning.Programs.CultureField;
        if (!IsFiniteNonNegative(cultureField.DecayPerRun) ||
            !IsFiniteNonNegative(cultureField.MinimumDecayPerRun) ||
            !IsFiniteNonNegative(cultureField.CullFloorRawMass) ||
            cultureField.DecayCadenceTicks <= 0 ||
            !IsFiniteNonNegative(cultureField.RemittanceShare) ||
            cultureField.RemittanceShare > 1 ||
            cultureField.RemittanceMinimumDistance < 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(options));
        }
    }

    private static bool IsFiniteNonNegative(FixedScalar value) =>
        !FixedScalar.IsNaN(value) && !FixedScalar.IsInfinity(value) && value >= 0;

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
        // WorldBeats writes no world state; its events attribute to the
        // chronicle source (the macro-chronicle is the director's sibling).
        KernelProgramCodes.WorldBeats => ChronicleSourceCodeId,
        KernelProgramCodes.MapFrontier => MapSourceCodeId,
        KernelProgramCodes.Movement => MapSourceCodeId,
        KernelProgramCodes.Effects => EffectsSourceCodeId,
        KernelProgramCodes.MinorActors => MapSourceCodeId,
        _ => 0,
    };

    private int SourceCodeIdForCultureProgram(int programId) => programId switch
    {
        KernelProgramCodes.CultureFieldDecay => MapSourceCodeId,
        KernelProgramCodes.CultureSlots => MapSourceCodeId,
        // The knowledge program writes only derived scalar modifiers (no world
        // events); it borrows the culture source for a valid, unused attribution.
        KernelProgramCodes.Knowledge => MapSourceCodeId,
        _ => 0,
    };

    private int SourceCodeIdForLowAltitudeProgram(int programId) => programId switch
    {
        KernelProgramCodes.LowAltitudeVerbs => LowAltitudeVerbSourceCodeId,
        KernelProgramCodes.Relationships => RelationshipSourceCodeId,
        _ => 0,
    };

}
