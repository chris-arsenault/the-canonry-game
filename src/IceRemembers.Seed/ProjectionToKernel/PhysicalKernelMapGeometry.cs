namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.Domain.Spatial;
using Engine.WorldGen;

/// <summary>
/// The sole mapping between the generated global microtriangle lattice and
/// the kernel's cell coordinates and local 24-triangle convention.
/// </summary>
internal sealed class PhysicalKernelMapGeometry
{
    private static readonly Vertex[] Corners =
    [
        new(2, 0),
        new(0, 2),
        new(-2, 2),
        new(-2, 0),
        new(0, -2),
        new(2, -2),
    ];

    private static readonly MicrotriangleCoord[] LocalCoords = CreateLocalCoords();

    private readonly Dictionary<HexCoord, int> _cellIndexes;
    private readonly HexCoord[] _coords;
    private readonly int[] _projectedCellIndexes;
    private readonly int[] _projectedLocalIndexes;
    private readonly int[] _triangleIndexes;

    public static PhysicalKernelMapGeometry FromGeneratedContent(PhysicalGeographyGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (grid.ContentTriangleCount % PhysicalGeographyCodes.MicrotrianglesPerHex != 0)
        {
            throw new InvalidOperationException(
                "Generated content triangle count cannot be projected into complete runtime cells.");
        }

        var cellCount = grid.ContentTriangleCount / PhysicalGeographyCodes.MicrotrianglesPerHex;
        var radius = (int)Math.Round(
            (-3d + Math.Sqrt((12d * cellCount) - 3d)) / 6d,
            MidpointRounding.AwayFromZero);
        if (radius <= 0 || ((3 * radius * radius) + (3 * radius) + 1) != cellCount)
        {
            throw new InvalidOperationException(
                "Generated content triangle count does not describe a kernel map radius.");
        }

        return new PhysicalKernelMapGeometry(radius, grid);
    }

