namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class UpkeepProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<AttributeUpkeepRule> AttributeRules { get; init; } = [];
    public IReadOnlyList<LifecycleUpkeepRule> LifecycleRules { get; init; } = [];
}

public sealed record AttributeUpkeepRule(
    int KindId,
    int AttributeId,
    float DriftRate,
    int LocationResourceId,
    float LocationResourceDriftPerUnit,
    float LocationResourceDriftMinOffset,
    float LocationResourceDriftMaxOffset,
    int CriticalTargetAttributeId,
    float CriticalThreshold,
    float CriticalDelta);

public sealed record LifecycleUpkeepRule(
    int KindId,
    int HealthAttributeId,
    float BaseRate,
    float AgeCoefficient,
    float Variance = 0.2f);
