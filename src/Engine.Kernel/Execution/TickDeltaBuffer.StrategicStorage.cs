namespace Engine.Kernel;

internal sealed partial class TickDeltaBuffer
{
    private DeltaAppendArena<AttributeSetIntent> _attributeSetArena = null!;
    private DeltaAppendArena<EntityStateIntent> _entityStateArena = null!;
    private DeltaAppendArena<EntityCreationIntent> _entityCreationArena = null!;
    private DeltaAppendArena<RelationshipCreationIntent> _relationshipCreationArena = null!;
    private DeltaAppendArena<RelationshipSetIntent> _relationshipSetArena = null!;
    private DeltaAppendArena<StrategicCellStateIntent>
        _strategicCellStateArena = null!;
    private DeltaAppendArena<TravelStructureIntent> _travelStructureArena = null!;
    private DeltaAppendArena<ForgeCellIntent> _forgeCellArena = null!;
    private DeltaAppendArena<DistrictIntent> _districtArena = null!;
    private AttributeSetIntent[] _attributeSets = null!;
    private EntityStateIntent[] _entityStates = null!;
    private EntityCreationIntent[] _entityCreations = null!;
    private int[] _entityCreationPhysicalCellIds = null!;
    private int[] _entityCreationSemanticCellIds = null!;
    private RelationshipCreationIntent[] _relationshipCreations = null!;
    private RelationshipCreationIntent[]
        _resolvedRelationshipCreations = null!;
    private RelationshipSetIntent[] _relationshipSets = null!;
    private StrategicCellStateIntent[] _strategicCellStates = null!;
    private int[] _strategicCellIds = null!;
    private int[] _strategicCellPreviousOwners = null!;
    private int[] _strategicCellAppliedOwners = null!;
    private bool[] _strategicCellChanged = null!;
    private bool[] _strategicCellTerrainChanged = null!;
    private TravelStructureIntent[] _travelStructures = null!;
    private ResolvedTravelStructure[] _resolvedTravelStructures = null!;
    private int[] _resolvedTravelStructurePreviousIds = null!;
    private bool[] _resolvedTravelStructureChanged = null!;
    private ForgeCellIntent[] _forgeCells = null!;
    private DistrictIntent[] _districts = null!;
    private int _attributeSetCount;
    private int _entityStateCount;
    private int _entityCreationCount;
    private int _relationshipCreationCount;
    private int _resolvedRelationshipCreationCount;
    private int _relationshipSetCount;
    private int _strategicCellStateCount;
    private int _travelStructureCount;
    private int _resolvedTravelStructureCount;
    private int _forgeCellCount;
    private int _districtCount;

    private int StrategicCount =>
        _attributeSetArena.Count +
        _entityStateArena.Count +
        _entityCreationArena.Count +
        _relationshipCreationArena.Count +
        _relationshipSetArena.Count +
        _strategicCellStateArena.Count +
        _travelStructureArena.Count +
        _forgeCellArena.Count +
        _districtArena.Count;

    private void InitializeStrategicStorage(SimulationOptions options)
    {
        var workerCount = options.Execution.WorkerCount;
        _attributeSetArena = new DeltaAppendArena<AttributeSetIntent>(
            options.AttributeCapacity,
            workerCount);
        _entityStateArena = new DeltaAppendArena<EntityStateIntent>(
            checked(options.EntityCapacity * 5),
            workerCount);
        _entityCreationArena = new DeltaAppendArena<EntityCreationIntent>(
            options.EntityCapacity,
            workerCount);
        _relationshipCreationArena =
            new DeltaAppendArena<RelationshipCreationIntent>(
                options.RelationshipCapacity,
                workerCount);
        _relationshipSetArena = new DeltaAppendArena<RelationshipSetIntent>(
            options.RelationshipCapacity,
            workerCount);
        var strategicCellCapacity = checked(
            (options.MapCellCapacity * 2) +
            options.EntityCapacity);
        _strategicCellStateArena =
            new DeltaAppendArena<StrategicCellStateIntent>(
            strategicCellCapacity,
            workerCount);
        _travelStructureArena =
            new DeltaAppendArena<TravelStructureIntent>(
                options.MapCellCapacity,
                workerCount);
        _forgeCellArena = new DeltaAppendArena<ForgeCellIntent>(
            options.EntityCapacity,
            workerCount);
        _districtArena = new DeltaAppendArena<DistrictIntent>(
            options.EntityCapacity,
            workerCount);
        InitializeStrategicResultStorage(
            options,
            strategicCellCapacity);
    }

