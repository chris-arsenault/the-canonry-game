namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Eventing;
using Engine.Kernel;

public sealed class WorldBeatsProgramRuntimeTests
{
    private const int FactionKind = 1;
    private const int WarRel = 4;
    private const int FallbackAgeId = 300;
    private const int StrivingAgeId = 301;
    private const int AccordsAgeId = 302;
    private const int WarGeneratorId = 61;

    [Fact]
    public void WorldBeats_FallbackOpensAndAtLeastOneTrackStaysOccupied()
    {
        var simulation = CreateSimulation();
        DefineFallbackAge(simulation);

        simulation.WorldBeats.Tick(simulation, tick: 10);

        Assert.Equal(FallbackAgeId, simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.World));
        Assert.Equal(0, simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.Peoples));
        AssertPhaseEmitted(simulation, FallbackAgeId, WorldBeatPhaseCodes.Opened);
    }

    [Fact]
    public void WorldBeats_AgeOpensOnRecognizedFactsAndClosesOnRecognizedFacts()
    {
        var simulation = CreateSimulation();
        DefineStrivingAge(simulation, minOpenTicks: 0, cooldownTicks: 0);
        var (a, b) = SeedFactions(simulation);

        // No wars: striving does not open.
        simulation.WorldBeats.Tick(simulation, tick: 10);
        Assert.Equal(0, simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.Peoples));

        // Two living wars: striving is recognized open.
        var war1 = simulation.CreateRelationship(WarRel, RelationshipPolarityCodes.Negative, a, b, FixedScalar.FromSingle(0.8f), tick: 12);
        var war2 = simulation.CreateRelationship(WarRel, RelationshipPolarityCodes.Negative, b, a, FixedScalar.FromSingle(0.8f), tick: 12);
        simulation.WorldBeats.Tick(simulation, tick: 20);
        Assert.Equal(StrivingAgeId, simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.Peoples));
        AssertPhaseEmitted(simulation, StrivingAgeId, WorldBeatPhaseCodes.Opened);

        // Wars end: striving closes on the recognized fact, never a timer.
        simulation.CullRelationship(war1, tick: 30);
        simulation.CullRelationship(war2, tick: 30);
        simulation.WorldBeats.Tick(simulation, tick: 40);
        Assert.Equal(0, simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.Peoples));
        AssertPhaseEmitted(simulation, StrivingAgeId, WorldBeatPhaseCodes.Closed);
    }

    [Fact]
    public void WorldBeats_MinOpenSpanAndCooldownGateTransitions()
    {
        var simulation = CreateSimulation();
        DefineStrivingAge(simulation, minOpenTicks: 100, cooldownTicks: 200);
        var (a, b) = SeedFactions(simulation);
        var war1 = simulation.CreateRelationship(WarRel, RelationshipPolarityCodes.Negative, a, b, FixedScalar.FromSingle(0.8f), tick: 0);
        var war2 = simulation.CreateRelationship(WarRel, RelationshipPolarityCodes.Negative, b, a, FixedScalar.FromSingle(0.8f), tick: 0);

        simulation.WorldBeats.Tick(simulation, tick: 10);
        Assert.Equal(StrivingAgeId, simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.Peoples));

        // Close condition holds, but the minimum span has not elapsed.
        simulation.CullRelationship(war1, tick: 20);
        simulation.CullRelationship(war2, tick: 20);
        Assert.Equal(0, simulation.WorldBeats.State.ActiveRelationshipCountsByKind[WarRel]);
        Assert.Equal(2, simulation.WorldBeats.State.EndedRelationshipCountsByKind[WarRel]);
        simulation.WorldBeats.Tick(simulation, tick: 30);
        Assert.Equal(StrivingAgeId, simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.Peoples));

        // Past the minimum span it closes; during cooldown it cannot reopen.
        simulation.WorldBeats.Tick(simulation, tick: 120);
        Assert.Equal(0, simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.Peoples));
        _ = simulation.CreateRelationship(WarRel, RelationshipPolarityCodes.Negative, a, b, FixedScalar.FromSingle(0.8f), tick: 130);
        _ = simulation.CreateRelationship(WarRel, RelationshipPolarityCodes.Negative, b, a, FixedScalar.FromSingle(0.8f), tick: 130);
        simulation.WorldBeats.Tick(simulation, tick: 140);
        Assert.Equal(0, simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.Peoples));
        simulation.WorldBeats.Tick(simulation, tick: 330);
        Assert.Equal(StrivingAgeId, simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.Peoples));
    }

    [Fact]
    public void WorldBeats_HigherPriorityAgeWinsABlankTrack()
    {
        var simulation = CreateSimulation();
        DefineStrivingAge(simulation, minOpenTicks: 0, cooldownTicks: 0);
        var accords = simulation.WorldBeats.AddAge(
            AccordsAgeId,
            WorldBeatTrackCodes.Peoples,
            priority: 1,
            minOpenTicks: 0,
            cooldownTicks: 0);
        _ = accords;
        var (a, b) = SeedFactions(simulation);
        _ = simulation.CreateRelationship(WarRel, RelationshipPolarityCodes.Negative, a, b, FixedScalar.FromSingle(0.8f), tick: 0);
        _ = simulation.CreateRelationship(WarRel, RelationshipPolarityCodes.Negative, b, a, FixedScalar.FromSingle(0.8f), tick: 0);

        // Both are eligible (accords has no conditions); striving's higher
        // priority wins the track.
        simulation.WorldBeats.Tick(simulation, tick: 10);
        Assert.Equal(StrivingAgeId, simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.Peoples));
    }

    [Fact]
    public void WorldBeats_ChargesPushWhileOpenAndResetWhenClosed()
    {
        var simulation = CreateSimulation();
        var striving = DefineStrivingAge(simulation, minOpenTicks: 0, cooldownTicks: 0);
        simulation.WorldBeats.AddCharge(striving, WorldBeatChargeDomainCodes.GambitScore, targetId: 7, multiplier: FixedScalar.FromSingle(3f));
        simulation.WorldBeats.AddCharge(striving, WorldBeatChargeDomainCodes.GeneratorFireWeight, WarGeneratorId, FixedScalar.FromSingle(2f));
        var gambitSlot = RegisterGambit(simulation, gambitId: 7);
        var (a, b) = SeedFactions(simulation);
        var war1 = simulation.CreateRelationship(WarRel, RelationshipPolarityCodes.Negative, a, b, FixedScalar.FromSingle(0.8f), tick: 0);
        var war2 = simulation.CreateRelationship(WarRel, RelationshipPolarityCodes.Negative, b, a, FixedScalar.FromSingle(0.8f), tick: 0);

        simulation.WorldBeats.Tick(simulation, tick: 10);
        Assert.Equal(FixedScalar.FromSingle(3f), simulation.Gambits.Data.AgeScoreMultipliers[gambitSlot]);

        simulation.CullRelationship(war1, tick: 20);
        simulation.CullRelationship(war2, tick: 20);
        simulation.WorldBeats.Tick(simulation, tick: 30);
        Assert.Equal(FixedScalar.FromSingle(1f), simulation.Gambits.Data.AgeScoreMultipliers[gambitSlot]);
    }

    private static void DefineFallbackAge(Simulation simulation) =>
        _ = simulation.WorldBeats.AddAge(
            FallbackAgeId,
            WorldBeatTrackCodes.World,
            priority: 0,
            minOpenTicks: 0,
            cooldownTicks: 0);

    private static int DefineStrivingAge(Simulation simulation, int minOpenTicks, int cooldownTicks)
    {
        var age = simulation.WorldBeats.AddAge(
            StrivingAgeId,
            WorldBeatTrackCodes.Peoples,
            priority: 10,
            minOpenTicks: minOpenTicks,
            cooldownTicks: cooldownTicks);
        simulation.WorldBeats.AddOpenCondition(
            age,
            WorldBeatFactCodes.LivingRelationshipsOfKind,
            WorldBeatComparatorCodes.AtLeast,
            value: FixedScalar.FromSingle(2f),
            paramId: WarRel);
        simulation.WorldBeats.AddCloseCondition(
            age,
            WorldBeatFactCodes.LivingRelationshipsOfKind,
            WorldBeatComparatorCodes.Below,
            value: FixedScalar.FromSingle(2f),
            paramId: WarRel);
        return age;
    }

    private static int RegisterGambit(Simulation simulation, int gambitId)
    {
        simulation.Gambits.ResetAgeScoreMultipliers();
        simulation.Gambits.Data.GambitIds[1] = gambitId;
        simulation.Gambits.Data.GambitCount = 1;
        return 1;
    }

    private static (int A, int B) SeedFactions(Simulation simulation)
    {
        var a = simulation.CreateEntity(FactionKind, 1, 1, 0, 0, 0, 0, 0, 0);
        var b = simulation.CreateEntity(FactionKind, 1, 1, 0, 1, 0, 0, 0, 0);
        return (a, b);
    }

    private static void AssertPhaseEmitted(Simulation simulation, int ageId, int phaseCode)
    {
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var found = false;
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType != KernelEventType.WorldBeatChanged)
            {
                continue;
            }

            var evt = read.Event.AsWorldBeatChanged();
            found |= evt.AgeId == ageId && evt.PhaseCode == phaseCode;
        }

        Assert.True(found);
    }

    private static Simulation CreateSimulation() => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 8,
        RelationshipCapacity = 16,
        RelationshipKindIdCapacity = 16,
        EventCapacity = 4096,
    });
}
