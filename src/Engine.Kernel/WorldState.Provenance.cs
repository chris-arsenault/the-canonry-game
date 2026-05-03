namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class WorldState
{
    private void InitializeProvenanceStorage(SimulationOptions options)
    {
        _provenanceInvocationTick = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationProducerKindCode = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationProducerId = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationProgramId = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationSourceCodeId = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationActionId = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationPrimaryEntityId = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationSecondaryEntityId = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationParentInvocationId = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationTriggerKindCode = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationTriggerId = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationFirstOutputId = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationLastOutputId = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceInvocationOutputCount = new int[options.ProvenanceInvocationCapacity + 1];
        _provenanceOutputInvocationId = new int[options.ProvenanceOutputCapacity + 1];
        _provenanceOutputKindCode = new int[options.ProvenanceOutputCapacity + 1];
        _provenanceOutputId = new int[options.ProvenanceOutputCapacity + 1];
        _provenanceOutputSubjectEntityId = new int[options.ProvenanceOutputCapacity + 1];
        _provenanceOutputSecondaryEntityId = new int[options.ProvenanceOutputCapacity + 1];
        _provenanceOutputValueId = new int[options.ProvenanceOutputCapacity + 1];
        _provenanceOutputChangeCodeId = new int[options.ProvenanceOutputCapacity + 1];
        _provenanceOutputNextId = new int[options.ProvenanceOutputCapacity + 1];
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
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(primaryEntityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(secondaryEntityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(triggerKindCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(triggerId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(parentInvocationId);
        ValidatePositive(context.SourceCodeId);

        var invocationId = AllocateProvenanceInvocationSlot(context);
        _provenanceInvocationTick[invocationId] = tick;
        _provenanceInvocationProducerKindCode[invocationId] = producerKindCode;
        _provenanceInvocationProducerId[invocationId] = producerId;
        _provenanceInvocationProgramId[invocationId] = context.ProgramId;
        _provenanceInvocationSourceCodeId[invocationId] = context.SourceCodeId;
        _provenanceInvocationActionId[invocationId] = context.ActionId;
        _provenanceInvocationPrimaryEntityId[invocationId] = primaryEntityId;
        _provenanceInvocationSecondaryEntityId[invocationId] = secondaryEntityId;
        _provenanceInvocationParentInvocationId[invocationId] = parentInvocationId;
        _provenanceInvocationTriggerKindCode[invocationId] = triggerKindCode;
        _provenanceInvocationTriggerId[invocationId] = triggerId;
        return invocationId;
    }

    internal void CompleteProvenanceInvocation(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
    }

    internal int ProvenanceInvocationHighWatermark => _provenanceInvocationHighWatermark;

    internal int ProvenanceOutputHighWatermark => _provenanceOutputHighWatermark;

    internal int ProvenanceInvocationCapacity => _provenanceInvocationTick.Length - 1;

    internal int ProvenanceOutputCapacity => _provenanceOutputInvocationId.Length - 1;

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

    internal int ProvenanceInvocationTick(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return _provenanceInvocationTick[invocationId];
    }

    internal int ProvenanceInvocationProducerKindCode(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return _provenanceInvocationProducerKindCode[invocationId];
    }

    internal int ProvenanceInvocationProducerId(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return _provenanceInvocationProducerId[invocationId];
    }

    internal int ProvenanceInvocationProgramId(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return _provenanceInvocationProgramId[invocationId];
    }

    internal int ProvenanceInvocationSourceCodeId(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return _provenanceInvocationSourceCodeId[invocationId];
    }

    internal int ProvenanceInvocationActionId(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return _provenanceInvocationActionId[invocationId];
    }

    internal int ProvenanceInvocationPrimaryEntityId(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return _provenanceInvocationPrimaryEntityId[invocationId];
    }

    internal int ProvenanceInvocationSecondaryEntityId(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return _provenanceInvocationSecondaryEntityId[invocationId];
    }

    internal int ProvenanceInvocationParentInvocationId(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return _provenanceInvocationParentInvocationId[invocationId];
    }

    internal int ProvenanceInvocationTriggerKindCode(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return _provenanceInvocationTriggerKindCode[invocationId];
    }

    internal int ProvenanceInvocationTriggerId(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return _provenanceInvocationTriggerId[invocationId];
    }

    internal int ProvenanceInvocationOutputCount(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return _provenanceInvocationOutputCount[invocationId];
    }

    internal ProvenanceOutputCursor OutputsOfProvenanceInvocation(int invocationId)
    {
        EnsureProvenanceInvocation(invocationId);
        return new ProvenanceOutputCursor(_provenanceInvocationFirstOutputId[invocationId]);
    }

    internal bool MoveNextProvenanceOutput(ref ProvenanceOutputCursor cursor, out int outputId)
    {
        outputId = cursor.OutputId;
        if (outputId == 0)
        {
            return false;
        }

        EnsureProvenanceOutput(outputId);
        cursor = new ProvenanceOutputCursor(_provenanceOutputNextId[outputId]);
        return true;
    }

    internal int ProvenanceOutputInvocationId(int outputId)
    {
        EnsureProvenanceOutput(outputId);
        return _provenanceOutputInvocationId[outputId];
    }

    internal int ProvenanceOutputKindCode(int outputId)
    {
        EnsureProvenanceOutput(outputId);
        return _provenanceOutputKindCode[outputId];
    }

    internal int ProvenanceOutputId(int outputId)
    {
        EnsureProvenanceOutput(outputId);
        return _provenanceOutputId[outputId];
    }

    internal int ProvenanceOutputSubjectEntityId(int outputId)
    {
        EnsureProvenanceOutput(outputId);
        return _provenanceOutputSubjectEntityId[outputId];
    }

    internal int ProvenanceOutputSecondaryEntityId(int outputId)
    {
        EnsureProvenanceOutput(outputId);
        return _provenanceOutputSecondaryEntityId[outputId];
    }

    internal int ProvenanceOutputValueId(int outputId)
    {
        EnsureProvenanceOutput(outputId);
        return _provenanceOutputValueId[outputId];
    }

    internal int ProvenanceOutputChangeCodeId(int outputId)
    {
        EnsureProvenanceOutput(outputId);
        return _provenanceOutputChangeCodeId[outputId];
    }

    internal void RecordProvenanceEntityCreated(int entityId, int kindId, in KernelExecutionContext context)
    {
        if (context.ProvenanceInvocationId == 0)
        {
            return;
        }

        AddProvenanceOutput(
            context,
            context.ProvenanceInvocationId,
            ProvenanceOutputKindCodes.EntityCreated,
            entityId,
            entityId,
            0,
            kindId,
            ProvenanceOutputKindCodes.EntityCreated);
        _entityCreatedByInvocationId[entityId] = context.ProvenanceInvocationId;
    }

    private void RecordProvenanceOutput(
        in KernelExecutionContext context,
        int outputKindCode,
        int outputId,
        int subjectEntityId,
        int secondaryEntityId,
        int valueId,
        int changeCodeId)
    {
        if (context.ProvenanceInvocationId == 0)
        {
            return;
        }

        AddProvenanceOutput(
            context,
            context.ProvenanceInvocationId,
            outputKindCode,
            outputId,
            subjectEntityId,
            secondaryEntityId,
            valueId,
            changeCodeId);
    }

    private void AddProvenanceOutput(
        in KernelExecutionContext context,
        int invocationId,
        int outputKindCode,
        int outputId,
        int subjectEntityId,
        int secondaryEntityId,
        int valueId,
        int changeCodeId)
    {
        EnsureProvenanceInvocation(invocationId);
        ValidatePositive(outputKindCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(outputId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(subjectEntityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(secondaryEntityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(valueId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(changeCodeId);

        var outputRowId = AllocateProvenanceOutputSlot(context);
        _provenanceOutputInvocationId[outputRowId] = invocationId;
        _provenanceOutputKindCode[outputRowId] = outputKindCode;
        _provenanceOutputId[outputRowId] = outputId;
        _provenanceOutputSubjectEntityId[outputRowId] = subjectEntityId;
        _provenanceOutputSecondaryEntityId[outputRowId] = secondaryEntityId;
        _provenanceOutputValueId[outputRowId] = valueId;
        _provenanceOutputChangeCodeId[outputRowId] = changeCodeId;

        var previousLast = _provenanceInvocationLastOutputId[invocationId];
        if (previousLast == 0)
        {
            _provenanceInvocationFirstOutputId[invocationId] = outputRowId;
        }
        else
        {
            _provenanceOutputNextId[previousLast] = outputRowId;
        }

        _provenanceInvocationLastOutputId[invocationId] = outputRowId;
        _provenanceInvocationOutputCount[invocationId]++;

    }

    private void EnsureProvenanceOutputCapacity(in KernelExecutionContext context)
    {
        if (context.ProvenanceInvocationId == 0)
        {
            return;
        }

        if (_provenanceOutputHighWatermark + 1 >= _provenanceOutputInvocationId.Length)
        {
            EnsureProvenanceOutputCapacity(_provenanceOutputHighWatermark + 1, context);
        }
    }

    internal void GrowStorageIfNeeded(int tick, in KernelExecutionContext context)
    {
        if (!_growableStoragePolicy.ShouldCheck(tick))
        {
            return;
        }

        GrowProvenanceInvocationStorageAtThreshold(context);
        GrowProvenanceOutputStorageAtThreshold(context);
    }

    private int AllocateProvenanceInvocationSlot(in KernelExecutionContext context)
    {
        var invocationId = _provenanceInvocationHighWatermark + 1;
        if ((uint)invocationId >= (uint)_provenanceInvocationTick.Length)
        {
            EnsureProvenanceInvocationCapacity(invocationId, context);
        }

        _provenanceInvocationHighWatermark = invocationId;
        return invocationId;
    }

    private int AllocateProvenanceOutputSlot(in KernelExecutionContext context)
    {
        var outputId = _provenanceOutputHighWatermark + 1;
        if ((uint)outputId >= (uint)_provenanceOutputInvocationId.Length)
        {
            EnsureProvenanceOutputCapacity(outputId, context);
        }

        _provenanceOutputHighWatermark = outputId;
        return outputId;
    }

    private void EnsureProvenanceInvocationCapacity(int requestedIndex, in KernelExecutionContext context)
    {
        if ((uint)requestedIndex < (uint)_provenanceInvocationTick.Length)
        {
            return;
        }

        if (!_growableStoragePolicy.Enabled)
        {
            ThrowCapacityExceeded(
                KernelCapacityCodes.ProvenanceInvocation,
                requestedIndex,
                ProvenanceInvocationCapacity);
        }

        GrowProvenanceInvocationStorage(requestedIndex, KernelStorageGrowthReasonCodes.Reserve, context);
    }

    private void EnsureProvenanceOutputCapacity(int requestedIndex, in KernelExecutionContext context)
    {
        if ((uint)requestedIndex < (uint)_provenanceOutputInvocationId.Length)
        {
            return;
        }

        if (!_growableStoragePolicy.Enabled)
        {
            ThrowCapacityExceeded(
                KernelCapacityCodes.ProvenanceOutput,
                requestedIndex,
                ProvenanceOutputCapacity);
        }

        GrowProvenanceOutputStorage(requestedIndex, KernelStorageGrowthReasonCodes.Reserve, context);
    }

    private void GrowProvenanceInvocationStorageAtThreshold(in KernelExecutionContext context)
    {
        if (!KernelGrowableStorage.ShouldGrowAtThreshold(
                _provenanceInvocationHighWatermark,
                ProvenanceInvocationCapacity,
                _growableStoragePolicy.ThresholdPermille))
        {
            return;
        }

        GrowProvenanceInvocationStorage(
            ProvenanceInvocationCapacity + 1,
            KernelStorageGrowthReasonCodes.Threshold,
            context);
    }

    private void GrowProvenanceOutputStorageAtThreshold(in KernelExecutionContext context)
    {
        if (!KernelGrowableStorage.ShouldGrowAtThreshold(
                _provenanceOutputHighWatermark,
                ProvenanceOutputCapacity,
                _growableStoragePolicy.ThresholdPermille))
        {
            return;
        }

        GrowProvenanceOutputStorage(
            ProvenanceOutputCapacity + 1,
            KernelStorageGrowthReasonCodes.Threshold,
            context);
    }

    private void GrowProvenanceInvocationStorage(
        int minimumCapacity,
        int reasonCode,
        in KernelExecutionContext context)
    {
        var previousCapacity = ProvenanceInvocationCapacity;
        var nextCapacity = KernelGrowableStorage.GrowthCapacity(
            previousCapacity,
            minimumCapacity,
            _growableStoragePolicy.GrowthFactor);
        _provenanceInvocationTick = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationTick, nextCapacity);
        _provenanceInvocationProducerKindCode = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationProducerKindCode, nextCapacity);
        _provenanceInvocationProducerId = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationProducerId, nextCapacity);
        _provenanceInvocationProgramId = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationProgramId, nextCapacity);
        _provenanceInvocationSourceCodeId = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationSourceCodeId, nextCapacity);
        _provenanceInvocationActionId = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationActionId, nextCapacity);
        _provenanceInvocationPrimaryEntityId = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationPrimaryEntityId, nextCapacity);
        _provenanceInvocationSecondaryEntityId = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationSecondaryEntityId, nextCapacity);
        _provenanceInvocationParentInvocationId = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationParentInvocationId, nextCapacity);
        _provenanceInvocationTriggerKindCode = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationTriggerKindCode, nextCapacity);
        _provenanceInvocationTriggerId = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationTriggerId, nextCapacity);
        _provenanceInvocationFirstOutputId = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationFirstOutputId, nextCapacity);
        _provenanceInvocationLastOutputId = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationLastOutputId, nextCapacity);
        _provenanceInvocationOutputCount = KernelGrowableStorage.GrowInt32Array(_provenanceInvocationOutputCount, nextCapacity);
        WriteStorageGrown(
            KernelStorageCodes.ProvenanceInvocation,
            reasonCode,
            _provenanceInvocationHighWatermark,
            previousCapacity,
            nextCapacity,
            context);
    }

    private void GrowProvenanceOutputStorage(
        int minimumCapacity,
        int reasonCode,
        in KernelExecutionContext context)
    {
        var previousCapacity = ProvenanceOutputCapacity;
        var nextCapacity = KernelGrowableStorage.GrowthCapacity(
            previousCapacity,
            minimumCapacity,
            _growableStoragePolicy.GrowthFactor);
        _provenanceOutputInvocationId = KernelGrowableStorage.GrowInt32Array(_provenanceOutputInvocationId, nextCapacity);
        _provenanceOutputKindCode = KernelGrowableStorage.GrowInt32Array(_provenanceOutputKindCode, nextCapacity);
        _provenanceOutputId = KernelGrowableStorage.GrowInt32Array(_provenanceOutputId, nextCapacity);
        _provenanceOutputSubjectEntityId = KernelGrowableStorage.GrowInt32Array(_provenanceOutputSubjectEntityId, nextCapacity);
        _provenanceOutputSecondaryEntityId = KernelGrowableStorage.GrowInt32Array(_provenanceOutputSecondaryEntityId, nextCapacity);
        _provenanceOutputValueId = KernelGrowableStorage.GrowInt32Array(_provenanceOutputValueId, nextCapacity);
        _provenanceOutputChangeCodeId = KernelGrowableStorage.GrowInt32Array(_provenanceOutputChangeCodeId, nextCapacity);
        _provenanceOutputNextId = KernelGrowableStorage.GrowInt32Array(_provenanceOutputNextId, nextCapacity);
        WriteStorageGrown(
            KernelStorageCodes.ProvenanceOutput,
            reasonCode,
            _provenanceOutputHighWatermark,
            previousCapacity,
            nextCapacity,
            context);
    }

    private void WriteStorageGrown(
        int storageCode,
        int reasonCode,
        int highWatermark,
        int previousCapacity,
        int currentCapacity,
        in KernelExecutionContext context)
    {
        Write(KernelEvent.From(new KernelStorageGrownKernelEvent(
            CurrentTick,
            WorldVersion,
            storageCode,
            reasonCode,
            highWatermark,
            previousCapacity,
            currentCapacity,
            _growableStoragePolicy.ThresholdPermille)),
            context);
    }

    private static void ThrowCapacityExceeded(int capacityCode, int requestedIndex, int capacity) =>
        throw new KernelCapacityExceededException(capacityCode, requestedIndex, capacity);

    private static void ThrowInvalidProvenanceId() =>
        throw new global::System.InvalidOperationException();

    private static void ThrowInvalidProvenanceOutputId() =>
        throw new global::System.InvalidOperationException();

    private void EnsureProvenanceInvocation(int invocationId)
    {
        if ((uint)invocationId >= (uint)_provenanceInvocationTick.Length ||
            _provenanceInvocationProducerKindCode[invocationId] == 0)
        {
            ThrowInvalidProvenanceId();
        }
    }

    private void EnsureProvenanceOutput(int outputId)
    {
        if ((uint)outputId >= (uint)_provenanceOutputInvocationId.Length ||
            _provenanceOutputInvocationId[outputId] == 0)
        {
            ThrowInvalidProvenanceOutputId();
        }
    }
}
