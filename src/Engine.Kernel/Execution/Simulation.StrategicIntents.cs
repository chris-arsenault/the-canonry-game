namespace Engine.Kernel;

using Engine.Constants;
using Engine.Eventing;

internal sealed partial class Simulation
{
    internal int CreateEntity(
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS,
        int tick)
    {
        if (IsCollectingDeltas)
        {
            var entityId = World.ReserveEntitySlot();
            QueueStrategicEntityCreation(
                entityId,
                kindId,
                subtypeId,
                cultureId,
                physicalQ,
                physicalR,
                semanticQ,
                semanticR,
                semanticS);
            return entityId;
        }

        return MaterializeEntity(
            0,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            tick,
            revealOnArrival: true);
    }

    internal void ReserveStrategicEntityBlock(
        global::System.Span<int> entityIds)
    {
        if (!IsCollectingDeltas)
        {
            throw new global::System.InvalidOperationException();
        }

        World.ReserveEntitySlots(entityIds);
    }

    internal int ReserveStrategicEntityId()
    {
        if (!IsCollectingDeltas)
        {
            throw new global::System.InvalidOperationException();
        }

        return World.ReserveEntitySlot();
    }

    internal void QueueStrategicEntityCreation(
        int entityId,
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS)
    {
        if (!IsCollectingDeltas)
        {
            throw new global::System.InvalidOperationException();
        }

        var context = ExecutionContext;
        World.ReserveProvenanceOutput(context);
        var intent = new EntityCreationIntent(
            entityId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            context);
        _continuousOwners.Enqueue(intent);
    }

    private int MaterializeEntity(
        int entityId,
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS,
        int tick,
        bool revealOnArrival)
    {
        AssertExecutionSource();
        var createdEntityId = MaterializeWorldEntity(
            entityId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            tick);
        PublishEntityCreated(
            createdEntityId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            tick);
        KernelData.Entities.InitializeEntity(this, createdEntityId, tick);
        if (revealOnArrival)
        {
            MapFrontier.RevealOnArrival(this, createdEntityId, tick);
        }

        return createdEntityId;
    }

    private int MaterializeWorldEntity(
        int entityId,
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS,
        int tick)
    {
        if (entityId > 0)
        {
            return World.MaterializeReservedEntity(
                entityId,
                kindId,
                subtypeId,
                cultureId,
                physicalQ,
                physicalR,
                semanticQ,
                semanticR,
                semanticS,
                tick,
                ExecutionContext);
        }

        return World.MaterializeEntity(
            entityId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            tick,
            ExecutionContext);
    }

    private void PublishEntityCreated(
        int entityId,
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS,
        int tick)
    {
        var physicalLocality = CultureLocality.CellAt(
            World,
            cultureId,
            PlaneCodes.Physical,
            physicalQ,
            physicalR);
        var semanticLocality = CultureLocality.CellAt(
            World,
            cultureId,
            PlaneCodes.Semantic,
            semanticQ,
            semanticR);
        WriteKernelEvent(KernelEvent.From(new EntityCreatedKernelEvent(
            tick,
            WorldVersion,
            World.EntityOccurrenceHandle(entityId),
            ExecutionContext.SourceCodeId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            World.EntityOccurrenceHandle(entityId),
            physicalLocality.StrengthBandCode,
            physicalLocality.CellKindCode,
            semanticLocality.StrengthBandCode,
            semanticLocality.CellKindCode,
            physicalLocality.LocalRawMass.Units,
            physicalLocality.ConnectedRawMass.Units,
            semanticLocality.LocalRawMass.Units,
            semanticLocality.ConnectedRawMass.Units)));
    }

    internal int IncreaseEntityWorkTier(int entityId, int maxTier)
    {
        if (!IsCollectingDeltas)
        {
            return World.IncreaseEntityWorkTier(entityId, maxTier);
        }

        var occurrence = World.EntityOccurrenceHandleForIntent(entityId);
        var intent = new EntityStateIntent(
            occurrence,
            EntityStateIntentCodes.IncreaseWorkTier,
            maxTier,
            ExecutionContext);
        _continuousOwners.Enqueue(intent);
        return 0;
    }

    internal int DecreaseEntityWorkTier(int entityId)
    {
        if (!IsCollectingDeltas)
        {
            return World.DecreaseEntityWorkTier(entityId);
        }

        var occurrence = World.EntityOccurrenceHandleForIntent(entityId);
        var intent = new EntityStateIntent(
            occurrence,
            EntityStateIntentCodes.DecreaseWorkTier,
            0,
            ExecutionContext);
        _continuousOwners.Enqueue(intent);
        return 0;
    }

    internal void QueueDistrictIntent(in DistrictIntent intent)
    {
        if (!IsCollectingDeltas)
        {
            throw new global::System.InvalidOperationException();
        }

        _continuousOwners.Enqueue(intent);
    }

    internal void ReserveStrategicProvenanceOutputs(int count)
    {
        NumericGuard.ThrowIfNegative(count);
        for (var index = 0; index < count; index++)
        {
            World.ReserveProvenanceOutput(ExecutionContext);
        }
    }

    internal void ReserveStrategicRelationshipBlock(
        global::System.Span<int> relationshipIds)
    {
        if (!IsCollectingDeltas)
        {
            throw new global::System.InvalidOperationException();
        }

        World.ReserveRelationshipSlots(relationshipIds);
    }

    internal int ReserveStrategicRelationshipId()
    {
        if (!IsCollectingDeltas)
        {
            throw new global::System.InvalidOperationException();
        }

        return World.ReserveRelationshipSlot();
    }

