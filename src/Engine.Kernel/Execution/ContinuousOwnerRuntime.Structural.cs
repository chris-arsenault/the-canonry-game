namespace Engine.Kernel;

internal sealed partial class ContinuousOwnerRuntime
{
    private bool DrainStructuralProjections(int ownerIndex)
    {
        if (ownerIndex != _topology.StructuralOwner)
        {
            return false;
        }

        var progressed = false;
        progressed |= DrainMovementStructures(ownerIndex);
        progressed |= DrainCellStructures(ownerIndex);
        progressed |= DrainCultureStructures(ownerIndex);
        progressed |= DrainRoadStructures(ownerIndex);
        progressed |= DrainRelationshipRetirements(ownerIndex);
        progressed |= DrainEntityStructures(ownerIndex);
        if (progressed)
        {
            _simulation.World.PublishOwnedRevisionChanges();
        }

        return progressed;
    }

    private bool DrainCellStatePreparations(int ownerIndex)
    {
        if (ownerIndex != _topology.StructuralOwner)
        {
            return false;
        }

        var cursor = _cellStatePreparations.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_cellStatePreparations.MoveNext(ref cursor, out var delta))
        {
            _ = _simulation.World.PrepareOwnedCellState(
                delta.PlaneCode,
                delta.Q,
                delta.R);
            _cellStates.Add(
                _topology.CellOwner(
                    delta.PlaneCode,
                    delta.Q,
                    delta.R),
                delta);
        }

