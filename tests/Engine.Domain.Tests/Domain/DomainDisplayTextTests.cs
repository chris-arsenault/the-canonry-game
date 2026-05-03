namespace Engine.Domain.Tests.Domain;

using Engine.Domain.Config;
using Engine.Kinds;

public sealed class DomainDisplayTextTests
{
    [Fact]
    public void GetCultureDisplayName_ResolvesKnownCultureAndFallsBackToId()
    {
        var config = CreateConfig();

        Assert.Equal("Moon Choir", config.GetCultureDisplayName("moon"));
        Assert.Equal("unknown", config.GetCultureDisplayName("unknown"));
    }

    [Fact]
    public void GetAttributeDisplayName_PrefersCultureSpecificTextThenDisplayNameThenRawName()
    {
        var config = CreateConfig();

        Assert.Equal("Moon Warmth", config.GetAttributeDisplayName(TestKinds.Penguin, "moon", "warmth"));
        Assert.Equal("Warmth", config.GetAttributeDisplayName(TestKinds.Penguin, "sun", "warmth"));
        Assert.Equal("echo", config.GetAttributeDisplayName(TestKinds.Penguin, cultureId: null, "echo"));
    }

    [Fact]
    public void GetAttributeDisplayName_FallsBackWhenKindOrAttributeIsUnknown()
    {
        var config = CreateConfig();

        Assert.Equal("warmth", config.GetAttributeDisplayName(TestKinds.Monster, "moon", "warmth"));
        Assert.Equal("missing", config.GetAttributeDisplayName(TestKinds.Penguin, "moon", "missing"));
    }

    [Fact]
    public void GetTraitDisplayName_ResolvesConfiguredTextAndFallsBackToRawName()
    {
        var config = CreateConfig();

        Assert.Equal("Keen Witness", config.GetTraitDisplayName(TestKinds.Penguin, "witness"));
        Assert.Equal("missing", config.GetTraitDisplayName(TestKinds.Penguin, "missing"));
        Assert.Equal("witness", config.GetTraitDisplayName(TestKinds.Monster, "witness"));
    }

    private static DomainConfig CreateConfig() =>
        new()
        {
            Kinds = new Dictionary<EntityKind, KindConfig>
            {
                [TestKinds.Penguin] = new()
                {
                    Category = EntityCategory.Agency,
                    Subtypes = new HashSet<string>(StringComparer.Ordinal) { "warden" },
                    Health = new HealthConfig
                    {
                        Name = "warmth",
                        DisplayName = "Warmth",
                        CultureDisplayNames = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["moon"] = "Moon Warmth",
                        },
                        Min = 0f,
                        Max = 100f,
                    },
                    Traits =
                    [
                        new TraitConfig
                        {
                            Name = "witness",
                            DisplayName = "Keen Witness",
                        },
                    ],
                    PrimaryPlane = PrimaryPlane.Physical,
                },
            },
            Relationships = [],
            Lifecycles = new Dictionary<EntityKind, LifecycleConfig>(),
            Terrains = [],
            Cultures = [new CultureConfig { Id = "moon", DisplayName = "Moon Choir" }],
        };
}
