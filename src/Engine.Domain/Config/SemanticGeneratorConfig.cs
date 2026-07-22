namespace Engine.Domain.Config;

using Engine.Constants;
using Engine.Domain.Spatial;
using Engine.Kinds;

public sealed class SemanticGeneratorConfig
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? CausalLimit { get; init; }
    public required SemanticGeneratorTarget Target { get; init; }
    public IReadOnlyList<SemanticGeneratorBindingTarget> SecondaryTargets { get; init; } = [];
    public required IReadOnlyList<SemanticProducedEntity> Produces { get; init; }
    public IReadOnlyList<SemanticGeneratorRelationship> Relationships { get; init; } = [];
    public IReadOnlyList<ISemanticGeneratorEffect> Effects { get; init; } = [];
    public required IReadOnlyList<SemanticGeneratorTrigger> Triggers { get; init; }
    public int PulseWeight { get; init; } = 1;
    public int? MaxRunsPerSim { get; init; }

    /// <summary>
    /// When positive, this is a timeline generator: it fires on its own
    /// cadence (roughly every CadenceTicks, offset by CadenceJitterTicks)
    /// outside the shared population-proportional pulse pool. Target
    /// eligibility still applies; a cadence firing with no eligible target
    /// passes quietly.
    /// </summary>
    public int CadenceTicks { get; init; }

    /// <summary>Uniform jitter applied to each cadence interval.</summary>
    public int CadenceJitterTicks { get; init; }
}

public sealed class SemanticGeneratorTarget
{
    public required EntityKind Kind { get; init; }
    public IReadOnlySet<string>? Subtypes { get; init; }
    public IReadOnlySet<string>? RequiredTags { get; init; }
    public IReadOnlySet<string>? WithoutTags { get; init; }
    public IReadOnlySet<string>? RequiredOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? RequiredIncomingRelationships { get; init; }
    public IReadOnlySet<string>? RequiredRelationships { get; init; }
    public IReadOnlyList<SemanticGeneratorRelationshipRequirement> RequiredOutgoingRelationshipRequirements { get; init; } = [];
    public IReadOnlyList<SemanticGeneratorRelationshipRequirement> RequiredIncomingRelationshipRequirements { get; init; } = [];
    public IReadOnlyList<SemanticGeneratorRelationshipRequirement> RequiredRelationshipRequirements { get; init; } = [];
    public IReadOnlyList<SemanticGeneratorTerrainCountRequirement> TerrainCountRequirements { get; init; } = [];
    public IReadOnlySet<string>? WithoutOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? WithoutIncomingRelationships { get; init; }

    /// <summary>
    /// Remembrance gate: only target entities currently cast in a running
    /// chronicle at or above this heat. 0 disables the gate.
    /// </summary>
    public float ChronicleHeatMin { get; init; }

    /// <summary>
    /// Remembrance gate: only target entities cast in a chronicle that
    /// resolved at legendary heat within this many ticks. 0 disables.
    /// </summary>
    public int ChronicleLegendaryWithinTicks { get; init; }
}

public sealed class SemanticGeneratorRelationshipRequirement
{
    public required string RelationshipKind { get; init; }
    public float MinimumStrength { get; init; }
}

public sealed class SemanticGeneratorTerrainCountRequirement
{
    public required string TerrainName { get; init; }
    public int Radius { get; init; }
    public int MaximumExclusive { get; init; }
}

