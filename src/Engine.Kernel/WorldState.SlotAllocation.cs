namespace Engine.Kernel;

internal sealed partial class WorldState
{
    private int FindEntitySlot()
    {
        var firstOwner = AllocationOwner(
            _entityAllocationOffsetsByOwner,
            ref _unownedEntityAllocationCursor,
            EntityCapacity);
        var allocationOwnerCount = global::System.Math.Min(
            _slotOwnerCount,
            EntityCapacity);
        for (var offset = 0; offset < allocationOwnerCount; offset++)
        {
            var owner = (firstOwner + offset) % allocationOwnerCount;
            if (TryFindEntitySlot(owner, out var slot))
            {
                return slot;
            }
        }

        throw new global::System.InvalidOperationException();
    }

    private bool TryFindEntitySlot(int owner, out int entityId)
    {
        lock (_slotOwnerGates[owner])
        {
            var freeCount = _freeEntitySlotCounts[owner];
            if (freeCount > 0)
            {
                entityId = _freeEntitySlots[
                    checked((owner * _entityFreeStride) + freeCount - 1)];
                _freeEntitySlotCounts[owner] = freeCount - 1;
                _entityReserved[entityId] = true;
                return true;
            }

            for (entityId = _nextEntitySlotsByOwner[owner];
                 entityId <= EntityCapacity;
                 entityId += _slotOwnerCount)
            {
                _nextEntitySlotsByOwner[owner] = checked(
                    entityId + _slotOwnerCount);
                if (_entityAllocated[entityId] ||
                    _entityReserved[entityId])
                {
                    continue;
                }

                _entityReserved[entityId] = true;
                AdvanceHighWatermark(
                    ref _entityHighWatermark,
                    entityId);
                return true;
            }
        }

        entityId = 0;
        return false;
    }

    private int FindRelationshipSlot()
    {
        var firstOwner = AllocationOwner(
            _relationshipAllocationOffsetsByOwner,
            ref _unownedRelationshipAllocationCursor,
            RelationshipCapacity);
        var allocationOwnerCount = global::System.Math.Min(
            _slotOwnerCount,
            RelationshipCapacity);
        for (var offset = 0; offset < allocationOwnerCount; offset++)
        {
            var owner = (firstOwner + offset) % allocationOwnerCount;
            if (TryFindRelationshipSlot(owner, out var slot))
            {
                return slot;
            }
        }

        throw new global::System.InvalidOperationException();
    }

    private bool TryFindRelationshipSlot(
        int owner,
        out int relationshipId)
    {
        lock (_slotOwnerGates[owner])
        {
            var freeCount = _freeRelationshipSlotCounts[owner];
            if (freeCount > 0)
            {
                relationshipId = _freeRelationshipSlots[
                    checked(
                        (owner * _relationshipFreeStride) +
                        freeCount - 1)];
                _freeRelationshipSlotCounts[owner] = freeCount - 1;
                _relationshipReserved[relationshipId] = true;
                return true;
            }

            for (relationshipId = _nextRelationshipSlotsByOwner[owner];
                 relationshipId <= RelationshipCapacity;
                 relationshipId += _slotOwnerCount)
            {
                _nextRelationshipSlotsByOwner[owner] = checked(
                    relationshipId + _slotOwnerCount);
                if (_relationshipAllocated[relationshipId] ||
                    _relationshipReserved[relationshipId])
                {
                    continue;
                }

                _relationshipReserved[relationshipId] = true;
                AdvanceHighWatermark(
                    ref _relationshipHighWatermark,
                    relationshipId);
                return true;
            }
        }

        relationshipId = 0;
        return false;
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
            _entityAllocated[entityId] ||
            !_entityReserved[entityId])
        {
            throw new global::System.InvalidOperationException();
        }

