namespace Engine.Domain.Tests.Domain;

using Engine.Domain;
using Engine.Domain.Compilation;
using IceRemembers;

public sealed class DomainIdRegistryTests
{
    [Fact]
    public void RegistersStableIdsFromOne()
    {
        var registry = new DomainIdRegistry();

        var health = registry.RegisterAttribute("health");
        var warmth = registry.RegisterAttribute("warmth");

        Assert.Equal(new AttributeRuntimeId(1), health);
        Assert.Equal(new AttributeRuntimeId(2), warmth);
        Assert.Equal(health, registry.AttributeIdOf("health"));
        Assert.Equal("warmth", registry.AttributeNameOf(warmth));
    }

    [Fact]
    public void KeepsCategoriesSeparate()
    {
        var registry = new DomainIdRegistry();

        var attribute = registry.RegisterAttribute("health");
        var pressure = registry.RegisterPressure("health");
        var source = registry.RegisterSourceCode("health");

        Assert.Equal(1, attribute.Value);
        Assert.Equal(1, pressure.Value);
        Assert.Equal(1, source.Value);
        Assert.Equal("health", registry.AttributeNameOf(attribute));
        Assert.Equal("health", registry.PressureNameOf(pressure));
        Assert.Equal("health", registry.SourceCodeNameOf(source));
    }

    [Fact]
    public void DuplicateRegistrationFails()
    {
        var registry = new DomainIdRegistry();
        registry.RegisterRelationshipKind("located_at");

        var error = Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterRelationshipKind("located_at"));
        Assert.Contains("Duplicate relationship kind", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownNameFails()
    {
        var registry = new DomainIdRegistry();

        var error = Assert.Throws<KeyNotFoundException>(() => registry.ResourceIdOf("food"));
        Assert.Contains("Unknown resource", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownIdFails()
    {
        var registry = new DomainIdRegistry();

        var error = Assert.Throws<KeyNotFoundException>(() =>
            registry.GeneratorNameOf(new GeneratorRuntimeId(1)));
        Assert.Contains("Unknown generator id", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BlankNameFails()
    {
        var registry = new DomainIdRegistry();

        var error = Assert.Throws<ArgumentException>(() => registry.RegisterCulture(" "));
        Assert.Contains("Stable names must be non-empty", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DomainCompiler_RegistersCanonicalDomainIdsAndSymbols()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var registry = compiled.Ids;

        Assert.True(registry.KindIdOf(IceRemembersKinds.Settlement).Value > 0);
        Assert.True(registry.AttributeIdOf("temp_hp").Value > 0);
        Assert.True(registry.RelationshipKindIdOf("member_of").Value > 0);
        Assert.True(registry.ResourceIdOf("food").Value > 0);
        Assert.True(registry.CultureIdOf(DomainRuntimeNames.NoneCultureId).Value > 0);
        Assert.True(registry.EventCodeIdOf(DomainRuntimeNames.RelationshipCreatedCode).Value > 0);
        Assert.True(registry.SourceCodeIdOf(DomainRuntimeNames.LowAltitudeVerbSourceCode).Value > 0);
        Assert.True(registry.SourceCodeIdOf(DomainRuntimeNames.UpkeepSourceCode).Value > 0);
        Assert.Contains(compiled.Symbols, symbol =>
            symbol.Kind == DomainSymbolKind.Kind &&
            symbol.Id == registry.KindIdOf(IceRemembersKinds.Settlement).Value &&
            symbol.StableName == IceRemembersKinds.Settlement.Value);
    }

    [Fact]
    public void Clone_PreservesIdsAndDoesNotShareMutation()
    {
        var registry = DomainCompiler.Compile(IceRemembersDefinition.Create()).Ids;
        var clone = registry.Clone();

        Assert.Equal(
            registry.KindIdOf(IceRemembersKinds.Settlement),
            clone.KindIdOf(IceRemembersKinds.Settlement));
        Assert.Equal(
            registry.AttributeIdOf("temp_hp"),
            clone.AttributeIdOf("temp_hp"));
        Assert.Equal(
            registry.SourceCodeIdOf(DomainRuntimeNames.UpkeepSourceCode),
            clone.SourceCodeIdOf(DomainRuntimeNames.UpkeepSourceCode));

        clone.RegisterKind("clone_only_kind");

        Assert.Throws<KeyNotFoundException>(() => registry.KindIdOf("clone_only_kind"));
    }
}
