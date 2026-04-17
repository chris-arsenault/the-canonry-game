namespace Engine.Domain.Graph;

using Engine.Domain.Config;
using Engine.Kinds;
using Engine.Processes;
using Engine.Wiring;

public sealed class DefinitionGraph
{
    public required IReadOnlyDictionary<EntityKind, DefinitionKindNode> Kinds { get; init; }
    public required IReadOnlyDictionary<string, DefinitionProcessNode> Processes { get; init; }
    public required IReadOnlyDictionary<DefinitionResourceId, DefinitionResourceNode> Resources { get; init; }
    public required IReadOnlyDictionary<DefinitionRelationshipId, DefinitionRelationshipNode> Relationships { get; init; }
    public required IReadOnlyDictionary<DefinitionPressureId, DefinitionPressureNode> Pressures { get; init; }
    public required IReadOnlyDictionary<EntityKind, DefinitionLifecycleNode> Lifecycles { get; init; }
    public required IReadOnlyDictionary<DefinitionAttributeKey, DefinitionAttributeNode> Attributes { get; init; }
    public SemanticFlowGraph SemanticFlows { get; } = new();
}

public sealed record DefinitionAttributeKey(EntityKind Kind, DefinitionAttributeId AttributeId);

public abstract record SemanticStockId
{
    public abstract string Label { get; }
}

public sealed record AttributeStockId(DefinitionAttributeKey Key) : SemanticStockId
{
    public override string Label => $"{Key.Kind}.{Key.AttributeId.Value}";
}

public sealed record ResourceStockId(DefinitionResourceId ResourceId) : SemanticStockId
{
    public override string Label => $"hex.{ResourceId.Value}";
}

public sealed class DefinitionKindNode
{
    public required EntityKind Kind { get; init; }
    public required IReadOnlyDictionary<DefinitionAttributeId, DefinitionAttributeNode> Attributes { get; init; }
    public DefinitionAttributeId? HealthAttribute { get; init; }
    public required IReadOnlyList<DefinitionStandingDriveNode> StandingDrives { get; init; }
}

public sealed class DefinitionAttributeNode
{
    public required DefinitionAttributeKey Key { get; init; }
    public required DefinitionAttributeShape Shape { get; init; }
    public required AttributeConfig SourceConfig { get; init; }
    public bool IsHealthAttribute { get; init; }
    public DefinitionCriticalEffectNode? CriticalEffect { get; init; }
    public DefinitionLocationResourceDriftNode? LocationResourceDrift { get; init; }
}

public sealed class DefinitionCriticalEffectNode
{
    public required DefinitionAttributeId TargetAttributeId { get; init; }
    public required float DeltaPerTick { get; init; }
    public string? Description { get; init; }
}

public sealed class DefinitionLocationResourceDriftNode
{
    public required DefinitionResourceId ResourceId { get; init; }
    public required float PerUnit { get; init; }
    public required float MinOffset { get; init; }
    public required float MaxOffset { get; init; }
}

public sealed class DefinitionStandingDriveNode
{
    public required TypedDefinitionRef DesiredEffect { get; init; }
    public required float BaseUrgency { get; init; }
}

public sealed class DefinitionResourceNode
{
    public required DefinitionResourceId Id { get; init; }
    public required ResourceConfig SourceConfig { get; init; }
}

public sealed class DefinitionRelationshipNode
{
    public required DefinitionRelationshipId Id { get; init; }
    public required RelationshipConfig SourceConfig { get; init; }
}

public sealed class DefinitionPressureNode
{
    public required DefinitionPressureId Id { get; init; }
    public required PressureConfig SourceConfig { get; init; }
}

public sealed class DefinitionLifecycleNode
{
    public required EntityKind Kind { get; init; }
    public required DefinitionAttributeId HealthAttribute { get; init; }
    public required LifecycleConfig SourceConfig { get; init; }
}

public sealed class DefinitionSupportRuleNode
{
    public required TypedDefinitionRef DesiredEffect { get; init; }
    public required float DesiredMinimum { get; init; }
    public required float UrgencyScale { get; init; }
    public required PlannerSupportRule SourceRule { get; init; }
}

public sealed class DefinitionProcessNode
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required DefinitionRole Role { get; init; }
    public required EntityKind TargetKind { get; init; }
    public required ProcessDefinition SourceDefinition { get; init; }
    public required IReadOnlySet<TypedDefinitionRef> Reads { get; init; }
    public required IReadOnlySet<TypedDefinitionRef> Writes { get; init; }
    public required IReadOnlySet<TypedDefinitionRef> GoalEffects { get; init; }
    public required IReadOnlySet<TypedDefinitionRef> ProviderSelectors { get; init; }
    public required IReadOnlyList<DefinitionSupportRuleNode> SupportRules { get; init; }
}

public abstract record DefinitionValueId;

public sealed record AttributeValueId(DefinitionAttributeId AttributeId) : DefinitionValueId;
public sealed record ResourceValueId(DefinitionResourceId ResourceId) : DefinitionValueId;
public sealed record PressureValueId(DefinitionPressureId PressureId) : DefinitionValueId;
public sealed record StructuralValueId(string Name) : DefinitionValueId;
public sealed record MapCellValueId(string Name) : DefinitionValueId;
public sealed record RelationalValueId(string Name) : DefinitionValueId;

public sealed record TypedDefinitionRef(
    RefScope Scope,
    DefinitionValueId ValueId,
    EntityKind? Kind = null,
    DefinitionRelationshipId? RelationshipId = null);

