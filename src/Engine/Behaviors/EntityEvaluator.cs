namespace Engine.Behaviors;

using Engine.Graph;
using Engine.Messages;
using Engine.Processes;
using Engine.Wiring;
using System;
using System.Text;

/// <summary>
/// Per-entity per-tick evaluation. Selects a goal, finds processes that serve it,
/// picks the best viable one, executes.
///
/// Goals are regenerated every N ticks or when a sub-goal fires.
/// No locality branching. All processes go through the same flow.
/// </summary>
public sealed class EntityEvaluator
{
    private readonly BehaviorDefinitionIndex _definitions;
    private readonly MessageBus _messageBus;
    private readonly Random _random;
    private readonly GoalGenerator? _goalGenerator;

    /// <summary>Minimum score for a process to be considered viable.</summary>
    public float MinimumViableScore { get; set; } = 0.01f;

    /// <summary>How often to regenerate goals (in ticks).</summary>
    public int GoalRegenerationInterval { get; set; } = 10;

    /// <summary>Width of the recency window for repetition penalty, in ticks.</summary>
    public int RecencyWindow { get; set; } = 10;

    /// <summary>Score multiplier applied per recent firing of the same (entity, process) pair.</summary>
    public float RecencyDecayFactor { get; set; } = 0.5f;

