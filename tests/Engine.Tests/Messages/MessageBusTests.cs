namespace Engine.Tests.Messages;

using Engine.Graph;
using Engine.Kinds;
using Engine.Messages;

public class MessageBusTests
{
    private readonly MessageBus _bus = new();

    [Fact]
    public void Subscribe_ReceivesPublishedMessages()
    {
        SimulationMessage? received = null;
        _bus.Subscribe(m => received = m);

        _bus.Publish(new ProcessExecutedMessage(1, new EntityId(1), EntityKind.Npc, "eat", "NPC ate"));

        Assert.NotNull(received);
        Assert.IsType<ProcessExecutedMessage>(received);
    }

    [Fact]
    public void Subscribe_ReceivesAllMessageTypes()
    {
        var messages = new List<SimulationMessage>();
        _bus.Subscribe(m => messages.Add(m));

        _bus.Publish(new ProcessExecutedMessage(1, new EntityId(1), EntityKind.Npc, "eat", "ate"));
        _bus.Publish(new DecisionExecutedMessage(2, new EntityId(2), EntityKind.Nation, "expand", "expanded"));
        _bus.Publish(new EntityDiedMessage(3, new EntityId(3), EntityKind.Npc, "Hero"));

        Assert.Equal(3, messages.Count);
    }

    [Fact]
    public void Subscribe_PatternMatchForSpecificType()
    {
        var deaths = new List<EntityDiedMessage>();
        _bus.Subscribe(m =>
        {
            if (m is EntityDiedMessage death)
            {
                deaths.Add(death);
            }
        });

        _bus.Publish(new ProcessExecutedMessage(1, new EntityId(1), EntityKind.Npc, "eat", "ate"));
        _bus.Publish(new EntityDiedMessage(2, new EntityId(2), EntityKind.Npc, "Hero"));
        _bus.Publish(new ProcessExecutedMessage(3, new EntityId(3), EntityKind.Settlement, "harvest", "harvested"));

        Assert.Single(deaths);
        Assert.Equal("Hero", deaths[0].Name);
    }

    [Fact]
    public void MultipleSubscribers_AllReceive()
    {
        var count = 0;
        _bus.Subscribe(_ => count++);
        _bus.Subscribe(_ => count++);

        _bus.Publish(new ProcessExecutedMessage(1, new EntityId(1), EntityKind.Npc, "eat", "ate"));

        Assert.Equal(2, count);
    }

    [Fact]
    public void Clear_RemovesAllSubscribers()
    {
        var received = false;
        _bus.Subscribe(_ => received = true);

        _bus.Clear();

        _bus.Publish(new ProcessExecutedMessage(1, new EntityId(1), EntityKind.Npc, "eat", "ate"));

        Assert.False(received);
    }

    // --- Message properties ---

    [Fact]
    public void ProcessExecutedMessage_CarriesAllData()
    {
        var affected = new List<EntityId> { new(10), new(20) };
        var msg = new ProcessExecutedMessage(42, new EntityId(1), EntityKind.Settlement,
            "harvest", "Settlement harvested crops", affected);

        Assert.Equal(42, msg.Tick);
        Assert.Equal(new EntityId(1), msg.ActorId);
        Assert.Equal(EntityKind.Settlement, msg.ActorKind);
        Assert.Equal("harvest", msg.ProcessName);
        Assert.Equal(2, msg.AffectedEntities.Count);
    }

    [Fact]
    public void DecisionExecutedMessage_CarriesCardInfo()
    {
        var msg = new DecisionExecutedMessage(10, new EntityId(5), EntityKind.Nation,
            "expand_borders", "Northern Compact expanded east",
            cardId: "card_expand", succeeded: true);

        Assert.Equal("card_expand", msg.CardId);
        Assert.True(msg.Succeeded);
    }

    [Fact]
    public void EntityDiedMessage_CarriesIdentity()
    {
        var msg = new EntityDiedMessage(500, new EntityId(7), EntityKind.Npc, "Captain Greyfeather");

        Assert.Equal(EntityKind.Npc, msg.Kind);
        Assert.Equal("Captain Greyfeather", msg.Name);
    }

    // --- Integration: EventLog as subscriber ---

    [Fact]
    public void EventLog_CanSubscribeViaPatternMatch()
    {
        var eventLog = new Engine.Events.EventLog();

        _bus.Subscribe(msg =>
        {
            if (msg is EntityDiedMessage death)
            {
                eventLog.Emit(death.Tick, Engine.Events.EventType.Lifecycle,
                    $"{death.Name} ({death.Kind}) died", "lifecycle", death.EntityId);
            }
        });

        _bus.Publish(new ProcessExecutedMessage(1, new EntityId(1), EntityKind.Npc, "eat", "ate"));
        _bus.Publish(new EntityDiedMessage(42, new EntityId(5), EntityKind.Npc, "Hero"));

        var events = eventLog.ByType(Engine.Events.EventType.Lifecycle).ToList();
        Assert.Single(events);
        Assert.Contains("Hero", events[0].Description, System.StringComparison.Ordinal);
    }
}
