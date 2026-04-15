namespace Engine.Processes;

using Engine.Graph;
using Engine.Kinds;
using Engine.Wiring;

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
        var allBlockers = new List<AttributeRef>();
        var allDiagnostics = new List<string>();
        var allPassed = true;

        foreach (var child in Children)
        {
            var result = child.Evaluate(context);
            if (!result.Passed)
            {
                allPassed = false;
                allDiagnostics.Add(result.Diagnostic);
                allBlockers.AddRange(result.Blockers);
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
public sealed class PressureThreshold(string pressureName, EntityId scopeId, float? min, float? max, bool plannerRelevant = false) : IPrecondition
{
    public string PressureName { get; } = pressureName;
    public EntityId ScopeId { get; } = scopeId;
    public float? Min { get; } = min;
    public float? Max { get; } = max;
    public bool PlannerRelevant { get; } = plannerRelevant;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var value = context.GetPressure(PressureName, ScopeId);
        IReadOnlyList<AttributeRef> blocker = PlannerRelevant
            ? [AttributeRef.Pressure(PressureName)]
            : Array.Empty<AttributeRef>();
        if (Min.HasValue && value < Min.Value)
            return PreconditionResult.Fail($"{PressureName}={value:F1} (need >= {Min})", blocker);
        if (Max.HasValue && value > Max.Value)
            return PreconditionResult.Fail($"{PressureName}={value:F1} (need <= {Max})", blocker);
        return PreconditionResult.Pass($"{PressureName}={value:F1}");
    }

    public string Describe() =>
        $"pressure({PressureName}, {(Min.HasValue ? $"min={Min}" : "")}{(Max.HasValue ? $"max={Max}" : "")})";
}

/// <summary>Checks an entity's attribute value against a threshold.</summary>
public sealed class AttributeThreshold(EntityId entityId, string attributeName, float? min, float? max, bool plannerRelevant = true) : IPrecondition
{
    public EntityId EntityId { get; } = entityId;
    public string AttributeName { get; } = attributeName;
    public float? Min { get; } = min;
    public float? Max { get; } = max;
    public bool PlannerRelevant { get; } = plannerRelevant;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var entity = context.GetEntity(EntityId);
        var blocker = new AttributeRef(RefScope.Self, AttributeName, entity?.Kind);
        IReadOnlyList<AttributeRef> blockers = PlannerRelevant
            ? [blocker]
            : Array.Empty<AttributeRef>();
        var value = context.GetAttribute(EntityId, AttributeName);
        if (Min.HasValue && value < Min.Value)
            return PreconditionResult.Fail($"{AttributeName}={value:F1} (need >= {Min})", blockers);
        if (Max.HasValue && value > Max.Value)
            return PreconditionResult.Fail($"{AttributeName}={value:F1} (need <= {Max})", blockers);
        return PreconditionResult.Pass($"{AttributeName}={value:F1}");
    }

    public string Describe() =>
        $"attribute({EntityId}, {AttributeName}, {(Min.HasValue ? $"min={Min}" : "")}{(Max.HasValue ? $"max={Max}" : "")})";
}

/// <summary>Checks that at least N entities of a kind exist.</summary>
public sealed class EntityCountCheck(EntityKind kind, int min) : IPrecondition
{
    public EntityKind Kind { get; } = kind;
    public int Min { get; } = min;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var count = context.CountEntitiesByKind(Kind);
        return count >= Min
            ? PreconditionResult.Pass($"{Kind} count={count}")
            : PreconditionResult.Fail($"{Kind} count={count} (need >= {Min})");
    }

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

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rel = context.FindRelationship(SourceId, TargetId, RelationshipKind);
        if (rel is null) return PreconditionResult.Fail($"no {RelationshipKind} relationship");
        if (MinStrength.HasValue && rel.Strength < MinStrength.Value)
            return PreconditionResult.Fail($"{RelationshipKind} strength={rel.Strength:F2} (need >= {MinStrength})");
        return PreconditionResult.Pass($"{RelationshipKind} exists (strength={rel.Strength:F2})");
    }

    public string Describe() =>
        $"relationship({SourceId}->{TargetId}, {RelationshipKind}{(MinStrength.HasValue ? $", min_strength={MinStrength}" : "")})";
}

/// <summary>
/// Checks whether the entity reached the resolved target through a named relationship.
/// Example: self.located_at == resolved target settlement.
/// </summary>
public sealed class RelatedEntityMatchesTarget(string relationshipKind, bool plannerRelevant = true) : IPrecondition
{
    public string RelationshipKind { get; } = relationshipKind;
    public bool PlannerRelevant { get; } = plannerRelevant;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var targetEntity = context.ResolvedTargetId is not null
            ? context.GetEntity(context.ResolvedTargetId.Value)
            : null;
        var relatedEntityId = context.FindRelatedEntity(context.SelfEntityId, RelationshipKind);
        var relatedEntity = relatedEntityId is not null
            ? context.GetEntity(relatedEntityId.Value)
            : null;

        var blocker = AttributeRef.TargetRelation(
            RelationshipKind,
            targetEntity?.Kind ?? relatedEntity?.Kind);
        IReadOnlyList<AttributeRef> blockers = PlannerRelevant
            ? [blocker]
            : Array.Empty<AttributeRef>();

