namespace Engine.Domain.Loader;

using Engine.Behaviors;
using Engine.Domain.Compilation;
using Engine.Domain.Config;
using Engine.Domain.Graph;
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
            var attributes = kindConfig.Attributes
                .Select(a => new AttributeDefinition(a.Name, a.Min, a.Max, a.Default))
                .ToList();

            registry.RegisterKind(new KindDefinition(
                kind,
                kindConfig.Category,
                new HashSet<string>(kindConfig.Subtypes, StringComparer.Ordinal),
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
    public static Engine.Lifecycle.LifecycleConfig LoadLifecycleConfig(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var lifecycleConfig = new Engine.Lifecycle.LifecycleConfig();

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
        return LoadPressureDefinitions(config, DomainCompiler.BuildCatalog(config));
    }

    public static IEnumerable<PressureDefinition> LoadPressureDefinitions(
        DomainConfig config,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(runtimeIds);

        return LoadPressureDefinitionsCore(config, runtimeIds);
    }

    private static IEnumerable<PressureDefinition> LoadPressureDefinitionsCore(
        DomainConfig config,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        foreach (var pc in config.Pressures)
        {
            yield return new PressureDefinition(
                pc.Name,
                pc.Scope,
                pc.Baseline,
                pc.HomeostasisRate,
                pc.SmoothingFactor,
                runtimeIds.PressureIdOf(pc.Name));
            // Contributing factors will be added via wiring contracts (#35)
        }
    }

    /// <summary>Extract wiring contracts from behavior definitions and validate.</summary>
    public static WiringValidator LoadAndValidateWiring(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var validator = new WiringValidator();

        // Register health attributes per kind
        foreach (var (kind, kindConfig) in config.Kinds)
        {
            validator.RegisterHealthAttribute(kind, kindConfig.HealthAttribute);
        }

        // Extract contracts from gambit definitions.
        foreach (var gambit in config.Gambits)
        {
            if (gambit.Reads.Count > 0 ||
                gambit.Writes.Count > 0 ||
                gambit.GoalEffects.Count > 0 ||
                gambit.ProviderSelectors.Count > 0)
            {
                var behaviorAnalysis = PreconditionInspector.Analyze(gambit);
                validator.RegisterContract(new WiringContract(
                    gambit.Id,
                    gambit.Reads,
                    gambit.Writes,
                    goalEffects: gambit.GoalEffects,
                    providerSelectors: gambit.ProviderSelectors,
                    behaviorBlockers: behaviorAnalysis.BehaviorBlockers,
                    behaviorSupportEffects: gambit.SupportRules.Select(r => r.DesiredEffect).ToHashSet(),
                    hasOpaqueBehaviorBranches: behaviorAnalysis.HasOpaqueBehaviorBranches));
            }
        }

        return validator;
    }

}
