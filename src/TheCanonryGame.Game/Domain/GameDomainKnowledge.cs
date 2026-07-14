namespace TheCanonryGame.Game.Domain;

using Engine.Domain;
using Engine.Domain.Compilation;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Projection;

using TheCanonryGame.Game.Cards;

internal enum GameCapabilityKind
{
    Generator,
    Gambit,
}

internal sealed record GameCapabilityKnowledge(
    GameCapabilityKind Kind,
    int RuntimeId,
    string StableName,
    string DisplayName,
    string Description,
    string? PrimaryKindStableName,
    IReadOnlyList<CardProducedEntityDescriptor> Produces,
    IReadOnlyList<CardRelationshipChangeDescriptor> RelationshipChanges,
    IReadOnlyList<CardTerrainTransformDescriptor> TerrainTransforms,
    IReadOnlyList<CardDecisionModifierDescriptor> DecisionModifiers);

internal sealed record GameChronicleQuery(
    string Id,
    int ParentProducerKindCode,
    int ParentProducerId,
    int TriggerRelationshipKindId,
    int ChildProducerKindCode,
    int ChildProducerId);

internal sealed class GameDomainKnowledge
{
    private GameDomainKnowledge(
        IReadOnlyDictionary<int, GameCapabilityKnowledge> generatorsById,
        IReadOnlyDictionary<int, GameCapabilityKnowledge> gambitsById,
        IReadOnlyList<GameChronicleQuery> chronicleQueries,
        IReadOnlyDictionary<int, GameChronicleArchetypeKnowledge>? chronicleArchetypesById = null)
    {
        GeneratorsById = generatorsById;
        GambitsById = gambitsById;
        ChronicleQueries = chronicleQueries;
        ChronicleArchetypesById = chronicleArchetypesById ?? new Dictionary<int, GameChronicleArchetypeKnowledge>();
    }

    public static GameDomainKnowledge Empty { get; } =
        new(new Dictionary<int, GameCapabilityKnowledge>(), new Dictionary<int, GameCapabilityKnowledge>(), []);

    public IReadOnlyDictionary<int, GameChronicleArchetypeKnowledge> ChronicleArchetypesById { get; }

    public IReadOnlyDictionary<int, GameCapabilityKnowledge> GeneratorsById { get; }

    public IReadOnlyDictionary<int, GameCapabilityKnowledge> GambitsById { get; }

    public IReadOnlyList<GameChronicleQuery> ChronicleQueries { get; }

    public static GameDomainKnowledge FromCompiledDomain(
        CompiledDomain? compiledDomain,
        IProjectionSymbolCatalog symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        if (compiledDomain is null)
        {
            return Empty;
        }

        var stableNames = compiledDomain.Ids.Entries()
            .ToDictionary(entry => (entry.Category, entry.Id), entry => entry.StableName);
        return new GameDomainKnowledge(
            BuildGenerators(compiledDomain, symbols),
            BuildGambits(compiledDomain, symbols, stableNames),
            BuildChronicleQueries(compiledDomain),
            BuildChronicleArchetypes(compiledDomain));
    }

    private static GameChronicleQuery[] BuildChronicleQueries(CompiledDomain compiledDomain) =>
        compiledDomain.ChronicleQueries
            .Select(static query => new GameChronicleQuery(
                query.Id,
                query.Parent.ProducerKindCode,
                query.Parent.ProducerId,
                query.TriggerRelationshipKindId,
                query.Child.ProducerKindCode,
                query.Child.ProducerId))
            .ToArray();

    private static Dictionary<int, GameCapabilityKnowledge> BuildGenerators(
        CompiledDomain compiledDomain,
        IProjectionSymbolCatalog symbols)
    {
        var generators = new Dictionary<int, GameCapabilityKnowledge>();
        foreach (var generator in compiledDomain.DefinitionGraph.Generators.Values)
        {
            var produced = generator.Produces
                .Select(produce => ProducedDescriptor(compiledDomain, symbols, produce))
                .ToArray();
            var relationships = generator.Relationships
                .Select(relationship => RelationshipDescriptor(symbols, relationship))
                .ToArray();
            var primaryKind = produced.FirstOrDefault(produce => !produce.IsOptional)?.KindStableName ??
                produced.FirstOrDefault()?.KindStableName;
            generators[generator.RuntimeId] = new GameCapabilityKnowledge(
                GameCapabilityKind.Generator,
                generator.RuntimeId,
                generator.Id,
                DisplayName(symbols, ProjectionSymbolKind.Generator, generator.RuntimeId, generator.Id, generator.Name),
                generator.Description ?? string.Empty,
                primaryKind,
                produced,
                relationships,
                [],
                []);
        }

        return generators;
    }

