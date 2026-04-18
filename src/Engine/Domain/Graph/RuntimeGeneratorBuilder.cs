namespace Engine.Domain.Graph;

using Engine.Domain.Config;
using Engine.Kinds;

public static class RuntimeGeneratorBuilder
{
    public static IReadOnlyDictionary<string, DefinitionGeneratorNode> Build(
        IReadOnlyList<SemanticGeneratorConfig> semantic,
        DefinitionGraph partialGraph)
    {
        ArgumentNullException.ThrowIfNull(semantic);
        ArgumentNullException.ThrowIfNull(partialGraph);

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

            var produces = new List<DefinitionProducedEntity>(config.Produces.Count);
            foreach (var produced in config.Produces)
            {
                EnsureKindDeclared(partialGraph, produced.Kind, $"Generator '{config.Id}' produce '{produced.Ref}'");
                ValidateSubtype(partialGraph, produced.Kind, produced.Subtype, $"Generator '{config.Id}' produce '{produced.Ref}'");

                if (!declaredRefs.Add(produced.Ref))
                {
                    throw new DefinitionGraphException(
                        $"Generator '{config.Id}' declares duplicate produce ref '{produced.Ref}'.");
                }

                EnsureDeclaredRef(config.Id, produced.InheritCultureFrom, declaredRefs, "InheritCultureFrom");

                produces.Add(new DefinitionProducedEntity
                {
                    Ref = produced.Ref,
                    Kind = produced.Kind,
                    Subtype = produced.Subtype,
                    Role = produced.Role,
                    CreateChance = produced.CreateChance,
                    Placement = produced.Placement,
                    InheritCultureFrom = produced.InheritCultureFrom,
                });
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

                    return new DefinitionGeneratorRelationship
                    {
                        SourceRef = relationship.SourceRef,
                        DestinationRef = relationship.DestinationRef,
                        RelationshipId = relationshipId,
                        Strength = relationship.Strength,
                    };
                })
                .ToList();

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
                SaturationLimits = config.Target.SaturationLimits
                    .Select(limit => new DefinitionSaturationLimit
                    {
                        RelationshipId = ResolveRelationshipId(
                            partialGraph,
                            limit.RelationshipKind,
                            $"Generator '{config.Id}' saturation limit"),
                        Direction = limit.Direction,
                        MaxCount = limit.MaxCount,
                    })
                    .ToList(),
            };

            generators[config.Id] = new DefinitionGeneratorNode
            {
                Id = config.Id,
                Name = config.Name,
                Description = config.Description,
                Target = target,
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
