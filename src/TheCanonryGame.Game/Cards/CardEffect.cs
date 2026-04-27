namespace TheCanonryGame.Game.Cards;

public sealed class CardEffect
{
    public CardEffect(string kind, string description = "")
    {
        ArgumentException.ThrowIfNullOrEmpty(kind);
        Kind = kind;
        Description = description ?? string.Empty;
    }

    public string Kind { get; }

    public string Description { get; }
}
