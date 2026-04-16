namespace Engine.Domain.Graph;

using Engine.Domain.Config;
using Engine.Kinds;
using Engine.Processes;
using Engine.Resources;
using Engine.Wiring;

public static class DefinitionGraphBuilder
{
    public static DefinitionGraph Build(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var kindNodes = new Dictionary<EntityKind, DefinitionKindNode>();
        var attributeIndex = new Dictionary<DefinitionAttributeKey, DefinitionAttributeNode>();
        foreach (var (kind, kindConfig) in config.Kinds)
        {
            var kindNode = BuildKindNode(kind, kindConfig);
            kindNodes[kind] = kindNode;

            foreach (var attributeNode in kindNode.Attributes.Values)
            {
                attributeIndex[attributeNode.Key] = attributeNode;
            }
        }

        var resourceNodes = BuildResourceNodes(config.Resources);
        var relationshipNodes = BuildRelationshipNodes(config.Relationships);
        var pressureNodes = BuildPressureNodes(config.Pressures);
        var lifecycleNodes = BuildLifecycleNodes(config, kindNodes);

        var processNodes = new Dictionary<string, DefinitionProcessNode>(StringComparer.Ordinal);
        foreach (var process in config.Processes)
        {
            if (!kindNodes.ContainsKey(process.TargetKind))
            {
                throw new DefinitionGraphException(
                    $"Process '{process.Id}' targets kind '{process.TargetKind}' which is not present in the domain kinds.");
            }

            processNodes[process.Id] = BuildProcessNode(process);
        }

        var graph = new DefinitionGraph
        {
            Kinds = kindNodes,
            Processes = processNodes,
            Resources = resourceNodes,
            Relationships = relationshipNodes,
            Pressures = pressureNodes,
            Lifecycles = lifecycleNodes,
            Attributes = attributeIndex,
        };

        DefinitionGraphValidator.ValidateOrThrow(graph);
        return graph;
    }

    private static DefinitionKindNode BuildKindNode(EntityKind kind, KindConfig kindConfig)
    {
        var attributes = new Dictionary<DefinitionAttributeId, DefinitionAttributeNode>();
        DefinitionAttributeId? healthAttribute = null;

        foreach (var attribute in kindConfig.Attributes)
        {
            var attributeId = ParseAttributeId(attribute.Name, kind, "attribute");
            var metadata = DefinitionAttributeCatalog.Get(attributeId);

            var observedShape = attribute.PlaneDefinition is null
                ? DefinitionAttributeShape.Scalar
                : DefinitionAttributeShape.Plane2D;
            if (observedShape != metadata.Shape)
            {
                throw new DefinitionGraphException(
                    $"Attribute '{attribute.Name}' on kind '{kind}' has shape '{observedShape}' but the declared shape is '{metadata.Shape}'.");
            }

            attributes[attributeId] = new DefinitionAttributeNode
            {
                Key = new DefinitionAttributeKey(kind, attributeId),
                Metadata = metadata,
                SourceConfig = attribute,
                IsHealthAttribute = false,
                CriticalEffect = BuildCriticalEffect(attribute.CriticalEffect, kind),
                LocationResourceDrift = BuildLocationResourceDrift(attribute),
            };
        }

        if (kindConfig.HealthAttribute is not null)
        {
            var healthId = ParseAttributeId(kindConfig.HealthAttribute, kind, "health attribute");
            if (!attributes.TryGetValue(healthId, out var healthNode))
            {
                throw new DefinitionGraphException(
                    $"Kind '{kind}' health attribute '{kindConfig.HealthAttribute}' is not present in its declared attributes.");
            }

            attributes[healthId] = new DefinitionAttributeNode
            {
                Key = healthNode.Key,
                Metadata = healthNode.Metadata,
                SourceConfig = healthNode.SourceConfig,
                CriticalEffect = healthNode.CriticalEffect,
                LocationResourceDrift = healthNode.LocationResourceDrift,
                IsHealthAttribute = true,
            };
            healthAttribute = healthId;
        }

        var standingDrives = kindConfig.StandingDrives
            .Select(drive => new DefinitionStandingDriveNode
            {
                DesiredEffect = ConvertRef(drive.DesiredEffect),
                BaseUrgency = drive.BaseUrgency,
            })
            .ToList();

        return new DefinitionKindNode
        {
            Kind = kind,
            Attributes = attributes,
            HealthAttribute = healthAttribute,
            StandingDrives = standingDrives,
        };
    }

