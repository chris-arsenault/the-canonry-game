namespace Engine.Generators;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Messages;

/// <summary>
/// Selects one applicable generator/target pair on each generator pulse and executes it.
/// </summary>
public sealed class GeneratorSystem
{
    private readonly WorldGraph _graph;
    private readonly GeneratorInstanceRegistry _instances;
    private readonly DefinitionGraph _definitionGraph;
    private readonly SpatialIndex _spatialIndex;
    private readonly SemanticPlaneIndex _semanticPlaneIndex;
    private readonly MapCoverageMap _coverageMap;
    private readonly MessageBus _messageBus;
    private readonly Random _random;

    public GeneratorSystem(
        WorldGraph graph,
        GeneratorInstanceRegistry instances,
        DefinitionGraph definitionGraph,
        SpatialIndex spatialIndex,
        SemanticPlaneIndex semanticPlaneIndex,
        MapCoverageMap coverageMap,
        MessageBus messageBus,
        Random random)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _instances = instances ?? throw new ArgumentNullException(nameof(instances));
        _definitionGraph = definitionGraph ?? throw new ArgumentNullException(nameof(definitionGraph));
        _spatialIndex = spatialIndex ?? throw new ArgumentNullException(nameof(spatialIndex));
        _semanticPlaneIndex = semanticPlaneIndex ?? throw new ArgumentNullException(nameof(semanticPlaneIndex));
        _coverageMap = coverageMap ?? throw new ArgumentNullException(nameof(coverageMap));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public int PulseInterval { get; init; } = 25;
    public int TotalFired { get; private set; }

    public void Tick(int currentTick, IReadOnlySet<string> activeGeneratorIds)
    {
        ArgumentNullException.ThrowIfNull(activeGeneratorIds);

        if (currentTick <= 0 || currentTick % PulseInterval != 0)
        {
            return;
        }

        var applicableByGenerator = new Dictionary<string, ApplicableGenerator>(StringComparer.Ordinal);

        foreach (var generatorId in activeGeneratorIds)
        {
            if (!_definitionGraph.Generators.TryGetValue(generatorId, out var definition))
            {
                continue;
            }

            var instance = _instances.GetOrCreate(definition);
            if (instance.Definition.MaxRunsPerSim is { } maxRuns && instance.TotalRuns >= maxRuns)
            {
                continue;
            }

            var weight = Math.Max(instance.Definition.PulseWeight, 0);
            if (weight == 0)
            {
                continue;
            }

            var targets = new List<ResolvedGeneratorTarget>();
            foreach (var candidate in ResolveTargets(instance.Definition))
            {
                if (!TriggersPass(instance.Definition, candidate.TargetEntity))
                {
                    continue;
                }

                targets.Add(candidate);
            }

            if (targets.Count == 0)
            {
                continue;
            }

            applicableByGenerator[generatorId] = new ApplicableGenerator(instance, targets, weight);
        }

        if (applicableByGenerator.Count == 0)
        {
            return;
        }

        while (applicableByGenerator.Count > 0)
        {
            var winner = ChooseWeightedGenerator(applicableByGenerator.Values);
            var target = winner.Targets[_random.Next(winner.Targets.Count)];
            if (Fire(winner.Instance, target, currentTick))
            {
                return;
            }

            winner.Targets.Remove(target);
            if (winner.Targets.Count == 0)
            {
                applicableByGenerator.Remove(winner.Instance.GeneratorId);
            }
        }
    }

