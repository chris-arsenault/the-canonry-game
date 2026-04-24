namespace Engine.Processes;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Resources;

/// <summary>
/// A mutation: a single effect applied to the world when a card fires.
/// Mutations execute in order within a single card firing.
/// </summary>
public interface IMutation
{
    /// <summary>Apply this mutation to the world state.</summary>
    void Execute(IMutationContext context);

    /// <summary>Human-readable description.</summary>
    string Describe();
}

/// <summary>Creates a new entity.</summary>
public sealed class CreateEntityMutation(
    EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) : IMutation
{
    /// <summary>After execution, holds the ID of the created entity for subsequent mutations.</summary>
    public EntityId? CreatedId { get; private set; }

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var entity = context.CreateEntity(kind, subtype, name, culture, coordinates);
        CreatedId = entity.Id;
    }

    public string Describe() => $"create_entity({kind}, {subtype}, {name})";
}

/// <summary>Creates a relationship between two entities.</summary>
public sealed class CreateRelationshipMutation(
    EntityId sourceId, EntityId targetId, string kind, RelationshipPolarity polarity, float strength) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateRelationship(kind, polarity, sourceId, targetId, strength);
    }

    public string Describe() => $"create_relationship({sourceId}->{targetId}, {kind}, strength={strength})";
}

/// <summary>
/// Modifies an entity's attribute by a delta with optional variance.
/// Actual delta = delta * (1 + random(-variance, +variance)).
/// Variance of 0 means exact delta. Variance of 0.3 means ±30%.
/// </summary>
public sealed class ModifyAttributeMutation(EntityId entityId, string attributeName, float delta, float variance = 0f) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ModifyAttribute(entityId, attributeName, Variance.Scale(delta, variance, context.Random));
    }

    public string Describe() => $"modify_attribute({entityId}, {attributeName}, delta={delta}, var={variance})";
}

/// <summary>Directly modifies a pressure value.</summary>
public sealed class ModifyPressureMutation(string pressureName, EntityId scopeId, float delta) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ModifyPressure(pressureName, scopeId, delta);
    }

    public string Describe() => $"modify_pressure({pressureName}, scope={scopeId}, delta={delta})";
}

/// <summary>Sets a tag on an entity.</summary>
public sealed class SetTagMutation(EntityId entityId, string key, string value) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.SetTag(entityId, key, value);
    }

    public string Describe() => $"set_tag({entityId}, {key}={value})";
}

/// <summary>Transitions an entity to historical status.</summary>
public sealed class SetHistoricalMutation(EntityId entityId) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.SetEntityHistorical(entityId);
    }

    public string Describe() => $"set_historical({entityId})";
}

/// <summary>
/// Modifies the current entity's attribute by a delta with optional variance.
/// Actual delta = delta * (1 + random(-variance, +variance)).
/// Uses SelfEntityId from context.
/// </summary>
public sealed class ModifySelfAttributeMutation(string attributeName, float delta, float variance = 0f) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ModifyAttribute(context.SelfEntityId, attributeName,
            Variance.Scale(delta, variance, context.Random));
    }

    public string Describe() => $"modify_self_attribute({attributeName}, delta={delta}, var={variance})";
}

/// <summary>
/// Modifies an attribute on the resolved target entity (from the process-provider pair).
/// e.g., NPC presence increases target settlement prestige.
/// No-op if no target is resolved.
/// </summary>
public sealed class ModifyTargetAttributeMutation(string attributeName, float delta, float variance = 0f) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ResolvedTargetId is null) return;
        context.ModifyAttribute(context.ResolvedTargetId.Value, attributeName,
            Variance.Scale(delta, variance, context.Random));
    }

    public string Describe() => $"modify_target({attributeName}, delta={delta}, var={variance})";
}

/// <summary>Which entity (relative to the process context) an endpoint refers to.</summary>
public enum TransferEndpoint
{
    Self,
    Target,
}

/// <summary>
/// Atomically transfers exactly <paramref name="Amount"/> units between two
/// attribute stocks. Either the full amount moves or nothing does — partial
/// transfers are not permitted. Conservation is expected to be enforced by a
/// precondition that gates availability at the source ≥ Amount; this mutation
/// double-checks at execution time and aborts the transfer if the source
/// somehow holds less (defensive, should never fire in practice).
///
/// Use this for any same-tick debit/credit pair (self-sac, cross-entity
/// conversions). The fixed-amount invariant keeps stock-flow math closed:
/// every verb firing moves the authored transfer rate exactly.
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

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sourceId = ResolveEndpoint(context, Source);
        var destinationId = ResolveEndpoint(context, Destination);
        if (sourceId is null || destinationId is null) return;

        var sourceEntity = context.GetEntity(sourceId.Value);
        if (sourceEntity is null) return;

        // Precondition should have gated this at source ≥ Amount. If a concurrent
        // mutation on another actor already drained the stock this tick, abort
        // rather than partial-transfer — conservation is all-or-nothing.
        var available = sourceEntity.Attributes.GetValue(SourceAttribute);
        if (available < Amount) return;

        context.ModifyAttribute(sourceId.Value, SourceAttribute, -Amount);
        context.ModifyAttribute(destinationId.Value, DestinationAttribute, +Amount);
    }

    public string Describe() => FormattableString.Invariant(
        $"transfer({Source}.{SourceAttribute} → {Destination}.{DestinationAttribute}, ={Amount})");

    private static EntityId? ResolveEndpoint(IMutationContext context, TransferEndpoint endpoint) =>
        endpoint switch
        {
            TransferEndpoint.Self => context.SelfEntityId,
            TransferEndpoint.Target => context.ResolvedTargetId,
            _ => null,
        };
}

