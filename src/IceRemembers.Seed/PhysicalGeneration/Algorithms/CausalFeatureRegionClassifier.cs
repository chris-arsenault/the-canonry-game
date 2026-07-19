namespace IceRemembers.Seed;

using Engine.WorldGen;

/// <summary>
/// Grows a feature from strong causal seeds through adjacent triangles where
/// the same cause remains materially present. The lower retention threshold
/// prevents field noise from fragmenting otherwise legible geography.
/// </summary>
internal sealed class CausalFeatureRegionClassifier
{
    private readonly PhysicalGeographyGrid _grid;
    private readonly bool[] _included;
    private readonly int[] _queue;

    public CausalFeatureRegionClassifier(PhysicalGeographyGrid grid)
    {
        _grid = grid;
        _included = new bool[grid.TriangleCount];
        _queue = new int[grid.TriangleCount];
    }

    public void Classify(Func<int, bool> isSeed, Func<int, bool> isRetained)
    {
        Array.Clear(_included);
        var head = 0;
        var tail = 0;
        for (var triangleIndex = 0; triangleIndex < _grid.TriangleCount; triangleIndex++)
        {
            if (!isSeed(triangleIndex))
            {
                continue;
            }

            _included[triangleIndex] = true;
            _queue[tail++] = triangleIndex;
        }

        while (head < tail)
        {
            var triangleIndex = _queue[head++];
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = _grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor < 0 || _included[neighbor] || !isRetained(neighbor))
                {
                    continue;
                }

                _included[neighbor] = true;
                _queue[tail++] = neighbor;
            }
        }
    }

    public bool Includes(int triangleIndex) => _included[triangleIndex];
}
