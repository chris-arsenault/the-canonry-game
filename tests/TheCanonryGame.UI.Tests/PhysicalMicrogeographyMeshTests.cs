using Engine.Projection;

using Godot;

using TheCanonryGame.UI;

namespace TheCanonryGame.UI.Tests;

public sealed class PhysicalMicrogeographyMeshTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, -64)]
    [InlineData(-100, 64)]
    public void AdjacentHexesUseTheSameQuantizedKeysAlongTheirSharedEdge(int q, int r)
    {
        var geometry = new PhysicalMicrogeographyGeometry(hexSize: 30f);
        var originKeys = VertexKeysForHex(geometry, new HexCoord(q, r));
        var eastKeys = VertexKeysForHex(geometry, new HexCoord(q + 1, r));

        var shared = originKeys.Intersect(eastKeys).ToArray();

        Assert.NotEmpty(shared);
        Assert.All(shared, key =>
        {
            Assert.Contains(key, originKeys);
            Assert.Contains(key, eastKeys);
        });
    }

    [Fact]
    public void SharedCornerHeightIsTheMeanOfEveryIncidentTriangleSample()
    {
        var shared = new PhysicalMap3DVertexKey(100, 200);
        var heights = PhysicalMicrogeographyMeshBuilder.ReconstructCornerHeights(
        [
            new PhysicalMap3DTriangleSample(
                shared,
                new PhysicalMap3DVertexKey(0, 0),
                new PhysicalMap3DVertexKey(1, 0),
                10f),
            new PhysicalMap3DTriangleSample(
                shared,
                new PhysicalMap3DVertexKey(2, 0),
                new PhysicalMap3DVertexKey(3, 0),
                30f),
        ]);

        Assert.Equal(20f, heights[shared]);
    }

    [Fact]
    public void EveryCanonicalMicrotriangleUsesGodotTopFacingWindingAndUpwardNormal()
    {
        var geometry = new PhysicalMicrogeographyGeometry(hexSize: 30f);
        for (var triangleIndex = 0; triangleIndex < 24; triangleIndex++)
        {
            var points = geometry.TrianglePoints(triangleIndex);
            var a = new Vector3(points[0].X, triangleIndex % 3, points[0].Y);
            var b = new Vector3(points[1].X, (triangleIndex + 1) % 4, points[1].Y);
            var c = new Vector3(points[2].X, (triangleIndex + 2) % 5, points[2].Y);

            var oriented = PhysicalMicrogeographyMeshBuilder.OrientTopFacing(a, b, c);
            var normal = PhysicalMicrogeographyMeshBuilder.UpwardNormal(
                a,
                oriented.B,
                oriented.C);

            Assert.True((oriented.B - a).Cross(oriented.C - a).Y < 0f);
            Assert.True(normal.Y > 0f);
        }
    }

    [Fact]
    public void MicrotriangleFanKeepsExactCenterAndTopFacingWatertightBoundary()
    {
        var center = new Vector3(0f, 12f, 0f);
        var a = new Vector3(-10f, 2f, -5f);
        var b = new Vector3(0f, 4f, 10f);
        var c = new Vector3(10f, 6f, -5f);

        var fan = PhysicalMicrogeographyMeshBuilder.BuildTopFacingFan(center, a, b, c);

        Assert.Equal(3, fan.Length);
        Assert.All(fan, face =>
        {
            Assert.Equal(center, face.A);
            Assert.True((face.B - face.A).Cross(face.C - face.A).Y < 0f);
        });
        var boundaryVertices = fan
            .SelectMany(static face => new[] { face.B, face.C })
            .ToArray();
        Assert.Equal(2, boundaryVertices.Count(point => point == a));
        Assert.Equal(2, boundaryVertices.Count(point => point == b));
        Assert.Equal(2, boundaryVertices.Count(point => point == c));
    }

    private static HashSet<PhysicalMap3DVertexKey> VertexKeysForHex(
        PhysicalMicrogeographyGeometry geometry,
        HexCoord coord)
    {
        var center = geometry.HexCenter(coord);
        var keys = new HashSet<PhysicalMap3DVertexKey>();
        for (var triangleIndex = 0; triangleIndex < 24; triangleIndex++)
        {
            foreach (var point in geometry.TrianglePoints(triangleIndex))
            {
                keys.Add(PhysicalMicrogeographyMeshBuilder.VertexKey(center + point));
            }
        }

        return keys;
    }
}
