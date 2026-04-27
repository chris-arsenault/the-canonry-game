namespace Engine.Domain.Graph;

using Engine.Kinds;
using Engine.Processes;
using Engine.Wiring;

public static class DefinitionGraphValidator
{
    public static IReadOnlyList<string> Validate(DefinitionGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var errors = new List<string>();

        foreach (var (kind, kindNode) in graph.Kinds)
        {
            ValidateKindNode(kind, kindNode, graph, errors);
        }

        foreach (var (resourceId, resourceNode) in graph.Resources)
        {
            if (resourceNode.Id != resourceId)
            {
                errors.Add($"Resource node key '{resourceId}' does not match node id '{resourceNode.Id}'.");
            }
        }

        foreach (var (relationshipId, relationshipNode) in graph.Relationships)
        {
            if (relationshipNode.Id != relationshipId)
            {
                errors.Add($"Relationship node key '{relationshipId}' does not match node id '{relationshipNode.Id}'.");
            }
        }

        foreach (var (pressureId, pressureNode) in graph.Pressures)
        {
            if (pressureNode.Id != pressureId)
            {
                errors.Add($"Pressure node key '{pressureId}' does not match node id '{pressureNode.Id}'.");
            }
        }

        foreach (var (kind, lifecycleNode) in graph.Lifecycles)
        {
            ValidateLifecycleNode(kind, lifecycleNode, graph, errors);
        }

        foreach (var process in graph.Processes.Values)
        {
            if (!graph.Kinds.ContainsKey(process.TargetKind))
            {
                errors.Add($"Process '{process.Id}' targets missing kind '{process.TargetKind}'.");
            }

            ValidateRefs(process.Id, process.Reads, graph, errors);
            ValidateRefs(process.Id, process.Writes, graph, errors);
            ValidateRefs(process.Id, process.GoalEffects, graph, errors);
            ValidateRefs(process.Id, process.ProviderSelectors, graph, errors);

            foreach (var supportRule in process.SupportRules)
            {
                ValidateRefs($"{process.Id}.support", [supportRule.DesiredEffect], graph, errors);
            }
        }

        foreach (var gambit in graph.Gambits.Values)
        {
            if (!graph.Kinds.ContainsKey(gambit.TargetKind))
            {
                errors.Add($"Gambit '{gambit.Id}' targets missing kind '{gambit.TargetKind}'.");
            }

            ValidateRefs(gambit.Id, gambit.Reads, graph, errors);
            ValidateRefs(gambit.Id, gambit.Writes, graph, errors);
            ValidateRefs(gambit.Id, gambit.GoalEffects, graph, errors);
            ValidateRefs(gambit.Id, gambit.ProviderSelectors, graph, errors);

            foreach (var supportRule in gambit.SupportRules)
            {
                ValidateRefs($"{gambit.Id}.support", [supportRule.DesiredEffect], graph, errors);
            }
        }

        ValidateSemanticFlows(graph, errors);
        ValidateSemanticVerbParticipation(graph, errors);
        ValidateResourceStockInflows(graph, errors);
        ValidateProjectedSemanticEdges(graph, errors);
        ValidateKindAttributeCategoryShape(graph, errors);
        ValidateHealthInflowFromHex(graph, errors);
        ValidateSelfSacrificeFromHealth(graph, errors);
        ValidateRpsRing(graph, errors);
        ValidateUntouchedAttributeOutflowToHex(graph, errors);
        ValidateMobilityAndDriftVerbs(graph, errors);
        ValidateNonHealthScalarDecay(graph, errors);
        ValidateTransferPreconditionGates(graph, errors);
        ValidateGeneratorSet(graph, errors);

        return errors;
    }

