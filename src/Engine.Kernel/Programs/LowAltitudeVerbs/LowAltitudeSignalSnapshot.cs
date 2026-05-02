namespace Engine.Kernel;

internal readonly struct LowAltitudeSignalSnapshot
{
    public int VisitCount { get; init; }
    public int LastVisitedTick { get; init; }
    public float Heat { get; init; }
    public int CurrentOccupancy { get; init; }
    public float CrowdingLoad { get; init; }
    public float AttractorMass { get; init; }
}
