namespace Engine.Domain.Compilation.KernelData;

public sealed class CompiledTerrainData
{
    public IReadOnlyList<CompiledTerrainDefinition> Terrains { get; init; } = [];
}

public sealed record CompiledTerrainDefinition(
    int TerrainId,
    int BoundaryGroupId);