public sealed class SemanticProducedEntity
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

    /// <summary>
    /// Geography site flags (summit, thermal, anomaly, polynya, crater) that
    /// boost physical placement score where the hex carries them.
    /// </summary>
    public IReadOnlySet<string>? PhysicalPlacementPreferredSites { get; init; }

    /// <summary>
    /// Signed band affinities (-1..1): placement score rises with the hex's
    /// band times the affinity, so content follows the geography that suits
    /// it without per-value rules.
    /// </summary>
    public float PhysicalPlacementWarmthAffinity { get; init; }
    public float PhysicalPlacementFireAffinity { get; init; }
    public float PhysicalPlacementAuroraAffinity { get; init; }
    public float PhysicalPlacementPressureAffinity { get; init; }
    public SemanticPlacement SemanticPlacement { get; init; } = SemanticPlacement.Near;
    public IReadOnlySet<string>? SemanticPlacementPreferredTerrains { get; init; }
    public IReadOnlySet<string>? SemanticPlacementAllowedTerrains { get; init; }
    public IReadOnlySet<string>? SemanticPlacementAvoidedTerrains { get; init; }
    public string? SemanticPlacementFallbackTerrain { get; init; }
    public string PlacementRef { get; init; } = "$target";
    /// <summary>
    /// Ref of the bound entity whose culture the produced entity inherits.
    /// Exactly one of this or <see cref="Culture"/> must be set.
    /// </summary>
    public string? InheritCultureFrom { get; init; }

    /// <summary>
    /// Authored culture for the produced entity, independent of the target's
    /// culture. Enables cross-cultural generators where one culture's activity
    /// creates another culture's entities.
    /// </summary>
    public string? Culture { get; init; }
    public GeneratorCulturePlacement CulturePlacement { get; init; } = GeneratorCulturePlacement.PreferInheritedCulture;

    /// <summary>
    /// For FeatureAnchor placement: which geological feature kind to claim
    /// (river_system, lake, range, peak, waterfall, crater, plain); null
    /// means any.
    /// </summary>
    public string? FeatureKind { get; init; }
    public SemanticEntityQuery? Query { get; init; }
    public SemanticRegionCoverage? Coverage { get; init; }
}

public sealed class SemanticRegionCoverage
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

public sealed class SemanticEntityQuery
{
    public IReadOnlySet<string>? Subtypes { get; init; }
    public IReadOnlySet<string>? RequiredTags { get; init; }
    public IReadOnlySet<string>? WithoutTags { get; init; }

    /// <summary>Restrict matches to an authored culture (stable name).</summary>
    public string? Culture { get; init; }

    public bool AdjacentToTarget { get; init; }
    public bool SamePhysicalCultureComponentAsTarget { get; init; }
    public IReadOnlySet<string>? WithoutOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? WithoutIncomingRelationships { get; init; }
    public QueryTargetCultureMatch TargetCultureMatch { get; init; } = QueryTargetCultureMatch.Any;

    /// <summary>
    /// When set, the candidate must have a live relationship of this kind
    /// with the generator's selected target (candidate→target by default,
    /// target→candidate when <see cref="RelatedToTargetIncoming"/> is set) at
    /// or above <see cref="RelatedToTargetMinStrength"/>. This is the one-hop
    /// causal filter that lets bundles bind entities already storied with the
    /// target: an ally who answers a call, a member who falls in the war.
    /// </summary>
    public string? RelatedToTargetRelationship { get; init; }

    public bool RelatedToTargetIncoming { get; init; }

    public float RelatedToTargetMinStrength { get; init; } = 0.1f;
}

public sealed class SemanticGeneratorBindingTarget
{
    public required string Ref { get; init; }
    public required EntityKind Kind { get; init; }
    public SemanticEntityQuery? Query { get; init; }
}

public enum QueryTargetCultureMatch
{
    Any,
    SameAsTarget,
    DifferentFromTarget,
}

public enum GeneratorCulturePlacement
{
    Ignore,
    PreferInheritedCulture,
    HostileIntrusion,
    CultureInterior,
    CultureFrontier,
    CultureRoute,
    Wilderness,
}

public interface ISemanticGeneratorEffect
{
    string Describe();
}

/// <summary>Sets a resolved secondary binding entity to historical status after the generator fires.</summary>
public sealed class SetBindingHistoricalEffect(string bindingRef) : ISemanticGeneratorEffect
{
    public string BindingRef { get; } = bindingRef;

    public string Describe() => $"set_binding_historical({BindingRef})";
}

/// <summary>
/// Claims all unowned semantic cells within a radius of the resolved
/// binding's semantic position as that entity's named coverage — how
/// generator-born canons take jurisdiction over idea-space.
/// </summary>
/// <summary>
/// War leaves marks on the land: erodes the target's culture influence in a
/// radius around the target's physical position when the generator fires.
/// </summary>
public sealed class ErodeCultureAtTargetEffect(int radius, float amountPerCell) : ISemanticGeneratorEffect
{
    public int Radius { get; } = radius;

    public float AmountPerCell { get; } = amountPerCell;

    public string Describe() => $"erode_culture_at_target(radius={Radius}, amount={AmountPerCell})";
}

public sealed class ClaimSemanticJurisdictionEffect(string bindingRef, int radius) : ISemanticGeneratorEffect
{
    public string BindingRef { get; } = bindingRef;

