namespace Engine.Kernel;

public sealed class RandomSource
{
    private uint _state;

    public RandomSource(int seed)
    {
        _state = (uint)seed;
        if (_state == 0u)
        {
            _state = 2463534242u;
        }
    }

    public int NextInt(int exclusiveMaximum)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMaximum);
        return (int)(NextUInt() % (uint)exclusiveMaximum);
    }

    public int NextInclusive(int minimum, int maximum)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfLessThan(maximum, minimum);
        return minimum + NextInt(maximum - minimum + 1);
    }

    public float NextFloat()
    {
        return (NextUInt() >> 8) * (1f / 16777216f);
    }

    private uint NextUInt()
    {
        var value = _state;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        _state = value;
        return value;
    }
}
