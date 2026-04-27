namespace Engine.Domain.Loader;

using Engine.Behaviors;
using Engine.Domain.Compilation;
using Engine.Domain.Config;
using Engine.Domain.Graph;
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

    /// <summary>Register process definitions from domain config into a definition-backed index.</summary>
    public static void LoadProcesses(DomainConfig config, BehaviorDefinitionIndex definitions)
    {
        ArgumentNullException.ThrowIfNull(config);
        LoadProcesses(config, definitions, DomainCompiler.BuildCatalog(config));
    }

    public static void LoadProcesses(
        DomainConfig config,
        BehaviorDefinitionIndex definitions,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(runtimeIds);

        foreach (var procDef in config.Processes)
        {
            procDef.BindRuntimeIds(runtimeIds);
            definitions.Register(procDef);
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
            if (procDef.Reads.Count > 0 ||
                procDef.Writes.Count > 0 ||
                procDef.GoalEffects.Count > 0 ||
                procDef.ProviderSelectors.Count > 0)
            {
                var processAnalysis = PreconditionInspector.Analyze(procDef);
                validator.RegisterContract(new WiringContract(
                    procDef.Id,
                    procDef.Reads,
                    procDef.Writes,
                    goalEffects: procDef.GoalEffects,
                    providerSelectors: procDef.ProviderSelectors,
                    processBlockers: processAnalysis.ProcessBlockers,
                    processSupportEffects: procDef.SupportRules.Select(r => r.DesiredEffect).ToHashSet(),
                    hasOpaqueProcessBranches: processAnalysis.HasOpaqueProcessBranches));
            }
        }

        return validator;
    }

    /// <summary>Initialize attributes for all entities in the graph from domain config.
    /// Creates ScalarAttributes on entities with drift effects.</summary>
    public static void InitializeEntityAttributes(
        Engine.Graph.WorldGraph graph, DomainConfig config, Random? random = null)
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
                    var rng = random ?? Random.Shared;
                    var defaultVal = attr.Default;
                    if (attr.DefaultVariance > 0f)
                    {
                        var jitter = 1f + ((float)(rng.NextDouble() * 2.0 - 1.0) * attr.DefaultVariance);
                        defaultVal = Math.Clamp(defaultVal * jitter, attr.Min, attr.Max);
                    }

                    var instance = new Attributes.ScalarValue(attr.Name, attr.Min, attr.Max, defaultVal);
                    if (attr.DriftRate != 0f)
                    {
                        instance.AddTickEffect(new Attributes.AttributeEffect(
                            $"innate_drift_{attr.Name}",
                            $"Natural {attr.Name} drift",
                            attr.DriftRate));
                    }

                    entity.Attributes.Add(instance);
                }
            }
        }
    }
}
