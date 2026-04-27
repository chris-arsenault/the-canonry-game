namespace Engine.Domain.Tests.Attributes;

using Engine.Attributes;
public class AttributeSetTests
{
    [Fact]
    public void GetScalar_ReturnsScalarAttributesOrNull()
    {
        var attributes = new AttributeSet();
        attributes.Add(new ScalarValue("population", 0f, 100f, 10f));

        Assert.NotNull(attributes.GetScalar("population"));
        Assert.Null(attributes.GetScalar("missing"));
    }

    [Fact]
    public void GetValue_ReturnsScalarValueAndZeroForMissingAttribute()
    {
        var attributes = new AttributeSet();
        attributes.Add(new ScalarValue("population", 0f, 100f, 10f));

        Assert.Equal(10f, attributes.GetValue("population"));
        Assert.Equal(0f, attributes.GetValue("missing"));
    }

    [Fact]
    public void Modify_ChangesScalarsAndIgnoresMissingAttribute()
    {
        var attributes = new AttributeSet();
        attributes.Add(new ScalarValue("population", 0f, 100f, 10f));

        attributes.Modify("population", 5f);
        attributes.Modify("missing", 5f);

        Assert.Equal(15f, attributes.GetValue("population"));
    }

    [Fact]
    public void ApplyTickEffects_AppliesToScalarAttributes()
    {
        var attributes = new AttributeSet();
        var scalar = new ScalarValue("population", 0f, 100f, 10f);
        scalar.AddTickEffect(new AttributeEffect("tick", "tick", 5f));

        attributes.Add(scalar);

        attributes.ApplyTickEffects();

        Assert.Equal(15f, scalar.Base);
    }

    [Fact]
    public void RemoveEffectsBySource_AppliesAcrossScalarAttributes()
    {
        var attributes = new AttributeSet();
        var scalar = new ScalarValue("population", 0f, 100f, 10f);
        scalar.AddTickEffect(new AttributeEffect("shared", "tick", 5f));

        attributes.Add(scalar);

        attributes.RemoveEffectsBySource("shared");

        Assert.Empty(scalar.TickEffects);
    }
}
