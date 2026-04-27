namespace TheCanonryGame.Game.Cards;

public enum CardFireStatus
{
    SkippedInactive,
    SkippedGenerator,
    SkippedCadence,
    SkippedCooldown,
    ConditionFailed,
    Fired,
}

public enum CardFireOutcome
{
    None,
    Success,
    Failure,
}

public sealed class CardFireResult
{
    private static readonly IReadOnlyList<CardEffect> NoEffects = Array.Empty<CardEffect>();

    private CardFireResult(
        CardFireStatus status,
        CardFireOutcome outcome,
        IReadOnlyList<CardEffect> effects)
    {
        Status = status;
        Outcome = outcome;
        Effects = effects;
    }

    public CardFireStatus Status { get; }

    public CardFireOutcome Outcome { get; }

    public IReadOnlyList<CardEffect> Effects { get; }

    public bool Fired => Status == CardFireStatus.Fired;

    public static CardFireResult Skipped(CardFireStatus status) =>
        new(status, CardFireOutcome.None, NoEffects);

    public static CardFireResult FiredWith(
        CardFireOutcome outcome,
        IEnumerable<CardEffect> effects)
    {
        ArgumentNullException.ThrowIfNull(effects);
        return new CardFireResult(CardFireStatus.Fired, outcome, effects.ToArray());
    }
}

public sealed class CardExecutor
{
    private readonly Random _random;

    public CardExecutor(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public CardFireResult TryFire(CardInstance instance, int currentTick)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (!instance.IsActive)
        {
            return CardFireResult.Skipped(CardFireStatus.SkippedInactive);
        }

        if (instance.Definition.Type == CardType.Generator)
        {
            return CardFireResult.Skipped(CardFireStatus.SkippedGenerator);
        }

        if (!instance.IsOnCadence(currentTick))
        {
            return CardFireResult.Skipped(CardFireStatus.SkippedCadence);
        }

        if (!instance.IsReady(currentTick))
        {
            return CardFireResult.Skipped(CardFireStatus.SkippedCooldown);
        }

        var conditionResult = instance.Definition.Condition.Evaluate(new CardEvaluationContext(currentTick));
        instance.LastConditionResult = conditionResult.Passed;
        if (!conditionResult.Passed)
        {
            return CardFireResult.Skipped(CardFireStatus.ConditionFailed);
        }

        if (instance.Definition.Type == CardType.Action &&
            instance.Definition.SuccessProbability < 1f &&
            (float)_random.NextDouble() > instance.Definition.SuccessProbability)
        {
            instance.LastFiredTick = currentTick;
            instance.FireCount++;
            return CardFireResult.FiredWith(
                CardFireOutcome.Failure,
                instance.Definition.FailureEffects);
        }

        instance.LastFiredTick = currentTick;
        instance.FireCount++;
        return CardFireResult.FiredWith(
            CardFireOutcome.Success,
            instance.Definition.SuccessEffects);
    }
}
