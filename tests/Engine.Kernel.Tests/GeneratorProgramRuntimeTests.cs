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
    public void FireWindow_FiresWhenGeneratorBecomesEligibleAfterWindowStart()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        var source = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 0);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetRequiredIncomingRelationship(slot, RelationshipKindId);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        Assert.Equal(2, simulation.World.EntityHighWatermark);
        Assert.DoesNotContain(ReadAvailableEventTypes(simulation, cursor), type => type == KernelEventType.GeneratorFired);

        simulation.CreateRelationship(RelationshipKindId, 0, source, target, FixedScalar.FromSingle(0.8f), tick: 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(3, simulation.World.EntityHighWatermark);
        Assert.Contains(ReadAvailableEventTypes(simulation, cursor), type => type == KernelEventType.GeneratorFired);
    }

    [Fact]
    public void FireWindowBudget_BlocksSecondFireUntilNextWindow()
    {
        var simulation = CreateSimulation(fireWindowTicks: 2, firesPerWindow: 1);
        SeedMap(simulation);
        CreateTarget(simulation);
        RegisterGenerator(simulation);

        simulation.AdvanceTicks(2);

        Assert.Equal(2, simulation.World.EntityHighWatermark);

        simulation.AdvanceTicks(1);

        Assert.Equal(3, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void FiresPerWindow_AllowsConfiguredFiresWithinWindow()
    {
        var simulation = CreateSimulation(fireWindowTicks: 25, firesPerWindow: 2);
        SeedMap(simulation);
        CreateTarget(simulation);
        RegisterGenerator(simulation);

        simulation.AdvanceTicks(2);

        Assert.Equal(3, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void SlotFillingGeneratorsReceiveFireSelectionWeightBonus()
    {
        const int slotFillingKindId = 3;
        const int slotFillingSubtypeId = 12;
        var simulation = CreateSimulation(seed: 5);
        SeedMap(simulation);
        CreateTarget(simulation);

        RegisterGeneratorWithProduced(
            simulation,
            generatorId: 101,
            createdKindId: CreatedKindId,
            createdSubtypeId: CreatedSubtypeId,
            fireWeight: 5);
        RegisterGeneratorWithProduced(
            simulation,
            generatorId: 102,
            createdKindId: slotFillingKindId,
            createdSubtypeId: slotFillingSubtypeId,
            fireWeight: 1);
        var slot = simulation.CultureSlots.RegisterSlot(
            CultureId,
            slotId: 1,
            RelationshipPolarityCodes.Positive,
            minimumScore: FixedScalar.FromSingle(0.5f),
            minimumTenureTicks: 0,
            displacementMargin: FixedScalar.FromSingle(0f),
            entityInfluenceMultiplierDelta: FixedScalar.FromSingle(0f),
            cultureInfluenceMultiplierDelta: FixedScalar.FromSingle(0f));
        simulation.CultureSlots.AddAllowedKind(slot, slotFillingKindId);
        simulation.CultureSlots.AddScoreRule(slot, CultureSlotScoreSignalCodes.ActiveEntity, relationshipKindId: 0, weight: FixedScalar.FromSingle(1f));

        simulation.AdvanceTicks(1);

        Assert.Equal(slotFillingKindId, simulation.World.EntityKindId(2));

        simulation.AdvanceTicks(9);

        Assert.Equal(2, simulation.CultureSlots.OccupantEntityId(CultureId, slotId: 1));
    }

    [Fact]
    public void Snapshot_RestoresFireWindowBudget()
    {
        var simulation = CreateSimulation(fireWindowTicks: 3, firesPerWindow: 1);
        SeedMap(simulation);
        CreateTarget(simulation);
        RegisterGenerator(simulation);

        simulation.AdvanceTicks(1);
        using var stream = new MemoryStream();
        KernelSnapshot.SaveKernelState(simulation, stream);

        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);

        loaded.AdvanceTicks(1);
        Assert.Equal(2, loaded.World.EntityHighWatermark);

        loaded.AdvanceTicks(2);
        Assert.Equal(3, loaded.World.EntityHighWatermark);
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

        simulation.CreateRelationship(RelationshipKindId, 0, source, target, FixedScalar.FromSingle(0.8f), tick: 1);
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

        simulation.CreateRelationship(RelationshipKindId, 0, target, destination, FixedScalar.FromSingle(0.8f), tick: 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(3, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void TargetRequiredOutgoingRelationshipMinimumStrength_BlocksUntilRelationshipIsStrongEnough()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        var destination = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 0);
        var relationship = simulation.CreateRelationship(RelationshipKindId, 0, target, destination, FixedScalar.FromSingle(0.5f), tick: 0);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetRequiredOutgoingRelationship(slot, RelationshipKindId, minimumStrength: FixedScalar.FromSingle(0.7f));

        simulation.AdvanceTicks(1);
        Assert.Equal(2, simulation.World.EntityHighWatermark);

        simulation.World.SetRelationshipStrength(relationship, FixedScalar.FromSingle(0.8f), tick: 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(3, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void TargetWithoutOutgoingRelationship_BlocksUntilRelationshipIsGone()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        var destination = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 0);
        var relationship = simulation.CreateRelationship(RelationshipKindId, 0, target, destination, FixedScalar.FromSingle(0.8f), tick: 0);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetWithoutOutgoingRelationship(slot, RelationshipKindId);

        simulation.AdvanceTicks(1);
        Assert.Equal(2, simulation.World.EntityHighWatermark);

        simulation.World.CullRelationship(relationship, tick: 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(3, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void TargetWithoutOutgoingRelationship_AllowsOneExistingRelationshipForSlotOccupant()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        MakeSlotOccupant(simulation, target);
        var firstDestination = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 0);
        simulation.CreateRelationship(RelationshipKindId, 0, target, firstDestination, FixedScalar.FromSingle(0.8f), tick: 0);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetWithoutOutgoingRelationship(slot, RelationshipKindId);

        simulation.AdvanceTicks(1);
        Assert.Equal(3, simulation.World.EntityHighWatermark);

        var secondDestination = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 1);
        simulation.CreateRelationship(RelationshipKindId, 0, target, secondDestination, FixedScalar.FromSingle(0.8f), tick: 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(4, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void TargetWithoutOutgoingRelationship_DoesNotApplySlotMultiplierToExclusiveKinds()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.Relationships.RegisterRelationshipKind(RelationshipKindId, cullThreshold: FixedScalar.FromSingle(0.05f), isExclusive: true);
        var target = CreateTarget(simulation);
        MakeSlotOccupant(simulation, target);
        var destination = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 0);
        var relationship = simulation.CreateRelationship(RelationshipKindId, 0, target, destination, FixedScalar.FromSingle(0.8f), tick: 0);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetWithoutOutgoingRelationship(slot, RelationshipKindId);

        simulation.AdvanceTicks(1);
        Assert.Equal(2, simulation.World.EntityHighWatermark);

        simulation.World.CullRelationship(relationship, tick: 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(3, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void TargetWithoutIncomingRelationship_BlocksUntilRelationshipIsGone()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        var source = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 0);
        var relationship = simulation.CreateRelationship(RelationshipKindId, 0, source, target, FixedScalar.FromSingle(0.8f), tick: 0);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetWithoutIncomingRelationship(slot, RelationshipKindId);

        simulation.AdvanceTicks(1);
        Assert.Equal(2, simulation.World.EntityHighWatermark);

        simulation.World.CullRelationship(relationship, tick: 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(3, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void TargetTerrainCountBelow_BlocksUntilLocalTerrainCountFallsBelowMaximum()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.RegisterTerrain(2, boundaryGroupId: 1);
        CreateTarget(simulation);
        simulation.World.SetCellTerrain(1, 0, 2);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetTerrainCountBelow(slot, terrainId: 2, radius: 1, maximumExclusive: 1);

        simulation.AdvanceTicks(1);
        Assert.Equal(1, simulation.World.EntityHighWatermark);

        simulation.World.SetCellTerrain(1, 0, 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(2, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void TargetTerrainCountBelow_ThrowsCapacityErrorWhenRequirementSpaceIsExhausted()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            GeneratorCapacity = 1,
            GeneratorItemCapacity = 1,
        });
        var slot = RegisterGenerator(simulation, includeProduced: false);
        simulation.Generators.AddTargetTerrainCountBelow(slot, terrainId: 1, radius: 1, maximumExclusive: 1);

        var exception = Assert.Throws<KernelCapacityExceededException>(() =>
            simulation.Generators.AddTargetTerrainCountBelow(slot, terrainId: 1, radius: 1, maximumExclusive: 1));

        Assert.Equal(KernelCapacityCodes.GeneratorTargetTerrainRequirement, exception.CapacityCode);
        Assert.Equal(2, exception.RequestedIndex);
        Assert.Equal(1, exception.Capacity);
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
        Assert.Equal(0, recorded.ParentInvocationId);
        Assert.Equal(ProvenanceTriggerKindCodes.Entity, recorded.TriggerKindCode);
        Assert.Equal(target, recorded.TriggerId);
        Assert.Equal(2, recorded.OutputCount);
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
    public void Fire_WithRequiredRelationshipRecordsTriggerAndParentInvocation()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        var source = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 0);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetRequiredIncomingRelationship(slot, RelationshipKindId);
        int triggerRelationship;
        using (simulation.EnterProvenanceInvocation(
                   ProvenanceProducerKindCodes.System,
                   producerId: 900,
                   primaryEntityId: source))
        {
            triggerRelationship = simulation.CreateRelationship(RelationshipKindId, 0, source, target, FixedScalar.FromSingle(0.8f), tick: 0);
        }

        var parentInvocationId = simulation.World.RelationshipCreatedByInvocationId(triggerRelationship);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var recorded = Assert.Single(
            ReadAvailableEvents(simulation, cursor),
            evt => evt.EventType == KernelEventType.ProvenanceInvocationRecorded).AsProvenanceInvocationRecorded();
        Assert.Equal(ProvenanceProducerKindCodes.Generator, recorded.ProducerKindCode);
        Assert.Equal(GeneratorId, recorded.ProducerId);
        Assert.Equal(target, recorded.PrimaryEntityId);
        Assert.Equal(parentInvocationId, recorded.ParentInvocationId);
        Assert.Equal(ProvenanceTriggerKindCodes.Relationship, recorded.TriggerKindCode);
        Assert.Equal(triggerRelationship, recorded.TriggerId);
    }

    [Fact]
    public void RemoveTargetRelationshipsEffect_CanCullSelectedTriggerAndReciprocalOnly()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        var opponent = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 0);
        var other = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 2, 0, 0, 0, 0, tick: 0);
        var trigger = simulation.CreateRelationship(RelationshipKindId, 0, target, opponent, FixedScalar.FromSingle(0.8f), tick: 0);
        var reciprocal = simulation.CreateRelationship(RelationshipKindId, 0, opponent, target, FixedScalar.FromSingle(0.8f), tick: 0);
        var unrelated = simulation.CreateRelationship(RelationshipKindId, 0, other, target, FixedScalar.FromSingle(0.8f), tick: 0);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetRequiredOutgoingRelationship(slot, RelationshipKindId);
        simulation.Generators.AddEffect(
            slot,
            GeneratorEffectTypeCodes.RemoveTargetRelationships,
            RelationshipKindId,
            GeneratorRelationshipInstanceSelectionCodes.TriggerRelationship,
            optionCode: 1);

        simulation.AdvanceTicks(1);

        Assert.False(simulation.World.RelationshipIsActive(trigger));
        Assert.False(simulation.World.RelationshipIsActive(reciprocal));
        Assert.True(simulation.World.RelationshipIsActive(unrelated));
    }

    [Fact]
    public void SetBindingHistoricalEffectPublishesCapturedTargetOccurrence()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        simulation.Generators.AddEffect(
            slot,
            GeneratorEffectTypeCodes.SetBindingHistorical,
            GeneratorProgramConstants.TargetReferenceCode);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var events = ReadAvailableEvents(simulation, cursor);
        Assert.False(simulation.World.EntityExists(target));
        Assert.Contains(events, evt =>
            evt.EventType == KernelEventType.GeneratorFired &&
            evt.AsGeneratorFired().TargetEntityId == target);
        Assert.Contains(events, evt =>
            evt.EventType == KernelEventType.GeneratorEvaluated &&
            evt.AsGeneratorEvaluated().SelectedTargetEntityId == target);
    }

    [Fact]
    public void AdjustTargetRelationshipStrengthEffect_CanSpendSelectedTriggerAndReciprocalOnly()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        var routeTarget = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 0);
        var other = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 2, 0, 0, 0, 0, tick: 0);
        var trigger = simulation.CreateRelationship(RelationshipKindId, 0, target, routeTarget, FixedScalar.FromSingle(0.72f), tick: 0);
        var reciprocal = simulation.CreateRelationship(RelationshipKindId, 0, routeTarget, target, FixedScalar.FromSingle(0.72f), tick: 0);
        var unrelated = simulation.CreateRelationship(RelationshipKindId, 0, target, other, FixedScalar.FromSingle(0.85f), tick: 0);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetRequiredOutgoingRelationship(slot, RelationshipKindId, minimumStrength: FixedScalar.FromSingle(0.7f));
        simulation.Generators.AddEffect(
            slot,
            GeneratorEffectTypeCodes.AdjustTargetRelationshipStrength,
            RelationshipKindId,
            GeneratorRelationshipInstanceSelectionCodes.TriggerRelationship,
            optionCode: 1,
            value: FixedScalar.FromSingle(-0.3f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(0.42f), simulation.World.RelationshipStrength(trigger));
        Assert.Equal(FixedScalar.FromSingle(0.42f), simulation.World.RelationshipStrength(reciprocal));
        Assert.Equal(FixedScalar.FromSingle(0.85f), simulation.World.RelationshipStrength(unrelated));
    }

    [Fact]
    public void AdjustTargetRelationshipStrengthEffect_HalvesNegativeSpendForSlotOccupant()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        MakeSlotOccupant(simulation, target);
        var routeTarget = simulation.CreateEntity(CreatedKindId, CreatedSubtypeId, CultureId, 1, 0, 0, 0, 0, tick: 0);
        var trigger = simulation.CreateRelationship(RelationshipKindId, 0, target, routeTarget, FixedScalar.FromSingle(0.72f), tick: 0);
        var reciprocal = simulation.CreateRelationship(RelationshipKindId, 0, routeTarget, target, FixedScalar.FromSingle(0.72f), tick: 0);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddTargetRequiredOutgoingRelationship(slot, RelationshipKindId, minimumStrength: FixedScalar.FromSingle(0.7f));
        simulation.Generators.AddEffect(
            slot,
            GeneratorEffectTypeCodes.AdjustTargetRelationshipStrength,
            RelationshipKindId,
            GeneratorRelationshipInstanceSelectionCodes.TriggerRelationship,
            optionCode: 1,
            value: FixedScalar.FromSingle(-0.3f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(0.57f), simulation.World.RelationshipStrength(trigger));
        Assert.Equal(FixedScalar.FromSingle(0.57f), simulation.World.RelationshipStrength(reciprocal));
    }

    [Fact]
    public void Fire_WithProducedCoverage_EmitsMapCellStateChangedForClaimedCells()
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
            physicalPlacementCode: GeneratorPlacementCodes.Area,
            semanticPlacementCode: GeneratorPlacementCodes.Area,
            placementRefCode: GeneratorProgramConstants.TargetReferenceCode,
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            createChance: FixedScalar.FromSingle(1f),
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
            .Where(evt => evt.EventType == KernelEventType.MapCellStateChanged)
            .Select(evt => evt.AsMapCellStateChanged())
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
    public void RetryFireTargets_WhenEnabled_TriesAnotherTargetAfterFireFailure()
    {
        var simulation = CreateSimulation(seed: 2, retryFireTargets: true);
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

        // The failed first fire commits nothing: only the two targets and the
        // one entity created by the successful second fire exist, and every
        // created entity is active.
        Assert.Equal(3, simulation.World.EntityHighWatermark);
        Assert.Equal(EntityStatusCodes.Active, simulation.World.EntityStatus(firstTarget + 2));
        Assert.DoesNotContain(events, evt =>
            evt.EventType == KernelEventType.EntityStatusChanged &&
            evt.AsEntityStatusChanged().CurrentStatusCode == EntityStatusCodes.Historical);
    }

    [Fact]
    public void RetryFireTargets_WhenDisabled_StopsAfterFirstFireFailure()
    {
        var simulation = CreateSimulation(seed: 2, retryFireTargets: false);
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

        // The failed fire commits nothing: no created entities, active or
        // historical, reach the graph.
        Assert.Equal(2, simulation.World.EntityHighWatermark);
        Assert.DoesNotContain(events, evt => evt.EventType == KernelEventType.EntityCreated);
    }

    [Fact]
    public void Fire_WhenLaterProducedFails_CommitsNoEarlierProducedEntities()
    {
        var simulation = CreateSimulation(seed: 1, retryFireTargets: false);
        CreateSparseCoverageTarget(simulation);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        AddProducedEntity(simulation, slot, createChance: 1f);
        AddCoverageProduced(simulation, slot, producedRefCode: CreatedRef + 1, createdKindId: CreatedKindId + 1);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        // The first produced entity resolves, but the fire fails on the second
        // produced's coverage. Nothing reaches the graph — no active entities,
        // no historical placeholders, no entity events at all.
        var events = ReadAvailableEvents(simulation, cursor);
        var evaluated = Assert.Single(events
            .Where(evt => evt.EventType == KernelEventType.GeneratorEvaluated)
            .Select(evt => evt.AsGeneratorEvaluated()));
        Assert.Equal(GeneratorStatusCodes.FireFailed, evaluated.StatusCode);
        Assert.Equal(1, simulation.World.EntityHighWatermark);
        Assert.DoesNotContain(events, evt => evt.EventType == KernelEventType.EntityCreated);
        Assert.DoesNotContain(events, evt => evt.EventType == KernelEventType.EntityStatusChanged);
    }

    [Fact]
    public void Fire_AttributesCreatedEntityAndRelationshipEventsToGeneratorContext()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.RegisterAttribute(CreatedKindId, CreatedAttributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(12f));
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
        bool includeProduced = true)
    {
        var slot = simulation.Generators.RegisterGenerator(
            GeneratorId,
            TargetKindId,
            fireWeight: 1);
        simulation.Generators.AddTrigger(slot, TargetKindId);
        if (includeProduced)
        {
            AddProducedEntity(simulation, slot, createChance: 1f);
        }

        return slot;
    }

    private static void RegisterGeneratorWithProduced(
        Simulation simulation,
        int generatorId,
        int createdKindId,
        int createdSubtypeId,
        int fireWeight)
    {
        var slot = simulation.Generators.RegisterGenerator(
            generatorId,
            TargetKindId,
            fireWeight);
        simulation.Generators.AddTrigger(slot, TargetKindId);
        simulation.Generators.AddProduced(
            slot,
            CreatedRef,
            createdKindId,
            createdSubtypeId,
            createIfMissing: 1,
            hasQuery: 0,
            queryAdjacentToTarget: 0,
            queryCultureMatchCode: GeneratorCultureMatchCodes.Any,
            physicalPlacementCode: GeneratorPlacementCodes.Area,
            semanticPlacementCode: GeneratorPlacementCodes.Area,
            placementRefCode: GeneratorProgramConstants.TargetReferenceCode,
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            createChance: FixedScalar.FromSingle(1f),
            coverageModeCode: GeneratorCoverageModeCodes.None,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: 0,
            coverageMinimumCellCount: 0,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 0,
            coverageTransformTerrainId: 0);
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
            physicalPlacementCode: GeneratorPlacementCodes.Area,
            semanticPlacementCode: GeneratorPlacementCodes.Area,
            placementRefCode: GeneratorProgramConstants.TargetReferenceCode,
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            FixedScalar.FromSingle(createChance),
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
            strength: FixedScalar.FromSingle(0.75f),
            replaceExistingOutgoing: 0);
    }

    private static void RegisterCoverageGenerator(Simulation simulation)
    {
        var slot = RegisterGenerator(simulation, includeProduced: false);
        AddCoverageProduced(simulation, slot, producedRefCode: CreatedRef, createdKindId: CreatedKindId);
    }

    private static void AddCoverageProduced(
        Simulation simulation,
        int slot,
        int producedRefCode,
        int createdKindId)
    {
        simulation.Generators.AddProduced(
            slot,
            producedRefCode,
            createdKindId,
            CreatedSubtypeId,
            createIfMissing: 1,
            hasQuery: 0,
            queryAdjacentToTarget: 0,
            queryCultureMatchCode: GeneratorCultureMatchCodes.Any,
            physicalPlacementCode: GeneratorPlacementCodes.Area,
            semanticPlacementCode: GeneratorPlacementCodes.Area,
            placementRefCode: GeneratorProgramConstants.TargetReferenceCode,
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            createChance: FixedScalar.FromSingle(1f),
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

    private static void MakeSlotOccupant(Simulation simulation, int entityId)
    {
        var slot = simulation.CultureSlots.RegisterSlot(
            CultureId,
            slotId: 2,
            RelationshipPolarityCodes.Neutral,
            minimumScore: FixedScalar.FromSingle(0f),
            minimumTenureTicks: 0,
            displacementMargin: FixedScalar.FromSingle(0f),
            entityInfluenceMultiplierDelta: FixedScalar.FromSingle(0f),
            cultureInfluenceMultiplierDelta: FixedScalar.FromSingle(0f));
        simulation.CultureSlots.State.SetOccupant(slot, entityId, score: FixedScalar.FromSingle(1f), tick: 0);
    }

    private static int CreateSparseCoverageTarget(Simulation simulation)
    {
        simulation.World.SetMapRadius(20);
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        simulation.RegisterTerrain(2, boundaryGroupId: 2);
        simulation.CreateCell(0, 0, terrainId: 1, ownerEntityId: 0);
        simulation.CreateCell(1, 0, terrainId: 2, ownerEntityId: 0);
        SeedSemanticPlacementCells(simulation);
        return CreateTarget(simulation, physicalQ: 0, physicalR: 0);
    }

    private static int CreateViableCoverageTarget(Simulation simulation)
    {
        simulation.CreateCell(10, 0, terrainId: 1, ownerEntityId: 0);
        simulation.CreateCell(10, 1, terrainId: 1, ownerEntityId: 0);
        simulation.CreateCell(11, 0, terrainId: 1, ownerEntityId: 0);
        SeedSemanticPlacementCells(simulation);
        return CreateTarget(simulation, physicalQ: 10, physicalR: 0);
    }

    private static void SeedSemanticPlacementCells(Simulation simulation)
    {
        for (var q = -2; q <= 2; q++)
        {
            var minR = Math.Max(-2, -q - 2);
            var maxR = Math.Min(2, -q + 2);
            for (var r = minR; r <= maxR; r++)
            {
                if (!simulation.World.CellIsKnown(PlaneCodes.Semantic, q, r))
                {
                    simulation.CreateCell(PlaneCodes.Semantic, q, r, terrainId: 1, ownerEntityId: 0);
                }
            }
        }
    }

    private static void SeedMap(Simulation simulation)
    {
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        simulation.World.SetMapRadius(2);
        for (var q = -2; q <= 2; q++)
        {
            var minR = Math.Max(-2, -q - 2);
            var maxR = Math.Min(2, -q + 2);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.CreateCell(q, r, terrainId: 1, ownerEntityId: 0);
                simulation.CreateCell(PlaneCodes.Semantic, q, r, terrainId: 1, ownerEntityId: 0);
            }
        }
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

    private static Simulation CreateSimulation(
        int seed = 1,
        bool retryFireTargets = true,
        int fireWindowTicks = GeneratorProgramConstants.DefaultFireWindowTicks,
        int firesPerWindow = GeneratorProgramConstants.DefaultFiresPerWindow) => KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 16,
            RelationshipCapacity = 16,
            AttributeCapacity = 32,
            MapCellCapacity = 128,
            EventCapacity = 512,
            GeneratorCapacity = 4,
            GeneratorItemCapacity = 64,
            RelationshipKindIdCapacity = 64,
            TerrainIdCapacity = 8,
            SystemCapacity = 24,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    Generators = new GeneratorProgramTuningOptions
                    {
                        FrontierFireWindowMultiplier = 1,
                        SettledFireWindowMultiplier = 1,
                        RetryFireTargets = retryFireTargets,
                        FireWindowTicks = fireWindowTicks,
                        FiresPerWindow = firesPerWindow,
                    },
                },
            },
            Seed = seed,
        });
}
