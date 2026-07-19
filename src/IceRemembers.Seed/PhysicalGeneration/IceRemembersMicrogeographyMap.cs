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
        FireHistory = new int[triangleCount];
        AuroraHistory = new int[triangleCount];
        PressureHistory = new int[triangleCount];
        PressureCoastalHistory = new int[triangleCount];
        FireFirstEpoch = NewEpochField(triangleCount);
        AuroraFirstEpoch = NewEpochField(triangleCount);
        PressureFirstEpoch = NewEpochField(triangleCount);
        PressureCoastalFirstEpoch = NewEpochField(triangleCount);
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

    public int[] FireHistory { get; }

    public int[] AuroraHistory { get; }

    public int[] PressureHistory { get; }

    public int[] PressureCoastalHistory { get; }

    public int[] FireFirstEpoch { get; }

    public int[] AuroraFirstEpoch { get; }

    public int[] PressureFirstEpoch { get; }

    public int[] PressureCoastalFirstEpoch { get; }

    public bool[] PressureWake { get; }

    public bool[] RogueImpact { get; }

    public int DeadGodTriangleIndex { get; set; } = -1;

    public int CompletedEpochCount { get; set; }

    public IceRemembersMicrogeographyFacts AvailableFacts { get; set; }

    private static int[] NewEpochField(int triangleCount)
    {
        var field = new int[triangleCount];
        Array.Fill(field, -1);
        return field;
    }
}
