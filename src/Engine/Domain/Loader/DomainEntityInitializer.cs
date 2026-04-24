namespace Engine.Domain.Loader;

using System;

using Engine.Attributes;
using Engine.Domain.Config;
using Engine.Graph;

/// <summary>
/// Subscribes to <see cref="WorldGraph.EntityCreated"/> and initialises each
/// new entity's scalar attributes from the domain configuration.
/// Replaces the old <c>DomainLoader.InitializeEntityAttributes</c> sweep so
/// attribute setup happens exactly once, on entity creation, for both initial
/// entities and generator-spawned entities.
///
/// The subscription is a one-way tie to the graph — initializer and graph are
/// collected together at sim teardown; no explicit unsubscribe needed.
/// </summary>
public sealed class DomainEntityInitializer
{
    private readonly DomainConfig _config;
    private readonly Random _random;

    public DomainEntityInitializer(WorldGraph graph, DomainConfig config, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(config);

        _config = config;
        _random = random ?? Random.Shared;
        graph.EntityCreated += OnEntityCreated;
    }

    private void OnEntityCreated(object? sender, EntityCreatedEventArgs e)
    {
        InitializeAttributes(e.Entity, _config, _random);
    }

    /// <summary>
    /// Apply attribute defaults + drift tick-effects to a single entity. Safe
    /// to call more than once on the same entity (attributes already present
    /// are skipped).
    /// </summary>
    public static void InitializeAttributes(Entity entity, DomainConfig config, Random random)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(random);

        if (!config.Kinds.TryGetValue(entity.Kind, out var kindConfig))
        {
            return;
        }

        foreach (var attr in kindConfig.Attributes)
        {
            if (entity.Attributes.Has(attr.Name))
            {
                continue;
            }

            var defaultVal = attr.Default;
            if (attr.DefaultVariance > 0f)
            {
                var jitter = 1f + ((float)(random.NextDouble() * 2.0 - 1.0) * attr.DefaultVariance);
                defaultVal = Math.Clamp(defaultVal * jitter, attr.Min, attr.Max);
            }

            var instance = new ScalarValue(attr.Name, attr.Min, attr.Max, defaultVal);
            if (attr.DriftRate != 0f)
            {
                instance.AddTickEffect(new AttributeEffect(
                    $"innate_drift_{attr.Name}",
                    $"Natural {attr.Name} drift",
                    attr.DriftRate));
            }

            entity.Attributes.Add(instance);
        }
    }
}
