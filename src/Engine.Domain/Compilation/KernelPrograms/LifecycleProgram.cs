namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class LifecycleProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<LifecycleRule> Rules { get; init; } = [];
}

public sealed record LifecycleRule(
    int KindId,
    int HealthAttributeId,
    FixedScalar BaseRate,
    FixedScalar AgeCoefficient,
    FixedScalar Variance);
