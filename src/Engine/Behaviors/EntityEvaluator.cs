namespace Engine.Behaviors;

using Engine.Graph;
using Engine.Messages;

/// <summary>
/// Three-phase entity evaluator. Replaces BehaviorSystem + UtilityEvaluator.
/// Per entity per tick:
///   1. Physics (automatic, unconditional -- handled externally by lifecycle/relationships/resources)
///   2. Process selection (utility AI, weighted random from ALL viable, one picked)
///   3. Decision selection (utility AI, weighted random from card-gated viable, one picked)
/// </summary>
public sealed class EntityEvaluator
{
    private readonly ActivityRegistry _registry;
    private readonly MessageBus _messageBus;
    private readonly Random _random;

    /// <summary>Minimum score for a process/decision to be considered viable.</summary>
    public float MinimumViableScore { get; set; } = 0.01f;

    public EntityEvaluator(ActivityRegistry registry, MessageBus messageBus, Random? random = null)
    {
        _registry = registry;
        _messageBus = messageBus;
        _random = random ?? Random.Shared;
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

        // Phase 2: Process selection (one from all viable)
        var process = SelectProcess(entity, worldState);
        if (process is not null)
        {
            var message = process.Execute(entity, worldState);
            _messageBus.Publish(message);
        }

        // Phase 3: Decision selection (one from card-gated viable)
        var decision = SelectDecision(entity, worldState);
        if (decision is not null)
        {
            var message = decision.Execute(entity, worldState);
            _messageBus.Publish(message);
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

    private IProcess? SelectProcess(Entity entity, IWorldState worldState)
    {
        var processes = _registry.GetProcesses(entity.Kind);
        if (processes.Count == 0)
        {
            return null;
        }

        // Score all viable processes
        var viable = new List<(IProcess Process, float Score)>();
        foreach (var process in processes)
        {
            if (!process.CanExecute(entity, worldState))
            {
                continue;
            }

            var score = process.Score(entity, worldState);
            if (score >= MinimumViableScore)
            {
                viable.Add((process, score));
            }
        }

        return WeightedRandomSelect(viable);
    }

    private IDecision? SelectDecision(Entity entity, IWorldState worldState)
    {
        var decisions = _registry.GetDecisions(entity.Kind);
        if (decisions.Count == 0)
        {
            return null;
        }

        // Score all viable decisions (card-gate checked in CanExecute)
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

        return WeightedRandomSelectDecision(viable);
    }

    private T? WeightedRandomSelect<T>(List<(T Item, float Score)> viable) where T : class
    {
        if (viable.Count == 0)
        {
            return null;
        }

        // ALL viable candidates compete -- no top-N cap
        var totalWeight = 0f;
        foreach (var (_, score) in viable)
        {
            totalWeight += score;
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        var roll = (float)(_random.NextDouble() * totalWeight);
        var cumulative = 0f;

        foreach (var (item, score) in viable)
        {
            cumulative += score;
            if (roll <= cumulative)
            {
                return item;
            }
        }

        return viable[^1].Item;
    }

    // Separate method to satisfy generic constraints (IProcess vs IDecision)
    private IDecision? WeightedRandomSelectDecision(List<(IDecision Decision, float Score)> viable)
    {
        return WeightedRandomSelect(viable);
    }
}
