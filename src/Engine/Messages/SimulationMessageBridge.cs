namespace Engine.Messages;

using Engine.Graph;
using Engine.Lifecycle;

/// <summary>
/// Bridges lower-level engine events onto the simulation message bus so
/// observers can subscribe to one stream instead of each subsystem separately.
/// </summary>
public static class SimulationMessageBridge
{
    public static void Wire(WorldGraph graph, LifecycleSystem lifecycle, MessageBus messageBus)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(lifecycle);
        ArgumentNullException.ThrowIfNull(messageBus);

        graph.EntityCreated += (_, e) =>
        {
            var sem = e.Entity.SemanticPosition;
            messageBus.Publish(new EntityCreatedMessage(
                e.Entity.CreatedAt,
                e.Entity.Id,
                e.Entity.Kind,
                e.Entity.Subtype,
                e.Entity.Name,
                e.Entity.Culture,
                e.Entity.Coordinates,
                (sem.Q, sem.R, sem.S)));
        };

        graph.EntityStatusChanged += (_, e) =>
        {
            messageBus.Publish(new EntityStatusChangedMessage(
                e.Entity.UpdatedAt,
                e.Entity.Id,
                e.Entity.Kind,
                e.Entity.Subtype,
                e.Entity.Name,
                e.PreviousStatus.ToString(),
                e.Entity.Status.ToString()));
        };

        graph.RelationshipCreated += (_, e) =>
        {
            messageBus.Publish(new RelationshipChangedMessage(
                e.Relationship.StartDate,
                "created",
                e.Relationship.Kind,
                e.Relationship.Polarity.ToString(),
                e.Relationship.SourceId,
                e.Relationship.TargetId,
                previousStrength: 0f,
                e.Relationship.Strength,
                e.Relationship.Facts.ToArray()));
        };

        graph.RelationshipCulled += (_, e) =>
        {
            messageBus.Publish(new RelationshipChangedMessage(
                e.EndTick,
                "culled",
                e.Relationship.Kind,
                e.Relationship.Polarity.ToString(),
                e.Relationship.SourceId,
                e.Relationship.TargetId,
                e.Relationship.Strength,
                newStrength: 0f,
                e.Relationship.Facts.ToArray()));
        };

        graph.RelationshipStrengthChanged += (_, e) =>
        {
            messageBus.Publish(new RelationshipChangedMessage(
                e.Tick,
                "strength_changed",
                e.Relationship.Kind,
                e.Relationship.Polarity.ToString(),
                e.Relationship.SourceId,
                e.Relationship.TargetId,
                e.PreviousStrength,
                e.Relationship.Strength,
                e.Relationship.Facts.ToArray()));
        };

        lifecycle.EntityDied += (_, e) =>
        {
            messageBus.Publish(new EntityDiedMessage(
                e.DeathTick,
                e.Entity.Id,
                e.Entity.Kind,
                e.Entity.Name));
        };
    }
}
