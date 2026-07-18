namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class Simulation
{
    internal float ModifyCellCultureInfluenceRaw(int cultureId, int planeCode, int q, int r, float delta, int tick) =>
        ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, z: 0, delta, tick);

    internal float ModifyCellCultureInfluenceRaw(int cultureId, int planeCode, int q, int r, int z, float delta, int tick)
    {
        var previousRaw = World.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r, z);
        var applied = World.ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, z, delta, tick, _executionContext);
        // The boundary/component model is a surface model: forged-cell mass at
        // z != 0 feeds the culture power ledgers directly and must not perturb
        // surface component bookkeeping.
        if (applied != 0f && z == 0)
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
        // Physical-plane deposits land at the entity's current z layer, so
        // entities in tunnels or spires accumulate forged-cell mass.
        var z = planeCode == PlaneCodes.Physical ? World.EntityPhysicalZ(entityId) : 0;

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
                return ApplyCellCultureInfluenceRawFromEntity(entityId, planeCode, q, r, z, delta - remitted, tick) +
                    ApplyCellCultureInfluenceRawFromEntity(entityId, planeCode, anchorQ, anchorR, z: 0, remitted, tick);
            }
        }

        return ApplyCellCultureInfluenceRawFromEntity(entityId, planeCode, q, r, z, delta, tick);
    }

    private float ApplyCellCultureInfluenceRawFromEntity(int entityId, int planeCode, int q, int r, int z, float delta, int tick)
    {
        var cultureId = World.EntityCultureId(entityId);
        delta = CultureProductionScaled(cultureId, planeCode, delta);
        var previousRaw = cultureId > 0
            ? World.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r, z)
            : 0f;
        var applied = World.ModifyCellCultureInfluenceRawFromEntity(entityId, planeCode, q, r, z, delta, tick, _executionContext);
        if (cultureId > 0 && applied != 0f && z == 0)
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

    /// <summary>
    /// Positive entity-driven production scales by the culture's per-plane
    /// production multiplier. Decay, cleanup, forge deposits, and player
    /// commands are not production and stay unscaled.
    /// </summary>
    internal float CultureProductionScaled(int cultureId, int planeCode, float delta) =>
        cultureId > 0 && delta > 0f
            ? delta *
                KernelData.Cultures.ProductionMultiplier(cultureId, planeCode) *
                Knowledge.Modifier(cultureId, KnowledgeGrantDomainCodes.CultureProduction, planeCode)
            : delta;

    private static bool CultureBoundarySupportChanged(float previousRaw, float currentRaw) =>
        (previousRaw >= WorldState.CultureClaimedRawMass) != (currentRaw >= WorldState.CultureClaimedRawMass);

    private static bool CultureBoundaryCleanupMayHaveChangedSupport(float currentRaw, float requestedDelta) =>
        currentRaw < WorldState.CultureClaimedRawMass &&
        currentRaw + requestedDelta >= WorldState.CultureClaimedRawMass;
}
