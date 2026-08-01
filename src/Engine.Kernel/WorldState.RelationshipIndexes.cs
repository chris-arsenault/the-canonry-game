namespace Engine.Kernel;

using Engine.Constants;
using Engine.Eventing;

internal sealed partial class WorldState
{
    public void AdvanceWorldVersion()
    {
        if (_singleWorker)
        {
            if (_worldVersionTick != CurrentTick)
            {
                WorldVersion++;
                _worldVersionTick = CurrentTick;
            }

            return;
        }

        if (global::System.Threading.Volatile.Read(ref _worldVersionTick) == CurrentTick)
        {
            return;
        }

        var spin = new global::System.Threading.SpinWait();
        while (global::System.Threading.Interlocked.CompareExchange(
                   ref _worldVersionGate,
                   1,
                   0) != 0)
        {
            spin.SpinOnce();
        }

        try
        {
            if (_worldVersionTick != CurrentTick)
            {
                WorldVersion++;
                global::System.Threading.Volatile.Write(
                    ref _worldVersionTick,
                    CurrentTick);
            }
        }
        finally
        {
            global::System.Threading.Volatile.Write(ref _worldVersionGate, 0);
        }
    }

    private int FindEntitySlot()
    {
        if (_freeEntitySlotCount > 0)
        {
            return _freeEntitySlots[--_freeEntitySlotCount];
        }

        if (_entityHighWatermark >= EntityCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        _entityHighWatermark++;
        return _entityHighWatermark;
    }

    private int FindRelationshipSlot()
    {
        if (_freeRelationshipSlotCount > 0)
        {
            return _freeRelationshipSlots[--_freeRelationshipSlotCount];
        }

        if (_relationshipHighWatermark >= RelationshipCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        _relationshipHighWatermark++;
        return _relationshipHighWatermark;
    }

    internal void ReserveEntitySlots(global::System.Span<int> entityIds)
    {
        for (var index = 0; index < entityIds.Length; index++)
        {
            entityIds[index] = FindEntitySlot();
        }
    }

    internal int ReserveEntitySlot() => FindEntitySlot();

    internal void ReleaseUnusedReservedEntitySlot(int entityId)
    {
        if ((uint)entityId == 0u ||
            entityId > EntityCapacity ||
            _entityAllocated[entityId])
        {
            throw new global::System.InvalidOperationException();
        }

        _freeEntitySlots[_freeEntitySlotCount++] = entityId;
    }

    internal void ReserveRelationshipSlots(
        global::System.Span<int> relationshipIds)
    {
        for (var index = 0; index < relationshipIds.Length; index++)
        {
            relationshipIds[index] = FindRelationshipSlot();
        }
    }

    internal int ReserveRelationshipSlot() => FindRelationshipSlot();

    internal void ReleaseUnusedReservedRelationshipSlot(int relationshipId)
    {
        if ((uint)relationshipId == 0u ||
            relationshipId > RelationshipCapacity ||
            _relationshipAllocated[relationshipId])
        {
            throw new global::System.InvalidOperationException();
        }

        _freeRelationshipSlots[_freeRelationshipSlotCount++] = relationshipId;
    }

    private void ReleaseEntitySlot(int entityId)
    {
        ClearEntitySlot(entityId);
        AdvanceEntityGeneration(entityId);
        _freeEntitySlots[_freeEntitySlotCount++] = entityId;
    }

    private void ClearEntitySlot(int entityId)
    {
        _entityAllocated[entityId] = false;
        _entityStatus[entityId] = 0;
        _entityTagBits[entityId] = 0;
        _entityKindId[entityId] = 0;
        _entitySubtypeId[entityId] = 0;
        _entityCultureId[entityId] = 0;
        _entityCultureInfluenceMultiplierDeltas[entityId] = 0;
        _entityPhysicalInfluenceProduction[entityId] = 0;
        _entitySemanticInfluenceProduction[entityId] = 0;
        _entityPhysicalQ[entityId] = 0;
        _entityPhysicalR[entityId] = 0;
        _entityPhysicalZ[entityId] = 0;
        _entitySemanticQ[entityId] = 0;
        _entitySemanticR[entityId] = 0;
        _entitySemanticS[entityId] = 0;
        _entityAnchorPhysicalQ[entityId] = 0;
        _entityAnchorPhysicalR[entityId] = 0;
        _entityAnchorSemanticQ[entityId] = 0;
        _entityAnchorSemanticR[entityId] = 0;
        _entityFavorMultiplierDelta[entityId] = 0;
        _entityFavorExpiresAtTick[entityId] = 0;
        _entityWorkTiers[entityId] = 0;
        _entityCreatedAt[entityId] = 0;
        _entityUpdatedAt[entityId] = 0;
        _entityCreatedByInvocationId[entityId] = 0;
        global::System.Array.Clear(_attributes, entityId * _attributeStride, _attributeStride);
        global::System.Array.Clear(_attributeAssigned, entityId * _attributeStride, _attributeStride);
        global::System.Array.Clear(_traits, entityId * _traitStride, _traitStride);
    }

    private void AdvanceEntityGeneration(int entityId)
    {
        var maximum = (global::System.Int32.MaxValue - entityId) / EntityCapacity;
        if (_entityGenerations[entityId] >= maximum)
        {
            throw new global::System.InvalidOperationException();
        }

        _entityGenerations[entityId]++;
    }

    private void ReleaseRelationshipSlot(int relationshipId)
    {
        _relationshipAllocated[relationshipId] = false;
        _relationshipActive[relationshipId] = false;
        _relationshipKindId[relationshipId] = 0;
        _relationshipPolarityCode[relationshipId] = 0;
        _relationshipSourceId[relationshipId] = 0;
        _relationshipTargetId[relationshipId] = 0;
        _relationshipStartTick[relationshipId] = 0;
        _relationshipEndTick[relationshipId] = 0;
        _relationshipStrength[relationshipId] = 0;
        _relationshipCreatedByInvocationId[relationshipId] = 0;
        _relationshipLastChangedByInvocationId[relationshipId] = 0;
        var maximum = (global::System.Int32.MaxValue - relationshipId) / RelationshipCapacity;
        if (_relationshipGenerations[relationshipId] >= maximum)
        {
            throw new global::System.InvalidOperationException();
        }

        _relationshipGenerations[relationshipId]++;
        _freeRelationshipSlots[_freeRelationshipSlotCount++] = relationshipId;
    }

    internal int EntityOccurrenceHandle(int entityId)
    {
        EnsureEntity(entityId);
        return checked((_entityGenerations[entityId] * EntityCapacity) + entityId);
    }

    internal int EntityOccurrenceHandleForIntent(int entityId)
    {
        if ((uint)entityId == 0u || entityId > EntityCapacity)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(entityId));
        }

        return checked(
            (_entityGenerations[entityId] * EntityCapacity) + entityId);
    }