        return progressed;
    }

    private bool DrainCulturePreparations(int ownerIndex)
    {
        if (ownerIndex != _topology.StructuralOwner)
        {
            return false;
        }

        var cursor = _culturePreparations.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_culturePreparations.MoveNext(ref cursor, out var delta))
        {
            _simulation.World.PrepareOwnedCultureInfluence(
                delta.CultureId,
                delta.PlaneCode,
                delta.Q,
                delta.R,
                delta.Z,
                delta.DeltaUnits);
            _culture.Add(
                _topology.CellOwner(
                    delta.PlaneCode,
                    delta.Q,
                    delta.R),
                delta);
        }

        return progressed;
    }

    private bool DrainMovementStructures(int ownerIndex)
    {
        var cursor = _movementStructures.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_movementStructures.MoveNext(ref cursor, out var movement))
        {
            _simulation.World.DetachEntityForResolvedMove(
                movement.EntityId,
                movement.PlaneCode,
                movement.FromQ,
                movement.FromR,
                movement.FromZ);
            _simulation.World.AttachEntityForResolvedMove(
                movement.EntityId,
                movement.PlaneCode,
                movement.ToQ,
                movement.ToR,
                movement.ToZ);
            if (movement.ToZ != 0 ||
                !_simulation.World.EntityIsActive(movement.EntityId))
            {
                continue;
            }

            using var scope =
                _simulation.EnterExecutionContext(movement.Context);
            using var producer = EnterProducer();
            _simulation.Movement.ApplyResolvedMoveSurfaceConsequences(
                _simulation,
                movement.EntityId,
                movement.PlaneCode,
                movement.ToQ,
                movement.ToR,
                movement.Tick,
                movement.CauseCode);
        }

        return progressed;
    }

    private bool DrainCellStructures(int ownerIndex)
    {
        var cursor = _cellStructures.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_cellStructures.MoveNext(ref cursor, out var reconciliation))
        {
            var delta = reconciliation.Delta;
            _simulation.World.ReconcileOwnedCellState(
                reconciliation.CellId,
                delta.PlaneCode,
                delta.Q,
                delta.R,
                reconciliation.PreviousOwnerEntityId,
                delta.OwnerEntityId);
            if (reconciliation.TerrainChanged)
            {
                _simulation.Movement.RecordTopologyChanged();
            }
        }

        return progressed;
    }

    private bool DrainCultureStructures(int ownerIndex)
    {
        var cursor = _cultureStructures.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_cultureStructures.MoveNext(ref cursor, out var key))
        {
            TrackCultureStorageReconciliation(key);
            if (key.Z == 0)
            {
                _simulation.CultureLocality.MarkDirty(
                    _simulation.World,
                    key.PlaneCode,
                    key.Q,
                    key.R);
            }
        }

        return progressed;
    }

    private bool DrainRoadStructures(int ownerIndex)
    {
        var cursor = _roadStructures.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_roadStructures.MoveNext(ref cursor, out var work))
        {
            using var scope =
                _simulation.EnterExecutionContext(work.Context);
            _ = _simulation.Movement.ReinforceRoadOwned(
                _simulation,
                work.PlaneCode,
                work.Q,
                work.R,
                work.Amount,
                work.Tick);
        }

        return progressed;
    }

    private bool DrainRelationshipRetirements(int ownerIndex)
    {
        var cursor = _relationshipRetirements.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_relationshipRetirements.MoveNext(
                   ref cursor,
                   out var projection))
        {
            if (!_simulation.World
                    .TryResolveRetiringRelationshipOccurrence(
                        projection.RelationshipOccurrenceHandle,
                        out var relationshipId))
            {
                continue;
            }

            using var scope =
                _simulation.EnterExecutionContext(projection.Context);
            _simulation.World.ReconcileRelationshipRetirement(
                relationshipId);
            TrackRetiringRelationship(
                projection.RelationshipOccurrenceHandle);
            QueueRelationshipFollowProjection(
                projection.TargetEntityId);
            _simulation.QueueWorldBeatRelationshipEnded(
                projection.RelationshipKindId);
        }

        return progressed;
    }

    private bool DrainEntityStructures(int ownerIndex)
    {
        var cursor = _entityStructures.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_entityStructures.MoveNext(
                   ref cursor,
                   out var projection))
        {
            using var scope =
                _simulation.EnterExecutionContext(projection.Context);
            switch (projection.OperationCode)
            {
                case EntityStructuralProjectionCodes.CultureChanged:
                    if (!_simulation.World.TryResolveEntityOccurrence(
                            projection.EntityOccurrenceHandle,
                            out var cultureEntityId))
                    {
                        break;
                    }

                    _simulation.World.ReconcileEntityCulture(
                        cultureEntityId,
                        projection.PreviousCultureId,
                        projection.CurrentCultureId);
                    break;
                case EntityStructuralProjectionCodes.Retired:
                    if (!_simulation.World
                            .TryResolveRetiringEntityOccurrence(
                                projection.EntityOccurrenceHandle,
                                out var retiringEntityId))
                    {
                        break;
                    }

                    ReconcileEntityRetirement(
                        retiringEntityId,
                        projection);
                    break;
                default:
                    throw new global::System.InvalidOperationException();
            }
        }

        return progressed;
    }

    private void ReconcileEntityRetirement(
        int entityId,
        in EntityStructuralProjection projection)
    {
        _simulation.World.ReconcileEntityRetirement(entityId);
        _simulation.ReleaseCoverageOwner(
            entityId,
            projection.Tick);
        for (var relationshipId =
                 _simulation.World.FirstIncidentRelationship(entityId);
             relationshipId > 0;)
        {
            var nextRelationshipId =
                _simulation.World.NextIncidentRelationship(
                    entityId,
                    relationshipId);
            _relationshipEnds.Add(
                _topology.RelationshipOwner(relationshipId),
                new RelationshipEndDelta(
                    _simulation.World
                        .RelationshipOccurrenceHandleForIntent(
                            relationshipId),
                    ThresholdUnits: 0,
                    Unconditional: true,
                    projection.Context));
            relationshipId = nextRelationshipId;
        }

        _simulation.World.RemoveDistrictsOfHost(entityId);
        QueueEntityRetirementCleanup(
            projection,
            KernelProgramCodes.CultureSlots);
        QueueEntityRetirementCleanup(
            projection,
            KernelProgramCodes.MinorActors);
        QueueEntityRetirementCleanup(
            projection,
            KernelProgramCodes.ChronicleDirector);
        TrackRetiringEntity(projection.EntityOccurrenceHandle);
    }

    private void QueueEntityRetirementCleanup(
        in EntityStructuralProjection projection,
        int programId) =>
        _entityRetirementCleanups.Add(
            _topology.ProgramOwner(programId),
            new EntityRetirementCleanup(
                projection.EntityOccurrenceHandle,
                programId,
                projection.Tick,
                projection.Context));

    private void QueueRelationshipFollowProjection(int targetEntityId)
    {
        var targetOccurrenceHandle =
            _simulation.World.EntityOccurrenceHandleForIntent(
                targetEntityId);
        _simulation.Relationships.ResolveFollowProjection(
            _simulation.World,
            targetEntityId,
            out var relationshipId,
            out var sourceEntityId);
        _relationshipFollowProjections.Add(
            _topology.EntityOccurrenceOwner(
                targetOccurrenceHandle),
            new RelationshipFollowProjection(
                targetOccurrenceHandle,
                relationshipId,
                sourceEntityId));
    }

    private void TrackRetiringEntity(int occurrenceHandle)
    {
        if (_retiringEntityCount >=
            _retiringEntityOccurrences.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _retiringEntityOccurrences[_retiringEntityCount++] =
            occurrenceHandle;
    }

    private void TrackRetiringRelationship(int occurrenceHandle)
    {
        if (_retiringRelationshipCount >=
            _retiringRelationshipOccurrences.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _retiringRelationshipOccurrences[
            _retiringRelationshipCount++] = occurrenceHandle;
    }

    private bool DrainAttributeSets(int ownerIndex)
    {
        var cursor = _attributeSets.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_attributeSets.MoveNext(ref cursor, out var intent))
        {
            if (_simulation.World.TryResolveEntityOccurrence(
                    intent.EntityOccurrenceHandle,
                    out var entityId))
            {
                using var scope =
                    _simulation.EnterExecutionContext(intent.Context);
                _simulation.World.SetAttribute(
                    entityId,
                    intent.AttributeId,
                    FixedScalar.FromUnits(intent.ValueUnits),
                    KernelWorkerThread.OwnerTick,
                    intent.Context);
            }
        }

        return progressed;
    }

    private bool DrainEntityStates(int ownerIndex)
    {
        var cursor = _entityStates.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_entityStates.MoveNext(ref cursor, out var intent))
        {
            if (!_simulation.World.TryResolveEntityOccurrence(
                    intent.EntityOccurrenceHandle,
                    out var entityId))
            {
                continue;
            }

            using var scope =
                _simulation.EnterExecutionContext(intent.Context);
            switch (intent.OperationCode)
            {
                case EntityStateIntentCodes.IncreaseWorkTier:
                    _ = _simulation.World.IncreaseEntityWorkTier(
                        entityId,
                        intent.Value);
                    break;
                case EntityStateIntentCodes.DecreaseWorkTier:
                    _ = _simulation.World.DecreaseEntityWorkTier(entityId);
                    break;
                case EntityStateIntentCodes.SetCulture:
                    ApplyEntityCulture(entityId, intent);
                    break;
                case EntityStateIntentCodes.SetStatus:
                    if (intent.Value == EntityStatusCodes.Historical)
                    {
                        BeginEntityRetirement(
                            entityId,
                            intent.EntityOccurrenceHandle,
                            intent.Context);
                    }
                    else
                    {
                        _simulation.World.SetEntityStatus(
                            entityId,
                            intent.Value,
                            KernelWorkerThread.OwnerTick,
                            intent.Context);
                    }
                    break;
                default:
                    throw new global::System.InvalidOperationException();
            }
        }

        return progressed;
    }

    private void ApplyEntityCulture(
        int entityId,
        in EntityStateIntent intent)
    {
        if (!_simulation.World.ApplyEntityCultureOwned(
                entityId,
                intent.Value,
                KernelWorkerThread.OwnerTick,
                intent.Context,
                out var previousCultureId))
        {
            return;
        }

        _entityStructures.Add(
            _topology.StructuralOwner,
            new EntityStructuralProjection(
                intent.EntityOccurrenceHandle,
                EntityStructuralProjectionCodes.CultureChanged,
                previousCultureId,
                intent.Value,
                KernelWorkerThread.OwnerTick,
                intent.Context));
    }

    private bool DrainEntityCreationRequests(int ownerIndex)
    {
        var cursor = _entityCreationRequests.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_entityCreationRequests.MoveNext(ref cursor, out var intent))
        {
            _simulation.PrepareStrategicEntity(
                intent,
                out var physicalCellId,
                out var semanticCellId);
            var prepared = new PreparedEntityCreation(
                intent,
                physicalCellId,
                semanticCellId);
            _entityCreations.Add(
                _topology.EntityOwner(intent.EntityId),
                prepared);
        }

        return progressed;
    }

    private bool DrainEntityCreations(int ownerIndex)
    {
        var cursor = _entityCreations.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_entityCreations.MoveNext(ref cursor, out var prepared))
        {
            _simulation.ApplyStrategicEntityOwned(prepared.Intent);
            _entityCreationStructures.Add(
                _topology.StructuralOwner,
                prepared);
        }

        return progressed;
    }

    private bool DrainEntityCreationStructures(int ownerIndex)
    {
        var cursor = _entityCreationStructures.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_entityCreationStructures.MoveNext(
                   ref cursor,
                   out var prepared))
        {
            using var producer = EnterProducer();
            _simulation.ReconcileStrategicEntity(
                prepared.Intent,
                prepared.PhysicalCellId,
                prepared.SemanticCellId);
        }

        return progressed;
    }

    private bool DrainRelationshipCreationRequests(int ownerIndex)
    {
        var cursor = _relationshipCreationRequests.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_relationshipCreationRequests.MoveNext(
                   ref cursor,
                   out var intent))
        {
            if ((!_simulation.World.EntityIsActive(
                     intent.SourceEntityId) &&
                 _simulation.World.EntityIsReserved(
                     intent.SourceEntityId)) ||
                (!_simulation.World.EntityIsActive(
                     intent.TargetEntityId) &&
                 _simulation.World.EntityIsReserved(
                     intent.TargetEntityId)))
            {
                NoteRelationshipCreationDeferral();
                _relationshipCreationRequests.Add(
                    _topology.StructuralOwner,
                    intent);
                continue;
            }

            if (_simulation.TryPrepareStrategicRelationship(intent))
            {
                _relationshipCreations.Add(
                    _topology.RelationshipOwner(intent.RelationshipId),
                    new PreparedRelationshipCreation(intent));
            }
        }

        return progressed;
    }

    private bool DrainRelationshipCreations(int ownerIndex)
    {
        var cursor = _relationshipCreations.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_relationshipCreations.MoveNext(ref cursor, out var prepared))
        {
            _simulation.ApplyStrategicRelationshipOwned(prepared.Intent);
            _relationshipCreationStructures.Add(
                _topology.StructuralOwner,
                prepared.Intent);
        }

        return progressed;
    }

    private bool DrainRelationshipCreationStructures(int ownerIndex)
    {
        var cursor = _relationshipCreationStructures.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_relationshipCreationStructures.MoveNext(
                   ref cursor,
                   out var intent))
        {
            _simulation.ReconcileStrategicRelationship(intent);
            QueueRelationshipFollowProjection(
                intent.TargetEntityId);
        }

        return progressed;
    }

    private bool DrainRelationshipSets(int ownerIndex)
    {
        var cursor = _relationshipSets.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_relationshipSets.MoveNext(ref cursor, out var intent))
        {
            if (_simulation.World.TryResolveRelationshipOccurrence(
                    intent.RelationshipOccurrenceHandle,
                    out var relationshipId))
            {
                using var scope =
                    _simulation.EnterExecutionContext(intent.Context);
                _simulation.World.SetRelationshipStrength(
                    relationshipId,
                    KernelFixedPoint.RelationshipStrengthScalar(
                        intent.StrengthUnits),
                    KernelWorkerThread.OwnerTick,
                    intent.Context);
            }
        }

        return progressed;
    }

    private bool DrainTravelStructures(int ownerIndex)
    {
        var cursor = _travelStructures.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_travelStructures.MoveNext(ref cursor, out var intent))
        {
            if (!_simulation.Movement.TryPrepareOwnedTravelStructure(
                    _simulation,
                    intent.PlaneCode,
                    intent.Q,
                    intent.R,
                    intent.TravelStructureId,
                    out var cellId))
            {
                continue;
            }

            var resolved = new ResolvedTravelStructure(cellId, intent);
            using var scope =
                _simulation.EnterExecutionContext(intent.Context);
            if (_simulation.Movement.ApplyOwnedTravelStructure(
                    _simulation,
                    resolved,
                    KernelWorkerThread.OwnerTick,
                    out var previousStructureId))
            {
                _simulation.Movement.ReconcileOwnedTravelStructure(
                    resolved,
                    previousStructureId);
            }
        }

        return progressed;
    }

    private bool DrainForgeCells(int ownerIndex)
    {
        var cursor = _forgeCells.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_forgeCells.MoveNext(ref cursor, out var intent))
        {
            if (!_simulation.World.TryResolveEntityOccurrence(
                    intent.EntityOccurrenceHandle,
                    out var entityId))
            {
                continue;
            }

            using var scope =
                _simulation.EnterExecutionContext(intent.Context);
            _ = _simulation.TryForgeCellVertical(
                entityId,
                intent.Direction,
                intent.TerrainId,
                KernelWorkerThread.OwnerTick);
        }

        return progressed;
    }

    private bool DrainDistricts(int ownerIndex)
    {
        var cursor = _districts.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_districts.MoveNext(ref cursor, out var intent))
        {
            using var scope =
                _simulation.EnterExecutionContext(intent.Context);
            EnqueueDistrictResolution(
                _simulation.Gambits.ResolveDistrictIntent(
                    _simulation,
                    intent));
        }

        return progressed;
    }

    private void EnqueueDistrictResolution(
        in DistrictResolution resolution)
    {
        if (resolution.CellState.Context.SourceCodeId > 0)
        {
            var intent = resolution.CellState;
            var world = _simulation.World;
            var terrainId = intent.TerrainId > 0
                ? intent.TerrainId
                : world.CellBaseTerrainId(
                    intent.PlaneCode,
                    intent.Q,
                    intent.R);
            var ownerEntityId = intent.OwnerEntityId >= 0
                ? intent.OwnerEntityId
                : world.CoverageOwnerAt(
                    intent.PlaneCode,
                    intent.Q,
                    intent.R);
            Enqueue(new CellStateDelta(
                intent.PlaneCode,
                intent.Q,
                intent.R,
                terrainId,
                ownerEntityId,
                intent.Context));
        }

        if (resolution.EntityState.Context.SourceCodeId > 0)
        {
            Enqueue(resolution.EntityState);
        }

        if (resolution.EntityCreation.EntityId > 0)
        {
            Enqueue(resolution.EntityCreation);
        }

        if (resolution.RelationshipCreation.RelationshipId > 0)
        {
            Enqueue(resolution.RelationshipCreation);
        }
    }

    private bool DrainCellMaterializations(int ownerIndex)
    {
        var cursor = _cellMaterializations.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_cellMaterializations.MoveNext(ref cursor, out var intent))
        {
            using var scope =
                _simulation.EnterExecutionContext(intent.Context);
            _simulation.MaterializeOwnedCell(intent);
        }

        return progressed;
    }

    private bool DrainTerrainCovers(int ownerIndex)
    {
        var cursor = _terrainCovers.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_terrainCovers.MoveNext(ref cursor, out var intent))
        {
            if (!_simulation.World.CellIsKnown(
                    PlaneCodes.Physical,
                    intent.Q,
                    intent.R))
            {
                NoteTerrainCoverDeferral();
                Enqueue(intent);
                continue;
            }

            using var scope =
                _simulation.EnterExecutionContext(intent.Context);
            _simulation.ApplyOwnedTerrainCover(intent);
        }

        return progressed;
    }

    private bool DrainProgramSignals(int ownerIndex)
    {
        var cursor = _programSignals.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_programSignals.MoveNext(ref cursor, out var signal))
        {
            _simulation.ApplyProgramSignal(signal);
        }

        return progressed;
    }

    private bool DrainRelationshipFollowProjections(
        int ownerIndex)
    {
        var cursor =
            _relationshipFollowProjections.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_relationshipFollowProjections.MoveNext(
                   ref cursor,
                   out var projection))
        {
            if (_simulation.World.TryResolveEntityOccurrence(
                    projection.TargetEntityOccurrenceHandle,
                    out var targetEntityId))
            {
                _simulation.Relationships.ApplyFollowProjection(
                    targetEntityId,
                    projection.RelationshipId,
                    projection.SourceEntityId);
            }
        }

        return progressed;
    }

    private bool DrainEntityRetirementCleanups(int ownerIndex)
    {
        var cursor =
            _entityRetirementCleanups.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_entityRetirementCleanups.MoveNext(
                   ref cursor,
                   out var cleanup))
        {
            if (!_simulation.World
                    .TryResolveRetiringEntityOccurrence(
                        cleanup.EntityOccurrenceHandle,
                        out var entityId))
            {
                continue;
            }

            using var scope =
                _simulation.EnterExecutionContext(cleanup.Context);
            switch (cleanup.ProgramId)
            {
                case KernelProgramCodes.CultureSlots:
                    _simulation.CultureSlots.NoteEntityRetired(
                        _simulation,
                        entityId,
                        cleanup.Tick);
                    break;
                case KernelProgramCodes.MinorActors:
                    _simulation.MinorActors.NoteEntityRetired(
                        _simulation,
                        entityId,
                        cleanup.Tick);
                    break;
                case KernelProgramCodes.ChronicleDirector:
                    _simulation.Chronicles.NoteEntityRetired(
                        entityId);
                    break;
                default:
                    throw new global::System.InvalidOperationException();
            }
        }

        return progressed;
    }

    private void FinalizeRetiredSlots()
    {
        for (var index = 0;
             index < _retiringRelationshipCount;
             index++)
        {
            var occurrenceHandle =
                _retiringRelationshipOccurrences[index];
            if (_simulation.World
                .TryResolveRetiringRelationshipOccurrence(
                    occurrenceHandle,
                    out var relationshipId))
            {
                _simulation.World.FinalizeRelationshipRetirement(
                    relationshipId);
            }

            _retiringRelationshipOccurrences[index] = 0;
        }

        _retiringRelationshipCount = 0;
        for (var index = 0;
             index < _retiringEntityCount;
             index++)
        {
            var occurrenceHandle =
                _retiringEntityOccurrences[index];
            if (_simulation.World
                .TryResolveRetiringEntityOccurrence(
                    occurrenceHandle,
                    out var entityId))
            {
                _simulation.World.FinalizeEntityRetirement(entityId);
            }

            _retiringEntityOccurrences[index] = 0;
        }

        _retiringEntityCount = 0;
    }

    private bool DrainEntityInfluenceMultipliers(int ownerIndex)
    {
        var cursor = _entityInfluenceMultipliers.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_entityInfluenceMultipliers.MoveNext(
                   ref cursor,
                   out var intent))
        {
            if (_simulation.World.TryResolveEntityOccurrence(
                    intent.EntityOccurrenceHandle,
                    out var entityId))
            {
                _simulation.World
                    .ModifyEntityCultureInfluenceMultiplierDeltaUnits(
                        entityId,
                        intent.DeltaUnits,
                        intent.Tick,
                        intent.Context);
            }
        }

        return progressed;
    }

    private bool DrainCultureInfluenceMultipliers(int ownerIndex)
    {
        var cursor = _cultureInfluenceMultipliers.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_cultureInfluenceMultipliers.MoveNext(
                   ref cursor,
                   out var intent))
        {
            _simulation.World.ModifyCultureInfluenceMultiplierDeltaUnits(
                intent.CultureId,
                intent.DeltaUnits,
                intent.Tick,
                intent.Context);
        }

        return progressed;
    }

    private static ContinuousProducerScope EnterProducer()
    {
        var worker = KernelWorkerThread.Current ??
            throw new global::System.InvalidOperationException();
        return new ContinuousProducerScope(worker);
    }

    private readonly struct ContinuousProducerScope :
        global::System.IDisposable
    {
        private readonly KernelWorker _worker;
        private readonly bool _previous;

        internal ContinuousProducerScope(KernelWorker worker)
        {
            _worker = worker;
            _previous = worker.IsContinuousOwnerEvaluation;
            worker.IsContinuousOwnerEvaluation = true;
        }

        public void Dispose()
        {
            _worker.IsContinuousOwnerEvaluation = _previous;
        }
    }
}
