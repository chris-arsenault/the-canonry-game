namespace Engine.Domain.Graph;

using Engine.Kinds;
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

        ValidateSemanticFlows(graph, errors);
        ValidateProjectedSemanticEdges(graph, errors);

        return errors;
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

            var observedShape = attributeNode.SourceConfig.PlaneDefinition is null
                ? DefinitionAttributeShape.Scalar
                : DefinitionAttributeShape.Plane2D;
            if (observedShape != attributeNode.Shape)
            {
                errors.Add($"Attribute '{attributeId}' on kind '{keyKind}' has shape '{observedShape}' but node shape is '{attributeNode.Shape}'.");
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

            if (stock.RequiredFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Inflow) &&
                coverage.ScalarInflows == 0)
            {
                errors.Add(
                    $"Semantic stock '{stock.AttributeKey.Kind}.{stock.AttributeKey.AttributeId}' requires at least one inflow but none are declared.");
            }

            if (stock.RequiredFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Outflow) &&
                coverage.ScalarOutflows == 0)
            {
                errors.Add(
                    $"Semantic stock '{stock.AttributeKey.Kind}.{stock.AttributeKey.AttributeId}' requires at least one outflow but none are declared.");
            }

            if (stock.RequiredXFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Inflow) &&
                coverage.XInflows == 0)
            {
                errors.Add(
                    $"Semantic stock '{stock.AttributeKey.Kind}.{stock.AttributeKey.AttributeId}' requires at least one X-axis inflow but none are declared.");
            }

            if (stock.RequiredXFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Outflow) &&
                coverage.XOutflows == 0)
            {
                errors.Add(
                    $"Semantic stock '{stock.AttributeKey.Kind}.{stock.AttributeKey.AttributeId}' requires at least one X-axis outflow but none are declared.");
            }

            if (stock.RequiredYFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Inflow) &&
                coverage.YInflows == 0)
            {
                errors.Add(
                    $"Semantic stock '{stock.AttributeKey.Kind}.{stock.AttributeKey.AttributeId}' requires at least one Y-axis inflow but none are declared.");
            }

            if (stock.RequiredYFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Outflow) &&
                coverage.YOutflows == 0)
            {
                errors.Add(
                    $"Semantic stock '{stock.AttributeKey.Kind}.{stock.AttributeKey.AttributeId}' requires at least one Y-axis outflow but none are declared.");
            }
        }
    }

    private static void ValidateProjectedSemanticEdges(DefinitionGraph graph, List<string> errors)
    {
        var projectedEdges = SemanticStockGraphProjection.ProjectAttributeEdges(graph.SemanticFlows);
        var edgeSet = projectedEdges
            .Select(edge => (edge.SourceKey, edge.TargetKey))
            .ToHashSet();

        foreach (var edge in projectedEdges)
        {
            if (edge.SourceKey == edge.TargetKey)
            {
                continue;
            }

            if (!graph.Attributes.TryGetValue(edge.SourceKey, out var sourceAttribute) ||
                !graph.Attributes.TryGetValue(edge.TargetKey, out var targetAttribute) ||
                sourceAttribute.Shape != DefinitionAttributeShape.Scalar ||
                targetAttribute.Shape != DefinitionAttributeShape.Scalar)
            {
                continue;
            }

            if (!edgeSet.Contains((edge.TargetKey, edge.SourceKey)))
            {
                continue;
            }

            var pair = $"{edge.SourceKey.Kind}.{edge.SourceKey.AttributeId.Value} <-> {edge.TargetKey.Kind}.{edge.TargetKey.AttributeId.Value}";
            if (errors.Any(error => error.Contains(pair, StringComparison.Ordinal)))
            {
                continue;
            }

            errors.Add(
                $"Semantic stock graph contains a direct two-node reversal '{pair}'. Encode a longer path or an explicit modifier/coupling instead of opposite one-hop flows.");
        }
    }
}
