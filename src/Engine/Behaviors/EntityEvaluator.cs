namespace Engine.Behaviors;

using Engine.Graph;
using Engine.Messages;
using Engine.Processes;

/// <summary>
/// Per-entity per-tick evaluation. Selects a goal, finds processes that serve it,
/// picks the best viable one, executes.
///
/// Goals are regenerated every N ticks or when a sub-goal fires.
/// No locality branching. All processes go through the same flow.
/// </summary>
public sealed class EntityEvaluator
{
    private readonly ActivityRegistry _registry;
    private readonly MessageBus _messageBus;
    private readonly Random _random;
    private readonly GoalGenerator? _goalGenerator;

    /// <summary>Minimum score for a process to be considered viable.</summary>
    public float MinimumViableScore { get; set; } = 0.01f;

    /// <summary>How often to regenerate goals (in ticks).</summary>
    public int GoalRegenerationInterval { get; set; } = 10;

    public EntityEvaluator(ActivityRegistry registry, MessageBus messageBus,
        Random? random = null, GoalGenerator? goalGenerator = null)
    {
        _registry = registry;
        _messageBus = messageBus;
        _random = random ?? Random.Shared;
        _goalGenerator = goalGenerator;
    }

    public void Evaluate(Entity entity, IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(worldState);

        if (entity.Status != EntityStatus.Active) return;

        // Regenerate goals if needed
        if (_goalGenerator is not null &&
            (entity.Goals.Count == 0 ||
             worldState.CurrentTick - entity.GoalsGeneratedAt >= GoalRegenerationInterval))
        {
            RegenerateGoals(entity, worldState);
        }

        EvaluateProcesses(entity, worldState);

        // Decision selection (card-gated, separate from processes)
        var decisions = _registry.GetDecisions(entity.Kind);
        if (decisions.Count > 0)
        {
            var viableDecisions = new List<(IDecision Decision, float Score)>();
            foreach (var decision in decisions)
            {
                if (!decision.CanExecute(entity, worldState)) continue;
                var score = decision.Score(entity, worldState);
                if (score >= MinimumViableScore) viableDecisions.Add((decision, score));
            }

            if (viableDecisions.Count > 0)
            {
                var winner = WeightedRandomSelectDecision(viableDecisions);
                if (winner is not null)
                {
                    var message = winner.Value.Decision.Execute(entity, worldState);
                    _messageBus.Publish(message);
                    entity.RecordAction(worldState.CurrentTick, winner.Value.Decision.Name);
                }
            }
        }
    }

