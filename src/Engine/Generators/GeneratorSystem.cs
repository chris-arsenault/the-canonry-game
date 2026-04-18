namespace Engine.Generators;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Graph;
using Engine.Map;
using Engine.Messages;

/// <summary>
/// Selects one applicable generator/target pair on each generator pulse and executes it.
/// </summary>
public sealed class GeneratorSystem
{
    private const string DefaultGeneratedCulture = "none";

    private readonly WorldGraph _graph;
    private readonly GeneratorInstanceRegistry _instances;
    private readonly DefinitionGraph _definitionGraph;
    private readonly SpatialIndex _spatialIndex;
    private readonly SemanticPlaneIndex _semanticPlaneIndex;
    private readonly MessageBus _messageBus;
    private readonly Random _random;

    public GeneratorSystem(
        WorldGraph graph,
        GeneratorInstanceRegistry instances,
        DefinitionGraph definitionGraph,
        SpatialIndex spatialIndex,
        SemanticPlaneIndex semanticPlaneIndex,
        MessageBus messageBus,
        Random random)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _instances = instances ?? throw new ArgumentNullException(nameof(instances));
        _definitionGraph = definitionGraph ?? throw new ArgumentNullException(nameof(definitionGraph));
        _spatialIndex = spatialIndex ?? throw new ArgumentNullException(nameof(spatialIndex));
        _semanticPlaneIndex = semanticPlaneIndex ?? throw new ArgumentNullException(nameof(semanticPlaneIndex));
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

