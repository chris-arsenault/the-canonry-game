namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class Simulation
{
    internal bool SetEntityTag(int entityId, int tagId, bool assigned, int tick)
    {
        if (IsCollectingDeltas)
        {
            var occurrence = World.EntityOccurrenceHandleForIntent(entityId);
            var delta = new TagDelta(
                occurrence,
                tagId,
                assigned,
                ExecutionContext);
            _continuousOwners.Enqueue(delta);

            return World.EntityHasTag(entityId, tagId) != assigned;
        }

        return ApplyEntityTagOwned(
            entityId,
            tagId,
            assigned,
            tick);
    }

    private bool ApplyEntityTagOwned(
        int entityId,
        int tagId,
        bool assigned,
        int tick)
    {
        if (!World.SetEntityTagBit(
                entityId,
                tagId,
                assigned,
                tick,
                ExecutionContext))
        {
            return false;
        }

        WriteKernelEvent(KernelEvent.From(new EntityTagChangedKernelEvent(
            tick,
            WorldVersion,
            World.EntityOccurrenceHandle(entityId),
            tagId,
            assigned ? 1 : 0,
            ExecutionContext.SourceCodeId)));
        return true;
    }

    internal void ApplyContinuousOwnerTag(
        in TagDelta delta,
        int tick)
    {
        if (!World.TryResolveEntityOccurrence(
                delta.EntityOccurrenceHandle,
                out var entityId))
        {
            return;
        }

        using var scope = EnterExecutionContext(delta.Context);
        _ = ApplyEntityTagOwned(
            entityId,
            delta.TagId,
            delta.Assigned,
            tick);
    }
}
