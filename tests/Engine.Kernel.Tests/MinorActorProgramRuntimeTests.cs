namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Eventing;
using Engine.Kernel;

public sealed partial class MinorActorProgramRuntimeTests
{
    private const int CultureId = 1;
    private const int SourceEntityId = 1;
    private const int SourceKindId = 1;
    private const int CultureVerbId = 101;

    [Fact]
    public void Spawn_DoesNotCreateGraphEntity()
    {
        var simulation = new Simulation(new SimulationOptions { MinorActorCapacity = 8 });

        var actorId = simulation.MinorActors.Spawn(
            cultureId: 1,
            sourceEntityId: 0,
            sourceRuleId: 0,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 10,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(0f),
            tick: 0);

        Assert.Equal(1, actorId);
        Assert.Equal(1, simulation.MinorActors.State.ActiveCount);
        Assert.Equal(0, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void CultureExpansion_ModifiesCultureTileWithoutEntityInfluence()
    {
        var simulation = new Simulation(new SimulationOptions
        {
            MinorActorCapacity = 8,
            KernelTuning = CultureInfluenceActionScaleTuning(1f),
        });
        simulation.World.SetCell(PlaneCodes.Physical, 0, 0, terrainId: 1);
        _ = simulation.MinorActors.Spawn(
            cultureId: 1,
            sourceEntityId: 0,
            sourceRuleId: 0,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 2,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(2f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(2f), simulation.World.GetCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0));
        Assert.Equal(FixedScalar.FromSingle(2f), simulation.World.GetCellCultureLaborInfluenceRaw(1, PlaneCodes.Physical, 0, 0));
        Assert.Equal(0, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void EmitterRulesCreateMinorActorsFromMajorEntitiesOnly()
    {
        var simulation = new Simulation(new SimulationOptions { MinorActorCapacity = 8 });
        simulation.World.SetCell(PlaneCodes.Physical, 0, 0, terrainId: 1);
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
        _ = simulation.MinorActors.RegisterEmitterRule(
            sourceKindId: 1,
            sourceSubtypeId: 0,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            ttlPulses: 4,
            emitCadencePulses: 1,
            maxActivePerSource: 1,
            spawnRadius: 0,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(0f));

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(1, simulation.MinorActors.State.ActiveCount);
        Assert.Equal(1, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void WorkTierAddsWorkerCapacityPerSource()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        simulation.World.SetCell(PlaneCodes.Physical, 0, 0, terrainId: 1);
        SeedSourceEntity(simulation);
        var rule = simulation.MinorActors.RegisterEmitterRule(
            sourceKindId: SourceKindId,
            sourceSubtypeId: 0,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            ttlPulses: 6,
            emitCadencePulses: 1,
            maxActivePerSource: 1,
            spawnRadius: 0,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(0f));

        _ = simulation.AdvanceTicks(1);
        _ = simulation.AdvanceTicks(1);
        Assert.Equal(1, simulation.MinorActors.State.SourceActiveCount(SourceEntityId, rule));

        Assert.Equal(1, simulation.MinorActors.IncreaseWorkTier(SourceEntityId, maxTier: 3));
        _ = simulation.AdvanceTicks(1);

        Assert.Equal(2, simulation.MinorActors.State.SourceActiveCount(SourceEntityId, rule));
    }

    [Fact]
    public void WorkTierCanIncreaseWithoutCap()
    {
        var simulation = new Simulation(new SimulationOptions { MinorActorCapacity = 8 });

        Assert.Equal(1, simulation.MinorActors.IncreaseWorkTier(SourceEntityId, maxTier: 0));
        Assert.Equal(2, simulation.MinorActors.IncreaseWorkTier(SourceEntityId, maxTier: 0));
        Assert.Equal(3, simulation.MinorActors.IncreaseWorkTier(SourceEntityId, maxTier: 3));
        Assert.Equal(3, simulation.MinorActors.IncreaseWorkTier(SourceEntityId, maxTier: 3));
    }

    [Fact]
    public void WorkTierExtendsMinorActorVerbOperatingRadius()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 4);
        SeedSourceEntity(simulation);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureExpansion,
            MinorActorVerbTargetingCodes.ReinforceHomeMass,
            PlaneCodes.Physical,
            radius: 1,
            sampleCount: 128,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 2, 0, FixedScalar.FromSingle(30f), tick: 0);
        var before = simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 2, 0);
        Assert.Equal(1, simulation.MinorActors.IncreaseWorkTier(SourceEntityId, maxTier: 3));
        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 1,
            r: 0,
            ttlPulses: 4,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(5f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        var evt = Assert.Single(ReadMinorActorVerbEvents(simulation).FindAll(e =>
            e.VerbId == CultureVerbId &&
            e.StatusCode == TraceStatusCodes.Selected));
        Assert.Equal(1, evt.SourceWorkTier);
        Assert.Equal(2, evt.EffectiveRadius);
        Assert.True(simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 2, 0) > before);
        Assert.Equal((2, 0), (simulation.MinorActors.State.Q(1), simulation.MinorActors.State.R(1)));
    }

    [Fact]
    public void SnapshotRoundTripsMinorActorStateAndRules()
    {
        var simulation = new Simulation(new SimulationOptions { MinorActorCapacity = 8 });
        _ = simulation.MinorActors.RegisterEmitterRule(
            sourceKindId: 1,
            sourceSubtypeId: 0,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            ttlPulses: 4,
            emitCadencePulses: 1,
            maxActivePerSource: 1,
            spawnRadius: 0,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(0f));
        _ = simulation.MinorActors.Spawn(
            cultureId: 1,
            sourceEntityId: 0,
            sourceRuleId: 0,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 2,
            r: -1,
            ttlPulses: 3,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(0f),
            tick: 0);
        _ = simulation.MinorActors.IncreaseWorkTier(1, maxTier: 3);
        using var stream = new MemoryStream();

        KernelSnapshot.SaveKernelState(simulation, stream);
        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);

        Assert.Equal(1, loaded.MinorActors.Data.RuleCount);
        Assert.Equal(1, loaded.MinorActors.State.ActiveCount);
        Assert.Equal(2, loaded.MinorActors.State.Q(1));
        Assert.Equal(-1, loaded.MinorActors.State.R(1));
        Assert.Equal(1, loaded.MinorActors.WorkTier(1));
    }

