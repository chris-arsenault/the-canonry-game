namespace Engine.Kernel;

internal sealed partial class TickDeltaBuffer
{
    private void ResolvePrepared(Simulation simulation)
    {
        if (_singleWorker)
        {
            ResolvePreparedSingleWorker(simulation);
            return;
        }

        ResolveDistricts(simulation);
        PrepareEntityCreations(simulation);
        ExecuteOwnerBatch(
            simulation,
            EntityCreationOwnerBatch,
            _entityCreationCount);
        ReconcileEntityCreations(simulation);

        PrepareSpatialOwners(simulation);
        ExecuteOwnerBatch(
            simulation,
            SpatialOwnerBatch,
            SpatialOwnerCount());
        ReconcileSpatialOwners(simulation);
        ResolveForgeCells(simulation);

        PrepareCulture(simulation);
        ExecuteOwnerBatch(
            simulation,
            PrimaryStateOwnerBatch,
            PrimaryStateOwnerCount());
        CompleteCulture(simulation);
        PrepareCultureReductions(simulation);

        ExecuteOwnerBatch(
            simulation,
            SecondaryStateOwnerBatch,
            SecondaryStateOwnerCount());
        CompleteCultureReductions(simulation);
        ResolveRelationshipEnds(simulation);
        PrepareRelationshipCreations(simulation);

        ExecuteOwnerBatch(
            simulation,
            FinalStateOwnerBatch,
            FinalStateOwnerCount());
        ReconcileRelationshipCreations(simulation);
        ResolveStructuralEntityStates(simulation);
        ResolveLifecycles(simulation);
    }

    private void ResolvePreparedSingleWorker(Simulation simulation)
    {
        ResolveDistricts(simulation);
        ResolveEntityCreations(simulation);
        ResolveMovements(simulation);
        ResolveCellStates(simulation);
        ResolveStrategicCellStates(simulation);
        ResolveTravelStructures(simulation);
        ResolveForgeCells(simulation);
        ExecuteOwnerStage(
            simulation,
            AttributeSetOwnerStage,
            _attributeSetCount);
        ExecuteOwnerStage(
            simulation,
            AttributeOwnerStage,
            _attributeCount);
        ResolveCulture(simulation);
        ResolveCultureReductions(simulation);
        ExecuteOwnerStage(
            simulation,
            RelationshipOwnerStage,
            _relationshipCount);
        ExecuteOwnerStage(
            simulation,
            RelationshipSetOwnerStage,
            _relationshipSetCount);
        ResolveRelationshipEnds(simulation);
        ResolveRelationshipCreations(simulation);
        ExecuteOwnerStage(
            simulation,
            TagOwnerStage,
            _tagCount);
        ExecuteOwnerStage(
            simulation,
            EntityWorkTierOwnerStage,
            _entityStateCount);
        ResolveStructuralEntityStates(simulation);
        ResolveLifecycles(simulation);
    }

    void IKernelOwnerWork.ExecuteOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        if (_ownerBatch != NoOwnerBatch)
        {
            ExecuteOwnerBatchWorker(
                simulation,
                ownerIndex,
                ownerCount);
            return;
        }

