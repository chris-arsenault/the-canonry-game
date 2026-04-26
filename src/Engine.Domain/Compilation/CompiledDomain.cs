namespace Engine.Domain.Compilation;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Compilation.KernelPrograms;

public sealed class CompiledDomain
{
    public required DomainConfig Config { get; init; }
    public required DomainIdRegistry Ids { get; init; }
    public required DefinitionGraph DefinitionGraph { get; init; }
    public required DomainRuntimeCodes RuntimeCodes { get; init; }
    public required KernelProgramSet Programs { get; init; }
    public required IReadOnlyList<DomainSymbolSeed> Symbols { get; init; }
}

public sealed record DomainRuntimeCodes(
    EventCodeId RelationshipCreatedCodeId,
    EventCodeId RelationshipCulledCodeId,
    EventCodeId RelationshipStrengthChangedCodeId,
    SourceCodeId ProcessSourceCodeId,
    SourceCodeId CardSourceCodeId,
    SourceCodeId UpkeepSourceCodeId);

public enum DomainSymbolKind
{
    Kind,
    Subtype,
    Culture,
    Attribute,
    Process,
    StructuralFact,
    RelationshipKind,
    RelationshipFact,
    Resource,
    Terrain,
    Pressure,
    Generator,
    Decision,
    Card,
    EventCode,
    ReasonCode,
    SourceCode,
    Detector,
    Goal,
}

public sealed record DomainSymbolSeed(
    DomainSymbolKind Kind,
    int Id,
    string StableName,
    string? DisplayName = null,
    string? Description = null,
    string? IconKey = null,
    string? ColorKey = null);
