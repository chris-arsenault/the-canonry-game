namespace Engine.Tests.Generators;

using Engine.Generators;
using Engine.Kinds;

public class NameSynthesizerTests
{
    [Theory]
    [InlineData(EntityKind.Npc, "hero", "culture_a", 42, "Captain Dalfel")]
    [InlineData(EntityKind.Npc, "scholar", "culture_a", 42, "Elder Dalfel")]
    [InlineData(EntityKind.Settlement, "village", "culture_a", 42, "Frostfrostwatch")]
    [InlineData(EntityKind.Faction, "generic_faction", "culture_a", 42, "The Forgotten League")]
    [InlineData(EntityKind.Artifact, "weapon", "culture_a", 42, "Nightnightsong")]
    [InlineData(EntityKind.Region, "region", "culture_a", 42, "Unnamed Region")]
    public void SynthesizeName_WithSeededRandom_ReturnsKnownOutput(
        EntityKind kind,
        string subtype,
        string culture,
        int seed,
        string expected)
    {
        var actual = NameSynthesizer.SynthesizeName(kind, subtype, culture, new Random(seed));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(EntityKind.Npc, "merchant")]
    [InlineData(EntityKind.Settlement, "village")]
    [InlineData(EntityKind.Faction, "generic_faction")]
    [InlineData(EntityKind.Artifact, "instrument")]
    [InlineData(EntityKind.Region, "region")]
    public void SynthesizeName_ReturnsNonEmptyNameWithinReasonableLength(
        EntityKind kind,
        string subtype)
    {
        var name = NameSynthesizer.SynthesizeName(kind, subtype, "culture_a", new Random(7));

        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.InRange(name.Length, 1, 40);
    }
}
