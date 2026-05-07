namespace Engine.Domain.Compilation.KernelData;

public sealed class CompiledKernelDataSet
{
    public required CompiledEntityData Entities { get; init; }

    public required CompiledMovementData Movement { get; init; }

    public required CompiledTerrainData Terrain { get; init; }
}
