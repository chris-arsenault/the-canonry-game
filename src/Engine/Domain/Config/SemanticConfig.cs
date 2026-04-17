namespace Engine.Domain.Config;

using Engine.Domain.Graph;
using Engine.Kinds;
using Engine.Wiring;

public sealed class SemanticDomainConfig
{
    public IReadOnlyList<SemanticStockConfig> Stocks { get; init; } = [];
    public IReadOnlyList<SemanticVerbConfig> Verbs { get; init; } = [];
    public IReadOnlyList<SemanticFlowConfig> Flows { get; init; } = [];
    public IReadOnlyList<SemanticModifierConfig> Modifiers { get; init; } = [];
    public IReadOnlyList<SemanticProcessReferenceConfig> ProcessReferences { get; init; } = [];
}

public abstract class SemanticStockConfig
{
    public SemanticStockClass StockClass { get; init; } = SemanticStockClass.Unknown;
    public SemanticFeedbackClass FeedbackClass { get; init; } = SemanticFeedbackClass.Unknown;
    public SemanticFlowCoverageRequirement RequiredFlowCoverage { get; init; } =
        SemanticFlowCoverageRequirement.Both;
    public SemanticFlowCoverageRequirement RequiredXFlowCoverage { get; init; } =
        SemanticFlowCoverageRequirement.None;
    public SemanticFlowCoverageRequirement RequiredYFlowCoverage { get; init; } =
        SemanticFlowCoverageRequirement.None;
    public string? BoundsLabel { get; init; }
    public string? ConservationLabel { get; init; }
    public string? CoverageNotes { get; init; }
    public string? Notes { get; init; }
}

public sealed class SemanticAttributeStockConfig : SemanticStockConfig
{
    public required EntityKind Kind { get; init; }
    public required string AttributeName { get; init; }
}

public sealed class SemanticResourceStockConfig : SemanticStockConfig
{
    public required string ResourceName { get; init; }
}

public sealed class SemanticVerbConfig
{
    public required string Id { get; init; }
    public required SemanticOwnerType OwnerType { get; init; }
    public EntityKind? OwnerKind { get; init; }
    public string? ImplementedProcessId { get; init; }
    public string? Notes { get; init; }
}

public sealed class SemanticFlowConfig
{
    public required string VerbId { get; init; }
    public required SemanticFlowRole Role { get; init; }
    public required EntityKind TargetKind { get; init; }
    public required string TargetAttribute { get; init; }
    public SemanticPlaneAxis PlaneAxis { get; init; } = SemanticPlaneAxis.None;
    public SemanticMechanism Mechanism { get; init; } = SemanticMechanism.Unknown;
    public SemanticFunctionalForm FunctionalForm { get; init; } = SemanticFunctionalForm.Unknown;
    public string? SignatureLabel { get; init; }
    public string? FormLabel { get; init; }
    public IReadOnlyList<AttributeRef> SourceRefs { get; init; } = [];
}

public sealed class SemanticModifierConfig
{
    public required EntityKind SourceKind { get; init; }
    public required string SourceAttribute { get; init; }
    public required string TargetVerbId { get; init; }
    public required SemanticModifierDirection Direction { get; init; }
    public SemanticFunctionalForm FunctionalForm { get; init; } = SemanticFunctionalForm.Unknown;
    public string? FormLabel { get; init; }
}

public sealed class SemanticProcessReferenceConfig
{
    public required string ProcessId { get; init; }
    public required SemanticProcessReferenceKind Kind { get; init; }
    public required EntityKind TargetKind { get; init; }
    public required string TargetAttribute { get; init; }
}
