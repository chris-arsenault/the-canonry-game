namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class WorldState
{
    public int CreateRelationship(
        int kindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        FixedScalar strength,
        int tick)
    {
        return CreateRelationship(
            kindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            tick,
            new KernelExecutionContext(0, _defaultSourceCodeId));
    }

    public int CreateRelationship(
        int kindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        FixedScalar strength,
        int tick,
        in KernelExecutionContext context)
    {
        return MaterializeRelationship(
            relationshipId: 0,
            kindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            tick,
            context,
            provenanceOutputReserved: false);
    }

    internal int MaterializeReservedRelationship(
        int relationshipId,
        int kindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        FixedScalar strength,
        int tick,
        in KernelExecutionContext context)
    {
        NumericGuard.ThrowIfNegativeOrZero(relationshipId);
        return MaterializeRelationship(
            relationshipId,
            kindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            tick,
            context,
            provenanceOutputReserved: true);
    }

    private int MaterializeRelationship(
        int relationshipId,
        int kindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        FixedScalar strength,
        int tick,
        in KernelExecutionContext context,
        bool provenanceOutputReserved)
    {
        if (!TryPrepareReservedRelationshipCreation(
                relationshipId,
                kindId,
                sourceEntityId,
                targetEntityId,
                context,
                out relationshipId,
                out var existingRelationshipId))
        {
            return existingRelationshipId;
        }

        ApplyReservedRelationshipCreationOwned(
            relationshipId,
            kindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            tick,
            context,
            provenanceOutputReserved);
        ReconcileReservedRelationshipCreation(relationshipId);
        return relationshipId;
    }

    internal bool TryPrepareReservedRelationshipCreation(
        int requestedRelationshipId,
        int kindId,
        int sourceEntityId,
        int targetEntityId,
        in KernelExecutionContext context,
        out int relationshipId,
        out int existingRelationshipId)
    {
        ValidatePositive(context.SourceCodeId);
        ValidatePositive(kindId);
        EnsureEntity(sourceEntityId);
        EnsureEntity(targetEntityId);
        if (TryUseExistingRelationship(
                requestedRelationshipId,
                sourceEntityId,
                targetEntityId,
                kindId,
                out existingRelationshipId))
        {
            relationshipId = 0;
            return false;
        }

        relationshipId = requestedRelationshipId == 0
            ? FindRelationshipSlot()
            : requestedRelationshipId;
        if ((uint)relationshipId > (uint)RelationshipCapacity ||
            _relationshipAllocated[relationshipId])
        {
            throw new global::System.InvalidOperationException();
        }

        existingRelationshipId = 0;
        return true;
    }

    internal void ApplyReservedRelationshipCreationOwned(
        int relationshipId,
        int kindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        FixedScalar strength,
        int tick,
        in KernelExecutionContext context,
        bool provenanceOutputReserved)
    {
        _relationshipAllocated[relationshipId] = true;
        _relationshipActive[relationshipId] = true;
        _relationshipKindId[relationshipId] = kindId;
        _relationshipPolarityCode[relationshipId] = polarityCode;
        _relationshipSourceId[relationshipId] = sourceEntityId;
        _relationshipTargetId[relationshipId] = targetEntityId;
        _relationshipStrength[relationshipId] =
            KernelFixedPoint.RelationshipStrengthUnits(Clamp01(strength));
        _relationshipStartTick[relationshipId] = tick;
        _relationshipEndTick[relationshipId] = 0;
        MarkEntityCultureWorkDirty(sourceEntityId);
        MarkEntityCultureWorkDirty(targetEntityId);
        AdvanceWorldVersion();
        if (provenanceOutputReserved)
        {
            RecordReservedProvenanceRelationshipCreated(
                relationshipId,
                context);
        }
        else
        {
            RecordProvenanceRelationshipCreated(relationshipId, context);
        }
        WriteRelationshipChanged(
            relationshipId,
            tick,
            _relationshipCreatedCodeId,
            context,
            0,
            _relationshipStrength[relationshipId]);
    }

    internal void ReconcileReservedRelationshipCreation(
        int relationshipId)
    {
        InsertActiveRelationship(relationshipId);
        _relationshipEndpoints.Attach(this, relationshipId);
    }

    private bool TryUseExistingRelationship(
        int reservedRelationshipId,
        int sourceEntityId,
        int targetEntityId,
        int kindId,
        out int existingRelationshipId)
    {
        existingRelationshipId = FindRelationship(
            sourceEntityId,
            targetEntityId,
            kindId);
        if (existingRelationshipId <= 0)
        {
            return false;
        }

        if (reservedRelationshipId > 0)
        {
            ReleaseUnusedReservedRelationshipSlot(reservedRelationshipId);
        }

        return true;
    }
}
