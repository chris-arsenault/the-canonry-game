namespace Engine.Tests.Behaviors;

using Engine.Behaviors;
using Engine.Graph;
using Engine.Kinds;
using Engine.Messages;

public class EntityEvaluatorTests
{
    // --- Process Selection ---

    [Fact]
    public void Process_SelectedViaWeightedRandom()
    {
        var (registry, bus) = Setup();
        registry.RegisterProcess(EntityKind.Npc, new TestProcess("eat", 0.8f));
        registry.RegisterProcess(EntityKind.Npc, new TestProcess("rest", 0.2f));

        var messages = new List<ProcessExecutedMessage>();
        bus.Subscribe(m => { if (m is ProcessExecutedMessage pm) messages.Add(pm); });

        var evaluator = new EntityEvaluator(registry, bus, new Random(42));
        evaluator.Evaluate(CreateEntity(), new StubWorldState());

        Assert.Single(messages); // One process per tick
    }

    [Fact]
    public void Process_AllViableCompete_NoCap()
    {
        var (registry, bus) = Setup();
        for (var i = 0; i < 10; i++)
        {
            registry.RegisterProcess(EntityKind.Npc, new TestProcess($"process_{i}", 0.3f + (i * 0.05f)));
        }

        var selectedNames = new HashSet<string>();
        bus.Subscribe(m => { if (m is ProcessExecutedMessage pm) selectedNames.Add(pm.ProcessName); });

        var evaluator = new EntityEvaluator(registry, bus, new Random(42));
        var entity = CreateEntity();

        for (var i = 0; i < 200; i++)
        {
            evaluator.Evaluate(entity, new StubWorldState());
        }

        Assert.True(selectedNames.Count >= 5,
            $"Only {selectedNames.Count} distinct processes selected out of 10");
    }

    [Fact]
    public void Process_SkipsFailedPrecondition()
    {
        var (registry, bus) = Setup();
        registry.RegisterProcess(EntityKind.Npc, new TestProcess("blocked", 0.9f, canExecute: false));
        registry.RegisterProcess(EntityKind.Npc, new TestProcess("available", 0.1f));

        var messages = new List<ProcessExecutedMessage>();
        bus.Subscribe(m => { if (m is ProcessExecutedMessage pm) messages.Add(pm); });

        var evaluator = new EntityEvaluator(registry, bus);
        evaluator.Evaluate(CreateEntity(), new StubWorldState());

        Assert.Single(messages);
        Assert.Equal("available", messages[0].ProcessName);
    }

    [Fact]
    public void Process_NoneSelected_WhenAllBelowThreshold()
    {
        var (registry, bus) = Setup();
        registry.RegisterProcess(EntityKind.Npc, new TestProcess("weak", 0.005f));

        var messages = new List<ProcessExecutedMessage>();
        bus.Subscribe(m => { if (m is ProcessExecutedMessage pm) messages.Add(pm); });

        var evaluator = new EntityEvaluator(registry, bus);
        evaluator.Evaluate(CreateEntity(), new StubWorldState());

        Assert.Empty(messages);
    }

    // --- Decision Selection ---

    [Fact]
    public void Decision_SelectedWhenAvailable()
    {
        var (registry, bus) = Setup();
        registry.RegisterDecision(EntityKind.Npc, new TestDecision("wield_artifact", 0.5f, "card_1"));

        var messages = new List<DecisionExecutedMessage>();
        bus.Subscribe(m => { if (m is DecisionExecutedMessage dm) messages.Add(dm); });

        var evaluator = new EntityEvaluator(registry, bus);
        evaluator.Evaluate(CreateEntity(), new StubWorldState());

        Assert.Single(messages);
        Assert.Equal("wield_artifact", messages[0].DecisionName);
        Assert.Equal("card_1", messages[0].CardId);
    }

    [Fact]
    public void Decision_SkippedWhenPreconditionFails()
    {
        var (registry, bus) = Setup();
        registry.RegisterDecision(EntityKind.Npc, new TestDecision("blocked", 0.9f, "card_1", canExecute: false));

        var messages = new List<DecisionExecutedMessage>();
        bus.Subscribe(m => { if (m is DecisionExecutedMessage dm) messages.Add(dm); });

        var evaluator = new EntityEvaluator(registry, bus);
        evaluator.Evaluate(CreateEntity(), new StubWorldState());

        Assert.Empty(messages);
    }

    // --- Both phases run ---

    [Fact]
    public void BothProcessAndDecision_CanFireSameTick()
    {
        var (registry, bus) = Setup();
        registry.RegisterProcess(EntityKind.Npc, new TestProcess("eat", 0.5f));
        registry.RegisterDecision(EntityKind.Npc, new TestDecision("wield", 0.5f, "card_1"));

        var processCount = 0;
        var decisionCount = 0;
        bus.Subscribe(m => { if (m is ProcessExecutedMessage) processCount++; });
        bus.Subscribe(m => { if (m is DecisionExecutedMessage) decisionCount++; });

        var evaluator = new EntityEvaluator(registry, bus);
        evaluator.Evaluate(CreateEntity(), new StubWorldState());

        Assert.Equal(1, processCount);
        Assert.Equal(1, decisionCount);
    }

