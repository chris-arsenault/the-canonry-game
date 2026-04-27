namespace Engine.Projection.Tests.Projection;

using Engine.Projection;

public sealed class ProjectionSymbolCatalogTests
{
    [Fact]
    public void ResolveDisplayName_ReturnsRegisteredSymbolDisplayName()
    {
        var catalog = new ProjectionSymbolCatalog()
            .RegisterSymbol(
                ProjectionSymbolKind.Generator,
                7,
                "ice_memory",
                "Ice Memory");

        Assert.True(catalog.TryResolve(ProjectionSymbolKind.Generator, 7, out var symbol));
        Assert.Equal("ice_memory", symbol.StableName);
        Assert.Equal("Ice Memory", catalog.ResolveDisplayName(ProjectionSymbolKind.Generator, 7));
    }

    [Fact]
    public void ResolveEntityName_UsesExplicitEntityNameWhenRegistered()
    {
        var catalog = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Kind, 2, "settlement")
            .RegisterSymbol(ProjectionSymbolKind.Subtype, 3, "harbor")
            .RegisterEntityName(42, "Port Rime");

        var name = catalog.ResolveEntityName(new ProjectionEntityNameKey(
            EntityId: 42,
            KindId: 2,
            SubtypeId: 3,
            CultureId: 4,
            NameSeed: 99));

        Assert.Equal("Port Rime", name);
    }

    [Fact]
    public void ResolveEntityName_GeneratesDeterministicNameFromPrimitiveSeed()
    {
        var catalog = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Kind, 2, "settlement")
            .RegisterSymbol(ProjectionSymbolKind.Subtype, 3, "harbor");
        var key = new ProjectionEntityNameKey(
            EntityId: 42,
            KindId: 2,
            SubtypeId: 3,
            CultureId: 4,
            NameSeed: 99);

        var first = catalog.ResolveEntityName(key);
        var second = catalog.ResolveEntityName(key);

        Assert.Equal(first, second);
        Assert.StartsWith("Harbor ", first, StringComparison.Ordinal);
    }
}
