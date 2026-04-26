namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class RelationshipProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<RelationshipRuntimeRule> Relationships { get; init; } = [];
}

public sealed record RelationshipRuntimeRule(
    int RelationshipKindId,
    float CullThreshold);
