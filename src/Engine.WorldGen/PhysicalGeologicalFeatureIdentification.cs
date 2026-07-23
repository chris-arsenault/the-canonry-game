namespace Engine.WorldGen;

using Engine.Constants;

/// <summary>
/// Identifies discrete features entirely on the generated microtriangle
/// lattice. Runtime-map coverage is assigned only after generation.
/// </summary>
public static class PhysicalGeologicalFeatureIdentification
{
    private const int RiverSystemMinimumDischarge = 60000;
    private const int LakeMinimumTriangles = 96;

    public static PhysicalGeologicalFeatureMap Identify(PhysicalGeographyField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        var map = new PhysicalGeologicalFeatureMap();
        IdentifyRiverSystems(field, map);
        IdentifyLakes(field, map);
        return map;
    }

    private static void IdentifyRiverSystems(
        PhysicalGeographyField field,
        PhysicalGeologicalFeatureMap map)
    {
        var targets = FlowTargets(field);
        var upstream = BuildUpstream(field, targets);
        var claimed = new bool[field.Grid.TriangleCount];
        for (var triangleIndex = 0;
            triangleIndex < field.Grid.TriangleCount;
            triangleIndex++)
        {
            if (!IsRiverMouth(field, targets, triangleIndex) ||
                field.FlowAccumulation[triangleIndex] < RiverSystemMinimumDischarge)
            {
                continue;
            }

            var members = new HashSet<int>();
            GatherUpstream(field, upstream, claimed, triangleIndex, members);
            if (members.Count == 0)
            {
                continue;
            }

            map.Add(
                new PhysicalGeologicalFeature(
                    FeatureId(
                        PhysicalGeologicalFeatureKind.RiverSystem,
                        field.Grid.TriangleCoordAt(triangleIndex)),
                    PhysicalGeologicalFeatureKind.RiverSystem,
                    triangleIndex,
                    field.FlowAccumulation[triangleIndex],
                    members.Count),
                members);
        }
    }

    private static void IdentifyLakes(
        PhysicalGeographyField field,
        PhysicalGeologicalFeatureMap map)
    {
        var membersByBody = new Dictionary<int, HashSet<int>>();
        var lowestByBody = new Dictionary<int, int>();
        for (var triangleIndex = 0;
            triangleIndex < field.Grid.TriangleCount;
            triangleIndex++)
        {
            if (field.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Lake ||
                field.WaterBodyIds[triangleIndex] <= 0)
            {
                continue;
            }

            var body = field.WaterBodyIds[triangleIndex];
            if (!membersByBody.TryGetValue(body, out var members))
            {
                members = [];
                membersByBody[body] = members;
            }

            members.Add(triangleIndex);
            if (!lowestByBody.TryGetValue(body, out var best) ||
                field.Elevation[triangleIndex] < field.Elevation[best])
            {
                lowestByBody[body] = triangleIndex;
            }
        }

        foreach (var (body, members) in membersByBody)
        {
            if (members.Count < LakeMinimumTriangles)
            {
                continue;
            }

            var anchor = lowestByBody[body];
            map.Add(
                new PhysicalGeologicalFeature(
                    FeatureId(
                        PhysicalGeologicalFeatureKind.Lake,
                        field.Grid.TriangleCoordAt(anchor)),
                    PhysicalGeologicalFeatureKind.Lake,
                    anchor,
                    members.Count,
                    members.Count),
                members);
        }
    }

    private static bool IsRiverMouth(
        PhysicalGeographyField field,
        int[] targets,
        int triangleIndex)
    {
        if (field.RiverOrders[triangleIndex] <= 0)
        {
            return false;
        }

        var target = targets[triangleIndex];
        return target < 0 || field.WaterKindCodes[target] == PhysicalWaterKindCodes.Ocean;
    }

    private static void GatherUpstream(
        PhysicalGeographyField field,
        List<int>[] upstream,
        bool[] claimed,
        int mouth,
        HashSet<int> members)
    {
        var stack = new Stack<int>();
        stack.Push(mouth);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (claimed[current] || field.RiverOrders[current] <= 0)
            {
                continue;
            }

            claimed[current] = true;
            members.Add(current);
            foreach (var source in upstream[current] ?? [])
            {
                stack.Push(source);
            }
        }
    }

    private static List<int>[] BuildUpstream(PhysicalGeographyField field, int[] targets)
    {
        var upstream = new List<int>[field.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < targets.Length; triangleIndex++)
        {
            var target = targets[triangleIndex];
            if (target < 0 || field.RiverOrders[triangleIndex] <= 0)
            {
                continue;
            }

            (upstream[target] ??= []).Add(triangleIndex);
        }

        return upstream;
    }

    private static int[] FlowTargets(PhysicalGeographyField field)
    {
        var targets = new int[field.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < targets.Length; triangleIndex++)
        {
            var edge = field.FlowEdges[triangleIndex];
            targets[triangleIndex] = edge is >= 0 and <= 2
                ? field.Grid.NeighborTriangleIndex(triangleIndex, edge)
                : -1;
        }

        return targets;
    }

    private static int FeatureId(
        PhysicalGeologicalFeatureKind kind,
        MicrotriangleCoord anchor)
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 486187739) + (int)kind;
            hash = (hash * 486187739) + anchor.U;
            hash = (hash * 486187739) + anchor.V;
            hash = (hash * 486187739) + (anchor.Inverted ? 1 : 0);
            return hash & 0x7fffffff;
        }
    }
}
