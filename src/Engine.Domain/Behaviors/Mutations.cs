namespace Engine.Behaviors;

using System.Globalization;

using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// A mutation: a single effect applied to the world when a data-driven behavior executes.
/// Mutations are config-DSL records — the kernel compiles them into primitive
/// <c>BehaviorProgramStepRule</c> ops via pattern matching in <c>DomainCompiler</c>.
/// They are not executed by the kernel at runtime.
/// </summary>
public interface IMutation
{
    /// <summary>Human-readable description for debug tooling and tests.</summary>
    string Describe();
}

/// <summary>Creates a new entity.</summary>
public sealed class CreateEntityMutation(
    EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) : IMutation
{
    public EntityKind Kind { get; } = kind;
    public string Subtype { get; } = subtype;
    public string Name { get; } = name;
    public string Culture { get; } = culture;
    public (int Q, int R)? Coordinates { get; } = coordinates;

    public string Describe() => $"create_entity({Kind}, {Subtype}, {Name})";
}

/// <summary>Creates a relationship between two entities.</summary>
public sealed class CreateRelationshipMutation(
    EntityId sourceId, EntityId targetId, string kind, RelationshipPolarity polarity, float strength) : IMutation
{
    public EntityId SourceId { get; } = sourceId;
    public EntityId TargetId { get; } = targetId;
    public string RelationshipKind { get; } = kind;
    public RelationshipPolarity Polarity { get; } = polarity;
    public float Strength { get; } = strength;

    public string Describe() => $"create_relationship({SourceId}->{TargetId}, {RelationshipKind}, strength={Strength})";
}

/// <summary>
/// Creates a relationship between the current behavior's self/target endpoints.
/// This is the runtime form gambits usually want: source and target are resolved
/// from the selected actor and selected target rather than fixed seed IDs.
/// </summary>
public sealed class CreateContextRelationshipMutation(
    TransferEndpoint source,
    TransferEndpoint target,
    string kind,
    RelationshipPolarity polarity,
    float strength) : IMutation
{
    public TransferEndpoint Source { get; } = source;
    public TransferEndpoint Target { get; } = target;
    public string RelationshipKind { get; } = kind;
    public RelationshipPolarity Polarity { get; } = polarity;
    public float Strength { get; } = strength;

    public string Describe() => $"create_relationship({Source}->{Target}, {RelationshipKind}, strength={Strength})";
}

/// <summary>
/// Removes an existing relationship between the current behavior's self/target endpoints.
/// Missing relationships are intentionally no-ops.
/// </summary>
/// <summary>
/// Culls every incoming relationship of a kind on the chosen endpoint,
/// regardless of who the sources are. The wrest primitive: taking an
/// artifact strips its previous holders.
/// </summary>
public sealed class CullEndpointIncomingRelationshipsMutation(TransferEndpoint endpoint, string relationshipKind) : IMutation
{
    public TransferEndpoint Endpoint { get; } = endpoint;
    public string RelationshipKind { get; } = relationshipKind;

    public string Describe() => $"cull_endpoint_incoming_relationships({Endpoint}, {RelationshipKind})";
}

public sealed class RemoveContextRelationshipMutation(
    TransferEndpoint source,
    TransferEndpoint target,
    string kind) : IMutation
{
    public TransferEndpoint Source { get; } = source;
    public TransferEndpoint Target { get; } = target;
    public string RelationshipKind { get; } = kind;

    public string Describe() => $"remove_relationship({Source}->{Target}, {RelationshipKind})";
}

/// <summary>
/// Sets strength on an existing relationship between the current behavior's
/// self/target endpoints. Missing relationships are intentionally no-ops.
/// </summary>
public sealed class SetContextRelationshipStrengthMutation(
    TransferEndpoint source,
    TransferEndpoint target,
    string kind,
    float strength) : IMutation
{
    public TransferEndpoint Source { get; } = source;
    public TransferEndpoint Target { get; } = target;
    public string RelationshipKind { get; } = kind;
    public float Strength { get; } = strength;

    public string Describe() => $"set_relationship_strength({Source}->{Target}, {RelationshipKind}, strength={Strength})";
}

/// <summary>
/// Modifies an entity's attribute by a delta with optional variance.
/// Actual delta = delta * (1 + random(-variance, +variance)).
/// Variance of 0 means exact delta. Variance of 0.3 means ±30%.
/// </summary>
public sealed class ModifyAttributeMutation(EntityId entityId, string attributeName, float delta, float variance = 0f) : IMutation
{
    public EntityId EntityId { get; } = entityId;
    public string AttributeName { get; } = attributeName;
    public float Delta { get; } = delta;
    public float VarianceValue { get; } = variance;

    public string Describe() => $"modify_attribute({EntityId}, {AttributeName}, delta={Delta}, var={VarianceValue})";
}

