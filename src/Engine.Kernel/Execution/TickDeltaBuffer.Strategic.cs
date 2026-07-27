namespace Engine.Kernel;

internal sealed partial class TickDeltaBuffer
{
    private void ExecuteStrategicOwner(
        Simulation simulation,
        int ownerStage,
        int ownerIndex,
        int ownerCount)
    {
        switch (ownerStage)
        {
            case AttributeSetOwnerStage:
                ResolveAttributeSetOwner(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            case RelationshipSetOwnerStage:
                ResolveRelationshipSetOwner(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            case StrategicCellOwnerStage:
                ResolveStrategicCellOwner(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            case EntityWorkTierOwnerStage:
                ResolveEntityWorkTierOwner(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            case EntityCreationOwnerStage:
                ResolveEntityCreationOwner(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            case RelationshipCreationOwnerStage:
                ResolveRelationshipCreationOwner(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            case TravelStructureOwnerStage:
                ResolveTravelStructureOwner(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            default:
                throw new global::System.InvalidOperationException();
        }
    }

    private bool SameStrategicOwnerAtBoundary(
        int index,
        int ownerStage)
    {
        if (ownerStage == AttributeSetOwnerStage)
        {
            return _attributeSets[index - 1].EntityOccurrenceHandle ==
                _attributeSets[index].EntityOccurrenceHandle;
        }

        if (ownerStage == RelationshipSetOwnerStage)
        {
            return _relationshipSets[index - 1].
                    RelationshipOccurrenceHandle ==
                _relationshipSets[index].RelationshipOccurrenceHandle;
        }

        if (ownerStage == StrategicCellOwnerStage)
        {
            return _strategicCellStates[index - 1].SameDestination(
                _strategicCellStates[index]);
        }

        if (ownerStage == EntityWorkTierOwnerStage)
        {
            return _entityStates[index - 1].EntityOccurrenceHandle ==
                _entityStates[index].EntityOccurrenceHandle;
        }

        if (ownerStage == TravelStructureOwnerStage)
        {
            return _resolvedTravelStructures[index - 1].CellId ==
                _resolvedTravelStructures[index].CellId;
        }

        throw new global::System.InvalidOperationException();
    }

    private void ResolveEntityCreations(Simulation simulation)
    {
        PrepareEntityCreations(simulation);
        ExecuteOwnerStage(
            simulation,
            EntityCreationOwnerStage,
            _entityCreationCount);
        ReconcileEntityCreations(simulation);
    }

    private void PrepareEntityCreations(Simulation simulation)
    {
        for (var index = 0; index < _entityCreationCount; index++)
        {
            simulation.PrepareStrategicEntity(
                _entityCreations[index],
                out _entityCreationPhysicalCellIds[index],
                out _entityCreationSemanticCellIds[index]);
        }
    }

    private void ReconcileEntityCreations(Simulation simulation)
    {
        for (var index = 0; index < _entityCreationCount; index++)
        {
            simulation.ReconcileStrategicEntity(
                _entityCreations[index],
                _entityCreationPhysicalCellIds[index],
                _entityCreationSemanticCellIds[index]);
        }
    }

    private void ResolveRelationshipCreations(Simulation simulation)
    {
        PrepareRelationshipCreations(simulation);
        ExecuteOwnerStage(
            simulation,
            RelationshipCreationOwnerStage,
            _resolvedRelationshipCreationCount);
        ReconcileRelationshipCreations(simulation);
    }

    private void ReconcileRelationshipCreations(Simulation simulation)
    {
        for (var index = 0;
             index < _resolvedRelationshipCreationCount;
             index++)
        {
            simulation.ReconcileStrategicRelationship(
                _resolvedRelationshipCreations[index]);
        }
    }

    private void PrepareRelationshipCreations(Simulation simulation)
    {
        _resolvedRelationshipCreationCount = 0;
        var index = 0;
        while (index < _relationshipCreationCount)
        {
            var candidate = _relationshipCreations[index];
            var end = index + 1;
            while (end < _relationshipCreationCount &&
                   SameRelationshipTopology(
                       candidate,
                       _relationshipCreations[end]))
            {
                end++;
            }

            if (simulation.TryPrepareStrategicRelationship(candidate))
            {
                _resolvedRelationshipCreations[
                    _resolvedRelationshipCreationCount++] = candidate;
            }

            for (var duplicate = index + 1;
                 duplicate < end;
                 duplicate++)
            {
                simulation.World.ReleaseUnusedReservedRelationshipSlot(
                    _relationshipCreations[duplicate].RelationshipId);
            }

            index = end;
        }
    }

    private static bool SameRelationshipTopology(
        in RelationshipCreationIntent left,
        in RelationshipCreationIntent right) =>
        left.SourceEntityId == right.SourceEntityId &&
        left.TargetEntityId == right.TargetEntityId &&
        left.KindId == right.KindId;

    private void ResolveEntityCreationOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = (int)((long)_entityCreationCount *
            ownerIndex / ownerCount);
        var limit = (int)((long)_entityCreationCount *
            (ownerIndex + 1) / ownerCount);
        while (index < limit)
        {
            simulation.ApplyStrategicEntityOwned(
                _entityCreations[index++]);
        }
    }

    private void ResolveRelationshipCreationOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = (int)((long)_resolvedRelationshipCreationCount *
            ownerIndex / ownerCount);
        var limit = (int)((long)_resolvedRelationshipCreationCount *
            (ownerIndex + 1) / ownerCount);
        while (index < limit)
        {
            simulation.ApplyStrategicRelationshipOwned(
                _resolvedRelationshipCreations[index++]);
        }
    }

    private void ResolveAttributeSetOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = OwnerStart(
            ownerIndex,
            ownerCount,
            _attributeSetCount,
            AttributeSetOwnerStage);
        var limit = OwnerStart(
            ownerIndex + 1,
            ownerCount,
            _attributeSetCount,
            AttributeSetOwnerStage);
        while (index < limit)
        {
            var first = _attributeSets[index];
            var end = index + 1;
            while (end < limit &&
                   _attributeSets[end].EntityOccurrenceHandle ==
                       first.EntityOccurrenceHandle &&
                   _attributeSets[end].AttributeId == first.AttributeId)
            {
                end++;
            }

            var winner = _attributeSets[end - 1];
            if (simulation.World.TryResolveEntityOccurrence(
                    winner.EntityOccurrenceHandle,
                    out var entityId))
            {
                using var scope =
                    simulation.EnterExecutionContext(winner.Context);
                simulation.SetAttribute(
                    entityId,
                    winner.AttributeId,
                    FixedScalar.FromUnits(winner.ValueUnits),
                    simulation.CurrentTick);
            }

            index = end;
        }
    }

    private void ResolveEntityWorkTierOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = OwnerStart(
            ownerIndex,
            ownerCount,
            _entityStateCount,
            EntityWorkTierOwnerStage);
        var limit = OwnerStart(
            ownerIndex + 1,
            ownerCount,
            _entityStateCount,
            EntityWorkTierOwnerStage);
        for (; index < limit; index++)
        {
            var intent = _entityStates[index];
            if (intent.OperationCode is
                EntityStateIntentCodes.SetCulture or
                EntityStateIntentCodes.SetStatus)
            {
                continue;
            }

            if (!simulation.World.TryResolveEntityOccurrence(
                    intent.EntityOccurrenceHandle,
                    out var entityId))
            {
                continue;
            }

            using var scope = simulation.EnterExecutionContext(intent.Context);
            switch (intent.OperationCode)
            {
                case EntityStateIntentCodes.IncreaseWorkTier:
                    _ = simulation.World.IncreaseEntityWorkTier(
                        entityId,
                        intent.Value);
                    break;
                case EntityStateIntentCodes.DecreaseWorkTier:
                    _ = simulation.World.DecreaseEntityWorkTier(entityId);
                    break;
                default:
                    throw new global::System.InvalidOperationException();
            }
        }
    }

    private void ResolveStructuralEntityStates(Simulation simulation)
    {
        for (var index = 0; index < _entityStateCount; index++)
        {
            var intent = _entityStates[index];
            if (intent.OperationCode is not
                (EntityStateIntentCodes.SetCulture or
                 EntityStateIntentCodes.SetStatus) ||
                !simulation.World.TryResolveEntityOccurrence(
                    intent.EntityOccurrenceHandle,
                    out var entityId))
            {
                continue;
            }

            using var scope = simulation.EnterExecutionContext(intent.Context);
            if (intent.OperationCode == EntityStateIntentCodes.SetCulture)
            {
                simulation.SetEntityCulture(
                    entityId,
                    intent.Value,
                    simulation.CurrentTick);
            }
            else
            {
                simulation.SetEntityStatus(
                    entityId,
                    intent.Value,
                    simulation.CurrentTick);
            }
        }
    }

    private void ResolveRelationshipSetOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = OwnerStart(
            ownerIndex,
            ownerCount,
            _relationshipSetCount,
            RelationshipSetOwnerStage);
        var limit = OwnerStart(
            ownerIndex + 1,
            ownerCount,
            _relationshipSetCount,
            RelationshipSetOwnerStage);
        while (index < limit)
        {
            var first = _relationshipSets[index];
            var end = index + 1;
            while (end < limit &&
                   _relationshipSets[end].RelationshipOccurrenceHandle ==
                       first.RelationshipOccurrenceHandle)
            {
                end++;
            }

            var winner = _relationshipSets[end - 1];
            if (simulation.World.TryResolveRelationshipOccurrence(
                    winner.RelationshipOccurrenceHandle,
                    out var relationshipId))
            {
                using var scope =
                    simulation.EnterExecutionContext(winner.Context);
                simulation.SetRelationshipStrength(
                    relationshipId,
                    KernelFixedPoint.RelationshipStrengthScalar(
                        winner.StrengthUnits),
                    simulation.CurrentTick);
            }

            index = end;
        }
    }

    private void ResolveStrategicCellStates(Simulation simulation)
    {
        PrepareStrategicCellStates(simulation.World);
        ExecuteOwnerStage(
            simulation,
            StrategicCellOwnerStage,
            _strategicCellStateCount);
        ReconcileStrategicCellStates(simulation);
    }

    private void PrepareStrategicCellStates(WorldState world)
    {
        for (var index = 0; index < _strategicCellStateCount; index++)
        {
            var intent = _strategicCellStates[index];
            _strategicCellIds[index] = intent.TerrainId > 0
                ? world.PrepareOwnedCellState(
                    intent.PlaneCode,
                    intent.Q,
                    intent.R)
                : world.RequireCellId(
                    intent.PlaneCode,
                    intent.Q,
                    intent.R,
                    z: 0);
        }
    }

    private void ReconcileStrategicCellStates(Simulation simulation)
    {
        var world = simulation.World;
        for (var index = 0; index < _strategicCellStateCount; index++)
        {
            if (!_strategicCellChanged[index])
            {
                continue;
            }

            var intent = _strategicCellStates[index];
            world.ReconcileOwnedCellState(
                _strategicCellIds[index],
                intent.PlaneCode,
                intent.Q,
                intent.R,
                _strategicCellPreviousOwners[index],
                _strategicCellAppliedOwners[index]);
            if (_strategicCellTerrainChanged[index])
            {
                simulation.Movement.RecordTopologyChanged();
            }
        }
    }

    private void ResolveStrategicCellOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = OwnerStart(
            ownerIndex,
            ownerCount,
            _strategicCellStateCount,
            StrategicCellOwnerStage);
        var limit = OwnerStart(
            ownerIndex + 1,
            ownerCount,
            _strategicCellStateCount,
            StrategicCellOwnerStage);
        while (index < limit)
        {
            var intent = _strategicCellStates[index];
            var world = simulation.World;
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
            _strategicCellAppliedOwners[index] = ownerEntityId;
            using var scope =
                simulation.EnterExecutionContext(intent.Context);
            _strategicCellChanged[index] = world.ApplyOwnedCellState(
                _strategicCellIds[index],
                intent.PlaneCode,
                intent.Q,
                intent.R,
                terrainId,
                ownerEntityId,
                simulation.CurrentTick,
                intent.Context,
                out _strategicCellPreviousOwners[index],
                out _strategicCellTerrainChanged[index]);
            index++;
        }
    }

    private void ResolveTravelStructures(Simulation simulation)
    {
        PrepareTravelStructures(simulation);
        ExecuteOwnerStage(
            simulation,
            TravelStructureOwnerStage,
            _resolvedTravelStructureCount);
        ReconcileTravelStructures(simulation);
    }

    private void PrepareTravelStructures(Simulation simulation)
    {
        _resolvedTravelStructureCount = 0;
        var index = 0;
        while (index < _travelStructureCount)
        {
            var first = _travelStructures[index];
            var end = index + 1;
            while (end < _travelStructureCount &&
                   _travelStructures[end].PlaneCode == first.PlaneCode &&
                   _travelStructures[end].Q == first.Q &&
                   _travelStructures[end].R == first.R)
            {
                end++;
            }

            var winner = _travelStructures[end - 1];
            if (simulation.Movement.TryPrepareOwnedTravelStructure(
                    simulation,
                    winner.PlaneCode,
                    winner.Q,
                    winner.R,
                    winner.TravelStructureId,
                    out var cellId))
            {
                _resolvedTravelStructures[
                    _resolvedTravelStructureCount++] =
                    new ResolvedTravelStructure(cellId, winner);
            }

            index = end;
        }
    }

    private void ReconcileTravelStructures(Simulation simulation)
    {
        for (var index = 0;
             index < _resolvedTravelStructureCount;
             index++)
        {
            if (_resolvedTravelStructureChanged[index])
            {
                simulation.Movement.ReconcileOwnedTravelStructure(
                    _resolvedTravelStructures[index],
                    _resolvedTravelStructurePreviousIds[index]);
            }
        }
    }

    private void ResolveTravelStructureOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = OwnerStart(
            ownerIndex,
            ownerCount,
            _resolvedTravelStructureCount,
            TravelStructureOwnerStage);
        var limit = OwnerStart(
            ownerIndex + 1,
            ownerCount,
            _resolvedTravelStructureCount,
            TravelStructureOwnerStage);
        while (index < limit)
        {
            var resolved = _resolvedTravelStructures[index];
            using var scope = simulation.EnterExecutionContext(
                resolved.Intent.Context);
            _resolvedTravelStructureChanged[index] =
                simulation.Movement.ApplyOwnedTravelStructure(
                    simulation,
                    resolved,
                    simulation.CurrentTick,
                    out _resolvedTravelStructurePreviousIds[index]);
            index++;
        }
    }

