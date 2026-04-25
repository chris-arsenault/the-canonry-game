namespace Engine.Simulation;

using Engine.Behaviors;
using Engine.Domain.Loader;
using Engine.Eventing;
using Engine.Graph;

internal static class KernelSimulationEventBridge
{
    public static void Wire(
        WorldGraph graph,
        KernelEventLog kernelEvents,
        global::Engine.Domain.DomainIdRegistry domainIds,
        IWorldVersionSource worldVersion)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(kernelEvents);
        ArgumentNullException.ThrowIfNull(domainIds);
        ArgumentNullException.ThrowIfNull(worldVersion);

        var relationshipCreatedCodeId = domainIds.EventCodeIdOf(DomainRuntimeIdLoader.RelationshipCreatedCode).Value;
        var relationshipCulledCodeId = domainIds.EventCodeIdOf(DomainRuntimeIdLoader.RelationshipCulledCode).Value;
        var relationshipStrengthChangedCodeId =
            domainIds.EventCodeIdOf(DomainRuntimeIdLoader.RelationshipStrengthChangedCode).Value;

        graph.EntityCreated += (_, e) =>
        {
            var sem = e.Entity.SemanticPosition;
            RequireRegistered(e.Entity);
            WriteOrThrow(kernelEvents, KernelEvent.From(new EntityCreatedKernelEvent(
                Tick: e.Entity.CreatedAt,
                WorldVersion: worldVersion.WorldVersion,
                EntityId: e.Entity.Id.Value,
                KindId: e.Entity.Kind.Id,
                SubtypeId: e.Entity.SubtypeId.Value,
                CultureId: e.Entity.CultureId.Value,
                PhysicalQ: e.Entity.Coordinates.Q,
                PhysicalR: e.Entity.Coordinates.R,
                SemanticQ: sem.Q,
                SemanticR: sem.R,
                SemanticS: sem.S)));
        };

        graph.EntityStatusChanged += (_, e) =>
        {
            RequireRegistered(e.Entity);
            WriteOrThrow(kernelEvents, KernelEvent.From(new EntityStatusChangedKernelEvent(
                Tick: e.Entity.UpdatedAt,
                WorldVersion: worldVersion.WorldVersion,
                EntityId: e.Entity.Id.Value,
                KindId: e.Entity.Kind.Id,
                SubtypeId: e.Entity.SubtypeId.Value,
                PreviousStatusCode: (int)e.PreviousStatus,
                CurrentStatusCode: (int)e.Entity.Status)));
        };

        graph.RelationshipCreated += (_, e) =>
        {
            WriteRelationshipChanged(
                kernelEvents,
                worldVersion,
                e.Relationship,
                e.Relationship.StartDate,
                relationshipCreatedCodeId,
                previousStrength: 0f,
                newStrength: e.Relationship.Strength);
        };

        graph.RelationshipCulled += (_, e) =>
        {
            WriteRelationshipChanged(
                kernelEvents,
                worldVersion,
                e.Relationship,
                e.EndTick,
                relationshipCulledCodeId,
                previousStrength: e.Relationship.Strength,
                newStrength: 0f);
        };

        graph.RelationshipStrengthChanged += (_, e) =>
        {
            WriteRelationshipChanged(
                kernelEvents,
                worldVersion,
                e.Relationship,
                e.Tick,
                relationshipStrengthChangedCodeId,
                previousStrength: e.PreviousStrength,
                newStrength: e.Relationship.Strength);
        };
    }

    private static void WriteRelationshipChanged(
        KernelEventLog kernelEvents,
        IWorldVersionSource worldVersion,
        Relationship relationship,
        int tick,
        int changeCodeId,
        float previousStrength,
        float newStrength)
    {
        RequireRegistered(relationship);

        Span<int> factIds = stackalloc int[4];
        for (var index = 0; index < relationship.FactCount; index++)
        {
            factIds[index] = relationship.GetFactId(index).Value;
        }

        WriteOrThrow(kernelEvents, KernelEvent.From(new RelationshipChangedKernelEvent(
            Tick: tick,
            WorldVersion: worldVersion.WorldVersion,
            ChangeCodeId: changeCodeId,
            RelationshipKindId: relationship.KindId.Value,
            PolarityCode: (int)relationship.Polarity,
            SourceEntityId: relationship.SourceId.Value,
            TargetEntityId: relationship.TargetId.Value,
            FactId0: factIds[0],
            FactId1: factIds[1],
            FactId2: factIds[2],
            FactId3: factIds[3],
            FactCount: relationship.FactCount,
            PreviousStrength: previousStrength,
            NewStrength: newStrength)));
    }

    private static void RequireRegistered(Entity entity)
    {
        if (entity.SubtypeId.Value <= 0 || entity.CultureId.Value <= 0)
        {
            throw new InvalidOperationException(
                $"Entity {entity.Id.Value} was created without registered primitive subtype/culture IDs.");
        }
    }

    private static void RequireRegistered(Relationship relationship)
    {
        if (relationship.KindId.Value <= 0)
        {
            throw new InvalidOperationException(
                $"Relationship '{relationship.Kind}' was created without a registered primitive relationship kind ID.");
        }

        for (var index = 0; index < relationship.FactCount; index++)
        {
            if (relationship.GetFactId(index).Value <= 0)
            {
                throw new InvalidOperationException(
                    $"Relationship '{relationship.Kind}' has an unregistered primitive fact at index {index}.");
            }
        }
    }

    private static void WriteOrThrow(KernelEventLog kernelEvents, in KernelEvent kernelEvent)
    {
        if (!kernelEvents.TryWrite(kernelEvent))
        {
            throw new InvalidOperationException("Kernel event writer rejected a graph event.");
        }
    }
}