    private bool Fire(GeneratorInstance instance, ResolvedGeneratorTarget target, int currentTick)
    {
        var generator = instance.Definition;
        var targetContext = new ExecutionTargetContext(
            target.TargetEntity,
            target.PhysicalOrigin,
            target.TargetEntity.SemanticPosition,
            target.BoundEntities);
        var symbols = new Dictionary<string, EntityId?>(StringComparer.Ordinal)
        {
            ["$target"] = targetContext.TargetEntity.Id,
        };
        foreach (var (reference, entity) in targetContext.BoundEntities)
        {
            symbols[reference] = entity.Id;
        }

        var createdEntityIds = new List<EntityId>();
        var createdEntities = new List<Entity>();
        var createdRelationshipCount = 0;

        try
        {
            foreach (var produced in generator.Produces)
            {
                if (produced.CreateChance < 1f && _random.NextSingle() > produced.CreateChance)
                {
                    symbols[produced.Ref] = null;
                    continue;
                }

                var boundEntity = produced.Query is null
                    ? null
                    : TryResolveBoundEntity(
                        produced.Kind,
                        MergeSubtypeFilter(produced.Query, produced.Subtype, produced.Subtypes),
                        targetContext,
                        allowUnconstrainedBind: false);
                if (boundEntity is not null)
                {
                    symbols[produced.Ref] = boundEntity.Id;
                    continue;
                }

                if (!produced.CreateIfMissing)
                {
                    symbols[produced.Ref] = null;
                    continue;
                }

                var chosenSubtype = ResolveProducedSubtype(produced);
                var culture = ResolveCulture(generator, produced, symbols, targetContext.TargetEntity);
                var name = NameSynthesizer.SynthesizeName(produced.Kind, chosenSubtype, culture, _random);
                var (physicalCoord, semanticCoord) = ResolvePlacement(produced, targetContext, symbols);
                var entity = _graph.CreateEntity(
                    produced.Kind,
                    chosenSubtype,
                    name,
                    culture,
                    currentTick,
                    (physicalCoord.Q, physicalCoord.R),
                    semanticCoord);
                if (produced.Coverage is not null)
                {
                    var coverageResult = _coverageMap.CreateCoverage(
                        _spatialIndex.Grid,
                        new MapCoveragePlacementRequest
                        {
                            Name = name,
                            OwnerEntityId = entity.Id,
                            Origin = physicalCoord,
                            Mode = produced.Coverage.Mode,
                            SourceCoverageEntityId = ResolveCoverageSourceRef(produced.Coverage.SourceRef, symbols),
                            DesiredCellCount = produced.Coverage.DesiredCellCount,
                            MinimumCellCount = produced.Coverage.MinimumCellCount,
                            MinimumSourceRemainingCellCount = produced.Coverage.MinimumSourceRemainingCellCount,
                            PreferredTerrains = produced.Coverage.PreferredTerrains,
                            AllowedTerrains = produced.Coverage.AllowedTerrains,
                            RespectTerrainBoundaries = produced.Coverage.RespectTerrainBoundaries,
                            TransformTerrain = produced.Coverage.TransformTerrain,
                        });

                    if (coverageResult.Anchor != physicalCoord)
                    {
                        _graph.SetCoordinates(entity.Id, coverageResult.Anchor.Q, coverageResult.Anchor.R);
                        physicalCoord = coverageResult.Anchor;
                    }
                }
                _spatialIndex.PlaceEntity(entity.Id, physicalCoord);

                symbols[produced.Ref] = entity.Id;
                createdEntityIds.Add(entity.Id);
                createdEntities.Add(entity);
            }
        }
        catch (InvalidOperationException)
        {
            foreach (var createdEntity in createdEntities)
            {
                _spatialIndex.RemoveEntity(createdEntity.Id);
                _graph.SetEntityHistorical(createdEntity.Id, currentTick);
            }

            instance.RecordFailure(currentTick, targetContext.TargetEntity.Id);
            return false;
        }

        foreach (var relationship in generator.Relationships)
        {
            if (!symbols.TryGetValue(relationship.SourceRef, out var sourceId) || sourceId is null ||
                !symbols.TryGetValue(relationship.DestinationRef, out var destinationId) || destinationId is null)
            {
                continue;
            }

            var shouldReplaceOutgoing = relationship.ReplaceExistingOutgoingOfSameKind ||
                                        _definitionGraph.Relationships[relationship.RelationshipId].SourceConfig.IsExclusive;
            if (shouldReplaceOutgoing)
            {
                var existing = _graph.OutgoingRelationships(sourceId.Value, relationship.RelationshipId.Value).ToList();
                foreach (var prior in existing)
                {
                    _graph.CullRelationship(prior, currentTick);
                }
            }

            _graph.CreateRelationship(
                relationship.RelationshipId.Value,
                RelationshipPolarity.Neutral,
                sourceId.Value,
                destinationId.Value,
                relationship.Strength,
                currentTick,
                relationship.FactIds.Select(factId => factId.Value));
            createdRelationshipCount++;
        }

        instance.RecordRun(currentTick, targetContext.TargetEntity.Id);

        TotalFired++;
        _messageBus.Publish(new GeneratorFiredMessage(
            currentTick,
            generator.Id,
            targetContext.TargetEntity.Id,
            createdEntityIds,
            createdRelationshipCount));
        return true;
    }

