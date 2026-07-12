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
        var cultureNodes = BuildCultureNodes(config, runtimeIds, kindNodes, relationshipNodes);
        var lifecycleNodes = BuildLifecycleNodes(config, kindNodes);
        var generatorNodes = new Dictionary<string, DefinitionGeneratorNode>(StringComparer.Ordinal);
        var gambitNodes = BuildGambitNodes(config.Gambits, kindNodes, relationshipNodes);
        var gameplayNodes = BuildGameplayNodes(config.Gameplay);

        var graph = new DefinitionGraph
        {
            Kinds = kindNodes,
            Cultures = cultureNodes,
            Gambits = gambitNodes,
            GameplayNodes = gameplayNodes,
            Generators = generatorNodes,
            Relationships = relationshipNodes,
            Lifecycles = lifecycleNodes,
            Attributes = attributeIndex,
            Tags = new HashSet<string>(config.Tags, StringComparer.Ordinal),
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

    private static Dictionary<string, DefinitionCultureNode> BuildCultureNodes(
        DomainConfig config,
        global::Engine.Domain.DomainIdRegistry runtimeIds,
        Dictionary<EntityKind, DefinitionKindNode> kindNodes,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode> relationshipNodes)
    {
        var cultureNodes = new Dictionary<string, DefinitionCultureNode>(StringComparer.Ordinal);
        foreach (var culture in config.Cultures)
        {
            if (cultureNodes.ContainsKey(culture.Id))
            {
                throw new DefinitionGraphException($"Domain declares duplicate culture '{culture.Id}'.");
            }

            cultureNodes[culture.Id] = new DefinitionCultureNode
            {
                Id = culture.Id,
                RuntimeId = runtimeIds.CultureIdOf(culture.Id).Value,
                DisplayName = culture.DisplayName,
                Slots = BuildCultureSlotNodes(culture, kindNodes, relationshipNodes),
            };
        }

        return cultureNodes;
    }

    private static Dictionary<string, DefinitionCultureSlotNode> BuildCultureSlotNodes(
        CultureConfig culture,
        Dictionary<EntityKind, DefinitionKindNode> kindNodes,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode> relationshipNodes)
    {
        var slots = new Dictionary<string, DefinitionCultureSlotNode>(StringComparer.Ordinal);
        var slotId = 0;
        foreach (var slot in culture.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
            {
                throw new DefinitionGraphException($"Culture '{culture.Id}' declares a slot with no id.");
            }

            if (slots.ContainsKey(slot.Id))
            {
                throw new DefinitionGraphException($"Culture '{culture.Id}' declares duplicate slot '{slot.Id}'.");
            }

            slotId++;
            slots[slot.Id] = new DefinitionCultureSlotNode
            {
                Id = slot.Id,
                SlotId = slotId,
                DisplayName = slot.DisplayName,
                Description = slot.Description,
                PolarityCode = (int)slot.Polarity,
                AllowedKindIds = BuildCultureSlotAllowedKindIds(culture.Id, slot, kindNodes),
                ScoreRules = BuildCultureSlotScoreRules(culture.Id, slot, relationshipNodes),
                MinimumScore = slot.Hysteresis.MinimumScore,
                MinimumTenureTicks = slot.Hysteresis.MinimumTenureTicks,
                DisplacementMargin = slot.Hysteresis.DisplacementMargin,
                EntityInfluenceMultiplierDelta = slot.Benefits.EntityInfluenceMultiplierDelta,
                CultureInfluenceMultiplierDelta = slot.Benefits.CultureInfluenceMultiplierDelta,
            };
        }

        return slots;
    }

    private static HashSet<int> BuildCultureSlotAllowedKindIds(
        string cultureId,
        CultureSlotConfig slot,
        Dictionary<EntityKind, DefinitionKindNode> kindNodes)
    {
        var ids = new HashSet<int>();
        foreach (var kind in slot.AllowedKinds)
        {
            if (!kindNodes.TryGetValue(kind, out var kindNode))
            {
                throw new DefinitionGraphException(
                    $"Culture '{cultureId}' slot '{slot.Id}' allows unknown kind '{kind}'.");
            }

            ids.Add(kindNode.RuntimeId);
        }

        return ids;
    }

    private static List<DefinitionCultureSlotScoreRule> BuildCultureSlotScoreRules(
        string cultureId,
        CultureSlotConfig slot,
        Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode> relationshipNodes)
    {
        var rules = new List<DefinitionCultureSlotScoreRule>();
        foreach (var rule in slot.ScoreRules)
        {
            var signalCode = (int)rule.Signal;
            var relationshipKindId = 0;
            if (rule.Signal is CultureSlotScoreSignal.OutgoingRelationshipStrength or CultureSlotScoreSignal.IncomingRelationshipStrength)
            {
                if (string.IsNullOrWhiteSpace(rule.RelationshipKind))
                {
                    throw new DefinitionGraphException(
                        $"Culture '{cultureId}' slot '{slot.Id}' relationship score rule requires a relationship kind.");
                }

                var relationshipId = new DefinitionRelationshipId(rule.RelationshipKind);
                if (!relationshipNodes.TryGetValue(relationshipId, out var relationshipNode))
                {
                    throw new DefinitionGraphException(
                        $"Culture '{cultureId}' slot '{slot.Id}' references unknown relationship '{rule.RelationshipKind}'.");
                }

                relationshipKindId = relationshipNode.RuntimeId;
            }

            rules.Add(new DefinitionCultureSlotScoreRule(signalCode, relationshipKindId, rule.Weight));
        }

        return rules;
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
            Tier = kindConfig.Tier,
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
            ProviderSelectors = ConvertRefs(gambit.ProviderSelectors, kindNodes, relationshipNodes),
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
