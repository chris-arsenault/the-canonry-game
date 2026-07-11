namespace Engine.Domain.Graph;

using Engine.Behaviors;
using Engine.Constants;
using Engine.Domain.Config;
using Engine.Gambits;
using Engine.Gameplay;
using Engine.Kinds;
using Engine.Wiring;
using Engine.WorldGen;

public sealed class DefinitionGraph
{
    public required IReadOnlyDictionary<EntityKind, DefinitionKindNode> Kinds { get; init; }
    public required IReadOnlyDictionary<string, DefinitionCultureNode> Cultures { get; init; }
    public required IReadOnlyDictionary<string, DefinitionGambitNode> Gambits { get; init; }
    public required IReadOnlyList<DefinitionGameplayNode> GameplayNodes { get; init; }
    public required IReadOnlyDictionary<string, DefinitionGeneratorNode> Generators { get; init; }
    public required IReadOnlyDictionary<DefinitionRelationshipId, DefinitionRelationshipNode> Relationships { get; init; }
    public required IReadOnlyDictionary<EntityKind, DefinitionLifecycleNode> Lifecycles { get; init; }
    public required IReadOnlyDictionary<DefinitionAttributeKey, DefinitionAttributeNode> Attributes { get; init; }
    public SemanticFlowGraph SemanticFlows { get; } = new();
}

public abstract class DefinitionGameplayNode
{
    public required string Id { get; init; }
    public required IReadOnlyList<GameplayRead> Reads { get; init; }
    public required IReadOnlyList<GameplayWrite> Writes { get; init; }
}

public sealed class DefinitionLowAltitudeVerbNode : DefinitionGameplayNode
{
    public required LowAltitudeVerbConfig SourceConfig { get; init; }
}

public sealed class DefinitionMapSignalNode : DefinitionGameplayNode
{
    public required string Source { get; init; }
}

public sealed class DefinitionEffectNode : DefinitionGameplayNode
{
    public required AttributeDeltaEffectConfig SourceConfig { get; init; }
}

public abstract record GameplayRead(string Label);

public abstract record GameplayWrite(string Label);

public sealed record MapSignalRead(string Label) : GameplayRead(Label);

public sealed record InfluenceWrite(string Label) : GameplayWrite(Label);

public sealed record AttributeDeltaWrite(string Label) : GameplayWrite(Label);

public sealed record DefinitionAttributeKey(EntityKind Kind, DefinitionAttributeId AttributeId);

public sealed record DefinitionTraitKey(EntityKind Kind, DefinitionTraitId TraitId);

public abstract record SemanticStockId
{
    public abstract string Label { get; }

    public string DisplayLabel => Label;
}

public sealed record AttributeStockId(DefinitionAttributeKey Key) : SemanticStockId
{
    public override string Label => $"{Key.Kind}.{Key.AttributeId.Value}";
}

public sealed class DefinitionKindNode
{
    public required EntityKind Kind { get; init; }
    public required int RuntimeId { get; init; }
    public required IReadOnlySet<string> Subtypes { get; init; }
    public required IReadOnlyDictionary<DefinitionAttributeId, DefinitionAttributeNode> Attributes { get; init; }
    public required IReadOnlyDictionary<DefinitionTraitId, DefinitionTraitNode> Traits { get; init; }
    public DefinitionAttributeId? HealthAttribute { get; init; }
    public string? Description { get; init; }
    public required Engine.Domain.Config.PrimaryPlane PrimaryPlane { get; init; }
    public EntityTier Tier { get; init; } = EntityTier.None;
}

