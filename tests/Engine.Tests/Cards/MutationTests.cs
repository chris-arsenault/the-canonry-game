namespace Engine.Tests.Cards;

using Engine.Cards;
using Engine.Graph;
using Engine.Kinds;

public class MutationTests
{
    [Fact]
    public void CreateEntityMutation_CreatesAndStoresId()
    {
        var ctx = new TrackingCardContext();
        var mutation = new CreateEntityMutation(
            EntityKind.Settlement, "village", "NewTown", "culture_a", (0, 0));

        mutation.Execute(ctx);

        Assert.NotNull(mutation.CreatedId);
        Assert.Single(ctx.CreatedEntities);
        Assert.Equal(EntityKind.Settlement, ctx.CreatedEntities[0].Kind);
    }

    [Fact]
    public void CreateRelationshipMutation_Creates()
    {
        var ctx = new TrackingCardContext();
        var mutation = new CreateRelationshipMutation(
            new EntityId(1), new EntityId(2), "controls", RelationshipPolarity.Neutral, 1f);

        mutation.Execute(ctx);

        Assert.Single(ctx.CreatedRelationships);
        Assert.Equal("controls", ctx.CreatedRelationships[0].Kind);
    }

    [Fact]
    public void ModifyAttributeMutation_Modifies()
    {
        var ctx = new TrackingCardContext();
        var mutation = new ModifyAttributeMutation(new EntityId(1), "stability", 10f);

        mutation.Execute(ctx);

        Assert.Single(ctx.AttributeModifications);
        Assert.Equal(("stability", 10f), ctx.AttributeModifications[0]);
    }

    [Fact]
    public void ModifyPressureMutation_Modifies()
    {
        var ctx = new TrackingCardContext();
        var mutation = new ModifyPressureMutation("military_tension", new EntityId(100), 20f);

        mutation.Execute(ctx);

        Assert.Single(ctx.PressureModifications);
        Assert.Equal(("military_tension", 20f), ctx.PressureModifications[0]);
    }

    [Fact]
    public void SetTagMutation_SetsTag()
    {
        var ctx = new TrackingCardContext();
        var mutation = new SetTagMutation(new EntityId(1), "goal", "acquire");

        mutation.Execute(ctx);

        Assert.Single(ctx.TagsSet);
        Assert.Equal(("goal", "acquire"), ctx.TagsSet[0]);
    }

    [Fact]
    public void MutationsExecuteInOrder()
    {
        var ctx = new TrackingCardContext();
        var order = new List<string>();

        var createEntity = new CreateEntityMutation(EntityKind.Settlement, "v", "T", "c", null);
        var modifyAttr = new ModifyAttributeMutation(new EntityId(1), "hp", -5f);

        // Execute in order
        createEntity.Execute(ctx);
        order.Add("create");
        modifyAttr.Execute(ctx);
        order.Add("modify");

        Assert.Equal(["create", "modify"], order);
        Assert.Single(ctx.CreatedEntities);
        Assert.Single(ctx.AttributeModifications);
    }

    [Fact]
    public void Describe_ProducesReadableOutput()
    {
        var mutation = new CreateEntityMutation(EntityKind.Nation, "monarchy", "Kesh", "c_a", null);
        Assert.Contains("create_entity", mutation.Describe(), StringComparison.Ordinal);
        Assert.Contains("Nation", mutation.Describe(), StringComparison.Ordinal);
    }

    // --- Tracking context ---

    private sealed class TrackingCardContext : ICardContext
    {
        public int CurrentTick => 10;
        public List<(EntityKind Kind, string Name)> CreatedEntities { get; } = [];
        public List<(string Kind, float Strength)> CreatedRelationships { get; } = [];
        public List<(string Attr, float Delta)> AttributeModifications { get; } = [];
        public List<(string Name, float Delta)> PressureModifications { get; } = [];
        public List<(string Key, string Value)> TagsSet { get; } = [];

        public float GetPressure(string pressureName, EntityId scopeId) => 0f;
        public float GetAttribute(EntityId entityId, string attributeName) => 0f;
        public int CountEntitiesByKind(EntityKind kind) => 0;
        public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) => null;
        public int? GetLastFiredTick() => null;
        public Entity? GetEntity(EntityId id) => null;

        public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates)
        {
            CreatedEntities.Add((kind, name));
            return new Entity(new EntityId(99), kind, subtype, name, culture, CurrentTick, coordinates);
        }

        public void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) =>
            CreatedRelationships.Add((kind, strength));

        public void ModifyAttribute(EntityId entityId, string attributeName, float delta) =>
            AttributeModifications.Add((attributeName, delta));

        public void ModifyPressure(string pressureName, EntityId scopeId, float delta) =>
            PressureModifications.Add((pressureName, delta));

        public void SetTag(EntityId entityId, string key, string value) =>
            TagsSet.Add((key, value));

        public void SetEntityHistorical(EntityId entityId) { }
    }
}
