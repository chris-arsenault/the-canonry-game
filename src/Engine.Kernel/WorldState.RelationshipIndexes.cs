namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class WorldState
{
    public void AdvanceWorldVersion()
    {
        WorldVersion++;
    }

    private int FindEntitySlot()
    {
        for (var i = 1; i < _entityAllocated.Length; i++)
        {
            if (!_entityAllocated[i])
            {
                return i;
            }
        }

        throw new global::System.InvalidOperationException();
    }

    private int FindRelationshipSlot()
    {
        for (var i = 1; i < _relationshipAllocated.Length; i++)
        {
            if (!_relationshipAllocated[i])
            {
                return i;
            }
        }

        throw new global::System.InvalidOperationException();
    }

    private void InsertEntityKindIndex(int entityId, int kindId)
    {
        EnsureKindIndex(kindId);
        _nextEntityByKind[entityId] = _firstEntityByKind[kindId];
        _firstEntityByKind[kindId] = entityId;
    }

    private void InsertPhysicalOccupant(int entityId, int q, int r)
    {
        var key = CoordinateKey(q, r);
        _nextPhysicalEntityAtCell[entityId] = _firstPhysicalEntityAtCell.GetOrDefault(key, 1);
        _firstPhysicalEntityAtCell.Set(key, 1, entityId);
    }

    private void RemovePhysicalOccupant(int entityId, int q, int r)
    {
        var key = CoordinateKey(q, r);
        var current = _firstPhysicalEntityAtCell.GetOrDefault(key, 1);
        if (current == entityId)
        {
            _firstPhysicalEntityAtCell.Set(key, 1, _nextPhysicalEntityAtCell[entityId]);
            _nextPhysicalEntityAtCell[entityId] = 0;
            return;
        }

        while (current > 0)
        {
            var next = _nextPhysicalEntityAtCell[current];
            if (next == entityId)
            {
                _nextPhysicalEntityAtCell[current] = _nextPhysicalEntityAtCell[entityId];
                _nextPhysicalEntityAtCell[entityId] = 0;
                return;
            }

            current = next;
        }
    }

    private void InsertSemanticOccupant(int entityId, int q, int r)
    {
        var key = CoordinateKey(q, r);
        _nextSemanticEntityAtCell[entityId] = _firstSemanticEntityAtCell.GetOrDefault(key, 1);
        _firstSemanticEntityAtCell.Set(key, 1, entityId);
    }

    private void RemoveSemanticOccupant(int entityId, int q, int r)
    {
        var key = CoordinateKey(q, r);
        var current = _firstSemanticEntityAtCell.GetOrDefault(key, 1);
        if (current == entityId)
        {
            _firstSemanticEntityAtCell.Set(key, 1, _nextSemanticEntityAtCell[entityId]);
            _nextSemanticEntityAtCell[entityId] = 0;
            return;
        }

        while (current > 0)
        {
            var next = _nextSemanticEntityAtCell[current];
            if (next == entityId)
            {
                _nextSemanticEntityAtCell[current] = _nextSemanticEntityAtCell[entityId];
                _nextSemanticEntityAtCell[entityId] = 0;
                return;
            }

            current = next;
        }
    }

    private int TraverseRelationshipsOfKind<TVisitor>(
        int startEntityId,
        int kindId,
        float minimumStrength,
        int maxDepth,
        int directionCode,
        ref TVisitor visitor)
        where TVisitor : struct, IRelationshipTraversalVisitor
    {
        EnsureEntity(startEntityId);
        ValidateRelationshipKindIndex(kindId);
        ValidateRelationshipThreshold(minimumStrength);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(maxDepth);
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
        float minimumStrength,
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
        float minimumStrength,
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

    private void AddRelationshipToGraphDelta(int relationshipId)
    {
        if (!_relationshipActive[relationshipId])
        {
            return;
        }

        RemoveRelationshipFromGraphDelta(relationshipId);
        if (_relationshipGraphDeltaCount >= _outgoingDeltaRelationships.Length)
        {
            CompactRelationshipGraph();
        }

        InsertDeltaRelationship(
            _outgoingDeltaOffsets,
            _outgoingDeltaRelationships,
            RelationshipRowIndexRaw(_relationshipSourceId[relationshipId], _relationshipKindId[relationshipId]),
            relationshipId);
        InsertDeltaRelationship(
            _incomingDeltaOffsets,
            _incomingDeltaRelationships,
            RelationshipRowIndexRaw(_relationshipTargetId[relationshipId], _relationshipKindId[relationshipId]),
            relationshipId);
        _relationshipInDeltaGraph[relationshipId] = true;
        _relationshipGraphDeltaCount++;
        if (_relationshipGraphDeltaMergeThreshold > 0 &&
            _relationshipGraphDeltaCount >= _relationshipGraphDeltaMergeThreshold)
        {
            CompactRelationshipGraph();
        }
    }

    private void RemoveRelationshipFromGraphDelta(int relationshipId)
    {
        if (!_relationshipInDeltaGraph[relationshipId])
        {
            return;
        }

        RemoveDeltaRelationship(
            _outgoingDeltaOffsets,
            _outgoingDeltaRelationships,
            RelationshipRowIndexRaw(_relationshipSourceId[relationshipId], _relationshipKindId[relationshipId]),
            relationshipId);
        RemoveDeltaRelationship(
            _incomingDeltaOffsets,
            _incomingDeltaRelationships,
            RelationshipRowIndexRaw(_relationshipTargetId[relationshipId], _relationshipKindId[relationshipId]),
            relationshipId);
        _relationshipInDeltaGraph[relationshipId] = false;
        _relationshipGraphDeltaCount--;
    }

    private void InsertDeltaRelationship(int[] offsets, int[] relationships, int row, int relationshipId)
    {
        var end = offsets[^1];
        if (end >= relationships.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var start = offsets[row];
        var insert = offsets[row + 1];
        for (var i = end; i > insert; i--)
        {
            relationships[i] = relationships[i - 1];
        }

        while (insert > start && RelationshipComesBefore(relationshipId, relationships[insert - 1]))
        {
            relationships[insert] = relationships[insert - 1];
            insert--;
        }

        relationships[insert] = relationshipId;
        for (var i = row + 1; i < offsets.Length; i++)
        {
            offsets[i]++;
        }
    }

    private void RemoveDeltaRelationship(int[] offsets, int[] relationships, int row, int relationshipId)
    {
        var start = offsets[row];
        var end = offsets[row + 1];
        for (var slot = start; slot < end; slot++)
        {
            if (relationships[slot] != relationshipId)
            {
                continue;
            }

            for (var i = slot; i < offsets[^1] - 1; i++)
            {
                relationships[i] = relationships[i + 1];
            }

            relationships[offsets[^1] - 1] = 0;
            for (var i = row + 1; i < offsets.Length; i++)
            {
                offsets[i]--;
            }

            return;
        }
    }

    private void BuildRelationshipCsr(int[] offsets, int[] relationships, bool outgoing)
    {
        global::System.Array.Clear(offsets);
        global::System.Array.Clear(relationships);
        for (var relationshipId = 1; relationshipId <= _relationshipHighWatermark; relationshipId++)
        {
            if (!_relationshipActive[relationshipId])
            {
                continue;
            }

            var entityId = outgoing ? _relationshipSourceId[relationshipId] : _relationshipTargetId[relationshipId];
            var row = RelationshipRowIndexRaw(entityId, _relationshipKindId[relationshipId]);
            offsets[row + 1]++;
        }

        for (var i = 1; i < offsets.Length; i++)
        {
            offsets[i] += offsets[i - 1];
        }

        global::System.Array.Copy(offsets, _relationshipBuildCursors, offsets.Length);
        for (var relationshipId = 1; relationshipId <= _relationshipHighWatermark; relationshipId++)
        {
            if (!_relationshipActive[relationshipId])
            {
                continue;
            }

            var entityId = outgoing ? _relationshipSourceId[relationshipId] : _relationshipTargetId[relationshipId];
            var row = RelationshipRowIndexRaw(entityId, _relationshipKindId[relationshipId]);
            var slot = _relationshipBuildCursors[row];
            relationships[slot] = relationshipId;
            _relationshipBuildCursors[row]++;
        }

        SortRelationshipCsrRows(offsets, relationships);
    }

    private void SortRelationshipCsrRows(int[] offsets, int[] relationships)
    {
        for (var row = 0; row < offsets.Length - 1; row++)
        {
            var start = offsets[row];
            var end = offsets[row + 1];
            for (var i = start + 1; i < end; i++)
            {
                var relationshipId = relationships[i];
                var j = i - 1;
                while (j >= start && RelationshipComesBefore(relationshipId, relationships[j]))
                {
                    relationships[j + 1] = relationships[j];
                    j--;
                }

                relationships[j + 1] = relationshipId;
            }
        }
    }

    private int FrozenCursorRelationshipId(RelationshipGraphCursor cursor)
    {
        return cursor.DirectionCode == OutgoingRelationshipDirectionCode
            ? _outgoingFrozenRelationships[cursor.FrozenIndex]
            : _incomingFrozenRelationships[cursor.FrozenIndex];
    }

    private int DeltaCursorRelationshipId(RelationshipGraphCursor cursor)
    {
        return cursor.DirectionCode == OutgoingRelationshipDirectionCode
            ? _outgoingDeltaRelationships[cursor.DeltaIndex]
            : _incomingDeltaRelationships[cursor.DeltaIndex];
    }

    private bool RelationshipComesBefore(int leftRelationshipId, int rightRelationshipId)
    {
        return leftRelationshipId < rightRelationshipId;
    }

    private int RelationshipRowIndex(int entityId, int kindId)
    {
        EnsureEntity(entityId);
        ValidateRelationshipKindIndex(kindId);
        return RelationshipRowIndexRaw(entityId, kindId);
    }

    private int RelationshipRowIndexRaw(int entityId, int kindId)
    {
        return (kindId - 1) * _relationshipEntityCapacity + entityId - 1;
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
        float previousStrength,
        float newStrength)
    {
        Write(KernelEvent.From(new RelationshipChangedKernelEvent(
            tick,
            WorldVersion,
            changeCodeId,
            context.SourceCodeId,
            _relationshipKindId[relationshipId],
            _relationshipPolarityCode[relationshipId],
            _relationshipSourceId[relationshipId],
            _relationshipTargetId[relationshipId],
            _relationshipFactId0[relationshipId],
            _relationshipFactId1[relationshipId],
            _relationshipFactId2[relationshipId],
            _relationshipFactId3[relationshipId],
            _relationshipFactCount[relationshipId],
            previousStrength,
            newStrength)),
            context);
    }

    private void Write(in KernelEvent kernelEvent, in KernelExecutionContext context)
    {
        var contextualEvent = kernelEvent.WithExecutionContext(
            context.ProgramId,
            context.SourceCodeId);
        if (!_events.TryWrite(contextualEvent))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static int CoordinateKey(int q, int r)
    {
        unchecked
        {
            var hash = (uint)q * 2654435761u;
            hash ^= (uint)r * 2246822519u;
            var value = (int)(hash & 2147483647u);
            return value == 0 ? 1 : value;
        }
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }

    private static float ClampInfluence(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 100f ? 100f : value;
    }

    private static void ValidateFactCount(int factCount)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(factCount);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThan(factCount, 4);
    }

    private static void ValidateRelationshipThreshold(float minimumStrength)
    {
        if (global::System.Single.IsNaN(minimumStrength))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private float ClampAttribute(int attributeId, float value)
    {
        return _kernelData.Entities.ClampAttribute(attributeId, value);
    }
}
