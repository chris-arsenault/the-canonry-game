namespace Engine.Pressure;

using Engine.Graph;

/// <summary>
/// A contributing factor that pushes a pressure value up or down.
/// Factors are evaluated within a scope (region or entity) each tick.
/// </summary>
public interface IContributingFactor
{
    /// <summary>Human-readable name for debugging.</summary>
    string Name { get; }

    /// <summary>
    /// Compute this factor's contribution to the pressure value.
    /// Positive pushes pressure up, negative pushes down.
    /// </summary>
    float Evaluate(IPressureContext context);
}

/// <summary>
/// Context provided to contributing factors for accessing world state within a scope.
/// </summary>
public interface IPressureContext
{
    /// <summary>The entity IDs within the current evaluation scope.</summary>
    IReadOnlyCollection<EntityId> EntitiesInScope { get; }

    /// <summary>Get an entity by ID.</summary>
    Entity? GetEntity(EntityId id);

    /// <summary>Count entities matching criteria within scope.</summary>
    int CountEntities(Func<Entity, bool> predicate);

    /// <summary>Count active relationships matching criteria within scope.</summary>
    int CountRelationships(Func<Relationship, bool> predicate);

    /// <summary>Get an attribute value for an entity.</summary>
    float GetAttribute(EntityId entityId, string attributeName);
}
