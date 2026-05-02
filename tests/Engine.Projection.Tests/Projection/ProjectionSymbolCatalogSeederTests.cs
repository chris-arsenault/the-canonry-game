namespace Engine.Projection.Tests.Projection;

public sealed class ProjectionSymbolCatalogSeederTests
{
    [Fact]
    public void Apply_RegistersSymbolsAndExplicitEntityNames()
    {
        var catalog = new ProjectionSymbolCatalog();

        ProjectionSymbolCatalogSeeder.Apply(
            catalog,
            [
                new ProjectionSymbolSeed(
                    ProjectionSymbolKind.Generator,
                    7,
                    "ice_memory",
                    "Ice Memory",
                    "A generator.",
                    "generator-icon",
                    "cold"),
            ],
            [
                new ProjectionEntityNameSeed(42, "Aster"),
            ]);

        Assert.True(catalog.TryResolve(ProjectionSymbolKind.Generator, 7, out var symbol));
        Assert.Equal("ice_memory", symbol.StableName);
        Assert.Equal("Ice Memory", symbol.DisplayName);
        Assert.Equal("A generator.", symbol.Description);
        Assert.Equal("generator-icon", symbol.IconKey);
        Assert.Equal("cold", symbol.ColorKey);
        Assert.Equal("Aster", catalog.ResolveEntityName(new ProjectionEntityNameKey(42, 0, 0, 0, 0)));
    }

    [Fact]
    public void Apply_UsesStableNameDisplayFallbackThroughCatalogRules()
    {
        var catalog = new ProjectionSymbolCatalog();

        ProjectionSymbolCatalogSeeder.Apply(
            catalog,
            [new ProjectionSymbolSeed(ProjectionSymbolKind.Terrain, 3, "broken-ice")],
            []);

        Assert.Equal("Broken Ice", catalog.ResolveDisplayName(ProjectionSymbolKind.Terrain, 3));
    }
}
