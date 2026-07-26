namespace Engine.Eventing.Tests;

using Engine.Eventing;
using Engine.Eventing.Schema;
using Engine.Kernel;

public sealed class KernelStartupSeedingTests
{
    [Fact]
    public void CreateEntityAndSeedEntityUseSameMaterializationSequence()
    {
        const int entityId = 7;
        const int kindId = 1;
        const int subtypeId = 2;
        const int cultureId = 3;
        const int attributeId = 4;

        var created = CreateSimulationWithDefaultAttribute(kindId, attributeId);
        var createdEntityId = created.CreateEntity(
            kindId,
            subtypeId,
            cultureId,
            physicalQ: 0,
            physicalR: 0,
            semanticQ: 0,
            semanticR: 0,
            semanticS: 0,
            tick: 0);

        var seeded = CreateSimulationWithDefaultAttribute(kindId, attributeId);
        seeded.SeedEntity(
            entityId,
            kindId,
            subtypeId,
            cultureId,
            physicalQ: 0,
            physicalR: 0,
            semanticQ: 0,
            semanticR: 0,
            semanticS: 0);

        Assert.Equal(FixedScalar.FromSingle(42f), created.World.GetAttribute(createdEntityId, attributeId));
        Assert.Equal(FixedScalar.FromSingle(42f), seeded.World.GetAttribute(entityId, attributeId));
        AssertEventTypes(
            created,
            KernelEventType.EntityCreated,
            KernelEventType.AttributeChanged);
        AssertEventTypes(
            seeded,
            KernelEventType.EntityCreated,
            KernelEventType.AttributeChanged);
    }

    [Fact]
    public void CreateCellAndSeedCellUseSameMaterializationSequence()
    {
        var created = CreateSimulationWithTerrain();
        var seeded = CreateSimulationWithTerrain();
        created.SeedEntity(
            entityId: 3,
            kindId: 1,
            subtypeId: 1,
            cultureId: 1,
            physicalQ: 0,
            physicalR: 0,
            semanticQ: 0,
            semanticR: 0,
            semanticS: 0);
        seeded.SeedEntity(
            entityId: 3,
            kindId: 1,
            subtypeId: 1,
            cultureId: 1,
            physicalQ: 0,
            physicalR: 0,
            semanticQ: 0,
            semanticR: 0,
            semanticS: 0);

        using var createdCursor = created.Events.CreateCursor(KernelEventCursorStart.Latest);
        using var seededCursor = seeded.Events.CreateCursor(KernelEventCursorStart.Latest);

        created.CreateCell(q: 1, r: -1, terrainId: 1, ownerEntityId: 3);
        seeded.SeedCell(q: 1, r: -1, terrainId: 1, ownerEntityId: 3);

        Assert.Equal(created.World.CellTerrainId(1, -1), seeded.World.CellTerrainId(1, -1));
        Assert.Equal(created.World.CoverageOwnerAt(1, -1), seeded.World.CoverageOwnerAt(1, -1));
        AssertEventTypes(created, createdCursor, KernelEventType.MapCellStateChanged);
        AssertEventTypes(seeded, seededCursor, KernelEventType.MapCellStateChanged);
    }

    [Fact]
    public void SetAttributeAndSeedAttributeUseSameMutationSequence()
    {
        var changed = CreateSimulationWithEntity();
        var seeded = CreateSimulationWithEntity();

        using var changedCursor = changed.Events.CreateCursor(KernelEventCursorStart.Latest);
        using var seededCursor = seeded.Events.CreateCursor(KernelEventCursorStart.Latest);

        changed.SetAttribute(entityId: 1, attributeId: 4, value: FixedScalar.FromSingle(11f), tick: 0);
        seeded.SeedAttribute(entityId: 1, attributeId: 4, value: FixedScalar.FromSingle(11f));

        Assert.Equal(changed.World.GetAttribute(1, 4), seeded.World.GetAttribute(1, 4));
        AssertEventTypes(changed, changedCursor, KernelEventType.AttributeChanged);
        AssertEventTypes(seeded, seededCursor, KernelEventType.AttributeChanged);
    }

    [Fact]
    public void CreateRelationshipAndSeedRelationshipUseSameMaterializationSequence()
    {
        var created = CreateSimulationWithTwoEntities();
        var seeded = CreateSimulationWithTwoEntities();

        using var createdCursor = created.Events.CreateCursor(KernelEventCursorStart.Latest);
        using var seededCursor = seeded.Events.CreateCursor(KernelEventCursorStart.Latest);

        var createdRelationship = created.CreateRelationship(
            relationshipKindId: 7,
            polarityCode: RelationshipPolarityCodes.Negative,
            sourceEntityId: 1,
            targetEntityId: 2,
            strength: FixedScalar.FromSingle(0.5f),
            tick: 0);
        seeded.SeedRelationship(
            relationshipKindId: 7,
            polarityCode: RelationshipPolarityCodes.Negative,
            sourceEntityId: 1,
            targetEntityId: 2,
            strength: FixedScalar.FromSingle(0.5f));

        Assert.Equal(createdRelationship, seeded.World.FindRelationship(1, 2, 7));
        AssertEventTypes(created, createdCursor, KernelEventType.RelationshipChanged);
        AssertEventTypes(seeded, seededCursor, KernelEventType.RelationshipChanged);
    }

    private static void AssertEventTypes(
        Simulation simulation,
        params KernelEventType[] expected)
    {
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var actual = new List<KernelEventType>();
        while (simulation.Events.TryRead(cursor, out var sequenced))
        {
            actual.Add(sequenced.Event.EventType);
        }

        Assert.Equal(expected, actual);
    }

    private static void AssertEventTypes(
        Simulation simulation,
        KernelEventCursor cursor,
        params KernelEventType[] expected)
    {
        var actual = new List<KernelEventType>();
        while (simulation.Events.TryRead(cursor, out var sequenced))
        {
            actual.Add(sequenced.Event.EventType);
        }

        Assert.Equal(expected, actual);
    }

    private static Simulation CreateSimulationWithTerrain()
    {
        var simulation = new Simulation(new SimulationOptions
        {
            EventSchemaValidator = KernelEventSchemaRegistry.Validator,
        });
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        return simulation;
    }

    private static Simulation CreateSimulationWithEntity()
    {
        var simulation = new Simulation(new SimulationOptions
        {
            EventSchemaValidator = KernelEventSchemaRegistry.Validator,
        });
        simulation.SeedEntity(
            entityId: 1,
            kindId: 1,
            subtypeId: 1,
            cultureId: 1,
            physicalQ: 0,
            physicalR: 0,
            semanticQ: 0,
            semanticR: 0,
            semanticS: 0);
        return simulation;
    }

    private static Simulation CreateSimulationWithTwoEntities()
    {
        var simulation = CreateSimulationWithEntity();
        simulation.SeedEntity(
            entityId: 2,
            kindId: 1,
            subtypeId: 1,
            cultureId: 1,
            physicalQ: 1,
            physicalR: 0,
            semanticQ: 1,
            semanticR: 0,
            semanticS: -1);
        return simulation;
    }

    private static Simulation CreateSimulationWithDefaultAttribute(
        int kindId,
        int attributeId)
    {
        var simulation = new Simulation(new SimulationOptions
        {
            EventSchemaValidator = KernelEventSchemaRegistry.Validator,
        });
        simulation.RegisterAttribute(
            kindId,
            attributeId,
            minimum: FixedScalar.FromSingle(0f),
            maximum: FixedScalar.FromSingle(100f),
            defaultValue: FixedScalar.FromSingle(42f));
        return simulation;
    }
}
