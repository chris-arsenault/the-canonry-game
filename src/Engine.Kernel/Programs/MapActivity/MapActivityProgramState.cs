namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class MapActivityProgramState : IKernelProgramState
{
    private readonly PrimitiveFloatTable _heat;
    private readonly PrimitiveIntTable _visitCounts;
    private readonly PrimitiveIntTable _lastVisitedTicks;
    private readonly PrimitiveIntTable _currentOccupancy;

    public MapActivityProgramState(int cellCapacity)
    {
        _heat = new PrimitiveFloatTable(cellCapacity);
        _visitCounts = new PrimitiveIntTable(cellCapacity);
        _lastVisitedTicks = new PrimitiveIntTable(cellCapacity);
        _currentOccupancy = new PrimitiveIntTable(cellCapacity);
    }

    public int ProgramId => KernelProgramCodes.MapActivity;

    public int SchemaVersion => 1;

    public float Heat(int planeCode, int q, int r) =>
        _heat.GetOrDefault(planeCode, CoordinateKey(q, r));

    public int VisitCount(int planeCode, int q, int r) =>
        _visitCounts.GetOrDefault(planeCode, CoordinateKey(q, r));

    public int LastVisitedTick(int planeCode, int q, int r) =>
        _lastVisitedTicks.GetOrDefault(planeCode, CoordinateKey(q, r));

    public int CurrentOccupancy(int planeCode, int q, int r) =>
        _currentOccupancy.GetOrDefault(planeCode, CoordinateKey(q, r));

    internal void BeginTick()
    {
        _currentOccupancy.Clear();
    }

    internal void RecordPresence(int planeCode, int q, int r, int tick, float heat)
    {
        var key = CoordinateKey(q, r);
        _heat.Add(planeCode, key, heat);
        _visitCounts.Add(planeCode, key, 1);
        _lastVisitedTicks.Set(planeCode, key, tick);
        _currentOccupancy.Add(planeCode, key, 1);
    }

    private static int CoordinateKey(int q, int r)
    {
        unchecked
        {
            var hash = (uint)q * 2654435761u;
            hash ^= (uint)r * 2246822519u;
            var value = (int)(hash & 2147483647u);
            return value == 0 ? 1 : value;
        }
    }
}

