namespace TheCanonryGame.Game.Cards;

internal enum CardType
{
    Generator,
    System,
    Action,
}

internal sealed class CardDefinition
{
    private string _name;

    public CardDefinition(
        string id,
        string name,
        CardType type,
        int generatorId = 0,
        int gambitId = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentOutOfRangeException.ThrowIfNegative(generatorId);
        ArgumentOutOfRangeException.ThrowIfNegative(gambitId);

        Id = id;
        RuntimeId = CardRuntimeId.FromStableName(id);
        _name = string.Empty;
        Type = type;
        GeneratorId = generatorId;
        GambitId = gambitId;
        Name = name;
    }

    public string Id { get; }

    public CardRuntimeId RuntimeId { get; }

    public string Name
    {
        get => _name;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            _name = value;
        }
    }

    public CardType Type { get; }

    public int GeneratorId { get; }

    public int GambitId { get; }

    public ISet<string> Tags { get; } = new HashSet<string>(StringComparer.Ordinal);

    public string Description { get; set; } = string.Empty;
}
