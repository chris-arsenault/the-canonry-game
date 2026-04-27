namespace TheCanonryGame.Game.Tests.Tableau;

using TheCanonryGame.Game.Cards;
using TheCanonryGame.Game.Tableau;

public sealed class TableauManagerTests
{
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

        tableau.ExpandManaCap(5f);
        tableau.RemoveCard(instance);

        Assert.True(tableau.CurrentMana <= tableau.ManaCap);
    }

    [Fact]
    public void Tick_ExpiresCardAtEndOfLifespan()
    {
        var tableau = new TableauManager(10f);
        tableau.PlaceCard(CreateCard(lifespan: 3, manaCost: 2), currentTick: 0);

        CardInstance? expired = null;
        tableau.CardExpired += (_, e) => expired = e.Instance;

        tableau.Tick(1);
        tableau.Tick(2);
        var result = tableau.Tick(3);

        Assert.NotNull(expired);
        Assert.Single(result.ExpiredCards);
        Assert.Empty(tableau.ActiveCards);
        Assert.Equal(10f, tableau.CurrentMana);
    }

    [Fact]
    public void Tick_FiresActiveCards()
    {
        var card = CreateCard();
        card.SuccessEffects.Add(new CardEffect("effect"));
        var tableau = new TableauManager(10f);
        tableau.PlaceCard(card, currentTick: 0);

        var fired = false;
        tableau.CardFired += (_, _) => fired = true;

        var result = tableau.Tick(1);

        Assert.True(fired);
        Assert.Single(result.FiredCards);
    }

    [Fact]
    public void Tick_DoesNotFireExpiredCards()
    {
        var tableau = new TableauManager(10f);
        tableau.PlaceCard(CreateCard(lifespan: 1), currentTick: 0);

        var fireCount = 0;
        tableau.CardFired += (_, _) => fireCount++;

        tableau.Tick(1);

        Assert.Equal(0, fireCount);
        Assert.Empty(tableau.ActiveCards);
    }

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

        tableau.ExpandManaCap(5f);

        Assert.Equal(15f, tableau.ManaCap);
        Assert.Equal(12f, tableau.CurrentMana);
    }

    [Fact]
    public void Tick_ProcessesMultipleCards()
    {
        var tableau = new TableauManager(20f);
        tableau.PlaceCard(CreateCard("a", manaCost: 2), currentTick: 0);
        tableau.PlaceCard(CreateCard("b", manaCost: 3), currentTick: 0);

        var firedCount = 0;
        tableau.CardFired += (_, _) => firedCount++;

        var result = tableau.Tick(1);

        Assert.Equal(2, firedCount);
        Assert.Equal(2, result.FiredCards.Count);
    }

    private static CardDefinition CreateCard(
        string id = "test",
        int manaCost = 2,
        int lifespan = 50,
        int cooldown = 0,
        CardType type = CardType.System) =>
        new(
            id,
            $"Card {id}",
            type,
            AlwaysCardCondition.Instance,
            manaCost: manaCost,
            cooldown: cooldown,
            cadence: 1,
            lifespan: lifespan);
}
