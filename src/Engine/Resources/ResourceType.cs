namespace Engine.Resources;

/// <summary>
/// MVP resource types. Resources are properties of the map graph, not entity attributes.
/// </summary>
public enum ResourceType
{
    /// <summary>Food. Produced from fertility + labor. Consumed by residents and NPCs.</summary>
    Food,

    /// <summary>Wealth. Produced from settlement activity and prestige. Redistributed through factions.</summary>
    Wealth,

    /// <summary>Ambient populace on a hex. Grows from food surplus and can be drawn into settlements.</summary>
    Population,
}

public static class ResourceTypeExtensions
{
    public static string ToDomainName(this ResourceType resourceType) => resourceType switch
    {
        ResourceType.Food => "food",
        ResourceType.Wealth => "wealth",
        ResourceType.Population => "population",
        _ => throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, "Unknown resource type"),
    };
}
