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
