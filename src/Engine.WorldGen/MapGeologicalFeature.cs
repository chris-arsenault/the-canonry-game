namespace Engine.WorldGen;

/// <summary>
/// Discrete geography as objects rather than as per-triangle facts: a river
/// system, a range, a lake, a notable fall.
///
/// Everything worldgen produces otherwise describes a triangle, which is the
/// wrong shape for naming, for chronicle reference, and for gameplay - none of
/// which may see a triangle at all. Features are identified here, where the
/// triangle data lives, and handed on as hex facts. They are deliberately not
/// derived on demand: doing that would put triangle data behind a call the
/// kernel makes, which is exactly the contamination the domain check exists to
/// prevent.
///
/// Identifiers are deterministic from position rather than from iteration
/// order, so regenerating a seed yields the same features. They are not
/// expected to survive a change to the generator.
/// </summary>
public sealed class MapGeologicalFeatureMap
{
    private readonly Dictionary<int, MapGeologicalFeature> _features = [];
    private readonly Dictionary<HexCoord, List<int>> _byCoord = [];

    public IReadOnlyDictionary<int, MapGeologicalFeature> Features => _features;

    public void Add(MapGeologicalFeature feature, IReadOnlyCollection<HexCoord> coords)
    {
        ArgumentNullException.ThrowIfNull(feature);
        ArgumentNullException.ThrowIfNull(coords);
        _features[feature.Id] = feature;
        foreach (var coord in coords)
        {
            if (!_byCoord.TryGetValue(coord, out var list))
            {
                list = [];
                _byCoord[coord] = list;
            }

            if (!list.Contains(feature.Id))
            {
                list.Add(feature.Id);
            }
        }
    }

    /// <summary>Every hex any feature covers.</summary>
    public IEnumerable<HexCoord> CoveredCoords() => _byCoord.Keys;

    /// <summary>Features covering this hex, by identifier.</summary>
    public IReadOnlyList<int> At(HexCoord coord) =>
        _byCoord.TryGetValue(coord, out var list) ? list : [];
}

/// <summary>
/// One named-able piece of geography. <paramref name="Magnitude"/> is the
/// measure that makes it worth naming and lets gameplay rank features of a
/// kind against each other: a river's discharge, a peak's height, a lake's
/// area.
/// </summary>
public sealed record MapGeologicalFeature(
    int Id,
    MapGeologicalFeatureKind Kind,
    HexCoord Anchor,
    int Magnitude,
    int HexCount);

public enum MapGeologicalFeatureKind
{
    RiverSystem,
    Lake,
    Range,
    Peak,
    Waterfall,
    Crater,
    Plain,
}
