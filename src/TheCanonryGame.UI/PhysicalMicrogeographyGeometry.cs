using Engine.Constants;
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

internal sealed class PhysicalMicrogeographyGeometry
{
    private readonly Vector2[][] _trianglePoints;
    private readonly Vector2[][] _closedTrianglePoints;
    private readonly Vector2[] _triangleCentroids;

    public PhysicalMicrogeographyGeometry(float hexSize)
    {
        HexSize = hexSize;
        HexPoints = BuildHexPoints(hexSize);
        ClosedHexPoints = Close(HexPoints);
        _trianglePoints = new Vector2[PhysicalGeographyCodes.MicrotrianglesPerHex][];
        _closedTrianglePoints = new Vector2[PhysicalGeographyCodes.MicrotrianglesPerHex][];
        _triangleCentroids = new Vector2[PhysicalGeographyCodes.MicrotrianglesPerHex];
        BuildTriangles();
    }

    public float HexSize { get; }

    public Vector2[] HexPoints { get; }

    public Vector2[] ClosedHexPoints { get; }

    public Vector2[] TrianglePoints(int triangleIndex) => _trianglePoints[triangleIndex];

    public Vector2[] ClosedTrianglePoints(int triangleIndex) => _closedTrianglePoints[triangleIndex];

    public Vector2 TriangleCentroid(int triangleIndex) => _triangleCentroids[triangleIndex];

    public Vector2 HexCenter(HexCoord coord) =>
        new(
            HexSize * (Mathf.Sqrt(3f) * coord.Q + (Mathf.Sqrt(3f) * 0.5f * coord.R)),
            HexSize * 1.5f * coord.R);

    public bool TryGetTriangleIndex(Vector2 localPosition, out int triangleIndex)
    {
        for (var index = 0; index < _trianglePoints.Length; index++)
        {
            if (PointIsInTriangle(localPosition, _trianglePoints[index]))
            {
                triangleIndex = index;
                return true;
            }
        }

        triangleIndex = -1;
        return false;
    }

    private void BuildTriangles()
    {
        for (var sector = 0; sector < PhysicalGeographyCodes.HexSectorCount; sector++)
        {
            var a = HexPoints[sector];
            var b = HexPoints[(sector + 1) % PhysicalGeographyCodes.HexSectorCount];
            var halfA = a * 0.5f;
            var halfB = b * 0.5f;
            var edgeMiddle = (a + b) * 0.5f;
            SetTriangle(sector, 0, Vector2.Zero, halfA, halfB);
            SetTriangle(sector, 1, a, halfA, edgeMiddle);
            SetTriangle(sector, 2, b, edgeMiddle, halfB);
            SetTriangle(sector, 3, halfA, halfB, edgeMiddle);
        }
    }

    private void SetTriangle(
        int sector,
        int localTriangle,
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        var index = PhysicalMicrotriangleTopology.TriangleIndex(sector, localTriangle);
        var points = new[] { a, b, c };
        _trianglePoints[index] = points;
        _closedTrianglePoints[index] = Close(points);
        _triangleCentroids[index] = (a + b + c) / 3f;
    }

    private static Vector2[] BuildHexPoints(float size)
    {
        var points = new Vector2[PhysicalGeographyCodes.HexSectorCount];
        for (var index = 0; index < points.Length; index++)
        {
            var angle = Mathf.DegToRad((60f * index) - 30f);
            points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size;
        }

        return points;
    }

    private static Vector2[] Close(Vector2[] points)
    {
        var closed = new Vector2[points.Length + 1];
        points.CopyTo(closed, 0);
        closed[^1] = points[0];
        return closed;
    }

    private static bool PointIsInTriangle(Vector2 point, Vector2[] triangle)
    {
        var d1 = Cross(point, triangle[0], triangle[1]);
        var d2 = Cross(point, triangle[1], triangle[2]);
        var d3 = Cross(point, triangle[2], triangle[0]);
        var hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        var hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNegative && hasPositive);
    }

    private static float Cross(Vector2 point, Vector2 a, Vector2 b) =>
        ((point.X - b.X) * (a.Y - b.Y)) - ((a.X - b.X) * (point.Y - b.Y));
}
