namespace Engine.Tableau;

using Engine.Cards;

/// <summary>
/// Manages the player's active card tableau and mana pool.
/// Cards are placed from hand (Phase 3) or programmatically (Phase 1 testing).
/// Handles activation, deactivation, lifespan expiration, and mana accounting.
/// </summary>
public sealed class TableauManager
{
    private readonly List<CardInstance> _activeCards = [];
    private readonly CardExecutor _executor;

    /// <summary>Current mana available for placing cards.</summary>
    public float CurrentMana { get; private set; }

    /// <summary>Maximum mana capacity. Expandable through deck modification events.</summary>
    public float ManaCap { get; private set; }

    /// <summary>Total mana currently consumed by active cards.</summary>
    public float ManaUsed => _activeCards.Where(c => c.IsActive).Sum(c => c.Definition.ManaCost);

    /// <summary>Cards currently in the tableau (active and expired awaiting removal).</summary>
    public IReadOnlyList<CardInstance> ActiveCards => _activeCards.Where(c => c.IsActive).ToList();

    /// <summary>Emitted when a card fires.</summary>
    public event EventHandler<CardFiredEventArgs>? CardFired;

    /// <summary>Emitted when a card expires from the tableau.</summary>
    public event EventHandler<CardExpiredEventArgs>? CardExpired;

    public TableauManager(float manaCap, CardExecutor? executor = null)
    {
        ManaCap = manaCap;
        CurrentMana = manaCap;
        _executor = executor ?? new CardExecutor();
    }

    /// <summary>
    /// Place a card into the tableau. Returns the card instance, or null if insufficient mana.
    /// </summary>
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

    /// <summary>
    /// Force-remove a card from the tableau (for testing or card-driven removal).
    /// Returns mana to the pool.
    /// </summary>
    public void RemoveCard(CardInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (instance.IsActive)
        {
            instance.IsActive = false;
            CurrentMana = Math.Min(CurrentMana + instance.Definition.ManaCost, ManaCap);
        }
    }

    /// <summary>
    /// Process one tick: tick lifespans, evaluate and fire active cards.
    /// Called during the CardExecution phase of the tick clock.
    /// </summary>
    public void Tick(int currentTick, IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var toExpire = new List<CardInstance>();

        foreach (var instance in _activeCards.Where(c => c.IsActive))
        {
            // Tick lifespan
            if (!instance.TickLifespan())
            {
                toExpire.Add(instance);
                continue;
            }

            // Try to fire
            if (_executor.TryFire(instance, context))
            {
                CardFired?.Invoke(this, new CardFiredEventArgs(instance, currentTick));
            }
        }

        // Handle expired cards
        foreach (var expired in toExpire)
        {
            CurrentMana = Math.Min(CurrentMana + expired.Definition.ManaCost, ManaCap);
            CardExpired?.Invoke(this, new CardExpiredEventArgs(expired, currentTick));
        }

        // Clean up inactive cards
        _activeCards.RemoveAll(c => !c.IsActive);
    }

    /// <summary>Expand the mana cap (from deck modification events).</summary>
    public void ExpandManaCap(float amount)
    {
        ManaCap += amount;
        CurrentMana += amount; // New capacity is immediately available
    }

    /// <summary>Set mana cap directly (for testing).</summary>
    public void SetManaCap(float cap)
    {
        ManaCap = cap;
        CurrentMana = Math.Min(CurrentMana, cap);
    }
}

public sealed class CardFiredEventArgs(CardInstance instance, int tick) : EventArgs
{
    public CardInstance Instance { get; } = instance;
    public int Tick { get; } = tick;
}

public sealed class CardExpiredEventArgs(CardInstance instance, int tick) : EventArgs
{
    public CardInstance Instance { get; } = instance;
    public int Tick { get; } = tick;
}
