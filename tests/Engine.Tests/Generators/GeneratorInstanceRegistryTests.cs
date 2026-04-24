namespace Engine.Tests.Generators;

using Engine.Domain.Graph;
using Engine.Generators;
using Engine.Graph;
using Engine.Kinds;

public class GeneratorInstanceRegistryTests
{
    [Fact]
    public void GetOrCreate_SameDefinitionId_ReturnsSharedInstance()
    {
        var definition = CreateDefinition("hero_emergence");
        var registry = new GeneratorInstanceRegistry();

        var first = registry.GetOrCreate(definition);
        var second = registry.GetOrCreate(definition);

        Assert.Same(first, second);
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void RecordRun_KeepsOnlyFiveRecentTargets()
    {
        var instance = new GeneratorInstance(CreateDefinition("hero_emergence"));

        for (var i = 1; i <= 6; i++)
        {
            instance.RecordRun(i, new EntityId(i));
        }

        Assert.Equal(6, instance.TotalRuns);
        Assert.Equal(6, instance.LastFiredTick);
        Assert.Equal([new EntityId(2), new EntityId(3), new EntityId(4), new EntityId(5), new EntityId(6)], instance.RecentTargets.ToArray());
    }

    [Fact]
    public void Reset_ClearsMutableGeneratorState()
    {
        var definition = CreateDefinition("hero_emergence");
        var registry = new GeneratorInstanceRegistry();
        var instance = registry.GetOrCreate(definition);

        instance.RecordRun(12, new EntityId(100));
        instance.RecordRun(20, new EntityId(101));

        registry.Reset();

        Assert.Equal(1, registry.Count);
        Assert.Equal(0, instance.TotalRuns);
        Assert.Null(instance.LastFiredTick);
        Assert.Empty(instance.RecentTargets);
        Assert.Same(instance, registry.GetOrCreate(definition));
    }

    private static DefinitionGeneratorNode CreateDefinition(string id) =>
        new()
        {
            Id = id,
            Name = "Hero Emergence",
            Target = new DefinitionGeneratorTarget
            {
                Kind = TestKinds.Settlement,
            },
            SecondaryTargets = [],
            Produces =
            [
                new DefinitionProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new DefinitionGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    RelationshipId = new DefinitionRelationshipId("home_base"),
                    FactIds = new HashSet<DefinitionRelationshipFactId>(),
                },
            ],
            Triggers =
            [
                new DefinitionGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                    AttributeId = new DefinitionAttributeId("shelter"),
                },
            ],
            SourceConfig = new Engine.Domain.Config.SemanticGeneratorConfig
            {
                Id = id,
                Name = "Hero Emergence",
                Target = new Engine.Domain.Config.SemanticGeneratorTarget
                {
                    Kind = TestKinds.Settlement,
                },
                Produces =
                [
                    new Engine.Domain.Config.SemanticProducedEntity
                    {
                        Ref = "$hero",
                        Kind = TestKinds.Penguin,
                        Subtype = "warden",
                        Role = "hero",
                        InheritCultureFrom = "$target",
                    },
                ],
                Relationships =
                [
                    new Engine.Domain.Config.SemanticGeneratorRelationship
                    {
                        SourceRef = "$hero",
                        DestinationRef = "$target",
                        Kind = "home_base",
                    },
                ],
                Triggers =
                [
                    new Engine.Domain.Config.SemanticGeneratorTrigger
                    {
                        OnKind = TestKinds.Settlement,
                        AttributeName = "shelter",
                    },
                ],
            },
        };
}
