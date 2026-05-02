namespace Engine.Domain.Compilation;

using Engine.Behaviors;
using Engine.Domain.Compilation.KernelData;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Gambits;
using Engine.Gameplay;
using Engine.Kinds;
using Engine.Resources;
using Engine.Wiring;
using Engine.WorldGen;

public static partial class DomainCompiler
{
    private static BehaviorProgramEndpoint EndpointCode(TransferEndpoint endpoint) => endpoint switch
    {
        TransferEndpoint.Target => BehaviorProgramEndpoint.Target,
        _ => BehaviorProgramEndpoint.Self,
    };

    private static int PlaneCode(PrimaryPlane plane) =>
        plane == PrimaryPlane.Semantic ? 2 : 1;

    private static int GameplayPlaneCode(string plane) =>
        string.Equals(plane, "semantic", StringComparison.OrdinalIgnoreCase) ? 2 : 1;

    private static RelationshipProgram BuildRelationshipProgram(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var relationships = new List<RelationshipRuntimeRule>();
        var maintenanceRules = new List<RelationshipMaintenanceRuntimeRule>();
        foreach (var relationship in config.Relationships)
        {
            var relationshipKindId = ids.RelationshipKindIdOf(relationship.Name).Value;
            relationships.Add(new RelationshipRuntimeRule(
                relationshipKindId,
                relationship.CullThreshold,
                relationship.TargetFollowsSourcePhysical));
            foreach (var maintenance in relationship.Maintenance)
            {
                maintenanceRules.Add(new RelationshipMaintenanceRuntimeRule(
                    relationshipKindId,
                    RelationshipMaintenanceConditionCode(maintenance.Condition, maintenance.Plane),
                    RelationshipMaintenancePlaneCode(maintenance.Condition, maintenance.Plane),
                    maintenance.MaxDistance,
                    maintenance.Threshold,
                    maintenance.Delta));
            }
        }

        return new RelationshipProgram { Relationships = relationships, MaintenanceRules = maintenanceRules };
    }

    private static int RelationshipMaintenanceConditionCode(
        RelationshipMaintenanceCondition condition,
        string? plane) =>
        condition switch
        {
            RelationshipMaintenanceCondition.Always => RelationshipMaintenanceConditionCodes.Always,
            RelationshipMaintenanceCondition.SameCulture => RelationshipMaintenanceConditionCodes.SameCulture,
            RelationshipMaintenanceCondition.DifferentCulture => RelationshipMaintenanceConditionCodes.DifferentCulture,
            RelationshipMaintenanceCondition.DistanceWithin => RelationshipMaintenanceDistanceConditionCode(plane),
            RelationshipMaintenanceCondition.CoPresence => RelationshipMaintenanceCoPresenceConditionCode(plane),
            RelationshipMaintenanceCondition.SourceInfluenceAtTarget => RelationshipMaintenanceConditionCodes.SourceInfluenceAtTarget,
            RelationshipMaintenanceCondition.TargetInfluenceAtSource => RelationshipMaintenanceConditionCodes.TargetInfluenceAtSource,
            RelationshipMaintenanceCondition.ContactMemory => RelationshipMaintenanceConditionCodes.ContactMemory,
            _ => throw new InvalidOperationException(
                $"Unsupported relationship maintenance condition '{condition}'."),
        };

    private static int RelationshipMaintenancePlaneCode(
        RelationshipMaintenanceCondition condition,
        string? plane)
    {
        if (RelationshipMaintenanceUsesPlane(condition))
        {
            return RequiredRelationshipMaintenancePlaneCode(condition, plane);
        }

        return PlaneCodes.None;
    }

    private static bool RelationshipMaintenanceUsesPlane(RelationshipMaintenanceCondition condition) =>
        condition switch
        {
            RelationshipMaintenanceCondition.DistanceWithin => true,
            RelationshipMaintenanceCondition.CoPresence => true,
            RelationshipMaintenanceCondition.ContactMemory => true,
            RelationshipMaintenanceCondition.SourceInfluenceAtTarget => true,
            RelationshipMaintenanceCondition.TargetInfluenceAtSource => true,
            _ => false,
        };

    private static int RelationshipMaintenanceDistanceConditionCode(string? plane) =>
        RequiredRelationshipMaintenancePlaneCode(RelationshipMaintenanceCondition.DistanceWithin, plane) == PlaneCodes.Semantic
            ? RelationshipMaintenanceConditionCodes.SemanticDistanceWithin
            : RelationshipMaintenanceConditionCodes.PhysicalDistanceWithin;

