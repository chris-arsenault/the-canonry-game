namespace Engine.Domain.Compilation.KernelData;

public sealed class CompiledEntityData
{
    public IReadOnlyList<CompiledEntityAttributeData> Attributes { get; init; } = [];
}

public sealed record CompiledEntityAttributeData(
    int KindId,
    int AttributeId,
    float Minimum,
    float Maximum,
    float DefaultValue,
    float DefaultVariance);
