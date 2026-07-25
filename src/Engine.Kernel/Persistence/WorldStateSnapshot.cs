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
        count += SnapshotCellEventCount(PlaneCodes.Physical);
        count += SnapshotCellEventCount(PlaneCodes.Semantic);

        for (var entityId = 1; entityId <= _entityHighWatermark; entityId++)
        {
            if (!EntityIsActiveRaw(entityId))
            {
                continue;
            }

            count++;
        }

        count += SnapshotAttributeEventCount();
        count += SnapshotTraitEventCount();
        count += SnapshotTagEventCount();
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
        EmitSnapshotRelationships(context, tick, version);
    }

    private void EmitSnapshotCells(KernelExecutionContext context, int tick, long version)
    {
        EmitSnapshotCells(context, tick, version, PlaneCodes.Physical);
        EmitSnapshotCells(context, tick, version, PlaneCodes.Semantic);
    }

    private int SnapshotCellEventCount(int planeCode)
    {
        var count = 0;
        var radius = MapRadiusForPlane(planeCode);
        for (var q = -radius; q <= radius; q++)
        {
            var minR = global::System.Math.Max(-radius, -q - radius);
            var maxR = global::System.Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                count += SnapshotCellColumnEventCount(planeCode, q, r);
            }
        }

        return count;
    }

    private int SnapshotCellColumnEventCount(int planeCode, int q, int r)
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

            count++;
        }

        return count;
    }

    private void EmitSnapshotCells(
        KernelExecutionContext context,
        int tick,
        long version,
        int planeCode)
    {
        var minZ = planeCode == PlaneCodes.Physical ? MinCellZ : 0;
        var maxZ = planeCode == PlaneCodes.Physical ? MaxCellZ : 0;
        var radius = MapRadiusForPlane(planeCode);
        for (var q = -radius; q <= radius; q++)
        {
            var minR = global::System.Math.Max(-radius, -q - radius);
            var maxR = global::System.Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    EmitSnapshotCell(context, tick, version, planeCode, q, r, z);
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
        int z)
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
    }

    private void EmitSnapshotEntities(KernelExecutionContext context, int tick, long version)
    {
        var cultureLocality = _cultureLocality ??
            throw new global::System.InvalidOperationException();
        for (var entityId = 1; entityId <= _entityHighWatermark; entityId++)
        {
            if (!EntityIsActiveRaw(entityId))
            {
                continue;
            }

            var cultureId = _entityCultureId[entityId];
            var physicalLocality = cultureLocality.CellAt(
                this,
                cultureId,
                PlaneCodes.Physical,
                _entityPhysicalQ[entityId],
                _entityPhysicalR[entityId]);
            var semanticLocality = cultureLocality.CellAt(
                this,
                cultureId,
                PlaneCodes.Semantic,
                _entitySemanticQ[entityId],
                _entitySemanticR[entityId]);
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
                _entitySemanticS[entityId],
                entityId,
                physicalLocality.StrengthBandCode,
                physicalLocality.CellKindCode,
                semanticLocality.StrengthBandCode,
                semanticLocality.CellKindCode,
                physicalLocality.LocalRawMass.Units,
                physicalLocality.ConnectedRawMass.Units,
                semanticLocality.LocalRawMass.Units,
                semanticLocality.ConnectedRawMass.Units)),
                context);

        }
    }

    private void EmitSnapshotAttributes(KernelExecutionContext context, int tick, long version)
    {
        _attributes.ForEach((entityId, attributeId, value) =>
        {
            if (EntityIsActiveRaw(entityId))
            {
                Write(KernelEvent.From(new AttributeChangedKernelEvent(
                    tick,
                    version,
                    entityId,
                    attributeId,
                    context.SourceCodeId,
                    0,
                    value)),
                    context);
            }
        });
    }

    private void EmitSnapshotTraits(KernelExecutionContext context, int tick, long version)
    {
        _traits.ForEach((entityId, traitId, assigned) =>
        {
            if (EntityIsActiveRaw(entityId) && assigned != 0)
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
            if (EntityIsActiveRaw(entityId))
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
            if (!EntityIsActiveRaw(entityId) || _entityTagBits[entityId] == 0)
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

    private int SnapshotAttributeEventCount()
    {
        var count = 0;
        for (var index = 0; index < _attributes.Count; index++)
        {
            _attributes.GetEntry(index, out var entityId, out _, out _);
            if (EntityIsActiveRaw(entityId))
            {
                count++;
            }
        }

        return count;
    }

    private int SnapshotTraitEventCount()
    {
        var count = 0;
        for (var index = 0; index < _traits.Count; index++)
        {
            _traits.GetEntry(index, out var entityId, out _, out var assigned);
            if (EntityIsActiveRaw(entityId) && assigned != 0)
            {
                count++;
            }
        }

        return count;
    }

    private static void EmitSnapshotInfluence(KernelExecutionContext context, int tick, long version)
    {
        _ = context;
        _ = tick;
        _ = version;
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
                0,
                _relationshipStrength[relationshipId],
                context.ActionId,
                1)),
                context);
        }
    }

    internal bool TryResolveCoordinateKey(int planeCode, int coordinateKey, int extraRadius, out int q, out int r)
    {
        var radius = MapRadiusForPlane(planeCode) + extraRadius;
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
