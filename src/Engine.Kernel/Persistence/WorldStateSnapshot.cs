namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class WorldState
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
        count += SnapshotCellEventCount(PlaneCodes.Physical, emitLegacyPhysicalEvents: true);
        count += SnapshotCellEventCount(PlaneCodes.Semantic, emitLegacyPhysicalEvents: false);

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
        count += _traits.Count;
        count += _mapLayers.CultureInfluence.Count;
        for (var relationshipId = 1; relationshipId <= _relationshipHighWatermark; relationshipId++)
        {
            if (_relationshipAllocated[relationshipId] && _relationshipActive[relationshipId])
            {
                count++;
            }
        }

        return count;
    }

    internal void EmitSnapshotEvents(KernelExecutionContext context)
    {
        var tick = CurrentTick;
        var version = WorldVersion;
        EmitSnapshotCells(context, tick, version);
        EmitSnapshotEntities(context, tick, version);
        EmitSnapshotAttributes(context, tick, version);
        EmitSnapshotTraits(context, tick, version);
        EmitSnapshotInfluence(context, tick, version);
        EmitSnapshotCultureInfluence(context, tick, version);
        EmitSnapshotRelationships(context, tick, version);
    }

    private void EmitSnapshotCells(KernelExecutionContext context, int tick, long version)
    {
        EmitSnapshotCells(context, tick, version, PlaneCodes.Physical, emitLegacyPhysicalEvents: true);
        EmitSnapshotCells(context, tick, version, PlaneCodes.Semantic, emitLegacyPhysicalEvents: false);
    }

    private int SnapshotCellEventCount(int planeCode, bool emitLegacyPhysicalEvents)
    {
        var count = 0;
        for (var q = -_mapRadius; q <= _mapRadius; q++)
        {
            var minR = global::System.Math.Max(-_mapRadius, -q - _mapRadius);
            var maxR = global::System.Math.Min(_mapRadius, -q + _mapRadius);
            for (var r = minR; r <= maxR; r++)
            {
                if (!CellIsKnown(planeCode, q, r))
                {
                    continue;
                }

                count++;
                if (emitLegacyPhysicalEvents)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void EmitSnapshotCells(
        KernelExecutionContext context,
        int tick,
        long version,
        int planeCode,
        bool emitLegacyPhysicalEvents)
    {
        for (var q = -_mapRadius; q <= _mapRadius; q++)
        {
            var minR = global::System.Math.Max(-_mapRadius, -q - _mapRadius);
            var maxR = global::System.Math.Min(_mapRadius, -q + _mapRadius);
            for (var r = minR; r <= maxR; r++)
            {
                if (!CellIsKnown(planeCode, q, r))
                {
                    continue;
                }

                Write(KernelEvent.From(new MapCellStateChangedKernelEvent(
                    tick,
                    version,
                    planeCode,
                    q,
                    r,
                    context.SourceCodeId,
                    0,
                    CellTerrainId(planeCode, q, r),
                    0,
                    CoverageOwnerAt(planeCode, q, r),
                    CellSuperregionId(planeCode, q, r),
                    CellContinentId(planeCode, q, r))),
                    context);
                if (emitLegacyPhysicalEvents)
                {
                    Write(KernelEvent.From(new CellCreatedKernelEvent(
                        tick,
                        version,
                        q,
                        r,
                        context.SourceCodeId,
                        CellTerrainId(q, r),
                        CoverageOwnerAt(q, r),
                        CellSuperregionId(q, r),
                        CellContinentId(q, r))),
                        context);
                }
            }
        }
    }

    private void EmitSnapshotEntities(KernelExecutionContext context, int tick, long version)
    {
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
                context.SourceCodeId,
                _entityKindId[entityId],
                _entitySubtypeId[entityId],
                _entityCultureId[entityId],
                _entityPhysicalQ[entityId],
                _entityPhysicalR[entityId],
                _entitySemanticQ[entityId],
                _entitySemanticR[entityId],
                _entitySemanticS[entityId])),
                context);

            if (_entityStatus[entityId] != EntityStatusCodes.Active)
            {
                Write(KernelEvent.From(new EntityStatusChangedKernelEvent(
                    tick,
                    version,
                    entityId,
                    context.SourceCodeId,
                    _entityKindId[entityId],
                    _entitySubtypeId[entityId],
                    EntityStatusCodes.Active,
                    _entityStatus[entityId])),
                    context);
            }
        }
    }

    private void EmitSnapshotAttributes(KernelExecutionContext context, int tick, long version)
    {
        _attributes.ForEach((entityId, attributeId, value) =>
        {
            if (_entityAllocated[entityId])
            {
                Write(KernelEvent.From(new AttributeChangedKernelEvent(
                    tick,
                    version,
                    entityId,
                    attributeId,
                    context.SourceCodeId,
                    0f,
                    value)),
                    context);
            }
        });
    }

    private void EmitSnapshotTraits(KernelExecutionContext context, int tick, long version)
    {
        _traits.ForEach((entityId, traitId, assigned) =>
        {
            if (_entityAllocated[entityId] && assigned != 0)
            {
                Write(KernelEvent.From(new TraitAssignedKernelEvent(
                    tick,
                    version,
                    entityId,
                    traitId,
                    context.SourceCodeId)),
                    context);
            }
        });
    }

    private static void EmitSnapshotInfluence(KernelExecutionContext context, int tick, long version)
    {
        _ = context;
        _ = tick;
        _ = version;
    }

    private void EmitSnapshotCultureInfluence(KernelExecutionContext context, int tick, long version)
    {
        _mapLayers.CultureInfluence.ForEach((cultureId, planeCode, coordinateKey, value) =>
        {
            if (value > 0f &&
                TryResolveCoordinateKey(coordinateKey, planeCode == PlaneCodes.Semantic ? 4 : 0, out var q, out var r))
            {
                var raw = PositiveCultureInfluenceRaw(cultureId, planeCode, coordinateKey);
                Write(KernelEvent.From(new CellCultureInfluenceChangedKernelEvent(
                    tick,
                    version,
                    q,
                    r,
                    context.SourceCodeId,
                    planeCode,
                    cultureId,
                    0f,
                    value,
                    0f,
                    raw,
                    CultureRawTotalAt(planeCode, coordinateKey))),
                    context);
            }
        });
    }

    private void EmitSnapshotRelationships(KernelExecutionContext context, int tick, long version)
    {
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
                context.SourceCodeId,
                _relationshipKindId[relationshipId],
                _relationshipPolarityCode[relationshipId],
                _relationshipSourceId[relationshipId],
                _relationshipTargetId[relationshipId],
                relationshipId,
                0f,
                _relationshipStrength[relationshipId],
                context.ActionId)),
                context);
        }
    }

    internal bool TryResolveCoordinateKey(int coordinateKey, int extraRadius, out int q, out int r)
    {
        var radius = _mapRadius + extraRadius;
        for (q = -radius; q <= radius; q++)
        {
            var minR = global::System.Math.Max(-radius, -q - radius);
            var maxR = global::System.Math.Min(radius, -q + radius);
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
