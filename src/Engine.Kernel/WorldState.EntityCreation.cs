namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class WorldState
{
    public int MaterializeEntity(
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
        in KernelExecutionContext context)
    {
        return MaterializeEntityCore(
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
            context,
            provenanceOutputReserved: false);
    }

    internal int MaterializeReservedEntity(
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
        in KernelExecutionContext context)
    {
        return MaterializeEntityCore(
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
            context,
            provenanceOutputReserved: true);
    }

    private int MaterializeEntityCore(
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
        in KernelExecutionContext context,
        bool provenanceOutputReserved)
    {
        PrepareReservedEntityCreation(
            entityId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            out entityId,
            out var physicalCellId,
            out var semanticCellId);
        ApplyReservedEntityCreationOwned(
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
            context,
            provenanceOutputReserved);
        ReconcileReservedEntityCreation(
            entityId,
            kindId,
            cultureId,
            physicalCellId,
            semanticCellId);
        return entityId;
    }

    internal void PrepareReservedEntityCreation(
        int requestedEntityId,
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        out int entityId,
        out int physicalCellId,
        out int semanticCellId)
    {
        ValidateRelationshipKindIndex(kindId);
        ValidatePositive(subtypeId);
        EnsureCultureIndex(cultureId);
        entityId = requestedEntityId;
        if (entityId == 0)
        {
            entityId = FindEntitySlot();
        }
        else if ((uint)entityId >= (uint)_entityAllocated.Length || _entityAllocated[entityId])
        {
            throw new global::System.InvalidOperationException();
        }

        _cultureHighWatermark = global::System.Math.Max(
            _cultureHighWatermark,
            cultureId);
        _entityHighWatermark = global::System.Math.Max(
            _entityHighWatermark,
            entityId);
        physicalCellId = _cells.GetOrAdd(
            PlaneCodes.Physical,
            physicalQ,
            physicalR,
            z: 0);
        semanticCellId = _cells.GetOrAdd(
            PlaneCodes.Semantic,
            semanticQ,
            semanticR,
            z: 0);
    }

    internal void ApplyReservedEntityCreationOwned(
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
        in KernelExecutionContext context,
        bool provenanceOutputReserved)
    {
        lock (_slotOwnerGates[SlotOwner(entityId)])
        {
            _entityAllocated[entityId] = true;
            _entityReserved[entityId] = false;
        }
        _entityStatus[entityId] = EntityStatusCodes.Active;
        _entityKindId[entityId] = kindId;
        _entitySubtypeId[entityId] = subtypeId;
        _entityCultureId[entityId] = cultureId;
        _entityPhysicalQ[entityId] = physicalQ;
        _entityPhysicalR[entityId] = physicalR;
        _entityPhysicalZ[entityId] = 0;
        _entitySemanticQ[entityId] = semanticQ;
        _entitySemanticR[entityId] = semanticR;
        _entitySemanticS[entityId] = semanticS;
        _entityAnchorPhysicalQ[entityId] = physicalQ;
        _entityAnchorPhysicalR[entityId] = physicalR;
        _entityAnchorSemanticQ[entityId] = semanticQ;
        _entityAnchorSemanticR[entityId] = semanticR;
        _entityWorkTiers[entityId] = 0;
        _entityCreatedAt[entityId] = tick;
        _entityUpdatedAt[entityId] = tick;
        MarkCultureWorkDirty(cultureId);
        AdvanceWorldVersion();
        if (provenanceOutputReserved)
        {
            RecordReservedProvenanceEntityCreated(entityId, context);
        }
        else
        {
            RecordProvenanceEntityCreated(entityId, kindId, context);
        }

    }

    internal void ReconcileReservedEntityCreation(
        int entityId,
        int kindId,
        int cultureId,
        int physicalCellId,
        int semanticCellId)
    {
        InsertEntityKindIndex(entityId, kindId);
        InsertEntityCultureIndex(entityId, cultureId);
        InsertActiveEntity(entityId);
        _entitySpatialMembership.AttachPhysical(entityId, physicalCellId);
        _entitySpatialMembership.AttachSemantic(entityId, semanticCellId);
    }
}
