namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class Simulation
{
    internal bool SetEntityTag(int entityId, int tagId, bool assigned, int tick)
    {
        if (_collectingDeltas)
        {
            var occurrence = World.EntityOccurrenceHandle(entityId);
            _tickDeltas.AddTag(new TagDelta(
                occurrence,
                tagId,
                assigned,
                Priority: 1,
                DeltaSourceKey(occurrence, tagId),
                _executionContext));
            return World.EntityHasTag(entityId, tagId) != assigned;
        }

        if (!World.SetEntityTagBit(entityId, tagId, assigned, tick, _executionContext))
        {
            return false;
        }

        WriteKernelEvent(KernelEvent.From(new EntityTagChangedKernelEvent(
            CurrentTick,
            WorldVersion,
            World.EntityOccurrenceHandle(entityId),
            tagId,
            assigned ? 1 : 0,
            _executionContext.SourceCodeId)));
        return true;
    }
}
