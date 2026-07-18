namespace Engine.Observability.Tests.Observability;

using System.Reflection;

using Engine.Observability;

public sealed class TelemetrySchemaTests
{
    [Fact]
    public void TelemetryDataRows_DoNotCarryStringsOutsideSymbolTables()
    {
        var allowedStringRows = new HashSet<Type>
        {
            typeof(SymbolRow),
            typeof(EntityNameSymbolRow),
            typeof(KnowledgeSiteSymbolRow),
        };

        var rowTypes = typeof(TelemetryBatch).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           type.Name.EndsWith("Row", StringComparison.Ordinal))
            .OrderBy(type => type.Name, StringComparer.Ordinal);

        foreach (var rowType in rowTypes)
        {
            if (allowedStringRows.Contains(rowType))
            {
                continue;
            }

            var stringProperties = rowType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.PropertyType == typeof(string));

            Assert.Empty(stringProperties);
        }
    }
}
