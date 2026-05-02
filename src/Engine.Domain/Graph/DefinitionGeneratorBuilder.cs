namespace Engine.Domain.Graph;

using Engine.Domain.Config;
using Engine.Kinds;

public static class DefinitionGeneratorBuilder
{
    public static IReadOnlyDictionary<string, DefinitionGeneratorNode> Build(
        IReadOnlyList<SemanticGeneratorConfig> semantic,
        DefinitionGraph partialGraph,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(semantic);
        ArgumentNullException.ThrowIfNull(partialGraph);
        ArgumentNullException.ThrowIfNull(runtimeIds);

        var generators = new Dictionary<string, DefinitionGeneratorNode>(StringComparer.Ordinal);

        foreach (var config in semantic)
        {
            if (generators.ContainsKey(config.Id))
            {
                throw new DefinitionGraphException(
                    $"Generator '{config.Id}' is declared more than once.");
            }

            generators[config.Id] = BuildGeneratorNode(config, partialGraph, runtimeIds);
        }

        return generators;
    }

    private static DefinitionGeneratorNode BuildGeneratorNode(
        SemanticGeneratorConfig config,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        var declaredRefs = new HashSet<string>(StringComparer.Ordinal)
        {
            "$target",
        };

        var target = BuildTarget(config, graph);
        var secondaryTargets = BuildSecondaryTargets(config, graph, declaredRefs);
        var produces = BuildProducedEntities(config, graph, declaredRefs);
        ValidateProducedReferences(config.Id, produces, declaredRefs);

        var relationships = BuildRelationships(config, graph, runtimeIds, declaredRefs);
        var targetSaturation = BuildTargetSaturation(config, graph, runtimeIds, relationships);
        var triggers = BuildTriggers(config, graph);
        var effects = BuildEffects(config, graph, runtimeIds, declaredRefs);

        return new DefinitionGeneratorNode
        {
            Id = config.Id,
            RuntimeId = runtimeIds.GeneratorIdOf(config.Id).Value,
            Name = config.Name,
            Description = config.Description,
            Target = target,
            TargetSaturation = targetSaturation,
            SecondaryTargets = secondaryTargets,
            Produces = produces,
            Relationships = relationships,
            Effects = effects,
            Triggers = triggers,
            PulseWeight = config.PulseWeight,
            MaxRunsPerSim = config.MaxRunsPerSim,
            SourceConfig = config,
        };
    }

    private static DefinitionGeneratorTarget BuildTarget(
        SemanticGeneratorConfig config,
        DefinitionGraph graph)
    {
        EnsureKindDeclared(graph, config.Target.Kind, $"Generator '{config.Id}' target");
        ValidateSubtypes(graph, config.Target.Kind, config.Target.Subtypes, $"Generator '{config.Id}' target");
        ValidateRelationshipNames(graph, config.Target.RequiredOutgoingRelationships, $"Generator '{config.Id}' target");
        ValidateRelationshipNames(graph, config.Target.RequiredIncomingRelationships, $"Generator '{config.Id}' target");
        ValidateRelationshipNames(graph, config.Target.RequiredRelationships, $"Generator '{config.Id}' target");

        return new DefinitionGeneratorTarget
        {
            Kind = config.Target.Kind,
            Subtypes = config.Target.Subtypes is null
                ? null
                : new HashSet<string>(config.Target.Subtypes, StringComparer.Ordinal),
            AllowWilderness = config.Target.AllowWilderness,
            RequiredOutgoingRelationships = CopyRelationshipSet(config.Target.RequiredOutgoingRelationships),
            RequiredIncomingRelationships = CopyRelationshipSet(config.Target.RequiredIncomingRelationships),
            RequiredRelationships = CopyRelationshipSet(config.Target.RequiredRelationships),
        };
    }

    private static List<DefinitionGeneratorBindingTarget> BuildSecondaryTargets(
        SemanticGeneratorConfig config,
        DefinitionGraph graph,
        HashSet<string> declaredRefs)
    {
        var secondaryTargets = new List<DefinitionGeneratorBindingTarget>(config.SecondaryTargets.Count);
        foreach (var binding in config.SecondaryTargets)
        {
            ValidateBinding(config.Id, graph, binding);
            AddDeclaredRef(config.Id, declaredRefs, binding.Ref, "ref");
            secondaryTargets.Add(new DefinitionGeneratorBindingTarget
            {
                Ref = binding.Ref,
                Kind = binding.Kind,
                Query = BuildQuery(graph, binding.Kind, binding.Query),
            });
        }

        return secondaryTargets;
    }

