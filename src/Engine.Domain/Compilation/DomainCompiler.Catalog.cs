namespace Engine.Domain.Compilation;

using Engine.Domain.Config;

public static partial class DomainCompiler
{
    public static DomainIdRegistry BuildCatalog(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var state = new CatalogRegistrationState();
        RegisterKindCatalog(config, state);
        RegisterWorldCatalog(config, state);
        RegisterCultureCatalog(config, state);
        RegisterGeneratorCatalog(config, state);
        RegisterBehaviorCatalog(config, state);
        RegisterRuntimeCatalog(state);
        RegisterDegeneracyCatalog(config, state);

        return state.Registry;
    }

    private static void RegisterKindCatalog(DomainConfig config, CatalogRegistrationState state)
    {
        foreach (var (kind, kindConfig) in config.Kinds)
        {
            RegisterOnceStrict(state.Kinds, kind.Value, state.Registry.RegisterKind, "kind");
            RegisterSubtypes(kindConfig.Subtypes, state);
            RegisterHealth(kindConfig.Health, state);
            RegisterTraits(kindConfig.Traits, state);
        }
    }

    private static void RegisterSubtypes(IEnumerable<string> subtypes, CatalogRegistrationState state)
    {
        foreach (var subtype in subtypes)
        {
            RegisterOnce(state.Subtypes, subtype, state.Registry.RegisterSubtype);
        }
    }

    private static void RegisterHealth(HealthConfig health, CatalogRegistrationState state)
    {
        RegisterOnce(state.Attributes, health.Name, state.Registry.RegisterAttribute);
    }

    private static void RegisterTraits(IEnumerable<TraitConfig> traits, CatalogRegistrationState state)
    {
        foreach (var trait in traits)
        {
            RegisterOnceStrict(state.Traits, trait.Name, state.Registry.RegisterTrait, "trait");
        }
    }

    private static void RegisterWorldCatalog(DomainConfig config, CatalogRegistrationState state)
    {
        RegisterRelationshipCatalog(config, state);
        RegisterTerrainCatalog(config, state);
    }

    private static void RegisterRelationshipCatalog(DomainConfig config, CatalogRegistrationState state)
    {
        foreach (var relationship in config.Relationships)
        {
            RegisterOnceStrict(state.RelationshipKinds, relationship.Name, state.Registry.RegisterRelationshipKind, "relationship kind");
        }

    }

    private static void RegisterTerrainCatalog(DomainConfig config, CatalogRegistrationState state)
    {
        foreach (var terrain in config.Terrains)
        {
            RegisterOnceStrict(state.Terrains, terrain.Name, state.Registry.RegisterTerrain, "terrain");
        }
    }

    private static void RegisterCultureCatalog(DomainConfig config, CatalogRegistrationState state)
    {
        foreach (var culture in config.Cultures)
        {
            RegisterOnceStrict(state.Cultures, culture.Id, state.Registry.RegisterCulture, "culture");
        }

        RegisterOnce(state.Cultures, DomainRuntimeNames.NoneCultureId, state.Registry.RegisterCulture);
    }

    private static void RegisterGeneratorCatalog(DomainConfig config, CatalogRegistrationState state)
    {
        if (config.Semantics is null)
        {
            return;
        }

        foreach (var generator in config.Semantics.Generators)
        {
            RegisterOnceStrict(state.Generators, generator.Id, state.Registry.RegisterGenerator, "generator");
            RegisterGeneratorSubtypes(generator, state.Subtypes, state.Registry);
        }
    }

    private static void RegisterBehaviorCatalog(DomainConfig config, CatalogRegistrationState state)
    {
        foreach (var verb in config.Gameplay?.LowAltitudeVerbs ?? [])
        {
            RegisterOnceStrict(state.Verbs, verb.Id, state.Registry.RegisterVerb, "verb");
        }

        foreach (var gambit in config.Gambits)
        {
            RegisterOnceStrict(state.Gambits, gambit.Id, state.Registry.RegisterGambit, "gambit");
            RegisterBehaviorStructuralFacts(gambit, state.StructuralFacts, state.Registry);
        }
    }

    private static void RegisterRuntimeCatalog(CatalogRegistrationState state)
    {
        RegisterEventCodes(state);
        RegisterSourceCodes(state);
    }

    private static void RegisterEventCodes(CatalogRegistrationState state)
    {
        RegisterOnceStrict(state.EventCodes, DomainRuntimeNames.RelationshipCreatedCode, state.Registry.RegisterEventCode, "event code");
        RegisterOnceStrict(state.EventCodes, DomainRuntimeNames.RelationshipCulledCode, state.Registry.RegisterEventCode, "event code");
        RegisterOnceStrict(
            state.EventCodes,
            DomainRuntimeNames.RelationshipStrengthChangedCode,
            state.Registry.RegisterEventCode,
            "event code");
    }

    private static void RegisterSourceCodes(CatalogRegistrationState state)
    {
        RegisterOnceStrict(state.SourceCodes, DomainRuntimeNames.GambitSourceCode, state.Registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(state.SourceCodes, DomainRuntimeNames.UpkeepSourceCode, state.Registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(state.SourceCodes, DomainRuntimeNames.StartupSourceCode, state.Registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(state.SourceCodes, DomainRuntimeNames.RelationshipSourceCode, state.Registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(state.SourceCodes, DomainRuntimeNames.GeneratorSourceCode, state.Registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(state.SourceCodes, DomainRuntimeNames.DegeneracySourceCode, state.Registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(state.SourceCodes, DomainRuntimeNames.MapControlSourceCode, state.Registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(state.SourceCodes, DomainRuntimeNames.GameplaySourceCode, state.Registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(state.SourceCodes, DomainRuntimeNames.GameplayEffectsSourceCode, state.Registry.RegisterSourceCode, "source code");
    }

    private static void RegisterDegeneracyCatalog(DomainConfig config, CatalogRegistrationState state)
    {
        foreach (var detector in config.Degeneracy.Detectors)
        {
            RegisterOnceStrict(state.Detectors, detector.Id, state.Registry.RegisterDetector, "detector");
            RegisterOnceStrict(state.ReasonCodes, detector.ReasonCode, state.Registry.RegisterReasonCode, "reason code");
        }
    }

    private sealed class CatalogRegistrationState
    {
        public DomainIdRegistry Registry { get; } = new();
        public HashSet<string> Kinds { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Attributes { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Traits { get; } = new(StringComparer.Ordinal);
        public HashSet<string> StructuralFacts { get; } = new(StringComparer.Ordinal);
        public HashSet<string> RelationshipKinds { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Terrains { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Subtypes { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Cultures { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Generators { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Verbs { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Gambits { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Detectors { get; } = new(StringComparer.Ordinal);
        public HashSet<string> EventCodes { get; } = new(StringComparer.Ordinal);
        public HashSet<string> SourceCodes { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ReasonCodes { get; } = new(StringComparer.Ordinal);
    }
}
