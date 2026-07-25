namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class Simulation
{
    internal FixedScalar ModifyCellCultureInfluenceRaw(int cultureId, int planeCode, int q, int r, FixedScalar delta, int tick) =>
        ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, z: 0, delta, tick);

    internal FixedScalar ModifyCellCultureInfluenceRaw(int cultureId, int planeCode, int q, int r, int z, FixedScalar delta, int tick)
    {
        var applied = World.ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, z, delta, tick, _executionContext);
        // Culture locality is a surface model: forged-cell mass at z != 0
        // feeds the culture power ledgers directly and must not perturb
        // surface facts.
        if (applied != 0 && z == 0)
        {
            CultureLocality.MarkDirty(planeCode, q, r);
        }

        return applied;
    }

    internal int ModifyCellCultureInfluenceRawUnits(
        int cultureId,
        int planeCode,
        int q,
        int r,
        int z,
        int deltaUnits,
        int tick)
    {
        var appliedUnits = World.ModifyCellCultureInfluenceRawUnits(
            cultureId,
            planeCode,
            q,
            r,
            z,
            deltaUnits,
            tick,
            _executionContext);
        if (appliedUnits != 0 && z == 0)
        {
            CultureLocality.MarkDirty(planeCode, q, r);
        }

        return appliedUnits;
    }

    internal FixedScalar ModifyCellCultureInfluenceRawFromEntity(int entityId, int planeCode, int q, int r, FixedScalar delta, int tick)
    {
        // Physical-plane deposits land at the entity's current z layer, so
        // entities in tunnels or spires accumulate forged-cell mass.
        var z = planeCode == PlaneCodes.Physical ? World.EntityPhysicalZ(entityId) : 0;

        // Positive production far from the entity's home anchor is split: a
        // remittance share reinforces the anchor cell so roaming entities feed
        // their culture's center instead of painting unbounded trails.
        var remittanceShare = Options.KernelTuning.Programs.CultureField.RemittanceShare;
        if (delta > 0 && remittanceShare > 0)
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

    private FixedScalar ApplyCellCultureInfluenceRawFromEntity(int entityId, int planeCode, int q, int r, int z, FixedScalar delta, int tick)
    {
        var cultureId = World.EntityCultureId(entityId);
        delta = CultureProductionScaled(cultureId, planeCode, delta);
        // Positive physical production takes root as readily as the ground
        // and the culture's taste allow; loss and semantic production are
        // untouched.
        if (delta > 0 && planeCode == PlaneCodes.Physical)
        {
            delta *= CultureGrowthMultiplier(cultureId, q, r);
        }

        var applied = World.ModifyCellCultureInfluenceRawFromEntity(entityId, planeCode, q, r, z, delta, tick, _executionContext);
        if (cultureId > 0 && applied != 0 && z == 0)
        {
            CultureLocality.MarkDirty(planeCode, q, r);
        }

        return applied;
    }

    internal FixedScalar ReduceOtherCultureSourcesAtCell(int ownCultureId, int planeCode, int q, int r, FixedScalar delta, int tick)
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
        if (changedCultureCount > 0)
        {
            CultureLocality.MarkDirty(planeCode, q, r);
        }

        return removed;
    }

    /// <summary>
    /// Positive entity-driven production scales by the culture's per-plane
    /// production multiplier. Decay, cleanup, forge deposits, and player
    /// commands are not production and stay unscaled.
    /// </summary>
    internal FixedScalar CultureProductionScaled(int cultureId, int planeCode, FixedScalar delta) =>
        cultureId > 0 && delta > 0
            ? delta *
                KernelData.Cultures.ProductionMultiplier(cultureId, planeCode) *
                Knowledge.Modifier(cultureId, KnowledgeGrantDomainCodes.CultureProduction, planeCode)
            : delta;

}
