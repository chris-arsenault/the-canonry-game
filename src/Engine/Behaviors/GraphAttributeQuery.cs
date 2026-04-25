namespace Engine.Behaviors;

using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Resources;
using Engine.Wiring;

/// <summary>
/// Find structurally eligible candidate entities for a planner provider selector.
/// Returns unscored candidates. Scoring is done by the caller.
/// </summary>
public static class GraphAttributeQuery
{
    /// <summary>
    /// Find entities that satisfy the referenced selector.
    /// Returns entity IDs — no scoring, no ordering.
    /// </summary>
    public static IReadOnlyList<EntityId> FindCandidates(
        AttributeRef selector,
        IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(worldState);

        if (worldState is ICachedWorldState cachedWorldState)
        {
            return cachedWorldState.QueryCache.FindProviderCandidates(selector);
        }

        var candidates = new List<EntityId>();

        if (selector.Scope == RefScope.Resource)
        {
            FindResourceCandidates(selector.Name, worldState, candidates);
        }
        else if (selector.Scope == RefScope.Structural && selector.Kind.HasValue)
        {
            FindKindCandidates(selector.Kind.Value, worldState, candidates);
        }
        else if (selector.Kind.HasValue)
        {
            if (selector.AttributeId.Value > 0)
            {
                FindAttributeCandidates(selector.AttributeId, selector.Kind.Value, worldState, candidates);
            }
            else
            {
                FindAttributeCandidates(selector.Name, selector.Kind.Value, worldState, candidates);
            }
        }

        return candidates;
    }

    private static void FindResourceCandidates(
        string resourceName, IWorldState worldState, List<EntityId> candidates)
    {
        var resourceType = resourceName switch
        {
            "food" => ResourceType.Food,
            "wealth" => ResourceType.Wealth,
            "population" => ResourceType.Population,
            _ => (ResourceType?)null,
        };

        if (resourceType is null || worldState.Resources is null) return;

        var layer = resourceType.Value switch
        {
            ResourceType.Food => worldState.Resources.Food,
            ResourceType.Wealth => worldState.Resources.Wealth,
            ResourceType.Population => worldState.Resources.PopulationLayer,
            _ => null,
        };

        if (layer is null) return;

        foreach (var (entityId, entity) in worldState.Graph.AllEntities)
        {
            if (entity is null || entity.Status != EntityStatus.Active)
                continue;

            var coord = new HexCoord(entity.Coordinates.Q, entity.Coordinates.R);
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

            if (entity.Attributes.Has(attributeName))
                candidates.Add(entityId);
        }
    }

    private static void FindAttributeCandidates(
        AttributeRuntimeId attributeId, EntityKind kind, IWorldState worldState, List<EntityId> candidates)
    {
        foreach (var entityId in worldState.Graph.GetEntitiesByKind(kind))
        {
            var entity = worldState.Graph.GetEntity(entityId);
            if (entity is null || entity.Status != EntityStatus.Active)
                continue;

            if (entity.Attributes.Has(attributeId))
                candidates.Add(entityId);
        }
    }

    private static void FindKindCandidates(
        EntityKind kind,
        IWorldState worldState,
        List<EntityId> candidates)
    {
        foreach (var entityId in worldState.Graph.GetEntitiesByKind(kind))
        {
            var entity = worldState.Graph.GetEntity(entityId);
            if (entity is null || entity.Status != EntityStatus.Active)
            {
                continue;
            }

            candidates.Add(entityId);
        }
    }
}
