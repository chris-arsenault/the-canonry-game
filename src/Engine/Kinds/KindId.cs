namespace Engine.Kinds;

using System.Collections.Concurrent;
using System.Threading;
using MessagePack;

/// <summary>
/// Domain-registered identifier for an entity kind.
/// Acts like an enum value at call sites while allowing the domain to own the set.
/// </summary>
 [MessagePackObject]
public readonly record struct KindId
{
    private static readonly ConcurrentDictionary<string, KindId> Registered =
        new(StringComparer.Ordinal);
    private static int _nextId;

    [Key(0)]
    public string Value { get; }

    [IgnoreMember]
    public int Id { get; }

    [SerializationConstructor]
    public KindId(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        var registered = Registered.GetOrAdd(value, static v => new KindId(v, Interlocked.Increment(ref _nextId)));
        Value = registered.Value;
        Id = registered.Id;
    }

    private KindId(string value, int id)
    {
        Value = value;
        Id = id;
    }

    public static KindId Register(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        return Registered.GetOrAdd(value, static v => new KindId(v, Interlocked.Increment(ref _nextId)));
    }

    public static void RegisterAll(IEnumerable<KindId> kinds)
    {
        ArgumentNullException.ThrowIfNull(kinds);
        foreach (var kind in kinds)
        {
            _ = Register(kind.Value);
        }
    }

    public static KindId From(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        if (Registered.TryGetValue(value, out var kind))
        {
            return kind;
        }

        throw new InvalidOperationException(
            $"Unknown kind '{value}'. Register it with the domain before use.");
    }

    public static IReadOnlyCollection<KindId> AllRegistered =>
        Registered.Values.ToArray();

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
