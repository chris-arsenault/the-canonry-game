namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class MapSignalsProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public float SemanticBaseCapacity { get; init; } = 2f;
    public int CrowdingRadius { get; init; } = 1;
    public IReadOnlyList<TerrainMapSignalRule> Terrains { get; init; } = [];
    public IReadOnlyList<KindMapSignalRule> Kinds { get; init; } = [];
    public IReadOnlyList<RelationshipMapSignalRule> Relationships { get; init; } = [];
}

public sealed record TerrainMapSignalRule(int TerrainId, float BaseCapacity);

public sealed record KindMapSignalRule(
    int KindId,
    int HealthAttributeId,
    int SignalRadius,
    float CapacityEmission,
    float SupportEmission,
    float ThreatEmission,
    float BaseAttractorMass,
    float CoverageAttractorScale,
    float InfluenceAttractorScale,
    float ActivityAttractorScale,
    float HealthAttractorScale,
    int AttractorRadius);

public sealed record RelationshipMapSignalRule(
    int RelationshipKindId,
    int SignalRadius,
    float SupportEmission,
    float ThreatEmission);
