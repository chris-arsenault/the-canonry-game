namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class UpkeepProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<LifecycleUpkeepRule> LifecycleRules { get; init; } = [];
}

public sealed record LifecycleUpkeepRule(
    int KindId,
    int HealthAttributeId,
    float BaseRate,
    float AgeCoefficient,
    float Variance = 0.2f);
