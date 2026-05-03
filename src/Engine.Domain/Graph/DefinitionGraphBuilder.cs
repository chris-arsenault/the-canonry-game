namespace Engine.Domain.Graph;

using Engine.Domain.Config;
using Engine.Gambits;
using Engine.Gameplay;
using Engine.Kinds;
using Engine.Wiring;

public static class DefinitionGraphBuilder
{
    public static DefinitionGraph Build(
        DomainConfig config,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(runtimeIds);

        var kindNodes = BuildKindNodes(config, runtimeIds);
        var attributeIndex = BuildAttributeIndex(kindNodes);
        var relationshipNodes = BuildRelationshipNodes(config.Relationships, runtimeIds);
        var relationshipFactNodes = BuildRelationshipFactNodes(config.RelationshipFacts, runtimeIds);
        var lifecycleNodes = BuildLifecycleNodes(config, kindNodes);
        var generatorNodes = new Dictionary<string, DefinitionGeneratorNode>(StringComparer.Ordinal);
        var gambitNodes = BuildGambitNodes(config.Gambits, kindNodes, relationshipNodes);
        var gameplayNodes = BuildGameplayNodes(config.Gameplay);

        var graph = new DefinitionGraph
        {
            Kinds = kindNodes,
            Gambits = gambitNodes,
            GameplayNodes = gameplayNodes,
            Generators = generatorNodes,
            Relationships = relationshipNodes,
            RelationshipFacts = relationshipFactNodes,
            Lifecycles = lifecycleNodes,
            Attributes = attributeIndex,
        };

        PopulateSemanticGraph(config, graph, runtimeIds, generatorNodes);

        DefinitionGraphValidator.ValidateOrThrow(graph);
        return graph;
    }

    private static List<DefinitionGameplayNode> BuildGameplayNodes(GameplayDefinition? gameplay)
    {
        if (gameplay is null)
        {
            return [];
        }

        var nodes = new List<DefinitionGameplayNode>();
        foreach (var verb in gameplay.LowAltitudeVerbs)
        {
            nodes.Add(new DefinitionLowAltitudeVerbNode
            {
                Id = $"LowAltitudeVerb:{verb.Id}",
                SourceConfig = verb,
                Reads = [new MapSignalRead($"{verb.Kind}:{verb.TargetMode}")],
                Writes = verb.Operation == LowAltitudeVerbOperation.Move
                    ? [new InfluenceWrite($"{verb.Kind}:movement-intent")]
                    : [new InfluenceWrite($"{verb.Kind}:{verb.Operation}")],
            });
        }

        if (gameplay.MapSignals.Kinds.Count > 0 ||
            gameplay.MapSignals.Terrains.Count > 0 ||
            gameplay.MapSignals.Relationships.Count > 0)
        {
            nodes.Add(new DefinitionMapSignalNode
            {
                Id = "MapSignals",
                Source = "MapSignals",
                Reads = [new MapSignalRead("map-activity"), new MapSignalRead("relationships")],
                Writes = [new InfluenceWrite("map-signals")],
            });
        }

        foreach (var effect in gameplay.Effects.AttributeDeltaEffects)
        {
            nodes.Add(new DefinitionEffectNode
            {
                Id = $"AttributeDeltaEffect:{effect.Kind}:{effect.Attribute}:{effect.Condition}",
                SourceConfig = effect,
                Reads = [new MapSignalRead($"{effect.Kind}:{effect.Condition}")],
                Writes = [new AttributeDeltaWrite($"{effect.Kind}:{effect.Attribute}")],
            });
        }

        return nodes;
    }

    private static Dictionary<EntityKind, DefinitionKindNode> BuildKindNodes(
        DomainConfig config,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        var kindNodes = new Dictionary<EntityKind, DefinitionKindNode>();
        foreach (var (kind, kindConfig) in config.Kinds)
        {
            kindNodes[kind] = BuildKindNode(kind, kindConfig, runtimeIds);
        }

        return kindNodes;
    }

    private static Dictionary<DefinitionAttributeKey, DefinitionAttributeNode> BuildAttributeIndex(
        Dictionary<EntityKind, DefinitionKindNode> kindNodes)
    {
        var attributeIndex = new Dictionary<DefinitionAttributeKey, DefinitionAttributeNode>();
        foreach (var kindNode in kindNodes.Values)
        {
            foreach (var attributeNode in kindNode.Attributes.Values)
            {
                attributeIndex[attributeNode.Key] = attributeNode;
            }
        }

        return attributeIndex;
    }

    private static Dictionary<string, DefinitionGambitNode> BuildGambitNodes(
        IReadOnlyList<GambitDefinition> gambits,
        Dictionary<EntityKind, DefinitionKindNode> kindNodes,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode> relationshipNodes)
    {
        var gambitNodes = new Dictionary<string, DefinitionGambitNode>(StringComparer.Ordinal);
        foreach (var gambit in gambits)
        {
            EnsureTargetKindExists("Gambit", gambit.Id, gambit.TargetKind, kindNodes);
            gambitNodes[gambit.Id] = BuildGambitNode(gambit, kindNodes, relationshipNodes);
        }

        return gambitNodes;
    }

    private static void EnsureTargetKindExists(
        string nodeType,
        string nodeId,
        EntityKind targetKind,
        Dictionary<EntityKind, DefinitionKindNode> kindNodes)
    {
        if (!kindNodes.ContainsKey(targetKind))
        {
            throw new DefinitionGraphException(
                $"{nodeType} '{nodeId}' targets kind '{targetKind}' which is not present in the domain kinds.");
        }
    }