    private void ResolveForgeCells(Simulation simulation)
    {
        for (var index = 0; index < _forgeCellCount; index++)
        {
            var intent = _forgeCells[index];
            if (!simulation.World.TryResolveEntityOccurrence(
                    intent.EntityOccurrenceHandle,
                    out var entityId))
            {
                continue;
            }

            using var scope = simulation.EnterExecutionContext(intent.Context);
            _ = simulation.TryForgeCellVertical(
                entityId,
                intent.Direction,
                intent.TerrainId,
                simulation.CurrentTick);
        }
    }

    private void ResolveDistricts(Simulation simulation)
    {
        for (var index = 0; index < _districtCount; index++)
        {
            var intent = _districts[index];
            using var scope = simulation.EnterExecutionContext(intent.Context);
            AppendDistrictResolution(
                simulation.Gambits.ResolveDistrictIntent(
                    simulation,
                    intent));
        }

        Prepare(
            _entityCreations,
            _entityCreationCount,
            reverse: false);
        Prepare(
            _relationshipCreations,
            _relationshipCreationCount,
            reverse: false);
        Prepare(
            _strategicCellStates,
            _strategicCellStateCount,
            reverse: false);
        Prepare(
            _entityStates,
            _entityStateCount,
            reverse: false);
    }

    private void AppendDistrictResolution(
        in DistrictResolution resolution)
    {
        if (resolution.CellState.Context.SourceCodeId > 0)
        {
            _strategicCellStates[_strategicCellStateCount++] =
                resolution.CellState;
        }

        if (resolution.EntityState.Context.SourceCodeId > 0)
        {
            _entityStates[_entityStateCount++] =
                resolution.EntityState;
        }

        if (resolution.EntityCreation.EntityId > 0)
        {
            _entityCreations[_entityCreationCount++] =
                resolution.EntityCreation;
        }

        if (resolution.RelationshipCreation.RelationshipId > 0)
        {
            _relationshipCreations[_relationshipCreationCount++] =
                resolution.RelationshipCreation;
        }
    }
}
