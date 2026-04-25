namespace Engine.Behaviors;

using Engine.Domain.Config;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Messages;
using Engine.Processes;
using Engine.Wiring;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// Per-entity per-tick evaluation. Selects a goal, finds processes that serve it,
/// picks the best viable one, executes.
///
/// Goals are regenerated every N ticks or when a sub-goal fires. The evaluator
/// now adds a short-horizon locality layer: destinations are scored by the bundle
/// of nearby goals they unlock so movement can feed several follow-up actions
/// instead of behaving like a pure one-goal tax.
/// </summary>
public sealed class EntityEvaluator
{
    private readonly BehaviorDefinitionIndex _definitions;
    private readonly IReadOnlyDictionary<EntityKind, KindConfig> _kindConfigs;
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

    /// <summary>Whether LastEvaluation and process telemetry messages include per-candidate detail rows.</summary>
    public bool CaptureEvaluationCandidates { get; set; } = true;

    /// <summary>Whether goal-generation messages include every generated goal snapshot.</summary>
    public bool PublishGoalDetails { get; set; } = true;

    /// <summary>How many on-site actions an entity should try to realize after arrival.</summary>
    public int LocalityVisitActionBudget { get; set; } = 2;

    /// <summary>Multiplier applied to goals inside the selected locality when choosing the next goal.</summary>
    public float LocalityGoalSelectionBoost { get; set; } = 3f;

    /// <summary>Decay applied to lower-priority goals when scoring a locality bundle.</summary>
    public float LocalityAdditionalGoalWeight { get; set; } = 0.65f;

    /// <summary>Reward for each extra actionable goal available at one locality.</summary>
    public float LocalityDensityBonus { get; set; } = 0.15f;

    /// <summary>Reward for mixed-target bundles at one locality.</summary>
    public float LocalityDistinctTargetBonus { get; set; } = 0.1f;

    /// <summary>Reward for mixed-effect bundles at one locality.</summary>
    public float LocalityDistinctEffectBonus { get; set; } = 0.08f;

    /// <summary>Penalty applied per step between the actor and a candidate locality.</summary>
    public float LocalityDistancePenaltyPerStep { get; set; } = 0.05f;

    /// <summary>Bonus for staying with an already-selected locality when plans are close.</summary>
    public float LocalityCommitmentBonus { get; set; } = 0.2f;

    /// <summary>Bonus when the actor is already at the locality.</summary>
    public float LocalityAlreadyThereBonus { get; set; } = 0.15f;

    /// <summary>Improvement required before abandoning an existing locality commitment.</summary>
    public float LocalitySwitchHysteresisRatio { get; set; } = 0.2f;

    public EntityEvaluator(BehaviorDefinitionIndex definitions, MessageBus messageBus,
        Random? random = null,
        GoalGenerator? goalGenerator = null,
        IReadOnlyDictionary<EntityKind, KindConfig>? kindConfigs = null)
    {
        _definitions = definitions;
        _kindConfigs = kindConfigs ?? new Dictionary<EntityKind, KindConfig>();
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
                var context = new ProcessMutationContext(
                    entity,
                    worldState,
                    definitionId: decision.RuntimeId,
                    capturePreconditionDiagnostics: false);
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
                    entity.RecordAction(worldState.CurrentTick, winner.Value.Definition.RuntimeId);
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

        if (worldState is ICachedWorldState cachedWorldState)
        {
            foreach (var entity in cachedWorldState.QueryCache.ActiveEntities())
            {
                Evaluate(entity, worldState);
            }

            return;
        }

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
        _messageBus.Publish(new GoalGenerationMessage(
            worldState.CurrentTick,
            entity.Id,
            entity.Kind,
            entity.Goals.Count,
            entity.Goals.Count(goal => goal.IsActionable),
            PublishGoalDetails
                ? entity.Goals.Select(goal => new GoalSnapshot(
                    goal.Id,
                    goal.DesiredEffect,
                    goal.TargetEntityId,
                    goal.Urgency,
                    goal.PlannedProcessId,
                    goal.ParentGoalId,
                    goal.IsActionable,
                    goal.Depth)).ToList()
                : []));
    }

    private void EvaluateProcesses(Entity entity, IWorldState worldState)
    {
        var tick = worldState.CurrentTick;
        var reportCandidates = CaptureEvaluationCandidates ? new List<ProcessCandidate>() : null;
        var evaluatedCandidateCount = 0;
        var capturePreconditionDiagnostics = false;
        var trace = CreateTraceBuilder();
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
            PublishEvaluation(entity, entity.LastEvaluation);
            return;
        }

        var processes = _definitions.GetProcesses(entity.Kind);
        GoalRuntimeId? selectedGoalId = null;
        ProcessRuntimeId? executedProcessId = null;
        EntityGoal? executedGoal = null;
        ProcessDefinition? executedProcess = null;
        AppendLineInvariant(trace, $"registered processes={processes.Count}");

