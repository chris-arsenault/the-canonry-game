namespace Engine.Processes;

using System.Globalization;
using System.Runtime.CompilerServices;

using Engine.Graph;
using Engine.Kinds;
using Engine.Resources;
using Engine.Wiring;
using static Engine.Processes.PreconditionDiagnostics;

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
        ArgumentNullException.ThrowIfNull(context);

        var captureDiagnostics = context is not IPreconditionDiagnosticContext diagnosticContext ||
            diagnosticContext.CapturePreconditionDiagnostics;
        var captureBlockers = context is not IPreconditionDiagnosticContext blockerContext ||
            blockerContext.CapturePreconditionBlockers;

        if (!captureDiagnostics && !captureBlockers)
        {
            foreach (var child in Children)
            {
                if (!child.Evaluate(context).Passed)
                {
                    return PreconditionResult.Fail("condition failed");
                }
            }

            return PreconditionResult.Pass("all conditions met");
        }

        List<AttributeRef>? allBlockers = captureBlockers ? [] : null;
        List<string>? allDiagnostics = captureDiagnostics ? [] : null;
        var allPassed = true;

        foreach (var child in Children)
        {
            var result = child.Evaluate(context);
            if (!result.Passed)
            {
                allPassed = false;
                allDiagnostics?.Add(result.Diagnostic);
                if (allBlockers is not null && result.Blockers.Count > 0)
                {
                    allBlockers.AddRange(result.Blockers);
                }
            }
        }

        return allPassed
            ? PreconditionResult.Pass("all conditions met")
            : PreconditionResult.Fail(
                allDiagnostics is null ? "condition failed" : string.Join("; ", allDiagnostics),
                allBlockers is null || allBlockers.Count == 0 ? [] : allBlockers.Distinct().ToList());
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
            return PreconditionResult.Fail(Diagnostic(context, $"{PressureName}={value:F1} (need >= {Min})"), blocker);
        if (Max.HasValue && value > Max.Value)
            return PreconditionResult.Fail(Diagnostic(context, $"{PressureName}={value:F1} (need <= {Max})"), blocker);
        return PreconditionResult.Pass(Diagnostic(context, $"{PressureName}={value:F1}"));
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
            return PreconditionResult.Fail(Diagnostic(context, $"{AttributeName}={value:F1} (need >= {Min})"), blockers);
        if (Max.HasValue && value > Max.Value)
            return PreconditionResult.Fail(Diagnostic(context, $"{AttributeName}={value:F1} (need <= {Max})"), blockers);
        return PreconditionResult.Pass(Diagnostic(context, $"{AttributeName}={value:F1}"));
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
            ? PreconditionResult.Pass(Diagnostic(context, $"{Kind} count={count}"))
            : PreconditionResult.Fail(Diagnostic(context, $"{Kind} count={count} (need >= {Min})"));
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
        if (rel is null) return PreconditionResult.Fail(Diagnostic(context, $"no {RelationshipKind} relationship"));
        if (MinStrength.HasValue && rel.Strength < MinStrength.Value)
            return PreconditionResult.Fail(Diagnostic(context, $"{RelationshipKind} strength={rel.Strength:F2} (need >= {MinStrength})"));
        return PreconditionResult.Pass(Diagnostic(context, $"{RelationshipKind} exists (strength={rel.Strength:F2})"));
    }

    public string Describe() =>
        $"relationship({SourceId}->{TargetId}, {RelationshipKind}{(MinStrength.HasValue ? $", min_strength={MinStrength}" : "")})";
}

/// <summary>
/// Checks that the resolved target has a named relationship back to self.
/// Example: target settlement --controlled_by--> self faction.
/// </summary>
public sealed class TargetHasRelationshipToSelf(string relationshipKind, float? minStrength = null, bool plannerRelevant = false) : IPrecondition
{
    public string RelationshipKind { get; } = relationshipKind;
    public float? MinStrength { get; } = minStrength;
    public bool PlannerRelevant { get; } = plannerRelevant;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var targetEntity = context.ResolvedTargetId is not null
            ? context.GetEntity(context.ResolvedTargetId.Value)
            : null;
        IReadOnlyList<AttributeRef> blockers = PlannerRelevant && targetEntity is not null
            ? [AttributeRef.Target(RelationshipKind, targetEntity.Kind)]
            : Array.Empty<AttributeRef>();

        if (context.ResolvedTargetId is null)
            return PreconditionResult.Fail("no target", blockers);

