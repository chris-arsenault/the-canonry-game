namespace Engine.Domain.Loader;

using Engine.Domain.Config;
using Engine.Map;
using Engine.Resources;

public static class DomainRuntimeIdLoader
{
    public const string NoneCultureId = "none";
    public const string RelationshipCreatedCode = "created";
    public const string RelationshipCulledCode = "culled";
    public const string RelationshipStrengthChangedCode = "strength_changed";
    public const string ProcessSourceCode = "process";
    public const string CardSourceCode = "card";
    public const string SettlementDepopulationDetector = "settlement_depopulation";
    public const string PoliticalMonocultureDetector = "political_monoculture";
    public const string NpcExtinctionDetector = "npc_extinction";
    public const string SettlementDepopulationReason = "settlement_depopulation_transition";
    public const string PoliticalMonocultureReason = "political_monoculture_transition";
    public const string NpcExtinctionReason = "npc_extinction_transition";

    public static global::Engine.Domain.DomainIdRegistry BuildRuntimeIds(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var registry = new global::Engine.Domain.DomainIdRegistry();
        var attributes = new HashSet<string>(StringComparer.Ordinal);
        var relationshipKinds = new HashSet<string>(StringComparer.Ordinal);
        var relationshipFacts = new HashSet<string>(StringComparer.Ordinal);
        var pressures = new HashSet<string>(StringComparer.Ordinal);
        var resources = new HashSet<string>(StringComparer.Ordinal);
        var terrains = new HashSet<string>(StringComparer.Ordinal);
        var subtypes = new HashSet<string>(StringComparer.Ordinal);
        var cultures = new HashSet<string>(StringComparer.Ordinal);
        var generators = new HashSet<string>(StringComparer.Ordinal);
        var detectors = new HashSet<string>(StringComparer.Ordinal);
        var eventCodes = new HashSet<string>(StringComparer.Ordinal);
        var sourceCodes = new HashSet<string>(StringComparer.Ordinal);
        var reasonCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (_, kindConfig) in config.Kinds)
        {
            foreach (var subtype in kindConfig.Subtypes)
            {
                RegisterOnce(subtypes, subtype, registry.RegisterSubtype);
            }

            foreach (var attribute in kindConfig.Attributes)
            {
                RegisterOnce(attributes, attribute.Name, registry.RegisterAttribute);
            }
        }

        foreach (var relationship in config.Relationships)
        {
            RegisterOnceStrict(relationshipKinds, relationship.Name, registry.RegisterRelationshipKind, "relationship kind");
        }

        foreach (var fact in config.RelationshipFacts)
        {
            RegisterOnceStrict(relationshipFacts, fact.Name, registry.RegisterRelationshipFact, "relationship fact");
        }

        foreach (var pressure in config.Pressures)
        {
            RegisterOnceStrict(pressures, pressure.Name, registry.RegisterPressure, "pressure");
        }

        foreach (var resource in config.Resources)
        {
            RegisterOnceStrict(resources, resource.Name, registry.RegisterResource, "resource");
        }

        RegisterOnce(resources, ResourceType.Food.ToDomainName(), registry.RegisterResource);
        RegisterOnce(resources, ResourceType.Wealth.ToDomainName(), registry.RegisterResource);
        RegisterOnce(resources, ResourceType.Population.ToDomainName(), registry.RegisterResource);

        foreach (var terrain in Enum.GetValues<TerrainType>())
        {
            RegisterOnceStrict(terrains, terrain.ToString(), registry.RegisterTerrain, "terrain");
        }

        foreach (var culture in config.Cultures)
        {
            RegisterOnceStrict(cultures, culture.Id, registry.RegisterCulture, "culture");
        }

        RegisterOnce(cultures, NoneCultureId, registry.RegisterCulture);

        if (config.Semantics is not null)
        {
            foreach (var generator in config.Semantics.Generators)
            {
                RegisterOnceStrict(generators, generator.Id, registry.RegisterGenerator, "generator");
                RegisterGeneratorSubtypes(generator, subtypes, registry);
            }
        }

        RegisterOnceStrict(eventCodes, RelationshipCreatedCode, registry.RegisterEventCode, "event code");
        RegisterOnceStrict(eventCodes, RelationshipCulledCode, registry.RegisterEventCode, "event code");
        RegisterOnceStrict(eventCodes, RelationshipStrengthChangedCode, registry.RegisterEventCode, "event code");
        RegisterOnceStrict(sourceCodes, ProcessSourceCode, registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(sourceCodes, CardSourceCode, registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(detectors, SettlementDepopulationDetector, registry.RegisterDetector, "detector");
        RegisterOnceStrict(detectors, PoliticalMonocultureDetector, registry.RegisterDetector, "detector");
        RegisterOnceStrict(detectors, NpcExtinctionDetector, registry.RegisterDetector, "detector");
        RegisterOnceStrict(reasonCodes, SettlementDepopulationReason, registry.RegisterReasonCode, "reason code");
        RegisterOnceStrict(reasonCodes, PoliticalMonocultureReason, registry.RegisterReasonCode, "reason code");
        RegisterOnceStrict(reasonCodes, NpcExtinctionReason, registry.RegisterReasonCode, "reason code");

        return registry;
    }

    private static void RegisterGeneratorSubtypes(
        SemanticGeneratorConfig generator,
        HashSet<string> subtypes,
        global::Engine.Domain.DomainIdRegistry registry)
    {
        RegisterSubtypeSet(generator.Target.Subtypes, subtypes, registry);
        foreach (var produced in generator.Produces)
        {
            if (!string.IsNullOrWhiteSpace(produced.Subtype))
            {
                RegisterOnce(subtypes, produced.Subtype, registry.RegisterSubtype);
            }

            RegisterSubtypeSet(produced.Subtypes, subtypes, registry);
            RegisterSubtypeSet(produced.Query?.Subtypes, subtypes, registry);
        }

        foreach (var secondaryTarget in generator.SecondaryTargets)
        {
            RegisterSubtypeSet(secondaryTarget.Query?.Subtypes, subtypes, registry);
        }
    }

    private static void RegisterSubtypeSet(
        IReadOnlySet<string>? values,
        HashSet<string> subtypes,
        global::Engine.Domain.DomainIdRegistry registry)
    {
        if (values is null)
        {
            return;
        }

        foreach (var subtype in values)
        {
            RegisterOnce(subtypes, subtype, registry.RegisterSubtype);
        }
    }

    private static void RegisterOnce<TId>(
        HashSet<string> seen,
        string stableName,
        Func<string, TId> register)
        where TId : struct
    {
        if (seen.Add(stableName))
        {
            _ = register(stableName);
        }
    }

    private static void RegisterOnceStrict<TId>(
        HashSet<string> seen,
        string stableName,
        Func<string, TId> register,
        string category)
        where TId : struct
    {
        if (!seen.Add(stableName))
        {
            throw new InvalidOperationException($"Duplicate {category} registration '{stableName}'.");
        }

        _ = register(stableName);
    }
}
