namespace Engine.Domain.Graph;

using Engine.Domain.Config;
using Engine.Kinds;

public static class DefinitionSemanticGraphBuilder
{
    public static void Populate(DefinitionGraph graph, SemanticDomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(config);

        Reset(graph.SemanticFlows);

        var stockById = BuildStocks(graph, config.Stocks);
        var verbById = BuildVerbs(graph, config.Verbs);

        foreach (var flow in config.Flows)
        {
            if (!verbById.TryGetValue(flow.VerbId, out var verb))
            {
                throw new DefinitionGraphException(
                    $"Semantic flow references undeclared verb '{flow.VerbId}'.");
            }

            var targetId = new AttributeStockId(new DefinitionAttributeKey(
                flow.TargetKind,
                new DefinitionAttributeId(flow.TargetAttribute)));
            if (!stockById.TryGetValue(targetId, out var targetStock))
            {
                throw new DefinitionGraphException(
                    $"Semantic flow '{flow.VerbId}' targets undeclared stock '{flow.TargetKind}.{flow.TargetAttribute}'.");
            }

            graph.SemanticFlows.Flows.Add(new SemanticFlowEdge
            {
                Id = $"{flow.VerbId}:{flow.Role}:{flow.TargetKind}.{flow.TargetAttribute}",
                Verb = verb,
                Role = flow.Role,
                TargetStock = targetStock,
                PlaneAxis = flow.PlaneAxis,
                RawSignature = flow.SignatureLabel,
                RawForm = flow.FormLabel,
                Reasoning = flow.Reasoning,
                Mechanism = flow.Mechanism,
                FunctionalForm = flow.FunctionalForm,
                SourceRefs = ConvertRefs(flow.SourceRefs, graph),
            });
        }

        foreach (var modifier in config.Modifiers)
        {
            var sourceId = new AttributeStockId(new DefinitionAttributeKey(
                modifier.SourceKind,
                new DefinitionAttributeId(modifier.SourceAttribute)));
            if (!stockById.TryGetValue(sourceId, out var sourceStock))
            {
                throw new DefinitionGraphException(
                    $"Semantic modifier references undeclared source stock '{modifier.SourceKind}.{modifier.SourceAttribute}'.");
            }

            if (!verbById.TryGetValue(modifier.TargetVerbId, out var targetVerb))
            {
                throw new DefinitionGraphException(
                    $"Semantic modifier references undeclared target verb '{modifier.TargetVerbId}'.");
            }

            graph.SemanticFlows.Modifiers.Add(new SemanticModifierEdge
            {
                Id = $"{modifier.SourceKind}.{modifier.SourceAttribute}->{modifier.TargetVerbId}",
                SourceStock = sourceStock,
                TargetVerbId = modifier.TargetVerbId,
                TargetVerb = targetVerb,
                Direction = modifier.Direction,
                FunctionalForm = modifier.FunctionalForm,
                RawForm = modifier.FormLabel,
            });
        }

    }

    private static Dictionary<SemanticStockId, SemanticStockNode> BuildStocks(
        DefinitionGraph graph,
        IReadOnlyList<SemanticStockConfig> configs)
    {
        var stocks = new Dictionary<SemanticStockId, SemanticStockNode>();
        foreach (var config in configs)
        {
            var id = ResolveStockId(graph, config);
            if (stocks.ContainsKey(id))
            {
                throw new DefinitionGraphException(
                    $"Semantic stock config duplicates '{id.Label}'.");
            }

            var stock = new SemanticStockNode
            {
                Id = id,
                StockClass = config.StockClass,
                FeedbackClass = config.FeedbackClass,
                RequiredFlowCoverage = config.RequiredFlowCoverage,
                RequiredXFlowCoverage = config.RequiredXFlowCoverage,
                RequiredYFlowCoverage = config.RequiredYFlowCoverage,
                RawBounds = config.BoundsLabel,
                RawConservation = config.ConservationLabel,
                CoverageNotes = config.CoverageNotes,
                Notes = config.Notes,
            };

            stocks[id] = stock;
            graph.SemanticFlows.Stocks.Add(stock);
        }

        return stocks;
    }

    private static SemanticStockId ResolveStockId(DefinitionGraph graph, SemanticStockConfig config)
    {
        switch (config)
        {
            case SemanticAttributeStockConfig attributeConfig:
            {
                var key = new DefinitionAttributeKey(
                    attributeConfig.Kind,
                    new DefinitionAttributeId(attributeConfig.AttributeName));
                if (!graph.Attributes.ContainsKey(key))
                {
                    throw new DefinitionGraphException(
                        $"Semantic stock references undeclared attribute '{attributeConfig.Kind}.{attributeConfig.AttributeName}'.");
                }

                return new AttributeStockId(key);
            }

            case SemanticResourceStockConfig resourceConfig:
            {
                var resourceId = new DefinitionResourceId(resourceConfig.ResourceName);
                if (!graph.Resources.ContainsKey(resourceId))
                {
                    throw new DefinitionGraphException(
                        $"Semantic stock references undeclared resource '{resourceConfig.ResourceName}'.");
                }

                return new ResourceStockId(resourceId);
            }

            default:
                throw new DefinitionGraphException(
                    $"Unsupported semantic stock config type '{config.GetType().Name}'.");
        }
    }