    private static void ValidateBinding(
        string generatorId,
        DefinitionGraph graph,
        SemanticGeneratorBindingTarget binding)
    {
        var context = $"Generator '{generatorId}' binding '{binding.Ref}'";
        EnsureKindDeclared(graph, binding.Kind, context);
        ValidateQuery(graph, binding.Kind, binding.Query, $"{context} query");
    }

    private static List<DefinitionProducedEntity> BuildProducedEntities(
        SemanticGeneratorConfig config,
        DefinitionGraph graph,
        HashSet<string> declaredRefs)
    {
        var produces = new List<DefinitionProducedEntity>(config.Produces.Count);
        foreach (var produced in config.Produces)
        {
            ValidateProducedEntity(config.Id, graph, produced);
            AddDeclaredRef(config.Id, declaredRefs, produced.Ref, "produce ref");
            produces.Add(CreateProducedEntity(graph, produced));
        }

        return produces;
    }

    private static void ValidateProducedEntity(
        string generatorId,
        DefinitionGraph graph,
        SemanticProducedEntity produced)
    {
        var context = $"Generator '{generatorId}' produce '{produced.Ref}'";
        EnsureKindDeclared(graph, produced.Kind, context);
        ValidateProducedSubtypeChoice(graph, produced, context);
        ValidateCoverage(graph, generatorId, produced);
        ValidateRequiredCultureInheritance(generatorId, produced);
        ValidateQuery(graph, produced.Kind, produced.Query, $"{context} query");
    }

