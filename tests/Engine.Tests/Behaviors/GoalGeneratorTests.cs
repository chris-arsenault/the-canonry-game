namespace Engine.Tests.Behaviors;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Pressure;
using Engine.Processes;
using Engine.Resources;
using Engine.Tests;
using Engine.Wiring;

public class GoalGeneratorTests
{
    [Theory]
    [InlineData(10f, 1.12f)]
    [InlineData(20f, 0.84f)]
    [InlineData(100f, 0.28f)]
    public void Generate_UsesBaselineUrgencyCurve(float population, float expectedUrgency)
    {
        var world = new StubWorldState(gridRadius: 5);
        var entity = CreateSettlement(world, "Self", population: population);
        var process = CreateProcess("grow", "population");
        var generator = new GoalGenerator(CreateSettlementKinds("population"), [process]);

        var goal = Assert.Single(generator.Generate(entity, world), g => !g.IsSubGoal);

        Assert.Equal(AttributeRef.Self("population", EntityKind.Settlement), goal.DesiredEffect);
        Assert.Equal(entity.Id, goal.TargetEntityId);
        Assert.InRange(goal.Urgency, expectedUrgency - 0.02f, expectedUrgency + 0.02f);
    }

    [Fact]
    public void Generate_ReturnsNoGoalsWhenNoAttributeHasADeficit()
    {
        var world = new StubWorldState(gridRadius: 5);
        var kinds = new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [new AttributeConfig { Name = "morale", Min = 0f, Max = 100f, Default = 20f }],
                HealthAttribute = "morale",
            },
        };

        var entity = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Self", "c", 0, (0, 0));
        entity.Attributes.Add(new ScalarValue("morale", 0f, 100f, 20f));

        var process = CreateProcess("celebrate", "morale");
        var generator = new GoalGenerator(kinds, [process]);

        var goals = generator.Generate(entity, world);

        Assert.Empty(goals);
    }

    [Fact]
    public void Generate_SortsGoalsByUrgencyAndCapsPrimaryGoalsAtThree()
    {
        var world = new StubWorldState(gridRadius: 5);
        var kinds = new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes =
                [
                    new AttributeConfig { Name = "a", Min = 0f, Max = 100f, Default = 100f },
                    new AttributeConfig { Name = "b", Min = 0f, Max = 100f, Default = 100f },
                    new AttributeConfig { Name = "c", Min = 0f, Max = 100f, Default = 100f },
                    new AttributeConfig { Name = "d", Min = 0f, Max = 100f, Default = 100f },
                ],
                HealthAttribute = "a",
            },
        };

        var entity = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Self", "c", 0, (0, 0));
        entity.Attributes.Add(new ScalarValue("a", 0f, 100f, 1f));
        entity.Attributes.Add(new ScalarValue("b", 0f, 100f, 10f));
        entity.Attributes.Add(new ScalarValue("c", 0f, 100f, 20f));
        entity.Attributes.Add(new ScalarValue("d", 0f, 100f, 30f));

        var generator = new GoalGenerator(kinds,
        [
            CreateProcess("proc_a", "a"),
            CreateProcess("proc_b", "b"),
            CreateProcess("proc_c", "c"),
            CreateProcess("proc_d", "d"),
        ]);

        var primaryGoals = generator.Generate(entity, world).Where(g => !g.IsSubGoal).ToList();

        Assert.Equal(3, primaryGoals.Count);
        Assert.Equal(["a", "b", "c"], primaryGoals.Select(EffectName).ToArray());
        Assert.True(primaryGoals[0].Urgency >= primaryGoals[1].Urgency);
        Assert.True(primaryGoals[1].Urgency >= primaryGoals[2].Urgency);
    }

    [Fact]
    public void Generate_ReturnsNoGoalWhenNoProcessWritesTheDeficitAttribute()
    {
        var world = new StubWorldState(gridRadius: 5);
        var entity = CreateSettlement(world, "Self", population: 10f, sustenance: 0f);
        var generator = new GoalGenerator(CreateSettlementKinds("population"),
            [CreateProcess("stockpile", "sustenance")]);

        var goals = generator.Generate(entity, world);

        Assert.DoesNotContain(goals, g => g.DesiredEffect.Equals(AttributeRef.Self("population", EntityKind.Settlement)));
    }

    [Fact]
    public void Generate_UsesSingleCommittedRootWhenRewardExpressionIsConstant()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", population: 10f, fame: 10f, coordinates: (0, 0));
        var otherA = CreateSettlement(world, "A", population: 30f, fame: 20f, coordinates: (1, 0));
        var otherB = CreateSettlement(world, "B", population: 30f, fame: 40f, coordinates: (2, 0));

        var process = CreateExternalProcess("grow", "population");
        var generator = new GoalGenerator(CreateSettlementKinds("population", "fame"), [process]);

        var goals = generator.Generate(self, world)
            .Where(g => !g.IsSubGoal && g.DesiredEffect.Equals(AttributeRef.Self("population", EntityKind.Settlement)))
            .ToList();

        var goal = Assert.Single(goals);
        Assert.InRange(goal.Urgency, 1.12f, 1.14f);
        Assert.Contains(goal.TargetEntityId, new[] { self.Id, otherA.Id, otherB.Id });
    }

    [Fact]
    public void Generate_ScoresProvidersUsingSelfAndCandidateContext_ChoosesBestCommittedRoot()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", population: 10f, fame: 10f, coordinates: (0, 0));
        CreateSettlement(world, "Low", population: 30f, fame: 10f, coordinates: (1, 0));
        var mid = CreateSettlement(world, "Mid", population: 30f, fame: 15f, coordinates: (2, 0));
        var high = CreateSettlement(world, "High", population: 30f, fame: 20f, coordinates: (3, 0));
        var best = CreateSettlement(world, "Best", population: 30f, fame: 30f, coordinates: (4, 0));
        var extra = CreateSettlement(world, "Extra", population: 30f, fame: 25f, coordinates: (5, 0));

        var process = CreateProcess("attract", "population");
        process.ProviderSelectors.Add(AttributeRef.Target("population", EntityKind.Settlement));
        process.RewardExpression = Expr.Sub(Expr.Target("fame"), Expr.Self("fame"));
        var generator = new GoalGenerator(CreateSettlementKinds("population", "fame"), [process]);

        var goals = generator.Generate(self, world)
            .Where(g => !g.IsSubGoal && g.DesiredEffect.Equals(AttributeRef.Self("population", EntityKind.Settlement)))
            .ToList();

        var goal = Assert.Single(goals);
        Assert.Equal(best.Id, goal.TargetEntityId);
        // Planning rewards are damped to keep long external chains from swamping direct self plans.
        Assert.InRange(goal.Urgency, 2.6f, 2.8f);
    }

    [Fact]
    public void Generate_CreatesOneSubGoalPerDistinctBlockingAttribute()
    {
        var world = new StubWorldState(gridRadius: 5);
        var entity = CreateSettlement(world, "Self", population: 10f, sustenance: 0f);
        var stockpile = CreateProcess("stockpile", "sustenance");

        var growA = CreateProcess("grow_a", "population",
            new SelfAttributeThreshold("sustenance", min: 3f, max: null));
        var growB = CreateProcess("grow_b", "population",
            new SelfAttributeThreshold("sustenance", min: 5f, max: null));
        var generator = new GoalGenerator(CreateSettlementKinds("population", "sustenance"), [growA, growB, stockpile]);

        var goals = generator.Generate(entity, world);

        Assert.Contains(goals, g => !g.IsSubGoal && !g.IsActionable &&
            g.DesiredEffect.Equals(AttributeRef.Self("population", EntityKind.Settlement)));
        var subGoal = Assert.Single(goals, g => g.IsSubGoal && g.IsActionable);
        Assert.Equal(AttributeRef.Self("sustenance", EntityKind.Settlement), subGoal.DesiredEffect);
        Assert.Equal(entity.Id, subGoal.TargetEntityId);
    }

    [Fact]
    public void Generate_SubGoalsInheritParentProviderAndUrgency()
    {
        var world = new StubWorldState(gridRadius: 5);
        var entity = CreateSettlement(world, "Self", population: 10f, sustenance: 0f);
        var stockpile = CreateProcess("stockpile", "sustenance");

        var grow = CreateProcess("grow", "population",
            new SelfAttributeThreshold("sustenance", min: 3f, max: null));
        var generator = new GoalGenerator(CreateSettlementKinds("population", "sustenance"), [grow, stockpile]);

        var goals = generator.Generate(entity, world);
        var parent = Assert.Single(goals, g => !g.IsSubGoal);
        var child = Assert.Single(goals, g => g.IsSubGoal && g.IsActionable);

        Assert.Equal(parent.TargetEntityId, child.TargetEntityId);
        Assert.True(parent.Urgency >= child.Urgency);
        Assert.Equal(parent.Id, child.ParentGoalId);
    }

    [Fact]
    public void Generate_TargetIsSelfOnlyCreatesBlockingSubGoalsForSelfProvider()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", population: 10f, sustenance: 0f, coordinates: (0, 0));
        var other = CreateSettlement(world, "Other", population: 10f, sustenance: 10f, coordinates: (2, 0));
        var aid = CreateProcessServingEffect("aid", AttributeRef.Target("sustenance", EntityKind.Settlement));

        var process = CreateExternalProcess("grow", "population",
            new AndPrecondition([new TargetIsSelf(), new TargetAttributeThreshold("sustenance", min: 3f, max: null)]));
        var generator = new GoalGenerator(CreateSettlementKinds("population", "sustenance"), [process, aid]);

        var goals = generator.Generate(self, world);

        var selfGoal = Assert.Single(goals, g => !g.IsSubGoal && g.TargetEntityId == self.Id);
        Assert.Contains(goals, g => g.ParentGoalId == selfGoal.Id &&
            g.DesiredEffect.Equals(AttributeRef.Target("sustenance", EntityKind.Settlement)));
        Assert.DoesNotContain(goals, g => !g.IsSubGoal && g.TargetEntityId == other.Id);
    }

    [Fact]
    public void Generate_TargetIsNotSelfOnlyCreatesBlockingSubGoalsForOtherProviders()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", population: 10f, sustenance: 10f, coordinates: (0, 0));
        var other = CreateSettlement(world, "Other", population: 10f, sustenance: 0f, coordinates: (2, 0));
        var aid = CreateProcessServingEffect("aid", AttributeRef.Target("sustenance", EntityKind.Settlement));

        var process = CreateExternalProcess("attract", "population",
            new AndPrecondition([new TargetIsNotSelf(), new TargetAttributeThreshold("sustenance", min: 3f, max: null)]));
        var generator = new GoalGenerator(CreateSettlementKinds("population", "sustenance"), [process, aid]);

        var goals = generator.Generate(self, world);

        var otherGoal = Assert.Single(goals, g => !g.IsSubGoal && g.TargetEntityId == other.Id);
        Assert.DoesNotContain(goals, g => !g.IsSubGoal && g.TargetEntityId == self.Id);
        Assert.Contains(goals, g => g.ParentGoalId == otherGoal.Id &&
            g.DesiredEffect.Equals(AttributeRef.Target("sustenance", EntityKind.Settlement)));
    }

    [Fact]
    public void Generate_BlockedSelfDirectedPlanStillProducesActionableEnablerWhenRewardIsZero()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", population: 10f, sustenance: 0f, fame: 10f, coordinates: (0, 0));

        var stockpile = CreateProcess("stockpile", "sustenance");

        var grow = CreateProcess("grow", "population",
            new SelfAttributeThreshold("sustenance", min: 3f, max: null));
        grow.RewardExpression = Expr.Div(Expr.Self("sustenance"), Expr.Lit(3f));

        var generator = new GoalGenerator(CreateSettlementKinds("population", "sustenance", "fame"),
            [grow, stockpile]);

        var goals = generator.Generate(self, world);

        Assert.Contains(goals, g => g.IsActionable &&
            g.PlannedProcessId == "stockpile" &&
            g.DesiredEffect.Equals(AttributeRef.Self("sustenance", EntityKind.Settlement)));
    }

    [Fact]
    public void Generate_SelectsSupportActionWhenImmediateProcessIsWeak()
    {
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var self = CreateSettlement(world, "Self", population: 10f, coordinates: (0, 0));
        SetAmbientResource(world, self, ResourceType.Food, 0.5f);
        SetAmbientResource(world, self, ResourceType.Population, 5f);

        var grow = new ProcessDefinition(
            "grow",
            "Grow",
            DefinitionRole.Process,
            EntityKind.Settlement,
            new AndPrecondition([
                new TargetIsSelf(),
                new LocationResourceThreshold(ResourceType.Population, min: 1f, max: null, plannerRelevant: false),
            ]),
            Expr.Score(Expr.Div(Expr.SelfLocationResource(ResourceType.Food), Expr.Lit(4f))));
        grow.GoalEffects.Add(AttributeRef.Self("population", EntityKind.Settlement));
        grow.Writes.Add(AttributeRef.Self("population", EntityKind.Settlement));
        grow.Reads.Add(AttributeRef.Resource("food"));
        grow.Reads.Add(AttributeRef.Resource("population"));
        grow.SupportRules.Add(new PlannerSupportRule(
            AttributeRef.Resource("food"),
            Expr.SelfLocationResource(ResourceType.Food),
            desiredMinimum: 4f));

        var irrigate = new ProcessDefinition(
            "irrigate",
            "Irrigate",
            DefinitionRole.Process,
            EntityKind.Settlement,
            new TargetIsSelf(),
            Expr.Score(
                Expr.Div(
                    Expr.Max(Expr.Lit(0f), Expr.Sub(Expr.Lit(8f), Expr.SelfLocationResource(ResourceType.Food))),
                    Expr.Lit(8f))));
        irrigate.GoalEffects.Add(AttributeRef.Resource("food"));
        irrigate.Reads.Add(AttributeRef.Resource("food"));
        irrigate.Writes.Add(AttributeRef.Resource("food"));

        var generator = new GoalGenerator(CreateSettlementKinds("population"), [grow, irrigate]);
        var goals = generator.Generate(self, world);

        var root = Assert.Single(goals, g => !g.IsSubGoal && g.PlannedProcessId == "grow");
        var child = Assert.Single(goals, g => g.IsSubGoal && g.PlannedProcessId == "irrigate");

        Assert.False(root.IsActionable);
        Assert.True(child.IsActionable);
        Assert.Equal(AttributeRef.Resource("food"), child.DesiredEffect);
        Assert.Equal(root.Id, child.ParentGoalId);
    }

    [Fact]
    public void Generate_UsesRelationshipPreconditionInPlannerContext()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", population: 10f, coordinates: (0, 0));
        var other = CreateSettlement(world, "Other", population: 30f, coordinates: (1, 0));
        world.Graph.CreateRelationship("allied_with", RelationshipPolarity.Positive, self.Id, other.Id, 0.8f, 0);

        var attract = CreateExternalProcess(
            "attract",
            "population",
            new AndPrecondition([
                new TargetIsNotSelf(),
                new RelationshipExists(self.Id, other.Id, "allied_with", minStrength: 0.5f),
            ]));
        var generator = new GoalGenerator(CreateSettlementKinds("population"), [attract]);

        var goals = generator.Generate(self, world)
            .Where(g => !g.IsSubGoal)
            .ToList();

        Assert.Contains(goals, g => g.TargetEntityId == other.Id && g.IsActionable);
    }

    [Fact]
    public void Generate_UsesPressureThresholdInPlannerContext()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", population: 10f, coordinates: (0, 0));
        SeedPressure(world, "demographic_pressure", self.Id, 4f);

        var process = CreateProcess(
            "stabilize",
            "population",
            new PressureThreshold("demographic_pressure", self.Id, min: null, max: 5f));
        var generator = new GoalGenerator(CreateSettlementKinds("population"), [process]);

        var goals = generator.Generate(self, world)
            .Where(g => !g.IsSubGoal)
            .ToList();

        Assert.Contains(goals, g => g.PlannedProcessId == "stabilize" && g.IsActionable);
    }

    [Fact]
    public void Generate_UsesPerProcessLastExecutionTickInPlannerContext()
    {
        var world = new StubWorldState(gridRadius: 5);
        world.CurrentTick = 6;
        var self = CreateSettlement(world, "Self", population: 10f, coordinates: (0, 0));

        var process = CreateProcess("grow", "population", new CooldownCheck(3));
        self.RecordAction(5, process.Name, process.Id);

        var generator = new GoalGenerator(CreateSettlementKinds("population"), [process]);
        var goals = generator.Generate(self, world);

        Assert.Empty(goals);
    }

    [Fact]
    public void Generate_IncludesStandingDrivesWhenNoDeficitExists()
    {
        var world = new StubWorldState(gridRadius: 5);
        var entity = CreateSettlement(world, "Self", population: 25f, sustenance: 5f);
        var kinds = new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes =
                [
                    new AttributeConfig { Name = "population", Min = 0f, Max = 1000f, Default = 25f, Baseline = 0f },
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

        var grow = CreateProcess("grow", "population");
        var generator = new GoalGenerator(kinds, [grow]);

        var goal = Assert.Single(generator.Generate(entity, world), g => !g.IsSubGoal);

        Assert.Equal("grow", goal.PlannedProcessId);
        Assert.InRange(goal.Urgency, 0.13f, 0.15f);
        Assert.Contains("Standing Drives:", entity.GoalGenerationTrace, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_StandingDriveUsesOnlyOneRootSlotAcrossTargets()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        self.Attributes.Add(new ScalarValue("hunger", 0f, 100f, 0f));
        self.Attributes.Add(new ScalarValue("ambition", 0f, 100f, 50f));
        self.Attributes.Add(new ScalarValue("vitality", 0f, 100f, 100f));
        var near = CreateSettlement(world, "Near", population: 20f, coordinates: (0, 0));
        var far = CreateSettlement(world, "Far", population: 40f, coordinates: (3, 0));

        var attend = new ProcessDefinition(
            "attend",
            "Attend",
            DefinitionRole.Process,
            EntityKind.Npc,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(1f)));
        attend.GoalEffects.Add(AttributeRef.StructuralFact("presence", EntityKind.Npc));
        attend.ProviderSelectors.Add(AttributeRef.Target("population", EntityKind.Settlement));
        attend.RewardExpression = Expr.Div(Expr.Target("population"), Expr.Add(Expr.Distance(), Expr.Lit(1f)));

        var kinds = new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Npc] = new()
            {
                Subtypes = new HashSet<string> { "warrior" },
                Attributes =
                [
                    new AttributeConfig { Name = "hunger", Min = 0f, Max = 100f, Default = 0f },
                    new AttributeConfig { Name = "ambition", Min = 0f, Max = 100f, Default = 50f },
                    new AttributeConfig { Name = "vitality", Min = 0f, Max = 100f, Default = 100f },
                ],
                HealthAttribute = "vitality",
                StandingDrives =
                [
                    new StandingDriveConfig
                    {
                        DesiredEffect = AttributeRef.StructuralFact("presence", EntityKind.Npc),
                        BaseUrgency = 0.04f,
                    },
                ],
            },
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = [new AttributeConfig { Name = "population", Min = 0f, Max = 1000f, Default = 10f }],
                HealthAttribute = "population",
            },
        };

        var generator = new GoalGenerator(kinds, [attend]);
        var goals = generator.Generate(self, world).Where(g => !g.IsSubGoal).ToList();

        var root = Assert.Single(goals);
        Assert.Equal("attend", root.PlannedProcessId);
        Assert.Equal(near.Id, root.TargetEntityId);
        Assert.DoesNotContain(goals, g => g.TargetEntityId == far.Id);
    }

    [Fact]
    public void Generate_KeepsPreviousTargetWhenAlternativeIsOnlyFractionallyBetter()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", population: 10f, fame: 10f, coordinates: (0, 0));
        var current = CreateSettlement(world, "Current", population: 30f, fame: 19f, coordinates: (2, 0));
        var challenger = CreateSettlement(world, "Challenger", population: 30f, fame: 20f, coordinates: (2, 1));

        var attract = CreateProcess("attract", "population");
        attract.ProviderSelectors.Add(AttributeRef.Target("population", EntityKind.Settlement));
        attract.RewardExpression = Expr.Sub(Expr.Target("fame"), Expr.Self("fame"));

        self.Goals.Add(new EntityGoal(
            AttributeRef.Self("population", EntityKind.Settlement),
            current.Id,
            urgency: 1f,
            plannedProcessId: "attract",
            isActionable: true));

        var generator = new GoalGenerator(CreateSettlementKinds("population", "fame"), [attract]);
        var goals = generator.Generate(self, world)
            .Where(g => !g.IsSubGoal && g.PlannedProcessId == "attract")
            .ToList();

        var root = Assert.Single(goals);
        Assert.Equal(current.Id, root.TargetEntityId);
        Assert.DoesNotContain(goals, g => g.TargetEntityId == challenger.Id);
        Assert.Contains("effect commitment kept", self.GoalGenerationTrace, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_RelationalBlockerInheritsParentTarget()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        self.Attributes.Add(new ScalarValue("hunger", 0f, 100f, 80f));
        var settlement = CreateSettlement(world, "Target", population: 25f, sustenance: 10f, coordinates: (2, 0));

        var eat = new ProcessDefinition(
            "eat",
            "Eat",
            DefinitionRole.Process,
            EntityKind.Npc,
            new AndPrecondition([
                new TargetIsNotSelf(),
                new RelatedEntityMatchesTarget("located_at"),
            ]),
            Expr.Score(Expr.Lit(1f)));
        eat.GoalEffects.Add(AttributeRef.Self("hunger", EntityKind.Npc));
        eat.ProviderSelectors.Add(AttributeRef.Target("sustenance", EntityKind.Settlement));
        eat.Writes.Add(AttributeRef.Self("hunger", EntityKind.Npc));

        var move = new ProcessDefinition(
            "move",
            "Move",
            DefinitionRole.Process,
            EntityKind.Npc,
            new TargetIsNotSelf(),
            Expr.Score(Expr.Lit(1f)));
        move.GoalEffects.Add(AttributeRef.TargetRelation("located_at", EntityKind.Settlement));

        var kinds = new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Npc] = new()
            {
                Subtypes = new HashSet<string> { "warrior" },
                Attributes = [new AttributeConfig { Name = "hunger", Min = 0f, Max = 100f, Default = 0f, Baseline = 20f }],
                HealthAttribute = "hunger",
            },
        };

        var generator = new GoalGenerator(kinds, [eat, move]);
        var goals = generator.Generate(self, world);

        var parent = Assert.Single(goals, g => !g.IsSubGoal);
        var child = Assert.Single(goals, g => g.IsSubGoal);
        Assert.Equal(settlement.Id, parent.TargetEntityId);
        Assert.Equal(settlement.Id, child.TargetEntityId);
        Assert.Equal(AttributeRef.TargetRelation("located_at", EntityKind.Settlement), child.DesiredEffect);
    }

    private static Dictionary<EntityKind, KindConfig> CreateSettlementKinds(params string[] attributes)
    {
        var configs = new List<AttributeConfig>();
        foreach (var attribute in attributes)
        {
            configs.Add(attribute switch
            {
                "population" => new AttributeConfig { Name = "population", Min = 0f, Max = 1000f, Default = 10f, Baseline = 20f },
                "sustenance" => new AttributeConfig { Name = "sustenance", Min = 0f, Max = 1000f, Default = 0f },
                "fame" => new AttributeConfig { Name = "fame", Min = 0f, Max = 1000f, Default = 0f },
                _ => new AttributeConfig { Name = attribute, Min = 0f, Max = 100f, Default = 0f },
            });
        }

        return new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "village" },
                Attributes = configs,
                HealthAttribute = "population",
            },
        };
    }

    private static Entity CreateSettlement(
        StubWorldState world,
        string name,
        float population,
        float sustenance = 0f,
        float fame = 0f,
        (int Q, int R)? coordinates = null)
    {
        var entity = world.Graph.CreateEntity(
            EntityKind.Settlement,
            "village",
            name,
            "c",
            tick: 0,
            coordinates: coordinates ?? (0, 0));

        entity.Attributes.Add(new ScalarValue("population", 0f, 1000f, population));
        entity.Attributes.Add(new ScalarValue("sustenance", 0f, 1000f, sustenance));
        entity.Attributes.Add(new ScalarValue("fame", 0f, 1000f, fame));
        entity.Attributes.Add(new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0.5f, 0.5f));
        return entity;
    }

    private static ProcessDefinition CreateProcess(string id, string attribute, IPrecondition? precondition = null)
    {
        var process = new ProcessDefinition(
            id,
            id,
            DefinitionRole.Process,
            EntityKind.Settlement,
            precondition ?? new AlwaysTrue(),
            Expr.Score(Expr.Lit(1f)));

        process.GoalEffects.Add(AttributeRef.Self(attribute, EntityKind.Settlement));
        process.Writes.Add(AttributeRef.Self(attribute, EntityKind.Settlement));
        return process;
    }

    private static ProcessDefinition CreateExternalProcess(string id, string attribute, IPrecondition? precondition = null)
    {
        var process = CreateProcess(id, attribute, precondition);
        process.ProviderSelectors.Add(AttributeRef.Target(attribute, EntityKind.Settlement));
        return process;
    }

    private static ProcessDefinition CreateProcessServingEffect(string id, AttributeRef effect)
    {
        var process = new ProcessDefinition(
            id,
            id,
            DefinitionRole.Process,
            EntityKind.Settlement,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(1f)));

        process.GoalEffects.Add(effect);
        process.Writes.Add(effect);
        return process;
    }

    private static string EffectName(EntityGoal goal) => goal.DesiredEffect.Name;

    private static void SetAmbientResource(StubWorldState world, Entity entity, ResourceType resourceType, float value)
    {
        if (entity.Coordinates is null)
        {
            throw new InvalidOperationException("Entity has no coordinates.");
        }

        var coord = new Engine.Map.HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
        var layer = resourceType switch
        {
            ResourceType.Food => world.Resources!.Food,
            ResourceType.Wealth => world.Resources!.Wealth,
            ResourceType.Population => world.Resources!.PopulationLayer,
            _ => throw new ArgumentOutOfRangeException(nameof(resourceType)),
        };
        layer.SetStockpile(coord, value);
    }

    private static void SeedPressure(StubWorldState world, string pressureName, EntityId scopeId, float value)
    {
        if (world.Pressure is null)
        {
            throw new InvalidOperationException("Stub world has no pressure system.");
        }

        var definition = new PressureDefinition(
            pressureName,
            PressureScope.Entity,
            baseline: 0f,
            homeostasisRate: 0f,
            smoothingFactor: 1f);
        world.Pressure.RegisterDefinition(definition);
        world.Pressure.ApplyDelta(pressureName, scopeId, value);
        world.Pressure.Compute(new Dictionary<EntityId, IPressureContext>
        {
            [scopeId] = new TestPressureContext([scopeId], world.Graph),
        });
    }

    private sealed class TestPressureContext(IReadOnlyCollection<EntityId> entitiesInScope, WorldGraph graph)
        : IPressureContext
    {
        public IReadOnlyCollection<EntityId> EntitiesInScope => entitiesInScope;
        public Entity? GetEntity(EntityId id) => graph.GetEntity(id);
        public int CountEntities(Func<Entity, bool> predicate) =>
            entitiesInScope
                .Select(id => graph.GetEntity(id))
                .Count(entity => entity is not null && predicate(entity));
        public int CountRelationships(Func<Relationship, bool> predicate) =>
            entitiesInScope.Sum(id => graph.OutgoingRelationships(id).Count(predicate));
        public float GetAttribute(EntityId entityId, string attributeName) =>
            graph.GetEntity(entityId)?.Attributes.GetValue(attributeName) ?? 0f;
    }
}
