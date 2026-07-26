namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class WorldState
{
    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        WriteSnapshotScalars(writer);
        WriteSnapshotMap(writer);
        WriteSnapshotCultures(writer);
        WriteSnapshotEntities(writer);
        WriteSnapshotRelationships(writer);
        WriteSnapshotDistricts(writer);
        WriteSnapshotDefinitions(writer);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        ReadSnapshotScalars(reader);
        ReadSnapshotMap(reader);
        ReadSnapshotCultures(reader);
        ReadSnapshotEntities(reader);
        ReadSnapshotRelationships(reader);
        ReadSnapshotDistricts(reader);
        ReadSnapshotDefinitions(reader);
        CompactRelationshipGraph();
    }

    private void WriteSnapshotScalars(global::System.IO.BinaryWriter writer)
    {
        writer.Write(CurrentTick);
        writer.Write(WorldVersion);
        writer.Write(_worldVersionTick);
        writer.Write(_physicalMapRadius);
        writer.Write(_semanticMapRadius);
        writer.Write(_physicalMapSegmentOriginRadius);
        writer.Write(_semanticMapSegmentOriginRadius);
        writer.Write(_totalPhysicalCultureRawMass);
        writer.Write(_entityHighWatermark);
        writer.Write(_relationshipHighWatermark);
        writer.Write(_nextProvenanceInvocationId);
    }

    private void ReadSnapshotScalars(global::System.IO.BinaryReader reader)
    {
        CurrentTick = reader.ReadInt32();
        WorldVersion = reader.ReadInt64();
        _worldVersionTick = reader.ReadInt32();
        _physicalMapRadius = reader.ReadInt32();
        _semanticMapRadius = reader.ReadInt32();
        _physicalMapSegmentOriginRadius = reader.ReadInt32();
        _semanticMapSegmentOriginRadius = reader.ReadInt32();
        _totalPhysicalCultureRawMass = reader.ReadInt64();
        _entityHighWatermark = reader.ReadInt32();
        _relationshipHighWatermark = reader.ReadInt32();
        _nextProvenanceInvocationId = reader.ReadInt32();
        if ((uint)_entityHighWatermark > (uint)EntityCapacity ||
            (uint)_relationshipHighWatermark > (uint)RelationshipCapacity ||
            _nextProvenanceInvocationId < 0)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void WriteSnapshotMap(global::System.IO.BinaryWriter writer)
    {
        _cells.WriteSnapshot(writer);
        writer.Write(_cells.Count);
        for (var cellId = 1; cellId <= _cells.Count; cellId++)
        {
            writer.Write(_mapCellKnown[cellId]);
            writer.Write(_mapCellTerrainIds[cellId]);
            writer.Write(_mapCellTerrainCoverIds[cellId]);
            writer.Write(_mapCellSuperregionIds[cellId]);
            writer.Write(_mapCellContinentIds[cellId]);
        }

        _mapLayers.WriteSnapshot(writer);
    }

    private void ReadSnapshotMap(global::System.IO.BinaryReader reader)
    {
        _cells.ReadSnapshot(reader);
        if (reader.ReadInt32() != _cells.Count)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var cellId = 1; cellId <= _cells.Count; cellId++)
        {
            _mapCellKnown[cellId] = reader.ReadBoolean();
            _mapCellTerrainIds[cellId] = reader.ReadInt32();
            _mapCellTerrainCoverIds[cellId] = reader.ReadInt32();
            _mapCellSuperregionIds[cellId] = reader.ReadInt32();
            _mapCellContinentIds[cellId] = reader.ReadInt32();
        }

        _mapLayers.ReadSnapshot(reader);
    }

    private void WriteSnapshotCultures(global::System.IO.BinaryWriter writer)
    {
        writer.Write(_cultureHighWatermark);
        for (var cultureId = 1; cultureId <= _cultureHighWatermark; cultureId++)
        {
            writer.Write(_cultureInfluenceMultiplierDeltas[cultureId]);
            writer.Write(_cultureFieldRevisions[cultureId]);
            writer.Write(_cultureWorkRevisions[cultureId]);
        }
    }

    private void ReadSnapshotCultures(global::System.IO.BinaryReader reader)
    {
        _cultureHighWatermark = reader.ReadInt32();
        if ((uint)_cultureHighWatermark >= (uint)_cultureInfluenceMultiplierDeltas.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var cultureId = 1; cultureId <= _cultureHighWatermark; cultureId++)
        {
            _cultureInfluenceMultiplierDeltas[cultureId] = reader.ReadInt32();
            _cultureFieldRevisions[cultureId] = reader.ReadInt64();
            _cultureWorkRevisions[cultureId] = reader.ReadInt64();
        }
    }

    private void WriteSnapshotEntities(global::System.IO.BinaryWriter writer)
    {
        KernelSnapshotBinary.Write(writer, _entityGenerations);
        writer.Write(_activeEntityCount);
        for (var index = 0; index < _activeEntityCount; index++)
        {
            var entityId = _activeEntitySlots[index];
            WriteSnapshotEntity(writer, entityId);
        }
    }

    private void WriteSnapshotEntity(global::System.IO.BinaryWriter writer, int entityId)
    {
        writer.Write(entityId);
        writer.Write(_entityKindId[entityId]);
        writer.Write(_entitySubtypeId[entityId]);
        writer.Write(_entityCultureId[entityId]);
        writer.Write(_entityCultureInfluenceMultiplierDeltas[entityId]);
        writer.Write(_entityPhysicalInfluenceProduction[entityId]);
        writer.Write(_entitySemanticInfluenceProduction[entityId]);
        writer.Write(_entityPhysicalQ[entityId]);
        writer.Write(_entityPhysicalR[entityId]);
        writer.Write(_entityPhysicalZ[entityId]);
        writer.Write(_entitySemanticQ[entityId]);
        writer.Write(_entitySemanticR[entityId]);
        writer.Write(_entitySemanticS[entityId]);
        writer.Write(_entityAnchorPhysicalQ[entityId]);
        writer.Write(_entityAnchorPhysicalR[entityId]);
        writer.Write(_entityAnchorSemanticQ[entityId]);
        writer.Write(_entityAnchorSemanticR[entityId]);
        writer.Write(_entityFavorMultiplierDelta[entityId]);
        writer.Write(_entityFavorExpiresAtTick[entityId]);
        writer.Write(_entityWorkTiers[entityId]);
        writer.Write(_entityCreatedAt[entityId]);
        writer.Write(_entityUpdatedAt[entityId]);
        writer.Write(_entityTagBits[entityId]);
        writer.Write(_entityCreatedByInvocationId[entityId]);
        WriteSnapshotEntityAttributes(writer, entityId);
        WriteSnapshotEntityTraits(writer, entityId);
    }

    private void WriteSnapshotEntityAttributes(global::System.IO.BinaryWriter writer, int entityId)
    {
        var row = entityId * _attributeStride;
        var count = 0;
        for (var attributeId = 1; attributeId < _attributeStride; attributeId++)
        {
            if (_attributeAssigned[row + attributeId])
            {
                count++;
            }
        }

        writer.Write(count);
        for (var attributeId = 1; attributeId < _attributeStride; attributeId++)
        {
            if (!_attributeAssigned[row + attributeId])
            {
                continue;
            }

            writer.Write(attributeId);
            writer.Write(_attributes[row + attributeId]);
        }
    }

    private void WriteSnapshotEntityTraits(global::System.IO.BinaryWriter writer, int entityId)
    {
        var row = entityId * _traitStride;
        var count = 0;
        for (var traitId = 1; traitId < _traitStride; traitId++)
        {
            if (_traits[row + traitId])
            {
                count++;
            }
        }

        writer.Write(count);
        for (var traitId = 1; traitId < _traitStride; traitId++)
        {
            if (_traits[row + traitId])
            {
                writer.Write(traitId);
            }
        }
    }

    private void ReadSnapshotEntities(global::System.IO.BinaryReader reader)
    {
        KernelSnapshotBinary.Read(reader, _entityGenerations);
        var count = reader.ReadInt32();
        if ((uint)count > (uint)EntityCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var index = 0; index < count; index++)
        {
            ReadSnapshotEntity(reader);
        }

        RebuildEntityFreeSlots();
    }

    private void ReadSnapshotEntity(global::System.IO.BinaryReader reader)
    {
        var entityId = reader.ReadInt32();
        if ((uint)entityId == 0u ||
            entityId > _entityHighWatermark ||
            _entityAllocated[entityId])
        {
            throw new global::System.InvalidOperationException();
        }

        _entityAllocated[entityId] = true;
        _entityStatus[entityId] = EntityStatusCodes.Active;
        _entityKindId[entityId] = reader.ReadInt32();
        _entitySubtypeId[entityId] = reader.ReadInt32();
        _entityCultureId[entityId] = reader.ReadInt32();
        _entityCultureInfluenceMultiplierDeltas[entityId] = reader.ReadInt32();
        _entityPhysicalInfluenceProduction[entityId] = reader.ReadInt32();
        _entitySemanticInfluenceProduction[entityId] = reader.ReadInt32();
        _entityPhysicalQ[entityId] = reader.ReadInt32();
        _entityPhysicalR[entityId] = reader.ReadInt32();
        _entityPhysicalZ[entityId] = reader.ReadInt32();
        _entitySemanticQ[entityId] = reader.ReadInt32();
        _entitySemanticR[entityId] = reader.ReadInt32();
        _entitySemanticS[entityId] = reader.ReadInt32();
        _entityAnchorPhysicalQ[entityId] = reader.ReadInt32();
        _entityAnchorPhysicalR[entityId] = reader.ReadInt32();
        _entityAnchorSemanticQ[entityId] = reader.ReadInt32();
        _entityAnchorSemanticR[entityId] = reader.ReadInt32();
        _entityFavorMultiplierDelta[entityId] = reader.ReadInt32();
        _entityFavorExpiresAtTick[entityId] = reader.ReadInt32();
        _entityWorkTiers[entityId] = reader.ReadInt32();
        _entityCreatedAt[entityId] = reader.ReadInt32();
        _entityUpdatedAt[entityId] = reader.ReadInt32();
        _entityTagBits[entityId] = reader.ReadInt32();
        _entityCreatedByInvocationId[entityId] = reader.ReadInt32();
        ReadSnapshotEntityAttributes(reader, entityId);
        ReadSnapshotEntityTraits(reader, entityId);
        InsertEntityKindIndex(entityId, _entityKindId[entityId]);
        InsertEntityCultureIndex(entityId, _entityCultureId[entityId]);
        InsertActiveEntity(entityId);
        InsertPhysicalOccupant(
            entityId,
            _entityPhysicalQ[entityId],
            _entityPhysicalR[entityId],
            _entityPhysicalZ[entityId]);
        InsertSemanticOccupant(entityId, _entitySemanticQ[entityId], _entitySemanticR[entityId]);
    }

    private void ReadSnapshotEntityAttributes(global::System.IO.BinaryReader reader, int entityId)
    {
        var count = reader.ReadInt32();
        var row = entityId * _attributeStride;
        for (var index = 0; index < count; index++)
        {
            var attributeId = reader.ReadInt32();
            if ((uint)attributeId == 0u || attributeId >= _attributeStride)
            {
                throw new global::System.InvalidOperationException();
            }

            _attributeAssigned[row + attributeId] = true;
            _attributes[row + attributeId] = reader.ReadInt32();
        }
    }

    private void ReadSnapshotEntityTraits(global::System.IO.BinaryReader reader, int entityId)
    {
        var count = reader.ReadInt32();
        var row = entityId * _traitStride;
        for (var index = 0; index < count; index++)
        {
            var traitId = reader.ReadInt32();
            if ((uint)traitId == 0u || traitId >= _traitStride)
            {
                throw new global::System.InvalidOperationException();
            }

            _traits[row + traitId] = true;
        }
    }

    private void RebuildEntityFreeSlots()
    {
        _freeEntitySlotCount = 0;
        for (var entityId = 1; entityId <= _entityHighWatermark; entityId++)
        {
            if (!_entityAllocated[entityId])
            {
                _freeEntitySlots[_freeEntitySlotCount++] = entityId;
            }
        }
    }

    private void WriteSnapshotRelationships(global::System.IO.BinaryWriter writer)
    {
        KernelSnapshotBinary.Write(writer, _relationshipGenerations);
        writer.Write(_activeRelationshipCount);
        for (var index = 0; index < _activeRelationshipCount; index++)
        {
            var relationshipId = _activeRelationshipSlots[index];
            writer.Write(relationshipId);
            writer.Write(_relationshipKindId[relationshipId]);
            writer.Write(_relationshipPolarityCode[relationshipId]);
            writer.Write(_relationshipSourceId[relationshipId]);
            writer.Write(_relationshipTargetId[relationshipId]);
            writer.Write(_relationshipStartTick[relationshipId]);
            writer.Write(_relationshipStrength[relationshipId]);
            writer.Write(_relationshipCreatedByInvocationId[relationshipId]);
            writer.Write(_relationshipLastChangedByInvocationId[relationshipId]);
        }
    }

    private void ReadSnapshotRelationships(global::System.IO.BinaryReader reader)
    {
        KernelSnapshotBinary.Read(reader, _relationshipGenerations);
        var count = reader.ReadInt32();
        if ((uint)count > (uint)RelationshipCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var index = 0; index < count; index++)
        {
            var relationshipId = reader.ReadInt32();
            if ((uint)relationshipId == 0u ||
                relationshipId > _relationshipHighWatermark ||
                _relationshipAllocated[relationshipId])
            {
                throw new global::System.InvalidOperationException();
            }

            _relationshipAllocated[relationshipId] = true;
            _relationshipActive[relationshipId] = true;
            _relationshipKindId[relationshipId] = reader.ReadInt32();
            _relationshipPolarityCode[relationshipId] = reader.ReadInt32();
            _relationshipSourceId[relationshipId] = reader.ReadInt32();
            _relationshipTargetId[relationshipId] = reader.ReadInt32();
            _relationshipStartTick[relationshipId] = reader.ReadInt32();
            _relationshipStrength[relationshipId] = reader.ReadInt32();
            _relationshipCreatedByInvocationId[relationshipId] = reader.ReadInt32();
            _relationshipLastChangedByInvocationId[relationshipId] = reader.ReadInt32();
            InsertActiveRelationship(relationshipId);
            InsertIncidentRelationship(relationshipId);
        }

        RebuildRelationshipFreeSlots();
    }

    private void RebuildRelationshipFreeSlots()
    {
        _freeRelationshipSlotCount = 0;
        for (var relationshipId = 1; relationshipId <= _relationshipHighWatermark; relationshipId++)
        {
            if (!_relationshipAllocated[relationshipId])
            {
                _freeRelationshipSlots[_freeRelationshipSlotCount++] = relationshipId;
            }
        }
    }

    private void WriteSnapshotDistricts(global::System.IO.BinaryWriter writer)
    {
        writer.Write(_districtCount);
        for (var index = 0; index < _districtCount; index++)
        {
            var district = _activeDistrictSlots[index];
            writer.Write(_districtArtifactKindIds[district]);
            writer.Write(_districtArtifactSubtypeIds[district]);
            writer.Write(_districtArtifactCultureIds[district]);
            writer.Write(_districtHostOccurrenceHandles[district]);
            writer.Write(_districtCellIds[district]);
            writer.Write(_districtBonusKindCodes[district]);
            writer.Write(_districtMagnitudes[district]);
            writer.Write(_districtRelationshipKindIds[district]);
            writer.Write(_districtRelationshipPolarityCodes[district]);
            writer.Write(_districtRelationshipStrengths[district]);
        }
    }

    private void ReadSnapshotDistricts(global::System.IO.BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if ((uint)count >= (uint)_districtActive.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var index = 0; index < count; index++)
        {
            var artifactKindId = reader.ReadInt32();
            var artifactSubtypeId = reader.ReadInt32();
            var artifactCultureId = reader.ReadInt32();
            var hostOccurrenceHandle = reader.ReadInt32();
            var cellId = reader.ReadInt32();
            var bonusKindCode = reader.ReadInt32();
            var magnitudeUnits = reader.ReadInt32();
            var relationshipKindId = reader.ReadInt32();
            var relationshipPolarityCode = reader.ReadInt32();
            var relationshipStrengthUnits = reader.ReadInt32();
            if (!TryResolveEntityOccurrence(hostOccurrenceHandle, out var hostId))
            {
                throw new global::System.InvalidOperationException();
            }

            InstallDistrict(
                artifactKindId,
                artifactSubtypeId,
                artifactCultureId,
                hostId,
                _cells.Q(cellId),
                _cells.R(cellId),
                bonusKindCode,
                DistrictMagnitudeValue(bonusKindCode, magnitudeUnits),
                relationshipKindId,
                relationshipPolarityCode,
                KernelFixedPoint.RelationshipStrengthScalar(relationshipStrengthUnits));
        }
    }

    private void WriteSnapshotDefinitions(global::System.IO.BinaryWriter writer)
    {
        writer.Write(_districtBonusRuleCount);
        for (var rule = 1; rule <= _districtBonusRuleCount; rule++)
        {
            writer.Write(_districtBonusSubtypeIds[rule]);
            writer.Write(_districtBonusKindRuleCodes[rule]);
            writer.Write(_districtBonusRuleMagnitudes[rule].Units);
        }

        writer.Write(_stageRuleCount);
        for (var rule = 1; rule <= _stageRuleCount; rule++)
        {
            writer.Write(_stageKindIds[rule]);
            writer.Write(_stageIndices[rule]);
            writer.Write(_stageBandMultipliers[rule].Units);
            writer.Write(_stageWorkerBonuses[rule]);
            writer.Write(_stageAmbientEmissions[rule].Units);
            writer.Write(_stageDistrictCapacities[rule]);
        }
    }

    private void ReadSnapshotDefinitions(global::System.IO.BinaryReader reader)
    {
        var districtRuleCount = reader.ReadInt32();
        for (var rule = 0; rule < districtRuleCount; rule++)
        {
            RegisterDistrictBonus(
                reader.ReadInt32(),
                reader.ReadInt32(),
                FixedScalar.FromUnits(reader.ReadInt64()));
        }

        var stageRuleCount = reader.ReadInt32();
        for (var rule = 0; rule < stageRuleCount; rule++)
        {
            RegisterStageProperties(
                reader.ReadInt32(),
                reader.ReadInt32(),
                FixedScalar.FromUnits(reader.ReadInt64()),
                reader.ReadInt32(),
                FixedScalar.FromUnits(reader.ReadInt64()),
                reader.ReadInt32());
        }
    }

    internal int SnapshotEventCount()
    {
        var count = 0;
        count += SnapshotCellEventCount(PlaneCodes.Physical);
        count += SnapshotCellEventCount(PlaneCodes.Semantic);

        for (var index = 0; index < _activeEntityCount; index++)
        {
            count++;
        }

        count += SnapshotAttributeEventCount();
        count += SnapshotTraitEventCount();
        count += SnapshotTagEventCount();
        count += _activeRelationshipCount;

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

        var cellId = RequireCellId(planeCode, q, r, z);
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
            EntityOccurrenceHandleOrZero(_mapLayers.Coverage.ValueAt(cellId)),
            _mapCellSuperregionIds[cellId],
            _mapCellContinentIds[cellId],
            z)),
            context);
    }

    private void EmitSnapshotEntities(KernelExecutionContext context, int tick, long version)
    {
        var cultureLocality = _cultureLocality ??
            throw new global::System.InvalidOperationException();
        for (var index = 0; index < _activeEntityCount; index++)
        {
            var entityId = _activeEntitySlots[index];
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
                EntityOccurrenceHandle(entityId),
                context.SourceCodeId,
                _entityKindId[entityId],
                _entitySubtypeId[entityId],
                _entityCultureId[entityId],
                _entityPhysicalQ[entityId],
                _entityPhysicalR[entityId],
                _entitySemanticQ[entityId],
                _entitySemanticR[entityId],
                _entitySemanticS[entityId],
                EntityOccurrenceHandle(entityId),
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
        for (var entityId = FirstActiveEntity(); entityId > 0; entityId = NextActiveEntity(entityId))
        {
            var row = entityId * _attributeStride;
            for (var attributeId = 1; attributeId < _attributeStride; attributeId++)
            {
                var index = row + attributeId;
                if (!_attributeAssigned[index])
                {
                    continue;
                }

                Write(KernelEvent.From(new AttributeChangedKernelEvent(
                    tick,
                    version,
                    EntityOccurrenceHandle(entityId),
                    attributeId,
                    context.SourceCodeId,
                    0,
                    _attributes[index])),
                    context);
            }
        }
    }

    private void EmitSnapshotTraits(KernelExecutionContext context, int tick, long version)
    {
        for (var entityId = FirstActiveEntity(); entityId > 0; entityId = NextActiveEntity(entityId))
        {
            var row = entityId * _traitStride;
            for (var traitId = 1; traitId < _traitStride; traitId++)
            {
                if (!_traits[row + traitId])
                {
                    continue;
                }

                Write(KernelEvent.From(new TraitAssignedKernelEvent(
                    tick,
                    version,
                    EntityOccurrenceHandle(entityId),
                    traitId,
                    context.SourceCodeId)),
                    context);
            }
        }
    }

    private int SnapshotTagEventCount()
    {
        var count = 0;
        for (var index = 0; index < _activeEntityCount; index++)
        {
            count += global::System.Numerics.BitOperations.PopCount(
                (uint)_entityTagBits[_activeEntitySlots[index]]);
        }

        return count;
    }

    private void EmitSnapshotTags(KernelExecutionContext context, int tick, long version)
    {
        for (var index = 0; index < _activeEntityCount; index++)
        {
            var entityId = _activeEntitySlots[index];
            if (_entityTagBits[entityId] == 0)
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
                        EntityOccurrenceHandle(entityId),
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
        for (var entityId = FirstActiveEntity(); entityId > 0; entityId = NextActiveEntity(entityId))
        {
            var row = entityId * _attributeStride;
            for (var attributeId = 1; attributeId < _attributeStride; attributeId++)
            {
                if (_attributeAssigned[row + attributeId])
                {
                    count++;
                }
            }
        }

        return count;
    }

    private int SnapshotTraitEventCount()
    {
        var count = 0;
        for (var entityId = FirstActiveEntity(); entityId > 0; entityId = NextActiveEntity(entityId))
        {
            var row = entityId * _traitStride;
            for (var traitId = 1; traitId < _traitStride; traitId++)
            {
                if (_traits[row + traitId])
                {
                    count++;
                }
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
        for (var index = 0; index < _activeRelationshipCount; index++)
        {
            var relationshipId = _activeRelationshipSlots[index];
            Write(KernelEvent.From(new RelationshipChangedKernelEvent(
                tick,
                version,
                _relationshipCreatedCodeId,
                context.SourceCodeId,
                _relationshipKindId[relationshipId],
                _relationshipPolarityCode[relationshipId],
                EntityOccurrenceHandle(_relationshipSourceId[relationshipId]),
                EntityOccurrenceHandle(_relationshipTargetId[relationshipId]),
                RelationshipOccurrenceHandle(relationshipId),
                0,
                _relationshipStrength[relationshipId],
                context.ActionId,
                1)),
                context);
        }
    }

}
