namespace Engine.Domain.Tests.Architecture;

public sealed class GambitPlanningRemovalArchitectureTests
{
    [Fact]
    public void KernelPlanningProgram_IsRemovedAfterGambitsBecomeOpportunistic()
    {
        var root = FindRepoRoot();
        var planningProgramPath = Path.Combine(root.FullName, "src", "Engine.Kernel", "Programs", "Planning");
        var kernelSource = string.Concat(
            ReadAllSourcesUnder(root, "src/Engine.Kernel"),
            ReadAllSourcesUnder(root, "src/Engine.Constants"));
        var forbiddenFragments = new[]
        {
            "PlanningProgramRuntime",
            "PlanningProgramData",
            "PlanningProgramState",
            "PlanningProgramScratch",
            "PlanningProgramSnapshot",
            "PlanningRequest",
            "PlanningDecision",
            "PlannerKindCodes",
            "PlanningStatusCodes",
            "PlanningStepRoleCodes",
            "BehaviorObjectiveUrgencyCodes",
            "GambitObjectiveCapacity",
            "GambitSupportRuleCapacity",
            "EmitPlan",
            "EmitGoalConsidered",
            "EmitGoalScored",
            "EmitGoalSelected",
            "PlanStartedKernelEvent",
            "PlanCommittedKernelEvent",
            "PlanAbandonedKernelEvent",
            "KernelProgramCodes.Planning",
            "Planners",
        };
        var violations = ContainedFragments(kernelSource, forbiddenFragments);

        Assert.False(
            Directory.Exists(planningProgramPath),
            "Gambits should no longer depend on the old generic planning program.");
        Assert.Empty(violations);
    }

    [Fact]
    public void GambitRuntime_DoesNotPlanBlockedGambits()
    {
        var root = FindRepoRoot();
        var gambitRuntime = ReadAllSourcesUnder(root, "src/Engine.Kernel/Programs/Gambits");
        var forbiddenFragments = new[]
        {
            "PlanGoap",
            "PlanOpportunity",
            "EvaluateBlockedGambitPlanCandidate",
            "EvaluateBestSupportPlan",
            "CommittedPlan",
            "GoalEffect",
            "Objective",
            "SupportRule",
            "SupportExpression",
            "PlanningRequest",
            "simulation.Planners",
        };
        var violations = ContainedFragments(gambitRuntime, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void DomainBehaviorContracts_DoNotExposePlannerGoalMetadata()
    {
        var root = FindRepoRoot();
        var domainSource = ReadAllSourcesUnder(root, "src/Engine.Domain");
        var forbiddenFragments = new[]
        {
            "GoalEffects",
            "BehaviorSupportRule",
            "SupportRules",
            "PreconditionInspector",
            "BehaviorBlockers",
            "BehaviorSupportEffects",
            "BehaviorProgramEffectRule",
            "BehaviorProgramSupportRule",
        };
        var violations = ContainedFragments(domainSource, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void TelemetryAndAnalysis_DoNotExposePlannerCompatibilityTables()
    {
        var root = FindRepoRoot();
        var source = string.Concat(
            ReadAllSourcesUnder(root, "src/Engine.Eventing"),
            ReadAllSourcesUnder(root, "src/Engine.Projection"),
            ReadAllSourcesUnder(root, "src/Engine.Observability"),
            ReadAllSourcesUnder(root, "src/Engine.Observability.Export"),
            ReadAllSourcesUnder(root, "tools/MonteCarlo"));
        var forbiddenFragments = new[]
        {
            "PlanStartedKernelEvent",
            "GoalConsideredKernelEvent",
            "GoalScoredKernelEvent",
            "GoalSelectedKernelEvent",
            "PlanStepEmittedKernelEvent",
            "PlanCommittedKernelEvent",
            "PlanAbandonedKernelEvent",
            "PlannerTrace",
            "PlannerGoal",
            "PlannerCandidate",
            "PlannerScore",
            "PlannerStep",
            "PlannerLocality",
            "planner_",
            "plan_id",
            "PlanId",
            "planId",
            "goal_id",
            "GoalId",
            "goalId",
            "action_executions",
            "ActionExecutionRow",
        };
        var violations = ContainedFragments(source, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void GambitContracts_DoNotExposeSeparateRewardExpressionLane()
    {
        var root = FindRepoRoot();
        var source = string.Concat(
            ReadAllSourcesUnder(root, "src/Engine.Domain"),
            ReadAllSourcesUnder(root, "src/Engine.Host"),
            ReadAllSourcesUnder(root, "src/Engine.Kernel"));
        var forbiddenFragments = new[]
        {
            "RewardExpression",
            "AddRewardExpressionOp",
            "EvaluateReward",
            "GambitRewardExpression",
        };
        var violations = ContainedFragments(source, forbiddenFragments);

        Assert.Empty(violations);
    }

    private static DirectoryInfo FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TheCanonryGame.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    private static string ReadAllSourcesUnder(DirectoryInfo root, string relativePath)
    {
        var path = Path.Combine(root.FullName, relativePath);
        if (!Directory.Exists(path))
        {
            return string.Empty;
        }

        return string.Concat(Directory
            .EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(File.ReadAllText));
    }

    private static string[] ContainedFragments(string source, IEnumerable<string> fragments) =>
        fragments
            .Where(fragment => source.Contains(fragment, StringComparison.Ordinal))
            .ToArray();
}