    // --- All entity kinds participate ---

    [Theory]
    [InlineData(EntityKind.Settlement)]
    [InlineData(EntityKind.Nation)]
    [InlineData(EntityKind.Npc)]
    [InlineData(EntityKind.Artifact)]
    [InlineData(EntityKind.Region)]
    public void AllKinds_CanHaveProcesses(EntityKind kind)
    {
        var (registry, bus) = Setup();
        registry.RegisterProcess(kind, new TestProcess("test", 0.5f));

        var messages = new List<ProcessExecutedMessage>();
        bus.Subscribe(m => { if (m is ProcessExecutedMessage pm) messages.Add(pm); });

        var evaluator = new EntityEvaluator(registry, bus);
        evaluator.Evaluate(CreateEntity(kind), new StubWorldState());

        Assert.Single(messages);
        Assert.Equal(kind, messages[0].ActorKind);
    }

    // --- Inactive entities skipped ---

    [Fact]
    public void InactiveEntity_Skipped()
    {
        var (registry, bus) = Setup();
        registry.RegisterProcess(EntityKind.Npc, new TestProcess("eat", 0.5f));

        var messages = new List<ProcessExecutedMessage>();
        bus.Subscribe(m => { if (m is ProcessExecutedMessage pm) messages.Add(pm); });

        var entity = CreateEntity();
        entity.Status = EntityStatus.Historical;

        var evaluator = new EntityEvaluator(registry, bus);
        evaluator.Evaluate(entity, new StubWorldState());

        Assert.Empty(messages);
    }

    // --- EvaluateAll ---

    [Fact]
    public void EvaluateAll_ProcessesAllActiveEntities()
    {
        var (registry, bus) = Setup();
        registry.RegisterProcess(EntityKind.Npc, new TestProcess("eat", 0.5f));
        registry.RegisterProcess(EntityKind.Settlement, new TestProcess("harvest", 0.5f));

        var graph = new WorldGraph();
        graph.CreateEntity(EntityKind.Npc, "generic", "NPC1", "c", 0);
        graph.CreateEntity(EntityKind.Npc, "generic", "NPC2", "c", 0);
        graph.CreateEntity(EntityKind.Settlement, "generic", "Town", "c", 0);

        var messages = new List<ProcessExecutedMessage>();
        bus.Subscribe(m => { if (m is ProcessExecutedMessage pm) messages.Add(pm); });

        var evaluator = new EntityEvaluator(registry, bus);
        evaluator.EvaluateAll(graph, new StubWorldState());

        Assert.Equal(3, messages.Count);
    }

    // --- Registry ---

    [Fact]
    public void Registry_UnregisterByCard_RemovesDecisions()
    {
        var (registry, _) = Setup();
        registry.RegisterDecision(EntityKind.Npc, new TestDecision("a", 0.5f, "card_1"));
        registry.RegisterDecision(EntityKind.Npc, new TestDecision("b", 0.5f, "card_2"));

        registry.UnregisterDecisionsByCard("card_1");

        var decisions = registry.GetDecisions(EntityKind.Npc);
        Assert.Single(decisions);
        Assert.Equal("b", decisions[0].Name);
    }

    // --- Helpers ---

    private static (ActivityRegistry Registry, MessageBus Bus) Setup() =>
        (new ActivityRegistry(), new MessageBus());

    private static Entity CreateEntity(EntityKind kind = EntityKind.Npc) =>
        new(new EntityId(1), kind, "generic", "Test", "c", 0);

    private sealed class TestProcess(string name, float score, bool canExecute = true) : IProcess
    {
        public string Name => name;
        public bool CanExecute(Entity entity, IWorldState worldState) => canExecute;
        public float Score(Entity entity, IWorldState worldState) => score;

        public SimulationMessage Execute(Entity entity, IWorldState worldState) =>
            new ProcessExecutedMessage(worldState.CurrentTick, entity.Id, entity.Kind, name,
                $"{entity.Name} performed {name}");
    }

    private sealed class TestDecision(string name, float score, string? cardId, bool canExecute = true) : IDecision
    {
        public string Name => name;
        public string? CardId => cardId;
        public bool CanExecute(Entity entity, IWorldState worldState) => canExecute;
        public float Score(Entity entity, IWorldState worldState) => score;

        public SimulationMessage Execute(Entity entity, IWorldState worldState) =>
            new DecisionExecutedMessage(worldState.CurrentTick, entity.Id, entity.Kind, name,
                $"{entity.Name} decided to {name}", cardId);
    }
}
