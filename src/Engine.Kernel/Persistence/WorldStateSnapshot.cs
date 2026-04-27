namespace Engine.Kernel;

using Engine.Eventing;

public sealed partial class WorldState
{
    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        KernelSnapshotBinary.WriteObject(writer, this);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        KernelSnapshotBinary.ReadObject(reader, this);
    }

    internal int SnapshotEventCount()
    {
        var count = 0;
        for (var q = -_mapRadius; q <= _mapRadius; q++)
        {
            var minR = global::System.Math.Max(-_mapRadius, -q - _mapRadius);
            var maxR = global::System.Math.Min(_mapRadius, -q + _mapRadius);
            for (var r = minR; r <= maxR; r++)
            {
                if (CellIsKnown(q, r))
                {
                    count++;
                }
            }
        }

        for (var entityId = 1; entityId <= _entityHighWatermark; entityId++)
        {
            if (!_entityAllocated[entityId])
            {
                continue;
            }

            count++;
            if (_entityStatus[entityId] != EntityStatusCodes.Active)
            {
                count++;
            }
        }

        count += _attributes.Count;
        count += _resources.Count;
        for (var relationshipId = 1; relationshipId <= _relationshipHighWatermark; relationshipId++)
        {
            if (_relationshipAllocated[relationshipId] && _relationshipActive[relationshipId])
            {
                count++;
            }
        }

        return count;
    }

    internal void EmitSnapshotEvents(int attributeSourceCodeId)
    {
        var tick = CurrentTick;
        var version = WorldVersion;
        for (var q = -_mapRadius; q <= _mapRadius; q++)
        {
            var minR = global::System.Math.Max(-_mapRadius, -q - _mapRadius);
            var maxR = global::System.Math.Min(_mapRadius, -q + _mapRadius);
            for (var r = minR; r <= maxR; r++)
            {
                if (!CellIsKnown(q, r))
                {
                    continue;
                }

                Write(KernelEvent.From(new CellCreatedKernelEvent(
                    tick,
                    version,
                    q,
                    r,
                    attributeSourceCodeId,
                    CellTerrainId(q, r),
                    CellCoverageOwner(q, r))));
            }
        }

        for (var entityId = 1; entityId <= _entityHighWatermark; entityId++)
        {
            if (!_entityAllocated[entityId])
            {
                continue;
            }

            Write(KernelEvent.From(new EntityCreatedKernelEvent(
                tick,
                version,
                entityId,
                attributeSourceCodeId,
                _entityKindId[entityId],
                _entitySubtypeId[entityId],
                _entityCultureId[entityId],
                _entityPhysicalQ[entityId],
                _entityPhysicalR[entityId],
                _entitySemanticQ[entityId],
                _entitySemanticR[entityId],
                _entitySemanticS[entityId])));

            if (_entityStatus[entityId] != EntityStatusCodes.Active)
            {
                Write(KernelEvent.From(new EntityStatusChangedKernelEvent(
                    tick,
                    version,
                    entityId,
                    attributeSourceCodeId,
                    _entityKindId[entityId],
                    _entitySubtypeId[entityId],
                    EntityStatusCodes.Active,
                    _entityStatus[entityId])));
            }
        }

        _attributes.ForEach((entityId, attributeId, value) =>
        {
            if (_entityAllocated[entityId])
            {
                Write(KernelEvent.From(new AttributeChangedKernelEvent(
                    tick,
                    version,
                    entityId,
                    attributeId,
                    attributeSourceCodeId,
                    0f,
                    value)));
            }
        });

        _resources.ForEach((resourceId, coordinateKey, value) =>
        {
            if (TryResolveCoordinateKey(coordinateKey, out var q, out var r))
            {
                Write(KernelEvent.From(new ResourceChangedKernelEvent(
                    tick,
                    version,
                    resourceId,
                    q,
                    r,
                    attributeSourceCodeId,
                    0f,
                    value)));
            }
        });

        for (var relationshipId = 1; relationshipId <= _relationshipHighWatermark; relationshipId++)
        {
            if (!_relationshipAllocated[relationshipId] || !_relationshipActive[relationshipId])
            {
                continue;
            }

            Write(KernelEvent.From(new RelationshipChangedKernelEvent(
                tick,
                version,
                _relationshipCreatedCodeId,
                attributeSourceCodeId,
                _relationshipKindId[relationshipId],
                _relationshipPolarityCode[relationshipId],
                _relationshipSourceId[relationshipId],
                _relationshipTargetId[relationshipId],
                _relationshipFactId0[relationshipId],
                _relationshipFactId1[relationshipId],
                _relationshipFactId2[relationshipId],
                _relationshipFactId3[relationshipId],
                _relationshipFactCount[relationshipId],
                0f,
                _relationshipStrength[relationshipId])));
        }
    }

    private bool TryResolveCoordinateKey(int coordinateKey, out int q, out int r)
    {
        for (q = -_mapRadius; q <= _mapRadius; q++)
        {
            var minR = global::System.Math.Max(-_mapRadius, -q - _mapRadius);
            var maxR = global::System.Math.Min(_mapRadius, -q + _mapRadius);
            for (r = minR; r <= maxR; r++)
            {
                if (CoordinateKey(q, r) == coordinateKey)
                {
                    return true;
                }
            }
        }

        q = 0;
        r = 0;
        return false;
    }
}
