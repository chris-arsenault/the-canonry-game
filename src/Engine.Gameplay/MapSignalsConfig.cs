namespace Engine.Gameplay;

public sealed class MapSignalsConfig
{
    public float SemanticBaseCapacity { get; init; } = 2f;

    public int CrowdingRadius { get; init; } = 1;

    public IReadOnlyList<TerrainMapSignalConfig> Terrains { get; init; } = [];

    /// <summary>
    /// Physical carrying capacity derived from hex facts. When set, physical
    /// capacity ignores terrain rows entirely; terrain rows then serve only
    /// the semantic plane.
    /// </summary>
    public PhysicalCapacityFormulaConfig? PhysicalCapacityFormula { get; init; }

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

    public int SignalRadius { get; init; } = 1;

    public float CapacityEmission { get; init; }

    public float SupportEmission { get; init; }

    public float ThreatEmission { get; init; }

    public float BaseAttractorMass { get; init; } = 1f;

    public float CoverageAttractorScale { get; init; }

    public float InfluenceAttractorScale { get; init; }

    public float ActivityAttractorScale { get; init; }

    public IReadOnlyList<MapSignalScalarInputConfig> AttractorScalarInputs { get; init; } = [];

    public int AttractorRadius { get; init; } = 1;
}

public sealed class MapSignalScalarInputConfig
{
    public required string Attribute { get; init; }

    public required float AttractorScale { get; init; }
}

public sealed class RelationshipMapSignalConfig
{
    public required string RelationshipKind { get; init; }

    public int SignalRadius { get; init; } = 1;

    public float SupportEmission { get; init; }

    public float ThreatEmission { get; init; }
}

/// <summary>
/// Authored parameters for fact-derived physical carrying capacity.
/// Temperatures are in generator intensity units (0-1000).
/// </summary>
public sealed class PhysicalCapacityFormulaConfig
{
    /// <summary>Below this warmth nothing lives; suitability is zero.</summary>
    public required int ColdFloorTemperature { get; init; }

    /// <summary>At or above this warmth suitability is full.</summary>
    public required int WarmPeakTemperature { get; init; }

    /// <summary>How strongly geothermal heat counts toward warmth (0-1).</summary>
    public float GeothermalBlend { get; init; } = 0.5f;

    /// <summary>Capacity bonus per full river coverage.</summary>
    public float FreshwaterWeight { get; init; } = 0.5f;

    /// <summary>Capacity bonus per full floodplain/delta coverage.</summary>
    public float FertileWeight { get; init; } = 0.5f;

    /// <summary>Relief fraction (0-1) below which ground counts as comfortable.</summary>
    public float ReliefComfort { get; init; } = 0.25f;

    /// <summary>Capacity penalty per unit of relief beyond comfort.</summary>
    public float ReliefPenalty { get; init; } = 1f;

    /// <summary>Capacity penalty per unit of roughness.</summary>
    public float RoughnessPenalty { get; init; } = 0.5f;

    /// <summary>Capacity of fully suitable, fully land ground before bonuses.</summary>
    public float LandScale { get; init; } = 3f;

    /// <summary>Capacity of fully suitable open water.</summary>
    public float MarineScale { get; init; } = 1.5f;
}
