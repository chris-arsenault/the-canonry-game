namespace Engine.Cards;

using Engine.Processes;

/// <summary>
/// The type of a card, determining which executor processes it.
/// </summary>
public enum CardType
{
    Generator,
    System,
    Action,
}

/// <summary>
/// A complete card definition: type, preconditions, mutations, and metadata.
/// Card definitions are mutable data structures to support Balatro-style runtime modification.
/// </summary>
public sealed class CardDefinition
{
    public string Id { get; }
    public string Name { get; set; }
    public CardType Type { get; }
    public string? GeneratorId { get; }
    public int ManaCost { get; set; }
    public int Cooldown { get; set; }
    public int Cadence { get; set; }
    public int Lifespan { get; set; }

    /// <summary>Tags for synergy identification and inter-card interaction.</summary>
    public HashSet<string> Tags { get; } = [];

    /// <summary>Precondition tree. Card fires only when this evaluates true.</summary>
    public IPrecondition Precondition { get; set; }

    /// <summary>Mutations applied when the card fires successfully.</summary>
    public System.Collections.ObjectModel.Collection<IMutation> Mutations { get; } = [];

    /// <summary>For action cards: mutations applied on failure.</summary>
    public System.Collections.ObjectModel.Collection<IMutation> FailureMutations { get; } = [];

    /// <summary>For action cards: base success probability (0-1).</summary>
    public float SuccessProbability { get; set; } = 1f;

    /// <summary>Human-readable description of what this card does.</summary>
    public string Description { get; set; } = string.Empty;

    public CardDefinition(
        string id,
        string name,
        CardType type,
        IPrecondition precondition,
        int manaCost = 2,
        int cooldown = 10,
        int cadence = 1,
        int lifespan = 100,
        string? generatorId = null)
    {
        Id = id;
        Name = name;
        Type = type;
        GeneratorId = generatorId;
        Precondition = precondition;
        ManaCost = manaCost;
        Cooldown = cooldown;
        Cadence = cadence;
        Lifespan = lifespan;
    }
}
