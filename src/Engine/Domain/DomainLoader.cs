namespace Engine.Domain;

using Engine.Kinds;
using Engine.Lifecycle;
using Engine.Pressure;

/// <summary>
/// Loads a DomainConfig into the engine's runtime systems.
/// Replaces the hard-coded Default*.cs files.
/// </summary>
public static class DomainLoader
{
    /// <summary>Build a KindRegistry from domain config.</summary>
    public static KindRegistry LoadKindRegistry(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var registry = new KindRegistry();

        foreach (var (kind, kindConfig) in config.Kinds)
        {
            var attributes = kindConfig.Attributes
                .Select(a => new AttributeDefinition(a.Name, a.Min, a.Max, a.Default))
                .ToList();

            registry.RegisterKind(new KindDefinition(
                kind,
                new HashSet<string>(kindConfig.Subtypes),
                attributes,
                kindConfig.HealthAttribute));
        }

        foreach (var relConfig in config.Relationships)
        {
            registry.RegisterRelationshipKind(new RelationshipKindDefinition(
                relConfig.Name,
                relConfig.Polarity,
                new HashSet<EntityCategory>(relConfig.ValidSourceCategories),
                new HashSet<EntityCategory>(relConfig.ValidTargetCategories),
                relConfig.IsSymmetric,
                relConfig.IsExclusive,
                relConfig.CullThreshold));
        }

        return registry;
    }

    /// <summary>Build a LifecycleConfig from domain config.</summary>
    public static Lifecycle.LifecycleConfig LoadLifecycleConfig(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var lifecycleConfig = new Lifecycle.LifecycleConfig();

        foreach (var (kind, lc) in config.Lifecycles)
        {
            lifecycleConfig.Register(kind, lc.HealthAttribute,
                new LinearAging { BaseRate = lc.BaseRate, AgeCoefficient = lc.AgeCoefficient });
        }

        return lifecycleConfig;
    }

    /// <summary>Build PressureDefinitions from domain config.</summary>
    public static IEnumerable<PressureDefinition> LoadPressureDefinitions(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        foreach (var pc in config.Pressures)
        {
            yield return new PressureDefinition(
                pc.Name, pc.Scope, pc.Baseline, pc.HomeostasisRate, pc.SmoothingFactor);
            // Contributing factors will be added via wiring contracts (#35)
        }
    }

    /// <summary>Initialize attributes for all entities in the graph from domain config.</summary>
    public static void InitializeEntityAttributes(
        Graph.WorldGraph graph, Attributes.AttributeStore attributes, DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentNullException.ThrowIfNull(config);

        foreach (var (entityId, entity) in graph.AllEntities)
        {
            if (!config.Kinds.TryGetValue(entity.Kind, out var kindConfig))
            {
                continue;
            }

            foreach (var attr in kindConfig.Attributes)
            {
                if (!attributes.Has(entityId, attr.Name))
                {
                    attributes.Initialize(entityId, attr.Name, attr.Min, attr.Max, attr.Default);
                }
            }
        }
    }
}
