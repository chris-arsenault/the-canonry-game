namespace Engine.Domain.Compilation;

using Engine.Behaviors;
using Engine.Domain.Compilation.KernelData;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Gambits;
using Engine.Kinds;
using Engine.Wiring;
using Engine.WorldGen;

public static partial class DomainCompiler
{
    private static int StableRuntimeCode(string stableName)
    {
        unchecked
        {
            var hash = 23;
            foreach (var ch in stableName)
            {
                hash = (hash * 31) + ch;
            }

            return hash == int.MinValue ? int.MaxValue : Math.Abs(hash);
        }
    }

    private static List<DomainSymbolSeed> BuildSymbols(DomainConfig config, DomainIdRegistry ids)
    {
        var symbols = new DomainSymbolAccumulator();
        AddKindSymbols(config, ids, symbols);
        AddWorldSymbols(config, ids, symbols);
        AddCultureSymbols(config, ids, symbols);
        AddGeneratorSymbols(config, ids, symbols);
        AddBehaviorSymbols(config, ids, symbols);
        AddRegistrySymbols(ids, symbols);

        return symbols.ToList();
    }

    private static void AddKindSymbols(
        DomainConfig config,
        DomainIdRegistry ids,
        DomainSymbolAccumulator symbols)
    {
        foreach (var (kind, kindConfig) in config.Kinds)
        {
            symbols.Add(DomainSymbolKind.Kind, ids.KindIdOf(kind).Value, kind.Value, description: kindConfig.Description);
            symbols.Add(
                DomainSymbolKind.Attribute,
                ids.AttributeIdOf(kindConfig.Health.Name).Value,
                kindConfig.Health.Name,
                kindConfig.Health.DisplayName);

            foreach (var trait in kindConfig.Traits)
            {
                symbols.Add(
                    DomainSymbolKind.Trait,
                    ids.TraitIdOf(trait.Name).Value,
                    trait.Name,
                    trait.DisplayName,
                    trait.Description);
            }
        }
    }

    private static void AddWorldSymbols(
        DomainConfig config,
        DomainIdRegistry ids,
        DomainSymbolAccumulator symbols)
    {
        foreach (var relationship in config.Relationships)
        {
            symbols.Add(
                DomainSymbolKind.RelationshipKind,
                ids.RelationshipKindIdOf(relationship.Name).Value,
                relationship.Name);
        }

        foreach (var terrain in config.Terrains)
        {
            symbols.Add(DomainSymbolKind.Terrain, ids.TerrainIdOf(terrain.Name).Value, terrain.Name, terrain.DisplayName);
        }

        foreach (var structure in config.TravelStructures)
        {
            symbols.Add(
                DomainSymbolKind.TravelStructure,
                ids.TravelStructureIdOf(structure.Name).Value,
                structure.Name,
                structure.DisplayName);
        }
    }

    private static void AddCultureSymbols(
        DomainConfig config,
        DomainIdRegistry ids,
        DomainSymbolAccumulator symbols)
    {
        foreach (var culture in config.Cultures)
        {
            symbols.Add(DomainSymbolKind.Culture, ids.CultureIdOf(culture.Id).Value, culture.Id, culture.DisplayName);
        }
    }

    private static void AddGeneratorSymbols(
        DomainConfig config,
        DomainIdRegistry ids,
        DomainSymbolAccumulator symbols)
    {
        if (config.Semantics is null)
        {
            return;
        }

        foreach (var generator in config.Semantics.Generators)
        {
            symbols.Add(
                DomainSymbolKind.Generator,
                ids.GeneratorIdOf(generator.Id).Value,
                generator.Id,
                generator.Name,
                generator.Description);
        }
    }

    private static void AddBehaviorSymbols(
        DomainConfig config,
        DomainIdRegistry ids,
        DomainSymbolAccumulator symbols)
    {
        foreach (var verb in config.Gameplay?.LowAltitudeVerbs ?? [])
        {
            symbols.Add(
                DomainSymbolKind.Verb,
                ids.VerbIdOf(verb.Id).Value,
                verb.Id,
                verb.DisplayName);
        }

        foreach (var verb in config.MinorActors.SelectMany(rule => rule.Verbs))
        {
            symbols.Add(
                DomainSymbolKind.Verb,
                ids.VerbIdOf(verb.Id).Value,
                verb.Id,
                verb.DisplayName);
        }

        foreach (var gambit in config.Gambits)
        {
            symbols.Add(
                DomainSymbolKind.Gambit,
                ids.GambitIdOf(gambit.Id).Value,
                gambit.Id,
                gambit.Name,
                gambit.Description);
        }
    }

    private static void AddRegistrySymbols(DomainIdRegistry ids, DomainSymbolAccumulator symbols)
    {
        foreach (var entry in ids.Entries())
        {
            var kind = ToSymbolKind(entry.Category);
            if (kind is not null)
            {
                symbols.Add(kind.Value, entry.Id, entry.StableName);
            }
        }
    }

    private sealed class DomainSymbolAccumulator
    {
        private readonly HashSet<(DomainSymbolKind Kind, int Id)> _seen = [];
        private readonly List<DomainSymbolSeed> _symbols = [];