/// <summary>
/// Modifies an ambient resource value at self or target location by a delta.
/// Positive deltas increase the location's resource field, negative deltas reduce it.
/// </summary>
public sealed class ModifyLocationResourceMutation(
    ResourceType resourceType,
    float delta,
    bool targetLocation = false,
    float variance = 0f) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var entityId = targetLocation ? context.ResolvedTargetId : context.SelfEntityId;
        if (entityId is null) return;

        var entity = context.GetEntity(entityId.Value);
        if (entity?.Coordinates is null) return;

        var amount = Variance.Scale(delta, variance, context.Random);
        context.ModifyResource(entity.Coordinates, resourceType, amount);
    }

    public string Describe() =>
        $"modify_{(targetLocation ? "target" : "self")}_location_resource({resourceType.ToDomainName()}, delta={delta}, var={variance})";
}

/// <summary>Traversal direction for relationship-based bulk mutations.</summary>
public enum RelationshipTraversalDirection
{
    Outgoing,
    Incoming,
}

/// <summary>
/// Modifies a scalar attribute on all entities related to self or target through a named relationship.
/// This is the generic bridge for processes that spill effects across the graph without hard-coding
/// domain semantics into the engine.
/// </summary>
public sealed class ModifyRelatedEntitiesAttributeMutation(
    string relationshipKind,
    RelationshipTraversalDirection direction,
    string attributeName,
    float delta,
    bool useTargetEntity = false,
    float variance = 0f) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not IWorldStateAccessor accessor)
        {
            return;
        }

        var anchorId = useTargetEntity ? context.ResolvedTargetId : context.SelfEntityId;
        if (anchorId is null)
        {
            return;
        }

        var relationships = direction == RelationshipTraversalDirection.Outgoing
            ? accessor.WorldState.Graph.OutgoingRelationships(anchorId.Value, relationshipKind)
            : accessor.WorldState.Graph.IncomingRelationships(anchorId.Value, relationshipKind);

        foreach (var relationship in relationships)
        {
            var relatedEntityId = direction == RelationshipTraversalDirection.Outgoing
                ? relationship.TargetId
                : relationship.SourceId;
            var relatedEntity = context.GetEntity(relatedEntityId);
            if (relatedEntity is null ||
                relatedEntity.Status != EntityStatus.Active ||
                !relatedEntity.Attributes.Has(attributeName))
            {
                continue;
            }

            context.ModifyAttribute(
                relatedEntityId,
                attributeName,
                Variance.Scale(delta, variance, context.Random));
        }
    }

    public string Describe() =>
        $"modify_related_attribute(anchor={(useTargetEntity ? "target" : "self")}, dir={direction}, rel={relationshipKind}, attr={attributeName}, delta={delta}, var={variance})";
}

/// <summary>
/// Evaluate an expression and store the result as a named context variable.
/// Subsequent mutations can read it via context.GetVariable().
/// </summary>
public sealed class CalculateMutation(string variableName, IExpression expression) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var self = context.GetEntity(context.SelfEntityId);
        if (self is null) return;

        Entity? target = context.ResolvedTargetId is not null
            ? context.GetEntity(context.ResolvedTargetId.Value) : null;

        Map.HexGrid? grid = null;
        if (context is IWorldStateAccessor wa)
            grid = wa.WorldState.Grid;

        var exprCtx = new ExpressionContext(self, target, grid, context);
        var result = expression.Evaluate(exprCtx);
        context.SetVariable(variableName, result);
    }

    public string Describe() => $"calculate({variableName})";
}

/// <summary>
/// Modifies self attribute by the value of a named context variable (set by CalculateMutation).
/// </summary>
public sealed class ModifySelfByVariableMutation(string attributeName, string variableName,
    float variance = 0f) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var delta = Variance.Scale(context.GetVariable(variableName), variance, context.Random);
        var entity = context.GetEntity(context.SelfEntityId);
        if (entity is null) return;

        entity.Attributes.Modify(attributeName, delta);
    }

    public string Describe() => $"modify_self_by_var({attributeName}, ${variableName}, var={variance})";
}

