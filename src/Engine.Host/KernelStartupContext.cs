namespace Engine.Host;

using Engine.Domain;
using Engine.Kernel;

internal sealed class KernelStartupContext
{
    private readonly Simulation _kernel;

    internal KernelStartupContext(
        Simulation kernel,
        DomainIdRegistry domainIds,
        KernelSymbolContext symbols)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        ArgumentNullException.ThrowIfNull(domainIds);
        ArgumentNullException.ThrowIfNull(symbols);

        _kernel = kernel;
        DomainIds = domainIds;
        Symbols = symbols;
    }

    public DomainIdRegistry DomainIds { get; }

    public KernelSymbolContext Symbols { get; }

    public int CurrentTick => _kernel.CurrentTick;

    public long WorldVersion => _kernel.WorldVersion;

    public void SeedCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId) =>
        _kernel.SeedCell(q, r, terrainId, ownerEntityId);

    public void SeedCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        ReadOnlySpan<CellResourceSeed> resources) =>
        _kernel.SeedCell(q, r, terrainId, ownerEntityId, resources);

    public void SeedEntity(
        int entityId,
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS) =>
        _kernel.SeedEntity(
            entityId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS);

    public void SeedAttribute(
        int entityId,
        int attributeId,
        float value) =>
        _kernel.SeedAttribute(entityId, attributeId, value);

    public void SeedResource(
        int resourceId,
        int q,
        int r,
        float value) =>
        _kernel.SeedResource(resourceId, q, r, value);

    public void SeedRelationship(
        int relationshipKindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        int factId0,
        int factId1,
        int factId2,
        int factId3,
        int factCount,
        float strength) =>
        _kernel.SeedRelationship(
            relationshipKindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            factId0,
            factId1,
            factId2,
            factId3,
            factCount,
            strength);
}
