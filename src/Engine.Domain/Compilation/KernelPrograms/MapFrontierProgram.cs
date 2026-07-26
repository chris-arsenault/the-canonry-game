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
    public int BaseRevealRadius { get; init; }
    public IReadOnlyList<(int KindId, int Radius)> KindRevealRadii { get; init; } = [];
    public int FrostTemperatureCeiling { get; init; }
    public FixedScalar IceThicknessPressureWeight { get; init; }
    public FixedScalar GeothermalRefugeWeight { get; init; }
    public FixedScalar FreezeAgePressureScalar { get; init; }
    public FixedScalar ThawAgePressureScalar { get; init; }
    public FixedScalar BaseIceResistance { get; init; }
    public FixedScalar CultureMassResistanceScale { get; init; }
    public FixedScalar RoadIceResistance { get; init; }
    public int IceAdvanceBudgetPerRun { get; init; }
    public int IceRetreatBudgetPerRun { get; init; }
    public bool SemanticGrowthEnabled { get; init; }
    public int SemanticMaxRadius { get; init; }
    public FixedScalar SemanticFrontierMassThreshold { get; init; }
    public int SemanticBudgetPerRun { get; init; }
    public int SeaIceCoverTerrainId { get; init; }
    public IReadOnlyList<int> SeaIceEligibleTerrainIds { get; init; } = [];
    public int SeaIceFreezeBudgetPerRun { get; init; }
    public int SeaIceMeltBudgetPerRun { get; init; }
}
