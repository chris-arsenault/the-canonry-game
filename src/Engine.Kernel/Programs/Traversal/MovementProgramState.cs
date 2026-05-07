namespace Engine.Kernel;

internal sealed partial class MovementProgramState : IKernelProgramState
{
    private readonly PrimitiveFloatTable _roadQuality;
    private readonly PrimitiveIntTable _roadTravelCounts;
    private readonly PrimitiveIntTable _lastRoadTicks;

    public MovementProgramState(int cellCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCapacity);
        _roadQuality = new PrimitiveFloatTable(cellCapacity);
        _roadTravelCounts = new PrimitiveIntTable(cellCapacity);
        _lastRoadTicks = new PrimitiveIntTable(cellCapacity);
    }

    public int ProgramId => KernelProgramCodes.Movement;

    public int SchemaVersion => 1;

    internal int RoadCellCount => _roadQuality.Count;

    public float RoadQuality(int planeCode, int q, int r, int tick, float roadDecayPerTick) =>
        DecayedRoadQuality(planeCode, CoordinateKey(q, r), tick, roadDecayPerTick);

    public int RoadTravelCount(int planeCode, int q, int r) =>
        _roadTravelCounts.GetOrDefault(planeCode, CoordinateKey(q, r));

    internal void RecordTravel(
        int planeCode,
        int q,
        int r,
        int tick,
        float roadBuild,
        float roadDecayPerTick)
    {
        var key = CoordinateKey(q, r);
        var decayedQuality = DecayedRoadQuality(planeCode, key, tick, roadDecayPerTick);
        var nextQuality = Clamp(decayedQuality + roadBuild, 0f, 100f);
        _roadQuality.Set(planeCode, key, nextQuality);
        _roadTravelCounts.Add(planeCode, key, 1);
        _lastRoadTicks.Set(planeCode, key, tick);
    }

    internal void ForEachRoadCell(global::System.Action<int, int, float, int, int> visitor)
    {
        global::System.ArgumentNullException.ThrowIfNull(visitor);
        _roadQuality.ForEach((planeCode, coordinateKey, quality) =>
        {
            visitor(
                planeCode,
                coordinateKey,
                quality,
                _lastRoadTicks.GetOrDefault(planeCode, coordinateKey),
                _roadTravelCounts.GetOrDefault(planeCode, coordinateKey));
        });
    }

    private float DecayedRoadQuality(int planeCode, int key, int tick, float roadDecayPerTick)
    {
        var roadQuality = _roadQuality.GetOrDefault(planeCode, key);
        var lastRoadTick = _lastRoadTicks.GetOrDefault(planeCode, key);
        var elapsedTicks = tick - lastRoadTick;
        if (roadQuality <= 0f || elapsedTicks <= 0)
        {
            return roadQuality;
        }

        return roadQuality * (float)global::System.Math.Pow(Clamp(roadDecayPerTick, 0f, 1f), elapsedTicks);
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
