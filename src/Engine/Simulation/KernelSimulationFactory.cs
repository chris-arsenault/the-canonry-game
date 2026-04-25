namespace Engine.Simulation;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.IceRemembers;
using Engine.Domain.Loader;
using Engine.Graph;
using Engine.Kernel;
using Engine.Kinds;
using Engine.Map;
using Engine.Pressure;
using Engine.Processes;
using Engine.Resources;
using Engine.Wiring;

public static class KernelSimulationFactory
{
    public static Simulation CreateFromTestMap(
        DomainConfig domainConfig,
        Random? random = null,
        SimulationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(domainConfig);

        KindId.RegisterAll(domainConfig.Kinds.Keys);
        var domainIds = DomainLoader.BuildRuntimeIds(domainConfig);
        var definitionGraph = DefinitionGraphBuilder.Build(domainConfig, domainIds);
        random ??= new Random();
        var worldSeed = TestMapBuilder.SelectAcceptedSeed(random);
        var testWorld = TestMapBuilder.BuildEmpty(worldSeed);
        TestMapBuilder.Populate(testWorld, worldSeed, domainIds);

        var simulation = new Simulation(options ?? new SimulationOptions
        {
            Seed = worldSeed.EntitySeed,
            ProcessSourceCodeId = domainIds.SourceCodeIdOf(DomainRuntimeIdLoader.ProcessSourceCode).Value,
            CardSourceCodeId = domainIds.SourceCodeIdOf(DomainRuntimeIdLoader.CardSourceCode).Value,
            RelationshipCreatedCodeId = domainIds.EventCodeIdOf(DomainRuntimeIdLoader.RelationshipCreatedCode).Value,
            RelationshipCulledCodeId = domainIds.EventCodeIdOf(DomainRuntimeIdLoader.RelationshipCulledCode).Value,
            RelationshipStrengthChangedCodeId = domainIds.EventCodeIdOf(DomainRuntimeIdLoader.RelationshipStrengthChangedCode).Value,
        });

        RegisterDomainRuntime(simulation, domainConfig, domainIds);
        RegisterTerrainRuntime(simulation, domainIds);
        RegisterPressures(simulation, domainConfig, domainIds);
        RegisterDegeneracyDetectors(simulation, domainConfig, domainIds);
        RegisterProcesses(simulation, domainConfig, domainIds);
        RegisterGenerators(simulation, definitionGraph, domainIds);
        CopyMapResources(simulation, testWorld.Grid, domainIds);
        var entityIds = CopyEntities(simulation, testWorld.Graph);
        CopyCoverageOwners(simulation, testWorld.CoverageMap, entityIds);
        CopyRelationships(simulation, testWorld.Graph, domainIds, entityIds);
        return simulation;
    }

    private static void RegisterDomainRuntime(
        Simulation simulation,
        DomainConfig domainConfig,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        foreach (var (kind, kindConfig) in domainConfig.Kinds)
        {
            var kindId = kind.Id;
            foreach (var attribute in kindConfig.Attributes)
            {
                var locationResourceId = attribute.LocationResourceDriftResource is { } resourceType
                    ? domainIds.ResourceIdOf(resourceType.ToDomainName()).Value
                    : 0;
                var criticalTargetAttributeId = attribute.CriticalEffect is { } criticalEffect
                    ? domainIds.AttributeIdOf(criticalEffect.TargetAttribute).Value
                    : 0;
                var criticalDelta = attribute.CriticalEffect?.DeltaPerTick ?? 0f;

                simulation.RegisterAttribute(
                    kindId,
                    domainIds.AttributeIdOf(attribute.Name).Value,
                    attribute.Min,
                    attribute.Max,
                    attribute.Default,
                    attribute.DefaultVariance,
                    attribute.DriftRate,
                    locationResourceId,
                    attribute.LocationResourceDriftPerUnit,
                    attribute.LocationResourceDriftMinOffset,
                    attribute.LocationResourceDriftMaxOffset,
                    criticalTargetAttributeId,
                    StandardNeedScale.Critical,
                    criticalDelta);
                simulation.Processes.RegisterAttributeDemand(
                    kindId,
                    domainIds.AttributeIdOf(attribute.Name).Value,
                    DemandCode(attribute),
                    attribute.Min,
                    attribute.Max,
                    attribute.Default,
                    attribute.Baseline);
            }

            foreach (var standingDrive in kindConfig.StandingDrives)
            {
                if (standingDrive.BaseUrgency < 0.01f)
                {
                    continue;
                }

                simulation.Processes.RegisterStandingDemand(
                    kindId,
                    EffectScopeCode(standingDrive.DesiredEffect),
                    EffectValueId(standingDrive.DesiredEffect, domainIds),
                    standingDrive.DesiredEffect.Kind?.Id ?? 0,
                    standingDrive.BaseUrgency);
            }
        }

        foreach (var (kind, lifecycle) in domainConfig.Lifecycles)
        {
            simulation.Attributes.RegisterLifecycle(
                kind.Id,
                domainIds.AttributeIdOf(lifecycle.HealthAttribute).Value,
                lifecycle.BaseRate,
                lifecycle.AgeCoefficient);
        }

        foreach (var relationship in domainConfig.Relationships)
        {
            simulation.Relationships.RegisterRelationshipKind(
                domainIds.RelationshipKindIdOf(relationship.Name).Value,
                relationship.CullThreshold);
        }
    }

    private static void RegisterProcesses(
        Simulation simulation,
        DomainConfig domainConfig,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        foreach (var process in domainConfig.Processes)
        {
            if (process.Role != DefinitionRole.Process)
            {
                continue;
            }

            if (IsPureMobilityProcess(process))
            {
                continue;
            }

            process.BindRuntimeIds(domainIds);
            var targetKindId = ResolveTargetKindId(process);
            var slot = simulation.Processes.RegisterProcess(
                process.TargetKind.Id,
                process.RuntimeId.Value,
                process.Cooldown,
                process.EntityCooldown,
                ResolveTargetMode(process, targetKindId),
                targetKindId,
                ResolveFallbackMovementOperation(process));
            RegisterPreconditions(simulation, slot, process.Precondition, domainIds);
            RegisterExpression(simulation, slot, process.ScoreExpression, domainIds);
            RegisterRewardExpression(simulation, slot, process.RewardExpression, domainIds);
            RegisterGoalEffect(simulation, slot, process, domainIds);
            RegisterProviderSelectors(simulation, slot, process, domainIds);
            RegisterSupportRules(simulation, slot, process, domainIds);
            foreach (var mutation in process.Mutations)
            {
                RegisterMutation(simulation, slot, mutation, domainIds);
            }
        }
    }

    private static void RegisterTerrainRuntime(
        Simulation simulation,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        foreach (var terrain in Enum.GetValues<TerrainType>())
        {
            simulation.World.RegisterTerrain(
                domainIds.TerrainIdOf(TerrainStableName(terrain)).Value,
                TerrainIsPassable(terrain),
                TerrainBoundaryGroupId(terrain));
        }
    }

