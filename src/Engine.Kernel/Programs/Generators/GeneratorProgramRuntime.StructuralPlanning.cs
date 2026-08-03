namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class GeneratorProgramRuntime
{
    private int CommitPlannedEntities(
        Simulation simulation,
        int[] refCodes,
        int[] refEntityIds,
        ref int refCount,
        int[] createdEntityIds)
    {
        var plannedCount = _plannedCreateCount;
        if (plannedCount > 0)
        {
            simulation.ReserveStrategicEntityBlock(
                global::System.MemoryExtensions.AsSpan(
                    _reservedEntityIds,
                    0,
                    plannedCount));
        }

        for (var planIndex = 0; planIndex < plannedCount; planIndex++)
        {
            var produced = _plannedProducedSlots[planIndex];
            var createdEntityId = CreateProducedEntity(
                simulation,
                _reservedEntityIds[planIndex],
                produced,
                _plannedSubtypeIds[planIndex],
                _plannedCultureIds[planIndex],
                _plannedSpawnPhysicalQ[planIndex],
                _plannedSpawnPhysicalR[planIndex],
                _plannedSemanticQ[planIndex],
                _plannedSemanticR[planIndex]);
            createdEntityIds[planIndex] = createdEntityId;
            SetRef(
                refCodes,
                refEntityIds,
                ref refCount,
                _producedRefCodes[produced],
                createdEntityId);
            CommitPlannedCoverage(simulation, planIndex, createdEntityId);
            CommitPlannedFinalPosition(
                simulation,
                planIndex,
                createdEntityId);
        }

        return plannedCount;
    }

    private void CommitPlannedFinalPosition(
        Simulation simulation,
        int planIndex,
        int createdEntityId)
    {
        if (_plannedFinalPhysicalQ[planIndex] ==
                _plannedSpawnPhysicalQ[planIndex] &&
            _plannedFinalPhysicalR[planIndex] ==
                _plannedSpawnPhysicalR[planIndex])
        {
            return;
        }

        _ = simulation.TryQueueMovement(
            createdEntityId,
            PlaneCodes.Physical,
            _plannedFinalPhysicalQ[planIndex],
            _plannedFinalPhysicalR[planIndex],
            z: 0,
            MovementCauseCodes.Placement);
    }

    private bool TryResolveMissingProducedPlacement(
        Simulation simulation,
        WorldState world,
        int produced,
        int targetId,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        out int subtypeId,
        out int producedCultureId,
        out int physicalQ,
        out int physicalR,
        out int semanticQ,
        out int semanticR,
        out int failureReasonCode)
    {
        if (!TryResolveProducedCreationInputs(
                simulation,
                world,
                produced,
                targetId,
                refCodes,
                refEntityIds,
                refCount,
                out subtypeId,
                out producedCultureId,
                out var placementAnchorId))
        {
            physicalQ = 0;
            physicalR = 0;
            semanticQ = 0;
            semanticR = 0;
            failureReasonCode =
                GeneratorTargetRejectionReasonCodes.SecondaryBinding;
            return false;
        }

        failureReasonCode = GeneratorTargetRejectionReasonCodes.Placement;
        return ResolveProducedPlacement(
            simulation,
            placementAnchorId,
            producedCultureId,
            produced,
            subtypeId,
            out physicalQ,
            out physicalR,
            out semanticQ,
            out semanticR);
    }

    private int CreateProducedEntity(
        Simulation simulation,
        int reservedEntityId,
        int produced,
        int subtypeId,
        int producedCultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR)
    {
        simulation.QueueStrategicEntityCreation(
            reservedEntityId,
            _producedKindIds[produced],
            subtypeId,
            producedCultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            -semanticQ - semanticR);
        return reservedEntityId;
    }

    private bool TryPlannedEntity(int entityId, out int planIndex)
    {
        for (var index = 0; index < _plannedCreateCount; index++)
        {
            if (_createdEntityIds[index] != entityId)
            {
                continue;
            }

            planIndex = index;
            return true;
        }

        planIndex = -1;
        return false;
    }

    private int PlannedOrWorldCultureId(WorldState world, int entityId) =>
        TryPlannedEntity(entityId, out var planIndex)
            ? _plannedCultureIds[planIndex]
            : world.EntityCultureId(entityId);

    private int PlannedOrWorldQ(
        WorldState world,
        int entityId,
        int planeCode)
    {
        if (!TryPlannedEntity(entityId, out var planIndex))
        {
            return CurrentQ(world, entityId, planeCode);
        }

        return planeCode == PlaneCodes.Semantic
            ? _plannedSemanticQ[planIndex]
            : _plannedFinalPhysicalQ[planIndex];
    }

    private int PlannedOrWorldR(
        WorldState world,
        int entityId,
        int planeCode)
    {
        if (!TryPlannedEntity(entityId, out var planIndex))
        {
            return CurrentR(world, entityId, planeCode);
        }

        return planeCode == PlaneCodes.Semantic
            ? _plannedSemanticR[planIndex]
            : _plannedFinalPhysicalR[planIndex];
    }

}
