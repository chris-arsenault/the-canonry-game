namespace Engine.Cards;

using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// A mutation: a single effect applied to the world when a card fires.
/// Mutations execute in order within a single card firing.
/// </summary>
public interface IMutation
{
    /// <summary>Apply this mutation to the world state.</summary>
    void Execute(ICardContext context);

    /// <summary>Human-readable description.</summary>
    string Describe();
}

/// <summary>Creates a new entity.</summary>
public sealed class CreateEntityMutation(
    EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) : IMutation
{
    /// <summary>After execution, holds the ID of the created entity for subsequent mutations.</summary>
    public EntityId? CreatedId { get; private set; }

    public void Execute(ICardContext context)
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
    public void Execute(ICardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateRelationship(kind, polarity, sourceId, targetId, strength);
    }

    public string Describe() => $"create_relationship({sourceId}->{targetId}, {kind}, strength={strength})";
}

/// <summary>Modifies an entity's attribute by a delta.</summary>
public sealed class ModifyAttributeMutation(EntityId entityId, string attributeName, float delta) : IMutation
{
    public void Execute(ICardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ModifyAttribute(entityId, attributeName, delta);
    }

    public string Describe() => $"modify_attribute({entityId}, {attributeName}, delta={delta})";
}

/// <summary>Directly modifies a pressure value.</summary>
public sealed class ModifyPressureMutation(string pressureName, EntityId scopeId, float delta) : IMutation
{
    public void Execute(ICardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ModifyPressure(pressureName, scopeId, delta);
    }

    public string Describe() => $"modify_pressure({pressureName}, scope={scopeId}, delta={delta})";
}

/// <summary>Sets a tag on an entity.</summary>
public sealed class SetTagMutation(EntityId entityId, string key, string value) : IMutation
{
    public void Execute(ICardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.SetTag(entityId, key, value);
    }

    public string Describe() => $"set_tag({entityId}, {key}={value})";
}

/// <summary>Transitions an entity to historical status.</summary>
public sealed class SetHistoricalMutation(EntityId entityId) : IMutation
{
    public void Execute(ICardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.SetEntityHistorical(entityId);
    }

    public string Describe() => $"set_historical({entityId})";
}
