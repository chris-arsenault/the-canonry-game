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
    public void TargetRequiredIncomingRelationship_BlocksGeneratorUntilRelationshipExists()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        var source = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 0);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetRequiredIncomingRelationship(slot, RelationshipKindId);

        simulation.AdvanceTicks(1);
        Assert.Equal(2, simulation.World.EntityHighWatermark);

        simulation.CreateRelationship(RelationshipKindId, 0, source, target, 0.8f, tick: 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(3, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void TargetRequiredRelationship_AcceptsEitherDirection()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        var destination = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 0);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetRequiredRelationship(slot, RelationshipKindId);

        simulation.AdvanceTicks(1);
        Assert.Equal(2, simulation.World.EntityHighWatermark);

        simulation.CreateRelationship(RelationshipKindId, 0, target, destination, 0.8f, tick: 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(3, simulation.World.EntityHighWatermark);
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
    public void Fire_RecordsProvenanceInvocationAndOutputs()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        RegisterGenerator(simulation);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        var relationship = simulation.World.FindRelationship(created, target, RelationshipKindId);
        var invocationId = simulation.World.EntityCreatedByInvocationId(created);
        var events = ReadAvailableEvents(simulation, cursor);
        var recorded = Assert.Single(
            events,
            evt => evt.EventType == KernelEventType.ProvenanceInvocationRecorded).AsProvenanceInvocationRecorded();

        Assert.True(invocationId > 0);
        Assert.Equal(invocationId, simulation.World.RelationshipCreatedByInvocationId(relationship));
        Assert.Equal(invocationId, recorded.InvocationId);
        Assert.Equal(ProvenanceProducerKindCodes.Generator, recorded.ProducerKindCode);
        Assert.Equal(GeneratorId, recorded.ProducerId);
        Assert.Equal(target, recorded.PrimaryEntityId);
        Assert.Equal(2, recorded.OutputCount);
        var outputKinds = ReadProvenanceOutputKinds(simulation, invocationId);
        Assert.Contains(ProvenanceOutputKindCodes.EntityCreated, outputKinds);
        Assert.Contains(ProvenanceOutputKindCodes.RelationshipChanged, outputKinds);
        Assert.Contains(events, evt =>
            evt.EventType == KernelEventType.EntityCreated &&
            evt.ProvenanceInvocationId == invocationId);
        Assert.Contains(events, evt =>
            evt.EventType == KernelEventType.RelationshipChanged &&
            evt.ProvenanceInvocationId == invocationId);
        Assert.Contains(events, evt =>
            evt.EventType == KernelEventType.GeneratorFired &&
            evt.ProvenanceInvocationId == invocationId);
    }

    [Fact]
    public void Fire_WithProducedCoverage_EmitsCellChangedForClaimedCells()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.RegisterTerrain(2, boundaryGroupId: 1);
        CreateTarget(simulation);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        simulation.Generators.AddProduced(
            slot,
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
            createChance: 1f,
            coverageModeCode: GeneratorCoverageModeCodes.CreateNew,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: 3,
            coverageMinimumCellCount: 1,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 1,
            coverageTransformTerrainId: 2);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        var cellChanges = ReadAvailableEvents(simulation, cursor)
            .Where(evt => evt.EventType == KernelEventType.CellChanged)
            .Select(evt => evt.AsCellChanged())
            .ToArray();
        Assert.NotEmpty(cellChanges);
        Assert.All(cellChanges, evt =>
        {
            Assert.Equal(simulation.GeneratorSourceCodeId, evt.SourceCodeId);
            Assert.Equal(1, evt.PreviousTerrainId);
            Assert.Equal(2, evt.CurrentTerrainId);
            Assert.Equal(0, evt.PreviousOwnerEntityId);
            Assert.Equal(created, evt.CurrentOwnerEntityId);
        });
        Assert.True(simulation.World.CoverageOwnerCellCount(created) >= 1);
    }

    [Fact]
    public void Fire_UsesConfiguredSemanticBoundsForProducedPlacement()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.World.SetMapRadius(5);
        simulation.Generators.ConfigureSemanticBounds(-1, 1, -1, 1);
        simulation.CreateEntity(TargetKindId, TargetSubtypeId, CultureId, 0, 0, 4, 0, -4, tick: 0);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        simulation.Generators.AddProduced(
            slot,
            CreatedRef,
            CreatedKindId,
            CreatedSubtypeId,
            createIfMissing: 1,
            hasQuery: 0,
            queryAdjacentToTarget: 0,
            queryCultureMatchCode: GeneratorCultureMatchCodes.Any,
            physicalPlacementCode: GeneratorPlacementCodes.Near,
            semanticPlacementCode: GeneratorPlacementCodes.On,
            placementRefCode: GeneratorProgramConstants.TargetReferenceCode,
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            createChance: 1f,
            coverageModeCode: GeneratorCoverageModeCodes.None,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: 0,
            coverageMinimumCellCount: 0,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 0,
            coverageTransformTerrainId: 0);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        var q = simulation.World.EntitySemanticQ(created);
        var r = simulation.World.EntitySemanticR(created);
        Assert.InRange(q, -1, 1);
        Assert.InRange(r, -1, 1);
        Assert.True(HexDistance(0, 0, q, r) <= 1);
    }

    [Fact]
    public void RetryFireTargets_WhenEnabled_TriesAnotherTargetAfterFireFailure()
    {
        var simulation = CreateSimulation(seed: 1, retryFireTargets: true);
        var firstTarget = CreateSparseCoverageTarget(simulation);
        var secondTarget = CreateViableCoverageTarget(simulation);
        RegisterCoverageGenerator(simulation);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var events = ReadAvailableEvents(simulation, cursor);
        var evaluated = Assert.Single(events
            .Where(evt => evt.EventType == KernelEventType.GeneratorEvaluated)
            .Select(evt => evt.AsGeneratorEvaluated()));
        Assert.Equal(GeneratorStatusCodes.Fired, evaluated.StatusCode);
        Assert.Equal(secondTarget, evaluated.SelectedTargetEntityId);
        Assert.Equal(1, evaluated.FailedFireCount);
        Assert.Contains(events, evt =>
            evt.EventType == KernelEventType.GeneratorFired &&
            evt.AsGeneratorFired().TargetEntityId == secondTarget);
        Assert.Equal(EntityStatusCodes.Historical, simulation.World.EntityStatus(firstTarget + 2));
        Assert.Equal(4, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void RetryFireTargets_WhenDisabled_StopsAfterFirstFireFailure()
    {
        var simulation = CreateSimulation(seed: 1, retryFireTargets: false);
        CreateSparseCoverageTarget(simulation);
        CreateViableCoverageTarget(simulation);
        RegisterCoverageGenerator(simulation);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var events = ReadAvailableEvents(simulation, cursor);
        var evaluated = Assert.Single(events
            .Where(evt => evt.EventType == KernelEventType.GeneratorEvaluated)
            .Select(evt => evt.AsGeneratorEvaluated()));
        Assert.Equal(GeneratorStatusCodes.FireFailed, evaluated.StatusCode);
        Assert.Equal(1, evaluated.SelectedTargetEntityId);
        Assert.Equal(1, evaluated.FailedFireCount);
        Assert.DoesNotContain(events, evt => evt.EventType == KernelEventType.GeneratorFired);
        Assert.Equal(3, simulation.World.EntityHighWatermark);
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
        foreach (var evt in ReadAvailableEvents(simulation, cursor))
        {
            sawCreated |= IsGeneratorCreatedEntityEvent(simulation, evt, created);
            sawAttribute |= IsGeneratorAttributeEvent(simulation, evt, created);
            sawRelationship |= IsGeneratorRelationshipEvent(simulation, evt, created, target);
        }

        Assert.True(sawCreated);
        Assert.True(sawAttribute);
        Assert.True(sawRelationship);
    }

    private static bool IsGeneratorCreatedEntityEvent(Simulation simulation, KernelEvent evt, int created)
    {
        if (evt.EventType != KernelEventType.EntityCreated)
        {
            return false;
        }

        var createdEvent = evt.AsEntityCreated();
        if (createdEvent.EntityId != created)
        {
            return false;
        }

        Assert.Equal(simulation.GeneratorSourceCodeId, createdEvent.SourceCodeId);
        return true;
    }

    private static bool IsGeneratorAttributeEvent(Simulation simulation, KernelEvent evt, int created)
    {
        if (evt.EventType != KernelEventType.AttributeChanged)
        {
            return false;
        }

        var attribute = evt.AsAttributeChanged();
        if (attribute.EntityId != created || attribute.AttributeId != CreatedAttributeId)
        {
            return false;
        }

        Assert.Equal(simulation.GeneratorSourceCodeId, attribute.SourceCodeId);
        return true;
    }

    private static bool IsGeneratorRelationshipEvent(
        Simulation simulation,
        KernelEvent evt,
        int created,
        int target)
    {
        if (evt.EventType != KernelEventType.RelationshipChanged)
        {
            return false;
        }

        var relationship = evt.AsRelationshipChanged();
        if (relationship.SourceEntityId != created || relationship.TargetEntityId != target)
        {
            return false;
        }

        Assert.Equal(simulation.GeneratorSourceCodeId, relationship.SourceCodeId);
        return true;
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
        simulation.Generators.AddTrigger(slot, TargetKindId);
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

    private static void RegisterCoverageGenerator(Simulation simulation)
    {
        var slot = RegisterGenerator(simulation, includeProduced: false);
        simulation.Generators.AddProduced(
            slot,
            CreatedRef,
            CreatedKindId,
            CreatedSubtypeId,
            createIfMissing: 1,
            hasQuery: 0,
            queryAdjacentToTarget: 0,
            queryCultureMatchCode: GeneratorCultureMatchCodes.Any,
            physicalPlacementCode: GeneratorPlacementCodes.On,
            semanticPlacementCode: GeneratorPlacementCodes.Near,
            placementRefCode: GeneratorProgramConstants.TargetReferenceCode,
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            createChance: 1f,
            coverageModeCode: GeneratorCoverageModeCodes.CreateNew,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: 2,
            coverageMinimumCellCount: 2,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 1,
            coverageTransformTerrainId: 0);
    }

    private static int CreateTarget(Simulation simulation) =>
        CreateTarget(simulation, physicalQ: 0, physicalR: 0);

    private static int CreateTarget(Simulation simulation, int physicalQ, int physicalR) =>
        simulation.CreateEntity(TargetKindId, TargetSubtypeId, CultureId, physicalQ, physicalR, 0, 0, 0, tick: 0);

    private static int CreateSparseCoverageTarget(Simulation simulation)
    {
        simulation.World.SetMapRadius(20);
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        simulation.CreateCell(0, 0, terrainId: 1, ownerEntityId: 0);
        return CreateTarget(simulation, physicalQ: 0, physicalR: 0);
    }

    private static int CreateViableCoverageTarget(Simulation simulation)
    {
        simulation.CreateCell(10, 0, terrainId: 1, ownerEntityId: 0);
        simulation.CreateCell(10, 1, terrainId: 1, ownerEntityId: 0);
        simulation.CreateCell(11, 0, terrainId: 1, ownerEntityId: 0);
        return CreateTarget(simulation, physicalQ: 10, physicalR: 0);
    }

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

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        return (Math.Abs(aq - bq) + Math.Abs(ar - br) + Math.Abs(asCoord - bsCoord)) / 2;
    }

    private static KernelEventType[] ReadAvailableEventTypes(Simulation simulation, KernelEventCursor cursor)
    {
        var result = ReadAvailableEvents(simulation, cursor)
            .Select(evt => evt.EventType)
            .ToArray();

        return result;
    }

    private static KernelEvent[] ReadAvailableEvents(Simulation simulation, KernelEventCursor cursor)
    {
        var result = new List<KernelEvent>();
        while (simulation.Events.TryRead(cursor, out var read))
        {
            result.Add(read.Event);
        }

        return result.ToArray();
    }

    private static int[] ReadProvenanceOutputKinds(Simulation simulation, int invocationId)
    {
        var result = new List<int>();
        var outputCursor = simulation.World.OutputsOfProvenanceInvocation(invocationId);
        while (simulation.World.MoveNextProvenanceOutput(ref outputCursor, out var outputId))
        {
            result.Add(simulation.World.ProvenanceOutputKindCode(outputId));
        }

        return result.ToArray();
    }

    private static Simulation CreateSimulation(int seed = 1, bool retryFireTargets = true) => new(new SimulationOptions
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
        KernelTuning = new KernelTuningOptions
        {
            Programs = new KernelProgramTuningOptions
            {
                Generators = new GeneratorProgramTuningOptions
                {
                    RetryFireTargets = retryFireTargets,
                },
            },
        },
        Seed = seed,
    });
}
