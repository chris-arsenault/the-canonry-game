namespace Engine.Tests.Attributes;

using Engine.Attributes;
using Engine.Graph;

public class EffectSystemTests
{
    private readonly EffectSystem _effects = new();

    // --- Basic Effects ---

    [Fact]
    public void SingleEffect_ReturnsValue()
    {
        var target = new EntityId(1);
        var source = new EntityId(10);

        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 10f, source, "Alliance with X"));

        Assert.Equal(10f, _effects.ComputeEffectModifier(target, "stability"));
    }

    [Fact]
    public void EffectiveValue_BasePlusModifier()
    {
        var target = new EntityId(1);
        var source = new EntityId(10);

        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 10f, source, "Alliance"));

        Assert.Equal(60f, _effects.ComputeEffectiveValue(50f, target, "stability"));
    }

    // --- PF2e Stacking: Same-Type ---

    [Fact]
    public void SameTypeBonuses_TakeBest()
    {
        var target = new EntityId(1);

        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 10f, new EntityId(10), "Alliance A"));
        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 15f, new EntityId(11), "Alliance B"));

        // Same type (political): take best = 15
        Assert.Equal(15f, _effects.ComputeEffectModifier(target, "stability"));
    }

    [Fact]
    public void SameTypePenalties_TakeWorst()
    {
        var target = new EntityId(1);

        _effects.AddEffect(new ActiveEffect(target, "stability", "political", -10f, new EntityId(10), "Enemy A"));
        _effects.AddEffect(new ActiveEffect(target, "stability", "political", -15f, new EntityId(11), "Enemy B"));

        // Same type (political): take worst penalty = -15
        Assert.Equal(-15f, _effects.ComputeEffectModifier(target, "stability"));
    }

    [Fact]
    public void SameType_BonusAndPenalty_BothApply()
    {
        var target = new EntityId(1);

        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 10f, new EntityId(10), "Alliance"));
        _effects.AddEffect(new ActiveEffect(target, "stability", "political", -5f, new EntityId(11), "Enemy"));

        // Same type: best bonus (10) + worst penalty (-5) = 5
        Assert.Equal(5f, _effects.ComputeEffectModifier(target, "stability"));
    }

    // --- PF2e Stacking: Cross-Type ---

    [Fact]
    public void CrossType_AlwaysStack()
    {
        var target = new EntityId(1);

        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 10f, new EntityId(10), "Alliance"));
        _effects.AddEffect(new ActiveEffect(target, "stability", "cultural", 5f, new EntityId(11), "Cultural bond"));
        _effects.AddEffect(new ActiveEffect(target, "stability", "artifact", 3f, new EntityId(12), "Holy relic"));

        // Cross-type: 10 + 5 + 3 = 18
        Assert.Equal(18f, _effects.ComputeEffectModifier(target, "stability"));
    }

    [Fact]
    public void MixedStacking_SameTypeTakesBest_CrossTypeStacks()
    {
        var target = new EntityId(1);

        // Two political bonuses: take best (15)
        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 10f, new EntityId(10), "Alliance A"));
        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 15f, new EntityId(11), "Alliance B"));

        // One cultural bonus: stacks with political
        _effects.AddEffect(new ActiveEffect(target, "stability", "cultural", 5f, new EntityId(12), "Culture"));

        // Total: 15 (best political) + 5 (cultural) = 20
        Assert.Equal(20f, _effects.ComputeEffectModifier(target, "stability"));
    }

    // --- Source Tracking ---

    [Fact]
    public void GetEffects_ReturnsAllWithSources()
    {
        var target = new EntityId(1);
        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 10f, new EntityId(10), "Alliance A"));
        _effects.AddEffect(new ActiveEffect(target, "stability", "cultural", 5f, new EntityId(11), "Culture bond"));
        _effects.AddEffect(new ActiveEffect(target, "hp", "environmental", 3f, new EntityId(12), "Location bonus"));

        var stabilityEffects = _effects.GetEffects(target, "stability");
        Assert.Equal(2, stabilityEffects.Count);

        var hpEffects = _effects.GetEffects(target, "hp");
        Assert.Single(hpEffects);
    }

    // --- Cleanup ---

    [Fact]
    public void RemoveEffectsFromSource_ClearsCorrectEffects()
    {
        var target = new EntityId(1);
        var sourceA = new EntityId(10);
        var sourceB = new EntityId(11);

        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 10f, sourceA, "Source A"));
        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 5f, sourceB, "Source B"));

        _effects.RemoveEffectsFromSource(sourceA);

        // Only sourceB's effect remains
        Assert.Equal(5f, _effects.ComputeEffectModifier(target, "stability"));
    }

    [Fact]
    public void RemoveEffectsOnTarget_ClearsAllEffects()
    {
        var target = new EntityId(1);
        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 10f, new EntityId(10), "A"));
        _effects.AddEffect(new ActiveEffect(target, "hp", "cultural", 5f, new EntityId(11), "B"));

        _effects.RemoveEffectsOnTarget(target);

        Assert.Equal(0f, _effects.ComputeEffectModifier(target, "stability"));
        Assert.Equal(0f, _effects.ComputeEffectModifier(target, "hp"));
    }

    // --- Edge Cases ---

    [Fact]
    public void NoEffects_ReturnsZero()
    {
        Assert.Equal(0f, _effects.ComputeEffectModifier(new EntityId(999), "anything"));
    }

    [Fact]
    public void DifferentAttributes_AreIndependent()
    {
        var target = new EntityId(1);
        _effects.AddEffect(new ActiveEffect(target, "stability", "political", 10f, new EntityId(10), "A"));
        _effects.AddEffect(new ActiveEffect(target, "hp", "political", 20f, new EntityId(11), "B"));

        Assert.Equal(10f, _effects.ComputeEffectModifier(target, "stability"));
        Assert.Equal(20f, _effects.ComputeEffectModifier(target, "hp"));
    }
}
