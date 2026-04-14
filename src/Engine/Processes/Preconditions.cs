namespace Engine.Processes;

using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// A composable precondition tree node. Evaluates against world state.
/// Returns PreconditionResult with diagnostics and blocking attributes.
/// </summary>
public interface IPrecondition
{
    /// <summary>Evaluate this precondition. Returns rich result with diagnostics
    /// and blocking attribute info for sub-goal generation.</summary>
    PreconditionResult Evaluate(IMutationContext context);

    /// <summary>Human-readable description for UI display.</summary>
    string Describe();
}

/// <summary>AND composition: all children must pass.</summary>
public sealed class AndPrecondition(IReadOnlyList<IPrecondition> children) : IPrecondition
{
    public IReadOnlyList<IPrecondition> Children { get; } = children;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        var allBlockers = new List<string>();
        var allDiagnostics = new List<string>();
        var allPassed = true;

        foreach (var child in Children)
        {
            var result = child.Evaluate(context);
            if (!result.Passed)
            {
                allPassed = false;
                allDiagnostics.Add(result.Diagnostic);
                allBlockers.AddRange(result.BlockingAttributes);
            }
        }

        return allPassed
            ? PreconditionResult.Pass("all conditions met")
            : PreconditionResult.Fail(string.Join("; ", allDiagnostics), allBlockers.Distinct().ToList());
    }

    public string Describe() =>
        $"ALL({string.Join(", ", Children.Select(c => c.Describe()))})";
}

/// <summary>OR composition: at least one child must pass.</summary>
public sealed class OrPrecondition(IReadOnlyList<IPrecondition> children) : IPrecondition
{
    public IReadOnlyList<IPrecondition> Children { get; } = children;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        foreach (var child in Children)
        {
            var result = child.Evaluate(context);
            if (result.Passed) return result;
        }

        return PreconditionResult.Fail("no conditions met");
    }

    public string Describe() =>
        $"ANY({string.Join(", ", Children.Select(c => c.Describe()))})";
}

// --- Leaf Preconditions ---

/// <summary>Checks a pressure value against a threshold.</summary>
public sealed class PressureThreshold(string pressureName, EntityId scopeId, float? min, float? max) : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var value = context.GetPressure(pressureName, scopeId);
        if (min.HasValue && value < min.Value)
            return PreconditionResult.Fail($"{pressureName}={value:F1} (need >= {min})", pressureName);
        if (max.HasValue && value > max.Value)
            return PreconditionResult.Fail($"{pressureName}={value:F1} (need <= {max})", pressureName);
        return PreconditionResult.Pass($"{pressureName}={value:F1}");
    }

    public string Describe() =>
        $"pressure({pressureName}, {(min.HasValue ? $"min={min}" : "")}{(max.HasValue ? $"max={max}" : "")})";
}

/// <summary>Checks an entity's attribute value against a threshold.</summary>
public sealed class AttributeThreshold(EntityId entityId, string attributeName, float? min, float? max) : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var value = context.GetAttribute(entityId, attributeName);
        if (min.HasValue && value < min.Value)
            return PreconditionResult.Fail($"{attributeName}={value:F1} (need >= {min})", attributeName);
        if (max.HasValue && value > max.Value)
            return PreconditionResult.Fail($"{attributeName}={value:F1} (need <= {max})", attributeName);
        return PreconditionResult.Pass($"{attributeName}={value:F1}");
    }

    public string Describe() =>
        $"attribute({entityId}, {attributeName}, {(min.HasValue ? $"min={min}" : "")}{(max.HasValue ? $"max={max}" : "")})";
}

/// <summary>Checks that at least N entities of a kind exist.</summary>
public sealed class EntityCountCheck(EntityKind kind, int min) : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var count = context.CountEntitiesByKind(kind);
        return count >= min
            ? PreconditionResult.Pass($"{kind} count={count}")
            : PreconditionResult.Fail($"{kind} count={count} (need >= {min})");
    }

    public string Describe() => $"entity_count({kind}, min={min})";
}

/// <summary>Checks that a relationship exists between two entities.</summary>
public sealed class RelationshipExists(
    EntityId sourceId, EntityId targetId, string relationshipKind, float? minStrength) : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rel = context.FindRelationship(sourceId, targetId, relationshipKind);
        if (rel is null) return PreconditionResult.Fail($"no {relationshipKind} relationship");
        if (minStrength.HasValue && rel.Strength < minStrength.Value)
            return PreconditionResult.Fail($"{relationshipKind} strength={rel.Strength:F2} (need >= {minStrength})");
        return PreconditionResult.Pass($"{relationshipKind} exists (strength={rel.Strength:F2})");
    }

    public string Describe() =>
        $"relationship({sourceId}->{targetId}, {relationshipKind}{(minStrength.HasValue ? $", min_strength={minStrength}" : "")})";
}

