namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;
using Engine.Kernel.Programs;

public sealed class KernelSnapshotTests
{
    [Fact]
    public void SaveAndLoadKernelStateRestoresWorldProgramsAndEmitsMaterializationEvents()
    {
        var simulation = CreateSimulation();
        simulation.World.SetMapRadius(1);
        simulation.RegisterTerrain(1, boundaryGroupId: 2);
        simulation.RegisterAttribute(1, 10, 0f, 100f, 0f);
        simulation.RegisterAttribute(2, 10, 0f, 100f, 0f);
        simulation.CreateCell(0, 0, terrainId: 1, ownerEntityId: 0);
        var actor = simulation.CreateEntity(1, 3, 4, 0, 0, 0, 0, 0, tick: 0);
        var target = simulation.CreateEntity(2, 3, 4, 1, 0, 1, 0, 0, tick: 0);
        simulation.World.SetCoverageOwner(0, 0, actor);
        using (simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0))
        {
            simulation.SetAttribute(actor, 10, 42f, tick: 0);
            simulation.SetCellInfluence(actor, PlaneCodes.Physical, 0, 0, 55f, tick: 0);
        }
        simulation.World.SetEntityStatus(target, EntityStatusCodes.Historical, tick: 0);
        var relationshipId = simulation.CreateRelationship(
            relationshipKindId: 6,
            polarityCode: RelationshipPolarityCodes.Negative,
            sourceEntityId: actor,
            targetEntityId: target,
            strength: 0.75f,
            tick: 0);
        simulation.Generators.RegisterGenerator(
            generatorId: 50,
            targetKindId: 1,
            pulseWeight: 1,
            maxRuns: 3);
        Assert.True(simulation.Generators.TrySetGeneratorEnabledById(50, enabled: false));
        simulation.SetProgramEnabled(KernelProgramCodes.Generators, enabled: false);