        if (_goalGenerator is not null && entity.Goals.Count == 0)
        {
            entity.LastEvaluation = CreateEvaluationReport(
                tick,
                sourceTick: tick,
                selectedGoalId: null,
                executedProcessId: null,
                isStale: false,
                statusNote: "no planner goals",
                selectedLocality: null,
                reportCandidates,
                localityOpportunities: [],
                evaluatedCandidateCount);
            AppendTraceLine(trace, "No planner goals present; idling.");
            PublishEvaluation(entity, entity.LastEvaluation);
            return;
        }

        var actionableGoals = new List<EntityGoal>(entity.Goals.Count);
        foreach (var candidateGoal in entity.Goals)
        {
            if (candidateGoal.IsActionable)
            {
                actionableGoals.Add(candidateGoal);
            }
        }

        if (entity.Goals.Count > 0 && actionableGoals.Count == 0)
        {
            entity.GoalsGeneratedAt = 0;
            entity.LastEvaluation = CreateEvaluationReport(
                tick,
                sourceTick: tick,
                selectedGoalId: null,
                executedProcessId: null,
                isStale: false,
                statusNote: "no actionable goals; replanning next tick",
                selectedLocality: null,
                reportCandidates,
                localityOpportunities: [],
                evaluatedCandidateCount);
            AppendTraceLine(trace, "No actionable goals; forcing replanning.");
            PublishEvaluation(entity, entity.LastEvaluation);
            return;
        }

        if (actionableGoals.Count == 0)
        {
            AppendTraceLine(trace, "No actionable goals present; evaluating unguided processes.");
        }
        else
        {
            AppendTraceLine(trace, "Actionable Goals:");
            foreach (var goal in actionableGoals.OrderByDescending(g => g.Urgency))
            {
                AppendLineInvariant(
                    trace,
                    $"  goal={goal.Id} process={DescribeProcessId(goal.PlannedProcessId)} target={DescribeTarget(worldState, goal.TargetEntityId)} effect={goal.DesiredEffect} urgency={goal.Urgency:F3} depth={goal.Depth}");
            }
        }

        var localityPlanning = actionableGoals.Count > 0
            ? SelectLocalityBundle(entity, worldState, actionableGoals, tick, trace)
            : null;
        if (localityPlanning?.Selected is not null)
        {
            AppendLineInvariant(
                trace,
                $"Selected locality visit={entity.ActiveVisitIntent?.VisitId.Value ?? 0} key={localityPlanning.Selected.LocalityKey} plane={localityPlanning.Selected.Plane} anchor={localityPlanning.Selected.AnchorName} score={localityPlanning.Selected.BundleScore:F3} goals={localityPlanning.Selected.GoalCount} distance={localityPlanning.Selected.Distance} reason={entity.ActiveVisitIntent?.SelectionReason ?? LocalitySelectionReason.None}");
        }

        // Evaluate all goals and all serving processes, recording everything
        var triedGoals = new HashSet<GoalRuntimeId>();
        var evaluatedPairs = new HashSet<(ProcessRuntimeId ProcessId, EntityId Target)>();
        var executed = false;
        var shouldReplan = false;
        var goalUrgencyMultipliers = localityPlanning?.GoalUrgencyMultipliers;

