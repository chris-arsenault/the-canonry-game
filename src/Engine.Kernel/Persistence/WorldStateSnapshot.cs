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
        count += SnapshotTagEventCount();
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
        EmitSnapshotTags(context, tick, version);
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
                count += SnapshotCellColumnEventCount(planeCode, q, r, emitLegacyPhysicalEvents);
            }
        }

        return count;
    }

    private int SnapshotCellColumnEventCount(int planeCode, int q, int r, bool emitLegacyPhysicalEvents)
    {
        var count = 0;
        var minZ = planeCode == PlaneCodes.Physical ? MinCellZ : 0;
        var maxZ = planeCode == PlaneCodes.Physical ? MaxCellZ : 0;
        for (var z = minZ; z <= maxZ; z++)
        {
            if (!CellIsKnown(planeCode, q, r, z))
            {
                continue;
            }

            count += emitLegacyPhysicalEvents ? 2 : 1;
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
        var minZ = planeCode == PlaneCodes.Physical ? MinCellZ : 0;
        var maxZ = planeCode == PlaneCodes.Physical ? MaxCellZ : 0;
        for (var q = -_mapRadius; q <= _mapRadius; q++)
        {
            var minR = global::System.Math.Max(-_mapRadius, -q - _mapRadius);
            var maxR = global::System.Math.Min(_mapRadius, -q + _mapRadius);
            for (var r = minR; r <= maxR; r++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    EmitSnapshotCell(context, tick, version, planeCode, q, r, z, emitLegacyPhysicalEvents);
                }
            }
        }
    }

    private void EmitSnapshotCell(
        KernelExecutionContext context,
        int tick,
        long version,
        int planeCode,
        int q,
        int r,
        int z,
        bool emitLegacyPhysicalEvents)
    {
        if (!CellIsKnown(planeCode, q, r, z))
        {
            return;
        }

        Write(KernelEvent.From(new MapCellStateChangedKernelEvent(
            tick,
            version,
            planeCode,
            q,
            r,
            context.SourceCodeId,
            0,
            CellTerrainId(planeCode, q, r, z),
            0,
            _mapLayers.Coverage.ValueAt(planeCode, CoordinateKey(q, r, z)),
            _mapCellSuperregionIds.GetOrDefault(CoordinateKey(q, r, z), planeCode),
            _mapCellContinentIds.GetOrDefault(CoordinateKey(q, r, z), planeCode),
            z)),
            context);
        if (emitLegacyPhysicalEvents)
        {
            Write(KernelEvent.From(new CellCreatedKernelEvent(
                tick,
                version,
                q,
                r,
                context.SourceCodeId,
                CellTerrainId(PlaneCodes.Physical, q, r, z),
                _mapLayers.Coverage.ValueAt(PlaneCodes.Physical, CoordinateKey(q, r, z)),
                _mapCellSuperregionIds.GetOrDefault(CoordinateKey(q, r, z), PlaneCodes.Physical),
                _mapCellContinentIds.GetOrDefault(CoordinateKey(q, r, z), PlaneCodes.Physical),
                z)),
                context);
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

    private int SnapshotTagEventCount()
    {
        var count = 0;
        for (var entityId = 1; entityId <= _entityHighWatermark; entityId++)
        {
            if (_entityAllocated[entityId])
            {
                count += global::System.Numerics.BitOperations.PopCount((uint)_entityTagBits[entityId]);
            }
        }

        return count;
    }

    private void EmitSnapshotTags(KernelExecutionContext context, int tick, long version)
    {
        for (var entityId = 1; entityId <= _entityHighWatermark; entityId++)
        {
            if (!_entityAllocated[entityId] || _entityTagBits[entityId] == 0)
            {
                continue;
            }

            for (var tagId = 1; tagId < 32; tagId++)
            {
                if ((_entityTagBits[entityId] & (1 << tagId)) != 0)
                {
                    Write(KernelEvent.From(new EntityTagChangedKernelEvent(
                        tick,
                        version,
                        entityId,
                        tagId,
                        1,
                        context.SourceCodeId)),
                        context);
                }
            }
        }
    }

    private static void EmitSnapshotInfluence(KernelExecutionContext context, int tick, long version)
    {
        _ = context;
        _ = tick;
        _ = version;
    }

    private void EmitSnapshotCultureInfluence(KernelExecutionContext context, int tick, long version)
    {
        for (var cultureId = 1; cultureId <= _cultureHighWatermark; cultureId++)
        {
            EmitSnapshotCultureInfluencePlane(context, tick, version, cultureId, PlaneCodes.Physical);
            EmitSnapshotCultureInfluencePlane(context, tick, version, cultureId, PlaneCodes.Semantic);
        }
    }

    private void EmitSnapshotCultureInfluencePlane(
        KernelExecutionContext context,
        int tick,
        long version,
        int cultureId,
        int planeCode)
    {
        var cursor = _mapLayers.CultureInfluence.CellsOfValue(cultureId, planeCode);
        while (_mapLayers.CultureInfluence.MoveNextCell(ref cursor, out var q, out var r, out var z, out var value))
        {
            if (value <= 0f)
            {
                continue;
            }

            var coordinateKey = CoordinateKey(q, r, z);
            var raw = PositiveCultureInfluenceRaw(cultureId, planeCode, coordinateKey);
            Write(KernelEvent.From(new CellCultureInfluenceChangedKernelEvent(
                tick,
                version,
                q,
                r,
                context.SourceCodeId,
                planeCode,
                cultureId,
                z,
                0f,
                value,
                0f,
                raw,
                CultureRawTotalAt(planeCode, coordinateKey))),
                context);
        }
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
