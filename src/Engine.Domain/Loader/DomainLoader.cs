namespace Engine.Domain.Loader;

using Engine.Behaviors;
using Engine.Domain.Compilation;
using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Kinds;
using Engine.Lifecycle;
using Engine.Wiring;

/// <summary>
/// Loads a DomainConfig into the engine's runtime systems.
/// Replaces the hard-coded Default*.cs files.
/// </summary>
public static class DomainLoader
{
    /// <summary>Build a typed domain definition graph for validation and higher-order reasoning.</summary>
    public static DefinitionGraph BuildDefinitionGraph(DomainConfig config) =>
        DomainCompiler.Compile(config).DefinitionGraph;

    /// <summary>Build registered primitive runtime IDs for the domain.</summary>
    public static global::Engine.Domain.DomainIdRegistry BuildRuntimeIds(DomainConfig config) =>
        DomainCompiler.BuildCatalog(config);

    /// <summary>Build a KindRegistry from domain config.</summary>
    public static KindRegistry LoadKindRegistry(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var registry = new KindRegistry();

        foreach (var (kind, kindConfig) in config.Kinds)
        {
            var health = kindConfig.Health;
            var attributes = new[]
            {
                new AttributeDefinition(health.Name, health.Min, health.Max, health.Default),
            };

            registry.RegisterKind(new KindDefinition(
                kind,
                kindConfig.Category,
                new HashSet<string>(kindConfig.Subtypes, StringComparer.Ordinal),
                attributes,
                health.Name));
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
    public static Engine.Lifecycle.LifecycleConfig LoadLifecycleConfig(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var lifecycleConfig = new Engine.Lifecycle.LifecycleConfig();

        foreach (var (kind, lc) in config.Lifecycles)
        {
            lifecycleConfig.Register(kind, lc.Health,
                new LinearAging { BaseRate = lc.BaseRate, AgeCoefficient = lc.AgeCoefficient });
        }

        return lifecycleConfig;
    }

    /// <summary>Extract wiring contracts from behavior definitions and validate.</summary>
    public static WiringValidator LoadAndValidateWiring(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var validator = new WiringValidator();

        // Register health attributes per kind
        foreach (var (kind, kindConfig) in config.Kinds)
        {
            validator.RegisterHealthAttribute(kind, kindConfig.Health.Name);
        }

        // Extract contracts from gambit definitions.
        foreach (var gambit in config.Gambits)
        {
            if (gambit.Reads.Count > 0 ||
                gambit.Writes.Count > 0 ||
                gambit.ProviderSelectors.Count > 0)
            {
                validator.RegisterContract(new WiringContract(
                    gambit.Id,
                    gambit.Reads,
                    gambit.Writes,
                    providerSelectors: gambit.ProviderSelectors));
            }
        }

        return validator;
    }

}
