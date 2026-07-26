namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class MinorActorProgramRuntimeTests
{
    private const int CultureId = 1;
    private const int OtherCultureId = 2;
    private const int SourceEntityId = 1;
    private const int SourceKindId = 1;

    [Fact]
    public void CulturalFrontPersistsWithoutCreatingGraphEntities()
    {
        var simulation = CreateFrontSimulation();
        var rule = RegisterFrontRule(simulation);

        simulation.AdvanceTicks(30);

        Assert.Equal(1, simulation.MinorActors.State.ActiveCount);
        Assert.Equal(1, simulation.MinorActors.State.TotalCreated);
        Assert.Equal(1, simulation.World.EntityHighWatermark);
        var actorId = simulation.MinorActors.State.ActorForSourceRule(SourceEntityId, rule);
        Assert.True(actorId > 0);
        Assert.Equal(MinorActorRoleCodes.CulturalFront, simulation.MinorActors.State.RoleCode(actorId));
        Assert.NotEqual(MinorActorPostureCodes.Unassigned, simulation.MinorActors.State.PostureCode(actorId));
    }

    [Fact]
    public void SourceRetirementClearsPersistentActorAndOccurrenceHandle()
    {
        var simulation = CreateFrontSimulation();
        var rule = RegisterFrontRule(simulation);
        simulation.AdvanceTicks(1);
        var actorId = simulation.MinorActors.State.ActorForSourceRule(SourceEntityId, rule);
        var occurrenceHandle = simulation.MinorActors.State.OccurrenceHandle(actorId);

        simulation.RetireEntity(SourceEntityId, simulation.CurrentTick);

        Assert.Equal(0, simulation.MinorActors.State.ActiveCount);
        Assert.False(simulation.MinorActors.State.TryResolveOccurrence(occurrenceHandle, out _));
        Assert.Equal(1, simulation.MinorActors.State.TotalRetired);
    }

    [Fact]
    public void ReusedActorSlotGetsNewOccurrenceHandle()
    {
        var state = new MinorActorProgramState(
            actorCapacity: 2,
            sourceRuleCapacity: 2,
            sourceEntityCapacity: 2,
            relationshipCapacity: 2,
            mapCellCapacity: 4);
        var first = state.Create(
            CultureId,
            SourceEntityId,
            sourceEntityOccurrenceHandle: 1,
            sourceRuleId: 1,
            bindingRelationshipId: 0,
            bindingRelationshipOccurrenceHandle: 0,
            MinorActorRoleCodes.CulturalFront,
            PlaneCodes.Physical,
            q: 0,
            r: 0,
            cellId: 1,
            strength: 1,
            tick: 0);
        var firstHandle = state.OccurrenceHandle(first);
        Assert.True(state.Retire(first));

        var second = state.Create(
            CultureId,
            sourceEntityId: 2,
            sourceEntityOccurrenceHandle: 2,
            sourceRuleId: 1,
            bindingRelationshipId: 0,
            bindingRelationshipOccurrenceHandle: 0,
            MinorActorRoleCodes.CulturalFront,
            PlaneCodes.Physical,
            q: 1,
            r: 0,
            cellId: 2,
            strength: 1,
            tick: 1);

        Assert.Equal(first, second);
        Assert.NotEqual(firstHandle, state.OccurrenceHandle(second));
        Assert.False(state.TryResolveOccurrence(firstHandle, out _));
    }

    [Fact]
    public void SnapshotRoundTripsPersistentAssignmentAndOccurrenceIdentity()
    {
        var simulation = CreateFrontSimulation();
        _ = RegisterFrontRule(simulation);
        simulation.AdvanceTicks(1);
        var actorId = simulation.MinorActors.State.ActiveActorAt(0);
        var occurrenceHandle = simulation.MinorActors.State.OccurrenceHandle(actorId);
        var expectedTarget = (
            simulation.MinorActors.State.TargetQ(actorId),
            simulation.MinorActors.State.TargetR(actorId));
        using var stream = new MemoryStream();
        KernelSnapshot.SaveKernelState(simulation, stream);

        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);

        Assert.Equal(1, loaded.MinorActors.State.ActiveCount);
        var loadedActorId = loaded.MinorActors.State.ActiveActorAt(0);
        Assert.Equal(occurrenceHandle, loaded.MinorActors.State.OccurrenceHandle(loadedActorId));
        Assert.Equal(
            expectedTarget,
            (loaded.MinorActors.State.TargetQ(loadedActorId),
             loaded.MinorActors.State.TargetR(loadedActorId)));
    }

    [Fact]
    public void CampaignRelationshipOwnsExactlyOneColumn()
    {
        const int campaignKindId = 6;
        var simulation = CreateFrontSimulation();
        var target = simulation.CreateEntity(
            kindId: SourceKindId,
            subtypeId: 1,
            cultureId: OtherCultureId,
            physicalQ: 2,
            physicalR: 0,
            semanticQ: 2,
            semanticR: 0,
            semanticS: 0,
            tick: 0);
        simulation.ModifyCellCultureInfluenceRaw(
            OtherCultureId,
            PlaneCodes.Physical,
            2,
            0,
            200,
            tick: 0);
        _ = simulation.MinorActors.RegisterRoleRule(
            SourceKindId,
            sourceSubtypeId: 0,
            MinorActorRoleCodes.CampaignColumn,
            PlaneCodes.Physical,
            assignmentRadius: 3,
            stride: 2,
            commitmentTicks: 2,
            replanTicks: 3,
            baseStrength: 2);
        simulation.MinorActors.SetCampaignRelationshipKindId(campaignKindId);
        var relationshipId = simulation.CreateRelationship(
            campaignKindId,
            RelationshipPolarityCodes.Negative,
            SourceEntityId,
            target,
            strength: 1,
            tick: 0);

        simulation.AdvanceTicks(2);

        Assert.Equal(1, simulation.MinorActors.State.ActiveCount);
        Assert.True(simulation.MinorActors.State.ActorForRelationship(relationshipId) > 0);

        simulation.CullRelationship(relationshipId, simulation.CurrentTick);
        simulation.AdvanceTicks(1);

        Assert.Equal(0, simulation.MinorActors.State.ActiveCount);
    }

    [Fact]
    public void CulturalFrontWorksOnlyAtBorderOrContestOpportunity()
    {
        var simulation = CreateFrontSimulation();
        _ = RegisterFrontRule(simulation);
        var before = SumCulture(simulation, radius: 2);

        simulation.AdvanceTicks(4);

        Assert.True(SumCulture(simulation, radius: 2) > before);
        Assert.Equal(
            FixedScalar.FromSingle(200f),
            simulation.World.GetCellCultureInfluenceRaw(
                CultureId,
                PlaneCodes.Physical,
                0,
                0));
    }

    private static Simulation CreateFrontSimulation()
    {
        var simulation = new Simulation(new SimulationOptions
        {
            MinorActorCapacity = 16,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    CultureInfluenceActionScale = 1,
                    CultureField = new CultureFieldTuningOptions
                    {
                        DecayPerRun = 0,
                        MinimumDecayPerRun = 0,
                    },
                },
            },
        });
        SetKnownDisc(simulation, radius: 4);
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
        simulation.ModifyCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Physical,
            0,
            0,
            200,
            tick: 0);
        return simulation;
    }

    private static int RegisterFrontRule(Simulation simulation) =>
        simulation.MinorActors.RegisterRoleRule(
            SourceKindId,
            sourceSubtypeId: 0,
            MinorActorRoleCodes.CulturalFront,
            PlaneCodes.Physical,
            assignmentRadius: 2,
            stride: 2,
            commitmentTicks: 3,
            replanTicks: 4,
            baseStrength: 2);

    private static void SetKnownDisc(Simulation simulation, int radius)
    {
        for (var q = -radius; q <= radius; q++)
        {
            var minR = Math.Max(-radius, -q - radius);
            var maxR = Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.World.SetCell(PlaneCodes.Physical, q, r, terrainId: 1);
            }
        }
    }

    private static FixedScalar SumCulture(Simulation simulation, int radius)
    {
        var total = FixedScalar.Zero;
        for (var q = -radius; q <= radius; q++)
        {
            var minR = Math.Max(-radius, -q - radius);
            var maxR = Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                total += simulation.World.GetCellCultureInfluenceRaw(
                    CultureId,
                    PlaneCodes.Physical,
                    q,
                    r);
            }
        }

        return total;
    }
}
