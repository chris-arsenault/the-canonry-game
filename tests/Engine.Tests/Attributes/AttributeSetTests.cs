namespace Engine.Tests.Attributes;

using Engine.Attributes;
using Engine.Tests;

public class AttributeSetTests
{
    [Fact]
    public void GetScalarAndGetPlane_ReturnTypedAttributesOrNull()
    {
        var attributes = new AttributeSet();
        attributes.Add(new ScalarValue("population", 0f, 100f, 10f));
        attributes.Add(new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0.2f, 0.8f));

        Assert.NotNull(attributes.GetScalar("population"));
        Assert.Null(attributes.GetScalar("ideology"));
        Assert.NotNull(attributes.GetPlane("ideology"));
        Assert.Null(attributes.GetPlane("population"));
    }

    [Fact]
    public void GetValue_ReturnsScalarValueAndZeroForPlanes()
    {
        var attributes = new AttributeSet();
        attributes.Add(new ScalarValue("population", 0f, 100f, 10f));
        attributes.Add(new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0.2f, 0.8f));

        Assert.Equal(10f, attributes.GetValue("population"));
        Assert.Equal(0f, attributes.GetValue("ideology"));
    }

    [Fact]
    public void Modify_ChangesScalarsAndIsNoOpForPlanes()
    {
        var attributes = new AttributeSet();
        attributes.Add(new ScalarValue("population", 0f, 100f, 10f));
        attributes.Add(new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0.2f, 0.8f));

        attributes.Modify("population", 5f);
        attributes.Modify("ideology", 5f);

        Assert.Equal(15f, attributes.GetValue("population"));
        var plane = attributes.GetPlane("ideology")!;
        Assert.Equal(0.2f, plane.X, precision: 3);
        Assert.Equal(0.8f, plane.Y, precision: 3);
    }

    [Fact]
    public void ApplyTickEffects_AppliesToAllAttributes()
    {
        var attributes = new AttributeSet();
        var scalar = new ScalarValue("population", 0f, 100f, 10f);
        scalar.AddTickEffect(new AttributeEffect("tick", "tick", 5f));
        var plane = new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0.2f, 0.8f);
        plane.AddTickEffect(new AttributeEffect("tick", "tick", 0.1f), "x");
        plane.AddTickEffect(new AttributeEffect("tick", "tick", -0.2f), "y");

        attributes.Add(scalar);
        attributes.Add(plane);

        attributes.ApplyTickEffects();

        Assert.Equal(15f, scalar.Base);
        Assert.Equal(0.3f, plane.X, precision: 3);
        Assert.Equal(0.6f, plane.Y, precision: 3);
    }

    [Fact]
    public void RemoveEffectsBySource_AppliesAcrossAllAttributes()
    {
        var attributes = new AttributeSet();
        var scalar = new ScalarValue("population", 0f, 100f, 10f);
        scalar.AddTickEffect(new AttributeEffect("shared", "tick", 5f));
        var plane = new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0.2f, 0.8f);
        plane.AddStaticEffect(new AttributeEffect("shared", "static", 0.1f), "x");

        attributes.Add(scalar);
        attributes.Add(plane);

        attributes.RemoveEffectsBySource("shared");

        Assert.Empty(scalar.TickEffects);
        Assert.Empty(plane.StaticEffectsX);
    }
}
