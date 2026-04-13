namespace Engine.Resources;

/// <summary>
/// MVP resource types. Resources are properties of the map graph, not entity attributes.
/// </summary>
public enum ResourceType
{
    /// <summary>Sustenance. Produced from fertility + labor. Consumed by population.</summary>
    Sustenance,

    /// <summary>Economic resource. Produced from terrain/trade. Consumed by military/development.</summary>
    Gold,
}
