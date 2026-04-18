namespace Engine.Generators;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Graph;
using Engine.Map;
using Engine.Messages;

/// <summary>
/// Selects one applicable generator/target pair on each generator pulse.
/// Execution is added in a later ticket.
/// </summary>
public sealed class GeneratorSystem
{
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

    public void Tick(int currentTick, IReadOnlySet<string> activeGeneratorIds)
    {
        ArgumentNullException.ThrowIfNull(activeGeneratorIds);

        if (currentTick <= 0 || currentTick % PulseInterval != 0)
        {
            return;
        }

        var applicable = new List<ApplicableGeneratorTarget>();

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

            foreach (var target in ResolveTargets(instance.Definition))
            {
                if (!TriggersPass(instance.Definition, target.TargetEntity))
                {
                    continue;
                }

                applicable.Add(new ApplicableGeneratorTarget(instance, target, weight));
            }
        }

        if (applicable.Count == 0)
        {
            return;
        }

        var winner = ChooseWeighted(applicable);
        winner.Instance.RecordRun(currentTick, winner.Target.TargetEntity.Id);
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

            yield return new ResolvedGeneratorTarget(entity, null);
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

    private ApplicableGeneratorTarget ChooseWeighted(IReadOnlyList<ApplicableGeneratorTarget> applicable)
    {
        var totalWeight = applicable.Sum(item => item.Weight);
        var roll = _random.Next(totalWeight);

        foreach (var item in applicable)
        {
            if (roll < item.Weight)
            {
                return item;
            }

            roll -= item.Weight;
        }

        return applicable[^1];
    }

    private sealed record ApplicableGeneratorTarget(
        GeneratorInstance Instance,
        ResolvedGeneratorTarget Target,
        int Weight);

    private sealed record ResolvedGeneratorTarget(
        Entity TargetEntity,
        HexCoord? WildernessOrigin);
}
