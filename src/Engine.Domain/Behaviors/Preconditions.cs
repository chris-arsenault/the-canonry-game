namespace Engine.Behaviors;

using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Wiring;

/// <summary>
/// A composable precondition tree node. Preconditions are config-DSL records;
/// the kernel compiles them into primitive <c>BehaviorProgramPreconditionOp</c>
/// sequences via pattern matching in <c>DomainCompiler</c> and does not evaluate
/// them at runtime.
/// </summary>
public interface IPrecondition
{
    /// <summary>Human-readable description for debug tooling.</summary>
    string Describe();
}

/// <summary>AND composition: all children must pass.</summary>
public sealed class AndPrecondition(IReadOnlyList<IPrecondition> children) : IPrecondition
{
    public IReadOnlyList<IPrecondition> Children { get; } = children;

    public string Describe() =>
        $"ALL({string.Join(", ", Children.Select(c => c.Describe()))})";
}

/// <summary>OR composition: at least one child must pass.</summary>
public sealed class OrPrecondition(IReadOnlyList<IPrecondition> children) : IPrecondition
{
    public IReadOnlyList<IPrecondition> Children { get; } = children;

    public string Describe() =>
        $"ANY({string.Join(", ", Children.Select(c => c.Describe()))})";
}

// --- Leaf Preconditions ---

/// <summary>Checks an entity's attribute value against a threshold.</summary>
public sealed class AttributeThreshold(EntityId entityId, string attributeName, float? min, float? max, bool behaviorRelevant = true) : IPrecondition
{
    public EntityId EntityId { get; } = entityId;
    public string AttributeName { get; } = attributeName;
    public float? Min { get; } = min;
    public float? Max { get; } = max;
    public bool BehaviorRelevant { get; } = behaviorRelevant;

    public string Describe() =>
        $"attribute({EntityId}, {AttributeName}, {(Min.HasValue ? $"min={Min}" : "")}{(Max.HasValue ? $"max={Max}" : "")})";
}

/// <summary>Checks that at least N entities of a kind exist.</summary>
public sealed class EntityCountCheck(EntityKind kind, int min) : IPrecondition
{
    public EntityKind Kind { get; } = kind;
    public int Min { get; } = min;

    public string Describe() => $"entity_count({Kind}, min={Min})";
}

/// <summary>Checks that a relationship exists between two entities.</summary>
public sealed class RelationshipExists(
    EntityId sourceId, EntityId targetId, string relationshipKind, float? minStrength) : IPrecondition
{
    public EntityId SourceId { get; } = sourceId;
    public EntityId TargetId { get; } = targetId;
    public string RelationshipKind { get; } = relationshipKind;
    public float? MinStrength { get; } = minStrength;

    public string Describe() =>
        $"relationship({SourceId}->{TargetId}, {RelationshipKind}{(MinStrength.HasValue ? $", min_strength={MinStrength}" : "")})";
}

/// <summary>
/// Checks that the resolved target has a named relationship back to self.
/// </summary>
public sealed class TargetHasRelationshipToSelf(string relationshipKind, float? minStrength = null, bool behaviorRelevant = false) : IPrecondition
{
    public string RelationshipKind { get; } = relationshipKind;
    public float? MinStrength { get; } = minStrength;
    public bool BehaviorRelevant { get; } = behaviorRelevant;

    public string Describe() =>
        $"target_has_relationship_to_self({RelationshipKind}{(MinStrength.HasValue ? $", min_strength={MinStrength}" : "")})";
}

/// <summary>
/// Checks that self has a named relationship to the resolved target.
/// </summary>
public sealed class SelfHasRelationshipToTarget(string relationshipKind, float? minStrength = null, bool behaviorRelevant = false) : IPrecondition
{
    public string RelationshipKind { get; } = relationshipKind;
    public float? MinStrength { get; } = minStrength;
    public bool BehaviorRelevant { get; } = behaviorRelevant;

    public string Describe() =>
        $"self_has_relationship_to_target({RelationshipKind}{(MinStrength.HasValue ? $", min_strength={MinStrength}" : "")})";
}

/// <summary>
/// Checks that self has any outgoing relationship of the requested kind,
/// regardless of the currently resolved target.
/// </summary>
public sealed class SelfHasOutgoingRelationship(string relationshipKind, float? minStrength = null, bool behaviorRelevant = false) : IPrecondition
{
    public string RelationshipKind { get; } = relationshipKind;
    public float? MinStrength { get; } = minStrength;
    public bool BehaviorRelevant { get; } = behaviorRelevant;

    public string Describe() =>
        $"self_has_outgoing_relationship({RelationshipKind}{(MinStrength.HasValue ? $", min_strength={MinStrength}" : "")})";
}

