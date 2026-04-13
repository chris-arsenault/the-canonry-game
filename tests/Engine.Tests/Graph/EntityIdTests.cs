namespace Engine.Tests.Graph;

using Engine.Graph;

public class EntityIdTests
{
    [Fact]
    public void EntityId_EqualityWorks()
    {
        var a = new EntityId(1);
        var b = new EntityId(1);
        var c = new EntityId(2);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void EntityId_ToStringFormat()
    {
        var id = new EntityId(42);
        Assert.Equal("E42", id.ToString());
    }

    [Fact]
    public void EntityId_WorksAsDictionaryKey()
    {
        var dict = new Dictionary<EntityId, string>
        {
            [new EntityId(1)] = "first",
            [new EntityId(2)] = "second",
        };

        Assert.Equal("first", dict[new EntityId(1)]);
        Assert.False(dict.ContainsKey(new EntityId(3)));
    }
}