public sealed class SemanticFlowGraph
{
    public ICollection<SemanticStockNode> Stocks { get; } = [];
    public ICollection<SemanticVerbNode> Verbs { get; } = [];
    public ICollection<SemanticFlowEdge> Flows { get; } = [];
    public ICollection<SemanticModifierEdge> Modifiers { get; } = [];
    public ICollection<SemanticCouplingEdge> Couplings { get; } = [];
    public ICollection<SemanticProcessReference> ProcessReferences { get; } = [];
    public ICollection<SemanticImportDiagnostic> Diagnostics { get; } = [];
}

public enum SemanticFlowRole
{
    Inflow,
    Outflow,
}

[Flags]
public enum SemanticFlowCoverageRequirement
{
    None = 0,
    Inflow = 1,
    Outflow = 2,
    Both = Inflow | Outflow,
}

public enum SemanticPlaneAxis
{
    None,
    X,
    Y,
}

public enum SemanticFunctionalForm
{
    Unknown,
    Linear,
    Product,
    Threshold,
    Logistic,
    Sigmoid,
    Event,
}

public enum SemanticStockClass
{
    Unknown,
    Anchor,
    Free,
    Plane,
}

public enum SemanticFeedbackClass
{
    Unknown,
    Anchor,
    Balancing,
}

public enum SemanticMechanism
{
    Unknown,
    Inflow,
    Conversion,
    Gate,
    Decay,
    Emanation,
    Sacrifice,
    Shift,
}

public enum SemanticOwnerType
{
    Unknown,
    Entity,
    Engine,
}

public enum SemanticModifierDirection
{
    Unknown,
    Positive,
    Negative,
}

public enum SemanticProcessReferenceKind
{
    Reads,
    DirectWrites,
    IndirectWrites,
}

public enum SemanticDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

public sealed class SemanticStockNode
{
    public required SemanticStockId Id { get; init; }
    public SemanticStockClass StockClass { get; init; } = SemanticStockClass.Unknown;
    public SemanticFeedbackClass FeedbackClass { get; init; } = SemanticFeedbackClass.Unknown;
    public SemanticFlowCoverageRequirement RequiredFlowCoverage { get; init; } =
        SemanticFlowCoverageRequirement.Both;
    public SemanticFlowCoverageRequirement RequiredXFlowCoverage { get; init; } =
        SemanticFlowCoverageRequirement.None;
    public SemanticFlowCoverageRequirement RequiredYFlowCoverage { get; init; } =
        SemanticFlowCoverageRequirement.None;
    public string? RawBounds { get; init; }
    public string? RawConservation { get; init; }
    public string? CoverageNotes { get; init; }
    public string? Notes { get; init; }
    public string? SourceWorksheet { get; init; }
    public int SourceRow { get; init; }

    public DefinitionAttributeKey? AttributeKey => (Id as AttributeStockId)?.Key;
    public DefinitionResourceId? ResourceId => (Id as ResourceStockId)?.ResourceId;
}

public sealed class SemanticVerbNode
{
    public required string Id { get; init; }
    public SemanticOwnerType OwnerType { get; init; } = SemanticOwnerType.Unknown;
    public EntityKind? OwnerKind { get; init; }
    public DefinitionProcessNode? ImplementedProcess { get; init; }
    public string? Notes { get; init; }
    public bool IsFlowless { get; init; }
    public bool IsImplementedProcess => ImplementedProcess is not null;
}

public sealed class SemanticFlowEdge
{
    public required string Id { get; init; }
    public required SemanticVerbNode Verb { get; init; }
    public required SemanticFlowRole Role { get; init; }
    public required SemanticStockNode TargetStock { get; init; }
    public SemanticPlaneAxis PlaneAxis { get; init; } = SemanticPlaneAxis.None;
    public string? RawSignature { get; init; }
    public string? RawForm { get; init; }
    public string? SourceWorksheet { get; init; }
    public int SourceRow { get; init; }
    public SemanticMechanism Mechanism { get; init; } = SemanticMechanism.Unknown;
    public SemanticFunctionalForm FunctionalForm { get; init; } = SemanticFunctionalForm.Unknown;
    public IReadOnlyList<TypedDefinitionRef> SourceRefs { get; init; } = [];

    public bool IsGate => Mechanism == SemanticMechanism.Gate;
}

public sealed class SemanticModifierEdge
{
    public required string Id { get; init; }
    public required SemanticStockNode SourceStock { get; init; }
    public required string TargetVerbId { get; init; }
    public SemanticVerbNode? TargetVerb { get; init; }
    public string? RawForm { get; init; }
    public string? SourceWorksheet { get; init; }
    public int SourceRow { get; init; }
    public SemanticModifierDirection Direction { get; init; } = SemanticModifierDirection.Unknown;
    public SemanticFunctionalForm FunctionalForm { get; init; } = SemanticFunctionalForm.Unknown;
}

public sealed class SemanticCouplingEdge
{
    public required string Id { get; init; }
    public required SemanticStockNode SourceStock { get; init; }
    public required SemanticStockNode TargetStock { get; init; }
    public string? RawDescription { get; init; }
    public string? SourceWorksheet { get; init; }
    public int SourceRow { get; init; }
}

public sealed class SemanticProcessReference
{
    public required string ProcessId { get; init; }
    public required SemanticProcessReferenceKind Kind { get; init; }
    public required SemanticStockNode TargetStock { get; init; }
    public DefinitionProcessNode? ResolvedProcess { get; init; }
    public bool MatchesDefinitionGraph { get; init; }
    public string? SourceWorksheet { get; init; }
    public int SourceRow { get; init; }
}

public sealed class SemanticImportDiagnostic
{
    public required SemanticDiagnosticSeverity Severity { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? SourceWorksheet { get; init; }
    public int? SourceRow { get; init; }
}
