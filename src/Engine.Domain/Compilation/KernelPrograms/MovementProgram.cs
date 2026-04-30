namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class MovementProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<MovementRule> Rules { get; init; } = [];
}

public sealed record MovementRule(
    int KindId,
    int PlaneCode,
    float MoveChance,
    float ExploreWeight,
    float StalenessWeight,
    float CrowdingWeight);

