namespace Engine.Tests.Cards;

using Engine.Cards;
using Engine.Processes;
using Engine.Graph;
using Engine.Kinds;
using ResourceType = Engine.Resources.ResourceType;

public class CardExecutorTests
{
    private readonly CardExecutor _executor = new(new Random(42));

    // --- Basic firing ---

    [Fact]
    public void TryFire_FiresWhenPreconditionsMet()
    {
        var card = CreateSimpleCard(precondition: new AlwaysTrue());
        var instance = new CardInstance(card, placedAtTick: 0);
        var ctx = new TrackingContext { CurrentTickValue = 1 };

        var result = _executor.TryFire(instance, ctx);

        Assert.True(result);
        Assert.Equal(1, instance.FireCount);
        Assert.Equal(1, instance.LastFiredTick);
    }

    [Fact]
    public void TryFire_DoesNotFireWhenPreconditionFails()
    {
        var card = CreateSimpleCard(precondition: new NeverTrue());
        var instance = new CardInstance(card, placedAtTick: 0);
        var ctx = new TrackingContext { CurrentTickValue = 1 };

        var result = _executor.TryFire(instance, ctx);

        Assert.False(result);
        Assert.Equal(0, instance.FireCount);
    }

    [Fact]
    public void TryFire_DoesNotFireWhenInactive()
    {
        var card = CreateSimpleCard(precondition: new AlwaysTrue());
        var instance = new CardInstance(card, placedAtTick: 0) { IsActive = false };
        var ctx = new TrackingContext { CurrentTickValue = 1 };

        Assert.False(_executor.TryFire(instance, ctx));
    }

    // --- Cooldown ---

    [Fact]
    public void TryFire_RespectsCooldown()
    {
        var card = CreateSimpleCard(precondition: new AlwaysTrue(), cooldown: 5);
        var instance = new CardInstance(card, placedAtTick: 0);

        // First fire at tick 1
        _executor.TryFire(instance, new TrackingContext { CurrentTickValue = 1 });
        Assert.Equal(1, instance.FireCount);

        // Attempt at tick 3 (within cooldown) - should not fire
        Assert.False(_executor.TryFire(instance, new TrackingContext { CurrentTickValue = 3 }));
        Assert.Equal(1, instance.FireCount);

        // Attempt at tick 6 (past cooldown) - should fire
        Assert.True(_executor.TryFire(instance, new TrackingContext { CurrentTickValue = 6 }));
        Assert.Equal(2, instance.FireCount);
    }

    // --- Cadence ---

    [Fact]
    public void TryFire_RespectsCadence()
    {
        var card = CreateSimpleCard(precondition: new AlwaysTrue(), cadence: 3, cooldown: 0);
        var instance = new CardInstance(card, placedAtTick: 0);

        // Tick 1: not on cadence (1 % 3 != 0)
        Assert.False(_executor.TryFire(instance, new TrackingContext { CurrentTickValue = 1 }));

        // Tick 3: on cadence
        Assert.True(_executor.TryFire(instance, new TrackingContext { CurrentTickValue = 3 }));
    }

    // --- Mutations execution ---

    [Fact]
    public void TryFire_ExecutesMutations()
    {
        var card = CreateSimpleCard(precondition: new AlwaysTrue());
        card.Mutations.Add(new ModifyAttributeMutation(new EntityId(1), "stability", 10f));
        card.Mutations.Add(new ModifyAttributeMutation(new EntityId(1), "hp", -5f));

        var instance = new CardInstance(card, placedAtTick: 0);
        var ctx = new TrackingContext { CurrentTickValue = 1 };

        _executor.TryFire(instance, ctx);

        Assert.Equal(2, ctx.AttributeModifications.Count);
    }

    // --- Action card success/failure ---

