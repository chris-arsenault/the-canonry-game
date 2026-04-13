namespace Engine.Cards;

using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// Context provided to preconditions and mutations for accessing and modifying world state.
/// Abstracts the card system from specific system implementations.
/// </summary>
public interface ICardContext
{
    int CurrentTick { get; }

    // --- Read operations (preconditions) ---
    float GetPressure(string pressureName, EntityId scopeId);
    float GetAttribute(EntityId entityId, string attributeName);
    int CountEntitiesByKind(EntityKind kind);
    Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind);
    int? GetLastFiredTick();
    Entity? GetEntity(EntityId id);

    // --- Write operations (mutations) ---
    Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates);
    void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength);
    void ModifyAttribute(EntityId entityId, string attributeName, float delta);
    void ModifyPressure(string pressureName, EntityId scopeId, float delta);
    void SetTag(EntityId entityId, string key, string value);
    void SetEntityHistorical(EntityId entityId);
}
