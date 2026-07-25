namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class RelationshipProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<RelationshipRuntimeRule> Relationships { get; init; } = [];
    public IReadOnlyList<RelationshipMaintenanceRuntimeRule> MaintenanceRules { get; init; } = [];
}

public sealed record RelationshipRuntimeRule(
    int RelationshipKindId,
    FixedScalar CullThreshold,
    bool TargetFollowsSourcePhysical,
    bool IsExclusive,
    bool IsCultureAnchor);

public sealed record RelationshipMaintenanceRuntimeRule(
    int RelationshipKindId,
    int ConditionCode,
    int PlaneCode,
    int MaxDistance,
    FixedScalar Threshold,
    FixedScalar Delta);
