namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;
using Engine.Kernel.Programs;

public sealed class GeneratorProgramRuntimeTests
{
    private const int TargetKindId = 1;
    private const int CreatedKindId = 2;
    private const int TargetSubtypeId = 10;
    private const int CreatedSubtypeId = 11;
    private const int CultureId = 20;
    private const int TriggerAttributeId = 30;
    private const int CreatedAttributeId = 31;
    private const int RelationshipKindId = 40;
    private const int GeneratorId = 100;
    private const int CreatedRef = 2;

    [Fact]
    public void Tick_BeforeCadence_DoesNotFire()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        CreateTarget(simulation);
        RegisterGenerator(simulation, cadence: 5);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(4);

        Assert.Equal(1, simulation.World.EntityHighWatermark);
        Assert.DoesNotContain(ReadAvailableEventTypes(simulation, cursor), type => type == KernelEventType.GeneratorFired);
    }

    [Fact]
    public void DisabledGenerator_DoesNotFireUntilEnabledAgain()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        CreateTarget(simulation);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.DisableGenerator(slot);

        simulation.AdvanceTicks(1);
        Assert.Equal(1, simulation.World.EntityHighWatermark);

        simulation.Generators.EnableGenerator(slot);
        simulation.AdvanceTicks(1);

        Assert.Equal(2, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void MaxRuns_IsRespected()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        CreateTarget(simulation);
        RegisterGenerator(simulation, maxRuns: 1);

        simulation.AdvanceTicks(3);

        Assert.Equal(2, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void TriggerBlocksGeneratorUntilThresholdPasses()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.RegisterAttribute(TargetKindId, TriggerAttributeId, 0f, 100f, 0f);
        var target = CreateTarget(simulation);
        var slot = simulation.Generators.RegisterGenerator(
            GeneratorId,
            cadence: 1,
            TargetKindId,
            pulseWeight: 1,
            maxRuns: 0,
            targetAllowWilderness: false);
        simulation.Generators.AddTrigger(slot, TargetKindId, TriggerAttributeId, GeneratorTriggerDirectionCodes.High, threshold: 50f);
        AddProducedEntity(simulation, slot, createChance: 1f);

        simulation.AdvanceTicks(1);
        Assert.Equal(1, simulation.World.EntityHighWatermark);

        using (simulation.EnterExecutionContext(simulation.ProcessSourceCodeId, programId: 0))
        {
            simulation.SetAttribute(target, TriggerAttributeId, 75f, tick: 1);
        }
        simulation.AdvanceTicks(1);

        Assert.Equal(2, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void Fire_CreatesEntityAndRelationshipAndEmitsEvents()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        RegisterGenerator(simulation);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.NotEqual(target, created);
        Assert.Equal(CreatedKindId, simulation.World.EntityKindId(created));
        Assert.Equal(CultureId, simulation.World.EntityCultureId(created));
        Assert.True(simulation.World.FindRelationship(created, target, RelationshipKindId) > 0);

        var eventTypes = ReadAvailableEventTypes(simulation, cursor);
        Assert.Contains(KernelEventType.EntityCreated, eventTypes);
        Assert.Contains(KernelEventType.RelationshipChanged, eventTypes);
        Assert.Contains(KernelEventType.GeneratorFired, eventTypes);
    }

    [Fact]
    public void Fire_AttributesCreatedEntityAndRelationshipEventsToGeneratorContext()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.RegisterAttribute(CreatedKindId, CreatedAttributeId, 0f, 100f, 12f);
        var target = CreateTarget(simulation);
        RegisterGenerator(simulation);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.NotEqual(target, created);

        var sawCreated = false;
        var sawAttribute = false;
        var sawRelationship = false;
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType == KernelEventType.EntityCreated)
            {
                var createdEvent = read.Event.AsEntityCreated();
                if (createdEvent.EntityId == created)
                {
                    Assert.Equal(simulation.GeneratorSourceCodeId, createdEvent.SourceCodeId);
                    sawCreated = true;
                }
            }
            else if (read.Event.EventType == KernelEventType.AttributeChanged)
            {
                var attribute = read.Event.AsAttributeChanged();
                if (attribute.EntityId == created && attribute.AttributeId == CreatedAttributeId)
                {
                    Assert.Equal(simulation.GeneratorSourceCodeId, attribute.SourceCodeId);
                    sawAttribute = true;
                }
            }
            else if (read.Event.EventType == KernelEventType.RelationshipChanged)
            {
                var relationship = read.Event.AsRelationshipChanged();
                if (relationship.SourceEntityId == created && relationship.TargetEntityId == target)
                {
                    Assert.Equal(simulation.GeneratorSourceCodeId, relationship.SourceCodeId);
                    sawRelationship = true;
                }
            }
        }

        Assert.True(sawCreated);
        Assert.True(sawAttribute);
        Assert.True(sawRelationship);
    }

    [Fact]
    public void ProducedCreateChanceZero_SkipsOptionalEntity()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        CreateTarget(simulation);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        AddProducedEntity(simulation, slot, createChance: 0f);

        simulation.AdvanceTicks(1);

        Assert.Equal(1, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void SetGeneratorEnabledById_TogglesIndividualGenerator()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        CreateTarget(simulation);
        RegisterGenerator(simulation);

        Assert.True(simulation.Generators.TrySetGeneratorEnabledById(GeneratorId, enabled: false));
        Assert.True(simulation.Generators.TryGetGeneratorEnabledById(GeneratorId, out var enabled));
        Assert.False(enabled);
        simulation.AdvanceTicks(1);
        Assert.Equal(1, simulation.World.EntityHighWatermark);

        Assert.True(simulation.Generators.TrySetGeneratorEnabledById(GeneratorId, enabled: true));
        simulation.AdvanceTicks(1);
        Assert.Equal(2, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void ProgramCanBeDisabledAsAWhole()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        CreateTarget(simulation);
        RegisterGenerator(simulation);
        simulation.SetProgramEnabled(KernelProgramCodes.Generators, enabled: false);

        simulation.AdvanceTicks(1);

        Assert.Equal(1, simulation.World.EntityHighWatermark);
    }

    private static int RegisterGenerator(
        Simulation simulation,
        int cadence = 1,
        int maxRuns = 0,
        bool includeProduced = true)
    {
        var slot = simulation.Generators.RegisterGenerator(
            GeneratorId,
            cadence,
            TargetKindId,
            pulseWeight: 1,
            maxRuns,
            targetAllowWilderness: false);
        simulation.Generators.AddTrigger(slot, TargetKindId, attributeId: 0, GeneratorTriggerDirectionCodes.Any, threshold: 0f);
        if (includeProduced)
        {
            AddProducedEntity(simulation, slot, createChance: 1f);
        }

        return slot;
    }

    private static void AddProducedEntity(Simulation simulation, int generatorSlot, float createChance)
    {
        simulation.Generators.AddProduced(
            generatorSlot,
            CreatedRef,
            CreatedKindId,
            CreatedSubtypeId,
            createIfMissing: 1,
            hasQuery: 0,
            queryAdjacentToTarget: 0,
            queryCultureMatchCode: GeneratorCultureMatchCodes.Any,
            physicalPlacementCode: GeneratorPlacementCodes.Near,
            semanticPlacementCode: GeneratorPlacementCodes.Near,
            placementRefCode: GeneratorProgramConstants.TargetReferenceCode,
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            createChance,
            coverageModeCode: GeneratorCoverageModeCodes.None,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: 0,
            coverageMinimumCellCount: 0,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 0,
            coverageTransformTerrainId: 0);
        simulation.Generators.AddRelationship(
            generatorSlot,
            CreatedRef,
            GeneratorProgramConstants.TargetReferenceCode,
            RelationshipKindId,
            strength: 0.75f,
            replaceExistingOutgoing: 0);
    }

    private static int CreateTarget(Simulation simulation) =>
        simulation.CreateEntity(TargetKindId, TargetSubtypeId, CultureId, 0, 0, 0, 0, 0, tick: 0);

    private static void SeedMap(Simulation simulation)
    {
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        for (var q = -2; q <= 2; q++)
        {
            var minR = Math.Max(-2, -q - 2);
            var maxR = Math.Min(2, -q + 2);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.CreateCell(q, r, terrainId: 1, ownerEntityId: 0);
            }
        }
    }

    private static KernelEventType[] ReadAvailableEventTypes(Simulation simulation, KernelEventCursor cursor)
    {
        var result = new List<KernelEventType>();
        while (simulation.Events.TryRead(cursor, out var read))
        {
            result.Add(read.Event.EventType);
        }

        return result.ToArray();
    }

    private static Simulation CreateSimulation() => new(new SimulationOptions
    {
        EntityCapacity = 16,
        RelationshipCapacity = 16,
        AttributeCapacity = 32,
        ResourceCapacity = 16,
        MapCellCapacity = 128,
        EventCapacity = 512,
        GeneratorCapacity = 4,
        GeneratorItemCapacity = 64,
        RelationshipKindIdCapacity = 64,
        TerrainIdCapacity = 8,
        SystemCapacity = 16,
        Seed = 1,
    });
}