    private static Dictionary<DefinitionResourceId, DefinitionResourceNode> BuildResourceNodes(
        IReadOnlyList<ResourceConfig> resources)
    {
        var nodes = new Dictionary<DefinitionResourceId, DefinitionResourceNode>();
        foreach (var resource in resources)
        {
            if (!DefinitionResourceCatalog.TryParse(resource.Name, out var resourceId))
            {
                throw new DefinitionGraphException(
                    $"Domain declares unknown resource '{resource.Name}'. Add it to DefinitionResourceCatalog first.");
            }

            nodes[resourceId] = new DefinitionResourceNode
            {
                Id = resourceId,
                Metadata = DefinitionResourceCatalog.Get(resourceId),
                SourceConfig = resource,
            };
        }

        return nodes;
    }

    private static Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode> BuildRelationshipNodes(
        IReadOnlyList<RelationshipConfig> relationships)
    {
        var nodes = new Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode>();
        foreach (var relationship in relationships)
        {
            if (!DefinitionRelationshipCatalog.TryParse(relationship.Name, out var relationshipId))
            {
                throw new DefinitionGraphException(
                    $"Domain declares unknown relationship '{relationship.Name}'. Add it to DefinitionRelationshipCatalog first.");
            }

            nodes[relationshipId] = new DefinitionRelationshipNode
            {
                Id = relationshipId,
                Metadata = DefinitionRelationshipCatalog.Get(relationshipId),
                SourceConfig = relationship,
            };
        }

        return nodes;
    }

    private static Dictionary<DefinitionPressureId, DefinitionPressureNode> BuildPressureNodes(
        IReadOnlyList<PressureConfig> pressures)
    {
        var nodes = new Dictionary<DefinitionPressureId, DefinitionPressureNode>();
        foreach (var pressure in pressures)
        {
            if (!DefinitionPressureCatalog.TryParse(pressure.Name, out var pressureId))
            {
                throw new DefinitionGraphException(
                    $"Domain declares unknown pressure '{pressure.Name}'. Add it to DefinitionPressureCatalog first.");
            }

            nodes[pressureId] = new DefinitionPressureNode
            {
                Id = pressureId,
                Metadata = DefinitionPressureCatalog.Get(pressureId),
                SourceConfig = pressure,
            };
        }

        return nodes;
    }

    private static Dictionary<EntityKind, DefinitionLifecycleNode> BuildLifecycleNodes(
        DomainConfig config,
        Dictionary<EntityKind, DefinitionKindNode> kindNodes)
    {
        var nodes = new Dictionary<EntityKind, DefinitionLifecycleNode>();
        foreach (var (kind, lifecycle) in config.Lifecycles)
        {
            if (!kindNodes.TryGetValue(kind, out var kindNode))
            {
                throw new DefinitionGraphException(
                    $"Lifecycle declared for missing kind '{kind}'.");
            }

            var healthAttribute = ParseAttributeId(lifecycle.HealthAttribute, kind, "lifecycle health attribute");
            nodes[kind] = new DefinitionLifecycleNode
            {
                Kind = kind,
                HealthAttribute = healthAttribute,
                SourceConfig = lifecycle,
            };
        }

        return nodes;
    }

    private static DefinitionProcessNode BuildProcessNode(ProcessDefinition process)
    {
        return new DefinitionProcessNode
        {
            Id = process.Id,
            Name = process.Name,
            Role = process.Role,
            TargetKind = process.TargetKind,
            SourceDefinition = process,
            Reads = ConvertRefs(process.Reads),
            Writes = ConvertRefs(process.Writes),
            GoalEffects = ConvertRefs(process.GoalEffects),
            ProviderSelectors = ConvertRefs(process.ProviderSelectors),
            SupportRules = process.SupportRules.Select(rule => new DefinitionSupportRuleNode
            {
                DesiredEffect = ConvertRef(rule.DesiredEffect),
                DesiredMinimum = rule.DesiredMinimum,
                UrgencyScale = rule.UrgencyScale,
                SourceRule = rule,
            }).ToList(),
        };
    }

    private static DefinitionCriticalEffectNode? BuildCriticalEffect(CriticalEffectConfig? config, EntityKind kind)
    {
        if (config is null)
        {
            return null;
        }

        return new DefinitionCriticalEffectNode
        {
            TargetAttributeId = ParseAttributeId(config.TargetAttribute, kind, "critical-effect target attribute"),
            DeltaPerTick = config.DeltaPerTick,
            Description = config.Description,
        };
    }

    private static DefinitionLocationResourceDriftNode? BuildLocationResourceDrift(AttributeConfig attribute)
    {
        if (attribute.LocationResourceDriftResource is null)
        {
            return null;
        }

        return new DefinitionLocationResourceDriftNode
        {
            ResourceId = ParseResourceId(attribute.LocationResourceDriftResource.Value.ToDomainName()),
            PerUnit = attribute.LocationResourceDriftPerUnit,
            MinOffset = attribute.LocationResourceDriftMinOffset,
            MaxOffset = attribute.LocationResourceDriftMaxOffset,
        };
    }

