namespace Engine.Kinds;

using MessagePack;

/// <summary>
/// Stable domain identifier for an entity kind.
/// Runtime numeric IDs are assigned by DomainIdRegistry during domain compilation.
/// </summary>
[MessagePackObject]
public readonly record struct KindId
{
    [Key(0)]
    public string Value { get; }

    [SerializationConstructor]
    public KindId(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        Value = value;
    }

    public static KindId Register(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        return new KindId(value);
    }

    public static void RegisterAll(IEnumerable<KindId> kinds)
    {
        ArgumentNullException.ThrowIfNull(kinds);
        foreach (var kind in kinds)
        {
            if (string.IsNullOrEmpty(kind.Value))
            {
                throw new ArgumentException("Kind IDs cannot be null or empty.", nameof(kinds));
            }
        }
    }

    public static KindId From(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        return new KindId(value);
    }

    public override string ToString() => Value;
}

/// <summary>
/// Category-level grouping for relationship constraint validation.
/// Relationships are defined at the category level rather than per specific kind.
/// </summary>
public enum EntityCategory
{
    Spatial,
    Agency,
    Asset,
    Record,
}
