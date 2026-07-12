namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class Simulation
{
    internal bool SetEntityTag(int entityId, int tagId, bool assigned, int tick)
    {
        if (!World.SetEntityTagBit(entityId, tagId, assigned, tick, _executionContext))
        {
            return false;
        }

        WriteKernelEvent(KernelEvent.From(new EntityTagChangedKernelEvent(
            CurrentTick,
            WorldVersion,
            entityId,
            tagId,
            assigned ? 1 : 0,
            _executionContext.SourceCodeId)));
        return true;
    }
}