    private void InitializeStrategicResultStorage(
        SimulationOptions options,
        int strategicCellCapacity)
    {
        _attributeSets = _attributeSetArena.Values;
        _entityStates = _entityStateArena.Values;
        _entityCreations = _entityCreationArena.Values;
        _entityCreationPhysicalCellIds =
            new int[options.EntityCapacity];
        _entityCreationSemanticCellIds =
            new int[options.EntityCapacity];
        _relationshipCreations = _relationshipCreationArena.Values;
        _resolvedRelationshipCreations =
            new RelationshipCreationIntent[options.RelationshipCapacity];
        _relationshipSets = _relationshipSetArena.Values;
        _strategicCellStates = _strategicCellStateArena.Values;
        _strategicCellIds = new int[strategicCellCapacity];
        _strategicCellPreviousOwners = new int[strategicCellCapacity];
        _strategicCellAppliedOwners = new int[strategicCellCapacity];
        _strategicCellChanged = new bool[strategicCellCapacity];
        _strategicCellTerrainChanged = new bool[strategicCellCapacity];
        _travelStructures = _travelStructureArena.Values;
        _resolvedTravelStructures =
            new ResolvedTravelStructure[options.MapCellCapacity];
        _resolvedTravelStructurePreviousIds =
            new int[options.MapCellCapacity];
        _resolvedTravelStructureChanged =
            new bool[options.MapCellCapacity];
        _forgeCells = _forgeCellArena.Values;
        _districts = _districtArena.Values;
    }

    private void ClearStrategicStorage()
    {
        _attributeSetArena.Clear();
        _entityStateArena.Clear();
        _entityCreationArena.Clear();
        _relationshipCreationArena.Clear();
        _relationshipSetArena.Clear();
        _strategicCellStateArena.Clear();
        _travelStructureArena.Clear();
        _forgeCellArena.Clear();
        _districtArena.Clear();
        _attributeSetCount = 0;
        _entityStateCount = 0;
        _entityCreationCount = 0;
        _relationshipCreationCount = 0;
        _resolvedRelationshipCreationCount = 0;
        _relationshipSetCount = 0;
        _strategicCellStateCount = 0;
        _travelStructureCount = 0;
        _resolvedTravelStructureCount = 0;
        _forgeCellCount = 0;
        _districtCount = 0;
    }

    internal void AddAttributeSet(in AttributeSetIntent intent) =>
        _attributeSetArena.Add(AppendWorkerIndex, intent);
    internal void AddEntityState(in EntityStateIntent intent) =>
        _entityStateArena.Add(AppendWorkerIndex, intent);
    internal void AddEntityCreation(in EntityCreationIntent intent) =>
        _entityCreationArena.Add(AppendWorkerIndex, intent);
    internal void AddRelationshipCreation(
        in RelationshipCreationIntent intent) =>
        _relationshipCreationArena.Add(AppendWorkerIndex, intent);
    internal void AddRelationshipSet(in RelationshipSetIntent intent) =>
        _relationshipSetArena.Add(AppendWorkerIndex, intent);
    internal void AddStrategicCellState(
        in StrategicCellStateIntent intent) =>
        _strategicCellStateArena.Add(AppendWorkerIndex, intent);
    internal void AddTravelStructure(in TravelStructureIntent intent) =>
        _travelStructureArena.Add(AppendWorkerIndex, intent);
    internal void AddForgeCell(in ForgeCellIntent intent) =>
        _forgeCellArena.Add(AppendWorkerIndex, intent);
    internal void AddDistrict(in DistrictIntent intent) =>
        _districtArena.Add(AppendWorkerIndex, intent);

    private void CompactStrategicStorage()
    {
        _attributeSetCount = _attributeSetArena.Compact();
        _entityStateCount = _entityStateArena.Compact();
        _entityCreationCount = _entityCreationArena.Compact();
        _relationshipCreationCount =
            _relationshipCreationArena.Compact();
        _relationshipSetCount = _relationshipSetArena.Compact();
        _strategicCellStateCount = _strategicCellStateArena.Compact();
        _travelStructureCount = _travelStructureArena.Compact();
        _forgeCellCount = _forgeCellArena.Compact();
        _districtCount = _districtArena.Compact();
    }

    private void PrepareStrategicStorage(bool reverseInsertionOrder)
    {
        Prepare(_attributeSets, _attributeSetCount, reverseInsertionOrder);
        Prepare(_entityStates, _entityStateCount, reverseInsertionOrder);
        Prepare(
            _entityCreations,
            _entityCreationCount,
            reverseInsertionOrder);
        Prepare(
            _relationshipCreations,
            _relationshipCreationCount,
            reverseInsertionOrder);
        Prepare(
            _relationshipSets,
            _relationshipSetCount,
            reverseInsertionOrder);
        Prepare(
            _strategicCellStates,
            _strategicCellStateCount,
            reverseInsertionOrder);
        Prepare(
            _travelStructures,
            _travelStructureCount,
            reverseInsertionOrder);
        Prepare(_forgeCells, _forgeCellCount, reverseInsertionOrder);
        Prepare(_districts, _districtCount, reverseInsertionOrder);
    }
}
