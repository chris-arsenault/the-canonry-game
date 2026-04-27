namespace TheCanonryGame.Game.Tests.Cards;

using TheCanonryGame.Game.Cards;

public sealed class CardExecutorTests
{
    private readonly CardExecutor _executor = new(new Random(42));

    [Fact]
    public void TryFire_FiresWhenConditionIsMet()
    {
        var card = CreateCard(condition: PassingCondition.Instance);
        var instance = new CardInstance(card, placedAtTick: 0);

        var result = _executor.TryFire(instance, currentTick: 1);

        Assert.True(result.Fired);
        Assert.Equal(CardFireOutcome.Success, result.Outcome);
        Assert.Equal(1, instance.FireCount);
        Assert.Equal(1, instance.LastFiredTick);
    }

    [Fact]
    public void TryFire_DoesNotFireWhenConditionFails()
    {
        var card = CreateCard(condition: FailingCondition.Instance);
        var instance = new CardInstance(card, placedAtTick: 0);

        var result = _executor.TryFire(instance, currentTick: 1);

        Assert.False(result.Fired);
        Assert.Equal(CardFireStatus.ConditionFailed, result.Status);
        Assert.Equal(0, instance.FireCount);
    }

    [Fact]
    public void TryFire_DoesNotFireWhenInactive()
    {
        var card = CreateCard(condition: PassingCondition.Instance);
        var instance = new CardInstance(card, placedAtTick: 0) { IsActive = false };

        var result = _executor.TryFire(instance, currentTick: 1);

        Assert.False(result.Fired);
        Assert.Equal(CardFireStatus.SkippedInactive, result.Status);
    }

    [Fact]
    public void TryFire_GeneratorCard_SkipsTableauExecution()
    {
        var card = new CardDefinition(
            "generator_card",
            "Generator Card",
            CardType.Generator,
            PassingCondition.Instance,
            manaCost: 0,
            cooldown: 0,
            cadence: 0,
            lifespan: int.MaxValue,
            generatorId: 1);
        card.SuccessEffects.Add(new CardEffect("stability_delta"));
        var instance = new CardInstance(card, placedAtTick: 0);

        var result = _executor.TryFire(instance, currentTick: 1);

        Assert.False(result.Fired);
        Assert.Equal(CardFireStatus.SkippedGenerator, result.Status);
        Assert.Equal(0, instance.FireCount);
        Assert.Empty(result.Effects);
        Assert.Null(instance.LastFiredTick);
    }

    [Fact]
    public void TryFire_RespectsCooldown()
    {
        var card = CreateCard(condition: PassingCondition.Instance, cooldown: 5);
        var instance = new CardInstance(card, placedAtTick: 0);

        Assert.True(_executor.TryFire(instance, currentTick: 1).Fired);
        Assert.False(_executor.TryFire(instance, currentTick: 3).Fired);
        Assert.True(_executor.TryFire(instance, currentTick: 6).Fired);
        Assert.Equal(2, instance.FireCount);
    }

    [Fact]
    public void TryFire_RespectsCadence()
    {
        var card = CreateCard(condition: PassingCondition.Instance, cadence: 3, cooldown: 0);
        var instance = new CardInstance(card, placedAtTick: 0);

        Assert.Equal(CardFireStatus.SkippedCadence, _executor.TryFire(instance, currentTick: 1).Status);
        Assert.True(_executor.TryFire(instance, currentTick: 3).Fired);
    }

    [Fact]
    public void TryFire_EmitsSuccessEffects()
    {
        var card = CreateCard(condition: PassingCondition.Instance);
        card.SuccessEffects.Add(new CardEffect("stability_delta"));
        card.SuccessEffects.Add(new CardEffect("health_delta"));
        var instance = new CardInstance(card, placedAtTick: 0);

        var result = _executor.TryFire(instance, currentTick: 1);

        Assert.True(result.Fired);
        Assert.Equal(2, result.Effects.Count);
    }

    [Fact]
    public void TryFire_ActionCard_CanFail()
    {
        var card = CreateCard(condition: PassingCondition.Instance, type: CardType.Action);
        card.SuccessProbability = 0f;
        card.FailureEffects.Add(new CardEffect("stability_loss"));
        var instance = new CardInstance(card, placedAtTick: 0);

        var result = _executor.TryFire(instance, currentTick: 1);

        Assert.True(result.Fired);
        Assert.Equal(CardFireOutcome.Failure, result.Outcome);
        Assert.Single(result.Effects);
        Assert.Equal("stability_loss", result.Effects[0].Kind);
    }

    [Fact]
    public void TryFire_ActionCard_CanSucceed()
    {
        var card = CreateCard(condition: PassingCondition.Instance, type: CardType.Action);
        card.SuccessProbability = 1f;
        card.SuccessEffects.Add(new CardEffect("stability_gain"));
        card.FailureEffects.Add(new CardEffect("stability_loss"));
        var instance = new CardInstance(card, placedAtTick: 0);

        var result = _executor.TryFire(instance, currentTick: 1);

        Assert.True(result.Fired);
        Assert.Equal(CardFireOutcome.Success, result.Outcome);
        Assert.Single(result.Effects);
        Assert.Equal("stability_gain", result.Effects[0].Kind);
    }

    [Fact]
    public void TryFire_TracksLastConditionResult()
    {
        var card = CreateCard(condition: PassingCondition.Instance);
        var instance = new CardInstance(card, placedAtTick: 0);

        _executor.TryFire(instance, currentTick: 1);

        Assert.True(instance.LastConditionResult);
    }

    private static CardDefinition CreateCard(
        ICardCondition condition,
        CardType type = CardType.System,
        int cooldown = 0,
        int cadence = 1) =>
        new("test_card", "Test Card", type, condition, manaCost: 2, cooldown: cooldown, cadence: cadence);

    private sealed class PassingCondition : ICardCondition
    {
        public static readonly PassingCondition Instance = new();

        private PassingCondition()
        {
        }

        public CardConditionResult Evaluate(CardEvaluationContext context) => CardConditionResult.Pass("pass");

        public string Describe() => "pass";
    }

    private sealed class FailingCondition : ICardCondition
    {
        public static readonly FailingCondition Instance = new();

        private FailingCondition()
        {
        }

        public CardConditionResult Evaluate(CardEvaluationContext context) => CardConditionResult.Fail("fail");

        public string Describe() => "fail";
    }
}
