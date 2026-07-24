namespace Engine.Gameplay;

public sealed class GameplayDefinition
{
    public IReadOnlyList<GameplayModule> Modules { get; init; } = [];

    public IReadOnlyList<LowAltitudeVerbConfig> LowAltitudeVerbs { get; init; } = [];

    public MapSignalsConfig MapSignals { get; init; } = new();

    public EffectsConfig Effects { get; init; } = new();

    public IReadOnlyList<InitialInfluenceRule> InitialInfluence { get; init; } = [];

    /// <summary>Installed-artifact district bonuses by artifact subtype
    /// (docs/design/artifact-districts.md).</summary>
    public IReadOnlyList<DistrictBonusConfig> DistrictBonuses { get; init; } = [];

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

public sealed record MapSignalDefinition(
    string Id,
    string SourceKind,
    string Plane);

public sealed record AttributeEffectDefinition(
    string Id,
    string Kind,
    string Attribute,
    string Condition,
    string Plane);

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