        if (context.ResolvedTargetId is null)
            return PreconditionResult.Fail("no target", blockers);
        if (relatedEntityId is null)
            return PreconditionResult.Fail($"no {RelationshipKind} relationship to target", blockers);
        if (relatedEntityId != context.ResolvedTargetId)
            return PreconditionResult.Fail($"{RelationshipKind} points to {relatedEntity?.Name ?? relatedEntityId.Value.ToString()} (need target)", blockers);

        return PreconditionResult.Pass($"{RelationshipKind} matches target");
    }

    public string Describe() => $"related_matches_target({RelationshipKind})";
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
public sealed class SelfAttributeThreshold(string attributeName, float? min, float? max, bool plannerRelevant = true) : IPrecondition
{
    public string AttributeName { get; } = attributeName;
    public float? Min { get; } = min;
    public float? Max { get; } = max;
    public bool PlannerRelevant { get; } = plannerRelevant;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var entity = context.GetEntity(context.SelfEntityId);
        if (entity is null) return PreconditionResult.Fail("entity not found");
        var blocker = AttributeRef.Self(AttributeName, entity.Kind);
        IReadOnlyList<AttributeRef> blockers = PlannerRelevant
            ? [blocker]
            : Array.Empty<AttributeRef>();
        var value = entity.Attributes.GetValue(AttributeName);
        if (Min.HasValue && value < Min.Value)
            return PreconditionResult.Fail($"self.{AttributeName}={value:F1} (need >= {Min})", blockers);
        if (Max.HasValue && value > Max.Value)
            return PreconditionResult.Fail($"self.{AttributeName}={value:F1} (need <= {Max})", blockers);
        return PreconditionResult.Pass($"self.{AttributeName}={value:F1}");
    }

    public string Describe() =>
        $"self.{AttributeName} {(Min.HasValue ? $">= {Min}" : "")} {(Max.HasValue ? $"<= {Max}" : "")}";
}

/// <summary>Checks the resolved target entity's attribute against a threshold.</summary>
public sealed class TargetAttributeThreshold(string attributeName, float? min, float? max, bool plannerRelevant = true) : IPrecondition
{
    public string AttributeName { get; } = attributeName;
    public float? Min { get; } = min;
    public float? Max { get; } = max;
    public bool PlannerRelevant { get; } = plannerRelevant;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var targetEntity = context.ResolvedTargetId is not null
            ? context.GetEntity(context.ResolvedTargetId.Value)
            : null;
        var blocker = AttributeRef.Target(AttributeName, targetEntity?.Kind);
        IReadOnlyList<AttributeRef> blockers = PlannerRelevant
            ? [blocker]
            : Array.Empty<AttributeRef>();
        if (context.ResolvedTargetId is null)
            return PreconditionResult.Fail("no target", blockers);
        if (targetEntity is null) return PreconditionResult.Fail("target not found", blockers);
        var value = targetEntity.Attributes.GetValue(AttributeName);
        if (Min.HasValue && value < Min.Value)
            return PreconditionResult.Fail($"target.{AttributeName}={value:F1} (need >= {Min})", blockers);
        if (Max.HasValue && value > Max.Value)
            return PreconditionResult.Fail($"target.{AttributeName}={value:F1} (need <= {Max})", blockers);
        return PreconditionResult.Pass($"target.{AttributeName}={value:F1}");
    }

    public string Describe() =>
        $"target.{AttributeName} {(Min.HasValue ? $">= {Min}" : "")} {(Max.HasValue ? $"<= {Max}" : "")}";
}

/// <summary>Checks that the semantic distance between self and target is below a threshold.</summary>
public sealed class SemanticDistanceThreshold(string attributeName, float maxDistance, bool plannerRelevant = true) : IPrecondition
{
    public string AttributeName { get; } = attributeName;
    public float MaxDistance { get; } = maxDistance;
    public bool PlannerRelevant { get; } = plannerRelevant;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var targetEntity = context.ResolvedTargetId is not null
            ? context.GetEntity(context.ResolvedTargetId.Value)
            : null;
        var blocker = AttributeRef.Target(AttributeName, targetEntity?.Kind);
        IReadOnlyList<AttributeRef> blockers = PlannerRelevant
            ? [blocker]
            : Array.Empty<AttributeRef>();
        if (context.ResolvedTargetId is null)
            return PreconditionResult.Fail("no target", blockers);
        var selfEntity = context.GetEntity(context.SelfEntityId);
        if (selfEntity is null || targetEntity is null)
            return PreconditionResult.Fail("entity not found", blockers);

        var selfPlane = selfEntity.Attributes.GetPlane(AttributeName);
        var targetPlane = targetEntity.Attributes.GetPlane(AttributeName);
        if (selfPlane is null || targetPlane is null)
            return PreconditionResult.Fail($"{AttributeName} not a semantic plane", blockers);

        var dist = selfPlane.DistanceTo(targetPlane);
        return dist <= MaxDistance
            ? PreconditionResult.Pass($"{AttributeName} distance={dist:F2} (max={MaxDistance})")
            : PreconditionResult.Fail($"{AttributeName} distance={dist:F2} (need <= {MaxDistance})", blockers);
    }

    public string Describe() => $"semantic_distance({AttributeName}) <= {MaxDistance}";
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
        if (context.ResolvedTargetId is null)
            return PreconditionResult.Fail("no target");
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
