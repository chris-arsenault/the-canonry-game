namespace Engine.Kernel;

/// <summary>
/// Provenance is streamed at invocation completion. The kernel retains only a
/// bounded stack of currently executing frames plus causal invocation tokens
/// on active entities and relationships.
/// </summary>
internal sealed partial class WorldState
{
    private const int ProvenanceFrameCapacity = 8;

    private int[] _provenanceFrameInvocationIds = [];
    private int[] _provenanceFrameTicks = [];
    private int[] _provenanceFrameProducerKindCodes = [];
    private int[] _provenanceFrameProducerIds = [];
    private int[] _provenanceFramePrimaryEntityIds = [];
    private int[] _provenanceFrameSecondaryEntityIds = [];
    private int[] _provenanceFrameParentInvocationIds = [];
    private int[] _provenanceFrameTriggerKindCodes = [];
    private int[] _provenanceFrameTriggerIds = [];
    private int[] _provenanceFrameOutputCounts = [];
    private int _provenanceFrameDepth;
    private int _nextProvenanceInvocationId;

    private void InitializeProvenanceStorage(SimulationOptions options)
    {
        _provenanceFrameInvocationIds = new int[ProvenanceFrameCapacity + 1];
        _provenanceFrameTicks = new int[ProvenanceFrameCapacity + 1];
        _provenanceFrameProducerKindCodes = new int[ProvenanceFrameCapacity + 1];
        _provenanceFrameProducerIds = new int[ProvenanceFrameCapacity + 1];
        _provenanceFramePrimaryEntityIds = new int[ProvenanceFrameCapacity + 1];
        _provenanceFrameSecondaryEntityIds = new int[ProvenanceFrameCapacity + 1];
        _provenanceFrameParentInvocationIds = new int[ProvenanceFrameCapacity + 1];
        _provenanceFrameTriggerKindCodes = new int[ProvenanceFrameCapacity + 1];
        _provenanceFrameTriggerIds = new int[ProvenanceFrameCapacity + 1];
        _provenanceFrameOutputCounts = new int[ProvenanceFrameCapacity + 1];
        _entityCreatedByInvocationId = new int[options.EntityCapacity + 1];
        _relationshipCreatedByInvocationId = new int[options.RelationshipCapacity + 1];
        _relationshipLastChangedByInvocationId = new int[options.RelationshipCapacity + 1];
    }

    internal int BeginProvenanceInvocation(
        int tick,
        int producerKindCode,
        int producerId,
        int primaryEntityId,
        int secondaryEntityId,
        int triggerKindCode,
        int triggerId,
        int parentInvocationId,
        in KernelExecutionContext context)
    {
        ValidatePositive(producerKindCode);
        ValidatePositive(producerId);
        NumericGuard.ThrowIfNegative(primaryEntityId);
        NumericGuard.ThrowIfNegative(secondaryEntityId);
        NumericGuard.ThrowIfNegative(triggerKindCode);
        NumericGuard.ThrowIfNegative(triggerId);
        NumericGuard.ThrowIfNegative(parentInvocationId);
        ValidatePositive(context.SourceCodeId);
        if (_provenanceFrameDepth >= ProvenanceFrameCapacity ||
            _nextProvenanceInvocationId == int.MaxValue)
        {
            throw new global::System.InvalidOperationException();
        }

        var frame = ++_provenanceFrameDepth;
        var invocationId = ++_nextProvenanceInvocationId;
        _provenanceFrameInvocationIds[frame] = invocationId;
        _provenanceFrameTicks[frame] = tick;
        _provenanceFrameProducerKindCodes[frame] = producerKindCode;
        _provenanceFrameProducerIds[frame] = producerId;
        _provenanceFramePrimaryEntityIds[frame] = EntityOccurrenceHandleOrZero(primaryEntityId);
        _provenanceFrameSecondaryEntityIds[frame] = EntityOccurrenceHandleOrZero(secondaryEntityId);
        _provenanceFrameParentInvocationIds[frame] = parentInvocationId;
        _provenanceFrameTriggerKindCodes[frame] = triggerKindCode;
        _provenanceFrameTriggerIds[frame] = triggerId;
        _provenanceFrameOutputCounts[frame] = 0;
        return invocationId;
    }

