namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed class ChronicleProgramRuntimeTests
{
    private const int MonsterKind = 1;
    private const int PenguinKind = 2;
    private const int HoldsRel = 3;
    private const int AtWarRel = 4;
    private const int ArchetypeId = 50;
    private const int PairBeatCode = 101;
    private const int DeathBeatCode = 102;
    private const int DeathOutcomeCode = 103;
    private const int FadeOutcomeCode = 104;
    private const int SlayGambitId = 7;
    private const int WarGeneratorId = 61;

    [Fact]
    public void Chronicle_CastsStrikesOnExecutionAndResolvesOnRecognizedDeath()
    {
        var simulation = CreateSimulation();
        var (quarry, hunter) = SeedHuntWorld(simulation);
        DefineHuntArchetype(simulation);

        simulation.Chronicles.Tick(simulation, tick: 20);
        Assert.True(simulation.Chronicles.State.Active[1]);
        Assert.Equal(quarry, simulation.Chronicles.State.RoleEntityIds[ChronicleProgramData.MaxRolesPerArchetype]);

        // The pair beat opens at cast and strikes when the gambit program
        // reports the hunter acting on the quarry.
        simulation.Chronicles.Tick(simulation, tick: 25);
        var heatBefore = simulation.Chronicles.State.Heat[1];
        simulation.Chronicles.NoteExecution(hunter, quarry, SlayGambitId, succeeded: true);
        simulation.Chronicles.Tick(simulation, tick: 30);
        Assert.True(simulation.Chronicles.State.Heat[1] > heatBefore);
        Assert.NotEqual(0L, simulation.Chronicles.State.StruckMasks[1]);

        // The terminal beat recognizes the quarry's death; the director
        // resolves without writing any world state of its own.
        simulation.SetEntityStatus(quarry, EntityStatusCodes.Historical, tick: 35);
        simulation.Chronicles.Tick(simulation, tick: 35);
        Assert.False(simulation.Chronicles.State.Active[1]);
        AssertResolvedWithOutcome(simulation, DeathOutcomeCode);
    }

    [Fact]
    public void Chronicle_ChargesAndGrantsOnlyWhileBeatIsOpen()
    {
        var simulation = CreateSimulation();
        var (quarry, hunter) = SeedHuntWorld(simulation);
        DefineHuntArchetype(simulation);

        Assert.Equal(FixedScalar.FromSingle(0f), simulation.Chronicles.ScoreBias(hunter, quarry, SlayGambitId));
        Assert.False(simulation.Chronicles.GrantsGambit(hunter, SlayGambitId));

        simulation.Chronicles.Tick(simulation, tick: 20);
        simulation.Chronicles.Tick(simulation, tick: 25);

        Assert.Equal(FixedScalar.FromSingle(2f), simulation.Chronicles.ScoreBias(hunter, quarry, SlayGambitId));
        Assert.Equal(FixedScalar.FromSingle(0.3f), simulation.Chronicles.SuccessBonus(hunter, quarry, SlayGambitId));
        Assert.True(simulation.Chronicles.GrantsGambit(hunter, SlayGambitId));
        Assert.False(simulation.Chronicles.GrantsGambit(quarry, SlayGambitId));

        // Striking the beat closes it; its charge and grant fall away.
        simulation.Chronicles.NoteExecution(hunter, quarry, SlayGambitId, succeeded: true);
        simulation.Chronicles.Tick(simulation, tick: 30);
        Assert.Equal(FixedScalar.FromSingle(0f), simulation.Chronicles.ScoreBias(hunter, quarry, SlayGambitId));
        Assert.Equal(FixedScalar.FromSingle(0f), simulation.Chronicles.SuccessBonus(hunter, quarry, SlayGambitId));
        Assert.False(simulation.Chronicles.GrantsGambit(hunter, SlayGambitId));
    }

    [Fact]
    public void Chronicle_RoleGambitAndGeneratorFireStrikes()
    {
        var simulation = CreateSimulation();
        var (quarry, hunter) = SeedHuntWorld(simulation);
        var archetype = DefineMinimalArchetype(simulation);
        _ = simulation.Chronicles.AddBeat(
            archetype,
            PairBeatCode,
            heatGain: FixedScalar.FromSingle(2f),
            terminalOutcomeCodeId: 0,
            openKind: ChronicleConditionCodes.Always,
            openRoleA: 0,
            openRoleB: 0,
            openRelOrAttrId: 0,
            openValue: FixedScalar.FromSingle(0f),
            strikeKind: ChronicleStrikeCodes.RoleGambit,
            strikeActorRole: 1,
            strikeTargetRole: 0,
            strikeGambitId: SlayGambitId,
            strikeAcceptFailure: 0,
            strikeJoinRole: 0,
            strikeFactKind: ChronicleConditionCodes.None,
            strikeFactRoleA: 0,
            strikeFactRoleB: 0,
            strikeFactRelOrAttrId: 0,
            strikeFactValue: FixedScalar.FromSingle(0f));
        _ = simulation.Chronicles.AddBeat(
            archetype,
            DeathBeatCode,
            heatGain: FixedScalar.FromSingle(2f),
            terminalOutcomeCodeId: 0,
            openKind: ChronicleConditionCodes.Always,
            openRoleA: 0,
            openRoleB: 0,
            openRelOrAttrId: 0,
            openValue: FixedScalar.FromSingle(0f),
            strikeKind: ChronicleStrikeCodes.GeneratorOnRole,
            strikeActorRole: 0,
            strikeTargetRole: 0,
            strikeGambitId: WarGeneratorId,
            strikeAcceptFailure: 0,
            strikeJoinRole: 0,
            strikeFactKind: ChronicleConditionCodes.None,
            strikeFactRoleA: 0,
            strikeFactRoleB: 0,
            strikeFactRelOrAttrId: 0,
            strikeFactValue: FixedScalar.FromSingle(0f));

        simulation.Chronicles.Tick(simulation, tick: 20);
        Assert.True(simulation.Chronicles.State.Active[1]);

        // The hunter acts on an unrelated third party: RoleGambit strikes.
        simulation.Chronicles.NoteExecution(hunter, 999, SlayGambitId, succeeded: true);
        // The wrong generator on the quarry does not strike; the right one does.
        simulation.Chronicles.NoteGeneratorFire(quarry, WarGeneratorId + 1);
        simulation.Chronicles.NoteGeneratorFire(quarry, WarGeneratorId);
        simulation.Chronicles.Tick(simulation, tick: 25);

        Assert.Equal(0b11L, simulation.Chronicles.State.StruckMasks[1]);
    }

    private static int DefineMinimalArchetype(Simulation simulation)
    {
        var archetype = simulation.Chronicles.AddArchetype(
            ArchetypeId,
            maxActive: 1,
            castingCadenceTicks: 10,
            primaryKindId: MonsterKind,
            primaryRequiredRelKindId: HoldsRel,
            primaryRequiredRelMinStrength: FixedScalar.FromSingle(0.5f),
            fadeOutcomeCodeId: FadeOutcomeCode);
        simulation.Chronicles.AddRole(archetype, PenguinKind, AtWarRel, relatedIncoming: 0, relatedMinStrength: FixedScalar.FromSingle(0.3f));
        return archetype;
    }

    [Fact]
    public void Chronicle_EntranceBeatJoinsQualifiedOutsiderToOpenRole()
    {
        var simulation = CreateSimulation();
        var (quarry, hunter) = SeedHuntWorld(simulation);
        var second = simulation.CreateEntity(PenguinKind, 1, 1, 0, 3, 0, 0, 0, 0);
        var archetype = DefineMinimalArchetype(simulation);
        simulation.Chronicles.AddRole(archetype, PenguinKind, 0, relatedIncoming: 0, relatedMinStrength: FixedScalar.FromSingle(0f), open: 1);
        _ = simulation.Chronicles.AddBeat(
            archetype,
            PairBeatCode,
            heatGain: FixedScalar.FromSingle(2.5f),
            terminalOutcomeCodeId: 0,
            openKind: ChronicleConditionCodes.Always,
            openRoleA: 0,
            openRoleB: 0,
            openRelOrAttrId: 0,
            openValue: FixedScalar.FromSingle(0f),
            strikeKind: ChronicleStrikeCodes.OutsiderGambitOnRole,
            strikeActorRole: 0,
            strikeTargetRole: 0,
            strikeGambitId: 0,
            strikeAcceptFailure: 1,
            strikeJoinRole: 2,
            strikeFactKind: ChronicleConditionCodes.None,
            strikeFactRoleA: 0,
            strikeFactRoleB: 0,
            strikeFactRelOrAttrId: 0,
            strikeFactValue: FixedScalar.FromSingle(0f));

        simulation.Chronicles.Tick(simulation, tick: 20);
        Assert.True(simulation.Chronicles.State.Active[1]);

        // A wrong-kind outsider does not enter; the entrance beat holds.
        var monsterOutsider = simulation.CreateEntity(MonsterKind, 1, 1, 0, 4, 0, 0, 0, 0);
        simulation.Chronicles.NoteExecution(monsterOutsider, quarry, SlayGambitId, succeeded: true);
        simulation.Chronicles.Tick(simulation, tick: 25);
        Assert.Equal(0L, simulation.Chronicles.State.StruckMasks[1]);

        // A qualified outsider penguin acting on the quarry joins the cast.
        simulation.Chronicles.NoteExecution(second, quarry, SlayGambitId, succeeded: true);
        simulation.Chronicles.Tick(simulation, tick: 30);
        Assert.NotEqual(0L, simulation.Chronicles.State.StruckMasks[1]);
        Assert.Equal(second, simulation.Chronicles.State.RoleEntityIds[ChronicleProgramData.MaxRolesPerArchetype + 2]);
        Assert.NotEqual(hunter, second);
    }

    [Fact]
    public void Chronicle_FadesWhenColdAndQuiet_AndSuppressesImmediateRecast()
    {
        var simulation = CreateSimulation();
        _ = SeedHuntWorld(simulation);
        DefineHuntArchetype(simulation);

        simulation.Chronicles.Tick(simulation, tick: 20);
        Assert.True(simulation.Chronicles.State.Active[1]);

        // Decay the heat well below the fade threshold, then step past the
        // grace and quiet windows with no strikes. The tale fades mid-loop.
        for (var pulse = 0; pulse < 400; pulse++)
        {
            simulation.Chronicles.Tick(simulation, tick: 25 + (pulse * 5));
        }

        Assert.False(simulation.Chronicles.State.Active[1]);
        AssertResolvedWithOutcome(simulation, FadeOutcomeCode);

        // The faded cast sits in the suppression ring: the same primary is
        // not immediately recast even though the world still qualifies it.
        simulation.Chronicles.Tick(simulation, tick: 2030);
        Assert.False(simulation.Chronicles.State.Active[1]);
    }

    private static void DefineHuntArchetype(Simulation simulation)
    {
        var archetype = simulation.Chronicles.AddArchetype(
            ArchetypeId,
            maxActive: 1,
            castingCadenceTicks: 10,
            primaryKindId: MonsterKind,
            primaryRequiredRelKindId: HoldsRel,
            primaryRequiredRelMinStrength: FixedScalar.FromSingle(0.5f),
            fadeOutcomeCodeId: FadeOutcomeCode);
        simulation.Chronicles.AddRole(archetype, PenguinKind, AtWarRel, relatedIncoming: 0, relatedMinStrength: FixedScalar.FromSingle(0.3f));
        var pairBeat = simulation.Chronicles.AddBeat(
            archetype,
            PairBeatCode,
            heatGain: FixedScalar.FromSingle(5f),
            terminalOutcomeCodeId: 0,
            openKind: ChronicleConditionCodes.Always,
            openRoleA: 0,
            openRoleB: 0,
            openRelOrAttrId: 0,
            openValue: FixedScalar.FromSingle(0f),
            strikeKind: ChronicleStrikeCodes.RolePairGambit,
            strikeActorRole: 1,
            strikeTargetRole: 0,
            strikeGambitId: 0,
            strikeAcceptFailure: 0,
            strikeJoinRole: 0,
            strikeFactKind: ChronicleConditionCodes.None,
            strikeFactRoleA: 0,
            strikeFactRoleB: 0,
            strikeFactRelOrAttrId: 0,
            strikeFactValue: FixedScalar.FromSingle(0f));
        simulation.Chronicles.AddBeatCharge(pairBeat, actorRole: 1, targetRole: 0, gambitId: SlayGambitId, scoreBonus: FixedScalar.FromSingle(2f), heatScaled: 0, successBonus: FixedScalar.FromSingle(0.3f));
        simulation.Chronicles.AddBeatGrant(pairBeat, role: 1, gambitId: SlayGambitId);
        _ = simulation.Chronicles.AddBeat(
            archetype,
            DeathBeatCode,
            heatGain: FixedScalar.FromSingle(3f),
            terminalOutcomeCodeId: DeathOutcomeCode,
            openKind: ChronicleConditionCodes.Always,
            openRoleA: 0,
            openRoleB: 0,
            openRelOrAttrId: 0,
            openValue: FixedScalar.FromSingle(0f),
            strikeKind: ChronicleStrikeCodes.StateFact,
            strikeActorRole: 0,
            strikeTargetRole: 0,
            strikeGambitId: 0,
            strikeAcceptFailure: 0,
            strikeJoinRole: 0,
            strikeFactKind: ChronicleConditionCodes.RoleHistorical,
            strikeFactRoleA: 0,
            strikeFactRoleB: 0,
            strikeFactRelOrAttrId: 0,
            strikeFactValue: FixedScalar.FromSingle(0f));
    }

    private static (int Quarry, int Hunter) SeedHuntWorld(Simulation simulation)
    {
        var quarry = simulation.CreateEntity(MonsterKind, 1, 1, 0, 0, 0, 0, 0, 0);
        var hoard = simulation.CreateEntity(MonsterKind, 1, 1, 0, 1, 0, 0, 0, 0);
        var hunter = simulation.CreateEntity(PenguinKind, 1, 1, 0, 2, 0, 0, 0, 0);
        simulation.CreateRelationship(HoldsRel, RelationshipPolarityCodes.Positive, quarry, hoard, FixedScalar.FromSingle(0.8f), tick: 0);
        simulation.CreateRelationship(AtWarRel, RelationshipPolarityCodes.Negative, hunter, quarry, FixedScalar.FromSingle(0.6f), tick: 0);
        return (quarry, hunter);
    }

    private static void AssertResolvedWithOutcome(Simulation simulation, int outcomeCodeId)
    {
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var resolvedOutcome = -1;
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType != KernelEventType.ChronicleChanged)
            {
                continue;
            }

            var evt = read.Event.AsChronicleChanged();
            if (evt.EventTypeCode == ChronicleEventTypeCodes.Resolved)
            {
                resolvedOutcome = evt.OutcomeCodeId;
            }
        }

        Assert.Equal(outcomeCodeId, resolvedOutcome);
    }

    private static Simulation CreateSimulation() => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 8,
        RelationshipCapacity = 8,
        RelationshipKindIdCapacity = 16,
        EventCapacity = 4096,
    });
}
