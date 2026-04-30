namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class EffectsProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<AttributeDeltaEffectRule> AttributeDeltaEffects { get; init; } = [];
}

public sealed record AttributeDeltaEffectRule(
    int KindId,
    int AttributeId,
    int ConditionCode,
    int RelationshipKindId,
    int RelationshipDirectionCode,
    int PlaneCode,
    int MaxDistance,
    float Threshold,
    float MinimumRelationshipStrength,
    float DeltaPerTick);