        using var stream = new MemoryStream();
        KernelSnapshot.SaveKernelState(simulation, stream);
        var expectedRandom = simulation.NextInt(10000);

        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);

        AssertLoadedSnapshotState(simulation, loaded, actor, target, relationshipId, expectedRandom);
        AssertMaterializationEvents(loaded);
    }

    [Fact]
    public void SaveAndLoadKernelStateRestoresProvenanceHistory()
    {
        var simulation = CreateSimulation();
        SeedSnapshotMap(simulation);
        var target = simulation.CreateEntity(1, 3, 4, 0, 0, 0, 0, 0, tick: 0);
        RegisterProvenanceSnapshotGenerator(simulation);

        simulation.AdvanceTicks(7);

        var created = simulation.World.EntityHighWatermark;
        var relationship = simulation.World.FindRelationship(created, target, 6);
        var invocationId = simulation.World.EntityCreatedByInvocationId(created);

        using var stream = new MemoryStream();
        KernelSnapshot.SaveKernelState(simulation, stream);

        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);

        Assert.Equal(invocationId, loaded.World.EntityCreatedByInvocationId(created));
        Assert.Equal(invocationId, loaded.World.RelationshipCreatedByInvocationId(relationship));
        Assert.Equal(ProvenanceProducerKindCodes.Generator, loaded.World.ProvenanceInvocationProducerKindCode(invocationId));
        Assert.Equal(50, loaded.World.ProvenanceInvocationProducerId(invocationId));
        Assert.Equal(2, loaded.World.ProvenanceInvocationOutputCount(invocationId));
    }

    private static void SeedSnapshotMap(Simulation simulation)
    {
        simulation.World.SetMapRadius(1);
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        for (var q = -1; q <= 1; q++)
        {
            var minR = Math.Max(-1, -q - 1);
            var maxR = Math.Min(1, -q + 1);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.CreateCell(q, r, terrainId: 1, ownerEntityId: 0);
            }
        }
    }

    private static void RegisterProvenanceSnapshotGenerator(Simulation simulation)
    {
        var generator = simulation.Generators.RegisterGenerator(50, 1, 1, 1);
        simulation.Generators.AddTrigger(generator, 1);
        simulation.Generators.AddProduced(
            generator,
            refCode: 2,
            kindId: 2,
            subtypeId: 3,
            createIfMissing: 1,
            hasQuery: 0,
            queryAdjacentToTarget: 0,
            queryCultureMatchCode: GeneratorCultureMatchCodes.Any,
            physicalPlacementCode: GeneratorPlacementCodes.Area,
            semanticPlacementCode: GeneratorPlacementCodes.Area,
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
        simulation.Generators.AddRelationship(
            generator,
            sourceRefCode: 2,
            destinationRefCode: GeneratorProgramConstants.TargetReferenceCode,
            relationshipKindId: 6,
            strength: 0.75f,
            replaceExistingOutgoing: 0);
    }

    private static Simulation CreateSimulation() =>
        new(new SimulationOptions
        {
            EntityCapacity = 16,
            RelationshipCapacity = 16,
            AttributeCapacity = 64,
            MapCellCapacity = 32,
            EventCapacity = 8,
            EventCursorCapacity = 8,
            CommandCapacity = 16,
            GambitCapacity = 4,
            GambitStepCapacity = 16,
            GambitCooldownCapacity = 16,
            GambitProviderSelectorCapacity = 4,
            GeneratorCapacity = 4,
            GeneratorItemCapacity = 16,
            LifecycleRuleCapacity = 4,
            EntityAttributeCapacity = 8,
            EffectRuleCapacity = 4,
            AttributeIdCapacity = 64,
            KindIdCapacity = 16,
            TerrainIdCapacity = 8,
            RelationshipRuleCapacity = 4,
            RelationshipKindIdCapacity = 16,
            KernelTuning = SnapshotKernelTuning(),
            Seed = 123,
        });

    private static KernelTuningOptions SnapshotKernelTuning() =>
        new()
        {
            Programs = new KernelProgramTuningOptions
            {
                MinorActorMainTickRatio = 7,
                CultureInfluenceActionScale = 0.25f,
                Upkeep = new UpkeepProgramTuningOptions
                {
                    AgingModelCode = UpkeepAgingModelCodes.LateLife,
                },
                Generators = new GeneratorProgramTuningOptions
                {
                    RetryFireTargets = false,
                    PulseWidthTicks = 17,
                    FiresPerPulse = 2,
                },
                LowAltitudeVerbs = new LowAltitudeVerbTuningOptions
                {
                    OpportunityRate = 0.35f,
                    OpportunityBonus = 1.75f,
                    RecentTargetCooldownTicks = 77,
                    RecentTargetRingSize = 2,
                    CellSaturationFactor = 0.5f,
                    ExploreDistanceScale = 4.5f,
                    TerrainDistanceScale = 5.5f,
                    EntityKindDistanceScale = 6.5f,
                    RelationshipTargetDistanceScale = 7.5f,
                    OwnInfluenceDistanceScale = 8.5f,
                    ForeignInfluenceDistanceScale = 9.5f,
                },
            },
        };

    private static void AssertLoadedSnapshotState(
        Simulation simulation,
        Simulation loaded,
        int actor,
        int target,
        int relationshipId,
        int expectedRandom)
    {
        Assert.Equal(simulation.CurrentTick, loaded.CurrentTick);
        Assert.Equal(simulation.WorldVersion, loaded.WorldVersion);
        Assert.Equal(actor, loaded.World.EntityHighWatermark - 1);
        Assert.Equal(EntityStatusCodes.Historical, loaded.World.EntityStatus(target));
        Assert.Equal(42f, loaded.World.GetAttribute(actor, 10));
        Assert.Equal(55f, loaded.World.GetCellInfluence(actor, PlaneCodes.Physical, 0, 0));
        Assert.Equal(1, loaded.World.CellTerrainId(0, 0));
        Assert.Equal(actor, loaded.World.CoverageOwnerAt(0, 0));
        Assert.Equal(1, loaded.World.CoverageOwnerCellCount(actor));
        Assert.Equal(2, loaded.World.TerrainBoundaryGroupId(1));
        Assert.Equal(0.75f, loaded.World.RelationshipStrength(relationshipId));
        Assert.False(loaded.ProgramIsEnabled(KernelProgramCodes.Generators));
        Assert.True(loaded.Generators.TryGetGeneratorEnabledById(50, out var generatorEnabled));
        Assert.False(generatorEnabled);
        Assert.Equal(7, loaded.Options.KernelTuning.Programs.MinorActorMainTickRatio);
        Assert.Equal(0.25f, loaded.Options.KernelTuning.Programs.CultureInfluenceActionScale);
        Assert.Equal(UpkeepAgingModelCodes.LateLife, loaded.Options.KernelTuning.Programs.Upkeep.AgingModelCode);
        Assert.False(loaded.Options.KernelTuning.Programs.Generators.RetryFireTargets);
        Assert.Equal(17, loaded.Options.KernelTuning.Programs.Generators.PulseWidthTicks);
        Assert.Equal(2, loaded.Options.KernelTuning.Programs.Generators.FiresPerPulse);
        Assert.Equal(0.35f, loaded.Options.KernelTuning.Programs.LowAltitudeVerbs.OpportunityRate);
        Assert.Equal(1.75f, loaded.Options.KernelTuning.Programs.LowAltitudeVerbs.OpportunityBonus);
        Assert.Equal(77, loaded.Options.KernelTuning.Programs.LowAltitudeVerbs.RecentTargetCooldownTicks);
        Assert.Equal(2, loaded.Options.KernelTuning.Programs.LowAltitudeVerbs.RecentTargetRingSize);
        Assert.Equal(0.5f, loaded.Options.KernelTuning.Programs.LowAltitudeVerbs.CellSaturationFactor);
        Assert.Equal(4.5f, loaded.Options.KernelTuning.Programs.LowAltitudeVerbs.ExploreDistanceScale);
        Assert.Equal(5.5f, loaded.Options.KernelTuning.Programs.LowAltitudeVerbs.TerrainDistanceScale);
        Assert.Equal(6.5f, loaded.Options.KernelTuning.Programs.LowAltitudeVerbs.EntityKindDistanceScale);
        Assert.Equal(7.5f, loaded.Options.KernelTuning.Programs.LowAltitudeVerbs.RelationshipTargetDistanceScale);
        Assert.Equal(8.5f, loaded.Options.KernelTuning.Programs.LowAltitudeVerbs.OwnInfluenceDistanceScale);
        Assert.Equal(9.5f, loaded.Options.KernelTuning.Programs.LowAltitudeVerbs.ForeignInfluenceDistanceScale);
        Assert.Equal(expectedRandom, loaded.NextInt(10000));
    }

    private static void AssertMaterializationEvents(Simulation loaded)
    {
        using var cursor = loaded.Events.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var eventTypes = new List<KernelEventType>();
        while (loaded.Events.TryRead(cursor, out var sequenced))
        {
            eventTypes.Add(sequenced.Event.EventType);
        }

        Assert.Contains(KernelEventType.CellCreated, eventTypes);
        Assert.Contains(KernelEventType.EntityCreated, eventTypes);
        Assert.Contains(KernelEventType.EntityStatusChanged, eventTypes);
        Assert.Contains(KernelEventType.AttributeChanged, eventTypes);
        Assert.Contains(KernelEventType.CellCultureInfluenceChanged, eventTypes);
        Assert.Contains(KernelEventType.RelationshipChanged, eventTypes);
    }
}
