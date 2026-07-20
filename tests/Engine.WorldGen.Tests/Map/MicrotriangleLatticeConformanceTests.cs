namespace Engine.WorldGen.Tests.Map;

using Engine.Constants;

/// <summary>
/// The adjacency table in <see cref="PhysicalMicrotriangleTopology"/> and the
/// triangle positions in <see cref="PhysicalGeographyGrid"/> are two
/// independent descriptions of one structure. Internal consistency of either
/// proves nothing: a neighbour table can be perfectly reciprocal, pass every
/// acyclicity and continuity check, and still be geometrically twisted. These
/// tests use world-space geometry as the oracle, exhaustively.
/// </summary>
public sealed class MicrotriangleLatticeConformanceTests
{
    // Radius 2 with padding 1 gives every sector of the centre hex a real
    // cross-hex neighbour, so no case is skipped for being off the disk.
    private static readonly PhysicalGeographyGrid Grid = new(radius: 2, padding: 1);

    private const int LocalTriangles = PhysicalGeographyCodes.MicrotrianglesPerHex;

    /// <summary>
    /// Adjacent triangle centroids are close. A twisted link joins triangles a
    /// full hex row apart, which this bound rejects by an order of magnitude.
    /// </summary>
    private const double MaximumCentroidStep = 0.35d;

