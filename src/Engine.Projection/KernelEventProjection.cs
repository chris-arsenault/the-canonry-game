namespace Engine.Projection;

using Engine.Eventing;

public interface IKernelEventProjection
{
    void Project(in SequencedKernelEvent kernelEvent);

    void OnEventsDropped(long droppedEventCount, long nextSequence)
    {
    }
}

public abstract class KernelEventProjector : IKernelEventProjection
{
    public void Project(in SequencedKernelEvent kernelEvent)
    {
        var evt = kernelEvent.Event;
        switch (evt.EventType)
        {
            case KernelEventType.EntityCreated:
                OnEntityCreated(kernelEvent.Sequence, evt.AsEntityCreated());
                break;
            case KernelEventType.EntityStatusChanged:
                OnEntityStatusChanged(kernelEvent.Sequence, evt.AsEntityStatusChanged());
                break;
            case KernelEventType.EntityMoved:
                OnEntityMoved(kernelEvent.Sequence, evt.AsEntityMoved());
                break;
            case KernelEventType.AttributeChanged:
                OnAttributeChanged(kernelEvent.Sequence, evt.AsAttributeChanged());
                break;
            case KernelEventType.ResourceChanged:
                OnResourceChanged(kernelEvent.Sequence, evt.AsResourceChanged());
                break;
            case KernelEventType.RelationshipChanged:
                OnRelationshipChanged(kernelEvent.Sequence, evt.AsRelationshipChanged());
                break;
            case KernelEventType.ProcessEvaluated:
                OnProcessEvaluated(kernelEvent.Sequence, evt.AsProcessEvaluated());
                break;
            case KernelEventType.ProcessExecuted:
                OnProcessExecuted(kernelEvent.Sequence, evt.AsProcessExecuted());
                break;
            case KernelEventType.GoalGenerated:
                OnGoalGenerated(kernelEvent.Sequence, evt.AsGoalGenerated());
                break;
            case KernelEventType.DegeneracyDetected:
                OnDegeneracyDetected(kernelEvent.Sequence, evt.AsDegeneracyDetected());
                break;
            case KernelEventType.GeneratorEvaluated:
                OnGeneratorEvaluated(kernelEvent.Sequence, evt.AsGeneratorEvaluated());
                break;
            case KernelEventType.GeneratorFired:
                OnGeneratorFired(kernelEvent.Sequence, evt.AsGeneratorFired());
                break;
            case KernelEventType.DecisionExecuted:
                OnDecisionExecuted(kernelEvent.Sequence, evt.AsDecisionExecuted());
                break;
        }
    }

    public virtual void OnEventsDropped(long droppedEventCount, long nextSequence)
    {
    }

    protected virtual void OnEntityCreated(long sequence, in EntityCreatedKernelEvent evt)
    {
    }

    protected virtual void OnEntityStatusChanged(long sequence, in EntityStatusChangedKernelEvent evt)
    {
    }

    protected virtual void OnEntityMoved(long sequence, in EntityMovedKernelEvent evt)
    {
    }

    protected virtual void OnAttributeChanged(long sequence, in AttributeChangedKernelEvent evt)
    {
    }

    protected virtual void OnResourceChanged(long sequence, in ResourceChangedKernelEvent evt)
    {
    }

    protected virtual void OnRelationshipChanged(long sequence, in RelationshipChangedKernelEvent evt)
    {
    }

    protected virtual void OnProcessEvaluated(long sequence, in ProcessEvaluatedKernelEvent evt)
    {
    }

    protected virtual void OnProcessExecuted(long sequence, in ProcessExecutedKernelEvent evt)
    {
    }

    protected virtual void OnGoalGenerated(long sequence, in GoalGeneratedKernelEvent evt)
    {
    }

    protected virtual void OnDegeneracyDetected(long sequence, in DegeneracyDetectedKernelEvent evt)
    {
    }

    protected virtual void OnGeneratorEvaluated(long sequence, in GeneratorEvaluatedKernelEvent evt)
    {
    }

    protected virtual void OnGeneratorFired(long sequence, in GeneratorFiredKernelEvent evt)
    {
    }

    protected virtual void OnDecisionExecuted(long sequence, in DecisionExecutedKernelEvent evt)
    {
    }
}

public sealed class KernelEventProjectionPump : IDisposable
{
    private readonly KernelEventLog _eventLog;
    private readonly KernelEventCursor _cursor;
    private readonly IKernelEventProjection[] _projections;
    private readonly SequencedKernelEvent[] _buffer;
    private bool _disposed;
    private long _lastObservedDroppedEvents;

    public KernelEventProjectionPump(
        KernelEventLog eventLog,
        KernelEventCursorStart cursorStart,
        IEnumerable<IKernelEventProjection> projections,
        int batchSize = 256)
    {
        ArgumentNullException.ThrowIfNull(eventLog);
        ArgumentNullException.ThrowIfNull(projections);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        _eventLog = eventLog;
        _cursor = eventLog.CreateCursor(cursorStart);
        _projections = projections.ToArray();
        _buffer = new SequencedKernelEvent[batchSize];
    }

    public KernelEventProjectionPump(
        KernelEventLog eventLog,
        KernelEventCursorStart cursorStart,
        IKernelEventProjection projection,
        int batchSize = 256)
        : this(eventLog, cursorStart, [projection], batchSize)
    {
    }

    public long NextSequence => _cursor.NextSequence;

    public long DroppedEvents => _cursor.DroppedEvents;

    public void DrainAvailable()
    {
        ThrowIfDisposed();

        while (true)
        {
            var read = _eventLog.Read(_cursor, _buffer);
            NotifyDroppedEvents();
            if (read == 0)
            {
                return;
            }

            for (var i = 0; i < read; i++)
            {
                for (var projectionIndex = 0; projectionIndex < _projections.Length; projectionIndex++)
                {
                    _projections[projectionIndex].Project(_buffer[i]);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cursor.Dispose();
    }

    private void NotifyDroppedEvents()
    {
        var dropped = _cursor.DroppedEvents;
        if (dropped == _lastObservedDroppedEvents)
        {
            return;
        }

        var delta = dropped - _lastObservedDroppedEvents;
        _lastObservedDroppedEvents = dropped;
        for (var projectionIndex = 0; projectionIndex < _projections.Length; projectionIndex++)
        {
            _projections[projectionIndex].OnEventsDropped(delta, _cursor.NextSequence);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