        var relationship = context.FindRelationship(context.ResolvedTargetId.Value, context.SelfEntityId, RelationshipKind);
        if (relationship is null)
            return PreconditionResult.Fail(Diagnostic(context, $"target has no {RelationshipKind} relationship to self"), blockers);
        if (MinStrength.HasValue && relationship.Strength < MinStrength.Value)
            return PreconditionResult.Fail(Diagnostic(context, $"{RelationshipKind} strength={relationship.Strength:F2} (need >= {MinStrength})"), blockers);

        return PreconditionResult.Pass(Diagnostic(context, $"target {RelationshipKind} self (strength={relationship.Strength:F2})"));
    }

    public string Describe() =>
        $"target_has_relationship_to_self({RelationshipKind}{(MinStrength.HasValue ? $", min_strength={MinStrength}" : "")})";
}

/// <summary>
/// Checks that self has a named relationship to the resolved target.
/// Example: self npc --loyal_to--> target faction.
/// </summary>
public sealed class SelfHasRelationshipToTarget(string relationshipKind, float? minStrength = null, bool plannerRelevant = false) : IPrecondition
{
    public string RelationshipKind { get; } = relationshipKind;
    public float? MinStrength { get; } = minStrength;
    public bool PlannerRelevant { get; } = plannerRelevant;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var targetEntity = context.ResolvedTargetId is not null
            ? context.GetEntity(context.ResolvedTargetId.Value)
            : null;
        IReadOnlyList<AttributeRef> blockers = PlannerRelevant && targetEntity is not null
            ? [AttributeRef.Target(RelationshipKind, targetEntity.Kind)]
            : Array.Empty<AttributeRef>();

        if (context.ResolvedTargetId is null)
            return PreconditionResult.Fail("no target", blockers);

        var relationship = context.FindRelationship(context.SelfEntityId, context.ResolvedTargetId.Value, RelationshipKind);
        if (relationship is null)
            return PreconditionResult.Fail(Diagnostic(context, $"self has no {RelationshipKind} relationship to target"), blockers);
        if (MinStrength.HasValue && relationship.Strength < MinStrength.Value)
            return PreconditionResult.Fail(Diagnostic(context, $"{RelationshipKind} strength={relationship.Strength:F2} (need >= {MinStrength})"), blockers);

        return PreconditionResult.Pass(Diagnostic(context, $"self {RelationshipKind} target (strength={relationship.Strength:F2})"));
    }

    public string Describe() =>
        $"self_has_relationship_to_target({RelationshipKind}{(MinStrength.HasValue ? $", min_strength={MinStrength}" : "")})";
}

/// <summary>
/// Checks whether the entity reached the resolved target through a named relationship.
/// Example: self.located_at == resolved target settlement.
/// TODO: Generalize this relation-backed arrival check into a target-agnostic presence
/// fact before adding more target-specific movement chains. The planner should converge
/// on one reusable "I can act on this target now" abstraction, not a growing family of
/// per-domain relationship blockers.
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
            return PreconditionResult.Fail(Diagnostic(context, $"no {RelationshipKind} relationship to target"), blockers);
        if (relatedEntityId != context.ResolvedTargetId)
            return PreconditionResult.Fail(Diagnostic(context, $"{RelationshipKind} points to {relatedEntity?.Name ?? relatedEntityId.Value.ToString()} (need target)"), blockers);

        return PreconditionResult.Pass(Diagnostic(context, $"{RelationshipKind} matches target"));
    }

    public string Describe() => $"related_matches_target({RelationshipKind})";
}

