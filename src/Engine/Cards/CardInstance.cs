namespace Engine.Cards;

/// <summary>
/// Runtime state for a card placed in the tableau. Tracks execution state
/// (last fired, cooldown, lifespan remaining, precondition status).
/// </summary>
public sealed class CardInstance
{
    public CardDefinition Definition { get; }
    public int PlacedAtTick { get; }

    /// <summary>Tick when this card last fired. Null if never fired.</summary>
    public int? LastFiredTick { get; set; }

    /// <summary>Ticks remaining before the card naturally expires from the tableau.</summary>
    public int LifespanRemaining { get; set; }

    /// <summary>Whether the card is still active in the tableau.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Achievement progress: how many times this card has fired.</summary>
    public int FireCount { get; set; }

    /// <summary>Last precondition evaluation result (for diagnostic display).</summary>
    public bool LastPreconditionResult { get; set; }

    public CardInstance(CardDefinition definition, int placedAtTick)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
        PlacedAtTick = placedAtTick;
        LifespanRemaining = definition.Lifespan;
    }

    /// <summary>Check if the card is ready to fire (not on cooldown).</summary>
    public bool IsReady(int currentTick)
    {
        if (!IsActive) return false;
        if (LastFiredTick is null) return true;
        return (currentTick - LastFiredTick.Value) >= Definition.Cooldown;
    }

    /// <summary>Check if the card should run on this tick (cadence check). Cadence 0 means every tick.</summary>
    public bool IsOnCadence(int currentTick)
    {
        if (Definition.Cadence <= 0) return true;
        return currentTick % Definition.Cadence == 0;
    }

    /// <summary>Decrement lifespan. Returns false if expired.</summary>
    public bool TickLifespan()
    {
        LifespanRemaining--;
        if (LifespanRemaining <= 0)
        {
            IsActive = false;
            return false;
        }

        return true;
    }
}