    private (IDecision Decision, float Score)? WeightedRandomSelectDecision(
        List<(IDecision Decision, float Score)> candidates)
    {
        if (candidates.Count == 0) return null;
        var totalWeight = 0f;
        foreach (var (_, score) in candidates) totalWeight += score;
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

    public void EvaluateAll(WorldGraph graph, IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(worldState);

        foreach (var (_, entity) in graph.AllEntities)
        {
            Evaluate(entity, worldState);
        }
    }

    private void RegenerateGoals(Entity entity, IWorldState worldState)
    {
        entity.Goals.Clear();
        foreach (var goal in _goalGenerator!.Generate(entity, worldState))
        {
            entity.Goals.Add(goal);
        }

        entity.GoalsGeneratedAt = worldState.CurrentTick;
    }

    private void EvaluateProcesses(Entity entity, IWorldState worldState)
    {
        // Entity-wide cooldown check
        if (worldState.CurrentTick < entity.EntityCooldownUntil) return;

        if (entity.Goals.Count == 0)
        {
            EvaluateWithoutGoals(entity, worldState);
            return;
        }

        // Try goals in weighted random order until one has a viable process
        var processes = _registry.GetProcesses(entity.Kind);
        var tick = worldState.CurrentTick;
        var triedGoals = new HashSet<string>();

        for (var attempts = 0; attempts < entity.Goals.Count; attempts++)
        {
            var goal = SelectGoal(entity, triedGoals);
            if (goal is null) break;
            triedGoals.Add(goal.Id);

            var candidates = new List<(IProcess Process, float Score)>();
            foreach (var process in processes)
            {
                if (!ServesGoal(process, goal, entity)) continue;

                if (entity.ProcessCooldowns.TryGetValue(process.Name, out var cooldownUntil)
                    && tick < cooldownUntil) continue;

                var score = process.Score(entity, worldState);
                if (score < MinimumViableScore) continue;

                if (!process.CanExecute(entity, worldState)) continue;

                candidates.Add((process, score));
            }

            if (candidates.Count == 0) continue; // Try next goal

            var winner = WeightedRandomSelect(candidates);
            if (winner is null) continue;

            var targetId = goal.Provider != entity.Id ? goal.Provider : (EntityId?)null;
            ExecuteProcess(entity, worldState, winner.Value.Process, targetId, goal);
            return;
        }
    }

    private void EvaluateWithoutGoals(Entity entity, IWorldState worldState)
    {
        var processes = _registry.GetProcesses(entity.Kind);
        var candidates = new List<(IProcess Process, float Score)>();

        foreach (var process in processes)
        {
            var score = process.Score(entity, worldState);
            if (score < MinimumViableScore) continue;
            if (!process.CanExecute(entity, worldState)) continue;
            candidates.Add((process, score));
        }

        if (candidates.Count == 0) return;

        var winner = WeightedRandomSelect(candidates);
        if (winner is not null)
        {
            ExecuteProcess(entity, worldState, winner.Value.Process, null, null);
        }
    }

    private void ExecuteProcess(Entity entity, IWorldState worldState,
        IProcess process, EntityId? targetId, EntityGoal? goal)
    {
        entity.CurrentTargetId = targetId;

        var message = process.Execute(entity, worldState);
        _messageBus.Publish(message);
        entity.RecordAction(worldState.CurrentTick, process.Name);

        entity.CurrentTargetId = null;

        // Apply cooldowns with ±25% jitter
        if (process is DataDrivenProcess ddp)
        {
            var tick = worldState.CurrentTick;
            if (ddp.Definition.Cooldown > 0)
            {
                var jitter = 1f + ((float)(_random.NextDouble() * 0.5 - 0.25));
                var cd = (int)(ddp.Definition.Cooldown * jitter);
                entity.ProcessCooldowns[process.Name] = tick + Math.Max(1, cd);
            }

            if (ddp.Definition.EntityCooldown > 0)
            {
                var jitter = 1f + ((float)(_random.NextDouble() * 0.5 - 0.25));
                var cd = (int)(ddp.Definition.EntityCooldown * jitter);
                entity.EntityCooldownUntil = tick + Math.Max(1, cd);
            }
        }

        // If a sub-goal's process fired, trigger goal regeneration next tick
        if (goal is not null && goal.IsSubGoal)
        {
            entity.GoalsGeneratedAt = 0; // Force regeneration next tick
        }
    }

    private EntityGoal? SelectGoal(Entity entity, HashSet<string>? exclude = null)
    {
        if (entity.Goals.Count == 0) return null;

        var totalUrgency = 0f;
        foreach (var goal in entity.Goals)
        {
            if (exclude is not null && exclude.Contains(goal.Id)) continue;
            totalUrgency += goal.Urgency;
        }

        if (totalUrgency <= 0f) return null;

        var roll = (float)(_random.NextDouble() * totalUrgency);
        var cumulative = 0f;
        EntityGoal? last = null;
        foreach (var goal in entity.Goals)
        {
            if (exclude is not null && exclude.Contains(goal.Id)) continue;
            last = goal;
            cumulative += goal.Urgency;
            if (roll <= cumulative) return goal;
        }

        return last;
    }

    private static bool ServesGoal(IProcess process, EntityGoal goal, Entity entity)
    {
        // A process serves a goal if it writes to the goal's target attribute
        if (process is not DataDrivenProcess ddp) return false;

        var targetAttr = goal.TargetAttribute;
        foreach (var write in ddp.Definition.Writes)
        {
            if (write.Name == targetAttr) return true;
        }

        return false;
    }

    private (IProcess Process, float Score)? WeightedRandomSelect(
        List<(IProcess Process, float Score)> candidates)
    {
        if (candidates.Count == 0) return null;

        var totalWeight = 0f;
        foreach (var (_, score) in candidates) totalWeight += score;
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
}
