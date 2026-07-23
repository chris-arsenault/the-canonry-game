namespace IceRemembers.Seed;

using Engine.Constants;

/// <summary>
/// The single startup conversion from the kernel map request to the amount of
/// microtriangle geography that must be generated. Nothing below this boundary
/// receives the kernel radius or cell count.
/// </summary>
internal readonly record struct PhysicalGenerationSize(int TriangleCount)
{
    public static PhysicalGenerationSize FromKernelRadius(int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        var cellCount = checked((3 * radius * radius) + (3 * radius) + 1);
        return new(checked(cellCount * PhysicalGeographyCodes.MicrotrianglesPerHex));
    }
}
