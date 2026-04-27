namespace TheCanonryGame.Game.Cards;

internal sealed class CardInstance
{
    public CardInstance(CardDefinition definition, int placedAtTick)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
        PlacedAtTick = placedAtTick;
        LifespanRemaining = definition.Lifespan;
    }

    public CardDefinition Definition { get; }

    public int PlacedAtTick { get; }

    public int? LastFiredTick { get; set; }

    public int LifespanRemaining { get; set; }

    public bool IsActive { get; set; } = true;

    public int FireCount { get; set; }

    public bool LastConditionResult { get; set; }

    public bool IsReady(int currentTick)
    {
        if (!IsActive)
        {
            return false;
        }

        if (LastFiredTick is null)
        {
            return true;
        }

        return currentTick - LastFiredTick.Value >= Definition.Cooldown;
    }

    public bool IsOnCadence(int currentTick)
    {
        if (Definition.Cadence <= 0)
        {
            return true;
        }

        return currentTick % Definition.Cadence == 0;
    }

    public bool TickLifespan()
    {
        LifespanRemaining--;
        if (LifespanRemaining > 0)
        {
            return true;
        }

        IsActive = false;
        return false;
    }
}
