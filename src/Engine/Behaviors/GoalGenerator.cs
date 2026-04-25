namespace Engine.Behaviors;

using Engine.Domain;
using Engine.Domain.Config;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Wiring;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// Generates bounded goal trees from deficits and planner contracts.
/// Only actionable leaf goals should be considered for execution; blocked parents
/// remain in the tree for inspectability and replanning.
/// </summary>
public sealed class GoalGenerator
{
    private readonly IReadOnlyDictionary<EntityKind, KindConfig> _kindConfigs;
    private readonly IReadOnlyList<ProcessDefinition> _processes;
    private readonly DomainIdRegistry? _domainIds;
    private readonly Dictionary<(EntityKind Kind, AttributeRef Effect), List<ProcessDefinition>> _servingProcessCache = [];

    /// <summary>Max primary plan roots persisted per entity.</summary>
    public int MaxGoals { get; set; } = 3;

    /// <summary>Max total generated goals including blocked parents and sub-goals.</summary>
    public int MaxGeneratedGoals { get; set; } = 12;

    /// <summary>Max recursive blocker expansion depth.</summary>
    public int MaxPlanningDepth { get; set; } = 2;

    /// <summary>Urgency penalty applied when a plan must descend into a blocker.</summary>
    public float SubGoalUrgencyScale { get; set; } = 0.5f;

    /// <summary>Per-depth attenuation so longer plans do not drown out shorter ones.</summary>
    public float DepthPenalty { get; set; } = 0.85f;

    /// <summary>
    /// Self-directed processes still need to exist as planning branches even when
    /// their provider reward is currently zero, because they may be unblocked by a
    /// sub-goal or support action (for example grow -> irrigate).
    /// </summary>
    public float MinimumSelfDirectedReward { get; set; } = 1f;

    /// <summary>Lower bound used when an actionable leaf passes preconditions but has a tiny score.</summary>
    public float MinimumActionableScoreWeight { get; set; } = 0.1f;

    /// <summary>
    /// Fractional improvement required before switching an existing root plan to a
    /// different committed plan for the same desired effect. This protects actors
    /// from oscillating between nearly equivalent targets across replans.
    /// </summary>
    public float TargetSwitchHysteresisRatio { get; set; } = 0.2f;

    private const bool CapturePlanningTraces = false;

    public GoalGenerator(
        IReadOnlyDictionary<EntityKind, KindConfig> kindConfigs,
        IReadOnlyList<ProcessDefinition> processes,
        DomainIdRegistry? domainIds = null)
    {
        _kindConfigs = kindConfigs;
        _processes = processes;
        _domainIds = domainIds;
        if (_domainIds is not null)
        {
            BindRuntimeIds(_domainIds);
        }
    }

    public IReadOnlyList<EntityGoal> Generate(Entity entity, IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(worldState);

        var trace = CreateTraceBuilder();
        AppendLineInvariant(trace, $"Goal Generation Tick {worldState.CurrentTick}");
        AppendLineInvariant(trace, $"Entity: {entity.Name} ({entity.Id}) kind={entity.Kind}");

        if (!_kindConfigs.TryGetValue(entity.Kind, out var kindConfig))
        {
            AppendTraceLine(trace, "No kind config found.");
            return [];
        }

        var primaryPlans = GeneratePrimaryPlans(entity, kindConfig, worldState, trace)
            .OrderByDescending(p => p.BestActionableUrgency)
            .Take(MaxGoals)
            .ToList();

        AppendTraceLine(trace, "");
        AppendTraceLine(trace, "Selected Root Plans:");
        foreach (var plan in primaryPlans)
        {
            AppendLineInvariant(
                trace,
                $"  root process={plan.Root.PlannedProcessId} target={DescribeTarget(worldState, plan.Root.TargetEntityId)} effect={DescribeEffect(plan.Root.DesiredEffect)} best_leaf={plan.BestActionableUrgency:F3}");
        }

        var goals = new List<EntityGoal>();
        foreach (var plan in primaryPlans)
        {
            goals.AddRange(plan.Goals);
        }

        var trimmedGoals = TrimGoals(goals);
        AppendTraceLine(trace, "");
        AppendTraceLine(trace, "Final Goals:");
        foreach (var goal in trimmedGoals.OrderByDescending(g => g.Urgency))
        {
                AppendLineInvariant(
                    trace,
                    $"  depth={goal.Depth} {(goal.IsActionable ? "next" : "blocked")} process={DescribeProcessId(goal.PlannedProcessId)} target={DescribeTarget(worldState, goal.TargetEntityId)} effect={DescribeEffect(goal.DesiredEffect)} urgency={goal.Urgency:F3}");
        }

        return trimmedGoals;
    }