    internal int RelationshipOccurrenceHandle(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        return checked((_relationshipGenerations[relationshipId] * RelationshipCapacity) + relationshipId);
    }

    internal int RelationshipOccurrenceHandleForIntent(int relationshipId)
    {
        if ((uint)relationshipId == 0u ||
            relationshipId > RelationshipCapacity)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(relationshipId));
        }

        return checked(
            (_relationshipGenerations[relationshipId] *
                RelationshipCapacity) +
            relationshipId);
    }

    internal bool TryResolveRelationshipOccurrence(int occurrenceHandle, out int relationshipId)
    {
        if (occurrenceHandle <= 0)
        {
            relationshipId = 0;
            return false;
        }

        relationshipId = ((occurrenceHandle - 1) % RelationshipCapacity) + 1;
        var generation = (occurrenceHandle - relationshipId) / RelationshipCapacity;
        return _relationshipAllocated[relationshipId] &&
            _relationshipActive[relationshipId] &&
            _relationshipGenerations[relationshipId] == generation;
    }

    internal bool TryResolveEntityOccurrence(int occurrenceHandle, out int entityId)
    {
        if (occurrenceHandle <= 0)
        {
            entityId = 0;
            return false;
        }

        entityId = ((occurrenceHandle - 1) % EntityCapacity) + 1;
        var generation = (occurrenceHandle - entityId) / EntityCapacity;
        return _entityAllocated[entityId] &&
            _entityStatus[entityId] == EntityStatusCodes.Active &&
            _entityGenerations[entityId] == generation;
    }

    private void InsertEntityKindIndex(int entityId, int kindId)
    {
        EnsureKindIndex(kindId);
        var current = _firstEntityByKind[kindId];
        _nextEntityByKind[entityId] = current;
        _previousEntityByKind[entityId] = 0;
        if (current > 0)
        {
            _previousEntityByKind[current] = entityId;
        }

        _firstEntityByKind[kindId] = entityId;
    }

    private void RemoveEntityKindIndex(int entityId, int kindId)
    {
        EnsureKindIndex(kindId);
        var previous = _previousEntityByKind[entityId];
        var next = _nextEntityByKind[entityId];
        if (previous > 0)
        {
            _nextEntityByKind[previous] = next;
        }
        else
        {
            _firstEntityByKind[kindId] = next;
        }

        if (next > 0)
        {
            _previousEntityByKind[next] = previous;
        }

        _previousEntityByKind[entityId] = 0;
        _nextEntityByKind[entityId] = 0;
    }

    private void InsertEntityCultureIndex(int entityId, int cultureId)
    {
        EnsureCultureIndex(cultureId);
        _activeEntityCountByCulture[cultureId]++;
        var current = _firstEntityByCulture[cultureId];
        _nextEntityByCulture[entityId] = current;
        _previousEntityByCulture[entityId] = 0;
        if (current > 0)
        {
            _previousEntityByCulture[current] = entityId;
        }

        _firstEntityByCulture[cultureId] = entityId;
    }

    private void RemoveEntityCultureIndex(int entityId, int cultureId)
    {
        EnsureCultureIndex(cultureId);
        _activeEntityCountByCulture[cultureId]--;
        var previous = _previousEntityByCulture[entityId];
        var next = _nextEntityByCulture[entityId];
        if (previous > 0)
        {
            _nextEntityByCulture[previous] = next;
        }
        else
        {
            _firstEntityByCulture[cultureId] = next;
        }

        if (next > 0)
        {
            _previousEntityByCulture[next] = previous;
        }

        _previousEntityByCulture[entityId] = 0;
        _nextEntityByCulture[entityId] = 0;
    }

    private void InsertActiveEntity(int entityId)
    {
        _activeEntitySlots[_activeEntityCount] = entityId;
        _activeEntityPositions[entityId] = _activeEntityCount + 1;
        _activeEntityCount++;
    }

    private void RemoveActiveEntity(int entityId)
    {
        var position = _activeEntityPositions[entityId] - 1;
        for (var index = position + 1; index < _activeEntityCount; index++)
        {
            var movedEntityId = _activeEntitySlots[index];
            _activeEntitySlots[index - 1] = movedEntityId;
            _activeEntityPositions[movedEntityId] = index;
        }

        _activeEntityCount--;
        _activeEntitySlots[_activeEntityCount] = 0;
        _activeEntityPositions[entityId] = 0;
    }

    private void InsertActiveRelationship(int relationshipId)
    {
        _activeRelationshipSlots[_activeRelationshipCount] = relationshipId;
        _activeRelationshipPositions[relationshipId] = _activeRelationshipCount + 1;
        _activeRelationshipCount++;
    }

    private void RemoveActiveRelationship(int relationshipId)
    {
        var position = _activeRelationshipPositions[relationshipId] - 1;
        var lastPosition = --_activeRelationshipCount;
        var movedRelationshipId = _activeRelationshipSlots[lastPosition];
        _activeRelationshipSlots[position] = movedRelationshipId;
        _activeRelationshipPositions[movedRelationshipId] = position + 1;
        _activeRelationshipSlots[lastPosition] = 0;
        _activeRelationshipPositions[relationshipId] = 0;
    }

    private int TraverseRelationshipsOfKind<TVisitor>(
        int startEntityId,
        int kindId,
        FixedScalar minimumStrength,
        int maxDepth,
        int directionCode,
        ref TVisitor visitor)
        where TVisitor : struct, IRelationshipTraversalVisitor
    {
        EnsureEntity(startEntityId);
        ValidateRelationshipKindIndex(kindId);
        ValidateRelationshipThreshold(minimumStrength);
        NumericGuard.ThrowIfNegative(maxDepth);
        if (maxDepth == 0)
        {
            return 0;
        }

        _graphVisited.BeginEpoch();
        var currentFrontier = _graphFrontierA;
        var nextFrontier = _graphFrontierB;
        currentFrontier[0] = startEntityId;
        var currentCount = 1;
        var visitedCount = 0;
        _graphVisited.Add(startEntityId);

        for (var depth = 0; depth < maxDepth && currentCount > 0; depth++)
        {
            var nextCount = TraverseRelationshipFrontier(
                currentFrontier,
                nextFrontier,
                currentCount,
                kindId,
                minimumStrength,
                directionCode,
                depth,
                ref visitedCount,
                ref visitor);
            var swap = currentFrontier;
            currentFrontier = nextFrontier;
            nextFrontier = swap;
            currentCount = nextCount;
        }

        return visitedCount;
    }

    private int TraverseRelationshipFrontier<TVisitor>(
        int[] currentFrontier,
        int[] nextFrontier,
        int currentCount,
        int kindId,
        FixedScalar minimumStrength,
        int directionCode,
        int depth,
        ref int visitedCount,
        ref TVisitor visitor)
        where TVisitor : struct, IRelationshipTraversalVisitor
    {
        var nextCount = 0;
        for (var i = 0; i < currentCount; i++)
        {
            TraverseRelationshipFrontierEntity(
                currentFrontier[i],
                nextFrontier,
                kindId,
                minimumStrength,
                directionCode,
                depth,
                ref nextCount,
                ref visitedCount,
                ref visitor);
        }

        return nextCount;
    }

    private void TraverseRelationshipFrontierEntity<TVisitor>(
        int entityId,
        int[] nextFrontier,
        int kindId,
        FixedScalar minimumStrength,
        int directionCode,
        int depth,
        ref int nextCount,
        ref int visitedCount,
        ref TVisitor visitor)
        where TVisitor : struct, IRelationshipTraversalVisitor
    {
        var cursor = directionCode == OutgoingRelationshipDirectionCode
            ? OutgoingRelationshipsOfKindAbove(entityId, kindId, minimumStrength)
            : IncomingRelationshipsOfKindAbove(entityId, kindId, minimumStrength);
        while (MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var nextEntityId = directionCode == OutgoingRelationshipDirectionCode
                ? _relationshipTargetId[relationshipId]
                : _relationshipSourceId[relationshipId];
            if (_graphVisited.Contains(nextEntityId))
            {
                continue;
            }

            _graphVisited.Add(nextEntityId);
            visitor.Visit(nextEntityId, relationshipId, depth + 1);
            nextFrontier[nextCount] = nextEntityId;
            nextCount++;
            visitedCount++;
        }
    }

    private int RelationshipRowIndex(int entityId, int kindId)
    {
        EnsureEntity(entityId);
        ValidateRelationshipKindIndex(kindId);
        return (kindId - 1) * (_entityAllocated.Length - 1) + entityId - 1;
    }

    private int FrozenCursorRelationshipId(RelationshipGraphCursor cursor)
    {
        var occurrenceHandle = cursor.DirectionCode == OutgoingRelationshipDirectionCode
            ? _endpointOutgoingFrozenRelationships[cursor.FrozenIndex]
            : _endpointIncomingFrozenRelationships[cursor.FrozenIndex];
        return TryResolveRelationshipOccurrence(occurrenceHandle, out var relationshipId)
            ? relationshipId
            : 0;
    }

    private int NextDeltaRelationship(RelationshipGraphCursor cursor)
    {
        return cursor.DirectionCode == OutgoingRelationshipDirectionCode
            ? _endpointNextOutgoingDeltaRelationship[cursor.DeltaRelationshipId]
            : _endpointNextIncomingDeltaRelationship[cursor.DeltaRelationshipId];
    }

    internal bool RelationshipIsHotActive(int relationshipId)
    {
        return (uint)relationshipId < (uint)_relationshipActive.Length &&
            _relationshipActive[relationshipId] &&
            EntityIsActiveRaw(_relationshipSourceId[relationshipId]) &&
            EntityIsActiveRaw(_relationshipTargetId[relationshipId]);
    }

    private bool EntityIsActiveRaw(int entityId)
    {
        return (uint)entityId < (uint)_entityAllocated.Length &&
            _entityAllocated[entityId] &&
            _entityStatus[entityId] == EntityStatusCodes.Active;
    }

    private void EnsureEntity(int entityId)
    {
        if (!EntityExists(entityId))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureRelationship(int relationshipId)
    {
        if ((uint)relationshipId >= (uint)_relationshipAllocated.Length || !_relationshipAllocated[relationshipId])
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureKindIndex(int kindId)
    {
        ValidatePositive(kindId);
        if ((uint)kindId >= (uint)_firstEntityByKind.Length)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureCultureIndex(int cultureId)
    {
        ValidatePositive(cultureId);
        if ((uint)cultureId >= (uint)_firstEntityByCulture.Length)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void ValidateRelationshipKindIndex(int kindId)
    {
        ValidatePositive(kindId);
        if ((uint)kindId > (uint)_relationshipKindCapacity)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void WriteRelationshipChanged(
        int relationshipId,
        int tick,
        int changeCodeId,
        in KernelExecutionContext context,
        int previousStrengthUnits,
        int newStrengthUnits)
    {
        Write(KernelEvent.From(new RelationshipChangedKernelEvent(
            tick,
            WorldVersion,
            changeCodeId,
            context.SourceCodeId,
            _relationshipKindId[relationshipId],
            _relationshipPolarityCode[relationshipId],
            EntityOccurrenceHandle(_relationshipSourceId[relationshipId]),
            EntityOccurrenceHandle(_relationshipTargetId[relationshipId]),
            RelationshipOccurrenceHandle(relationshipId),
            previousStrengthUnits,
            newStrengthUnits,
            context.ActionId,
            newStrengthUnits > 0 ? 1 : 0)),
            context);
    }

    private void Write(in KernelEvent kernelEvent, in KernelExecutionContext context)
    {
        var contextualEvent = kernelEvent.WithExecutionContext(
            context.ProgramId,
            context.SourceCodeId,
            context.ProvenanceInvocationId);
        if (!_events.TryWrite(contextualEvent))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    internal int EntityOccurrenceHandleOrZero(int entityId)
    {
        return entityId == 0 ? 0 : EntityOccurrenceHandle(entityId);
    }

    private static void ValidatePositive(int value)
    {
        NumericGuard.ThrowIfNegativeOrZero(value);
    }

    private static void ValidateInfluencePlaneCode(int planeCode)
    {
        if (planeCode is not (PlaneCodes.Physical or PlaneCodes.Semantic))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }
    }

    private static void ValidateRelationshipThreshold(FixedScalar minimumStrength)
    {
        if (FixedScalar.IsNaN(minimumStrength))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static FixedScalar Clamp01(FixedScalar value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > 1 ? 1 : value;
    }

    private FixedScalar ClampAttribute(int attributeId, FixedScalar value)
    {
        return _kernelData.Entities.ClampAttribute(attributeId, value);
    }
}

internal partial struct RelationshipEndpointIndex
{
    internal void Attach(WorldState world, int relationshipId)
    {
        InsertIncidentRelationship(relationshipId);
        AddRelationshipToGraphDelta(world, relationshipId);
    }

    internal void Detach(WorldState world, int relationshipId)
    {
        RemoveIncidentRelationship(relationshipId);
        RemoveRelationshipFromGraphDelta(world, relationshipId);
    }

    internal int FirstIncidentRelationship(int entityId) =>
        _firstIncidentRelationship[entityId];

    internal int NextIncidentRelationship(int entityId, int relationshipId) =>
        IncidentNext(entityId, relationshipId);

    internal void Compact(WorldState world)
    {
        BuildRelationshipCsr(
            world,
            _outgoingFrozenOffsets,
            _outgoingFrozenRelationships,
            outgoing: true);
        BuildRelationshipCsr(
            world,
            _incomingFrozenOffsets,
            _incomingFrozenRelationships,
            outgoing: false);
        global::System.Array.Clear(_firstOutgoingDeltaRelationshipByRow);
        global::System.Array.Clear(_firstIncomingDeltaRelationshipByRow);
        global::System.Array.Clear(_nextOutgoingDeltaRelationship);
        global::System.Array.Clear(_nextIncomingDeltaRelationship);
        global::System.Array.Clear(_relationshipInDeltaGraph);
        _graphDeltaCount = 0;
    }

    private void InsertIncidentRelationship(int relationshipId)
    {
        var sourceEntityId = _relationshipSourceId[relationshipId];
        var targetEntityId = _relationshipTargetId[relationshipId];
        InsertIncidentEndpoint(sourceEntityId, relationshipId);
        if (targetEntityId != sourceEntityId)
        {
            InsertIncidentEndpoint(targetEntityId, relationshipId);
        }

    }

    private void InsertIncidentEndpoint(int entityId, int relationshipId)
    {
        var current = _firstIncidentRelationship[entityId];
        SetIncidentPrevious(entityId, relationshipId, 0);
        SetIncidentNext(entityId, relationshipId, current);
        if (current > 0)
        {
            SetIncidentPrevious(entityId, current, relationshipId);
        }

        _firstIncidentRelationship[entityId] = relationshipId;
    }

    private void RemoveIncidentRelationship(int relationshipId)
    {
        var sourceEntityId = _relationshipSourceId[relationshipId];
        var targetEntityId = _relationshipTargetId[relationshipId];
        RemoveIncidentEndpoint(sourceEntityId, relationshipId);
        if (targetEntityId != sourceEntityId)
        {
            RemoveIncidentEndpoint(targetEntityId, relationshipId);
        }

    }

    private void RemoveIncidentEndpoint(int entityId, int relationshipId)
    {
        var previous = IncidentPrevious(entityId, relationshipId);
        var next = IncidentNext(entityId, relationshipId);
        if (previous > 0)
        {
            SetIncidentNext(entityId, previous, next);
        }
        else
        {
            _firstIncidentRelationship[entityId] = next;
        }

        if (next > 0)
        {
            SetIncidentPrevious(entityId, next, previous);
        }

        SetIncidentPrevious(entityId, relationshipId, 0);
        SetIncidentNext(entityId, relationshipId, 0);
    }

    private int IncidentNext(int entityId, int relationshipId)
    {
        if (_relationshipSourceId[relationshipId] == entityId)
        {
            return _nextIncidentRelationshipFromSource[relationshipId];
        }

        if (_relationshipTargetId[relationshipId] == entityId)
        {
            return _nextIncidentRelationshipFromTarget[relationshipId];
        }

        throw new global::System.InvalidOperationException();
    }

    private int IncidentPrevious(int entityId, int relationshipId)
    {
        if (_relationshipSourceId[relationshipId] == entityId)
        {
            return _previousIncidentRelationshipFromSource[relationshipId];
        }

        if (_relationshipTargetId[relationshipId] == entityId)
        {
            return _previousIncidentRelationshipFromTarget[relationshipId];
        }

        throw new global::System.InvalidOperationException();
    }

    private void SetIncidentNext(
        int entityId,
        int relationshipId,
        int nextRelationshipId)
    {
        if (_relationshipSourceId[relationshipId] == entityId)
        {
            _nextIncidentRelationshipFromSource[relationshipId] = nextRelationshipId;
            return;
        }

        if (_relationshipTargetId[relationshipId] == entityId)
        {
            _nextIncidentRelationshipFromTarget[relationshipId] = nextRelationshipId;
            return;
        }

        throw new global::System.InvalidOperationException();
    }

    private void SetIncidentPrevious(
        int entityId,
        int relationshipId,
        int previousRelationshipId)
    {
        if (_relationshipSourceId[relationshipId] == entityId)
        {
            _previousIncidentRelationshipFromSource[relationshipId] = previousRelationshipId;
            return;
        }

        if (_relationshipTargetId[relationshipId] == entityId)
        {
            _previousIncidentRelationshipFromTarget[relationshipId] = previousRelationshipId;
            return;
        }

        throw new global::System.InvalidOperationException();
    }

    private void AddRelationshipToGraphDelta(WorldState world, int relationshipId)
    {
        if (!RelationshipIsHotActive(relationshipId))
        {
            return;
        }

        RemoveRelationshipFromGraphDelta(world, relationshipId);
        if (_graphDeltaCount >= _nextOutgoingDeltaRelationship.Length - 1)
        {
            Compact(world);
        }

        InsertDeltaRelationship(
            _firstOutgoingDeltaRelationshipByRow,
            _nextOutgoingDeltaRelationship,
            RelationshipRowIndex(
                _relationshipSourceId[relationshipId],
                _relationshipKindId[relationshipId]),
            relationshipId);
        InsertDeltaRelationship(
            _firstIncomingDeltaRelationshipByRow,
            _nextIncomingDeltaRelationship,
            RelationshipRowIndex(
                _relationshipTargetId[relationshipId],
                _relationshipKindId[relationshipId]),
            relationshipId);
        _relationshipInDeltaGraph[relationshipId] = true;
        _graphDeltaCount++;
        if (_graphDeltaMergeThreshold > 0 &&
            _graphDeltaCount >= _graphDeltaMergeThreshold)
        {
            Compact(world);
        }
    }

    private void RemoveRelationshipFromGraphDelta(WorldState world, int relationshipId)
    {
        if (!_relationshipInDeltaGraph[relationshipId])
        {
            return;
        }

        RemoveDeltaRelationship(
            _firstOutgoingDeltaRelationshipByRow,
            _nextOutgoingDeltaRelationship,
            RelationshipRowIndex(
                _relationshipSourceId[relationshipId],
                _relationshipKindId[relationshipId]),
            relationshipId);
        RemoveDeltaRelationship(
            _firstIncomingDeltaRelationshipByRow,
            _nextIncomingDeltaRelationship,
            RelationshipRowIndex(
                _relationshipTargetId[relationshipId],
                _relationshipKindId[relationshipId]),
            relationshipId);
        _relationshipInDeltaGraph[relationshipId] = false;
        _graphDeltaCount--;
    }

    private static void InsertDeltaRelationship(
        int[] firstRelationshipByRow,
        int[] nextRelationship,
        int row,
        int relationshipId)
    {
        var first = firstRelationshipByRow[row];
        if (first == 0 || relationshipId < first)
        {
            nextRelationship[relationshipId] = first;
            firstRelationshipByRow[row] = relationshipId;
            return;
        }

        var previous = first;
        var current = nextRelationship[previous];
        while (current > 0 && current < relationshipId)
        {
            previous = current;
            current = nextRelationship[current];
        }

        nextRelationship[relationshipId] = current;
        nextRelationship[previous] = relationshipId;
    }

    private static void RemoveDeltaRelationship(
        int[] firstRelationshipByRow,
        int[] nextRelationship,
        int row,
        int relationshipId)
    {
        var current = firstRelationshipByRow[row];
        if (current == relationshipId)
        {
            firstRelationshipByRow[row] = nextRelationship[relationshipId];
            nextRelationship[relationshipId] = 0;
            return;
        }

        while (current > 0)
        {
            var next = nextRelationship[current];
            if (next == relationshipId)
            {
                nextRelationship[current] = nextRelationship[relationshipId];
                nextRelationship[relationshipId] = 0;
                return;
            }

            current = next;
        }
    }

    private void BuildRelationshipCsr(
        WorldState world,
        int[] offsets,
        int[] relationships,
        bool outgoing)
    {
        global::System.Array.Clear(offsets);
        global::System.Array.Clear(relationships);
        for (var index = 0; index < world.ActiveRelationshipCount; index++)
        {
            var relationshipId = world.ActiveRelationshipAt(index);
            var entityId = outgoing
                ? _relationshipSourceId[relationshipId]
                : _relationshipTargetId[relationshipId];
            var row = RelationshipRowIndex(
                entityId,
                _relationshipKindId[relationshipId]);
            offsets[row + 1]++;
        }

        for (var index = 1; index < offsets.Length; index++)
        {
            offsets[index] += offsets[index - 1];
        }

        global::System.Array.Copy(offsets, _relationshipBuildCursors, offsets.Length);
        for (var index = 0; index < world.ActiveRelationshipCount; index++)
        {
            var relationshipId = world.ActiveRelationshipAt(index);
            var entityId = outgoing
                ? _relationshipSourceId[relationshipId]
                : _relationshipTargetId[relationshipId];
            var row = RelationshipRowIndex(
                entityId,
                _relationshipKindId[relationshipId]);
            var slot = _relationshipBuildCursors[row];
            relationships[slot] = RelationshipOccurrenceHandle(relationshipId);
            _relationshipBuildCursors[row]++;
        }

        SortRelationshipCsrRows(_relationshipCapacity, offsets, relationships);
    }

    private static void SortRelationshipCsrRows(
        int relationshipCapacity,
        int[] offsets,
        int[] relationships)
    {
        for (var row = 0; row < offsets.Length - 1; row++)
        {
            var start = offsets[row];
            var end = offsets[row + 1];
            for (var index = start + 1; index < end; index++)
            {
                var relationshipOccurrence = relationships[index];
                var cursor = index - 1;
                while (cursor >= start &&
                       FrozenOccurrenceComesBefore(
                           relationshipCapacity,
                           relationshipOccurrence,
                           relationships[cursor]))
                {
                    relationships[cursor + 1] = relationships[cursor];
                    cursor--;
                }

                relationships[cursor + 1] = relationshipOccurrence;
            }
        }
    }

}
