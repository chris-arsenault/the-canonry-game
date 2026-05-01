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

    public float Heat(int planeCode, int q, int r, int tick, float heatDecayPerTick) =>
        DecayedHeat(planeCode, CoordinateKey(q, r), tick, heatDecayPerTick);

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

    internal void RecordPresence(
        int planeCode,
        int q,
        int r,
        int tick,
        float heat,
        float heatDecayPerTick,
        float heatEmaAlpha)
    {
        var key = CoordinateKey(q, r);
        var decayedHeat = DecayedHeat(planeCode, key, tick, heatDecayPerTick);
        var targetHeat = Clamp(heat, 0f, 1f) * 100f;
        var nextHeat = decayedHeat + ((targetHeat - decayedHeat) * Clamp(heatEmaAlpha, 0f, 1f));
        _heat.Set(planeCode, key, nextHeat);
        _visitCounts.Add(planeCode, key, 1);
        _lastVisitedTicks.Set(planeCode, key, tick);
        _currentOccupancy.Add(planeCode, key, 1);
    }

    private float DecayedHeat(int planeCode, int key, int tick, float heatDecayPerTick)
    {
        var heat = _heat.GetOrDefault(planeCode, key);
        var lastVisitedTick = _lastVisitedTicks.GetOrDefault(planeCode, key);
        var elapsedTicks = tick - lastVisitedTick;
        if (heat <= 0f || elapsedTicks <= 0)
        {
            return heat;
        }

        return heat * (float)global::System.Math.Pow(Clamp(heatDecayPerTick, 0f, 1f), elapsedTicks);
    }

    private static float Clamp(float value, float minimum, float maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
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
