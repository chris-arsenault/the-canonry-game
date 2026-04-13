namespace Engine.Tests.Messages;

using Engine.Graph;
using Engine.Kinds;
using Engine.Messages;

public class MessageBusTests
{
    private readonly MessageBus _bus = new();

    [Fact]
    public void Subscribe_ReceivesCorrectType()
    {
        ProcessExecutedMessage? received = null;
        _bus.Subscribe<ProcessExecutedMessage>(m => received = m);

        _bus.Publish(new ProcessExecutedMessage(1, new EntityId(1), EntityKind.Npc, "eat", "NPC ate"));

        Assert.NotNull(received);
        Assert.Equal("eat", received.ProcessName);
    }

    [Fact]
    public void Subscribe_DoesNotReceiveWrongType()
    {
        ProcessExecutedMessage? received = null;
        _bus.Subscribe<ProcessExecutedMessage>(m => received = m);

        _bus.Publish(new DecisionExecutedMessage(1, new EntityId(1), EntityKind.Nation, "expand", "Nation expanded"));

        Assert.Null(received);
    }

    [Fact]
    public void SubscribeAll_ReceivesEverything()
    {
        var messages = new List<SimulationMessage>();
        _bus.SubscribeAll(m => messages.Add(m));

        _bus.Publish(new ProcessExecutedMessage(1, new EntityId(1), EntityKind.Npc, "eat", "ate"));
        _bus.Publish(new DecisionExecutedMessage(2, new EntityId(2), EntityKind.Nation, "expand", "expanded"));
        _bus.Publish(new EntityDiedMessage(3, new EntityId(3), EntityKind.Npc, "Hero"));

        Assert.Equal(3, messages.Count);
    }

    [Fact]
    public void MultipleSubscribers_AllReceive()
    {
        var count = 0;
        _bus.Subscribe<ProcessExecutedMessage>(_ => count++);
        _bus.Subscribe<ProcessExecutedMessage>(_ => count++);

        _bus.Publish(new ProcessExecutedMessage(1, new EntityId(1), EntityKind.Npc, "eat", "ate"));

        Assert.Equal(2, count);
    }

    [Fact]
    public void Clear_RemovesAllSubscribers()
    {
        var received = false;
        _bus.Subscribe<ProcessExecutedMessage>(_ => received = true);
        _bus.SubscribeAll(_ => received = true);

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
        Assert.Equal("Settlement harvested crops", msg.Description);
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
    public void DecisionExecutedMessage_TracksFailure()
    {
        var msg = new DecisionExecutedMessage(10, new EntityId(5), EntityKind.Nation,
            "forge_alliance", "Alliance attempt failed",
            succeeded: false);

        Assert.False(msg.Succeeded);
    }

    [Fact]
    public void EntityMovedMessage_CarriesCoordinates()
    {
        var msg = new EntityMovedMessage(5, new EntityId(1), (0, -2), (1, -2));

        Assert.Equal((0, -2), msg.From);
        Assert.Equal((1, -2), msg.To);
    }

    [Fact]
    public void SubtypeChangedMessage_CarriesBothTypes()
    {
        var msg = new SubtypeChangedMessage(100, new EntityId(3), "village", "town");

        Assert.Equal("village", msg.OldSubtype);
        Assert.Equal("town", msg.NewSubtype);
    }

    [Fact]
    public void EntityDiedMessage_CarriesIdentity()
    {
        var msg = new EntityDiedMessage(500, new EntityId(7), EntityKind.Npc, "Captain Greyfeather");

        Assert.Equal(EntityKind.Npc, msg.Kind);
        Assert.Equal("Captain Greyfeather", msg.Name);
    }

    [Fact]
    public void DegeneracyWarningMessage_CarriesSeverity()
    {
        var msg = new DegeneracyWarningMessage(10, "npc_extinction",
            Engine.Degeneracy.Severity.Warning, "NPC population low");

        Assert.Equal(Engine.Degeneracy.Severity.Warning, msg.Severity);
    }

    // --- Integration: EventLog as subscriber ---

    [Fact]
    public void EventLog_CanSubscribeToMessageBus()
    {
        var eventLog = new Engine.Events.EventLog();
        var bus = new MessageBus();

        // EventLog subscribes to death messages
        bus.Subscribe<EntityDiedMessage>(msg =>
            eventLog.Emit(msg.Tick, Engine.Events.EventType.Lifecycle,
                $"{msg.Name} ({msg.Kind}) died", "lifecycle", msg.EntityId));

        bus.Publish(new EntityDiedMessage(42, new EntityId(5), EntityKind.Npc, "Hero"));

        var events = eventLog.ByType(Engine.Events.EventType.Lifecycle).ToList();
        Assert.Single(events);
        Assert.Contains("Hero", events[0].Description, System.StringComparison.Ordinal);
    }
}
