namespace Engine.Domain.Spatial;

/// <summary>
/// Semantic hex coordinate: a position on the neutral idea-space plane. The two
/// free axes (Q, R) are directions only - like compass bearings they carry no
/// meaning of their own. Meaning lives in the terrain at a coordinate and the
/// entities standing on it, not in the axis values. S is the derived third cube
/// coordinate (S = -Q - R); distance between coordinates is the only relation the
/// simulation reads from the plane. All entity kinds carry a semantic position.
/// Physical-mobile entities also carry a physical <see cref="HexCoord"/>; planning
/// uses whichever grid is relevant to the verb being invoked.
/// </summary>
public readonly record struct SemanticHexCoord(int Q, int R)
{
    /// <summary>Derived third cube coordinate; S = -Q - R, so Q + R + S = 0.</summary>
    public int S => -Q - R;

    public static SemanticHexCoord Origin => new(0, 0);

    /// <summary>Hex distance between two semantic coordinates.</summary>
    public static int Distance(SemanticHexCoord a, SemanticHexCoord b)
    {
        return (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.S - b.S)) / 2;
    }

    private static readonly SemanticHexCoord[] Directions =
    [
        new(1, 0),
        new(1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, 1),
    ];

    public SemanticHexCoord Neighbor(int direction)
    {
        var dir = Directions[direction % 6];
        return new SemanticHexCoord(Q + dir.Q, R + dir.R);
    }

    public IEnumerable<SemanticHexCoord> Neighbors()
    {
        for (var i = 0; i < 6; i++)
        {
            yield return Neighbor(i);
        }
    }

    /// <summary>Get all coordinates within a radius (hex ring fill).</summary>
    public static IEnumerable<SemanticHexCoord> CoordsInRadius(SemanticHexCoord center, int radius)
    {
        for (var q = -radius; q <= radius; q++)
        {
            var r1 = Math.Max(-radius, -q - radius);
            var r2 = Math.Min(radius, -q + radius);
            for (var r = r1; r <= r2; r++)
            {
                yield return new SemanticHexCoord(center.Q + q, center.R + r);
            }
        }
    }

    public override string ToString() => $"(q={Q},r={R},s={S})";
}