/// <summary>
/// Checks that the actor has a relationship of a kind (outgoing by default,
/// incoming when <c>incoming</c> is set) whose counterpart is a live entity
/// of a specific kind (and optionally subtype). This is the gambit-grant
/// gate: a gambit carrying this precondition is only available while the
/// actor holds/practices a granting entity, and is revoked when that
/// relationship ends.
/// </summary>
public sealed class SelfHasRelationshipToEntityOfSubtype(
    string relationshipKind,
    EntityKind targetKind,
    string? targetSubtype = null,
    float? minStrength = null,
    bool incoming = false,
    bool behaviorRelevant = false) : IPrecondition
{
    public string RelationshipKind { get; } = relationshipKind;
    public EntityKind TargetKind { get; } = targetKind;
    public string? TargetSubtype { get; } = targetSubtype;
    public float? MinStrength { get; } = minStrength;
    public bool Incoming { get; } = incoming;
    public bool BehaviorRelevant { get; } = behaviorRelevant;

    public string Describe() =>
        $"self_has_relationship_to_entity_of_subtype({RelationshipKind}, {TargetKind.Value}{(TargetSubtype is null ? "" : $"/{TargetSubtype}")}{(Incoming ? ", incoming" : "")}{(MinStrength.HasValue ? $", min_strength={MinStrength}" : "")})";
}

/// <summary>
/// Checks that the selected endpoint has fewer than N distinct counterpart
/// entities connected by a relationship kind.
/// </summary>
public sealed class EndpointRelationshipCountBelow(
    TransferEndpoint endpoint,
    string relationshipKind,
    int maximumExclusive,
    float? minStrength = null,
    bool behaviorRelevant = false) : IPrecondition
{
    public TransferEndpoint Endpoint { get; } = endpoint;
    public string RelationshipKind { get; } = relationshipKind;
    public int MaximumExclusive { get; } = maximumExclusive;
    public float? MinStrength { get; } = minStrength;
    public bool BehaviorRelevant { get; } = behaviorRelevant;

    public string Describe() =>
        $"{Endpoint}_relationship_count({RelationshipKind}) < {MaximumExclusive}{(MinStrength.HasValue ? $", min_strength={MinStrength}" : "")}";
}

/// <summary>
/// Checks that the selected behavior endpoint currently belongs to a culture.
/// </summary>
/// <summary>Checks that the selected endpoint entity has a specific subtype.</summary>
public sealed class EndpointSubtypeIs(TransferEndpoint endpoint, string subtype) : IPrecondition
{
    public TransferEndpoint Endpoint { get; } = endpoint;
    public string Subtype { get; } = subtype;

    public string Describe() => $"endpoint_subtype_is({Endpoint}, {Subtype})";
}

public sealed class EndpointCultureIs(TransferEndpoint endpoint, string culture) : IPrecondition
{
    public TransferEndpoint Endpoint { get; } = endpoint;
    public string Culture { get; } = culture;

    public string Describe() => $"{Endpoint}_culture == {Culture}";
}

/// <summary>
/// Checks whether the entity reached the resolved target through a named relationship.
/// </summary>
public sealed class RelatedEntityMatchesTarget(string relationshipKind, bool behaviorRelevant = true) : IPrecondition
{
    public string RelationshipKind { get; } = relationshipKind;
    public bool BehaviorRelevant { get; } = behaviorRelevant;

    public string Describe() => $"related_matches_target({RelationshipKind})";
}

/// <summary>Checks that enough ticks have passed since the current behavior last fired.</summary>
public sealed class CooldownCheck(int minimumTicksSinceFire) : IPrecondition
{
    public int MinimumTicksSinceFire => minimumTicksSinceFire;

    public string Describe() => $"cooldown(min_ticks={minimumTicksSinceFire})";
}

/// <summary>Checks the current entity's own attribute against a threshold.</summary>
public sealed class SelfAttributeThreshold(string attributeName, float? min, float? max, bool behaviorRelevant = true) : IPrecondition
{
    public string AttributeName { get; } = attributeName;
    public float? Min { get; } = min;
    public float? Max { get; } = max;
    public bool BehaviorRelevant { get; } = behaviorRelevant;

    public string Describe() =>
        $"self.{AttributeName} {(Min.HasValue ? $">= {Min}" : "")} {(Max.HasValue ? $"<= {Max}" : "")}";
}

/// <summary>Checks the resolved target entity's attribute against a threshold.</summary>
public sealed class TargetAttributeThreshold(string attributeName, float? min, float? max, bool behaviorRelevant = true) : IPrecondition
{
    public string AttributeName { get; } = attributeName;
    public float? Min { get; } = min;
    public float? Max { get; } = max;
    public bool BehaviorRelevant { get; } = behaviorRelevant;

    public string Describe() =>
        $"target.{AttributeName} {(Min.HasValue ? $">= {Min}" : "")} {(Max.HasValue ? $"<= {Max}" : "")}";
}

