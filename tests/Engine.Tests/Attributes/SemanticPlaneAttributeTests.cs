namespace Engine.Tests.Attributes;

using Engine.Attributes;
using Engine.Tests;

public class SemanticPlaneAttributeTests
{
    [Fact]
    public void Components_HaveIndependentStaticEffects()
    {
        var plane = new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0.2f, 0.8f);
        plane.AddStaticEffect(new AttributeEffect("x", "x", 0.3f), "x");
        plane.AddStaticEffect(new AttributeEffect("y", "y", -0.2f), "y");

        Assert.Equal(0.5f, plane.X, precision: 3);
        Assert.Equal(0.6f, plane.Y, precision: 3);
    }

    [Fact]
    public void ModifyXAndModifyY_ClampToBounds()
    {
        var plane = new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0.2f, 0.8f);

        plane.ModifyX(2f);
        plane.ModifyY(-2f);

        Assert.Equal(1f, plane.X);
        Assert.Equal(0f, plane.Y);
    }

    [Fact]
    public void Label_ReducesCurrentPositionThroughDefinition()
    {
        var plane = new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0.2f, 0.8f);

        Assert.Equal("low-high", plane.Label);
    }

    [Fact]
    public void DistanceTo_ComputesEuclideanDistance()
    {
        var a = new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0f, 0f);
        var b = new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0.3f, 0.4f);

        Assert.Equal(0.5f, a.DistanceTo(b), precision: 3);
    }

    [Fact]
    public void ApplyTickEffects_UpdatesComponentsIndependently()
    {
        var plane = new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0.2f, 0.8f);
        plane.AddTickEffect(new AttributeEffect("x", "x", 0.1f), "x");
        plane.AddTickEffect(new AttributeEffect("y", "y", -0.2f), "y");

        plane.ApplyTickEffects();

        Assert.Equal(0.3f, plane.X, precision: 3);
        Assert.Equal(0.6f, plane.Y, precision: 3);
    }

    [Fact]
    public void RemoveEffectsBySource_RemovesPerComponentEffectsWithoutCrossTalk()
    {
        var plane = new SemanticPlane("ideology", TestHelpers.CreateIdeologyPlane(), 0f, 1f, 0.2f, 0.8f);
        plane.AddTickEffect(new AttributeEffect("shared", "x", 0.1f), "x");
        plane.AddStaticEffect(new AttributeEffect("shared", "y", -0.2f), "y");
        plane.AddStaticEffect(new AttributeEffect("other", "x", 0.3f), "x");

        plane.RemoveEffectsBySource("shared");

        Assert.Empty(plane.TickEffectsX);
        Assert.Empty(plane.StaticEffectsY);
        Assert.Single(plane.StaticEffectsX);
    }
}
