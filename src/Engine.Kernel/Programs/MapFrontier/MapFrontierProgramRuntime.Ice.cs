namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MapFrontierProgramRuntime
{
    /// <summary>
    /// The ice layer: one cold-pressure formula over the hex facts decides
    /// where deep-ice cover advances and where it melts, scaled by the open
    /// age. Pressure comes from the ground itself - cold, ice-thick,
    /// geothermally dead hexes freeze first and melt last. Resistance comes
    /// from what mortals built there: roads, culture mass, and current
    /// occupancy hold the cover off. Advance rotates over frontier-grown
    /// cells only, so startup geography is never silently rewritten, and a
    /// covered cell stays known: the ice closes ground, it does not erase it.
    /// </summary>
    private void AdvanceIce(Simulation simulation, int tick)
    {
        var world = simulation.World;
        var budget = global::System.Math.Min(Data.IceAdvanceBudgetPerPulse, _candidateQ.Length);
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
            if (IceShouldAdvance(simulation, world, q, r))
            {
                _candidateQ[count] = q;
                _candidateR[count] = r;
                count++;
            }
        }

        for (var i = 0; i < count; i++)
        {
            simulation.SetTerrainCover(_candidateQ[i], _candidateR[i], Data.FrozenTerrainId, tick);
            State.TotalReclaimed++;
        }
    }

    /// <summary>
    /// Thaw ages melt the cover back where pressure no longer beats
    /// resistance, working the same rotating ring so retreat is as bounded
    /// and directionless as advance.
    /// </summary>
    private void RetreatIce(Simulation simulation, int tick)
    {
        var world = simulation.World;
        var budget = global::System.Math.Min(Data.IceRetreatBudgetPerPulse, _candidateQ.Length);
        var count = 0;
        var visited = 0;
        while (visited < State.ThawedCount && count < budget)
        {
            if (State.IceRetreatCursor >= State.ThawedCount)
            {
                State.IceRetreatCursor = 0;
            }

            var q = State.ThawedQ[State.IceRetreatCursor];
            var r = State.ThawedR[State.IceRetreatCursor];
            State.IceRetreatCursor++;
            visited++;
            if (world.CellIsKnown(PlaneCodes.Physical, q, r) &&
                world.CellTerrainCoverId(PlaneCodes.Physical, q, r) == Data.FrozenTerrainId &&
                ColdPressure(simulation, q, r, Data.ThawAgePressureScalar) <
                    IceResistance(simulation, world, q, r))
            {
                _candidateQ[count] = q;
                _candidateR[count] = r;
                count++;
            }
        }

        for (var i = 0; i < count; i++)
        {
            simulation.ClearTerrainCover(_candidateQ[i], _candidateR[i], tick);
            State.TotalThawed++;
        }
    }

    private bool IceShouldAdvance(Simulation simulation, WorldState world, int q, int r)
    {
        if (!world.CellIsKnown(PlaneCodes.Physical, q, r) ||
            world.CellTerrainCoverId(PlaneCodes.Physical, q, r) != 0 ||
            simulation.MapActivity.CurrentOccupancy(PlaneCodes.Physical, q, r) > 0 ||
            !IsIceEdgeCell(world, q, r))
        {
            return false;
        }

        return ColdPressure(simulation, q, r, Data.FreezeAgePressureScalar) >
            IceResistance(simulation, world, q, r);
    }

    /// <summary>
    /// Cold pressure derives from the hex's real facts: how far its
    /// temperature sits below the frost ceiling, thickened by resident ice,
    /// relieved by geothermal heat, and scaled by the open age. The formula
    /// is the only geography opinion the ice holds.
    /// </summary>
    private float ColdPressure(Simulation simulation, int q, int r, float ageScalar)
    {
        var geography = simulation.KernelData.PhysicalGeography;
        var cold = Data.FrostTemperatureCeiling - geography.Fact(q, r, PhysicalHexFactCodes.Temperature);
        if (cold <= 0)
        {
            return 0f;
        }

        var pressure = cold +
            (Data.IceThicknessPressureWeight * geography.Fact(q, r, PhysicalHexFactCodes.IceThickness)) -
            (Data.GeothermalRefugeWeight * geography.Fact(q, r, PhysicalHexFactCodes.Geothermal));
        return pressure <= 0f ? 0f : pressure * ageScalar;
    }

    private float IceResistance(Simulation simulation, WorldState world, int q, int r)
    {
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

        var knowledgeScale = simulation.Knowledge.Modifier(
            strongestCultureId,
            KnowledgeGrantDomainCodes.FrontierReclaimThreshold);
        return Data.BaseIceResistance +
            ((strongestRaw * Data.CultureMassResistanceScale) +
             (simulation.Movement.RoadTravelCount(PlaneCodes.Physical, q, r) * Data.RoadIceResistance)) *
            knowledgeScale;
    }

    private bool IsIceEdgeCell(WorldState world, int q, int r)
    {
        for (var direction = 0; direction < 6; direction++)
        {
            var nq = q + NeighborDQ[direction];
            var nr = r + NeighborDR[direction];
            if (!world.CellIsKnown(PlaneCodes.Physical, nq, nr) ||
                world.CellTerrainCoverId(PlaneCodes.Physical, nq, nr) == Data.FrozenTerrainId)
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
        int iceAdvanceBudgetPerPulse,
        int iceRetreatBudgetPerPulse)
    {
        if (maxRadius <= 0 ||
            (Data.GenerationRadiusCapacity > 0 && maxRadius > Data.GenerationRadiusCapacity))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(maxRadius));
        }

        if (iceAdvanceBudgetPerPulse <= 0 || iceAdvanceBudgetPerPulse > _candidateQ.Length ||
            iceRetreatBudgetPerPulse <= 0 || iceRetreatBudgetPerPulse > _candidateQ.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(iceAdvanceBudgetPerPulse));
        }

        Data.ThawAgeIds[0] = thawAgeId;
        Data.RefreezeAgeIds[0] = refreezeAgeId;
        Data.FrozenTerrainId = frozenTerrainId;
        Data.MaxRadius = maxRadius;
        Data.IceAdvanceBudgetPerPulse = iceAdvanceBudgetPerPulse;
        Data.IceRetreatBudgetPerPulse = iceRetreatBudgetPerPulse;
    }

    public void SetIcePressureConfiguration(
        int frostTemperatureCeiling,
        float iceThicknessPressureWeight,
        float geothermalRefugeWeight,
        float freezeAgePressureScalar,
        float thawAgePressureScalar,
        float baseIceResistance,
        float cultureMassResistanceScale,
        float roadIceResistance)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(frostTemperatureCeiling);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(iceThicknessPressureWeight);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(geothermalRefugeWeight);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(freezeAgePressureScalar);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(thawAgePressureScalar);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(baseIceResistance);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(cultureMassResistanceScale);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(roadIceResistance);
        Data.FrostTemperatureCeiling = frostTemperatureCeiling;
        Data.IceThicknessPressureWeight = iceThicknessPressureWeight;
        Data.GeothermalRefugeWeight = geothermalRefugeWeight;
        Data.FreezeAgePressureScalar = freezeAgePressureScalar;
        Data.ThawAgePressureScalar = thawAgePressureScalar;
        Data.BaseIceResistance = baseIceResistance;
        Data.CultureMassResistanceScale = cultureMassResistanceScale;
        Data.RoadIceResistance = roadIceResistance;
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

    public void RegisterKindRevealRadius(int kindId, int radius) =>
        Data.RegisterKindRevealRadius(kindId, radius);
}
