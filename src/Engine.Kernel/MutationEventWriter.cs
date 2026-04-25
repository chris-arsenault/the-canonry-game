namespace Engine.Kernel;

using Engine.Eventing;

public sealed class MutationEventWriter
{
    private readonly IKernelEventWriter _events;
    private readonly int _processSourceCodeId;
    private readonly int _cardSourceCodeId;

    public MutationEventWriter(
        IKernelEventWriter events,
        int processSourceCodeId,
        int cardSourceCodeId)
    {
        global::System.ArgumentNullException.ThrowIfNull(events);
        _events = events;
        _processSourceCodeId = processSourceCodeId;
        _cardSourceCodeId = cardSourceCodeId;
    }

    public void WriteProcessAttributeChanged(
        int tick,
        long worldVersion,
        int entityId,
        int attributeId,
        float oldValue,
        float newValue) =>
        WriteAttributeChanged(tick, worldVersion, entityId, attributeId, _processSourceCodeId, oldValue, newValue);

    public void WriteCardAttributeChanged(
        int tick,
        long worldVersion,
        int entityId,
        int attributeId,
        float oldValue,
        float newValue) =>
        WriteAttributeChanged(tick, worldVersion, entityId, attributeId, _cardSourceCodeId, oldValue, newValue);

    public void WriteProcessResourceChanged(
        int tick,
        long worldVersion,
        int resourceId,
        int q,
        int r,
        float oldValue,
        float newValue) =>
        WriteResourceChanged(tick, worldVersion, resourceId, q, r, _processSourceCodeId, oldValue, newValue);

    public void WriteCardResourceChanged(
        int tick,
        long worldVersion,
        int resourceId,
        int q,
        int r,
        float oldValue,
        float newValue) =>
        WriteResourceChanged(tick, worldVersion, resourceId, q, r, _cardSourceCodeId, oldValue, newValue);

    public void WriteEntityMoved(
        int tick,
        long worldVersion,
        int entityId,
        int planeCode,
        int fromQ,
        int fromR,
        int fromS,
        int toQ,
        int toR,
        int toS)
    {
        Write(KernelEvent.From(new EntityMovedKernelEvent(
            tick,
            worldVersion,
            entityId,
            planeCode,
            fromQ,
            fromR,
            fromS,
            toQ,
            toR,
            toS)));
    }

    private void WriteAttributeChanged(
        int tick,
        long worldVersion,
        int entityId,
        int attributeId,
        int sourceCodeId,
        float oldValue,
        float newValue)
    {
        Write(KernelEvent.From(new AttributeChangedKernelEvent(
            tick,
            worldVersion,
            entityId,
            attributeId,
            sourceCodeId,
            oldValue,
            newValue)));
    }

    private void WriteResourceChanged(
        int tick,
        long worldVersion,
        int resourceId,
        int q,
        int r,
        int sourceCodeId,
        float oldValue,
        float newValue)
    {
        Write(KernelEvent.From(new ResourceChangedKernelEvent(
            tick,
            worldVersion,
            resourceId,
            q,
            r,
            sourceCodeId,
            oldValue,
            newValue)));
    }

    private void Write(in KernelEvent kernelEvent)
    {
        if (!_events.TryWrite(kernelEvent))
        {
            throw new global::System.InvalidOperationException();
        }
    }
}
