namespace Engine.Kernel.Tests;

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

        var relationship = simulation.World.CreateRelationship(7, 0, first, third, 1f, 2);

        Assert.Equal(relationship, FirstOutgoingByCursor(simulation.World, first, 7));
        Assert.Equal(relationship, FirstIncomingByCursor(simulation.World, third, 7));
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
        var low = simulation.World.CreateRelationship(9, 0, source, lowTarget, 0.2f, 0);
        var high = simulation.World.CreateRelationship(9, 0, source, highTarget, 0.9f, 0);
        var mid = simulation.World.CreateRelationship(9, 0, source, midTarget, 0.5f, 0);
        var incomingLow = simulation.World.CreateRelationship(9, 0, lowSource, sink, 0.1f, 0);
        var incomingHigh = simulation.World.CreateRelationship(9, 0, highSource, sink, 0.8f, 0);
        var incomingMid = simulation.World.CreateRelationship(9, 0, midSource, sink, 0.4f, 0);

        Assert.Equal([low, high, mid], OutgoingRelationshipsOfKind(simulation.World, source, 9));
        Assert.Equal([incomingLow, incomingHigh, incomingMid], IncomingRelationshipsOfKind(simulation.World, sink, 9));

        simulation.World.CompactRelationshipGraph();

        Assert.Equal([low, high, mid], OutgoingRelationshipsOfKind(simulation.World, source, 9));
        Assert.Equal([incomingLow, incomingHigh, incomingMid], IncomingRelationshipsOfKind(simulation.World, sink, 9));
        Assert.Equal([high, mid], OutgoingRelationshipsOfKindAbove(simulation.World, source, 9, 0.5f));
        Assert.Equal([incomingHigh], IncomingRelationshipsOfKindAbove(simulation.World, sink, 9, 0.5f));

        simulation.World.SetRelationshipStrength(low, 0.95f, 1);
        simulation.World.SetRelationshipStrength(incomingLow, 0.85f, 1);

        Assert.Equal([low, high, mid], OutgoingRelationshipsOfKind(simulation.World, source, 9));
        Assert.Equal([incomingLow, incomingHigh, incomingMid], IncomingRelationshipsOfKind(simulation.World, sink, 9));

        simulation.World.CullRelationship(high, 2);
        simulation.World.CullRelationship(incomingHigh, 2);

        Assert.Equal([low, mid], OutgoingRelationshipsOfKind(simulation.World, source, 9));
        Assert.Equal([incomingLow, incomingMid], IncomingRelationshipsOfKind(simulation.World, sink, 9));
    }

    [Fact]
    public void TraversesTypedRelationshipCsrWithStrengthThreshold()
    {
        var simulation = CreateSimulation();
        var first = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var second = simulation.CreateEntity(1, 1, 1, 1, 0, 0, 0, 0, 0);
        var third = simulation.CreateEntity(1, 1, 1, 2, 0, 0, 0, 0, 0);
        var fourth = simulation.CreateEntity(1, 1, 1, 3, 0, 0, 0, 0, 0);
        var firstToSecond = simulation.World.CreateRelationship(9, 0, first, second, 0.9f, 0);
        var firstToThird = simulation.World.CreateRelationship(9, 0, first, third, 0.6f, 0);
        var secondToFourth = simulation.World.CreateRelationship(9, 0, second, fourth, 0.7f, 0);
        simulation.World.CreateRelationship(9, 0, third, fourth, 0.4f, 0);
        simulation.World.CreateRelationship(10, 0, first, fourth, 1f, 0);
        simulation.World.CompactRelationshipGraph();

        var outgoing = new RecordingTraversalVisitor(8);
        var outgoingCount = simulation.World.TraverseOutgoingRelationshipsOfKind(first, 9, 0.5f, 2, ref outgoing);

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
        var incomingCount = simulation.World.TraverseIncomingRelationshipsOfKind(fourth, 9, 0.5f, 2, ref incoming);

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
    public void MapLayerSetStoresExclusiveCoverageAndOverlappingInfluence()
    {
        var layers = new MapLayerSet(entityCapacity: 4, mapCellCapacity: 16, mapInfluenceCapacity: 16);
        const int cellKey = 1;

        layers.Coverage.SetValue(cellKey, 0, 0, value: 1);
        layers.Influence.SetValue(entityId: 1, PlaneCodes.Physical, cellKey, value: 40f);
        layers.Influence.SetValue(entityId: 2, PlaneCodes.Physical, cellKey, value: 60f);

        Assert.Equal(1, layers.Coverage.ValueAt(cellKey));
        Assert.Equal(40f, layers.Influence.ValueAt(entityId: 1, PlaneCodes.Physical, cellKey));
        Assert.Equal(60f, layers.Influence.ValueAt(entityId: 2, PlaneCodes.Physical, cellKey));
        Assert.Equal(0f, layers.Influence.ValueAt(entityId: 1, PlaneCodes.Semantic, cellKey));

        layers.Coverage.SetValue(cellKey, 0, 0, value: 2);

        Assert.Equal(2, layers.Coverage.ValueAt(cellKey));
        Assert.Equal(0, layers.Coverage.ValueCellCount(1));
        Assert.Equal(1, layers.Coverage.ValueCellCount(2));
        Assert.Equal(40f, layers.Influence.ValueAt(entityId: 1, PlaneCodes.Physical, cellKey));
        Assert.Equal(60f, layers.Influence.ValueAt(entityId: 2, PlaneCodes.Physical, cellKey));
    }

    [Fact]
    public void MapLayerSetSnapshotRestoresCoverageAndInfluenceTogether()
    {
        var layers = new MapLayerSet(entityCapacity: 4, mapCellCapacity: 16, mapInfluenceCapacity: 16);
        var loaded = new MapLayerSet(entityCapacity: 4, mapCellCapacity: 16, mapInfluenceCapacity: 16);
        const int cellKey = 2;
        layers.Coverage.SetValue(cellKey, 1, -1, value: 3);
        layers.Influence.SetValue(entityId: 3, PlaneCodes.Semantic, cellKey, value: 75f);

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

        Assert.Equal(3, loaded.Coverage.ValueAt(cellKey));
        Assert.Equal(75f, loaded.Influence.ValueAt(entityId: 3, PlaneCodes.Semantic, cellKey));
    }

    private static Simulation CreateSimulation()
    {
        return new Simulation(new SimulationOptions
        {
            EntityCapacity = 8,
            RelationshipCapacity = 8,
            AttributeCapacity = 32,
            ResourceCapacity = 32,
            MapCellCapacity = 64,
            EventCapacity = 128,
            ProcessCapacity = 8,
            ProcessStepCapacity = 64,
            ProcessCooldownCapacity = 32,
            GeneratorCapacity = 4,
            GeneratorItemCapacity = 32,
            ProcessObjectiveCapacity = 16,
            PressureCapacity = 4,
            PressureValueCapacity = 16,
            DegeneracyDetectorCapacity = 4,
            DegeneracyStateCapacity = 16,
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
        var cursor = world.OutgoingRelationshipsOfKindAbove(source, kindId, minimumStrength);
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
        var cursor = world.IncomingRelationshipsOfKindAbove(target, kindId, minimumStrength);
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