    private List<GoalPlan> GeneratePrimaryPlans(
        Entity entity, KindConfig kindConfig, IWorldState worldState, StringBuilder? trace)
    {
        var providerCandidateCache = new Dictionary<AttributeRef, IReadOnlyList<EntityId>>();
        var demands = new List<(AttributeRef Effect, float Urgency, string Source)>();
        foreach (var attrConfig in kindConfig.Attributes)
        {
            var scalar = _domainIds is null
                ? entity.Attributes.GetScalar(attrConfig.Name)
                : entity.Attributes.GetScalar(_domainIds.AttributeIdOf(attrConfig.Name));
            if (scalar is null) continue;

            float urgency;
            if (StandardNeedScale.AppliesTo(attrConfig))
            {
                urgency = StandardNeedScale.ComputeUrgency(scalar.Value);
            }
            else if (attrConfig.Baseline > 0f)
            {
                urgency = attrConfig.Baseline / (attrConfig.Baseline + scalar.Value);
            }
            else
            {
                var range = attrConfig.Max - attrConfig.Min;
                urgency = range > 0f ? Math.Abs(scalar.Value - attrConfig.Default) / range : 0f;
            }

            if (urgency < 0.01f) continue;
            demands.Add((BindRef(AttributeRef.Self(attrConfig.Name, entity.Kind)), urgency, "deficit"));
        }

        var standingDrives = kindConfig.StandingDrives
            .Where(d => d.BaseUrgency >= 0.01f)
            .ToList();
        foreach (var standingDrive in standingDrives)
        {
            demands.Add((BindRef(standingDrive.DesiredEffect), standingDrive.BaseUrgency, "standing"));
        }

        var orderedDemands = demands
            .GroupBy(d => d.Effect)
            .Select(g => (Effect: g.Key, Urgency: g.Max(x => x.Urgency), Sources: g.Select(x => x.Source).Distinct().ToList()))
            .OrderByDescending(d => d.Urgency)
            .ToList();

        if (demands.Count == 0)
        {
            AppendTraceLine(trace, "No deficits or standing drives above threshold.");
        }
        else
        {
            var deficitDemands = demands.Where(d => d.Source == "deficit").ToList();
            if (deficitDemands.Count == 0)
            {
                AppendTraceLine(trace, "No deficits above threshold.");
            }
            else
            {
                AppendTraceLine(trace, "Deficits:");
                foreach (var (effect, urgency, _) in deficitDemands.OrderByDescending(d => d.Urgency))
                {
                    AppendLineInvariant(trace, $"  {DescribeEffect(effect)} urgency={urgency:F3}");
                }
            }

            if (standingDrives.Count > 0)
            {
                AppendTraceLine(trace, "Standing Drives:");
                foreach (var standingDrive in standingDrives.OrderByDescending(d => d.BaseUrgency))
                {
                    AppendLineInvariant(
                        trace,
                        $"  {DescribeEffect(standingDrive.DesiredEffect)} urgency={standingDrive.BaseUrgency:F3}");
                }
            }
        }

        var plans = new List<GoalPlan>();

        foreach (var (effect, urgency, sources) in orderedDemands)
        {
            var servingProcesses = FindProcessesServingEffect(entity.Kind, effect);
            if (servingProcesses.Count == 0)
            {
                AppendLineInvariant(trace, $"No serving processes for {DescribeEffect(effect)}.");
                continue;
            }

            var candidatePlans = new List<GoalPlan>();

            foreach (var process in servingProcesses)
            {
                foreach (var targetId in EnumeratePrimaryTargets(entity, process, worldState, providerCandidateCache))
                {
                    var plan = BuildPlan(
                        entity,
                        worldState,
                        desiredEffect: effect,
                        targetEntityId: targetId,
                        process: process,
                        baseUrgency: urgency,
                        parentGoalId: null,
                        depth: 0,
                        visited: [],
                        trace: trace);

                    if (plan is not null && plan.BestActionableUrgency > 0f)
                    {
                        candidatePlans.Add(plan);
                    }
                }
            }

            if (candidatePlans.Count == 0)
            {
                continue;
            }

            var selectedPlan = SelectCommittedPlan(entity, effect, candidatePlans, trace);
            if (selectedPlan is not null)
            {
                plans.Add(selectedPlan);
            }
        }

        return plans;
    }

