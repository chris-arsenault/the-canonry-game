namespace Engine.Tests.Behaviors;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Domain;
using Engine.Domain.Config;
using Engine.Domain.Compilation;
using Engine.Eventing;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Tests;
using Engine.Wiring;

public class EntityEvaluatorTests
{
    private static readonly DomainIdRegistry TestDomainIds = CreateTestDomainIds();

    [Fact]
    public void Evaluate_RecencyDecay_ShiftsRepeatedWinnerToAlternateProcess()
    {
        // Two viable processes with equal raw score. Without recency decay the
        // weighted random would pick both roughly 50/50. With decay, repeat firings
        // of the same process drop its score — so the loser rebounds and both
        // processes end up fairly close to 50/50, but NEITHER process ever fires
        // more than ~15 times in any 10-tick window (decay keeps it dominated).
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f);
        self.Goals.Add(CreateGoal("population", self.Id, 0.5f));
        self.Goals.Add(CreateGoal("fame", self.Id, 0.5f));

        var definitions = new BehaviorDefinitionIndex();
        var grow = CreateProcess("Grow", "population");
        var celebrate = CreateProcess("Celebrate", "fame");
        definitions.Register(grow);
        definitions.Register(celebrate);

        var kernelEvents = new KernelEventLog(256);
        var cursor = kernelEvents.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var evaluator = CreateEvaluator(definitions, random: new Random(42), kernelEvents: kernelEvents);
        for (var i = 0; i < 100; i++)
        {
            world.CurrentTick = i + 1;
            evaluator.Evaluate(self, world);
        }

        var executed = ReadProcessExecutions(kernelEvents, cursor)
            .Select(process => (process.Tick, ProcessId: new ProcessRuntimeId(process.ProcessId)))
            .ToList();