    private static void ValidateGeneratorSet(DefinitionGraph graph, List<string> errors)
    {
        foreach (var generator in graph.Generators.Values)
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

            var referencedRefs = generator.Relationships
                .SelectMany(relationship => new[] { relationship.SourceRef, relationship.DestinationRef })
                .ToHashSet(StringComparer.Ordinal);

            foreach (var produced in generator.Produces)
            {
                if (!referencedRefs.Contains(produced.Ref))
                {
                    errors.Add(
                        $"Generator '{generator.Id}' produces '{produced.Ref}' but no relationship references it.");
                }

                if (produced.CreateChance < 1f)
                {
                    var referenceCount = generator.Relationships.Count(relationship =>
                        relationship.SourceRef == produced.Ref || relationship.DestinationRef == produced.Ref);
                    if (referenceCount > 2)
                    {
                        errors.Add(
                            $"Generator '{generator.Id}' optional produce '{produced.Ref}' is referenced by {referenceCount} relationships; keep optional anchors at 2 or fewer.");
                    }
                }
            }

            ValidateGeneratorCultureInheritance(generator, errors);
        }
    }

    /// <summary>
    /// Every kind's Health attribute must have at least one inflow whose source ref
    /// is a hex resource. Hex resources are the canonical supply for Health.
    /// </summary>
    private static void ValidateHealthInflowFromHex(DefinitionGraph graph, List<string> errors)
    {
        foreach (var (kind, kindNode) in graph.Kinds)
        {
            if (kindNode.HealthAttribute is not DefinitionAttributeId healthId)
            {
                continue;
            }

            var hasHexInflow = graph.SemanticFlows.Flows.Any(flow =>
                flow.Role == SemanticFlowRole.Inflow &&
                flow.TargetStock.Id is AttributeStockId stockId &&
                stockId.Key.Kind == kind &&
                stockId.Key.AttributeId == healthId &&
                flow.SourceRefs.Any(src => src.ValueId is ResourceValueId));

            if (!hasHexInflow)
            {
                errors.Add(
                    $"Kind '{kind}' health attribute '{healthId.Value}' has no hex-resource inflow. Author an inflow whose source ref is a hex resource.");
            }
        }
    }

    /// <summary>
    /// Every kind must declare exactly one self-sacrifice flow whose source is the
    /// Health attribute and whose target is one of the other two attributes (Outward
    /// or Internal). This guarantees every kind has a last-resort H→X drain action.
    /// </summary>
    private static void ValidateSelfSacrificeFromHealth(DefinitionGraph graph, List<string> errors)
    {
        foreach (var (kind, kindNode) in graph.Kinds)
        {
            if (kindNode.HealthAttribute is not DefinitionAttributeId healthId)
            {
                continue;
            }

            var selfSacFlows = graph.SemanticFlows.Flows
                .Where(flow =>
                    flow.Role == SemanticFlowRole.Inflow &&
                    flow.TargetStock.Id is AttributeStockId targetId &&
                    targetId.Key.Kind == kind &&
                    !targetId.Key.AttributeId.Equals(healthId) &&
                    flow.SourceRefs.Any(src =>
                        src.Scope == RefScope.Self &&
                        src.Kind == kind &&
                        src.ValueId is AttributeValueId healthVal &&
                        healthVal.AttributeId.Equals(healthId)))
                .ToList();

            if (selfSacFlows.Count == 0)
            {
                errors.Add(
                    $"Kind '{kind}' has no self-sacrifice flow from Health ('{healthId.Value}'). Author one inflow on K.O or K.I whose source ref is Self(K, {healthId.Value}).");
            }
            else if (selfSacFlows.Count > 1)
            {
                errors.Add(
                    $"Kind '{kind}' declares {selfSacFlows.Count} self-sacrifice flows from Health; exactly one is required.");
            }
        }
    }

    /// <summary>
    /// RPS ring invariant. Given a domain-authored ring ordering:
    ///   For every K_N in the ring there must be at least one mass-flow projected edge
    ///   from K_N to K_{N+1} (the +1 edge) and at least one from K_N to K_{N+2} (the +2
    ///   edge). Endpoints must be non-Health attributes on each side. Authors are free
    ///   to choose which non-Health attribute on each side carries the edge — the ring
    ///   only constrains kind-pair connectivity, not attribute roles.
    /// </summary>
    private static void ValidateRpsRing(DefinitionGraph graph, List<string> errors)
    {
        if (graph.RingOrder.Count == 0)
        {
            return;
        }

        var kindsInGraph = new HashSet<EntityKind>(graph.Kinds.Keys);
        if (!graph.RingOrder.All(kindsInGraph.Contains))
        {
            return;
        }

        var projectedEdges = SemanticStockGraphProjection.ProjectAttributeEdges(graph.SemanticFlows)
            .Where(edge =>
                !edge.IsGateOnly &&
                edge.SourceId is AttributeStockId &&
                edge.TargetId is AttributeStockId)
            .Select(edge => (
                SourceKind: ((AttributeStockId)edge.SourceId).Key.Kind,
                SourceAttr: ((AttributeStockId)edge.SourceId).Key.AttributeId,
                TargetKind: ((AttributeStockId)edge.TargetId).Key.Kind,
                TargetAttr: ((AttributeStockId)edge.TargetId).Key.AttributeId))
            .ToHashSet();

        for (var n = 0; n < graph.RingOrder.Count; n++)
        {
            var kN = graph.RingOrder[n];
            var kNext = graph.RingOrder[(n + 1) % graph.RingOrder.Count];
            var kSkip = graph.RingOrder[(n + 2) % graph.RingOrder.Count];

            var nHealth = graph.Kinds[kN].HealthAttribute;
            var nextHealth = graph.Kinds[kNext].HealthAttribute;
            var skipHealth = graph.Kinds[kSkip].HealthAttribute;

            if (!HasNonHealthEdge(projectedEdges, kN, kNext, nHealth, nextHealth))
            {
                errors.Add(
                    $"RPS ring: missing +1 edge from {kN} to {kNext} (need at least one non-Health attribute pair).");
            }

            if (!HasNonHealthEdge(projectedEdges, kN, kSkip, nHealth, skipHealth))
            {
                errors.Add(
                    $"RPS ring: missing +2 edge from {kN} to {kSkip} (need at least one non-Health attribute pair).");
            }
        }
    }

    /// <summary>
    /// Every process that issues a <see cref="TransferAttributeMutation"/> must
    /// gate its source side at stock ≥ transfer amount. The transfer mutation is
    /// all-or-nothing; without the gate, a process would be allowed to fire when
    /// it can't actually move the authored rate, and the mutation would silently
    /// abort — making the authored economy dishonest. This encodes the soft-gate
    /// invariant so it's impossible to author a transfer without the matching
    /// precondition.
    /// </summary>
    private static void ValidateTransferPreconditionGates(DefinitionGraph graph, List<string> errors)
    {
        foreach (var (id, processNode) in graph.Processes)
        {
            ValidateTransferPreconditionGate(id, "Process", processNode.SourceDefinition, errors);
        }

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

    /// <summary>
    /// The semantic catalog declares engine_decay on every scalar stock; at runtime
    /// that shows up as <c>AttributeConfig.DriftRate</c>. Health attributes are
    /// drained separately by the lifecycle system, so they're exempt. Every other
    /// scalar attribute must declare a negative DriftRate — otherwise the agent
    /// can fill the stock once and never face renewed deficit, collapsing the
    /// process program's dynamic behavior.
    /// </summary>
    private static void ValidateNonHealthScalarDecay(DefinitionGraph graph, List<string> errors)
    {
        foreach (var (kind, kindNode) in graph.Kinds)
        {
            foreach (var (attrId, attrNode) in kindNode.Attributes)
            {
                if (attrNode.IsHealthAttribute) continue;
                if (attrNode.Shape != DefinitionAttributeShape.Scalar) continue;

                var driftRate = attrNode.SourceConfig.DriftRate;
                if (driftRate >= 0f)
                {
                    errors.Add(
                        $"Kind '{kind}' non-Health attribute '{attrId.Value}' has DriftRate={driftRate:F2}; " +
                        "every non-Health scalar stock must decay (the semantic catalog declares engine_decay for it). " +
                        "Set a negative DriftRate on the AttributeConfig.");
                }
            }
        }
    }

    /// <summary>
    /// Every entity kind must declare exactly one mobility verb (enablement: physical
    /// or semantic-plane movement into position) and exactly one drift verb (downtime
    /// random-step behavior on its assigned grid).
    /// </summary>
    private static void ValidateMobilityAndDriftVerbs(DefinitionGraph graph, List<string> errors)
    {
        foreach (var kind in graph.Kinds.Keys)
        {
            ValidateVerbRoleCount(graph, kind, SemanticVerbRole.Mobility, errors);
            ValidateVerbRoleCount(graph, kind, SemanticVerbRole.Drift, errors);
        }
    }

    private static void ValidateVerbRoleCount(
        DefinitionGraph graph,
        EntityKind kind,
        SemanticVerbRole role,
        List<string> errors)
    {
        var count = graph.SemanticFlows.Verbs
            .Count(verb =>
                verb.OwnerType == SemanticOwnerType.Entity &&
                verb.OwnerKind == kind &&
                verb.Role == role);

        if (count == 0)
        {
            errors.Add(
                $"Kind '{kind}' has no {role} verb. Declare exactly one entity-owned verb with Role = SemanticVerbRole.{role}.");
        }
        else if (count > 1)
        {
            errors.Add(
                $"Kind '{kind}' declares {count} {role} verbs; exactly one is required.");
        }
    }

    private static bool HasNonHealthEdge(
        HashSet<(EntityKind SourceKind, DefinitionAttributeId SourceAttr, EntityKind TargetKind, DefinitionAttributeId TargetAttr)> edges,
        EntityKind sourceKind,
        EntityKind targetKind,
        DefinitionAttributeId? sourceHealth,
        DefinitionAttributeId? targetHealth) =>
        edges.Any(edge =>
            edge.SourceKind == sourceKind &&
            edge.TargetKind == targetKind &&
            (sourceHealth is null || !edge.SourceAttr.Equals(sourceHealth.Value)) &&
            (targetHealth is null || !edge.TargetAttr.Equals(targetHealth.Value)));

    /// <summary>
    /// The attribute not involved in the self-sacrifice (the untouched one) must
    /// outflow to a hex resource. Provides the production side of the hex cycle.
    /// </summary>
    private static void ValidateUntouchedAttributeOutflowToHex(DefinitionGraph graph, List<string> errors)
    {
        foreach (var (kind, kindNode) in graph.Kinds)
        {
            if (kindNode.HealthAttribute is not DefinitionAttributeId healthId)
            {
                continue;
            }

            // Find the self-sac target (the attribute receiving from Health).
            var sacTarget = graph.SemanticFlows.Flows
                .Where(flow =>
                    flow.Role == SemanticFlowRole.Inflow &&
                    flow.TargetStock.Id is AttributeStockId target &&
                    target.Key.Kind == kind &&
                    target.Key.AttributeId != healthId &&
                    flow.SourceRefs.Any(src =>
                        src.Scope == RefScope.Self &&
                        src.Kind == kind &&
                        src.ValueId is AttributeValueId va &&
                        va.AttributeId == healthId))
                .Select(flow => ((AttributeStockId)flow.TargetStock.Id).Key.AttributeId)
                .FirstOrDefault();

            if (sacTarget == default)
            {
                continue; // self-sac invariant will report its own error
            }

            // Untouched = the other non-Health attribute.
            var untouched = kindNode.Attributes.Keys
                .FirstOrDefault(id => id != healthId && id != sacTarget);
            if (untouched == default)
            {
                continue;
            }

            var hasHexOutflow = graph.SemanticFlows.Flows.Any(flow =>
                flow.Role == SemanticFlowRole.Outflow &&
                flow.TargetStock.Id is AttributeStockId source &&
                source.Key.Kind == kind &&
                source.Key.AttributeId == untouched &&
                flow.SourceRefs.Any(src => src.ValueId is ResourceValueId));

            if (!hasHexOutflow)
            {
                errors.Add(
                    $"Kind '{kind}' untouched attribute '{untouched.Value}' has no outflow to a hex resource. Author an outflow on {kind}.{untouched.Value} whose source ref is a hex resource.");
            }
        }
    }

    private static void ValidateKindAttributeCategoryShape(DefinitionGraph graph, List<string> errors)
    {
        foreach (var (kind, kindNode) in graph.Kinds)
        {
            var attributes = kindNode.Attributes.Values.ToList();

            if (attributes.Count != 3)
            {
                errors.Add(
                    $"Kind '{kind}' declares {attributes.Count} attributes; exactly 3 are required (Health + one of Capacity/Projection + one of Need/Drive).");
                continue;
            }

            var healthCount = attributes.Count(a => a.SourceConfig.Category == Engine.Domain.Config.AttributeCategory.Health);
            var outwardCount = attributes.Count(a =>
                a.SourceConfig.Category == Engine.Domain.Config.AttributeCategory.Capacity ||
                a.SourceConfig.Category == Engine.Domain.Config.AttributeCategory.Projection);
            var internalCount = attributes.Count(a =>
                a.SourceConfig.Category == Engine.Domain.Config.AttributeCategory.Need ||
                a.SourceConfig.Category == Engine.Domain.Config.AttributeCategory.Drive);

            if (healthCount != 1)
            {
                errors.Add(
                    $"Kind '{kind}' has {healthCount} Health attributes; exactly one is required.");
            }

            if (outwardCount != 1)
            {
                errors.Add(
                    $"Kind '{kind}' has {outwardCount} outward attributes (Capacity or Projection); exactly one is required.");
            }

            if (internalCount != 1)
            {
                errors.Add(
                    $"Kind '{kind}' has {internalCount} internal attributes (Need or Drive); exactly one is required.");
            }

            if (kindNode.HealthAttribute is DefinitionAttributeId healthId)
            {
                if (kindNode.Attributes.TryGetValue(healthId, out var healthAttr) &&
                    healthAttr.SourceConfig.Category != Engine.Domain.Config.AttributeCategory.Health)
                {
                    errors.Add(
                        $"Kind '{kind}' declares health attribute '{healthId.Value}' but its Category is '{healthAttr.SourceConfig.Category}', not Health.");
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

    private static void ValidateResourceStockInflows(DefinitionGraph graph, List<string> errors)
    {
        var resourceStocks = graph.SemanticFlows.Stocks
            .Where(stock => stock.Id is ResourceStockId)
            .ToList();
        if (resourceStocks.Count == 0)
        {
            return;
        }

        var projected = SemanticStockGraphProjection.ProjectAttributeEdges(graph.SemanticFlows);
        var inflowTargets = projected
            .Select(edge => edge.TargetId)
            .ToHashSet();

        foreach (var stock in resourceStocks)
        {
            if (!inflowTargets.Contains(stock.Id))
            {
                errors.Add(
                    $"Resource stock '{stock.Id.Label}' has no inflow producer. Author an outflow on some entity stock whose source ref is this resource.");
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

            if (attributeNode.CriticalEffect is not null)
            {
                var targetKey = new DefinitionAttributeKey(keyKind, attributeNode.CriticalEffect.TargetAttributeId);
                if (!graph.Attributes.ContainsKey(targetKey))
                {
                    errors.Add($"Critical effect on '{keyKind}.{attributeId}' targets missing attribute '{attributeNode.CriticalEffect.TargetAttributeId}'.");
                }
            }

            if (attributeNode.LocationResourceDrift is not null &&
                !graph.Resources.ContainsKey(attributeNode.LocationResourceDrift.ResourceId))
            {
                errors.Add($"Location resource drift on '{keyKind}.{attributeId}' references undeclared resource '{attributeNode.LocationResourceDrift.ResourceId}'.");
            }
        }

        if (kindNode.HealthAttribute is not null &&
            !kindNode.Attributes.ContainsKey(kindNode.HealthAttribute.Value))
        {
            errors.Add($"Kind '{keyKind}' health attribute '{kindNode.HealthAttribute}' is not present in the kind attribute set.");
        }

        foreach (var standingDrive in kindNode.StandingDrives)
        {
            ValidateRefs($"{keyKind}.standing_drive", [standingDrive.DesiredEffect], graph, errors);
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

        if (lifecycleNode.SourceConfig.HealthAttribute != lifecycleNode.HealthAttribute.Value)
        {
            errors.Add(
                $"Lifecycle node health id '{lifecycleNode.HealthAttribute}' does not match source config name '{lifecycleNode.SourceConfig.HealthAttribute}'.");
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

                case ResourceValueId resourceValue when !graph.Resources.ContainsKey(resourceValue.ResourceId):
                    errors.Add($"Definition ref on '{ownerId}' uses undeclared resource '{resourceValue.ResourceId}'.");
                    break;

                case PressureValueId pressureValue when !graph.Pressures.ContainsKey(pressureValue.PressureId):
                    errors.Add($"Definition ref on '{ownerId}' uses undeclared pressure '{pressureValue.PressureId}'.");
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

    private static void ValidateSemanticFlows(DefinitionGraph graph, List<string> errors)
    {
        foreach (var stock in graph.SemanticFlows.Stocks)
        {
            var coverage = SemanticCoverageAnalyzer.Summarize(stock, graph.SemanticFlows);
            var stockLabel = stock.Id.Label;

            if (stock.RequiredFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Inflow) &&
                coverage.ScalarInflows == 0)
            {
                errors.Add(
                    $"Semantic stock '{stockLabel}' requires at least one inflow but none are declared.");
            }

            if (stock.RequiredFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Outflow) &&
                coverage.ScalarOutflows == 0)
            {
                errors.Add(
                    $"Semantic stock '{stockLabel}' requires at least one outflow but none are declared.");
            }

            if (stock.RequiredXFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Inflow) &&
                coverage.XInflows == 0)
            {
                errors.Add(
                    $"Semantic stock '{stockLabel}' requires at least one X-axis inflow but none are declared.");
            }

            if (stock.RequiredXFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Outflow) &&
                coverage.XOutflows == 0)
            {
                errors.Add(
                    $"Semantic stock '{stockLabel}' requires at least one X-axis outflow but none are declared.");
            }

            if (stock.RequiredYFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Inflow) &&
                coverage.YInflows == 0)
            {
                errors.Add(
                    $"Semantic stock '{stockLabel}' requires at least one Y-axis inflow but none are declared.");
            }

            if (stock.RequiredYFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Outflow) &&
                coverage.YOutflows == 0)
            {
                errors.Add(
                    $"Semantic stock '{stockLabel}' requires at least one Y-axis outflow but none are declared.");
            }

            ValidateStockDecayRules(stock, graph.SemanticFlows, stockLabel, errors);
            ValidatePlaneAxisBalance(stock, coverage, stockLabel, errors);
        }
    }

    private static void ValidatePlaneAxisBalance(
        SemanticStockNode stock,
        SemanticCoverageSummary coverage,
        string stockLabel,
        List<string> errors)
    {
        if (stock.StockClass != SemanticStockClass.Plane)
        {
            return;
        }

        if (coverage.XOutflows > 0 && coverage.XInflows == 0)
        {
            errors.Add(
                $"Plane stock '{stockLabel}' has X-axis outflows but no X-axis inflows; planes with outflow on an axis must have inflow on that axis.");
        }

        if (coverage.YOutflows > 0 && coverage.YInflows == 0)
        {
            errors.Add(
                $"Plane stock '{stockLabel}' has Y-axis outflows but no Y-axis inflows; planes with outflow on an axis must have inflow on that axis.");
        }
    }

    private static void ValidateStockDecayRules(
        SemanticStockNode stock,
        SemanticFlowGraph graph,
        string stockLabel,
        List<string> errors)
    {
        var relatedFlows = graph.Flows.Where(flow => flow.TargetStock == stock).ToList();
        var decayFlows = relatedFlows
            .Where(flow => flow.Mechanism == SemanticMechanism.Decay)
            .ToList();

        if (stock.Id is ResourceStockId)
        {
            if (decayFlows.Count > 0)
            {
                errors.Add(
                    $"Semantic stock '{stockLabel}' is a resource; resource stocks must not declare engine_decay flows.");
            }

            return;
        }

        if (stock.StockClass == SemanticStockClass.Plane)
        {
            if (decayFlows.Count > 0)
            {
                errors.Add(
                    $"Semantic stock '{stockLabel}' is a plane; plane stocks must not declare engine_decay flows.");
            }

            return;
        }

        var hasDecayOutflow = decayFlows.Any(flow => flow.Role == SemanticFlowRole.Outflow);
        if (!hasDecayOutflow)
        {
            errors.Add(
                $"Semantic stock '{stockLabel}' is scalar and must declare an engine_decay outflow.");
        }

        var nonOutflowDecay = decayFlows.Any(flow => flow.Role != SemanticFlowRole.Outflow);
        if (nonOutflowDecay)
        {
            errors.Add(
                $"Semantic stock '{stockLabel}' declares a decay flow that is not an outflow; decay only applies as an outflow.");
        }
    }

    private static void ValidateProjectedSemanticEdges(DefinitionGraph graph, List<string> errors)
    {
        var projectedEdges = SemanticStockGraphProjection.ProjectAttributeEdges(graph.SemanticFlows);
        var massFlowPairs = projectedEdges
            .Where(edge => !edge.IsGateOnly)
            .Select(edge => (edge.SourceId, edge.TargetId))
            .ToHashSet();

        foreach (var edge in projectedEdges)
        {
            if (edge.IsGateOnly || edge.SourceId == edge.TargetId)
            {
                continue;
            }

            if (edge.SourceId is not AttributeStockId sourceAttributeId ||
                edge.TargetId is not AttributeStockId targetAttributeId)
            {
                continue;
            }

            if (!graph.Attributes.TryGetValue(sourceAttributeId.Key, out var sourceAttribute) ||
                !graph.Attributes.TryGetValue(targetAttributeId.Key, out var targetAttribute) ||
                sourceAttribute.Shape != DefinitionAttributeShape.Scalar ||
                targetAttribute.Shape != DefinitionAttributeShape.Scalar)
            {
                continue;
            }

            if (!massFlowPairs.Contains((edge.TargetId, edge.SourceId)))
            {
                continue;
            }

            var pair = $"{sourceAttributeId.Label} <-> {targetAttributeId.Label}";
            if (errors.Any(error => error.Contains(pair, StringComparison.Ordinal)))
            {
                continue;
            }

            errors.Add(
                $"Semantic stock graph contains a direct two-node reversal '{pair}'. Encode a longer path or an explicit modifier/coupling instead of opposite one-hop flows.");
        }
    }
}
