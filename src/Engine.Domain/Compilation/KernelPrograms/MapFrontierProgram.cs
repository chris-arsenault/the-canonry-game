namespace Engine.Domain.Compilation.KernelPrograms;

/// <summary>Compiled growing-map configuration for the map-frontier program.</summary>
public sealed class MapFrontierProgram
{
    public bool Enabled { get; init; }
    public int ThawAgeId { get; init; }
    public int RefreezeAgeId { get; init; }
    public IReadOnlyList<int> AdditionalThawAgeIds { get; init; } = [];
    public IReadOnlyList<int> AdditionalRefreezeAgeIds { get; init; } = [];
    public int FrozenTerrainId { get; init; }
    public int MaxRadius { get; init; }
    public int RevealBudgetPerPulse { get; init; }
    public int BaseRevealRadius { get; init; }
    public IReadOnlyList<(int KindId, int Radius)> KindRevealRadii { get; init; } = [];
    public int FrostTemperatureCeiling { get; init; }
    public float IceThicknessPressureWeight { get; init; }
    public float GeothermalRefugeWeight { get; init; }
    public float FreezeAgePressureScalar { get; init; }
    public float ThawAgePressureScalar { get; init; }
    public float BaseIceResistance { get; init; }
    public float CultureMassResistanceScale { get; init; }
    public float RoadIceResistance { get; init; }
    public int IceAdvanceBudgetPerPulse { get; init; }
    public int IceRetreatBudgetPerPulse { get; init; }
    public bool SemanticGrowthEnabled { get; init; }
    public int SemanticMaxRadius { get; init; }
    public float SemanticFrontierMassThreshold { get; init; }
    public int SemanticBudgetPerPulse { get; init; }
    public int SeaIceCoverTerrainId { get; init; }
    public IReadOnlyList<int> SeaIceEligibleTerrainIds { get; init; } = [];
    public int SeaIceFreezeBudgetPerPulse { get; init; }
    public int SeaIceMeltBudgetPerPulse { get; init; }
}
