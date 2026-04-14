namespace Engine.Behaviors;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Resources;
using Engine.Wiring;

/// <summary>
/// Generic graph query: find entities that provide a needed attribute or resource.
/// Weight by provider value and inverse distance. Return top N candidates.
///
/// This is the provider/consumer target resolution. Entities on the graph ARE
/// attribute providers by virtue of having attributes/resources. Consumers query
/// for what they need. No coupling between provider and consumer.
/// </summary>
public static class GraphAttributeQuery
{
    /// <summary>
    /// Find entities on the graph that provide the needed attribute/resource.
    /// Returns candidates sorted by weight (highest first), limited to topN.
    /// </summary>
    public static IReadOnlyList<(EntityId Id, float Weight)> FindProviders(
        AttributeRef providerRef,
        Entity consumer,
        IWorldState worldState,
        int topN = 5)
    {
        ArgumentNullException.ThrowIfNull(providerRef);
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(worldState);

        if (consumer.Coordinates is null)
        {
            return [];
        }

        var consumerCoord = new HexCoord(consumer.Coordinates.Value.Q, consumer.Coordinates.Value.R);
        var candidates = new List<(EntityId Id, float Weight)>();

        if (providerRef.Scope == RefScope.Resource)
        {
            // Search for resource providers: settlements (or any entity) at hexes with stockpile
            FindResourceProviders(providerRef.Name, consumerCoord, worldState, candidates);
        }
        else if (providerRef.Scope == RefScope.Self && providerRef.Kind.HasValue)
        {
            // Search for entities of a specific kind with a named attribute > 0
            FindAttributeProviders(providerRef.Name, providerRef.Kind.Value, consumerCoord, worldState, candidates);
        }

        // Sort by weight descending, take top N
        candidates.Sort((a, b) => b.Weight.CompareTo(a.Weight));
        if (candidates.Count > topN)
        {
            candidates.RemoveRange(topN, candidates.Count - topN);
        }

        return candidates;
    }

    private static void FindResourceProviders(
        string resourceName, HexCoord consumerCoord, IWorldState worldState,
        List<(EntityId Id, float Weight)> candidates)
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

        // Find all settlements — they're the entities at hexes with resources
        foreach (var entityId in worldState.Graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            var entity = worldState.Graph.GetEntity(entityId);
            if (entity is null || entity.Status != EntityStatus.Active || entity.Coordinates is null)
            {
                continue;
            }

            var coord = new HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
            var stockpile = layer.GetStockpile(coord);
            if (stockpile <= 0f) continue;

            var distance = HexCoord.Distance(consumerCoord, coord);
            var weight = stockpile * (1f / (1f + distance));
            candidates.Add((entityId, weight));
        }
    }

    private static void FindAttributeProviders(
        string attributeName, EntityKind kind, HexCoord consumerCoord, IWorldState worldState,
        List<(EntityId Id, float Weight)> candidates)
    {
        foreach (var entityId in worldState.Graph.GetEntitiesByKind(kind))
        {
            var entity = worldState.Graph.GetEntity(entityId);
            if (entity is null || entity.Status != EntityStatus.Active || entity.Coordinates is null)
            {
                continue;
            }

            var value = entity.Attributes.GetValue(attributeName);
            if (value <= 0f) continue;

            var coord = new HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
            var distance = HexCoord.Distance(consumerCoord, coord);
            var weight = value * (1f / (1f + distance));
            candidates.Add((entityId, weight));
        }
    }

    /// <summary>
    /// Pick a target from weighted candidates using weighted random selection.
    /// Returns null if candidates is empty.
    /// </summary>
    public static EntityId? WeightedRandomPick(IReadOnlyList<(EntityId Id, float Weight)> candidates, Random random)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(random);
        if (candidates.Count == 0) return null;

        var totalWeight = 0f;
        foreach (var (_, weight) in candidates)
        {
            totalWeight += weight;
        }

        if (totalWeight <= 0f) return null;

        var roll = (float)(random.NextDouble() * totalWeight);
        var cumulative = 0f;
        foreach (var (id, weight) in candidates)
        {
            cumulative += weight;
            if (roll <= cumulative) return id;
        }

        return candidates[^1].Id;
    }
}
