namespace Engine.Domain.Graph;

using Engine.Domain.Config;
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
                ValidateRefs(
                    $"{process.Id}.support",
                    [supportRule.DesiredEffect],
                    graph,
                    errors);
            }
        }

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

            var metadata = DefinitionAttributeCatalog.Get(attributeId);
            if (!metadata.AllowedKinds.Contains(keyKind))
            {
                errors.Add($"Attribute '{attributeId}' is not declared for kind '{keyKind}'.");
            }

            var observedShape = attributeNode.SourceConfig.PlaneDefinition is null
                ? DefinitionAttributeShape.Scalar
                : DefinitionAttributeShape.Plane2D;
            if (observedShape != metadata.Shape)
            {
                errors.Add($"Attribute '{attributeId}' on kind '{keyKind}' has shape '{observedShape}' but should be '{metadata.Shape}'.");
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
        var metadata = DefinitionAttributeCatalog.Get(attributeValue.AttributeId);

        if (reference.Kind is EntityKind kind)
        {
            if (!metadata.AllowedKinds.Contains(kind))
            {
                errors.Add(
                    $"Definition ref on '{ownerId}' uses attribute '{attributeValue.AttributeId}' with incompatible kind '{kind}'.");
            }

            var key = new DefinitionAttributeKey(kind, attributeValue.AttributeId);
            if (graph.Kinds.TryGetValue(kind, out var kindNode) &&
                !kindNode.Attributes.ContainsKey(attributeValue.AttributeId) &&
                (reference.Scope == RefScope.Self || reference.Scope == RefScope.Location))
            {
                errors.Add(
                    $"Definition ref on '{ownerId}' uses attribute '{attributeValue.AttributeId}' on kind '{kind}' but the domain did not declare it.");
            }

            if (!graph.Attributes.ContainsKey(key) &&
                reference.Scope == RefScope.Self)
            {
                errors.Add(
                    $"Definition ref on '{ownerId}' points to missing self attribute '{kind}.{attributeValue.AttributeId}'.");
            }
        }
    }
}
