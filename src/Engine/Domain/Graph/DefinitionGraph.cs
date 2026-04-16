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
    public ICollection<SemanticFlowEdge> Flows { get; } = [];
    public ICollection<SemanticModifierEdge> Modifiers { get; } = [];
    public ICollection<SemanticCouplingEdge> Couplings { get; } = [];
}

public enum SemanticFlowRole
{
    Inflow,
    Outflow,
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

public sealed class SemanticStockNode
{
    public required DefinitionAttributeKey AttributeKey { get; init; }
    public DefinitionConservationClass Conservation { get; init; } = DefinitionConservationClass.Unspecified;
}

public sealed class SemanticFlowEdge
{
    public required string Id { get; init; }
    public required SemanticFlowRole Role { get; init; }
    public required EntityKind OwnerKind { get; init; }
    public required SemanticStockNode TargetStock { get; init; }
    public SemanticFunctionalForm FunctionalForm { get; init; } = SemanticFunctionalForm.Unknown;
}

public sealed class SemanticModifierEdge
{
    public required string Id { get; init; }
    public required SemanticStockNode SourceStock { get; init; }
    public required SemanticFlowEdge TargetFlow { get; init; }
    public SemanticFunctionalForm FunctionalForm { get; init; } = SemanticFunctionalForm.Unknown;
}

public sealed class SemanticCouplingEdge
{
    public required string Id { get; init; }
    public required SemanticStockNode SourceStock { get; init; }
    public required SemanticStockNode TargetStock { get; init; }
}