public sealed class DefinitionCultureNode
{
    public required string Id { get; init; }
    public required int RuntimeId { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyDictionary<string, DefinitionCultureSlotNode> Slots { get; init; }
}

public sealed class DefinitionCultureSlotNode
{
    public required string Id { get; init; }
    public required int SlotId { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required int PolarityCode { get; init; }
    public required IReadOnlySet<int> AllowedKindIds { get; init; }
    public required IReadOnlyList<DefinitionCultureSlotScoreRule> ScoreRules { get; init; }
    public required float MinimumScore { get; init; }
    public required int MinimumTenureTicks { get; init; }
    public required float DisplacementMargin { get; init; }
    public required float EntityInfluenceMultiplierDelta { get; init; }
    public required float CultureInfluenceMultiplierDelta { get; init; }
}

public sealed record DefinitionCultureSlotScoreRule(
    int SignalCode,
    int RelationshipKindId,
    float Weight);

public sealed class DefinitionAttributeNode
{
    public required DefinitionAttributeKey Key { get; init; }
    public required DefinitionAttributeShape Shape { get; init; }
    public required HealthConfig SourceConfig { get; init; }
    public bool IsHealthAttribute { get; init; }
}

public sealed class DefinitionTraitNode
{
    public required DefinitionTraitKey Key { get; init; }
    public required int RuntimeId { get; init; }
    public required TraitConfig SourceConfig { get; init; }
}

public sealed class DefinitionRelationshipNode
{
    public required DefinitionRelationshipId Id { get; init; }
    public required int RuntimeId { get; init; }
    public required RelationshipConfig SourceConfig { get; init; }
}

public sealed class DefinitionLifecycleNode
{
    public required EntityKind Kind { get; init; }
    public required DefinitionAttributeId HealthAttribute { get; init; }
    public required LifecycleConfig SourceConfig { get; init; }
}

public sealed class DefinitionGeneratorNode
{
    public required string Id { get; init; }
    public required int RuntimeId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required DefinitionGeneratorTarget Target { get; init; }
    public required IReadOnlyList<DefinitionGeneratorBindingTarget> SecondaryTargets { get; init; }
    public required IReadOnlyList<DefinitionProducedEntity> Produces { get; init; }
    public required IReadOnlyList<DefinitionGeneratorRelationship> Relationships { get; init; }
    public IReadOnlyList<IDefinitionGeneratorEffect> Effects { get; init; } = [];
    public required IReadOnlyList<DefinitionGeneratorTrigger> Triggers { get; init; }
    public int PulseWeight { get; init; }
    public int? MaxRunsPerSim { get; init; }
    public int CadenceTicks { get; init; }
    public int CadenceJitterTicks { get; init; }
    public required SemanticGeneratorConfig SourceConfig { get; init; }
}

public interface IDefinitionGeneratorEffect
{
    string Describe();
}

public sealed class DefinitionSetBindingHistoricalEffect(string bindingRef) : IDefinitionGeneratorEffect
{
    public string BindingRef { get; } = bindingRef;

