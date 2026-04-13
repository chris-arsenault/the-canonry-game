namespace Engine.Tests.Behaviors;

using Engine.Behaviors;
using Engine.Graph;
using Engine.Kinds;

public class UtilityEvaluatorTests
{
    private readonly ImpulseRegistry _registry = new();

    // --- Test impulse implementations ---

    private sealed class FixedScoreImpulse(string name, float score) : IImpulse
    {
        public string Name => name;
        public int ExecuteCount { get; private set; }

        public float Score(Entity entity, IWorldState worldState) => score;

        public void Execute(Entity entity, IWorldState worldState) => ExecuteCount++;
    }

    private static Entity CreateTestEntity() =>
        new(new EntityId(1), EntityKind.Npc, "generic_npc", "Test", "c", 0);

    // --- ImpulseRegistry ---

    [Fact]
    public void Registry_RegisterAndRetrieve()
    {
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("walk", 0.5f));
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("eat", 0.3f));

        Assert.Equal(2, _registry.GetImpulses(EntityKind.Npc).Count);
    }

    [Fact]
    public void Registry_EmptyForUnregisteredKind()
    {
        Assert.Empty(_registry.GetImpulses(EntityKind.Nation));
    }

    [Fact]
    public void Registry_Unregister_RemovesByName()
    {
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("walk", 0.5f));
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("eat", 0.3f));

        _registry.Unregister(EntityKind.Npc, "walk");

        var remaining = _registry.GetImpulses(EntityKind.Npc);
        Assert.Single(remaining);
        Assert.Equal("eat", remaining[0].Name);
    }

    [Fact]
    public void Registry_HasImpulses()
    {
        Assert.False(_registry.HasImpulses(EntityKind.Npc));

        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("walk", 0.5f));

        Assert.True(_registry.HasImpulses(EntityKind.Npc));
    }

    // --- UtilityEvaluator ---

    [Fact]
    public void Evaluate_ReturnsNull_WhenNoImpulses()
    {
        var evaluator = new UtilityEvaluator(_registry);
        var entity = CreateTestEntity();

        var result = evaluator.Evaluate(entity, new StubWorldState());
        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_ReturnsNull_WhenAllBelowThreshold()
    {
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("useless", 0.001f));

        var evaluator = new UtilityEvaluator(_registry) { MinimumViableScore = 0.01f };
        var entity = CreateTestEntity();

        var result = evaluator.Evaluate(entity, new StubWorldState());
        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_SelectsSingleViableImpulse()
    {
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("only_option", 0.5f));

        var evaluator = new UtilityEvaluator(_registry);
        var entity = CreateTestEntity();

        var result = evaluator.Evaluate(entity, new StubWorldState());
        Assert.NotNull(result);
        Assert.Equal("only_option", result.SelectedImpulse.Name);
        Assert.Equal(0.5f, result.SelectedScore);
    }

    [Fact]
    public void Evaluate_HighScoreSelectedMostOften()
    {
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("strong", 0.9f));
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("weak", 0.1f));

        var evaluator = new UtilityEvaluator(_registry, new Random(42));
        var entity = CreateTestEntity();

        var selections = new Dictionary<string, int> { ["strong"] = 0, ["weak"] = 0 };
        for (var i = 0; i < 100; i++)
        {
            var result = evaluator.Evaluate(entity, new StubWorldState());
            Assert.NotNull(result);
            selections[result.SelectedImpulse.Name]++;
        }

        // Strong should be selected significantly more often (>60% of the time)
        Assert.True(selections["strong"] > 60, $"Strong selected {selections["strong"]}/100 times");
    }

    [Fact]
    public void Evaluate_WeakImpulseStillHasChance()
    {
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("strong", 0.8f));
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("weak", 0.2f));

        var evaluator = new UtilityEvaluator(_registry, new Random(42));
        var entity = CreateTestEntity();

        var weakSelected = false;
        for (var i = 0; i < 200; i++)
        {
            var result = evaluator.Evaluate(entity, new StubWorldState());
            if (result?.SelectedImpulse.Name == "weak")
            {
                weakSelected = true;
                break;
            }
        }

        Assert.True(weakSelected, "Weak impulse was never selected in 200 evaluations");
    }

    [Fact]
    public void Evaluate_RespectsTopCandidateCount()
    {
        for (var i = 0; i < 10; i++)
        {
            _registry.Register(EntityKind.Npc, new FixedScoreImpulse($"impulse_{i}", 0.1f * (i + 1)));
        }

        var evaluator = new UtilityEvaluator(_registry, new Random(42)) { TopCandidateCount = 3 };
        var entity = CreateTestEntity();

        var result = evaluator.Evaluate(entity, new StubWorldState());
        Assert.NotNull(result);
        Assert.Equal(3, result.Candidates.Count);
    }

    [Fact]
    public void Evaluate_CandidatesIncludeScores()
    {
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("a", 0.8f));
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("b", 0.3f));

        var evaluator = new UtilityEvaluator(_registry, new Random(42));
        var entity = CreateTestEntity();

        var result = evaluator.Evaluate(entity, new StubWorldState());
        Assert.NotNull(result);
        Assert.Equal(2, result.Candidates.Count);
        Assert.Contains(result.Candidates, c => c.Name == "a" && Math.Abs(c.Score - 0.8f) < 0.01f);
        Assert.Contains(result.Candidates, c => c.Name == "b" && Math.Abs(c.Score - 0.3f) < 0.01f);
    }

    // --- EvaluateAndExecute ---

    [Fact]
    public void EvaluateAndExecute_CallsExecute()
    {
        var impulse = new FixedScoreImpulse("action", 0.5f);
        _registry.Register(EntityKind.Npc, impulse);

        var evaluator = new UtilityEvaluator(_registry);
        var entity = CreateTestEntity();

        evaluator.EvaluateAndExecute(entity, new StubWorldState());

        Assert.Equal(1, impulse.ExecuteCount);
    }

    [Fact]
    public void EvaluateAndExecute_ReturnsNull_WhenNoAction()
    {
        var evaluator = new UtilityEvaluator(_registry);
        var entity = CreateTestEntity();

        var result = evaluator.EvaluateAndExecute(entity, new StubWorldState());
        Assert.Null(result);
    }

    // --- Kind isolation ---

    [Fact]
    public void Evaluate_OnlyConsidersCorrectKind()
    {
        _registry.Register(EntityKind.Npc, new FixedScoreImpulse("npc_impulse", 0.8f));
        _registry.Register(EntityKind.Nation, new FixedScoreImpulse("nation_impulse", 0.9f));

        var evaluator = new UtilityEvaluator(_registry);
        var npc = CreateTestEntity(); // EntityKind.Npc

        var result = evaluator.Evaluate(npc, new StubWorldState());
        Assert.NotNull(result);
        Assert.Equal("npc_impulse", result.SelectedImpulse.Name);
    }
}