    public int Radius { get; } = radius;

    public string Describe() => $"claim_semantic_jurisdiction({BindingRef}, r={Radius})";
}

/// <summary>Sets a discrete tag on a resolved binding (or "$target") after the generator fires.</summary>
public sealed class SetBindingTagEffect(string bindingRef, string tag) : ISemanticGeneratorEffect
{
    public string BindingRef { get; } = bindingRef;

    public string Tag { get; } = tag;

    public string Describe() => $"set_binding_tag({BindingRef}, {Tag})";
}

/// <summary>Clears a discrete tag on a resolved binding (or "$target") after the generator fires.</summary>
public sealed class ClearBindingTagEffect(string bindingRef, string tag) : ISemanticGeneratorEffect
{
    public string BindingRef { get; } = bindingRef;

    public string Tag { get; } = tag;

    public string Describe() => $"clear_binding_tag({BindingRef}, {Tag})";
}

/// <summary>Removes relationships of a kind from the target, or a selected relationship instance.</summary>
public sealed class RemoveTargetRelationshipsEffect(
    string relationshipKind,
    GeneratorRelationshipInstanceSelection instanceSelection = GeneratorRelationshipInstanceSelection.AllTargetRelationships,
    bool includeReciprocal = false) : ISemanticGeneratorEffect
{
    public string RelationshipKind { get; } = relationshipKind;

    public GeneratorRelationshipInstanceSelection InstanceSelection { get; } = instanceSelection;

    public bool IncludeReciprocal { get; } = includeReciprocal;

    public string Describe() =>
        $"remove_target_relationships({RelationshipKind}, instance={InstanceSelection}, reciprocal={IncludeReciprocal})";
}

public enum GeneratorRelationshipInstanceSelection
{
    AllTargetRelationships,
    TriggerRelationship,
}

/// <summary>Adjusts relationship strength on the target, or on a selected relationship instance.</summary>
public sealed class AdjustTargetRelationshipStrengthEffect(
    string relationshipKind,
    float delta,
    GeneratorRelationshipInstanceSelection instanceSelection = GeneratorRelationshipInstanceSelection.TriggerRelationship,
    bool includeReciprocal = false) : ISemanticGeneratorEffect
{
    public string RelationshipKind { get; } = relationshipKind;

    public float Delta { get; } = delta;

    public GeneratorRelationshipInstanceSelection InstanceSelection { get; } = instanceSelection;

    public bool IncludeReciprocal { get; } = includeReciprocal;

    public string Describe() =>
        $"adjust_target_relationship_strength({RelationshipKind}, delta={Delta}, instance={InstanceSelection}, reciprocal={IncludeReciprocal})";
}

/// <summary>Transforms a bounded map patch around a resolved generator binding.</summary>
public sealed class SetTerrainPatchEffect(
    string centerRef,
    string terrain,
    int radius,
    int maxCellCount = 0,
    bool respectTerrainBoundaries = true,
    float chance = 1f,
    int planeCode = PlaneCodes.Physical) : ISemanticGeneratorEffect
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
        $"set_terrain_patch(center={CenterRef}, terrain={Terrain}, radius={Radius}, max={MaxCellCount}, chance={Chance}, plane={PlaneCode})";
}

public sealed class SemanticGeneratorRelationship
{
    public required string SourceRef { get; init; }
    public required string DestinationRef { get; init; }
    public required string Kind { get; init; }
    public float Strength { get; init; } = 1f;
    public bool ReplaceExistingOutgoingOfSameKind { get; init; }
}

public sealed class SemanticGeneratorTrigger
{
    public required EntityKind OnKind { get; init; }
}

public enum SemanticPlacement
{
    On,
    Near,
    Across,
    InRegionLocality,

    /// <summary>
    /// Place at the leading edge of the produced entity's culture on this plane -
    /// a known, free cell just beyond the culture's raw-influence border,
    /// preferring the outermost. This is how the culture expands: generator-made
    /// entities land at the frontier and become new anchors that push it further
    /// out. Falls back to a near-origin search when no frontier cell is available.
    /// </summary>
    Frontier,

    /// <summary>
    /// Place on the revealed, unoccupied anchor hex of a geological feature
    /// (nearest matching one). Discovery placement: the produced entity is
    /// the name the feature earns, and its occupancy is the claim.
    /// </summary>
    FeatureAnchor,
}
