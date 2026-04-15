namespace Engine.Behaviors;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Resources;
using Engine.Wiring;

/// <summary>
/// Find candidate entities that have a named attribute or resource.
/// Returns unscored candidates. Scoring is done by the caller
/// (typically GoalGenerator using the process's RewardExpression).
/// </summary>
public static class GraphAttributeQuery
{
    /// <summary>
    /// Find entities that have the referenced attribute/resource with value > 0.
    /// Returns entity IDs — no scoring, no ordering.
    /// </summary>
    public static IReadOnlyList<EntityId> FindCandidates(
        AttributeRef providerRef,
        IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(providerRef);
        ArgumentNullException.ThrowIfNull(worldState);

        var candidates = new List<EntityId>();

        if (providerRef.Scope == RefScope.Resource)
        {
            FindResourceCandidates(providerRef.Name, worldState, candidates);
        }
        else if (providerRef.Kind.HasValue)
        {
            FindAttributeCandidates(providerRef.Name, providerRef.Kind.Value, worldState, candidates);
        }

        return candidates;
    }

    private static void FindResourceCandidates(
        string resourceName, IWorldState worldState, List<EntityId> candidates)
    {
        var resourceType = resourceName switch
        {
            "sustenance" => ResourceType.Sustenance,
            "gold" => ResourceType.Gold,
            "population" => ResourceType.Population,
            _ => (ResourceType?)null,
        };

        if (resourceType is null || worldState.Resources is null) return;

        var layer = resourceType.Value switch
        {
            ResourceType.Sustenance => worldState.Resources.Sustenance,
            ResourceType.Gold => worldState.Resources.Gold,
            ResourceType.Population => worldState.Resources.PopulationLayer,
            _ => null,
        };

        if (layer is null) return;

        foreach (var entityId in worldState.Graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            var entity = worldState.Graph.GetEntity(entityId);
            if (entity is null || entity.Status != EntityStatus.Active || entity.Coordinates is null)
                continue;

            var coord = new HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
            if (layer.GetStockpile(coord) > 0f)
                candidates.Add(entityId);
        }
    }

    private static void FindAttributeCandidates(
        string attributeName, EntityKind kind, IWorldState worldState, List<EntityId> candidates)
    {
        foreach (var entityId in worldState.Graph.GetEntitiesByKind(kind))
        {
            var entity = worldState.Graph.GetEntity(entityId);
            if (entity is null || entity.Status != EntityStatus.Active)
                continue;

            if (entity.Attributes.GetValue(attributeName) > 0f)
                candidates.Add(entityId);
        }
    }
}
