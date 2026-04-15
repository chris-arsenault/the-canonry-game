namespace Engine.Resources;

/// <summary>
/// MVP resource types. Resources are properties of the map graph, not entity attributes.
/// </summary>
public enum ResourceType
{
    /// <summary>Ambient food potential of a hex. Supports conversion actions such as stockpiling.</summary>
    Food,

    /// <summary>Ambient wealth potential of a hex. Shapes extraction and location desirability.</summary>
    Wealth,

    /// <summary>Ambient populace on a hex. Constrains unnamed labor, movement, and other background support.</summary>
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
