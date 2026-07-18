namespace IceRemembers.Seed;

using Engine.WorldGen;

internal sealed class IceRemembersMicrogeographyMap
{
    public IceRemembersMicrogeographyMap(PhysicalGeographyField physical)
    {
        ArgumentNullException.ThrowIfNull(physical);
        Physical = physical;
        var triangleCount = physical.Grid.TriangleCount;
        BaseSurfaceIndexes = new int[triangleCount];
        SurfaceIndexes = new int[triangleCount];
        SignificantFeatureFlags = new ulong[triangleCount];
        FireIntensity = new int[triangleCount];
        AuroraIntensity = new int[triangleCount];
        PressureIntensity = new int[triangleCount];
        PressureWake = new bool[triangleCount];
        RogueImpact = new bool[triangleCount];
    }

    public PhysicalGeographyField Physical { get; }

    public int[] BaseSurfaceIndexes { get; }

    public int[] SurfaceIndexes { get; }

    public ulong[] SignificantFeatureFlags { get; }

    public int[] FireIntensity { get; }

    public int[] AuroraIntensity { get; }

    public int[] PressureIntensity { get; }

    public bool[] PressureWake { get; }

    public bool[] RogueImpact { get; }

    public int DeadGodTriangleIndex { get; set; } = -1;

    public IceRemembersMicrogeographyFacts AvailableFacts { get; set; }
}