    public string Describe() => $"set_binding_historical({BindingRef})";
}

public sealed class DefinitionRemoveTargetRelationshipsEffect(
    int relationshipKindId,
    GeneratorRelationshipInstanceSelection instanceSelection,
    bool includeReciprocal) : IDefinitionGeneratorEffect
{
    public int RelationshipKindId { get; } = relationshipKindId;

    public GeneratorRelationshipInstanceSelection InstanceSelection { get; } = instanceSelection;

    public bool IncludeReciprocal { get; } = includeReciprocal;

    public string Describe() =>
        $"remove_target_relationships(kind={RelationshipKindId}, instance={InstanceSelection}, reciprocal={IncludeReciprocal})";
}

public sealed class DefinitionAdjustTargetRelationshipStrengthEffect(
    int relationshipKindId,
    float delta,
    GeneratorRelationshipInstanceSelection instanceSelection,
    bool includeReciprocal) : IDefinitionGeneratorEffect
{
    public int RelationshipKindId { get; } = relationshipKindId;

    public float Delta { get; } = delta;

    public GeneratorRelationshipInstanceSelection InstanceSelection { get; } = instanceSelection;

    public bool IncludeReciprocal { get; } = includeReciprocal;

    public string Describe() =>
        $"adjust_target_relationship_strength(kind={RelationshipKindId}, delta={Delta}, instance={InstanceSelection}, reciprocal={IncludeReciprocal})";
}

public sealed class DefinitionSetTerrainPatchEffect(
    string centerRef,
    string terrain,
    int radius,
    int maxCellCount,
    bool respectTerrainBoundaries,
    float chance,
    int planeCode = PlaneCodes.Physical) : IDefinitionGeneratorEffect
{
    public string CenterRef { get; } = centerRef;

    public string Terrain { get; } = terrain;

    public int Radius { get; } = radius;

    public int MaxCellCount { get; } = maxCellCount;

    public bool RespectTerrainBoundaries { get; } = respectTerrainBoundaries;

    public float Chance { get; } = chance;

    public int PlaneCode { get; } = planeCode;

    public IReadOnlySet<string>? PreferredTerrains { get; init; }

    public IReadOnlySet<string>? AllowedTerrains { get; init; }

    public string Describe() =>
        $"set_terrain_patch(center={CenterRef}, terrain={Terrain}, radius={Radius}, max={MaxCellCount}, chance={Chance})";
}

public sealed class DefinitionGeneratorTarget
{
    public required EntityKind Kind { get; init; }
    public IReadOnlySet<string>? Subtypes { get; init; }
    public IReadOnlySet<string>? RequiredOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? RequiredIncomingRelationships { get; init; }
    public IReadOnlySet<string>? RequiredRelationships { get; init; }
    public IReadOnlyList<DefinitionTargetRelationshipRequirement> RequiredOutgoingRelationshipRequirements { get; init; } = [];
    public IReadOnlyList<DefinitionTargetRelationshipRequirement> RequiredIncomingRelationshipRequirements { get; init; } = [];
    public IReadOnlyList<DefinitionTargetRelationshipRequirement> RequiredRelationshipRequirements { get; init; } = [];
    public IReadOnlyList<DefinitionTargetTerrainCountRequirement> TerrainCountRequirements { get; init; } = [];
    public IReadOnlySet<string>? WithoutOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? WithoutIncomingRelationships { get; init; }
}

public sealed class DefinitionTargetRelationshipRequirement
{
    public required DefinitionRelationshipId RelationshipId { get; init; }
    public required int RelationshipRuntimeId { get; init; }
    public float MinimumStrength { get; init; }
}

public sealed class DefinitionTargetTerrainCountRequirement
{
    public required string TerrainName { get; init; }
    public int Radius { get; init; }
    public int MaximumExclusive { get; init; }
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
    public IReadOnlySet<string>? PhysicalPlacementPreferredTerrains { get; init; }
    public IReadOnlySet<string>? PhysicalPlacementAllowedTerrains { get; init; }
    public IReadOnlySet<string>? PhysicalPlacementAvoidedTerrains { get; init; }
    public string? PhysicalPlacementFallbackTerrain { get; init; }
    public SemanticPlacement SemanticPlacement { get; init; } = SemanticPlacement.Near;
    public IReadOnlySet<string>? SemanticPlacementPreferredTerrains { get; init; }
    public IReadOnlySet<string>? SemanticPlacementAllowedTerrains { get; init; }
    public IReadOnlySet<string>? SemanticPlacementAvoidedTerrains { get; init; }
    public string? SemanticPlacementFallbackTerrain { get; init; }
    public string PlacementRef { get; init; } = "$target";
    public string? InheritCultureFrom { get; init; }
    public string? Culture { get; init; }
    public GeneratorCulturePlacement CulturePlacement { get; init; } = GeneratorCulturePlacement.PreferInheritedCulture;
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
    public IReadOnlySet<string>? PreferredTerrains { get; init; }
    public IReadOnlySet<string>? AllowedTerrains { get; init; }
    public bool RespectTerrainBoundaries { get; init; } = true;
    public string? TransformTerrain { get; init; }
}

public sealed class DefinitionEntityQuery
{
    public IReadOnlySet<string>? Subtypes { get; init; }
    public bool AdjacentToTarget { get; init; }
    public bool SamePhysicalCultureComponentAsTarget { get; init; }
    public IReadOnlySet<string>? WithoutOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? WithoutIncomingRelationships { get; init; }
    public QueryTargetCultureMatch TargetCultureMatch { get; init; } = QueryTargetCultureMatch.Any;
}

public sealed class DefinitionGeneratorBindingTarget
{
    public required string Ref { get; init; }
    public required EntityKind Kind { get; init; }
    public DefinitionEntityQuery? Query { get; init; }
}

public sealed class DefinitionGeneratorRelationship
{
    public required string SourceRef { get; init; }
    public required string DestinationRef { get; init; }
    public required DefinitionRelationshipId RelationshipId { get; init; }
    public required int RelationshipRuntimeId { get; init; }
    public float Strength { get; init; } = 1f;
    public bool ReplaceExistingOutgoingOfSameKind { get; init; }
}

public sealed class DefinitionGeneratorTrigger
{
    public required EntityKind OnKind { get; init; }
}

public sealed class DefinitionGambitNode
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required EntityKind TargetKind { get; init; }
    public required GambitDefinition SourceDefinition { get; init; }
    public required IReadOnlySet<TypedDefinitionRef> Reads { get; init; }
    public required IReadOnlySet<TypedDefinitionRef> Writes { get; init; }
    public required IReadOnlySet<TypedDefinitionRef> ProviderSelectors { get; init; }
}

public abstract record DefinitionValueId
{
    public abstract string Label { get; }

    public string DisplayLabel => Label;
}

public sealed record AttributeValueId(DefinitionAttributeId AttributeId) : DefinitionValueId
{
    public override string Label => AttributeId.Value;
}

public sealed record StructuralValueId(string Name) : DefinitionValueId
{
    public override string Label => Name;
}

public sealed record MapCellValueId(string Name) : DefinitionValueId
{
    public override string Label => Name;
}

public sealed record RelationalValueId(string Name) : DefinitionValueId
{
    public override string Label => Name;
}

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
    /// (Penguin, Artifact) move on the map hex grid. Abstract entities (Settlement,
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
