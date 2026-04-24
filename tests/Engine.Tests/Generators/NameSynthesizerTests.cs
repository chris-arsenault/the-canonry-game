namespace Engine.Tests.Generators;

using Engine.Generators;
using Engine.Kinds;

public class NameSynthesizerTests
{
    [Fact]
    public void SynthesizeName_WithSeededRandom_ReturnsKnownOutput()
    {
        var cases = new (EntityKind Kind, string Subtype, string Culture, int Seed, string Expected)[]
        {
            (TestKinds.Penguin, "warden", "culture_a", 42, "Captain Dalfel"),
            (TestKinds.Penguin, "scholar", "culture_a", 42, "Elder Dalfel"),
            (TestKinds.Settlement, "village", "culture_a", 42, "Frostfrostwatch"),
            (TestKinds.Faction, "order", "culture_a", 42, "The Forgotten League"),
            (TestKinds.Artifact, "weapon", "culture_a", 42, "Nightnightsong"),
            (TestKinds.Region, "scar", "culture_a", 42, "Black Scar"),
        };

        foreach (var testCase in cases)
        {
            var actual = NameSynthesizer.SynthesizeName(
                testCase.Kind,
                testCase.Subtype,
                testCase.Culture,
                new Random(testCase.Seed));

            Assert.Equal(testCase.Expected, actual);
        }
    }

    [Fact]
    public void SynthesizeName_ReturnsNonEmptyNameWithinReasonableLength()
    {
        var cases = new (EntityKind Kind, string Subtype)[]
        {
            (TestKinds.Penguin, "merchant"),
            (TestKinds.Settlement, "village"),
            (TestKinds.Faction, "order"),
            (TestKinds.Artifact, "instrument"),
            (TestKinds.Region, "region"),
        };

        foreach (var testCase in cases)
        {
            var name = NameSynthesizer.SynthesizeName(testCase.Kind, testCase.Subtype, "culture_a", new Random(7));

            Assert.False(string.IsNullOrWhiteSpace(name));
            Assert.InRange(name.Length, 1, 40);
            if (testCase.Kind == TestKinds.Region)
            {
                Assert.NotEqual("Unnamed Region", name);
                Assert.NotEqual("Unnamed", name);
            }
        }
    }
}
