namespace TheCanonryGame.Game.Tableau;

using TheCanonryGame.Game.Cards;

internal sealed class TableauManager
{
    private readonly List<CardInstance> _activeCards = [];
    private readonly CardExecutor _executor;

    public TableauManager(float manaCap, CardExecutor? executor = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(manaCap);
        ManaCap = manaCap;
        CurrentMana = manaCap;
        _executor = executor ?? new CardExecutor();
    }

    public float CurrentMana { get; private set; }

    public float ManaCap { get; private set; }

    public float ManaUsed => _activeCards.Where(card => card.IsActive).Sum(card => card.Definition.ManaCost);

    public IReadOnlyList<CardInstance> ActiveCards => _activeCards.Where(card => card.IsActive).ToArray();

    public event EventHandler<CardFiredEventArgs>? CardFired;

    public event EventHandler<CardExpiredEventArgs>? CardExpired;

    public CardInstance? PlaceCard(CardDefinition definition, int currentTick)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.ManaCost > CurrentMana)
        {
            return null;
        }

        var instance = new CardInstance(definition, currentTick);
        _activeCards.Add(instance);
        CurrentMana -= definition.ManaCost;
        return instance;
    }

    public void RemoveCard(CardInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (!instance.IsActive)
        {
            return;
        }

        instance.IsActive = false;
        CurrentMana = Math.Min(CurrentMana + instance.Definition.ManaCost, ManaCap);
        _activeCards.Remove(instance);
    }

    public CardTableauTickResult Tick(int currentTick)
    {
        var firedCards = new List<CardTableauFiredCard>();
        var expiredCards = new List<CardInstance>();

        foreach (var instance in _activeCards.Where(card => card.IsActive).ToArray())
        {
            if (!instance.TickLifespan())
            {
                expiredCards.Add(instance);
                continue;
            }

            var fireResult = _executor.TryFire(instance, currentTick);
            if (!fireResult.Fired)
            {
                continue;
            }

            var fired = new CardTableauFiredCard(instance, fireResult);
            firedCards.Add(fired);
            CardFired?.Invoke(this, new CardFiredEventArgs(instance, fireResult, currentTick));
        }

        foreach (var expired in expiredCards)
        {
            CurrentMana = Math.Min(CurrentMana + expired.Definition.ManaCost, ManaCap);
            CardExpired?.Invoke(this, new CardExpiredEventArgs(expired, currentTick));
        }

        _activeCards.RemoveAll(card => !card.IsActive);
        return new CardTableauTickResult(firedCards, expiredCards);
    }

    public void ExpandManaCap(float amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ManaCap += amount;
        CurrentMana += amount;
    }

    public void SetManaCap(float cap)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cap);
        ManaCap = cap;
        CurrentMana = Math.Min(CurrentMana, cap);
    }
}

internal sealed record CardTableauFiredCard(CardInstance Instance, CardFireResult Result);

internal sealed class CardTableauTickResult
{
    public CardTableauTickResult(
        IReadOnlyList<CardTableauFiredCard> firedCards,
        IReadOnlyList<CardInstance> expiredCards)
    {
        ArgumentNullException.ThrowIfNull(firedCards);
        ArgumentNullException.ThrowIfNull(expiredCards);
        FiredCards = firedCards;
        ExpiredCards = expiredCards;
    }

    public IReadOnlyList<CardTableauFiredCard> FiredCards { get; }

    public IReadOnlyList<CardInstance> ExpiredCards { get; }
}

internal sealed class CardFiredEventArgs : EventArgs
{
    public CardFiredEventArgs(CardInstance instance, CardFireResult result, int tick)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(result);
        Instance = instance;
        Result = result;
        Tick = tick;
    }

    public CardInstance Instance { get; }

    public CardFireResult Result { get; }

    public int Tick { get; }
}

internal sealed class CardExpiredEventArgs : EventArgs
{
    public CardExpiredEventArgs(CardInstance instance, int tick)
    {
        ArgumentNullException.ThrowIfNull(instance);
        Instance = instance;
        Tick = tick;
    }

    public CardInstance Instance { get; }

    public int Tick { get; }
}