/// <summary>
/// Checks an ambient resource value at self or target location against a threshold.
/// Planner-relevant variants emit a resource blocker so other processes can supply it.
/// </summary>
public sealed class LocationResourceThreshold(
    ResourceType resourceType,
    float? min,
    float? max,
    bool targetLocation = false,
    bool plannerRelevant = true) : IPrecondition
{
    public ResourceType ResourceType { get; } = resourceType;
    public float? Min { get; } = min;
    public float? Max { get; } = max;
    public bool TargetLocation { get; } = targetLocation;
    public bool PlannerRelevant { get; } = plannerRelevant;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var entityId = TargetLocation ? context.ResolvedTargetId : context.SelfEntityId;
        var entity = entityId is not null ? context.GetEntity(entityId.Value) : null;
        IReadOnlyList<AttributeRef> blockers = PlannerRelevant
            ? [AttributeRef.Resource(ResourceType.ToDomainName())]
            : Array.Empty<AttributeRef>();

        if (entityId is null)
            return PreconditionResult.Fail("no target", blockers);
        if (entity?.Coordinates is null)
            return PreconditionResult.Fail(TargetLocation ? "target has no location" : "self has no location", blockers);

        var value = context.GetResource(entity.Coordinates, ResourceType);
        var prefix = TargetLocation ? "target_location" : "self_location";
        if (Min.HasValue && value < Min.Value)
            return PreconditionResult.Fail(Diagnostic(context, $"{prefix}.{ResourceType.ToDomainName()}={value:F1} (need >= {Min})"), blockers);
        if (Max.HasValue && value > Max.Value)
            return PreconditionResult.Fail(Diagnostic(context, $"{prefix}.{ResourceType.ToDomainName()}={value:F1} (need <= {Max})"), blockers);
        return PreconditionResult.Pass(Diagnostic(context, $"{prefix}.{ResourceType.ToDomainName()}={value:F1}"));
    }

    public string Describe() =>
        $"{(TargetLocation ? "target" : "self")}_location_resource({ResourceType.ToDomainName()}, {(Min.HasValue ? $"min={Min}" : "")}{(Max.HasValue ? $"max={Max}" : "")})";
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
            ? PreconditionResult.Pass(Diagnostic(context, $"cooldown elapsed ({elapsed} ticks)"))
            : PreconditionResult.Fail(Diagnostic(context, $"cooldown: {elapsed}/{minimumTicksSinceFire} ticks"));
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
            return PreconditionResult.Fail(Diagnostic(context, $"self.{AttributeName}={value:F1} (need >= {Min})"), blockers);
        if (Max.HasValue && value > Max.Value)
            return PreconditionResult.Fail(Diagnostic(context, $"self.{AttributeName}={value:F1} (need <= {Max})"), blockers);
        return PreconditionResult.Pass(Diagnostic(context, $"self.{AttributeName}={value:F1}"));
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
            return PreconditionResult.Fail(Diagnostic(context, $"target.{AttributeName}={value:F1} (need >= {Min})"), blockers);
        if (Max.HasValue && value > Max.Value)
            return PreconditionResult.Fail(Diagnostic(context, $"target.{AttributeName}={value:F1} (need <= {Max})"), blockers);
        return PreconditionResult.Pass(Diagnostic(context, $"target.{AttributeName}={value:F1}"));
    }

    public string Describe() =>
        $"target.{AttributeName} {(Min.HasValue ? $">= {Min}" : "")} {(Max.HasValue ? $"<= {Max}" : "")}";
}

/// <summary>Target must be the entity itself.</summary>
public sealed class TargetIsSelf : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.ResolvedTargetId == context.SelfEntityId
            ? PreconditionResult.Pass(Diagnostic(context, "target is self"))
            : PreconditionResult.Fail(Diagnostic(context, "target is not self"));
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
            return PreconditionResult.Fail(Diagnostic(context, "no target"));
        return context.ResolvedTargetId != context.SelfEntityId
            ? PreconditionResult.Pass(Diagnostic(context, "target is not self"))
            : PreconditionResult.Fail(Diagnostic(context, "target is self"));
    }

    public string Describe() => "target != self";
}

/// <summary>Always passes.</summary>
public sealed class AlwaysTrue : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context) => PreconditionResult.Pass("always");
    public string Describe() => "always_true";
}