    public PhysicalKernelMapGeometry(int radius, PhysicalGeographyGrid grid)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        ArgumentNullException.ThrowIfNull(grid);
        Radius = radius;
        Grid = grid;
        _coords = HexCoord.CoordsInRadius(new HexCoord(0, 0), radius).ToArray();
        _cellIndexes = new Dictionary<HexCoord, int>(_coords.Length);
        _triangleIndexes = new int[checked(
            _coords.Length * PhysicalGeographyCodes.MicrotrianglesPerHex)];
        _projectedCellIndexes = new int[grid.TriangleCount];
        _projectedLocalIndexes = new int[grid.TriangleCount];
        Array.Fill(_projectedCellIndexes, -1);
        Array.Fill(_projectedLocalIndexes, -1);
        var claimed = new HashSet<int>();
        for (var cellIndex = 0; cellIndex < _coords.Length; cellIndex++)
        {
            var coord = _coords[cellIndex];
            _cellIndexes.Add(coord, cellIndex);
            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                var globalCoord = GlobalCoord(coord, local);
                if (!grid.TryGetTriangleIndex(globalCoord, out var triangleIndex))
                {
                    throw new InvalidOperationException(
                        $"Generated microtriangle field does not cover projected cell ({coord.Q},{coord.R}) local {local}.");
                }

                if (!claimed.Add(triangleIndex))
                {
                    throw new InvalidOperationException(
                        $"Projected cells overlap at generated microtriangle {globalCoord}.");
                }

                _triangleIndexes[Offset(cellIndex, local)] = triangleIndex;
                _projectedCellIndexes[triangleIndex] = cellIndex;
                _projectedLocalIndexes[triangleIndex] = local;
            }
        }
    }

    public int Radius { get; }

    public PhysicalGeographyGrid Grid { get; }

    public int CellCount => _coords.Length;

    public int ProjectedTriangleCount => _triangleIndexes.Length;

    public HexCoord CoordAt(int cellIndex) => _coords[cellIndex];

    public bool TryGetCellIndex(HexCoord coord, out int cellIndex) =>
        _cellIndexes.TryGetValue(coord, out cellIndex);

    public int TriangleIndex(int cellIndex, int localTriangle) =>
        _triangleIndexes[Offset(cellIndex, localTriangle)];

    public int TriangleIndex(HexCoord coord, int localTriangle)
    {
        if (!_cellIndexes.TryGetValue(coord, out var cellIndex))
        {
            return -1;
        }

        return TriangleIndex(cellIndex, localTriangle);
    }

    public bool TryLocateTriangle(
        int triangleIndex,
        out int cellIndex,
        out int localTriangle)
    {
        if ((uint)triangleIndex >= (uint)_projectedCellIndexes.Length ||
            _projectedCellIndexes[triangleIndex] < 0)
        {
            cellIndex = -1;
            localTriangle = -1;
            return false;
        }

        cellIndex = _projectedCellIndexes[triangleIndex];
        localTriangle = _projectedLocalIndexes[triangleIndex];
        return true;
    }

    public bool TryResolveProjectionNeighbor(
        int cellIndex,
        int localTriangle,
        int projectionEdge,
        out int neighborCellIndex,
        out int neighborLocalTriangle)
    {
        var coord = _coords[cellIndex];
        PhysicalMicrotriangleTopology.ResolveNeighbor(
            coord.Q,
            coord.R,
            localTriangle,
            projectionEdge,
            out var neighborQ,
            out var neighborR,
            out neighborLocalTriangle);
        return _cellIndexes.TryGetValue(
            new HexCoord(neighborQ, neighborR),
            out neighborCellIndex);
    }

    public int GenerationEdgeForProjectionEdge(
        int cellIndex,
        int localTriangle,
        int projectionEdge)
    {
        var coord = _coords[cellIndex];
        PhysicalMicrotriangleTopology.ResolveNeighbor(
            coord.Q,
            coord.R,
            localTriangle,
            projectionEdge,
            out var neighborQ,
            out var neighborR,
            out var neighborLocal);
        var source = TriangleIndex(cellIndex, localTriangle);
        var neighborCoord = GlobalCoord(new HexCoord(neighborQ, neighborR), neighborLocal);
        return Grid.TryGetTriangleIndex(neighborCoord, out var target)
            ? Grid.EdgeLeadingTo(source, target)
            : -1;
    }

    public int ProjectionEdgeForGenerationEdge(
        int cellIndex,
        int localTriangle,
        int generationEdge)
    {
        for (var projectionEdge = 0; projectionEdge < 3; projectionEdge++)
        {
            if (GenerationEdgeForProjectionEdge(cellIndex, localTriangle, projectionEdge) ==
                generationEdge)
            {
                return projectionEdge;
            }
        }

        return -1;
    }

    public int TranslateGenerationEdgeMask(
        int cellIndex,
        int localTriangle,
        int generationMask)
    {
        var result = 0;
        for (var generationEdge = 0; generationEdge < 3; generationEdge++)
        {
            if ((generationMask & (1 << generationEdge)) == 0)
            {
                continue;
            }

            var projectionEdge = ProjectionEdgeForGenerationEdge(
                cellIndex,
                localTriangle,
                generationEdge);
            if (projectionEdge >= 0)
            {
                result |= 1 << projectionEdge;
            }
        }

        return result;
    }

    private static MicrotriangleCoord GlobalCoord(HexCoord coord, int localTriangle)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(localTriangle);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(
            localTriangle,
            PhysicalGeographyCodes.MicrotrianglesPerHex);
        var centerU = checked((2 * coord.Q) - (2 * coord.R));
        var centerV = checked((2 * coord.Q) + (4 * coord.R));
        var local = LocalCoords[localTriangle];
        return new(
            checked(centerU + local.U),
            checked(centerV + local.V),
            local.Inverted);
    }

    private static MicrotriangleCoord[] CreateLocalCoords()
    {
        var result = new MicrotriangleCoord[PhysicalGeographyCodes.MicrotrianglesPerHex];
        for (var sector = 0; sector < PhysicalGeographyCodes.HexSectorCount; sector++)
        {
            var a = Corners[sector];
            var b = Corners[(sector + 1) % Corners.Length];
            var halfA = a / 2;
            var halfB = b / 2;
            var middle = (a + b) / 2;
            result[PhysicalMicrotriangleTopology.TriangleIndex(sector, 0)] =
                FromVertices(new(0, 0), halfA, halfB);
            result[PhysicalMicrotriangleTopology.TriangleIndex(sector, 1)] =
                FromVertices(a, halfA, middle);
            result[PhysicalMicrotriangleTopology.TriangleIndex(sector, 2)] =
                FromVertices(b, middle, halfB);
            result[PhysicalMicrotriangleTopology.TriangleIndex(sector, 3)] =
                FromVertices(halfA, halfB, middle);
        }

        return result;
    }

    private static MicrotriangleCoord FromVertices(Vertex a, Vertex b, Vertex c)
    {
        var minimumU = Math.Min(a.U, Math.Min(b.U, c.U));
        var minimumV = Math.Min(a.V, Math.Min(b.V, c.V));
        var containsMinimum =
            a == new Vertex(minimumU, minimumV) ||
            b == new Vertex(minimumU, minimumV) ||
            c == new Vertex(minimumU, minimumV);
        return new(minimumU, minimumV, !containsMinimum);
    }

    private static int Offset(int cellIndex, int localTriangle)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(localTriangle);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(
            localTriangle,
            PhysicalGeographyCodes.MicrotrianglesPerHex);
        return checked(
            (cellIndex * PhysicalGeographyCodes.MicrotrianglesPerHex) + localTriangle);
    }

    private readonly record struct Vertex(int U, int V)
    {
        public static Vertex operator +(Vertex first, Vertex second) =>
            new(first.U + second.U, first.V + second.V);

        public static Vertex operator /(Vertex value, int divisor) =>
            new(value.U / divisor, value.V / divisor);
    }
}