        ExecuteOwnerStageWorker(
            simulation,
            _ownerStage,
            ownerIndex,
            ownerCount);
    }

    private void ExecuteOwnerStageWorker(
        Simulation simulation,
        int ownerStage,
        int ownerIndex,
        int ownerCount)
    {
        switch (ownerStage)
        {
            case AttributeOwnerStage:
                ResolveAttributeOwner(simulation, ownerIndex, ownerCount);
                return;
            case RelationshipOwnerStage:
                ResolveRelationshipOwner(simulation, ownerIndex, ownerCount);
                return;
            case TagOwnerStage:
                ResolveTagOwner(simulation, ownerIndex, ownerCount);
                return;
            case MovementOwnerStage:
                ResolveMovementOwner(simulation, ownerIndex, ownerCount);
                return;
            case CellOwnerStage:
                ResolveCellOwner(simulation, ownerIndex, ownerCount);
                return;
            case CultureOwnerStage:
                ResolveCultureOwner(simulation, ownerIndex, ownerCount);
                return;
            case CultureReductionOwnerStage:
                ResolveCultureReductionOwner(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            case InfluenceCreditOwnerStage:
                ResolveInfluenceCreditOwner(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            default:
                ExecuteStrategicOwner(
                    simulation,
                    ownerStage,
                    ownerIndex,
                    ownerCount);
                return;
        }
    }

    private void ExecuteOwnerBatch(
        Simulation simulation,
        int ownerBatch,
        int maximumWorkItemCount)
    {
        if (maximumWorkItemCount == 0)
        {
            return;
        }

        if (_ownerStage != NoOwnerStage ||
            _ownerBatch != NoOwnerBatch)
        {
            throw new global::System.InvalidOperationException();
        }

        _ownerBatch = ownerBatch;
        try
        {
            simulation.ExecuteOwnerWork(this, maximumWorkItemCount);
        }
        finally
        {
            _ownerBatch = NoOwnerBatch;
        }

        simulation.World.PublishOwnedRevisionChanges();
    }

    private void ExecuteOwnerBatchWorker(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        switch (_ownerBatch)
        {
            case EntityCreationOwnerBatch:
                ExecuteOwnerBatchStage(
                    simulation,
                    EntityCreationOwnerStage,
                    _entityCreationCount,
                    ownerIndex,
                    ownerCount);
                return;
            case SpatialOwnerBatch:
                ExecuteSpatialOwnerBatch(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            case PrimaryStateOwnerBatch:
                ExecutePrimaryStateOwnerBatch(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            case SecondaryStateOwnerBatch:
                ExecuteSecondaryStateOwnerBatch(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            case FinalStateOwnerBatch:
                ExecuteFinalStateOwnerBatch(
                    simulation,
                    ownerIndex,
                    ownerCount);
                return;
            default:
                throw new global::System.InvalidOperationException();
        }
    }

    private void ExecuteSpatialOwnerBatch(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        ExecuteOwnerBatchStage(
            simulation,
            MovementOwnerStage,
            _resolvedMovementCount,
            ownerIndex,
            ownerCount);
        ExecuteOwnerBatchStage(
            simulation,
            CellOwnerStage,
            _resolvedCellStateCount,
            ownerIndex,
            ownerCount);
        ExecuteOwnerBatchStage(
            simulation,
            StrategicCellOwnerStage,
            _strategicCellStateCount,
            ownerIndex,
            ownerCount);
        ExecuteOwnerBatchStage(
            simulation,
            TravelStructureOwnerStage,
            _resolvedTravelStructureCount,
            ownerIndex,
            ownerCount);
    }

    private void ExecutePrimaryStateOwnerBatch(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        ExecuteOwnerBatchStage(
            simulation,
            AttributeSetOwnerStage,
            _attributeSetCount,
            ownerIndex,
            ownerCount);
        ExecuteOwnerBatchStage(
            simulation,
            CultureOwnerStage,
            _resolvedCultureGroupCount,
            ownerIndex,
            ownerCount);
        ExecuteOwnerBatchStage(
            simulation,
            RelationshipOwnerStage,
            _relationshipCount,
            ownerIndex,
            ownerCount);
    }

    private void ExecuteSecondaryStateOwnerBatch(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        ExecuteOwnerBatchStage(
            simulation,
            AttributeOwnerStage,
            _attributeCount,
            ownerIndex,
            ownerCount);
        ExecuteOwnerBatchStage(
            simulation,
            CultureReductionOwnerStage,
            _resolvedCultureReductionGroupCount,
            ownerIndex,
            ownerCount);
        ExecuteOwnerBatchStage(
            simulation,
            InfluenceCreditOwnerStage,
            _influenceCreditCount,
            ownerIndex,
            ownerCount);
        ExecuteOwnerBatchStage(
            simulation,
            RelationshipSetOwnerStage,
            _relationshipSetCount,
            ownerIndex,
            ownerCount);
    }

    private void ExecuteFinalStateOwnerBatch(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        ExecuteOwnerBatchStage(
            simulation,
            RelationshipCreationOwnerStage,
            _resolvedRelationshipCreationCount,
            ownerIndex,
            ownerCount);
        ExecuteOwnerBatchStage(
            simulation,
            TagOwnerStage,
            _tagCount,
            ownerIndex,
            ownerCount);
        ExecuteOwnerBatchStage(
            simulation,
            EntityWorkTierOwnerStage,
            _entityStateCount,
            ownerIndex,
            ownerCount);
    }

    private void ExecuteOwnerBatchStage(
        Simulation simulation,
        int ownerStage,
        int workItemCount,
        int ownerIndex,
        int batchOwnerCount)
    {
        var ownerCount = global::System.Math.Min(
            batchOwnerCount,
            workItemCount);
        if (ownerIndex >= ownerCount)
        {
            return;
        }

        ExecuteOwnerStageWorker(
            simulation,
            ownerStage,
            ownerIndex,
            ownerCount);
    }

    private void PrepareSpatialOwners(Simulation simulation)
    {
        PrepareResolvedMovements(simulation.World);
        DetachResolvedMovements(simulation.World);
        PrepareResolvedCellStates(simulation.World);
        PrepareStrategicCellStates(simulation.World);
        PrepareTravelStructures(simulation);
    }

    private void ReconcileSpatialOwners(Simulation simulation)
    {
        AttachResolvedMovements(simulation.World);
        ApplyResolvedMovementConsequences(simulation);
        ReconcileResolvedCellStates(simulation);
        ReconcileStrategicCellStates(simulation);
        ReconcileTravelStructures(simulation);
    }

    private int SpatialOwnerCount()
    {
        var count = global::System.Math.Max(
            _resolvedMovementCount,
            _resolvedCellStateCount);
        count = global::System.Math.Max(
            count,
            _strategicCellStateCount);
        return global::System.Math.Max(
            count,
            _resolvedTravelStructureCount);
    }

    private int PrimaryStateOwnerCount()
    {
        var count = global::System.Math.Max(
            _attributeSetCount,
            _resolvedCultureGroupCount);
        return global::System.Math.Max(
            count,
            _relationshipCount);
    }

    private int SecondaryStateOwnerCount()
    {
        var count = global::System.Math.Max(
            _attributeCount,
            _resolvedCultureReductionGroupCount);
        count = global::System.Math.Max(
            count,
            _influenceCreditCount);
        return global::System.Math.Max(
            count,
            _relationshipSetCount);
    }

    private int FinalStateOwnerCount()
    {
        var count = global::System.Math.Max(
            _resolvedRelationshipCreationCount,
            _tagCount);
        return global::System.Math.Max(
            count,
            _entityStateCount);
    }
}