// Future marker, status, or fact mutations should be designed as explicit
// domain contracts. The old generic entity tag mutation was removed because it
// had no authored semantics.

/// <summary>Transitions an entity to historical status.</summary>
public sealed class SetHistoricalMutation(EntityId entityId) : IMutation
{
    public EntityId EntityId { get; } = entityId;

    public string Describe() => $"set_historical({EntityId})";
}

/// <summary>Transitions the resolved target entity to historical status.</summary>
public sealed class SetTargetHistoricalMutation : IMutation
{
    public string Describe() => "set_historical(target)";
}

/// <summary>
/// Changes the resolved target's culture to the current actor's culture.
/// </summary>
public sealed class SetTargetCultureFromSelfMutation : IMutation
{
    public string Describe() => "set_target_culture_from_self";
}

/// <summary>Raises the current entity's minor-labor work tier by one. A max tier of zero means uncapped.</summary>
public sealed class IncreaseWorkTierMutation(int maxTier = 0) : IMutation
{
    public int MaxTier { get; } = maxTier;

    public string Describe() => MaxTier > 0
        ? $"increase_work_tier(max={MaxTier})"
        : "increase_work_tier";
}

/// <summary>
/// Modifies the current entity's attribute by a delta with optional variance.
/// Actual delta = delta * (1 + random(-variance, +variance)).
/// </summary>
public sealed class ModifySelfAttributeMutation(string attributeName, float delta, float variance = 0f) : IMutation
{
    public string AttributeName { get; } = attributeName;
    public float Delta { get; } = delta;
    public float VarianceValue { get; } = variance;

    public string Describe() => $"modify_self_attribute({AttributeName}, delta={Delta}, var={VarianceValue})";
}

/// <summary>
/// Modifies an attribute on the resolved target entity (from the behavior-provider pair).
/// </summary>
public sealed class ModifyTargetAttributeMutation(string attributeName, float delta, float variance = 0f) : IMutation
{
    public string AttributeName { get; } = attributeName;
    public float Delta { get; } = delta;
    public float VarianceValue { get; } = variance;

    public string Describe() => $"modify_target({AttributeName}, delta={Delta}, var={VarianceValue})";
}

/// <summary>Which entity (relative to the behavior context) an endpoint refers to.</summary>
public enum TransferEndpoint
{
    Self,
    Target,
}

/// <summary>
/// Atomically transfers exactly <paramref name="Amount"/> units between two
/// attribute stocks. Either the full amount moves or nothing does — partial
/// transfers are not permitted. Conservation is expected to be enforced by a
/// precondition that gates availability at the source ≥ Amount.
/// </summary>
public sealed class TransferAttributeMutation(
    TransferEndpoint source,
    string sourceAttribute,
    TransferEndpoint destination,
    string destinationAttribute,
    float amount) : IMutation
{
    public TransferEndpoint Source { get; } = source;
    public string SourceAttribute { get; } = sourceAttribute;
    public TransferEndpoint Destination { get; } = destination;
    public string DestinationAttribute { get; } = destinationAttribute;
    public float Amount { get; } = amount;

    public string Describe() => string.Create(CultureInfo.InvariantCulture, $"transfer({Source}.{SourceAttribute} → {Destination}.{DestinationAttribute}, ={Amount})");
}

/// <summary>Traversal direction for relationship-based bulk mutations.</summary>
public enum RelationshipTraversalDirection
{
    Outgoing,
    Incoming,
}

/// <summary>
/// Modifies a scalar attribute on all entities related to self or target through a named relationship.
/// </summary>
public sealed class ModifyRelatedEntitiesAttributeMutation(
    string relationshipKind,
    RelationshipTraversalDirection direction,
    string attributeName,
    float delta,
    bool useTargetEntity = false,
    float variance = 0f) : IMutation
{
    public string RelationshipKind { get; } = relationshipKind;
    public RelationshipTraversalDirection Direction { get; } = direction;
    public string AttributeName { get; } = attributeName;
    public float Delta { get; } = delta;
    public bool UseTargetEntity { get; } = useTargetEntity;
    public float VarianceValue { get; } = variance;

    public string Describe() =>
        $"modify_related_attribute(anchor={(UseTargetEntity ? "target" : "self")}, dir={Direction}, rel={RelationshipKind}, attr={AttributeName}, delta={Delta}, var={VarianceValue})";
}

/// <summary>
/// Evaluate an expression and store the result as a named context variable.
/// Subsequent mutations can read it via the runtime variable store.
/// </summary>
public sealed class CalculateMutation(string variableName, IExpression expression) : IMutation
{
    public string VariableName { get; } = variableName;
    public IExpression Expression { get; } = expression;

    public string Describe() => $"calculate({VariableName})";
}

