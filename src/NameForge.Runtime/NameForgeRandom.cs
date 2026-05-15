namespace NameForge.Runtime;

internal sealed class NameForgeRandom
{
    private ulong _state;

    public NameForgeRandom(string seed)
    {
        _state = HashSeed(seed);
        if (_state == 0)
        {
            _state = 0x9E3779B97F4A7C15ul;
        }
    }

    public double NextDouble()
    {
        var value = NextUInt64();
        return (value >> 11) * (1.0 / (1ul << 53));
    }

    public int NextInt(int minInclusive, int maxInclusive)
    {
        if (maxInclusive <= minInclusive)
        {
            return minInclusive;
        }

        var span = maxInclusive - minInclusive + 1;
        return minInclusive + (int)Math.Floor(NextDouble() * span);
    }

    public bool Chance(double probability) =>
        probability > 0d && NextDouble() < probability;

    public T Pick<T>(IReadOnlyList<T> values, T fallback)
    {
        if (values.Count == 0)
        {
            return fallback;
        }

        return values[Math.Min((int)Math.Floor(NextDouble() * values.Count), values.Count - 1)];
    }

    public T PickWeighted<T>(IReadOnlyList<T> values, IReadOnlyList<double> weights, T fallback)
    {
        if (values.Count == 0)
        {
            return fallback;
        }

        if (weights.Count != values.Count)
        {
            return Pick(values, fallback);
        }

        var total = 0d;
        for (var i = 0; i < weights.Count; i++)
        {
            total += Math.Max(0d, weights[i]);
        }

        if (total <= 0d)
        {
            return Pick(values, fallback);
        }

        var roll = NextDouble() * total;
        for (var i = 0; i < values.Count; i++)
        {
            roll -= Math.Max(0d, weights[i]);
            if (roll <= 0d)
            {
                return values[i];
            }
        }

        return values[^1];
    }

    private ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15ul;
        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9ul;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBul;
        return z ^ (z >> 31);
    }

    private static ulong HashSeed(string seed)
    {
        const ulong offset = 14695981039346656037ul;
        const ulong prime = 1099511628211ul;

        var hash = offset;
        foreach (var ch in seed)
        {
            hash ^= ch;
            hash *= prime;
        }

        return hash;
    }
}