    private static void RegisterPressures(
        Simulation simulation,
        DomainConfig domainConfig,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        foreach (var pressure in domainConfig.Pressures)
        {
            simulation.Pressures.RegisterPressure(
                domainIds.PressureIdOf(pressure.Name).Value,
                PressureScopeCode(pressure.Scope),
                pressure.Baseline,
                pressure.HomeostasisRate,
                pressure.SmoothingFactor);
        }
    }

    private static void RegisterGenerators(
        Simulation simulation,
        DefinitionGraph definitionGraph,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        foreach (var generator in definitionGraph.Generators.Values)
        {
            var refCodes = BuildGeneratorRefCodes(generator);
            var slot = simulation.Generators.RegisterGenerator(
                generator.RuntimeId,
                cadence: 25,
                generator.Target.Kind.Id,
                Math.Max(generator.PulseWeight, 0),
                generator.MaxRunsPerSim ?? 0,
                generator.Target.AllowWilderness);

            if (generator.Target.Subtypes is { Count: > 0 })
            {
                foreach (var subtype in generator.Target.Subtypes)
                {
                    simulation.Generators.AddTargetSubtype(slot, domainIds.SubtypeIdOf(subtype).Value);
                }
            }

            if (generator.TargetSaturation is { } saturation)
            {
                simulation.Generators.SetTargetSaturation(
                    slot,
                    saturation.RelationshipRuntimeId,
                    saturation.RequiredFactRuntimeId,
                    SaturationDirectionCode(saturation.Direction),
                    saturation.MaxExisting);
            }

            foreach (var trigger in generator.Triggers)
            {
                var directionCode = TriggerDirectionCode(trigger.Direction);
                simulation.Generators.AddTrigger(
                    slot,
                    trigger.OnKind.Id,
                    trigger.AttributeId is { } attributeId ? domainIds.AttributeIdOf(attributeId.Value).Value : 0,
                    directionCode,
                    TriggerThreshold(directionCode));
            }

            foreach (var binding in generator.SecondaryTargets)
            {
                var bindingSlot = simulation.Generators.AddSecondaryBinding(
                    slot,
                    refCodes[binding.Ref],
                    binding.Kind.Id,
                    binding.Query?.AdjacentToTarget == true ? 1 : 0,
                    CultureMatchCode(binding.Query?.TargetCultureMatch ?? QueryTargetCultureMatch.Any));
                RegisterEntityQuery(
                    simulation,
                    bindingSlot,
                    binding.Query,
                    domainIds,
                    isProducedQuery: false);
            }

            foreach (var produced in generator.Produces)
            {
                var querySubtypeSource = ProducedQuerySubtypes(produced);
                var producedSlot = simulation.Generators.AddProduced(
                    slot,
                    refCodes[produced.Ref],
                    produced.Kind.Id,
                    !string.IsNullOrWhiteSpace(produced.Subtype)
                        ? domainIds.SubtypeIdOf(produced.Subtype).Value
                        : 0,
                    produced.CreateIfMissing ? 1 : 0,
                    produced.Query is null ? 0 : 1,
                    produced.Query?.AdjacentToTarget == true ? 1 : 0,
                    CultureMatchCode(produced.Query?.TargetCultureMatch ?? QueryTargetCultureMatch.Any),
                    PlacementCode(produced.PhysicalPlacement),
                    PlacementCode(produced.SemanticPlacement),
                    refCodes[produced.PlacementRef],
                    refCodes[produced.InheritCultureFrom],
                    produced.CreateChance,
                    CoverageModeCode(produced.Coverage?.Mode),
                    produced.Coverage?.SourceRef is { Length: > 0 } sourceRef ? refCodes[sourceRef] : 0,
                    produced.Coverage?.DesiredCellCount ?? 0,
                    produced.Coverage?.MinimumCellCount ?? 0,
                    produced.Coverage?.MinimumSourceRemainingCellCount ?? 0,
                    produced.Coverage?.RespectTerrainBoundaries == false ? 0 : 1,
                    produced.Coverage?.TransformTerrain is { } transformedTerrain
                        ? domainIds.TerrainIdOf(TerrainStableName(transformedTerrain)).Value
                        : 0,
                    produced.Coverage?.TransformTerrain is { } transformedPassableTerrain &&
                    TerrainIsPassable(transformedPassableTerrain)
                        ? 1
                        : 0);

                if (produced.Subtypes is { Count: > 0 } subtypeChoices)
                {
                    foreach (var subtype in subtypeChoices)
                    {
                        simulation.Generators.AddProducedSubtypeChoice(
                            producedSlot,
                            domainIds.SubtypeIdOf(subtype).Value);
                    }
                }

                if (querySubtypeSource is not null)
                {
                    foreach (var subtype in querySubtypeSource)
                    {
                        simulation.Generators.AddProducedQuerySubtype(
                            producedSlot,
                            domainIds.SubtypeIdOf(subtype).Value);
                    }
                }

                RegisterProducedQueryRemainder(simulation, producedSlot, produced.Query, domainIds);
                RegisterCoverageTerrainFilters(simulation, producedSlot, produced.Coverage, domainIds);
            }

            foreach (var relationship in generator.Relationships)
            {
                var (factId0, factId1, factId2, factId3, factCount) = RelationshipFacts(relationship);
                simulation.Generators.AddRelationship(
                    slot,
                    refCodes[relationship.SourceRef],
                    refCodes[relationship.DestinationRef],
                    relationship.RelationshipRuntimeId,
                    relationship.Strength,
                    relationship.ReplaceExistingOutgoingOfSameKind ||
                    definitionGraph.Relationships[relationship.RelationshipId].SourceConfig.IsExclusive
                        ? 1
                        : 0,
                    factId0,
                    factId1,
                    factId2,
                    factId3,
                    factCount);
            }
        }
    }