    private IEnumerable<ResolvedGeneratorTarget> ResolveTargets(DefinitionGeneratorNode generator)
    {
        foreach (var targetId in _graph.GetEntitiesByKind(generator.Target.Kind))
        {
            var entity = _graph.GetEntity(targetId);
            if (entity is null || entity.Status != EntityStatus.Active)
            {
                continue;
            }

            if (!MatchesSubtypeFilter(entity, generator.Target.Subtypes))
            {
                continue;
            }

            if (ExceedsTargetSaturation(generator.TargetSaturation, entity.Id))
            {
                continue;
            }

            var targetOrigin = new HexCoord(entity.Coordinates.Q, entity.Coordinates.R);
            var targetContext = new ExecutionTargetContext(
                entity,
                targetOrigin,
                entity.SemanticPosition,
                new Dictionary<string, Entity>(StringComparer.Ordinal));
            var secondaryBindings = ResolveSecondaryTargets(generator, targetContext);
            if (secondaryBindings is null)
            {
                continue;
            }

            if (generator.Target.AllowWilderness)
            {
                var wildernessOrigin = _spatialIndex.FindFreePhysicalHex(targetOrigin);
                if (wildernessOrigin is not HexCoord freeHex)
                {
                    continue;
                }

                yield return new ResolvedGeneratorTarget(entity, freeHex, secondaryBindings);
                continue;
            }

            yield return new ResolvedGeneratorTarget(entity, targetOrigin, secondaryBindings);
        }
    }

    private static bool MatchesSubtypeFilter(Entity entity, IReadOnlySet<string>? allowedSubtypes)
    {
        if (allowedSubtypes is null || allowedSubtypes.Count == 0)
        {
            return true;
        }

        return allowedSubtypes.Contains(entity.Subtype);
    }

    private bool ExceedsTargetSaturation(
        DefinitionTargetSaturation? targetSaturation,
        EntityId targetId)
    {
        if (targetSaturation is null)
        {
            return false;
        }

        var count = CountTargetSaturation(targetSaturation, targetId);
        return count > targetSaturation.MaxExisting;
    }

    private int CountTargetSaturation(
        DefinitionTargetSaturation targetSaturation,
        EntityId targetId)
    {
        return targetSaturation.Direction switch
        {
            TargetSaturationDirection.OutgoingFromTarget =>
                _graph.OutgoingRelationships(targetId, targetSaturation.RelationshipId.Value)
                    .Count(relationship => MatchesRequiredFact(relationship, targetSaturation.RequiredFactId)),
            TargetSaturationDirection.IncomingToTarget =>
                _graph.IncomingRelationships(targetId, targetSaturation.RelationshipId.Value)
                    .Count(relationship => MatchesRequiredFact(relationship, targetSaturation.RequiredFactId)),
            TargetSaturationDirection.Symmetric =>
                _graph.OutgoingRelationships(targetId, targetSaturation.RelationshipId.Value)
                    .Concat(_graph.IncomingRelationships(targetId, targetSaturation.RelationshipId.Value))
                    .Count(relationship => MatchesRequiredFact(relationship, targetSaturation.RequiredFactId)),
            _ => 0,
        };
    }

