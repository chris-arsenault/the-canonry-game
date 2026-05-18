namespace Engine.Projection.Tests.Projection;

using Engine.Projection;

using IceRemembers;

public sealed class IceRemembersEntityNameSynthesizerTests
{
    [Theory]
    [InlineData("penguin", "warden", "aurora_stack")]
    [InlineData("settlement", "rookery", "aurora_stack")]
    [InlineData("faction", "order", "nightshelf")]
    [InlineData("artifact", "weapon", "aurora_stack")]
    [InlineData("region", "scar", "nightshelf")]
    [InlineData("orca", "scout", "wake_singers")]
    [InlineData("point_of_interest", "vent", "aurora_stack")]
    [InlineData("ability", "rite", "aurora_stack")]
    [InlineData("monster", "wraith", "nightshelf")]
    [InlineData("transport", "trade", "wake_singers")]
    [InlineData("occurrence", "war", "wake_singers")]
    [InlineData("region", "face", "the_ice")]
    [InlineData("point_of_interest", "vent", "the_ice")]
    [InlineData("monster", "wraith", "the_ice")]
    [InlineData("penguin", "warden", "the_ice")]
    public void SynthesizeName_WithSeededContext_IsDeterministic(string kind, string subtype, string culture)
    {
        var context = CreateContext(kind, subtype, culture, entityId: 10, nameSeed: 42);

        var first = IceRemembersEntityNameSynthesizer.Instance.TrySynthesizeName(context, out var firstName);
        var second = IceRemembersEntityNameSynthesizer.Instance.TrySynthesizeName(context, out var secondName);

        Assert.True(first);
        Assert.True(second);
        Assert.Equal(firstName, secondName);
        Assert.False(string.IsNullOrWhiteSpace(firstName));
        Assert.InRange(firstName.Length, 1, 96);
    }

    [Fact]
    public void SynthesizeName_ReturnsFalseForUnknownKind()
    {
        var context = CreateContext("unknown", "none", "aurora_stack", entityId: 10, nameSeed: 42);

        var result = IceRemembersEntityNameSynthesizer.Instance.TrySynthesizeName(context, out var displayName);

        Assert.False(result);
        Assert.Equal(string.Empty, displayName);
    }

    [Fact]
    public void SynthesizedNames_VaryByCulture()
    {
        var aurora = CreateContext("penguin", "warden", "aurora_stack", entityId: 10, nameSeed: 42);
        var nightshelf = CreateContext("penguin", "warden", "nightshelf", entityId: 10, nameSeed: 42);

        var first = IceRemembersEntityNameSynthesizer.Instance.TrySynthesizeName(aurora, out var auroraName);
        var second = IceRemembersEntityNameSynthesizer.Instance.TrySynthesizeName(nightshelf, out var nightshelfName);

        Assert.True(first);
        Assert.True(second);
        Assert.NotEqual(auroraName, nightshelfName);
    }

    [Fact]
    public void SynthesizeNames_ProvidesReferenceNameForGeneratedActors()
    {
        var context = CreateContext("orca", "scout", "wake_singers", entityId: 10, nameSeed: 42);

        var result = IceRemembersEntityNameSynthesizer.Instance.TrySynthesizeNames(context, out var names);

        Assert.True(result);
        Assert.False(string.IsNullOrWhiteSpace(names.DisplayName));
        Assert.False(string.IsNullOrWhiteSpace(names.ReferenceName));
        Assert.NotEqual(names.DisplayName, names.ReferenceName);
        Assert.Contains(names.ReferenceName, names.DisplayName, StringComparison.Ordinal);
    }

    private static ProjectionEntityNameContext CreateContext(
        string kind,
        string subtype,
        string culture,
        int entityId,
        int nameSeed)
    {
        var kindId = kind.GetHashCode(StringComparison.Ordinal) & 0x7fffffff;
        var subtypeId = subtype.GetHashCode(StringComparison.Ordinal) & 0x7fffffff;
        var cultureId = culture.GetHashCode(StringComparison.Ordinal) & 0x7fffffff;
        return new ProjectionEntityNameContext(
            new ProjectionEntityNameKey(entityId, kindId, subtypeId, cultureId, nameSeed),
            new ProjectionSymbol(ProjectionSymbolKind.Kind, kindId, kind, ToDisplay(kind)),
            new ProjectionSymbol(ProjectionSymbolKind.Subtype, subtypeId, subtype, ToDisplay(subtype)),
            new ProjectionSymbol(ProjectionSymbolKind.Culture, cultureId, culture, ToDisplay(culture)));
    }

    private static string ToDisplay(string stableName) =>
        string.Join(' ', stableName.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
