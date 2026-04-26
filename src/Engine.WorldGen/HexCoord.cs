namespace Engine.WorldGen;

/// <summary>
/// Axial hex coordinate (q, r). Uses the "pointy-top" orientation convention.
/// Reference: https://www.redblobgames.com/grids/hexagons/
/// </summary>
public readonly record struct HexCoord(int Q, int R)
{
    /// <summary>Cube coordinate S, derived from Q and R. Q + R + S = 0.</summary>
    public int S => -Q - R;

    /// <summary>Hex distance between two coordinates.</summary>
    public static int Distance(HexCoord a, HexCoord b)
    {
        return (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.S - b.S)) / 2;
    }

    /// <summary>The 6 axial direction vectors for pointy-top hexagons.</summary>
    private static readonly HexCoord[] Directions =
    [
        new(1, 0),   // East
        new(1, -1),  // NorthEast
        new(0, -1),  // NorthWest
        new(-1, 0),  // West
        new(-1, 1),  // SouthWest
        new(0, 1),   // SouthEast
    ];

    /// <summary>Get the neighbor in the given direction (0-5).</summary>
    public HexCoord Neighbor(int direction)
    {
        var dir = Directions[direction % 6];
        return new HexCoord(Q + dir.Q, R + dir.R);
    }

    /// <summary>Get all 6 neighbors.</summary>
    public IEnumerable<HexCoord> Neighbors()
    {
        for (var i = 0; i < 6; i++)
        {
            yield return Neighbor(i);
        }
    }

    /// <summary>Get all coordinates within a radius (hex ring fill).</summary>
    public static IEnumerable<HexCoord> CoordsInRadius(HexCoord center, int radius)
    {
        for (var q = -radius; q <= radius; q++)
        {
            var r1 = Math.Max(-radius, -q - radius);
            var r2 = Math.Min(radius, -q + radius);
            for (var r = r1; r <= r2; r++)
            {
                yield return new HexCoord(center.Q + q, center.R + r);
            }
        }
    }

    public override string ToString() => $"({Q},{R})";
}
