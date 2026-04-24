namespace Engine.Domain.Graph;

using Engine.Domain.Config;
using Engine.Kinds;
using Engine.Map;
using Engine.Processes;
using Engine.Wiring;

public sealed class DefinitionGraph
{
    public IReadOnlyList<EntityKind> RingOrder { get; init; } = [];
    public required IReadOnlyDictionary<EntityKind, DefinitionKindNode> Kinds { get; init; }
    public required IReadOnlyDictionary<string, DefinitionProcessNode> Processes { get; init; }
    public required IReadOnlyDictionary<string, DefinitionGeneratorNode> Generators { get; init; }
    public required IReadOnlyDictionary<DefinitionResourceId, DefinitionResourceNode> Resources { get; init; }
    public required IReadOnlyDictionary<DefinitionRelationshipId, DefinitionRelationshipNode> Relationships { get; init; }
    public required IReadOnlyDictionary<DefinitionRelationshipFactId, DefinitionRelationshipFactNode> RelationshipFacts { get; init; }
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
    public required IReadOnlySet<string> Subtypes { get; init; }
    public required IReadOnlyDictionary<DefinitionAttributeId, DefinitionAttributeNode> Attributes { get; init; }
    public DefinitionAttributeId? HealthAttribute { get; init; }
    public required IReadOnlyList<DefinitionStandingDriveNode> StandingDrives { get; init; }
    public string? Description { get; init; }
    public required Engine.Domain.Config.PrimaryPlane PrimaryPlane { get; init; }
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

public sealed class DefinitionRelationshipFactNode
{
    public required DefinitionRelationshipFactId Id { get; init; }
    public required RelationshipFactConfig SourceConfig { get; init; }
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

public sealed class DefinitionGeneratorNode
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required DefinitionGeneratorTarget Target { get; init; }
    public DefinitionTargetSaturation? TargetSaturation { get; init; }
    public required IReadOnlyList<DefinitionGeneratorBindingTarget> SecondaryTargets { get; init; }
    public required IReadOnlyList<DefinitionProducedEntity> Produces { get; init; }
    public required IReadOnlyList<DefinitionGeneratorRelationship> Relationships { get; init; }
    public required IReadOnlyList<DefinitionGeneratorTrigger> Triggers { get; init; }
    public int PulseWeight { get; init; }
    public int? MaxRunsPerSim { get; init; }
    public required SemanticGeneratorConfig SourceConfig { get; init; }
}

public sealed class DefinitionGeneratorTarget
{
    public required EntityKind Kind { get; init; }
    public IReadOnlySet<string>? Subtypes { get; init; }
    public bool AllowWilderness { get; init; }
}

public sealed class DefinitionTargetSaturation
{
    public required DefinitionRelationshipId RelationshipId { get; init; }
    public DefinitionRelationshipFactId? RequiredFactId { get; init; }
    public TargetSaturationDirection Direction { get; init; } = TargetSaturationDirection.Symmetric;
    public int MaxExisting { get; init; }
}

public sealed class DefinitionProducedEntity
{
    public required string Ref { get; init; }
    public required EntityKind Kind { get; init; }
    public string? Subtype { get; init; }
    public IReadOnlySet<string>? Subtypes { get; init; }
    public required string Role { get; init; }
    public float CreateChance { get; init; } = 1f;
    public bool CreateIfMissing { get; init; } = true;
    public SemanticPlacement PhysicalPlacement { get; init; } = SemanticPlacement.Near;
    public SemanticPlacement SemanticPlacement { get; init; } = SemanticPlacement.Near;
    public string PlacementRef { get; init; } = "$target";
    public required string InheritCultureFrom { get; init; }
    public DefinitionEntityQuery? Query { get; init; }
    public DefinitionRegionCoverage? Coverage { get; init; }
}

public sealed class DefinitionRegionCoverage
{
    public required CoveragePlacementMode Mode { get; init; }
    public string? SourceRef { get; init; }
    public int DesiredCellCount { get; init; } = 18;
    public int MinimumCellCount { get; init; } = 8;
    public int MinimumSourceRemainingCellCount { get; init; } = 8;
    public IReadOnlySet<TerrainType>? PreferredTerrains { get; init; }
    public IReadOnlySet<TerrainType>? AllowedTerrains { get; init; }
    public bool RespectTerrainBoundaries { get; init; } = true;
    public TerrainType? TransformTerrain { get; init; }
}

public sealed class DefinitionEntityQuery
{
    public IReadOnlySet<string>? Subtypes { get; init; }
    public bool AdjacentToTarget { get; init; }
    public IReadOnlySet<string>? WithoutOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? WithoutIncomingRelationships { get; init; }
    public QueryTargetCultureMatch TargetCultureMatch { get; init; } = QueryTargetCultureMatch.Any;
    public IReadOnlyList<DefinitionQueryAttributeCondition> AttributeConditions { get; init; } = [];
}

public sealed class DefinitionGeneratorBindingTarget
{
    public required string Ref { get; init; }
    public required EntityKind Kind { get; init; }
    public DefinitionEntityQuery? Query { get; init; }
}

public sealed class DefinitionQueryAttributeCondition
{
    public required DefinitionAttributeId AttributeId { get; init; }
    public TriggerDirection Direction { get; init; } = TriggerDirection.Any;
}

public sealed class DefinitionGeneratorRelationship
{
    public required string SourceRef { get; init; }
    public required string DestinationRef { get; init; }
    public required DefinitionRelationshipId RelationshipId { get; init; }
    public required IReadOnlySet<DefinitionRelationshipFactId> FactIds { get; init; }
    public float Strength { get; init; } = 1f;
    public bool ReplaceExistingOutgoingOfSameKind { get; init; }
}

public sealed class DefinitionGeneratorTrigger
{
    public required EntityKind OnKind { get; init; }
    public DefinitionAttributeId? AttributeId { get; init; }
    public TriggerDirection Direction { get; init; } = TriggerDirection.Any;
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

public enum SemanticVerbRole
{
    /// <summary>Default: authored cross-stock action (conversion, self-sac, hex-exchange, etc.).</summary>
    Action,

    /// <summary>
    /// Enablement verb: gets the entity into position to act. Physical entities
    /// (NPC, Artifact) move on the map hex grid. Abstract entities (Settlement,
    /// Faction, Region) move on the semantic-coord plane toward targets.
    /// Exactly one per entity kind.
    /// </summary>
    Mobility,

    /// <summary>
    /// Downtime verb: a random one-step movement on the entity's assigned grid
    /// when no other action is chosen. Exactly one per entity kind.
    /// </summary>
    Drift,
}

public enum SemanticModifierDirection
{
    Unknown,
    Positive,
    Negative,
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
    public SemanticVerbRole Role { get; init; } = SemanticVerbRole.Action;
    public string? Notes { get; init; }
    public bool IsFlowless { get; init; }
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
    public string? Reasoning { get; init; }
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

public sealed class SemanticImportDiagnostic
{
    public required SemanticDiagnosticSeverity Severity { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? SourceWorksheet { get; init; }
    public int? SourceRow { get; init; }
}
