namespace Engine.Kernel;

internal sealed partial class Simulation
{
    internal float ModifyCellCultureInfluenceRaw(int cultureId, int planeCode, int q, int r, float delta, int tick)
    {
        var previousRaw = World.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
        var applied = World.ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, delta, tick, _executionContext);
        if (applied != 0f)
        {
            CultureBoundaries.MarkDirty(
                cultureId,
                planeCode,
                q,
                r,
                previousRaw,
                previousRaw + applied,
                CultureBoundarySupportChanged(previousRaw, previousRaw + applied));
        }

        return applied;
    }

    internal float ModifyCellCultureInfluenceRawFromEntity(int entityId, int planeCode, int q, int r, float delta, int tick)
    {
        // Positive production far from the entity's home anchor is split: a
        // remittance share reinforces the anchor cell so roaming entities feed
        // their culture's center instead of painting unbounded trails.
        var remittanceShare = Options.KernelTuning.Programs.CultureField.RemittanceShare;
        if (delta > 0f && remittanceShare > 0f)
        {
            var anchorQ = World.EntityCultureAnchorQ(entityId, planeCode);
            var anchorR = World.EntityCultureAnchorR(entityId, planeCode);
            if (KernelQuery.HexDistance(q, r, anchorQ, anchorR) >
                Options.KernelTuning.Programs.CultureField.RemittanceMinimumDistance)
            {
                var remitted = delta * remittanceShare;
                return ApplyCellCultureInfluenceRawFromEntity(entityId, planeCode, q, r, delta - remitted, tick) +
                    ApplyCellCultureInfluenceRawFromEntity(entityId, planeCode, anchorQ, anchorR, remitted, tick);
            }
        }

        return ApplyCellCultureInfluenceRawFromEntity(entityId, planeCode, q, r, delta, tick);
    }

    private float ApplyCellCultureInfluenceRawFromEntity(int entityId, int planeCode, int q, int r, float delta, int tick)
    {
        var cultureId = World.EntityCultureId(entityId);
        var previousRaw = cultureId > 0
            ? World.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r)
            : 0f;
        var applied = World.ModifyCellCultureInfluenceRawFromEntity(entityId, planeCode, q, r, delta, tick, _executionContext);
        if (cultureId > 0 && applied != 0f)
        {
            CultureBoundaries.MarkDirty(
                cultureId,
                planeCode,
                q,
                r,
                previousRaw,
                previousRaw + applied,
                CultureBoundarySupportChanged(previousRaw, previousRaw + applied));
        }

        return applied;
    }

    internal float ReduceOtherCultureSourcesAtCell(int ownCultureId, int planeCode, int q, int r, float delta, int tick)
    {
        var removed = World.ReduceOtherCultureSourcesAtCell(
            ownCultureId,
            planeCode,
            q,
            r,
            delta,
            tick,
            _executionContext,
            _dirtyCultureScratch,
            out var changedCultureCount);
        for (var i = 0; i < changedCultureCount; i++)
        {
            var cultureId = _dirtyCultureScratch[i];
            var currentRaw = World.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
            CultureBoundaries.MarkDirty(
                cultureId,
                planeCode,
                q,
                r,
                currentRaw + delta,
                currentRaw,
                CultureBoundaryCleanupMayHaveChangedSupport(currentRaw, delta));
        }

        return removed;
    }

    private static bool CultureBoundarySupportChanged(float previousRaw, float currentRaw) =>
        (previousRaw >= WorldState.CultureClaimedRawMass) != (currentRaw >= WorldState.CultureClaimedRawMass);

    private static bool CultureBoundaryCleanupMayHaveChangedSupport(float currentRaw, float requestedDelta) =>
        currentRaw < WorldState.CultureClaimedRawMass &&
        currentRaw + requestedDelta >= WorldState.CultureClaimedRawMass;
}
