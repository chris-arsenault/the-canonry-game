namespace Engine.Tests.Events;

using Engine.Events;
using Engine.Graph;

public class EventLogTests
{
    private readonly EventLog _log = new(capacity: 100);

    [Fact]
    public void Emit_AddsEvent()
    {
        _log.Emit(1, EventType.Creation, "Settlement founded: Frosthaven", "generator",
            new EntityId(1));

        Assert.Equal(1, _log.Count);
        Assert.Equal(1, _log.TotalEmitted);
    }

    [Fact]
    public void Emit_MostRecentFirst()
    {
        _log.Emit(1, EventType.Creation, "First", "sys");
        _log.Emit(2, EventType.Creation, "Second", "sys");
        _log.Emit(3, EventType.Creation, "Third", "sys");

        var events = _log.All().ToList();
        Assert.Equal("Third", events[0].Description);
        Assert.Equal("First", events[2].Description);
    }

    [Fact]
    public void Emit_EvictsOldestOverCapacity()
    {
        var log = new EventLog(capacity: 3);

        log.Emit(1, EventType.Creation, "A", "sys");
        log.Emit(2, EventType.Creation, "B", "sys");
        log.Emit(3, EventType.Creation, "C", "sys");
        log.Emit(4, EventType.Creation, "D", "sys");

        Assert.Equal(3, log.Count);
        Assert.Equal(4, log.TotalEmitted);

        var events = log.All().ToList();
        Assert.Equal("D", events[0].Description);
        Assert.DoesNotContain(events, e => e.Description == "A");
    }

    [Fact]
    public void Emit_FiresEventEmitted()
    {
        GameEventArgs? received = null;
        _log.EventEmitted += (_, e) => received = e;

        _log.Emit(1, EventType.Lifecycle, "NPC died", "lifecycle", new EntityId(5));

        Assert.NotNull(received);
        Assert.Equal(EventType.Lifecycle, received.Type);
    }

    // --- Filtering ---

    [Fact]
    public void ByType_FiltersCorrectly()
    {
        _log.Emit(1, EventType.Creation, "Created", "gen");
        _log.Emit(2, EventType.Lifecycle, "Died", "lifecycle");
        _log.Emit(3, EventType.Creation, "Created 2", "gen");

        var creations = _log.ByType(EventType.Creation).ToList();
        Assert.Equal(2, creations.Count);
        Assert.All(creations, e => Assert.Equal(EventType.Creation, e.Type));
    }

    [Fact]
    public void ByEntity_FiltersCorrectly()
    {
        var target = new EntityId(42);
        _log.Emit(1, EventType.Creation, "A involves 42", "sys", target);
        _log.Emit(2, EventType.Creation, "B involves 99", "sys", new EntityId(99));
        _log.Emit(3, EventType.Relationship, "C involves 42 and 99", "sys", target, new EntityId(99));

        var events = _log.ByEntity(target).ToList();
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public void ByTickRange_FiltersCorrectly()
    {
        _log.Emit(5, EventType.Creation, "tick 5", "sys");
        _log.Emit(10, EventType.Creation, "tick 10", "sys");
        _log.Emit(15, EventType.Creation, "tick 15", "sys");
        _log.Emit(20, EventType.Creation, "tick 20", "sys");

        var events = _log.ByTickRange(8, 16).ToList();
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.InRange(e.Tick, 8, 16));
    }

    [Fact]
    public void Recent_ReturnsTopN()
    {
        for (var i = 0; i < 10; i++)
        {
            _log.Emit(i, EventType.Creation, $"Event {i}", "sys");
        }

        var recent = _log.Recent(3).ToList();
        Assert.Equal(3, recent.Count);
        Assert.Equal("Event 9", recent[0].Description);
    }

    [Fact]
    public void ByEntity_ReturnsEmptyForNonexistent()
    {
        _log.Emit(1, EventType.Creation, "A", "sys", new EntityId(1));

        Assert.Empty(_log.ByEntity(new EntityId(999)));
    }

    [Fact]
    public void Clear_RemovesAllEvents()
    {
        _log.Emit(1, EventType.Creation, "A", "sys");
        _log.Emit(2, EventType.Creation, "B", "sys");

        _log.Clear();

        Assert.Equal(0, _log.Count);
        Assert.Equal(2, _log.TotalEmitted); // Total is preserved
    }

    // --- GameEventArgs properties ---

    [Fact]
    public void GameEventArgs_StoresAllProperties()
    {
        var e = new GameEventArgs(42, EventType.Conflict, "War declared",
            "action_executor", [new EntityId(1), new EntityId(2)]);

        Assert.Equal(42, e.Tick);
        Assert.Equal(EventType.Conflict, e.Type);
        Assert.Equal("War declared", e.Description);
        Assert.Equal("action_executor", e.SourceSystem);
        Assert.Equal(2, e.InvolvedEntities.Count);
    }

    [Fact]
    public void GameEventArgs_DefaultsToEmptyEntities()
    {
        var e = new GameEventArgs(1, EventType.Card, "Card fired", "tableau");

        Assert.Empty(e.InvolvedEntities);
    }
}
