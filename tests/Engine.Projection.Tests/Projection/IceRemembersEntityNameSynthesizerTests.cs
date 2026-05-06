namespace Engine.Projection.Tests.Projection;

using Engine.Projection;

using IceRemembers;

public sealed class IceRemembersEntityNameSynthesizerTests
{
    [Theory]
    [InlineData("penguin", "warden")]
    [InlineData("penguin", "scholar")]
    [InlineData("settlement", "village")]
    [InlineData("faction", "order")]
    [InlineData("artifact", "weapon")]
    [InlineData("region", "scar")]
    public void SynthesizeName_WithSeededContext_IsDeterministic(string kind, string subtype)
    {
        var context = CreateContext(kind, subtype, entityId: 10, nameSeed: 42);

        var first = IceRemembersEntityNameSynthesizer.Instance.TrySynthesizeName(context, out var firstName);
        var second = IceRemembersEntityNameSynthesizer.Instance.TrySynthesizeName(context, out var secondName);

        Assert.True(first);
        Assert.True(second);
        Assert.Equal(firstName, secondName);
        Assert.False(string.IsNullOrWhiteSpace(firstName));
        Assert.InRange(firstName.Length, 1, 40);
    }

    [Fact]
    public void SynthesizeName_ReturnsFalseForUnknownKind()
    {
        var context = CreateContext("unknown", "none", entityId: 10, nameSeed: 42);

        var result = IceRemembersEntityNameSynthesizer.Instance.TrySynthesizeName(context, out var displayName);

        Assert.False(result);
        Assert.Equal(string.Empty, displayName);
    }

    [Fact]
    public void RegionName_UsesRegionSpecificSuffix()
    {
        var context = CreateContext("region", "shelf", entityId: 10, nameSeed: 42);

        var result = IceRemembersEntityNameSynthesizer.Instance.TrySynthesizeName(context, out var displayName);

        Assert.True(result);
        Assert.Contains(
            displayName.Split(' ')[^1],
            new[] { "Shelf", "Ledge", "Face", "Wall" });
    }

    private static ProjectionEntityNameContext CreateContext(
        string kind,
        string subtype,
        int entityId,
        int nameSeed)
    {
        var kindId = kind.GetHashCode(StringComparison.Ordinal) & 0x7fffffff;
        var subtypeId = subtype.GetHashCode(StringComparison.Ordinal) & 0x7fffffff;
        var cultureId = "culture_a".GetHashCode(StringComparison.Ordinal) & 0x7fffffff;
        return new ProjectionEntityNameContext(
            new ProjectionEntityNameKey(entityId, kindId, subtypeId, cultureId, nameSeed),
            new ProjectionSymbol(ProjectionSymbolKind.Kind, kindId, kind, ToDisplay(kind)),
            new ProjectionSymbol(ProjectionSymbolKind.Subtype, subtypeId, subtype, ToDisplay(subtype)),
            new ProjectionSymbol(ProjectionSymbolKind.Culture, cultureId, "culture_a", "Culture A"));
    }

    private static string ToDisplay(string stableName) =>
        string.Join(' ', stableName.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
