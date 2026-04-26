namespace Engine.Domain.Graph;

using Engine.Domain.Config;
using Engine.Kinds;
using Engine.Processes;
using Engine.Resources;
using Engine.Wiring;

public static class DefinitionGraphBuilder
{
    public static DefinitionGraph Build(
        DomainConfig config,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(runtimeIds);

        var kindNodes = new Dictionary<EntityKind, DefinitionKindNode>();
        var attributeIndex = new Dictionary<DefinitionAttributeKey, DefinitionAttributeNode>();

        foreach (var (kind, kindConfig) in config.Kinds)
        {
            var kindNode = BuildKindNode(kind, kindConfig, runtimeIds);
            kindNodes[kind] = kindNode;

            foreach (var attributeNode in kindNode.Attributes.Values)
            {
                attributeIndex[attributeNode.Key] = attributeNode;
            }
        }

        var resourceNodes = BuildResourceNodes(config.Resources);
        var relationshipNodes = BuildRelationshipNodes(config.Relationships, runtimeIds);
        var relationshipFactNodes = BuildRelationshipFactNodes(config.RelationshipFacts, runtimeIds);
        var pressureNodes = BuildPressureNodes(config.Pressures);
        var lifecycleNodes = BuildLifecycleNodes(config, kindNodes);
        var generatorNodes = new Dictionary<string, DefinitionGeneratorNode>(StringComparer.Ordinal);

        var processNodes = new Dictionary<string, DefinitionProcessNode>(StringComparer.Ordinal);
        foreach (var process in config.Processes)
        {
            if (!kindNodes.ContainsKey(process.TargetKind))
            {
                throw new DefinitionGraphException(
                    $"Process '{process.Id}' targets kind '{process.TargetKind}' which is not present in the domain kinds.");
            }

            processNodes[process.Id] = BuildProcessNode(
                process,
                kindNodes,
                resourceNodes,
                relationshipNodes,
                pressureNodes);
        }

        var graph = new DefinitionGraph
        {
            RingOrder = config.Semantics?.RingOrder ?? [],
            Kinds = kindNodes,
            Processes = processNodes,
            Generators = generatorNodes,
            Resources = resourceNodes,
            Relationships = relationshipNodes,
            RelationshipFacts = relationshipFactNodes,
            Pressures = pressureNodes,
            Lifecycles = lifecycleNodes,
            Attributes = attributeIndex,
        };

        if (config.Semantics is not null)
        {
            DefinitionSemanticGraphBuilder.Populate(graph, config.Semantics);

            foreach (var (id, node) in DefinitionGeneratorBuilder.Build(config.Semantics.Generators, graph, runtimeIds))
            {
                generatorNodes[id] = node;
            }
        }

        DefinitionGraphValidator.ValidateOrThrow(graph);
        return graph;
    }