        for (var attempts = 0; !executed && attempts < Math.Max(1, actionableGoals.Count); attempts++)
        {
            var goal = actionableGoals.Count > 0
                ? SelectGoal(actionableGoals, triedGoals, goalUrgencyMultipliers)
                : null;
            if (goal is not null) triedGoals.Add(goal.Id);

            var goalId = goal?.Id;
            var providerId = goal?.TargetEntityId ?? entity.Id;
            AppendLineInvariant(
                trace,
                $"Attempt {attempts + 1}: goal={(goal is null ? GoalRuntimeId.None : goal.Id)} target={providerId} effect={(goal is null ? "none" : goal.DesiredEffect.ToString())}");

            var candidates = new List<(ProcessDefinition Definition, float Score)>();

            void EvaluateCandidate(ProcessDefinition process)
            {
                // Skip process-provider pairs already evaluated in a prior goal attempt.
                // Two goals with the same Provider and TargetAttribute produce identical
                // precondition/score results, so re-evaluating is wasteful and creates
                // duplicate entries in the report.
                if (goal is not null && !evaluatedPairs.Add((process.RuntimeId, goal.TargetEntityId)))
                {
                    AppendLineInvariant(
                        trace,
                        $"  skip process={process.Name} target={providerId}: already evaluated for this target");
                    return;
                }

                var onCooldown = entity.ProcessCooldowns.TryGetValue(process.RuntimeId, out var cdUntil)
                    && tick < cdUntil;
                ProcessMutationContext? processContext = null;

                // Evaluate precondition first (reveals real failure reason)
                PreconditionResult precondResult;
                if (onCooldown)
                {
                    precondResult = PreconditionResult.Fail($"cooldown until tick {cdUntil}");
                }
                else
                {
                    processContext = new ProcessMutationContext(
                        entity,
                        worldState,
                        goal: goal,
                        definitionId: process.RuntimeId,
                        capturePreconditionDiagnostics: capturePreconditionDiagnostics);
                    precondResult = process.Precondition.Evaluate(processContext);
                }

                // Score only matters if precondition passed
                var rawScore = precondResult.Passed
                    ? process.ScoreExpression.Evaluate(
                        new ExpressionContext(
                            entity,
                            goal is not null ? GetEntity(worldState, goal.TargetEntityId) : null,
                            worldState.Grid,
                            processContext ?? new ProcessMutationContext(
                                entity,
                                worldState,
                                goal: goal,
                                definitionId: process.RuntimeId,
                                capturePreconditionDiagnostics: capturePreconditionDiagnostics)))
                    : 0f;

                // Per-entity, per-process recency decay: each time this entity fired
                // this process inside the RecencyWindow, score is multiplied by
                // RecencyDecayFactor. Structurally discourages loops on any verb.
                var recentFirings = entity.CountRecentActions(process.RuntimeId, tick - RecencyWindow);
                var score = recentFirings > 0
                    ? rawScore * MathF.Pow(RecencyDecayFactor, recentFirings)
                    : rawScore;

                var viable = precondResult.Passed && score >= MinimumViableScore;
                if (viable) candidates.Add((process, score));

                evaluatedCandidateCount++;
                if (reportCandidates is not null)
                {
                    reportCandidates.Add(new ProcessCandidate
                    {
                        ProcessId = process.RuntimeId,
                        GoalId = goalId,
                        ProviderId = providerId,
                        Score = score,
                        PreconditionPassed = precondResult.Passed,
                        PreconditionDiagnostic = precondResult.Diagnostic,
                        OnCooldown = onCooldown,
                        WasSelected = false, // Updated below if selected
                    });
                }

                AppendLineInvariant(
                    trace,
                    $"  candidate process={process.Name} target={providerId} precondition={(precondResult.Passed ? "pass" : "fail")} score={score:F3} viable={viable} diagnostic={precondResult.Diagnostic}");
            }

            if (goal?.PlannedProcessId is { } plannedProcessId)
            {
                if (_definitions.TryGetProcess(entity.Kind, plannedProcessId, out var plannedProcess) &&
                    plannedProcess is not null &&
                    ServesGoal(plannedProcess, goal))
                {
                    EvaluateCandidate(plannedProcess);
                }
                else
                {
                    AppendLineInvariant(
                        trace,
                        $"  planned process={plannedProcessId} unavailable for goal={goal.Id}");
                }
            }
            else
            {
                foreach (var process in processes)
                {
                    if (goal is not null && !ServesGoal(process, goal)) continue;
                    EvaluateCandidate(process);
                }
            }

            if (candidates.Count == 0)
            {
                shouldReplan = shouldReplan || goal is not null;
                AppendLineInvariant(
                    trace,
                    $"  no viable candidates for goal={(goal is null ? "(none)" : goal.Id.Value.ToString(CultureInfo.InvariantCulture))} should_replan={shouldReplan}");
                continue;
            }

            var winner = WeightedRandomSelect(candidates);
            if (winner is null)
            {
                AppendTraceLine(trace, "  weighted selection produced no winner.");
                continue;
            }

            selectedGoalId = goalId;
            executedProcessId = winner.Value.Definition.RuntimeId;
            executedGoal = goal;
            executedProcess = winner.Value.Definition;
            AppendLineInvariant(
                trace,
                $"  selected process={winner.Value.Definition.Name} score={winner.Value.Score:F3}");

            // Mark winner in report
            if (reportCandidates is not null)
            {
                foreach (var c in reportCandidates)
                {
                    if (c.ProcessId == executedProcessId && c.GoalId == goalId)
                        c.WasSelected = true;
                }
            }

            var message = ExecuteProcess(winner.Value.Definition, entity, worldState, goal);
            _messageBus.Publish(message);
            entity.RecordAction(tick, winner.Value.Definition.RuntimeId);

            // Apply cooldowns
            if (winner.Value.Definition.Cooldown > 0)
            {
                var jitter = 1f + ((float)(_random.NextDouble() * 0.5 - 0.25));
                entity.ProcessCooldowns[winner.Value.Definition.RuntimeId] =
                    tick + Math.Max(1, (int)(winner.Value.Definition.Cooldown * jitter));
                AppendLineInvariant(
                    trace,
                    $"  process cooldown {winner.Value.Definition.Name} until tick {entity.ProcessCooldowns[winner.Value.Definition.RuntimeId]}");
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
                AppendTraceLine(trace, "  executed sub-goal; forcing replanning.");
            }

            executed = true;
        }

        var localitySelection = FinalizeLocalitySelection(
            entity,
            localityPlanning,
            executedGoal,
            executedProcess,
            worldState,
            trace);

        if (shouldReplan)
        {
            entity.GoalsGeneratedAt = 0;
            AppendTraceLine(trace, "Plan invalidated for next tick.");
            if (!executed)
            {
                AppendTraceLine(trace, "No executable process found; forcing replanning next tick.");
            }
        }

