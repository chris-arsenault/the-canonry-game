namespace Engine.Behaviors;

using Engine.Graph;
using Engine.Messages;

/// <summary>
/// Per-entity per-tick process and decision evaluation.
///
/// Every process is scored. Processes that operate on other entities are scored
/// against providers (distance-weighted). The winning process-provider pair is
/// selected via weighted random. If the precondition passes, the process executes.
///
/// No locality branching. No movement. All processes go through the same flow.
/// </summary>
public sealed class EntityEvaluator
{
    private readonly ActivityRegistry _registry;
    private readonly MessageBus _messageBus;
    private readonly Random _random;
    private readonly NeedResolver? _needResolver;

    /// <summary>Minimum score for a process to be considered viable.</summary>
    public float MinimumViableScore { get; set; } = 0.01f;

    public EntityEvaluator(ActivityRegistry registry, MessageBus messageBus,
        Random? random = null, NeedResolver? needResolver = null)
    {
        _registry = registry;
        _messageBus = messageBus;
        _random = random ?? Random.Shared;
        _needResolver = needResolver;
    }

    public void Evaluate(Entity entity, IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(worldState);

        if (entity.Status != EntityStatus.Active) return;

        EvaluateProcesses(entity, worldState);

        var decision = SelectDecision(entity, worldState);
        if (decision is not null)
        {
            var message = decision.Execute(entity, worldState);
            _messageBus.Publish(message);
            entity.LastActionName = decision.Name;
            entity.LastActionTick = worldState.CurrentTick;
        }
    }

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

        // Score every process. Find providers for each. No locality branching.
        var candidates = new List<(IProcess Process, EntityId? TargetId, float Score)>();

        foreach (var process in processes)
        {
            var baseScore = process.Score(entity, worldState);
            if (baseScore < MinimumViableScore) continue;

            // Find providers for this process
            var providers = FindProviders(entity, worldState);

            if (providers.Count > 0)
            {
                // Score each process-provider pair with distance weighting
                foreach (var (providerId, _, weight) in providers)
                {
                    var compositeScore = baseScore * weight;
                    if (compositeScore >= MinimumViableScore)
                    {
                        candidates.Add((process, providerId, compositeScore));
                    }
                }
            }

            // Also add as self-targeted candidate (no provider)
            candidates.Add((process, null, baseScore));
        }

        if (candidates.Count == 0) return;

        // Weighted random selection
        var winner = WeightedRandomSelect(candidates);
        if (winner is null) return;

        var (winProcess, winTargetId, _) = winner.Value;

        // Set goal if target exists
        if (winTargetId is not null)
        {
            var targetEntity = worldState.Graph.GetEntity(winTargetId.Value);
            if (targetEntity?.Coordinates is not null)
            {
                entity.CurrentGoal = new EntityGoal(
                    winProcess.Name, winTargetId.Value,
                    targetEntity.Coordinates.Value, 0f);
            }
        }
        else
        {
            entity.CurrentGoal = null;
        }

        // Check precondition and execute
        if (winProcess.CanExecute(entity, worldState))
        {
            var message = winProcess.Execute(entity, worldState);
            _messageBus.Publish(message);
            entity.LastActionName = winProcess.Name;
            entity.LastActionTick = worldState.CurrentTick;
            entity.CurrentGoal = null;
        }
    }

    private List<(EntityId Id, (int Q, int R) Coord, float Weight)> FindProviders(
        Entity entity, IWorldState worldState)
    {
        if (_needResolver is null) return [];

        var need = _needResolver.Resolve(entity);
        if (need is null) return [];

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

    private (IProcess Process, EntityId? TargetId, float Score)? WeightedRandomSelect(
        List<(IProcess Process, EntityId? TargetId, float Score)> candidates)
    {
        if (candidates.Count == 0) return null;

        var totalWeight = 0f;
        foreach (var (_, _, score) in candidates) totalWeight += score;
        if (totalWeight <= 0f) return null;

        var roll = (float)(_random.NextDouble() * totalWeight);
        var cumulative = 0f;

        foreach (var candidate in candidates)
        {
            cumulative += candidate.Score;
            if (roll <= cumulative) return candidate;
        }

        return candidates[^1];
    }

    private IDecision? SelectDecision(Entity entity, IWorldState worldState)
    {
        var decisions = _registry.GetDecisions(entity.Kind);
        if (decisions.Count == 0) return null;

        var viable = new List<(IDecision Decision, float Score)>();
        foreach (var decision in decisions)
        {
            if (!decision.CanExecute(entity, worldState)) continue;
            var score = decision.Score(entity, worldState);
            if (score >= MinimumViableScore) viable.Add((decision, score));
        }

        if (viable.Count == 0) return null;

        var totalWeight = 0f;
        foreach (var (_, score) in viable) totalWeight += score;
        if (totalWeight <= 0f) return null;

        var roll = (float)(_random.NextDouble() * totalWeight);
        var cumulative = 0f;
        foreach (var (decision, score) in viable)
        {
            cumulative += score;
            if (roll <= cumulative) return decision;
        }

        return viable[^1].Decision;
    }
}
