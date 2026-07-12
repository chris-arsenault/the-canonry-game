namespace Engine.Domain.Compilation.KernelData;

public sealed class CompiledCultureData
{
    public IReadOnlyList<CompiledCultureDefinition> Cultures { get; init; } = [];
}

public sealed record CompiledCultureDefinition(
    int CultureId,
    float PhysicalInfluenceProductionMultiplier,
    float SemanticInfluenceProductionMultiplier);
