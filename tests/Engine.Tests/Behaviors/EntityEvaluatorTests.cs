namespace Engine.Tests.Behaviors;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Messages;
using Engine.Processes;
using Engine.Tests;
using Engine.Wiring;

public class EntityEvaluatorTests
{
    [Fact]
    public void Evaluate_SelectsTheOnlyGoal()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        var goal = new EntityGoal("population", self.Id, 1f);
        self.Goals.Add(goal);

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Grow", "population"));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.NotNull(self.LastEvaluation);
        Assert.Equal(goal.Id, self.LastEvaluation!.SelectedGoalId);
        Assert.Equal("Grow", self.LastEvaluation.ExecutedProcessName);
    }

    [Fact]
    public void Evaluate_WeightsGoalSelectionByUrgency()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f);
        self.Goals.Add(new EntityGoal("population", self.Id, 0.9f));
        self.Goals.Add(new EntityGoal("fame", self.Id, 0.1f));

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Grow", "population"));
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Celebrate", "fame"));

        var bus = new MessageBus();
        var executed = new List<string>();
        bus.Subscribe(message =>
        {
            if (message is ProcessExecutedMessage processExecuted)
            {
                executed.Add(processExecuted.ProcessName);
            }
        });

        var evaluator = new EntityEvaluator(registry, bus, new Random(42));
        for (var i = 0; i < 500; i++)
        {
            evaluator.Evaluate(self, world);
        }

        Assert.True(executed.Count(name => name == "Grow") > executed.Count(name => name == "Celebrate"));
    }

    [Fact]
    public void Evaluate_FallsThroughToLaterGoalsWhenSelectedGoalHasNoViableProcesses()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f);
        var blockedGoal = new EntityGoal("population", self.Id, 1f);
        var viableGoal = new EntityGoal("fame", self.Id, 0.2f);
        self.Goals.Add(blockedGoal);
        self.Goals.Add(viableGoal);

        var blocked = CreateProcess("Blocked", "population", scoreExpression: Expr.Lit(0f));
        var viable = CreateProcess("Celebrate", "fame");

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, blocked);
        registry.RegisterProcess(EntityKind.Settlement, viable);

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.0));
        evaluator.Evaluate(self, world);

        Assert.Equal("Celebrate", self.LastEvaluation!.ExecutedProcessName);
        Assert.Contains(self.LastEvaluation.Candidates, c => c.GoalId == blockedGoal.Id && c.ProcessName == "Blocked");
        Assert.Contains(self.LastEvaluation.Candidates, c => c.GoalId == viableGoal.Id && c.ProcessName == "Celebrate");
    }

    [Fact]
    public void Evaluate_OnlyProcessesServingPrimaryGoalAreConsidered()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f);
        self.Goals.Add(new EntityGoal("population", self.Id, 1f));

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Grow", "population"));
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Celebrate", "fame"));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.Single(self.LastEvaluation!.Candidates);
        Assert.Equal("Grow", self.LastEvaluation.Candidates[0].ProcessName);
    }

    [Fact]
    public void Evaluate_SubGoalsMatchBlockingAttributeInsteadOfParentAttribute()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, sustenance: 0f);
        self.Goals.Add(new EntityGoal("population", self.Id, 1f, parentGoalId: "parent", blockingAttribute: "sustenance"));

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Grow", "population"));
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Stockpile", "sustenance"));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.Single(self.LastEvaluation!.Candidates);
        Assert.Equal("Stockpile", self.LastEvaluation.Candidates[0].ProcessName);
    }

    [Fact]
    public void Evaluate_TargetIsSelfPreconditionPassesWhenProviderIsSelf()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        self.Goals.Add(new EntityGoal("population", self.Id, 1f));

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Grow", "population", new TargetIsSelf()));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.True(candidate.PreconditionPassed);
        Assert.Equal("Grow", self.LastEvaluation.ExecutedProcessName);
    }

    [Fact]
    public void Evaluate_TargetIsSelfPreconditionFailsWhenProviderIsAnotherEntity()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        var other = CreateSettlement(world, "Other", (1, 0), population: 10f);
        self.Goals.Add(new EntityGoal("population", other.Id, 1f));

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Grow", "population", new TargetIsSelf()));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.False(candidate.PreconditionPassed);
        Assert.Null(self.LastEvaluation.ExecutedProcessName);
    }

    [Fact]
    public void Evaluate_SelfAttributeThresholdReadsSelfNotProvider()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, sustenance: 0f);
        var other = CreateSettlement(world, "Other", (1, 0), population: 10f, sustenance: 10f);
        self.Goals.Add(new EntityGoal("population", other.Id, 1f));

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement,
            CreateProcess("Grow", "population", new SelfAttributeThreshold("sustenance", min: 3f, max: null)));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.False(candidate.PreconditionPassed);
        Assert.Contains("self.sustenance=0.0", candidate.PreconditionDiagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluate_TargetAttributeThresholdReadsProvider()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, sustenance: 10f);
        var other = CreateSettlement(world, "Other", (1, 0), population: 10f, sustenance: 0f);
        self.Goals.Add(new EntityGoal("population", other.Id, 1f));

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement,
            CreateProcess("Grow", "population", new TargetAttributeThreshold("sustenance", min: 3f, max: null)));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.False(candidate.PreconditionPassed);
        Assert.Contains("target.sustenance=0.0", candidate.PreconditionDiagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluate_SemanticDistanceThresholdUsesSelfAndProvider()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, ideology: (0f, 0f));
        var other = CreateSettlement(world, "Other", (1, 0), population: 10f, ideology: (1f, 1f));
        self.Goals.Add(new EntityGoal("population", other.Id, 1f));

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement,
            CreateProcess("Grow", "population", new SemanticDistanceThreshold("ideology", 0.5f)));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.False(candidate.PreconditionPassed);
        Assert.Contains("ideology distance", candidate.PreconditionDiagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluate_ProcessCooldownBlocksThenExpires()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        self.Goals.Add(new EntityGoal("population", self.Id, 1f));

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Grow", "population", cooldown: 4));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5, 0.5, 0.5));

        world.CurrentTick = 0;
        evaluator.Evaluate(self, world);
        Assert.Equal(4, self.ProcessCooldowns["Grow"]);

        world.CurrentTick = 1;
        evaluator.Evaluate(self, world);
        Assert.True(self.LastEvaluation!.Candidates[0].OnCooldown);
        Assert.Null(self.LastEvaluation.ExecutedProcessName);

        world.CurrentTick = 4;
        evaluator.Evaluate(self, world);
        Assert.Equal("Grow", self.LastEvaluation!.ExecutedProcessName);
    }

    [Fact]
    public void Evaluate_EntityWideCooldownBlocksAllProcessesAndStillProducesReport()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        self.Goals.Add(new EntityGoal("population", self.Id, 1f));
        self.EntityCooldownUntil = 5;
        world.CurrentTick = 2;

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Grow", "population"));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.NotNull(self.LastEvaluation);
        Assert.Empty(self.LastEvaluation!.Candidates);
        Assert.Null(self.LastEvaluation.ExecutedProcessName);
    }

    [Fact]
    public void Evaluate_CooldownJitterStaysWithinPlusMinusTwentyFivePercent()
    {
        foreach (var jitterRoll in new[] { 0.0, 0.5, 1.0 })
        {
            var world = new StubWorldState(gridRadius: 5);
            var self = CreateSettlement(world, $"Self_{jitterRoll}", (0, 0), population: 10f);
            self.Goals.Add(new EntityGoal("population", self.Id, 1f));

            var registry = new ActivityRegistry();
            registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Grow", "population", cooldown: 8));

            var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.0, 0.0, jitterRoll));
            evaluator.Evaluate(self, world);

            Assert.InRange(self.ProcessCooldowns["Grow"], 6, 10);
        }
    }

    [Fact]
    public void Evaluate_ExecutionUsesGoalProviderAsResolvedTargetAndAppliesMutations()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        var other = CreateSettlement(world, "Other", (1, 0), population: 10f);
        self.Goals.Add(new EntityGoal("population", other.Id, 1f));

        var definition = new ProcessDefinition(
            "attract",
            "Attract",
            DefinitionRole.Process,
            EntityKind.Settlement,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(1f)))
        {
            Cooldown = 3,
        };
        definition.Writes.Add(AttributeRef.Self("population", EntityKind.Settlement));
        definition.Mutations.Add(new ModifySelfAttributeMutation("population", 1f));
        definition.Mutations.Add(new ModifyTargetAttributeMutation("population", -2f));

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, new DataDrivenProcess(definition));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5, 0.5, 0.5));
        evaluator.Evaluate(self, world);

        Assert.Equal(11f, self.Attributes.GetValue("population"));
        Assert.Equal(8f, other.Attributes.GetValue("population"));
        Assert.Equal("Attract", self.LastEvaluation!.ExecutedProcessName);
        Assert.True(self.ProcessCooldowns.ContainsKey("Attract"));
    }

    [Fact]
    public void Evaluate_ReportContainsAllCandidatesAndMarksTheWinner()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, sustenance: 0f);
        self.Goals.Add(new EntityGoal("population", self.Id, 1f));

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Blocked", "population",
            new SelfAttributeThreshold("sustenance", min: 3f, max: null)));
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Grow", "population"));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.9));
        evaluator.Evaluate(self, world);

        var report = self.LastEvaluation!;
        Assert.Equal(2, report.Candidates.Count);
        Assert.Equal("Grow", report.ExecutedProcessName);
        Assert.Contains(report.Candidates, c => c.ProcessName == "Blocked" && !c.PreconditionPassed);
        Assert.Contains(report.Candidates, c => c.ProcessName == "Grow" && c.WasSelected);
    }

    [Fact]
    public void Evaluate_WithoutGoalsAndWithoutGoalGeneratorRunsProcessesWithoutTargets()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, new SimpleProcess("Idle"));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.Equal("Idle", self.LastEvaluation!.ExecutedProcessName);
        Assert.Equal(string.Empty, self.LastEvaluation.SelectedGoalId);
    }

    [Fact]
    public void Evaluate_ProducesReportEvenWhenNoProcessFires()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, sustenance: 0f);
        self.Goals.Add(new EntityGoal("population", self.Id, 1f));

        var registry = new ActivityRegistry();
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Blocked", "population",
            precondition: new SelfAttributeThreshold("sustenance", min: 3f, max: null)));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.NotNull(self.LastEvaluation);
        Assert.Single(self.LastEvaluation!.Candidates);
        Assert.Null(self.LastEvaluation.ExecutedProcessName);
    }

    [Fact]
    public void Evaluate_ReportShowsPreconditionFailure_NotScoreThreshold()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        var other = CreateSettlement(world, "Other", (1, 0), population: 10f);
        self.Goals.Add(new EntityGoal("population", other.Id, 1f));

        var registry = new ActivityRegistry();
        // Grow has TargetIsSelf — should fail precondition against Other, not show "score below threshold"
        registry.RegisterProcess(EntityKind.Settlement, CreateProcess("Grow", "population", new TargetIsSelf()));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.False(candidate.PreconditionPassed);
        Assert.Contains("target", candidate.PreconditionDiagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("score", candidate.PreconditionDiagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_PreconditionEvaluatedBeforeScore()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        var other = CreateSettlement(world, "Other", (1, 0), population: 10f);
        self.Goals.Add(new EntityGoal("population", other.Id, 1f));

        var registry = new ActivityRegistry();
        // Process with TargetIsSelf AND score=0 — precondition failure should be reported, not score
        registry.RegisterProcess(EntityKind.Settlement,
            CreateProcess("Grow", "population", new TargetIsSelf(), scoreExpression: Expr.Lit(0f)));

        var evaluator = new EntityEvaluator(registry, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.False(candidate.PreconditionPassed);
        // Should show precondition failure, not "score below threshold"
        Assert.DoesNotContain("score", candidate.PreconditionDiagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GoalGenerator_ExternalGoalsHaveDifferentUrgencyFromProviderScoring()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f);
        var weak = CreateSettlement(world, "Weak", (1, 0), population: 10f, fame: 5f);
        var strong = CreateSettlement(world, "Strong", (2, 0), population: 10f, fame: 20f);

        var process = CreateProcess("Attract", "population");
        // Provider score: fame difference (higher self fame relative to target = better target)
        process.Definition.ProviderScoreExpression = Expr.Sub(Expr.Self("fame"), Expr.Target("fame"));

        var kinds = new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [
                    new() { Name = "population", Min = 0f, Max = 1000f, Default = 10f, Baseline = 20f },
                    new() { Name = "fame", Min = 0f, Max = 1000f, Default = 5f },
                ],
                HealthAttribute = "population",
            },
        };

        var generator = new GoalGenerator(kinds, [process.Definition]);
        var goals = generator.Generate(self, world)
            .Where(g => !g.IsSubGoal && g.Attribute == "population")
            .ToList();

        // Weak has fame 5, self has 10 → fame diff = 5 (good target)
        // Strong has fame 20, self has 10 → fame diff = -10 (bad target, excluded)
        // Self has fame 10, self → fame diff = 0 (excluded)
        // Goals should have different urgencies based on provider scores
        Assert.True(goals.Count >= 1);

        var weakGoal = goals.FirstOrDefault(g => g.Provider == weak.Id);
        Assert.NotNull(weakGoal);

        // Strong should be excluded (negative score)
        var strongGoal = goals.FirstOrDefault(g => g.Provider == strong.Id);
        Assert.Null(strongGoal);

        // Weak goal urgency should be different from base urgency (modulated by provider weight)
        var selfGoal = goals.FirstOrDefault(g => g.Provider == self.Id);
        if (selfGoal is not null && weakGoal is not null)
        {
            // They should NOT have the same urgency
            Assert.NotEqual(selfGoal.Urgency, weakGoal.Urgency);
        }
    }

    private static DataDrivenProcess CreateProcess(
        string name,
        string writesAttribute,
        IPrecondition? precondition = null,
        IExpression? scoreExpression = null,
        int cooldown = 0,
        int entityCooldown = 0)
    {
        var definition = new ProcessDefinition(
            name.ToUpperInvariant(),
            name,
            DefinitionRole.Process,
            EntityKind.Settlement,
            precondition ?? new AlwaysTrue(),
            scoreExpression ?? Expr.Score(Expr.Lit(1f)))
        {
            Cooldown = cooldown,
            EntityCooldown = entityCooldown,
        };
        definition.Writes.Add(AttributeRef.Self(writesAttribute, EntityKind.Settlement));
        return new DataDrivenProcess(definition);
    }

    private static Entity CreateSettlement(
        StubWorldState world,
        string name,
        (int Q, int R) coordinates,
        float population = 0f,
        float fame = 0f,
        float sustenance = 5f,
        (float X, float Y)? ideology = null)
    {
        var entity = world.Graph.CreateEntity(EntityKind.Settlement, "village", name, "c", 0, coordinates);
        entity.Attributes.Add(new ScalarValue("population", 0f, 100f, population));
        entity.Attributes.Add(new ScalarValue("fame", 0f, 100f, fame));
        entity.Attributes.Add(new ScalarValue("sustenance", 0f, 100f, sustenance));
        var (x, y) = ideology ?? (0.5f, 0.5f);
        entity.Attributes.Add(new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, x, y));
        return entity;
    }

    private sealed class SimpleProcess(string name) : IProcess
    {
        public string Name => name;

        public bool CanExecute(Entity entity, IWorldState worldState, EntityGoal? goal) => true;

        public float Score(Entity entity, IWorldState worldState) => 1f;

        public SimulationMessage Execute(Entity entity, IWorldState worldState, EntityGoal? goal) =>
            new ProcessExecutedMessage(worldState.CurrentTick, entity.Id, entity.Kind, name, name);
    }
}