    private GoalPlan? SelectCommittedPlan(
        Entity entity,
        AttributeRef effect,
        IReadOnlyList<GoalPlan> candidatePlans,
        StringBuilder? trace)
    {
        var bestPlan = candidatePlans
            .OrderByDescending(p => p.BestActionableUrgency)
            .FirstOrDefault();
        if (bestPlan is null)
        {
            return null;
        }

        var previousRoot = entity.Goals
            .Where(g => !g.IsSubGoal &&
                        g.DesiredEffect.Equals(effect))
            .OrderByDescending(g => g.Urgency)
            .FirstOrDefault();
        if (previousRoot is null)
        {
            return bestPlan;
        }

        var previousPlan = candidatePlans.FirstOrDefault(p =>
            p.Root.TargetEntityId == previousRoot.TargetEntityId &&
            p.Root.PlannedProcessId == previousRoot.PlannedProcessId);
        if (previousPlan is null)
        {
                AppendLineInvariant(
                    trace,
                    $"effect commitment released effect={DescribeEffect(effect)} old_process={DescribeProcessId(previousRoot.PlannedProcessId)} old_target={previousRoot.TargetEntityId} reason=prior plan unavailable");
            return bestPlan;
        }

        var requiredImprovement = previousPlan.BestActionableUrgency * (1f + TargetSwitchHysteresisRatio);
        if (bestPlan.BestActionableUrgency <= requiredImprovement)
        {
                AppendLineInvariant(
                    trace,
                    $"effect commitment kept process={DescribeProcessId(previousPlan.Root.PlannedProcessId)} effect={DescribeEffect(effect)} target={previousPlan.Root.TargetEntityId} previous={previousPlan.BestActionableUrgency:F3} challenger={bestPlan.BestActionableUrgency:F3}");
            return previousPlan;
        }

        AppendLineInvariant(
            trace,
            $"effect commitment released effect={DescribeEffect(effect)} old_process={DescribeProcessId(previousPlan.Root.PlannedProcessId)} old_target={previousPlan.Root.TargetEntityId} old={previousPlan.BestActionableUrgency:F3} new_process={DescribeProcessId(bestPlan.Root.PlannedProcessId)} new_target={bestPlan.Root.TargetEntityId} new={bestPlan.BestActionableUrgency:F3}");
        return bestPlan;
    }

    private GoalPlan? BuildPlan(
        Entity entity,
        IWorldState worldState,
        AttributeRef desiredEffect,
        EntityId targetEntityId,
        ProcessDefinition process,
        float baseUrgency,
        GoalRuntimeId? parentGoalId,
        int depth,
        HashSet<PlanKey> visited,
        StringBuilder? trace)
    {
        if (depth > MaxPlanningDepth)
        {
            AppendTraceLine(trace, depth, $"skip process={process.Name} effect={DescribeEffect(desiredEffect)}: depth>{MaxPlanningDepth}");
            return null;
        }

        if (!visited.Add(new PlanKey(desiredEffect, targetEntityId, process.RuntimeId)))
        {
            AppendTraceLine(trace, depth, $"skip process={process.Name} effect={DescribeEffect(desiredEffect)} target={targetEntityId}: cycle detected");
            return null;
        }

        try
        {
            var targetEntity = worldState.Graph.GetEntity(targetEntityId);
            if (targetEntity is null || targetEntity.Status != EntityStatus.Active)
            {
                AppendTraceLine(trace, depth, $"reject process={process.Name} target={targetEntityId}: target missing/inactive");
                return null;
            }

            var planningReward = EvaluatePlanningRewardInfo(process, entity, targetEntity, worldState);
            AppendTraceLine(trace, depth,
                $"consider process={process.Name} target={targetEntity.Name} effect={DescribeEffect(desiredEffect)} raw_reward={planningReward.RawReward:F3} planning_weight={planningReward.PlanningWeight:F3} reason={planningReward.Reason}");
            if (!planningReward.Included)
                return null;

            var weightedUrgency = baseUrgency * planningReward.PlanningWeight * MathF.Pow(DepthPenalty, depth);
            var goal = new EntityGoal(
                desiredEffect,
                targetEntityId,
                weightedUrgency,
                plannedProcessId: process.RuntimeId,
                parentGoalId: parentGoalId,
                isActionable: false,
                depth: depth);

            var context = new GoalCheckContext(
                entity,
                goal,
                process.RuntimeId,
                worldState,
                _domainIds,
                capturePreconditionDiagnostics: CapturePlanningTraces,
                capturePreconditionBlockers: true);
            var precondition = process.Precondition.Evaluate(context);

            if (precondition.Passed)
            {
                var score = EvaluateProcessScore(process, entity, targetEntity, worldState);
                var immediateUrgency = score > 0f
                    ? weightedUrgency * MathF.Max(score, MinimumActionableScoreWeight)
                    : 0f;

                var supportPlan = depth < MaxPlanningDepth
                    ? BuildBestSupportPlan(
                        entity,
                        worldState,
                        process,
                        targetEntity,
                        parentGoal: goal,
                        weightedUrgency: weightedUrgency,
                        processScore: score,
                        depth: depth + 1,
                        visited: visited,
                        trace: trace)
                    : null;

                if (supportPlan is not null && supportPlan.BestActionableUrgency > immediateUrgency)
                {
                    goal.Urgency = supportPlan.BestActionableUrgency;
                    AppendTraceLine(trace, depth,
                        $"support path selected over immediate action for process={process.Name} score={score:F3} support_leaf={supportPlan.BestActionableUrgency:F3}");

                    var supportGoals = new List<EntityGoal> { goal };
                    supportGoals.AddRange(supportPlan.Goals);
                    return new GoalPlan(goal, supportGoals, supportPlan.BestActionableUrgency);
                }

                if (immediateUrgency <= 0f)
                {
                    AppendTraceLine(trace, depth, $"passed preconditions but rejected process={process.Name}: score={score:F3}");
                    return null;
                }

                goal.Urgency = immediateUrgency;
                goal.IsActionable = true;
                if (supportPlan is not null)
                {
                    AppendTraceLine(trace, depth,
                        $"actionable process={process.Name} score={score:F3} final_urgency={goal.Urgency:F3} support_available={supportPlan.BestActionableUrgency:F3}");
                }
                else
                {
                    AppendTraceLine(trace, depth,
                        $"actionable process={process.Name} score={score:F3} final_urgency={goal.Urgency:F3}");
                }

                return new GoalPlan(goal, [goal], goal.Urgency);
            }

            if (depth == MaxPlanningDepth || precondition.Blockers.Count == 0)
            {
                AppendTraceLine(trace, depth,
                    $"blocked process={process.Name} diagnostic=\"{precondition.Diagnostic}\" blockers={DescribeBlockers(precondition.Blockers)} no further expansion");
                return null;
            }

            AppendTraceLine(trace, depth,
                $"blocked process={process.Name} diagnostic=\"{precondition.Diagnostic}\" blockers={DescribeBlockers(precondition.Blockers)}");

            var childPlans = new List<GoalPlan>();
            foreach (var blocker in precondition.Blockers.Distinct())
            {
                var childPlan = BuildBestPlanForBlocker(
                    entity,
                    worldState,
                    blocker,
                    parentGoal: goal,
                    childBaseUrgency: weightedUrgency * SubGoalUrgencyScale,
                    depth: depth + 1,
                    visited: visited,
                    trace: trace);

                if (childPlan is not null)
                {
                    childPlans.Add(childPlan);
                }
            }

            if (childPlans.Count == 0)
            {
                AppendTraceLine(trace, depth, $"no viable child plans for blocked process={process.Name}");
                return null;
            }

            var bestActionableUrgency = childPlans.Max(p => p.BestActionableUrgency);
            goal.Urgency = bestActionableUrgency;
            AppendTraceLine(trace, depth, $"blocked root process={process.Name} inherits best_leaf={bestActionableUrgency:F3}");
            var goals = new List<EntityGoal> { goal };
            foreach (var childPlan in childPlans)
            {
                goals.AddRange(childPlan.Goals);
            }

            return new GoalPlan(goal, goals, bestActionableUrgency);
        }
        finally
        {
                visited.Remove(new PlanKey(desiredEffect, targetEntityId, process.RuntimeId));
        }
    }

