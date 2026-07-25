namespace Engine.Domain.Compilation.KernelData;

public sealed class CompiledEntityData
{
    public IReadOnlyList<CompiledEntityAttributeData> Attributes { get; init; } = [];
    public IReadOnlyList<CompiledEntityTraitData> Traits { get; init; } = [];
}

public sealed record CompiledEntityAttributeData(
    int KindId,
    int AttributeId,
    FixedScalar Minimum,
    FixedScalar Maximum,
    FixedScalar DefaultValue,
    FixedScalar DefaultVariance);

public sealed record CompiledEntityTraitData(
    int KindId,
    int TraitId,
    FixedScalar AssignmentChance);
