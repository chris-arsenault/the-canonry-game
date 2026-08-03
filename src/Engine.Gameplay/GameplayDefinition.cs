namespace Engine.Gameplay;

public sealed class GameplayDefinition
{
    public IReadOnlyList<GameplayModule> Modules { get; init; } = [];

    /// <summary>
    /// Ordered stable ids of the domain's authored technologies. The order is
    /// the kernel knowledge site index order; the startup seed's knowledge
    /// sites must list the same techs in the same order. Content gates
    /// (preconditions, generator gates, movement gates) reference techs by
    /// these ids.
    /// </summary>
    public IReadOnlyList<string> KnowledgeTechIds { get; init; } = [];

    public IReadOnlyList<LowAltitudeVerbConfig> LowAltitudeVerbs { get; init; } = [];

    public LocalityConfig Locality { get; init; } = new();

    public EffectsConfig Effects { get; init; } = new();

    public IReadOnlyList<InitialInfluenceRule> InitialInfluence { get; init; } = [];

    /// <summary>Installed-artifact district bonuses by artifact subtype
    /// (docs/design/artifact-districts.md).</summary>
    public IReadOnlyList<DistrictBonusConfig> DistrictBonuses { get; init; } = [];

    /// <summary>Named stage catalogs per kind (docs/design/entity-stages.md).</summary>
    public IReadOnlyList<KindStagesConfig> KindStages { get; init; } = [];

    public IReadOnlyList<MapControlDefinition> MapControl { get; init; } = [];

    public SemanticPlaneConfig SemanticPlane { get; init; } = SemanticPlaneConfig.Default;
}

public sealed record GameplayModule(string Id);

public sealed record LowAltitudeVerbDefinition(
    string Id,
    string Kind,
    string Operation,
    string TargetMode,
    string Plane);

public sealed record AttributeEffectDefinition(
    string Id,
    string Kind,
    string Attribute,
    string Condition,
    string Plane);

/// <summary>One named stage in a kind's maturity ladder, with the
/// extensible property set the kernel consumes
/// (docs/design/entity-stages.md).</summary>
public sealed record StageConfig(
    string Name,
    string DisplayName,
    float BandMultiplier,
    int WorkerBonus,
    float AmbientEmission,
    int DistrictCapacity);

public sealed record KindStagesConfig(
    string Kind,
    IReadOnlyList<StageConfig> Stages);

public enum DistrictBonusKind
{
    Healing,
    Culture,
    Road,
}

/// <summary>What district an installed artifact of this subtype becomes,
/// and how strongly it acts per tick.</summary>
public sealed record DistrictBonusConfig(
    string ArtifactSubtype,
    DistrictBonusKind Bonus,
    float Magnitude);

public sealed record InitialInfluenceRule(
    string Kind,
    string Plane,
    float InfluenceValue);

public sealed record MapControlDefinition(
    string Id,
    string Influence,
    string Coverage,
    string Territory);

public sealed class SemanticPlaneConfig
{
    public static SemanticPlaneConfig Default { get; } = new();
}
