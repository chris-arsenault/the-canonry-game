namespace Engine.Kernel;

internal readonly struct KernelGrowableStoragePolicy
{
    public KernelGrowableStoragePolicy(
        int modeCode,
        int checkCadenceTicks,
        int thresholdPermille,
        int growthFactor)
    {
        ModeCode = modeCode;
        CheckCadenceTicks = checkCadenceTicks;
        ThresholdPermille = thresholdPermille;
        GrowthFactor = growthFactor;
    }

    internal int ModeCode { get; }

    internal int CheckCadenceTicks { get; }

    internal int ThresholdPermille { get; }

    internal int GrowthFactor { get; }

    internal bool Enabled => ModeCode == KernelGrowableStorageModeCodes.Enabled;

    internal bool ShouldCheck(int tick) =>
        Enabled &&
        CheckCadenceTicks > 0 &&
        tick > 0 &&
        tick % CheckCadenceTicks == 0;

    internal static KernelGrowableStoragePolicy FromOptions(SimulationOptions options)
    {
        var modeCode = options.GrowableStorageModeCode;
        if (modeCode != KernelGrowableStorageModeCodes.Fixed &&
            modeCode != KernelGrowableStorageModeCodes.Enabled)
        {
            modeCode = KernelGrowableStorageModeCodes.Enabled;
        }

        var cadence = options.GrowableStorageCheckCadenceTicks;
        if (cadence <= 0)
        {
            cadence = 100;
        }

        var threshold = options.GrowableStorageThresholdPermille;
        if (threshold <= 0 || threshold > 1000)
        {
            threshold = 900;
        }

        var factor = options.GrowableStorageGrowthFactor;
        if (factor <= 1)
        {
            factor = 2;
        }

        return new KernelGrowableStoragePolicy(modeCode, cadence, threshold, factor);
    }
}

internal static class KernelGrowableStorage
{
    internal static bool ShouldGrowAtThreshold(int highWatermark, int capacity, int thresholdPermille)
    {
        if (capacity <= 0)
        {
            return false;
        }

        return (long)highWatermark * 1000L > (long)capacity * thresholdPermille;
    }

    internal static int GrowthCapacity(int currentCapacity, int minimumCapacity, int growthFactor)
    {
        var factor = growthFactor <= 1 ? 2 : growthFactor;
        var next = currentCapacity <= 0 ? 1 : currentCapacity;
        var minimum = minimumCapacity <= 0 ? 1 : minimumCapacity;
        while (next < minimum || next == currentCapacity)
        {
            var expanded = checked((long)next * factor);
            if (expanded > int.MaxValue - 1)
            {
                throw new global::System.InvalidOperationException();
            }

            next = (int)expanded;
        }

        return next;
    }

    internal static int[] GrowInt32Array(int[] source, int newCapacity)
    {
        var destination = new int[checked(newCapacity + 1)];
        global::System.Array.Copy(source, destination, source.Length);
        return destination;
    }

    internal static float[] GrowFloat32Array(float[] source, int newCapacity)
    {
        var destination = new float[checked(newCapacity + 1)];
        global::System.Array.Copy(source, destination, source.Length);
        return destination;
    }

    internal static bool[] GrowBoolArray(bool[] source, int newCapacity)
    {
        var destination = new bool[checked(newCapacity + 1)];
        global::System.Array.Copy(source, destination, source.Length);
        return destination;
    }
}