/// <summary>Target must be the entity itself.</summary>
public sealed class TargetIsSelf : IPrecondition
{
    public string Describe() => "target == self";
}

/// <summary>Target must NOT be the entity itself.</summary>
public sealed class TargetIsNotSelf : IPrecondition
{
    public string Describe() => "target != self";
}

/// <summary>Always passes.</summary>
public sealed class AlwaysTrue : IPrecondition
{
    public string Describe() => "always_true";
}

/// <summary>
/// Target must be within the given hex distance on the specified plane.
/// </summary>
public sealed class WithinPlaneDistancePrecondition(int maxDistance, Engine.Domain.Config.PrimaryPlane plane) : IPrecondition
{
    public const string PhysicalInRangeFact = "in_physical_range";
    public const string SemanticInRangeFact = "in_semantic_range";

    public int MaxDistance { get; } = maxDistance;
    public Engine.Domain.Config.PrimaryPlane Plane { get; } = plane;

    public string InRangeFactName => Plane == Engine.Domain.Config.PrimaryPlane.Physical
        ? PhysicalInRangeFact
        : SemanticInRangeFact;

    public string Describe() =>
        Plane == Engine.Domain.Config.PrimaryPlane.Physical
            ? $"physical_distance ≤ {MaxDistance}"
            : $"semantic_distance ≤ {MaxDistance}";
}

/// <summary>
/// Target must be reachable within the given summed travel cost along the
/// line between actor and target on the plane. Impassable or un-thawed
/// ground on the route makes it unreachable; roads shrink the cost. This is
/// the reach gate that scales with a growing map and its infrastructure.
/// </summary>
public sealed class WithinTravelReachPrecondition(float maxCost, Engine.Domain.Config.PrimaryPlane plane) : IPrecondition
{
    public float MaxCost { get; } = maxCost;
    public Engine.Domain.Config.PrimaryPlane Plane { get; } = plane;

    public string Describe() =>
        Plane == Engine.Domain.Config.PrimaryPlane.Physical
            ? $"physical_travel_cost ≤ {MaxCost}"
            : $"semantic_travel_cost ≤ {MaxCost}";
}

/// <summary>
/// Requires self and target to have overlapping influence on at least one cell
/// of the selected plane. Used for territorial actions without teaching the
/// kernel any domain-specific relationship or kind names.
/// </summary>
public sealed class ContestedInfluencePrecondition(
    Engine.Domain.Config.PrimaryPlane plane,
    float selfMinimum,
    float targetMinimum) : IPrecondition
{
    public Engine.Domain.Config.PrimaryPlane Plane { get; } = plane;
    public float SelfMinimum { get; } = selfMinimum;
    public float TargetMinimum { get; } = targetMinimum;

    public string Describe() =>
        $"contested_influence({Plane}, self >= {SelfMinimum}, target >= {TargetMinimum})";
}

/// <summary>
/// Requires self and target to both influence at least one cell of the selected plane.
/// This is neutral overlap language for constructive gambits like trade routes.
/// </summary>
public sealed class SharedInfluencePrecondition(
    Engine.Domain.Config.PrimaryPlane plane,
    float selfMinimum,
    float targetMinimum) : IPrecondition
{
    public Engine.Domain.Config.PrimaryPlane Plane { get; } = plane;
    public float SelfMinimum { get; } = selfMinimum;
    public float TargetMinimum { get; } = targetMinimum;

    public string Describe() =>
        $"shared_influence({Plane}, self >= {SelfMinimum}, target >= {TargetMinimum})";
}

/// <summary>
/// Requires self to stand inside its own culture's mass on the selected plane.
/// This reads the derived culture field rather than a domain-specific entity kind.
/// </summary>
public sealed class SelfCultureMassPrecondition(
    Engine.Domain.Config.PrimaryPlane plane,
    float minimumRawMass,
    float minimumConnectedRawMass) : IPrecondition
{
    public Engine.Domain.Config.PrimaryPlane Plane { get; } = plane;
    public float MinimumRawMass { get; } = minimumRawMass;
    public float MinimumConnectedRawMass { get; } = minimumConnectedRawMass;

    public string Describe() =>
        $"self_culture_mass({Plane}, raw >= {MinimumRawMass}, connected >= {MinimumConnectedRawMass})";
}

/// <summary>Requires self's minor-labor work tier to be below a maximum.</summary>
public sealed class SelfWorkTierBelowPrecondition(int maximumExclusive) : IPrecondition
{
    public int MaximumExclusive { get; } = maximumExclusive;

    public string Describe() => $"self_work_tier < {MaximumExclusive}";
}

/// <summary>Inverts the result of a wrapped precondition.</summary>
public sealed class NotPrecondition(IPrecondition inner) : IPrecondition
{
    public IPrecondition Inner { get; } = inner;

