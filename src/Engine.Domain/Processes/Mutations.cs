namespace Engine.Processes;

using Engine.Behaviors;
using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.WorldGen;
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

    void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
    }
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

    /// <summary>After execution, holds the ID of the created entity for subsequent mutations.</summary>
    public EntityId? CreatedId { get; private set; }

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var entity = context.CreateEntity(Kind, Subtype, Name, Culture, Coordinates);
        CreatedId = entity.Id;
    }

    public string Describe() => $"create_entity({Kind}, {Subtype}, {Name})";
}

/// <summary>Creates a relationship between two entities.</summary>
public sealed class CreateRelationshipMutation(
    EntityId sourceId, EntityId targetId, string kind, RelationshipPolarity polarity, float strength) : IMutation
{
    private RelationshipKindRuntimeId _relationshipKindId;
    public EntityId SourceId { get; } = sourceId;
    public EntityId TargetId { get; } = targetId;
    public string RelationshipKind { get; } = kind;
    public RelationshipPolarity Polarity { get; } = polarity;
    public float Strength { get; } = strength;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_relationshipKindId.Value > 0)
        {
            context.CreateRelationship(_relationshipKindId, Polarity, SourceId, TargetId, Strength);
            return;
        }

        context.CreateRelationship(RelationshipKind, Polarity, SourceId, TargetId, Strength);
    }

    public string Describe() => $"create_relationship({SourceId}->{TargetId}, {RelationshipKind}, strength={Strength})";

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _relationshipKindId = runtimeIds.RelationshipKindIdOf(RelationshipKind);
    }
}

/// <summary>
/// Modifies an entity's attribute by a delta with optional variance.
/// Actual delta = delta * (1 + random(-variance, +variance)).
/// Variance of 0 means exact delta. Variance of 0.3 means ±30%.
/// </summary>
public sealed class ModifyAttributeMutation(EntityId entityId, string attributeName, float delta, float variance = 0f) : IMutation
{
    private AttributeRuntimeId _attributeId;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_attributeId.Value > 0)
        {
            context.ModifyAttribute(entityId, _attributeId, Variance.Scale(delta, variance, context.Random));
            return;
        }

        context.ModifyAttribute(entityId, attributeName, Variance.Scale(delta, variance, context.Random));
    }

    public string Describe() => $"modify_attribute({entityId}, {attributeName}, delta={delta}, var={variance})";

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _attributeId = runtimeIds.AttributeIdOf(attributeName);
    }
}

/// <summary>Directly modifies a pressure value.</summary>
public sealed class ModifyPressureMutation(string pressureName, EntityId scopeId, float delta) : IMutation
{
    private PressureRuntimeId _pressureId;
    public string PressureName { get; } = pressureName;
    public EntityId ScopeId { get; } = scopeId;
    public float Delta { get; } = delta;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_pressureId.Value > 0)
        {
            context.ModifyPressure(_pressureId, ScopeId, Delta);
            return;
        }

        context.ModifyPressure(PressureName, ScopeId, Delta);
    }

    public string Describe() => $"modify_pressure({PressureName}, scope={ScopeId}, delta={Delta})";

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _pressureId = runtimeIds.PressureIdOf(PressureName);
    }
}

/// <summary>Sets a tag on an entity.</summary>
public sealed class SetTagMutation(EntityId entityId, string key, string value) : IMutation
{
    public EntityId EntityId { get; } = entityId;
    public string Key { get; } = key;
    public string Value { get; } = value;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.SetTag(EntityId, Key, Value);
    }

    public string Describe() => $"set_tag({EntityId}, {Key}={Value})";
}

/// <summary>Transitions an entity to historical status.</summary>
public sealed class SetHistoricalMutation(EntityId entityId) : IMutation
{
    public EntityId EntityId { get; } = entityId;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.SetEntityHistorical(EntityId);
    }

    public string Describe() => $"set_historical({EntityId})";
}

/// <summary>
/// Modifies the current entity's attribute by a delta with optional variance.
/// Actual delta = delta * (1 + random(-variance, +variance)).
/// Uses SelfEntityId from context.
/// </summary>
public sealed class ModifySelfAttributeMutation(string attributeName, float delta, float variance = 0f) : IMutation
{
    private AttributeRuntimeId _attributeId;
    public string AttributeName { get; } = attributeName;
    public float Delta { get; } = delta;
    public float VarianceValue { get; } = variance;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scaled = Variance.Scale(Delta, VarianceValue, context.Random);
        if (_attributeId.Value > 0)
        {
            context.ModifyAttribute(context.SelfEntityId, _attributeId, scaled);
            return;
        }

