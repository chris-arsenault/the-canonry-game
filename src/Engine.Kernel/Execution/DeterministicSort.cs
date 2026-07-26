namespace Engine.Kernel;

internal interface IDeterministicOrder<in T>
{
    int CompareTo(T other);
}

internal static class DeterministicSort
{
    public static void Sort<T>(T[] values, int count)
        where T : struct, IDeterministicOrder<T>
    {
        if (count < 2)
        {
            return;
        }

        for (var root = (count / 2) - 1; root >= 0; root--)
        {
            SiftDown(values, root, count);
        }

        for (var end = count - 1; end > 0; end--)
        {
            (values[0], values[end]) = (values[end], values[0]);
            SiftDown(values, 0, end);
        }
    }

    public static void Reverse<T>(T[] values, int count)
    {
        for (var left = 0; left < count / 2; left++)
        {
            var right = count - left - 1;
            (values[left], values[right]) = (values[right], values[left]);
        }
    }

    private static void SiftDown<T>(T[] values, int root, int count)
        where T : struct, IDeterministicOrder<T>
    {
        while (true)
        {
            var child = (root * 2) + 1;
            if (child >= count)
            {
                return;
            }

            if (child + 1 < count && values[child].CompareTo(values[child + 1]) < 0)
            {
                child++;
            }

            if (values[root].CompareTo(values[child]) >= 0)
            {
                return;
            }

            (values[root], values[child]) = (values[child], values[root]);
            root = child;
        }
    }
}
