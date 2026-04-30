namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class MapSignalsProgramState : IKernelProgramState
{
    private const int ValueKey = 1;
    private readonly PrimitiveFloatTable _capacityAdditions;
    private readonly PrimitiveFloatTable _support;
    private readonly PrimitiveFloatTable _threat;
    private readonly PrimitiveFloatTable _crowdingPressure;
    private readonly PrimitiveFloatTable _crowdingComfort;
    private readonly PrimitiveFloatTable _attractorMass;
    private readonly PrimitiveFloatTable _entityCapacity;
    private readonly PrimitiveFloatTable _entitySupport;
    private readonly PrimitiveFloatTable _entityThreat;
    private readonly PrimitiveFloatTable _entityCrowdingPressure;
    private readonly PrimitiveFloatTable _entityCrowdingComfort;
    private readonly PrimitiveFloatTable _entityAttractorMass;

    public MapSignalsProgramState(int cellCapacity, int entityCapacity)
    {
        _capacityAdditions = new PrimitiveFloatTable(cellCapacity);
        _support = new PrimitiveFloatTable(cellCapacity);
        _threat = new PrimitiveFloatTable(cellCapacity);
        _crowdingPressure = new PrimitiveFloatTable(cellCapacity);
        _crowdingComfort = new PrimitiveFloatTable(cellCapacity);
        _attractorMass = new PrimitiveFloatTable(cellCapacity);
        _entityCapacity = new PrimitiveFloatTable(entityCapacity);
        _entitySupport = new PrimitiveFloatTable(entityCapacity);
        _entityThreat = new PrimitiveFloatTable(entityCapacity);
        _entityCrowdingPressure = new PrimitiveFloatTable(entityCapacity);
        _entityCrowdingComfort = new PrimitiveFloatTable(entityCapacity);
        _entityAttractorMass = new PrimitiveFloatTable(entityCapacity);
    }

    public int ProgramId => KernelProgramCodes.MapSignals;

    public int SchemaVersion => 1;

    public void BeginTick()
    {
        _capacityAdditions.Clear();
        _support.Clear();
        _threat.Clear();
        _crowdingPressure.Clear();
        _crowdingComfort.Clear();
        _attractorMass.Clear();
        _entityCapacity.Clear();
        _entitySupport.Clear();
        _entityThreat.Clear();
        _entityCrowdingPressure.Clear();
        _entityCrowdingComfort.Clear();
        _entityAttractorMass.Clear();
    }

    public void AddCapacity(int planeCode, int q, int r, float value) =>
        Add(_capacityAdditions, planeCode, q, r, value);

    public void AddSupport(int planeCode, int q, int r, float value) =>
        Add(_support, planeCode, q, r, value);

    public void AddThreat(int planeCode, int q, int r, float value) =>
        Add(_threat, planeCode, q, r, value);

    public void AddAttractorMass(int planeCode, int q, int r, float value) =>
        Add(_attractorMass, planeCode, q, r, value);

    public void SetCrowdingPressure(int planeCode, int q, int r, float value) =>
        Set(_crowdingPressure, planeCode, q, r, value);

    public void SetCrowdingComfort(int planeCode, int q, int r, float value) =>
        Set(_crowdingComfort, planeCode, q, r, value);

    public void SetEntityCapacity(int entityId, int planeCode, float value) =>
        _entityCapacity.Set(entityId, planeCode, value);

    public void SetEntitySupport(int entityId, int planeCode, float value) =>
        _entitySupport.Set(entityId, planeCode, value);

    public void SetEntityThreat(int entityId, int planeCode, float value) =>
        _entityThreat.Set(entityId, planeCode, value);

    public void SetEntityCrowdingPressure(int entityId, int planeCode, float value) =>
        _entityCrowdingPressure.Set(entityId, planeCode, value);

    public void SetEntityCrowdingComfort(int entityId, int planeCode, float value) =>
        _entityCrowdingComfort.Set(entityId, planeCode, value);

    public void SetEntityAttractorMass(int entityId, int planeCode, float value) =>
        _entityAttractorMass.Set(entityId, planeCode, value);

    public float CapacityAdditionAt(int planeCode, int q, int r) =>
        Get(_capacityAdditions, planeCode, q, r);

    public float SupportAt(int planeCode, int q, int r) =>
        Get(_support, planeCode, q, r);

    public float ThreatAt(int planeCode, int q, int r) =>
        Get(_threat, planeCode, q, r);

    public float CrowdingPressureAt(int planeCode, int q, int r) =>
        Get(_crowdingPressure, planeCode, q, r);

    public float CrowdingComfortAt(int planeCode, int q, int r) =>
        Get(_crowdingComfort, planeCode, q, r);

    public float AttractorMassAt(int planeCode, int q, int r) =>
        Get(_attractorMass, planeCode, q, r);

    public float EntityCapacity(int entityId, int planeCode) =>
        _entityCapacity.GetOrDefault(entityId, planeCode);

    public float EntitySupport(int entityId, int planeCode) =>
        _entitySupport.GetOrDefault(entityId, planeCode);

    public float EntityThreat(int entityId, int planeCode) =>
        _entityThreat.GetOrDefault(entityId, planeCode);

    public float EntityCrowdingPressure(int entityId, int planeCode) =>
        _entityCrowdingPressure.GetOrDefault(entityId, planeCode);

    public float EntityCrowdingComfort(int entityId, int planeCode) =>
        _entityCrowdingComfort.GetOrDefault(entityId, planeCode);

    public float EntityAttractorMass(int entityId, int planeCode) =>
        _entityAttractorMass.GetOrDefault(entityId, planeCode);

    private static void Add(PrimitiveFloatTable table, int planeCode, int q, int r, float value)
    {
        if (value != 0f)
        {
            table.Add(planeCode, CoordinateKey(q, r), value);
        }
    }

    private static void Set(PrimitiveFloatTable table, int planeCode, int q, int r, float value) =>
        table.Set(planeCode, CoordinateKey(q, r), value);

    private static float Get(PrimitiveFloatTable table, int planeCode, int q, int r) =>
        table.GetOrDefault(planeCode, CoordinateKey(q, r));

    private static int CoordinateKey(int q, int r)
    {
        unchecked
        {
            var hash = (uint)q * 2654435761u;
            hash ^= (uint)r * 2246822519u;
            var value = (int)(hash & 2147483647u);
            return value == 0 ? ValueKey : value;
        }
    }
}
