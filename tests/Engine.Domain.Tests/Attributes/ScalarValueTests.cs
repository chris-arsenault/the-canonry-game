namespace Engine.Domain.Tests.Attributes;

using Engine.Attributes;

public class ScalarValueTests
{
    [Fact]
    public void Value_ReturnsBaseWhenNoStaticEffectsExist()
    {
        var value = new ScalarValue("population", 0f, 100f, 10f);

        Assert.Equal(10f, value.Value);
    }

    [Fact]
    public void Value_ComposesStaticEffectsFromRequirements()
    {
        var value = new ScalarValue("population", 0f, 100f, 10f);
        value.AddStaticEffect(new AttributeEffect("bonus", "bonus", 5f));
        value.AddStaticEffect(new AttributeEffect("multiplier", "multiplier", 2f, isMultiplier: true));

        Assert.Equal(30f, value.Value);
    }

    [Fact]
    public void Modify_ChangesBaseAndClampsToMinMax()
    {
        var value = new ScalarValue("population", 0f, 100f, 10f);

        value.Modify(200f);
        Assert.Equal(100f, value.Base);

        value.Modify(-500f);
        Assert.Equal(0f, value.Base);
    }

    [Fact]
    public void ApplyTickEffects_ChangesBaseByScaledTickDelta()
    {
        var value = new ScalarValue("population", 0f, 100f, 10f);
        value.AddTickEffect(new AttributeEffect("delta", "delta", 5f));
        value.AddTickEffect(new AttributeEffect("multiplier", "multiplier", 2f, isMultiplier: true));

        value.ApplyTickEffects();

        Assert.Equal(20f, value.Base);
    }

    [Fact]
    public void Trend_TracksAverageOfRecentDeltas()
    {
        var value = new ScalarValue("population", 0f, 100f, 10f);

        value.Modify(4f);
        value.Modify(-2f);
        value.Modify(6f);

        Assert.Equal(8f / 3f, value.Trend, precision: 3);
    }

    [Fact]
    public void RemoveEffectsBySource_RemovesTickAndStaticEffects()
    {
        var value = new ScalarValue("population", 0f, 100f, 10f);
        value.AddTickEffect(new AttributeEffect("shared", "tick", 2f));
        value.AddStaticEffect(new AttributeEffect("shared", "static", 3f));
        value.AddTickEffect(new AttributeEffect("other", "tick", 1f));

        value.RemoveEffectsBySource("shared");

        Assert.Single(value.TickEffects);
        Assert.Empty(value.StaticEffects);
        Assert.Equal("other", value.TickEffects[0].SourceId);
    }
}
