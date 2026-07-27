namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class Simulation
{
    internal bool SetEntityTag(int entityId, int tagId, bool assigned, int tick)
    {
        if (_collectingDeltas)
        {
            var occurrence = World.EntityOccurrenceHandleForIntent(entityId);
            _tickDeltas.AddTag(new TagDelta(
                occurrence,
                tagId,
                assigned,
                Priority: 1,
                DeltaSourceKey(occurrence, tagId),
                ExecutionContext));
            return World.EntityHasTag(entityId, tagId) != assigned;
        }

        if (!World.SetEntityTagBit(entityId, tagId, assigned, tick, ExecutionContext))
        {
            return false;
        }

        WriteKernelEvent(KernelEvent.From(new EntityTagChangedKernelEvent(
            CurrentTick,
            WorldVersion,
            World.EntityOccurrenceHandle(entityId),
            tagId,
            assigned ? 1 : 0,
            ExecutionContext.SourceCodeId)));
        return true;
    }
}