    internal void QueueStrategicRelationshipCreation(
        int relationshipId,
        int relationshipKindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        FixedScalar strength)
    {
        if (!IsCollectingDeltas)
        {
            throw new global::System.InvalidOperationException();
        }

        var context = ExecutionContext;
        World.ReserveProvenanceOutput(context);
        var intent = new RelationshipCreationIntent(
            relationshipId,
            relationshipKindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            KernelFixedPoint.RelationshipStrengthUnits(strength),
            context);
        _continuousOwners.Enqueue(intent);
    }

    internal int CreateRelationship(
        int relationshipKindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        FixedScalar strength,
        int tick)
    {
        if (IsCollectingDeltas)
        {
            var queuedRelationshipId = World.ReserveRelationshipSlot();
            QueueStrategicRelationshipCreation(
                queuedRelationshipId,
                relationshipKindId,
                polarityCode,
                sourceEntityId,
                targetEntityId,
                strength);
            return queuedRelationshipId;
        }

        var relationshipId = MaterializeRelationship(
            relationshipKindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            tick);
        NoteCreatedRelationship(
            relationshipId,
            relationshipKindId,
            sourceEntityId,
            targetEntityId);
        return relationshipId;
    }

    private int MaterializeRelationship(
        int relationshipKindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        FixedScalar strength,
        int tick)
    {
        return World.CreateRelationship(
            relationshipKindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            tick,
            ExecutionContext);
    }

    private void NoteCreatedRelationship(
        int relationshipId,
        int relationshipKindId,
        int sourceEntityId,
        int targetEntityId)
    {
        if (relationshipId <= 0)
        {
            return;
        }

        Relationships.NoteRelationshipRecordCreated(relationshipId);
        RefreshFollowProjectionDirect(targetEntityId);
        if (Relationships.KindIsCultureAnchor(relationshipKindId))
        {
            World.SetEntityCultureAnchorToEntity(sourceEntityId, targetEntityId);
        }

        QueueWorldBeatRelationshipCreated(relationshipKindId);
    }

    internal void PrepareStrategicEntity(
        in EntityCreationIntent intent,
        out int physicalCellId,
        out int semanticCellId)
    {
        World.PrepareReservedEntityCreation(
            intent.EntityId,
            intent.KindId,
            intent.SubtypeId,
            intent.CultureId,
            intent.PhysicalQ,
            intent.PhysicalR,
            intent.SemanticQ,
            intent.SemanticR,
            out var entityId,
            out physicalCellId,
            out semanticCellId);
        if (entityId != intent.EntityId)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    internal void ApplyStrategicEntityOwned(
        in EntityCreationIntent intent)
    {
        using var scope = EnterExecutionContext(intent.Context);
        World.ApplyReservedEntityCreationOwned(
            intent.EntityId,
            intent.KindId,
            intent.SubtypeId,
            intent.CultureId,
            intent.PhysicalQ,
            intent.PhysicalR,
            intent.SemanticQ,
            intent.SemanticR,
            intent.SemanticS,
            ExecutionTick,
            intent.Context,
            provenanceOutputReserved: true);
        PublishEntityCreated(
            intent.EntityId,
            intent.KindId,
            intent.SubtypeId,
            intent.CultureId,
            intent.PhysicalQ,
            intent.PhysicalR,
            intent.SemanticQ,
            intent.SemanticR,
            intent.SemanticS,
            ExecutionTick);
        KernelData.Entities.InitializeEntity(
            this,
            intent.EntityId,
            ExecutionTick);
    }

    internal void ReconcileStrategicEntity(
        in EntityCreationIntent intent,
        int physicalCellId,
        int semanticCellId)
    {
        World.ReconcileReservedEntityCreation(
            intent.EntityId,
            intent.KindId,
            intent.CultureId,
            physicalCellId,
            semanticCellId);
        using var scope = EnterExecutionContext(intent.Context);
        MapFrontier.RevealOnArrival(
            this,
            intent.EntityId,
            ExecutionTick);
    }

    internal bool TryPrepareStrategicRelationship(
        in RelationshipCreationIntent intent)
    {
        var prepared = World.TryPrepareReservedRelationshipCreation(
            intent.RelationshipId,
            intent.KindId,
            intent.SourceEntityId,
            intent.TargetEntityId,
            intent.Context,
            out var relationshipId,
            out _);
        if (prepared && relationshipId != intent.RelationshipId)
        {
            throw new global::System.InvalidOperationException();
        }

        return prepared;
    }

    internal void ApplyStrategicRelationshipOwned(
        in RelationshipCreationIntent intent)
    {
        using var scope = EnterExecutionContext(intent.Context);
        World.ApplyReservedRelationshipCreationOwned(
            intent.RelationshipId,
            intent.KindId,
            intent.PolarityCode,
            intent.SourceEntityId,
            intent.TargetEntityId,
            KernelFixedPoint.RelationshipStrengthScalar(intent.StrengthUnits),
            ExecutionTick,
            intent.Context,
            provenanceOutputReserved: true);
        Relationships.NoteRelationshipRecordCreated(
            intent.RelationshipId);
    }

    internal void ReconcileStrategicRelationship(
        in RelationshipCreationIntent intent)
    {
        var relationshipId = intent.RelationshipId;
        World.ReconcileReservedRelationshipCreation(relationshipId);
        if (Relationships.KindIsCultureAnchor(intent.KindId))
        {
            World.SetEntityCultureAnchorToEntity(
                intent.SourceEntityId,
                intent.TargetEntityId);
        }

        QueueWorldBeatRelationshipCreated(intent.KindId);
    }

    private void RefreshFollowProjectionDirect(int targetEntityId)
    {
        Relationships.ResolveFollowProjection(
            World,
            targetEntityId,
            out var relationshipId,
            out var sourceEntityId);
        Relationships.ApplyFollowProjection(
            targetEntityId,
            relationshipId,
            sourceEntityId);
    }
}