/// <summary>
/// Modifies target attribute by the value of a named context variable.
/// </summary>
public sealed class ModifyTargetByVariableMutation(string attributeName, string variableName,
    float variance = 0f) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ResolvedTargetId is null) return;

        var delta = Variance.Scale(context.GetVariable(variableName), variance, context.Random);
        var entity = context.GetEntity(context.ResolvedTargetId.Value);
        if (entity is null) return;

        entity.Attributes.Modify(attributeName, delta);
    }

    public string Describe() => $"modify_target_by_var({attributeName}, ${variableName}, var={variance})";
}

/// <summary>
/// Hex→Health conversion. Reads the hex resource value at self's location and
/// credits self's attribute by <c>ratePerHexValue × hex_value</c>. The hex value
/// does NOT drain — hex resources are possibility space that act as a rate
/// multiplier on the health gain.
/// </summary>
public sealed class HexToHealthMutation(
    string attributeName,
    ResourceType resource,
    float ratePerHexValue = 2f,
    bool targetLocation = false) : IMutation
{
    public string AttributeName { get; } = attributeName;
    public ResourceType Resource { get; } = resource;
    public float RatePerHexValue { get; } = ratePerHexValue;
    public bool TargetLocation { get; } = targetLocation;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var sourceEntityId = TargetLocation ? context.ResolvedTargetId : context.SelfEntityId;
        if (sourceEntityId is null)
        {
            return;
        }

        var sourceEntity = context.GetEntity(sourceEntityId.Value);
        if (sourceEntity is null) return;

        var hexValue = context.GetResource(
            (sourceEntity.Coordinates.Q, sourceEntity.Coordinates.R),
            Resource);
        if (hexValue <= 0f) return;

        var delta = RatePerHexValue * hexValue;
        context.ModifyAttribute(context.SelfEntityId, AttributeName, delta);
    }

    public string Describe() =>
        $"hex_to_health({AttributeName}, {(TargetLocation ? "target" : "self")}.hex.{Resource.ToDomainName()}, rate={RatePerHexValue})";
}

/// <summary>
/// Moves self one step along a path toward the resolved target.
/// Optionally maintains an exclusive location-style relationship supplied by the domain.
/// TODO: This is the first reusable movement primitive, not the final planner shape.
/// Keep future work focused on generalizing the arrival/presence fact rather than
/// proliferating target-specific movement mutations.
/// </summary>
public sealed class MoveTowardTargetMutation(
    string? locationRelationshipKind = null,
    EntityKind? locationEntityKind = null) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not IWorldStateAccessor accessor) return;
        if (context.ResolvedTargetId is null) return;

        var self = context.GetEntity(context.SelfEntityId);
        var target = context.GetEntity(context.ResolvedTargetId.Value);
        if (self?.Coordinates is null || target?.Coordinates is null) return;

        var start = new HexCoord(self.Coordinates.Q, self.Coordinates.R);
        var destination = new HexCoord(target.Coordinates.Q, target.Coordinates.R);

        if (start != destination)
        {
            var path = accessor.WorldState.SpatialIndex.FindPath(start, destination);
            if (path is null || path.Count < 2) return;

            var nextStep = path[1];
            context.SetCoordinates(context.SelfEntityId, nextStep.Q, nextStep.R);
        }

        MovementHelper.SyncLocationRelationship(
            context,
            accessor.WorldState,
            locationRelationshipKind,
            locationEntityKind,
            preferredEntityId: context.ResolvedTargetId);
    }

    public string Describe() => $"move_toward_target(rel={locationRelationshipKind ?? "none"})";
}

/// <summary>
/// Moves self to a random walkable neighboring cell. Useful for ambient movement processes.
/// Optionally maintains an exclusive location-style relationship supplied by the domain.
/// </summary>
public sealed class WanderMutation(
    string? locationRelationshipKind = null,
    EntityKind? locationEntityKind = null) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not IWorldStateAccessor accessor) return;

        var self = context.GetEntity(context.SelfEntityId);
        if (self?.Coordinates is null) return;

        var current = new HexCoord(self.Coordinates.Q, self.Coordinates.R);
        var candidates = accessor.WorldState.Grid.GetNeighbors(current)
            .Where(cell => !float.IsPositiveInfinity(cell.MovementCost))
            .Select(cell => cell.Coord)
            .ToList();

        if (candidates.Count == 0) return;

        var index = Math.Clamp((int)(context.Random.NextDouble() * candidates.Count), 0, candidates.Count - 1);
        var nextStep = candidates[index];
        context.SetCoordinates(context.SelfEntityId, nextStep.Q, nextStep.R);

        MovementHelper.SyncLocationRelationship(
            context,
            accessor.WorldState,
            locationRelationshipKind,
            locationEntityKind);
    }

    public string Describe() => $"wander(rel={locationRelationshipKind ?? "none"})";
}

