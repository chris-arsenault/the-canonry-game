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
    public required IReadOnlyList<CompiledChronicleQuery> ChronicleQueries { get; init; }
    public required IReadOnlyList<DomainCompileDiagnostic> Diagnostics { get; init; }
}

public enum DomainCompileDiagnosticSeverity
{
    Info,
    Warning,
}

public sealed record DomainCompileDiagnostic(
    DomainCompileDiagnosticSeverity Severity,
    string Code,
    string Message,
    string SourceKind,
    string Source,
    string ActorKind,
    string RelationshipKind,
    int RelationshipDirectionCode,
    int PolarityCode,
    float Threshold);

public sealed record CompiledChronicleQuery(
    string Id,
    CompiledChronicleProducer Parent,
    int TriggerRelationshipKindId,
    CompiledChronicleProducer Child);

public sealed record CompiledChronicleProducer(
    int ProducerKindCode,
    int ProducerId);

public sealed record DomainRuntimeCodes(
    EventCodeId RelationshipCreatedCodeId,
    EventCodeId RelationshipCulledCodeId,
    EventCodeId RelationshipStrengthChangedCodeId,
    SourceCodeId GambitSourceCodeId,
    SourceCodeId UpkeepSourceCodeId,
    SourceCodeId StartupSourceCodeId,
    SourceCodeId RelationshipSourceCodeId,
    SourceCodeId GeneratorSourceCodeId,
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
    Trait,
    Gambit,
    StructuralFact,
    RelationshipKind,
    Terrain,
    TravelStructure,
    Generator,
    Verb,
    EventCode,
    ReasonCode,
    SourceCode,
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
