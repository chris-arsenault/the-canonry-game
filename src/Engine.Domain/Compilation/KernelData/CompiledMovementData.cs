namespace Engine.Domain.Compilation.KernelData;

public sealed class CompiledMovementData
{
    public IReadOnlyList<CompiledMovementProfileDefault> Defaults { get; init; } = [];
    public IReadOnlyList<CompiledMovementProfileSubtypeRule> SubtypeRules { get; init; } = [];
    public IReadOnlyList<CompiledMovementProfileTraitRule> TraitRules { get; init; } = [];
    public IReadOnlyList<CompiledTravelStructureDefinition> TravelStructures { get; init; } = [];
}

public sealed record CompiledMovementProfileDefault(
    int KindId,
    int PlaneCode,
    int MovementProfileId);

public sealed record CompiledMovementProfileSubtypeRule(
    int KindId,
    int SubtypeId,
    int PlaneCode,
    int MovementProfileId);

public sealed record CompiledMovementProfileTraitRule(
    int KindId,
    int TraitId,
    int PlaneCode,
    int MovementProfileId);

public sealed record CompiledTravelStructureDefinition(
    int TravelStructureId,
    int PlaneCode,
    IReadOnlyList<CompiledTravelStructureMovementProfileDefinition> MovementProfiles);

public sealed record CompiledTravelStructureMovementProfileDefinition(
    int TravelStructureId,
    int MovementProfileId,
    float MovementCost);
