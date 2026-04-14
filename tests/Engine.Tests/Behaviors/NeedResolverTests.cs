namespace Engine.Tests.Behaviors;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Cards;
using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Wiring;

public class NeedResolverTests
{
    private static Dictionary<EntityKind, KindConfig> CreateNpcKindConfig()
    {
        return new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Npc] = new()
            {
                Subtypes = new HashSet<string> { "generic_npc" },
                Attributes =
                [
                    new() { Name = "hp", Min = 0f, Max = 100f, Default = 100f },
                    new() { Name = "hunger", Min = 0f, Max = 100f, Default = 0f },
                    new() { Name = "ambition", Min = 0f, Max = 100f, Default = 50f },
                ],
                HealthAttribute = "hp",
            },
        };
    }

    private static List<ProcessDefinition> CreateNpcProcesses()
    {
        var eat = new ProcessDefinition("npc_eat", "Eat",
            DefinitionRole.Process, EntityKind.Npc, new AlwaysTrue(), new ConstantScore(0.5f));
        eat.Reads.Add(AttributeRef.Self("hunger", EntityKind.Npc));
        eat.Reads.Add(AttributeRef.Resource("sustenance"));
        eat.Writes.Add(AttributeRef.Self("hunger", EntityKind.Npc));

        return [eat];
    }

    [Fact]
    public void Resolve_HungryNpc_ReturnsSustenanceAsProvider()
    {
        var kinds = CreateNpcKindConfig();
        var processes = CreateNpcProcesses();
        var resolver = new NeedResolver(kinds, processes);

        var graph = new WorldGraph();
        var attributes = new AttributeStore();
        var npc = graph.CreateEntity(EntityKind.Npc, "generic_npc", "Test", "c", tick: 0);
        attributes.Initialize(npc.Id, "hp", 0f, 100f, 100f);
        attributes.Initialize(npc.Id, "hunger", 0f, 100f, 80f); // Very hungry
        attributes.Initialize(npc.Id, "ambition", 0f, 100f, 50f);

        var result = resolver.Resolve(npc, attributes);

        Assert.NotNull(result);
        Assert.Equal("hunger", result.DominantAttribute);
        Assert.True(result.Urgency > 0.5f);
        Assert.Contains(result.ProviderRefs, r => r.Name == "sustenance");
    }

    [Fact]
    public void Resolve_HealthyWellFedNpc_ReturnsNull()
    {
        var kinds = CreateNpcKindConfig();
        var processes = CreateNpcProcesses();
        var resolver = new NeedResolver(kinds, processes);

        var graph = new WorldGraph();
        var attributes = new AttributeStore();
        var npc = graph.CreateEntity(EntityKind.Npc, "generic_npc", "Test", "c", tick: 0);
        attributes.Initialize(npc.Id, "hp", 0f, 100f, 100f);
        attributes.Initialize(npc.Id, "hunger", 0f, 100f, 0f); // Not hungry at all
        attributes.Initialize(npc.Id, "ambition", 0f, 100f, 50f); // At ideal

        var result = resolver.Resolve(npc, attributes);

        Assert.Null(result); // All attributes near ideal
    }

    [Fact]
    public void Resolve_DamagedNpc_HpIsDominant()
    {
        var kinds = CreateNpcKindConfig();
        // Add a process that writes to hp and reads from something
        var rest = new ProcessDefinition("npc_rest", "Rest",
            DefinitionRole.Process, EntityKind.Npc, new AlwaysTrue(), new ConstantScore(0.5f));
        rest.Reads.Add(AttributeRef.Self("hp", EntityKind.Npc));
        rest.Writes.Add(AttributeRef.Self("hp", EntityKind.Npc));

        var processes = CreateNpcProcesses();
        processes.Add(rest);
        var resolver = new NeedResolver(kinds, processes);

        var graph = new WorldGraph();
        var attributes = new AttributeStore();
        var npc = graph.CreateEntity(EntityKind.Npc, "generic_npc", "Test", "c", tick: 0);
        attributes.Initialize(npc.Id, "hp", 0f, 100f, 10f); // Very low HP
        attributes.Initialize(npc.Id, "hunger", 0f, 100f, 20f); // Slightly hungry
        attributes.Initialize(npc.Id, "ambition", 0f, 100f, 50f);

        var result = resolver.Resolve(npc, attributes);

        Assert.NotNull(result);
        Assert.Equal("hp", result.DominantAttribute);
        Assert.True(result.Urgency > 0.8f);
    }

    [Fact]
    public void Resolve_UnknownKind_ReturnsNull()
    {
        var resolver = new NeedResolver(
            new Dictionary<EntityKind, KindConfig>(),
            new List<ProcessDefinition>());

        var graph = new WorldGraph();
        var attributes = new AttributeStore();
        var npc = graph.CreateEntity(EntityKind.Npc, "generic_npc", "Test", "c", tick: 0);

        Assert.Null(resolver.Resolve(npc, attributes));
    }
}