    private static Dictionary<int, GameCapabilityKnowledge> BuildGambits(
        CompiledDomain compiledDomain,
        IProjectionSymbolCatalog symbols,
        IReadOnlyDictionary<(DomainIdCategory Category, int Id), string> stableNames)
    {
        var graphByRuntimeId = compiledDomain.DefinitionGraph.Gambits.Values
            .ToDictionary(
                gambit => compiledDomain.Ids.GambitIdOf(gambit.Id).Value,
                gambit => gambit);
        var modifiersByGambitId = compiledDomain.Programs.Gambits.TraitModifiers
            .GroupBy(modifier => modifier.GambitId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var gambits = new Dictionary<int, GameCapabilityKnowledge>();
        foreach (var gambit in compiledDomain.Programs.Gambits.Gambits)
        {
            if (!graphByRuntimeId.TryGetValue(gambit.GambitId, out var graphNode))
            {
                continue;
            }

            var relationships = gambit.Steps
                .Select(step => RelationshipDescriptor(symbols, stableNames, step))
                .Where(static descriptor => descriptor is not null)
                .Select(static descriptor => descriptor!)
                .ToArray();
            var terrainTransforms = gambit.Steps
                .Where(static step => step.Operation == BehaviorProgramOperation.SetTargetCoverageTerrain)
                .Select(step => TerrainTransformDescriptor(symbols, stableNames, step.A))
                .ToArray();
            var decisionModifiers = modifiersByGambitId.TryGetValue(gambit.GambitId, out var modifiers)
                ? modifiers.Select(modifier => DecisionModifierDescriptor(symbols, stableNames, modifier)).ToArray()
                : [];

            var primaryKind = StableName(
                stableNames,
                DomainIdCategory.Kind,
                gambit.KindId,
                graphNode.SourceDefinition.TargetKind.Value);
            gambits[gambit.GambitId] = new GameCapabilityKnowledge(
                GameCapabilityKind.Gambit,
                gambit.GambitId,
                graphNode.Id,
                DisplayName(symbols, ProjectionSymbolKind.Gambit, gambit.GambitId, graphNode.Id, graphNode.Name),
                graphNode.SourceDefinition.Description ?? string.Empty,
                primaryKind,
                [],
                relationships,
                terrainTransforms,
                decisionModifiers);
        }

        return gambits;
    }

    private static CardProducedEntityDescriptor ProducedDescriptor(
        CompiledDomain compiledDomain,
        IProjectionSymbolCatalog symbols,
        DefinitionProducedEntity produced)
    {
        var kindId = compiledDomain.DefinitionGraph.Kinds[produced.Kind].RuntimeId;
        var subtypeId = string.IsNullOrWhiteSpace(produced.Subtype)
            ? 0
            : compiledDomain.Ids.SubtypeIdOf(produced.Subtype).Value;
        var subtypeOptions = produced.Subtypes is null
            ? []
            : produced.Subtypes
                .Order(StringComparer.Ordinal)
                .Select(subtype => SubtypeOption(compiledDomain, symbols, subtype))
                .ToArray();
        return new CardProducedEntityDescriptor(
            produced.Ref,
            kindId,
            produced.Kind.Value,
            DisplayName(symbols, ProjectionSymbolKind.Kind, kindId, produced.Kind.Value),
            subtypeId,
            produced.Subtype,
            string.IsNullOrWhiteSpace(produced.Subtype)
                ? null
                : DisplayName(symbols, ProjectionSymbolKind.Subtype, subtypeId, produced.Subtype),
            subtypeOptions,
            produced.Role,
            produced.CreateChance,
            produced.CreateIfMissing);
    }

    private static CardSubtypeOptionDescriptor SubtypeOption(
        CompiledDomain compiledDomain,
        IProjectionSymbolCatalog symbols,
        string subtype)
    {
        var subtypeId = compiledDomain.Ids.SubtypeIdOf(subtype).Value;
        return new CardSubtypeOptionDescriptor(
            subtypeId,
            subtype,
            DisplayName(symbols, ProjectionSymbolKind.Subtype, subtypeId, subtype));
    }

    private static CardRelationshipChangeDescriptor RelationshipDescriptor(
        IProjectionSymbolCatalog symbols,
        DefinitionGeneratorRelationship relationship) =>
        new(
            CardRelationshipChangeKind.Create,
            relationship.SourceRef,
            relationship.DestinationRef,
            relationship.RelationshipRuntimeId,
            relationship.RelationshipId.Value,
            DisplayName(
                symbols,
                ProjectionSymbolKind.RelationshipKind,
                relationship.RelationshipRuntimeId,
                relationship.RelationshipId.Value),
            relationship.Strength);

    private static CardRelationshipChangeDescriptor? RelationshipDescriptor(
        IProjectionSymbolCatalog symbols,
        IReadOnlyDictionary<(DomainIdCategory Category, int Id), string> stableNames,
        BehaviorProgramStepRule step)
    {
        var changeKind = step.Operation switch
        {
            BehaviorProgramOperation.CreateEndpointRelationship or
                BehaviorProgramOperation.CreateFixedRelationship => CardRelationshipChangeKind.Create,
            BehaviorProgramOperation.RemoveEndpointRelationship => CardRelationshipChangeKind.Remove,
            BehaviorProgramOperation.SetEndpointRelationshipStrength => CardRelationshipChangeKind.SetStrength,
            _ => (CardRelationshipChangeKind?)null,
        };
        if (changeKind is null)
        {
            return null;
        }

        var stableName = StableName(stableNames, DomainIdCategory.RelationshipKind, step.A, $"relationship_{step.A}");
        return new CardRelationshipChangeDescriptor(
            changeKind.Value,
            EndpointLabel(step.Operation, step.B),
            EndpointLabel(step.Operation, step.C),
            step.A,
            stableName,
            DisplayName(symbols, ProjectionSymbolKind.RelationshipKind, step.A, stableName),
            step.X);
    }

    private static CardTerrainTransformDescriptor TerrainTransformDescriptor(
        IProjectionSymbolCatalog symbols,
        IReadOnlyDictionary<(DomainIdCategory Category, int Id), string> stableNames,
        int terrainId)
    {
        var stableName = StableName(stableNames, DomainIdCategory.Terrain, terrainId, $"terrain_{terrainId}");
        return new CardTerrainTransformDescriptor(
            terrainId,
            stableName,
            DisplayName(symbols, ProjectionSymbolKind.Terrain, terrainId, stableName));
    }

    private static CardDecisionModifierDescriptor DecisionModifierDescriptor(
        IProjectionSymbolCatalog symbols,
        IReadOnlyDictionary<(DomainIdCategory Category, int Id), string> stableNames,
        CompiledTraitGambitModifierRule modifier)
    {
        var stableName = StableName(stableNames, DomainIdCategory.Trait, modifier.TraitId, $"trait_{modifier.TraitId}");
        return new CardDecisionModifierDescriptor(
            modifier.TraitId,
            stableName,
            DisplayName(symbols, ProjectionSymbolKind.Trait, modifier.TraitId, stableName),
            modifier.ScoreDelta,
            modifier.SuccessChanceDelta);
    }

    private static string EndpointLabel(BehaviorProgramOperation operation, int endpointCode)
    {
        if (operation == BehaviorProgramOperation.CreateFixedRelationship)
        {
            return $"entity:{endpointCode}";
        }

        return endpointCode switch
        {
            (int)BehaviorProgramEndpoint.Self => "$self",
            (int)BehaviorProgramEndpoint.Target => "$target",
            _ => "$unknown",
        };
    }

    private static string StableName(
        IReadOnlyDictionary<(DomainIdCategory Category, int Id), string> stableNames,
        DomainIdCategory category,
        int id,
        string fallback) =>
        stableNames.TryGetValue((category, id), out var stableName) ? stableName : fallback;

    private static string DisplayName(
        IProjectionSymbolCatalog symbols,
        ProjectionSymbolKind kind,
        int id,
        string stableName,
        string? authoredName = null) =>
        symbols.ResolveDisplayName(
            kind,
            id,
            string.IsNullOrWhiteSpace(authoredName)
                ? ProjectionSymbolCatalog.ToDisplayName(stableName)
                : authoredName);

    private static Dictionary<int, GameChronicleArchetypeKnowledge> BuildChronicleArchetypes(
        Engine.Domain.Compilation.CompiledDomain compiledDomain)
    {
        var result = new Dictionary<int, GameChronicleArchetypeKnowledge>();
        var sharedBeats = compiledDomain.Config.Semantics?.ChronicleSharedBeats ?? [];
        foreach (var archetype in compiledDomain.Config.Semantics?.ChronicleArchetypes ?? [])
        {
            var roleRefs = new List<string> { archetype.PrimaryRoleRef };
            roleRefs.AddRange(archetype.SupportingRoles.Select(static role => role.Ref));
            roleRefs.AddRange(archetype.OpenRoles.Select(static role => role.Ref));
            var outcomeNarratives = new Dictionary<int, string>();
            var beatNarratives = new Dictionary<int, string>();
            foreach (var beat in ChronicleBeatSet.EffectiveBeats(archetype, sharedBeats))
            {
                if (string.IsNullOrWhiteSpace(beat.Narrative))
                {
                    continue;
                }

                beatNarratives[compiledDomain.Ids.ReasonCodeIdOf(beat.Id).Value] = beat.Narrative;
                if (beat.TerminalOutcomeId is not null)
                {
                    outcomeNarratives[compiledDomain.Ids.ReasonCodeIdOf(beat.TerminalOutcomeId).Value] = beat.Narrative;
                }
            }

            if (!string.IsNullOrWhiteSpace(archetype.FadeNarrative))
            {
                outcomeNarratives[compiledDomain.Ids.ReasonCodeIdOf(archetype.FadeOutcomeId).Value] = archetype.FadeNarrative;
            }

            result[compiledDomain.Ids.GeneratorIdOf(archetype.Id).Value] = new GameChronicleArchetypeKnowledge(
                archetype.TitleTemplate,
                roleRefs,
                beatNarratives,
                outcomeNarratives);
        }

        return result;
    }
}

internal sealed record GameChronicleArchetypeKnowledge(
    string? TitleTemplate,
    IReadOnlyList<string> RoleRefs,
    IReadOnlyDictionary<int, string> BeatNarrativesByCodeId,
    IReadOnlyDictionary<int, string> OutcomeNarrativesByCodeId);
