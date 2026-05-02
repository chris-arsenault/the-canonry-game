namespace TheCanonryGame.Game.Tests;

using TheCanonryGame.Game.Cards;
using TheCanonryGame.Game.Tableau;

public sealed class TableauManagerTests
{
    [Fact]
    public void PlaceCard_AppendsInstancesInPlacementOrderAndPreservesPlacementTick()
    {
        var manager = new TableauManager();
        var firstDefinition = new CardDefinition("card:first", "First", CardType.System);
        var secondDefinition = new CardDefinition("card:second", "Second", CardType.Action, gambitId: 3);

        var first = manager.PlaceCard(firstDefinition, currentTick: 4);
        var second = manager.PlaceCard(secondDefinition, currentTick: 9);

        Assert.Collection(
            manager.ActiveCards,
            card =>
            {
                Assert.Same(first, card);
                Assert.Same(firstDefinition, card.Definition);
                Assert.Equal(4, card.PlacedAtTick);
            },
            card =>
            {
                Assert.Same(second, card);
                Assert.Same(secondDefinition, card.Definition);
                Assert.Equal(9, card.PlacedAtTick);
            });
    }

    [Fact]
    public void PlaceCard_RejectsMissingDefinitionWithoutChangingActiveCards()
    {
        var manager = new TableauManager();

        Assert.Throws<ArgumentNullException>(() => manager.PlaceCard(null!, currentTick: 4));
        Assert.Empty(manager.ActiveCards);
    }
}
