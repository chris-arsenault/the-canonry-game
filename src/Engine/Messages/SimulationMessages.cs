namespace Engine.Messages;

using Engine.Graph;
using Engine.Kinds;

/// <summary>A process was executed by an entity.</summary>
public sealed class ProcessExecutedMessage : SimulationMessage
{
    public EntityId ActorId { get; }
    public EntityKind ActorKind { get; }
    public string ProcessName { get; }
    public string Description { get; }
    public IReadOnlyList<EntityId> AffectedEntities { get; }

    public ProcessExecutedMessage(int tick, EntityId actorId, EntityKind actorKind,
        string processName, string description, IReadOnlyList<EntityId>? affectedEntities = null)
        : base(tick)
    {
        ActorId = actorId;
        ActorKind = actorKind;
        ProcessName = processName;
        Description = description;
        AffectedEntities = affectedEntities ?? [];
    }
}

/// <summary>A card-gated decision was executed by an entity.</summary>
public sealed class DecisionExecutedMessage : SimulationMessage
{
    public EntityId ActorId { get; }
    public EntityKind ActorKind { get; }
    public string DecisionName { get; }
    public string Description { get; }
    public string? CardId { get; }
    public bool Succeeded { get; }
    public IReadOnlyList<EntityId> AffectedEntities { get; }

    public DecisionExecutedMessage(int tick, EntityId actorId, EntityKind actorKind,
        string decisionName, string description, string? cardId = null,
        bool succeeded = true, IReadOnlyList<EntityId>? affectedEntities = null)
        : base(tick)
    {
        ActorId = actorId;
        ActorKind = actorKind;
        DecisionName = decisionName;
        Description = description;
        CardId = cardId;
        Succeeded = succeeded;
        AffectedEntities = affectedEntities ?? [];
    }
}

/// <summary>An entity's attribute changed significantly.</summary>
public sealed class AttributeChangedMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public string AttributeName { get; }
    public float OldValue { get; }
    public float NewValue { get; }
    public string Source { get; }

    public AttributeChangedMessage(int tick, EntityId entityId, string attributeName,
        float oldValue, float newValue, string source)
        : base(tick)
    {
        EntityId = entityId;
        AttributeName = attributeName;
        OldValue = oldValue;
        NewValue = newValue;
        Source = source;
    }
}

/// <summary>An entity moved to a new location.</summary>
public sealed class EntityMovedMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public (int Q, int R) From { get; }
    public (int Q, int R) To { get; }

    public EntityMovedMessage(int tick, EntityId entityId, (int Q, int R) from, (int Q, int R) to)
        : base(tick)
    {
        EntityId = entityId;
        From = from;
        To = to;
    }
}

/// <summary>An entity's subtype changed.</summary>
public sealed class SubtypeChangedMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public string OldSubtype { get; }
    public string NewSubtype { get; }

    public SubtypeChangedMessage(int tick, EntityId entityId, string oldSubtype, string newSubtype)
        : base(tick)
    {
        EntityId = entityId;
        OldSubtype = oldSubtype;
        NewSubtype = newSubtype;
    }
}

/// <summary>An entity died (transitioned to historical).</summary>
public sealed class EntityDiedMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public EntityKind Kind { get; }
    public string Name { get; }

    public EntityDiedMessage(int tick, EntityId entityId, EntityKind kind, string name)
        : base(tick)
    {
        EntityId = entityId;
        Kind = kind;
        Name = name;
    }
}

/// <summary>A degeneracy warning was triggered or resolved.</summary>
public sealed class DegeneracyWarningMessage : SimulationMessage
{
    public string DetectorName { get; }
    public Degeneracy.Severity Severity { get; }
    public string Description { get; }

    public DegeneracyWarningMessage(int tick, string detectorName,
        Degeneracy.Severity severity, string description)
        : base(tick)
    {
        DetectorName = detectorName;
        Severity = severity;
        Description = description;
    }
}
