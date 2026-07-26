namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class WorldStateIndexTests
{
    [Fact]
    public void MaintainsEntityKindOccupancyAndRelationshipIndexes()
    {
        var simulation = CreateSimulation();
        var first = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var second = simulation.CreateEntity(1, 1, 1, 0, 0, 1, 0, -1, 0);
        var third = simulation.CreateEntity(2, 1, 1, 1, 0, 2, 0, -2, 0);

        Assert.Equal(second, simulation.World.FirstEntityOfKind(1));
        Assert.Equal(first, simulation.World.NextEntityOfKind(second));
        Assert.Equal(0, simulation.World.NextEntityOfKind(first));
        Assert.Equal(third, simulation.World.FirstEntityOfKind(2));

        Assert.Contains(first, PhysicalEntitiesAt(simulation.World, 0, 0));
        Assert.Contains(second, PhysicalEntitiesAt(simulation.World, 0, 0));

        simulation.World.MoveEntityPhysical(first, 1, 0, 1);

        Assert.DoesNotContain(first, PhysicalEntitiesAt(simulation.World, 0, 0));
        Assert.Contains(first, PhysicalEntitiesAt(simulation.World, 1, 0));

        var relationship = simulation.World.CreateRelationship(7, 0, first, third, FixedScalar.FromSingle(1f), 2);

        Assert.Equal(relationship, FirstOutgoingByCursor(simulation.World, first, 7));
        Assert.Equal(relationship, FirstIncomingByCursor(simulation.World, third, 7));
    }

    [Fact]
    public void MaintainsEntityCultureIndexAcrossCultureChanges()
    {
        var simulation = CreateSimulation();
        var first = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var second = simulation.CreateEntity(1, 1, 1, 0, 0, 1, 0, -1, 0);
        var third = simulation.CreateEntity(2, 1, 2, 1, 0, 2, 0, -2, 0);

        Assert.Equal(second, simulation.World.FirstEntityOfCulture(1));
        Assert.Equal(first, simulation.World.NextEntityOfCulture(second));
        Assert.Equal(0, simulation.World.NextEntityOfCulture(first));
        Assert.Equal(third, simulation.World.FirstEntityOfCulture(2));

        simulation.World.SetEntityCulture(second, 2, tick: 1);

        Assert.Equal(first, simulation.World.FirstEntityOfCulture(1));
        Assert.Equal(0, simulation.World.NextEntityOfCulture(first));
        Assert.Equal(second, simulation.World.FirstEntityOfCulture(2));
        Assert.Equal(third, simulation.World.NextEntityOfCulture(second));
    }

    [Fact]
    public void MaintainsTypedRelationshipCsrRowsInStableRelationshipOrder()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var lowTarget = simulation.CreateEntity(2, 1, 1, 1, 0, 0, 0, 0, 0);
        var highTarget = simulation.CreateEntity(2, 1, 1, 2, 0, 0, 0, 0, 0);
        var midTarget = simulation.CreateEntity(2, 1, 1, 3, 0, 0, 0, 0, 0);
        var lowSource = simulation.CreateEntity(1, 1, 1, 4, 0, 0, 0, 0, 0);
        var highSource = simulation.CreateEntity(1, 1, 1, 5, 0, 0, 0, 0, 0);
        var midSource = simulation.CreateEntity(1, 1, 1, 6, 0, 0, 0, 0, 0);
        var sink = simulation.CreateEntity(2, 1, 1, 7, 0, 0, 0, 0, 0);
        var low = simulation.World.CreateRelationship(9, 0, source, lowTarget, FixedScalar.FromSingle(0.2f), 0);
        var high = simulation.World.CreateRelationship(9, 0, source, highTarget, FixedScalar.FromSingle(0.9f), 0);
        var mid = simulation.World.CreateRelationship(9, 0, source, midTarget, FixedScalar.FromSingle(0.5f), 0);
        var incomingLow = simulation.World.CreateRelationship(9, 0, lowSource, sink, FixedScalar.FromSingle(0.1f), 0);
        var incomingHigh = simulation.World.CreateRelationship(9, 0, highSource, sink, FixedScalar.FromSingle(0.8f), 0);
        var incomingMid = simulation.World.CreateRelationship(9, 0, midSource, sink, FixedScalar.FromSingle(0.4f), 0);

        Assert.Equal([low, high, mid], OutgoingRelationshipsOfKind(simulation.World, source, 9));
        Assert.Equal([incomingLow, incomingHigh, incomingMid], IncomingRelationshipsOfKind(simulation.World, sink, 9));

        simulation.World.CompactRelationshipGraph();

        Assert.Equal([low, high, mid], OutgoingRelationshipsOfKind(simulation.World, source, 9));
        Assert.Equal([incomingLow, incomingHigh, incomingMid], IncomingRelationshipsOfKind(simulation.World, sink, 9));
        Assert.Equal([high, mid], OutgoingRelationshipsOfKindAbove(simulation.World, source, 9, 0.5f));
        Assert.Equal([incomingHigh], IncomingRelationshipsOfKindAbove(simulation.World, sink, 9, 0.5f));

        simulation.World.SetRelationshipStrength(low, FixedScalar.FromSingle(0.95f), 1);
        simulation.World.SetRelationshipStrength(incomingLow, FixedScalar.FromSingle(0.85f), 1);

        Assert.Equal([low, high, mid], OutgoingRelationshipsOfKind(simulation.World, source, 9));
        Assert.Equal([incomingLow, incomingHigh, incomingMid], IncomingRelationshipsOfKind(simulation.World, sink, 9));

        simulation.World.CullRelationship(high, 2);
        simulation.World.CullRelationship(incomingHigh, 2);

        Assert.Equal([low, mid], OutgoingRelationshipsOfKind(simulation.World, source, 9));
        Assert.Equal([incomingLow, incomingMid], IncomingRelationshipsOfKind(simulation.World, sink, 9));
    }

    [Fact]
    public void RetirementEndsIncidentRelationshipsAndReusesTheirSlots()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var activeTarget = simulation.CreateEntity(2, 1, 1, 1, 0, 0, 0, 0, 0);
        var historicalTarget = simulation.CreateEntity(2, 1, 1, 2, 0, 0, 0, 0, 0);
        var kept = simulation.CreateRelationship(9, 0, source, activeTarget, FixedScalar.FromSingle(0.9f), 0);
        var ended = simulation.CreateRelationship(9, 0, source, historicalTarget, FixedScalar.FromSingle(0.9f), 0);

        simulation.SetEntityStatus(historicalTarget, EntityStatusCodes.Historical, 1);

        Assert.False(simulation.World.RelationshipIsActive(ended));
        Assert.False(simulation.World.EntityExists(historicalTarget));
        Assert.Equal([kept], OutgoingRelationshipsOfKind(simulation.World, source, 9));

        var replacement = simulation.CreateRelationship(9, 0, activeTarget, source, FixedScalar.FromSingle(0.8f), 2);
        Assert.Equal(ended, replacement);
    }

    [Fact]
    public void RecycledEntitySlotsRejectStaleOccurrenceHandlesAcrossSnapshotReload()
    {
        var simulation = CreateSimulation();
        var first = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var firstOccurrence = simulation.World.EntityOccurrenceHandle(first);

        simulation.SetEntityStatus(first, EntityStatusCodes.Historical, tick: 1);
        var replacement = simulation.CreateEntity(2, 1, 1, 1, 0, 0, 0, 0, tick: 2);
        var replacementOccurrence = simulation.World.EntityOccurrenceHandle(replacement);

        Assert.Equal(first, replacement);
        Assert.NotEqual(firstOccurrence, replacementOccurrence);
        Assert.False(simulation.World.TryResolveEntityOccurrence(firstOccurrence, out _));
        Assert.True(simulation.World.TryResolveEntityOccurrence(replacementOccurrence, out var resolved));
        Assert.Equal(replacement, resolved);

        using var stream = new MemoryStream();
        KernelSnapshot.SaveKernelState(simulation, stream);
        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);

        Assert.False(loaded.World.TryResolveEntityOccurrence(firstOccurrence, out _));
        Assert.True(loaded.World.TryResolveEntityOccurrence(replacementOccurrence, out resolved));
        Assert.Equal(replacement, resolved);
    }

    [Fact]
    public void RecycledRelationshipSlotsCannotLeakThroughFrozenAdjacency()
    {
        var simulation = CreateSimulation();
        var oldSource = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var oldTarget = simulation.CreateEntity(2, 1, 1, 1, 0, 0, 0, 0, 0);
        var newSource = simulation.CreateEntity(1, 1, 1, 2, 0, 0, 0, 0, 0);
        var newTarget = simulation.CreateEntity(2, 1, 1, 3, 0, 0, 0, 0, 0);
        var oldRelationship = simulation.CreateRelationship(
            9,
            0,
            oldSource,
            oldTarget,
            FixedScalar.FromSingle(0.8f),
            tick: 0);
        var oldOccurrence = simulation.World.RelationshipOccurrenceHandle(oldRelationship);
        simulation.World.CompactRelationshipGraph();

        simulation.CullRelationship(oldRelationship, tick: 1);
        var replacement = simulation.CreateRelationship(
            9,
            0,
            newSource,
            newTarget,
            FixedScalar.FromSingle(0.7f),
            tick: 2);
        var replacementOccurrence = simulation.World.RelationshipOccurrenceHandle(replacement);

        Assert.Equal(oldRelationship, replacement);
        Assert.NotEqual(oldOccurrence, replacementOccurrence);
        Assert.False(simulation.World.TryResolveRelationshipOccurrence(oldOccurrence, out _));
        Assert.True(simulation.World.TryResolveRelationshipOccurrence(replacementOccurrence, out var resolved));
        Assert.Equal(replacement, resolved);
        Assert.Empty(OutgoingRelationshipsOfKind(simulation.World, oldSource, 9));
        Assert.Equal([replacement], OutgoingRelationshipsOfKind(simulation.World, newSource, 9));
    }

    [Fact]
    public void RelationshipCreationDeduplicatesEndpointAndKind()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(2, 1, 1, 1, 0, 0, 0, 0, 0);

        var first = simulation.CreateRelationship(9, 0, source, target, FixedScalar.FromSingle(0.4f), tick: 0);
        var duplicate = simulation.CreateRelationship(9, 1, source, target, FixedScalar.FromSingle(0.9f), tick: 1);

        Assert.Equal(first, duplicate);
        Assert.Equal([first], OutgoingRelationshipsOfKind(simulation.World, source, 9));
        Assert.Equal(FixedScalar.FromSingle(0.4f), simulation.World.RelationshipStrength(first));
    }

    [Fact]
    public void CultureInfluenceCapacityTracksLiveRowsInsteadOfLifetimeCells()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 4,
            RelationshipCapacity = 4,
            MapCellCapacity = 8,
            MapInfluenceCapacity = 2,
            KindIdCapacity = 4,
            CultureIdCapacity = 4,
            RelationshipKindIdCapacity = 4,
        });
        simulation.ModifyCellCultureInfluenceRaw(
            1,
            PlaneCodes.Physical,
            0,
            0,
            FixedScalar.FromSingle(10f),
            tick: 0);
        simulation.CultureLocality.Invalidate();
        simulation.ModifyCellCultureInfluenceRaw(
            1,
            PlaneCodes.Physical,
            1,
            0,
            FixedScalar.FromSingle(20f),
            tick: 0);
        simulation.CultureLocality.Invalidate();
        simulation.ModifyCellCultureInfluenceRaw(
            1,
            PlaneCodes.Physical,
            0,
            0,
            FixedScalar.FromSingle(-10f),
            tick: 1);
        simulation.CultureLocality.Invalidate();

        simulation.ModifyCellCultureInfluenceRaw(
            1,
            PlaneCodes.Physical,
            2,
            0,
            FixedScalar.FromSingle(30f),
            tick: 2);

        Assert.Equal(2, simulation.World.CultureInfluenceCellCount(1, PlaneCodes.Physical));
        Assert.Equal(
            [(1, 0, 100f), (2, 0, 100f)],
            CultureInfluenceCellsOf(simulation.World, 1, PlaneCodes.Physical));
    }

    [Fact]
    public void TraversesTypedRelationshipCsrWithStrengthThreshold()
    {
        var simulation = CreateSimulation();
        var first = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var second = simulation.CreateEntity(1, 1, 1, 1, 0, 0, 0, 0, 0);
        var third = simulation.CreateEntity(1, 1, 1, 2, 0, 0, 0, 0, 0);
        var fourth = simulation.CreateEntity(1, 1, 1, 3, 0, 0, 0, 0, 0);
        var firstToSecond = simulation.World.CreateRelationship(9, 0, first, second, FixedScalar.FromSingle(0.9f), 0);
        var firstToThird = simulation.World.CreateRelationship(9, 0, first, third, FixedScalar.FromSingle(0.6f), 0);
        var secondToFourth = simulation.World.CreateRelationship(9, 0, second, fourth, FixedScalar.FromSingle(0.7f), 0);
        simulation.World.CreateRelationship(9, 0, third, fourth, FixedScalar.FromSingle(0.4f), 0);
        simulation.World.CreateRelationship(10, 0, first, fourth, FixedScalar.FromSingle(1f), 0);
        simulation.World.CompactRelationshipGraph();

        var outgoing = new RecordingTraversalVisitor(8);
        var outgoingCount = simulation.World.TraverseOutgoingRelationshipsOfKind(first, 9, FixedScalar.FromSingle(0.5f), 2, ref outgoing);

        Assert.Equal(3, outgoingCount);
        Assert.Equal(3, outgoing.Count);
        Assert.Equal(second, outgoing.EntityAt(0));
        Assert.Equal(firstToSecond, outgoing.RelationshipAt(0));
        Assert.Equal(1, outgoing.DepthAt(0));
        Assert.Equal(third, outgoing.EntityAt(1));
        Assert.Equal(firstToThird, outgoing.RelationshipAt(1));
        Assert.Equal(1, outgoing.DepthAt(1));
        Assert.Equal(fourth, outgoing.EntityAt(2));
        Assert.Equal(secondToFourth, outgoing.RelationshipAt(2));
        Assert.Equal(2, outgoing.DepthAt(2));

        var incoming = new RecordingTraversalVisitor(8);
        var incomingCount = simulation.World.TraverseIncomingRelationshipsOfKind(fourth, 9, FixedScalar.FromSingle(0.5f), 2, ref incoming);

        Assert.Equal(2, incomingCount);
        Assert.Equal(second, incoming.EntityAt(0));
        Assert.Equal(secondToFourth, incoming.RelationshipAt(0));
        Assert.Equal(first, incoming.EntityAt(1));
        Assert.Equal(firstToSecond, incoming.RelationshipAt(1));
    }

    [Fact]
    public void MaintainsCoverageLayerReverseIndex()
    {
        var simulation = CreateSimulation();
        var ownerA = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var ownerB = simulation.CreateEntity(1, 1, 1, 1, 0, 0, 0, 0, 0);
        simulation.World.MaterializeCell(0, 0, terrainId: 1, ownerA);
        simulation.World.MaterializeCell(1, 0, terrainId: 1, ownerA);

        Assert.Equal(ownerA, simulation.World.CoverageOwnerAt(0, 0));
        Assert.Equal(2, simulation.World.CoverageOwnerCellCount(ownerA));
        Assert.Equal([(0, 0), (1, 0)], CoverageCellsOf(simulation.World, ownerA));

        simulation.World.SetCoverageOwner(0, 0, ownerB);

        Assert.Equal(ownerB, simulation.World.CoverageOwnerAt(0, 0));
        Assert.Equal(1, simulation.World.CoverageOwnerCellCount(ownerA));
        Assert.Equal(1, simulation.World.CoverageOwnerCellCount(ownerB));
        Assert.Equal([(1, 0)], CoverageCellsOf(simulation.World, ownerA));
        Assert.Equal([(0, 0)], CoverageCellsOf(simulation.World, ownerB));

        simulation.World.SetCoverageOwner(1, 0, ownerEntityId: 0);

        Assert.Equal(0, simulation.World.CoverageOwnerAt(1, 0));
        Assert.False(simulation.World.CoverageOwnerHasCells(ownerA));
    }

    [Fact]
    public void CoverageLayerIteratesOwnerCellsAfterPageBoundaryRemoval()
    {
        var simulation = CreateSimulation();
        var ownerA = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var ownerB = simulation.CreateEntity(1, 1, 1, 1, 0, 0, 0, 0, 0);
        for (var i = 0; i < 20; i++)
        {
            simulation.World.MaterializeCell(i, -i, terrainId: 1, ownerA);
        }

        simulation.World.SetCoverageOwner(0, 0, ownerB);

        var ownerACells = CoverageCellsOf(simulation.World, ownerA);
        Assert.Equal(19, ownerACells.Length);
        Assert.DoesNotContain((0, 0), ownerACells);
        Assert.Equal([(0, 0)], CoverageCellsOf(simulation.World, ownerB));
    }

    [Fact]
    public void MapLayerSetStoresExclusiveCoverageAndOverlappingCultureInfluence()
    {
        var cells = new MapCellRegistry(capacity: 16);
        var layers = new MapLayerSet(cells, entityCapacity: 4, cultureIdCapacity: 4, mapInfluenceCapacity: 16);
        var cellId = cells.GetOrAdd(PlaneCodes.Physical, q: 0, r: 0, z: 0);

        layers.Coverage.SetValue(cellId, value: 1);
        layers.RawCultureInfluence.SetValue(
            cultureId: 1,
            cellId,
            value: KernelFixedPoint.CultureMassUnits(40f));
        layers.RawCultureInfluence.SetValue(
            cultureId: 2,
            cellId,
            value: KernelFixedPoint.CultureMassUnits(60f));

        Assert.Equal(1, layers.Coverage.ValueAt(cellId));
        Assert.Equal(KernelFixedPoint.CultureMassUnits(40f), layers.RawCultureInfluence.ValueAt(cultureId: 1, cellId));
        Assert.Equal(KernelFixedPoint.CultureMassUnits(60f), layers.RawCultureInfluence.ValueAt(cultureId: 2, cellId));

        layers.Coverage.SetValue(cellId, value: 2);

        Assert.Equal(2, layers.Coverage.ValueAt(cellId));
        Assert.Equal(0, layers.Coverage.ValueCellCount(1, PlaneCodes.Physical));
        Assert.Equal(1, layers.Coverage.ValueCellCount(2, PlaneCodes.Physical));
        Assert.Equal(KernelFixedPoint.CultureMassUnits(40f), layers.RawCultureInfluence.ValueAt(cultureId: 1, cellId));
        Assert.Equal(KernelFixedPoint.CultureMassUnits(60f), layers.RawCultureInfluence.ValueAt(cultureId: 2, cellId));
    }

    [Fact]
    public void MapLayerSetSnapshotRestoresCoverageAndCultureInfluenceTogether()
    {
        var cells = new MapCellRegistry(capacity: 16);
        var loadedCells = new MapCellRegistry(capacity: 16);
        var layers = new MapLayerSet(cells, entityCapacity: 4, cultureIdCapacity: 4, mapInfluenceCapacity: 16);
        var loaded = new MapLayerSet(loadedCells, entityCapacity: 4, cultureIdCapacity: 4, mapInfluenceCapacity: 16);
        var cellId = cells.GetOrAdd(PlaneCodes.Semantic, q: 1, r: -1, z: 0);
        var loadedCellId = loadedCells.GetOrAdd(PlaneCodes.Semantic, q: 1, r: -1, z: 0);
        Assert.Equal(cellId, loadedCellId);
        layers.Coverage.SetValue(cellId, value: 3);
        layers.RawCultureInfluence.SetValue(
            cultureId: 3,
            cellId,
            value: KernelFixedPoint.CultureMassUnits(75f));

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            layers.WriteSnapshot(writer);
        }

        stream.Position = 0;
        using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            loaded.ReadSnapshot(reader);
        }

        Assert.Equal(3, loaded.Coverage.ValueAt(loadedCellId));
        Assert.Equal(KernelFixedPoint.CultureMassUnits(75f), loaded.RawCultureInfluence.ValueAt(cultureId: 3, loadedCellId));
        Assert.Equal([(1, -1, 75f)], InfluenceCellsOf(loaded.RawCultureInfluence, entityId: 3, PlaneCodes.Semantic));
    }

    [Fact]
    public void MaintainsCultureInfluenceLayerReverseIndexByCultureAndPlane()
    {
        var simulation = CreateSimulation();
        simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(40f), tick: 0);
        simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 1, 0, FixedScalar.FromSingle(55f), tick: 0);
        simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Semantic, 2, -1, FixedScalar.FromSingle(65f), tick: 0);
        simulation.ModifyCellCultureInfluenceRaw(2, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(75f), tick: 0);

        Assert.Equal(2, simulation.World.CultureInfluenceCellCount(1, PlaneCodes.Physical));
        var physicalCultureOne = CultureInfluenceCellsOf(simulation.World, 1, PlaneCodes.Physical);
        Assert.Equal(2, physicalCultureOne.Length);
        Assert.Contains(physicalCultureOne, cell => cell is { Q: 0, R: 0 } && cell.Value > 34f && cell.Value < 35f);
        Assert.Contains(physicalCultureOne, cell => cell is { Q: 1, R: 0 } && cell.Value == 100f);
        Assert.Equal([(2, -1, 100f)], CultureInfluenceCellsOf(simulation.World, 1, PlaneCodes.Semantic));
        var physicalCultureTwo = Assert.Single(CultureInfluenceCellsOf(simulation.World, 2, PlaneCodes.Physical));
        Assert.Equal((0, 0), (physicalCultureTwo.Q, physicalCultureTwo.R));
        Assert.True(physicalCultureTwo.Value > 65f && physicalCultureTwo.Value < 66f);

        simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(-40f), tick: 1);

        Assert.Equal(1, simulation.World.CultureInfluenceCellCount(1, PlaneCodes.Physical));
        Assert.Equal([(1, 0, 100f)], CultureInfluenceCellsOf(simulation.World, 1, PlaneCodes.Physical));
        Assert.Equal(FixedScalar.FromSingle(0f), simulation.World.GetCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void CultureInfluenceLayerIteratesCulturePlaneCellsAfterPageBoundaryRemoval()
    {
        var simulation = CreateSimulation();
        for (var i = 0; i < 20; i++)
        {
            simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, i, -i, FixedScalar.FromSingle(25f) + i, tick: 0);
        }

        simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(-25f), tick: 1);

        var cells = CultureInfluenceCellsOf(simulation.World, 1, PlaneCodes.Physical);
        Assert.Equal(19, cells.Length);
        Assert.DoesNotContain(cells, cell => cell is { Q: 0, R: 0 });
        Assert.Contains((19, -19, 100f), cells);
    }

    [Fact]
    public void CultureInfluenceRollupNormalizesCompetingEntityInfluence()
    {
        var simulation = CreateSimulation();
        var auroraA = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var auroraB = simulation.CreateEntity(1, 1, 1, 1, 0, 0, 0, 0, 0);
        var wake = simulation.CreateEntity(1, 1, 2, 0, 1, 0, 0, 0, 0);

        simulation.SetCellInfluence(auroraA, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);
        simulation.SetCellInfluence(wake, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);

        Assert.Equal(FixedScalar.FromSingle(50f), simulation.World.GetCellCultureInfluence(1, PlaneCodes.Physical, 0, 0));
        Assert.Equal(FixedScalar.FromSingle(50f), simulation.World.GetCellCultureInfluence(2, PlaneCodes.Physical, 0, 0));

        simulation.SetCellInfluence(auroraB, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);

        Assert.Equal(FixedScalar.FromRatio(200, 3), simulation.World.GetCellCultureInfluence(1, PlaneCodes.Physical, 0, 0));
        Assert.Equal(FixedScalar.FromRatio(100, 3), simulation.World.GetCellCultureInfluence(2, PlaneCodes.Physical, 0, 0));
        var cells = CultureInfluenceCellsOf(simulation.World, 1, PlaneCodes.Physical);
        var cell = Assert.Single(cells);
        Assert.Equal((0, 0), (cell.Q, cell.R));
        Assert.Equal(66.667f, cell.Value, precision: 3);
    }

    [Fact]
    public void CultureInfluenceTracksDominanceSeparatelyFromRawMaturity()
    {
        var simulation = CreateSimulation();
        var aurora = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);

        simulation.SetCellInfluence(aurora, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(0.6f), tick: 0);

        Assert.Equal(FixedScalar.FromSingle(100f), simulation.World.GetCellCultureInfluence(1, PlaneCodes.Physical, 0, 0));
        Assert.Equal(
            FixedScalar.FromSingle(0.6f) * FixedScalar.FromSingle(100f) / WorldState.CultureFullRawMass,
            simulation.World.GetCellCultureMaturity(1, PlaneCodes.Physical, 0, 0));

        simulation.SetCellInfluence(aurora, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(6f), tick: 1);

        var influence = simulation.World.GetCellCultureInfluence(1, PlaneCodes.Physical, 0, 0);
        Assert.Equal(FixedScalar.FromSingle(100f), influence);
        Assert.Equal(
            FixedScalar.FromSingle(6f) * FixedScalar.FromSingle(100f) / WorldState.CultureFullRawMass,
            simulation.World.GetCellCultureMaturity(1, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void CultureFieldQuery_ClassifiesInteriorBorderIsolatedAndContestedCells()
    {
        var simulation = CreateSimulation();
        simulation.World.SetMapRadius(3);
        var aurora = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var rival = simulation.CreateEntity(1, 1, 2, 1, 0, 0, 0, 0, 0);

        foreach (var (q, r) in new[] { (0, 0), (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) })
        {
            simulation.SetCellInfluence(aurora, PlaneCodes.Physical, q, r, FixedScalar.FromSingle(100f), tick: 0);
        }

        var interior = simulation.World.CultureFieldAt(1, PlaneCodes.Physical, 0, 0);
        simulation.SetCellInfluence(aurora, PlaneCodes.Physical, 3, -3, FixedScalar.FromSingle(100f), tick: 0);
        simulation.SetCellInfluence(rival, PlaneCodes.Physical, 1, 0, FixedScalar.FromSingle(20f), tick: 0);
        simulation.SetCellInfluence(rival, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);

        var border = simulation.World.CultureFieldAt(1, PlaneCodes.Physical, 1, 0);
        var isolated = simulation.World.CultureFieldAt(1, PlaneCodes.Physical, 3, -3);
        var contestedInterior = simulation.World.CultureFieldAt(1, PlaneCodes.Physical, 0, 0);
        var contested = simulation.World.CultureFieldAt(2, PlaneCodes.Physical, 0, 0);

        Assert.Equal(CultureFieldCellKindCodes.Interior, interior.CellKindCode);
        Assert.Equal(CultureFieldCellKindCodes.Contested, contestedInterior.CellKindCode);
        Assert.Equal(CultureFieldCellKindCodes.Border, border.CellKindCode);
        Assert.Equal(CultureFieldCellKindCodes.Isolated, isolated.CellKindCode);
        Assert.Equal(CultureFieldCellKindCodes.Contested, contested.CellKindCode);
        Assert.True(border.ConnectedRawMass > border.LocalRawMass);
        Assert.True(contested.ForeignShare > FixedScalar.FromSingle(0f));
    }

    [Fact]
    public void CultureFieldQuery_UsesCurrentLocalityFacts()
    {
        var simulation = CreateSimulation();
        var aurora = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.SetCellInfluence(aurora, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);

        var field = simulation.World.CultureFieldAt(1, PlaneCodes.Physical, 0, 0);
        var locality = simulation.CultureLocality.CellAt(
            simulation.World,
            1,
            PlaneCodes.Physical,
            0,
            0);

        Assert.Equal(locality.RawMass, field.RawMass);
        Assert.Equal(locality.TotalRawMass, field.TotalRawMass);
        Assert.Equal(locality.ForeignRawMass, field.ForeignRawMass);
        Assert.Equal(locality.ConnectedRawMass, field.ConnectedRawMass);
        Assert.Equal(locality.CellKindCode, field.CellKindCode);
    }

    [Fact]
    public void CultureFieldQuery_SupportsCultureIdsAboveEntityCapacity()
    {
        var simulation = CreateSimulation();
        var cultureId = simulation.World.EntityCapacity + 10;
        var aurora = simulation.CreateEntity(1, 1, cultureId, 0, 0, 0, 0, 0, 0);
        simulation.SetCellInfluence(aurora, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);

        var field = simulation.World.CultureFieldAt(cultureId, PlaneCodes.Physical, 0, 0);

        Assert.Equal(FixedScalar.FromSingle(100f), field.RawMass);
        Assert.True(field.ConnectedRawMass >= field.RawMass);
    }

    [Fact]
    public void CultureSlotScoringPrefersCandidatesInsideStrongLocalCultureField()
    {
        var simulation = CreateSimulation();
        simulation.World.SetMapRadius(3);
        var unsupported = simulation.CreateEntity(1, 1, 1, -3, 0, 0, 0, 0, 0);
        var supported = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var fieldMaker = simulation.CreateEntity(2, 1, 1, 0, 0, 0, 0, 0, 0);
        foreach (var (q, r) in new[] { (0, 0), (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) })
        {
            simulation.SetCellInfluence(fieldMaker, PlaneCodes.Physical, q, r, FixedScalar.FromSingle(100f), tick: 0);
        }

        var slot = simulation.CultureSlots.RegisterSlot(
            cultureId: 1,
            slotId: 1,
            polarityCode: RelationshipPolarityCodes.Positive,
            minimumScore: FixedScalar.FromSingle(1f),
            minimumTenureTicks: 0,
            displacementMargin: FixedScalar.FromSingle(0f),
            entityInfluenceMultiplierDelta: FixedScalar.FromSingle(0f),
            cultureInfluenceMultiplierDelta: FixedScalar.FromSingle(0f));
        simulation.CultureSlots.AddAllowedKind(slot, 1);
        simulation.CultureSlots.AddScoreRule(slot, CultureSlotScoreSignalCodes.ActiveEntity, relationshipKindId: 0, weight: FixedScalar.FromSingle(1f));
        simulation.CultureSlots.AddScoreRule(slot, CultureSlotScoreSignalCodes.PhysicalCultureSupport, relationshipKindId: 0, weight: FixedScalar.FromSingle(1f));

        simulation.AdvanceTicks(10);

        Assert.NotEqual(unsupported, simulation.CultureSlots.OccupantEntityId(1, slotId: 1));
        Assert.Equal(supported, simulation.CultureSlots.OccupantEntityId(1, slotId: 1));
    }

    [Fact]
    public void CultureSlotScoringReadsSupportFromLocalInteriorHoles()
    {
        var simulation = CreateSimulation();
        simulation.World.SetMapRadius(3);
        var unsupported = simulation.CreateEntity(1, 1, 1, -3, 0, 0, 0, 0, 0);
        var supportedHole = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        foreach (var (q, r) in NeighborCells(0, 0))
        {
            simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, q, r, WorldState.CultureControlledRawMass, tick: 0);
        }

        var locality = simulation.CultureLocality.CellAt(simulation.World, 1, PlaneCodes.Physical, 0, 0);
        Assert.True(locality.IsInteriorHole);
        Assert.Equal(FixedScalar.FromSingle(0f), locality.RawMass);
        Assert.True(locality.ConnectedRawMass > FixedScalar.FromSingle(0f));

        var slot = simulation.CultureSlots.RegisterSlot(
            cultureId: 1,
            slotId: 1,
            polarityCode: RelationshipPolarityCodes.Positive,
            minimumScore: FixedScalar.FromSingle(1f),
            minimumTenureTicks: 0,
            displacementMargin: FixedScalar.FromSingle(0f),
            entityInfluenceMultiplierDelta: FixedScalar.FromSingle(0f),
            cultureInfluenceMultiplierDelta: FixedScalar.FromSingle(0f));
        simulation.CultureSlots.AddAllowedKind(slot, 1);
        simulation.CultureSlots.AddScoreRule(slot, CultureSlotScoreSignalCodes.ActiveEntity, relationshipKindId: 0, weight: FixedScalar.FromSingle(1f));
        simulation.CultureSlots.AddScoreRule(slot, CultureSlotScoreSignalCodes.PhysicalCultureSupport, relationshipKindId: 0, weight: FixedScalar.FromSingle(1f));

        simulation.AdvanceTicks(10);

        Assert.NotEqual(unsupported, simulation.CultureSlots.OccupantEntityId(1, slotId: 1));
        Assert.Equal(supportedHole, simulation.CultureSlots.OccupantEntityId(1, slotId: 1));
    }

    [Fact]
    public void CultureSlotScoringUsesInfluenceProductionHistoryNotOnlyCurrentInfluence()
    {
        var simulation = CreateSimulation();
        var quiet = simulation.CreateEntity(1, 1, 1, -1, 0, 0, 0, 0, 0);
        var producer = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);

        simulation.SetCellInfluence(producer, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);
        simulation.SetCellInfluence(producer, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(0f), tick: 1);

        var slot = simulation.CultureSlots.RegisterSlot(
            cultureId: 1,
            slotId: 1,
            polarityCode: RelationshipPolarityCodes.Positive,
            minimumScore: FixedScalar.FromSingle(1f),
            minimumTenureTicks: 0,
            displacementMargin: FixedScalar.FromSingle(0f),
            entityInfluenceMultiplierDelta: FixedScalar.FromSingle(0f),
            cultureInfluenceMultiplierDelta: FixedScalar.FromSingle(0f));
        simulation.CultureSlots.AddAllowedKind(slot, 1);
        simulation.CultureSlots.AddScoreRule(slot, CultureSlotScoreSignalCodes.ActiveEntity, relationshipKindId: 0, weight: FixedScalar.FromSingle(1f));
        simulation.CultureSlots.AddScoreRule(slot, CultureSlotScoreSignalCodes.PhysicalInfluenceProduction, relationshipKindId: 0, weight: FixedScalar.FromSingle(0.1f));

        simulation.AdvanceTicks(10);

        Assert.Equal(FixedScalar.FromSingle(0f), simulation.World.GetCellInfluence(producer, PlaneCodes.Physical, 0, 0));
        Assert.True(simulation.World.EntityInfluenceProduction(producer, PlaneCodes.Physical) > FixedScalar.FromSingle(0f));
        Assert.NotEqual(quiet, simulation.CultureSlots.OccupantEntityId(1, slotId: 1));
        Assert.Equal(producer, simulation.CultureSlots.OccupantEntityId(1, slotId: 1));
    }

    [Fact]
    public void CultureSlotBenefitDoesNotReweightExistingCultureInfluence()
    {
        var simulation = CreateSimulation();
        var aurora = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var wake = simulation.CreateEntity(1, 1, 2, 0, 1, 0, 0, 0, 0);
        simulation.SetCellInfluence(aurora, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);
        simulation.SetCellInfluence(wake, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);
        var slot = simulation.CultureSlots.RegisterSlot(
            cultureId: 1,
            slotId: 1,
            polarityCode: RelationshipPolarityCodes.Positive,
            minimumScore: FixedScalar.FromSingle(0.5f),
            minimumTenureTicks: 0,
            displacementMargin: FixedScalar.FromSingle(0f),
            entityInfluenceMultiplierDelta: FixedScalar.FromSingle(1f),
            cultureInfluenceMultiplierDelta: FixedScalar.FromSingle(0f));
        simulation.CultureSlots.AddAllowedKind(slot, 1);
        simulation.CultureSlots.AddScoreRule(slot, CultureSlotScoreSignalCodes.ActiveEntity, relationshipKindId: 0, weight: FixedScalar.FromSingle(1f));

        simulation.AdvanceTicks(10);

        Assert.Equal(FixedScalar.FromSingle(50f), simulation.World.GetCellCultureInfluence(1, PlaneCodes.Physical, 0, 0));
        Assert.Equal(FixedScalar.FromSingle(50f), simulation.World.GetCellCultureInfluence(2, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void CultureInfluencePersistsWhenEntityBecomesHistorical()
    {
        var simulation = CreateSimulation();
        var aurora = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var wake = simulation.CreateEntity(1, 1, 2, 0, 1, 0, 0, 0, 0);
        simulation.SetCellInfluence(aurora, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);
        simulation.SetCellInfluence(wake, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);

        simulation.SetEntityStatus(wake, EntityStatusCodes.Historical, tick: 1);

        Assert.Equal(FixedScalar.FromSingle(50f), simulation.World.GetCellCultureInfluence(1, PlaneCodes.Physical, 0, 0));
        Assert.Equal(FixedScalar.FromSingle(50f), simulation.World.GetCellCultureInfluence(2, PlaneCodes.Physical, 0, 0));
        Assert.Equal(1, simulation.World.CultureInfluenceCellCount(2, PlaneCodes.Physical));
    }

    [Fact]
    public void CultureInfluenceDoesNotMoveWhenEntityCultureChanges()
    {
        var simulation = CreateSimulation();
        var place = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var rival = simulation.CreateEntity(1, 1, 2, 0, 1, 0, 0, 0, 0);
        simulation.SetCellInfluence(place, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);
        simulation.SetCellInfluence(rival, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);

        simulation.SetEntityCulture(place, 3, tick: 1);

        Assert.Equal(FixedScalar.FromSingle(50f), simulation.World.GetCellCultureInfluence(1, PlaneCodes.Physical, 0, 0));
        Assert.Equal(FixedScalar.FromSingle(50f), simulation.World.GetCellCultureInfluence(2, PlaneCodes.Physical, 0, 0));
        Assert.Equal(FixedScalar.FromSingle(0f), simulation.World.GetCellCultureInfluence(3, PlaneCodes.Physical, 0, 0));
        Assert.Equal(1, simulation.World.CultureInfluenceCellCount(1, PlaneCodes.Physical));
    }

    private static Simulation CreateSimulation()
    {
        return KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 8,
            RelationshipCapacity = 8,
            AttributeCapacity = 32,
            MapCellCapacity = 64,
            EventCapacity = 128,
            GambitCapacity = 8,
            GambitStepCapacity = 64,
            GeneratorCapacity = 4,
            GeneratorItemCapacity = 32,
            LifecycleRuleCapacity = 8,
            EntityAttributeCapacity = 8,
            EffectRuleCapacity = 8,
            RelationshipRuleCapacity = 8,
            KindIdCapacity = 8,
            TerrainIdCapacity = 8,
            RelationshipKindIdCapacity = 16,
            Seed = 1,
        });
    }

    private static int[] PhysicalEntitiesAt(WorldState world, int q, int r)
    {
        var result = new int[4];
        var count = 0;
        for (var entityId = world.FirstPhysicalEntityAt(q, r);
             entityId > 0;
             entityId = world.NextPhysicalEntityAt(entityId))
        {
            result[count++] = entityId;
        }

        return result[..count];
    }

    private static (int Q, int R)[] CoverageCellsOf(WorldState world, int ownerEntityId)
    {
        var result = new (int Q, int R)[64];
        var count = 0;
        var cursor = world.CoverageCells(ownerEntityId);
        while (world.MoveNextCoverageCell(ref cursor, out var q, out var r))
        {
            result[count++] = (q, r);
        }

        Array.Sort(result, 0, count);
        return result[..count];
    }

    private static (int Q, int R, float Value)[] InfluenceCellsOf(
        MapFixedScalarEntityLayer layer,
        int entityId,
        int planeCode)
    {
        var result = new (int Q, int R, float Value)[64];
        var count = 0;
        var cursor = layer.CellsOfValue(entityId, planeCode);
        while (layer.MoveNextCell(ref cursor, out var q, out var r, out var value))
        {
            result[count++] = (q, r, KernelFixedPoint.CultureMassValue(value));
        }

        Array.Sort(result, 0, count);
        return result[..count];
    }

    private static (int Q, int R, float Value)[] CultureInfluenceCellsOf(WorldState world, int cultureId, int planeCode)
    {
        var result = new (int Q, int R, float Value)[64];
        var count = 0;
        var cursor = world.CultureInfluenceCells(cultureId, planeCode);
        while (world.MoveNextCultureInfluenceCell(ref cursor, out var q, out var r, out var value))
        {
            result[count++] = (q, r, value.ToSingle());
        }

        Array.Sort(result, 0, count);
        return result[..count];
    }

    private static int[] OutgoingRelationshipsOfKind(WorldState world, int source, int kindId)
    {
        var result = new int[8];
        var count = 0;
        var cursor = world.OutgoingRelationshipsOfKind(source, kindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            result[count++] = relationshipId;
        }

        return result[..count];
    }

    private static int[] OutgoingRelationshipsOfKindAbove(WorldState world, int source, int kindId, float minimumStrength)
    {
        var result = new int[8];
        var count = 0;
        var cursor = world.OutgoingRelationshipsOfKindAbove(
            source,
            kindId,
            FixedScalar.FromSingle(minimumStrength));
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            result[count++] = relationshipId;
        }

        return result[..count];
    }

    private static int[] IncomingRelationshipsOfKind(WorldState world, int target, int kindId)
    {
        var result = new int[8];
        var count = 0;
        var cursor = world.IncomingRelationshipsOfKind(target, kindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            result[count++] = relationshipId;
        }

        return result[..count];
    }

    private static int[] IncomingRelationshipsOfKindAbove(WorldState world, int target, int kindId, float minimumStrength)
    {
        var result = new int[8];
        var count = 0;
        var cursor = world.IncomingRelationshipsOfKindAbove(
            target,
            kindId,
            FixedScalar.FromSingle(minimumStrength));
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            result[count++] = relationshipId;
        }

        return result[..count];
    }

    private static int FirstOutgoingByCursor(WorldState world, int source, int kindId)
    {
        var cursor = world.OutgoingRelationshipsOfKind(source, kindId);
        return world.MoveNextRelationship(ref cursor, out var relationshipId) ? relationshipId : 0;
    }

    private static int FirstIncomingByCursor(WorldState world, int target, int kindId)
    {
        var cursor = world.IncomingRelationshipsOfKind(target, kindId);
        return world.MoveNextRelationship(ref cursor, out var relationshipId) ? relationshipId : 0;
    }

    private static (int Q, int R)[] NeighborCells(int q, int r) =>
    [
        (q + 1, r),
        (q + 1, r - 1),
        (q, r - 1),
        (q - 1, r),
        (q - 1, r + 1),
        (q, r + 1),
    ];

    private struct RecordingTraversalVisitor : IRelationshipTraversalVisitor
    {
        private readonly int[] _entities;
        private readonly int[] _relationships;
        private readonly int[] _depths;

        public RecordingTraversalVisitor(int capacity)
        {
            _entities = new int[capacity];
            _relationships = new int[capacity];
            _depths = new int[capacity];
        }

        public int Count { get; private set; }

        public readonly int EntityAt(int index) => _entities[index];

        public readonly int RelationshipAt(int index) => _relationships[index];

        public readonly int DepthAt(int index) => _depths[index];

        public void Visit(int entityId, int relationshipId, int depth)
        {
            _entities[Count] = entityId;
            _relationships[Count] = relationshipId;
            _depths[Count] = depth;
            Count++;
        }
    }
}
