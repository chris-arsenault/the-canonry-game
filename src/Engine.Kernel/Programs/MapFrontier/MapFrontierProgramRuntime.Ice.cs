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
        var budget = global::System.Math.Min(Data.IceAdvanceBudgetPerRun, _candidateQ.Length);
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
        var budget = global::System.Math.Min(Data.IceRetreatBudgetPerRun, _candidateQ.Length);
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
            simulation.Locality.OccupancyAt(world, PlaneCodes.Physical, q, r) > 0 ||
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
    private FixedScalar ColdPressure(Simulation simulation, int q, int r, FixedScalar ageScalar)
    {
        var geography = simulation.KernelData.PhysicalGeography;
        var cold = Data.FrostTemperatureCeiling - geography.Fact(q, r, PhysicalHexFactCodes.Temperature);
        if (cold <= 0)
        {
            return 0;
        }

        var pressure = cold +
            (Data.IceThicknessPressureWeight * geography.Fact(q, r, PhysicalHexFactCodes.IceThickness)) -
            (Data.GeothermalRefugeWeight * geography.Fact(q, r, PhysicalHexFactCodes.Geothermal));
        return pressure <= 0 ? 0 : pressure * ageScalar;
    }

    private FixedScalar IceResistance(Simulation simulation, WorldState world, int q, int r)
    {
        var strongestCultureId = 0;
        var strongestRaw = FixedScalar.Zero;
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
        int iceAdvanceBudgetPerRun,
        int iceRetreatBudgetPerRun)
    {
        if (maxRadius <= 0 ||
            (Data.GenerationRadiusCapacity > 0 && maxRadius > Data.GenerationRadiusCapacity))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(maxRadius));
        }

        if (iceAdvanceBudgetPerRun <= 0 || iceAdvanceBudgetPerRun > _candidateQ.Length ||
            iceRetreatBudgetPerRun <= 0 || iceRetreatBudgetPerRun > _candidateQ.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(iceAdvanceBudgetPerRun));
        }

        Data.ThawAgeIds[0] = thawAgeId;
        Data.RefreezeAgeIds[0] = refreezeAgeId;
        Data.FrozenTerrainId = frozenTerrainId;
        Data.MaxRadius = maxRadius;
        Data.IceAdvanceBudgetPerRun = iceAdvanceBudgetPerRun;
        Data.IceRetreatBudgetPerRun = iceRetreatBudgetPerRun;
    }

    public void SetIcePressureConfiguration(
        int frostTemperatureCeiling,
        FixedScalar iceThicknessPressureWeight,
        FixedScalar geothermalRefugeWeight,
        FixedScalar freezeAgePressureScalar,
        FixedScalar thawAgePressureScalar,
        FixedScalar baseIceResistance,
        FixedScalar cultureMassResistanceScale,
        FixedScalar roadIceResistance)
    {
        NumericGuard.ThrowIfNegative(frostTemperatureCeiling);
        NumericGuard.ThrowIfNegative(iceThicknessPressureWeight);
        NumericGuard.ThrowIfNegative(geothermalRefugeWeight);
        NumericGuard.ThrowIfNegative(freezeAgePressureScalar);
        NumericGuard.ThrowIfNegative(thawAgePressureScalar);
        NumericGuard.ThrowIfNegative(baseIceResistance);
        NumericGuard.ThrowIfNegative(cultureMassResistanceScale);
        NumericGuard.ThrowIfNegative(roadIceResistance);
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
