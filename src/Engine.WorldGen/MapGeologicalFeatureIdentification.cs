namespace Engine.WorldGen;

using Engine.Constants;

/// <summary>
/// Turns per-triangle geography into discrete features. Runs after physical
/// generation, where triangles are still in scope.
/// </summary>
public static class MapGeologicalFeatureIdentification
{
    /// <summary>Discharge a river must carry at its mouth to be worth naming.</summary>
    private const int RiverSystemMinimumDischarge = 60000;

    /// <summary>Lake area, in triangles, worth naming.</summary>
    private const int LakeMinimumTriangles = 96;

    public static MapGeologicalFeatureMap Identify(PhysicalGeographyField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        var map = new MapGeologicalFeatureMap();
        IdentifyRiverSystems(field, map);
        IdentifyLakes(field, map);
        return map;
    }

    /// <summary>
    /// A river system is everything that drains through one mouth. Walking
    /// upstream from the mouth gathers the trunk and every tributary as a
    /// single thing, which is what a name attaches to - the whole river, not
    /// the reach in front of you.
    /// </summary>
    private static void IdentifyRiverSystems(PhysicalGeographyField field, MapGeologicalFeatureMap map)
    {
        var targets = FlowTargets(field);
        var upstream = BuildUpstream(field, targets);
        var claimed = new bool[field.Grid.TriangleCount];
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!IsRiverMouth(field, targets, t) ||
                field.FlowAccumulation[t] < RiverSystemMinimumDischarge)
            {
                continue;
            }

            var coords = new HashSet<HexCoord>();
            var count = GatherUpstream(field, upstream, claimed, t, coords);
            if (count == 0)
            {
                continue;
            }

            var anchor = HexOf(field, t);
            map.Add(
                new MapGeologicalFeature(
                    FeatureId(MapGeologicalFeatureKind.RiverSystem, anchor),
                    MapGeologicalFeatureKind.RiverSystem,
                    anchor,
                    field.FlowAccumulation[t],
                    coords.Count),
                coords);
        }
    }

    private static void IdentifyLakes(PhysicalGeographyField field, MapGeologicalFeatureMap map)
    {
        var sizes = new Dictionary<int, int>();
        var coords = new Dictionary<int, HashSet<HexCoord>>();
        var lowest = new Dictionary<int, int>();
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.Lake ||
                field.WaterBodyIds[t] <= 0)
            {
                continue;
            }

            var body = field.WaterBodyIds[t];
            sizes[body] = sizes.GetValueOrDefault(body) + 1;
            if (!coords.TryGetValue(body, out var set))
            {
                set = [];
                coords[body] = set;
            }

            set.Add(HexOf(field, t));
            if (!lowest.TryGetValue(body, out var best) ||
                field.Elevation[t] < field.Elevation[best])
            {
                lowest[body] = t;
            }
        }

        foreach (var (body, size) in sizes)
        {
            if (size < LakeMinimumTriangles)
            {
                continue;
            }

            var anchor = HexOf(field, lowest[body]);
            map.Add(
                new MapGeologicalFeature(
                    FeatureId(MapGeologicalFeatureKind.Lake, anchor),
                    MapGeologicalFeatureKind.Lake,
                    anchor,
                    size,
                    coords[body].Count),
                coords[body]);
        }
    }

    private static bool IsRiverMouth(PhysicalGeographyField field, int[] targets, int triangleIndex)
    {
        if (field.RiverOrders[triangleIndex] <= 0)
        {
            return false;
        }

        var target = targets[triangleIndex];
        return target < 0 || field.WaterKindCodes[target] == PhysicalWaterKindCodes.Ocean;
    }

    private static int GatherUpstream(
        PhysicalGeographyField field,
        List<int>[] upstream,
        bool[] claimed,
        int mouth,
        HashSet<HexCoord> coords)
    {
        var stack = new Stack<int>();
        stack.Push(mouth);
        var count = 0;
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (claimed[current] || field.RiverOrders[current] <= 0)
            {
                continue;
            }

            claimed[current] = true;
            count++;
            coords.Add(HexOf(field, current));
            foreach (var source in upstream[current] ?? [])
            {
                stack.Push(source);
            }
        }

        return count;
    }

    private static List<int>[] BuildUpstream(PhysicalGeographyField field, int[] targets)
    {
        var upstream = new List<int>[field.Grid.TriangleCount];
        for (var t = 0; t < targets.Length; t++)
        {
            var target = targets[t];
            if (target < 0 || field.RiverOrders[t] <= 0)
            {
                continue;
            }

            (upstream[target] ??= []).Add(t);
        }

        return upstream;
    }

    private static int[] FlowTargets(PhysicalGeographyField field)
    {
        var targets = new int[field.Grid.TriangleCount];
        for (var t = 0; t < targets.Length; t++)
        {
            var edge = field.FlowEdges[t];
            targets[t] = edge is >= 0 and <= 2 ? field.Grid.NeighborTriangleIndex(t, edge) : -1;
        }

        return targets;
    }

    private static HexCoord HexOf(PhysicalGeographyField field, int triangleIndex)
    {
        var hexIndex = field.Grid.TriangleHexIndex(triangleIndex);
        return field.Grid.HexCoordAt(hexIndex);
    }

    /// <summary>
    /// Derived from position and kind rather than from the order features
    /// happen to be found, so the same seed yields the same identifiers.
    /// </summary>
    private static int FeatureId(MapGeologicalFeatureKind kind, HexCoord anchor)
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 486187739) + (int)kind;
            hash = (hash * 486187739) + anchor.Q;
            hash = (hash * 486187739) + anchor.R;
            return hash & 0x7fffffff;
        }
    }
}
