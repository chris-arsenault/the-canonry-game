namespace Engine.Graph;

/// <summary>
/// Events emitted by the WorldGraph when mutations occur.
/// Systems subscribe to these to react to graph changes.
/// </summary>
public sealed class EntityCreatedEventArgs(Entity entity) : EventArgs
{
    public Entity Entity { get; } = entity;
}

public sealed class EntityStatusChangedEventArgs(Entity entity, EntityStatus previousStatus) : EventArgs
{
    public Entity Entity { get; } = entity;
    public EntityStatus PreviousStatus { get; } = previousStatus;
}

public sealed class RelationshipCreatedEventArgs(Relationship relationship) : EventArgs
{
    public Relationship Relationship { get; } = relationship;
}

public sealed class RelationshipCulledEventArgs(Relationship relationship, int endTick) : EventArgs
{
    public Relationship Relationship { get; } = relationship;
    public int EndTick { get; } = endTick;
}

public sealed class RelationshipStrengthChangedEventArgs(Relationship relationship, float previousStrength, int tick) : EventArgs
{
    public Relationship Relationship { get; } = relationship;
    public float PreviousStrength { get; } = previousStrength;
    public int Tick { get; } = tick;
}