    private GoalPlan? BuildBestSupportPlan(
        Entity entity,
        IWorldState worldState,
        ProcessDefinition process,
        Entity targetEntity,
        EntityGoal parentGoal,
        float weightedUrgency,
        float processScore,
        int depth,
        HashSet<PlanKey> visited,
        StringBuilder? trace)
    {
        GoalPlan? best = null;
        var scoreHeadroom = 1f - Math.Clamp(processScore, 0f, 1f);
        foreach (var rule in process.SupportRules)
        {
            var currentValue = EvaluateSupportCurrentValue(process, entity, targetEntity, worldState, rule);
            if (rule.DesiredMinimum <= 0f)
            {
                continue;
            }

            var deficitRatio = Math.Clamp((rule.DesiredMinimum - currentValue) / rule.DesiredMinimum, 0f, 1f);
            if (deficitRatio <= 0f)
            {
                continue;
            }

            var childBaseUrgency = weightedUrgency
                * SubGoalUrgencyScale
                * rule.UrgencyScale
                * MathF.Max(deficitRatio, scoreHeadroom);
            if (childBaseUrgency <= 0f)
            {
                continue;
            }

            AppendTraceLine(trace, depth,
                $"support effect={DescribeEffect(rule.DesiredEffect)} current={currentValue:F3} desired={rule.DesiredMinimum:F3} deficit={deficitRatio:F3} child_base={childBaseUrgency:F3}");

            var candidate = BuildBestPlanForBlocker(
                entity,
                worldState,
                rule.DesiredEffect,
                parentGoal,
                childBaseUrgency,
                depth,
                visited,
                trace);

            if (candidate is null)
            {
                continue;
            }

            if (best is null || candidate.BestActionableUrgency > best.BestActionableUrgency)
            {
                best = candidate;
            }
        }

        return best;
    }

