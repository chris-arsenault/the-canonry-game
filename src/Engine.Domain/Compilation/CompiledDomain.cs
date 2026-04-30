namespace Engine.Domain.Compilation;

using Engine.Domain.Compilation.KernelData;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;
using Engine.Domain.Graph;

public sealed class CompiledDomain
{
    public required DomainConfig Config { get; init; }
    public required DomainIdRegistry Ids { get; init; }
    public required DefinitionGraph DefinitionGraph { get; init; }
    public required DomainRuntimeCodes RuntimeCodes { get; init; }
    public required CompiledKernelDataSet KernelData { get; init; }
    public required KernelProgramSet Programs { get; init; }
    public required IReadOnlyList<DomainSymbolSeed> Symbols { get; init; }
}

public sealed record DomainRuntimeCodes(
    EventCodeId RelationshipCreatedCodeId,
    EventCodeId RelationshipCulledCodeId,
    EventCodeId RelationshipStrengthChangedCodeId,
    SourceCodeId ProcessSourceCodeId,
    SourceCodeId GambitSourceCodeId,
    SourceCodeId UpkeepSourceCodeId,
    SourceCodeId StartupSourceCodeId,
    SourceCodeId RelationshipSourceCodeId,
    SourceCodeId GeneratorSourceCodeId,
    SourceCodeId PressureSourceCodeId,
    SourceCodeId DegeneracySourceCodeId,
    SourceCodeId PlanningSourceCodeId,
    SourceCodeId MovementSourceCodeId,
    SourceCodeId MapActivitySourceCodeId,
    SourceCodeId MapSignalsSourceCodeId,
    SourceCodeId LowAltitudeVerbSourceCodeId,
    SourceCodeId EffectsSourceCodeId);

public enum DomainSymbolKind
{
    Kind,
    Subtype,
    Culture,
    Attribute,
    Process,
    Gambit,
    StructuralFact,
    RelationshipKind,
    RelationshipFact,
    Resource,
    Terrain,
    Pressure,
    Generator,
    Verb,
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