    private static DefinitionKindNode BuildKindNode(
        EntityKind kind,
        KindConfig kindConfig,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        var attributes = new Dictionary<DefinitionAttributeId, DefinitionAttributeNode>();
        DefinitionAttributeId? healthAttribute = null;

        foreach (var attribute in kindConfig.Attributes)
        {
            var attributeId = new DefinitionAttributeId(attribute.Name);
            if (attributes.ContainsKey(attributeId))
            {
                throw new DefinitionGraphException(
                    $"Kind '{kind}' declares duplicate attribute '{attribute.Name}'.");
            }

            attributes[attributeId] = new DefinitionAttributeNode
            {
                Key = new DefinitionAttributeKey(kind, attributeId),
                Shape = DefinitionAttributeShape.Scalar,
                SourceConfig = attribute,
                IsHealthAttribute = false,
                CriticalEffect = BuildCriticalEffect(attribute.CriticalEffect),
                LocationResourceDrift = BuildLocationResourceDrift(attribute),
            };
        }

        if (kindConfig.HealthAttribute is not null)
        {
            var healthId = new DefinitionAttributeId(kindConfig.HealthAttribute);
            if (!attributes.TryGetValue(healthId, out var healthNode))
            {
                throw new DefinitionGraphException(
                    $"Kind '{kind}' health attribute '{kindConfig.HealthAttribute}' is not present in its declared attributes.");
            }

            attributes[healthId] = new DefinitionAttributeNode
            {
                Key = healthNode.Key,
                Shape = healthNode.Shape,
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
                DesiredEffect = ConvertRef(
                    drive.DesiredEffect,
                    kindNodes: null,
                    resourceNodes: null,
                    relationshipNodes: null,
                    pressureNodes: null),
                BaseUrgency = drive.BaseUrgency,
            })
            .ToList();

        return new DefinitionKindNode
        {
            Kind = kind,
            RuntimeId = runtimeIds.KindIdOf(kind).Value,
            Subtypes = new HashSet<string>(kindConfig.Subtypes),
            Attributes = attributes,
            HealthAttribute = healthAttribute,
            StandingDrives = standingDrives,
            Description = kindConfig.Description,
            PrimaryPlane = kindConfig.PrimaryPlane,
        };
    }

    private static Dictionary<DefinitionResourceId, DefinitionResourceNode> BuildResourceNodes(
        IReadOnlyList<ResourceConfig> resources)
    {
        var nodes = new Dictionary<DefinitionResourceId, DefinitionResourceNode>();
        foreach (var resource in resources)
        {
            var resourceId = new DefinitionResourceId(resource.Name);
            if (nodes.ContainsKey(resourceId))
            {
                throw new DefinitionGraphException(
                    $"Domain declares duplicate resource '{resource.Name}'.");
            }

            nodes[resourceId] = new DefinitionResourceNode
            {
                Id = resourceId,
                SourceConfig = resource,
            };
        }

        return nodes;
    }

    private static Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode> BuildRelationshipNodes(
        IReadOnlyList<RelationshipConfig> relationships,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        var nodes = new Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode>();
        foreach (var relationship in relationships)
        {
            var relationshipId = new DefinitionRelationshipId(relationship.Name);
            if (nodes.ContainsKey(relationshipId))
            {
                throw new DefinitionGraphException(
                    $"Domain declares duplicate relationship '{relationship.Name}'.");
            }

            nodes[relationshipId] = new DefinitionRelationshipNode
            {
                Id = relationshipId,
                RuntimeId = runtimeIds.RelationshipKindIdOf(relationship.Name).Value,
                SourceConfig = relationship,
            };
        }

        return nodes;
    }