    public string Describe() => $"not({Inner.Describe()})";
}

/// <summary>
/// Self's hex must contain at least one other named entity.
/// </summary>
public sealed class HasColocatedNamedEntityPrecondition : IPrecondition
{
    public string Describe() => "has_colocated_named_entity";
}

/// <summary>
/// A latent-geography fact column a precondition can band against.
/// Authored thresholds use each fact's natural scale — intensity facts
/// 0..1000, Elevation in world elevation units, Relief/Roughness in relief
/// units, coverage facts in microtriangles 0..24 — and the compiler
/// quantizes them with the same functions worldgen used to write the fact.
/// </summary>
public enum PhysicalHexFact
{
    /// <summary>Intensity 0..1000.</summary>
    Temperature = Engine.Constants.PhysicalHexFactCodes.Temperature,

    /// <summary>Intensity 0..1000.</summary>
    Moisture = Engine.Constants.PhysicalHexFactCodes.Moisture,

    /// <summary>Intensity 0..1000.</summary>
    Geothermal = Engine.Constants.PhysicalHexFactCodes.Geothermal,

    /// <summary>World elevation units (negative = depth).</summary>
    Elevation = Engine.Constants.PhysicalHexFactCodes.Elevation,

    /// <summary>Relief units.</summary>
    Relief = Engine.Constants.PhysicalHexFactCodes.Relief,

    /// <summary>Relief units.</summary>
    Roughness = Engine.Constants.PhysicalHexFactCodes.Roughness,

    /// <summary>Microtriangle coverage 0..24.</summary>
    LandFraction = Engine.Constants.PhysicalHexFactCodes.LandFraction,

    /// <summary>Microtriangle coverage 0..24.</summary>
    RiverCoverage = Engine.Constants.PhysicalHexFactCodes.RiverCoverage,

    /// <summary>Microtriangle coverage 0..24.</summary>
    FloodplainCoverage = Engine.Constants.PhysicalHexFactCodes.FloodplainCoverage,

    /// <summary>Intensity 0..1000.</summary>
    IceThickness = Engine.Constants.PhysicalHexFactCodes.IceThickness,

    /// <summary>Intensity 0..1000.</summary>
    FireMean = Engine.Constants.PhysicalHexFactCodes.FireMean,

    /// <summary>Intensity 0..1000.</summary>
    FireMax = Engine.Constants.PhysicalHexFactCodes.FireMax,

    /// <summary>Intensity 0..1000.</summary>
    AuroraMean = Engine.Constants.PhysicalHexFactCodes.AuroraMean,

    /// <summary>Intensity 0..1000.</summary>
    AuroraMax = Engine.Constants.PhysicalHexFactCodes.AuroraMax,

    /// <summary>Intensity 0..1000.</summary>
    PressureMean = Engine.Constants.PhysicalHexFactCodes.PressureMean,

    /// <summary>Intensity 0..1000.</summary>
    PressureMax = Engine.Constants.PhysicalHexFactCodes.PressureMax,
}

/// <summary>
/// The latent geography under self's feet must hold a fact inside the given
/// band. Bounds are authored in the fact's natural scale (see
/// <see cref="PhysicalHexFact"/>); either bound may be omitted. Off the
/// installed field every fact reads as its zero, so a min bound fails there.
/// </summary>
public sealed class SelfHexFactThreshold(PhysicalHexFact fact, int? min = null, int? max = null) : IPrecondition
{
    public PhysicalHexFact Fact { get; } = fact;
    public int? Min { get; } = min;
    public int? Max { get; } = max;

    public string Describe() =>
        $"self_hex_fact({Fact}{(Min.HasValue ? $", min={Min}" : "")}{(Max.HasValue ? $", max={Max}" : "")})";
}

/// <summary>
/// The latent geography under the target's position must hold a fact inside
/// the given band. Same scales and semantics as
/// <see cref="SelfHexFactThreshold"/>.
/// </summary>
public sealed class TargetHexFactThreshold(PhysicalHexFact fact, int? min = null, int? max = null) : IPrecondition
{
    public PhysicalHexFact Fact { get; } = fact;
    public int? Min { get; } = min;
    public int? Max { get; } = max;

    public string Describe() =>
        $"target_hex_fact({Fact}{(Min.HasValue ? $", min={Min}" : "")}{(Max.HasValue ? $", max={Max}" : "")})";
}

/// <summary>
/// Self must stand on terrain belonging to the named boundary group
/// (e.g. warm_land, frozen_land, maritime). Groups, not per-terrain lists:
/// content states the class of ground it wants and stays valid as terrains
/// join or leave the group.
/// </summary>
public sealed class SelfOnTerrainGroup(string boundaryGroup) : IPrecondition
{
    public string BoundaryGroup { get; } = boundaryGroup;

    public string Describe() => $"self_on_terrain_group({BoundaryGroup})";
}
