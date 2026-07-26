namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class WorldState
{
    internal void DetachEntityForResolvedMove(
        int entityId,
        int planeCode,
        int q,
        int r,
        int z)
    {
        EnsureEntity(entityId);
        var cellId = _cells.Require(planeCode, q, r, z);
        if (planeCode == PlaneCodes.Physical)
        {
            _entitySpatialMembership.DetachPhysical(entityId, cellId);
            return;
        }

        _entitySpatialMembership.DetachSemantic(entityId, cellId);
    }

    internal void AttachEntityForResolvedMove(
        int entityId,
        int planeCode,
        int q,
        int r,
        int z)
    {
        EnsureEntity(entityId);
        var cellId = _cells.Require(planeCode, q, r, z);
        if (planeCode == PlaneCodes.Physical)
        {
            _entitySpatialMembership.AttachPhysical(entityId, cellId);
            return;
        }

        _entitySpatialMembership.AttachSemantic(entityId, cellId);
    }

    internal void ApplyEntityResolvedMove(
        int entityId,
        int planeCode,
        int fromQ,
        int fromR,
        int fromZ,
        int q,
        int r,
        int z,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        EnsureEntity(entityId);
        var fromS = planeCode == PlaneCodes.Semantic ? -fromQ - fromR : 0;
        var toS = planeCode == PlaneCodes.Semantic ? -q - r : 0;
        if (planeCode == PlaneCodes.Physical)
        {
            _entityPhysicalQ[entityId] = q;
            _entityPhysicalR[entityId] = r;
            _entityPhysicalZ[entityId] = z;
        }
        else
        {
            _entitySemanticQ[entityId] = q;
            _entitySemanticR[entityId] = r;
            _entitySemanticS[entityId] = toS;
        }

        _entityUpdatedAt[entityId] = tick;
        MarkEntityCultureWorkDirty(entityId);
        AdvanceWorldVersion();
        Write(KernelEvent.From(new EntityMovedKernelEvent(
            tick,
            WorldVersion,
            EntityOccurrenceHandle(entityId),
            context.SourceCodeId,
            planeCode,
            fromQ,
            fromR,
            fromS,
            q,
            r,
            toS,
            fromZ,
            z)),
            context);
    }

    public void MoveEntityPhysical(int entityId, int q, int r, int tick) =>
        MoveEntityPhysical(entityId, q, r, tick, new KernelExecutionContext(0, _defaultSourceCodeId));

    public void MoveEntityPhysical(int entityId, int q, int r, int tick, in KernelExecutionContext context) =>
        MoveEntityPhysical(entityId, q, r, _entityPhysicalZ[entityId], tick, context);

    public void MoveEntityPhysical(int entityId, int q, int r, int z, int tick, in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        EnsureEntity(entityId);
        var fromQ = _entityPhysicalQ[entityId];
        var fromR = _entityPhysicalR[entityId];
        var fromZ = _entityPhysicalZ[entityId];
        if (fromQ == q && fromR == r && fromZ == z)
        {
            return;
        }

        var fromCellId = _cells.Require(PlaneCodes.Physical, fromQ, fromR, fromZ);
        var toCellId = _cells.Require(PlaneCodes.Physical, q, r, z);
        _entityPhysicalQ[entityId] = q;
        _entityPhysicalR[entityId] = r;
        _entityPhysicalZ[entityId] = z;
        _entitySpatialMembership.MovePhysical(entityId, fromCellId, toCellId);
        _entityUpdatedAt[entityId] = tick;
        MarkEntityCultureWorkDirty(entityId);
        AdvanceWorldVersion();
        Write(KernelEvent.From(new EntityMovedKernelEvent(
            tick,
            WorldVersion,
            EntityOccurrenceHandle(entityId),
            context.SourceCodeId,
            PlaneCodes.Physical,
            fromQ,
            fromR,
            0,
            q,
            r,
            0,
            fromZ,
            z)),
            context);
    }

    public void MoveEntitySemantic(int entityId, int q, int r, int s, int tick) =>
        MoveEntitySemantic(entityId, q, r, s, tick, new KernelExecutionContext(0, _defaultSourceCodeId));

    public void MoveEntitySemantic(int entityId, int q, int r, int s, int tick, in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        EnsureEntity(entityId);
        var fromQ = _entitySemanticQ[entityId];
        var fromR = _entitySemanticR[entityId];
        var fromS = _entitySemanticS[entityId];
        if (fromQ == q && fromR == r && fromS == s)
        {
            return;
        }

        var fromCellId = _cells.Require(PlaneCodes.Semantic, fromQ, fromR, z: 0);
        var toCellId = _cells.Require(PlaneCodes.Semantic, q, r, z: 0);
        _entitySemanticQ[entityId] = q;
        _entitySemanticR[entityId] = r;
        _entitySemanticS[entityId] = s;
        _entitySpatialMembership.MoveSemantic(entityId, fromCellId, toCellId);
        _entityUpdatedAt[entityId] = tick;
        MarkEntityCultureWorkDirty(entityId);
        AdvanceWorldVersion();
        Write(KernelEvent.From(new EntityMovedKernelEvent(
            tick,
            WorldVersion,
            EntityOccurrenceHandle(entityId),
            context.SourceCodeId,
            PlaneCodes.Semantic,
            fromQ,
            fromR,
            fromS,
            q,
            r,
            s,
            0,
            0)),
            context);
    }

    public int EntityPhysicalQ(int entityId)
    {
        EnsureEntity(entityId);
        return _entityPhysicalQ[entityId];
    }

    public int EntityPhysicalR(int entityId)
    {
        EnsureEntity(entityId);
        return _entityPhysicalR[entityId];
    }

    public int EntityPhysicalZ(int entityId)
    {
        EnsureEntity(entityId);
        return _entityPhysicalZ[entityId];
    }

    public int EntitySemanticQ(int entityId)
    {
        EnsureEntity(entityId);
        return _entitySemanticQ[entityId];
    }

    public int EntitySemanticR(int entityId)
    {
        EnsureEntity(entityId);
        return _entitySemanticR[entityId];
    }

    public int EntitySemanticS(int entityId)
    {
        EnsureEntity(entityId);
        return _entitySemanticS[entityId];
    }
}
