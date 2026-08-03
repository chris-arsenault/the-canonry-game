namespace Engine.Domain.Graph;

using Engine.Constants;
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

        var target = BuildTarget(config, graph, runtimeIds);
        var secondaryTargets = BuildSecondaryTargets(config, graph, declaredRefs);
        var produces = BuildProducedEntities(config, graph, declaredRefs);
        ValidateProducedReferences(config.Id, produces, declaredRefs);

        var relationships = BuildRelationships(config, graph, runtimeIds, declaredRefs);
        var triggers = BuildTriggers(config, graph);
        var effects = BuildEffects(config, graph, runtimeIds, declaredRefs);

        return new DefinitionGeneratorNode
        {
            Id = config.Id,
            RuntimeId = runtimeIds.GeneratorIdOf(config.Id).Value,
            Name = config.Name,
            Description = config.Description,
            Target = target,
            SecondaryTargets = secondaryTargets,
            Produces = produces,
            Relationships = relationships,
            Effects = effects,
            Triggers = triggers,
            FireWeight = config.FireWeight,
            MaxRunsPerSim = config.MaxRunsPerSim,
            DesperationRatio = ValidatedDesperationRatio(config),
            SourceConfig = config,
        };
    }

    private static DefinitionGeneratorTarget BuildTarget(
        SemanticGeneratorConfig config,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        EnsureKindDeclared(graph, config.Target.Kind, $"Generator '{config.Id}' target");
        ValidateSubtypes(graph, config.Target.Kind, config.Target.Subtypes, $"Generator '{config.Id}' target");
        ValidateRelationshipNames(graph, config.Target.RequiredOutgoingRelationships, $"Generator '{config.Id}' target");
        ValidateRelationshipNames(graph, config.Target.RequiredIncomingRelationships, $"Generator '{config.Id}' target");
        ValidateRelationshipNames(graph, config.Target.RequiredRelationships, $"Generator '{config.Id}' target");
        ValidateRelationshipRequirements(graph, config.Target.RequiredOutgoingRelationshipRequirements, $"Generator '{config.Id}' target");
        ValidateRelationshipRequirements(graph, config.Target.RequiredIncomingRelationshipRequirements, $"Generator '{config.Id}' target");
        ValidateRelationshipRequirements(graph, config.Target.RequiredRelationshipRequirements, $"Generator '{config.Id}' target");
        ValidateTerrainCountRequirements(config.Target.TerrainCountRequirements, $"Generator '{config.Id}' target");
        ValidateRelationshipNames(graph, config.Target.WithoutOutgoingRelationships, $"Generator '{config.Id}' target");
        ValidateRelationshipNames(graph, config.Target.WithoutIncomingRelationships, $"Generator '{config.Id}' target");

        ValidateTagNames(graph, config.Target.RequiredTags, $"Generator '{config.Id}' target");
        ValidateTagNames(graph, config.Target.WithoutTags, $"Generator '{config.Id}' target");
        return new DefinitionGeneratorTarget
        {
            Kind = config.Target.Kind,
            SecondKind = config.Target.SecondKind,
            Subtypes = config.Target.Subtypes is null
                ? null
                : new HashSet<string>(config.Target.Subtypes, StringComparer.Ordinal),
            RequiredTags = CopyRelationshipSet(config.Target.RequiredTags),
            WithoutTags = CopyRelationshipSet(config.Target.WithoutTags),
            RequiredOutgoingRelationships = CopyRelationshipSet(config.Target.RequiredOutgoingRelationships),
            RequiredIncomingRelationships = CopyRelationshipSet(config.Target.RequiredIncomingRelationships),
            RequiredRelationships = CopyRelationshipSet(config.Target.RequiredRelationships),
            RequiredOutgoingRelationshipRequirements = BuildRelationshipRequirements(
                graph,
                runtimeIds,
                config.Target.RequiredOutgoingRelationshipRequirements,
                $"Generator '{config.Id}' target"),
            RequiredIncomingRelationshipRequirements = BuildRelationshipRequirements(
                graph,
                runtimeIds,
                config.Target.RequiredIncomingRelationshipRequirements,
                $"Generator '{config.Id}' target"),
            RequiredRelationshipRequirements = BuildRelationshipRequirements(
                graph,
                runtimeIds,
                config.Target.RequiredRelationshipRequirements,
                $"Generator '{config.Id}' target"),
            TerrainCountRequirements = BuildTerrainCountRequirements(config.Target.TerrainCountRequirements),
            WithoutOutgoingRelationships = CopyRelationshipSet(config.Target.WithoutOutgoingRelationships),
            WithoutIncomingRelationships = CopyRelationshipSet(config.Target.WithoutIncomingRelationships),
            ChronicleHeatMin = config.Target.ChronicleHeatMin,
            ChronicleLegendaryWithinTicks = config.Target.ChronicleLegendaryWithinTicks,
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
                SecondKind = binding.SecondKind,
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
        ValidateProducedCultureSource(generatorId, graph, produced);
        ValidateQuery(graph, produced.Kind, produced.Query, $"{context} query");
    }

    private static float ValidatedDesperationRatio(SemanticGeneratorConfig config)
    {
        if (config.DesperationRatio < 0)
        {
            throw new DefinitionGraphException(
                $"Generator '{config.Id}' DesperationRatio must be non-negative.");
        }

        if (config.DesperationRatio > 0 &&
            config.Produces.Count > 0 &&
            config.Produces.Any(produced => string.IsNullOrWhiteSpace(produced.Culture)))
        {
            throw new DefinitionGraphException(
                $"Generator '{config.Id}' declares DesperationRatio and must give every product an authored fixed Culture.");
        }

        return config.DesperationRatio;
    }

    private static void ValidateProducedCultureSource(
        string generatorId,
        DefinitionGraph graph,
        SemanticProducedEntity produced)
    {
        var hasRef = !string.IsNullOrWhiteSpace(produced.InheritCultureFrom);
        var hasFixed = !string.IsNullOrWhiteSpace(produced.Culture);
        if (produced.CultureSource != ProducedCultureSource.Default)
        {
            if (hasRef || hasFixed)
            {
                throw new DefinitionGraphException(
                    $"Generator '{generatorId}' produce '{produced.Ref}' sets CultureSource.{produced.CultureSource} and must not also declare InheritCultureFrom or Culture.");
            }

            if (produced.CultureSource == ProducedCultureSource.World &&
                string.IsNullOrWhiteSpace(graph.WorldCulture))
            {
                throw new DefinitionGraphException(
                    $"Generator '{generatorId}' produce '{produced.Ref}' uses CultureSource.World but the domain declares no WorldCulture.");
            }

            return;
        }

        if (hasRef == hasFixed)
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' produce '{produced.Ref}' must declare exactly one of InheritCultureFrom or Culture.");
        }

        if (hasFixed && !graph.Cultures.ContainsKey(produced.Culture!))
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' produce '{produced.Ref}' references undeclared culture '{produced.Culture}'.");
        }
    }

    private static DefinitionProducedEntity CreateProducedEntity(
        DefinitionGraph graph,
        SemanticProducedEntity produced) =>
        new()
        {
            Ref = produced.Ref,
            Kind = produced.Kind,
            OnlyWhenTargetKind = produced.OnlyWhenTargetKind,
            Subtype = produced.Subtype,
            Subtypes = produced.Subtypes is null
                ? null
                : new HashSet<string>(produced.Subtypes, StringComparer.Ordinal),
            Role = produced.Role,
            CreateChance = produced.CreateChance,
            CreateIfMissing = produced.CreateIfMissing,
            PhysicalPlacement = produced.PhysicalPlacement,
            PhysicalPlacementPreferredTerrains = CopyTerrainSet(produced.PhysicalPlacementPreferredTerrains),
            PhysicalPlacementAllowedTerrains = CopyTerrainSet(produced.PhysicalPlacementAllowedTerrains),
            PhysicalPlacementAvoidedTerrains = CopyTerrainSet(produced.PhysicalPlacementAvoidedTerrains),
            PhysicalPlacementFallbackTerrain = produced.PhysicalPlacementFallbackTerrain,
            PhysicalPlacementPreferredSites = CopyTerrainSet(produced.PhysicalPlacementPreferredSites),
            FeatureKind = produced.FeatureKind,
            PhysicalPlacementWarmthAffinity = produced.PhysicalPlacementWarmthAffinity,
            PhysicalPlacementFireAffinity = produced.PhysicalPlacementFireAffinity,
            PhysicalPlacementAuroraAffinity = produced.PhysicalPlacementAuroraAffinity,
            PhysicalPlacementPressureAffinity = produced.PhysicalPlacementPressureAffinity,
            SemanticPlacement = produced.SemanticPlacement,
            SemanticPlacementPreferredTerrains = CopyTerrainSet(produced.SemanticPlacementPreferredTerrains),
            SemanticPlacementAllowedTerrains = CopyTerrainSet(produced.SemanticPlacementAllowedTerrains),
            SemanticPlacementAvoidedTerrains = CopyTerrainSet(produced.SemanticPlacementAvoidedTerrains),
            SemanticPlacementFallbackTerrain = produced.SemanticPlacementFallbackTerrain,
            PlacementRef = produced.PlacementRef,
            InheritCultureFrom = produced.InheritCultureFrom,
            Culture = produced.Culture,
            CultureSource = produced.CultureSource,
            CulturePlacement = produced.CulturePlacement,
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

        return new DefinitionGeneratorRelationship
        {
            SourceRef = relationship.SourceRef,
            DestinationRef = relationship.DestinationRef,
            RelationshipId = relationshipId,
            RelationshipRuntimeId = runtimeIds.RelationshipKindIdOf(relationshipId.Value).Value,
            Strength = relationship.Strength,
            ReplaceExistingOutgoingOfSameKind = relationship.ReplaceExistingOutgoingOfSameKind,
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
        };
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
            .Select(effect => BuildEffect(config.Id, config.Target, effect, graph, runtimeIds, declaredRefs))
            .ToList();

    private static IDefinitionGeneratorEffect BuildEffect(
        string generatorId,
        SemanticGeneratorTarget target,
        ISemanticGeneratorEffect effect,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds,
        HashSet<string> declaredRefs) =>
        BuildRefScopedEffect(generatorId, effect, graph, declaredRefs) ??
        BuildTargetScopedEffect(generatorId, target, effect, graph, runtimeIds) ??
        throw new DefinitionGraphException(
            $"Generator '{generatorId}' unsupported effect type '{effect.GetType().Name}'.");

    private static IDefinitionGeneratorEffect? BuildRefScopedEffect(
        string generatorId,
        ISemanticGeneratorEffect effect,
        DefinitionGraph graph,
        HashSet<string> declaredRefs) =>
        effect switch
        {
            SetBindingHistoricalEffect or DemoteBindingStageEffect => BuildBindingEntityEffect(generatorId, effect, declaredRefs),
            SetBindingTagEffect setTag => BuildBindingTagEffect(generatorId, setTag.BindingRef, setTag.Tag, assigned: true, graph, declaredRefs),
            ClearBindingTagEffect clearTag => BuildBindingTagEffect(generatorId, clearTag.BindingRef, clearTag.Tag, assigned: false, graph, declaredRefs),
            ClaimSemanticJurisdictionEffect claim => BuildClaimJurisdictionEffect(generatorId, claim, declaredRefs),
            SetTerrainPatchEffect setTerrainPatch => BuildSetTerrainPatchEffect(generatorId, setTerrainPatch, declaredRefs),
            _ => null,
        };

    private static IDefinitionGeneratorEffect? BuildTargetScopedEffect(
        string generatorId,
        SemanticGeneratorTarget target,
        ISemanticGeneratorEffect effect,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds) =>
        effect switch
        {
            ErodeCultureAtTargetEffect erode => new DefinitionErodeCultureAtTargetEffect(erode.Radius, erode.AmountPerCell),
            RemoveTargetRelationshipsEffect or AdjustTargetRelationshipStrengthEffect => BuildTargetRelationshipEffect(generatorId, target, effect, graph, runtimeIds),
            _ => null,
        };

    private static DefinitionClaimSemanticJurisdictionEffect BuildClaimJurisdictionEffect(
        string generatorId,
        ClaimSemanticJurisdictionEffect claim,
        HashSet<string> declaredRefs)
    {
        if (claim.BindingRef != "$target")
        {
            EnsureDeclaredRef(generatorId, claim.BindingRef, declaredRefs, "BindingRef");
        }

        if (claim.Radius is <= 0 or > 6)
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' jurisdiction radius must be within 1-6.");
        }

        return new DefinitionClaimSemanticJurisdictionEffect(claim.BindingRef, claim.Radius);
    }

    private static DefinitionSetBindingTagEffect BuildBindingTagEffect(
        string generatorId,
        string bindingRef,
        string tag,
        bool assigned,
        DefinitionGraph graph,
        HashSet<string> declaredRefs)
    {
        if (bindingRef != "$target")
        {
            EnsureDeclaredRef(generatorId, bindingRef, declaredRefs, "BindingRef");
        }

        ValidateTagName(generatorId, graph, tag);
        return new DefinitionSetBindingTagEffect(bindingRef, tag, assigned);
    }

    private static void ValidateTagNames(DefinitionGraph graph, IReadOnlySet<string>? tags, string context)
    {
        foreach (var tag in tags ?? (IReadOnlySet<string>)new HashSet<string>(StringComparer.Ordinal))
        {
            if (!graph.Tags.Contains(tag))
            {
                throw new DefinitionGraphException($"{context} references undeclared tag '{tag}'.");
            }
        }
    }

    private static void ValidateTagName(string generatorId, DefinitionGraph graph, string tag)
    {
        if (!graph.Tags.Contains(tag))
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' references undeclared tag '{tag}'.");
        }
    }

    private static IDefinitionGeneratorEffect BuildTargetRelationshipEffect(
        string generatorId,
        SemanticGeneratorTarget target,
        ISemanticGeneratorEffect effect,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds) =>
        effect is RemoveTargetRelationshipsEffect removeRelationships
            ? BuildRemoveTargetRelationshipsEffect(generatorId, target, removeRelationships, graph, runtimeIds)
            : BuildAdjustTargetRelationshipStrengthEffect(generatorId, target, (AdjustTargetRelationshipStrengthEffect)effect, graph, runtimeIds);

    private static IDefinitionGeneratorEffect BuildBindingEntityEffect(
        string generatorId,
        ISemanticGeneratorEffect effect,
        HashSet<string> declaredRefs)
    {
        if (effect is SetBindingHistoricalEffect setHistorical)
        {
            EnsureDeclaredRef(generatorId, setHistorical.BindingRef, declaredRefs, "BindingRef");
            return new DefinitionSetBindingHistoricalEffect(setHistorical.BindingRef);
        }

        var demote = (DemoteBindingStageEffect)effect;
        EnsureDeclaredRef(generatorId, demote.BindingRef, declaredRefs, "BindingRef");
        return new DefinitionDemoteBindingStageEffect(demote.BindingRef);
    }

    private static DefinitionRemoveTargetRelationshipsEffect BuildRemoveTargetRelationshipsEffect(
        string generatorId,
        SemanticGeneratorTarget target,
        RemoveTargetRelationshipsEffect removeRelationships,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        var context = $"Generator '{generatorId}' RemoveTargetRelationships effect";
        var relationshipId = ResolveRelationshipId(graph, removeRelationships.RelationshipKind, context);
        EnsureTriggerInstanceMatchesSoleGate(
            generatorId,
            target,
            removeRelationships.RelationshipKind,
            removeRelationships.InstanceSelection,
            "RemoveTargetRelationships");

        return new DefinitionRemoveTargetRelationshipsEffect(
            runtimeIds.RelationshipKindIdOf(relationshipId.Value).Value,
            removeRelationships.InstanceSelection,
            removeRelationships.IncludeReciprocal);
    }

    private static DefinitionAdjustTargetRelationshipStrengthEffect BuildAdjustTargetRelationshipStrengthEffect(
        string generatorId,
        SemanticGeneratorTarget target,
        AdjustTargetRelationshipStrengthEffect adjustRelationship,
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        var context = $"Generator '{generatorId}' AdjustTargetRelationshipStrength effect";
        var relationshipId = ResolveRelationshipId(graph, adjustRelationship.RelationshipKind, context);
        EnsureTriggerInstanceMatchesSoleGate(
            generatorId,
            target,
            adjustRelationship.RelationshipKind,
            adjustRelationship.InstanceSelection,
            "AdjustTargetRelationshipStrength");

        return new DefinitionAdjustTargetRelationshipStrengthEffect(
            runtimeIds.RelationshipKindIdOf(relationshipId.Value).Value,
            adjustRelationship.Delta,
            adjustRelationship.InstanceSelection,
            adjustRelationship.IncludeReciprocal);
    }

    private static DefinitionSetTerrainPatchEffect BuildSetTerrainPatchEffect(
        string generatorId,
        SetTerrainPatchEffect setTerrainPatch,
        HashSet<string> declaredRefs)
    {
        EnsureDeclaredRef(generatorId, setTerrainPatch.CenterRef, declaredRefs, "CenterRef");
        ValidateSetTerrainPatchEffect(generatorId, setTerrainPatch);

        return new DefinitionSetTerrainPatchEffect(
            setTerrainPatch.CenterRef,
            setTerrainPatch.Terrain,
            setTerrainPatch.Radius,
            setTerrainPatch.MaxCellCount,
            setTerrainPatch.RespectTerrainBoundaries,
            setTerrainPatch.Chance,
            setTerrainPatch.PlaneCode)
        {
            PreferredTerrains = setTerrainPatch.PreferredTerrains is null
                ? null
                : new HashSet<string>(setTerrainPatch.PreferredTerrains, StringComparer.Ordinal),
            AllowedTerrains = setTerrainPatch.AllowedTerrains is null
                ? null
                : new HashSet<string>(setTerrainPatch.AllowedTerrains, StringComparer.Ordinal),
        };
    }

    private static void EnsureTriggerInstanceMatchesSoleGate(
        string generatorId,
        SemanticGeneratorTarget target,
        string relationshipKind,
        GeneratorRelationshipInstanceSelection instanceSelection,
        string effectName)
    {
        if (instanceSelection == GeneratorRelationshipInstanceSelection.TriggerRelationship &&
            !TargetHasOnlyRelationshipGate(target, relationshipKind))
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' {effectName} trigger instance effect must match the sole target relationship gate.");
        }
    }

    private static void ValidateSetTerrainPatchEffect(string generatorId, SetTerrainPatchEffect setTerrainPatch)
    {
        if (string.IsNullOrWhiteSpace(setTerrainPatch.Terrain))
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' SetTerrainPatch effect must name a terrain.");
        }

        if (setTerrainPatch.Radius < 0)
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' SetTerrainPatch effect radius must be non-negative.");
        }

        if (setTerrainPatch.MaxCellCount < 0)
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' SetTerrainPatch effect max cell count must be non-negative.");
        }

        if (setTerrainPatch.Chance is < 0f or > 1f)
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' SetTerrainPatch effect chance must be between 0 and 1.");
        }

        if (setTerrainPatch.PlaneCode is not PlaneCodes.Physical and not PlaneCodes.Semantic)
        {
            throw new DefinitionGraphException(
                $"Generator '{generatorId}' SetTerrainPatch effect plane code must be physical or semantic.");
        }
    }

    private static bool TargetHasOnlyRelationshipGate(SemanticGeneratorTarget target, string relationshipKind)
    {
        var count = 0;
        var found = false;
        CountRelationshipGate(target.RequiredOutgoingRelationships, relationshipKind, ref count, ref found);
        CountRelationshipGate(target.RequiredIncomingRelationships, relationshipKind, ref count, ref found);
        CountRelationshipGate(target.RequiredRelationships, relationshipKind, ref count, ref found);
        CountRelationshipGate(target.RequiredOutgoingRelationshipRequirements, relationshipKind, ref count, ref found);
        CountRelationshipGate(target.RequiredIncomingRelationshipRequirements, relationshipKind, ref count, ref found);
        CountRelationshipGate(target.RequiredRelationshipRequirements, relationshipKind, ref count, ref found);
        return found && count == 1;
    }

    private static void CountRelationshipGate(
        IReadOnlySet<string>? values,
        string relationshipKind,
        ref int count,
        ref bool found)
    {
        if (values is null)
        {
            return;
        }

        count += values.Count;
        found |= values.Contains(relationshipKind);
    }

    private static void CountRelationshipGate(
        IReadOnlyList<SemanticGeneratorRelationshipRequirement> values,
        string relationshipKind,
        ref int count,
        ref bool found)
    {
        count += values.Count;
        found |= values.Any(value => string.Equals(value.RelationshipKind, relationshipKind, StringComparison.Ordinal));
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

    private static HashSet<string>? CopyTerrainSet(IReadOnlySet<string>? terrains) =>
        terrains is null
            ? null
            : new HashSet<string>(terrains, StringComparer.Ordinal);

    private static void ValidateRelationshipRequirements(
        DefinitionGraph graph,
        IReadOnlyList<SemanticGeneratorRelationshipRequirement> requirements,
        string context)
    {
        foreach (var requirement in requirements)
        {
            ResolveRelationshipId(graph, requirement.RelationshipKind, context);
            if (requirement.MinimumStrength is < 0f or > 1f)
            {
                throw new DefinitionGraphException(
                    $"{context} relationship requirement '{requirement.RelationshipKind}' minimum strength must be between 0 and 1.");
            }
        }
    }

    private static List<DefinitionTargetRelationshipRequirement> BuildRelationshipRequirements(
        DefinitionGraph graph,
        global::Engine.Domain.DomainIdRegistry runtimeIds,
        IReadOnlyList<SemanticGeneratorRelationshipRequirement> requirements,
        string context)
    {
        var compiled = new List<DefinitionTargetRelationshipRequirement>(requirements.Count);
        foreach (var requirement in requirements)
        {
            var relationshipId = ResolveRelationshipId(graph, requirement.RelationshipKind, context);
            compiled.Add(new DefinitionTargetRelationshipRequirement
            {
                RelationshipId = relationshipId,
                RelationshipRuntimeId = runtimeIds.RelationshipKindIdOf(relationshipId.Value).Value,
                MinimumStrength = requirement.MinimumStrength,
            });
        }

        return compiled;
    }

    private static void ValidateTerrainCountRequirements(
        IReadOnlyList<SemanticGeneratorTerrainCountRequirement> requirements,
        string context)
    {
        foreach (var requirement in requirements)
        {
            if (string.IsNullOrWhiteSpace(requirement.TerrainName))
            {
                throw new DefinitionGraphException($"{context} terrain count requirement must name a terrain.");
            }

            if (requirement.Radius < 0)
            {
                throw new DefinitionGraphException(
                    $"{context} terrain count requirement '{requirement.TerrainName}' radius must be non-negative.");
            }

            if (requirement.MaximumExclusive <= 0)
            {
                throw new DefinitionGraphException(
                    $"{context} terrain count requirement '{requirement.TerrainName}' maximum must be positive.");
            }
        }
    }

    private static List<DefinitionTargetTerrainCountRequirement> BuildTerrainCountRequirements(
        IReadOnlyList<SemanticGeneratorTerrainCountRequirement> requirements)
    {
        var compiled = new List<DefinitionTargetTerrainCountRequirement>(requirements.Count);
        foreach (var requirement in requirements)
        {
            compiled.Add(new DefinitionTargetTerrainCountRequirement
            {
                TerrainName = requirement.TerrainName,
                Radius = requirement.Radius,
                MaximumExclusive = requirement.MaximumExclusive,
            });
        }

        return compiled;
    }

    private static DefinitionEntityQuery? BuildQuery(
        DefinitionGraph graph,
        EntityKind kind,
        SemanticEntityQuery? query)
    {
        _ = kind;
        if (query is null)
        {
            return null;
        }

        ValidateTagNames(graph, query.RequiredTags, "Generator binding query");
        ValidateTagNames(graph, query.WithoutTags, "Generator binding query");
        if (query.Culture is not null && !graph.Cultures.ContainsKey(query.Culture))
        {
            throw new DefinitionGraphException(
                $"Generator binding query references undeclared culture '{query.Culture}'.");
        }

        return new DefinitionEntityQuery
        {
            Subtypes = query.Subtypes is null
                ? null
                : new HashSet<string>(query.Subtypes, StringComparer.Ordinal),
            RequiredTags = CopyRelationshipSet(query.RequiredTags),
            WithoutTags = CopyRelationshipSet(query.WithoutTags),
            Culture = query.Culture,
            LeaderCultureOnly = query.LeaderCultureOnly,
            AdjacentToTarget = query.AdjacentToTarget,
            RequireTargetCultureRoute = query.RequireTargetCultureRoute,
            WithoutOutgoingRelationships = query.WithoutOutgoingRelationships is null
                ? null
                : new HashSet<string>(query.WithoutOutgoingRelationships, StringComparer.Ordinal),
            WithoutIncomingRelationships = query.WithoutIncomingRelationships is null
                ? null
                : new HashSet<string>(query.WithoutIncomingRelationships, StringComparer.Ordinal),
            TargetCultureMatch = query.TargetCultureMatch,
            RelatedToTargetRelationship = query.RelatedToTargetRelationship,
            RelatedToTargetIncoming = query.RelatedToTargetIncoming,
            RelatedToTargetMinStrength = query.RelatedToTargetMinStrength,
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