    private static Dictionary<DefinitionRelationshipFactId, DefinitionRelationshipFactNode> BuildRelationshipFactNodes(
        IReadOnlyList<RelationshipFactConfig> relationshipFacts,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        var nodes = new Dictionary<DefinitionRelationshipFactId, DefinitionRelationshipFactNode>();
        foreach (var fact in relationshipFacts)
        {
            var factId = new DefinitionRelationshipFactId(fact.Name);
            if (nodes.ContainsKey(factId))
            {
                throw new DefinitionGraphException(
                    $"Domain declares duplicate relationship fact '{fact.Name}'.");
            }

            nodes[factId] = new DefinitionRelationshipFactNode
            {
                Id = factId,
                RuntimeId = runtimeIds.RelationshipFactIdOf(fact.Name).Value,
                SourceConfig = fact,
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
            var pressureId = new DefinitionPressureId(pressure.Name);
            if (nodes.ContainsKey(pressureId))
            {
                throw new DefinitionGraphException(
                    $"Domain declares duplicate pressure '{pressure.Name}'.");
            }

            nodes[pressureId] = new DefinitionPressureNode
            {
                Id = pressureId,
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
            if (!kindNodes.ContainsKey(kind))
            {
                throw new DefinitionGraphException(
                    $"Lifecycle declared for missing kind '{kind}'.");
            }

            nodes[kind] = new DefinitionLifecycleNode
            {
                Kind = kind,
                HealthAttribute = new DefinitionAttributeId(lifecycle.HealthAttribute),
                SourceConfig = lifecycle,
            };
        }

        return nodes;
    }

    private static DefinitionProcessNode BuildProcessNode(
        ProcessDefinition process,
        Dictionary<EntityKind, DefinitionKindNode> kindNodes,
        Dictionary<DefinitionResourceId, DefinitionResourceNode> resourceNodes,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode> relationshipNodes,
        Dictionary<DefinitionPressureId, DefinitionPressureNode> pressureNodes)
    {
        return new DefinitionProcessNode
        {
            Id = process.Id,
            Name = process.Name,
            Role = process.Role,
            TargetKind = process.TargetKind,
            SourceDefinition = process,
            Reads = ConvertRefs(process.Reads, kindNodes, resourceNodes, relationshipNodes, pressureNodes),
            Writes = ConvertRefs(process.Writes, kindNodes, resourceNodes, relationshipNodes, pressureNodes),
            GoalEffects = ConvertRefs(process.GoalEffects, kindNodes, resourceNodes, relationshipNodes, pressureNodes),
            ProviderSelectors = ConvertRefs(process.ProviderSelectors, kindNodes, resourceNodes, relationshipNodes, pressureNodes),
            SupportRules = process.SupportRules.Select(rule => new DefinitionSupportRuleNode
            {
                DesiredEffect = ConvertRef(rule.DesiredEffect, kindNodes, resourceNodes, relationshipNodes, pressureNodes),
                DesiredMinimum = rule.DesiredMinimum,
                UrgencyScale = rule.UrgencyScale,
                SourceRule = rule,
            }).ToList(),
        };
    }

    private static DefinitionCriticalEffectNode? BuildCriticalEffect(CriticalEffectConfig? config)
    {
        if (config is null)
        {
            return null;
        }

        return new DefinitionCriticalEffectNode
        {
            TargetAttributeId = new DefinitionAttributeId(config.TargetAttribute),
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
            ResourceId = new DefinitionResourceId(attribute.LocationResourceDriftResource.Value.ToDomainName()),
            PerUnit = attribute.LocationResourceDriftPerUnit,
            MinOffset = attribute.LocationResourceDriftMinOffset,
            MaxOffset = attribute.LocationResourceDriftMaxOffset,
        };
    }

    private static HashSet<TypedDefinitionRef> ConvertRefs(
        IEnumerable<AttributeRef> refs,
        Dictionary<EntityKind, DefinitionKindNode>? kindNodes,
        Dictionary<DefinitionResourceId, DefinitionResourceNode>? resourceNodes,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode>? relationshipNodes,
        Dictionary<DefinitionPressureId, DefinitionPressureNode>? pressureNodes) =>
        refs.Select(attrRef => ConvertRef(attrRef, kindNodes, resourceNodes, relationshipNodes, pressureNodes)).ToHashSet();

    private static TypedDefinitionRef ConvertRef(
        AttributeRef attrRef,
        Dictionary<EntityKind, DefinitionKindNode>? kindNodes,
        Dictionary<DefinitionResourceId, DefinitionResourceNode>? resourceNodes,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode>? relationshipNodes,
        Dictionary<DefinitionPressureId, DefinitionPressureNode>? pressureNodes)
    {
        ArgumentNullException.ThrowIfNull(attrRef);

        return attrRef.Scope switch
        {
            RefScope.Self or RefScope.Target or RefScope.Location or RefScope.Related
                => ConvertAttributeScopedRef(attrRef, kindNodes, relationshipNodes),
            RefScope.Resource
                => ConvertResourceRef(attrRef, resourceNodes),
            RefScope.Pressure
                => ConvertPressureRef(attrRef, pressureNodes),
            RefScope.Structural
                => new TypedDefinitionRef(attrRef.Scope, new StructuralValueId(attrRef.Name), attrRef.Kind),
            RefScope.MapCell
                => new TypedDefinitionRef(attrRef.Scope, new MapCellValueId(attrRef.Name), attrRef.Kind),
            RefScope.Relational
                => ConvertRelationalRef(attrRef, relationshipNodes),
            _ => throw new DefinitionGraphException($"Unsupported ref scope '{attrRef.Scope}'."),
        };
    }

    private static TypedDefinitionRef ConvertAttributeScopedRef(
        AttributeRef attrRef,
        Dictionary<EntityKind, DefinitionKindNode>? kindNodes,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode>? relationshipNodes)
    {
        var attributeId = new DefinitionAttributeId(attrRef.Name);
        if (kindNodes is not null && attrRef.Kind is EntityKind kind)
        {
            EnsureKindAttributeDeclared(kindNodes, kind, attributeId, attrRef);
        }

        return new TypedDefinitionRef(
            attrRef.Scope,
            new AttributeValueId(attributeId),
            attrRef.Kind,
            TryParseRelationship(attrRef.RelationshipKind, relationshipNodes));
    }

    private static TypedDefinitionRef ConvertResourceRef(
        AttributeRef attrRef,
        Dictionary<DefinitionResourceId, DefinitionResourceNode>? resourceNodes)
    {
        var resourceId = new DefinitionResourceId(attrRef.Name);
        if (resourceNodes is not null && !resourceNodes.ContainsKey(resourceId))
        {
            throw new DefinitionGraphException(
                $"Unknown resource ref '{attrRef.Name}'. Declare it in the domain resources first.");
        }

        return new TypedDefinitionRef(attrRef.Scope, new ResourceValueId(resourceId), attrRef.Kind);
    }

    private static TypedDefinitionRef ConvertPressureRef(
        AttributeRef attrRef,
        Dictionary<DefinitionPressureId, DefinitionPressureNode>? pressureNodes)
    {
        var pressureId = new DefinitionPressureId(attrRef.Name);
        if (pressureNodes is not null && !pressureNodes.ContainsKey(pressureId))
        {
            throw new DefinitionGraphException(
                $"Unknown pressure ref '{attrRef.Name}'. Declare it in the domain pressures first.");
        }

        return new TypedDefinitionRef(attrRef.Scope, new PressureValueId(pressureId), attrRef.Kind);
    }

    private static TypedDefinitionRef ConvertRelationalRef(
        AttributeRef attrRef,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode>? relationshipNodes)
    {
        return new TypedDefinitionRef(
            attrRef.Scope,
            new RelationalValueId(attrRef.Name),
            attrRef.Kind,
            TryParseRelationship(attrRef.RelationshipKind, relationshipNodes));
    }

    private static void EnsureKindAttributeDeclared(
        Dictionary<EntityKind, DefinitionKindNode> kindNodes,
        EntityKind kind,
        DefinitionAttributeId attributeId,
        AttributeRef attrRef)
    {
        if (!kindNodes.TryGetValue(kind, out var kindNode))
        {
            throw new DefinitionGraphException(
                $"Ref '{attrRef}' targets kind '{kind}' which is not declared in the domain.");
        }

        if (!kindNode.Attributes.ContainsKey(attributeId))
        {
            throw new DefinitionGraphException(
                $"Unknown attribute ref '{attrRef.Name}' for kind '{kind}'. Declare it on that kind first.");
        }
    }

    private static DefinitionRelationshipId? TryParseRelationship(
        string? relationshipKind,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode>? relationshipNodes)
    {
        if (string.IsNullOrWhiteSpace(relationshipKind))
        {
            return null;
        }

        var relationshipId = new DefinitionRelationshipId(relationshipKind);
        if (relationshipNodes is not null && !relationshipNodes.ContainsKey(relationshipId))
        {
            throw new DefinitionGraphException(
                $"Unknown relationship ref '{relationshipKind}'. Declare it in the domain relationships first.");
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
