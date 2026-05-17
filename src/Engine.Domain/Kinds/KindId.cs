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

/// <summary>
/// Significance tier for an entity kind. Used by read-side projections to bucket entities
/// into the world's Stage (top-significance per tier) and to render the hierarchy of
/// who-contains-what for the player. Not consumed by the kernel. Distinct from runtime
/// altitude (low-altitude verbs vs high-altitude gambits/generators).
/// </summary>
public enum EntityTier
{
    /// <summary>Default. Entities without a meaningful tier (Occurrence-like records).</summary>
    None,

    /// <summary>Top tier: political/territorial entities. Factions, nations, federations.</summary>
    Realm,

    /// <summary>Mid tier: named locations. Settlements, regions, points of interest.</summary>
    Place,

    /// <summary>Bottom tier: individuals. Heroes, monsters, transports, artifacts, abilities.</summary>
    Life,
}