        // With decay on, no process should monopolize a 10-tick window.
        for (var startTick = 1; startTick <= 91; startTick++)
        {
            var window = executed.Where(e => e.Tick >= startTick && e.Tick < startTick + 10).ToList();
            var growInWindow = window.Count(e => e.ProcessId == grow.RuntimeId);
            var celebrateInWindow = window.Count(e => e.ProcessId == celebrate.RuntimeId);
            Assert.True(growInWindow <= 7,
                $"Grow fired {growInWindow} times in ticks [{startTick},{startTick + 10}) — recency decay should cap sustained repetition.");
            Assert.True(celebrateInWindow <= 7,
                $"Celebrate fired {celebrateInWindow} times in ticks [{startTick},{startTick + 10}) — recency decay should cap sustained repetition.");
        }
    }

    [Fact]
    public void Evaluate_SelectsTheOnlyGoal()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        var goal = CreateGoal("population", self.Id, 1f);
        self.Goals.Add(goal);

        var definitions = new BehaviorDefinitionIndex();
        var grow = CreateProcess("Grow", "population");
        definitions.Register(grow);

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.NotNull(self.LastEvaluation);
        Assert.Equal(goal.Id, self.LastEvaluation!.SelectedGoalId);
        Assert.Equal(grow.RuntimeId, self.LastEvaluation.ExecutedProcessId);
    }

    [Fact]
    public void Evaluate_WritesPrimitiveKernelEventsForProcessExecutionAndEvaluation()
    {
        var world = new StubWorldState(gridRadius: 5);
        world.CurrentTick = 7;
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        var grow = CreateProcess("Grow", "population");
        definitions.Register(grow);

        var kernelEvents = new KernelEventLog(8, KernelEventOverflowPolicy.RejectWrite);
        var cursor = kernelEvents.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var evaluator = CreateEvaluator(
            definitions,
            random: new FixedRandom(0.5),
            kernelEvents: kernelEvents);

        evaluator.Evaluate(self, world);

        Assert.True(kernelEvents.TryRead(cursor, out var executedEvent));
        Assert.Equal(KernelEventType.ProcessExecuted, executedEvent.Event.EventType);
        var executed = executedEvent.Event.AsProcessExecuted();
        Assert.Equal(7, executed.Tick);
        Assert.Equal(self.Id.Value, executed.ActorId);
        Assert.Equal(TestDomainIds.KindIdOf(self.Kind).Value, executed.ActorKindId);
        Assert.Equal(grow.RuntimeId.Value, executed.ProcessId);
        Assert.Equal(KernelEventState.Succeeded, executed.State);

        Assert.True(kernelEvents.TryRead(cursor, out var evaluatedEvent));
        Assert.Equal(KernelEventType.ProcessEvaluated, evaluatedEvent.Event.EventType);
        var evaluated = evaluatedEvent.Event.AsProcessEvaluated();
        Assert.Equal(self.Id.Value, evaluated.EntityId);
        Assert.Equal(grow.RuntimeId.Value, evaluated.ExecutedProcessId);
        Assert.Equal(KernelEventState.Succeeded, evaluated.State);
        Assert.True(evaluated.WorldVersion >= executed.WorldVersion);
    }

    [Fact]
    public void Evaluate_FailsWhenPrimitiveKernelEventWriterRejectsEvent()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateProcess("Grow", "population"));

        var kernelEvents = new KernelEventLog(1, KernelEventOverflowPolicy.RejectWrite);
        _ = kernelEvents.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var evaluator = CreateEvaluator(
            definitions,
            random: new FixedRandom(0.5),
            kernelEvents: kernelEvents);

        Assert.Throws<InvalidOperationException>(() => evaluator.Evaluate(self, world));
        Assert.Equal(1, kernelEvents.RejectedWriteCount);
    }

    [Fact]
    public void Evaluate_WeightsGoalSelectionByUrgency()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f);
        self.Goals.Add(CreateGoal("population", self.Id, 0.9f));
        self.Goals.Add(CreateGoal("fame", self.Id, 0.1f));

        var definitions = new BehaviorDefinitionIndex();
        var grow = CreateProcess("Grow", "population");
        var celebrate = CreateProcess("Celebrate", "fame");
        definitions.Register(grow);
        definitions.Register(celebrate);

        var kernelEvents = new KernelEventLog(256);
        var cursor = kernelEvents.CreateCursor(KernelEventCursorStart.EarliestAvailable);

        // Turn off recency decay: this test is about urgency-weighted goal selection,
        // not the anti-repetition penalty (which is exercised elsewhere).
        var evaluator = CreateEvaluator(definitions, random: new Random(42), kernelEvents: kernelEvents);
        evaluator.RecencyDecayFactor = 1f;
        for (var i = 0; i < 500; i++)
        {
            evaluator.Evaluate(self, world);
        }

        var executed = ReadProcessExecutions(kernelEvents, cursor)
            .Select(process => new ProcessRuntimeId(process.ProcessId))
            .ToList();

        Assert.True(executed.Count(id => id == grow.RuntimeId) > executed.Count(id => id == celebrate.RuntimeId));
    }

    [Fact]
    public void Evaluate_LocalityBundleBiasesGoalSelectionTowardMultiGoalDestination()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f, sustenance: 10f);
        var farA = CreateSettlement(world, "FarA", (2, 0), population: 10f, fame: 10f, sustenance: 10f);
        var farB = CreateSettlement(world, "FarB", (2, 0), population: 10f, fame: 10f, sustenance: 10f);
        var near = CreateSettlement(world, "Near", (1, 0), population: 10f, fame: 10f, sustenance: 10f);

        self.Goals.Add(CreateGoal("population", farA.Id, 0.6f));
        self.Goals.Add(CreateGoal("fame", farB.Id, 0.6f));
        self.Goals.Add(CreateGoal("sustenance", near.Id, 1.0f));

        var definitions = new BehaviorDefinitionIndex();
        var grow = CreateProcess("Grow", "population", new TargetIsNotSelf());
        var celebrate = CreateProcess("Celebrate", "fame", new TargetIsNotSelf());
        var stockpile = CreateProcess("Stockpile", "sustenance", new TargetIsNotSelf());
        definitions.Register(grow);
        definitions.Register(celebrate);
        definitions.Register(stockpile);

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.6));
        evaluator.Evaluate(self, world);

        Assert.NotNull(self.LastEvaluation);
        Assert.NotNull(self.LastEvaluation!.SelectedLocality);
        Assert.Equal(PrimaryPlane.Physical, self.LastEvaluation.SelectedLocality!.Plane);
        Assert.Equal(2, self.LastEvaluation.SelectedLocality.Q);
        Assert.Equal(0, self.LastEvaluation.SelectedLocality.R);
        Assert.Equal(2, self.LastEvaluation.SelectedLocality.GoalCount);
        Assert.True(
            self.LastEvaluation.ExecutedProcessId == grow.RuntimeId ||
            self.LastEvaluation.ExecutedProcessId == celebrate.RuntimeId,
            $"Expected locality-selected process to come from the bundled destination, got '{self.LastEvaluation.ExecutedProcessId}'.");
        Assert.NotNull(self.ActiveVisitIntent);
        Assert.Equal(2, self.ActiveVisitIntent!.Q);
        Assert.Equal(0, self.ActiveVisitIntent.R);
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

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.0));
        evaluator.Evaluate(self, world);

        Assert.Equal(viable.RuntimeId, self.LastEvaluation!.ExecutedProcessId);
        Assert.Contains(self.LastEvaluation.Candidates, c => c.GoalId == blockedGoal.Id && c.ProcessId == blocked.RuntimeId);
        Assert.Contains(self.LastEvaluation.Candidates, c => c.GoalId == viableGoal.Id && c.ProcessId == viable.RuntimeId);
        Assert.Equal(0, self.GoalsGeneratedAt);
    }

    [Fact]
    public void Evaluate_OnlyProcessesServingPrimaryGoalAreConsidered()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f);
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        var grow = CreateProcess("Grow", "population");
        definitions.Register(grow);
        definitions.Register(CreateProcess("Celebrate", "fame"));

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.Single(self.LastEvaluation!.Candidates);
        Assert.Equal(grow.RuntimeId, self.LastEvaluation.Candidates[0].ProcessId);
    }

    [Fact]
    public void Evaluate_SubGoalsMatchBlockingAttributeInsteadOfParentAttribute()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, sustenance: 0f);
        self.Goals.Add(CreateGoal("sustenance", self.Id, 1f, parentGoalId: new GoalRuntimeId(999)));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(CreateProcess("Grow", "population"));
        var stockpile = CreateProcess("Stockpile", "sustenance");
        definitions.Register(stockpile);

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.Single(self.LastEvaluation!.Candidates);
        Assert.Equal(stockpile.RuntimeId, self.LastEvaluation.Candidates[0].ProcessId);
    }

    [Fact]
    public void Evaluate_TargetIsSelfPreconditionPassesWhenProviderIsSelf()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        var grow = CreateProcess("Grow", "population", new TargetIsSelf());
        definitions.Register(grow);

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.True(candidate.PreconditionPassed);
        Assert.Equal(grow.RuntimeId, self.LastEvaluation.ExecutedProcessId);
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

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.False(candidate.PreconditionPassed);
        Assert.Null(self.LastEvaluation.ExecutedProcessId);
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

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.False(candidate.PreconditionPassed);
        Assert.Empty(candidate.PreconditionDiagnostic);
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

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.False(candidate.PreconditionPassed);
        Assert.Empty(candidate.PreconditionDiagnostic);
    }

    [Fact]
    public void Evaluate_ProcessCooldownBlocksThenExpires()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        var grow = CreateProcess("Grow", "population", cooldown: 4);
        definitions.Register(grow);

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5, 0.5, 0.5));

        world.CurrentTick = 0;
        evaluator.Evaluate(self, world);
        Assert.Equal(4, self.ProcessCooldowns[grow.RuntimeId]);

        world.CurrentTick = 1;
        evaluator.Evaluate(self, world);
        Assert.True(self.LastEvaluation!.Candidates[0].OnCooldown);
        Assert.Null(self.LastEvaluation.ExecutedProcessId);

        world.CurrentTick = 4;
        evaluator.Evaluate(self, world);
        Assert.Equal(grow.RuntimeId, self.LastEvaluation!.ExecutedProcessId);
    }

    [Fact]
    public void Evaluate_EntityWideCooldownBlocksAllProcessesAndStillProducesReport()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        var grow = CreateProcess("Grow", "population");
        definitions.Register(grow);

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5));

        world.CurrentTick = 0;
        evaluator.Evaluate(self, world);

        self.EntityCooldownUntil = 5;
        world.CurrentTick = 2;
        evaluator.Evaluate(self, world);

        Assert.NotNull(self.LastEvaluation);
        Assert.True(self.LastEvaluation!.IsStale);
        Assert.Equal(0, self.LastEvaluation.SourceTick);
        Assert.Single(self.LastEvaluation.Candidates);
        Assert.Equal(grow.RuntimeId, self.LastEvaluation.Candidates[0].ProcessId);
        Assert.Equal(2, self.LastEvaluation.Tick);
        Assert.Contains("entity cooldown until tick 5", self.LastEvaluation.StatusNote, StringComparison.Ordinal);
        Assert.Equal(grow.RuntimeId, self.LastEvaluation.ExecutedProcessId);
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
            var grow = CreateProcess("Grow", "population", cooldown: 8);
            definitions.Register(grow);

            var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.0, 0.0, jitterRoll));
            evaluator.Evaluate(self, world);

            Assert.InRange(self.ProcessCooldowns[grow.RuntimeId], 6, 10);
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
            TestKinds.Settlement,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(1f)))
        {
            Cooldown = 3,
        };
        definition.GoalEffects.Add(AttributeRef.Self("population", TestKinds.Settlement));
        definition.Writes.Add(AttributeRef.Self("population", TestKinds.Settlement));
        definition.Mutations.Add(new ModifySelfAttributeMutation("population", 1f));
        definition.Mutations.Add(new ModifyTargetAttributeMutation("population", -2f));

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(definition);

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5, 0.5, 0.5));
        evaluator.Evaluate(self, world);

        Assert.Equal(11f, self.Attributes.GetValue("population"));
        Assert.Equal(8f, other.Attributes.GetValue("population"));
        Assert.Equal(definition.RuntimeId, self.LastEvaluation!.ExecutedProcessId);
        Assert.True(self.ProcessCooldowns.ContainsKey(definition.RuntimeId));
    }

    [Fact]
    public void Evaluate_ReportContainsAllCandidatesAndMarksTheWinner()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, sustenance: 0f);
        self.Goals.Add(CreateGoal("population", self.Id, 1f));

        var definitions = new BehaviorDefinitionIndex();
        var blocked = CreateProcess("Blocked", "population",
            new SelfAttributeThreshold("sustenance", min: 3f, max: null));
        var grow = CreateProcess("Grow", "population");
        definitions.Register(blocked);
        definitions.Register(grow);

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.9));
        evaluator.Evaluate(self, world);

        var report = self.LastEvaluation!;
        Assert.Equal(2, report.Candidates.Count);
        Assert.Equal(grow.RuntimeId, report.ExecutedProcessId);
        Assert.Contains(report.Candidates, c => c.ProcessId == blocked.RuntimeId && !c.PreconditionPassed);
        Assert.Contains(report.Candidates, c => c.ProcessId == grow.RuntimeId && c.WasSelected);
    }

    [Fact]
    public void Evaluate_WithoutGoalsAndWithoutGoalGeneratorRunsProcessesWithoutTargets()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);

        var definitions = new BehaviorDefinitionIndex();
        var idle = CreateIdleProcess("Idle");
        definitions.Register(idle);

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.Equal(idle.RuntimeId, self.LastEvaluation!.ExecutedProcessId);
        Assert.Null(self.LastEvaluation.SelectedGoalId);
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
            [TestKinds.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [
                    new() { Name = "population", Min = 0f, Max = 1000f, Default = 10f, Baseline = 0f },
                ],
                HealthAttribute = "population",
                PrimaryPlane = PrimaryPlane.Physical,
            },
        };

        var generator = new GoalGenerator(kinds, [CreateIdleProcess("Idle")]);
        var evaluator = CreateEvaluator(
            definitions,
            random: new FixedRandom(0.5),
            goalGenerator: generator);
        evaluator.Evaluate(self, world);

        Assert.NotNull(self.LastEvaluation);
        Assert.Null(self.LastEvaluation!.ExecutedProcessId);
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
            [TestKinds.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [
                    new() { Name = "population", Min = 0f, Max = 1000f, Default = 10f, Baseline = 0f },
                ],
                HealthAttribute = "population",
                PrimaryPlane = PrimaryPlane.Physical,
                StandingDrives =
                [
                    new StandingDriveConfig
                    {
                        DesiredEffect = AttributeRef.Self("population", TestKinds.Settlement),
                        BaseUrgency = 0.08f,
                    },
                ],
            },
        };

        var definitions = new BehaviorDefinitionIndex();
        definitions.Register(idleGrowth);

        var generator = new GoalGenerator(kinds, [idleGrowth]);
        var evaluator = CreateEvaluator(
            definitions,
            random: new FixedRandom(0.5),
            goalGenerator: generator);
        evaluator.Evaluate(self, world);

        Assert.Equal(idleGrowth.RuntimeId, self.LastEvaluation!.ExecutedProcessId);
        Assert.NotNull(self.LastEvaluation.SelectedGoalId);
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

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        Assert.NotNull(self.LastEvaluation);
        Assert.Single(self.LastEvaluation!.Candidates);
        Assert.Null(self.LastEvaluation.ExecutedProcessId);
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

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.False(candidate.PreconditionPassed);
        Assert.Empty(candidate.PreconditionDiagnostic);
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

        var evaluator = CreateEvaluator(definitions, random: new FixedRandom(0.5));
        evaluator.Evaluate(self, world);

        var candidate = Assert.Single(self.LastEvaluation!.Candidates);
        Assert.False(candidate.PreconditionPassed);
        Assert.Empty(candidate.PreconditionDiagnostic);
    }

    [Fact]
    public void GoalGenerator_ExternalGoalsHaveDifferentUrgencyFromProviderScoring()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f);
        var weak = CreateSettlement(world, "Weak", (1, 0), population: 10f, fame: 5f);
        var strong = CreateSettlement(world, "Strong", (2, 0), population: 10f, fame: 20f);

        var process = CreateProcess("Attract", "population");
        process.ProviderSelectors.Add(AttributeRef.Target("population", TestKinds.Settlement));
        // Provider score: fame difference (higher self fame relative to target = better target)
        process.RewardExpression = Expr.Sub(Expr.Self("fame"), Expr.Target("fame"));

        var kinds = new Dictionary<EntityKind, KindConfig>
        {
            [TestKinds.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [
                    new() { Name = "population", Min = 0f, Max = 1000f, Default = 10f, Baseline = 20f },
                    new() { Name = "fame", Min = 0f, Max = 1000f, Default = 5f },
                ],
                HealthAttribute = "population",
                PrimaryPlane = PrimaryPlane.Physical,
            },
        };

        var generator = new GoalGenerator(kinds, [process]);
        var goals = generator.Generate(self, world)
            .Where(g => !g.IsSubGoal && g.DesiredEffect.Equals(AttributeRef.Self("population", TestKinds.Settlement)))
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
            [TestKinds.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [
                    new() { Name = "population", Min = 0f, Max = 1000f, Default = 10f, Baseline = 20f },
                ],
                HealthAttribute = "population",
                PrimaryPlane = PrimaryPlane.Physical,
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
    public void GoalGenerator_CommitsToSingleGoalPerDesiredEffect()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 10f);
        var weak = CreateSettlement(world, "Weak", (1, 0), population: 10f, fame: 3f);
        var strong = CreateSettlement(world, "Strong", (2, 0), population: 10f, fame: 20f);

        var attract = CreateProcess("Attract", "population", new TargetIsNotSelf());
        attract.ProviderSelectors.Add(AttributeRef.Target("population", TestKinds.Settlement));
        attract.RewardExpression = Expr.Sub(Expr.Self("fame"), Expr.Target("fame"));

        var grow = CreateProcess("Grow", "population", new TargetIsSelf());

        var kinds = new Dictionary<EntityKind, KindConfig>
        {
            [TestKinds.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [
                    new() { Name = "population", Min = 0f, Max = 1000f, Default = 10f, Baseline = 20f },
                    new() { Name = "fame", Min = 0f, Max = 1000f, Default = 5f },
                ],
                HealthAttribute = "population",
                PrimaryPlane = PrimaryPlane.Physical,
            },
        };

        var generator = new GoalGenerator(kinds, [grow, attract]);
        var goals = generator.Generate(self, world).Where(g => !g.IsSubGoal).ToList();

        var goal = Assert.Single(goals);
        Assert.DoesNotContain(goals, g => g.TargetEntityId == strong.Id);
        Assert.True(goal.TargetEntityId == self.Id || goal.TargetEntityId == weak.Id,
            $"Unexpected committed goal target: {goal.TargetEntityId}");
    }

    [Fact]
    public void GoalGenerator_ProducesPrimitivePlannerExpansion()
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
            [TestKinds.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [
                    new() { Name = "population", Min = 0f, Max = 1000f, Default = 10f, Baseline = 20f },
                    new() { Name = "sustenance", Min = 0f, Max = 1000f, Default = 5f, Baseline = 5f },
                ],
                HealthAttribute = "population",
                PrimaryPlane = PrimaryPlane.Physical,
            },
        };

        var generator = new GoalGenerator(kinds, [grow, stockpile]);
        var goals = generator.Generate(self, world);

        Assert.Contains(goals, g => !g.IsActionable && g.PlannedProcessId == grow.RuntimeId);
        Assert.Contains(goals, g => g.IsActionable && g.PlannedProcessId == stockpile.RuntimeId);
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
            TestKinds.Settlement,
            precondition ?? new AlwaysTrue(),
            scoreExpression ?? Expr.Score(Expr.Lit(1f)))
        {
            Cooldown = cooldown,
            EntityCooldown = entityCooldown,
        };
        definition.GoalEffects.Add(AttributeRef.Self(writesAttribute, TestKinds.Settlement));
        definition.Writes.Add(AttributeRef.Self(writesAttribute, TestKinds.Settlement));
        return definition;
    }

    private static ProcessDefinition CreateIdleProcess(string name) =>
        new(
            name.ToUpperInvariant(),
            name,
            DefinitionRole.Process,
            TestKinds.Settlement,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(1f)));

    private static EntityGoal CreateGoal(
        string attribute,
        EntityId targetEntityId,
        float urgency,
        GoalRuntimeId? parentGoalId = null,
        RefScope scope = RefScope.Self)
    {
        var effect = scope switch
        {
            RefScope.Target => AttributeRef.Target(attribute, TestKinds.Settlement),
            _ => AttributeRef.Self(attribute, TestKinds.Settlement),
        };

        return new EntityGoal(effect, targetEntityId, urgency, parentGoalId: parentGoalId);
    }

    private static EntityEvaluator CreateEvaluator(
        BehaviorDefinitionIndex definitions,
        Random? random = null,
        GoalGenerator? goalGenerator = null,
        IReadOnlyDictionary<EntityKind, KindConfig>? kindConfigs = null,
        IKernelEventWriter? kernelEvents = null)
    {
        return new EntityEvaluator(
            definitions,
            random: random,
            goalGenerator: goalGenerator,
            kindConfigs: kindConfigs,
            kernelEvents: kernelEvents ?? new KernelEventLog(1024),
            domainIds: TestDomainIds);
    }

    private static DomainIdRegistry CreateTestDomainIds()
    {
        var ids = new DomainIdRegistry();
        ids.RegisterKind(TestKinds.Settlement);
        ids.RegisterSourceCode(DomainRuntimeNames.ProcessSourceCode);
        ids.RegisterSourceCode(DomainRuntimeNames.CardSourceCode);
        ids.RegisterAttribute("population");
        ids.RegisterAttribute("fame");
        ids.RegisterAttribute("sustenance");
        return ids;
    }

    private static List<ProcessExecutedKernelEvent> ReadProcessExecutions(
        KernelEventLog kernelEvents,
        KernelEventCursor cursor)
    {
        var executed = new List<ProcessExecutedKernelEvent>();
        while (kernelEvents.TryRead(cursor, out var sequenced))
        {
            if (sequenced.Event.EventType == KernelEventType.ProcessExecuted)
            {
                executed.Add(sequenced.Event.AsProcessExecuted());
            }
        }

        return executed;
    }

    private static Entity CreateSettlement(
        StubWorldState world,
        string name,
        (int Q, int R) coordinates,
        float population = 0f,
        float fame = 0f,
        float sustenance = 5f)
    {
        var entity = world.Graph.CreateEntity(TestKinds.Settlement, "village", name, "c", 0, coordinates);
        entity.Attributes.Add(new ScalarValue(
            "population",
            0f,
            100f,
            population,
            TestDomainIds.AttributeIdOf("population")));
        entity.Attributes.Add(new ScalarValue(
            "fame",
            0f,
            100f,
            fame,
            TestDomainIds.AttributeIdOf("fame")));
        entity.Attributes.Add(new ScalarValue(
            "sustenance",
            0f,
            100f,
            sustenance,
            TestDomainIds.AttributeIdOf("sustenance")));
        return entity;
    }
}