    private static void RegisterDegeneracyDetectors(
        Simulation simulation,
        DomainConfig domainConfig,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        if (!domainConfig.Kinds.ContainsKey(IceRemembersKinds.Settlement) ||
            !domainConfig.Kinds.ContainsKey(IceRemembersKinds.Faction) ||
            !domainConfig.Kinds.ContainsKey(IceRemembersKinds.Penguin))
        {
            return;
        }

        simulation.Degeneracy.RegisterDetector(
            domainIds.DetectorIdOf(DomainRuntimeIdLoader.SettlementDepopulationDetector).Value,
            DegeneracyOperationCodes.DecliningAttributeRatio,
            domainIds.ReasonCodeIdOf(DomainRuntimeIdLoader.SettlementDepopulationReason).Value,
            IceRemembersKinds.Settlement.Id,
            secondaryKindId: 0,
            domainIds.AttributeIdOf(IceRemembersAttributeId.Residents.ToDomainName()).Value,
            relationshipKindId: 0,
            warningThreshold: 0.5f,
            criticalThreshold: 0.8f);

        simulation.Degeneracy.RegisterDetector(
            domainIds.DetectorIdOf(DomainRuntimeIdLoader.PoliticalMonocultureDetector).Value,
            DegeneracyOperationCodes.RelationshipDominanceRatio,
            domainIds.ReasonCodeIdOf(DomainRuntimeIdLoader.PoliticalMonocultureReason).Value,
            IceRemembersKinds.Settlement.Id,
            IceRemembersKinds.Faction.Id,
            attributeId: 0,
            domainIds.RelationshipKindIdOf(IceRemembersRelationship.ControlledBy.ToDomainName()).Value,
            warningThreshold: 0.6f,
            criticalThreshold: 0.8f);

        simulation.Degeneracy.RegisterDetector(
            domainIds.DetectorIdOf(DomainRuntimeIdLoader.NpcExtinctionDetector).Value,
            DegeneracyOperationCodes.ActiveKindCountBelow,
            domainIds.ReasonCodeIdOf(DomainRuntimeIdLoader.NpcExtinctionReason).Value,
            IceRemembersKinds.Penguin.Id,
            secondaryKindId: 0,
            attributeId: 0,
            relationshipKindId: 0,
            warningThreshold: 5f,
            criticalThreshold: 2f);
    }

    private static Dictionary<string, int> BuildGeneratorRefCodes(DefinitionGeneratorNode generator)
    {
        var refCodes = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["$target"] = GeneratorRuntime.TargetReferenceCode,
        };
        var next = GeneratorRuntime.TargetReferenceCode + 1;
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

    private static void RegisterEntityQuery(
        Simulation simulation,
        int bindingSlot,
        DefinitionEntityQuery? query,
        global::Engine.Domain.DomainIdRegistry domainIds,
        bool isProducedQuery)
    {
        if (query is null)
        {
            return;
        }

        if (query.Subtypes is { Count: > 0 })
        {
            foreach (var subtype in query.Subtypes)
            {
                if (isProducedQuery)
                {
                    simulation.Generators.AddProducedQuerySubtype(bindingSlot, domainIds.SubtypeIdOf(subtype).Value);
                }
                else
                {
                    simulation.Generators.AddBindingSubtype(bindingSlot, domainIds.SubtypeIdOf(subtype).Value);
                }
            }
        }

        if (query.WithoutOutgoingRelationships is { Count: > 0 })
        {
            foreach (var relationshipKind in query.WithoutOutgoingRelationships)
            {
                if (isProducedQuery)
                {
                    simulation.Generators.AddProducedQueryWithoutOutgoingRelationship(
                        bindingSlot,
                        domainIds.RelationshipKindIdOf(relationshipKind).Value);
                }
                else
                {
                    simulation.Generators.AddBindingWithoutOutgoingRelationship(
                        bindingSlot,
                        domainIds.RelationshipKindIdOf(relationshipKind).Value);
                }
            }
        }

        if (query.WithoutIncomingRelationships is { Count: > 0 })
        {
            foreach (var relationshipKind in query.WithoutIncomingRelationships)
            {
                if (isProducedQuery)
                {
                    simulation.Generators.AddProducedQueryWithoutIncomingRelationship(
                        bindingSlot,
                        domainIds.RelationshipKindIdOf(relationshipKind).Value);
                }
                else
                {
                    simulation.Generators.AddBindingWithoutIncomingRelationship(
                        bindingSlot,
                        domainIds.RelationshipKindIdOf(relationshipKind).Value);
                }
            }
        }

        foreach (var condition in query.AttributeConditions)
        {
            var directionCode = TriggerDirectionCode(condition.Direction);
            if (isProducedQuery)
            {
                simulation.Generators.AddProducedQueryAttributeCondition(
                    bindingSlot,
                    domainIds.AttributeIdOf(condition.AttributeId.Value).Value,
                    directionCode,
                    TriggerThreshold(directionCode));
            }
            else
            {
                simulation.Generators.AddBindingAttributeCondition(
                    bindingSlot,
                    domainIds.AttributeIdOf(condition.AttributeId.Value).Value,
                    directionCode,
                    TriggerThreshold(directionCode));
            }
        }
    }

