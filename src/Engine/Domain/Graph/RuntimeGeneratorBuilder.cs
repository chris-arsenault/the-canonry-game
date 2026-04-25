namespace Engine.Domain.Graph;

using Engine.Domain.Config;
using Engine.Kinds;
using Engine.Map;

public static class RuntimeGeneratorBuilder
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

            EnsureKindDeclared(partialGraph, config.Target.Kind, $"Generator '{config.Id}' target");
            ValidateSubtypes(partialGraph, config.Target.Kind, config.Target.Subtypes, $"Generator '{config.Id}' target");

            var declaredRefs = new HashSet<string>(StringComparer.Ordinal)
            {
                "$target",
            };

            var secondaryTargets = new List<DefinitionGeneratorBindingTarget>(config.SecondaryTargets.Count);
            foreach (var binding in config.SecondaryTargets)
            {
                EnsureKindDeclared(partialGraph, binding.Kind, $"Generator '{config.Id}' binding '{binding.Ref}'");
                ValidateSubtypes(
                    partialGraph,
                    binding.Kind,
                    binding.Query?.Subtypes,
                    $"Generator '{config.Id}' binding '{binding.Ref}' query");
                ValidateRelationshipNames(
                    partialGraph,
                    binding.Query?.WithoutOutgoingRelationships,
                    $"Generator '{config.Id}' binding '{binding.Ref}' query");
                ValidateRelationshipNames(
                    partialGraph,
                    binding.Query?.WithoutIncomingRelationships,
                    $"Generator '{config.Id}' binding '{binding.Ref}' query");
                ValidateQueryAttributeConditions(
                    partialGraph,
                    binding.Kind,
                    binding.Query?.AttributeConditions,
                    $"Generator '{config.Id}' binding '{binding.Ref}' query");

                if (!declaredRefs.Add(binding.Ref))
                {
                    throw new DefinitionGraphException(
                        $"Generator '{config.Id}' declares duplicate ref '{binding.Ref}'.");
                }

                secondaryTargets.Add(new DefinitionGeneratorBindingTarget
                {
                    Ref = binding.Ref,
                    Kind = binding.Kind,
                    Query = BuildQuery(partialGraph, binding.Kind, binding.Query),
                });
            }

            var produces = new List<DefinitionProducedEntity>(config.Produces.Count);
            foreach (var produced in config.Produces)
            {
                EnsureKindDeclared(partialGraph, produced.Kind, $"Generator '{config.Id}' produce '{produced.Ref}'");
                ValidateProducedSubtypeChoice(partialGraph, produced, $"Generator '{config.Id}' produce '{produced.Ref}'");
                ValidateCoverage(partialGraph, config.Id, produced);
                if (string.IsNullOrWhiteSpace(produced.InheritCultureFrom))
                {
                    throw new DefinitionGraphException(
                        $"Generator '{config.Id}' produce '{produced.Ref}' must declare InheritCultureFrom.");
                }
                ValidateSubtypes(
                    partialGraph,
                    produced.Kind,
                    produced.Query?.Subtypes,
                    $"Generator '{config.Id}' produce '{produced.Ref}' query");
                ValidateRelationshipNames(
                    partialGraph,
                    produced.Query?.WithoutOutgoingRelationships,
                    $"Generator '{config.Id}' produce '{produced.Ref}' query");
                ValidateRelationshipNames(
                    partialGraph,
                    produced.Query?.WithoutIncomingRelationships,
                    $"Generator '{config.Id}' produce '{produced.Ref}' query");
                ValidateQueryAttributeConditions(
                    partialGraph,
                    produced.Kind,
                    produced.Query?.AttributeConditions,
                    $"Generator '{config.Id}' produce '{produced.Ref}' query");

                if (!declaredRefs.Add(produced.Ref))
                {
                    throw new DefinitionGraphException(
                        $"Generator '{config.Id}' declares duplicate produce ref '{produced.Ref}'.");
                }

                produces.Add(new DefinitionProducedEntity
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
                    Query = BuildQuery(partialGraph, produced.Kind, produced.Query),
                    Coverage = BuildCoverage(produced.Coverage),
                });
            }

            foreach (var produced in produces)
            {
                EnsureDeclaredRef(config.Id, produced.InheritCultureFrom, declaredRefs, "InheritCultureFrom");
                EnsureDeclaredRef(config.Id, produced.PlacementRef, declaredRefs, "PlacementRef");
                if (!string.IsNullOrWhiteSpace(produced.Coverage?.SourceRef))
                {
                    EnsureDeclaredRef(config.Id, produced.Coverage.SourceRef, declaredRefs, "Coverage.SourceRef");
                }
            }

            var relationships = config.Relationships
                .Select(relationship =>
                {
                    EnsureDeclaredRef(config.Id, relationship.SourceRef, declaredRefs, "SourceRef");
                    EnsureDeclaredRef(config.Id, relationship.DestinationRef, declaredRefs, "DestinationRef");
                    var relationshipId = ResolveRelationshipId(
                        partialGraph,
                        relationship.Kind,
                        $"Generator '{config.Id}' relationship");

                    var relationshipFactIds = ResolveRelationshipFactIds(
                        partialGraph,
                        relationship.Facts,
                        $"Generator '{config.Id}' relationship '{relationship.Kind}'");

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
                })
                .ToList();

            DefinitionTargetSaturation? targetSaturation = null;
            if (config.TargetSaturation is not null)
            {
                var requiredFactId = string.IsNullOrWhiteSpace(config.TargetSaturation.RequiredFact)
                    ? (DefinitionRelationshipFactId?)null
                    : ResolveRelationshipFactId(
                        partialGraph,
                        config.TargetSaturation.RequiredFact,
                        $"Generator '{config.Id}' target saturation");

                var saturationRelationshipId = ResolveRelationshipId(
                    partialGraph,
                    config.TargetSaturation.RelationshipKind,
                    $"Generator '{config.Id}' target saturation");

                targetSaturation = new DefinitionTargetSaturation
                {
                    RelationshipId = saturationRelationshipId,
                    RelationshipRuntimeId = runtimeIds.RelationshipKindIdOf(saturationRelationshipId.Value).Value,
                    RequiredFactId = requiredFactId,
                    RequiredFactRuntimeId = requiredFactId is null
                        ? 0
                        : runtimeIds.RelationshipFactIdOf(requiredFactId.Value.Value).Value,
                    Direction = config.TargetSaturation.Direction,
                    MaxExisting = config.TargetSaturation.MaxExisting,
                };

                ValidateTargetSaturationHasMatchingGeneratedRelationship(config.Id, targetSaturation, relationships);
            }

            var triggers = config.Triggers
                .Select(trigger =>
                {
                    EnsureKindDeclared(partialGraph, trigger.OnKind, $"Generator '{config.Id}' trigger");

                    DefinitionAttributeId? attributeId = null;
                    if (!string.IsNullOrWhiteSpace(trigger.AttributeName))
                    {
                        attributeId = new DefinitionAttributeId(trigger.AttributeName);
                        if (!partialGraph.Kinds[trigger.OnKind].Attributes.ContainsKey(attributeId.Value))
                        {
                            throw new DefinitionGraphException(
                                $"Generator '{config.Id}' trigger references unknown attribute '{trigger.AttributeName}' on kind '{trigger.OnKind}'.");
                        }
                    }

                    return new DefinitionGeneratorTrigger
                    {
                        OnKind = trigger.OnKind,
                        AttributeId = attributeId,
                        Direction = trigger.Direction,
                    };
                })
                .ToList();

            var target = new DefinitionGeneratorTarget
            {
                Kind = config.Target.Kind,
                Subtypes = config.Target.Subtypes is null ? null : new HashSet<string>(config.Target.Subtypes, StringComparer.Ordinal),
                AllowWilderness = config.Target.AllowWilderness,
            };

            generators[config.Id] = new DefinitionGeneratorNode
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
                Triggers = triggers,
                PulseWeight = config.PulseWeight,
                MaxRunsPerSim = config.MaxRunsPerSim,
                SourceConfig = config,
            };
        }

        return generators;
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
                : new HashSet<TerrainType>(coverage.PreferredTerrains),
            AllowedTerrains = coverage.AllowedTerrains is null
                ? null
                : new HashSet<TerrainType>(coverage.AllowedTerrains),
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