    private GoalPlan? BuildBestPlanForBlocker(
        Entity entity,
        IWorldState worldState,
        AttributeRef blocker,
        EntityGoal parentGoal,
        float childBaseUrgency,
        int depth,
        HashSet<PlanKey> visited,
        StringBuilder? trace)
    {
        var targetEntityId = ResolveBlockedTarget(entity, parentGoal, blocker);
        GoalPlan? best = null;
        var servingProcesses = FindProcessesServingEffect(entity.Kind, blocker);

        AppendTraceLine(trace, depth,
            $"expand blocker effect={DescribeEffect(blocker)} target={DescribeTarget(worldState, targetEntityId)} serving_processes={servingProcesses.Count}");

        if (servingProcesses.Count == 0)
        {
            AppendTraceLine(trace, depth, $"no serving processes for blocker {DescribeEffect(blocker)}");
            return null;
        }

        foreach (var process in servingProcesses)
        {
            var candidate = BuildPlan(
                entity,
                worldState,
                desiredEffect: blocker,
                targetEntityId: targetEntityId,
                process: process,
                baseUrgency: childBaseUrgency,
                parentGoalId: parentGoal.Id,
                depth: depth,
                visited: visited,
                trace: trace);

            if (candidate is null)
                continue;

            if (best is null || candidate.BestActionableUrgency > best.BestActionableUrgency)
            {
                best = candidate;
            }
        }

        if (best is not null)
        {
            AppendTraceLine(trace, depth,
                $"selected child process={best.Root.PlannedProcessId} best_leaf={best.BestActionableUrgency:F3}");
        }

        return best;
    }

    private EntityId ResolveBlockedTarget(Entity entity, EntityGoal parentGoal, AttributeRef blocker)
    {
        // Target and Relational blockers always inherit parent target (they're about
        // the actor's relationship with the parent's target).
        // Structural blockers (e.g., "in_physical_range", "in_semantic_range") inherit
        // when the parent goal itself has a non-self target — those facts describe the
        // actor's position relative to that target.
        // Self-scope or self-directed parents fall back to self.
        return blocker.Scope switch
        {
            RefScope.Target or RefScope.Relational => parentGoal.TargetEntityId,
            RefScope.Structural when parentGoal.TargetEntityId != entity.Id => parentGoal.TargetEntityId,
            _ => entity.Id,
        };
    }