    private static int RelationshipMaintenanceCoPresenceConditionCode(string? plane) =>
        RequiredRelationshipMaintenancePlaneCode(RelationshipMaintenanceCondition.CoPresence, plane) == PlaneCodes.Semantic
            ? RelationshipMaintenanceConditionCodes.SemanticCoPresence
            : RelationshipMaintenanceConditionCodes.PhysicalCoPresence;

    private static int RequiredRelationshipMaintenancePlaneCode(
        RelationshipMaintenanceCondition condition,
        string? plane)
    {
        if (string.IsNullOrWhiteSpace(plane))
        {
            throw new InvalidOperationException(
                $"Relationship maintenance condition '{condition}' requires an explicit plane.");
        }

        return GameplayPlaneCode(plane);
    }

    private static CompiledTerrainData BuildTerrainData(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var terrains = new List<CompiledTerrainDefinition>();
        var boundaryGroups = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var terrain in config.Terrains)
        {
            terrains.Add(new CompiledTerrainDefinition(
                ids.TerrainIdOf(terrain.Name).Value,
                BoundaryGroupId(boundaryGroups, terrain.BoundaryGroup)));
        }

        return new CompiledTerrainData { Terrains = terrains };
    }

    private static PressureProgram BuildPressureProgram(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var pressures = new List<PressureRuntimeRule>();
        foreach (var pressure in config.Pressures)
        {
            pressures.Add(new PressureRuntimeRule(
                ids.PressureIdOf(pressure.Name).Value,
                PressureScope(pressure.Scope),
                pressure.Baseline,
                pressure.HomeostasisRate,
                pressure.SmoothingFactor));
        }

        return new PressureProgram { Pressures = pressures };
    }

    private static DegeneracyProgram BuildDegeneracyProgram(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var detectors = new List<CompiledDegeneracyDetectorRule>();
        foreach (var detector in config.Degeneracy.Detectors)
        {
            detectors.Add(CompileDegeneracyDetector(config, ids, detector));
        }

        return new DegeneracyProgram { Detectors = detectors };
    }

    private static CompiledDegeneracyDetectorRule CompileDegeneracyDetector(
        DomainConfig config,
        DomainIdRegistry ids,
        DegeneracyDetectorConfig detector)
    {
        EnsureKind(config, detector.PrimaryKind, $"Degeneracy detector '{detector.Id}' primary kind");
        ValidateDegeneracyThresholds(detector);

        var secondaryKindId = 0;
        var attributeId = 0;
        var relationshipKindId = 0;

        switch (detector.Metric)
        {
            case DegeneracyMetric.AttributeDeclineRatio:
                attributeId = ResolveDegeneracyAttributeId(config, ids, detector.PrimaryKind, detector.Attribute, detector.Id);
                EnsureNoSecondaryKind(detector);
                EnsureNoRelationshipKind(detector);
                break;
            case DegeneracyMetric.RelationshipEndpointDominanceRatio:
                secondaryKindId = ResolveDegeneracySecondaryKindId(config, ids, detector);
                relationshipKindId = ResolveDegeneracyRelationshipKindId(config, ids, detector.RelationshipKind, detector.Id);
                EnsureNoAttribute(detector);
                break;
            case DegeneracyMetric.ActiveKindCount:
                EnsureNoSecondaryKind(detector);
                EnsureNoAttribute(detector);
                EnsureNoRelationshipKind(detector);
                break;
            default:
                throw new InvalidOperationException(
                    $"Degeneracy detector '{detector.Id}' uses unsupported metric '{detector.Metric}'.");
        }

        return new CompiledDegeneracyDetectorRule(
            ids.DetectorIdOf(detector.Id).Value,
            ToDegeneracyProgramMetric(detector.Metric),
            ToDegeneracyProgramThresholdDirection(detector.ThresholdDirection),
            ToDegeneracyProgramSeverity(detector.NoSubjectsSeverity),
            ids.ReasonCodeIdOf(detector.ReasonCode).Value,
            ids.KindIdOf(detector.PrimaryKind).Value,
            secondaryKindId,
            attributeId,
            relationshipKindId,
            ToDegeneracyProgramRelationshipEndpoint(detector.DominantRelationshipEndpoint),
            detector.WarningThreshold,
            detector.CriticalThreshold);
    }

    private static int ResolveDegeneracySecondaryKindId(
        DomainConfig config,
        DomainIdRegistry ids,
        DegeneracyDetectorConfig detector)
    {
        if (detector.SecondaryKind is not { } secondaryKind)
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' requires SecondaryKind for metric '{detector.Metric}'.");
        }

        EnsureKind(config, secondaryKind, $"Degeneracy detector '{detector.Id}' secondary kind");
        return ids.KindIdOf(secondaryKind).Value;
    }

    private static int ResolveDegeneracyAttributeId(
        DomainConfig config,
        DomainIdRegistry ids,
        EntityKind kind,
        string? attributeName,
        string detectorId)
    {
        if (string.IsNullOrWhiteSpace(attributeName))
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detectorId}' requires Attribute for attribute decline metrics.");
        }

        if (!config.Kinds[kind].Attributes.Any(attribute => string.Equals(attribute.Name, attributeName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detectorId}' references unknown attribute '{attributeName}' on kind '{kind}'.");
        }

        return ids.AttributeIdOf(attributeName).Value;
    }

    private static int ResolveDegeneracyRelationshipKindId(
        DomainConfig config,
        DomainIdRegistry ids,
        string? relationshipKind,
        string detectorId)
    {
        if (string.IsNullOrWhiteSpace(relationshipKind))
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detectorId}' requires RelationshipKind for relationship dominance metrics.");
        }

        if (!config.Relationships.Any(relationship => string.Equals(relationship.Name, relationshipKind, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detectorId}' references unknown relationship kind '{relationshipKind}'.");
        }

        return ids.RelationshipKindIdOf(relationshipKind).Value;
    }

    private static void EnsureKind(DomainConfig config, EntityKind kind, string context)
    {
        if (!config.Kinds.ContainsKey(kind))
        {
            throw new InvalidOperationException($"{context} references unknown kind '{kind}'.");
        }
    }

    private static void EnsureNoSecondaryKind(DegeneracyDetectorConfig detector)
    {
        if (detector.SecondaryKind is not null)
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' does not support SecondaryKind for metric '{detector.Metric}'.");
        }
    }

    private static void EnsureNoAttribute(DegeneracyDetectorConfig detector)
    {
        if (!string.IsNullOrWhiteSpace(detector.Attribute))
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' does not support Attribute for metric '{detector.Metric}'.");
        }
    }

    private static void EnsureNoRelationshipKind(DegeneracyDetectorConfig detector)
    {
        if (!string.IsNullOrWhiteSpace(detector.RelationshipKind))
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' does not support RelationshipKind for metric '{detector.Metric}'.");
        }
    }

    private static void ValidateDegeneracyThresholds(DegeneracyDetectorConfig detector)
    {
        if (detector.WarningThreshold < 0f || detector.CriticalThreshold < 0f)
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' thresholds must be non-negative.");
        }

        if (detector.ThresholdDirection == DegeneracyThresholdDirection.HighIsBad &&
            detector.CriticalThreshold < detector.WarningThreshold)
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' critical threshold must be greater than or equal to warning threshold.");
        }

        if (detector.ThresholdDirection == DegeneracyThresholdDirection.LowIsBad &&
            detector.CriticalThreshold > detector.WarningThreshold)
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' critical threshold must be less than or equal to warning threshold.");
        }
    }

    private static GeneratorProgram BuildGeneratorProgram(
        DomainConfig config,
        DomainIdRegistry ids,
        DefinitionGraph graph)
    {
        var terrainByName = config.Terrains.ToDictionary(
            static terrain => terrain.Name,
            StringComparer.Ordinal);
        var generators = new List<CompiledGeneratorRule>();

        foreach (var generator in graph.Generators.Values)
        {
            var refCodes = BuildGeneratorRefCodes(generator);
            generators.Add(new CompiledGeneratorRule(
                generator.RuntimeId,
                GeneratorProgramConstants.DefaultCadenceTicks,
                ids.KindIdOf(generator.Target.Kind).Value,
                CompileGeneratorSubtypeIds(generator.Target.Subtypes, ids),
                generator.Target.AllowWilderness,
                CompileGeneratorTargetRelationshipRequirements(generator.Target, ids),
                Math.Max(generator.PulseWeight, 0),
                generator.MaxRunsPerSim ?? 0,
                CompileGeneratorTargetSaturation(generator.TargetSaturation),
                CompileGeneratorTriggers(generator.Triggers, ids),
                CompileGeneratorBindings(generator.SecondaryTargets, ids, refCodes),
                CompileGeneratorProducedEntities(generator.Produces, ids, terrainByName, refCodes),
                CompileGeneratorRelationships(generator.Relationships, graph, refCodes),
                CompileGeneratorEffects(generator.Effects, refCodes)));
        }

        return new GeneratorProgram { Generators = generators };
    }

    private static Dictionary<string, int> BuildGeneratorRefCodes(DefinitionGeneratorNode generator)
    {
        var refCodes = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["$target"] = GeneratorProgramConstants.TargetReferenceCode,
        };
        var next = GeneratorProgramConstants.TargetReferenceCode + 1;

        foreach (var binding in generator.SecondaryTargets)
        {
            refCodes.Add(binding.Ref, next++);
        }

        foreach (var produced in generator.Produces)
        {
            refCodes.Add(produced.Ref, next++);
        }

        return refCodes;
    }

    private static GeneratorTargetSaturationRule? CompileGeneratorTargetSaturation(
        DefinitionTargetSaturation? saturation)
    {
        return saturation is null
            ? null
            : new GeneratorTargetSaturationRule(
                saturation.RelationshipRuntimeId,
                saturation.RequiredFactRuntimeId,
                ToGeneratorSaturationDirection(saturation.Direction),
                saturation.MaxExisting);
    }

    private static GeneratorTargetRelationshipRequirements CompileGeneratorTargetRelationshipRequirements(
        DefinitionGeneratorTarget target,
        DomainIdRegistry ids) =>
        new(
            CompileRelationshipKindIds(target.RequiredOutgoingRelationships, ids),
            CompileRelationshipKindIds(target.RequiredIncomingRelationships, ids),
            CompileRelationshipKindIds(target.RequiredRelationships, ids));

    private static List<GeneratorTriggerRule> CompileGeneratorTriggers(
        IReadOnlyList<DefinitionGeneratorTrigger> triggers,
        DomainIdRegistry ids)
    {
        var compiled = new List<GeneratorTriggerRule>();
        foreach (var trigger in triggers)
        {
            var direction = ToGeneratorTriggerDirection(trigger.Direction);
            compiled.Add(new GeneratorTriggerRule(
                ids.KindIdOf(trigger.OnKind).Value,
                trigger.AttributeId is { } attributeId ? ids.AttributeIdOf(attributeId.Value).Value : 0,
                direction,
                GeneratorTriggerThreshold(direction)));
        }

        return compiled;
    }

    private static List<GeneratorBindingRule> CompileGeneratorBindings(
        IReadOnlyList<DefinitionGeneratorBindingTarget> bindings,
        DomainIdRegistry ids,
        Dictionary<string, int> refCodes)
    {
        var compiled = new List<GeneratorBindingRule>();
        foreach (var binding in bindings)
        {
            compiled.Add(new GeneratorBindingRule(
                refCodes[binding.Ref],
                ids.KindIdOf(binding.Kind).Value,
                CompileGeneratorQuery(binding.Query, ids)));
        }

        return compiled;
    }

    private static List<GeneratorProducedEntityRule> CompileGeneratorProducedEntities(
        IReadOnlyList<DefinitionProducedEntity> producedEntities,
        DomainIdRegistry ids,
        Dictionary<string, TerrainConfig> terrainByName,
        Dictionary<string, int> refCodes)
    {
        var compiled = new List<GeneratorProducedEntityRule>();
        foreach (var produced in producedEntities)
        {
            compiled.Add(new GeneratorProducedEntityRule(
                refCodes[produced.Ref],
                ids.KindIdOf(produced.Kind).Value,
                !string.IsNullOrWhiteSpace(produced.Subtype)
                    ? ids.SubtypeIdOf(produced.Subtype).Value
                    : 0,
                CompileGeneratorSubtypeIds(produced.Subtypes, ids),
                produced.CreateIfMissing,
                produced.Query is not null,
                produced.Query is null
                    ? null
                    : CompileGeneratorQuery(produced.Query, ids, ProducedQuerySubtypes(produced)),
                ToGeneratorPlacement(produced.PhysicalPlacement),
                ToGeneratorPlacement(produced.SemanticPlacement),
                refCodes[produced.PlacementRef],
                refCodes[produced.InheritCultureFrom],
                produced.CreateChance,
                CompileGeneratorCoverage(produced.Coverage, terrainByName, refCodes, ids)));
        }

        return compiled;
    }

    private static GeneratorEntityQueryRule? CompileGeneratorQuery(
        DefinitionEntityQuery? query,
        DomainIdRegistry ids,
        IEnumerable<string>? subtypeSource = null)
    {
        if (query is null)
        {
            return null;
        }

        return new GeneratorEntityQueryRule(
            CompileGeneratorSubtypeIds(subtypeSource ?? query.Subtypes, ids),
            query.AdjacentToTarget,
            ToGeneratorCultureMatch(query.TargetCultureMatch),
            CompileRelationshipKindIds(query.WithoutOutgoingRelationships, ids),
            CompileRelationshipKindIds(query.WithoutIncomingRelationships, ids),
            CompileGeneratorAttributeConditions(query.AttributeConditions, ids));
    }

    private static List<GeneratorAttributeConditionRule> CompileGeneratorAttributeConditions(
        IReadOnlyList<DefinitionQueryAttributeCondition> conditions,
        DomainIdRegistry ids)
    {
        var compiled = new List<GeneratorAttributeConditionRule>();
        foreach (var condition in conditions)
        {
            var direction = ToGeneratorTriggerDirection(condition.Direction);
            compiled.Add(new GeneratorAttributeConditionRule(
                ids.AttributeIdOf(condition.AttributeId.Value).Value,
                direction,
                GeneratorTriggerThreshold(direction)));
        }

        return compiled;
    }

    private static GeneratorCoverageRule? CompileGeneratorCoverage(
        DefinitionRegionCoverage? coverage,
        Dictionary<string, TerrainConfig> terrainByName,
        Dictionary<string, int> refCodes,
        DomainIdRegistry ids)
    {
        if (coverage is null)
        {
            return null;
        }

        TerrainConfig? transformTerrain = null;
        if (!string.IsNullOrWhiteSpace(coverage.TransformTerrain))
        {
            transformTerrain = ResolveTerrain(terrainByName, coverage.TransformTerrain);
        }

        return new GeneratorCoverageRule(
            ToGeneratorCoverageMode(coverage.Mode),
            !string.IsNullOrWhiteSpace(coverage.SourceRef)
                ? refCodes[coverage.SourceRef]
                : 0,
            coverage.DesiredCellCount,
            coverage.MinimumCellCount,
            coverage.MinimumSourceRemainingCellCount,
            coverage.RespectTerrainBoundaries,
            transformTerrain is null ? 0 : ids.TerrainIdOf(transformTerrain.Name).Value,
            CompileTerrainIds(coverage.PreferredTerrains, terrainByName, ids),
            CompileTerrainIds(coverage.AllowedTerrains, terrainByName, ids));
    }

    private static List<GeneratorRelationshipRule> CompileGeneratorRelationships(
        IReadOnlyList<DefinitionGeneratorRelationship> relationships,
        DefinitionGraph graph,
        Dictionary<string, int> refCodes)
    {
        var compiled = new List<GeneratorRelationshipRule>();
        foreach (var relationship in relationships)
        {
            compiled.Add(new GeneratorRelationshipRule(
                refCodes[relationship.SourceRef],
                refCodes[relationship.DestinationRef],
                relationship.RelationshipRuntimeId,
                relationship.Strength,
                relationship.ReplaceExistingOutgoingOfSameKind ||
                graph.Relationships[relationship.RelationshipId].SourceConfig.IsExclusive,
                relationship.FactRuntimeIds.Order().ToList()));
        }

        return compiled;
    }

    private static List<GeneratorEffectRule> CompileGeneratorEffects(
        IReadOnlyList<IDefinitionGeneratorEffect> effects,
        Dictionary<string, int> refCodes)
    {
        var compiled = new List<GeneratorEffectRule>();
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case DefinitionSetBindingHistoricalEffect setHistorical:
                    compiled.Add(new GeneratorEffectRule(
                        GeneratorEffectTypeCodes.SetBindingHistorical,
                        refCodes[setHistorical.BindingRef]));
                    break;
                case DefinitionRemoveTargetRelationshipsEffect removeRelationships:
                    compiled.Add(new GeneratorEffectRule(
                        GeneratorEffectTypeCodes.RemoveTargetRelationships,
                        removeRelationships.RelationshipKindId));
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported generator effect type '{effect.GetType().Name}'.");
            }
        }

        return compiled;
    }

    private static List<int> CompileGeneratorSubtypeIds(
        IEnumerable<string>? subtypes,
        DomainIdRegistry ids)
    {
        var compiled = new List<int>();
        if (subtypes is null)
        {
            return compiled;
        }

        foreach (var subtype in subtypes)
        {
            compiled.Add(ids.SubtypeIdOf(subtype).Value);
        }

        return compiled;
    }

    private static List<int> CompileRelationshipKindIds(
        IEnumerable<string>? relationshipKinds,
        DomainIdRegistry ids)
    {
        var compiled = new List<int>();
        if (relationshipKinds is null)
        {
            return compiled;
        }

        foreach (var relationshipKind in relationshipKinds)
        {
            compiled.Add(ids.RelationshipKindIdOf(relationshipKind).Value);
        }

        return compiled;
    }

    private static List<int> CompileTerrainIds(
        IEnumerable<string>? terrains,
        Dictionary<string, TerrainConfig> terrainByName,
        DomainIdRegistry ids)
    {
        var compiled = new List<int>();
        if (terrains is null)
        {
            return compiled;
        }

        foreach (var terrain in terrains)
        {
            compiled.Add(ids.TerrainIdOf(ResolveTerrain(terrainByName, terrain).Name).Value);
        }

        return compiled;
    }

    private static TerrainConfig ResolveTerrain(
        Dictionary<string, TerrainConfig> terrainByName,
        string terrainName)
    {
        if (!terrainByName.TryGetValue(terrainName, out var terrain))
        {
            throw new InvalidOperationException($"Generator coverage references undeclared terrain '{terrainName}'.");
        }

        return terrain;
    }

    private static IEnumerable<string>? ProducedQuerySubtypes(DefinitionProducedEntity produced)
    {
        if (produced.Query?.Subtypes is { Count: > 0 } querySubtypes)
        {
            return querySubtypes;
        }

        if (!string.IsNullOrWhiteSpace(produced.Subtype))
        {
            return [produced.Subtype];
        }

        return produced.Subtypes;
    }

    private static GeneratorProgramSaturationDirection ToGeneratorSaturationDirection(
        TargetSaturationDirection direction) => direction switch
        {
            TargetSaturationDirection.OutgoingFromTarget => GeneratorProgramSaturationDirection.OutgoingFromTarget,
            TargetSaturationDirection.IncomingToTarget => GeneratorProgramSaturationDirection.IncomingToTarget,
            TargetSaturationDirection.Symmetric => GeneratorProgramSaturationDirection.Symmetric,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };

    private static GeneratorProgramPlacement ToGeneratorPlacement(SemanticPlacement placement) => placement switch
    {
        SemanticPlacement.On => GeneratorProgramPlacement.On,
        SemanticPlacement.Near => GeneratorProgramPlacement.Near,
        SemanticPlacement.Across => GeneratorProgramPlacement.Across,
        SemanticPlacement.InRegionLocality => GeneratorProgramPlacement.InCoverageLocality,
        _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, null),
    };

    private static GeneratorProgramCultureMatch ToGeneratorCultureMatch(
        QueryTargetCultureMatch cultureMatch) => cultureMatch switch
        {
            QueryTargetCultureMatch.Any => GeneratorProgramCultureMatch.Any,
            QueryTargetCultureMatch.SameAsTarget => GeneratorProgramCultureMatch.SameAsTarget,
            QueryTargetCultureMatch.DifferentFromTarget => GeneratorProgramCultureMatch.DifferentFromTarget,
            _ => throw new ArgumentOutOfRangeException(nameof(cultureMatch), cultureMatch, null),
        };

    private static GeneratorProgramTriggerDirection ToGeneratorTriggerDirection(
        TriggerDirection direction) => direction switch
        {
            TriggerDirection.High => GeneratorProgramTriggerDirection.High,
            TriggerDirection.Low => GeneratorProgramTriggerDirection.Low,
            TriggerDirection.Critical => GeneratorProgramTriggerDirection.Critical,
            TriggerDirection.Any => GeneratorProgramTriggerDirection.Any,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };

    private static GeneratorProgramCoverageMode ToGeneratorCoverageMode(CoveragePlacementMode mode) => mode switch
    {
        CoveragePlacementMode.CreateNew => GeneratorProgramCoverageMode.CreateNew,
        CoveragePlacementMode.SplitFromExisting => GeneratorProgramCoverageMode.SplitFromExisting,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    private static float GeneratorTriggerThreshold(GeneratorProgramTriggerDirection direction) => direction switch
    {
        GeneratorProgramTriggerDirection.High => StandardNeedScale.Comfortable,
        GeneratorProgramTriggerDirection.Low => StandardNeedScale.Concern,
        GeneratorProgramTriggerDirection.Critical => StandardNeedScale.Critical,
        _ => 0f,
    };

    private static DegeneracyProgramMetric ToDegeneracyProgramMetric(DegeneracyMetric metric) => metric switch
    {
        DegeneracyMetric.AttributeDeclineRatio => DegeneracyProgramMetric.AttributeDeclineRatio,
        DegeneracyMetric.RelationshipEndpointDominanceRatio => DegeneracyProgramMetric.RelationshipEndpointDominanceRatio,
        DegeneracyMetric.ActiveKindCount => DegeneracyProgramMetric.ActiveKindCount,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null),
    };

    private static DegeneracyProgramThresholdDirection ToDegeneracyProgramThresholdDirection(
        DegeneracyThresholdDirection direction) => direction switch
        {
            DegeneracyThresholdDirection.HighIsBad => DegeneracyProgramThresholdDirection.HighIsBad,
            DegeneracyThresholdDirection.LowIsBad => DegeneracyProgramThresholdDirection.LowIsBad,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };

    private static DegeneracyProgramRelationshipEndpoint ToDegeneracyProgramRelationshipEndpoint(
        DegeneracyRelationshipEndpoint endpoint) => endpoint switch
        {
            DegeneracyRelationshipEndpoint.Source => DegeneracyProgramRelationshipEndpoint.Source,
            DegeneracyRelationshipEndpoint.Target => DegeneracyProgramRelationshipEndpoint.Target,
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, null),
        };

    private static DegeneracyProgramSeverity ToDegeneracyProgramSeverity(DegeneracySeverity severity) => severity switch
    {
        DegeneracySeverity.Normal => DegeneracyProgramSeverity.Normal,
        DegeneracySeverity.Warning => DegeneracyProgramSeverity.Warning,
        DegeneracySeverity.Critical => DegeneracyProgramSeverity.Critical,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null),
    };

    private static BehaviorProgramEffectScope EffectScope(AttributeRef reference) => reference.Scope switch
    {
        RefScope.Self => BehaviorProgramEffectScope.SelfAttribute,
        RefScope.Target => BehaviorProgramEffectScope.TargetAttribute,
        RefScope.Resource => BehaviorProgramEffectScope.Resource,
        RefScope.Structural => BehaviorProgramEffectScope.Structural,
        RefScope.Relational => BehaviorProgramEffectScope.Relational,
        _ => throw new InvalidOperationException($"Unsupported behavior effect scope '{reference.Scope}'."),
    };

    private static int EffectValueId(AttributeRef reference, DomainIdRegistry ids) => reference.Scope switch
    {
        RefScope.Self or RefScope.Target => ids.AttributeIdOf(reference.Name).Value,
        RefScope.Resource => ids.ResourceIdOf(reference.Name).Value,
        RefScope.Structural => ids.StructuralFactIdOf(reference.Name).Value,
        RefScope.Relational => ids.RelationshipKindIdOf(reference.RelationshipKind ?? reference.Name).Value,
        _ => throw new InvalidOperationException($"Unsupported behavior effect scope '{reference.Scope}'."),
    };

    private static int BoundaryGroupId(Dictionary<string, int> boundaryGroups, string stableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableName);
        if (boundaryGroups.TryGetValue(stableName, out var id))
        {
            return id;
        }

        id = boundaryGroups.Count + 1;
        boundaryGroups.Add(stableName, id);
        return id;
    }

    private static PressureRuntimeScope PressureScope(Engine.Pressure.PressureScope scope) => scope switch
    {
        Engine.Pressure.PressureScope.Regional => PressureRuntimeScope.Coverage,
        Engine.Pressure.PressureScope.Entity => PressureRuntimeScope.Entity,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
    };

}
