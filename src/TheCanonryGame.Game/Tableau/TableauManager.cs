namespace TheCanonryGame.Game.Tableau;

using TheCanonryGame.Game.Cards;

/// <summary>
/// Holds the set of cards visible to the player. Cards are passive
/// representations of kernel-driven gambits and generators — this manager
/// does not run them.
/// </summary>
internal sealed class TableauManager
{
    private readonly List<CardInstance> _activeCards = [];

    public IReadOnlyList<CardInstance> ActiveCards => _activeCards;

    public CardInstance PlaceCard(CardDefinition definition, int currentTick)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var instance = new CardInstance(definition, currentTick);
        _activeCards.Add(instance);
        return instance;
    }
}
