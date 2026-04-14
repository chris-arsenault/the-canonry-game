namespace Engine.Domain;

using Engine.Behaviors;
using Engine.Processes;
using Engine.Kinds;
using Engine.Lifecycle;
using Engine.Pressure;
using Engine.Wiring;

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

    /// <summary>Register data-driven processes and decisions from domain config.</summary>
    public static void LoadProcesses(DomainConfig config, ActivityRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(registry);

        foreach (var procDef in config.Processes)
        {
            if (procDef.Role == DefinitionRole.Process)
            {
                registry.RegisterProcess(procDef.TargetKind, new DataDrivenProcess(procDef));
            }
            else
            {
                registry.RegisterDecision(procDef.TargetKind, new DataDrivenDecision(procDef));
            }
        }
    }

    /// <summary>Extract wiring contracts from process definitions and validate.</summary>
    public static WiringValidator LoadAndValidateWiring(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var validator = new WiringValidator();

        // Register health attributes per kind
        foreach (var (kind, kindConfig) in config.Kinds)
        {
            validator.RegisterHealthAttribute(kind, kindConfig.HealthAttribute);
        }

        // Extract contracts from process definitions
        foreach (var procDef in config.Processes)
        {
            if (procDef.Reads.Count > 0 || procDef.Writes.Count > 0)
            {
                validator.RegisterContract(new WiringContract(
                    procDef.Id, procDef.Reads, procDef.Writes));
            }
        }

        return validator;
    }

    /// <summary>Initialize attributes for all entities in the graph from domain config.
    /// Creates ScalarAttributes on entities with drift effects.</summary>
    public static void InitializeEntityAttributes(
        Graph.WorldGraph graph, DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(config);

        foreach (var (_, entity) in graph.AllEntities)
        {
            if (!config.Kinds.TryGetValue(entity.Kind, out var kindConfig))
            {
                continue;
            }

            foreach (var attr in kindConfig.Attributes)
            {
                if (!entity.Attributes.Has(attr.Name))
                {
                    Attributes.IEntityProperty instance;
                    if (attr.PlaneDefinition is not null)
                    {
                        instance = new Attributes.SemanticPlane(
                            attr.Name, attr.PlaneDefinition, attr.Min, attr.Max, attr.DefaultX, attr.DefaultY);
                    }
                    else
                    {
                        var scalar = new Attributes.ScalarValue(attr.Name, attr.Min, attr.Max, attr.Default);

                        // Register drift as a tick effect (scalar only for now)
                        if (attr.DriftRate != 0f)
                        {
                            scalar.AddTickEffect(new Attributes.AttributeEffect(
                                $"innate_drift_{attr.Name}",
                                $"Natural {attr.Name} drift",
                                attr.DriftRate));
                        }

                        instance = scalar;
                    }

                    entity.Attributes.Add(instance);
                }
            }
        }
    }
}