    private static void RegisterProducedQueryRemainder(
        Simulation simulation,
        int producedSlot,
        DefinitionEntityQuery? query,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        if (query is null)
        {
            return;
        }

        if (query.WithoutOutgoingRelationships is { Count: > 0 })
        {
            foreach (var relationshipKind in query.WithoutOutgoingRelationships)
            {
                simulation.Generators.AddProducedQueryWithoutOutgoingRelationship(
                    producedSlot,
                    domainIds.RelationshipKindIdOf(relationshipKind).Value);
            }
        }

        if (query.WithoutIncomingRelationships is { Count: > 0 })
        {
            foreach (var relationshipKind in query.WithoutIncomingRelationships)
            {
                simulation.Generators.AddProducedQueryWithoutIncomingRelationship(
                    producedSlot,
                    domainIds.RelationshipKindIdOf(relationshipKind).Value);
            }
        }

        foreach (var condition in query.AttributeConditions)
        {
            var directionCode = TriggerDirectionCode(condition.Direction);
            simulation.Generators.AddProducedQueryAttributeCondition(
                producedSlot,
                domainIds.AttributeIdOf(condition.AttributeId.Value).Value,
                directionCode,
                TriggerThreshold(directionCode));
        }
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

    private static void RegisterCoverageTerrainFilters(
        Simulation simulation,
        int producedSlot,
        DefinitionRegionCoverage? coverage,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        if (coverage is null)
        {
            return;
        }

        if (coverage.PreferredTerrains is { Count: > 0 })
        {
            foreach (var terrain in coverage.PreferredTerrains)
            {
                simulation.Generators.AddProducedCoveragePreferredTerrain(
                    producedSlot,
                    domainIds.TerrainIdOf(TerrainStableName(terrain)).Value);
            }
        }

        if (coverage.AllowedTerrains is { Count: > 0 })
        {
            foreach (var terrain in coverage.AllowedTerrains)
            {
                simulation.Generators.AddProducedCoverageAllowedTerrain(
                    producedSlot,
                    domainIds.TerrainIdOf(TerrainStableName(terrain)).Value);
            }
        }
    }

    private static (
        int FactId0,
        int FactId1,
        int FactId2,
        int FactId3,
        int FactCount) RelationshipFacts(DefinitionGeneratorRelationship relationship)
    {
        var factId0 = 0;
        var factId1 = 0;
        var factId2 = 0;
        var factId3 = 0;
        var factCount = 0;
        foreach (var factRuntimeId in relationship.FactRuntimeIds)
        {
            switch (factCount)
            {
                case 0:
                    factId0 = factRuntimeId;
                    break;
                case 1:
                    factId1 = factRuntimeId;
                    break;
                case 2:
                    factId2 = factRuntimeId;
                    break;
                case 3:
                    factId3 = factRuntimeId;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Generator relationship '{relationship.RelationshipId.Value}' has more than four primitive facts.");
            }

            factCount++;
        }

        return (factId0, factId1, factId2, factId3, factCount);
    }

    private static int SaturationDirectionCode(TargetSaturationDirection direction) => direction switch
    {
        TargetSaturationDirection.OutgoingFromTarget => GeneratorSaturationDirectionCodes.OutgoingFromTarget,
        TargetSaturationDirection.IncomingToTarget => GeneratorSaturationDirectionCodes.IncomingToTarget,
        TargetSaturationDirection.Symmetric => GeneratorSaturationDirectionCodes.Symmetric,
        _ => throw new InvalidOperationException($"Unsupported target saturation direction '{direction}'."),
    };

    private static int PlacementCode(SemanticPlacement placement) => placement switch
    {
        SemanticPlacement.On => GeneratorPlacementCodes.On,
        SemanticPlacement.Near => GeneratorPlacementCodes.Near,
        SemanticPlacement.Across => GeneratorPlacementCodes.Across,
        SemanticPlacement.InRegionLocality => GeneratorPlacementCodes.InRegionLocality,
        _ => throw new InvalidOperationException($"Unsupported generator placement '{placement}'."),
    };

    private static int CultureMatchCode(QueryTargetCultureMatch cultureMatch) => cultureMatch switch
    {
        QueryTargetCultureMatch.Any => GeneratorCultureMatchCodes.Any,
        QueryTargetCultureMatch.SameAsTarget => GeneratorCultureMatchCodes.SameAsTarget,
        QueryTargetCultureMatch.DifferentFromTarget => GeneratorCultureMatchCodes.DifferentFromTarget,
        _ => throw new InvalidOperationException($"Unsupported generator culture match '{cultureMatch}'."),
    };

    private static int PressureScopeCode(PressureScope scope) => scope switch
    {
        PressureScope.Regional => PressureScopeCodes.Regional,
        PressureScope.Entity => PressureScopeCodes.Entity,
        _ => throw new InvalidOperationException($"Unsupported pressure scope '{scope}'."),
    };

    private static int TriggerDirectionCode(TriggerDirection direction) => direction switch
    {
        TriggerDirection.High => GeneratorTriggerDirectionCodes.High,
        TriggerDirection.Low => GeneratorTriggerDirectionCodes.Low,
        TriggerDirection.Critical => GeneratorTriggerDirectionCodes.Critical,
        TriggerDirection.Any => GeneratorTriggerDirectionCodes.Any,
        _ => throw new InvalidOperationException($"Unsupported generator trigger direction '{direction}'."),
    };

    private static float TriggerThreshold(int directionCode) => directionCode switch
    {
        GeneratorTriggerDirectionCodes.High => StandardNeedScale.Comfortable,
        GeneratorTriggerDirectionCodes.Low => StandardNeedScale.Concern,
        GeneratorTriggerDirectionCodes.Critical => StandardNeedScale.Critical,
        _ => 0f,
    };

    private static int CoverageModeCode(CoveragePlacementMode? mode) => mode switch
    {
        null => GeneratorCoverageModeCodes.None,
        CoveragePlacementMode.CreateNew => GeneratorCoverageModeCodes.CreateNew,
        CoveragePlacementMode.SplitFromExisting => GeneratorCoverageModeCodes.SplitFromExisting,
        _ => throw new InvalidOperationException($"Unsupported coverage placement mode '{mode}'."),
    };

    private static int DemandCode(AttributeConfig attribute)
    {
        return StandardNeedScale.AppliesTo(attribute)
            ? PlannerDemandCodes.StandardNeed
            : attribute.Baseline > 0f
                ? PlannerDemandCodes.Baseline
                : PlannerDemandCodes.RangeDefault;
    }

    private static bool TerrainIsPassable(TerrainType terrain) =>
        terrain != TerrainType.Ocean;

    private static int TerrainBoundaryGroupId(TerrainType terrain) => terrain switch
    {
        TerrainType.Ocean or TerrainType.Reef or TerrainType.Shelf => 1,
        TerrainType.Ice or TerrainType.Tundra => 2,
        TerrainType.Wastes or TerrainType.Crags => 3,
        TerrainType.HotSprings => 4,
        _ => 5,
    };

    private static string TerrainStableName(TerrainType terrain) => terrain.ToString();

    private static int ResolveTargetKindId(ProcessDefinition process)
    {
        foreach (var selector in process.ProviderSelectors)
        {
            if (selector.Kind is { } kind)
            {
                return kind.Id;
            }
        }

        foreach (var effect in process.GoalEffects)
        {
            if (effect.Scope == RefScope.Target && effect.Kind is { } kind)
            {
                return kind.Id;
            }
        }

        return 0;
    }

    private static bool IsPureMobilityProcess(ProcessDefinition process)
    {
        return process.Mutations.Count == 1 &&
               (process.Mutations[0] is MoveTowardTargetMutation ||
                process.Mutations[0] is MoveTowardTargetOnSemanticPlaneMutation);
    }

    private static int ResolveFallbackMovementOperation(ProcessDefinition process)
    {
        if (IsPureMobilityProcess(process))
        {
            return 0;
        }

        return FindPlaneDistancePrecondition(process.Precondition) switch
        {
            PrimaryPlane.Physical => ProcessOperationCodes.MoveTowardTargetPhysical,
            PrimaryPlane.Semantic => ProcessOperationCodes.MoveTowardTargetSemantic,
            _ => 0,
        };
    }

    private static PrimaryPlane? FindPlaneDistancePrecondition(IPrecondition precondition)
    {
        return precondition switch
        {
            WithinPlaneDistancePrecondition distance => distance.Plane,
            AndPrecondition and => FindPlaneDistancePrecondition(and),
            _ => null,
        };
    }

    private static PrimaryPlane? FindPlaneDistancePrecondition(AndPrecondition precondition)
    {
        foreach (var child in precondition.Children)
        {
            var plane = FindPlaneDistancePrecondition(child);
            if (plane is not null)
            {
                return plane;
            }
        }

        return null;
    }

    private static int ResolveTargetMode(ProcessDefinition process, int targetKindId)
    {
        return ContainsPrecondition<TargetIsSelf>(process.Precondition)
            ? ProcessTargetModeCodes.Self
            : ContainsPrecondition<TargetIsNotSelf>(process.Precondition)
                ? targetKindId > 0
                    ? ProcessTargetModeCodes.KindOther
                    : ProcessTargetModeCodes.AnyOther
                : ProcessTargetModeCodes.Self;
    }

    private static void RegisterGoalEffect(
        Simulation simulation,
        int processSlot,
        ProcessDefinition process,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        foreach (var effect in process.GoalEffects)
        {
            simulation.Processes.SetGoalEffect(
                processSlot,
                EffectScopeCode(effect),
                EffectValueId(effect, domainIds),
                effect.Kind?.Id ?? 0);
            return;
        }
    }

    private static void RegisterProviderSelectors(
        Simulation simulation,
        int processSlot,
        ProcessDefinition process,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        foreach (var selector in process.ProviderSelectors)
        {
            simulation.Processes.AddProviderSelector(
                processSlot,
                ProviderSelectorCode(selector),
                selector.Kind?.Id ?? 0,
                ProviderSelectorValueId(selector, domainIds),
                ProviderSelectorRelationshipKindId(selector, domainIds),
                ProviderSelectorFactId(selector, domainIds));
        }
    }

    private static void RegisterSupportRules(
        Simulation simulation,
        int processSlot,
        ProcessDefinition process,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        foreach (var rule in process.SupportRules)
        {
            var supportSlot = simulation.Processes.AddSupportRule(
                processSlot,
                EffectScopeCode(rule.DesiredEffect),
                EffectValueId(rule.DesiredEffect, domainIds),
                rule.DesiredEffect.Kind?.Id ?? 0,
                rule.DesiredMinimum,
                rule.UrgencyScale);
            RegisterSupportExpression(simulation, supportSlot, rule.CurrentValueExpression, domainIds);
        }
    }

    private static int ProviderSelectorCode(AttributeRef selector) => selector.Scope switch
    {
        RefScope.Target => PlannerProviderSelectorCodes.TargetAttribute,
        RefScope.Structural => PlannerProviderSelectorCodes.Structural,
        RefScope.Resource => PlannerProviderSelectorCodes.Resource,
        RefScope.Location => PlannerProviderSelectorCodes.LocationAttribute,
        RefScope.Related or RefScope.Relational => PlannerProviderSelectorCodes.OutgoingRelationship,
        _ => throw new InvalidOperationException($"Unsupported kernel provider selector scope '{selector.Scope}'."),
    };

    private static int ProviderSelectorValueId(
        AttributeRef selector,
        global::Engine.Domain.DomainIdRegistry domainIds) => selector.Scope switch
    {
        RefScope.Target or RefScope.Location or RefScope.Related => domainIds.AttributeIdOf(selector.Name).Value,
        RefScope.Structural => StructuralFactRuntimeCode(selector.Name),
        RefScope.Resource => domainIds.ResourceIdOf(selector.Name).Value,
        _ => 0,
    };

    private static int ProviderSelectorRelationshipKindId(
        AttributeRef selector,
        global::Engine.Domain.DomainIdRegistry domainIds) =>
        selector.Scope is RefScope.Related or RefScope.Relational
            ? domainIds.RelationshipKindIdOf(selector.RelationshipKind ?? selector.Name).Value
            : 0;

    private static int ProviderSelectorFactId(
        AttributeRef selector,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        if (selector.Scope != RefScope.Relational ||
            string.IsNullOrWhiteSpace(selector.Name) ||
            string.Equals(selector.Name, "matched", StringComparison.Ordinal))
        {
            return 0;
        }

        return domainIds.RelationshipFactIdOf(selector.Name).Value;
    }

    private static int EffectScopeCode(AttributeRef reference) => reference.Scope switch
    {
        RefScope.Self => PlannerEffectScopeCodes.SelfAttribute,
        RefScope.Target => PlannerEffectScopeCodes.TargetAttribute,
        RefScope.Resource => PlannerEffectScopeCodes.Resource,
        RefScope.Structural => PlannerEffectScopeCodes.Structural,
        RefScope.Relational => PlannerEffectScopeCodes.Relational,
        _ => throw new InvalidOperationException($"Unsupported kernel planner effect scope '{reference.Scope}'."),
    };

    private static int EffectValueId(
        AttributeRef reference,
        global::Engine.Domain.DomainIdRegistry domainIds) => reference.Scope switch
    {
        RefScope.Self or RefScope.Target => domainIds.AttributeIdOf(reference.Name).Value,
        RefScope.Resource => domainIds.ResourceIdOf(reference.Name).Value,
        RefScope.Structural => StructuralFactRuntimeCode(reference.Name),
        RefScope.Relational => domainIds.RelationshipKindIdOf(reference.RelationshipKind ?? reference.Name).Value,
        _ => throw new InvalidOperationException($"Unsupported kernel planner effect scope '{reference.Scope}'."),
    };

    private static int StructuralFactRuntimeCode(string stableName)
    {
        return StableRuntimeCode(stableName);
    }

    private static int StableRuntimeCode(string stableName)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var character in stableName)
            {
                hash ^= character;
                hash *= 16777619u;
            }

