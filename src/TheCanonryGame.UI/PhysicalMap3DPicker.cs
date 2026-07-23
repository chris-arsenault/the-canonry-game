using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

public sealed class PhysicalMap3DPicker
{
    private readonly PhysicalMicrogeographyGeometry _geometry;
    private readonly PhysicalMicrogeographyMeshData _mesh;

    public PhysicalMap3DPicker(
        PhysicalMicrogeographyGeometry geometry,
        PhysicalMicrogeographyMeshData mesh)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(mesh);
        _geometry = geometry;
        _mesh = mesh;
    }

    public bool TryPick(
        Camera3D camera,
        Vector2 screenPosition,
        float verticalExaggeration,
        out PhysicalMicrotriangleHit hit)
    {
        ArgumentNullException.ThrowIfNull(camera);
        var origin = camera.ProjectRayOrigin(screenPosition);
        var direction = camera.ProjectRayNormal(screenPosition).Normalized();
        if (Math.Abs(direction.Y) < 0.0001f)
        {
            hit = default;
            return false;
        }

        var nearest = float.PositiveInfinity;
        var found = false;
        var foundHit = default(PhysicalMicrotriangleHit);
        foreach (var coord in CandidateCoords(origin, direction, verticalExaggeration))
        {
            if (!_mesh.TryGetFaces(coord, out var faces))
            {
                continue;
            }

            foreach (var face in faces)
            {
                if (!TryIntersectTriangle(
                    origin,
                    direction,
                    ScaleHeight(face.A, verticalExaggeration),
                    ScaleHeight(face.B, verticalExaggeration),
                    ScaleHeight(face.C, verticalExaggeration),
                    out var distance) ||
                    distance >= nearest)
                {
                    continue;
                }

                nearest = distance;
                foundHit = new PhysicalMicrotriangleHit(face.Coord, face.TriangleIndex);
                found = true;
            }
        }

        hit = foundHit;
        return found;
    }

    private IEnumerable<HexCoord> CandidateCoords(
        Vector3 origin,
        Vector3 direction,
        float verticalExaggeration)
    {
        var maximumHeight = _mesh.MaximumAbsoluteHeight * verticalExaggeration;
        var high = ProjectToHeight(origin, direction, maximumHeight);
        var low = ProjectToHeight(origin, direction, -maximumHeight);
        var start = PixelToHex(new Vector2(high.X, high.Z));
        var end = PixelToHex(new Vector2(low.X, low.Z));
        var yielded = new HashSet<HexCoord>();
        foreach (var lineCoord in HexLine(start, end))
        {
            foreach (var candidate in HexCoord.CoordsInRadius(lineCoord, 1))
            {
                if (yielded.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }
    }

    private static Vector3 ProjectToHeight(Vector3 origin, Vector3 direction, float height)
    {
        var distance = (height - origin.Y) / direction.Y;
        return origin + direction * Math.Max(0f, distance);
    }

    private static IEnumerable<HexCoord> HexLine(HexCoord start, HexCoord end)
    {
        var steps = HexCoord.Distance(start, end);
        if (steps == 0)
        {
            yield return start;
            yield break;
        }

        for (var step = 0; step <= steps; step++)
        {
            var amount = step / (float)steps;
            yield return RoundAxial(
                Mathf.Lerp(start.Q, end.Q, amount),
                Mathf.Lerp(start.R, end.R, amount));
        }
    }

    public static bool TryIntersectTriangle(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        out float distance)
    {
        const float epsilon = 0.000001f;
        var edge1 = b - a;
        var edge2 = c - a;
        var perpendicular = rayDirection.Cross(edge2);
        var determinant = edge1.Dot(perpendicular);
        if (Math.Abs(determinant) < epsilon)
        {
            distance = 0f;
            return false;
        }

        var inverse = 1f / determinant;
        var fromA = rayOrigin - a;
        var u = fromA.Dot(perpendicular) * inverse;
        if (u is < 0f or > 1f)
        {
            distance = 0f;
            return false;
        }

        var cross = fromA.Cross(edge1);
        var v = rayDirection.Dot(cross) * inverse;
        if (v < 0f || u + v > 1f)
        {
            distance = 0f;
            return false;
        }

        distance = edge2.Dot(cross) * inverse;
        return distance > epsilon;
    }

    private HexCoord PixelToHex(Vector2 pixel)
    {
        var q = ((Mathf.Sqrt(3f) / 3f * pixel.X) - (pixel.Y / 3f)) / _geometry.HexSize;
        var r = (2f / 3f * pixel.Y) / _geometry.HexSize;
        return RoundAxial(q, r);
    }

    private static HexCoord RoundAxial(float q, float r)
    {
        var s = -q - r;
        var roundedQ = (int)MathF.Round(q, MidpointRounding.AwayFromZero);
        var roundedR = (int)MathF.Round(r, MidpointRounding.AwayFromZero);
        var roundedS = (int)MathF.Round(s, MidpointRounding.AwayFromZero);
        var qDifference = MathF.Abs(roundedQ - q);
        var rDifference = MathF.Abs(roundedR - r);
        var sDifference = MathF.Abs(roundedS - s);
        if (qDifference > rDifference && qDifference > sDifference)
        {
            roundedQ = -roundedR - roundedS;
        }
        else if (rDifference > sDifference)
        {
            roundedR = -roundedQ - roundedS;
        }

        return new HexCoord(roundedQ, roundedR);
    }

    private static Vector3 ScaleHeight(Vector3 point, float verticalExaggeration) =>
        new(point.X, point.Y * verticalExaggeration, point.Z);
}
