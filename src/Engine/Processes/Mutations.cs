namespace Engine.Processes;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;

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
/// e.g., NPC eats → target settlement loses sustenance.
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
    string? component = null, float variance = 0f) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var delta = Variance.Scale(context.GetVariable(variableName), variance, context.Random);
        var entity = context.GetEntity(context.SelfEntityId);
        if (entity is null) return;

        MutationHelper.ModifyByComponent(entity, attributeName, component, delta);
    }

    public string Describe() => $"modify_self_by_var({attributeName}.{component ?? "value"}, ${variableName}, var={variance})";
}

/// <summary>
/// Modifies target attribute by the value of a named context variable.
/// </summary>
public sealed class ModifyTargetByVariableMutation(string attributeName, string variableName,
    string? component = null, float variance = 0f) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ResolvedTargetId is null) return;

        var delta = Variance.Scale(context.GetVariable(variableName), variance, context.Random);
        var entity = context.GetEntity(context.ResolvedTargetId.Value);
        if (entity is null) return;

        MutationHelper.ModifyByComponent(entity, attributeName, component, delta);
    }

    public string Describe() => $"modify_target_by_var({attributeName}.{component ?? "value"}, ${variableName}, var={variance})";
}

/// <summary>
/// Moves self one step along a path toward the resolved target.
/// Optionally maintains an exclusive location-style relationship such as located_at.
/// TODO: This is the first reusable movement primitive, not the final planner shape.
/// Keep future work focused on generalizing the arrival/presence fact rather than
/// proliferating target-specific movement mutations.
/// </summary>
public sealed class MoveTowardTargetMutation(
    string? locationRelationshipKind = "located_at",
    EntityKind? locationEntityKind = EntityKind.Settlement) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not IWorldStateAccessor accessor) return;
        if (context.ResolvedTargetId is null) return;

        var self = context.GetEntity(context.SelfEntityId);
        var target = context.GetEntity(context.ResolvedTargetId.Value);
        if (self?.Coordinates is null || target?.Coordinates is null) return;

        var start = new HexCoord(self.Coordinates.Value.Q, self.Coordinates.Value.R);
        var destination = new HexCoord(target.Coordinates.Value.Q, target.Coordinates.Value.R);

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
/// Optionally maintains an exclusive location-style relationship such as located_at.
/// </summary>
public sealed class WanderMutation(
    string? locationRelationshipKind = "located_at",
    EntityKind? locationEntityKind = EntityKind.Settlement) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not IWorldStateAccessor accessor) return;

        var self = context.GetEntity(context.SelfEntityId);
        if (self?.Coordinates is null) return;

        var current = new HexCoord(self.Coordinates.Value.Q, self.Coordinates.Value.R);
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

/// <summary>Helper to modify an attribute by component (scalar, x, or y).</summary>
internal static class MutationHelper
{
    internal static void ModifyByComponent(
        Graph.Entity entity, string attributeName, string? component, float delta)
    {
        var attr = entity.Attributes[attributeName];
        if (attr is Attributes.SemanticPlane plane && component is not null)
        {
            if (component is "x") plane.ModifyX(delta);
            else if (component is "y") plane.ModifyY(delta);
        }
        else if (attr is Attributes.ScalarValue scalar)
        {
            scalar.Modify(delta);
        }
    }
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
        if (string.IsNullOrWhiteSpace(relationshipKind) || locationEntityKind is null)
        {
            return;
        }

        var self = context.GetEntity(context.SelfEntityId);
        if (self?.Coordinates is null)
        {
            return;
        }

        var currentRelatedEntityId = context.FindRelatedEntity(context.SelfEntityId, relationshipKind);
        var coord = new HexCoord(self.Coordinates.Value.Q, self.Coordinates.Value.R);
        var desiredEntityId = ResolveLocationEntityAtCell(
            worldState,
            coord,
            locationEntityKind.Value,
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