        ReleaseReservedEntitySlot(entityId);
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
            _relationshipAllocated[relationshipId] ||
            !_relationshipReserved[relationshipId])
        {
            throw new global::System.InvalidOperationException();
        }

        ReleaseReservedRelationshipSlot(relationshipId);
    }

    private void ReleaseEntitySlot(int entityId)
    {
        ClearEntitySlot(entityId);
        AdvanceEntityGeneration(entityId);
        ReleaseAllocatedEntitySlot(entityId);
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
        global::System.Array.Clear(
            _attributes,
            entityId * _attributeStride,
            _attributeStride);
        global::System.Array.Clear(
            _attributeAssigned,
            entityId * _attributeStride,
            _attributeStride);
        global::System.Array.Clear(
            _traits,
            entityId * _traitStride,
            _traitStride);
    }

    private void AdvanceEntityGeneration(int entityId)
    {
        var maximum =
            (global::System.Int32.MaxValue - entityId) / EntityCapacity;
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
        var maximum =
            (global::System.Int32.MaxValue - relationshipId) /
            RelationshipCapacity;
        if (_relationshipGenerations[relationshipId] >= maximum)
        {
            throw new global::System.InvalidOperationException();
        }

        _relationshipGenerations[relationshipId]++;
        ReleaseAllocatedRelationshipSlot(relationshipId);
    }

    private int AllocationOwner(
        int[] ownerOffsets,
        ref int unownedCursor,
        int capacity)
    {
        var allocationOwnerCount = global::System.Math.Min(
            _slotOwnerCount,
            capacity);
        var owner = KernelWorkerThread.OwnerIndex;
        if (owner >= 0)
        {
            var normalizedOwner = owner % allocationOwnerCount;
            var offset =
                global::System.Threading.Interlocked.Increment(
                    ref ownerOffsets[owner]) - 1;
            return (normalizedOwner + offset) % allocationOwnerCount;
        }

        return (global::System.Threading.Interlocked.Increment(
            ref unownedCursor) - 1) % allocationOwnerCount;
    }

    private static void AdvanceHighWatermark(
        ref int highWatermark,
        int slot)
    {
        var current = global::System.Threading.Volatile.Read(
            ref highWatermark);
        while (current < slot)
        {
            var observed =
                global::System.Threading.Interlocked.CompareExchange(
                    ref highWatermark,
                    slot,
                    current);
            if (observed == current)
            {
                return;
            }

            current = observed;
        }
    }

    private void ReleaseReservedEntitySlot(int entityId)
    {
        var owner = SlotOwner(entityId);
        lock (_slotOwnerGates[owner])
        {
            _entityReserved[entityId] = false;
            PushEntityFreeSlot(owner, entityId);
        }
    }

    private void ReleaseAllocatedEntitySlot(int entityId)
    {
        var owner = SlotOwner(entityId);
        lock (_slotOwnerGates[owner])
        {
            _entityReserved[entityId] = false;
            PushEntityFreeSlot(owner, entityId);
        }
    }

    private void PushEntityFreeSlot(int owner, int entityId)
    {
        var count = _freeEntitySlotCounts[owner];
        if (count >= _entityFreeStride)
        {
            throw new global::System.InvalidOperationException();
        }

        _freeEntitySlots[
            checked((owner * _entityFreeStride) + count)] = entityId;
        _freeEntitySlotCounts[owner] = count + 1;
    }

    private void ReleaseReservedRelationshipSlot(int relationshipId)
    {
        var owner = SlotOwner(relationshipId);
        lock (_slotOwnerGates[owner])
        {
            _relationshipReserved[relationshipId] = false;
            PushRelationshipFreeSlot(owner, relationshipId);
        }
    }

    private void ReleaseAllocatedRelationshipSlot(int relationshipId)
    {
        var owner = SlotOwner(relationshipId);
        lock (_slotOwnerGates[owner])
        {
            _relationshipReserved[relationshipId] = false;
            PushRelationshipFreeSlot(owner, relationshipId);
        }
    }

    private void PushRelationshipFreeSlot(
        int owner,
        int relationshipId)
    {
        var count = _freeRelationshipSlotCounts[owner];
        if (count >= _relationshipFreeStride)
        {
            throw new global::System.InvalidOperationException();
        }

        _freeRelationshipSlots[
            checked((owner * _relationshipFreeStride) + count)] =
            relationshipId;
        _freeRelationshipSlotCounts[owner] = count + 1;
    }

    private int SlotOwner(int slotId) =>
        (slotId - 1) % _slotOwnerCount;
}