    private static void ValidateRequiredCultureInheritance(
        string generatorId,
        SemanticProducedEntity produced)
    {
        if (string.IsNullOrWhiteSpace(produced.InheritCultureFrom))
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' produce '{produced.Ref}' must declare InheritCultureFrom.");
        }
    }

    private static DefinitionProducedEntity CreateProducedEntity(
        DefinitionGraph graph,
        SemanticProducedEntity produced) =>
        new()
        {
            Ref = produced.Ref,
            Kind = produced.Kind,
            Subtype = produced.Subtype,
            Subtypes = produced.Subtypes is null
                ? null
                : new HashSet<string>(produced.Subtypes, StringComparer.Ordinal),
            Role = produced.Role,
            CreateChance = produced.CreateChance,
            CreateIfMissing = produced.CreateIfMissing,
            PhysicalPlacement = produced.PhysicalPlacement,
            SemanticPlacement = produced.SemanticPlacement,
            PlacementRef = produced.PlacementRef,
            InheritCultureFrom = produced.InheritCultureFrom,
            Query = BuildQuery(graph, produced.Kind, produced.Query),
            Coverage = BuildCoverage(produced.Coverage),
        };

    private static void ValidateProducedReferences(
        string generatorId,
        IReadOnlyList<DefinitionProducedEntity> produces,
        HashSet<string> declaredRefs)
    {
        foreach (var produced in produces)
        {
            EnsureDeclaredRef(generatorId, produced.InheritCultureFrom, declaredRefs, "InheritCultureFrom");
            EnsureDeclaredRef(generatorId, produced.PlacementRef, declaredRefs, "PlacementRef");
            EnsureDeclaredRef(generatorId, produced.Coverage?.SourceRef, declaredRefs, "Coverage.SourceRef");
        }
    }

    private static List<DefinitionGeneratorRelationship> BuildRelationships(
        SemanticGeneratorConfig config,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds,
        HashSet<string> declaredRefs) =>
        config.Relationships
            .Select(relationship => BuildRelationship(config.Id, relationship, graph, runtimeIds, declaredRefs))
            .ToList();

    private static DefinitionGeneratorRelationship BuildRelationship(
        string generatorId,
        SemanticGeneratorRelationship relationship,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds,
        HashSet<string> declaredRefs)
    {
        EnsureDeclaredRef(generatorId, relationship.SourceRef, declaredRefs, "SourceRef");
        EnsureDeclaredRef(generatorId, relationship.DestinationRef, declaredRefs, "DestinationRef");

        var context = $"Generator '{generatorId}' relationship";
        var relationshipId = ResolveRelationshipId(graph, relationship.Kind, context);
        var relationshipFactIds = ResolveRelationshipFactIds(graph, relationship.Facts, $"{context} '{relationship.Kind}'");

        return new DefinitionGeneratorRelationship
        {
            SourceRef = relationship.SourceRef,
            DestinationRef = relationship.DestinationRef,
            RelationshipId = relationshipId,
            RelationshipRuntimeId = runtimeIds.RelationshipKindIdOf(relationshipId.Value).Value,
            FactIds = relationshipFactIds,
            FactRuntimeIds = relationshipFactIds
                .Select(factId => runtimeIds.RelationshipFactIdOf(factId.Value).Value)
                .ToHashSet(),
            Strength = relationship.Strength,
            ReplaceExistingOutgoingOfSameKind = relationship.ReplaceExistingOutgoingOfSameKind,
        };
    }

    private static DefinitionTargetSaturation? BuildTargetSaturation(
        SemanticGeneratorConfig config,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds,
        IReadOnlyList<DefinitionGeneratorRelationship> relationships)
    {
        if (config.TargetSaturation is null)
        {
            return null;
        }

        var targetSaturation = CreateTargetSaturation(config, graph, runtimeIds);
        ValidateTargetSaturationHasMatchingGeneratedRelationship(config.Id, targetSaturation, relationships);
        return targetSaturation;
    }

    private static DefinitionTargetSaturation CreateTargetSaturation(
        SemanticGeneratorConfig config,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        var saturation = config.TargetSaturation!;
        var context = $"Generator '{config.Id}' target saturation";
        var requiredFactId = string.IsNullOrWhiteSpace(saturation.RequiredFact)
            ? (DefinitionRelationshipFactId?)null
            : ResolveRelationshipFactId(graph, saturation.RequiredFact, context);

        var relationshipId = ResolveRelationshipId(graph, saturation.RelationshipKind, context);

        return new DefinitionTargetSaturation
        {
            RelationshipId = relationshipId,
            RelationshipRuntimeId = runtimeIds.RelationshipKindIdOf(relationshipId.Value).Value,
            RequiredFactId = requiredFactId,
            RequiredFactRuntimeId = requiredFactId is null
                ? 0
                : runtimeIds.RelationshipFactIdOf(requiredFactId.Value.Value).Value,
            Direction = saturation.Direction,
            MaxExisting = saturation.MaxExisting,
        };
    }

    private static List<DefinitionGeneratorTrigger> BuildTriggers(
        SemanticGeneratorConfig config,
        DefinitionGraph graph) =>
        config.Triggers
            .Select(trigger => BuildTrigger(config.Id, graph, trigger))
            .ToList();

    private static DefinitionGeneratorTrigger BuildTrigger(
        string generatorId,
        DefinitionGraph graph,
        SemanticGeneratorTrigger trigger)
    {
        EnsureKindDeclared(graph, trigger.OnKind, $"Generator '{generatorId}' trigger");

        return new DefinitionGeneratorTrigger
        {
            OnKind = trigger.OnKind,
            AttributeId = ResolveTriggerAttribute(generatorId, graph, trigger),
            Direction = trigger.Direction,
        };
    }

    private static DefinitionAttributeId? ResolveTriggerAttribute(
        string generatorId,
        DefinitionGraph graph,
        SemanticGeneratorTrigger trigger)
    {
        if (string.IsNullOrWhiteSpace(trigger.AttributeName))
        {
            return null;
        }

        var attributeId = new DefinitionAttributeId(trigger.AttributeName);
        if (!graph.Kinds[trigger.OnKind].Attributes.ContainsKey(attributeId))
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' trigger references unknown attribute '{trigger.AttributeName}' on kind '{trigger.OnKind}'.");
        }

        return attributeId;
    }

    private static void ValidateQuery(
        DefinitionGraph graph,
        EntityKind kind,
        SemanticEntityQuery? query,
        string context)
    {
        ValidateSubtypes(graph, kind, query?.Subtypes, context);
        ValidateRelationshipNames(graph, query?.WithoutOutgoingRelationships, context);
        ValidateRelationshipNames(graph, query?.WithoutIncomingRelationships, context);
        ValidateQueryAttributeConditions(graph, kind, query?.AttributeConditions, context);
    }

    private static void AddDeclaredRef(
        string generatorId,
        HashSet<string> declaredRefs,
        string reference,
        string fieldName)
    {
        if (!declaredRefs.Add(reference))
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' declares duplicate {fieldName} '{reference}'.");
        }
    }

    private static List<IDefinitionGeneratorEffect> BuildEffects(
        SemanticGeneratorConfig config,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds,
        HashSet<string> declaredRefs) =>
        config.Effects
            .Select(effect => BuildEffect(config.Id, effect, graph, runtimeIds, declaredRefs))
            .ToList();

    private static IDefinitionGeneratorEffect BuildEffect(
        string generatorId,
        ISemanticGeneratorEffect effect,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds,
        HashSet<string> declaredRefs)
    {
        switch (effect)
        {
            case SetBindingHistoricalEffect setHistorical:
                {
                    EnsureDeclaredRef(generatorId, setHistorical.BindingRef, declaredRefs, "BindingRef");
                    return new DefinitionSetBindingHistoricalEffect(setHistorical.BindingRef);
                }
            case RemoveTargetRelationshipsEffect removeRelationships:
                {
                    var context = $"Generator '{generatorId}' RemoveTargetRelationships effect";
                    var relationshipId = ResolveRelationshipId(graph!, removeRelationships.RelationshipKind, context);
                    return new DefinitionRemoveTargetRelationshipsEffect(
                        runtimeIds.RelationshipKindIdOf(relationshipId.Value).Value);
                }
            default:
                throw new DefinitionGraphException(
                    $"Generator '{generatorId}' unsupported effect type '{effect.GetType().Name}'.");
        }
    }

    private static void EnsureKindDeclared(DefinitionGraph graph, EntityKind kind, string context)
    {
        if (!graph.Kinds.ContainsKey(kind))
        {
            throw new DefinitionGraphException(
                $"{context} references undeclared kind '{kind}'.");
        }
    }

    private static void ValidateSubtypes(
        DefinitionGraph graph,
        EntityKind kind,
        IReadOnlySet<string>? subtypes,
        string context)
    {
        if (subtypes is null)
        {
            return;
        }

        foreach (var subtype in subtypes)
        {
            ValidateSubtype(graph, kind, subtype, context);
        }
    }

    private static void ValidateSubtype(
        DefinitionGraph graph,
        EntityKind kind,
        string subtype,
        string context)
    {
        if (!graph.Kinds[kind].Subtypes.Contains(subtype))
        {
            throw new DefinitionGraphException(
                $"{context} references unknown subtype '{subtype}' for kind '{kind}'.");
        }
    }

    private static void ValidateProducedSubtypeChoice(
        DefinitionGraph graph,
        SemanticProducedEntity produced,
        string context)
    {
        var hasSingle = !string.IsNullOrWhiteSpace(produced.Subtype);
        var hasMany = produced.Subtypes is { Count: > 0 };

        if (hasSingle == hasMany)
        {
            throw new DefinitionGraphException(
                $"{context} must declare exactly one of Subtype or Subtypes.");
        }

        if (hasSingle)
        {
            ValidateSubtype(graph, produced.Kind, produced.Subtype!, context);
            return;
        }

        ValidateSubtypes(graph, produced.Kind, produced.Subtypes, context);
    }

    private static void ValidateCoverage(
        DefinitionGraph graph,
        string generatorId,
        SemanticProducedEntity produced)
    {
        _ = graph;
        if (string.Equals(produced.Kind.Value, "region", StringComparison.Ordinal))
        {
            if (produced.Coverage is null)
            {
                throw new DefinitionGraphException(
                    $"Generator '{generatorId}' region produce '{produced.Ref}' must declare Coverage.");
            }

            return;
        }

        if (produced.Coverage is not null)
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' non-region produce '{produced.Ref}' may not declare Coverage.");
        }
    }

    private static void ValidateRelationshipNames(
        DefinitionGraph graph,
        IReadOnlySet<string>? relationshipKinds,
        string context)
    {
        if (relationshipKinds is null)
        {
            return;
        }

        foreach (var relationshipKind in relationshipKinds)
        {
            ResolveRelationshipId(graph, relationshipKind, context);
        }
    }

    private static HashSet<string>? CopyRelationshipSet(IReadOnlySet<string>? relationshipKinds) =>
        relationshipKinds is null
            ? null
            : new HashSet<string>(relationshipKinds, StringComparer.Ordinal);

    private static void ValidateQueryAttributeConditions(
        DefinitionGraph graph,
        EntityKind kind,
        IReadOnlyList<SemanticQueryAttributeCondition>? conditions,
        string context)
    {
        if (conditions is null)
        {
            return;
        }

        foreach (var condition in conditions)
        {
            var attributeId = new DefinitionAttributeId(condition.AttributeName);
            if (!graph.Kinds[kind].Attributes.ContainsKey(attributeId))
            {
                throw new DefinitionGraphException(
                    $"{context} references unknown attribute '{condition.AttributeName}' on kind '{kind}'.");
            }
        }
    }

    private static DefinitionEntityQuery? BuildQuery(
        DefinitionGraph graph,
        EntityKind kind,
        SemanticEntityQuery? query)
    {
        _ = graph;
        _ = kind;
        if (query is null)
        {
            return null;
        }

        return new DefinitionEntityQuery
        {
            Subtypes = query.Subtypes is null
                ? null
                : new HashSet<string>(query.Subtypes, StringComparer.Ordinal),
            AdjacentToTarget = query.AdjacentToTarget,
            WithoutOutgoingRelationships = query.WithoutOutgoingRelationships is null
                ? null
                : new HashSet<string>(query.WithoutOutgoingRelationships, StringComparer.Ordinal),
            WithoutIncomingRelationships = query.WithoutIncomingRelationships is null
                ? null
                : new HashSet<string>(query.WithoutIncomingRelationships, StringComparer.Ordinal),
            TargetCultureMatch = query.TargetCultureMatch,
            AttributeConditions = query.AttributeConditions
                .Select(condition => new DefinitionQueryAttributeCondition
                {
                    AttributeId = new DefinitionAttributeId(condition.AttributeName),
                    Direction = condition.Direction,
                })
                .ToList(),
        };
    }

    private static DefinitionRegionCoverage? BuildCoverage(SemanticRegionCoverage? coverage)
    {
        if (coverage is null)
        {
            return null;
        }

        return new DefinitionRegionCoverage
        {
            Mode = coverage.Mode,
            SourceRef = coverage.SourceRef,
            DesiredCellCount = coverage.DesiredCellCount,
            MinimumCellCount = coverage.MinimumCellCount,
            MinimumSourceRemainingCellCount = coverage.MinimumSourceRemainingCellCount,
            PreferredTerrains = coverage.PreferredTerrains is null
                ? null
                : new HashSet<string>(coverage.PreferredTerrains, StringComparer.Ordinal),
            AllowedTerrains = coverage.AllowedTerrains is null
                ? null
                : new HashSet<string>(coverage.AllowedTerrains, StringComparer.Ordinal),
            RespectTerrainBoundaries = coverage.RespectTerrainBoundaries,
            TransformTerrain = coverage.TransformTerrain,
        };
    }

    private static DefinitionRelationshipId ResolveRelationshipId(
        DefinitionGraph graph,
        string relationshipKind,
        string context)
    {
        var relationshipId = new DefinitionRelationshipId(relationshipKind);
        if (!graph.Relationships.ContainsKey(relationshipId))
        {
            throw new DefinitionGraphException(
                $"{context} references undeclared relationship kind '{relationshipKind}'.");
        }

        return relationshipId;
    }

    private static HashSet<DefinitionRelationshipFactId> ResolveRelationshipFactIds(
        DefinitionGraph graph,
        IReadOnlySet<string> factNames,
        string context)
    {
        var factIds = new HashSet<DefinitionRelationshipFactId>();
        foreach (var factName in factNames)
        {
            factIds.Add(ResolveRelationshipFactId(graph, factName, context));
        }

        return factIds;
    }

    private static DefinitionRelationshipFactId ResolveRelationshipFactId(
        DefinitionGraph graph,
        string factName,
        string context)
    {
        var factId = new DefinitionRelationshipFactId(factName);
        if (!graph.RelationshipFacts.ContainsKey(factId))
        {
            throw new DefinitionGraphException(
                $"{context} references undeclared relationship fact '{factName}'.");
        }

        return factId;
    }

    private static void ValidateTargetSaturationHasMatchingGeneratedRelationship(
        string generatorId,
        DefinitionTargetSaturation targetSaturation,
        IReadOnlyList<DefinitionGeneratorRelationship> relationships)
    {
        var matches = relationships.Any(relationship =>
            relationship.RelationshipId == targetSaturation.RelationshipId &&
            TargetSaturationDirectionMatchesRelationship(targetSaturation.Direction, relationship) &&
            (targetSaturation.RequiredFactId is null || relationship.FactIds.Contains(targetSaturation.RequiredFactId.Value)));

        if (!matches)
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' target saturation must match a generated relationship involving '$target'.");
        }
    }

    private static bool TargetSaturationDirectionMatchesRelationship(
        TargetSaturationDirection direction,
        DefinitionGeneratorRelationship relationship) =>
        direction switch
        {
            TargetSaturationDirection.OutgoingFromTarget => relationship.SourceRef == "$target",
            TargetSaturationDirection.IncomingToTarget => relationship.DestinationRef == "$target",
            TargetSaturationDirection.Symmetric => relationship.SourceRef == "$target" || relationship.DestinationRef == "$target",
            _ => false,
        };

    private static void EnsureDeclaredRef(
        string generatorId,
        string? reference,
        HashSet<string> declaredRefs,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return;
        }

        if (!declaredRefs.Contains(reference))
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' {fieldName} references undeclared ref '{reference}'.");
        }
    }
}
