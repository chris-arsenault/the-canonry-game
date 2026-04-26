namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class EntityDataProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<EntityAttributeDataRule> Attributes { get; init; } = [];
}

public sealed record EntityAttributeDataRule(
    int KindId,
    int AttributeId,
    float Minimum,
    float Maximum,
    float DefaultValue,
    float DefaultVariance);
