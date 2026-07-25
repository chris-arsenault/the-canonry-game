namespace Engine.Host;

using Engine.Constants;
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
        int ownerEntityId,
        int superregionId = 0,
        int continentId = 0) =>
        _kernel.SeedCell(q, r, terrainId, ownerEntityId, superregionId, continentId);

    public void SeedCell(
        int planeCode,
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId = 0,
        int continentId = 0) =>
        _kernel.SeedCell(planeCode, q, r, terrainId, ownerEntityId, superregionId, continentId);

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
        _kernel.SeedAttribute(entityId, attributeId, FixedScalar.FromSingle(value));

    public void SeedRelationship(
        int relationshipKindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        float strength) =>
        _kernel.SeedRelationship(
            relationshipKindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            FixedScalar.FromSingle(strength));
}
