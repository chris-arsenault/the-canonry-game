namespace Engine.Kernel;

internal readonly struct LowAltitudeSignalSnapshot
{
    public int VisitCount { get; init; }
    public int LastVisitedTick { get; init; }
    public float Heat { get; init; }
    public int CurrentOccupancy { get; init; }
    public float CrowdingPressure { get; init; }
    public float AttractorMass { get; init; }
}
