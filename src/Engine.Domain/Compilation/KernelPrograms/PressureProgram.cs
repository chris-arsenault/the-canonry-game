namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class PressureProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<PressureRuntimeRule> Pressures { get; init; } = [];
}

public enum PressureRuntimeScope
{
    Coverage,
    Entity,
}

public sealed record PressureRuntimeRule(
    int PressureId,
    PressureRuntimeScope Scope,
    float Baseline,
    float HomeostasisRate,
    float SmoothingFactor);