    private static Dictionary<string, SemanticVerbNode> BuildVerbs(
        DefinitionGraph graph,
        IReadOnlyList<SemanticVerbConfig> configs)
    {
        var verbs = new Dictionary<string, SemanticVerbNode>(StringComparer.Ordinal);
        foreach (var config in configs)
        {
            if (verbs.ContainsKey(config.Id))
            {
                throw new DefinitionGraphException($"Semantic verb config duplicates '{config.Id}'.");
            }

            var verb = new SemanticVerbNode
            {
                Id = config.Id,
                OwnerType = config.OwnerType,
                OwnerKind = config.OwnerKind,
                Role = config.Role,
                Notes = config.Notes,
                IsFlowless = config.IsFlowless,
            };

            verbs[config.Id] = verb;
            graph.SemanticFlows.Verbs.Add(verb);
        }

        return verbs;
    }

    private static TypedDefinitionRef[] ConvertRefs(IReadOnlyList<Engine.Wiring.AttributeRef> refs, DefinitionGraph graph)
    {
        if (refs.Count == 0)
        {
            return [];
        }

        return refs.Select(reference => ConvertRef(reference, graph)).ToArray();
    }

    private static TypedDefinitionRef ConvertRef(Engine.Wiring.AttributeRef attrRef, DefinitionGraph graph)
    {
        return attrRef.Scope switch
        {
            Engine.Wiring.RefScope.Self or Engine.Wiring.RefScope.Target or Engine.Wiring.RefScope.Location or Engine.Wiring.RefScope.Related =>
                ConvertAttributeScopedRef(attrRef, graph),
            Engine.Wiring.RefScope.Resource =>
                ConvertResourceRef(attrRef, graph),
            Engine.Wiring.RefScope.Pressure =>
                ConvertPressureRef(attrRef, graph),
            Engine.Wiring.RefScope.Structural =>
                new TypedDefinitionRef(attrRef.Scope, new StructuralValueId(attrRef.Name), attrRef.Kind),
            Engine.Wiring.RefScope.MapCell =>
                new TypedDefinitionRef(attrRef.Scope, new MapCellValueId(attrRef.Name), attrRef.Kind),
            Engine.Wiring.RefScope.Relational =>
                ConvertRelationalRef(attrRef, graph),
            _ => throw new DefinitionGraphException($"Unsupported semantic ref scope '{attrRef.Scope}'."),
        };
    }

    private static TypedDefinitionRef ConvertAttributeScopedRef(Engine.Wiring.AttributeRef attrRef, DefinitionGraph graph)
    {
        var attributeId = new DefinitionAttributeId(attrRef.Name);
        if (attrRef.Kind is EntityKind kind)
        {
            var key = new DefinitionAttributeKey(kind, attributeId);
            if (!graph.Attributes.ContainsKey(key))
            {
                throw new DefinitionGraphException(
                    $"Semantic ref uses undeclared attribute '{kind}.{attrRef.Name}'.");
            }
        }

        return new TypedDefinitionRef(
            attrRef.Scope,
            new AttributeValueId(attributeId),
            attrRef.Kind,
            TryParseRelationship(attrRef.RelationshipKind, graph));
    }

    private static TypedDefinitionRef ConvertResourceRef(Engine.Wiring.AttributeRef attrRef, DefinitionGraph graph)
    {
        var resourceId = new DefinitionResourceId(attrRef.Name);
        if (!graph.Resources.ContainsKey(resourceId))
        {
            throw new DefinitionGraphException(
                $"Semantic ref uses undeclared resource '{attrRef.Name}'.");
        }

        return new TypedDefinitionRef(attrRef.Scope, new ResourceValueId(resourceId), attrRef.Kind);
    }

    private static TypedDefinitionRef ConvertPressureRef(Engine.Wiring.AttributeRef attrRef, DefinitionGraph graph)
    {
        var pressureId = new DefinitionPressureId(attrRef.Name);
        if (!graph.Pressures.ContainsKey(pressureId))
        {
            throw new DefinitionGraphException(
                $"Semantic ref uses undeclared pressure '{attrRef.Name}'.");
        }

        return new TypedDefinitionRef(attrRef.Scope, new PressureValueId(pressureId), attrRef.Kind);
    }

    private static TypedDefinitionRef ConvertRelationalRef(Engine.Wiring.AttributeRef attrRef, DefinitionGraph graph) =>
        new(
            attrRef.Scope,
            new RelationalValueId(attrRef.Name),
            attrRef.Kind,
            TryParseRelationship(attrRef.RelationshipKind, graph));

    private static DefinitionRelationshipId? TryParseRelationship(string? relationshipKind, DefinitionGraph graph)
    {
        if (string.IsNullOrWhiteSpace(relationshipKind))
        {
            return null;
        }

        var relationshipId = new DefinitionRelationshipId(relationshipKind);
        if (!graph.Relationships.ContainsKey(relationshipId))
        {
            throw new DefinitionGraphException(
                $"Semantic ref uses undeclared relationship '{relationshipKind}'.");
        }

        return relationshipId;
    }

    private static void Reset(SemanticFlowGraph graph)
    {
        graph.Stocks.Clear();
        graph.Verbs.Clear();
        graph.Flows.Clear();
        graph.Modifiers.Clear();
        graph.Couplings.Clear();
        graph.Diagnostics.Clear();
    }
}
