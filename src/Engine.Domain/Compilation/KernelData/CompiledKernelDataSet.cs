namespace Engine.Domain.Compilation.KernelData;

public sealed class CompiledKernelDataSet
{
    public required CompiledEntityData Entities { get; init; }

    public required CompiledMovementData Movement { get; init; }

    public required CompiledTerrainData Terrain { get; init; }

    public required CompiledCultureData Cultures { get; init; }

    public CompiledPlacementTasteData PlacementTaste { get; init; } = new();
}

public sealed class CompiledPlacementTasteData
{
    public IReadOnlyList<CompiledPlacementTasteWeight> KindWeights { get; init; } = [];

    public IReadOnlyList<CompiledPlacementTasteWeight> CultureWeights { get; init; } = [];
}

public sealed record CompiledPlacementTasteWeight(int Id, int AxisCode, float Weight);
