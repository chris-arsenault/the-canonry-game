namespace Engine.Domain.Compilation.KernelData;

public sealed class CompiledTerrainData
{
    public IReadOnlyList<CompiledTerrainDefinition> Terrains { get; init; } = [];
}

public sealed record CompiledTerrainDefinition(
    int TerrainId,
    int BoundaryGroupId,
    float MovementCost,
    float InfluenceMultiplier,
    float ClaimCostMultiplier,
    float ClaimChanceMultiplier,
    float RoadBuildMultiplier,
    float RoadSpeedMultiplier,
    IReadOnlyList<CompiledTerrainKindProfileDefinition> KindProfiles);

public sealed record CompiledTerrainKindProfileDefinition(
    int TerrainId,
    int KindId,
    int FieldFlags,
    float MovementCost,
    float InfluenceMultiplier,
    float ClaimCostMultiplier,
    float ClaimChanceMultiplier);
