namespace TheCanonryGame.Game.Cards;

internal enum CardType
{
    Generator,
    System,
    Action,
    Intervention,
}

internal sealed class CardDefinition
{
    private string _name;

    public CardDefinition(
        string id,
        string name,
        CardType type,
        int generatorId = 0,
        int gambitId = 0,
        int interventionId = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentOutOfRangeException.ThrowIfNegative(generatorId);
        ArgumentOutOfRangeException.ThrowIfNegative(gambitId);
        ArgumentOutOfRangeException.ThrowIfNegative(interventionId);

        Id = id;
        RuntimeId = CardRuntimeId.FromStableName(id);
        _name = string.Empty;
        Type = type;
        GeneratorId = generatorId;
        GambitId = gambitId;
        InterventionId = interventionId;
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

    public int InterventionId { get; }

    public ISet<string> Tags { get; } = new HashSet<string>(StringComparer.Ordinal);

    public string Description { get; set; } = string.Empty;

    public IReadOnlyList<CardProducedEntityDescriptor> Produces { get; init; } = [];

    public IReadOnlyList<CardRelationshipChangeDescriptor> RelationshipChanges { get; init; } = [];

    public IReadOnlyList<CardTerrainTransformDescriptor> TerrainTransforms { get; init; } = [];

    public IReadOnlyList<CardDecisionModifierDescriptor> DecisionModifiers { get; init; } = [];
}

internal sealed record CardProducedEntityDescriptor(
    string Ref,
    int KindId,
    string KindStableName,
    string KindDisplayName,
    int SubtypeId,
    string? SubtypeStableName,
    string? SubtypeDisplayName,
    IReadOnlyList<CardSubtypeOptionDescriptor> SubtypeOptions,
    string Role,
    float CreateChance,
    bool CreateIfMissing)
{
    public bool IsOptional => CreateChance < 1f;

    public string DisplayLabel =>
        SubtypeDisplayName ??
        (SubtypeOptions.Count > 0 ? SubtypeOptions[0].DisplayName : null) ??
        KindDisplayName;
}

internal sealed record CardSubtypeOptionDescriptor(
    int SubtypeId,
    string StableName,
    string DisplayName);

internal enum CardRelationshipChangeKind
{
    Create,
    Remove,
    SetStrength,
}

internal sealed record CardRelationshipChangeDescriptor(
    CardRelationshipChangeKind ChangeKind,
    string SourceRef,
    string TargetRef,
    int RelationshipKindId,
    string RelationshipStableName,
    string RelationshipDisplayName,
    float Strength);

internal sealed record CardTerrainTransformDescriptor(
    int TerrainId,
    string TerrainStableName,
    string TerrainDisplayName);

internal sealed record CardDecisionModifierDescriptor(
    int TraitId,
    string TraitStableName,
    string TraitDisplayName,
    float ScoreDelta,
    float SuccessChanceDelta);
