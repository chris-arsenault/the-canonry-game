namespace Engine.Domain.Graph;

using Engine.Behaviors;
using Engine.Gameplay;
using Engine.Kinds;
using Engine.Wiring;

public static partial class DefinitionGraphValidator
{
    public static IReadOnlyList<string> Validate(DefinitionGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var errors = new List<string>();

        ValidateKindNodes(graph, errors);
        ValidateRelationshipKeys(graph, errors);
        ValidateLifecycleNodes(graph, errors);
        ValidateGambitSet(graph, errors);
        ValidateGameplay(graph, errors);
        ValidateSemanticSet(graph, errors);
        ValidateGeneratorSet(graph, errors);

        return errors;
    }

    private static void ValidateKindNodes(DefinitionGraph graph, List<string> errors)
    {
        foreach (var (kind, kindNode) in graph.Kinds)
        {
            ValidateKindNode(kind, kindNode, graph, errors);
        }
    }

    private static void ValidateRelationshipKeys(DefinitionGraph graph, List<string> errors)
    {
        foreach (var (relationshipId, relationshipNode) in graph.Relationships)
        {
            if (relationshipNode.Id != relationshipId)
            {
                errors.Add($"Relationship node key '{relationshipId}' does not match node id '{relationshipNode.Id}'.");
            }

            foreach (var error in GameplayValidator.ValidateRelationshipMaintenance(
                relationshipId.Value,
                relationshipNode.SourceConfig.Maintenance))
            {
                errors.Add($"Relationship '{relationshipId.Value}' maintenance: {error.Message}");
            }
        }
    }

    private static void ValidateLifecycleNodes(DefinitionGraph graph, List<string> errors)
    {
        foreach (var (kind, lifecycleNode) in graph.Lifecycles)
        {
            ValidateLifecycleNode(kind, lifecycleNode, graph, errors);
        }
    }

    private static void ValidateGambitSet(DefinitionGraph graph, List<string> errors)
    {
        foreach (var gambit in graph.Gambits.Values)
        {
            ValidateBehaviorNode(
                "Gambit",
                gambit.Id,
                gambit.TargetKind,
                gambit.Reads,
                gambit.Writes,
                gambit.GoalEffects,
                gambit.ProviderSelectors,
                gambit.SupportRules,
                graph,
                errors);
        }
    }

    private static void ValidateBehaviorNode(
        string behaviorType,
        string behaviorId,
        EntityKind targetKind,
        IEnumerable<TypedDefinitionRef> reads,
        IEnumerable<TypedDefinitionRef> writes,
        IEnumerable<TypedDefinitionRef> goalEffects,
        IEnumerable<TypedDefinitionRef> providerSelectors,
        IEnumerable<DefinitionSupportRuleNode> supportRules,
        DefinitionGraph graph,
        List<string> errors)
    {
        if (!graph.Kinds.ContainsKey(targetKind))
        {
            errors.Add($"{behaviorType} '{behaviorId}' targets missing kind '{targetKind}'.");
        }

        ValidateRefs(behaviorId, reads, graph, errors);
        ValidateRefs(behaviorId, writes, graph, errors);
        ValidateRefs(behaviorId, goalEffects, graph, errors);
        ValidateRefs(behaviorId, providerSelectors, graph, errors);
        ValidateSupportRules(behaviorId, supportRules, graph, errors);
    }

    private static void ValidateSupportRules(
        string behaviorId,
        IEnumerable<DefinitionSupportRuleNode> supportRules,
        DefinitionGraph graph,
        List<string> errors)
    {
        foreach (var supportRule in supportRules)
        {
            ValidateRefs($"{behaviorId}.support", [supportRule.DesiredEffect], graph, errors);
        }
    }

    private static void ValidateGameplay(DefinitionGraph graph, List<string> errors)
    {
        foreach (var node in graph.GameplayNodes)
        {
            switch (node)
            {
                case DefinitionLowAltitudeVerbNode lowAltitudeVerb:
                    ValidateLowAltitudeVerb(lowAltitudeVerb, errors);
                    break;
                case DefinitionMapSignalNode mapSignals:
                    ValidateMapSignals(mapSignals, errors);
                    break;
                case DefinitionEffectNode effect:
                    ValidateEffects(effect, errors);
                    break;
            }
        }
    }