/// <summary>
/// Semantic-plane mobility. Steps self's SemanticPosition one hex toward the
/// resolved target's SemanticPosition along whichever axis shrinks the distance
/// most. No pathfinding — semantic space is always passable.
/// </summary>
public sealed class MoveTowardTargetOnSemanticPlaneMutation : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ResolvedTargetId is null) return;

        var self = context.GetEntity(context.SelfEntityId);
        var target = context.GetEntity(context.ResolvedTargetId.Value);
        if (self is null || target is null) return;

        var current = self.SemanticPosition;
        var dest = target.SemanticPosition;
        if (current == dest) return;

        SemanticHexCoord? best = null;
        var bestDistance = SemanticHexCoord.Distance(current, dest);
        foreach (var neighbor in current.Neighbors())
        {
            var d = SemanticHexCoord.Distance(neighbor, dest);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = neighbor;
            }
        }

        if (best.HasValue)
        {
            self.SemanticPosition = best.Value;
        }
    }

    public string Describe() => "move_toward_target_semantic";
}

/// <summary>
/// Semantic-plane drift. Steps self's SemanticPosition to a uniformly random
/// neighbor on the autonomy/resonance/tradition plane.
/// </summary>
public sealed class WanderOnSemanticPlaneMutation : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var self = context.GetEntity(context.SelfEntityId);
        if (self is null) return;

        var direction = Math.Clamp((int)(context.Random.NextDouble() * 6), 0, 5);
        self.SemanticPosition = self.SemanticPosition.Neighbor(direction);
    }

    public string Describe() => "wander_semantic";
}

internal static class MovementHelper
{
    internal static void SyncLocationRelationship(
        IMutationContext context,
        Behaviors.IWorldState worldState,
        string? relationshipKind,
        EntityKind? locationEntityKind,
        EntityId? preferredEntityId = null)
    {
        if (string.IsNullOrWhiteSpace(relationshipKind))
        {
            return;
        }

        var self = context.GetEntity(context.SelfEntityId);
        if (self?.Coordinates is null)
        {
            return;
        }

        var currentRelatedEntityId = context.FindRelatedEntity(context.SelfEntityId, relationshipKind);
        var resolvedLocationKind = ResolveLocationKind(
            context,
            locationEntityKind,
            preferredEntityId,
            currentRelatedEntityId);
        if (resolvedLocationKind is null)
        {
            return;
        }

        var coord = new HexCoord(self.Coordinates.Q, self.Coordinates.R);
        var desiredEntityId = ResolveLocationEntityAtCell(
            worldState,
            coord,
            resolvedLocationKind.Value,
            preferredEntityId);

        if (currentRelatedEntityId == desiredEntityId)
        {
            return;
        }

        if (currentRelatedEntityId is not null)
        {
            context.CullRelationship(context.SelfEntityId, relationshipKind);
        }

        if (desiredEntityId is not null)
        {
            context.CreateRelationship(
                relationshipKind,
                RelationshipPolarity.Neutral,
                context.SelfEntityId,
                desiredEntityId.Value,
                1f);
        }
    }

    private static EntityKind? ResolveLocationKind(
        IMutationContext context,
        EntityKind? locationEntityKind,
        EntityId? preferredEntityId,
        EntityId? currentRelatedEntityId)
    {
        if (locationEntityKind is not null)
        {
            return locationEntityKind;
        }

        if (preferredEntityId is not null)
        {
            return context.GetEntity(preferredEntityId.Value)?.Kind;
        }

        if (currentRelatedEntityId is not null)
        {
            return context.GetEntity(currentRelatedEntityId.Value)?.Kind;
        }

        return null;
    }

    private static EntityId? ResolveLocationEntityAtCell(
        Behaviors.IWorldState worldState,
        HexCoord coord,
        EntityKind locationEntityKind,
        EntityId? preferredEntityId)
    {
        var candidates = worldState.SpatialIndex.EntitiesAtCell(coord)
            .Select(worldState.Graph.GetEntity)
            .Where(entity => entity is not null &&
                entity.Status == EntityStatus.Active &&
                entity.Kind == locationEntityKind)
            .Select(entity => entity!.Id)
            .ToList();

        if (preferredEntityId is not null && candidates.Contains(preferredEntityId.Value))
        {
            return preferredEntityId.Value;
        }

        return candidates.Count > 0 ? candidates[0] : null;
    }
}

/// <summary>
/// Marker interface: the mutation context can provide direct IWorldState access.
/// Used by evaluator internals that need spatial queries or pathfinding.
/// </summary>
public interface IWorldStateAccessor
{
    Behaviors.IWorldState WorldState { get; }
}
