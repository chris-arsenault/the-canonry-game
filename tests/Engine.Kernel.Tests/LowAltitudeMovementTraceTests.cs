namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed class LowAltitudeMovementTraceTests
{
    [Fact]
    public void LowAltitudeVerbProgram_CommitsPhysicalTargetAndMovesWithVerbSource()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 11 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 77;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(3);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(1, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(2, 0, terrainId, ownerEntityId: 0);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 2, 0, 0, 0, 0, tick: 0);
        var verbSlot = RegisterMovementVerb(simulation, actorKindId, verbId, targetKindId);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.MoveActor);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        Assert.Equal((1, 0), (simulation.World.EntityPhysicalQ(actorId), simulation.World.EntityPhysicalR(actorId)));
        AssertMovedByProgram(
            simulation,
            cursor,
            actorId,
            KernelProgramCodes.LowAltitudeVerbs,
            simulation.LowAltitudeVerbSourceCodeId);
    }

    [Fact]
    public void LowAltitudeVerbProgram_EmitsMovementIntentTraceWithAffordanceAggregateAttribution()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 17, TraceEnabled = true });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 99;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var targetId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        RegisterPresenceAffordance(simulation, AffordanceSourceKindCodes.Generator, sourceId: 7, actorKindId, targetKindId);
        RegisterPresenceAffordance(simulation, AffordanceSourceKindCodes.Gambit, sourceId: 8, actorKindId, targetKindId);
        var verbSlot = RegisterMovementVerb(simulation, actorKindId, verbId, targetKindId);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.MoveActor);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var trace = AssertTrace(simulation, cursor, actorId);
        Assert.Equal(TraceSubjectCodes.MovementIntent, trace.TraceSubjectCode);
        Assert.Equal(ActionFamilyCodes.LowAltitudeVerb, trace.ActionFamilyCode);
        Assert.Equal(verbId, trace.ActionId);
        Assert.Equal(targetId, trace.TargetEntityId);
        Assert.Equal(TraceStatusCodes.Selected, trace.StatusCode);
        Assert.Equal(AffordanceSourceKindCodes.Generator, trace.AffordanceSourceKindCode);
        Assert.Equal(7, trace.AffordanceSourceId);
        Assert.Equal(LowAltitudeAffordanceShapeCodes.PresenceNearEntity, trace.AffordanceShapeCode);
        Assert.Equal(3, trace.AffordanceSourceKindMask);
        Assert.Equal(1, trace.AffordanceShapeMask);
        Assert.Equal(2, trace.AffordanceContributionCount);
        Assert.Equal(1, trace.GeneratorAffordanceCount);
        Assert.Equal(1, trace.GambitAffordanceCount);
        Assert.Equal(1, trace.HiddenAffordanceCount);
    }

    private static int RegisterMovementVerb(
        Simulation simulation,
        int actorKindId,
        int verbId,
        int targetKindId) =>
        simulation.LowAltitudeVerbs.RegisterVerb(
            actorKindId,
            verbId,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.MoveActor,
            TargetSelectorCodes.EntityKind,
            targetKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 3,
            maxCommitmentTicks: 3,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 1f,
            exploreWeight: 0f,
            stalenessWeight: 0f,
            hotspotWeight: 0f,
            crowdingWeight: 0f,
            affordanceWeight: 0f,
            influenceDelta: 0f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);

    private static void RegisterPresenceAffordance(
        Simulation simulation,
        int sourceKindCode,
        int sourceId,
        int actorKindId,
        int targetKindId) =>
        simulation.LowAltitudeVerbs.RegisterAffordance(
            sourceKindCode,
            sourceId,
            LowAltitudeAffordanceShapeCodes.PresenceNearEntity,
            actorKindId,
            PlaneCodes.Physical,
            targetKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: 0,
            operationCode: 0,
            terrainId: 0,
            polarityCode: AffordancePolarityCodes.None,
            threshold: 0f,
            weight: 0.5f);

    private static void SeedPhysicalNeighborhood(Simulation simulation, int terrainId)
    {
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        for (var direction = 0; direction < 6; direction++)
        {
            simulation.SeedCell(DirectionQ(direction), DirectionR(direction), terrainId, ownerEntityId: 0);
        }
    }

    private static void AddPrimitiveSteps(Simulation simulation, int verbSlot, int operationCode) =>
        simulation.LowAltitudeVerbs.AddPrimitiveStep(verbSlot, operationCode, 0, 0f);

    private static void AssertMovedByProgram(
        Simulation simulation,
        KernelEventCursor cursor,
        int entityId,
        int programId,
        int sourceCodeId)
    {
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType != KernelEventType.EntityMoved)
            {
                continue;
            }

            var moved = read.Event.AsEntityMoved();
            if (moved.EntityId != entityId)
            {
                continue;
            }

            Assert.Equal(programId, read.Event.ProgramId);
            Assert.Equal(sourceCodeId, read.Event.SourceCodeId);
            return;
        }

        Assert.Fail("Expected movement event attributed to the low-altitude verb program.");
    }

    private static TraceKernelEvent AssertTrace(
        Simulation simulation,
        KernelEventCursor cursor,
        int entityId)
    {
        var found = false;
        var selected = default(TraceKernelEvent);
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType != KernelEventType.Trace)
            {
                continue;
            }

            var trace = read.Event.AsTrace();
            if (trace.EntityId == entityId)
            {
                found = true;
                if (trace.StatusCode == TraceStatusCodes.Selected)
                {
                    selected = trace;
                }
            }
        }

        if (found)
        {
            return selected;
        }

        Assert.Fail("Expected movement intent trace event.");
        return selected;
    }

    private static int DirectionQ(int direction) => direction switch
    {
        0 => 1,
        1 => 1,
        2 => 0,
        3 => -1,
        4 => -1,
        _ => 0,
    };

    private static int DirectionR(int direction) => direction switch
    {
        0 => 0,
        1 => -1,
        2 => -1,
        3 => 0,
        4 => 1,
        _ => 1,
    };
}
