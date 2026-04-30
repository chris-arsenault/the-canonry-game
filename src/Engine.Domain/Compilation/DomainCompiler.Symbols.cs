namespace Engine.Domain.Compilation;

using Engine.Domain.Compilation.KernelData;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Gambits;
using Engine.Kinds;
using Engine.Processes;
using Engine.Resources;
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
        AddDegeneracySymbols(config, ids, symbols);
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
            foreach (var attribute in kindConfig.Attributes)
            {
                symbols.Add(
                    DomainSymbolKind.Attribute,
                    ids.AttributeIdOf(attribute.Name).Value,
                    attribute.Name,
                    attribute.DisplayName);
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

        foreach (var fact in config.RelationshipFacts)
        {
            symbols.Add(
                DomainSymbolKind.RelationshipFact,
                ids.RelationshipFactIdOf(fact.Name).Value,
                fact.Name,
                fact.DisplayName,
                fact.Description);
        }

        foreach (var pressure in config.Pressures)
        {
            symbols.Add(DomainSymbolKind.Pressure, ids.PressureIdOf(pressure.Name).Value, pressure.Name);
        }

        foreach (var resource in config.Resources)
        {
            symbols.Add(DomainSymbolKind.Resource, ids.ResourceIdOf(resource.Name).Value, resource.Name, resource.DisplayName);
        }

        foreach (var terrain in config.Terrains)
        {
            symbols.Add(DomainSymbolKind.Terrain, ids.TerrainIdOf(terrain.Name).Value, terrain.Name, terrain.DisplayName);
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

    private static void AddDegeneracySymbols(
        DomainConfig config,
        DomainIdRegistry ids,
        DomainSymbolAccumulator symbols)
    {
        foreach (var detector in config.Degeneracy.Detectors)
        {
            symbols.Add(
                DomainSymbolKind.Detector,
                ids.DetectorIdOf(detector.Id).Value,
                detector.Id,
                detector.DisplayName,
                detector.Description);
            symbols.Add(DomainSymbolKind.ReasonCode, ids.ReasonCodeIdOf(detector.ReasonCode).Value, detector.ReasonCode);
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
        foreach (var process in config.Processes)
        {
            symbols.Add(
                DomainSymbolKind.Process,
                ids.ProcessIdOf(process.Id).Value,
                process.Id,
                process.Name);
        }

        foreach (var verb in config.LowAltitudeVerbs)
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
                gambit.Name);
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
        DomainIdCategory.Process => DomainSymbolKind.Process,
        DomainIdCategory.Verb => DomainSymbolKind.Verb,
        DomainIdCategory.Gambit => DomainSymbolKind.Gambit,
        DomainIdCategory.StructuralFact => DomainSymbolKind.StructuralFact,
        DomainIdCategory.Pressure => DomainSymbolKind.Pressure,
        DomainIdCategory.Resource => DomainSymbolKind.Resource,
        _ => null,
    };

    private static DomainSymbolKind? ToRelationshipSymbolKind(DomainIdCategory category) => category switch
    {
        DomainIdCategory.RelationshipKind => DomainSymbolKind.RelationshipKind,
        DomainIdCategory.RelationshipFact => DomainSymbolKind.RelationshipFact,
        _ => null,
    };

    private static DomainSymbolKind? ToSecondarySymbolKind(DomainIdCategory category) => category switch
    {
        DomainIdCategory.Terrain => DomainSymbolKind.Terrain,
        DomainIdCategory.Subtype => DomainSymbolKind.Subtype,
        DomainIdCategory.Culture => DomainSymbolKind.Culture,
        DomainIdCategory.Generator => DomainSymbolKind.Generator,
        DomainIdCategory.Detector => DomainSymbolKind.Detector,
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

    private static void RegisterProcessStructuralFacts(
        BehaviorDefinition process,
        HashSet<string> structuralFacts,
        DomainIdRegistry registry)
    {
        RegisterStructuralFacts(process.Reads, structuralFacts, registry);
        RegisterStructuralFacts(process.Writes, structuralFacts, registry);
        RegisterStructuralFacts(process.GoalEffects, structuralFacts, registry);
        RegisterStructuralFacts(process.ProviderSelectors, structuralFacts, registry);
        RegisterPreconditionStructuralFacts(process.Precondition, structuralFacts, registry);
        foreach (var supportRule in process.SupportRules)
        {
            RegisterStructuralFactRef(supportRule.DesiredEffect, structuralFacts, registry);
        }
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
