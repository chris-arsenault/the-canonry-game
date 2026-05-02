namespace TheCanonryGame.Game.Tests;

using TheCanonryGame.Game.Cards;

public sealed class CardTests
{
    [Fact]
    public void CardDefinition_UsesStableIdForRuntimeIdEvenIfDisplayNameChanges()
    {
        var card = new CardDefinition(
            "generator:ice_settles_accounts",
            "Ice Settles Accounts",
            CardType.Generator,
            generatorId: 10);
        var originalRuntimeId = card.RuntimeId;

        card.Name = "Renamed Generator";

        Assert.Equal(887608761, originalRuntimeId.Value);
        Assert.Equal(originalRuntimeId, card.RuntimeId);
        Assert.Equal("Renamed Generator", card.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CardDefinition_RejectsMissingStableId(string? id)
    {
        Assert.ThrowsAny<ArgumentException>(() => new CardDefinition(id!, "Name", CardType.System));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CardDefinition_RejectsMissingName(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => new CardDefinition("card:id", name!, CardType.System));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void CardDefinition_RejectsNegativeRuntimeBindings(int generatorId, int gambitId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CardDefinition(
            "card:id",
            "Name",
            CardType.System,
            generatorId,
            gambitId));
    }

    [Fact]
    public void CardDefinition_TagsAreOrdinalCaseSensitive()
    {
        var card = new CardDefinition("card:id", "Name", CardType.System);

        card.Tags.Add("generator");
        card.Tags.Add("Generator");

        Assert.Equal(2, card.Tags.Count);
        Assert.Contains("generator", card.Tags);
        Assert.Contains("Generator", card.Tags);
    }

    [Fact]
    public void CardRuntimeId_FromStableNameIsDeterministicAndCaseSensitive()
    {
        var lower = CardRuntimeId.FromStableName("alpha");
        var lowerAgain = CardRuntimeId.FromStableName("alpha");
        var upper = CardRuntimeId.FromStableName("Alpha");

        Assert.Equal(1569418667, lower.Value);
        Assert.Equal(55079499, upper.Value);
        Assert.Equal(lower, lowerAgain);
        Assert.NotEqual(lower, upper);
        Assert.False(lower.IsNone);
    }

    [Fact]
    public void CardRuntimeId_NoneIsReservedForMissingCard()
    {
        Assert.True(CardRuntimeId.None.IsNone);
        Assert.Equal(0, CardRuntimeId.None.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CardRuntimeId_RejectsMissingStableName(string? stableName)
    {
        Assert.ThrowsAny<ArgumentException>(() => CardRuntimeId.FromStableName(stableName!));
    }

    [Fact]
    public void CardInstance_RejectsMissingDefinition()
    {
        Assert.Throws<ArgumentNullException>(() => new CardInstance(null!, placedAtTick: 1));
    }
}
