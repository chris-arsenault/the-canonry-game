namespace Engine.Tests.Attributes;

using Engine.Attributes;
using Engine.Graph;

public class SemanticPlaneTests
{
    private static SemanticPlaneDefinition CreateGovernmentPlane() => new(
        "government",
        "authoritarian_libertarian",
        "secular_theocratic",
        [
            "authoritarian-secular", "moderate-secular", "libertarian-secular",
            "authoritarian-moderate", "moderate-moderate", "libertarian-moderate",
            "authoritarian-theocratic", "moderate-theocratic", "libertarian-theocratic",
        ]);

    // --- Definition ---

    [Fact]
    public void Definition_RequiresExactly9Labels()
    {
        Assert.Throws<ArgumentException>(() => new SemanticPlaneDefinition(
            "bad", "x", "y", ["only", "three", "labels"]));
    }

    // --- Reducer ---

    [Theory]
    [InlineData(0.1f, 0.1f, "authoritarian-secular")]
    [InlineData(0.5f, 0.5f, "moderate-moderate")]
    [InlineData(0.9f, 0.9f, "libertarian-theocratic")]
    [InlineData(0.1f, 0.9f, "authoritarian-theocratic")]
    [InlineData(0.9f, 0.1f, "libertarian-secular")]
    public void Reduce_MapsPositionToCorrectLabel(float x, float y, string expectedLabel)
    {
        var plane = CreateGovernmentPlane();
        Assert.Equal(expectedLabel, plane.Reduce(x, y));
    }

    // --- Store ---

    [Fact]
    public void SetAndGetPosition()
    {
        var store = new SemanticPlaneStore();
        store.RegisterPlane(CreateGovernmentPlane());

        var id = new EntityId(1);
        store.SetPosition(id, "government", 0.8f, 0.2f);

        var pos = store.GetPosition(id, "government");
        Assert.NotNull(pos);
        Assert.Equal(0.8f, pos.Value.X);
        Assert.Equal(0.2f, pos.Value.Y);
    }

    [Fact]
    public void SetPosition_ClampsToRange()
    {
        var store = new SemanticPlaneStore();
        store.RegisterPlane(CreateGovernmentPlane());

        var id = new EntityId(1);
        store.SetPosition(id, "government", 1.5f, -0.3f);

        var pos = store.GetPosition(id, "government");
        Assert.NotNull(pos);
        Assert.Equal(1.0f, pos.Value.X);
        Assert.Equal(0.0f, pos.Value.Y);
    }

    [Fact]
    public void GetLabel_ReturnsReducedLabel()
    {
        var store = new SemanticPlaneStore();
        store.RegisterPlane(CreateGovernmentPlane());

        var id = new EntityId(1);
        store.SetPosition(id, "government", 0.8f, 0.2f);

        Assert.Equal("libertarian-secular", store.GetLabel(id, "government"));
    }

    [Fact]
    public void GetLabel_ReturnsNullForNoPosition()
    {
        var store = new SemanticPlaneStore();
        store.RegisterPlane(CreateGovernmentPlane());

        Assert.Null(store.GetLabel(new EntityId(999), "government"));
    }

    [Fact]
    public void GetLabel_ReturnsNullForUndefinedPlane()
    {
        var store = new SemanticPlaneStore();
        var id = new EntityId(1);

        Assert.Null(store.GetLabel(id, "nonexistent"));
    }

    // --- Distance ---

    [Fact]
    public void GetDistance_ComputesEuclidean()
    {
        var store = new SemanticPlaneStore();
        store.RegisterPlane(CreateGovernmentPlane());

        var a = new EntityId(1);
        var b = new EntityId(2);
        store.SetPosition(a, "government", 0.0f, 0.0f);
        store.SetPosition(b, "government", 0.3f, 0.4f);

        var dist = store.GetDistance(a, b, "government");
        Assert.NotNull(dist);
        Assert.Equal(0.5f, dist.Value, precision: 4); // 3-4-5 triangle
    }

    [Fact]
    public void GetDistance_ReturnsNullWhenMissing()
    {
        var store = new SemanticPlaneStore();
        store.RegisterPlane(CreateGovernmentPlane());

        var a = new EntityId(1);
        store.SetPosition(a, "government", 0.5f, 0.5f);

        Assert.Null(store.GetDistance(a, new EntityId(999), "government"));
    }

    [Fact]
    public void GetDistance_ZeroForSamePosition()
    {
        var store = new SemanticPlaneStore();
        store.RegisterPlane(CreateGovernmentPlane());

        var a = new EntityId(1);
        var b = new EntityId(2);
        store.SetPosition(a, "government", 0.5f, 0.5f);
        store.SetPosition(b, "government", 0.5f, 0.5f);

        Assert.Equal(0f, store.GetDistance(a, b, "government"));
    }

    [Fact]
    public void RemoveEntity_ClearsPositions()
    {
        var store = new SemanticPlaneStore();
        store.RegisterPlane(CreateGovernmentPlane());

        var id = new EntityId(1);
        store.SetPosition(id, "government", 0.5f, 0.5f);

        store.RemoveEntity(id);

        Assert.Null(store.GetPosition(id, "government"));
    }
}
