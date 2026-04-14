namespace Engine.Tests.Behaviors;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Tests;
using Engine.Wiring;

public class GoalGeneratorTests
{
    [Theory]
    [InlineData(10f, 0.667f)]
    [InlineData(20f, 0.5f)]
    [InlineData(100f, 0.167f)]
    public void Generate_UsesBaselineUrgencyCurve(float population, float expectedUrgency)
    {
        var world = new StubWorldState(gridRadius: 5);
        var entity = CreateSettlement(world, "Self", population: population);
        var process = CreateProcess("grow", "population");
        var generator = new GoalGenerator(CreateSettlementKinds("population"), [process]);

        var goal = Assert.Single(generator.Generate(entity, world), g => !g.IsSubGoal);

        Assert.Equal("population", goal.Attribute);
        Assert.Equal(entity.Id, goal.Provider);
        Assert.InRange(goal.Urgency, expectedUrgency - 0.01f, expectedUrgency + 0.01f);
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
        Assert.Equal(["a", "b", "c"], primaryGoals.Select(g => g.Attribute).ToArray());
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

        Assert.DoesNotContain(goals, g => g.Attribute == "population");
    }

    [Fact]
    public void Generate_UsesWeightOneForAllProvidersWhenScoreExpressionIsMissing()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", population: 10f, fame: 10f, coordinates: (0, 0));
        var otherA = CreateSettlement(world, "A", population: 30f, fame: 20f, coordinates: (1, 0));
        var otherB = CreateSettlement(world, "B", population: 30f, fame: 40f, coordinates: (2, 0));

        var generator = new GoalGenerator(CreateSettlementKinds("population", "fame"),
            [CreateProcess("grow", "population")]);

        var goals = generator.Generate(self, world)
            .Where(g => !g.IsSubGoal && g.Attribute == "population")
            .OrderBy(g => g.Provider.Value)
            .ToList();

