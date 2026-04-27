namespace Engine.Host;

using Engine.Projection;

public sealed class KernelSymbolContext
{
    private readonly ProjectionSymbolCatalog _symbols;

    internal KernelSymbolContext(ProjectionSymbolCatalog symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        _symbols = symbols;
    }

    public IProjectionSymbolCatalog Catalog => _symbols;

    public KernelSymbolContext UseEntityNameSynthesizer(IProjectionEntityNameSynthesizer synthesizer)
    {
        _symbols.UseEntityNameSynthesizer(synthesizer);
        return this;
    }

    public KernelSymbolContext RegisterSymbol(
        ProjectionSymbolKind kind,
        int id,
        string stableName,
        string? displayName = null,
        string? description = null,
        string? iconKey = null,
        string? colorKey = null)
    {
        _symbols.RegisterSymbol(kind, id, stableName, displayName, description, iconKey, colorKey);
        return this;
    }

    public KernelSymbolContext RegisterEntityName(int entityId, string displayName)
    {
        _symbols.RegisterEntityName(entityId, displayName);
        return this;
    }

    public KernelSymbolContext Apply(
        IEnumerable<ProjectionSymbolSeed> symbols,
        IEnumerable<ProjectionEntityNameSeed> entityNames)
    {
        ProjectionSymbolCatalogSeeder.Apply(_symbols, symbols, entityNames);
        return this;
    }
}