/// <summary>Checks that enough ticks have passed since a card last fired.</summary>
public sealed class CooldownCheck(int minimumTicksSinceFire) : IPrecondition
{
    public int MinimumTicksSinceFire => minimumTicksSinceFire;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var lastFired = context.GetLastFiredTick();
        if (lastFired is null) return PreconditionResult.Pass("never fired");
        var elapsed = context.CurrentTick - lastFired.Value;
        return elapsed >= minimumTicksSinceFire
            ? PreconditionResult.Pass($"cooldown elapsed ({elapsed} ticks)")
            : PreconditionResult.Fail($"cooldown: {elapsed}/{minimumTicksSinceFire} ticks");
    }

    public string Describe() => $"cooldown(min_ticks={minimumTicksSinceFire})";
}

/// <summary>Checks the current entity's own attribute against a threshold.</summary>
public sealed class SelfAttributeThreshold(string attributeName, float? min, float? max) : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var entity = context.GetEntity(context.SelfEntityId);
        if (entity is null) return PreconditionResult.Fail("entity not found");
        var value = entity.Attributes.GetValue(attributeName);
        if (min.HasValue && value < min.Value)
            return PreconditionResult.Fail($"self.{attributeName}={value:F1} (need >= {min})", attributeName);
        if (max.HasValue && value > max.Value)
            return PreconditionResult.Fail($"self.{attributeName}={value:F1} (need <= {max})", attributeName);
        return PreconditionResult.Pass($"self.{attributeName}={value:F1}");
    }

    public string Describe() =>
        $"self.{attributeName} {(min.HasValue ? $">= {min}" : "")} {(max.HasValue ? $"<= {max}" : "")}";
}

/// <summary>Checks the resolved target entity's attribute against a threshold.</summary>
public sealed class TargetAttributeThreshold(string attributeName, float? min, float? max) : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ResolvedTargetId is null)
            return PreconditionResult.Fail("no target", attributeName);
        var entity = context.GetEntity(context.ResolvedTargetId.Value);
        if (entity is null) return PreconditionResult.Fail("target not found", attributeName);
        var value = entity.Attributes.GetValue(attributeName);
        if (min.HasValue && value < min.Value)
            return PreconditionResult.Fail($"target.{attributeName}={value:F1} (need >= {min})", attributeName);
        if (max.HasValue && value > max.Value)
            return PreconditionResult.Fail($"target.{attributeName}={value:F1} (need <= {max})", attributeName);
        return PreconditionResult.Pass($"target.{attributeName}={value:F1}");
    }

    public string Describe() =>
        $"target.{attributeName} {(min.HasValue ? $">= {min}" : "")} {(max.HasValue ? $"<= {max}" : "")}";
}

/// <summary>Checks that the semantic distance between self and target is below a threshold.</summary>
public sealed class SemanticDistanceThreshold(string attributeName, float maxDistance) : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ResolvedTargetId is null)
            return PreconditionResult.Fail("no target", attributeName);
        var selfEntity = context.GetEntity(context.SelfEntityId);
        var targetEntity = context.GetEntity(context.ResolvedTargetId.Value);
        if (selfEntity is null || targetEntity is null)
            return PreconditionResult.Fail("entity not found", attributeName);

        var selfPlane = selfEntity.Attributes.GetPlane(attributeName);
        var targetPlane = targetEntity.Attributes.GetPlane(attributeName);
        if (selfPlane is null || targetPlane is null)
            return PreconditionResult.Fail($"{attributeName} not a semantic plane", attributeName);

        var dist = selfPlane.DistanceTo(targetPlane);
        return dist <= maxDistance
            ? PreconditionResult.Pass($"{attributeName} distance={dist:F2} (max={maxDistance})")
            : PreconditionResult.Fail($"{attributeName} distance={dist:F2} (need <= {maxDistance})", attributeName);
    }

    public string Describe() => $"semantic_distance({attributeName}) <= {maxDistance}";
}

/// <summary>Target must be the entity itself.</summary>
public sealed class TargetIsSelf : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.ResolvedTargetId == context.SelfEntityId
            ? PreconditionResult.Pass("target is self")
            : PreconditionResult.Fail("target is not self");
    }

    public string Describe() => "target == self";
}

/// <summary>Target must NOT be the entity itself.</summary>
public sealed class TargetIsNotSelf : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.ResolvedTargetId != context.SelfEntityId
            ? PreconditionResult.Pass("target is not self")
            : PreconditionResult.Fail("target is self");
    }

    public string Describe() => "target != self";
}

/// <summary>Always passes.</summary>
public sealed class AlwaysTrue : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context) => PreconditionResult.Pass("always");
    public string Describe() => "always_true";
}