    private List<ProcessDefinition> FindProcessesServingEffect(EntityKind kind, AttributeRef effect)
    {
        var key = (kind, effect);
        if (_servingProcessCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var servingProcesses = _processes
            .Where(p => p.TargetKind == kind && p.GoalEffects.Contains(effect))
            .ToList();
        _servingProcessCache[key] = servingProcesses;
        return servingProcesses;
    }

    private HashSet<EntityId> EnumeratePrimaryTargets(
        Entity entity,
        ProcessDefinition process,
        IWorldState worldState,
        Dictionary<AttributeRef, IReadOnlyList<EntityId>> providerCandidateCache)
    {
        if (process.ProviderSelectors.Count == 0)
        {
            return [entity.Id];
        }

        var candidates = new HashSet<EntityId>();
        foreach (var selector in process.ProviderSelectors)
        {
            if (!providerCandidateCache.TryGetValue(selector, out var selectorCandidates))
            {
                selectorCandidates = GraphAttributeQuery.FindCandidates(selector, worldState);
                providerCandidateCache[selector] = selectorCandidates;
            }

            foreach (var candidateId in selectorCandidates)
            {
                candidates.Add(candidateId);
            }
        }

        return candidates;
    }

    private PlanningRewardInfo EvaluatePlanningRewardInfo(
        ProcessDefinition process, Entity entity, Entity targetEntity, IWorldState worldState)
    {
        var desiredEffect = process.GoalEffects.FirstOrDefault()
            ?? AttributeRef.StructuralFact("planning_probe", entity.Kind);
        var expressionContext = new GoalCheckContext(
            entity,
            new EntityGoal(
                desiredEffect: desiredEffect,
                targetEntityId: targetEntity.Id,
                urgency: 0f,
                plannedProcessId: process.RuntimeId),
            process.RuntimeId,
            worldState,
            _domainIds,
            capturePreconditionDiagnostics: CapturePlanningTraces,
            capturePreconditionBlockers: false);
        var rawReward = process.RewardExpression.Evaluate(
            new ExpressionContext(entity, targetEntity, worldState.Grid, expressionContext));

        if (process.ProviderSelectors.Count == 0)
        {
            var flooredReward = MathF.Max(rawReward, MinimumSelfDirectedReward);
            return new PlanningRewardInfo(
                true,
                rawReward,
                1f + MathF.Log(1f + flooredReward),
                rawReward <= 0f ? "self-directed floor applied" : "accepted");
        }

        if (rawReward <= 0f)
        {
            return new PlanningRewardInfo(false, rawReward, 0f, "reward <= 0");
        }

        return new PlanningRewardInfo(true, rawReward, 1f + MathF.Log(1f + rawReward), "accepted");
    }

    private float EvaluateProcessScore(
        ProcessDefinition process, Entity entity, Entity targetEntity, IWorldState worldState)
    {
        var desiredEffect = process.GoalEffects.FirstOrDefault()
            ?? AttributeRef.StructuralFact("planning_probe", entity.Kind);
        var expressionContext = new GoalCheckContext(
            entity,
            new EntityGoal(
                desiredEffect: desiredEffect,
                targetEntityId: targetEntity.Id,
                urgency: 0f,
                plannedProcessId: process.RuntimeId),
            process.RuntimeId,
            worldState,
            _domainIds,
            capturePreconditionDiagnostics: CapturePlanningTraces,
            capturePreconditionBlockers: false);
        return MathF.Max(0f, process.ScoreExpression.Evaluate(
            new ExpressionContext(entity, targetEntity, worldState.Grid, expressionContext)));
    }

    private float EvaluateSupportCurrentValue(
        ProcessDefinition process,
        Entity entity,
        Entity targetEntity,
        IWorldState worldState,
        PlannerSupportRule rule)
    {
        var desiredEffect = process.GoalEffects.FirstOrDefault()
            ?? AttributeRef.StructuralFact("planning_probe", entity.Kind);
        var expressionContext = new GoalCheckContext(
            entity,
            new EntityGoal(
                desiredEffect: desiredEffect,
                targetEntityId: targetEntity.Id,
                urgency: 0f,
                plannedProcessId: process.RuntimeId),
            process.RuntimeId,
            worldState,
            _domainIds,
            capturePreconditionDiagnostics: CapturePlanningTraces,
            capturePreconditionBlockers: false);
        return MathF.Max(0f, rule.CurrentValueExpression.Evaluate(
            new ExpressionContext(entity, targetEntity, worldState.Grid, expressionContext)));
    }

    private List<EntityGoal> TrimGoals(List<EntityGoal> goals)
    {
        var dedupedGoals = DedupeActionableGoals(goals);

        if (dedupedGoals.Count <= MaxGeneratedGoals)
            return dedupedGoals.OrderByDescending(g => g.Urgency).ToList();

        var primaryGoals = dedupedGoals.Where(g => !g.IsSubGoal).ToList();
        var subGoals = dedupedGoals.Where(g => g.IsSubGoal)
            .OrderByDescending(g => g.Urgency)
            .ToList();

        if (primaryGoals.Count >= MaxGeneratedGoals)
        {
            return primaryGoals
                .OrderByDescending(g => g.Urgency)
                .Take(MaxGeneratedGoals)
                .ToList();
        }

        var trimmed = new List<EntityGoal>(primaryGoals);
        trimmed.AddRange(subGoals.Take(MaxGeneratedGoals - trimmed.Count));
        return trimmed.OrderByDescending(g => g.Urgency).ToList();
    }

    private static List<EntityGoal> DedupeActionableGoals(List<EntityGoal> goals)
    {
        var deduped = new List<EntityGoal>();
        var actionableGoals = new Dictionary<(ProcessRuntimeId? ProcessId, EntityId Target, AttributeRef Effect), EntityGoal>();

        foreach (var goal in goals)
        {
            if (!goal.IsActionable)
            {
                deduped.Add(goal);
                continue;
            }

            var key = (goal.PlannedProcessId, goal.TargetEntityId, goal.DesiredEffect);
            if (!actionableGoals.TryGetValue(key, out var existing) || goal.Urgency > existing.Urgency)
            {
                actionableGoals[key] = goal;
            }
        }

        deduped.AddRange(actionableGoals.Values);
        return deduped;
    }

    private readonly record struct PlanKey(AttributeRef Effect, EntityId TargetEntityId, ProcessRuntimeId ProcessId);

    private readonly record struct PlanningRewardInfo(
        bool Included,
        float RawReward,
        float PlanningWeight,
        string Reason);

    private void BindRuntimeIds(DomainIdRegistry runtimeIds)
    {
        foreach (var process in _processes)
        {
            process.BindRuntimeIds(runtimeIds);
        }

        foreach (var kindConfig in _kindConfigs.Values)
        {
            foreach (var standingDrive in kindConfig.StandingDrives)
            {
                standingDrive.DesiredEffect.BindRuntimeIds(runtimeIds);
            }
        }
    }

    private AttributeRef BindRef(AttributeRef reference)
    {
        if (_domainIds is not null && reference.AttributeId.Value == 0 &&
            reference.PressureId.Value == 0 &&
            reference.ResourceId.Value == 0 &&
            reference.RelationshipKindId.Value == 0)
        {
            reference.BindRuntimeIds(_domainIds);
        }

        return reference;
    }

    private sealed class GoalPlan(EntityGoal root, IReadOnlyList<EntityGoal> goals, float bestActionableUrgency)
    {
        public EntityGoal Root { get; } = root;
        public IReadOnlyList<EntityGoal> Goals { get; } = goals;
        public float BestActionableUrgency { get; } = bestActionableUrgency;
    }

    private sealed class GoalCheckContext : IMutationContext, IWorldStateAccessor, IPreconditionDiagnosticContext
    {
        private readonly Entity _entity;
        private readonly ProcessRuntimeId? _definitionId;
        private readonly IWorldState _worldState;
        private readonly DomainIdRegistry? _domainIds;

        public int CurrentTick => _worldState.CurrentTick;
        public EntityId SelfEntityId => _entity.Id;
        public Random Random => Random.Shared;
        public EntityId? ResolvedTargetId { get; }
        public IWorldState WorldState => _worldState;
        public bool CapturePreconditionDiagnostics { get; }
        public bool CapturePreconditionBlockers { get; }

        public GoalCheckContext(
            Entity entity,
            EntityGoal goal,
            ProcessRuntimeId? definitionId,
            IWorldState worldState,
            DomainIdRegistry? domainIds,
            bool capturePreconditionDiagnostics,
            bool capturePreconditionBlockers)
        {
            _entity = entity;
            _definitionId = definitionId;
            ResolvedTargetId = goal.TargetEntityId;
            _worldState = worldState;
            _domainIds = domainIds;
            CapturePreconditionDiagnostics = capturePreconditionDiagnostics;
            CapturePreconditionBlockers = capturePreconditionBlockers;
        }

        public float GetPressure(string pressureName, EntityId scopeId) =>
            _domainIds is null
                ? _worldState.Pressure?.GetValue(pressureName, scopeId) ?? 0f
                : GetPressure(_domainIds.PressureIdOf(pressureName), scopeId);
        public float GetPressure(PressureRuntimeId pressureId, EntityId scopeId) =>
            _worldState.Pressure?.GetValue(pressureId, scopeId) ?? 0f;
        public float GetAttribute(EntityId entityId, string attributeName) =>
            _domainIds is null
                ? GetEntity(entityId)?.Attributes.GetValue(attributeName) ?? 0f
                : GetAttribute(entityId, _domainIds.AttributeIdOf(attributeName));
        public float GetAttribute(EntityId entityId, AttributeRuntimeId attributeId) =>
            GetEntity(entityId)?.Attributes.GetValue(attributeId) ?? 0f;
        public int CountEntitiesByKind(EntityKind kind) => _worldState.Graph.GetEntitiesByKind(kind).Count;
        public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) =>
            _domainIds is null
                ? FindRelationshipByName(sourceId, targetId, kind)
                : FindRelationship(sourceId, targetId, _domainIds.RelationshipKindIdOf(kind));
        public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, RelationshipKindRuntimeId kind) =>
            _worldState is ICachedWorldState cachedWorldState
                ? cachedWorldState.QueryCache.FindRelationship(sourceId, targetId, kind)
                : _worldState.Graph.OutgoingRelationships(sourceId, kind)
                    .FirstOrDefault(r => r.TargetId == targetId);
        public int? GetLastFiredTick() =>
            _definitionId is not null && _entity.LastDefinitionExecutionTicks.TryGetValue(_definitionId.Value, out var tick)
                ? tick
                : null;
        public Entity? GetEntity(EntityId id) =>
            _worldState is ICachedWorldState cachedWorldState
                ? cachedWorldState.QueryCache.GetEntity(id)
                : _worldState.Graph.GetEntity(id);
        public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind) =>
            _domainIds is null
                ? FindRelatedEntityByName(sourceId, relationshipKind)
                : FindRelatedEntity(sourceId, _domainIds.RelationshipKindIdOf(relationshipKind));
        public EntityId? FindRelatedEntity(EntityId sourceId, RelationshipKindRuntimeId relationshipKind) =>
            _worldState is ICachedWorldState cachedWorldState
                ? cachedWorldState.QueryCache.FindRelatedEntity(sourceId, relationshipKind)
                : _worldState.Graph.OutgoingRelationships(sourceId, relationshipKind).FirstOrDefault()?.TargetId;
        public IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind) => _worldState.Graph.GetEntitiesByKind(kind);
        public float GetResource((int Q, int R) coordinates, Resources.ResourceType resourceType)
        {
            if (_worldState.Resources is null)
            {
                return 0f;
            }

            var coord = new Engine.Map.HexCoord(coordinates.Q, coordinates.R);
            return resourceType switch
            {
                Resources.ResourceType.Food => _worldState.Resources.Food.GetStockpile(coord),
                Resources.ResourceType.Wealth => _worldState.Resources.Wealth.GetStockpile(coord),
                Resources.ResourceType.Population => _worldState.Resources.PopulationLayer.GetStockpile(coord),
                _ => 0f,
            };
        }
        public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) => throw ReadOnlyContextMutationException();
        public void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) => throw ReadOnlyContextMutationException();
        public void CreateRelationship(RelationshipKindRuntimeId kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) => throw ReadOnlyContextMutationException();
        public void ModifyAttribute(EntityId entityId, string attributeName, float delta) => throw ReadOnlyContextMutationException();
        public void ModifyAttribute(EntityId entityId, AttributeRuntimeId attributeId, float delta) => throw ReadOnlyContextMutationException();
        public void ModifyPressure(string pressureName, EntityId scopeId, float delta) => throw ReadOnlyContextMutationException();
        public void ModifyPressure(PressureRuntimeId pressureId, EntityId scopeId, float delta) => throw ReadOnlyContextMutationException();
        public void ModifyResource((int Q, int R) coordinates, Resources.ResourceType resourceType, float delta) => throw ReadOnlyContextMutationException();
        public void SetTag(EntityId entityId, string key, string value) => throw ReadOnlyContextMutationException();
        public void SetEntityHistorical(EntityId entityId) => throw ReadOnlyContextMutationException();
        public float ConsumeResource((int Q, int R) coordinates, Resources.ResourceType resourceType, float amount) => throw ReadOnlyContextMutationException();
        public void SetCoordinates(EntityId entityId, int q, int r) => throw ReadOnlyContextMutationException();
        public void SetSemanticCoordinates(EntityId entityId, int q, int r) => throw ReadOnlyContextMutationException();
        public void CullRelationship(EntityId sourceId, string relationshipKind) => throw ReadOnlyContextMutationException();
        public void CullRelationship(EntityId sourceId, RelationshipKindRuntimeId relationshipKind) => throw ReadOnlyContextMutationException();
        public void SetVariable(string name, float value) { }
        public float GetVariable(string name) => 0f;

        private Relationship? FindRelationshipByName(EntityId sourceId, EntityId targetId, string kind) =>
            _worldState is ICachedWorldState cachedWorldState
                ? cachedWorldState.QueryCache.FindRelationship(sourceId, targetId, kind)
                : _worldState.Graph.OutgoingRelationships(sourceId, kind)
                    .FirstOrDefault(r => r.TargetId == targetId);

        private EntityId? FindRelatedEntityByName(EntityId sourceId, string relationshipKind) =>
            _worldState is ICachedWorldState cachedWorldState
                ? cachedWorldState.QueryCache.FindRelatedEntity(sourceId, relationshipKind)
                : _worldState.Graph.OutgoingRelationships(sourceId, relationshipKind).FirstOrDefault()?.TargetId;

        private static NotSupportedException ReadOnlyContextMutationException() =>
            new("Planner goal checks are read-only and cannot mutate engine state.");
    }

    private static void AppendTraceLine(StringBuilder? trace, string text)
    {
        trace?.AppendLine(text);
    }

    private static void AppendTraceLine(
        StringBuilder? trace,
        [InterpolatedStringHandlerArgument(nameof(trace))] GoalTraceInterpolatedStringHandler text)
    {
        trace?.AppendLine();
    }

    private static void AppendTraceLine(StringBuilder? trace, int depth, string text)
    {
        if (trace is null) return;
        trace.Append(' ', depth * 2);
        trace.AppendLine(text);
    }

    private static void AppendTraceLine(
        StringBuilder? trace,
        int depth,
        [InterpolatedStringHandlerArgument(nameof(trace), nameof(depth))] GoalTraceInterpolatedStringHandler text)
    {
        trace?.AppendLine();
    }

    private static void AppendLineInvariant(
        StringBuilder? trace,
        [InterpolatedStringHandlerArgument(nameof(trace))] GoalTraceInterpolatedStringHandler text)
    {
        trace?.AppendLine();
    }

    private static string DescribeEffect(AttributeRef effect) => effect.ToString();

    private static string DescribeBlockers(IEnumerable<AttributeRef> blockers) =>
        string.Join(", ", blockers.Select(DescribeEffect));

    private static string DescribeTarget(IWorldState worldState, EntityId targetEntityId) =>
        worldState.Graph.GetEntity(targetEntityId)?.Name ?? targetEntityId.ToString();

    private static string DescribeProcessId(ProcessRuntimeId? processId) =>
        processId is { } value && !value.IsNone
            ? value.Value.ToString(CultureInfo.InvariantCulture)
            : "(abstract)";

    private static StringBuilder? CreateTraceBuilder() => null;

    [InterpolatedStringHandler]
    private ref struct GoalTraceInterpolatedStringHandler
    {
        private readonly StringBuilder? _trace;

        public GoalTraceInterpolatedStringHandler(
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

        public GoalTraceInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            StringBuilder? trace,
            int depth,
            out bool shouldAppend)
            : this(literalLength + depth * 2, formattedCount, trace, out shouldAppend)
        {
            if (shouldAppend)
            {
                trace!.Append(' ', depth * 2);
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
