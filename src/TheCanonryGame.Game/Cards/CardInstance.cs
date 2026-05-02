namespace TheCanonryGame.Game.Cards;

internal sealed class CardInstance
{
    public CardInstance(CardDefinition definition, int placedAtTick)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
        PlacedAtTick = placedAtTick;
    }

    public CardDefinition Definition { get; }

    public int PlacedAtTick { get; }
}
