namespace Engine.Tests.Behaviors;

using Engine.Behaviors;
using Engine.Behaviors.Processes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Messages;
using Engine.Simulation;
using Engine.Wiring;

public class SmokeTestProcessTests
{
    [Fact]
    public void AllKinds_HaveRegisteredProcesses()
    {
        var registry = new ActivityRegistry();
        SmokeTestProcesses.RegisterAll(registry);

        Assert.True(registry.HasProcesses(EntityKind.Settlement));
        Assert.True(registry.HasProcesses(EntityKind.Nation));
        Assert.True(registry.HasProcesses(EntityKind.Npc));
        Assert.True(registry.HasProcesses(EntityKind.Artifact));
        Assert.True(registry.HasProcesses(EntityKind.Region));
    }

    [Fact]
    public void Contracts_RegisterWithoutError()
    {
        var validator = new WiringValidator();
        validator.RegisterHealthAttribute(EntityKind.Settlement, "population");
        validator.RegisterHealthAttribute(EntityKind.Nation, "cohesion");
        validator.RegisterHealthAttribute(EntityKind.Npc, "hp");
        validator.RegisterHealthAttribute(EntityKind.Artifact, "condition");
        validator.RegisterHealthAttribute(EntityKind.Region, null);

        SmokeTestProcesses.RegisterContracts(validator);

        var errors = validator.Validate();
        // Smoke test contracts include feedback loops, should pass
        Assert.Empty(errors);
    }

    [Fact]
    public void FullPipeline_ProcessesFireAndEmitMessages()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        var messages = new List<SimulationMessage>();
        sim.MessageBus.Subscribe(m => messages.Add(m));

        // Run one tick
        sim.Tick();

        // Should have received ProcessExecutedMessages from the evaluator
        var processMessages = messages.OfType<ProcessExecutedMessage>().ToList();
        Assert.True(processMessages.Count > 0,
            $"Expected process messages, got {processMessages.Count}");
    }

    [Fact]
    public void FullPipeline_MultipleKindsProcess()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        var kindsSeen = new HashSet<EntityKind>();
        sim.MessageBus.Subscribe(m =>
        {
            if (m is ProcessExecutedMessage pm)
            {
                kindsSeen.Add(pm.ActorKind);
            }
        });

        // Run several ticks to give all kinds a chance
        for (var i = 0; i < 20; i++)
        {
            sim.Tick();
        }

        // Should see processes from multiple entity kinds
        Assert.True(kindsSeen.Count >= 3,
            $"Expected processes from 3+ kinds, got {kindsSeen.Count}: {string.Join(", ", kindsSeen)}");
    }

    [Fact]
    public void FullPipeline_AttributesChangeOverTime()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        var npcId = sim.Graph.GetEntitiesByKind(EntityKind.Npc).First();
        var initialHunger = sim.Attributes.Get(npcId, "hunger");

        for (var i = 0; i < 50; i++)
        {
            sim.Tick();
        }

        var finalHunger = sim.Attributes.Get(npcId, "hunger");
        Assert.NotEqual(initialHunger, finalHunger);
    }

    [Fact]
    public void FullPipeline_100Ticks_NoExceptions()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        for (var i = 0; i < 100; i++)
        {
            sim.Tick();
        }

        Assert.Equal(100, sim.Clock.CurrentTick);
    }
}
