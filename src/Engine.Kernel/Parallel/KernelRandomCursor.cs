namespace Engine.Kernel;

internal struct KernelRandomCursor
{
    private const ulong Increment = 0x9E3779B97F4A7C15UL;

    private readonly ulong _key;
    private ulong _counter;

    private KernelRandomCursor(ulong key)
    {
        _key = key;
        _counter = 0UL;
    }

    internal static KernelRandomCursor Create(
        int seed,
        int tick,
        int phase,
        int programId,
        int subjectId,
        int purposeId)
    {
        var key = Mix(unchecked((uint)seed) + Increment);
        key = Mix(key ^ unchecked((uint)tick));
        key = Mix(key ^ (unchecked((uint)phase) * Increment));
        key = Mix(key ^ (unchecked((uint)programId) * 0xD6E8FEB86659FD93UL));
        key = Mix(key ^ (unchecked((uint)subjectId) * 0xA0761D6478BD642FUL));
        key = Mix(key ^ (unchecked((uint)purposeId) * 0xE7037ED1A0B428DBUL));
        return new KernelRandomCursor(key);
    }

    internal int NextInt(int exclusiveMaximum)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMaximum);
        return (int)(NextUInt() % (uint)exclusiveMaximum);
    }

    internal int NextInclusive(int minimum, int maximum)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfLessThan(maximum, minimum);
        return minimum + NextInt(maximum - minimum + 1);
    }

    internal float NextFloat() =>
        (NextUInt() >> 8) * (1f / 16777216f);

    private uint NextUInt()
    {
        var value = Mix(_key + (_counter * Increment));
        _counter++;
        return (uint)(value ^ (value >> 32));
    }

    private static ulong Mix(ulong value)
    {
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
