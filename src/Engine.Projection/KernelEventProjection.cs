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
        OnProjectingEvent(kernelEvent);

        var eventType = kernelEvent.Event.EventType;
        if (eventType >= KernelEventType.PlanStarted)
        {
            ProjectPlanningEvent(kernelEvent);
            return;
        }

        if (eventType <= KernelEventType.TraitAssigned)
        {
            ProjectEntityEvent(kernelEvent);
            return;
        }

        if (eventType is KernelEventType.TickCompleted or
            KernelEventType.GeneratorEnabledChanged or
            KernelEventType.CellCreated or
            KernelEventType.CellChanged or
            KernelEventType.CellInfluenceChanged or
            KernelEventType.ProvenanceInvocationRecorded or
            KernelEventType.KernelStorageGrown)
        {
            ProjectWorldEvent(kernelEvent);
            return;
        }

        ProjectBehaviorEvent(kernelEvent);
    }

    private void ProjectEntityEvent(in SequencedKernelEvent kernelEvent)
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
            case KernelEventType.TraitAssigned:
                OnTraitAssigned(kernelEvent.Sequence, evt.AsTraitAssigned());
                break;
            case KernelEventType.RelationshipChanged:
                OnRelationshipChanged(kernelEvent.Sequence, evt.AsRelationshipChanged());
                break;
        }
    }

    private void ProjectBehaviorEvent(in SequencedKernelEvent kernelEvent)
    {
        var evt = kernelEvent.Event;
        switch (evt.EventType)
        {
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
            case KernelEventType.GambitEvaluated:
                OnGambitEvaluated(kernelEvent.Sequence, evt.AsGambitEvaluated());
                break;
            case KernelEventType.GambitExecuted:
                OnGambitExecuted(kernelEvent.Sequence, evt.AsGambitExecuted());
                break;
        }
    }

    private void ProjectWorldEvent(in SequencedKernelEvent kernelEvent)
    {
        var evt = kernelEvent.Event;
        switch (evt.EventType)
        {
            case KernelEventType.TickCompleted:
                OnTickCompleted(kernelEvent.Sequence, evt.AsTickCompleted());
                break;
            case KernelEventType.GeneratorEnabledChanged:
                OnGeneratorEnabledChanged(kernelEvent.Sequence, evt.AsGeneratorEnabledChanged());
                break;
            case KernelEventType.CellCreated:
                OnCellCreated(kernelEvent.Sequence, evt.AsCellCreated());
                break;
            case KernelEventType.CellChanged:
                OnCellChanged(kernelEvent.Sequence, evt.AsCellChanged());
                break;
            case KernelEventType.CellInfluenceChanged:
                OnCellInfluenceChanged(kernelEvent.Sequence, evt.AsCellInfluenceChanged());
                break;
            case KernelEventType.ProvenanceInvocationRecorded:
                OnProvenanceInvocationRecorded(kernelEvent.Sequence, evt.AsProvenanceInvocationRecorded());
                break;
            case KernelEventType.KernelStorageGrown:
                OnKernelStorageGrown(kernelEvent.Sequence, evt.AsKernelStorageGrown());
                break;
        }
    }

    private void ProjectPlanningEvent(in SequencedKernelEvent kernelEvent)
    {
        var evt = kernelEvent.Event;
        switch (evt.EventType)
        {
            case KernelEventType.PlanStarted:
                OnPlanStarted(kernelEvent.Sequence, evt.AsPlanStarted());
                break;
            case KernelEventType.GoalConsidered:
                OnGoalConsidered(kernelEvent.Sequence, evt.AsGoalConsidered());
                break;
            case KernelEventType.GoalScored:
                OnGoalScored(kernelEvent.Sequence, evt.AsGoalScored());
                break;
            case KernelEventType.GoalSelected:
                OnGoalSelected(kernelEvent.Sequence, evt.AsGoalSelected());
                break;
            case KernelEventType.PlanStepEmitted:
                OnPlanStepEmitted(kernelEvent.Sequence, evt.AsPlanStepEmitted());
                break;
            case KernelEventType.PlanCommitted:
                OnPlanCommitted(kernelEvent.Sequence, evt.AsPlanCommitted());
                break;
            case KernelEventType.PlanAbandoned:
                OnPlanAbandoned(kernelEvent.Sequence, evt.AsPlanAbandoned());
                break;
            case KernelEventType.Trace:
                OnTrace(kernelEvent.Sequence, evt.AsTrace());
                break;
        }
    }

    public virtual void OnEventsDropped(long droppedEventCount, long nextSequence)
    {
    }

    protected virtual void OnProjectingEvent(in SequencedKernelEvent kernelEvent)
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

    protected virtual void OnTraitAssigned(long sequence, in TraitAssignedKernelEvent evt)
    {
    }

    protected virtual void OnRelationshipChanged(long sequence, in RelationshipChangedKernelEvent evt)
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

    protected virtual void OnGambitEvaluated(long sequence, in GambitEvaluatedKernelEvent evt)
    {
    }

    protected virtual void OnGambitExecuted(long sequence, in GambitExecutedKernelEvent evt)
    {
    }

    protected virtual void OnTickCompleted(long sequence, in TickCompletedKernelEvent evt)
    {
    }

    protected virtual void OnGeneratorEnabledChanged(long sequence, in GeneratorEnabledChangedKernelEvent evt)
    {
    }

    protected virtual void OnCellCreated(long sequence, in CellCreatedKernelEvent evt)
    {
    }

    protected virtual void OnCellChanged(long sequence, in CellChangedKernelEvent evt)
    {
    }

    protected virtual void OnCellInfluenceChanged(long sequence, in CellInfluenceChangedKernelEvent evt)
    {
    }

    protected virtual void OnProvenanceInvocationRecorded(long sequence, in ProvenanceInvocationRecordedKernelEvent evt)
    {
    }

    protected virtual void OnKernelStorageGrown(long sequence, in KernelStorageGrownKernelEvent evt)
    {
    }

    protected virtual void OnPlanStarted(long sequence, in PlanStartedKernelEvent evt)
    {
    }

    protected virtual void OnGoalConsidered(long sequence, in GoalConsideredKernelEvent evt)
    {
    }

    protected virtual void OnGoalScored(long sequence, in GoalScoredKernelEvent evt)
    {
    }

    protected virtual void OnGoalSelected(long sequence, in GoalSelectedKernelEvent evt)
    {
    }

    protected virtual void OnPlanStepEmitted(long sequence, in PlanStepEmittedKernelEvent evt)
    {
    }

    protected virtual void OnPlanCommitted(long sequence, in PlanCommittedKernelEvent evt)
    {
    }

    protected virtual void OnPlanAbandoned(long sequence, in PlanAbandonedKernelEvent evt)
    {
    }

    protected virtual void OnTrace(long sequence, in TraceKernelEvent evt)
    {
    }
}

public sealed class KernelEventProjectionPump : IDisposable
{
    private readonly IKernelEventReader _eventLog;
    private readonly KernelEventCursor _cursor;
    private readonly IKernelEventProjection[] _projections;
    private readonly SequencedKernelEvent[] _buffer;
    private bool _disposed;

    public KernelEventProjectionPump(
        IKernelEventReader eventLog,
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
        IKernelEventReader eventLog,
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
            var result = _eventLog.ReadWithResult(_cursor, _buffer);
            NotifyDroppedEvents(result);
            if (result.Count == 0)
            {
                return;
            }

            for (var i = 0; i < result.Count; i++)
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

    private void NotifyDroppedEvents(KernelEventReadResult result)
    {
        if (result.DroppedEventCount == 0)
        {
            return;
        }

        for (var projectionIndex = 0; projectionIndex < _projections.Length; projectionIndex++)
        {
            _projections[projectionIndex].OnEventsDropped(result.DroppedEventCount, result.NextSequence);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
