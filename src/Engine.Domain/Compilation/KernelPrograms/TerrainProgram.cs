namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class TerrainProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<TerrainRuntimeRule> Terrains { get; init; } = [];
}

public sealed record TerrainRuntimeRule(
    int TerrainId,
    bool IsPassable,
    int BoundaryGroupId);