        var winner = ChooseWeightedGenerator(applicableByGenerator.Values);
        var target = winner.Targets[_random.Next(winner.Targets.Count)];
        Fire(winner.Instance.Definition, target, currentTick);
    }

    private void Fire(DefinitionGeneratorNode generator, ResolvedGeneratorTarget target, int currentTick)
    {
        var targetContext = new ExecutionTargetContext(
            target.TargetEntity,
            target.PhysicalOrigin,
            target.TargetEntity.SemanticPosition);

        var instance = _instances.GetOrCreate(generator);
        var symbols = new Dictionary<string, EntityId?>(StringComparer.Ordinal)
        {
            ["$target"] = targetContext.TargetEntity.Id,
        };
        var createdEntityIds = new List<EntityId>();
        var createdRelationshipCount = 0;

        foreach (var produced in generator.Produces)
        {
            if (produced.CreateChance < 1f && _random.NextSingle() > produced.CreateChance)
            {
                symbols[produced.Ref] = null;
                continue;
            }

            var culture = ResolveCulture(generator, produced, symbols, targetContext.TargetEntity);
            var name = NameSynthesizer.SynthesizeName(produced.Kind, produced.Subtype, culture, _random);
            var (physicalCoord, semanticCoord) = ResolvePlacement(produced, targetContext);
            var entity = _graph.CreateEntity(
                produced.Kind,
                produced.Subtype,
                name,
                culture,
                currentTick,
                (physicalCoord.Q, physicalCoord.R));

            entity.SemanticPosition = semanticCoord;
            _spatialIndex.PlaceEntity(entity.Id, physicalCoord);

            symbols[produced.Ref] = entity.Id;
            createdEntityIds.Add(entity.Id);
        }

        foreach (var relationship in generator.Relationships)
        {
            if (!symbols.TryGetValue(relationship.SourceRef, out var sourceId) || sourceId is null ||
                !symbols.TryGetValue(relationship.DestinationRef, out var destinationId) || destinationId is null)
            {
                continue;
            }

            _graph.CreateRelationship(
                relationship.RelationshipId.Value,
                RelationshipPolarity.Neutral,
                sourceId.Value,
                destinationId.Value,
                relationship.Strength,
                currentTick);
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

            if (ExceedsSaturation(generator.Target.SaturationLimits, entity.Id))
            {
                continue;
            }

            if (generator.Target.AllowWilderness)
            {
                var regionOrigin = new HexCoord(entity.Coordinates.Q, entity.Coordinates.R);
                var wildernessOrigin = _spatialIndex.FindFreePhysicalHex(regionOrigin);
                if (wildernessOrigin is not HexCoord freeHex)
                {
                    continue;
                }

                yield return new ResolvedGeneratorTarget(entity, freeHex);
                continue;
            }

            yield return new ResolvedGeneratorTarget(entity, new HexCoord(entity.Coordinates.Q, entity.Coordinates.R));
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

    private bool ExceedsSaturation(
        IReadOnlyList<DefinitionSaturationLimit> saturationLimits,
        EntityId targetId)
    {
        foreach (var limit in saturationLimits)
        {
            var count = limit.Direction.Equals("out", StringComparison.OrdinalIgnoreCase)
                ? _graph.OutgoingRelationships(targetId, limit.RelationshipId.Value).Count()
                : _graph.IncomingRelationships(targetId, limit.RelationshipId.Value).Count();
            if (count >= limit.MaxCount)
            {
                return true;
            }
        }

        return false;
    }

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
        return trigger.Direction switch
        {
            TriggerDirection.Low => value <= StandardNeedScale.Concern,
            TriggerDirection.Critical => value <= StandardNeedScale.Critical,
            TriggerDirection.High => value >= StandardNeedScale.Comfortable,
            TriggerDirection.Any => true,
            _ => false,
        };
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

    private string ResolveCulture(
        DefinitionGeneratorNode generator,
        DefinitionProducedEntity produced,
        Dictionary<string, EntityId?> symbols,
        Entity? targetEntity)
    {
        if (string.IsNullOrWhiteSpace(produced.InheritCultureFrom))
        {
            return DefaultGeneratedCulture;
        }

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

    private (HexCoord Physical, SemanticHexCoord Semantic) ResolvePlacement(
        DefinitionProducedEntity produced,
        ExecutionTargetContext target)
    {
        return produced.Placement switch
        {
            SemanticPlacement.On => ResolveOnPlacement(target),
            SemanticPlacement.Across => ResolveAcrossPlacement(produced, target),
            _ => ResolveNearPlacement(target),
        };
    }

    private (HexCoord Physical, SemanticHexCoord Semantic) ResolveAcrossPlacement(
        DefinitionProducedEntity produced,
        ExecutionTargetContext target)
    {
        System.Diagnostics.Debug.WriteLine(
            $"Generator placement '{produced.Placement}' is not implemented; falling back to Near.");
        return ResolveNearPlacement(target);
    }

    private (HexCoord Physical, SemanticHexCoord Semantic) ResolveOnPlacement(ExecutionTargetContext target)
    {
        if (IsPhysicalOccupied(target.PhysicalOrigin) || IsSemanticOccupied(target.SemanticOrigin))
        {
            return ResolveNearPlacement(target);
        }

        return (target.PhysicalOrigin, target.SemanticOrigin);
    }

    private (HexCoord Physical, SemanticHexCoord Semantic) ResolveNearPlacement(ExecutionTargetContext target)
    {
        var physical = _spatialIndex.FindFreePhysicalHex(target.PhysicalOrigin) ?? target.PhysicalOrigin;
        var semantic = _semanticPlaneIndex.FindFreeSemanticHex(target.SemanticOrigin) ?? target.SemanticOrigin;
        return (physical, semantic);
    }

    private bool IsPhysicalOccupied(HexCoord coord) =>
        _spatialIndex.EntitiesAtCell(coord)
            .Select(entityId => _graph.GetEntity(entityId))
            .Any(entity => entity?.Status == EntityStatus.Active);

    private bool IsSemanticOccupied(SemanticHexCoord coord) =>
        _graph.AllEntities.Values.Any(entity => entity.Status == EntityStatus.Active && entity.SemanticPosition == coord);


    private sealed record ApplicableGenerator(
        GeneratorInstance Instance,
        IReadOnlyList<ResolvedGeneratorTarget> Targets,
        int Weight);

    private sealed record ResolvedGeneratorTarget(
        Entity TargetEntity,
        HexCoord PhysicalOrigin);

    private sealed record ExecutionTargetContext(
        Entity TargetEntity,
        HexCoord PhysicalOrigin,
        SemanticHexCoord SemanticOrigin);
}
