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
    private readonly List<DomainCompileDiagnostic> _diagnostics;
    private readonly HashSet<DomainCompileDiagnostic> _seenDiagnostics = [];

    public DomainGameplayCompilationContext(
        DomainConfig config,
        DomainIdRegistry ids,
        List<DomainCompileDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(diagnostics);
        _config = config;
        _ids = ids;
        _diagnostics = diagnostics;
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

    public int ResolveCultureId(string culture) => _ids.CultureIdOf(culture).Value;

    public int ResolveCultureSlotId(string? culture, string slot, string context)
    {
        if (!string.IsNullOrWhiteSpace(culture))
        {
            foreach (var candidate in _config.Cultures)
            {
                if (!string.Equals(candidate.Id, culture, StringComparison.Ordinal))
                {
                    continue;
                }

                var slotId = ResolveCultureSlotId(candidate, slot);
                if (slotId > 0)
                {
                    return slotId;
                }

                throw new InvalidOperationException($"{context} references unknown culture slot '{slot}' in culture '{culture}'.");
            }

            throw new InvalidOperationException($"{context} references unknown culture '{culture}'.");
        }

        var resolved = 0;
        var resolvedCulture = string.Empty;
        foreach (var candidate in _config.Cultures)
        {
            var slotId = ResolveCultureSlotId(candidate, slot);
            if (slotId <= 0)
            {
                continue;
            }

            if (resolved > 0)
            {
                throw new InvalidOperationException(
                    $"{context} references ambiguous culture slot '{slot}' in cultures '{resolvedCulture}' and '{candidate.Id}'.");
            }

            resolved = slotId;
            resolvedCulture = candidate.Id;
        }

        return resolved > 0
            ? resolved
            : throw new InvalidOperationException($"{context} references unknown culture slot '{slot}'.");
    }

    public int ResolveTraitId(string trait) => _ids.TraitIdOf(trait).Value;

    public int ResolveTagId(string tag) => _ids.TagIdOf(tag).Value;

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

    private void AddGeneratorRelationshipAffordances(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        SemanticGeneratorConfig generator)
    {
        var source = generator.Id;
        var targetKind = generator.Target.Kind.Value;
        foreach (var requirement in generator.Target.RequiredOutgoingRelationshipRequirements)
        {
            AddActionableRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, source, targetKind, requirement.RelationshipKind, RelationshipMatchDirectionCodes.Outgoing, requirement.MinimumStrength, AffordancePolarityCodes.Strengthen);
        }

        foreach (var requirement in generator.Target.RequiredIncomingRelationshipRequirements)
        {
            AddActionableRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, source, targetKind, requirement.RelationshipKind, RelationshipMatchDirectionCodes.Incoming, requirement.MinimumStrength, AffordancePolarityCodes.Strengthen);
        }

        foreach (var requirement in generator.Target.RequiredRelationshipRequirements)
        {
            AddActionableRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, source, targetKind, requirement.RelationshipKind, RelationshipMatchDirectionCodes.Either, requirement.MinimumStrength, AffordancePolarityCodes.Strengthen);
        }
    }

    private void AddGeneratorEffectAffordance(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        SemanticGeneratorConfig generator,
        ISemanticGeneratorEffect effect)
    {
        switch (effect)
        {
            case AdjustTargetRelationshipStrengthEffect adjust when adjust.Delta < 0f:
                AddActionableRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Generator, generator.Id, generator.Target.Kind.Value, adjust.RelationshipKind, RelationshipMatchDirectionCodes.Either, 1f, AffordancePolarityCodes.Strengthen, weight: -adjust.Delta);
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

    private static int ResolveCultureSlotId(CultureConfig culture, string slot)
    {
        var slotId = 0;
        foreach (var candidate in culture.Slots)
        {
            slotId++;
            if (string.Equals(candidate.Id, slot, StringComparison.Ordinal))
            {
                return slotId;
            }
        }

        return 0;
    }

    private void AddGambitRelationshipAffordances(
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

    private void AddGambitRelationshipRequirement(
        List<AffordanceDefinition> affordances,
        HashSet<AffordanceDefinition> seen,
        string source,
        string actorKind,
        string relationshipKind,
        int directionCode,
        float? minimumStrength)
    {
        if (minimumStrength is { } minimum)
        {
            AddActionableRelationshipStrengthAffordance(affordances, seen, AffordanceSourceKindCodes.Gambit, source, actorKind, relationshipKind, directionCode, minimum, AffordancePolarityCodes.Strengthen);
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

    private void AddActionableRelationshipStrengthAffordance(
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
        if (!HasRelationshipStrengthVerb(actorKind, relationshipKind, relationshipDirectionCode, polarityCode))
        {
            AddSuppressedRelationshipStrengthAffordanceDiagnostic(
                sourceKindCode,
                source,
                actorKind,
                relationshipKind,
                relationshipDirectionCode,
                polarityCode,
                threshold);
            return;
        }

        AddRelationshipStrengthAffordance(affordances, seen, sourceKindCode, source, actorKind, relationshipKind, relationshipDirectionCode, threshold, polarityCode, weight);
    }

    private bool HasRelationshipStrengthVerb(
        string actorKind,
        string relationshipKind,
        int relationshipDirectionCode,
        int polarityCode)
    {
        if (_config.Gameplay is null)
        {
            return false;
        }

        foreach (var verb in _config.Gameplay.LowAltitudeVerbs)
        {
            if (verb is RelationshipStrengthLowAltitudeVerbConfig relationshipVerb &&
                string.Equals(verb.Kind, actorKind, StringComparison.Ordinal) &&
                string.Equals(verb.RelationshipKind, relationshipKind, StringComparison.Ordinal) &&
                RelationshipDirectionsOverlap(RelationshipDirectionCode(verb.RelationshipDirection), relationshipDirectionCode) &&
                RelationshipStrengthPolarityCode(relationshipVerb.RelationshipStrengthDelta) == polarityCode)
            {
                return true;
            }
        }

        return false;
    }

    private void AddSuppressedRelationshipStrengthAffordanceDiagnostic(
        int sourceKindCode,
        string source,
        string actorKind,
        string relationshipKind,
        int relationshipDirectionCode,
        int polarityCode,
        float threshold)
    {
        var sourceKind = SourceKindName(sourceKindCode);
        var diagnostic = new DomainCompileDiagnostic(
            DomainCompileDiagnosticSeverity.Warning,
            DomainCompileDiagnosticCodes.SuppressedRelationshipStrengthAffordance,
            $"Suppressed relationship-strength affordance for {sourceKind} '{source}' on actor kind '{actorKind}' and relationship '{relationshipKind}' ({RelationshipDirectionName(relationshipDirectionCode)}, {PolarityName(polarityCode)}) because no matching low-altitude relationship-strength verb exists.",
            sourceKind,
            source,
            actorKind,
            relationshipKind,
            relationshipDirectionCode,
            polarityCode,
            threshold);
        if (_seenDiagnostics.Add(diagnostic))
        {
            _diagnostics.Add(diagnostic);
        }
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

    private static int RelationshipDirectionCode(LowAltitudeVerbRelationshipDirection direction) => direction switch
    {
        LowAltitudeVerbRelationshipDirection.Outgoing => RelationshipMatchDirectionCodes.Outgoing,
        LowAltitudeVerbRelationshipDirection.Incoming => RelationshipMatchDirectionCodes.Incoming,
        LowAltitudeVerbRelationshipDirection.Either => RelationshipMatchDirectionCodes.Either,
        _ => RelationshipMatchDirectionCodes.Either,
    };

    private static bool RelationshipDirectionsOverlap(int first, int second) =>
        first == RelationshipMatchDirectionCodes.Either ||
        second == RelationshipMatchDirectionCodes.Either ||
        first == second;

    private static int RelationshipStrengthPolarityCode(float delta) =>
        delta >= 0f ? AffordancePolarityCodes.Strengthen : AffordancePolarityCodes.Weaken;

    private static string SourceKindName(int sourceKindCode) => sourceKindCode switch
    {
        AffordanceSourceKindCodes.Generator => "generator",
        AffordanceSourceKindCodes.Gambit => "gambit",
        _ => "unknown",
    };

    private static string RelationshipDirectionName(int relationshipDirectionCode) => relationshipDirectionCode switch
    {
        RelationshipMatchDirectionCodes.Outgoing => "outgoing",
        RelationshipMatchDirectionCodes.Incoming => "incoming",
        RelationshipMatchDirectionCodes.Either => "either",
        _ => "unknown",
    };

    private static string PolarityName(int polarityCode) => polarityCode switch
    {
        AffordancePolarityCodes.Strengthen => "strengthen",
        AffordancePolarityCodes.Weaken => "weaken",
        _ => "none",
    };

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
