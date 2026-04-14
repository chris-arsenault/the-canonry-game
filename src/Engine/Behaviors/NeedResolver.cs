namespace Engine.Behaviors;

using Engine.Attributes;
using Engine.Cards;
using Engine.Graph;
using Engine.Kinds;
using Engine.Wiring;

/// <summary>
/// Determines what an entity needs most urgently by examining its attributes,
/// then traces through process wiring contracts to find what provider attribute
/// to search for on the graph.
///
/// Chain: entity state → dominant need → process wiring → provider attribute.
/// No explicit goal definitions — goals are implicit from attribute state.
/// </summary>
public sealed class NeedResolver
{
    private readonly IReadOnlyDictionary<EntityKind, Domain.KindConfig> _kindConfigs;
    private readonly IReadOnlyList<ProcessDefinition> _processes;

    public NeedResolver(
        IReadOnlyDictionary<EntityKind, Domain.KindConfig> kindConfigs,
        IReadOnlyList<ProcessDefinition> processes)
    {
        _kindConfigs = kindConfigs;
        _processes = processes;
    }

    /// <summary>
    /// Resolve what the entity needs and what to search for on the graph.
    /// Returns null if the entity has no urgent needs or no provider can be derived.
    /// </summary>
    public NeedResult? Resolve(Entity entity, AttributeStore attributes)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(attributes);

        if (!_kindConfigs.TryGetValue(entity.Kind, out var kindConfig))
        {
            return null;
        }

        // Find the attribute with the highest urgency (most distant from its ideal)
        var bestUrgency = 0f;
        string? dominantAttribute = null;
        float dominantIdeal = 0f;

        foreach (var attrConfig in kindConfig.Attributes)
        {
            var current = attributes.Get(entity.Id, attrConfig.Name);
            var range = attrConfig.Max - attrConfig.Min;
            if (range <= 0f) continue;

            // Urgency = how far from default (ideal), normalized to [0, 1]
            var distance = Math.Abs(current - attrConfig.Default);
            var urgency = distance / range;

            if (urgency > bestUrgency)
            {
                bestUrgency = urgency;
                dominantAttribute = attrConfig.Name;
                dominantIdeal = attrConfig.Default;
            }
        }

        if (dominantAttribute is null || bestUrgency < 0.01f)
        {
            return null; // All attributes near ideal — no urgent need
        }

        // Find processes that write to the dominant attribute for this entity kind
        // Then find what those processes read — that's the provider attribute
        var providerRefs = new List<AttributeRef>();

        foreach (var process in _processes)
        {
            if (process.TargetKind != entity.Kind) continue;

            var writesToDominant = false;
            foreach (var write in process.Writes)
            {
                if (write.Name == dominantAttribute)
                {
                    writesToDominant = true;
                    break;
                }
            }

            if (!writesToDominant) continue;

            // This process writes to the dominant attribute. Its reads are the provider attributes.
            foreach (var read in process.Reads)
            {
                // Skip self-referential reads (reading the attribute you're writing to)
                if (read.Name == dominantAttribute && read.Scope == RefScope.Self) continue;
                providerRefs.Add(read);
            }
        }

        return new NeedResult(dominantAttribute, bestUrgency, providerRefs);
    }
}

/// <summary>
/// Result of need resolution: what the entity needs and what to search for.
/// </summary>
public sealed class NeedResult
{
    /// <summary>The attribute the entity most urgently needs to change.</summary>
    public string DominantAttribute { get; }

    /// <summary>How urgent the need is (0 = at ideal, 1 = at extreme).</summary>
    public float Urgency { get; }

    /// <summary>Attribute references to search for on the graph (provider attributes).</summary>
    public IReadOnlyList<AttributeRef> ProviderRefs { get; }

    public NeedResult(string dominantAttribute, float urgency, IReadOnlyList<AttributeRef> providerRefs)
    {
        DominantAttribute = dominantAttribute;
        Urgency = urgency;
        ProviderRefs = providerRefs;
    }
}
