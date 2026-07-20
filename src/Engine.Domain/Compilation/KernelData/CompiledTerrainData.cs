namespace Engine.Domain.Compilation.KernelData;

public sealed class CompiledTerrainData
{
    public IReadOnlyList<CompiledTerrainDefinition> Terrains { get; init; } = [];

    public IReadOnlyList<CompiledCrossingProfileDefinition> CrossingProfiles { get; init; } = [];
}

public sealed record CompiledCrossingProfileDefinition(
    int CrossingClass,
    int MovementProfileId,
    float Factor,
    bool RoadNegated);

public sealed record CompiledTerrainDefinition(
    int TerrainId,
    int BoundaryGroupId,
    float MovementCost,
    float InfluenceMultiplier,
    float ClaimCostMultiplier,
    float ClaimChanceMultiplier,
    float RoadBuildMultiplier,
    float RoadSpeedMultiplier,
    IReadOnlyList<CompiledTerrainMovementProfileDefinition> MovementProfiles,
    IReadOnlyList<CompiledTerrainKindProfileDefinition> KindProfiles);

public sealed record CompiledTerrainMovementProfileDefinition(
    int TerrainId,
    int MovementProfileId,
    float MovementCost);

public sealed record CompiledTerrainKindProfileDefinition(
    int TerrainId,
    int KindId,
    int FieldFlags,
    float InfluenceMultiplier,
    float ClaimCostMultiplier,
    float ClaimChanceMultiplier);
