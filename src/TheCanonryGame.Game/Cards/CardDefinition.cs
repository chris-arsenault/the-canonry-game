namespace TheCanonryGame.Game.Cards;

using System.Collections.ObjectModel;

public enum CardType
{
    Generator,
    System,
    Action,
}

public sealed class CardDefinition
{
    private string _name;
    private ICardCondition _condition;
    private int _manaCost;
    private int _cooldown;
    private int _cadence;
    private int _lifespan;
    private float _successProbability = 1f;

    public CardDefinition(
        string id,
        string name,
        CardType type,
        ICardCondition condition,
        int manaCost = 2,
        int cooldown = 10,
        int cadence = 1,
        int lifespan = 100,
        int generatorId = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentOutOfRangeException.ThrowIfNegative(generatorId);

        Id = id;
        RuntimeId = CardRuntimeId.FromStableName(id);
        _name = string.Empty;
        _condition = condition;
        Type = type;
        GeneratorId = generatorId;
        Name = name;
        ManaCost = manaCost;
        Cooldown = cooldown;
        Cadence = cadence;
        Lifespan = lifespan;
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

    public int ManaCost
    {
        get => _manaCost;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _manaCost = value;
        }
    }

    public int Cooldown
    {
        get => _cooldown;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _cooldown = value;
        }
    }

    public int Cadence
    {
        get => _cadence;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _cadence = value;
        }
    }

    public int Lifespan
    {
        get => _lifespan;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _lifespan = value;
        }
    }

    public ISet<string> Tags { get; } = new HashSet<string>(StringComparer.Ordinal);

    public ICardCondition Condition
    {
        get => _condition;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _condition = value;
        }
    }

    public Collection<CardEffect> SuccessEffects { get; } = [];

    public Collection<CardEffect> FailureEffects { get; } = [];

    public float SuccessProbability
    {
        get => _successProbability;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1f);
            _successProbability = value;
        }
    }

    public string Description { get; set; } = string.Empty;
}
