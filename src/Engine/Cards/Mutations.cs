namespace Engine.Cards;

using Engine.Graph;
using Engine.Kinds;
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
/// Step one hex toward the ResolvedTarget on the context. If no target,
/// step in a random direction (wander). Pure locomotion — no domain knowledge.
/// Uses A* pathfinding via IWorldStateAccessor. Manages located_at relationship.
/// </summary>
public sealed class StepTowardTargetMutation : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var entity = context.GetEntity(context.SelfEntityId);
        if (entity?.Coordinates is null) return;

        var currentCoord = new Map.HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
        Map.HexCoord nextHex;

        if (context.ResolvedTarget is not null)
        {
            var targetCoord = new Map.HexCoord(context.ResolvedTarget.Value.Q, context.ResolvedTarget.Value.R);
            if (targetCoord == currentCoord) return; // Already there

            // Need SpatialIndex for A* — access through WorldState
            if (context is not IWorldStateAccessor worldAccessor) return;

            var path = worldAccessor.WorldState.SpatialIndex.FindPath(currentCoord, targetCoord);
            if (path is null || path.Count < 2) return;

            nextHex = path[1]; // path[0] is current
        }
        else
        {
            // No target — wander randomly
            var direction = context.Random.Next(6);
            nextHex = currentCoord.Neighbor(direction);
        }

        // Validate the hex is walkable
        if (context is IWorldStateAccessor wa)
        {
            var cell = wa.WorldState.Grid.GetCell(nextHex);
            if (cell is null || float.IsPositiveInfinity(cell.MovementCost)) return;
        }

        // Leave current settlement
        var currentLocation = context.FindRelatedEntity(context.SelfEntityId, "located_at");
        if (currentLocation is not null)
        {
            context.CullRelationship(context.SelfEntityId, "located_at");
        }

        // Step
        context.SetCoordinates(context.SelfEntityId, nextHex.Q, nextHex.R);

        // Arrive at new settlement?
        if (context is IWorldStateAccessor wa2)
        {
            foreach (var sid in wa2.WorldState.Graph.GetEntitiesByKind(Kinds.EntityKind.Settlement))
            {
                var settlement = wa2.WorldState.Graph.GetEntity(sid);
                if (settlement?.Coordinates is not null &&
                    settlement.Coordinates.Value.Q == nextHex.Q &&
                    settlement.Coordinates.Value.R == nextHex.R)
                {
                    context.CreateRelationship("located_at", Graph.RelationshipPolarity.Neutral,
                        context.SelfEntityId, sid, 1f);
                    break;
                }
            }
        }
    }

    public string Describe() => "step_toward_target(A* + wander fallback)";
}

/// <summary>
/// Marker interface: the mutation context can provide direct IWorldState access.
/// Used by StepTowardTargetMutation for A* pathfinding and settlement detection.
/// </summary>
public interface IWorldStateAccessor
{
    Behaviors.IWorldState WorldState { get; }
}

/// <summary>
/// Consume a resource at the current entity's hex coordinates and reduce an attribute by
/// the amount consumed. Generic: works with any resource type and any attribute.
/// e.g., consume sustenance at my hex, reduce my hunger by amount consumed.
/// </summary>
public sealed class ConsumeResourceAtSelfMutation(
    ResourceType resourceType, float amount, string attributeToReduce, float variance = 0f) : IMutation
{
    public void Execute(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var entity = context.GetEntity(context.SelfEntityId);
        if (entity?.Coordinates is null) return;

        var toConsume = Variance.Scale(amount, variance, context.Random);
        var consumed = context.ConsumeResource(entity.Coordinates.Value, resourceType, toConsume);

        if (consumed > 0f)
        {
            context.ModifyAttribute(context.SelfEntityId, attributeToReduce, -consumed);
        }
    }

    public string Describe() => $"consume_resource({resourceType}, amount={amount}, reduces={attributeToReduce}, var={variance})";
}