    private static bool MatchesRequiredFact(
        Relationship relationship,
        DefinitionRelationshipFactId? requiredFactId) =>
        requiredFactId is null || relationship.HasFact(requiredFactId.Value.Value);

    private static bool TriggersPass(DefinitionGeneratorNode generator, Entity target)
    {
        foreach (var trigger in generator.Triggers)
        {
            if (TriggerPasses(trigger, target))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TriggerPasses(DefinitionGeneratorTrigger trigger, Entity target)
    {
        if (trigger.Direction == TriggerDirection.Any)
        {
            return true;
        }

        if (trigger.AttributeId is not DefinitionAttributeId attributeId)
        {
            return false;
        }

        var value = target.Attributes.GetValue(attributeId.Value);
        return TriggerPasses(trigger.Direction, value);
    }

    private ApplicableGenerator ChooseWeightedGenerator(IEnumerable<ApplicableGenerator> applicable)
    {
        var list = applicable.ToList();
        var totalWeight = list.Sum(item => item.Weight);
        var roll = _random.Next(totalWeight);

        foreach (var item in list)
        {
            if (roll < item.Weight)
            {
                return item;
            }

            roll -= item.Weight;
        }

        return list[^1];
    }

    private Dictionary<string, Entity>? ResolveSecondaryTargets(
        DefinitionGeneratorNode generator,
        ExecutionTargetContext target)
    {
        if (generator.SecondaryTargets.Count == 0)
        {
            return new Dictionary<string, Entity>(StringComparer.Ordinal);
        }

        var resolved = new Dictionary<string, Entity>(StringComparer.Ordinal);
        foreach (var binding in generator.SecondaryTargets)
        {
            var entity = TryResolveBoundEntity(binding.Kind, binding.Query, target, allowUnconstrainedBind: true);
            if (entity is null)
            {
                return null;
            }

            resolved[binding.Ref] = entity;
        }

        return resolved;
    }

    private Entity? TryResolveBoundEntity(
        EntityKind kind,
        DefinitionEntityQuery? query,
        ExecutionTargetContext target,
        bool allowUnconstrainedBind)
    {
        if (query is null && !allowUnconstrainedBind)
        {
            return null;
        }

        var matches = _graph.GetEntitiesByKind(kind)
            .Select(entityId => _graph.GetEntity(entityId))
            .Where(entity =>
                entity is not null &&
                entity.Status == EntityStatus.Active &&
                entity.Id != target.TargetEntity.Id &&
                !target.BoundEntities.Values.Any(bound => bound.Id == entity.Id) &&
                MatchesSubtypeFilter(entity, query?.Subtypes) &&
                MatchesTargetCulture(entity, target.TargetEntity, query?.TargetCultureMatch ?? QueryTargetCultureMatch.Any) &&
                HasNoOutgoingRelationships(entity.Id, query?.WithoutOutgoingRelationships) &&
                HasNoIncomingRelationships(entity.Id, query?.WithoutIncomingRelationships) &&
                MatchesAttributeConditions(entity, query?.AttributeConditions) &&
                (!(query?.AdjacentToTarget ?? false) ||
                 HexCoord.Distance(
                     new HexCoord(entity.Coordinates.Q, entity.Coordinates.R),
                     target.PhysicalOrigin) == 1))
            .Cast<Entity>()
            .ToList();

        if (matches.Count == 0)
        {
            return null;
        }

        return matches[_random.Next(matches.Count)];
    }

    private static DefinitionEntityQuery MergeSubtypeFilter(
        DefinitionEntityQuery query,
        string? subtype,
        IReadOnlySet<string>? subtypes)
    {
        if (query.Subtypes is { Count: > 0 })
        {
            return query;
        }

        IReadOnlySet<string>? effectiveSubtypes = null;
        if (!string.IsNullOrWhiteSpace(subtype))
        {
            effectiveSubtypes = new HashSet<string>(StringComparer.Ordinal) { subtype };
        }
        else if (subtypes is { Count: > 0 })
        {
            effectiveSubtypes = new HashSet<string>(subtypes, StringComparer.Ordinal);
        }

        if (effectiveSubtypes is null)
        {
            return query;
        }

        return new DefinitionEntityQuery
        {
            Subtypes = effectiveSubtypes,
            AdjacentToTarget = query.AdjacentToTarget,
            WithoutOutgoingRelationships = query.WithoutOutgoingRelationships,
            WithoutIncomingRelationships = query.WithoutIncomingRelationships,
            TargetCultureMatch = query.TargetCultureMatch,
            AttributeConditions = query.AttributeConditions,
        };
    }

    private bool HasNoOutgoingRelationships(EntityId entityId, IReadOnlySet<string>? kinds)
    {
        if (kinds is null || kinds.Count == 0)
        {
            return true;
        }

        return kinds.All(kind => !_graph.OutgoingRelationships(entityId, kind).Any());
    }

    private bool HasNoIncomingRelationships(EntityId entityId, IReadOnlySet<string>? kinds)
    {
        if (kinds is null || kinds.Count == 0)
        {
            return true;
        }

        return kinds.All(kind => !_graph.IncomingRelationships(entityId, kind).Any());
    }

    private static bool MatchesAttributeConditions(
        Entity entity,
        IReadOnlyList<DefinitionQueryAttributeCondition>? conditions)
    {
        if (conditions is null || conditions.Count == 0)
        {
            return true;
        }

        return conditions.All(condition =>
            TriggerPasses(condition.Direction, entity.Attributes.GetValue(condition.AttributeId.Value)));
    }

    private static bool MatchesTargetCulture(
        Entity entity,
        Entity target,
        QueryTargetCultureMatch cultureMatch)
    {
        return cultureMatch switch
        {
            QueryTargetCultureMatch.Any => true,
            QueryTargetCultureMatch.SameAsTarget => string.Equals(entity.Culture, target.Culture, StringComparison.Ordinal),
            QueryTargetCultureMatch.DifferentFromTarget => !string.Equals(entity.Culture, target.Culture, StringComparison.Ordinal),
            _ => true,
        };
    }

    private static bool TriggerPasses(TriggerDirection direction, float value)
    {
        return direction switch
        {
            TriggerDirection.Low => value <= StandardNeedScale.Concern,
            TriggerDirection.Critical => value <= StandardNeedScale.Critical,
            TriggerDirection.High => value >= StandardNeedScale.Comfortable,
            TriggerDirection.Any => true,
            _ => false,
        };
    }

    private string ResolveCulture(
        DefinitionGeneratorNode generator,
        DefinitionProducedEntity produced,
        Dictionary<string, EntityId?> symbols,
        Entity? targetEntity)
    {
        string? resolved;
        if (string.Equals(produced.InheritCultureFrom, "$target", StringComparison.Ordinal))
        {
            resolved = targetEntity?.Culture;
        }
        else
        {
            if (!symbols.TryGetValue(produced.InheritCultureFrom, out var entityId) || entityId is null)
            {
                throw new InvalidOperationException(
                    $"Generator '{generator.Id}' produce '{produced.Ref}' inherits culture from '{produced.InheritCultureFrom}' which has not been created (optional produce that did not roll?).");
            }
            resolved = _graph.GetEntity(entityId.Value)?.Culture;
        }

        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException(
                $"Generator '{generator.Id}' produce '{produced.Ref}' inherits culture from '{produced.InheritCultureFrom}' which has no culture assigned. Explicit culture required — no silent fallback.");
        }

        return resolved;
    }

    private string ResolveProducedSubtype(DefinitionProducedEntity produced)
    {
        if (!string.IsNullOrWhiteSpace(produced.Subtype))
        {
            return produced.Subtype;
        }

        if (produced.Subtypes is { Count: > 0 })
        {
            var options = produced.Subtypes.ToArray();
            return options[_random.Next(options.Length)];
        }

        throw new InvalidOperationException(
            $"Produced ref '{produced.Ref}' has no concrete subtype choice.");
    }

    private (HexCoord Physical, SemanticHexCoord Semantic) ResolvePlacement(
        DefinitionProducedEntity produced,
        ExecutionTargetContext target,
        IReadOnlyDictionary<string, EntityId?> symbols)
    {
        var anchor = ResolvePlacementAnchor(produced.PlacementRef, target, symbols);
        var physical = ResolvePhysicalPlacement(produced.PhysicalPlacement, anchor);
        var semantic = ResolveSemanticPlacement(produced.SemanticPlacement, anchor);
        return (physical, semantic);
    }

    private ExecutionTargetContext ResolvePlacementAnchor(
        string reference,
        ExecutionTargetContext target,
        IReadOnlyDictionary<string, EntityId?> symbols)
    {
        if (string.Equals(reference, "$target", StringComparison.Ordinal))
        {
            return target;
        }

        if (!symbols.TryGetValue(reference, out var entityId) || entityId is null)
        {
            return target;
        }

        var entity = _graph.GetEntity(entityId.Value);
        if (entity is null)
        {
            return target;
        }

        return new ExecutionTargetContext(
            entity,
            new HexCoord(entity.Coordinates.Q, entity.Coordinates.R),
            entity.SemanticPosition,
            target.BoundEntities,
            target.PrimaryPhysicalOrigin,
            target.PrimarySemanticOrigin);
    }

    private HexCoord ResolvePhysicalPlacement(SemanticPlacement placement, ExecutionTargetContext target)
    {
        return placement switch
        {
            SemanticPlacement.On => ResolveOnPhysicalPlacement(target),
            SemanticPlacement.Across => FindAcrossPhysicalHex(target) ??
                                        _spatialIndex.FindFreePhysicalHex(ReflectAcrossMapCenter(target.PhysicalOrigin), maxRadius: _spatialIndex.Grid.Radius) ??
                                        target.PhysicalOrigin,
            SemanticPlacement.InRegionLocality => _spatialIndex.FindRandomFreePhysicalHexInCoverage(target.TargetEntity.Id, _random) ??
                                                  _spatialIndex.FindFreePhysicalHex(target.PhysicalOrigin) ??
                                                  target.PhysicalOrigin,
            _ => _spatialIndex.FindFreePhysicalHex(target.PhysicalOrigin) ?? target.PhysicalOrigin,
        };
    }

    private SemanticHexCoord ResolveSemanticPlacement(SemanticPlacement placement, ExecutionTargetContext target)
    {
        return placement switch
        {
            SemanticPlacement.On => ResolveOnSemanticPlacement(target),
            SemanticPlacement.Across => FindAcrossSemanticHex(target) ??
                                        _semanticPlaneIndex.FindFreeSemanticHex(ReflectAcrossMapCenter(target.SemanticOrigin), maxRadius: 24) ??
                                        target.SemanticOrigin,
            SemanticPlacement.InRegionLocality => _semanticPlaneIndex.FindFreeSemanticHex(target.SemanticOrigin) ??
                                                  target.SemanticOrigin,
            _ => _semanticPlaneIndex.FindFreeSemanticHex(target.SemanticOrigin) ?? target.SemanticOrigin,
        };
    }

    private HexCoord ResolveOnPhysicalPlacement(ExecutionTargetContext target)
    {
        if (IsPhysicalOccupied(target.PhysicalOrigin))
        {
            return _spatialIndex.FindFreePhysicalHex(target.PhysicalOrigin) ?? target.PhysicalOrigin;
        }

        return target.PhysicalOrigin;
    }

    private SemanticHexCoord ResolveOnSemanticPlacement(ExecutionTargetContext target)
    {
        if (IsSemanticOccupied(target.SemanticOrigin))
        {
            return _semanticPlaneIndex.FindFreeSemanticHex(target.SemanticOrigin) ?? target.SemanticOrigin;
        }

        return target.SemanticOrigin;
    }

    private HexCoord? FindAcrossPhysicalHex(ExecutionTargetContext anchor)
    {
        var mirrored = ReflectAcrossMapCenter(anchor.PhysicalOrigin);
        if (IsFreePhysical(mirrored))
        {
            return mirrored;
        }

        return _spatialIndex.FindFreePhysicalHex(mirrored, maxRadius: _spatialIndex.Grid.Radius);
    }

    private SemanticHexCoord? FindAcrossSemanticHex(ExecutionTargetContext anchor)
    {
        var mirrored = ReflectAcrossMapCenter(anchor.SemanticOrigin);
        if (!IsSemanticOccupied(mirrored))
        {
            return mirrored;
        }

        return _semanticPlaneIndex.FindFreeSemanticHex(mirrored, maxRadius: 24);
    }

    private static HexCoord ReflectAcrossMapCenter(HexCoord origin) =>
        new(-origin.Q, -origin.R);

    private static SemanticHexCoord ReflectAcrossMapCenter(SemanticHexCoord origin) =>
        new(-origin.Q, -origin.R);

    private bool IsPhysicalOccupied(HexCoord coord) =>
        _spatialIndex.EntitiesAtCell(coord)
            .Select(entityId => _graph.GetEntity(entityId))
            .Any(entity => entity?.Status == EntityStatus.Active);

    private bool IsFreePhysical(HexCoord coord)
    {
        var cell = _spatialIndex.Grid.GetCell(coord);
        return cell is not null &&
               !float.IsPositiveInfinity(cell.MovementCost) &&
               !IsPhysicalOccupied(coord);
    }

    private bool IsSemanticOccupied(SemanticHexCoord coord) =>
        _graph.AllEntities.Values.Any(entity => entity.Status == EntityStatus.Active && entity.SemanticPosition == coord);

    private static EntityId? ResolveCoverageSourceRef(
        string? sourceRef,
        Dictionary<string, EntityId?> symbols)
    {
        if (string.IsNullOrWhiteSpace(sourceRef))
        {
            return null;
        }

        return symbols.TryGetValue(sourceRef, out var entityId) ? entityId : null;
    }


    private sealed record ApplicableGenerator(
        GeneratorInstance Instance,
        List<ResolvedGeneratorTarget> Targets,
        int Weight);

    private sealed record ResolvedGeneratorTarget(
        Entity TargetEntity,
        HexCoord PhysicalOrigin,
        IReadOnlyDictionary<string, Entity> BoundEntities);

    private sealed record ExecutionTargetContext(
        Entity TargetEntity,
        HexCoord PhysicalOrigin,
        SemanticHexCoord SemanticOrigin,
        IReadOnlyDictionary<string, Entity> BoundEntities,
        HexCoord PrimaryPhysicalOrigin,
        SemanticHexCoord PrimarySemanticOrigin)
    {
        public ExecutionTargetContext(
            Entity targetEntity,
            HexCoord physicalOrigin,
            SemanticHexCoord semanticOrigin,
            IReadOnlyDictionary<string, Entity> boundEntities)
            : this(targetEntity, physicalOrigin, semanticOrigin, boundEntities, physicalOrigin, semanticOrigin)
        {
        }
    }
}
