namespace Engine.Behaviors;

using Engine.Graph;
using Engine.Map;
using Engine.Messages;

/// <summary>
/// Three-phase entity evaluator.
/// Per entity per tick:
///   1. Physics (automatic, unconditional -- handled externally by lifecycle/relationships/resources)
///   2. Process selection (utility AI, process-provider pairs scored with distance)
///   3. Decision selection (utility AI, weighted random from card-gated viable, one picked)
///
/// Process selection uses the Sims model: each process scores against all reachable
/// providers on the graph. The winning process-provider pair becomes the entity's
/// current goal. If the entity is at the target, execute the process. If not, step
/// one hex toward it. Re-evaluate each tick for preemption.
/// </summary>
public sealed class EntityEvaluator
{
    private readonly ActivityRegistry _registry;
    private readonly MessageBus _messageBus;
    private readonly Random _random;
    private readonly NeedResolver? _needResolver;

    /// <summary>Minimum score for a process/decision to be considered viable.</summary>
    public float MinimumViableScore { get; set; } = 0.01f;

    public EntityEvaluator(ActivityRegistry registry, MessageBus messageBus,
        Random? random = null, NeedResolver? needResolver = null)
    {
        _registry = registry;
        _messageBus = messageBus;
        _random = random ?? Random.Shared;
        _needResolver = needResolver;
    }

    /// <summary>
    /// Evaluate one entity: select and execute one process, then one decision.
    /// Physics are handled by separate systems (lifecycle, relationships, resources).
    /// </summary>
    public void Evaluate(Entity entity, IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(worldState);

        if (entity.Status != EntityStatus.Active)
        {
            return;
        }

        // Phase 2: Process-provider selection
        EvaluateProcesses(entity, worldState);

        // Phase 3: Decision selection (one from card-gated viable)
        var decision = SelectDecision(entity, worldState);
        if (decision is not null)
        {
            var message = decision.Execute(entity, worldState);
            _messageBus.Publish(message);
            entity.LastActionName = decision.Name;
            entity.LastActionTick = worldState.CurrentTick;
        }
    }

    /// <summary>
    /// Evaluate ALL active entities of ALL kinds.
    /// Called once per tick during the process/decision phases.
    /// </summary>
    public void EvaluateAll(WorldGraph graph, IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(worldState);

        foreach (var (_, entity) in graph.AllEntities)
        {
            Evaluate(entity, worldState);
        }
    }

    private void EvaluateProcesses(Entity entity, IWorldState worldState)
    {
        var processes = _registry.GetProcesses(entity.Kind);
        if (processes.Count == 0) return;

        // Score all process-provider pairs.
        // Preconditions are NOT checked here — they only gate execution at target.
        // This allows processes like "eat" to score against distant providers
        // even when the precondition (food at current hex) fails.
        var candidates = new List<(IProcess Process, EntityId? ProviderId, (int Q, int R)? ProviderCoord, float Score)>();

        foreach (var process in processes)
        {
            var baseScore = process.Score(entity, worldState);
            if (baseScore < MinimumViableScore) continue;

            var canExecuteLocally = process.CanExecute(entity, worldState);

            if (canExecuteLocally)
            {
                // Process can fire right here — score locally, no provider needed
                candidates.Add((process, null, null, baseScore));
            }
            else
            {
                // Can't execute locally — score against distant providers
                var providerCandidates = FindProviders(entity, process, worldState);
                foreach (var (providerId, providerCoord, providerWeight) in providerCandidates)
                {
                    var compositeScore = baseScore * providerWeight;
                    if (compositeScore >= MinimumViableScore)
                    {
                        candidates.Add((process, providerId, providerCoord, compositeScore));
                    }
                }
            }
        }

        if (candidates.Count == 0) return;

        // Weighted random selection from all process-provider pairs
        var totalWeight = 0f;
        foreach (var (_, _, _, score) in candidates) totalWeight += score;
        if (totalWeight <= 0f) return;

        var roll = (float)(_random.NextDouble() * totalWeight);
        var cumulative = 0f;
        var winner = candidates[^1];

        foreach (var candidate in candidates)
        {
            cumulative += candidate.Score;
            if (roll <= cumulative)
            {
                winner = candidate;
                break;
            }
        }

        // Set or update entity goal
        if (winner.ProviderId is not null && winner.ProviderCoord is not null)
        {
            entity.CurrentGoal = new EntityGoal(
                winner.Process.Name, winner.ProviderId.Value,
                winner.ProviderCoord.Value, winner.Score);
        }
        else
        {
            entity.CurrentGoal = null;
        }

        // At target (or no target): check precondition and execute
        if (IsAtTarget(entity))
        {
            if (winner.Process.CanExecute(entity, worldState))
            {
                var message = winner.Process.Execute(entity, worldState);
                _messageBus.Publish(message);
                entity.LastActionName = winner.Process.Name;
                entity.LastActionTick = worldState.CurrentTick;
            }

            entity.CurrentGoal = null;
        }
        else if (entity.CurrentGoal is not null && entity.Coordinates is not null)
        {
            // Not at target — step one hex toward it
            StepTowardGoal(entity, worldState);
            entity.LastActionName = $"Moving toward {entity.CurrentGoal.TargetCoordinates}";
            entity.LastActionTick = worldState.CurrentTick;
        }
    }

