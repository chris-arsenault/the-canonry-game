namespace Engine.Kernel;

using Engine.Constants;
using Engine.Eventing;

internal sealed partial class WorldState
{
    public void SetEntityStatus(
        int entityId,
        int statusCode,
        int tick) =>
        SetEntityStatus(
            entityId,
            statusCode,
            tick,
            new KernelExecutionContext(
                0,
                _defaultSourceCodeId));

    public void SetEntityStatus(
        int entityId,
        int statusCode,
        int tick,
        in KernelExecutionContext context)
    {
        if (!BeginEntityRetirementOwned(
                entityId,
                statusCode,
                tick,
                context))
        {
            return;
        }

        ReconcileEntityRetirement(entityId);
        FinalizeEntityRetirement(entityId);
    }

    internal bool BeginEntityRetirementOwned(
        int entityId,
        int statusCode,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        EnsureEntity(entityId);
        ValidatePositive(statusCode);
        var previous = _entityStatus[entityId];
        if (previous == statusCode)
        {
            return false;
        }

        if (previous != EntityStatusCodes.Active ||
            statusCode != EntityStatusCodes.Historical)
        {
            throw new global::System.InvalidOperationException();
        }

        _entityStatus[entityId] = statusCode;
        _entityUpdatedAt[entityId] = tick;
        MarkCultureWorkDirty(_entityCultureId[entityId]);
        AdvanceWorldVersion();
        Write(KernelEvent.From(
            new EntityStatusChangedKernelEvent(
                tick,
                WorldVersion,
                EntityOccurrenceHandle(entityId),
                context.SourceCodeId,
                _entityKindId[entityId],
                _entitySubtypeId[entityId],
                previous,
                statusCode)),
            context);
        return true;
    }

    internal void ReconcileEntityRetirement(int entityId)
    {
        EnsureEntity(entityId);
        if (_entityStatus[entityId] !=
            EntityStatusCodes.Historical)
        {
            throw new global::System.InvalidOperationException();
        }

        RemoveActiveEntity(entityId);
        RemoveEntityKindIndex(entityId, _entityKindId[entityId]);
        RemoveEntityCultureIndex(
            entityId,
            _entityCultureId[entityId]);
        _entitySpatialMembership.DetachPhysical(
            entityId,
            _cells.Require(
                PlaneCodes.Physical,
                _entityPhysicalQ[entityId],
                _entityPhysicalR[entityId],
                _entityPhysicalZ[entityId]));
        _entitySpatialMembership.DetachSemantic(
            entityId,
            _cells.Require(
                PlaneCodes.Semantic,
                _entitySemanticQ[entityId],
                _entitySemanticR[entityId],
                z: 0));
    }

    internal void FinalizeEntityRetirement(int entityId)
    {
        EnsureEntity(entityId);
        if (_entityStatus[entityId] !=
            EntityStatusCodes.Historical)
        {
            throw new global::System.InvalidOperationException();
        }

        ReleaseEntitySlot(entityId);
    }

    public void SetEntityCulture(
        int entityId,
        int cultureId,
        int tick) =>
        SetEntityCulture(
            entityId,
            cultureId,
            tick,
            new KernelExecutionContext(
                0,
                _defaultSourceCodeId));

    public void SetEntityCulture(
        int entityId,
        int cultureId,
        int tick,
        in KernelExecutionContext context)
    {
        if (!ApplyEntityCultureOwned(
                entityId,
                cultureId,
                tick,
                context,
                out var previous))
        {
            return;
        }

        ReconcileEntityCulture(entityId, previous, cultureId);
    }

    internal bool ApplyEntityCultureOwned(
        int entityId,
        int cultureId,
        int tick,
        in KernelExecutionContext context,
        out int previous)
    {
        ValidatePositive(context.SourceCodeId);
        EnsureEntity(entityId);
        EnsureCultureIndex(cultureId);
        previous = _entityCultureId[entityId];
        if (previous == cultureId)
        {
            return false;
        }

        _entityCultureId[entityId] = cultureId;
        _entityUpdatedAt[entityId] = tick;
        MarkCultureWorkDirty(previous);
        MarkCultureWorkDirty(cultureId);
        AdvanceWorldVersion();
        Write(KernelEvent.From(
            new EntityCultureChangedKernelEvent(
                tick,
                WorldVersion,
                EntityOccurrenceHandle(entityId),
                context.SourceCodeId,
                _entityKindId[entityId],
                _entitySubtypeId[entityId],
                previous,
                cultureId)),
            context);
        return true;
    }

    internal void ReconcileEntityCulture(
        int entityId,
        int previousCultureId,
        int cultureId)
    {
        EnsureEntity(entityId);
        EnsureCultureIndex(previousCultureId);
        EnsureCultureIndex(cultureId);
        _cultureHighWatermark =
            global::System.Math.Max(
                _cultureHighWatermark,
                cultureId);
        if (!EntityIsActiveRaw(entityId))
        {
            return;
        }

        RemoveEntityCultureIndex(entityId, previousCultureId);
        InsertEntityCultureIndex(entityId, cultureId);
    }

    public void CullRelationship(
        int relationshipId,
        int tick) =>
        CullRelationship(
            relationshipId,
            tick,
            new KernelExecutionContext(
                0,
                _defaultSourceCodeId));

    public void CullRelationship(
        int relationshipId,
        int tick,
        in KernelExecutionContext context)
    {
        if (!BeginRelationshipRetirementOwned(
                relationshipId,
                tick,
                context))
        {
            return;
        }

        ReconcileRelationshipRetirement(relationshipId);
        FinalizeRelationshipRetirement(relationshipId);
    }

    internal bool BeginRelationshipRetirementOwned(
        int relationshipId,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        EnsureRelationship(relationshipId);
        if (!_relationshipActive[relationshipId])
        {
            return false;
        }

        var previous = _relationshipStrength[relationshipId];
        _relationshipActive[relationshipId] = false;
        _relationshipEndTick[relationshipId] = tick;
        MarkEntityCultureWorkDirty(
            _relationshipSourceId[relationshipId]);
        MarkEntityCultureWorkDirty(
            _relationshipTargetId[relationshipId]);
        AdvanceWorldVersion();
        if (context.ProvenanceInvocationId > 0)
        {
            _relationshipLastChangedByInvocationId[
                relationshipId] =
                context.ProvenanceInvocationId;
        }

        WriteRelationshipChanged(
            relationshipId,
            tick,
            _relationshipCulledCodeId,
            context,
            previous,
            0);
        return true;
    }

    internal void ReconcileRelationshipRetirement(
        int relationshipId)
    {
        EnsureRelationship(relationshipId);
        if (_relationshipActive[relationshipId])
        {
            throw new global::System.InvalidOperationException();
        }

        RemoveActiveRelationship(relationshipId);
        _relationshipEndpoints.Detach(this, relationshipId);
    }

    internal void FinalizeRelationshipRetirement(
        int relationshipId)
    {
        EnsureRelationship(relationshipId);
        if (_relationshipActive[relationshipId])
        {
            throw new global::System.InvalidOperationException();
        }

        ReleaseRelationshipSlot(relationshipId);
    }
}
