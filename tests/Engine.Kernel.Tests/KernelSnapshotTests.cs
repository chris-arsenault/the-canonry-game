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
        using (simulation.EnterExecutionContext(simulation.ProcessSourceCodeId, programId: 0))
        {
            simulation.SetAttribute(actor, 10, 42f, tick: 0);
            simulation.SetResource(20, 0, 0, 5f, tick: 0);
            simulation.SetCellInfluence(actor, PlaneCodes.Physical, 0, 0, 55f, tick: 0);
        }
        simulation.World.SetEntityStatus(target, EntityStatusCodes.Historical, tick: 0);
        var relationshipId = simulation.CreateRelationship(
            relationshipKindId: 6,
            polarityCode: RelationshipPolarityCodes.Negative,
            sourceEntityId: actor,
            targetEntityId: target,
            strength: 0.75f,
            tick: 0,
            factId0: 70,
            factCount: 1);
        simulation.Pressures.RegisterPressure(40, PressureScopeCodes.Entity, 1f, 0f, 1f);
        simulation.Pressures.ApplyDelta(40, actor, 6f);
        simulation.Pressures.Tick(simulation, tick: 0);
        simulation.Generators.RegisterGenerator(
            generatorId: 50,
            cadence: 1,
            targetKindId: 1,
            pulseWeight: 1,
            maxRuns: 3,
            targetAllowWilderness: true);
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

    private static Simulation CreateSimulation() =>
        new(new SimulationOptions
        {
            EntityCapacity = 16,
            RelationshipCapacity = 16,
            AttributeCapacity = 64,
            ResourceCapacity = 32,
            MapCellCapacity = 32,
            EventCapacity = 8,
            EventCursorCapacity = 8,
            CommandCapacity = 16,
            ProcessCapacity = 4,
            ProcessStepCapacity = 16,
            ProcessCooldownCapacity = 16,
            ProcessObjectiveCapacity = 4,
            ProcessProviderSelectorCapacity = 4,
            ProcessSupportRuleCapacity = 4,
            ProcessCommitmentCapacity = 16,
            PlanningOverrideCapacity = 16,
            GambitCapacity = 4,
            GambitStepCapacity = 16,
            GambitCooldownCapacity = 16,
            GambitObjectiveCapacity = 4,
            GambitProviderSelectorCapacity = 4,
            GambitSupportRuleCapacity = 4,
            GambitCommitmentCapacity = 16,
            GeneratorCapacity = 4,
            GeneratorItemCapacity = 16,
            PressureCapacity = 4,
            PressureValueCapacity = 16,
            DegeneracyDetectorCapacity = 4,
            DegeneracyStateCapacity = 16,
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
                Processes = new ProcessProgramTuningOptions
                {
                    WorkCommitmentCode = ProcessWorkCommitmentCodes.Pilot,
                },
                Planning = new PlanningProgramTuningOptions
                {
                    DefaultPlannerKindCode = PlannerKindCodes.Opportunity,
                    GoapTargetReservationsEnabled = true,
                    OpportunityTargetCapacity = 3,
                    KindPlannerOverrides =
                    [
                        new PlanningKindPlannerOverride(1, PlannerKindCodes.Goap),
                    ],
                    EntityPlannerOverrides =
                    [
                        new PlanningEntityPlannerOverride(2, PlannerKindCodes.Opportunity),
                    ],
                },
                Pressures = new PressureProgramTuningOptions
                {
                    Enabled = true,
                },
                Upkeep = new UpkeepProgramTuningOptions
                {
                    AgingModelCode = UpkeepAgingModelCodes.LateLife,
                },
                Generators = new GeneratorProgramTuningOptions
                {
                    RetryFireTargets = false,
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
        Assert.Equal(5f, loaded.World.GetResource(20, 0, 0));
        Assert.Equal(55f, loaded.World.GetCellInfluence(actor, PlaneCodes.Physical, 0, 0));
        Assert.Equal(1, loaded.World.CellTerrainId(0, 0));
        Assert.Equal(actor, loaded.World.CoverageOwnerAt(0, 0));
        Assert.Equal(1, loaded.World.CoverageOwnerCellCount(actor));
        Assert.Equal(2, loaded.World.TerrainBoundaryGroupId(1));
        Assert.Equal(0.75f, loaded.World.RelationshipStrength(relationshipId));
        Assert.Equal(7f, loaded.Pressures.GetValue(40, actor));
        Assert.False(loaded.ProgramIsEnabled(KernelProgramCodes.Generators));
        Assert.True(loaded.Generators.TryGetGeneratorEnabledById(50, out var generatorEnabled));
        Assert.False(generatorEnabled);
        Assert.Equal(ProcessWorkCommitmentCodes.Pilot, loaded.Options.KernelTuning.Programs.Processes.WorkCommitmentCode);
        Assert.Equal(PlannerKindCodes.Opportunity, loaded.Options.KernelTuning.Programs.Planning.DefaultPlannerKindCode);
        Assert.True(loaded.Options.KernelTuning.Programs.Planning.GoapTargetReservationsEnabled);
        Assert.Equal(3, loaded.Options.KernelTuning.Programs.Planning.OpportunityTargetCapacity);
        Assert.Contains(loaded.Options.KernelTuning.Programs.Planning.KindPlannerOverrides, item =>
            item.KindId == 1 && item.PlannerKindCode == PlannerKindCodes.Goap);
        Assert.Contains(loaded.Options.KernelTuning.Programs.Planning.EntityPlannerOverrides, item =>
            item.EntityId == 2 && item.PlannerKindCode == PlannerKindCodes.Opportunity);
        Assert.Equal(PlannerKindCodes.Goap, loaded.Planners.PlannerKindFor(1, actor));
        Assert.Equal(PlannerKindCodes.Opportunity, loaded.Planners.PlannerKindFor(1, target));
        Assert.True(loaded.Options.KernelTuning.Programs.Pressures.Enabled);
        Assert.Equal(UpkeepAgingModelCodes.LateLife, loaded.Options.KernelTuning.Programs.Upkeep.AgingModelCode);
        Assert.False(loaded.Options.KernelTuning.Programs.Generators.RetryFireTargets);
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
        Assert.Contains(KernelEventType.ResourceChanged, eventTypes);
        Assert.Contains(KernelEventType.CellInfluenceChanged, eventTypes);
        Assert.Contains(KernelEventType.RelationshipChanged, eventTypes);
    }
}
