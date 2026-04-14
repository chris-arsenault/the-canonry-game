namespace Engine.Tests.Tableau;

using Engine.Cards;
using Engine.Processes;
using Engine.Graph;
using Engine.Kinds;
using ResourceType = Engine.Resources.ResourceType;
using Engine.Tableau;

public class TableauManagerTests
{
    private static CardDefinition CreateCard(
        string id = "test",
        int manaCost = 2,
        int lifespan = 50,
        int cooldown = 0,
        CardType type = CardType.System)
    {
        return new CardDefinition(id, $"Card {id}", type, new AlwaysTrue(),
            manaCost: manaCost, cooldown: cooldown, cadence: 1, lifespan: lifespan);
    }

    private static StubContext CreateContext(int tick = 1) => new() { CurrentTickValue = tick };

    // --- Placement ---

    [Fact]
    public void PlaceCard_ReducesMana()
    {
        var tableau = new TableauManager(10f);
        var card = CreateCard(manaCost: 3);

        var instance = tableau.PlaceCard(card, currentTick: 0);

        Assert.NotNull(instance);
        Assert.Equal(7f, tableau.CurrentMana);
    }

    [Fact]
    public void PlaceCard_RejectsInsufficientMana()
    {
        var tableau = new TableauManager(5f);
        var card = CreateCard(manaCost: 8);

        var instance = tableau.PlaceCard(card, currentTick: 0);

        Assert.Null(instance);
        Assert.Equal(5f, tableau.CurrentMana);
    }

    [Fact]
    public void PlaceCard_AppearsInActiveCards()
    {
        var tableau = new TableauManager(10f);
        tableau.PlaceCard(CreateCard("a"), currentTick: 0);
        tableau.PlaceCard(CreateCard("b"), currentTick: 0);

        Assert.Equal(2, tableau.ActiveCards.Count);
    }

    // --- Removal ---

    [Fact]
    public void RemoveCard_ReturnsMana()
    {
        var tableau = new TableauManager(10f);
        var instance = tableau.PlaceCard(CreateCard(manaCost: 3), currentTick: 0)!;

        tableau.RemoveCard(instance);

        Assert.Equal(10f, tableau.CurrentMana);
        Assert.Empty(tableau.ActiveCards);
    }

    [Fact]
    public void RemoveCard_ManaDoesNotExceedCap()
    {
        var tableau = new TableauManager(10f);
        var instance = tableau.PlaceCard(CreateCard(manaCost: 3), currentTick: 0)!;

        // Expand cap, then remove
        tableau.ExpandManaCap(5f);
        tableau.RemoveCard(instance);

        Assert.True(tableau.CurrentMana <= tableau.ManaCap);
    }

    // --- Lifespan ---

    [Fact]
    public void Tick_ExpiresCardAtEndOfLifespan()
    {
        var tableau = new TableauManager(10f);
        tableau.PlaceCard(CreateCard(lifespan: 3, manaCost: 2), currentTick: 0);

        CardInstance? expired = null;
        tableau.CardExpired += (_, e) => expired = e.Instance;

        var ctx = CreateContext(1);
        tableau.Tick(1, ctx);
        tableau.Tick(2, ctx);
        tableau.Tick(3, ctx); // Lifespan = 3, should expire after 3 ticks

        Assert.NotNull(expired);
        Assert.Empty(tableau.ActiveCards);
        Assert.Equal(10f, tableau.CurrentMana); // Mana returned
    }

    // --- Card firing ---

    [Fact]
    public void Tick_FiresActiveCards()
    {
        var card = CreateCard();
        card.Mutations.Add(new ModifyAttributeMutation(new EntityId(1), "stability", 5f));

        var tableau = new TableauManager(10f);
        tableau.PlaceCard(card, currentTick: 0);

        var fired = false;
        tableau.CardFired += (_, _) => fired = true;

        tableau.Tick(1, CreateContext(1));

        Assert.True(fired);
    }

    [Fact]
    public void Tick_DoesNotFireExpiredCards()
    {
        var tableau = new TableauManager(10f);
        var card = CreateCard(lifespan: 1);
        tableau.PlaceCard(card, currentTick: 0);

        var fireCount = 0;
        tableau.CardFired += (_, _) => fireCount++;

        // Tick 1: lifespan expires before firing
        tableau.Tick(1, CreateContext(1));

        // Card expired, should not fire
        Assert.Empty(tableau.ActiveCards);
    }

    // --- Mana management ---

    [Fact]
    public void ManaUsed_TracksActiveCards()
    {
        var tableau = new TableauManager(20f);
        tableau.PlaceCard(CreateCard("a", manaCost: 3), currentTick: 0);
        tableau.PlaceCard(CreateCard("b", manaCost: 5), currentTick: 0);

        Assert.Equal(8f, tableau.ManaUsed);
    }

    [Fact]
    public void ExpandManaCap_IncreasesCapAndAvailable()
    {
        var tableau = new TableauManager(10f);
        tableau.PlaceCard(CreateCard(manaCost: 3), currentTick: 0);

        Assert.Equal(7f, tableau.CurrentMana);

        tableau.ExpandManaCap(5f);

        Assert.Equal(15f, tableau.ManaCap);
        Assert.Equal(12f, tableau.CurrentMana);
    }

    // --- Multiple cards ---

    [Fact]
    public void Tick_ProcessesMultipleCards()
    {
        var tableau = new TableauManager(20f);

        var cardA = CreateCard("a", manaCost: 2);
        var cardB = CreateCard("b", manaCost: 3);

        tableau.PlaceCard(cardA, 0);
        tableau.PlaceCard(cardB, 0);

        var firedCount = 0;
        tableau.CardFired += (_, _) => firedCount++;

        tableau.Tick(1, CreateContext(1));

        Assert.Equal(2, firedCount);
    }

    // --- Stub context ---

    private sealed class StubContext : IMutationContext
    {
        public int CurrentTick => CurrentTickValue;
        public EntityId SelfEntityId => new EntityId(0);
        public Random Random { get; } = new Random(42);
        public EntityId? ResolvedTargetId => null;
        public int CurrentTickValue { get; set; }

        public float GetPressure(string pressureName, EntityId scopeId) => 0f;
        public float GetAttribute(EntityId entityId, string attributeName) => 0f;
        public int CountEntitiesByKind(EntityKind kind) => 0;
        public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) => null;
        public int? GetLastFiredTick() => null;
        public Entity? GetEntity(EntityId id) => null;
        public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) =>
            new(new EntityId(99), kind, subtype, name, culture, CurrentTick, coordinates);
        public void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) { }
        public void ModifyAttribute(EntityId entityId, string attributeName, float delta) { }
        public void ModifyPressure(string pressureName, EntityId scopeId, float delta) { }
        public void SetTag(EntityId entityId, string key, string value) { }
        public void SetEntityHistorical(EntityId entityId) { }
        public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind) => null;
        public IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind) => [];
        public float GetResource((int Q, int R) coordinates, ResourceType resourceType) => 0f;
        public float ConsumeResource((int Q, int R) coordinates, ResourceType resourceType, float amount) => 0f;
        public void SetCoordinates(EntityId entityId, int q, int r) { }
        public void CullRelationship(EntityId sourceId, string relationshipKind) { }
    }
}