    internal ProvenanceInvocationRecord CompleteProvenanceInvocation(int invocationId)
    {
        var frame = _provenanceFrameDepth;
        if (frame == 0 || _provenanceFrameInvocationIds[frame] != invocationId)
        {
            throw new global::System.InvalidOperationException();
        }

        var record = new ProvenanceInvocationRecord(
            invocationId,
            _provenanceFrameTicks[frame],
            _provenanceFrameProducerKindCodes[frame],
            _provenanceFrameProducerIds[frame],
            _provenanceFramePrimaryEntityIds[frame],
            _provenanceFrameSecondaryEntityIds[frame],
            _provenanceFrameParentInvocationIds[frame],
            _provenanceFrameTriggerKindCodes[frame],
            _provenanceFrameTriggerIds[frame],
            _provenanceFrameOutputCounts[frame]);

        _provenanceFrameInvocationIds[frame] = 0;
        _provenanceFrameTicks[frame] = 0;
        _provenanceFrameProducerKindCodes[frame] = 0;
        _provenanceFrameProducerIds[frame] = 0;
        _provenanceFramePrimaryEntityIds[frame] = 0;
        _provenanceFrameSecondaryEntityIds[frame] = 0;
        _provenanceFrameParentInvocationIds[frame] = 0;
        _provenanceFrameTriggerKindCodes[frame] = 0;
        _provenanceFrameTriggerIds[frame] = 0;
        _provenanceFrameOutputCounts[frame] = 0;
        _provenanceFrameDepth--;
        return record;
    }

    internal int EntityCreatedByInvocationId(int entityId)
    {
        EnsureEntity(entityId);
        return _entityCreatedByInvocationId[entityId];
    }

    internal int RelationshipCreatedByInvocationId(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        return _relationshipCreatedByInvocationId[relationshipId];
    }

    internal int RelationshipLastChangedByInvocationId(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        return _relationshipLastChangedByInvocationId[relationshipId];
    }

    internal void RecordProvenanceEntityCreated(int entityId, int kindId, in KernelExecutionContext context)
    {
        if (context.ProvenanceInvocationId == 0)
        {
            return;
        }

        ValidatePositive(kindId);
        NoteProvenanceOutput(context.ProvenanceInvocationId);
        _entityCreatedByInvocationId[entityId] = context.ProvenanceInvocationId;
    }

    internal void ReserveProvenanceOutput(
        in KernelExecutionContext context)
    {
        if (context.ProvenanceInvocationId > 0)
        {
            NoteProvenanceOutput(context.ProvenanceInvocationId);
        }
    }

    private void RecordReservedProvenanceEntityCreated(
        int entityId,
        in KernelExecutionContext context)
    {
        if (context.ProvenanceInvocationId > 0)
        {
            _entityCreatedByInvocationId[entityId] =
                context.ProvenanceInvocationId;
        }
    }

    private void RecordProvenanceRelationshipCreated(
        int relationshipId,
        in KernelExecutionContext context)
    {
        if (context.ProvenanceInvocationId == 0)
        {
            return;
        }

        ValidatePositive(relationshipId);
        NoteProvenanceOutput(context.ProvenanceInvocationId);
        _relationshipCreatedByInvocationId[relationshipId] = context.ProvenanceInvocationId;
        _relationshipLastChangedByInvocationId[relationshipId] = context.ProvenanceInvocationId;
    }

    private void RecordReservedProvenanceRelationshipCreated(
        int relationshipId,
        in KernelExecutionContext context)
    {
        if (context.ProvenanceInvocationId == 0)
        {
            return;
        }

        _relationshipCreatedByInvocationId[relationshipId] =
            context.ProvenanceInvocationId;
        _relationshipLastChangedByInvocationId[relationshipId] =
            context.ProvenanceInvocationId;
    }

    private void NoteProvenanceOutput(int invocationId)
    {
        for (var frame = _provenanceFrameDepth; frame > 0; frame--)
        {
            if (_provenanceFrameInvocationIds[frame] == invocationId)
            {
                _provenanceFrameOutputCounts[frame]++;
                return;
            }
        }

        throw new global::System.InvalidOperationException();
    }

}

internal readonly record struct ProvenanceInvocationRecord(
    int InvocationId,
    int Tick,
    int ProducerKindCode,
    int ProducerId,
    int PrimaryEntityId,
    int SecondaryEntityId,
    int ParentInvocationId,
    int TriggerKindCode,
    int TriggerId,
    int OutputCount);