        context.ModifyAttribute(context.SelfEntityId, AttributeName, scaled);
    }

    public string Describe() => $"modify_self_attribute({AttributeName}, delta={Delta}, var={VarianceValue})";

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _attributeId = runtimeIds.AttributeIdOf(AttributeName);
    }
}

/// <summary>
/// Modifies an attribute on the resolved target entity (from the process-provider pair).
/// e.g., NPC presence increases target settlement prestige.
/// No-op if no target is resolved.
/// </summary>
public sealed class ModifyTargetAttributeMutation(string attributeName, float delta, float variance = 0f) : IMutation
{
    private AttributeRuntimeId _attributeId;
    public string AttributeName { get; } = attributeName;
    public float Delta { get; } = delta;
    public float VarianceValue { get; } = variance;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ResolvedTargetId is null) return;
        var scaled = Variance.Scale(Delta, VarianceValue, context.Random);
        if (_attributeId.Value > 0)
        {
            context.ModifyAttribute(context.ResolvedTargetId.Value, _attributeId, scaled);
            return;
        }

        context.ModifyAttribute(context.ResolvedTargetId.Value, AttributeName, scaled);
    }

    public string Describe() => $"modify_target({AttributeName}, delta={Delta}, var={VarianceValue})";

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _attributeId = runtimeIds.AttributeIdOf(AttributeName);
    }
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
    private AttributeRuntimeId _sourceAttributeId;
    private AttributeRuntimeId _destinationAttributeId;

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
        var available = _sourceAttributeId.Value > 0
            ? sourceEntity.Attributes.GetValue(_sourceAttributeId)
            : sourceEntity.Attributes.GetValue(SourceAttribute);
        if (available < Amount) return;

        if (_sourceAttributeId.Value > 0 && _destinationAttributeId.Value > 0)
        {
            context.ModifyAttribute(sourceId.Value, _sourceAttributeId, -Amount);
            context.ModifyAttribute(destinationId.Value, _destinationAttributeId, +Amount);
            return;
        }

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

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _sourceAttributeId = runtimeIds.AttributeIdOf(SourceAttribute);
        _destinationAttributeId = runtimeIds.AttributeIdOf(DestinationAttribute);
    }
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
    public ResourceType ResourceType { get; } = resourceType;
    public float Delta { get; } = delta;
    public bool TargetLocation { get; } = targetLocation;
    public float VarianceValue { get; } = variance;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var entityId = TargetLocation ? context.ResolvedTargetId : context.SelfEntityId;
        if (entityId is null) return;

        var entity = context.GetEntity(entityId.Value);
        if (entity?.Coordinates is null) return;

        var amount = Variance.Scale(Delta, VarianceValue, context.Random);
        context.ModifyResource(entity.Coordinates, ResourceType, amount);
    }

    public string Describe() =>
        $"modify_{(TargetLocation ? "target" : "self")}_location_resource({ResourceType.ToDomainName()}, delta={Delta}, var={VarianceValue})";
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
    private RelationshipKindRuntimeId _relationshipKindId;
    private AttributeRuntimeId _attributeId;
    public string RelationshipKind { get; } = relationshipKind;
    public RelationshipTraversalDirection Direction { get; } = direction;
    public string AttributeName { get; } = attributeName;
    public float Delta { get; } = delta;
    public bool UseTargetEntity { get; } = useTargetEntity;
    public float VarianceValue { get; } = variance;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not IWorldStateAccessor accessor)
        {
            return;
        }

        var anchorId = UseTargetEntity ? context.ResolvedTargetId : context.SelfEntityId;
        if (anchorId is null)
        {
            return;
        }

        var relationships = _relationshipKindId.Value > 0
            ? Direction == RelationshipTraversalDirection.Outgoing
                ? GetOutgoingRelationships(accessor.WorldState, anchorId.Value, _relationshipKindId)
                : GetIncomingRelationships(accessor.WorldState, anchorId.Value, _relationshipKindId)
            : Direction == RelationshipTraversalDirection.Outgoing
                ? GetOutgoingRelationships(accessor.WorldState, anchorId.Value, RelationshipKind)
                : GetIncomingRelationships(accessor.WorldState, anchorId.Value, RelationshipKind);

        foreach (var relationship in relationships)
        {
            var relatedEntityId = Direction == RelationshipTraversalDirection.Outgoing
                ? relationship.TargetId
                : relationship.SourceId;
            var relatedEntity = context.GetEntity(relatedEntityId);
            if (relatedEntity is null ||
                relatedEntity.Status != EntityStatus.Active ||
                !(_attributeId.Value > 0
                    ? relatedEntity.Attributes.Has(_attributeId)
                    : relatedEntity.Attributes.Has(AttributeName)))
            {
                continue;
            }

            var scaled = Variance.Scale(Delta, VarianceValue, context.Random);
            if (_attributeId.Value > 0)
            {
                context.ModifyAttribute(relatedEntityId, _attributeId, scaled);
            }
            else
            {
                context.ModifyAttribute(relatedEntityId, AttributeName, scaled);
            }
        }
    }

    public string Describe() =>
        $"modify_related_attribute(anchor={(UseTargetEntity ? "target" : "self")}, dir={Direction}, rel={RelationshipKind}, attr={AttributeName}, delta={Delta}, var={VarianceValue})";

    private static IReadOnlyList<Relationship> GetOutgoingRelationships(
        IWorldState worldState,
        EntityId sourceId,
        string kind) =>
        worldState is ICachedWorldState cachedWorldState
            ? cachedWorldState.QueryCache.OutgoingRelationships(sourceId, kind)
            : worldState.Graph.OutgoingRelationships(sourceId, kind).ToArray();

    private static IReadOnlyList<Relationship> GetOutgoingRelationships(
        IWorldState worldState,
        EntityId sourceId,
        RelationshipKindRuntimeId kind) =>
        worldState is ICachedWorldState cachedWorldState
            ? cachedWorldState.QueryCache.OutgoingRelationships(sourceId, kind)
            : worldState.Graph.OutgoingRelationships(sourceId, kind).ToArray();

    private static IReadOnlyList<Relationship> GetIncomingRelationships(
        IWorldState worldState,
        EntityId targetId,
        string kind) =>
        worldState is ICachedWorldState cachedWorldState
            ? cachedWorldState.QueryCache.IncomingRelationships(targetId, kind)
            : worldState.Graph.IncomingRelationships(targetId, kind).ToArray();

    private static IReadOnlyList<Relationship> GetIncomingRelationships(
        IWorldState worldState,
        EntityId targetId,
        RelationshipKindRuntimeId kind) =>
        worldState is ICachedWorldState cachedWorldState
            ? cachedWorldState.QueryCache.IncomingRelationships(targetId, kind)
            : worldState.Graph.IncomingRelationships(targetId, kind).ToArray();

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _relationshipKindId = runtimeIds.RelationshipKindIdOf(RelationshipKind);
        _attributeId = runtimeIds.AttributeIdOf(AttributeName);
    }
}

