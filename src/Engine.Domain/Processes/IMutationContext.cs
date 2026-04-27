namespace Engine.Processes;

using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Resources;

/// <summary>
/// Context provided to preconditions and mutations for accessing and modifying world state.
/// Used by data-driven behavior code that evaluates preconditions or executes mutations.
/// </summary>
public interface IMutationContext
{
    int CurrentTick { get; }

    /// <summary>The entity currently being evaluated (for self-referencing mutations).</summary>
    EntityId SelfEntityId { get; }

    /// <summary>Shared RNG for variance in mutations and score expressions.</summary>
    Random Random { get; }

    /// <summary>Target entity resolved by the evaluator's process-provider selection.
    /// Set before mutations execute. Null when no provider target.</summary>
    EntityId? ResolvedTargetId { get; }

    /// <summary>Set a named variable for use by subsequent mutations in this process execution.</summary>
    void SetVariable(string name, float value);

    /// <summary>Get a named variable set by a prior mutation. Returns 0 if not set.</summary>
    float GetVariable(string name);

    // --- Read operations (preconditions + scoring) ---
    float GetPressure(string pressureName, EntityId scopeId);
    float GetPressure(PressureRuntimeId pressureId, EntityId scopeId) =>
        throw PrimitiveRuntimeIdNotSupported(nameof(GetPressure));
    float GetAttribute(EntityId entityId, string attributeName);
    float GetAttribute(EntityId entityId, AttributeRuntimeId attributeId) =>
        throw PrimitiveRuntimeIdNotSupported(nameof(GetAttribute));
    int CountEntitiesByKind(EntityKind kind);
    Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind);
    Relationship? FindRelationship(EntityId sourceId, EntityId targetId, RelationshipKindRuntimeId kind) =>
        throw PrimitiveRuntimeIdNotSupported(nameof(FindRelationship));
    int? GetLastFiredTick();
    Entity? GetEntity(EntityId id);

    /// <summary>Find the target of an exclusive relationship from a source entity.
    /// e.g., FindRelatedEntity(npcId, "located_at") returns the settlement the NPC is at.
    /// Returns null if no such relationship exists.</summary>
    EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind);
    EntityId? FindRelatedEntity(EntityId sourceId, RelationshipKindRuntimeId relationshipKind) =>
        throw PrimitiveRuntimeIdNotSupported(nameof(FindRelatedEntity));

    /// <summary>Get all active entity IDs of a given kind.</summary>
    IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind);

    /// <summary>Read the ambient resource value at the given hex coordinates.</summary>
    float GetResource((int Q, int R) coordinates, ResourceType resourceType);

    // --- Write operations (mutations) ---
    Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates);
    void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength);
    void CreateRelationship(
        RelationshipKindRuntimeId kind,
        RelationshipPolarity polarity,
        EntityId sourceId,
        EntityId targetId,
        float strength) =>
        throw PrimitiveRuntimeIdNotSupported(nameof(CreateRelationship));
    void ModifyAttribute(EntityId entityId, string attributeName, float delta);
    void ModifyAttribute(EntityId entityId, AttributeRuntimeId attributeId, float delta) =>
        throw PrimitiveRuntimeIdNotSupported(nameof(ModifyAttribute));
    void ModifyPressure(string pressureName, EntityId scopeId, float delta);
    void ModifyPressure(PressureRuntimeId pressureId, EntityId scopeId, float delta) =>
        throw PrimitiveRuntimeIdNotSupported(nameof(ModifyPressure));
    void ModifyResource((int Q, int R) coordinates, ResourceType resourceType, float delta);
    void SetEntityHistorical(EntityId entityId);

    /// <summary>Consume resource at hex coordinates. Returns actual amount consumed (may be less than requested).</summary>
    float ConsumeResource((int Q, int R) coordinates, ResourceType resourceType, float amount);

    /// <summary>Update an entity's hex position.</summary>
    void SetCoordinates(EntityId entityId, int q, int r);

    /// <summary>Update an entity's semantic-plane position.</summary>
    void SetSemanticCoordinates(EntityId entityId, int q, int r);

    /// <summary>Cull an exclusive relationship from a source entity (e.g., NPC leaving a settlement).</summary>
    void CullRelationship(EntityId sourceId, string relationshipKind);
    void CullRelationship(EntityId sourceId, RelationshipKindRuntimeId relationshipKind) =>
        throw PrimitiveRuntimeIdNotSupported(nameof(CullRelationship));

    private static NotSupportedException PrimitiveRuntimeIdNotSupported(string operation) =>
        new($"{operation} by runtime ID requires a kernel mutation context bound to domain runtime IDs.");
}

/// <summary>
/// Optional context capability for callers that only need boolean precondition
/// results and can skip expensive diagnostic/blocker aggregation.
/// </summary>
public interface IPreconditionDiagnosticContext
{
    bool CapturePreconditionDiagnostics { get; }
    bool CapturePreconditionBlockers { get; }
}
