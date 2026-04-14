namespace Engine.Cards;

using Engine.Graph;
using Engine.Kinds;
using Engine.Resources;

/// <summary>
/// A composable precondition tree node. Evaluates against world state.
/// Leaf nodes check specific conditions. Composite nodes compose with AND/OR.
/// </summary>
public interface IPrecondition
{
    /// <summary>Evaluate this precondition against the current world state.</summary>
    bool Evaluate(IMutationContext context);

    /// <summary>Human-readable description for UI display.</summary>
    string Describe();
}

/// <summary>
/// AND composition: all children must be true.
/// </summary>
public sealed class AndPrecondition(IReadOnlyList<IPrecondition> children) : IPrecondition
{
    public IReadOnlyList<IPrecondition> Children { get; } = children;

    public bool Evaluate(IMutationContext context) =>
        Children.All(c => c.Evaluate(context));

    public string Describe() =>
        $"ALL({string.Join(", ", Children.Select(c => c.Describe()))})";
}

/// <summary>
/// OR composition: at least one child must be true.
/// </summary>
public sealed class OrPrecondition(IReadOnlyList<IPrecondition> children) : IPrecondition
{
    public IReadOnlyList<IPrecondition> Children { get; } = children;

    public bool Evaluate(IMutationContext context) =>
        Children.Any(c => c.Evaluate(context));

    public string Describe() =>
        $"ANY({string.Join(", ", Children.Select(c => c.Describe()))})";
}

// --- Leaf Preconditions ---

/// <summary>Checks a pressure value against a threshold.</summary>
public sealed class PressureThreshold(string pressureName, EntityId scopeId, float? min, float? max) : IPrecondition
{
    public bool Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var value = context.GetPressure(pressureName, scopeId);
        if (min.HasValue && value < min.Value) return false;
        if (max.HasValue && value > max.Value) return false;
        return true;
    }

    public string Describe() =>
        $"pressure({pressureName}, {(min.HasValue ? $"min={min}" : "")}{(max.HasValue ? $"max={max}" : "")})";
}

/// <summary>Checks an entity's attribute value against a threshold.</summary>
public sealed class AttributeThreshold(EntityId entityId, string attributeName, float? min, float? max) : IPrecondition
{
    public bool Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var value = context.GetAttribute(entityId, attributeName);
        if (min.HasValue && value < min.Value) return false;
        if (max.HasValue && value > max.Value) return false;
        return true;
    }

    public string Describe() =>
        $"attribute({entityId}, {attributeName}, {(min.HasValue ? $"min={min}" : "")}{(max.HasValue ? $"max={max}" : "")})";
}

/// <summary>Checks that at least N entities of a kind exist.</summary>
public sealed class EntityCountCheck(EntityKind kind, int min) : IPrecondition
{
    public bool Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.CountEntitiesByKind(kind) >= min;
    }

    public string Describe() => $"entity_count({kind}, min={min})";
}

/// <summary>Checks that a relationship of a specific kind exists between two entities.</summary>
public sealed class RelationshipExists(
    EntityId sourceId, EntityId targetId, string relationshipKind, float? minStrength) : IPrecondition
{
    public bool Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rel = context.FindRelationship(sourceId, targetId, relationshipKind);
        if (rel is null) return false;
        if (minStrength.HasValue && rel.Strength < minStrength.Value) return false;
        return true;
    }

    public string Describe() =>
        $"relationship({sourceId}->{targetId}, {relationshipKind}{(minStrength.HasValue ? $", min_strength={minStrength}" : "")})";
}

/// <summary>Checks that enough ticks have passed since a card last fired.</summary>
public sealed class CooldownCheck(int minimumTicksSinceFire) : IPrecondition
{
    public int MinimumTicksSinceFire => minimumTicksSinceFire;

    public bool Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var lastFired = context.GetLastFiredTick();
        if (lastFired is null) return true; // Never fired = ready
        return (context.CurrentTick - lastFired.Value) >= minimumTicksSinceFire;
    }

    public string Describe() => $"cooldown(min_ticks={minimumTicksSinceFire})";
}

/// <summary>Always true. Used for system cards that run unconditionally.</summary>
public sealed class AlwaysTrue : IPrecondition
{
    public bool Evaluate(IMutationContext context) => true;
    public string Describe() => "always_true";
}

/// <summary>Checks that a resource is available at the entity's current hex coordinates.</summary>
public sealed class ResourceAvailableAtSelf(Resources.ResourceType resourceType, float minAmount = 0.01f) : IPrecondition
{
    public bool Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var entity = context.GetEntity(context.SelfEntityId);
        if (entity?.Coordinates is null) return false;
        var amount = context.GetResource(entity.Coordinates.Value, resourceType);
        return amount >= minAmount;
    }

    public string Describe() => $"resource_available({resourceType}, min={minAmount})";
}
