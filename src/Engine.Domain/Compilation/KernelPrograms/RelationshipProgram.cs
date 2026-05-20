namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class RelationshipProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<RelationshipRuntimeRule> Relationships { get; init; } = [];
    public IReadOnlyList<RelationshipMaintenanceRuntimeRule> MaintenanceRules { get; init; } = [];
}

public sealed record RelationshipRuntimeRule(
    int RelationshipKindId,
    float CullThreshold,
    bool TargetFollowsSourcePhysical,
    bool IsExclusive);

public sealed record RelationshipMaintenanceRuntimeRule(
    int RelationshipKindId,
    int ConditionCode,
    int PlaneCode,
    int MaxDistance,
    float Threshold,
    float Delta);