    [Fact]
    public void EveryTriangleEdge_IsReciprocal()
    {
        var checkedEdges = 0;
        for (var triangle = 0; triangle < Grid.TriangleCount; triangle++)
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = Grid.NeighborTriangleIndex(triangle, edge);
                if (neighbor < 0)
                {
                    continue;
                }

                Assert.True(
                    Grid.EdgeLeadingTo(neighbor, triangle) >= 0,
                    $"triangle {triangle} edge {edge} -> {neighbor} has no return edge");
                checkedEdges++;
            }
        }

        Assert.True(checkedEdges > 0, "no triangle edges were exercised");
    }

    [Fact]
    public void EveryTriangleEdge_JoinsGeometricallyAdjacentTriangles()
    {
        var worst = 0d;
        var worstDescription = string.Empty;
        for (var triangle = 0; triangle < Grid.TriangleCount; triangle++)
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = Grid.NeighborTriangleIndex(triangle, edge);
                if (neighbor < 0)
                {
                    continue;
                }

                var dx = Grid.TriangleX(neighbor) - Grid.TriangleX(triangle);
                var dy = Grid.TriangleY(neighbor) - Grid.TriangleY(triangle);
                var step = Math.Sqrt((dx * dx) + (dy * dy));
                if (step > worst)
                {
                    worst = step;
                    worstDescription =
                        $"triangle {triangle} (local {Grid.LocalTriangleIndex(triangle)}) " +
                        $"edge {edge} -> {neighbor} (local {Grid.LocalTriangleIndex(neighbor)})";
                }
            }
        }

        Assert.True(
            worst <= MaximumCentroidStep,
            $"largest centroid step {worst:0.0000} exceeds {MaximumCentroidStep}: {worstDescription}");
    }

    /// <summary>
    /// The strongest available check: both sides of a shared edge must place
    /// that edge's midpoint at the same world position. This is what the
    /// twisted cross-hex table violated while remaining reciprocal.
    /// </summary>
    [Fact]
    public void SharedEdgeMidpoints_CoincideFromBothSides()
    {
        for (var localTriangle = 0; localTriangle < LocalTriangles; localTriangle++)
        {
            for (var edge = 0; edge < 3; edge++)
            {
                PhysicalMicrotriangleTopology.ResolveNeighbor(
                    0,
                    0,
                    localTriangle,
                    edge,
                    out var neighborQ,
                    out var neighborR,
                    out var neighborLocal);

                var back = ReturnEdge(neighborQ, neighborR, neighborLocal, localTriangle);
                Assert.True(
                    back >= 0,
                    $"local {localTriangle} edge {edge} -> ({neighborQ},{neighborR}) " +
                    $"local {neighborLocal} does not resolve back");

                var mine = EdgeMidpoint(0, 0, localTriangle, edge);
                var theirs = EdgeMidpoint(neighborQ, neighborR, neighborLocal, back);
                var separation = Math.Sqrt(
                    ((mine.X - theirs.X) * (mine.X - theirs.X)) +
                    ((mine.Y - theirs.Y) * (mine.Y - theirs.Y)));

                Assert.True(
                    separation < 0.000001d,
                    $"local {localTriangle} edge {edge} midpoint ({mine.X:0.0000},{mine.Y:0.0000}) " +
                    $"disagrees with ({neighborQ},{neighborR}) local {neighborLocal} edge {back} " +
                    $"midpoint ({theirs.X:0.0000},{theirs.Y:0.0000}) by {separation:0.0000}");
            }
        }
    }

    [Fact]
    public void CrossHexNeighbors_LandInTheHexTheSectorFaces()
    {
        for (var sector = 0; sector < PhysicalGeographyCodes.HexSectorCount; sector++)
        {
            var expected = new HexCoord(0, 0).Neighbor(SectorHexDirection(sector));
            for (var sectorLocal = 1; sectorLocal <= 2; sectorLocal++)
            {
                var localTriangle = PhysicalMicrotriangleTopology.TriangleIndex(sector, sectorLocal);
                PhysicalMicrotriangleTopology.ResolveNeighbor(
                    0,
                    0,
                    localTriangle,
                    2,
                    out var neighborQ,
                    out var neighborR,
                    out _);

                Assert.True(
                    neighborQ == expected.Q && neighborR == expected.R,
                    $"sector {sector} outer edge left the hex toward ({neighborQ},{neighborR}), " +
                    $"expected ({expected.Q},{expected.R})");
            }
        }
    }

    /// <summary>
    /// The lattice is six-fold symmetric by construction, so a purely
    /// topological statistic must be uniform across the six sector rotations.
    /// The twisted table produced a visibly lopsided distribution.
    /// </summary>
    [Fact]
    public void EdgeDirections_AreSixFoldIsotropic()
    {
        var bins = new int[12];
        for (var triangle = 0; triangle < Grid.TriangleCount; triangle++)
        {
            if (!Grid.TriangleIsInsideConfiguredDisk(triangle))
            {
                continue;
            }

            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = Grid.NeighborTriangleIndex(triangle, edge);
                if (neighbor < 0)
                {
                    continue;
                }

                bins[AngleBin(
                    Grid.TriangleX(neighbor) - Grid.TriangleX(triangle),
                    Grid.TriangleY(neighbor) - Grid.TriangleY(triangle))]++;
            }
        }

        // Edges fall only on the six hex-facing directions (0, 60, ... 300).
        for (var bin = 1; bin < bins.Length; bin += 2)
        {
            Assert.True(
                bins[bin] == 0,
                $"edge direction {bin * 30}° should be unreachable but has {bins[bin]} edges");
        }

        var populated = new List<int>();
        for (var bin = 0; bin < bins.Length; bin += 2)
        {
            populated.Add(bins[bin]);
        }

        var smallest = populated.Min();
        var largest = populated.Max();
        Assert.True(
            smallest > 0 && largest - smallest <= largest * 0.02,
            $"edge directions are not six-fold isotropic: [{string.Join(", ", populated)}]");
    }

    [Fact]
    public void CrossHexLinkCounts_AreEqualInEveryDirection()
    {
        var perDirection = new int[6];
        for (var triangle = 0; triangle < Grid.TriangleCount; triangle++)
        {
            if (!Grid.TriangleIsInsideConfiguredDisk(triangle))
            {
                continue;
            }

            var hex = Grid.HexCoordAt(Grid.TriangleHexIndex(triangle));
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = Grid.NeighborTriangleIndex(triangle, edge);
                if (neighbor < 0 || Grid.TriangleHexIndex(neighbor) == Grid.TriangleHexIndex(triangle))
                {
                    continue;
                }

                var neighborHex = Grid.HexCoordAt(Grid.TriangleHexIndex(neighbor));
                var direction = DirectionBetween(hex, neighborHex);
                Assert.True(direction >= 0, $"({hex.Q},{hex.R}) and ({neighborHex.Q},{neighborHex.R}) are not hex neighbours");
                perDirection[direction]++;
            }
        }

        Assert.All(perDirection, count => Assert.Equal(perDirection[0], count));
    }

    [Fact]
    public void EdgeSlopeScales_ArePositiveAndBounded()
    {
        for (var triangle = 0; triangle < Grid.TriangleCount; triangle++)
        {
            for (var edge = 0; edge < 3; edge++)
            {
                if (Grid.NeighborTriangleIndex(triangle, edge) < 0)
                {
                    continue;
                }

                var scale = Grid.TriangleEdgeSlopeScale(triangle, edge);
                Assert.True(
                    scale is > 0.1f and < 10f,
                    $"triangle {triangle} edge {edge} has implausible slope scale {scale}");
            }
        }
    }

    private static int ReturnEdge(int q, int r, int localTriangle, int expectedLocal)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            PhysicalMicrotriangleTopology.ResolveNeighbor(
                q,
                r,
                localTriangle,
                edge,
                out var backQ,
                out var backR,
                out var backLocal);
            if (backQ == 0 && backR == 0 && backLocal == expectedLocal)
            {
                return edge;
            }
        }

        return -1;
    }

    private static (double X, double Y) EdgeMidpoint(int q, int r, int localTriangle, int edge)
    {
        var vertices = TriangleVertices(q, r, localTriangle);
        var (first, second) = EdgeVertices(localTriangle % PhysicalGeographyCodes.MicrotrianglesPerSector, edge);
        return (
            (vertices[first].X + vertices[second].X) / 2d,
            (vertices[first].Y + vertices[second].Y) / 2d);
    }

    /// <summary>
    /// Mirrors the render geometry: sector corners at 60°·s − 30°, the inner
    /// triangle on half-corners, two outer triangles, and the outer cap.
    /// </summary>
    private static (double X, double Y)[] TriangleVertices(int q, int r, int localTriangle)
    {
        var sector = localTriangle / PhysicalGeographyCodes.MicrotrianglesPerSector;
        var sectorLocal = localTriangle % PhysicalGeographyCodes.MicrotrianglesPerSector;
        var center = HexCenter(q, r);
        var a = Corner(sector);
        var b = Corner((sector + 1) % PhysicalGeographyCodes.HexSectorCount);
        var halfA = (a.X / 2d, a.Y / 2d);
        var halfB = (b.X / 2d, b.Y / 2d);
        var edgeMiddle = ((a.X + b.X) / 2d, (a.Y + b.Y) / 2d);
        var local = sectorLocal switch
        {
            0 => new[] { (0d, 0d), halfA, halfB },
            1 => new[] { (a.X, a.Y), halfA, edgeMiddle },
            2 => new[] { (b.X, b.Y), edgeMiddle, halfB },
            _ => new[] { halfA, halfB, edgeMiddle },
        };

        var result = new (double X, double Y)[3];
        for (var index = 0; index < 3; index++)
        {
            result[index] = (center.X + local[index].Item1, center.Y + local[index].Item2);
        }

        return result;
    }

    private static (int First, int Second) EdgeVertices(int sectorLocal, int edge)
    {
        int[] first = [1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1];
        int[] second = [2, 1, 2, 2, 1, 2, 2, 2, 1, 1, 2, 2];
        var index = (sectorLocal * 3) + edge;
        return (first[index], second[index]);
    }

    private static (double X, double Y) Corner(int sector)
    {
        var angle = ((60d * sector) - 30d) * Math.PI / 180d;
        return (Math.Cos(angle), Math.Sin(angle));
    }

    private static (double X, double Y) HexCenter(int q, int r) =>
        (Math.Sqrt(3d) * (q + (r / 2d)), 1.5d * r);

    private static int SectorHexDirection(int sector) => (6 - sector) % 6;

    private static int DirectionBetween(HexCoord from, HexCoord to)
    {
        for (var direction = 0; direction < 6; direction++)
        {
            var candidate = from.Neighbor(direction);
            if (candidate.Q == to.Q && candidate.R == to.R)
            {
                return direction;
            }
        }

        return -1;
    }

    private static int AngleBin(double dx, double dy)
    {
        var angle = Math.Atan2(dy, dx) * 180d / Math.PI;
        if (angle < 0)
        {
            angle += 360d;
        }

        return (int)Math.Round(angle / 30d, MidpointRounding.AwayFromZero) % 12;
    }
}