            var value = (int)(hash & 2147483647u);
            return value == 0 ? 1 : value;
        }
    }

    private static bool ContainsPrecondition<T>(IPrecondition precondition)
        where T : IPrecondition
    {
        if (precondition is T)
        {
            return true;
        }

        return precondition switch
        {
            AndPrecondition and => and.Children.Any(ContainsPrecondition<T>),
            OrPrecondition or => or.Children.Any(ContainsPrecondition<T>),
            NotPrecondition not => ContainsPrecondition<T>(not.Inner),
            _ => false,
        };
    }

    private static void RegisterPreconditions(
        Simulation simulation,
        int processSlot,
        IPrecondition precondition,
        global::Engine.Domain.DomainIdRegistry domainIds) =>
        RegisterPreconditionExpression(simulation, processSlot, precondition, domainIds);

    private static void RegisterPreconditionExpression(
        Simulation simulation,
        int processSlot,
        IPrecondition precondition,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        switch (precondition)
        {
            case AndPrecondition and:
                RegisterPreconditionChildren(
                    simulation,
                    processSlot,
                    and.Children,
                    domainIds,
                    ProcessPreconditionCodes.And);
                break;
            case OrPrecondition or:
                RegisterPreconditionChildren(
                    simulation,
                    processSlot,
                    or.Children,
                    domainIds,
                    ProcessPreconditionCodes.Or);
                break;
            case NotPrecondition not:
                RegisterPreconditionExpression(simulation, processSlot, not.Inner, domainIds);
                simulation.Processes.AddPrecondition(processSlot, ProcessPreconditionCodes.Not);
                break;
            case AlwaysTrue:
                simulation.Processes.AddPrecondition(processSlot, ProcessPreconditionCodes.Always);
                break;
            case TargetIsSelf:
                simulation.Processes.AddPrecondition(processSlot, ProcessPreconditionCodes.TargetIsSelf);
                break;
            case TargetIsNotSelf:
                simulation.Processes.AddPrecondition(processSlot, ProcessPreconditionCodes.TargetIsNotSelf);
                break;
            case SelfAttributeThreshold self:
                simulation.Processes.AddPrecondition(
                    processSlot,
                    ProcessPreconditionCodes.SelfAttributeRange,
                    domainIds.AttributeIdOf(self.AttributeName).Value,
                    self.Min.HasValue ? 1 : 0,
                    self.Max.HasValue ? 1 : 0,
                    x: self.Min ?? 0f,
                    y: self.Max ?? 0f);
                break;
            case TargetAttributeThreshold target:
                simulation.Processes.AddPrecondition(
                    processSlot,
                    ProcessPreconditionCodes.TargetAttributeRange,
                    domainIds.AttributeIdOf(target.AttributeName).Value,
                    target.Min.HasValue ? 1 : 0,
                    target.Max.HasValue ? 1 : 0,
                    x: target.Min ?? 0f,
                    y: target.Max ?? 0f);
                break;
            case WithinPlaneDistancePrecondition distance:
                simulation.Processes.AddPrecondition(
                    processSlot,
                    ProcessPreconditionCodes.PlaneDistance,
                    distance.MaxDistance,
                    distance.Plane == PrimaryPlane.Semantic ? PlaneCodes.Semantic : PlaneCodes.Physical);
                break;
            case LocationResourceThreshold resource:
                simulation.Processes.AddPrecondition(
                    processSlot,
                    ProcessPreconditionCodes.LocationResourceRange,
                    domainIds.ResourceIdOf(resource.ResourceType.ToDomainName()).Value,
                    resource.TargetLocation ? 1 : 0,
                    resource.Min.HasValue ? 1 : 0,
                    resource.Max.HasValue ? 1 : 0,
                    resource.Min ?? 0f,
                    resource.Max ?? 0f);
                break;
            case SelfHasRelationshipToTarget relationship:
                simulation.Processes.AddPrecondition(
                    processSlot,
                    ProcessPreconditionCodes.SelfHasRelationshipToTarget,
                    domainIds.RelationshipKindIdOf(relationship.RelationshipKind).Value,
                    relationship.MinStrength.HasValue ? 1 : 0,
                    x: relationship.MinStrength ?? 0f);
                break;
            case TargetHasRelationshipToSelf relationship:
                simulation.Processes.AddPrecondition(
                    processSlot,
                    ProcessPreconditionCodes.TargetHasRelationshipToSelf,
                    domainIds.RelationshipKindIdOf(relationship.RelationshipKind).Value,
                    relationship.MinStrength.HasValue ? 1 : 0,
                    x: relationship.MinStrength ?? 0f);
                break;
            case RelatedEntityMatchesTarget relationship:
                simulation.Processes.AddPrecondition(
                    processSlot,
                    ProcessPreconditionCodes.RelatedEntityMatchesTarget,
                    domainIds.RelationshipKindIdOf(relationship.RelationshipKind).Value);
                break;
            case RelationshipExists relationship:
                simulation.Processes.AddPrecondition(
                    processSlot,
                    ProcessPreconditionCodes.RelationshipExists,
                    domainIds.RelationshipKindIdOf(relationship.RelationshipKind).Value,
                    relationship.MinStrength.HasValue ? 1 : 0,
                    relationship.SourceId.Value,
                    relationship.TargetId.Value,
                    relationship.MinStrength ?? 0f);
                break;
            case PressureThreshold pressure:
                simulation.Processes.AddPrecondition(
                    processSlot,
                    ProcessPreconditionCodes.PressureRange,
                    domainIds.PressureIdOf(pressure.PressureName).Value,
                    pressure.ScopeId.Value,
                    pressure.Min.HasValue ? 1 : 0,
                    pressure.Max.HasValue ? 1 : 0,
                    pressure.Min ?? 0f,
                    pressure.Max ?? 0f);
                break;
            case CooldownCheck cooldown:
                simulation.Processes.AddPrecondition(
                    processSlot,
                    ProcessPreconditionCodes.CooldownElapsed,
                    cooldown.MinimumTicksSinceFire);
                break;
            case EntityCountCheck count:
                simulation.Processes.AddPrecondition(
                    processSlot,
                    ProcessPreconditionCodes.EntityCountAtLeast,
                    count.Kind.Id,
                    count.Min);
                break;
            case HasColocatedNamedEntityPrecondition:
                simulation.Processes.AddPrecondition(processSlot, ProcessPreconditionCodes.HasColocatedEntity);
                break;
            default:
                throw new InvalidOperationException($"Unsupported kernel process precondition '{precondition.GetType().Name}'.");
        }
    }

    private static void RegisterPreconditionChildren(
        Simulation simulation,
        int processSlot,
        IReadOnlyList<IPrecondition> children,
        global::Engine.Domain.DomainIdRegistry domainIds,
        int operatorCode)
    {
        if (children.Count == 0)
        {
            simulation.Processes.AddPrecondition(processSlot, ProcessPreconditionCodes.Always);
            return;
        }

        RegisterPreconditionExpression(simulation, processSlot, children[0], domainIds);
        for (var i = 1; i < children.Count; i++)
        {
            RegisterPreconditionExpression(simulation, processSlot, children[i], domainIds);
            simulation.Processes.AddPrecondition(processSlot, operatorCode);
        }
    }

    private static void RegisterExpression(
        Simulation simulation,
        int processSlot,
        IExpression expression,
        global::Engine.Domain.DomainIdRegistry domainIds) =>
        RegisterExpression(simulation, processSlot, expression, domainIds, reward: false);

    private static void RegisterRewardExpression(
        Simulation simulation,
        int processSlot,
        IExpression expression,
        global::Engine.Domain.DomainIdRegistry domainIds) =>
        RegisterExpression(simulation, processSlot, expression, domainIds, reward: true);

    private static void RegisterSupportExpression(
        Simulation simulation,
        int supportRuleSlot,
        IExpression expression,
        global::Engine.Domain.DomainIdRegistry domainIds) =>
        RegisterExpression(
            simulation,
            processSlot: 0,
            supportRuleSlot,
            expression,
            domainIds,
            KernelExpressionTarget.Support);

    private static void RegisterMutationExpression(
        Simulation simulation,
        int stepSlot,
        IExpression expression,
        global::Engine.Domain.DomainIdRegistry domainIds) =>
        RegisterExpression(
            simulation,
            processSlot: 0,
            supportRuleSlot: 0,
            stepSlot,
            expression,
            domainIds,
            KernelExpressionTarget.Mutation);

    private static void RegisterExpression(
        Simulation simulation,
        int processSlot,
        IExpression expression,
        global::Engine.Domain.DomainIdRegistry domainIds,
        bool reward) =>
        RegisterExpression(
            simulation,
            processSlot,
            supportRuleSlot: 0,
            expression,
            domainIds,
            reward ? KernelExpressionTarget.Reward : KernelExpressionTarget.Score);

    private static void RegisterExpression(
        Simulation simulation,
        int processSlot,
        int supportRuleSlot,
        IExpression expression,
        global::Engine.Domain.DomainIdRegistry domainIds,
        KernelExpressionTarget target) =>
        RegisterExpression(
            simulation,
            processSlot,
            supportRuleSlot,
            stepSlot: 0,
            expression,
            domainIds,
            target);

    private static void RegisterExpression(
        Simulation simulation,
        int processSlot,
        int supportRuleSlot,
        int stepSlot,
        IExpression expression,
        global::Engine.Domain.DomainIdRegistry domainIds,
        KernelExpressionTarget target)
    {
        switch (expression)
        {
            case Literal literal:
                AddKernelExpressionOp(simulation, processSlot, supportRuleSlot, stepSlot, target, ProcessExpressionCodes.Literal, x: literal.Value);
                break;
            case AttributePath attr:
                AddKernelExpressionOp(
                    simulation,
                    processSlot,
                    supportRuleSlot,
                    stepSlot,
                    target,
                    attr.EntityRef == "target" ? ProcessExpressionCodes.TargetAttribute : ProcessExpressionCodes.SelfAttribute,
                    domainIds.AttributeIdOf(attr.AttributeName).Value);
                break;
            case VariableRef variable:
                AddKernelExpressionOp(
                    simulation,
                    processSlot,
                    supportRuleSlot,
                    stepSlot,
                    target,
                    ProcessExpressionCodes.Variable,
                    StableRuntimeCode(variable.VariableName));
                break;
            case LocationResourcePath resource:
                AddKernelExpressionOp(
                    simulation,
                    processSlot,
                    supportRuleSlot,
                    stepSlot,
                    target,
                    resource.EntityRef == "target"
                        ? ProcessExpressionCodes.TargetLocationResource
                        : ProcessExpressionCodes.SelfLocationResource,
                    domainIds.ResourceIdOf(resource.ResourceType.ToDomainName()).Value);
                break;
            case Distance:
                AddKernelExpressionOp(
                    simulation,
                    processSlot,
                    supportRuleSlot,
                    stepSlot,
                    target,
                    ProcessExpressionCodes.Distance);
                break;
            case Add add:
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, add.Left, domainIds, target);
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, add.Right, domainIds, target);
                AddKernelExpressionOp(simulation, processSlot, supportRuleSlot, stepSlot, target, ProcessExpressionCodes.Add);
                break;
            case Subtract subtract:
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, subtract.Left, domainIds, target);
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, subtract.Right, domainIds, target);
                AddKernelExpressionOp(simulation, processSlot, supportRuleSlot, stepSlot, target, ProcessExpressionCodes.Subtract);
                break;
            case Multiply multiply:
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, multiply.Left, domainIds, target);
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, multiply.Right, domainIds, target);
                AddKernelExpressionOp(simulation, processSlot, supportRuleSlot, stepSlot, target, ProcessExpressionCodes.Multiply);
                break;
            case Divide divide:
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, divide.Left, domainIds, target);
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, divide.Right, domainIds, target);
                AddKernelExpressionOp(simulation, processSlot, supportRuleSlot, stepSlot, target, ProcessExpressionCodes.Divide);
                break;
            case Min min:
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, min.A, domainIds, target);
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, min.B, domainIds, target);
                AddKernelExpressionOp(simulation, processSlot, supportRuleSlot, stepSlot, target, ProcessExpressionCodes.Min);
                break;
            case Max max:
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, max.A, domainIds, target);
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, max.B, domainIds, target);
                AddKernelExpressionOp(simulation, processSlot, supportRuleSlot, stepSlot, target, ProcessExpressionCodes.Max);
                break;
            case Clamp clamp:
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, clamp.Value, domainIds, target);
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, clamp.Min, domainIds, target);
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, clamp.Max, domainIds, target);
                AddKernelExpressionOp(simulation, processSlot, supportRuleSlot, stepSlot, target, ProcessExpressionCodes.Clamp);
                break;
            case Abs abs:
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, abs.Value, domainIds, target);
                AddKernelExpressionOp(simulation, processSlot, supportRuleSlot, stepSlot, target, ProcessExpressionCodes.Abs);
                break;
            case Jitter jitter:
                RegisterExpression(simulation, processSlot, supportRuleSlot, stepSlot, jitter.Value, domainIds, target);
                break;
            default:
                throw new InvalidOperationException($"Unsupported kernel process expression '{expression.GetType().Name}'.");
        }
    }

    private static void AddKernelExpressionOp(
        Simulation simulation,
        int processSlot,
        int supportRuleSlot,
        int stepSlot,
        KernelExpressionTarget target,
        int expressionCode,
        int a = 0,
        float x = 0f)
    {
        if (target == KernelExpressionTarget.Mutation)
        {
            simulation.Processes.AddStepExpressionOp(stepSlot, expressionCode, a, x);
            return;
        }

        if (target == KernelExpressionTarget.Support)
        {
            simulation.Processes.AddSupportExpressionOp(supportRuleSlot, expressionCode, a, x);
            return;
        }

        if (target == KernelExpressionTarget.Reward)
        {
            simulation.Processes.AddRewardExpressionOp(processSlot, expressionCode, a, x);
            return;
        }

        simulation.Processes.AddExpressionOp(processSlot, expressionCode, a, x);
    }

    private enum KernelExpressionTarget
    {
        Score,
        Reward,
        Support,
        Mutation,
    }

    private static void RegisterMutation(
        Simulation simulation,
        int processSlot,
        IMutation mutation,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        switch (mutation)
        {
            case CreateEntityMutation createEntity:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.CreateEntity,
                    createEntity.Kind.Id,
                    domainIds.SubtypeIdOf(createEntity.Subtype).Value,
                    domainIds.CultureIdOf(createEntity.Culture).Value,
                    createEntity.Coordinates.HasValue ? 1 : 0,
                    createEntity.Coordinates?.Q ?? 0,
                    createEntity.Coordinates?.R ?? 0);
                break;
            case CreateRelationshipMutation createRelationship:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.CreateFixedRelationship,
                    domainIds.RelationshipKindIdOf(createRelationship.RelationshipKind).Value,
                    createRelationship.SourceId.Value,
                    createRelationship.TargetId.Value,
                    (int)createRelationship.Polarity,
                    createRelationship.Strength);
                break;
            case ModifySelfAttributeMutation modify:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.AddAttribute,
                    domainIds.AttributeIdOf(modify.AttributeName).Value,
                    x: modify.Delta);
                break;
            case ModifyTargetAttributeMutation modify:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.AddTargetAttribute,
                    domainIds.AttributeIdOf(modify.AttributeName).Value,
                    x: modify.Delta);
                break;
            case ModifyPressureMutation modify:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.AddPressure,
                    domainIds.PressureIdOf(modify.PressureName).Value,
                    modify.ScopeId.Value,
                    x: modify.Delta);
                break;
            case SetTagMutation tag:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.SetEntityTag,
                    tag.EntityId.Value,
                    StableRuntimeCode(tag.Key),
                    StableRuntimeCode(tag.Value));
                break;
            case SetHistoricalMutation historical:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.SetHistorical,
                    historical.EntityId.Value);
                break;
            case TransferAttributeMutation transfer:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.TransferAttribute,
                    EndpointCode(transfer.Source),
                    domainIds.AttributeIdOf(transfer.SourceAttribute).Value,
                    EndpointCode(transfer.Destination),
                    domainIds.AttributeIdOf(transfer.DestinationAttribute).Value,
                    transfer.Amount);
                break;
            case ModifyLocationResourceMutation resource:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.AddLocationResource,
                    domainIds.ResourceIdOf(resource.ResourceType.ToDomainName()).Value,
                    resource.TargetLocation ? 1 : 0,
                    x: resource.Delta);
                break;
            case HexToHealthMutation hex:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.HexToHealth,
                    domainIds.AttributeIdOf(hex.AttributeName).Value,
                    domainIds.ResourceIdOf(hex.Resource.ToDomainName()).Value,
                    hex.TargetLocation ? 1 : 0,
                    x: hex.RatePerHexValue);
                break;
            case ModifyRelatedEntitiesAttributeMutation related:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.AddRelatedEntitiesAttribute,
                    domainIds.RelationshipKindIdOf(related.RelationshipKind).Value,
                    domainIds.AttributeIdOf(related.AttributeName).Value,
                    related.Direction == RelationshipTraversalDirection.Incoming
                        ? RelationshipTraversalDirectionCodes.Incoming
                        : RelationshipTraversalDirectionCodes.Outgoing,
                    related.UseTargetEntity ? 1 : 0,
                    related.Delta);
                break;
            case CalculateMutation calculate:
            {
                var stepSlot = simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.CalculateVariable,
                    StableRuntimeCode(calculate.VariableName));
                RegisterMutationExpression(simulation, stepSlot, calculate.Expression, domainIds);
                break;
            }
            case ModifySelfByVariableMutation modify:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.AddAttributeFromVariable,
                    domainIds.AttributeIdOf(modify.AttributeName).Value,
                    StableRuntimeCode(modify.VariableName),
                    x: 1f);
                break;
            case ModifyTargetByVariableMutation modify:
                simulation.Processes.AddStep(
                    processSlot,
                    ProcessOperationCodes.AddTargetAttributeFromVariable,
                    domainIds.AttributeIdOf(modify.AttributeName).Value,
                    StableRuntimeCode(modify.VariableName),
                    x: 1f);
                break;
            case MoveTowardTargetMutation:
                simulation.Processes.AddStep(processSlot, ProcessOperationCodes.MoveTowardTargetPhysical);
                break;
            case WanderMutation:
                simulation.Processes.AddStep(processSlot, ProcessOperationCodes.WanderPhysical);
                break;
            case MoveTowardTargetOnSemanticPlaneMutation:
                simulation.Processes.AddStep(processSlot, ProcessOperationCodes.MoveTowardTargetSemantic);
                break;
            case WanderOnSemanticPlaneMutation:
                simulation.Processes.AddStep(processSlot, ProcessOperationCodes.WanderSemantic);
                break;
            default:
                throw new InvalidOperationException($"Unsupported kernel process mutation '{mutation.GetType().Name}'.");
        }
    }

    private static int EndpointCode(TransferEndpoint endpoint) => endpoint switch
    {
        TransferEndpoint.Target => ProcessEndpointCodes.Target,
        _ => ProcessEndpointCodes.Self,
    };

    private static void CopyMapResources(
        Simulation simulation,
        HexGrid grid,
        global::Engine.Domain.DomainIdRegistry domainIds)
    {
        var foodId = domainIds.ResourceIdOf(ResourceType.Food.ToDomainName()).Value;
        var wealthId = domainIds.ResourceIdOf(ResourceType.Wealth.ToDomainName()).Value;
        var populationId = domainIds.ResourceIdOf(ResourceType.Population.ToDomainName()).Value;

        simulation.World.SetMapRadius(grid.Radius);
        foreach (var cell in grid.AllCells)
        {
            simulation.World.SetCell(
                cell.Coord.Q,
                cell.Coord.R,
                domainIds.TerrainIdOf(TerrainStableName(cell.Terrain)).Value,
                !float.IsPositiveInfinity(cell.MovementCost));
            simulation.World.SetResource(
                foodId,
                cell.Coord.Q,
                cell.Coord.R,
                simulation.ProcessSourceCodeId,
                cell.AmbientFood,
                tick: 0);
            simulation.World.SetResource(
                wealthId,
                cell.Coord.Q,
                cell.Coord.R,
                simulation.ProcessSourceCodeId,
                cell.AmbientWealth,
                tick: 0);
            simulation.World.SetResource(
                populationId,
                cell.Coord.Q,
                cell.Coord.R,
                simulation.ProcessSourceCodeId,
                cell.AmbientPopulation,
                tick: 0);
        }
    }

    private static void CopyCoverageOwners(
        Simulation simulation,
        MapCoverageMap coverageMap,
        Dictionary<EntityId, int> entityIds)
    {
        foreach (var coverage in coverageMap.AllCoverages)
        {
            if (!entityIds.TryGetValue(coverage.OwnerEntityId, out var ownerKernelId))
            {
                continue;
            }

            foreach (var cell in coverage.Cells)
            {
                simulation.World.SetCellCoverageOwner(cell.Q, cell.R, ownerKernelId);
            }
        }
    }

    private static Dictionary<EntityId, int> CopyEntities(Simulation simulation, WorldGraph graph)
    {
        var entityIds = new Dictionary<EntityId, int>();
        foreach (var entity in graph.AllEntities.Values.OrderBy(entity => entity.Id.Value))
        {
            var kernelId = simulation.CreateEntity(
                entity.Kind.Id,
                entity.SubtypeId.Value,
                entity.CultureId.Value,
                entity.Coordinates.Q,
                entity.Coordinates.R,
                entity.SemanticPosition.Q,
                entity.SemanticPosition.R,
                entity.SemanticPosition.S,
                entity.CreatedAt);
            entityIds.Add(entity.Id, kernelId);
        }

        return entityIds;
    }

    private static void CopyRelationships(
        Simulation simulation,
        WorldGraph graph,
        global::Engine.Domain.DomainIdRegistry domainIds,
        Dictionary<EntityId, int> entityIds)
    {
        foreach (var entity in graph.AllEntities.Values.OrderBy(entity => entity.Id.Value))
        {
            foreach (var relationship in graph.OutgoingRelationships(entity.Id))
            {
                if (!relationship.IsActive)
                {
                    continue;
                }

                simulation.World.CreateRelationship(
                    relationship.KindId.Value > 0
                        ? relationship.KindId.Value
                        : domainIds.RelationshipKindIdOf(relationship.Kind).Value,
                    (int)relationship.Polarity,
                    entityIds[relationship.SourceId],
                    entityIds[relationship.TargetId],
                    relationship.Strength,
                    relationship.StartDate,
                    relationship.FactId0.Value,
                    relationship.FactId1.Value,
                    relationship.FactId2.Value,
                    relationship.FactId3.Value,
                    relationship.FactCount);
            }
        }
    }
}