    [Fact]
    public void CultureVerbPrefersConnectedMassOverIsolatedTrace()
    {
        var simulation = CreateVerbSimulation();
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 5);
        SeedSourceEntity(simulation);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureExpansion,
            MinorActorVerbTargetingCodes.ReinforceHomeMass,
            PlaneCodes.Physical,
            radius: 5,
            sampleCount: 128,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        var connectedCells = new[] { (0, 0), (1, 0), (1, -1), (0, -1) };
        foreach (var (q, r) in connectedCells)
        {
            simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, q, r, FixedScalar.FromSingle(18f), tick: 0);
        }

        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, -2, 0, FixedScalar.FromSingle(18f), tick: 0);
        var connectedBefore = SumCultureRaw(simulation, connectedCells);
        var isolatedBefore = simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, -2, 0);

        _ = simulation.AdvanceTicks(1);

        Assert.True(SumCultureRaw(simulation, connectedCells) > connectedBefore);
        Assert.Equal(isolatedBefore, simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, -2, 0));
    }

    [Fact]
    public void CultureVerbReinforcesEntrenchedCoreBelowFullRawCap()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 1);
        SeedSourceEntity(simulation);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureExpansion,
            MinorActorVerbTargetingCodes.ReinforceHomeMass,
            PlaneCodes.Physical,
            radius: 1,
            sampleCount: 64,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        simulation.ModifyCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Physical,
            0,
            0,
            WorldState.CultureEntrenchedRawMass,
            tick: 0);
        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 4,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(5f),
            tick: 0);
        var before = simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0);

        _ = simulation.AdvanceTicks(1);

        var evt = Assert.Single(ReadMinorActorVerbEvents(simulation));
        Assert.Equal(TraceStatusCodes.Selected, evt.StatusCode);
        Assert.Equal((0, 0), (evt.Q, evt.R));
        Assert.True(simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0) > before);
    }

    [Fact]
    public void CultureVerbRepairsInteriorHoleInsideSupportedMass()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 1);
        SeedSourceEntity(simulation);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureExpansion,
            MinorActorVerbTargetingCodes.ReinforceHomeMass,
            PlaneCodes.Physical,
            radius: 1,
            sampleCount: 64,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        foreach (var cell in new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) })
        {
            simulation.ModifyCellCultureInfluenceRaw(
                CultureId,
                PlaneCodes.Physical,
                cell.Item1,
                cell.Item2,
                WorldState.CultureFullRawMass,
                tick: 0);
        }

        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 4,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(5f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        var evt = Assert.Single(ReadMinorActorVerbEvents(simulation));
        Assert.Equal(TraceStatusCodes.Selected, evt.StatusCode);
        Assert.Equal((0, 0), (evt.Q, evt.R));
        Assert.True(simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0) > FixedScalar.FromSingle(0f));
    }

    [Fact]
    public void CultureVerbActsLocallyInsteadOfTeleportingToBestFarMass()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 7);
        SeedSourceEntity(simulation);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureExpansion,
            MinorActorVerbTargetingCodes.ReinforceHomeMass,
            PlaneCodes.Physical,
            radius: 7,
            sampleCount: 128,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(18f), tick: 0);
        var farCells = new[] { (5, 0), (5, -1), (6, -1), (6, 0) };
        foreach (var (q, r) in farCells)
        {
            simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, q, r, FixedScalar.FromSingle(30f), tick: 0);
        }

        var farBefore = SumCultureRaw(simulation, farCells);
        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 4,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(5f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        Assert.True(HexDistance(0, 0, simulation.MinorActors.State.Q(1), simulation.MinorActors.State.R(1)) <= 1);
        Assert.Equal(farBefore, SumCultureRaw(simulation, farCells));
    }

    [Fact]
    public void CultureVerbExtendsSupportedBorderInsteadOfUnsupportedIsland()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 5);
        SeedSourceEntity(simulation);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureExpansion,
            MinorActorVerbTargetingCodes.ExtendConnectedBorder,
            PlaneCodes.Physical,
            radius: 5,
            sampleCount: 128,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        var supportedCells = new[] { (0, 0), (1, 0), (1, -1) };
        foreach (var (q, r) in supportedCells)
        {
            simulation.ModifyCellCultureInfluenceRaw(
                CultureId,
                PlaneCodes.Physical,
                q,
                r,
                WorldState.CultureEntrenchedRawMass,
                tick: 0);
        }

        var borderCells = new[] { (-1, 0), (-1, 1), (0, 1), (1, -2), (2, -1), (2, 0) };
        var borderBefore = SumCultureRaw(simulation, borderCells);
        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 40,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(5f),
            tick: 0);

        _ = simulation.AdvanceTicks(30);

        Assert.True(SumCultureRaw(simulation, borderCells) > borderBefore);
        Assert.Equal(FixedScalar.FromSingle(0f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, -4, 0));
    }

    [Fact]
    public void CultureVerbBridgesGapBetweenNearbySupportedPatches()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 4);
        SeedSourceEntity(simulation);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureExpansion,
            MinorActorVerbTargetingCodes.BridgeGap,
            PlaneCodes.Physical,
            radius: 4,
            sampleCount: 128,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        simulation.ModifyCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Physical,
            1,
            0,
            WorldState.CultureEntrenchedRawMass,
            tick: 0);
        simulation.ModifyCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Physical,
            -1,
            0,
            WorldState.CultureEntrenchedRawMass,
            tick: 0);
        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 4,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(5f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        Assert.True(simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0) > FixedScalar.FromSingle(0f));
        Assert.Equal((0, 0), (simulation.MinorActors.State.Q(1), simulation.MinorActors.State.R(1)));
    }

    [Fact]
    public void CultureCleanupVerbReducesEveryOtherCultureSourceInsideSupportedMass()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 4);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureCleanup,
            MinorActorVerbTargetingCodes.ReduceOtherCulture,
            PlaneCodes.Physical,
            radius: 4,
            sampleCount: 128,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        foreach (var cell in new[] { (0, 0), (1, 0), (0, -1), (1, -1) })
        {
            simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, cell.Item1, cell.Item2, FixedScalar.FromSingle(30f), tick: 0);
        }

        var foreignEntityA = simulation.CreateEntity(2, subtypeId: 1, cultureId: 2, 0, 0, 0, 0, 0, tick: 0);
        var foreignEntityB = simulation.CreateEntity(3, subtypeId: 1, cultureId: 3, 0, 0, 0, 0, 0, tick: 0);
        var ownEntity = simulation.CreateEntity(4, subtypeId: 1, cultureId: CultureId, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(foreignEntityA, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(12f), tick: 0);
        simulation.SetCellInfluence(foreignEntityB, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(9f), tick: 0);
        simulation.SetCellInfluence(ownEntity, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(8f), tick: 0);
        simulation.ModifyCellCultureLaborInfluenceRaw(2, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(7f), tick: 0);
        simulation.ModifyCellCultureLaborInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(6f), tick: 0);
        var foreignABefore = simulation.World.GetCellInfluence(foreignEntityA, PlaneCodes.Physical, 0, 0);
        var foreignBBefore = simulation.World.GetCellInfluence(foreignEntityB, PlaneCodes.Physical, 0, 0);
        var ownEntityBefore = simulation.World.GetCellInfluence(ownEntity, PlaneCodes.Physical, 0, 0);
        var foreignLaborBefore = simulation.World.GetCellCultureLaborInfluenceRaw(2, PlaneCodes.Physical, 0, 0);
        var ownLaborBefore = simulation.World.GetCellCultureLaborInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0);
        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: 0,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 4,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(5f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        var evt = Assert.Single(ReadMinorActorVerbEvents(simulation));
        Assert.Equal(MinorActorActionCodes.CultureCleanup, evt.ActionCode);
        Assert.Equal(MinorActorVerbTargetingCodes.ReduceOtherCulture, evt.TargetingCode);
        Assert.Equal(TraceStatusCodes.Selected, evt.StatusCode);
        Assert.True(simulation.World.GetCellInfluence(foreignEntityA, PlaneCodes.Physical, 0, 0) < foreignABefore);
        Assert.True(simulation.World.GetCellInfluence(foreignEntityB, PlaneCodes.Physical, 0, 0) < foreignBBefore);
        Assert.True(simulation.World.GetCellCultureLaborInfluenceRaw(2, PlaneCodes.Physical, 0, 0) < foreignLaborBefore);
        Assert.Equal(ownEntityBefore, simulation.World.GetCellInfluence(ownEntity, PlaneCodes.Physical, 0, 0));
        Assert.Equal(ownLaborBefore, simulation.World.GetCellCultureLaborInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void CultureCleanupVerbRequiresOwnCultureSupport()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 4);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureCleanup,
            MinorActorVerbTargetingCodes.ReduceOtherCulture,
            PlaneCodes.Physical,
            radius: 4,
            sampleCount: 128,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        simulation.ModifyCellCultureInfluenceRaw(2, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(20f), tick: 0);
        var foreignBefore = simulation.World.GetCellCultureInfluenceRaw(2, PlaneCodes.Physical, 0, 0);
        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: 0,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 4,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(5f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        var evt = Assert.Single(ReadMinorActorVerbEvents(simulation));
        Assert.Equal(TraceStatusCodes.NoTarget, evt.StatusCode);
        Assert.Equal(foreignBefore, simulation.World.GetCellCultureInfluenceRaw(2, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void LifecycleMaintenanceVerbRepairsSourceFromAgeLoadInsideSupportedMass()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        const int healthAttributeId = 10;
        simulation.RegisterAttribute(SourceKindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.Effects.RegisterLifecycle(SourceKindId, healthAttributeId, baseRate: FixedScalar.FromSingle(0f), ageCoefficient: FixedScalar.FromSingle(1f), variance: FixedScalar.FromSingle(0f));
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 2);
        var source = simulation.CreateEntity(SourceKindId, subtypeId: 1, cultureId: CultureId, 0, 0, 0, 0, 0, tick: 0);
        Assert.Equal(SourceEntityId, source);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.LifecycleMaintenance,
            MinorActorVerbTargetingCodes.MaintainSourceLifecycle,
            PlaneCodes.Physical,
            radius: 2,
            sampleCount: 64,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        foreach (var cell in new[] { (0, 0), (1, 0), (0, -1), (1, -1) })
        {
            simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, cell.Item1, cell.Item2, FixedScalar.FromSingle(30f), tick: 0);
        }

        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 4,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(100f),
            tick: 0);
        var before = simulation.World.GetAttribute(SourceEntityId, healthAttributeId);

        _ = simulation.AdvanceTicks(1);

        var evt = Assert.Single(ReadMinorActorVerbEvents(simulation));
        Assert.Equal(MinorActorActionCodes.LifecycleMaintenance, evt.ActionCode);
        Assert.Equal(MinorActorVerbTargetingCodes.MaintainSourceLifecycle, evt.TargetingCode);
        Assert.Equal(TraceStatusCodes.Selected, evt.StatusCode);
        Assert.True(evt.DeltaUnits > FixedScalar.Zero.Units);
        Assert.True(simulation.World.GetAttribute(SourceEntityId, healthAttributeId) > before);
    }

    [Fact]
    public void VerbLayerSamplesWeightedCandidatesInsteadOfAlwaysTakingHighestScore()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 1);
        SeedSourceEntity(simulation);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureExpansion,
            MinorActorVerbTargetingCodes.ReinforceHomeMass,
            PlaneCodes.Physical,
            radius: 1,
            sampleCount: 64,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(0.01f));
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId + 1,
            MinorActorActionCodes.CultureCleanup,
            MinorActorVerbTargetingCodes.ReduceOtherCulture,
            PlaneCodes.Physical,
            radius: 1,
            sampleCount: 64,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(0.01f));
        foreach (var cell in new[] { (0, 0), (1, 0), (0, -1), (-1, 0), (0, 1), (1, -1), (-1, 1) })
        {
            simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, cell.Item1, cell.Item2, FixedScalar.FromSingle(100f), tick: 0);
            simulation.ModifyCellCultureInfluenceRaw(2, PlaneCodes.Physical, cell.Item1, cell.Item2, FixedScalar.FromSingle(100f), tick: 0);
        }

        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 60,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(5f),
            tick: 0);

        _ = simulation.AdvanceTicks(50);

        var selectedActions = ReadMinorActorVerbEvents(simulation)
            .Where(e => e.StatusCode == TraceStatusCodes.Selected)
            .Select(e => e.ActionCode)
            .ToHashSet();
        Assert.Contains(MinorActorActionCodes.CultureExpansion, selectedActions);
        Assert.Contains(MinorActorActionCodes.CultureCleanup, selectedActions);
    }

    [Fact]
    public void LifecycleMaintenanceVerbRequiresSupportedCultureMass()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        const int healthAttributeId = 10;
        simulation.RegisterAttribute(SourceKindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.Effects.RegisterLifecycle(SourceKindId, healthAttributeId, baseRate: FixedScalar.FromSingle(0f), ageCoefficient: FixedScalar.FromSingle(1f), variance: FixedScalar.FromSingle(0f));
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 2);
        var source = simulation.CreateEntity(SourceKindId, subtypeId: 1, cultureId: CultureId, 0, 0, 0, 0, 0, tick: 0);
        Assert.Equal(SourceEntityId, source);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.LifecycleMaintenance,
            MinorActorVerbTargetingCodes.MaintainSourceLifecycle,
            PlaneCodes.Physical,
            radius: 2,
            sampleCount: 64,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 4,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(100f),
            tick: 0);
        var before = simulation.World.GetAttribute(SourceEntityId, healthAttributeId);

        _ = simulation.AdvanceTicks(1);

        var evt = Assert.Single(ReadMinorActorVerbEvents(simulation));
        Assert.Equal(TraceStatusCodes.NoTarget, evt.StatusCode);
        Assert.True(simulation.World.GetAttribute(SourceEntityId, healthAttributeId) < before);
    }

    [Fact]
    public void TransportRouteCultureVerbPrefersTraveledRouteCell()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        simulation.World.SetMapRadius(2);
        simulation.RegisterTerrain(
            terrainId: 1,
            boundaryGroupId: 1,
            movementCost: FixedScalar.FromSingle(1f),
            influenceMultiplier: FixedScalar.FromSingle(1f),
            claimCostMultiplier: FixedScalar.FromSingle(1f),
            claimChanceMultiplier: FixedScalar.FromSingle(1f),
            roadBuildMultiplier: FixedScalar.FromSingle(20f),
            roadSpeedMultiplier: FixedScalar.FromSingle(2f));
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 2);
        SeedSourceEntity(simulation);
        var travelerId = simulation.CreateEntity(2, subtypeId: 1, cultureId: CultureId, 0, 0, 0, 0, 0, tick: 0);
        var move = simulation.MoveEntityPhysical(travelerId, 1, 0, tick: 0, MovementCauseCodes.Travel);
        Assert.True(move.Moved);
        var routeQ = move.ToQ;
        var routeR = move.ToR;
        _ = simulation.AdvanceTicks(1);
        Assert.True(simulation.Movement.RoadQuality(PlaneCodes.Physical, routeQ, routeR, simulation.CurrentTick) > FixedScalar.FromSingle(0f));
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(18f), tick: simulation.CurrentTick);
        var rule = RegisterRouteEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureExpansion,
            MinorActorVerbTargetingCodes.ExtendConnectedBorder,
            PlaneCodes.Physical,
            radius: 2,
            sampleCount: 128,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        SpawnRouteWorker(simulation, rule);

        _ = simulation.AdvanceTicks(1);

        var evt = Assert.Single(ReadMinorActorVerbEvents(simulation));
        Assert.Equal(CultureVerbId, evt.VerbId);
        Assert.Equal(MinorActorActionCodes.CultureExpansion, evt.ActionCode);
        Assert.Equal(TraceStatusCodes.Selected, evt.StatusCode);
        Assert.Equal((routeQ, routeR), (evt.Q, evt.R));
        Assert.Equal((routeQ, routeR), (simulation.MinorActors.State.Q(1), simulation.MinorActors.State.R(1)));
        Assert.True(simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, routeQ, routeR) > FixedScalar.FromSingle(0f));
    }

    [Fact]
    public void CultureVerbEmitsSelectionTelemetry()
    {
        var simulation = CreateVerbSimulation();
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 4);
        SeedSourceEntity(simulation);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureExpansion,
            MinorActorVerbTargetingCodes.ReinforceHomeMass,
            PlaneCodes.Physical,
            radius: 4,
            sampleCount: 96,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 1, 0, FixedScalar.FromSingle(24f), tick: 0);
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 1, -1, FixedScalar.FromSingle(24f), tick: 0);

        _ = simulation.AdvanceTicks(1);

        var evt = Assert.Single(ReadMinorActorVerbEvents(simulation));
        Assert.Equal(CultureVerbId, evt.VerbId);
        Assert.Equal(MinorActorVerbTargetingCodes.ReinforceHomeMass, evt.TargetingCode);
        Assert.Equal(TraceStatusCodes.Selected, evt.StatusCode);
        Assert.Equal(0, evt.SourceWorkTier);
        Assert.Equal(4, evt.EffectiveRadius);
        Assert.True(evt.ScoreUnits > FixedScalar.Zero.Units);
        Assert.True(evt.DeltaUnits > FixedScalar.Zero.Units);
        Assert.True(evt.ConnectedRawMassUnits > FixedScalar.Zero.Units);
    }

    [Fact]
    public void VerbLayerReadsCurrentTargetCultureLocality()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 1);
        SeedSourceEntity(simulation);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureExpansion,
            MinorActorVerbTargetingCodes.ReinforceHomeMass,
            PlaneCodes.Physical,
            radius: 1,
            sampleCount: 64,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(80f), tick: 0);
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 1, 0, FixedScalar.FromSingle(40f), tick: 0);
        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 4,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(0f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        var evt = Assert.Single(ReadMinorActorVerbEvents(simulation));
        Assert.Equal(TraceStatusCodes.NoReward, evt.StatusCode);
        var locality = simulation.CultureLocality.CellAt(
            simulation.World,
            CultureId,
            PlaneCodes.Physical,
            evt.Q,
            evt.R);

        Assert.Equal(locality.CellKindCode, evt.CellKindCode);
        Assert.Equal(locality.LocalRawMass.Units, evt.LocalRawMassUnits);
        Assert.Equal(locality.ConnectedRawMass.Units, evt.ConnectedRawMassUnits);
    }

    private static Simulation CreateVerbSimulation(int mainTickRatio = 1) =>
        new(new SimulationOptions
        {
            MinorActorCapacity = 8,
            MinorActorRuleCapacity = 8,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    MinorActorSubstepsPerPulse = mainTickRatio,
                    // These tests verify verb targeting; field decay would
                    // erode the small synthetic deltas they assert on.
                    CultureField = new CultureFieldTuningOptions
                    {
                        DecayPerPulse = FixedScalar.FromSingle(0f),
                        MinimumDecayPerPulse = FixedScalar.FromSingle(0f),
                    },
                },
            },
        });

    private static KernelTuningOptions CultureInfluenceActionScaleTuning(float scale) =>
        new()
        {
            Programs = new KernelProgramTuningOptions
            {
                CultureInfluenceActionScale = FixedScalar.FromSingle(scale),
            },
        };

    private static void SeedSourceEntity(Simulation simulation) =>
        simulation.SeedEntity(
            SourceEntityId,
            SourceKindId,
            subtypeId: 1,
            cultureId: CultureId,
            physicalQ: 0,
            physicalR: 0,
            semanticQ: 0,
            semanticR: 0,
            semanticS: 0);

    private static int RegisterCultureEmitter(Simulation simulation) =>
        simulation.MinorActors.RegisterEmitterRule(
            sourceKindId: SourceKindId,
            sourceSubtypeId: 0,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            ttlPulses: 4,
            emitCadencePulses: 10,
            maxActivePerSource: 1,
            spawnRadius: 0,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(5f));

    private static int RegisterRouteEmitter(Simulation simulation) =>
        simulation.MinorActors.RegisterEmitterRule(
            sourceKindId: SourceKindId,
            sourceSubtypeId: 0,
            purposeCode: MinorActorPurposeCodes.RoadLabor,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.RoadMaintenance,
            planeCode: PlaneCodes.Physical,
            ttlPulses: 4,
            emitCadencePulses: 10,
            maxActivePerSource: 1,
            spawnRadius: 0,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(5f));

    private static void SpawnRouteWorker(Simulation simulation, int rule) =>
        _ = simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.RoadMaintenance,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 4,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(5f),
            tick: 0,
            purposeCode: MinorActorPurposeCodes.RoadLabor);

    private static void SetKnownDisc(Simulation simulation, int planeCode, int centerQ, int centerR, int radius)
    {
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = Math.Max(-radius, -dq - radius);
            var maxDr = Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                simulation.World.SetCell(planeCode, centerQ + dq, centerR + dr, terrainId: 1);
            }
        }
    }

    private static FixedScalar SumCultureRaw(
        Simulation simulation,
        IEnumerable<(int Q, int R)> cells)
    {
        var total = FixedScalar.Zero;
        foreach (var cell in cells)
        {
            total += simulation.World.GetCellCultureInfluenceRaw(
                CultureId,
                PlaneCodes.Physical,
                cell.Q,
                cell.R);
        }

        return total;
    }

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        return (Math.Abs(aq - bq) +
            Math.Abs(ar - br) +
            Math.Abs(asCoord - bsCoord)) / 2;
    }

    private static List<MinorActorVerbExecutedKernelEvent> ReadMinorActorVerbEvents(Simulation simulation)
    {
        var events = new List<MinorActorVerbExecutedKernelEvent>();
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        while (simulation.Events.TryRead(cursor, out var sequenced))
        {
            if (sequenced.Event.EventType == KernelEventType.MinorActorVerbExecuted)
            {
                events.Add(sequenced.Event.AsMinorActorVerbExecuted());
            }
        }

        return events;
    }
}
