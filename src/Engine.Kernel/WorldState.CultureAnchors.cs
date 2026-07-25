namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class WorldState
{
    internal int EntityCultureAnchorQ(int entityId, int planeCode)
    {
        EnsureEntity(entityId);
        return planeCode == PlaneCodes.Semantic
            ? _entityAnchorSemanticQ[entityId]
            : _entityAnchorPhysicalQ[entityId];
    }

    internal int EntityCultureAnchorR(int entityId, int planeCode)
    {
        EnsureEntity(entityId);
        return planeCode == PlaneCodes.Semantic
            ? _entityAnchorSemanticR[entityId]
            : _entityAnchorPhysicalR[entityId];
    }

    internal void SetEntityCultureAnchorToEntity(int entityId, int anchorEntityId)
    {
        EnsureEntity(entityId);
        EnsureEntity(anchorEntityId);
        _entityAnchorPhysicalQ[entityId] = _entityPhysicalQ[anchorEntityId];
        _entityAnchorPhysicalR[entityId] = _entityPhysicalR[anchorEntityId];
        _entityAnchorSemanticQ[entityId] = _entitySemanticQ[anchorEntityId];
        _entityAnchorSemanticR[entityId] = _entitySemanticR[anchorEntityId];
    }

    internal void SetEntityFavor(int entityId, FixedScalar multiplierDelta, int expiresAtTick)
    {
        EnsureEntity(entityId);
        NumericGuard.ThrowIfNegative(multiplierDelta);
        NumericGuard.ThrowIfNegative(expiresAtTick);
        _entityFavorMultiplierDelta[entityId] = KernelFixedPoint.ModifierUnits(multiplierDelta);
        _entityFavorExpiresAtTick[entityId] = expiresAtTick;
        AdvanceWorldVersion();
    }

    internal FixedScalar EntityFavorMultiplierDelta(int entityId)
    {
        EnsureEntity(entityId);
        return CurrentTick < _entityFavorExpiresAtTick[entityId]
            ? KernelFixedPoint.ModifierScalar(_entityFavorMultiplierDelta[entityId])
            : 0;
    }
}
