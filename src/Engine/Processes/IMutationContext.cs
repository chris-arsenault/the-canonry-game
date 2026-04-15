namespace Engine.Processes;

using Engine.Graph;
using Engine.Kinds;
using Engine.Resources;

/// <summary>
/// Context provided to preconditions and mutations for accessing and modifying world state.
/// Used by processes, decisions, and cards — any code that evaluates preconditions or executes mutations.
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
    float GetAttribute(EntityId entityId, string attributeName);
    int CountEntitiesByKind(EntityKind kind);
    Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind);
    int? GetLastFiredTick();
    Entity? GetEntity(EntityId id);

    /// <summary>Find the target of an exclusive relationship from a source entity.
    /// e.g., FindRelatedEntity(npcId, "located_at") returns the settlement the NPC is at.
    /// Returns null if no such relationship exists.</summary>
    EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind);

    /// <summary>Get all active entity IDs of a given kind.</summary>
    IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind);

    /// <summary>Read resource stockpile at the given hex coordinates.</summary>
    float GetResource((int Q, int R) coordinates, ResourceType resourceType);

    // --- Write operations (mutations) ---
    Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates);
    void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength);
    void ModifyAttribute(EntityId entityId, string attributeName, float delta);
    void ModifyPressure(string pressureName, EntityId scopeId, float delta);
    void ModifyResource((int Q, int R) coordinates, ResourceType resourceType, float delta);
    void SetTag(EntityId entityId, string key, string value);
    void SetEntityHistorical(EntityId entityId);

    /// <summary>Consume resource at hex coordinates. Returns actual amount consumed (may be less than requested).</summary>
    float ConsumeResource((int Q, int R) coordinates, ResourceType resourceType, float amount);

    /// <summary>Update an entity's hex position.</summary>
    void SetCoordinates(EntityId entityId, int q, int r);

    /// <summary>Cull an exclusive relationship from a source entity (e.g., NPC leaving a settlement).</summary>
    void CullRelationship(EntityId sourceId, string relationshipKind);
}