    private static HashSet<TypedDefinitionRef> ConvertRefs(IEnumerable<AttributeRef> refs) =>
        refs.Select(ConvertRef).ToHashSet();

    private static TypedDefinitionRef ConvertRef(AttributeRef attrRef)
    {
        ArgumentNullException.ThrowIfNull(attrRef);

        return attrRef.Scope switch
        {
            RefScope.Self or RefScope.Target or RefScope.Location or RefScope.Related
                => ConvertAttributeScopedRef(attrRef),
            RefScope.Resource
                => ConvertResourceRef(attrRef),
            RefScope.Pressure
                => ConvertPressureRef(attrRef),
            RefScope.Structural
                => new TypedDefinitionRef(attrRef.Scope, new StructuralValueId(attrRef.Name), attrRef.Kind),
            RefScope.MapCell
                => new TypedDefinitionRef(attrRef.Scope, new MapCellValueId(attrRef.Name), attrRef.Kind),
            RefScope.Relational
                => ConvertRelationalRef(attrRef),
            _ => throw new DefinitionGraphException($"Unsupported ref scope '{attrRef.Scope}'."),
        };
    }

    private static TypedDefinitionRef ConvertAttributeScopedRef(AttributeRef attrRef)
    {
        if (!DefinitionAttributeCatalog.TryParse(attrRef.Name, out var attributeId))
        {
            throw new DefinitionGraphException(
                $"Unknown attribute ref '{attrRef.Name}' in scope '{attrRef.Scope}'. Add it to DefinitionAttributeCatalog first.");
        }

        return new TypedDefinitionRef(
            attrRef.Scope,
            new AttributeValueId(attributeId),
            attrRef.Kind,
            TryParseRelationship(attrRef.RelationshipKind));
    }

    private static TypedDefinitionRef ConvertResourceRef(AttributeRef attrRef) =>
        new(attrRef.Scope, new ResourceValueId(ParseResourceId(attrRef.Name)), attrRef.Kind);

    private static TypedDefinitionRef ConvertPressureRef(AttributeRef attrRef)
    {
        if (!DefinitionPressureCatalog.TryParse(attrRef.Name, out var pressureId))
        {
            throw new DefinitionGraphException(
                $"Unknown pressure ref '{attrRef.Name}'. Add it to DefinitionPressureCatalog first.");
        }

        return new TypedDefinitionRef(attrRef.Scope, new PressureValueId(pressureId), attrRef.Kind);
    }

    private static TypedDefinitionRef ConvertRelationalRef(AttributeRef attrRef)
    {
        return new TypedDefinitionRef(
            attrRef.Scope,
            new RelationalValueId(attrRef.Name),
            attrRef.Kind,
            TryParseRelationship(attrRef.RelationshipKind));
    }

    private static DefinitionAttributeId ParseAttributeId(string name, EntityKind kind, string context)
    {
        if (!DefinitionAttributeCatalog.TryParse(name, out var attributeId))
        {
            throw new DefinitionGraphException(
                $"Kind '{kind}' declares unknown {context} '{name}'. Add it to DefinitionAttributeCatalog first.");
        }

        var metadata = DefinitionAttributeCatalog.Get(attributeId);
        if (!metadata.AllowedKinds.Contains(kind))
        {
            throw new DefinitionGraphException(
                $"Attribute '{name}' is not declared for kind '{kind}'.");
        }

        return attributeId;
    }

    private static DefinitionResourceId ParseResourceId(string name)
    {
        if (!DefinitionResourceCatalog.TryParse(name, out var resourceId))
        {
            throw new DefinitionGraphException(
                $"Unknown resource ref '{name}'. Add it to DefinitionResourceCatalog first.");
        }

        return resourceId;
    }

    private static DefinitionRelationshipId? TryParseRelationship(string? relationshipKind)
    {
        if (string.IsNullOrWhiteSpace(relationshipKind))
        {
            return null;
        }

        if (!DefinitionRelationshipCatalog.TryParse(relationshipKind, out var relationshipId))
        {
            throw new DefinitionGraphException(
                $"Unknown relationship ref '{relationshipKind}'. Add it to DefinitionRelationshipCatalog first.");
        }

        return relationshipId;
    }
}

public sealed class DefinitionGraphException : Exception
{
    public DefinitionGraphException()
    {
    }

    public DefinitionGraphException(string message)
        : base(message)
    {
    }

    public DefinitionGraphException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