    public EntityEvaluator(BehaviorDefinitionIndex definitions, MessageBus messageBus,
        Random? random = null, GoalGenerator? goalGenerator = null)
    {
        _definitions = definitions;
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
             worldState.CurrentTick - entity.GoalsGeneratedAt >= GoalRegenerationInterval ||
             GoalsRequireInvalidation(entity, worldState)))
        {
            RegenerateGoals(entity, worldState);
        }

        EvaluateProcesses(entity, worldState);

        // Decision selection (card-gated, separate from processes)
        var decisions = _definitions.GetDecisions(entity.Kind);
        if (decisions.Count > 0)
        {
            var viableDecisions = new List<(ProcessDefinition Definition, float Score)>();
            foreach (var decision in decisions)
            {
                var context = new ProcessMutationContext(entity, worldState, definitionId: decision.Id);
                if (!decision.Precondition.Evaluate(context).Passed) continue;
                var score = decision.ScoreExpression.Evaluate(
                    new ExpressionContext(entity, grid: worldState.Grid, mutationContext: context));
                if (score >= MinimumViableScore) viableDecisions.Add((decision, score));
            }

            if (viableDecisions.Count > 0)
            {
                var winner = WeightedRandomSelectDecision(viableDecisions);
                if (winner is not null)
                {
                    var message = ExecuteDecision(winner.Value.Definition, entity, worldState);
                    _messageBus.Publish(message);
                    entity.RecordAction(worldState.CurrentTick, winner.Value.Definition.Name, winner.Value.Definition.Id);
                }
            }
        }
    }

    private (ProcessDefinition Definition, float Score)? WeightedRandomSelectDecision(
        List<(ProcessDefinition Definition, float Score)> candidates)
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
        var generatedGoals = _goalGenerator!.Generate(entity, worldState);
        entity.Goals.Clear();
        foreach (var goal in generatedGoals)
        {
            entity.Goals.Add(goal);
        }

        entity.GoalsGeneratedAt = worldState.CurrentTick;
    }

    private void EvaluateProcesses(Entity entity, IWorldState worldState)
    {
        var tick = worldState.CurrentTick;
        var reportCandidates = new List<ProcessCandidate>();
        var trace = new StringBuilder();
        AppendLineInvariant(trace, $"Evaluation Tick {tick}");
        AppendLineInvariant(trace, $"Entity: {entity.Name} ({entity.Id}) kind={entity.Kind}");

        // Entity-wide cooldown check
        if (tick < entity.EntityCooldownUntil)
        {
            entity.LastEvaluation = CreateStaleEvaluationReport(
                entity,
                tick,
                $"entity cooldown until tick {entity.EntityCooldownUntil}");
            var staleAge = Math.Max(0, tick - entity.LastEvaluation.SourceTick);
            AppendLineInvariant(
                trace,
                $"entity cooldown active until tick {entity.EntityCooldownUntil}; reusing report from tick {entity.LastEvaluation.SourceTick} age={staleAge}");
            entity.EvaluationTrace = trace.ToString();
            return;
        }

        var processes = _definitions.GetProcesses(entity.Kind);
        string? selectedGoalId = null;
        string? executedName = null;
        AppendLineInvariant(trace, $"registered processes={processes.Count}");

        if (_goalGenerator is not null && entity.Goals.Count == 0)
        {
            entity.LastEvaluation = CreateEvaluationReport(
                tick,
                sourceTick: tick,
                selectedGoalId: null,
                executedProcessName: null,
                isStale: false,
                statusNote: "no planner goals",
                reportCandidates);
            trace.AppendLine("No planner goals present; idling.");
            entity.EvaluationTrace = trace.ToString();
            return;
        }

        var actionableGoals = entity.Goals.Where(g => g.IsActionable).ToList();
        if (entity.Goals.Count > 0 && actionableGoals.Count == 0)
        {
            entity.GoalsGeneratedAt = 0;
            entity.LastEvaluation = CreateEvaluationReport(
                tick,
                sourceTick: tick,
                selectedGoalId: null,
                executedProcessName: null,
                isStale: false,
                statusNote: "no actionable goals; replanning next tick",
                reportCandidates);
            trace.AppendLine("No actionable goals; forcing replanning.");
            entity.EvaluationTrace = trace.ToString();
            return;
        }

        if (actionableGoals.Count == 0)
        {
            trace.AppendLine("No actionable goals present; evaluating unguided processes.");
        }
        else
        {
            trace.AppendLine("Actionable Goals:");
            foreach (var goal in actionableGoals.OrderByDescending(g => g.Urgency))
            {
                AppendLineInvariant(
                    trace,
                    $"  goal={goal.Id} process={goal.PlannedProcessId ?? "(any)"} target={DescribeTarget(worldState, goal.TargetEntityId)} effect={goal.DesiredEffect} urgency={goal.Urgency:F3} depth={goal.Depth}");
            }
        }

        // Evaluate all goals and all serving processes, recording everything
        var triedGoals = new HashSet<string>();
        var evaluatedPairs = new HashSet<(string ProcessId, EntityId Target)>();
        var executed = false;
        var shouldReplan = false;

        for (var attempts = 0; !executed && attempts < Math.Max(1, actionableGoals.Count); attempts++)
        {
            var goal = actionableGoals.Count > 0 ? SelectGoal(actionableGoals, triedGoals) : null;
            if (goal is not null) triedGoals.Add(goal.Id);

            var goalId = goal?.Id ?? "";
            var providerName = goal is not null
                ? (worldState.Graph.GetEntity(goal.TargetEntityId)?.Name ?? goal.TargetEntityId.ToString())
                : "none";
            AppendLineInvariant(
                trace,
                $"Attempt {attempts + 1}: goal={(goal is null ? "(none)" : goal.Id)} target={providerName} effect={(goal is null ? "(none)" : goal.DesiredEffect.ToString())}");

            var candidates = new List<(ProcessDefinition Definition, float Score)>();

            foreach (var process in processes)
            {
                if (goal is not null && !ServesGoal(process, goal)) continue;

                // Skip process-provider pairs already evaluated in a prior goal attempt.
                // Two goals with the same Provider and TargetAttribute produce identical
                // precondition/score results, so re-evaluating is wasteful and creates
                // duplicate entries in the report.
                if (goal is not null && !evaluatedPairs.Add((process.Id, goal.TargetEntityId)))
                {
                    AppendLineInvariant(
                        trace,
                        $"  skip process={process.Name} target={providerName}: already evaluated for this target");
                    continue;
                }

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
                    var processContext = new ProcessMutationContext(entity, worldState, goal: goal, definitionId: process.Id);
                    precondResult = process.Precondition.Evaluate(processContext);
                }

                // Score only matters if precondition passed
                var targetEntity = goal is not null
                    ? worldState.Graph.GetEntity(goal.TargetEntityId)
                    : null;
                var scoreContext = new ProcessMutationContext(entity, worldState, goal: goal, definitionId: process.Id);
                var rawScore = precondResult.Passed
                    ? process.ScoreExpression.Evaluate(
                        new ExpressionContext(entity, targetEntity, worldState.Grid, scoreContext))
                    : 0f;

                // Per-entity, per-process recency decay: each time this entity fired
                // this process inside the RecencyWindow, score is multiplied by
                // RecencyDecayFactor. Structurally discourages loops on any verb.
                var recentFirings = entity.CountRecentActions(process.Name, tick - RecencyWindow);
                var score = recentFirings > 0
                    ? rawScore * MathF.Pow(RecencyDecayFactor, recentFirings)
                    : rawScore;

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

                AppendLineInvariant(
                    trace,
                    $"  candidate process={process.Name} target={providerName} precondition={(precondResult.Passed ? "pass" : "fail")} score={score:F3} viable={viable} diagnostic={precondResult.Diagnostic}");
            }

            if (candidates.Count == 0)
            {
                shouldReplan = shouldReplan || goal is not null;
                AppendLineInvariant(
                    trace,
                    $"  no viable candidates for goal={(goal is null ? "(none)" : goal.Id)} should_replan={shouldReplan}");
                continue;
            }

            var winner = WeightedRandomSelect(candidates);
            if (winner is null)
            {
                trace.AppendLine("  weighted selection produced no winner.");
                continue;
            }

            selectedGoalId = goalId;
            executedName = winner.Value.Definition.Name;
            AppendLineInvariant(
                trace,
                $"  selected process={executedName} score={winner.Value.Score:F3}");

            // Mark winner in report
            foreach (var c in reportCandidates)
            {
                if (c.ProcessName == executedName && c.GoalId == goalId)
                    c.WasSelected = true;
            }

            var message = ExecuteProcess(winner.Value.Definition, entity, worldState, goal);
            _messageBus.Publish(message);
            entity.RecordAction(tick, winner.Value.Definition.Name, winner.Value.Definition.Id);

            // Apply cooldowns
            if (winner.Value.Definition.Cooldown > 0)
            {
                var jitter = 1f + ((float)(_random.NextDouble() * 0.5 - 0.25));
                entity.ProcessCooldowns[winner.Value.Definition.Name] =
                    tick + Math.Max(1, (int)(winner.Value.Definition.Cooldown * jitter));
                AppendLineInvariant(
                    trace,
                    $"  process cooldown {winner.Value.Definition.Name} until tick {entity.ProcessCooldowns[winner.Value.Definition.Name]}");
            }

            if (winner.Value.Definition.EntityCooldown > 0)
            {
                var jitter = 1f + ((float)(_random.NextDouble() * 0.5 - 0.25));
                entity.EntityCooldownUntil =
                    tick + Math.Max(1, (int)(winner.Value.Definition.EntityCooldown * jitter));
                AppendLineInvariant(
                    trace,
                    $"  entity cooldown until tick {entity.EntityCooldownUntil}");
            }

            if (goal is not null && goal.IsSubGoal)
            {
                entity.GoalsGeneratedAt = 0;
                trace.AppendLine("  executed sub-goal; forcing replanning.");
            }

            executed = true;
        }

        if (shouldReplan)
        {
            entity.GoalsGeneratedAt = 0;
            trace.AppendLine("Plan invalidated for next tick.");
            if (!executed)
            {
                trace.AppendLine("No executable process found; forcing replanning next tick.");
            }
        }

        entity.LastEvaluation = CreateEvaluationReport(
            tick,
            sourceTick: tick,
            selectedGoalId: selectedGoalId,
            executedProcessName: executedName,
            isStale: false,
            statusNote: !executed && shouldReplan ? "no viable actionable process; replanning next tick" : null,
            reportCandidates);
        entity.EvaluationTrace = trace.ToString();
    }

    private EntityGoal? SelectGoal(List<EntityGoal> goals, HashSet<string>? exclude = null)
    {
        if (goals.Count == 0) return null;

        var totalUrgency = 0f;
        foreach (var goal in goals)
        {
            if (exclude is not null && exclude.Contains(goal.Id)) continue;
            totalUrgency += goal.Urgency;
        }

        if (totalUrgency <= 0f) return null;

        var roll = (float)(_random.NextDouble() * totalUrgency);
        var cumulative = 0f;
        EntityGoal? last = null;
        foreach (var goal in goals)
        {
            if (exclude is not null && exclude.Contains(goal.Id)) continue;
            last = goal;
            cumulative += goal.Urgency;
            if (roll <= cumulative) return goal;
        }

        return last;
    }

    private static bool ServesGoal(ProcessDefinition process, EntityGoal goal)
    {
        if (goal.PlannedProcessId is not null && process.Id != goal.PlannedProcessId)
            return false;

        return process.GoalEffects.Contains(goal.DesiredEffect);
    }

    private static ProcessExecutedMessage ExecuteProcess(
        ProcessDefinition definition, Entity entity, IWorldState worldState, EntityGoal? goal)
    {
        var context = new ProcessMutationContext(entity, worldState, goal: goal, definitionId: definition.Id);
        foreach (var mutation in definition.Mutations)
        {
            mutation.Execute(context);
        }

        var description = definition.DescriptionTemplate
            .Replace("{name}", entity.Name, StringComparison.Ordinal)
            .Replace("{process}", definition.Name, StringComparison.Ordinal);

        return new ProcessExecutedMessage(
            worldState.CurrentTick, entity.Id, entity.Kind, definition.Name, description);
    }

    private static DecisionExecutedMessage ExecuteDecision(
        ProcessDefinition definition, Entity entity, IWorldState worldState)
    {
        var context = new ProcessMutationContext(entity, worldState, definitionId: definition.Id);
        foreach (var mutation in definition.Mutations)
        {
            mutation.Execute(context);
        }

        var description = definition.DescriptionTemplate
            .Replace("{name}", entity.Name, StringComparison.Ordinal)
            .Replace("{process}", definition.Name, StringComparison.Ordinal);

        return new DecisionExecutedMessage(
            worldState.CurrentTick, entity.Id, entity.Kind, definition.Name,
            description, definition.EnablingCardId);
    }

    private (ProcessDefinition Definition, float Score)? WeightedRandomSelect(
        List<(ProcessDefinition Definition, float Score)> candidates)
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

    private bool GoalsRequireInvalidation(Entity entity, IWorldState worldState)
    {
        if (entity.Goals.Count == 0)
        {
            return false;
        }

        var processesById = _definitions.GetProcesses(entity.Kind).ToDictionary(p => p.Id, StringComparer.Ordinal);
        var goalIds = entity.Goals.Select(g => g.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var goal in entity.Goals)
        {
            var target = worldState.Graph.GetEntity(goal.TargetEntityId);
            if (target is null || target.Status != EntityStatus.Active)
            {
                return true;
            }

            if (goal.IsSubGoal && goal.ParentGoalId is not null && !goalIds.Contains(goal.ParentGoalId))
            {
                return true;
            }

            if (goal.PlannedProcessId is null)
            {
                continue;
            }

            if (!processesById.TryGetValue(goal.PlannedProcessId, out var process))
            {
                return true;
            }

            if (!process.GoalEffects.Contains(goal.DesiredEffect))
            {
                return true;
            }
        }

        return false;
    }

    private static EvaluationReport CreateStaleEvaluationReport(Entity entity, int tick, string statusNote)
    {
        var previous = entity.LastEvaluation;
        if (previous is null)
        {
            return CreateEvaluationReport(
                tick,
                sourceTick: tick,
                selectedGoalId: null,
                executedProcessName: null,
                isStale: true,
                statusNote: statusNote,
                []);
        }

        return CreateEvaluationReport(
            tick,
            sourceTick: GetSourceTick(previous),
            selectedGoalId: previous.SelectedGoalId,
            executedProcessName: previous.ExecutedProcessName,
            isStale: true,
            statusNote: statusNote,
            previous.Candidates.Select(CloneCandidate).ToList());
    }

    private static EvaluationReport CreateEvaluationReport(
        int tick,
        int sourceTick,
        string? selectedGoalId,
        string? executedProcessName,
        bool isStale,
        string? statusNote,
        IReadOnlyList<ProcessCandidate> candidates)
    {
        return new EvaluationReport
        {
            Tick = tick,
            SourceTick = sourceTick,
            SelectedGoalId = selectedGoalId,
            ExecutedProcessName = executedProcessName,
            IsStale = isStale,
            StatusNote = statusNote,
            Candidates = candidates,
        };
    }

    private static ProcessCandidate CloneCandidate(ProcessCandidate candidate)
    {
        return new ProcessCandidate
        {
            ProcessName = candidate.ProcessName,
            GoalId = candidate.GoalId,
            ProviderName = candidate.ProviderName,
            Score = candidate.Score,
            PreconditionPassed = candidate.PreconditionPassed,
            PreconditionDiagnostic = candidate.PreconditionDiagnostic,
            OnCooldown = candidate.OnCooldown,
            WasSelected = candidate.WasSelected,
        };
    }

    private static int GetSourceTick(EvaluationReport report)
    {
        return report.SourceTick == 0 && report.Tick != 0
            ? report.Tick
            : report.SourceTick;
    }

    private static string DescribeTarget(IWorldState worldState, EntityId targetEntityId)
    {
        return worldState.Graph.GetEntity(targetEntityId)?.Name ?? targetEntityId.ToString();
    }

    private static void AppendLineInvariant(StringBuilder trace, FormattableString text)
    {
        trace.AppendLine(FormattableString.Invariant(text));
    }
}
