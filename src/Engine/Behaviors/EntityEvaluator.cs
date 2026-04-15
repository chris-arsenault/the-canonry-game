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
        var tick = worldState.CurrentTick;
        var reportCandidates = new List<ProcessCandidate>();

        // Entity-wide cooldown check
        if (tick < entity.EntityCooldownUntil)
        {
            entity.LastEvaluation = new EvaluationReport
            { Tick = tick, Candidates = reportCandidates };
            return;
        }

        var processes = _registry.GetProcesses(entity.Kind);
        string? selectedGoalId = null;
        string? executedName = null;

        // Evaluate all goals and all serving processes, recording everything
        var triedGoals = new HashSet<string>();
        var executed = false;

        for (var attempts = 0; !executed && attempts < Math.Max(1, entity.Goals.Count); attempts++)
        {
            var goal = entity.Goals.Count > 0 ? SelectGoal(entity, triedGoals) : null;
            if (goal is not null) triedGoals.Add(goal.Id);

            var goalId = goal?.Id ?? "";
            var providerName = goal is not null
                ? (worldState.Graph.GetEntity(goal.Provider)?.Name ?? goal.Provider.ToString())
                : "none";

            var candidates = new List<(IProcess Process, float Score)>();

            foreach (var process in processes)
            {
                if (goal is not null && !ServesGoal(process, goal)) continue;

                var onCooldown = entity.ProcessCooldowns.TryGetValue(process.Name, out var cdUntil)
                    && tick < cdUntil;

                // Evaluate precondition first (reveals real failure reason)
                PreconditionResult precondResult;
                if (onCooldown)
                {
                    precondResult = PreconditionResult.Fail($"cooldown until tick {cdUntil}");
                }
                else
                {
                    precondResult = process is DataDrivenProcess ddp2
                        ? ddp2.Definition.Precondition.Evaluate(
                            new ProcessMutationContext(entity, worldState, goal: goal))
                        : process.CanExecute(entity, worldState, goal)
                            ? PreconditionResult.Pass()
                            : PreconditionResult.Fail("precondition failed");
                }

                // Score only matters if precondition passed
                var score = precondResult.Passed ? process.Score(entity, worldState) : 0f;
                var viable = precondResult.Passed && score >= MinimumViableScore;
                if (viable) candidates.Add((process, score));

                reportCandidates.Add(new ProcessCandidate
                {
                    ProcessName = process.Name,
                    GoalId = goalId,
                    ProviderName = providerName,
                    Score = score,
                    PreconditionPassed = precondResult.Passed,
                    PreconditionDiagnostic = precondResult.Diagnostic,
                    OnCooldown = onCooldown,
                    WasSelected = false, // Updated below if selected
                });
            }

            if (candidates.Count == 0) continue;

            var winner = WeightedRandomSelect(candidates);
            if (winner is null) continue;

            selectedGoalId = goalId;
            executedName = winner.Value.Process.Name;

            // Mark winner in report
            foreach (var c in reportCandidates)
            {
                if (c.ProcessName == executedName && c.GoalId == goalId)
                    c.WasSelected = true;
            }

            var message = winner.Value.Process.Execute(entity, worldState, goal);
            _messageBus.Publish(message);
            entity.RecordAction(tick, winner.Value.Process.Name);

            // Apply cooldowns
            if (winner.Value.Process is DataDrivenProcess ddp)
            {
                if (ddp.Definition.Cooldown > 0)
                {
                    var jitter = 1f + ((float)(_random.NextDouble() * 0.5 - 0.25));
                    entity.ProcessCooldowns[ddp.Name] = tick + Math.Max(1, (int)(ddp.Definition.Cooldown * jitter));
                }

                if (ddp.Definition.EntityCooldown > 0)
                {
                    var jitter = 1f + ((float)(_random.NextDouble() * 0.5 - 0.25));
                    entity.EntityCooldownUntil = tick + Math.Max(1, (int)(ddp.Definition.EntityCooldown * jitter));
                }
            }

            if (goal is not null && goal.IsSubGoal)
            {
                entity.GoalsGeneratedAt = 0;
            }

            executed = true;
        }

        entity.LastEvaluation = new EvaluationReport
        {
            Tick = tick,
            SelectedGoalId = selectedGoalId,
            ExecutedProcessName = executedName,
            Candidates = reportCandidates,
        };
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

    private static bool ServesGoal(IProcess process, EntityGoal goal)
    {
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
