namespace Engine.Domain.Compilation;

using Engine.Behaviors;
using Engine.Domain.Compilation.KernelData;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Spatial;
using Engine.Gambits;
using Engine.Gameplay;
using Engine.Kinds;
using Engine.Wiring;

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
                KernelFixedPoint.RelationshipStrengthScalar(
                    KernelFixedPoint.RelationshipStrengthUnits(relationship.CullThreshold)),
                relationship.TargetFollowsSourcePhysical,
                relationship.IsExclusive,
                relationship.IsCultureAnchor));
            foreach (var maintenance in relationship.Maintenance)
            {
                maintenanceRules.Add(new RelationshipMaintenanceRuntimeRule(
                    relationshipKindId,
                    RelationshipMaintenanceConditionCode(maintenance.Condition, maintenance.Plane),
                    RelationshipMaintenancePlaneCode(maintenance.Condition, maintenance.Plane),
                    maintenance.MaxDistance,
                    FixedScalar.FromSingle(maintenance.Threshold),
                    KernelFixedPoint.RelationshipStrengthScalar(
                        KernelFixedPoint.RelationshipStrengthDeltaUnits(maintenance.Delta))));
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
        DomainIdRegistry ids,
        Dictionary<string, int> movementProfileIds)
    {
        var terrains = new List<CompiledTerrainDefinition>();
        foreach (var terrain in config.Terrains)
        {
            var terrainId = ids.TerrainIdOf(terrain.Name).Value;
            terrains.Add(new CompiledTerrainDefinition(
                terrainId,
                ids.TerrainBoundaryGroupIdOf(terrain.BoundaryGroup).Value,
                ClampNonNegative(terrain.MovementCost),
                ClampNonNegative(terrain.InfluenceMultiplier),
                ClampNonNegative(terrain.ClaimCostMultiplier),
                ClampNonNegative(terrain.ClaimChanceMultiplier),
                ClampNonNegative(terrain.RoadBuildMultiplier),
                ClampAtLeast(terrain.RoadSpeedMultiplier, 1f),
                CompileTerrainMovementProfiles(terrainId, terrain.MovementProfiles, movementProfileIds),
                CompileTerrainKindProfiles(terrainId, terrain.KindProfiles, ids),
                IsWater: terrain.IsWater));
        }

        return new CompiledTerrainData
        {
            Terrains = terrains,
            CrossingProfiles = CompileCrossingProfiles(config.CrossingProfiles, movementProfileIds),
        };
    }

    private static List<CompiledCrossingProfileDefinition> CompileCrossingProfiles(
        IReadOnlyList<CrossingProfileConfig> profiles,
        Dictionary<string, int> movementProfileIds)
    {
        var compiled = new List<CompiledCrossingProfileDefinition>(profiles.Count);
        foreach (var profile in profiles)
        {
            if (profile.CrossingClass <= PhysicalCrossingCodes.Open ||
                profile.CrossingClass > PhysicalCrossingCodes.ClassMask)
            {
                throw new InvalidOperationException(
                    $"Crossing profile for '{profile.Profile}' names an unknown crossing class {profile.CrossingClass}.");
            }

            compiled.Add(new CompiledCrossingProfileDefinition(
                profile.CrossingClass,
                ResolveMovementProfileId(profile.Profile, movementProfileIds),
                ClampAtLeast(profile.Factor, 1f),
                profile.RoadNegated));
        }

        return compiled;
    }

    private static List<CompiledTerrainMovementProfileDefinition> CompileTerrainMovementProfiles(
        int terrainId,
        IReadOnlyList<TerrainMovementProfileConfig> profiles,
        Dictionary<string, int> movementProfileIds)
    {
        var compiled = new List<CompiledTerrainMovementProfileDefinition>(profiles.Count);
        foreach (var profile in profiles)
        {
            compiled.Add(new CompiledTerrainMovementProfileDefinition(
                terrainId,
                ResolveMovementProfileId(profile.Profile, movementProfileIds),
                ClampNonNegative(profile.MovementCost)));
        }

        return compiled;
    }

    private static List<CompiledTerrainKindProfileDefinition> CompileTerrainKindProfiles(
        int terrainId,
        IReadOnlyList<TerrainKindProfileConfig> profiles,
        DomainIdRegistry ids)
    {
        var compiled = new List<CompiledTerrainKindProfileDefinition>(profiles.Count);
        foreach (var profile in profiles)
        {
            var fieldFlags = TerrainKindProfileFieldFlags(profile);
            if (fieldFlags == 0)
            {
                continue;
            }

            compiled.Add(new CompiledTerrainKindProfileDefinition(
                terrainId,
                ids.KindIdOf(profile.Kind).Value,
                fieldFlags,
                ClampNonNegative(profile.InfluenceMultiplier ?? 0f),
                ClampNonNegative(profile.ClaimCostMultiplier ?? 0f),
                ClampNonNegative(profile.ClaimChanceMultiplier ?? 0f)));
        }

        return compiled;
    }

    private static int TerrainKindProfileFieldFlags(TerrainKindProfileConfig profile)
    {
        var flags = 0;
        if (profile.InfluenceMultiplier.HasValue)
        {
            flags |= TerrainKindProfileFieldCodes.InfluenceMultiplier;
        }

        if (profile.ClaimCostMultiplier.HasValue)
        {
            flags |= TerrainKindProfileFieldCodes.ClaimCostMultiplier;
        }

        if (profile.ClaimChanceMultiplier.HasValue)
        {
            flags |= TerrainKindProfileFieldCodes.ClaimChanceMultiplier;
        }

        return flags;
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
                ids.KindIdOf(generator.Target.Kind).Value,
                CompileGeneratorSubtypeIds(generator.Target.Subtypes, ids),
                CompileGeneratorTargetRelationshipRequirements(generator.Target, terrainByName, ids),
                Math.Max(generator.FireWeight, 0),
                FixedScalar.FromSingle(generator.DesperationRatio),
                generator.DesperationRatio > 0
                    ? ids.CultureIdOf(generator.Produces[0].Culture!).Value
                    : 0,
                CompileTagBits(generator.Target.RequiredTags, ids),
                CompileTagBits(generator.Target.WithoutTags, ids),
                FixedScalar.FromSingle(generator.Target.ChronicleHeatMin),
                generator.Target.ChronicleLegendaryWithinTicks,
                CompileGeneratorTriggers(generator.Triggers, ids),
                CompileGeneratorBindings(generator.SecondaryTargets, ids, refCodes),
                CompileGeneratorProducedEntities(generator.Produces, ids, terrainByName, refCodes, graph.WorldCulture),
                CompileGeneratorRelationships(generator.Relationships, graph, refCodes),
                CompileGeneratorEffects(generator.Effects, terrainByName, ids, refCodes),
                generator.Target.SecondKind is null ? 0 : ids.KindIdOf(generator.Target.SecondKind.Value).Value));
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

    private static GeneratorTargetRelationshipRequirements CompileGeneratorTargetRelationshipRequirements(
        DefinitionGeneratorTarget target,
        Dictionary<string, TerrainConfig> terrainByName,
        DomainIdRegistry ids) =>
        new(
            CompileRelationshipRequirements(target.RequiredOutgoingRelationships, target.RequiredOutgoingRelationshipRequirements, ids),
            CompileRelationshipRequirements(target.RequiredIncomingRelationships, target.RequiredIncomingRelationshipRequirements, ids),
            CompileRelationshipRequirements(target.RequiredRelationships, target.RequiredRelationshipRequirements, ids),
            CompileGeneratorTerrainCountRequirements(target.TerrainCountRequirements, terrainByName, ids),
            CompileRelationshipKindIds(target.WithoutOutgoingRelationships, ids),
            CompileRelationshipKindIds(target.WithoutIncomingRelationships, ids));

    private static List<GeneratorTerrainCountRequirementRule> CompileGeneratorTerrainCountRequirements(
        IReadOnlyList<DefinitionTargetTerrainCountRequirement> requirements,
        Dictionary<string, TerrainConfig> terrainByName,
        DomainIdRegistry ids)
    {
        var compiled = new List<GeneratorTerrainCountRequirementRule>(requirements.Count);
        foreach (var requirement in requirements)
        {
            var terrain = ResolveTerrain(terrainByName, requirement.TerrainName);
            compiled.Add(new GeneratorTerrainCountRequirementRule(
                ids.TerrainIdOf(terrain.Name).Value,
                requirement.Radius,
                requirement.MaximumExclusive));
        }

        return compiled;
    }

    private static List<GeneratorRelationshipRequirementRule> CompileRelationshipRequirements(
        IReadOnlySet<string>? relationshipKinds,
        IReadOnlyList<DefinitionTargetRelationshipRequirement> relationshipRequirements,
        DomainIdRegistry ids)
    {
        var compiled = new List<GeneratorRelationshipRequirementRule>();
        if (relationshipKinds is not null)
        {
            foreach (var relationshipKind in relationshipKinds)
            {
                compiled.Add(new GeneratorRelationshipRequirementRule(
                    ids.RelationshipKindIdOf(relationshipKind).Value,
                    FixedScalar.Zero));
            }
        }

        foreach (var requirement in relationshipRequirements)
        {
            compiled.Add(new GeneratorRelationshipRequirementRule(
                requirement.RelationshipRuntimeId,
                KernelFixedPoint.RelationshipStrengthScalar(
                    KernelFixedPoint.RelationshipStrengthUnits(requirement.MinimumStrength))));
        }

        return compiled;
    }

    private static List<GeneratorTriggerRule> CompileGeneratorTriggers(
        IReadOnlyList<DefinitionGeneratorTrigger> triggers,
        DomainIdRegistry ids)
    {
        var compiled = new List<GeneratorTriggerRule>();
        foreach (var trigger in triggers)
        {
            compiled.Add(new GeneratorTriggerRule(ids.KindIdOf(trigger.OnKind).Value));
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
                CompileGeneratorQuery(binding.Query, ids),
                binding.SecondKind is null ? 0 : ids.KindIdOf(binding.SecondKind.Value).Value));
        }

        return compiled;
    }

    private static int CompileQueryCultureId(DefinitionEntityQuery query, DomainIdRegistry ids)
    {
        if (query.LeaderCultureOnly)
        {
            return GeneratorProgramConstants.LeaderCultureFilter;
        }

        return query.Culture is null ? 0 : ids.CultureIdOf(query.Culture).Value;
    }

    private static int CompileProducedFixedCultureId(
        DefinitionProducedEntity produced,
        DomainIdRegistry ids,
        string? worldCulture)
    {
        if (produced.CultureSource == ProducedCultureSource.World)
        {
            return ids.CultureIdOf(worldCulture!).Value;
        }

        return produced.Culture is null ? 0 : ids.CultureIdOf(produced.Culture).Value;
    }

    private static List<GeneratorProducedEntityRule> CompileGeneratorProducedEntities(
        IReadOnlyList<DefinitionProducedEntity> producedEntities,
        DomainIdRegistry ids,
        Dictionary<string, TerrainConfig> terrainByName,
        Dictionary<string, int> refCodes,
        string? worldCulture)
    {
        var compiled = new List<GeneratorProducedEntityRule>();
        foreach (var produced in producedEntities)
        {
            var physicalPlacement = ToGeneratorPlacement(produced.PhysicalPlacement);
            var semanticPlacement = ToGeneratorPlacement(produced.SemanticPlacement);
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
                physicalPlacement.Placement,
                physicalPlacement.MinRadius,
                physicalPlacement.MaxRadius,
                CompileTerrainIds(produced.PhysicalPlacementPreferredTerrains, terrainByName, ids),
                CompileTerrainIds(produced.PhysicalPlacementAllowedTerrains, terrainByName, ids),
                CompileTerrainIds(produced.PhysicalPlacementAvoidedTerrains, terrainByName, ids),
                CompileOptionalTerrainId(produced.PhysicalPlacementFallbackTerrain, terrainByName, ids),
                semanticPlacement.Placement,
                semanticPlacement.MinRadius,
                semanticPlacement.MaxRadius,
                CompileTerrainIds(produced.SemanticPlacementPreferredTerrains, terrainByName, ids),
                CompileTerrainIds(produced.SemanticPlacementAllowedTerrains, terrainByName, ids),
                CompileTerrainIds(produced.SemanticPlacementAvoidedTerrains, terrainByName, ids),
                CompileOptionalTerrainId(produced.SemanticPlacementFallbackTerrain, terrainByName, ids),
                refCodes[produced.PlacementRef],
                produced.InheritCultureFrom is null ? 0 : refCodes[produced.InheritCultureFrom],
                CompileProducedFixedCultureId(produced, ids, worldCulture),
                ToGeneratorCulturePlacement(produced.CulturePlacement),
                FixedScalar.FromSingle(produced.CreateChance),
                CompileGeneratorCoverage(produced.Coverage, terrainByName, refCodes, ids),
                PhysicalPlacementSiteFlags: CompilePlacementSiteFlags(produced.PhysicalPlacementPreferredSites),
                PhysicalPlacementFeatureKind: CompileFeatureKind(produced.FeatureKind),
                PhysicalPlacementWarmthAffinity: ClampAffinity(produced.PhysicalPlacementWarmthAffinity),
                PhysicalPlacementFireAffinity: ClampAffinity(produced.PhysicalPlacementFireAffinity),
                PhysicalPlacementAuroraAffinity: ClampAffinity(produced.PhysicalPlacementAuroraAffinity),
                PhysicalPlacementPressureAffinity: ClampAffinity(produced.PhysicalPlacementPressureAffinity),
                CultureModeCode: produced.CultureSource == ProducedCultureSource.AnyParticipant
                    ? GeneratorProducedCultureModeCodes.AnyParticipant
                    : GeneratorProducedCultureModeCodes.Default,
                OnlyWhenTargetKindId: produced.OnlyWhenTargetKind is null
                    ? 0
                    : ids.KindIdOf(produced.OnlyWhenTargetKind.Value).Value));
        }

        return compiled;
    }

    private static int CompilePlacementSiteFlags(IReadOnlySet<string>? sites)
    {
        if (sites is null)
        {
            return 0;
        }

        var flags = 0;
        foreach (var site in sites)
        {
            flags |= site switch
            {
                "summit" => PhysicalHexFieldCodes.SummitFlag,
                "thermal" => PhysicalHexFieldCodes.ThermalFlag,
                "anomaly" => PhysicalHexFieldCodes.AnomalyFlag,
                "polynya" => PhysicalHexFieldCodes.PolynyaFlag,
                "crater" => PhysicalHexFieldCodes.CraterFlag,
                _ => throw new InvalidOperationException($"Unknown placement site flag '{site}'."),
            };
        }

        return flags;
    }

    private static FixedScalar ClampAffinity(float value) =>
        FixedScalar.FromSingle(Math.Clamp(value, -1f, 1f));

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
            query.RequireTargetCultureRoute,
            ToGeneratorCultureMatch(query.TargetCultureMatch),
            CompileRelationshipKindIds(query.WithoutOutgoingRelationships, ids),
            CompileRelationshipKindIds(query.WithoutIncomingRelationships, ids),
            CompileTagBits(query.RequiredTags, ids),
            CompileTagBits(query.WithoutTags, ids),
            CompileQueryCultureId(query, ids),
            query.RelatedToTargetRelationship is null ? 0 : ids.RelationshipKindIdOf(query.RelatedToTargetRelationship).Value,
            query.RelatedToTargetIncoming,
            KernelFixedPoint.RelationshipStrengthScalar(
                KernelFixedPoint.RelationshipStrengthUnits(query.RelatedToTargetMinStrength)));
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
            if (transformTerrain.IsWater)
            {
                throw new InvalidOperationException(
                    $"Coverage transform terrain '{transformTerrain.Name}' is water; authored repaints never cross the land/water divide.");
            }
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
                KernelFixedPoint.RelationshipStrengthScalar(
                    KernelFixedPoint.RelationshipStrengthUnits(relationship.Strength)),
                relationship.ReplaceExistingOutgoingOfSameKind ||
                graph.Relationships[relationship.RelationshipId].SourceConfig.IsExclusive));
        }

        return compiled;
    }

    private static List<GeneratorEffectRule> CompileGeneratorEffects(
        IReadOnlyList<IDefinitionGeneratorEffect> effects,
        Dictionary<string, TerrainConfig> terrainByName,
        DomainIdRegistry ids,
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
                case DefinitionDemoteBindingStageEffect demote:
                    compiled.Add(new GeneratorEffectRule(
                        GeneratorEffectTypeCodes.DemoteBindingStage,
                        refCodes[demote.BindingRef]));
                    break;
                case DefinitionRemoveTargetRelationshipsEffect removeRelationships:
                    compiled.Add(new GeneratorEffectRule(
                        GeneratorEffectTypeCodes.RemoveTargetRelationships,
                        removeRelationships.RelationshipKindId,
                        ToGeneratorRelationshipInstanceSelection(removeRelationships.InstanceSelection),
                        removeRelationships.IncludeReciprocal ? 1 : 0));
                    break;
                case DefinitionAdjustTargetRelationshipStrengthEffect adjustRelationship:
                    compiled.Add(new GeneratorEffectRule(
                        GeneratorEffectTypeCodes.AdjustTargetRelationshipStrength,
                        adjustRelationship.RelationshipKindId,
                        ToGeneratorRelationshipInstanceSelection(adjustRelationship.InstanceSelection),
                        adjustRelationship.IncludeReciprocal ? 1 : 0,
                        KernelFixedPoint.RelationshipStrengthScalar(KernelFixedPoint.RelationshipStrengthDeltaUnits(adjustRelationship.Delta))));
                    break;
                case DefinitionErodeCultureAtTargetEffect erode:
                    compiled.Add(new GeneratorEffectRule(
                        GeneratorEffectTypeCodes.ErodeCultureAtTarget,
                        GeneratorProgramConstants.TargetReferenceCode,
                        Value: KernelFixedPoint.CultureMassScalar(KernelFixedPoint.CultureMassUnits(erode.AmountPerCell)),
                        Radius: erode.Radius));
                    break;
                case DefinitionClaimSemanticJurisdictionEffect claim:
                    compiled.Add(new GeneratorEffectRule(
                        GeneratorEffectTypeCodes.ClaimSemanticJurisdiction,
                        refCodes[claim.BindingRef],
                        claim.Radius));
                    break;
                case DefinitionSetBindingTagEffect setTag:
                    compiled.Add(new GeneratorEffectRule(
                        GeneratorEffectTypeCodes.SetBindingTag,
                        refCodes[setTag.BindingRef],
                        ids.TagIdOf(setTag.Tag).Value,
                        setTag.Assigned ? 1 : 0));
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported generator effect type '{effect.GetType().Name}'.");
            }
        }

        return compiled;
    }

    private static int CompileTagBits(IReadOnlySet<string>? tags, DomainIdRegistry ids)
    {
        var bits = 0;
        foreach (var tag in tags ?? (IReadOnlySet<string>)new HashSet<string>(StringComparer.Ordinal))
        {
            bits |= 1 << ids.TagIdOf(tag).Value;
        }

        return bits;
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

    private static int CompileOptionalTerrainId(
        string? terrain,
        Dictionary<string, TerrainConfig> terrainByName,
        DomainIdRegistry ids) =>
        string.IsNullOrWhiteSpace(terrain)
            ? 0
            : ids.TerrainIdOf(ResolveTerrain(terrainByName, terrain).Name).Value;

    private static TerrainConfig ResolveTerrain(
        Dictionary<string, TerrainConfig> terrainByName,
        string terrainName)
    {
        if (!terrainByName.TryGetValue(terrainName, out var terrain))
        {
            throw new InvalidOperationException($"Generator references undeclared terrain '{terrainName}'.");
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

    private static int ToGeneratorRelationshipInstanceSelection(
        GeneratorRelationshipInstanceSelection selection) => selection switch
        {
            GeneratorRelationshipInstanceSelection.AllTargetRelationships => GeneratorRelationshipInstanceSelectionCodes.AllTargetRelationships,
            GeneratorRelationshipInstanceSelection.TriggerRelationship => GeneratorRelationshipInstanceSelectionCodes.TriggerRelationship,
            _ => throw new ArgumentOutOfRangeException(nameof(selection), selection, null),
        };

    private static readonly Dictionary<string, int> FeatureKindCodes =
        new(StringComparer.Ordinal)
        {
            ["river_system"] = PhysicalGeologicalFeatureKindCodes.RiverSystem,
            ["lake"] = PhysicalGeologicalFeatureKindCodes.Lake,
            ["range"] = PhysicalGeologicalFeatureKindCodes.Range,
            ["peak"] = PhysicalGeologicalFeatureKindCodes.Peak,
            ["waterfall"] = PhysicalGeologicalFeatureKindCodes.Waterfall,
            ["crater"] = PhysicalGeologicalFeatureKindCodes.Crater,
            ["plain"] = PhysicalGeologicalFeatureKindCodes.Plain,
        };

    private static int CompileFeatureKind(string? featureKind)
    {
        if (featureKind is null)
        {
            return PhysicalGeologicalFeatureKindCodes.Any;
        }

        return FeatureKindCodes.TryGetValue(featureKind, out var code)
            ? code
            : throw new InvalidOperationException($"Unknown geological feature kind '{featureKind}'.");
    }

    private static (GeneratorProgramPlacement Placement, int MinRadius, int MaxRadius) ToGeneratorPlacement(SemanticPlacement placement) => placement switch
    {
        SemanticPlacement.On => (
            GeneratorProgramPlacement.Area,
            0,
            0),
        SemanticPlacement.Near => (
            GeneratorProgramPlacement.Area,
            GeneratorProgramConstants.DefaultPlacementMinRadius,
            GeneratorProgramConstants.DefaultPlacementMaxRadius),
        SemanticPlacement.Across => (
            GeneratorProgramPlacement.Area,
            GeneratorProgramConstants.FarPlacementMinRadius,
            GeneratorProgramConstants.UnboundedPlacementMaxRadius),
        SemanticPlacement.InRegionLocality => (
            GeneratorProgramPlacement.Locality,
            GeneratorProgramConstants.DefaultPlacementMinRadius,
            GeneratorProgramConstants.UnboundedPlacementMaxRadius),
        SemanticPlacement.Frontier => (
            GeneratorProgramPlacement.Frontier,
            GeneratorProgramConstants.DefaultPlacementMinRadius,
            GeneratorProgramConstants.UnboundedPlacementMaxRadius),
        SemanticPlacement.FeatureAnchor => (
            GeneratorProgramPlacement.FeatureAnchor,
            GeneratorProgramConstants.DefaultPlacementMinRadius,
            GeneratorProgramConstants.UnboundedPlacementMaxRadius),
        _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, null),
    };

    private static GeneratorProgramCulturePlacement ToGeneratorCulturePlacement(GeneratorCulturePlacement placement) => placement switch
    {
        GeneratorCulturePlacement.Ignore => GeneratorProgramCulturePlacement.Ignore,
        GeneratorCulturePlacement.PreferInheritedCulture => GeneratorProgramCulturePlacement.PreferInheritedCulture,
        GeneratorCulturePlacement.HostileIntrusion => GeneratorProgramCulturePlacement.HostileIntrusion,
        GeneratorCulturePlacement.CultureInterior => GeneratorProgramCulturePlacement.CultureInterior,
        GeneratorCulturePlacement.CultureFrontier => GeneratorProgramCulturePlacement.CultureFrontier,
        GeneratorCulturePlacement.CultureRoute => GeneratorProgramCulturePlacement.CultureRoute,
        GeneratorCulturePlacement.Wilderness => GeneratorProgramCulturePlacement.Wilderness,
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

    private static GeneratorProgramCoverageMode ToGeneratorCoverageMode(CoveragePlacementMode mode) => mode switch
    {
        CoveragePlacementMode.CreateNew => GeneratorProgramCoverageMode.CreateNew,
        CoveragePlacementMode.SplitFromExisting => GeneratorProgramCoverageMode.SplitFromExisting,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    private static FixedScalar ClampNonNegative(float value) =>
        FixedScalar.FromSingle(value < 0f ? 0f : value);

    private static FixedScalar ClampAtLeast(float value, float minimum) =>
        FixedScalar.FromSingle(value < minimum ? minimum : value);

}
