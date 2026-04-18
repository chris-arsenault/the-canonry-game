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
                Kind = EntityKind.Settlement,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = EntityKind.Npc,
                    Subtype = "hero",
                    Role = "hero",
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = EntityKind.Settlement,
                },
            ],
        };

        Assert.Equal(1, config.PulseWeight);
        Assert.Null(config.MaxRunsPerSim);
        Assert.Empty(config.Relationships);
        Assert.Empty(config.Target.SaturationLimits);
        Assert.Equal(1f, config.Produces[0].CreateChance);
        Assert.Equal(SemanticPlacement.Near, config.Produces[0].Placement);
        Assert.Equal(TriggerDirection.Any, config.Triggers[0].Direction);
    }
}