    [Fact]
    public void TryFire_ActionCard_CanFail()
    {
        var card = CreateSimpleCard(precondition: new AlwaysTrue(), type: CardType.Action);
        card.SuccessProbability = 0f; // Always fail
        card.FailureMutations.Add(new ModifyAttributeMutation(new EntityId(1), "stability", -5f));

        var instance = new CardInstance(card, placedAtTick: 0);
        var ctx = new TrackingContext { CurrentTickValue = 1 };

        var result = _executor.TryFire(instance, ctx);

        Assert.True(result); // It "fired" (with failure)
        Assert.Single(ctx.AttributeModifications);
        Assert.Equal(-5f, ctx.AttributeModifications[0].Delta);
    }

    [Fact]
    public void TryFire_ActionCard_CanSucceed()
    {
        var card = CreateSimpleCard(precondition: new AlwaysTrue(), type: CardType.Action);
        card.SuccessProbability = 1f; // Always succeed
        card.Mutations.Add(new ModifyAttributeMutation(new EntityId(1), "stability", 10f));
        card.FailureMutations.Add(new ModifyAttributeMutation(new EntityId(1), "stability", -5f));

        var instance = new CardInstance(card, placedAtTick: 0);
        var ctx = new TrackingContext { CurrentTickValue = 1 };

        _executor.TryFire(instance, ctx);

        Assert.Single(ctx.AttributeModifications);
        Assert.Equal(10f, ctx.AttributeModifications[0].Delta); // Success mutation, not failure
    }

    // --- Precondition status tracking ---

    [Fact]
    public void TryFire_TracksLastPreconditionResult()
    {
        var card = CreateSimpleCard(precondition: new AlwaysTrue());
        var instance = new CardInstance(card, placedAtTick: 0);

        _executor.TryFire(instance, new TrackingContext { CurrentTickValue = 1 });
        Assert.True(instance.LastPreconditionResult);
    }

    // --- Helpers ---

    private static CardDefinition CreateSimpleCard(
        IPrecondition precondition,
        CardType type = CardType.System,
        int cooldown = 0,
        int cadence = 1)
    {
        return new CardDefinition(
            "test_card", "Test Card", type, precondition,
            manaCost: 2, cooldown: cooldown, cadence: cadence, lifespan: 100);
    }

    private sealed class NeverTrue : IPrecondition
    {
        public PreconditionResult Evaluate(IMutationContext context) => PreconditionResult.Fail("never");
        public string Describe() => "never";
    }

    private sealed class TrackingContext : IMutationContext
    {
        public int CurrentTick => CurrentTickValue;
        public EntityId SelfEntityId => new EntityId(0);
        public Random Random { get; } = new Random(42);
        public EntityId? ResolvedTargetId => null;
        public int CurrentTickValue { get; set; }
        public List<(string Attr, float Delta)> AttributeModifications { get; } = [];

        public float GetPressure(string pressureName, EntityId scopeId) => 0f;
        public float GetAttribute(EntityId entityId, string attributeName) => 0f;
        public int CountEntitiesByKind(EntityKind kind) => 0;
        public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) => null;
        public int? GetLastFiredTick() => null;
        public Entity? GetEntity(EntityId id) => null;
        public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) =>
            new(new EntityId(99), kind, subtype, name, culture, CurrentTick, coordinates);
        public void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) { }
        public void ModifyAttribute(EntityId entityId, string attributeName, float delta) =>
            AttributeModifications.Add((attributeName, delta));
        public void ModifyPressure(string pressureName, EntityId scopeId, float delta) { }
        public void ModifyResource((int Q, int R) coordinates, ResourceType resourceType, float delta) { }
        public void SetTag(EntityId entityId, string key, string value) { }
        public void SetEntityHistorical(EntityId entityId) { }
        public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind) => null;
        public IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind) => [];
        public float GetResource((int Q, int R) coordinates, ResourceType resourceType) => 0f;
        public float ConsumeResource((int Q, int R) coordinates, ResourceType resourceType, float amount) => 0f;
        public void SetCoordinates(EntityId entityId, int q, int r) { }
        public void CullRelationship(EntityId sourceId, string relationshipKind) { }
        public void SetVariable(string name, float value) { }
        public float GetVariable(string name) => 0f;
    }
}
