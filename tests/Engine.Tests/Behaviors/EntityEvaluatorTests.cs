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
        var goal = CreateGoal("population", self.Id, 1f);
        self.Goals.Add(goal);

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateProcess("Grow", "population"));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));
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
        self.Goals.Add(CreateGoal("population", self.Id, 0.9f));
        self.Goals.Add(CreateGoal("fame", self.Id, 0.1f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateProcess("Grow", "population"));
        definitions.Register(CreateProcess("Celebrate", "fame"));

        var bus = new MessageBus();
        var executed = new List<string>();
        bus.Subscribe(message =>
        {
            if (message is ProcessExecutedMessage processExecuted)
            {
                executed.Add(processExecuted.ProcessName);
            }
        });

        var evaluator = new EntityEvaluator(definitions, bus, new Random(42));
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
        world.CurrentTick = 7;
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f);
        var blockedGoal = CreateGoal("population", self.Id, 1f);
        var viableGoal = CreateGoal("fame", self.Id, 0.2f);
        self.Goals.Add(blockedGoal);
        self.Goals.Add(viableGoal);
        self.GoalsGeneratedAt = 7;

        var blocked = CreateProcess("Blocked", "population", scoreExpression: Expr.Lit(0f));
        var viable = CreateProcess("Celebrate", "fame");

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(blocked);
        definitions.Register(viable);

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.0));
        evaluator.Evaluate(self, world);

        Assert.Equal("Celebrate", self.LastEvaluation!.ExecutedProcessName);
        Assert.Contains(self.LastEvaluation.Candidates, c => c.GoalId == blockedGoal.Id && c.ProcessName == "Blocked");
        Assert.Contains(self.LastEvaluation.Candidates, c => c.GoalId == viableGoal.Id && c.ProcessName == "Celebrate");
        Assert.Equal(0, self.GoalsGeneratedAt);
    }

    [Fact]
    public void Evaluate_OnlyProcessesServingPrimaryGoalAreConsidered()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f);
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateProcess("Grow", "population"));
        definitions.Register(CreateProcess("Celebrate", "fame"));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.Single(self.LastEvaluation!.Candidates);
        Assert.Equal("Grow", self.LastEvaluation.Candidates[0].ProcessName);
    }

    [Fact]
    public void Evaluate_SubGoalsMatchBlockingAttributeInsteadOfParentAttribute()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, sustenance: 0f);
        self.Goals.Add(CreateGoal("sustenance", self.Id, 1f, parentGoalId: "parent"));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateProcess("Grow", "population"));
        definitions.Register(CreateProcess("Stockpile", "sustenance"));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.Single(self.LastEvaluation!.Candidates);
        Assert.Equal("Stockpile", self.LastEvaluation.Candidates[0].ProcessName);
    }

    [Fact]
    public void Evaluate_TargetIsSelfPreconditionPassesWhenProviderIsSelf()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateProcess("Grow", "population", new TargetIsSelf()));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));
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
        self.Goals.Add(CreateGoal("population", other.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateProcess("Grow", "population", new TargetIsSelf()));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));
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
        self.Goals.Add(CreateGoal("population", other.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(
            CreateProcess("Grow", "population", new SelfAttributeThreshold("sustenance", min: 3f, max: null)));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));
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
        self.Goals.Add(CreateGoal("population", other.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(
            CreateProcess("Grow", "population", new TargetAttributeThreshold("sustenance", min: 3f, max: null)));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));
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
        self.Goals.Add(CreateGoal("population", other.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(
            CreateProcess("Grow", "population", new SemanticDistanceThreshold("ideology", 0.5f)));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));
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
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateProcess("Grow", "population", cooldown: 4));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5, 0.5, 0.5));

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
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateProcess("Grow", "population"));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));

        world.CurrentTick = 0;
        evaluator.Evaluate(self, world);

        self.EntityCooldownUntil = 5;
        world.CurrentTick = 2;
        evaluator.Evaluate(self, world);

        Assert.NotNull(self.LastEvaluation);
        Assert.True(self.LastEvaluation!.IsStale);
        Assert.Equal(0, self.LastEvaluation.SourceTick);
        Assert.Single(self.LastEvaluation.Candidates);
        Assert.Equal("Grow", self.LastEvaluation.Candidates[0].ProcessName);
        Assert.Equal(2, self.LastEvaluation.Tick);
        Assert.Contains("entity cooldown until tick 5", self.LastEvaluation.StatusNote, StringComparison.Ordinal);
        Assert.Equal("Grow", self.LastEvaluation.ExecutedProcessName);
        Assert.Contains("reusing report from tick 0", self.EvaluationTrace, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluate_CooldownJitterStaysWithinPlusMinusTwentyFivePercent()
    {
        foreach (var jitterRoll in new[] { 0.0, 0.5, 1.0 })
        {
            var world = new StubWorldState(gridRadius: 5);
            var self = CreateSettlement(world, $"Self_{jitterRoll}", (0, 0), population: 10f);
            self.Goals.Add(CreateGoal("population", self.Id, 1f));

            var definitions = new BehaviorDefinitionIndex();
            definitions.Register(CreateProcess("Grow", "population", cooldown: 8));

            var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.0, 0.0, jitterRoll));
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
        self.Goals.Add(CreateGoal("population", other.Id, 1f));

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
        definition.GoalEffects.Add(AttributeRef.Self("population", EntityKind.Settlement));
        definition.Writes.Add(AttributeRef.Self("population", EntityKind.Settlement));
        definition.Mutations.Add(new ModifySelfAttributeMutation("population", 1f));
        definition.Mutations.Add(new ModifyTargetAttributeMutation("population", -2f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(definition);

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5, 0.5, 0.5));
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
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateProcess("Blocked", "population",
            new SelfAttributeThreshold("sustenance", min: 3f, max: null)));
        definitions.Register(CreateProcess("Grow", "population"));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.9));
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

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateIdleProcess("Idle"));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.Equal("Idle", self.LastEvaluation!.ExecutedProcessName);
        Assert.Equal(string.Empty, self.LastEvaluation.SelectedGoalId);
    }

    [Fact]
    public void Evaluate_WithGoalGeneratorAndNoGoals_DoesNotRunUnguidedProcesses()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateIdleProcess("Idle"));

        var kinds = new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [
                    new() { Name = "population", Min = 0f, Max = 1000f, Default = 10f, Baseline = 0f },
                ],
                HealthAttribute = "population",
            },
        };

        var generator = new GoalGenerator(kinds, [CreateIdleProcess("Idle")]);
        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5), generator);
        evaluator.Evaluate(self, world);

        Assert.NotNull(self.LastEvaluation);
        Assert.Null(self.LastEvaluation!.ExecutedProcessName);
        Assert.Equal("no planner goals", self.LastEvaluation.StatusNote);
    }

    [Fact]
    public void Evaluate_StandingDriveExecutesThroughNormalGoalPath()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 25f);

        var idleGrowth = CreateProcess("IdleGrowth", "population");

        var kinds = new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [
                    new() { Name = "population", Min = 0f, Max = 1000f, Default = 10f, Baseline = 0f },
                ],
                HealthAttribute = "population",
                StandingDrives =
                [
                    new StandingDriveConfig
                    {
                        DesiredEffect = AttributeRef.Self("population", EntityKind.Settlement),
                        BaseUrgency = 0.08f,
                    },
                ],
            },
        };

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(idleGrowth);

        var generator = new GoalGenerator(kinds, [idleGrowth]);
        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5), generator);
        evaluator.Evaluate(self, world);

        Assert.Equal("IdleGrowth", self.LastEvaluation!.ExecutedProcessName);
        Assert.False(string.IsNullOrEmpty(self.LastEvaluation.SelectedGoalId));
        Assert.NotEmpty(self.Goals);
    }

    [Fact]
    public void Evaluate_ProducesReportEvenWhenNoProcessFires()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, sustenance: 0f);
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateProcess("Blocked", "population",
            precondition: new SelfAttributeThreshold("sustenance", min: 3f, max: null)));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));
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
        self.Goals.Add(CreateGoal("population", other.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        // Grow has TargetIsSelf — should fail precondition against Other, not show "score below threshold"
        definitions.Register(CreateProcess("Grow", "population", new TargetIsSelf()));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));
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
        self.Goals.Add(CreateGoal("population", other.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        // Process with TargetIsSelf AND score=0 — precondition failure should be reported, not score
        definitions.Register(
            CreateProcess("Grow", "population", new TargetIsSelf(), scoreExpression: Expr.Lit(0f)));

        var evaluator = new EntityEvaluator(definitions, new MessageBus(), new FixedRandom(0.5));
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
        process.ProviderSelectors.Add(AttributeRef.Target("population", EntityKind.Settlement));
        // Provider score: fame difference (higher self fame relative to target = better target)
        process.RewardExpression = Expr.Sub(Expr.Self("fame"), Expr.Target("fame"));

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

        var generator = new GoalGenerator(kinds, [process]);
        var goals = generator.Generate(self, world)
            .Where(g => !g.IsSubGoal && g.DesiredEffect.Equals(AttributeRef.Self("population", EntityKind.Settlement)))
            .ToList();

        // Weak has fame 5, self has 10 → fame diff = 5 (good target)
        // Strong has fame 20, self has 10 → fame diff = -10 (bad target, excluded)
        // Self has fame 10, self → fame diff = 0 (excluded)
        // Goals should have different urgencies based on provider scores
        Assert.True(goals.Count >= 1);

        var weakGoal = goals.FirstOrDefault(g => g.TargetEntityId == weak.Id);
        Assert.NotNull(weakGoal);

        // Strong should be excluded (negative score)
        var strongGoal = goals.FirstOrDefault(g => g.TargetEntityId == strong.Id);
        Assert.Null(strongGoal);

        // Weak goal urgency should be different from base urgency (modulated by provider weight)
        var selfGoal = goals.FirstOrDefault(g => g.TargetEntityId == self.Id);
        if (selfGoal is not null && weakGoal is not null)
        {
            // They should NOT have the same urgency
            Assert.NotEqual(selfGoal.Urgency, weakGoal.Urgency);
        }
    }

    [Fact]
    public void GoalGenerator_ProcessWithoutProviderScore_DoesNotDuplicateGoals()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        CreateSettlement(world, "Other1", (1, 0), population: 10f);
        CreateSettlement(world, "Other2", (2, 0), population: 10f);

        var grow = CreateProcess("Grow", "population", new TargetIsSelf());

        var kinds = new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [
                    new() { Name = "population", Min = 0f, Max = 1000f, Default = 10f, Baseline = 20f },
                ],
                HealthAttribute = "population",
            },
        };

        var generator = new GoalGenerator(kinds, [grow]);
        var goals = generator.Generate(self, world).Where(g => !g.IsSubGoal).ToList();

        // Grow reward is Literal(1f) — all providers offer equal reward.
        // Self-only processes now emit only self goals.
        Assert.Single(goals);
        Assert.Equal(self.Id, goals[0].TargetEntityId);
    }

    [Fact]
    public void GoalGenerator_GoalsForSameEntityHaveDifferentUrgencies()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f);
        CreateSettlement(world, "Weak", (1, 0), population: 10f, fame: 3f);
        CreateSettlement(world, "Strong", (2, 0), population: 10f, fame: 20f);

        var attract = CreateProcess("Attract", "population", new TargetIsNotSelf());
        attract.ProviderSelectors.Add(AttributeRef.Target("population", EntityKind.Settlement));
        attract.RewardExpression = Expr.Sub(Expr.Self("fame"), Expr.Target("fame"));

        var grow = CreateProcess("Grow", "population", new TargetIsSelf());

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

        var generator = new GoalGenerator(kinds, [grow, attract]);
        var goals = generator.Generate(self, world).Where(g => !g.IsSubGoal).ToList();

        var urgencies = goals.Select(g => g.Urgency).Distinct().ToList();
        Assert.True(urgencies.Count > 1,
            $"All goals have same urgency: {string.Join(", ", goals.Select(g => $"{g.TargetEntityId}={g.Urgency:F3}"))}");
    }

    [Fact]
    public void GoalGenerator_RecordsTraceForPlannerExpansion()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, sustenance: 0f);

        var grow = CreateProcess(
            "Grow",
            "population",
            new SelfAttributeThreshold("sustenance", min: 3f, max: null));
        var stockpile = CreateProcess("Stockpile", "sustenance");

        var kinds = new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [
                    new() { Name = "population", Min = 0f, Max = 1000f, Default = 10f, Baseline = 20f },
                    new() { Name = "sustenance", Min = 0f, Max = 1000f, Default = 5f, Baseline = 5f },
                ],
                HealthAttribute = "population",
            },
        };

        var generator = new GoalGenerator(kinds, [grow, stockpile]);
        _ = generator.Generate(self, world);

        Assert.NotNull(self.GoalGenerationTrace);
        Assert.Contains("blocked process=Grow", self.GoalGenerationTrace, StringComparison.Ordinal);
        Assert.Contains("selected child process=STOCKPILE", self.GoalGenerationTrace, StringComparison.Ordinal);
    }

    private static ProcessDefinition CreateProcess(
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
        definition.GoalEffects.Add(AttributeRef.Self(writesAttribute, EntityKind.Settlement));
        definition.Writes.Add(AttributeRef.Self(writesAttribute, EntityKind.Settlement));
        return definition;
    }

    private static ProcessDefinition CreateIdleProcess(string name) =>
        new(
            name.ToUpperInvariant(),
            name,
            DefinitionRole.Process,
            EntityKind.Settlement,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(1f)));

    private static EntityGoal CreateGoal(
        string attribute,
        EntityId targetEntityId,
        float urgency,
        string? parentGoalId = null,
        RefScope scope = RefScope.Self)
    {
        var effect = scope switch
        {
            RefScope.Target => AttributeRef.Target(attribute, EntityKind.Settlement),
            _ => AttributeRef.Self(attribute, EntityKind.Settlement),
        };

        return new EntityGoal(effect, targetEntityId, urgency, parentGoalId: parentGoalId);
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
}
