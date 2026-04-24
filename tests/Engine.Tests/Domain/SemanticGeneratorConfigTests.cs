namespace Engine.Tests.Domain;

using Engine.Domain.Config;
using Engine.Kinds;

public class SemanticGeneratorConfigTests
{
    [Fact]
    public void SemanticDomainConfig_GeneratorsDefaultsEmpty()
    {
        var semantics = new SemanticDomainConfig();

        Assert.Empty(semantics.Generators);
    }

    [Fact]
    public void SemanticGeneratorConfig_DefaultsOptionalCollectionsAndWeights()
    {
        var config = new SemanticGeneratorConfig
        {
            Id = "hero_emergence",
            Name = "Hero Emergence",
            Target = new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                },
            ],
        };

        Assert.Equal(1, config.PulseWeight);
        Assert.Null(config.MaxRunsPerSim);
        Assert.Empty(config.Relationships);
        Assert.Null(config.TargetSaturation);
        Assert.Equal(1f, config.Produces[0].CreateChance);
        Assert.True(config.Produces[0].CreateIfMissing);
        Assert.Equal(SemanticPlacement.Near, config.Produces[0].PhysicalPlacement);
        Assert.Equal(SemanticPlacement.Near, config.Produces[0].SemanticPlacement);
        Assert.Null(config.Produces[0].Subtypes);
        Assert.Null(config.Produces[0].Query);
        Assert.Equal(TriggerDirection.Any, config.Triggers[0].Direction);
    }

    [Fact]
    public void SemanticEntityQuery_DefaultsToAnyCultureMatch()
    {
        var query = new SemanticEntityQuery();

        Assert.Equal(QueryTargetCultureMatch.Any, query.TargetCultureMatch);
    }
}