/// <summary>
/// Target must be within the given hex distance on the specified plane. Physical
/// plane uses map hex coordinates (Entity.Coordinates); semantic plane uses
/// Entity.SemanticPosition on the autonomy/resonance/tradition axes. When the
/// check fails, emits a structural-fact blocker (<see cref="InRangeFactName"/>)
/// so the planner can route to the owner kind's mobility verb as a sub-goal.
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

    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var self = context.GetEntity(context.SelfEntityId);
        var blocker = AttributeRef.StructuralFact(InRangeFactName, self?.Kind);

        if (context.ResolvedTargetId is null)
        {
            return PreconditionResult.Fail("no target", blocker);
        }

        var target = context.GetEntity(context.ResolvedTargetId.Value);
        if (self is null || target is null)
        {
            return PreconditionResult.Fail("missing entity", blocker);
        }

        int distance;
        string planeLabel;
        if (Plane == Engine.Domain.Config.PrimaryPlane.Physical)
        {
            var a = new Map.HexCoord(self.Coordinates.Q, self.Coordinates.R);
            var b = new Map.HexCoord(target.Coordinates.Q, target.Coordinates.R);
            distance = Map.HexCoord.Distance(a, b);
            planeLabel = "physical";
        }
        else
        {
            distance = Map.SemanticHexCoord.Distance(self.SemanticPosition, target.SemanticPosition);
            planeLabel = "semantic";
        }

        return distance <= MaxDistance
            ? PreconditionResult.Pass(Diagnostic(context, $"{planeLabel}_distance={distance} (≤ {MaxDistance})"))
            : PreconditionResult.Fail(Diagnostic(context, $"{planeLabel}_distance={distance} (need ≤ {MaxDistance})"), blocker);
    }

    public string Describe() =>
        Plane == Engine.Domain.Config.PrimaryPlane.Physical
            ? $"physical_distance ≤ {MaxDistance}"
            : $"semantic_distance ≤ {MaxDistance}";
}

/// <summary>Inverts the result of a wrapped precondition.</summary>
public sealed class NotPrecondition(IPrecondition inner) : IPrecondition
{
    public IPrecondition Inner { get; } = inner;

    public PreconditionResult Evaluate(IMutationContext context)
    {
        var result = Inner.Evaluate(context);
        return result.Passed
            ? PreconditionResult.Fail(Diagnostic(context, $"not({result.Diagnostic})"))
            : PreconditionResult.Pass(Diagnostic(context, $"not({result.Diagnostic})"));
    }

    public string Describe() => $"not({Inner.Describe()})";
}

/// <summary>
/// Self's hex must contain at least one other named entity. Used to gate hex→Health
/// verbs ("must happen at a named entity, not in empty wilderness").
/// </summary>
public sealed class HasColocatedNamedEntityPrecondition : IPrecondition
{
    public PreconditionResult Evaluate(IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not IWorldStateAccessor accessor) return PreconditionResult.Fail("no world state");

        var self = context.GetEntity(context.SelfEntityId);
        if (self is null) return PreconditionResult.Fail("self has no location");

        var coord = new Map.HexCoord(self.Coordinates.Q, self.Coordinates.R);
        var hasOther = accessor.WorldState.SpatialIndex.EntitiesAtCell(coord)
            .Any(id => id != context.SelfEntityId);

        return hasOther
            ? PreconditionResult.Pass("co-located with named entity")
            : PreconditionResult.Fail("no other named entity at hex");
    }

    public string Describe() => "has_colocated_named_entity";
}

file static class PreconditionDiagnostics
{
    public static string Diagnostic(
        IMutationContext context,
        [InterpolatedStringHandlerArgument(nameof(context))] PreconditionDiagnosticStringHandler diagnostic) =>
        diagnostic.GetText();

    public static string Diagnostic(IMutationContext context, string diagnostic) =>
        CaptureDiagnostics(context) ? diagnostic : string.Empty;

    private static bool CaptureDiagnostics(IMutationContext context) =>
        context is not IPreconditionDiagnosticContext diagnosticContext ||
        diagnosticContext.CapturePreconditionDiagnostics;

    [InterpolatedStringHandler]
    public ref struct PreconditionDiagnosticStringHandler
    {
        private DefaultInterpolatedStringHandler _inner;
        private readonly bool _enabled;

        public PreconditionDiagnosticStringHandler(
            int literalLength,
            int formattedCount,
            IMutationContext context,
            out bool shouldAppend)
        {
            _enabled = CaptureDiagnostics(context);
            shouldAppend = _enabled;
            _inner = _enabled
                ? new DefaultInterpolatedStringHandler(
                    literalLength,
                    formattedCount,
                    CultureInfo.InvariantCulture)
                : default;
        }

        public void AppendLiteral(string value)
        {
            if (_enabled)
            {
                _inner.AppendLiteral(value);
            }
        }

        public void AppendFormatted<T>(T value)
        {
            if (_enabled)
            {
                _inner.AppendFormatted(value);
            }
        }

        public void AppendFormatted<T>(T value, string? format)
        {
            if (_enabled)
            {
                _inner.AppendFormatted(value, format);
            }
        }

        public void AppendFormatted(string? value)
        {
            if (_enabled)
            {
                _inner.AppendFormatted(value);
            }
        }

        public string GetText() => _enabled ? _inner.ToStringAndClear() : string.Empty;
    }
}