/// <summary>
/// Evaluate an expression and store the result as a named context variable.
/// Subsequent mutations can read it via context.GetVariable().
/// </summary>
public sealed class CalculateMutation(string variableName, IExpression expression) : IMutation
{
    public string VariableName { get; } = variableName;
    public IExpression Expression { get; } = expression;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var self = context.GetEntity(context.SelfEntityId);
        if (self is null) return;

        Entity? target = context.ResolvedTargetId is not null
            ? context.GetEntity(context.ResolvedTargetId.Value) : null;

        HexGrid? grid = null;
        if (context is IWorldStateAccessor wa)
            grid = wa.WorldState.Grid;

        var exprCtx = new ExpressionContext(self, target, grid, context);
        var result = Expression.Evaluate(exprCtx);
        context.SetVariable(VariableName, result);
    }

    public string Describe() => $"calculate({VariableName})";

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        Expression.BindRuntimeIds(runtimeIds);
    }
}

/// <summary>
/// Modifies self attribute by the value of a named context variable (set by CalculateMutation).
/// </summary>
public sealed class ModifySelfByVariableMutation(string attributeName, string variableName,
    float variance = 0f) : IMutation
{
    private AttributeRuntimeId _attributeId;
    public string AttributeName { get; } = attributeName;
    public string VariableName { get; } = variableName;
    public float VarianceValue { get; } = variance;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var delta = Variance.Scale(context.GetVariable(VariableName), VarianceValue, context.Random);
        if (_attributeId.Value > 0)
        {
            context.ModifyAttribute(context.SelfEntityId, _attributeId, delta);
            return;
        }

        context.ModifyAttribute(context.SelfEntityId, AttributeName, delta);
    }

    public string Describe() => $"modify_self_by_var({AttributeName}, ${VariableName}, var={VarianceValue})";

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _attributeId = runtimeIds.AttributeIdOf(AttributeName);
    }
}

