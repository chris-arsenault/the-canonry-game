namespace Engine.Behaviors;

using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Wiring;
using System;
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
    /// sub-goal (for example grow -> stockpile).
    /// </summary>
    public float MinimumSelfDirectedReward { get; set; } = 1f;

    /// <summary>Lower bound used when an actionable leaf passes preconditions but has a tiny score.</summary>
    public float MinimumActionableScoreWeight { get; set; } = 0.1f;

    public GoalGenerator(
        IReadOnlyDictionary<EntityKind, KindConfig> kindConfigs,
        IReadOnlyList<ProcessDefinition> processes)
    {
        _kindConfigs = kindConfigs;
        _processes = processes;
    }

    public IReadOnlyList<EntityGoal> Generate(Entity entity, IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(worldState);

        var trace = new StringBuilder();
        AppendLineInvariant(trace, $"Goal Generation Tick {worldState.CurrentTick}");
        AppendLineInvariant(trace, $"Entity: {entity.Name} ({entity.Id}) kind={entity.Kind}");

        if (!_kindConfigs.TryGetValue(entity.Kind, out var kindConfig))
        {
            trace.AppendLine("No kind config found.");
            entity.GoalGenerationTrace = trace.ToString();
            return [];
        }

        var primaryPlans = GeneratePrimaryPlans(entity, kindConfig, worldState, trace)
            .OrderByDescending(p => p.BestActionableUrgency)
            .Take(MaxGoals)
            .ToList();

        trace.AppendLine();
        trace.AppendLine("Selected Root Plans:");
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
        trace.AppendLine();
        trace.AppendLine("Final Goals:");
        foreach (var goal in trimmedGoals.OrderByDescending(g => g.Urgency))
        {
            AppendLineInvariant(
                trace,
                $"  depth={goal.Depth} {(goal.IsActionable ? "next" : "blocked")} process={goal.PlannedProcessId ?? "(abstract)"} target={DescribeTarget(worldState, goal.TargetEntityId)} effect={DescribeEffect(goal.DesiredEffect)} urgency={goal.Urgency:F3}");
        }

        entity.GoalGenerationTrace = trace.ToString();
        return trimmedGoals;
    }

    private List<GoalPlan> GeneratePrimaryPlans(
        Entity entity, KindConfig kindConfig, IWorldState worldState, StringBuilder trace)
    {
        var demands = new List<(AttributeRef Effect, float Urgency, string Source)>();
        foreach (var attrConfig in kindConfig.Attributes)
        {
            if (attrConfig.PlaneDefinition is not null) continue;

            var scalar = entity.Attributes.GetScalar(attrConfig.Name);
            if (scalar is null) continue;

            float urgency;
            if (attrConfig.Baseline > 0f)
            {
                urgency = attrConfig.Baseline / (attrConfig.Baseline + scalar.Value);
            }
            else
            {
                var range = attrConfig.Max - attrConfig.Min;
                urgency = range > 0f ? Math.Abs(scalar.Value - attrConfig.Default) / range : 0f;
            }

            if (urgency < 0.01f) continue;
            demands.Add((AttributeRef.Self(attrConfig.Name, entity.Kind), urgency, "deficit"));
        }

        var standingDrives = kindConfig.StandingDrives
            .Where(d => d.BaseUrgency >= 0.01f)
            .ToList();
        foreach (var standingDrive in standingDrives)
        {
            demands.Add((standingDrive.DesiredEffect, standingDrive.BaseUrgency, "standing"));
        }

        var orderedDemands = demands
            .GroupBy(d => d.Effect)
            .Select(g => (Effect: g.Key, Urgency: g.Max(x => x.Urgency), Sources: g.Select(x => x.Source).Distinct().ToList()))
            .OrderByDescending(d => d.Urgency)
            .ToList();

        if (demands.Count == 0)
        {
            trace.AppendLine("No deficits or standing drives above threshold.");
        }
        else
        {
            var deficitDemands = demands.Where(d => d.Source == "deficit").ToList();
            if (deficitDemands.Count == 0)
            {
                trace.AppendLine("No deficits above threshold.");
            }
            else
            {
                trace.AppendLine("Deficits:");
                foreach (var (effect, urgency, _) in deficitDemands.OrderByDescending(d => d.Urgency))
                {
                    AppendLineInvariant(trace, $"  {DescribeEffect(effect)} urgency={urgency:F3}");
                }
            }

            if (standingDrives.Count > 0)
            {
                trace.AppendLine("Standing Drives:");
                foreach (var standingDrive in standingDrives.OrderByDescending(d => d.BaseUrgency))
                {
                    AppendLineInvariant(
                        trace,
                        $"  {DescribeEffect(standingDrive.DesiredEffect)} urgency={standingDrive.BaseUrgency:F3}");
                }
            }
        }

        var plans = new List<GoalPlan>();
        var rootPlans = new Dictionary<(AttributeRef Effect, string ProcessId, EntityId Target), GoalPlan>();

        foreach (var (effect, urgency, sources) in orderedDemands)
        {
            var servingProcesses = FindProcessesServingEffect(entity.Kind, effect);
            if (servingProcesses.Count == 0)
            {
                AppendLineInvariant(trace, $"No serving processes for {DescribeEffect(effect)}.");
                continue;
            }

            foreach (var process in servingProcesses)
            {
                foreach (var targetId in EnumeratePrimaryTargets(entity, process, worldState))
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
                        var key = (effect, process.Id, targetId);
                        if (!rootPlans.TryGetValue(key, out var existing) ||
                            plan.BestActionableUrgency > existing.BestActionableUrgency)
                        {
                            rootPlans[key] = plan;
                        }
                    }
                }
            }
        }

        plans.AddRange(rootPlans.Values);
        return plans;
    }

    private GoalPlan? BuildPlan(
        Entity entity,
        IWorldState worldState,
        AttributeRef desiredEffect,
        EntityId targetEntityId,
        ProcessDefinition process,
        float baseUrgency,
        string? parentGoalId,
        int depth,
        HashSet<PlanKey> visited,
        StringBuilder trace)
    {
        if (depth > MaxPlanningDepth)
        {
            AppendTraceLine(trace, depth, $"skip process={process.Name} effect={DescribeEffect(desiredEffect)}: depth>{MaxPlanningDepth}");
            return null;
        }

        if (!visited.Add(new PlanKey(desiredEffect, targetEntityId, process.Id)))
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
                plannedProcessId: process.Id,
                parentGoalId: parentGoalId,
                isActionable: false,
                depth: depth);

            var context = new GoalCheckContext(entity, goal, process.Id, worldState);
            var precondition = process.Precondition.Evaluate(context);

            if (precondition.Passed)
            {
                var score = EvaluateProcessScore(process, entity, targetEntity, worldState);
                if (score <= 0f)
                {
                    AppendTraceLine(trace, depth, $"passed preconditions but rejected process={process.Name}: score={score:F3}");
                    return null;
                }

                goal.Urgency = weightedUrgency * MathF.Max(score, MinimumActionableScoreWeight);
                goal.IsActionable = true;
                AppendTraceLine(trace, depth,
                    $"actionable process={process.Name} score={score:F3} final_urgency={goal.Urgency:F3}");

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
            visited.Remove(new PlanKey(desiredEffect, targetEntityId, process.Id));
        }
    }

    private GoalPlan? BuildBestPlanForBlocker(
        Entity entity,
        IWorldState worldState,
        AttributeRef blocker,
        EntityGoal parentGoal,
        float childBaseUrgency,
        int depth,
        HashSet<PlanKey> visited,
        StringBuilder trace)
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
        return blocker.Scope switch
        {
            RefScope.Target or RefScope.Relational => parentGoal.TargetEntityId,
            _ => entity.Id,
        };
    }

    private List<ProcessDefinition> FindProcessesServingEffect(EntityKind kind, AttributeRef effect)
    {
        return _processes
            .Where(p => p.TargetKind == kind && p.GoalEffects.Contains(effect))
            .ToList();
    }

    private HashSet<EntityId> EnumeratePrimaryTargets(
        Entity entity, ProcessDefinition process, IWorldState worldState)
    {
        if (process.ProviderSelectors.Count == 0)
        {
            return [entity.Id];
        }

        var candidates = new HashSet<EntityId>();
        foreach (var selector in process.ProviderSelectors)
        {
            foreach (var candidateId in GraphAttributeQuery.FindCandidates(selector, worldState))
            {
                candidates.Add(candidateId);
            }
        }

        return candidates;
    }

    private PlanningRewardInfo EvaluatePlanningRewardInfo(
        ProcessDefinition process, Entity entity, Entity targetEntity, IWorldState worldState)
    {
        var rawReward = process.RewardExpression.Evaluate(
            new ExpressionContext(entity, targetEntity, worldState.Grid));

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

    private static float EvaluateProcessScore(
        ProcessDefinition process, Entity entity, Entity targetEntity, IWorldState worldState)
    {
        return MathF.Max(0f, process.ScoreExpression.Evaluate(
            new ExpressionContext(entity, targetEntity, worldState.Grid)));
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
        var actionableGoals = new Dictionary<(string? ProcessId, EntityId Target, AttributeRef Effect), EntityGoal>();

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

    private readonly record struct PlanKey(AttributeRef Effect, EntityId TargetEntityId, string ProcessId);

    private readonly record struct PlanningRewardInfo(
        bool Included,
        float RawReward,
        float PlanningWeight,
        string Reason);

    private sealed class GoalPlan(EntityGoal root, IReadOnlyList<EntityGoal> goals, float bestActionableUrgency)
    {
        public EntityGoal Root { get; } = root;
        public IReadOnlyList<EntityGoal> Goals { get; } = goals;
        public float BestActionableUrgency { get; } = bestActionableUrgency;
    }

        private sealed class GoalCheckContext : IMutationContext
    {
        private readonly Entity _entity;
        private readonly string? _definitionId;
        private readonly IWorldState _worldState;

        public int CurrentTick => _worldState.CurrentTick;
        public EntityId SelfEntityId => _entity.Id;
        public Random Random => Random.Shared;
        public EntityId? ResolvedTargetId { get; }

        public GoalCheckContext(Entity entity, EntityGoal goal, string? definitionId, IWorldState worldState)
        {
            _entity = entity;
            _definitionId = definitionId;
            ResolvedTargetId = goal.TargetEntityId;
            _worldState = worldState;
        }

        public float GetPressure(string pressureName, EntityId scopeId) =>
            _worldState.Pressure?.GetValue(pressureName, scopeId) ?? 0f;
        public float GetAttribute(EntityId entityId, string attributeName) =>
            _worldState.Graph.GetEntity(entityId)?.Attributes.GetValue(attributeName) ?? 0f;
        public int CountEntitiesByKind(EntityKind kind) => _worldState.Graph.GetEntitiesByKind(kind).Count;
        public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) =>
            _worldState.Graph.OutgoingRelationships(sourceId, kind)
                .FirstOrDefault(r => r.TargetId == targetId);
        public int? GetLastFiredTick() =>
            _definitionId is not null && _entity.LastDefinitionExecutionTicks.TryGetValue(_definitionId, out var tick)
                ? tick
                : null;
        public Entity? GetEntity(EntityId id) => _worldState.Graph.GetEntity(id);
        public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind) =>
            _worldState.Graph.OutgoingRelationships(sourceId, relationshipKind).FirstOrDefault()?.TargetId;
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
                Resources.ResourceType.Sustenance => _worldState.Resources.Sustenance.GetStockpile(coord),
                Resources.ResourceType.Gold => _worldState.Resources.Gold.GetStockpile(coord),
                Resources.ResourceType.Population => _worldState.Resources.PopulationLayer.GetStockpile(coord),
                _ => 0f,
            };
        }
        public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) => throw new NotSupportedException();
        public void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) { }
        public void ModifyAttribute(EntityId entityId, string attributeName, float delta) { }
        public void ModifyPressure(string pressureName, EntityId scopeId, float delta) { }
        public void SetTag(EntityId entityId, string key, string value) { }
        public void SetEntityHistorical(EntityId entityId) { }
        public float ConsumeResource((int Q, int R) coordinates, Resources.ResourceType resourceType, float amount) => 0f;
        public void SetCoordinates(EntityId entityId, int q, int r) { }
        public void CullRelationship(EntityId sourceId, string relationshipKind) { }
        public void SetVariable(string name, float value) { }
        public float GetVariable(string name) => 0f;
    }

    private static void AppendTraceLine(StringBuilder trace, int depth, string text)
    {
        trace.Append(' ', depth * 2);
        trace.AppendLine(text);
    }

    private static void AppendTraceLine(StringBuilder trace, int depth, FormattableString text)
    {
        trace.Append(' ', depth * 2);
        trace.AppendLine(FormattableString.Invariant(text));
    }

    private static void AppendLineInvariant(StringBuilder trace, FormattableString text)
    {
        trace.AppendLine(FormattableString.Invariant(text));
    }

    private static string DescribeEffect(AttributeRef effect) => effect.ToString();

    private static string DescribeBlockers(IEnumerable<AttributeRef> blockers) =>
        string.Join(", ", blockers.Select(DescribeEffect));

    private static string DescribeTarget(IWorldState worldState, EntityId targetEntityId) =>
        worldState.Graph.GetEntity(targetEntityId)?.Name ?? targetEntityId.ToString();
}