/// <summary>
/// Modifies self attribute by the value of a named context variable (set by CalculateMutation).
/// </summary>
public sealed class ModifySelfByVariableMutation(string attributeName, string variableName,
    float variance = 0f) : IMutation
{
    public string AttributeName { get; } = attributeName;
    public string VariableName { get; } = variableName;
    public float VarianceValue { get; } = variance;

    public string Describe() => $"modify_self_by_var({AttributeName}, ${VariableName}, var={VarianceValue})";
}

/// <summary>
/// Modifies target attribute by the value of a named context variable.
/// </summary>
public sealed class ModifyTargetByVariableMutation(string attributeName, string variableName,
    float variance = 0f) : IMutation
{
    public string AttributeName { get; } = attributeName;
    public string VariableName { get; } = variableName;
    public float VarianceValue { get; } = variance;

    public string Describe() => $"modify_target_by_var({AttributeName}, ${VariableName}, var={VarianceValue})";
}

/// <summary>
/// Moves self one step along a path toward the resolved target.
/// </summary>
/// <summary>
/// War leaves marks on the land: erodes the target's culture influence in a
/// radius around the target's physical position. Conflict couples to
/// concentrated territorial loss - the front becomes visible.
/// </summary>
public sealed class ErodeTargetCultureMutation(int radius, float amountPerCell) : IMutation
{
    public int Radius { get; } = radius;

    public float AmountPerCell { get; } = amountPerCell;

    public string Describe() => $"erode_target_culture(radius={Radius}, amount={AmountPerCell})";
}

public sealed class MoveTowardTargetMutation(
    string? locationRelationshipKind = null,
    EntityKind? locationEntityKind = null) : IMutation
{
    public string? LocationRelationshipKind { get; } = locationRelationshipKind;
    public EntityKind? LocationEntityKind { get; } = locationEntityKind;

    public string Describe() => $"move_toward_target(rel={LocationRelationshipKind ?? "none"})";
}

/// <summary>
/// Moves self to a random walkable neighboring cell.
/// </summary>
public sealed class WanderMutation(
    string? locationRelationshipKind = null,
    EntityKind? locationEntityKind = null) : IMutation
{
    public string? LocationRelationshipKind { get; } = locationRelationshipKind;
    public EntityKind? LocationEntityKind { get; } = locationEntityKind;

    public string Describe() => $"wander(rel={LocationRelationshipKind ?? "none"})";
}

/// <summary>
/// Semantic-plane mobility. Steps self's SemanticPosition one hex toward the
/// resolved target's SemanticPosition along whichever axis shrinks the distance most.
/// </summary>
public sealed class MoveTowardTargetOnSemanticPlaneMutation : IMutation
{
    public string Describe() => "move_toward_target_semantic";
}

/// <summary>
/// Moves the resolved target onto self's physical cell. Persistent attachment is
/// represented by relationship config; this mutation handles the moment the
/// behavior establishes the relationship.
/// </summary>
public sealed class MoveTargetToSelfPhysicalMutation : IMutation
{
    public string Describe() => "move_target_to_self_physical";
}

/// <summary>
/// Changes every map cell owned by the resolved target's named coverage to a terrain.
/// </summary>
/// <summary>
/// Forges a new physical cell one layer above (direction +1, spire tier) or
/// below (direction -1, tunnel gallery) the acting entity's column, creeping
/// to adjacent columns once the actor's own column is fully forged.
/// </summary>
public sealed class ForgeCellVerticalMutation(int direction, string terrainName) : IMutation
{
    public int Direction { get; } = direction;

    public string TerrainName { get; } = terrainName;

    public string Describe() => Direction > 0
        ? $"forge_spire_tier({TerrainName})"
        : $"forge_tunnel_gallery({TerrainName})";
}

public sealed class SetTargetCoverageTerrainMutation(string terrainName) : IMutation
{
    public string TerrainName { get; } = terrainName;

    public string Describe() => $"set_target_coverage_terrain({TerrainName})";
}

/// <summary>
/// Builds a travel structure on the straight hex line between self and target.
/// The structure is a movement-surface overlay; it does not change terrain or
/// create an entity.
/// </summary>
public sealed class BuildTravelStructureLineMutation(
    string structureName,
    int planeCode,
    int maxDistance) : IMutation
{
    public string StructureName { get; } = structureName;
    public int PlaneCode { get; } = planeCode;
    public int MaxDistance { get; } = maxDistance;

    public string Describe() => $"build_travel_structure_line({StructureName}, plane={PlaneCode}, max={MaxDistance})";
}

/// <summary>
/// Semantic-plane drift. Steps self's SemanticPosition to a uniformly random
/// neighbor on the neutral semantic (idea-space) plane.
/// </summary>
public sealed class WanderOnSemanticPlaneMutation : IMutation
{
    public string Describe() => "wander_semantic";
}
