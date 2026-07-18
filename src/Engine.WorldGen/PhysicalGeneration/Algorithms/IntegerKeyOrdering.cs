namespace Engine.WorldGen;

internal static class IntegerKeyOrdering
{
    public static int[] DescendingThenIndex(ReadOnlySpan<int> keys)
    {
        if (keys.IsEmpty)
        {
            return [];
        }

        var minimum = int.MaxValue;
        var maximum = int.MinValue;
        foreach (var key in keys)
        {
            minimum = Math.Min(minimum, key);
            maximum = Math.Max(maximum, key);
        }

        var heads = new int[checked(maximum - minimum + 1)];
        var next = new int[keys.Length];
        Array.Fill(heads, -1);
        Array.Fill(next, -1);
        for (var index = 0; index < keys.Length; index++)
        {
            var bucket = keys[index] - minimum;
            next[index] = heads[bucket];
            heads[bucket] = index;
        }

        var result = new int[keys.Length];
        var offset = 0;
        for (var bucket = heads.Length - 1; bucket >= 0; bucket--)
        {
            for (var index = heads[bucket]; index >= 0; index = next[index])
            {
                result[offset++] = index;
            }
        }

        return result;
    }
}