        public void Add(
            DomainSymbolKind kind,
            int id,
            string stableName,
            string? displayName = null,
            string? description = null,
            string? iconKey = null,
            string? colorKey = null)
        {
            if (id <= 0 || !_seen.Add((kind, id)))
            {
                return;
            }

            _symbols.Add(new DomainSymbolSeed(kind, id, stableName, displayName, description, iconKey, colorKey));
        }

        public List<DomainSymbolSeed> ToList() => _symbols;
    }

    private static DomainSymbolKind? ToSymbolKind(DomainIdCategory category) =>
        ToPrimarySymbolKind(category) ?? ToSecondarySymbolKind(category);

    private static DomainSymbolKind? ToPrimarySymbolKind(DomainIdCategory category) =>
        ToCoreSymbolKind(category) ?? ToRelationshipSymbolKind(category);

    private static DomainSymbolKind? ToCoreSymbolKind(DomainIdCategory category) => category switch
    {
        DomainIdCategory.Kind => DomainSymbolKind.Kind,
        DomainIdCategory.Attribute => DomainSymbolKind.Attribute,
        DomainIdCategory.Trait => DomainSymbolKind.Trait,
        DomainIdCategory.Verb => DomainSymbolKind.Verb,
        DomainIdCategory.Gambit => DomainSymbolKind.Gambit,
        DomainIdCategory.StructuralFact => DomainSymbolKind.StructuralFact,
        _ => null,
    };

    private static DomainSymbolKind? ToRelationshipSymbolKind(DomainIdCategory category) => category switch
    {
        DomainIdCategory.RelationshipKind => DomainSymbolKind.RelationshipKind,
        _ => null,
    };

    private static DomainSymbolKind? ToSecondarySymbolKind(DomainIdCategory category) => category switch
    {
        DomainIdCategory.Terrain => DomainSymbolKind.Terrain,
        DomainIdCategory.TravelStructure => DomainSymbolKind.TravelStructure,
        DomainIdCategory.Subtype => DomainSymbolKind.Subtype,
        DomainIdCategory.Culture => DomainSymbolKind.Culture,
        DomainIdCategory.Generator => DomainSymbolKind.Generator,
        DomainIdCategory.EventCode => DomainSymbolKind.EventCode,
        DomainIdCategory.ReasonCode => DomainSymbolKind.ReasonCode,
        DomainIdCategory.SourceCode => DomainSymbolKind.SourceCode,
        _ => null,
    };

    private static bool ContainsPrecondition<T>(IPrecondition precondition)
        where T : IPrecondition
    {
        if (precondition is T)
        {
            return true;
        }

        return precondition switch
        {
            AndPrecondition and => and.Children.Any(ContainsPrecondition<T>),
            OrPrecondition or => or.Children.Any(ContainsPrecondition<T>),
            NotPrecondition not => ContainsPrecondition<T>(not.Inner),
            _ => false,
        };
    }

    private static void RegisterBehaviorStructuralFacts(
        BehaviorDefinition behavior,
        HashSet<string> structuralFacts,
        DomainIdRegistry registry)
    {
        RegisterStructuralFacts(behavior.Reads, structuralFacts, registry);
        RegisterStructuralFacts(behavior.Writes, structuralFacts, registry);
        RegisterStructuralFacts(behavior.ProviderSelectors, structuralFacts, registry);
        RegisterPreconditionStructuralFacts(behavior.Precondition, structuralFacts, registry);
    }

    private static void RegisterStructuralFacts(
        IEnumerable<AttributeRef> references,
        HashSet<string> structuralFacts,
        DomainIdRegistry registry)
    {
        foreach (var reference in references)
        {
            RegisterStructuralFactRef(reference, structuralFacts, registry);
        }
    }

    private static void RegisterStructuralFactRef(
        AttributeRef reference,
        HashSet<string> structuralFacts,
        DomainIdRegistry registry)
    {
        if (reference.Scope == RefScope.Structural)
        {
            RegisterOnce(structuralFacts, reference.Name, registry.RegisterStructuralFact);
        }
    }

    private static void RegisterPreconditionStructuralFacts(
        IPrecondition precondition,
        HashSet<string> structuralFacts,
        DomainIdRegistry registry)
    {
        switch (precondition)
        {
            case AndPrecondition and:
                foreach (var child in and.Children)
                {
                    RegisterPreconditionStructuralFacts(child, structuralFacts, registry);
                }

                break;
            case OrPrecondition or:
                foreach (var child in or.Children)
                {
                    RegisterPreconditionStructuralFacts(child, structuralFacts, registry);
                }

                break;
            case NotPrecondition not:
                RegisterPreconditionStructuralFacts(not.Inner, structuralFacts, registry);
                break;
            case WithinPlaneDistancePrecondition distance:
                RegisterOnce(structuralFacts, distance.InRangeFactName, registry.RegisterStructuralFact);
                break;
        }
    }

    private static void RegisterGeneratorSubtypes(
        SemanticGeneratorConfig generator,
        HashSet<string> subtypes,
        DomainIdRegistry registry)
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
        DomainIdRegistry registry)
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