/// <summary>
/// Modifies target attribute by the value of a named context variable.
/// </summary>
public sealed class ModifyTargetByVariableMutation(string attributeName, string variableName,
    float variance = 0f) : IMutation
{
    private AttributeRuntimeId _attributeId;
    public string AttributeName { get; } = attributeName;
    public string VariableName { get; } = variableName;
    public float VarianceValue { get; } = variance;

    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ResolvedTargetId is null) return;

        var delta = Variance.Scale(context.GetVariable(VariableName), VarianceValue, context.Random);
        if (_attributeId.Value > 0)
        {
            context.ModifyAttribute(context.ResolvedTargetId.Value, _attributeId, delta);
            return;
        }

        context.ModifyAttribute(context.ResolvedTargetId.Value, AttributeName, delta);
    }

    public string Describe() => $"modify_target_by_var({AttributeName}, ${VariableName}, var={VarianceValue})";

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _attributeId = runtimeIds.AttributeIdOf(AttributeName);
    }
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
    private AttributeRuntimeId _attributeId;

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
        if (_attributeId.Value > 0)
        {
            context.ModifyAttribute(context.SelfEntityId, _attributeId, delta);
            return;
        }

        context.ModifyAttribute(context.SelfEntityId, AttributeName, delta);
    }

    public string Describe() =>
        $"hex_to_health({AttributeName}, {(TargetLocation ? "target" : "self")}.hex.{Resource.ToDomainName()}, rate={RatePerHexValue})";

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _attributeId = runtimeIds.AttributeIdOf(AttributeName);
    }
}

/// <summary>
/// Moves self one step along a path toward the resolved target.
/// Optionally maintains an exclusive location-style relationship supplied by the domain.
/// TODO: This is the first reusable movement primitive, not the final process program shape.
/// Keep future work focused on generalizing the arrival/presence fact rather than
/// proliferating target-specific movement mutations.
/// </summary>
public sealed class MoveTowardTargetMutation(
    string? locationRelationshipKind = null,
    EntityKind? locationEntityKind = null) : IMutation
{
    private RelationshipKindRuntimeId _locationRelationshipKindId;

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
            _locationRelationshipKindId,
            locationEntityKind,
            preferredEntityId: context.ResolvedTargetId);
    }

    public string Describe() => $"move_toward_target(rel={locationRelationshipKind ?? "none"})";

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _locationRelationshipKindId = string.IsNullOrWhiteSpace(locationRelationshipKind)
            ? RelationshipKindRuntimeId.None
            : runtimeIds.RelationshipKindIdOf(locationRelationshipKind);
    }
}

/// <summary>
/// Moves self to a random walkable neighboring cell. Useful for ambient movement processes.
/// Optionally maintains an exclusive location-style relationship supplied by the domain.
/// </summary>
public sealed class WanderMutation(
    string? locationRelationshipKind = null,
    EntityKind? locationEntityKind = null) : IMutation
{
    private RelationshipKindRuntimeId _locationRelationshipKindId;

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
            _locationRelationshipKindId,
            locationEntityKind);
    }

    public string Describe() => $"wander(rel={locationRelationshipKind ?? "none"})";

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _locationRelationshipKindId = string.IsNullOrWhiteSpace(locationRelationshipKind)
            ? RelationshipKindRuntimeId.None
            : runtimeIds.RelationshipKindIdOf(locationRelationshipKind);
    }
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
            context.SetSemanticCoordinates(context.SelfEntityId, best.Value.Q, best.Value.R);
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
        var next = self.SemanticPosition.Neighbor(direction);
        context.SetSemanticCoordinates(context.SelfEntityId, next.Q, next.R);
    }

    public string Describe() => "wander_semantic";
}

internal static class MovementHelper
{
    internal static void SyncLocationRelationship(
        IMutationContext context,
        IWorldState worldState,
        string? relationshipKind,
        RelationshipKindRuntimeId relationshipKindId,
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

        var currentRelatedEntityId = relationshipKindId.Value > 0
            ? context.FindRelatedEntity(context.SelfEntityId, relationshipKindId)
            : context.FindRelatedEntity(context.SelfEntityId, relationshipKind);
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
            if (relationshipKindId.Value > 0)
            {
                context.CullRelationship(context.SelfEntityId, relationshipKindId);
            }
            else
            {
                context.CullRelationship(context.SelfEntityId, relationshipKind);
            }
        }

        if (desiredEntityId is not null)
        {
            if (relationshipKindId.Value > 0)
            {
                context.CreateRelationship(
                    relationshipKindId,
                    RelationshipPolarity.Neutral,
                    context.SelfEntityId,
                    desiredEntityId.Value,
                    1f);
            }
            else
            {
                context.CreateRelationship(
                    relationshipKind,
                    RelationshipPolarity.Neutral,
                    context.SelfEntityId,
                    desiredEntityId.Value,
                    1f);
            }
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
        IWorldState worldState,
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
    IWorldState WorldState { get; }
}
