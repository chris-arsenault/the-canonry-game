namespace Engine.Tests.Persistence;

using Engine.Attributes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Persistence;

public class SaveManagerTests
{
    [Fact]
    public void Capture_CapturesEntities()
    {
        var graph = new WorldGraph();

        graph.CreateEntity(EntityKind.Nation, "monarchy", "Kesh", "culture_a", tick: 0);
        graph.CreateEntity(EntityKind.Settlement, "city", "Capital", "culture_a", tick: 0, (1, 2));

        var state = SaveManager.Capture(graph, currentTick: 50);

        Assert.Equal(2, state.Entities.Count);
        Assert.Equal(50, state.CurrentTick);
    }

    [Fact]
    public void Capture_CapturesRelationships()
    {
        var graph = new WorldGraph();

        var nation = graph.CreateEntity(EntityKind.Nation, "generic", "N", "c", 0);
        var settlement = graph.CreateEntity(EntityKind.Settlement, "generic", "S", "c", 0);
        graph.CreateRelationship("controls", RelationshipPolarity.Neutral, nation.Id, settlement.Id, 0.9f, 0);

        var state = SaveManager.Capture(graph, currentTick: 10);

        Assert.Single(state.Relationships);
        Assert.Equal("controls", state.Relationships[0].Kind);
        Assert.Equal(0.9f, state.Relationships[0].Strength);
    }

    [Fact]
    public void Capture_CapturesAttributes()
    {
        var graph = new WorldGraph();

        var entity = graph.CreateEntity(EntityKind.Npc, "hero", "Captain", "c", 0);
        entity.Attributes.Add(new ScalarValue("hp", 0, 100, 85));
        entity.Attributes.Add(new ScalarValue("hunger", 0, 100, 15));

        var state = SaveManager.Capture(graph, currentTick: 20);

        Assert.Equal(2, state.Attributes.Count);
    }

    [Fact]
    public void Capture_CapturesCoordinates()
    {
        var graph = new WorldGraph();

        graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (3, -1));

        var state = SaveManager.Capture(graph, currentTick: 0);

        Assert.Equal(3, state.Entities[0].CoordQ);
        Assert.Equal(-1, state.Entities[0].CoordR);
    }

    [Fact]
    public void Capture_NullCoordinatesForNonSpatial()
    {
        var graph = new WorldGraph();

        graph.CreateEntity(EntityKind.Nation, "generic", "N", "c", 0);

        var state = SaveManager.Capture(graph, currentTick: 0);

        Assert.Null(state.Entities[0].CoordQ);
        Assert.Null(state.Entities[0].CoordR);
    }

    // --- Serialization round-trip ---

    [Fact]
    public void RoundTrip_PreservesState()
    {
        var graph = new WorldGraph();

        var nation = graph.CreateEntity(EntityKind.Nation, "monarchy", "Kesh", "culture_a", tick: 0);
        var settlement = graph.CreateEntity(EntityKind.Settlement, "city", "Capital", "culture_a", tick: 5, (1, 2));
        nation.Attributes.Add(new ScalarValue("stability", 0, 100, 75));
        settlement.Attributes.Add(new ScalarValue("population", 0, 10000, 500));
        graph.CreateRelationship("controls", RelationshipPolarity.Neutral, nation.Id, settlement.Id, 0.9f, 5);

        var original = SaveManager.Capture(graph, currentTick: 42, currentMana: 7f, manaCap: 12f);

        // Serialize and deserialize
        var bytes = SaveManager.Serialize(original);
        var restored = SaveManager.Deserialize(bytes);

        // Verify
        Assert.Equal(42, restored.CurrentTick);
        Assert.Equal(7f, restored.CurrentMana);
        Assert.Equal(12f, restored.ManaCap);
        Assert.Equal(2, restored.Entities.Count);
        Assert.Single(restored.Relationships);
        Assert.Equal(2, restored.Attributes.Count);
        Assert.Equal(1, restored.Version);

        // Entity details
        var nationData = restored.Entities.First(e => e.Kind == EntityKind.Nation);
        Assert.Equal("monarchy", nationData.Subtype);
        Assert.Equal("Kesh", nationData.Name);
        Assert.Equal("culture_a", nationData.Culture);

        var settlementData = restored.Entities.First(e => e.Kind == EntityKind.Settlement);
        Assert.Equal(1, settlementData.CoordQ);
        Assert.Equal(2, settlementData.CoordR);

        // Relationship details
        Assert.Equal("controls", restored.Relationships[0].Kind);
        Assert.Equal(0.9f, restored.Relationships[0].Strength);

        // Attribute details
        var stabilityAttr = restored.Attributes.First(a => a.Name == "stability");
        Assert.Equal(75f, stabilityAttr.Value);
    }

    [Fact]
    public void RoundTrip_CompactBinaryFormat()
    {
        var graph = new WorldGraph();

        // Create a moderate world
        for (var i = 0; i < 50; i++)
        {
            var e = graph.CreateEntity(EntityKind.Settlement, "village", $"S{i}", "c", 0, (i % 10, i / 10));
            e.Attributes.Add(new ScalarValue("population", 0, 10000, 100 + i));
            e.Attributes.Add(new ScalarValue("development", 0, 100, 10));
        }

        var state = SaveManager.Capture(graph, currentTick: 100);
        var bytes = SaveManager.Serialize(state);

        // 50 entities with 2 attributes each should be well under 50KB
        Assert.True(bytes.Length < 50000, $"Save file too large: {bytes.Length} bytes");
        Assert.True(bytes.Length > 0);
    }

    // --- File operations ---

    [Fact]
    public void SaveAndLoad_FileRoundTrip()
    {
        var graph = new WorldGraph();
        graph.CreateEntity(EntityKind.Nation, "generic", "TestNation", "c", 0);

        var state = SaveManager.Capture(graph, currentTick: 99);
        var path = Path.GetTempFileName();

        try
        {
            SaveManager.SaveToFile(state, path);
            var loaded = SaveManager.LoadFromFile(path);

            Assert.Equal(99, loaded.CurrentTick);
            Assert.Single(loaded.Entities);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Version_IsSet()
    {
        var state = new SaveState();
        Assert.Equal(1, state.Version);
    }
}
