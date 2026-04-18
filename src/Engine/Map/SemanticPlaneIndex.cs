namespace Engine.Map;

using Engine.Graph;

public sealed class SemanticPlaneIndex(WorldGraph graph)
{
    private readonly WorldGraph _graph = graph;

    public SemanticHexCoord? FindFreeSemanticHex(SemanticHexCoord origin, int maxRadius = 5)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRadius);

        var occupied = _graph.AllEntities.Values
            .Where(entity => entity.Status == EntityStatus.Active)
            .Select(entity => entity.SemanticPosition)
            .ToHashSet();

        for (var radius = 0; radius <= maxRadius; radius++)
        {
            foreach (var coord in SemanticHexCoord.CoordsInRadius(origin, radius))
            {
                if (SemanticHexCoord.Distance(origin, coord) != radius)
                {
                    continue;
                }

                if (!occupied.Contains(coord))
                {
                    return coord;
                }
            }
        }

        return null;
    }
}
