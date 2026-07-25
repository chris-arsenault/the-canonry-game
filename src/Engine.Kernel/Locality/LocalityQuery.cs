namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// Direct, bounded queries over current local state. Locality facts are not a
/// second materialized world: consumers derive them only where they act.
/// </summary>
internal sealed class LocalityQuery
{
    private static readonly FixedScalar CultureMaturityCapacityScale = FixedScalar.FromUnits(400000L);
    private static readonly FixedScalar CultureSignalRawScale = WorldState.CultureClaimedRawMass;
    private static readonly FixedScalar ChronicleAttractorScale = 2;

    public LocalityQuery(LocalityData data)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        Data = data;
    }

    public LocalityData Data { get; }

    public void RegisterTerrainCapacity(int terrainId, FixedScalar baseCapacity) =>
        Data.RegisterTerrainCapacity(terrainId, baseCapacity);

    public void RegisterKind(
        int kindId,
        int localityRadius,
        FixedScalar capacity,
        FixedScalar support,
        FixedScalar threat,
        FixedScalar baseAttractorMass,
        FixedScalar coverageAttractorScale,
        FixedScalar influenceAttractorScale,
        int attractorRadius) =>
        Data.RegisterKindLocality(
            kindId,
            localityRadius,
            capacity,
            support,
            threat,
            baseAttractorMass,
            coverageAttractorScale,
            influenceAttractorScale,
            attractorRadius);

    public void AddKindAttractorScalar(int kindId, int attributeId, FixedScalar attractorScale) =>
        Data.AddKindAttractorScalar(kindId, attributeId, attractorScale);

    public void RegisterRelationship(
        int relationshipKindId,
        FixedScalar support,
        FixedScalar threat) =>
        Data.RegisterRelationshipLocality(relationshipKindId, support, threat);

    public int OccupancyAt(WorldState world, int planeCode, int q, int r)
    {
        var count = 0;
        for (var entityId = FirstEntityAt(world, planeCode, q, r);
             entityId > 0;
             entityId = NextEntityAt(world, planeCode, entityId))
        {
            count += world.EntityIsActive(entityId) ? 1 : 0;
        }

        return count;
    }

    public FixedScalar EntityCapacity(Simulation simulation, int entityId, int planeCode)
    {
        var world = simulation.World;
        var q = CurrentQ(world, entityId, planeCode);
        var r = CurrentR(world, entityId, planeCode);
        var cultureId = world.EntityCultureId(entityId);
        return DynamicCapacityAt(world, planeCode, q, r) *
            simulation.Knowledge.Modifier(cultureId, KnowledgeGrantDomainCodes.CarryingCapacity, planeCode);
    }

    public LocalityPressure EntityPressure(Simulation simulation, int entityId, int planeCode)
    {
        var world = simulation.World;
        var q = CurrentQ(world, entityId, planeCode);
        var r = CurrentR(world, entityId, planeCode);
        KindLocalityAt(world, planeCode, q, r, out _, out var support, out var threat);
        var cultureId = world.EntityCultureId(entityId);
        if (cultureId > 0)
        {
            support += world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r) / CultureSignalRawScale;
            var locality = simulation.CultureLocality.CellAt(world, cultureId, planeCode, q, r);
            threat += locality.ForeignRawMass / CultureSignalRawScale;
        }

        if (world.CoverageOwnerAt(planeCode, q, r) == entityId)
        {
            support += 1;
        }

        RelationshipLocalityFor(world, entityId, out var relationshipSupport, out var relationshipThreat);
        return new LocalityPressure(
            support + relationshipSupport,
            threat + relationshipThreat);
    }

    public FixedScalar EntitySupport(Simulation simulation, int entityId, int planeCode) =>
        EntityPressure(simulation, entityId, planeCode).Support;

    public FixedScalar EntityThreat(Simulation simulation, int entityId, int planeCode) =>
        EntityPressure(simulation, entityId, planeCode).Threat;

    public LocalityCrowding EntityCrowding(Simulation simulation, int entityId, int planeCode)
    {
        var world = simulation.World;
        var q = CurrentQ(world, entityId, planeCode);
        var r = CurrentR(world, entityId, planeCode);
        var occupancy = LocalOccupancy(world, planeCode, q, r, Data.CrowdingRadius);
        var capacity = EntityCapacity(simulation, entityId, planeCode);
        return new LocalityCrowding(
            CrowdingLoad(occupancy, capacity),
            CrowdingComfort(occupancy, capacity));
    }

    public FixedScalar EntityCrowdingLoad(Simulation simulation, int entityId, int planeCode) =>
        EntityCrowding(simulation, entityId, planeCode).Load;

    public FixedScalar EntityCrowdingComfort(Simulation simulation, int entityId, int planeCode) =>
        EntityCrowding(simulation, entityId, planeCode).Comfort;

    public FixedScalar AttractorMassAt(Simulation simulation, int planeCode, int q, int r)
    {
        var total = FixedScalar.Zero;
        var radius = Data.MaxAttractorRadius;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var sourceQ = q + dq;
                var sourceR = r + dr;
                var distance = HexDistance(q, r, sourceQ, sourceR);
                for (var entityId = FirstEntityAt(simulation.World, planeCode, sourceQ, sourceR);
                     entityId > 0;
                     entityId = NextEntityAt(simulation.World, planeCode, entityId))
                {
                    if (!simulation.World.EntityIsActive(entityId))
                    {
                        continue;
                    }

                    var kindId = simulation.World.EntityKindId(entityId);
                    if (distance > Data.KindAttractorRadius(kindId))
                    {
                        continue;
                    }

                    total += EntityAttractorMass(simulation, entityId, planeCode) / (1 + distance);
                }
            }
        }

        return total;
    }

    public FixedScalar EntityAttractorMass(Simulation simulation, int entityId, int planeCode)
    {
        var world = simulation.World;
        var kindId = world.EntityKindId(entityId);
        var mass = Data.KindBaseAttractorMass(kindId);
        mass += world.CoverageOwnerCellCount(entityId, planeCode) * Data.KindCoverageAttractorScale(kindId);
        var cultureId = world.EntityCultureId(entityId);
        if (cultureId > 0)
        {
            mass += world.GetCellCultureMaturity(
                    cultureId,
                    planeCode,
                    CurrentQ(world, entityId, planeCode),
                    CurrentR(world, entityId, planeCode)) *
                Data.KindInfluenceAttractorScale(kindId);
        }

        var start = Data.KindScalarStart(kindId);
        var count = Data.KindScalarCount(kindId);
        for (var offset = 0; offset < count; offset++)
        {
            var scalar = start + offset;
            mass += world.GetAttribute(entityId, Data.ScalarAttributeId(scalar)) *
                Data.ScalarAttractorScale(scalar);
        }

        mass += simulation.Chronicles.HeatFor(entityId) * ChronicleAttractorScale;
        var pressure = EntityPressure(simulation, entityId, planeCode);
        mass += pressure.Support - pressure.Threat;
        return FixedScalar.Max(0, mass) *
            simulation.Knowledge.Modifier(cultureId, KnowledgeGrantDomainCodes.Attraction, planeCode);
    }

    private FixedScalar DynamicCapacityAt(WorldState world, int planeCode, int q, int r)
    {
        KindLocalityAt(world, planeCode, q, r, out var capacity, out _, out _);
        capacity += BaseCapacityAt(world, planeCode, q, r);
        if (!world.CellIsKnown(planeCode, q, r))
        {
            return capacity;
        }

        var ownerId = world.CoverageOwnerAt(planeCode, q, r);
        if (ownerId > 0 && world.EntityIsActive(ownerId))
        {
            capacity += Data.KindCapacityEmission(world.EntityKindId(ownerId));
        }

        return capacity + (DominantCultureMaturityAt(world, planeCode, q, r) * CultureMaturityCapacityScale);
    }

    private void KindLocalityAt(
        WorldState world,
        int planeCode,
        int q,
        int r,
        out FixedScalar capacity,
        out FixedScalar support,
        out FixedScalar threat)
    {
        capacity = 0;
        support = 0;
        threat = 0;
        var radius = Data.MaxKindLocalityRadius;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var sourceQ = q + dq;
                var sourceR = r + dr;
                var distance = HexDistance(q, r, sourceQ, sourceR);
                for (var entityId = FirstEntityAt(world, planeCode, sourceQ, sourceR);
                     entityId > 0;
                     entityId = NextEntityAt(world, planeCode, entityId))
                {
                    if (!world.EntityIsActive(entityId))
                    {
                        continue;
                    }

                    var kindId = world.EntityKindId(entityId);
                    if (distance > Data.KindLocalityRadius(kindId))
                    {
                        continue;
                    }

                    capacity += Data.KindCapacityEmission(kindId);
                    support += Data.KindSupportEmission(kindId);
                    threat += Data.KindThreatEmission(kindId);
                }
            }
        }
    }

    private void RelationshipLocalityFor(
        WorldState world,
        int entityId,
        out FixedScalar support,
        out FixedScalar threat)
    {
        support = 0;
        threat = 0;
        for (var index = 1; index <= Data.RelationshipKindCount; index++)
        {
            var kindId = Data.RelationshipKindId(index);
            var supportEmission = Data.RelationshipSupportEmission(kindId);
            var threatEmission = Data.RelationshipThreatEmission(kindId);
            var outgoing = world.OutgoingRelationshipsOfKind(entityId, kindId);
            while (world.MoveNextRelationship(ref outgoing, out var relationshipId))
            {
                var strength = world.RelationshipStrength(relationshipId);
                support += supportEmission * strength;
                threat += threatEmission * strength;
            }

            var incoming = world.IncomingRelationshipsOfKind(entityId, kindId);
            while (world.MoveNextRelationship(ref incoming, out var relationshipId))
            {
                var strength = world.RelationshipStrength(relationshipId);
                support += supportEmission * strength;
                threat += threatEmission * strength;
            }
        }
    }

    private FixedScalar BaseCapacityAt(WorldState world, int planeCode, int q, int r)
    {
        if (!world.CellIsKnown(planeCode, q, r))
        {
            return planeCode == PlaneCodes.Semantic ? Data.SemanticBaseCapacity : 0;
        }

        if (planeCode == PlaneCodes.Physical && Data.HasCapacityFormula)
        {
            return PhysicalBaseCapacity(world, q, r);
        }

        var capacity = Data.TerrainBaseCapacity(world.CellTerrainId(planeCode, q, r));
        if (capacity > 0)
        {
            return capacity;
        }

        return planeCode == PlaneCodes.Semantic ? Data.SemanticBaseCapacity : 1;
    }

    internal FixedScalar PhysicalBaseCapacity(WorldState world, int q, int r)
    {
        var temperature = world.PhysicalHexFact(q, r, PhysicalHexFactCodes.Temperature);
        var geothermal = world.PhysicalHexFact(q, r, PhysicalHexFactCodes.Geothermal);
        var warmth = temperature + (geothermal * Data.CapacityGeothermalBlend);
        var suitability = Clamp01(
            (warmth - Data.CapacityColdFloor) / (Data.CapacityWarmPeak - Data.CapacityColdFloor));
        var land = FixedScalar.FromRatio(
            world.PhysicalHexFact(q, r, PhysicalHexFactCodes.LandFraction),
            255);
        var fresh = 1 +
            (Data.CapacityFreshwaterWeight * world.PhysicalHexFact(q, r, PhysicalHexFactCodes.RiverCoverage) / 255) +
            (Data.CapacityFertileWeight * world.PhysicalHexFact(q, r, PhysicalHexFactCodes.FloodplainCoverage) / 255);
        var reliefExcess = FixedScalar.Max(
            0,
            FixedScalar.FromRatio(
                world.PhysicalHexFact(q, r, PhysicalHexFactCodes.Relief),
                255) - Data.CapacityReliefComfort);
        var shelter = Clamp01(1 -
            (Data.CapacityReliefPenalty * reliefExcess) -
            (Data.CapacityRoughnessPenalty * world.PhysicalHexFact(q, r, PhysicalHexFactCodes.Roughness) / 255));
        return (Data.CapacityLandScale * suitability * land * fresh * shelter) +
            (Data.CapacityMarineScale * suitability * (1 - land));
    }

    private int LocalOccupancy(WorldState world, int planeCode, int q, int r, int radius)
    {
        var occupancy = 0;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                occupancy += OccupancyAt(world, planeCode, q + dq, r + dr);
            }
        }

        return occupancy;
    }

    private static FixedScalar DominantCultureMaturityAt(WorldState world, int planeCode, int q, int r)
    {
        var maturity = FixedScalar.Zero;
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
        {
            maturity = FixedScalar.Max(
                maturity,
                world.GetCellCultureMaturity(cultureId, planeCode, q, r));
        }

        return maturity;
    }

    private static FixedScalar CrowdingLoad(int occupancy, FixedScalar capacity)
    {
        if (capacity <= 0)
        {
            return occupancy > 0 ? occupancy : 0;
        }

        return occupancy <= capacity ? 0 : (occupancy - capacity) / capacity;
    }

    private static FixedScalar CrowdingComfort(int occupancy, FixedScalar capacity)
    {
        if (occupancy <= 1 || capacity <= 1 || occupancy > capacity)
        {
            return 0;
        }

        return Clamp01((occupancy - 1) / (capacity - 1));
    }

    private static int FirstEntityAt(WorldState world, int planeCode, int q, int r) =>
        planeCode == PlaneCodes.Semantic
            ? world.FirstSemanticEntityAt(q, r)
            : world.FirstPhysicalEntityAt(q, r);

    private static int NextEntityAt(WorldState world, int planeCode, int entityId) =>
        planeCode == PlaneCodes.Semantic
            ? world.NextSemanticEntityAt(entityId)
            : world.NextPhysicalEntityAt(entityId);

    private static int CurrentQ(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticQ(entityId) : world.EntityPhysicalQ(entityId);

    private static int CurrentR(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticR(entityId) : world.EntityPhysicalR(entityId);

    private static int HexDistance(int aq, int ar, int bq, int br) =>
        (global::System.Math.Abs(aq - bq) +
            global::System.Math.Abs(ar - br) +
            global::System.Math.Abs((aq + ar) - (bq + br))) / 2;

    private static FixedScalar Clamp01(FixedScalar value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > 1 ? 1 : value;
    }
}

internal readonly record struct LocalityPressure(FixedScalar Support, FixedScalar Threat);

internal readonly record struct LocalityCrowding(FixedScalar Load, FixedScalar Comfort);