    private static void ValidateLowAltitudeVerb(DefinitionLowAltitudeVerbNode node, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(node.SourceConfig.Id))
        {
            errors.Add("Low-altitude gameplay verb must declare an id.");
        }
    }

    private static void ValidateMapSignals(DefinitionMapSignalNode node, List<string> errors)
    {
        if (node.Writes.Count == 0)
        {
            errors.Add($"Gameplay map signal node '{node.Id}' must declare writes.");
        }
    }

    private static void ValidateEffects(DefinitionEffectNode node, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(node.SourceConfig.Kind) ||
            string.IsNullOrWhiteSpace(node.SourceConfig.Attribute))
        {
            errors.Add($"Gameplay effect node '{node.Id}' must declare kind and attribute.");
        }
    }

    private static void ValidateSemanticSet(DefinitionGraph graph, List<string> errors)
    {
        ValidateKindHealthAttributeShape(graph, errors);
        ValidateKindTraits(graph, errors);
        ValidateTransferPreconditionGates(graph, errors);

        if (graph.SemanticFlows.Verbs.Count == 0 && graph.SemanticFlows.Flows.Count == 0)
        {
            return;
        }

        ValidateSemanticFlows(graph, errors);
        ValidateSemanticVerbParticipation(graph, errors);
        ValidateProjectedSemanticEdges(graph, errors);
    }

    private static void ValidateGeneratorSet(DefinitionGraph graph, List<string> errors)
    {
        foreach (var generator in graph.Generators.Values)
        {
            ValidateGeneratorTriggers(generator, errors);
            ValidateGeneratorProduces(generator, errors);
            ValidateGeneratorCultureInheritance(generator, errors);
        }
    }

    private static void ValidateGeneratorTriggers(DefinitionGeneratorNode generator, List<string> errors)
    {
        if (generator.Triggers.Count == 0)
        {
            errors.Add($"Generator '{generator.Id}' must declare at least one trigger.");
        }

        foreach (var trigger in generator.Triggers)
        {
            if (trigger.OnKind != generator.Target.Kind)
            {
                errors.Add(
                    $"Generator '{generator.Id}' target kind '{generator.Target.Kind}' does not match trigger kind '{trigger.OnKind}'.");
            }
        }
    }

    private static void ValidateGeneratorProduces(DefinitionGeneratorNode generator, List<string> errors)
    {
        var referencedRefs = GeneratorRelationshipRefs(generator);
        foreach (var produced in generator.Produces)
        {
            ValidateProducedRelationshipReference(generator, produced, referencedRefs, errors);
            ValidateOptionalProducedReferenceCount(generator, produced, errors);
        }
    }

    private static HashSet<string> GeneratorRelationshipRefs(DefinitionGeneratorNode generator) =>
        generator.Relationships
            .SelectMany(relationship => new[] { relationship.SourceRef, relationship.DestinationRef })
            .ToHashSet(StringComparer.Ordinal);

    private static void ValidateProducedRelationshipReference(
        DefinitionGeneratorNode generator,
        DefinitionProducedEntity produced,
        HashSet<string> referencedRefs,
        List<string> errors)
    {
        if (!referencedRefs.Contains(produced.Ref))
        {
            errors.Add(
                $"Generator '{generator.Id}' produces '{produced.Ref}' but no relationship references it.");
        }
    }

    private static void ValidateOptionalProducedReferenceCount(
        DefinitionGeneratorNode generator,
        DefinitionProducedEntity produced,
        List<string> errors)
    {
        if (produced.CreateChance >= 1f)
        {
            return;
        }

        var referenceCount = generator.Relationships.Count(relationship =>
            relationship.SourceRef == produced.Ref || relationship.DestinationRef == produced.Ref);
        if (referenceCount > 2)
        {
            errors.Add(
                $"Generator '{generator.Id}' optional produce '{produced.Ref}' is referenced by {referenceCount} relationships; keep optional anchors at 2 or fewer.");
        }
    }

    /// <summary>
    /// Every gambit that issues a <see cref="TransferAttributeMutation"/> must
    /// gate its source side at stock ≥ transfer amount. The transfer mutation is
    /// all-or-nothing; without the gate, a behavior would be allowed to fire when
    /// it can't actually move the authored rate, and the mutation would silently
    /// abort — making the authored economy dishonest. This encodes the soft-gate
    /// invariant so it's impossible to author a transfer without the matching
    /// precondition.
    /// </summary>
    private static void ValidateTransferPreconditionGates(DefinitionGraph graph, List<string> errors)
    {
        foreach (var (id, gambitNode) in graph.Gambits)
        {
            ValidateTransferPreconditionGate(id, "Gambit", gambitNode.SourceDefinition, errors);
        }
    }

    private static void ValidateTransferPreconditionGate(
        string id,
        string behaviorKind,
        BehaviorDefinition behavior,
        List<string> errors)
    {
        foreach (var mutation in behavior.Mutations)
        {
            if (mutation is not TransferAttributeMutation transfer) continue;

            var hasGate = PreconditionTreeHasSourceGate(
                behavior.Precondition,
                transfer.Source,
                transfer.SourceAttribute,
                transfer.Amount);
            if (!hasGate)
            {
                errors.Add(
                    $"{behaviorKind} '{id}' has a TransferAttributeMutation moving {transfer.Amount} from " +
                    $"{transfer.Source}.{transfer.SourceAttribute} but no precondition gates the source at ≥ {transfer.Amount}. " +
                    "Transfers are all-or-nothing; add a threshold precondition on the source stock.");
            }
        }
    }

    private static bool PreconditionTreeHasSourceGate(
        IPrecondition node,
        TransferEndpoint source,
        string attributeName,
        float amount)
    {
        switch (node)
        {
            case AndPrecondition and:
                foreach (var child in and.Children)
                {
                    if (PreconditionTreeHasSourceGate(child, source, attributeName, amount))
                        return true;
                }
                return false;
            case SelfAttributeThreshold selfThreshold
                when source == TransferEndpoint.Self &&
                     string.Equals(selfThreshold.AttributeName, attributeName, StringComparison.Ordinal) &&
                     selfThreshold.Min is { } min && min >= amount:
                return true;
            case TargetAttributeThreshold targetThreshold
                when source == TransferEndpoint.Target &&
                     string.Equals(targetThreshold.AttributeName, attributeName, StringComparison.Ordinal) &&
                     targetThreshold.Min is { } min && min >= amount:
                return true;
            default:
                return false;
        }
    }

    private static void ValidateKindHealthAttributeShape(DefinitionGraph graph, List<string> errors)
    {
        foreach (var (kind, kindNode) in graph.Kinds)
        {
            if (kindNode.Attributes.Count != 1)
            {
                errors.Add(
                    $"Kind '{kind}' declares {kindNode.Attributes.Count} attributes; exactly one health attribute is required.");
                continue;
            }

            if (kindNode.HealthAttribute is not DefinitionAttributeId healthId)
            {
                errors.Add(
                    $"Kind '{kind}' must declare HealthAttribute for its single mutable scalar.");
                continue;
            }

            if (!kindNode.Attributes.TryGetValue(healthId, out var healthAttr) ||
                !healthAttr.IsHealthAttribute)
            {
                errors.Add(
                    $"Kind '{kind}' declares health attribute '{healthId.Value}' but it is not the kind's single attribute.");
            }
        }
    }

    private static void ValidateKindTraits(DefinitionGraph graph, List<string> errors)
    {
        foreach (var (kind, kindNode) in graph.Kinds)
        {
            foreach (var (traitId, traitNode) in kindNode.Traits)
            {
                if (traitNode.Key.Kind != kind || traitNode.Key.TraitId != traitId)
                {
                    errors.Add($"Trait node key '{kind}.{traitId}' does not match node key '{traitNode.Key}'.");
                }

                if (string.IsNullOrWhiteSpace(traitNode.SourceConfig.Name))
                {
                    errors.Add($"Kind '{kind}' declares a trait with a blank name.");
                }

                if (traitNode.SourceConfig.AssignmentChance is < 0f or > 1f ||
                    float.IsNaN(traitNode.SourceConfig.AssignmentChance))
                {
                    errors.Add(
                        $"Trait '{traitNode.SourceConfig.Name}' assignment chance must be in the 0..1 range.");
                }
            }
        }
    }

    private static void ValidateSemanticVerbParticipation(DefinitionGraph graph, List<string> errors)
    {
        var verbsWithFlows = graph.SemanticFlows.Flows
            .Select(flow => flow.Verb.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var verb in graph.SemanticFlows.Verbs)
        {
            if (verb.IsFlowless)
            {
                continue;
            }

            if (!verbsWithFlows.Contains(verb.Id))
            {
                errors.Add(
                    $"Semantic verb '{verb.Id}' is declared but has no flows. Either author a flow for it or set IsFlowless = true.");
            }
        }
    }

    private static void ValidateGeneratorCultureInheritance(
        DefinitionGeneratorNode generator,
        List<string> errors)
    {
        var producesByRef = generator.Produces.ToDictionary(produced => produced.Ref, StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);

        foreach (var produced in generator.Produces)
        {
            if (HasGeneratorCultureCycle(produced.Ref, producesByRef, visited, visiting))
            {
                errors.Add(
                    $"Generator '{generator.Id}' has cyclic InheritCultureFrom chain involving '{produced.Ref}'.");
                return;
            }
        }
    }

    private static bool HasGeneratorCultureCycle(
        string currentRef,
        IReadOnlyDictionary<string, DefinitionProducedEntity> producesByRef,
        HashSet<string> visited,
        HashSet<string> visiting)
    {
        if (visiting.Contains(currentRef))
        {
            return true;
        }

        if (!visited.Add(currentRef))
        {
            return false;
        }

        visiting.Add(currentRef);

        if (producesByRef[currentRef].InheritCultureFrom is { } nextRef &&
            nextRef != "$target" &&
            producesByRef.ContainsKey(nextRef) &&
            HasGeneratorCultureCycle(nextRef, producesByRef, visited, visiting))
        {
            return true;
        }

        visiting.Remove(currentRef);
        return false;
    }

    public static void ValidateOrThrow(DefinitionGraph graph)
    {
        var errors = Validate(graph);
        if (errors.Count > 0)
        {
            throw new DefinitionGraphException(string.Join(Environment.NewLine, errors));
        }
    }

    private static void ValidateKindNode(
        EntityKind keyKind,
        DefinitionKindNode kindNode,
        DefinitionGraph graph,
        List<string> errors)
    {
        if (kindNode.Kind != keyKind)
        {
            errors.Add($"Kind node key '{keyKind}' does not match node kind '{kindNode.Kind}'.");
        }

        foreach (var (attributeId, attributeNode) in kindNode.Attributes)
        {
            ValidateAttributeNode(keyKind, attributeId, attributeNode, graph, errors);
        }

        ValidateKindHealthAttribute(keyKind, kindNode, errors);
    }

    private static void ValidateAttributeNode(
        EntityKind keyKind,
        DefinitionAttributeId attributeId,
        DefinitionAttributeNode attributeNode,
        DefinitionGraph graph,
        List<string> errors)
    {
        ValidateAttributeIdentity(keyKind, attributeId, attributeNode, errors);
    }

    private static void ValidateAttributeIdentity(
        EntityKind keyKind,
        DefinitionAttributeId attributeId,
        DefinitionAttributeNode attributeNode,
        List<string> errors)
    {
        if (attributeNode.Key.Kind != keyKind)
        {
            errors.Add($"Attribute '{attributeId}' is registered under '{keyKind}' but belongs to '{attributeNode.Key.Kind}'.");
        }

        if (attributeNode.Key.AttributeId != attributeId)
        {
            errors.Add($"Attribute node key mismatch for '{keyKind}.{attributeId}'.");
        }

        if (attributeNode.Shape != DefinitionAttributeShape.Scalar)
        {
            errors.Add($"Attribute '{attributeId}' on kind '{keyKind}' has non-scalar shape '{attributeNode.Shape}'.");
        }

        if (attributeNode.SourceConfig.Name != attributeId.Value)
        {
            errors.Add($"Attribute node id '{attributeId}' does not match source config name '{attributeNode.SourceConfig.Name}'.");
        }
    }

    private static void ValidateKindHealthAttribute(
        EntityKind keyKind,
        DefinitionKindNode kindNode,
        List<string> errors)
    {
        if (kindNode.HealthAttribute is not null &&
            !kindNode.Attributes.ContainsKey(kindNode.HealthAttribute.Value))
        {
            errors.Add($"Kind '{keyKind}' health attribute '{kindNode.HealthAttribute}' is not present in the kind attribute set.");
        }
    }

    private static void ValidateLifecycleNode(
        EntityKind keyKind,
        DefinitionLifecycleNode lifecycleNode,
        DefinitionGraph graph,
        List<string> errors)
    {
        if (lifecycleNode.Kind != keyKind)
        {
            errors.Add($"Lifecycle node key '{keyKind}' does not match node kind '{lifecycleNode.Kind}'.");
        }

        if (!graph.Kinds.TryGetValue(keyKind, out var kindNode))
        {
            errors.Add($"Lifecycle node references missing kind '{keyKind}'.");
            return;
        }

        if (!kindNode.Attributes.ContainsKey(lifecycleNode.HealthAttribute))
        {
            errors.Add($"Lifecycle for kind '{keyKind}' references missing health attribute '{lifecycleNode.HealthAttribute}'.");
        }

        if (kindNode.HealthAttribute is not null && kindNode.HealthAttribute != lifecycleNode.HealthAttribute)
        {
            errors.Add(
                $"Lifecycle for kind '{keyKind}' uses health attribute '{lifecycleNode.HealthAttribute}' but kind health is '{kindNode.HealthAttribute}'.");
        }

        if (lifecycleNode.SourceConfig.Health != lifecycleNode.HealthAttribute.Value)
        {
            errors.Add(
                $"Lifecycle node health id '{lifecycleNode.HealthAttribute}' does not match source config name '{lifecycleNode.SourceConfig.Health}'.");
        }
    }

    private static void ValidateRefs(
        string ownerId,
        IEnumerable<TypedDefinitionRef> refs,
        DefinitionGraph graph,
        List<string> errors)
    {
        foreach (var reference in refs)
        {
            switch (reference.ValueId)
            {
                case AttributeValueId attributeValue:
                    ValidateAttributeRef(ownerId, reference, attributeValue, graph, errors);
                    break;

            }

            if ((reference.Scope == RefScope.Related || reference.Scope == RefScope.Relational) &&
                reference.RelationshipId is null)
            {
                errors.Add($"Definition ref on '{ownerId}' uses scope '{reference.Scope}' without a relationship id.");
            }

            if (reference.RelationshipId is not null &&
                !graph.Relationships.ContainsKey(reference.RelationshipId.Value))
            {
                errors.Add($"Definition ref on '{ownerId}' uses undeclared relationship '{reference.RelationshipId}'.");
            }
        }
    }

    private static void ValidateAttributeRef(
        string ownerId,
        TypedDefinitionRef reference,
        AttributeValueId attributeValue,
        DefinitionGraph graph,
        List<string> errors)
    {
        if (reference.Kind is not EntityKind kind)
        {
            return;
        }

        if (!graph.Kinds.TryGetValue(kind, out var kindNode))
        {
            errors.Add($"Definition ref on '{ownerId}' uses attribute '{attributeValue.AttributeId}' with missing kind '{kind}'.");
            return;
        }

        var key = new DefinitionAttributeKey(kind, attributeValue.AttributeId);
        var shouldExistOnKind = reference.Scope is RefScope.Self or RefScope.Location;
        if (shouldExistOnKind && !kindNode.Attributes.ContainsKey(attributeValue.AttributeId))
        {
            errors.Add(
                $"Definition ref on '{ownerId}' uses attribute '{attributeValue.AttributeId}' on kind '{kind}' but the domain did not declare it.");
        }

        if (reference.Scope == RefScope.Self && !graph.Attributes.ContainsKey(key))
        {
            errors.Add(
                $"Definition ref on '{ownerId}' points to missing self attribute '{kind}.{attributeValue.AttributeId}'.");
        }
    }

}
