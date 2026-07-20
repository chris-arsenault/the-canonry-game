namespace Engine.Kernel;

using Engine.Constants;

internal sealed class MapSignalsProgramRuntime :
    IKernelProgramRuntime<MapSignalsProgramData, MapSignalsProgramState>
{
    private const float CultureMaturityCapacityScale = 0.4f;
    private const float ChronicleAttractorScale = 2f;

    public MapSignalsProgramRuntime(MapSignalsProgramData data, MapSignalsProgramState state)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        Data = data;
        State = state;
    }

    public MapSignalsProgramData Data { get; }

    public MapSignalsProgramState State { get; }

    public void RegisterTerrainCapacity(int terrainId, float baseCapacity) =>
        Data.RegisterTerrainCapacity(terrainId, baseCapacity);

    public void RegisterKindSignal(
        int kindId,
        int signalRadius,
        float capacityEmission,
        float supportEmission,
        float threatEmission,
        float baseAttractorMass,
        float coverageAttractorScale,
        float influenceAttractorScale,
        float activityAttractorScale,
        int attractorRadius) =>
        Data.RegisterKindSignal(
            kindId,
            signalRadius,
            capacityEmission,
            supportEmission,
            threatEmission,
            baseAttractorMass,
            coverageAttractorScale,
            influenceAttractorScale,
            activityAttractorScale,
            attractorRadius);

    public void AddKindAttractorScalar(int kindId, int attributeId, float attractorScale) =>
        Data.AddKindAttractorScalar(kindId, attributeId, attractorScale);

    public void RegisterRelationshipSignal(
        int relationshipKindId,
        int signalRadius,
        float supportEmission,
        float threatEmission) =>
        Data.RegisterRelationshipSignal(
            relationshipKindId,
            signalRadius,
            supportEmission,
            threatEmission);

    public float EntityCapacity(int entityId, int planeCode) =>
        State.EntityCapacity(entityId, planeCode);

    public float SupportAt(int planeCode, int q, int r) =>
        State.SupportAt(planeCode, q, r);

    public float ThreatAt(int planeCode, int q, int r) =>
        State.ThreatAt(planeCode, q, r);

    public float CrowdingLoadAt(int planeCode, int q, int r) =>
        State.CrowdingLoadAt(planeCode, q, r);

    public float CrowdingComfortAt(int planeCode, int q, int r) =>
        State.CrowdingComfortAt(planeCode, q, r);

    public float AttractorMassAt(int planeCode, int q, int r) =>
        State.AttractorMassAt(planeCode, q, r);

    public float EntitySupport(int entityId, int planeCode) =>
        State.EntitySupport(entityId, planeCode);

    public float EntityThreat(int entityId, int planeCode) =>
        State.EntityThreat(entityId, planeCode);

    public float EntityCrowdingLoad(int entityId, int planeCode) =>
        State.EntityCrowdingLoad(entityId, planeCode);

    public float EntityCrowdingComfort(int entityId, int planeCode) =>
        State.EntityCrowdingComfort(entityId, planeCode);

    public float EntityAttractorMass(int entityId, int planeCode) =>
        State.EntityAttractorMass(entityId, planeCode);

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        State.BeginTick();
        EmitKindFields(simulation);
        EmitRelationshipFields(simulation);
        ComputeEntitySignals(simulation, tick);
    }

    private void EmitKindFields(Simulation simulation)
    {
        var world = simulation.World;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!world.EntityIsActive(entityId))
            {
                continue;
            }

            var kindId = world.EntityKindId(entityId);
            var radius = Data.KindSignalRadius(kindId);
            EmitFieldAround(
                PlaneCodes.Physical,
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId),
                radius,
                Data.KindCapacityEmission(kindId),
                Data.KindSupportEmission(kindId),
                Data.KindThreatEmission(kindId));
            EmitFieldAround(
                PlaneCodes.Semantic,
                world.EntitySemanticQ(entityId),
                world.EntitySemanticR(entityId),
                radius,
                Data.KindCapacityEmission(kindId),
                Data.KindSupportEmission(kindId),
                Data.KindThreatEmission(kindId));
        }
    }

    private void EmitRelationshipFields(Simulation simulation)
    {
        var world = simulation.World;
        for (var relationshipId = 1; relationshipId <= world.RelationshipHighWatermark; relationshipId++)
        {
            if (!world.RelationshipIsActive(relationshipId))
            {
                continue;
            }

            var kindId = world.RelationshipKindId(relationshipId);
            var radius = Data.RelationshipSignalRadius(kindId);
            if (radius == 0 &&
                Data.RelationshipSupportEmission(kindId) == 0f &&
                Data.RelationshipThreatEmission(kindId) == 0f)
            {
                continue;
            }

            var strength = world.RelationshipStrength(relationshipId);
            var support = Data.RelationshipSupportEmission(kindId) * strength;
            var threat = Data.RelationshipThreatEmission(kindId) * strength;
            EmitRelationshipEndpointFields(world, world.RelationshipSourceId(relationshipId), radius, support, threat);
            EmitRelationshipEndpointFields(world, world.RelationshipTargetId(relationshipId), radius, support, threat);
        }
    }

    private void EmitRelationshipEndpointFields(
        WorldState world,
        int entityId,
        int radius,
        float support,
        float threat)
    {
        if (!world.EntityIsActive(entityId))
        {
            return;
        }

        EmitFieldAround(
            PlaneCodes.Physical,
            world.EntityPhysicalQ(entityId),
            world.EntityPhysicalR(entityId),
            radius,
            capacity: 0f,
            support,
            threat);
        EmitFieldAround(
            PlaneCodes.Semantic,
            world.EntitySemanticQ(entityId),
            world.EntitySemanticR(entityId),
            radius,
            capacity: 0f,
            support,
            threat);
    }

    private void EmitFieldAround(
        int planeCode,
        int centerQ,
        int centerR,
        int radius,
        float capacity,
        float support,
        float threat)
    {
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = centerQ + dq;
                var r = centerR + dr;
                State.AddCapacity(planeCode, q, r, capacity);
                State.AddSupport(planeCode, q, r, support);
                State.AddThreat(planeCode, q, r, threat);
            }
        }
    }

    private void ComputeEntitySignals(Simulation simulation, int tick)
    {
        var world = simulation.World;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!world.EntityIsActive(entityId))
            {
                continue;
            }

            ComputeEntitySignalsForPlane(simulation, entityId, PlaneCodes.Physical, tick);
            ComputeEntitySignalsForPlane(simulation, entityId, PlaneCodes.Semantic, tick);
        }
    }

    private void ComputeEntitySignalsForPlane(Simulation simulation, int entityId, int planeCode, int tick)
    {
        var world = simulation.World;
        var q = CurrentQ(world, entityId, planeCode);
        var r = CurrentR(world, entityId, planeCode);
        var cultureId = world.EntityCultureId(entityId);
        var capacity = DynamicCapacityAt(world, planeCode, q, r) *
            simulation.Knowledge.Modifier(
                cultureId,
                KnowledgeGrantDomainCodes.CarryingCapacity,
                planeCode);
        var occupancy = LocalOccupancy(simulation, planeCode, q, r, Data.CrowdingRadius);
        var load = CrowdingLoad(occupancy, capacity);
        var comfort = CrowdingComfort(occupancy, capacity);
        var support = SupportAt(planeCode, q, r);
        var threat = ThreatAt(planeCode, q, r);
        var attractor = EntityAttractorMass(simulation, entityId, planeCode, q, r, tick, support, threat);

        State.SetEntityCapacity(entityId, planeCode, capacity);
        State.SetEntitySupport(entityId, planeCode, support);
        State.SetEntityThreat(entityId, planeCode, threat);
        State.SetEntityCrowdingLoad(entityId, planeCode, load);
        State.SetEntityCrowdingComfort(entityId, planeCode, comfort);
        State.SetEntityAttractorMass(entityId, planeCode, attractor);
        State.SetCrowdingLoad(planeCode, q, r, load);
        State.SetCrowdingComfort(planeCode, q, r, comfort);
        EmitAttractorAround(planeCode, q, r, Data.KindAttractorRadius(world.EntityKindId(entityId)), attractor);
    }

    private float EntityAttractorMass(
        Simulation simulation,
        int entityId,
        int planeCode,
        int q,
        int r,
        int tick,
        float support,
        float threat)
    {
        var world = simulation.World;
        var kindId = world.EntityKindId(entityId);
        var mass = Data.KindBaseAttractorMass(kindId);
        mass += world.CoverageOwnerCellCount(entityId, planeCode) * Data.KindCoverageAttractorScale(kindId);

        var cultureId = world.EntityCultureId(entityId);
        if (cultureId > 0)
        {
            mass += world.GetCellCultureMaturity(cultureId, planeCode, q, r) *
                Data.KindInfluenceAttractorScale(kindId);
        }

        mass += simulation.MapActivity.Heat(planeCode, q, r, tick) * Data.KindActivityAttractorScale(kindId);
        mass += AttractorScalarMass(world, entityId, kindId);

        // Convergence made spatial: the cast of a hot chronicle radiates
        // pull, so running stories become places the world is drawn toward.
        mass += simulation.Chronicles.HeatFor(entityId) * ChronicleAttractorScale;

        mass += support - threat;
        return global::System.MathF.Max(0f, mass) *
            simulation.Knowledge.Modifier(cultureId, KnowledgeGrantDomainCodes.Attraction, planeCode);
    }

    private float AttractorScalarMass(WorldState world, int entityId, int kindId)
    {
        var mass = 0f;
        var start = Data.KindScalarStart(kindId);
        var count = Data.KindScalarCount(kindId);
        for (var offset = 0; offset < count; offset++)
        {
            var scalar = start + offset;
            mass += world.GetAttribute(entityId, Data.ScalarAttributeId(scalar)) *
                Data.ScalarAttractorScale(scalar);
        }

        return mass;
    }

    private void EmitAttractorAround(int planeCode, int centerQ, int centerR, int radius, float mass)
    {
        if (mass <= 0f)
        {
            return;
        }

        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = centerQ + dq;
                var r = centerR + dr;
                var distance = HexDistance(centerQ, centerR, q, r);
                State.AddAttractorMass(planeCode, q, r, mass / (1f + distance));
            }
        }
    }

    private float DynamicCapacityAt(WorldState world, int planeCode, int q, int r)
    {
        var capacity = BaseCapacityAt(world, planeCode, q, r) + State.CapacityAdditionAt(planeCode, q, r);
        if (!world.CellIsKnown(planeCode, q, r))
        {
            return capacity;
        }

        var ownerId = world.CoverageOwnerAt(planeCode, q, r);
        if (ownerId > 0 && world.EntityIsActive(ownerId))
        {
            capacity += Data.KindCapacityEmission(world.EntityKindId(ownerId));
        }

        capacity += DominantCultureMaturityAt(world, planeCode, q, r) * CultureMaturityCapacityScale;
        return capacity;
    }

    private static float DominantCultureMaturityAt(WorldState world, int planeCode, int q, int r)
    {
        var maturity = 0f;
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
        {
            maturity = global::System.MathF.Max(maturity, world.GetCellCultureMaturity(cultureId, planeCode, q, r));
        }

        return maturity;
    }

    private float BaseCapacityAt(WorldState world, int planeCode, int q, int r)
    {
        if (!world.CellIsKnown(planeCode, q, r))
        {
            return planeCode == PlaneCodes.Semantic ? Data.SemanticBaseCapacity : 0f;
        }

        if (planeCode == PlaneCodes.Physical && Data.HasCapacityFormula)
        {
            return PhysicalBaseCapacity(world, q, r);
        }

        var capacity = Data.TerrainBaseCapacity(world.CellTerrainId(planeCode, q, r));
        if (capacity > 0f)
        {
            return capacity;
        }

        return planeCode == PlaneCodes.Semantic ? Data.SemanticBaseCapacity : 1f;
    }

    /// <summary>
    /// Carrying capacity derived from a hex's real facts: how much life this
    /// exact ground supports. Warmth (with geothermal refuges) sets the
    /// suitability curve, fresh water and fertile river ground multiply it,
    /// broken extreme relief suppresses it, and open water carries its own
    /// temperature-driven marine life. A verdant river valley is rich
    /// because it is warm, wet, and flat - never because a table said so.
    /// </summary>
    internal float PhysicalBaseCapacity(WorldState world, int q, int r)
    {
        var temperature = world.PhysicalHexFact(q, r, PhysicalHexFactCodes.Temperature);
        var geothermal = world.PhysicalHexFact(q, r, PhysicalHexFactCodes.Geothermal);
        var warmth = temperature + (geothermal * Data.CapacityGeothermalBlend);
        var suitability = Clamp01(
            (warmth - Data.CapacityColdFloor) / (Data.CapacityWarmPeak - Data.CapacityColdFloor));
        var land = world.PhysicalHexFact(q, r, PhysicalHexFactCodes.LandFraction) / 255f;
        var fresh = 1f +
            (Data.CapacityFreshwaterWeight * world.PhysicalHexFact(q, r, PhysicalHexFactCodes.RiverCoverage) / 255f) +
            (Data.CapacityFertileWeight * world.PhysicalHexFact(q, r, PhysicalHexFactCodes.FloodplainCoverage) / 255f);
        var reliefExcess = global::System.MathF.Max(
            0f,
            (world.PhysicalHexFact(q, r, PhysicalHexFactCodes.Relief) / 255f) - Data.CapacityReliefComfort);
        var shelter = Clamp01(1f -
            (Data.CapacityReliefPenalty * reliefExcess) -
            (Data.CapacityRoughnessPenalty * world.PhysicalHexFact(q, r, PhysicalHexFactCodes.Roughness) / 255f));
        var landCapacity = Data.CapacityLandScale * suitability * land * fresh * shelter;
        var marineCapacity = Data.CapacityMarineScale * suitability * (1f - land);
        return landCapacity + marineCapacity;
    }

    private int LocalOccupancy(Simulation simulation, int planeCode, int centerQ, int centerR, int radius)
    {
        var occupancy = 0;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                occupancy += simulation.MapActivity.CurrentOccupancy(planeCode, centerQ + dq, centerR + dr);
            }
        }

        return occupancy;
    }

    private static float CrowdingLoad(int occupancy, float capacity)
    {
        if (capacity <= 0f)
        {
            return occupancy > 0 ? occupancy : 0f;
        }

        return occupancy <= capacity ? 0f : (occupancy - capacity) / capacity;
    }

    private static float CrowdingComfort(int occupancy, float capacity)
    {
        if (occupancy <= 1 || capacity <= 1f || occupancy > capacity)
        {
            return 0f;
        }

        return Clamp01((occupancy - 1f) / (capacity - 1f));
    }

    private static int CurrentQ(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticQ(entityId) : world.EntityPhysicalQ(entityId);

    private static int CurrentR(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticR(entityId) : world.EntityPhysicalR(entityId);

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        return (global::System.Math.Abs(aq - bq) +
                global::System.Math.Abs(ar - br) +
                global::System.Math.Abs(asCoord - bsCoord)) / 2;
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }
}
