namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MapFrontierProgramRuntime
{
    /// <summary>
    /// The Long Dark's reach follows the retreating frontier. It rotates over
    /// only cells opened by map growth, so startup geography is never silently
    /// rewritten. Weakly-held edge cells with no road receive transient ice
    /// cover; their durable terrain remains intact underneath.
    /// </summary>
    private void ReclaimRim(Simulation simulation, int tick)
    {
        var world = simulation.World;
        var count = CollectReclaimCandidates(simulation, world);
        for (var i = 0; i < count; i++)
        {
            simulation.SetTerrainCover(_candidateQ[i], _candidateR[i], Data.FrozenTerrainId, tick);
            State.TotalReclaimed++;
        }
    }

    private int CollectReclaimCandidates(Simulation simulation, WorldState world)
    {
        var budget = global::System.Math.Min(Data.RefreezeBudgetPerPulse, _candidateQ.Length);
        var count = 0;
        var visited = 0;
        while (visited < State.ThawedCount && count < budget)
        {
            if (State.ReclaimCursor >= State.ThawedCount)
            {
                State.ReclaimCursor = 0;
            }

            var q = State.ThawedQ[State.ReclaimCursor];
            var r = State.ThawedR[State.ReclaimCursor];
            State.ReclaimCursor++;
            visited++;
            if (ShouldReclaim(simulation, world, q, r))
            {
                _candidateQ[count] = q;
                _candidateR[count] = r;
                count++;
            }
        }

        return count;
    }

    private bool ShouldReclaim(Simulation simulation, WorldState world, int q, int r)
    {
        if (!world.CellIsKnown(PlaneCodes.Physical, q, r) ||
            world.CellTerrainCoverId(PlaneCodes.Physical, q, r) != 0 ||
            simulation.Movement.RoadTravelCount(PlaneCodes.Physical, q, r) > 0)
        {
            return false;
        }

        var strongestCultureId = 0;
        var strongestRaw = 0f;
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
        {
            var raw = world.CultureFieldAt(cultureId, PlaneCodes.Physical, q, r).TotalRawMass;
            if (raw > strongestRaw)
            {
                strongestRaw = raw;
                strongestCultureId = cultureId;
            }
        }

        var reclaimCeiling = Data.ReclaimMassCeiling * simulation.Knowledge.Modifier(
            strongestCultureId,
            KnowledgeGrantDomainCodes.FrontierReclaimThreshold);
        if (strongestRaw > reclaimCeiling)
        {
            return false;
        }

        return IsEdgeCell(world, q, r);
    }

    private bool IsEdgeCell(WorldState world, int q, int r)
    {
        for (var direction = 0; direction < 6; direction++)
        {
            if (!world.CellIsKnown(PlaneCodes.Physical, q + NeighborDQ[direction], r + NeighborDR[direction]) ||
                world.CellTerrainCoverId(PlaneCodes.Physical, q + NeighborDQ[direction], r + NeighborDR[direction]) != 0)
            {
                return true;
            }
        }

        return false;
    }

    public void SetAgeConfiguration(
        int thawAgeId,
        int refreezeAgeId,
        int frozenTerrainId,
        int maxRadius,
        float frontierMassThreshold,
        float reclaimMassCeiling,
        int thawBudgetPerPulse,
        int refreezeBudgetPerPulse)
    {
        ValidateRadiusAndBudget(maxRadius, thawBudgetPerPulse);
        if (refreezeBudgetPerPulse <= 0 || refreezeBudgetPerPulse > _candidateQ.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(refreezeBudgetPerPulse));
        }

        Data.ThawAgeIds[0] = thawAgeId;
        Data.RefreezeAgeIds[0] = refreezeAgeId;
        Data.FrozenTerrainId = frozenTerrainId;
        Data.MaxRadius = maxRadius;
        Data.FrontierMassThreshold = frontierMassThreshold;
        Data.ReclaimMassCeiling = reclaimMassCeiling;
        Data.ThawBudgetPerPulse = thawBudgetPerPulse;
        Data.RefreezeBudgetPerPulse = refreezeBudgetPerPulse;
    }

    private void ValidateRadiusAndBudget(int radius, int budgetPerPulse)
    {
        if (radius <= 0 ||
            (Data.GenerationRadiusCapacity > 0 && radius > Data.GenerationRadiusCapacity))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(radius));
        }

        if (budgetPerPulse <= 0 || budgetPerPulse > _candidateQ.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(budgetPerPulse));
        }
    }

    /// <summary>Registers an additional thaw and/or reclaim driver age (0 slots ignored).</summary>
    public void AddDriverAge(int thawAgeId, int refreezeAgeId)
    {
        AppendAge(Data.ThawAgeIds, thawAgeId);
        AppendAge(Data.RefreezeAgeIds, refreezeAgeId);
    }

    private static void AppendAge(int[] ages, int ageId)
    {
        if (ageId == 0)
        {
            return;
        }

        for (var i = 0; i < ages.Length; i++)
        {
            if (ages[i] == 0)
            {
                ages[i] = ageId;
                return;
            }
        }
    }
}
