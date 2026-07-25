namespace Engine.Kernel;

internal sealed partial class RandomSource
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
        NumericGuard.ThrowIfNegativeOrZero(exclusiveMaximum);
        return (int)(NextUInt() % (uint)exclusiveMaximum);
    }

    public int NextInclusive(int minimum, int maximum)
    {
        NumericGuard.ThrowIfLessThan(maximum, minimum);
        return minimum + NextInt(maximum - minimum + 1);
    }

    public FixedScalar NextScalar()
    {
        return FixedScalar.FromRatio(NextUInt() >> 8, 16_777_216);
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