    private static void PopulateSemanticGraph(
        DomainConfig config,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds,
        Dictionary<string, DefinitionGeneratorNode> generatorNodes)
    {
        if (config.Semantics is null)
        {
            return;
        }

        DefinitionSemanticGraphBuilder.Populate(graph, config.Semantics);
        foreach (var (id, node) in DefinitionGeneratorBuilder.Build(config.Semantics.Generators, graph, runtimeIds))
        {
            generatorNodes[id] = node;
        }
    }

    private static DefinitionKindNode BuildKindNode(
        EntityKind kind,
        KindConfig kindConfig,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        var attributes = BuildAttributeNodes(kind, kindConfig.Health);
        var traits = BuildTraitNodes(kind, kindConfig.Traits, runtimeIds);
        var healthAttribute = new DefinitionAttributeId(kindConfig.Health.Name);

        return new DefinitionKindNode
        {
            Kind = kind,
            RuntimeId = runtimeIds.KindIdOf(kind).Value,
            Subtypes = new HashSet<string>(kindConfig.Subtypes, StringComparer.Ordinal),
            Attributes = attributes,
            Traits = traits,
            HealthAttribute = healthAttribute,
            Description = kindConfig.Description,
            PrimaryPlane = kindConfig.PrimaryPlane,
        };
    }

    private static Dictionary<DefinitionAttributeId, DefinitionAttributeNode> BuildAttributeNodes(
        EntityKind kind,
        HealthConfig health)
    {
        var attributes = new Dictionary<DefinitionAttributeId, DefinitionAttributeNode>();
        var attributeId = new DefinitionAttributeId(health.Name);
        attributes[attributeId] = CreateAttributeNode(kind, attributeId, health);

        return attributes;
    }

    private static Dictionary<DefinitionTraitId, DefinitionTraitNode> BuildTraitNodes(
        EntityKind kind,
        IReadOnlyList<TraitConfig> traitConfigs,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        var traits = new Dictionary<DefinitionTraitId, DefinitionTraitNode>();
        foreach (var trait in traitConfigs)
        {
            var traitId = new DefinitionTraitId(trait.Name);
            if (traits.ContainsKey(traitId))
            {
                throw new DefinitionGraphException(
                    $"Kind '{kind}' declares duplicate trait '{trait.Name}'.");
            }

            traits[traitId] = new DefinitionTraitNode
            {
                Key = new DefinitionTraitKey(kind, traitId),
                RuntimeId = runtimeIds.TraitIdOf(trait.Name).Value,
                SourceConfig = trait,
            };
        }

        return traits;
    }

    private static DefinitionAttributeNode CreateAttributeNode(
        EntityKind kind,
        DefinitionAttributeId attributeId,
        HealthConfig health) =>
        new()
        {
            Key = new DefinitionAttributeKey(kind, attributeId),
            Shape = DefinitionAttributeShape.Scalar,
            SourceConfig = health,
            IsHealthAttribute = true,
        };

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
                HealthAttribute = new DefinitionAttributeId(lifecycle.Health),
                SourceConfig = lifecycle,
            };
        }

        return nodes;
    }

    private static DefinitionGambitNode BuildGambitNode(
        GambitDefinition gambit,
        Dictionary<EntityKind, DefinitionKindNode> kindNodes,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode> relationshipNodes)
    {
        return new DefinitionGambitNode
        {
            Id = gambit.Id,
            Name = gambit.Name,
            TargetKind = gambit.TargetKind,
            SourceDefinition = gambit,
            Reads = ConvertRefs(gambit.Reads, kindNodes, relationshipNodes),
            Writes = ConvertRefs(gambit.Writes, kindNodes, relationshipNodes),
            GoalEffects = ConvertRefs(gambit.GoalEffects, kindNodes, relationshipNodes),
            ProviderSelectors = ConvertRefs(gambit.ProviderSelectors, kindNodes, relationshipNodes),
            SupportRules = gambit.SupportRules.Select(rule => new DefinitionSupportRuleNode
            {
                DesiredEffect = ConvertRef(rule.DesiredEffect, kindNodes, relationshipNodes),
                DesiredMinimum = rule.DesiredMinimum,
                UrgencyScale = rule.UrgencyScale,
                SourceRule = rule,
            }).ToList(),
        };
    }

    private static HashSet<TypedDefinitionRef> ConvertRefs(
        IEnumerable<AttributeRef> refs,
        Dictionary<EntityKind, DefinitionKindNode>? kindNodes,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode>? relationshipNodes) =>
        refs.Select(attrRef => ConvertRef(attrRef, kindNodes, relationshipNodes)).ToHashSet();

    private static TypedDefinitionRef ConvertRef(
        AttributeRef attrRef,
        Dictionary<EntityKind, DefinitionKindNode>? kindNodes,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode>? relationshipNodes)
    {
        ArgumentNullException.ThrowIfNull(attrRef);

        if (attrRef.Scope is RefScope.Self or RefScope.Target or RefScope.Location or RefScope.Related)
        {
            return ConvertAttributeScopedRef(attrRef, kindNodes, relationshipNodes);
        }

        return ConvertNonAttributeScopedRef(attrRef, relationshipNodes);
    }

    private static TypedDefinitionRef ConvertNonAttributeScopedRef(
        AttributeRef attrRef,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode>? relationshipNodes) =>
        attrRef.Scope switch
        {
            RefScope.Structural
                => new TypedDefinitionRef(attrRef.Scope, new StructuralValueId(attrRef.Name), attrRef.Kind),
            RefScope.MapCell
                => new TypedDefinitionRef(attrRef.Scope, new MapCellValueId(attrRef.Name), attrRef.Kind),
            RefScope.Relational
                => ConvertRelationalRef(attrRef, relationshipNodes),
            _ => throw new DefinitionGraphException($"Unsupported ref scope '{attrRef.Scope}'."),
        };

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
