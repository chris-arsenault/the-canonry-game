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
    public float FrontierMassThreshold { get; init; }
    public float ReclaimMassCeiling { get; init; }
    public int ThawBudgetPerPulse { get; init; }
    public int RefreezeBudgetPerPulse { get; init; }
    public bool SemanticGrowthEnabled { get; init; }
    public int SemanticMaxRadius { get; init; }
    public float SemanticFrontierMassThreshold { get; init; }
    public int SemanticBudgetPerPulse { get; init; }
    public int SeaIceCoverTerrainId { get; init; }
    public IReadOnlyList<int> SeaIceEligibleTerrainIds { get; init; } = [];
    public int SeaIceFreezeBudgetPerPulse { get; init; }
    public int SeaIceMeltBudgetPerPulse { get; init; }
}
