namespace Engine.Kernel;

internal readonly struct LowAltitudeSignalSnapshot
{
    public int CurrentOccupancy { get; init; }
    public float CrowdingLoad { get; init; }
}
