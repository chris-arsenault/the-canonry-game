namespace IceRemembers.Seed;

using Engine.Domain.Spatial;
using Engine.WorldGen;

/// <summary>
/// Assigns generated microtriangle features to runtime-map cells. Features
/// wholly outside the projected map remain generation padding and are not
/// emitted to the kernel blueprint.
/// </summary>
internal static class ProjectedGeologicalFeatureIdentification
{
    public static ProjectedGeologicalFeatureMap Identify(
        PhysicalGeographyField field,
        PhysicalKernelMapGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(geometry);
        var generated = PhysicalGeologicalFeatureIdentification.Identify(field);
        var projected = new ProjectedGeologicalFeatureMap();
        foreach (var feature in generated.Features.Values)
        {
            var coveredCellIndexes = new HashSet<int>();
            foreach (var triangleIndex in generated.TriangleIndexes(feature.Id))
            {
                if (geometry.TryLocateTriangle(triangleIndex, out var cellIndex, out _))
                {
                    coveredCellIndexes.Add(cellIndex);
                }
            }

            if (coveredCellIndexes.Count == 0)
            {
                continue;
            }

            var anchorCellIndex = geometry.TryLocateTriangle(
                feature.AnchorTriangleIndex,
                out var projectedAnchor,
                out _)
                ? projectedAnchor
                : coveredCellIndexes.Min();
            var coords = coveredCellIndexes
                .Select(geometry.CoordAt)
                .ToArray();
            projected.Add(
                new ProjectedGeologicalFeature(
                    feature.Id,
                    feature.Kind,
                    geometry.CoordAt(anchorCellIndex),
                    feature.Magnitude,
                    coords.Length),
                coords);
        }

        return projected;
    }
}

internal sealed class ProjectedGeologicalFeatureMap
{
    private readonly Dictionary<int, ProjectedGeologicalFeature> _features = [];
    private readonly Dictionary<HexCoord, List<int>> _byCoord = [];

    public IReadOnlyDictionary<int, ProjectedGeologicalFeature> Features => _features;

    public void Add(
        ProjectedGeologicalFeature feature,
        IReadOnlyCollection<HexCoord> coords)
    {
        _features[feature.Id] = feature;
        foreach (var coord in coords)
        {
            if (!_byCoord.TryGetValue(coord, out var ids))
            {
                ids = [];
                _byCoord[coord] = ids;
            }

            if (!ids.Contains(feature.Id))
            {
                ids.Add(feature.Id);
            }
        }
    }

    public IEnumerable<HexCoord> CoveredCoords() => _byCoord.Keys;

    public IReadOnlyList<int> At(HexCoord coord) =>
        _byCoord.TryGetValue(coord, out var ids) ? ids : [];
}

internal sealed record ProjectedGeologicalFeature(
    int Id,
    PhysicalGeologicalFeatureKind Kind,
    HexCoord Anchor,
    int Magnitude,
    int HexCount);
