namespace Engine.Tests.Attributes;

using Engine.Attributes;
using Engine.Graph;

public class AttributeStoreTests
{
    private readonly AttributeStore _store = new();

    [Fact]
    public void Initialize_SetsValueClamped()
    {
        var id = new EntityId(1);
        _store.Initialize(id, "hp", 0f, 100f, 50f);
        Assert.Equal(50f, _store.Get(id, "hp"));
    }

    [Fact]
    public void Initialize_ClampsAboveMax()
    {
        var id = new EntityId(1);
        _store.Initialize(id, "hp", 0f, 100f, 200f);
        Assert.Equal(100f, _store.Get(id, "hp"));
    }

    [Fact]
    public void Get_ReturnsZeroForUninitialized()
    {
        Assert.Equal(0f, _store.Get(new EntityId(999), "nonexistent"));
    }

    [Fact]
    public void Set_ClampsToRange()
    {
        var id = new EntityId(1);
        _store.Initialize(id, "hp", 0f, 100f, 50f);

        _store.Set(id, "hp", 150f);
        Assert.Equal(100f, _store.Get(id, "hp"));

        _store.Set(id, "hp", -20f);
        Assert.Equal(0f, _store.Get(id, "hp"));
    }

    [Fact]
    public void Modify_AppliesDelta()
    {
        var id = new EntityId(1);
        _store.Initialize(id, "hp", 0f, 100f, 50f);

        _store.Modify(id, "hp", 10f);
        Assert.Equal(60f, _store.Get(id, "hp"));

        _store.Modify(id, "hp", -30f);
        Assert.Equal(30f, _store.Get(id, "hp"));
    }

    [Fact]
    public void Modify_ClampsResult()
    {
        var id = new EntityId(1);
        _store.Initialize(id, "hp", 0f, 100f, 90f);

        _store.Modify(id, "hp", 50f);
        Assert.Equal(100f, _store.Get(id, "hp"));
    }

    [Fact]
    public void GetTrend_ReturnsAverageDelta()
    {
        var id = new EntityId(1);
        _store.Initialize(id, "hp", 0f, 100f, 50f);

        _store.Modify(id, "hp", 10f); // delta +10
        _store.Modify(id, "hp", -4f); // delta -4
        // Average = (10 + -4) / 2 = 3

        Assert.Equal(3f, _store.GetTrend(id, "hp"));
    }

    [Fact]
    public void GetTrend_ReturnsZeroWithNoDeltas()
    {
        var id = new EntityId(1);
        _store.Initialize(id, "hp", 0f, 100f, 50f);

        Assert.Equal(0f, _store.GetTrend(id, "hp"));
    }

    [Fact]
    public void Has_ReturnsTrueForInitialized()
    {
        var id = new EntityId(1);
        _store.Initialize(id, "hp", 0f, 100f, 50f);

        Assert.True(_store.Has(id, "hp"));
        Assert.False(_store.Has(id, "nonexistent"));
    }

    [Fact]
    public void GetAttributeNames_ReturnsAllForEntity()
    {
        var id = new EntityId(1);
        _store.Initialize(id, "hp", 0, 100, 50);
        _store.Initialize(id, "hunger", 0, 100, 0);
        _store.Initialize(id, "ambition", 0, 100, 50);

        var names = _store.GetAttributeNames(id).ToHashSet();
        Assert.Equal(3, names.Count);
        Assert.Contains("hp", names);
        Assert.Contains("hunger", names);
        Assert.Contains("ambition", names);
    }

    [Fact]
    public void RemoveEntity_ClearsAllAttributes()
    {
        var id = new EntityId(1);
        _store.Initialize(id, "hp", 0, 100, 50);
        _store.Initialize(id, "hunger", 0, 100, 0);

        _store.RemoveEntity(id);

        Assert.False(_store.Has(id, "hp"));
        Assert.False(_store.Has(id, "hunger"));
    }
}
