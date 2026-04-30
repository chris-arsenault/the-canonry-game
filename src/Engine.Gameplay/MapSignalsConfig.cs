namespace Engine.Gameplay;

public sealed class MapSignalsConfig
{
    public float SemanticBaseCapacity { get; init; } = 2f;

    public int CrowdingRadius { get; init; } = 1;

    public IReadOnlyList<TerrainMapSignalConfig> Terrains { get; init; } = [];

    public IReadOnlyList<KindMapSignalConfig> Kinds { get; init; } = [];

    public IReadOnlyList<RelationshipMapSignalConfig> Relationships { get; init; } = [];
}

public sealed class TerrainMapSignalConfig
{
    public required string Terrain { get; init; }

    public required float BaseCapacity { get; init; }
}

public sealed class KindMapSignalConfig
{
    public required string Kind { get; init; }

    public string? HealthAttribute { get; init; }

    public int SignalRadius { get; init; } = 1;

    public float CapacityEmission { get; init; }

    public float SupportEmission { get; init; }

    public float ThreatEmission { get; init; }

    public float BaseAttractorMass { get; init; } = 1f;

    public float CoverageAttractorScale { get; init; }

    public float InfluenceAttractorScale { get; init; }

    public float ActivityAttractorScale { get; init; }

    public float HealthAttractorScale { get; init; }

    public int AttractorRadius { get; init; } = 1;
}

public sealed class RelationshipMapSignalConfig
{
    public required string RelationshipKind { get; init; }

    public int SignalRadius { get; init; } = 1;

    public float SupportEmission { get; init; }

    public float ThreatEmission { get; init; }
}