    private List<(EntityId Id, (int Q, int R) Coord, float Weight)> FindProviders(
        Entity entity, IProcess process, IWorldState worldState)
    {
        if (entity.Coordinates is null || _needResolver is null) return [];

        // Use NeedResolver to find what the entity needs, then query for providers
        var need = _needResolver.Resolve(entity);
        if (need is null) return [];

        var consumerCoord = new HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
        var results = new List<(EntityId Id, (int Q, int R) Coord, float Weight)>();

        foreach (var providerRef in need.ProviderRefs)
        {
            var providers = GraphAttributeQuery.FindProviders(providerRef, entity, worldState, topN: 5);
            foreach (var (providerId, weight) in providers)
            {
                var providerEntity = worldState.Graph.GetEntity(providerId);
                if (providerEntity?.Coordinates is null) continue;
                results.Add((providerId, providerEntity.Coordinates.Value, weight));
            }
        }

        return results;
    }

    private static bool IsAtTarget(Entity entity)
    {
        if (entity.CurrentGoal is null) return true; // No goal = "at target"
        if (entity.Coordinates is null) return true;

        return entity.Coordinates.Value.Q == entity.CurrentGoal.TargetCoordinates.Q
            && entity.Coordinates.Value.R == entity.CurrentGoal.TargetCoordinates.R;
    }

    private void StepTowardGoal(Entity entity, IWorldState worldState)
    {
        var current = new HexCoord(entity.Coordinates!.Value.Q, entity.Coordinates!.Value.R);
        var target = new HexCoord(entity.CurrentGoal!.TargetCoordinates.Q, entity.CurrentGoal.TargetCoordinates.R);

        var path = worldState.SpatialIndex.FindPath(current, target);
        if (path is null || path.Count < 2) return;

        var nextHex = path[1];
        var cell = worldState.Grid.GetCell(nextHex);
        if (cell is null || float.IsPositiveInfinity(cell.MovementCost)) return;

        // Cull located_at if leaving a settlement
        foreach (var rel in worldState.Graph.OutgoingRelationships(entity.Id, "located_at"))
        {
            worldState.Graph.CullRelationship(rel, worldState.CurrentTick);
            break; // located_at is exclusive
        }

        // Move
        worldState.Graph.SetCoordinates(entity.Id, nextHex.Q, nextHex.R);

        // Create located_at if arriving at an entity's hex
        foreach (var (_, other) in worldState.Graph.AllEntities)
        {
            if (other.Id == entity.Id) continue;
            if (other.Coordinates is null) continue;
            if (other.Coordinates.Value.Q != nextHex.Q || other.Coordinates.Value.R != nextHex.R) continue;
            if (other.Kind == entity.Kind) continue; // Don't locate_at same kind

            worldState.Graph.CreateRelationship(
                "located_at", RelationshipPolarity.Neutral, entity.Id, other.Id, 1f, worldState.CurrentTick);
            break;
        }

        _messageBus.Publish(new ProcessExecutedMessage(
            worldState.CurrentTick, entity.Id, entity.Kind,
            "move", $"{entity.Name} moved toward {entity.CurrentGoal.TargetCoordinates}"));
    }

    private IDecision? SelectDecision(Entity entity, IWorldState worldState)
    {
        var decisions = _registry.GetDecisions(entity.Kind);
        if (decisions.Count == 0)
        {
            return null;
        }

        var viable = new List<(IDecision Decision, float Score)>();
        foreach (var decision in decisions)
        {
            if (!decision.CanExecute(entity, worldState))
            {
                continue;
            }

            var score = decision.Score(entity, worldState);
            if (score >= MinimumViableScore)
            {
                viable.Add((decision, score));
            }
        }

        return WeightedRandomSelect(viable);
    }

    private T? WeightedRandomSelect<T>(List<(T Item, float Score)> viable) where T : class
    {
        if (viable.Count == 0) return null;

        var totalWeight = 0f;
        foreach (var (_, score) in viable) totalWeight += score;
        if (totalWeight <= 0f) return null;

        var roll = (float)(_random.NextDouble() * totalWeight);
        var cumulative = 0f;

        foreach (var (item, score) in viable)
        {
            cumulative += score;
            if (roll <= cumulative) return item;
        }

        return viable[^1].Item;
    }
}