        entity.LastEvaluation = CreateEvaluationReport(
            tick,
            sourceTick: tick,
            selectedGoalId: selectedGoalId,
            executedProcessId: executedProcessId,
            isStale: false,
            statusNote: !executed && shouldReplan ? "no viable actionable process; replanning next tick" : null,
            selectedLocality: localitySelection,
            reportCandidates,
            localityOpportunities: localityPlanning?.Opportunities ?? [],
            evaluatedCandidateCount);
        PublishEvaluation(entity, entity.LastEvaluation);
    }

    private EntityGoal? SelectGoal(
        List<EntityGoal> goals,
        HashSet<GoalRuntimeId>? exclude = null,
        IReadOnlyDictionary<GoalRuntimeId, float>? urgencyMultipliers = null)
    {
        if (goals.Count == 0) return null;

        var totalUrgency = 0f;
        foreach (var goal in goals)
        {
            if (exclude is not null && exclude.Contains(goal.Id)) continue;
            totalUrgency += goal.Urgency * GoalUrgencyMultiplier(goal, urgencyMultipliers);
        }

        if (totalUrgency <= 0f) return null;

        var roll = (float)(_random.NextDouble() * totalUrgency);
        var cumulative = 0f;
        EntityGoal? last = null;
        foreach (var goal in goals)
        {
            if (exclude is not null && exclude.Contains(goal.Id)) continue;
            last = goal;
            cumulative += goal.Urgency * GoalUrgencyMultiplier(goal, urgencyMultipliers);
            if (roll <= cumulative) return goal;
        }

        return last;
    }

    private static float GoalUrgencyMultiplier(
        EntityGoal goal,
        IReadOnlyDictionary<GoalRuntimeId, float>? urgencyMultipliers)
    {
        if (urgencyMultipliers is null)
        {
            return 1f;
        }

        return urgencyMultipliers.TryGetValue(goal.Id, out var multiplier)
            ? multiplier
            : 1f;
    }

    private static bool ServesGoal(ProcessDefinition process, EntityGoal goal)
    {
        if (goal.PlannedProcessId is not null && process.RuntimeId != goal.PlannedProcessId)
            return false;

        return process.GoalEffects.Contains(goal.DesiredEffect);
    }

    private ProcessExecutedMessage ExecuteProcess(
        ProcessDefinition definition, Entity entity, IWorldState worldState, EntityGoal? goal)
    {
        var context = new ProcessMutationContext(
            entity,
            worldState,
            goal: goal,
            definitionId: definition.RuntimeId,
            messageBus: _messageBus);
        foreach (var mutation in definition.Mutations)
        {
            mutation.Execute(context);
        }

        return new ProcessExecutedMessage(
            worldState.CurrentTick, entity.Id, entity.Kind, definition.RuntimeId);
    }

    private DecisionExecutedMessage ExecuteDecision(
        ProcessDefinition definition, Entity entity, IWorldState worldState)
    {
        var context = new ProcessMutationContext(
            entity,
            worldState,
            definitionId: definition.RuntimeId,
            messageBus: _messageBus);
        foreach (var mutation in definition.Mutations)
        {
            mutation.Execute(context);
        }

        return new DecisionExecutedMessage(
            worldState.CurrentTick, entity.Id, entity.Kind, definition.RuntimeId,
            definition.EnablingCardRuntimeId);
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

    private LocalityPlanningResult? SelectLocalityBundle(
        Entity entity,
        IWorldState worldState,
        List<EntityGoal> actionableGoals,
        int tick,
        StringBuilder? trace)
    {
        if (actionableGoals.Count == 0)
        {
            return null;
        }

        if (entity.ActiveVisitIntent is { ActionsRemaining: <= 0 } expiredIntent)
        {
            AppendLineInvariant(
                trace,
                $"Locality visit expired visit={expiredIntent.VisitId} key={expiredIntent.LocalityKey} realized={expiredIntent.RealizedActions}");
            entity.ActiveVisitIntent = null;
        }

        var plane = GetPrimaryPlane(entity.Kind);
        var candidates = actionableGoals
            .Select(goal => CreateLocalityContribution(entity, goal, worldState, plane))
            .Where(contribution => contribution is not null)
            .Cast<LocalityContribution>()
            .GroupBy(contribution => contribution.LocalityKey)
            .Select(group => BuildLocalityCandidate(entity, group.Key, plane, group.ToList(), worldState))
            .OrderByDescending(candidate => candidate.BundleScore)
            .ThenBy(candidate => candidate.Distance)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var best = candidates[0];
        var committed = entity.ActiveVisitIntent is null
            ? null
            : candidates.FirstOrDefault(candidate =>
                candidate.LocalityKey == entity.ActiveVisitIntent.LocalityKey
                && candidate.Plane == entity.ActiveVisitIntent.Plane);
        var selected = best;
        var selectionReason = LocalitySelectionReason.BestBundle;
        if (committed is not null)
        {
            var requiredImprovement = committed.BundleScore * (1f + LocalitySwitchHysteresisRatio);
            if (best != committed && best.BundleScore <= requiredImprovement)
            {
                selected = committed;
                selectionReason = LocalitySelectionReason.KeptCommitment;
            }
            else if (best == committed)
            {
                selectionReason = LocalitySelectionReason.ContinuedCommitment;
            }
            else
            {
                selectionReason = LocalitySelectionReason.SwitchedCommitment;
            }
        }

        EnsureVisitIntent(entity, selected, tick, selectionReason);

        var multipliers = new Dictionary<GoalRuntimeId, float>();
        foreach (var goalId in selected.GoalIds)
        {
            multipliers[goalId] = LocalityGoalSelectionBoost;
        }

        var opportunities = candidates.Select(candidate => new LocalityOpportunityReport
        {
            LocalityKey = candidate.LocalityKey,
            Plane = candidate.Plane,
            Q = candidate.Q,
            R = candidate.R,
            S = candidate.S,
            AnchorEntityId = candidate.AnchorEntityId.Value,
            AnchorName = candidate.AnchorName,
            BundleScore = candidate.BundleScore,
            GoalCount = candidate.GoalCount,
            DistinctTargetCount = candidate.DistinctTargetCount,
            DistinctEffectCount = candidate.DistinctEffectCount,
            Distance = candidate.Distance,
            IsCurrentLocality = candidate.IsCurrentLocality,
            IsCommitted = committed is not null && candidate.LocalityKey == committed.LocalityKey,
            WasSelected = candidate.LocalityKey == selected.LocalityKey,
            SelectionReason = candidate.LocalityKey == selected.LocalityKey
                ? selectionReason
                : LocalitySelectionReason.None,
        }).ToList();

        AppendTraceLine(trace, "Locality Opportunities:");
        foreach (var opportunity in opportunities)
        {
            AppendLineInvariant(
                trace,
                $"  locality={opportunity.LocalityKey} plane={opportunity.Plane} anchor={opportunity.AnchorName} score={opportunity.BundleScore:F3} goals={opportunity.GoalCount} targets={opportunity.DistinctTargetCount} effects={opportunity.DistinctEffectCount} distance={opportunity.Distance} current={opportunity.IsCurrentLocality} committed={opportunity.IsCommitted} selected={opportunity.WasSelected}");
        }

        return new LocalityPlanningResult(selected, opportunities, multipliers);
    }

    private LocalityContribution? CreateLocalityContribution(
        Entity entity,
        EntityGoal goal,
        IWorldState worldState,
        PrimaryPlane plane)
    {
        var target = GetEntity(worldState, goal.TargetEntityId);
        if (target is null || target.Status != EntityStatus.Active)
        {
            return null;
        }

        if (plane == PrimaryPlane.Physical)
        {
            return new LocalityContribution(
                BuildLocalityKey(plane, target.Coordinates.Q, target.Coordinates.R, null),
                plane,
                target.Coordinates.Q,
                target.Coordinates.R,
                null,
                target.Id,
                target.Name,
                goal);
        }

        return new LocalityContribution(
            BuildLocalityKey(plane, target.SemanticPosition.Q, target.SemanticPosition.R, target.SemanticPosition.S),
            plane,
            target.SemanticPosition.Q,
            target.SemanticPosition.R,
            target.SemanticPosition.S,
            target.Id,
            target.Name,
            goal);
    }

    private LocalityCandidate BuildLocalityCandidate(
        Entity entity,
        LocalityRuntimeKey localityKey,
        PrimaryPlane plane,
        IReadOnlyList<LocalityContribution> contributions,
        IWorldState worldState)
    {
        var orderedUrgencies = contributions
            .Select(contribution => contribution.Goal.Urgency)
            .OrderByDescending(value => value)
            .ToList();
        var weightedUrgency = 0f;
        for (var i = 0; i < orderedUrgencies.Count; i++)
        {
            weightedUrgency += orderedUrgencies[i] * MathF.Pow(LocalityAdditionalGoalWeight, i);
        }

        var distinctTargetCount = contributions
            .Select(contribution => contribution.AnchorEntityId)
            .Distinct()
            .Count();
        var distinctEffectCount = contributions
            .Select(contribution => contribution.Goal.DesiredEffect)
            .Distinct()
            .Count();

        var q = contributions[0].Q;
        var r = contributions[0].R;
        var s = contributions[0].S;
        var distance = plane == PrimaryPlane.Physical
            ? HexCoord.Distance(
                new HexCoord(entity.Coordinates.Q, entity.Coordinates.R),
                new HexCoord(q, r))
            : SemanticHexCoord.Distance(
                entity.SemanticPosition,
                new SemanticHexCoord(q, r));
        var isCurrentLocality = distance == 0;
        var isCommitted = entity.ActiveVisitIntent is not null
            && entity.ActiveVisitIntent.LocalityKey == localityKey
            && entity.ActiveVisitIntent.Plane == plane;

        var score = weightedUrgency
            + Math.Max(0, contributions.Count - 1) * LocalityDensityBonus
            + Math.Max(0, distinctTargetCount - 1) * LocalityDistinctTargetBonus
            + Math.Max(0, distinctEffectCount - 1) * LocalityDistinctEffectBonus
            + (isCommitted ? LocalityCommitmentBonus : 0f)
            + (isCurrentLocality ? LocalityAlreadyThereBonus : 0f)
            - distance * LocalityDistancePenaltyPerStep;

        return new LocalityCandidate(
            localityKey,
            plane,
            q,
            r,
            s,
            contributions[0].AnchorEntityId,
            contributions[0].AnchorName,
            MathF.Max(0f, score),
            contributions.Count,
            distinctTargetCount,
            distinctEffectCount,
            distance,
            isCurrentLocality,
            contributions.Select(contribution => contribution.Goal.Id).ToHashSet());
    }

    private void EnsureVisitIntent(Entity entity, LocalityCandidate selected, int tick, LocalitySelectionReason selectionReason)
    {
        if (entity.ActiveVisitIntent is null ||
            entity.ActiveVisitIntent.LocalityKey != selected.LocalityKey ||
            entity.ActiveVisitIntent.Plane != selected.Plane)
        {
            entity.ActiveVisitIntent = new LocalityVisitIntent
            {
                LocalityKey = selected.LocalityKey,
                Plane = selected.Plane,
                Q = selected.Q,
                R = selected.R,
                S = selected.S,
                AnchorEntityId = selected.AnchorEntityId,
                CreatedTick = tick,
                SelectionReason = selectionReason,
                BundleScore = selected.BundleScore,
                GoalCount = selected.GoalCount,
                DistinctTargetCount = selected.DistinctTargetCount,
                DistinctEffectCount = selected.DistinctEffectCount,
                DistanceAtSelection = selected.Distance,
                Arrived = selected.IsCurrentLocality,
                ActionsRemaining = LocalityVisitActionBudget,
                RealizedActions = 0,
            };
            return;
        }

        entity.ActiveVisitIntent.SelectionReason = selectionReason;
        entity.ActiveVisitIntent.BundleScore = selected.BundleScore;
        entity.ActiveVisitIntent.GoalCount = selected.GoalCount;
        entity.ActiveVisitIntent.DistinctTargetCount = selected.DistinctTargetCount;
        entity.ActiveVisitIntent.DistinctEffectCount = selected.DistinctEffectCount;
        entity.ActiveVisitIntent.DistanceAtSelection = selected.Distance;
        entity.ActiveVisitIntent.Arrived = entity.ActiveVisitIntent.Arrived || selected.IsCurrentLocality;
    }

    private LocalitySelectionReport? FinalizeLocalitySelection(
        Entity entity,
        LocalityPlanningResult? planning,
        EntityGoal? executedGoal,
        ProcessDefinition? executedProcess,
        IWorldState worldState,
        StringBuilder? trace)
    {
        if (planning?.Selected is null || entity.ActiveVisitIntent is null)
        {
            return null;
        }

        var intent = entity.ActiveVisitIntent;
        var selected = planning.Selected;
        intent.Arrived = intent.Arrived || IsAtLocality(entity, selected);

        if (executedGoal is not null &&
            planning.GoalUrgencyMultipliers.ContainsKey(executedGoal.Id) &&
            intent.Arrived &&
            executedProcess is not null &&
            !IsMovementProcess(executedProcess))
        {
            intent.RealizedActions++;
            intent.ActionsRemaining = Math.Max(0, intent.ActionsRemaining - 1);
            AppendLineInvariant(
                trace,
                $"Locality action realized visit={intent.VisitId} process={executedProcess.Name} remaining={intent.ActionsRemaining} realized={intent.RealizedActions}");
        }

        return new LocalitySelectionReport
        {
            VisitId = intent.VisitId,
            LocalityKey = selected.LocalityKey,
            Plane = selected.Plane,
            Q = selected.Q,
            R = selected.R,
            S = selected.S,
            AnchorEntityId = selected.AnchorEntityId.Value,
            AnchorName = selected.AnchorName,
            BundleScore = selected.BundleScore,
            GoalCount = selected.GoalCount,
            DistinctTargetCount = selected.DistinctTargetCount,
            DistinctEffectCount = selected.DistinctEffectCount,
            Distance = selected.Distance,
            IsCurrentLocality = IsAtLocality(entity, selected),
            IsCommitted = true,
            Arrived = intent.Arrived,
            ActionsRemaining = intent.ActionsRemaining,
            RealizedActions = intent.RealizedActions,
            SelectionReason = intent.SelectionReason,
        };
    }

    private bool IsAtLocality(Entity entity, LocalityCandidate candidate)
    {
        if (candidate.Plane == PrimaryPlane.Physical)
        {
            return entity.Coordinates.Q == candidate.Q && entity.Coordinates.R == candidate.R;
        }

        return entity.SemanticPosition.Q == candidate.Q
            && entity.SemanticPosition.R == candidate.R
            && entity.SemanticPosition.S == (candidate.S ?? entity.SemanticPosition.S);
    }

    private static bool IsMovementProcess(ProcessDefinition definition)
    {
        return definition.Mutations.Any(mutation =>
            mutation is MoveTowardTargetMutation or MoveTowardTargetOnSemanticPlaneMutation);
    }

    private PrimaryPlane GetPrimaryPlane(EntityKind kind)
    {
        return _kindConfigs.TryGetValue(kind, out var kindConfig)
            ? kindConfig.PrimaryPlane
            : PrimaryPlane.Physical;
    }

    private static LocalityRuntimeKey BuildLocalityKey(PrimaryPlane plane, int q, int r, int? s) =>
        LocalityRuntimeKey.Create(plane, q, r, s);

    private void PublishEvaluation(Entity entity, EvaluationReport report)
    {
        _messageBus.Publish(new ProcessEvaluationMessage(
            report.Tick,
            entity.Id,
            entity.Kind,
            report.SelectedGoalId,
            report.ExecutedProcessId,
            report.IsStale,
            report.StatusNote,
            report.Candidates.Select(candidate => new ProcessCandidateSnapshot(
                candidate.ProcessId,
                candidate.GoalId,
                candidate.ProviderId,
                candidate.Score,
                candidate.PreconditionPassed,
                candidate.PreconditionDiagnostic,
                candidate.OnCooldown,
                candidate.WasSelected)).ToList(),
            report.SelectedLocality is null
                ? null
                : new LocalitySelectionSnapshot(
                    report.SelectedLocality.VisitId,
                    report.SelectedLocality.LocalityKey,
                    report.SelectedLocality.Plane,
                    report.SelectedLocality.Q,
                    report.SelectedLocality.R,
                    report.SelectedLocality.S,
                    new EntityId(report.SelectedLocality.AnchorEntityId),
                    report.SelectedLocality.AnchorName,
                    report.SelectedLocality.BundleScore,
                    report.SelectedLocality.GoalCount,
                    report.SelectedLocality.DistinctTargetCount,
                    report.SelectedLocality.DistinctEffectCount,
                    report.SelectedLocality.Distance,
                    report.SelectedLocality.IsCurrentLocality,
                    report.SelectedLocality.IsCommitted,
                    report.SelectedLocality.Arrived,
                    report.SelectedLocality.ActionsRemaining,
                    report.SelectedLocality.RealizedActions,
                    report.SelectedLocality.SelectionReason),
            report.LocalityOpportunities.Select(locality => new LocalityOpportunitySnapshot(
                locality.LocalityKey,
                locality.Plane,
                locality.Q,
                locality.R,
                locality.S,
                new EntityId(locality.AnchorEntityId),
                locality.AnchorName,
                locality.BundleScore,
                locality.GoalCount,
                locality.DistinctTargetCount,
                locality.DistinctEffectCount,
                locality.Distance,
                locality.IsCurrentLocality,
                locality.IsCommitted,
                locality.WasSelected,
                locality.SelectionReason)).ToList(),
            report.CandidateCount));
    }

    private bool GoalsRequireInvalidation(Entity entity, IWorldState worldState)
    {
        if (entity.Goals.Count == 0)
        {
            return false;
        }

        var goalIds = entity.Goals.Select(g => g.Id).ToHashSet();

        foreach (var goal in entity.Goals)
        {
            var target = worldState.Graph.GetEntity(goal.TargetEntityId);
            if (target is null || target.Status != EntityStatus.Active)
            {
                return true;
            }

            if (goal.IsSubGoal && goal.ParentGoalId is not null && !goalIds.Contains(goal.ParentGoalId.Value))
            {
                return true;
            }

            if (goal.PlannedProcessId is null)
            {
                continue;
            }

            if (!_definitions.TryGetProcess(entity.Kind, goal.PlannedProcessId.Value, out var process))
            {
                return true;
            }

            if (process is null || !process.GoalEffects.Contains(goal.DesiredEffect))
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
                executedProcessId: null,
                isStale: true,
                statusNote: statusNote,
                selectedLocality: null,
                []);
        }

        return CreateEvaluationReport(
            tick,
            sourceTick: GetSourceTick(previous),
            selectedGoalId: previous.SelectedGoalId,
            executedProcessId: previous.ExecutedProcessId,
            isStale: true,
            statusNote: statusNote,
            selectedLocality: CloneLocality(previous.SelectedLocality),
            previous.Candidates.Select(CloneCandidate).ToList(),
            previous.LocalityOpportunities.Select(CloneLocalityOpportunity).ToList(),
            previous.CandidateCount);
    }

    private static EvaluationReport CreateEvaluationReport(
        int tick,
        int sourceTick,
        GoalRuntimeId? selectedGoalId,
        ProcessRuntimeId? executedProcessId,
        bool isStale,
        string? statusNote,
        LocalitySelectionReport? selectedLocality,
        IReadOnlyList<ProcessCandidate>? candidates,
        IReadOnlyList<LocalityOpportunityReport>? localityOpportunities = null,
        int? candidateCount = null)
    {
        var capturedCandidates = candidates ?? [];
        var capturedLocalities = localityOpportunities ?? [];
        return new EvaluationReport
        {
            Tick = tick,
            SourceTick = sourceTick,
            SelectedGoalId = selectedGoalId,
            ExecutedProcessId = executedProcessId,
            IsStale = isStale,
            StatusNote = statusNote,
            SelectedLocality = selectedLocality,
            LocalityOpportunities = capturedLocalities,
            CandidateCount = candidateCount ?? capturedCandidates.Count,
            Candidates = capturedCandidates,
        };
    }

    private static ProcessCandidate CloneCandidate(ProcessCandidate candidate)
    {
        return new ProcessCandidate
        {
            ProcessId = candidate.ProcessId,
            GoalId = candidate.GoalId,
            ProviderId = candidate.ProviderId,
            Score = candidate.Score,
            PreconditionPassed = candidate.PreconditionPassed,
            PreconditionDiagnostic = candidate.PreconditionDiagnostic,
            OnCooldown = candidate.OnCooldown,
            WasSelected = candidate.WasSelected,
        };
    }

    private static LocalitySelectionReport? CloneLocality(LocalitySelectionReport? locality)
    {
        if (locality is null)
        {
            return null;
        }

        return new LocalitySelectionReport
        {
            VisitId = locality.VisitId,
            LocalityKey = locality.LocalityKey,
            Plane = locality.Plane,
            Q = locality.Q,
            R = locality.R,
            S = locality.S,
            AnchorEntityId = locality.AnchorEntityId,
            AnchorName = locality.AnchorName,
            BundleScore = locality.BundleScore,
            GoalCount = locality.GoalCount,
            DistinctTargetCount = locality.DistinctTargetCount,
            DistinctEffectCount = locality.DistinctEffectCount,
            Distance = locality.Distance,
            IsCurrentLocality = locality.IsCurrentLocality,
            IsCommitted = locality.IsCommitted,
            Arrived = locality.Arrived,
            ActionsRemaining = locality.ActionsRemaining,
            RealizedActions = locality.RealizedActions,
            SelectionReason = locality.SelectionReason,
        };
    }

    private static LocalityOpportunityReport CloneLocalityOpportunity(LocalityOpportunityReport locality)
    {
        return new LocalityOpportunityReport
        {
            LocalityKey = locality.LocalityKey,
            Plane = locality.Plane,
            Q = locality.Q,
            R = locality.R,
            S = locality.S,
            AnchorEntityId = locality.AnchorEntityId,
            AnchorName = locality.AnchorName,
            BundleScore = locality.BundleScore,
            GoalCount = locality.GoalCount,
            DistinctTargetCount = locality.DistinctTargetCount,
            DistinctEffectCount = locality.DistinctEffectCount,
            Distance = locality.Distance,
            IsCurrentLocality = locality.IsCurrentLocality,
            IsCommitted = locality.IsCommitted,
            WasSelected = locality.WasSelected,
            SelectionReason = locality.SelectionReason,
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
        return GetEntity(worldState, targetEntityId)?.Name ?? targetEntityId.ToString();
    }

    private static string DescribeProcessId(ProcessRuntimeId? processId)
    {
        return processId is { } value && !value.IsNone
            ? value.Value.ToString(CultureInfo.InvariantCulture)
            : "(any)";
    }

    private static StringBuilder? CreateTraceBuilder() => null;

    private static Entity? GetEntity(IWorldState worldState, EntityId entityId) =>
        worldState is ICachedWorldState cachedWorldState
            ? cachedWorldState.QueryCache.GetEntity(entityId)
            : worldState.Graph.GetEntity(entityId);

    private sealed record LocalityContribution(
        LocalityRuntimeKey LocalityKey,
        PrimaryPlane Plane,
        int Q,
        int R,
        int? S,
        EntityId AnchorEntityId,
        string AnchorName,
        EntityGoal Goal);

    private sealed record LocalityCandidate(
        LocalityRuntimeKey LocalityKey,
        PrimaryPlane Plane,
        int Q,
        int R,
        int? S,
        EntityId AnchorEntityId,
        string AnchorName,
        float BundleScore,
        int GoalCount,
        int DistinctTargetCount,
        int DistinctEffectCount,
        int Distance,
        bool IsCurrentLocality,
        HashSet<GoalRuntimeId> GoalIds);

    private sealed record LocalityPlanningResult(
        LocalityCandidate Selected,
        IReadOnlyList<LocalityOpportunityReport> Opportunities,
        IReadOnlyDictionary<GoalRuntimeId, float> GoalUrgencyMultipliers);

    private static void AppendTraceLine(StringBuilder? trace, string text)
    {
        trace?.AppendLine(text);
    }

    private static void AppendTraceLine(
        StringBuilder? trace,
        [InterpolatedStringHandlerArgument(nameof(trace))] EvaluationTraceInterpolatedStringHandler text)
    {
        trace?.AppendLine();
    }

    private static void AppendLineInvariant(
        StringBuilder? trace,
        [InterpolatedStringHandlerArgument(nameof(trace))] EvaluationTraceInterpolatedStringHandler text)
    {
        trace?.AppendLine();
    }

    [InterpolatedStringHandler]
    private ref struct EvaluationTraceInterpolatedStringHandler
    {
        private readonly StringBuilder? _trace;

        public EvaluationTraceInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            StringBuilder? trace,
            out bool shouldAppend)
        {
            _trace = trace;
            shouldAppend = trace is not null;
            if (shouldAppend)
            {
                trace!.EnsureCapacity(trace.Length + literalLength + formattedCount * 8);
            }
        }

        public void AppendLiteral(string value)
        {
            _trace?.Append(value);
        }

        public void AppendFormatted<T>(T value)
        {
            if (_trace is null)
            {
                return;
            }

            _trace.Append(value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : value?.ToString());
        }

        public void AppendFormatted<T>(T value, string? format)
        {
            if (_trace is null)
            {
                return;
            }

            _trace.Append(value is IFormattable formattable
                ? formattable.ToString(format, CultureInfo.InvariantCulture)
                : value?.ToString());
        }

        public void AppendFormatted(string? value)
        {
            _trace?.Append(value);
        }
    }
}
