namespace Engine.Kinds;

/// <summary>
/// Defines a single attribute that an entity kind can have.
/// </summary>
public sealed class AttributeDefinition
{
    public string Name { get; }
    public float Min { get; }
    public float Max { get; }
    public float Default { get; }

    public AttributeDefinition(string name, float min, float max, float defaultValue)
    {
        Name = name;
        Min = min;
        Max = max;
        Default = Math.Clamp(defaultValue, min, max);
    }
}
