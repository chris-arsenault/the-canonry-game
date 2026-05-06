namespace Engine.Domain.Compilation;

using Engine.Behaviors;
using Engine.Constants;
using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Graph;

internal sealed class DomainGameplayCompilationContext : IGameplayCompilationContext
{
    private readonly DomainConfig _config;
    private readonly DomainIdRegistry _ids;

    public DomainGameplayCompilationContext(DomainConfig config, DomainIdRegistry ids)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(ids);
        _config = config;
        _ids = ids;
    }

    public int ResolveKindId(string kind, string context) =>
        _ids.KindIdOf(ResolveKind(kind, context)).Value;

    public int PrimaryPlaneCodeForKind(string kind, string context) =>
        _config.Kinds[ResolveKind(kind, context)].PrimaryPlane == PrimaryPlane.Semantic
            ? PlaneCodes.Semantic
            : PlaneCodes.Physical;

    public int ResolveVerbId(string verb) => _ids.VerbIdOf(verb).Value;

    public int ResolveGeneratorId(string generator) => _ids.GeneratorIdOf(generator).Value;

    public int ResolveGambitId(string gambit, string context) => _ids.GambitIdOf(gambit).Value;

    public int ResolveTerrainId(string terrain) => _ids.TerrainIdOf(terrain).Value;

    public int ResolveRelationshipKindId(string relationship) =>
        _ids.RelationshipKindIdOf(relationship).Value;

    public int ResolveAttributeId(string attribute) => _ids.AttributeIdOf(attribute).Value;

    public int ResolveTraitId(string trait) => _ids.TraitIdOf(trait).Value;

    public bool TerrainExists(string terrain) =>
        _config.Terrains.Any(candidate => string.Equals(candidate.Name, terrain, StringComparison.Ordinal));

    public bool RelationshipExists(string relationship) =>
        _config.Relationships.Any(candidate => string.Equals(candidate.Name, relationship, StringComparison.Ordinal));

    public bool KindHasAttribute(string kind, string attribute)
    {
        var kindId = ResolveKind(kind, "Gameplay kind");
        return string.Equals(_config.Kinds[kindId].Health.Name, attribute, StringComparison.Ordinal);
    }

    public IReadOnlyList<AffordanceDefinition> Affordances()
    {
        var affordances = new List<AffordanceDefinition>();
        var seen = new HashSet<AffordanceDefinition>();
        AddGeneratorAffordances(affordances, seen);
        AddGambitAffordances(affordances, seen);
        return affordances;
    }

    public IReadOnlyList<TraitOperationDefinition> TraitOperations()
    {
        var operations = new List<TraitOperationDefinition>();
        foreach (var (kind, kindConfig) in _config.Kinds)
        {
            foreach (var trait in kindConfig.Traits)
            {
                foreach (var operation in trait.Operations)
                {
                    operations.Add(new TraitOperationDefinition(kind.Value, trait.Name, operation));
                }
            }
        }

        return operations;
    }

    private void AddGeneratorAffordances(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen)
    {
        if (_config.Semantics is null)
        {
            return;
        }

        foreach (var generator in _config.Semantics.Generators)
        {
            var source = generator.Id;
            var targetKind = generator.Target.Kind.Value;
            AddGeneratorRelationshipAffordances(affordances, seen, generator);
            AddGeneratorTerrainAffordances(affordances, seen, generator);
            foreach (var binding in generator.SecondaryTargets)
            {
                if (binding.Query?.AdjacentToTarget != true)
                {
                    continue;
                }

                var bindingKind = binding.Kind.Value;
                AddActionablePresenceAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, source, targetKind, bindingKind, "physical");
                AddActionablePresenceAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, source, bindingKind, targetKind, "physical");
            }

            foreach (var effect in generator.Effects)
            {
                AddGeneratorEffectAffordance(affordances, seen, generator, effect);
            }
        }
    }

    private static void AddGeneratorRelationshipAffordances(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        SemanticGeneratorConfig generator)
    {
        var source = generator.Id;
        var targetKind = generator.Target.Kind.Value;
        foreach (var relationship in RelationshipsOrEmpty(generator.Target.RequiredOutgoingRelationships))
        {
            AddRelationshipTargetPresenceAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, source, targetKind, relationship, RelationshipMatchDirectionCodes.Outgoing);
        }

        foreach (var relationship in RelationshipsOrEmpty(generator.Target.RequiredIncomingRelationships))
        {
            AddRelationshipTargetPresenceAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, source, targetKind, relationship, RelationshipMatchDirectionCodes.Incoming);
        }

        foreach (var relationship in RelationshipsOrEmpty(generator.Target.RequiredRelationships))
        {
            AddRelationshipTargetPresenceAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, source, targetKind, relationship, RelationshipMatchDirectionCodes.Either);
        }

        foreach (var requirement in generator.Target.RequiredOutgoingRelationshipRequirements)
        {
            AddRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, source, targetKind, requirement.RelationshipKind, RelationshipMatchDirectionCodes.Outgoing, requirement.MinimumStrength, AffordancePolarityCodes.Strengthen);
        }

        foreach (var requirement in generator.Target.RequiredIncomingRelationshipRequirements)
        {
            AddRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, source, targetKind, requirement.RelationshipKind, RelationshipMatchDirectionCodes.Incoming, requirement.MinimumStrength, AffordancePolarityCodes.Strengthen);
        }

        foreach (var requirement in generator.Target.RequiredRelationshipRequirements)
        {
            AddRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, source, targetKind, requirement.RelationshipKind, RelationshipMatchDirectionCodes.Either, requirement.MinimumStrength, AffordancePolarityCodes.Strengthen);
        }
    }

    private static void AddGeneratorTerrainAffordances(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        SemanticGeneratorConfig generator)
    {
        foreach (var requirement in generator.Target.TerrainCountRequirements)
        {
            AddAffordance(
                affordances,
                seen,
                new AffordanceDefinition(
                    AffordanceSourceKindCodes.Generator,
                    generator.Id,
                    LowAltitudeAffordanceShapeCodes.TerrainPresence,
                    generator.Target.Kind.Value,
                    Plane: "physical",
                    Terrain: requirement.TerrainName));
        }
    }

    private static void AddGeneratorEffectAffordance(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        SemanticGeneratorConfig generator,
        ISemanticGeneratorEffect effect)
    {
        switch (effect)
        {
            case AdjustTargetRelationshipStrengthEffect adjust when adjust.Delta < 0f:
                AddRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, generator.Id, generator.Target.Kind.Value, adjust.RelationshipKind, RelationshipMatchDirectionCodes.Either, 1f, AffordancePolarityCodes.Strengthen, weight: -adjust.Delta);
                break;
            case RemoveTargetRelationshipsEffect remove:
                AddRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, generator.Id, generator.Target.Kind.Value, remove.RelationshipKind, RelationshipMatchDirectionCodes.Either, 0f, AffordancePolarityCodes.Weaken);
                break;
        }
    }

    private void AddGambitAffordances(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen)
    {
        foreach (var gambit in _config.Gambits)
        {
            var source = gambit.Id;
            var actorKind = gambit.TargetKind.Value;
            AddGambitDistanceAffordances(affordances, seen, gambit, source, actorKind);
            AddGambitRelationshipAffordances(affordances, seen, gambit.Precondition, source, actorKind);
            AddGambitInfluenceAffordances(affordances, seen, gambit.Precondition, source, actorKind);

            foreach (var mutation in gambit.Mutations)
            {
                AddGambitMutationAffordance(affordances, seen, source, actorKind, mutation);
            }
        }
    }

    private void AddGambitDistanceAffordances(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        Engine.Gambits.GambitDefinition gambit,
        string source,
        string actorKind)
    {
        var targetKind = GambitTargetKind(gambit);
        if (targetKind is null)
        {
            return;
        }

        foreach (var distance in PreconditionLeaves<WithinPlaneDistancePrecondition>(gambit.Precondition, includeNegated: false))
        {
            var plane = PlaneName(distance.Plane);
            AddActionablePresenceAffordance(affordances, seen, AffordanceSourceKindCodes.Gambit, source, actorKind, targetKind, plane);
            AddActionablePresenceAffordance(affordances, seen, AffordanceSourceKindCodes.Gambit, source, targetKind, actorKind, plane);
        }
    }

    private void AddActionablePresenceAffordance(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        int sourceKindCode,
        string source,
        string actorKind,
        string candidateKind,
        string plane)
    {
        if (HasMovementVerbTargetingKind(actorKind, candidateKind, plane))
        {
            AddPresenceAffordance(affordances, seen, sourceKindCode, source, actorKind, candidateKind, plane);
        }
    }

    private bool HasMovementVerbTargetingKind(string actorKind, string candidateKind, string plane)
    {
        if (_config.Gameplay is null)
        {
            return false;
        }

        foreach (var verb in _config.Gameplay.LowAltitudeVerbs)
        {
            if (verb is MovementLowAltitudeVerbConfig &&
                verb.TargetMode == LowAltitudeVerbTargetMode.EntityKind &&
                string.Equals(verb.Kind, actorKind, StringComparison.Ordinal) &&
                string.Equals(verb.TargetKind, candidateKind, StringComparison.Ordinal) &&
                string.Equals(VerbPlane(verb), plane, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private string VerbPlane(LowAltitudeVerbConfig verb) =>
        verb.Plane ?? PlaneName(_config.Kinds[ResolveKind(verb.Kind, $"Low-altitude verb '{verb.Id}' kind")].PrimaryPlane);

    private static void AddGambitRelationshipAffordances(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        IPrecondition precondition,
        string source,
        string actorKind)
    {
        foreach (var relationship in PreconditionLeaves<SelfHasRelationshipToTarget>(precondition, includeNegated: false))
        {
            AddGambitRelationshipRequirement(affordances, seen, source, actorKind, relationship.RelationshipKind, RelationshipMatchDirectionCodes.Outgoing, relationship.MinStrength);
        }

        foreach (var relationship in PreconditionLeaves<TargetHasRelationshipToSelf>(precondition, includeNegated: false))
        {
            AddGambitRelationshipRequirement(affordances, seen, source, actorKind, relationship.RelationshipKind, RelationshipMatchDirectionCodes.Incoming, relationship.MinStrength);
        }

        foreach (var relationship in PreconditionLeaves<SelfHasOutgoingRelationship>(precondition, includeNegated: false))
        {
            AddGambitRelationshipRequirement(affordances, seen, source, actorKind, relationship.RelationshipKind, RelationshipMatchDirectionCodes.Outgoing, relationship.MinStrength);
        }
    }

    private static void AddGambitRelationshipRequirement(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        string source,
        string actorKind,
        string relationshipKind,
        int directionCode,
        float? minimumStrength)
    {
        AddRelationshipTargetPresenceAffordance(affordances, seen, AffordanceSourceKindCodes.Gambit, source, actorKind, relationshipKind, directionCode);
        if (minimumStrength is { } minimum)
        {
            AddRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Gambit, source, actorKind, relationshipKind, directionCode, minimum, AffordancePolarityCodes.Strengthen);
        }
    }

    private static void AddGambitInfluenceAffordances(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        IPrecondition precondition,
        string source,
        string actorKind)
    {
        foreach (var contested in PreconditionLeaves<ContestedInfluencePrecondition>(precondition, includeNegated: false))
        {
            AddInfluenceAffordance(affordances, seen, AffordanceSourceKindCodes.Gambit, source, actorKind, contested.Plane, contested.SelfMinimum);
        }

        foreach (var shared in PreconditionLeaves<SharedInfluencePrecondition>(precondition, includeNegated: false))
        {
            AddInfluenceAffordance(affordances, seen, AffordanceSourceKindCodes.Gambit, source, actorKind, shared.Plane, shared.SelfMinimum);
        }
    }

    private static void AddGambitMutationAffordance(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        string source,
        string actorKind,
        IMutation mutation)
    {
        switch (mutation)
        {
            case SetContextRelationshipStrengthMutation set:
                AddRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Gambit, source, actorKind, set.RelationshipKind, RelationshipMatchDirectionCodes.Either, set.Strength, AffordancePolarityCodes.Strengthen);
                break;
            case RemoveContextRelationshipMutation remove:
                AddRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Gambit, source, actorKind, remove.RelationshipKind, RelationshipMatchDirectionCodes.Either, 0f, AffordancePolarityCodes.Weaken);
                break;
        }
    }

    private static void AddPresenceAffordance(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        int sourceKindCode,
        string source,
        string actorKind,
        string candidateKind,
        string? plane = null)
    {
        AddAffordance(
            affordances,
            seen,
            new AffordanceDefinition(
                sourceKindCode,
                source,
                LowAltitudeAffordanceShapeCodes.PresenceNearEntity,
                actorKind,
                Plane: plane,
                CandidateKind: candidateKind));
    }

    private static void AddRelationshipTargetPresenceAffordance(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        int sourceKindCode,
        string source,
        string actorKind,
        string relationshipKind,
        int relationshipDirectionCode)
    {
        AddAffordance(
            affordances,
            seen,
            new AffordanceDefinition(
                sourceKindCode,
                source,
                LowAltitudeAffordanceShapeCodes.PresenceNearRelationshipTarget,
                actorKind,
                RelationshipKind: relationshipKind,
                RelationshipDirectionCode: relationshipDirectionCode));
    }

    private static void AddRelationshipStrengthAffordance(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        int sourceKindCode,
        string source,
        string actorKind,
        string relationshipKind,
        int relationshipDirectionCode,
        float threshold,
        int polarityCode,
        float weight = 1f)
    {
        AddAffordance(
            affordances,
            seen,
            new AffordanceDefinition(
                sourceKindCode,
                source,
                LowAltitudeAffordanceShapeCodes.RelationshipStrengthChange,
                actorKind,
                RelationshipKind: relationshipKind,
                RelationshipDirectionCode: relationshipDirectionCode,
                PolarityCode: polarityCode,
                Threshold: threshold,
                Weight: weight));
    }

    private static void AddInfluenceAffordance(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        int sourceKindCode,
        string source,
        string actorKind,
        PrimaryPlane plane,
        float threshold)
    {
        AddAffordance(
            affordances,
            seen,
            new AffordanceDefinition(
                sourceKindCode,
                source,
                LowAltitudeAffordanceShapeCodes.InfluenceChange,
                actorKind,
                Plane: PlaneName(plane),
                OperationCode: ProgramOpcodeCodes.AddOwnedCellScalar,
                PolarityCode: AffordancePolarityCodes.Increase,
                Threshold: threshold));
    }

    private static void AddAffordance(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        AffordanceDefinition affordance)
    {
        if (seen.Add(affordance))
        {
            affordances.Add(affordance);
        }
    }

    private static string? GambitTargetKind(Engine.Gambits.GambitDefinition gambit)
    {
        foreach (var selector in gambit.ProviderSelectors)
        {
            if (selector.Kind is { } kind)
            {
                return kind.Value;
            }
        }

        return null;
    }

    private static IEnumerable<T> PreconditionLeaves<T>(IPrecondition precondition, bool includeNegated)
        where T : IPrecondition
    {
        foreach (var (leaf, negated) in WalkPreconditions(precondition, negated: false))
        {
            if ((includeNegated || !negated) && leaf is T typed)
            {
                yield return typed;
            }
        }
    }

    private static IEnumerable<(IPrecondition Leaf, bool Negated)> WalkPreconditions(IPrecondition precondition, bool negated)
    {
        switch (precondition)
        {
            case AndPrecondition and:
                foreach (var child in and.Children)
                {
                    foreach (var leaf in WalkPreconditions(child, negated))
                    {
                        yield return leaf;
                    }
                }

                break;
            case OrPrecondition or:
                foreach (var child in or.Children)
                {
                    foreach (var leaf in WalkPreconditions(child, negated))
                    {
                        yield return leaf;
                    }
                }

                break;
            case NotPrecondition not:
                foreach (var leaf in WalkPreconditions(not.Inner, !negated))
                {
                    yield return leaf;
                }

                break;
            default:
                yield return (precondition, negated);
                break;
        }
    }

    private static string PlaneName(PrimaryPlane plane) =>
        plane == PrimaryPlane.Semantic ? "semantic" : "physical";

    private static IEnumerable<string> RelationshipsOrEmpty(IReadOnlySet<string>? relationships) =>
        relationships is null ? Array.Empty<string>() : relationships;

    private EntityKind ResolveKind(string kindName, string context)
    {
        foreach (var kind in _config.Kinds.Keys)
        {
            if (string.Equals(kind.Value, kindName, StringComparison.Ordinal))
            {
                return kind;
            }
        }

        throw new InvalidOperationException($"{context} references unknown kind '{kindName}'.");
    }
}