        Assert.Equal(3, goals.Count);
        Assert.All(goals, g => Assert.InRange(g.Urgency, 0.66f, 0.67f));
        Assert.Equal([self.Id, otherA.Id, otherB.Id], goals.Select(g => g.Provider).ToArray());
    }

    [Fact]
    public void Generate_ScoresProvidersUsingSelfAndCandidateContext_ExcludesZeroAndKeepsTopThree()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", population: 10f, fame: 10f, coordinates: (0, 0));
        CreateSettlement(world, "Low", population: 30f, fame: 10f, coordinates: (1, 0));
        var mid = CreateSettlement(world, "Mid", population: 30f, fame: 15f, coordinates: (2, 0));
        var high = CreateSettlement(world, "High", population: 30f, fame: 20f, coordinates: (3, 0));
        var best = CreateSettlement(world, "Best", population: 30f, fame: 30f, coordinates: (4, 0));
        var extra = CreateSettlement(world, "Extra", population: 30f, fame: 25f, coordinates: (5, 0));

        var process = CreateProcess("attract", "population");
        process.ProviderScoreExpression = Expr.Sub(Expr.Target("fame"), Expr.Self("fame"));
        var generator = new GoalGenerator(CreateSettlementKinds("population", "fame"), [process]);

        var goals = generator.Generate(self, world)
            .Where(g => !g.IsSubGoal && g.Attribute == "population")
            .OrderByDescending(g => g.Urgency)
            .ToList();

        Assert.Equal(3, goals.Count);
        Assert.Equal([best.Id, extra.Id, high.Id], goals.Select(g => g.Provider).ToArray());
        Assert.InRange(goals[0].Urgency, 0.66f, 0.67f);
        Assert.InRange(goals[1].Urgency, 0.49f, 0.51f);
        Assert.InRange(goals[2].Urgency, 0.33f, 0.34f);
        Assert.DoesNotContain(goals, g => g.Provider == self.Id || g.Provider == mid.Id);
    }

    [Fact]
    public void Generate_CreatesOneSubGoalPerDistinctBlockingAttribute()
    {
        var world = new StubWorldState(gridRadius: 5);
        var entity = CreateSettlement(world, "Self", population: 10f, sustenance: 0f);

        var growA = CreateProcess("grow_a", "population",
            new SelfAttributeThreshold("sustenance", min: 3f, max: null));
        var growB = CreateProcess("grow_b", "population",
            new SelfAttributeThreshold("sustenance", min: 5f, max: null));
        var generator = new GoalGenerator(CreateSettlementKinds("population", "sustenance"), [growA, growB]);

        var goals = generator.Generate(entity, world);

        Assert.Contains(goals, g => !g.IsSubGoal && g.Attribute == "population");
        var subGoal = Assert.Single(goals, g => g.IsSubGoal);
        Assert.Equal("sustenance", subGoal.BlockingAttribute);
        Assert.Equal(entity.Id, subGoal.Provider);
    }

    [Fact]
    public void Generate_SubGoalsInheritParentProviderAndUrgency()
    {
        var world = new StubWorldState(gridRadius: 5);
        var entity = CreateSettlement(world, "Self", population: 10f, sustenance: 0f);

        var grow = CreateProcess("grow", "population",
            new SelfAttributeThreshold("sustenance", min: 3f, max: null));
        var generator = new GoalGenerator(CreateSettlementKinds("population", "sustenance"), [grow]);

        var goals = generator.Generate(entity, world);
        var parent = Assert.Single(goals, g => !g.IsSubGoal);
        var child = Assert.Single(goals, g => g.IsSubGoal);

        Assert.Equal(parent.Provider, child.Provider);
        Assert.Equal(parent.Urgency, child.Urgency);
        Assert.Equal(parent.Id, child.ParentGoalId);
    }

    [Fact]
    public void Generate_TargetIsSelfOnlyCreatesBlockingSubGoalsForSelfProvider()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", population: 10f, sustenance: 0f, coordinates: (0, 0));
        var other = CreateSettlement(world, "Other", population: 10f, sustenance: 10f, coordinates: (2, 0));

        var process = CreateProcess("grow", "population",
            new AndPrecondition([new TargetIsSelf(), new TargetAttributeThreshold("sustenance", min: 3f, max: null)]));
        var generator = new GoalGenerator(CreateSettlementKinds("population", "sustenance"), [process]);

        var goals = generator.Generate(self, world);

        var selfGoal = Assert.Single(goals, g => !g.IsSubGoal && g.Provider == self.Id);
        var otherGoal = Assert.Single(goals, g => !g.IsSubGoal && g.Provider == other.Id);
        Assert.Contains(goals, g => g.ParentGoalId == selfGoal.Id && g.BlockingAttribute == "sustenance");
        Assert.DoesNotContain(goals, g => g.ParentGoalId == otherGoal.Id);
    }

    [Fact]
    public void Generate_TargetIsNotSelfOnlyCreatesBlockingSubGoalsForOtherProviders()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", population: 10f, sustenance: 10f, coordinates: (0, 0));
        var other = CreateSettlement(world, "Other", population: 10f, sustenance: 0f, coordinates: (2, 0));

        var process = CreateProcess("attract", "population",
            new AndPrecondition([new TargetIsNotSelf(), new TargetAttributeThreshold("sustenance", min: 3f, max: null)]));
        var generator = new GoalGenerator(CreateSettlementKinds("population", "sustenance"), [process]);

        var goals = generator.Generate(self, world);

        var selfGoal = Assert.Single(goals, g => !g.IsSubGoal && g.Provider == self.Id);
        var otherGoal = Assert.Single(goals, g => !g.IsSubGoal && g.Provider == other.Id);
        Assert.DoesNotContain(goals, g => g.ParentGoalId == selfGoal.Id);
        Assert.Contains(goals, g => g.ParentGoalId == otherGoal.Id && g.BlockingAttribute == "sustenance");
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

        process.Writes.Add(AttributeRef.Self(attribute, EntityKind.Settlement));
        return process;
    }
}
