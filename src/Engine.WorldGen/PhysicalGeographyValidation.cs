namespace Engine.WorldGen;

using Engine.Constants;

public static class PhysicalGeographyValidator
{
    public static void Validate(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(profile);
        ValidateCodes(field);
        ValidateOcean(field);
        ValidateFlowGraph(field);
        ValidateRivers(field);
    }

    private static void ValidateCodes(PhysicalGeographyField field)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (field.LandformCodes[triangleIndex] < PhysicalLandformCodes.Min ||
                field.LandformCodes[triangleIndex] > PhysicalLandformCodes.Max ||
                field.WaterKindCodes[triangleIndex] < PhysicalWaterKindCodes.Min ||
                field.WaterKindCodes[triangleIndex] > PhysicalWaterKindCodes.Max ||
                (field.FeatureFlags[triangleIndex] & ~PhysicalGeographyFeatureCodes.All) != 0)
            {
                throw new InvalidOperationException("Physical geography contains an invalid primitive code.");
            }
        }
    }

    private static void ValidateOcean(PhysicalGeographyField field)
    {
        var oceanCount = 0;
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                oceanCount++;
                if (field.WaterBodyIds[triangleIndex] <= 0 || field.WaterDepths[triangleIndex] <= 0)
                {
                    throw new InvalidOperationException("Physical ocean water is missing body or depth data.");
                }
            }
        }

        if (oceanCount == 0)
        {
            throw new InvalidOperationException("Physical geography contains no connected ocean.");
        }
    }

    private static void ValidateFlowGraph(PhysicalGeographyField field)
    {
        var state = new byte[field.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            ValidateFlowEdge(field, triangleIndex);
            if (state[triangleIndex] == 0)
            {
                TraceFlow(field, triangleIndex, state);
            }
        }
    }

    private static void ValidateFlowEdge(PhysicalGeographyField field, int triangleIndex)
    {
        var target = field.FlowTargets[triangleIndex];
        var edge = field.FlowEdges[triangleIndex];
        if (target < 0)
        {
            if (field.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean ||
                edge != PhysicalGeographyCodes.NoFlowEdge)
            {
                throw new InvalidOperationException("Only ocean outlets may omit a physical flow edge.");
            }

            return;
        }

        if (edge is < 0 or > 2 || field.Grid.NeighborTriangleIndex(triangleIndex, edge) != target)
        {
            throw new InvalidOperationException("Physical drainage contains a non-neighbor flow target.");
        }
    }

    private static void TraceFlow(PhysicalGeographyField field, int start, byte[] state)
    {
        var triangleIndex = start;
        while (triangleIndex >= 0 && state[triangleIndex] == 0)
        {
            state[triangleIndex] = 1;
            triangleIndex = field.FlowTargets[triangleIndex];
        }

        if (triangleIndex >= 0 && state[triangleIndex] == 1)
        {
            throw new InvalidOperationException("Physical drainage contains a cycle.");
        }

        triangleIndex = start;
        while (triangleIndex >= 0 && state[triangleIndex] == 1)
        {
            state[triangleIndex] = 2;
            triangleIndex = field.FlowTargets[triangleIndex];
        }
    }

    private static void ValidateRivers(PhysicalGeographyField field)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (field.RiverOrders[triangleIndex] <= 0)
            {
                continue;
            }

            var target = field.FlowTargets[triangleIndex];
            if (target < 0 || field.FlowAccumulation[target] < field.FlowAccumulation[triangleIndex])
            {
                throw new InvalidOperationException("Physical river accumulation does not continue downstream.");
            }
        }
    }
}
