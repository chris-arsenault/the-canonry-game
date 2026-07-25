namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// The macro-chronicle recognizer: each pulse it reads macro facts from world
/// state (population, culture mass and its growth, wars living and ended,
/// legendary resolutions), closes ages whose closing conditions hold, opens
/// the highest-priority eligible age on each blank track, and pushes the open
/// ages' rate charges into the target programs. Transitions are recognized,
/// never scheduled; the program itself writes no world state.
/// </summary>
internal sealed partial class WorldBeatsProgramRuntime : IKernelProgramRuntime<WorldBeatsProgramData, WorldBeatsProgramState>
{
    public WorldBeatsProgramRuntime(SimulationOptions options)
    {
        global::System.ArgumentNullException.ThrowIfNull(options);
        Data = new WorldBeatsProgramData();
        Data.Allocate(options.WorldBeatAgeCapacity, options.WorldBeatItemCapacity);
        State = new WorldBeatsProgramState(
            options.WorldBeatAgeCapacity,
            options.RelationshipKindIdCapacity);
    }

    public WorldBeatsProgramData Data { get; }

    public WorldBeatsProgramState State { get; }

    public int AddAge(int ageId, int trackCode, int priority, int minOpenTicks, int cooldownTicks) =>
        Data.AddAge(ageId, trackCode, priority, minOpenTicks, cooldownTicks);

    public void AddOpenCondition(int ageSlot, int factCode, int comparatorCode, FixedScalar value, int paramId) =>
        Data.AddOpenCondition(ageSlot, factCode, comparatorCode, value, paramId);

    public void AddCloseCondition(int ageSlot, int factCode, int comparatorCode, FixedScalar value, int paramId) =>
        Data.AddCloseCondition(ageSlot, factCode, comparatorCode, value, paramId);

    public void AddCharge(int ageSlot, int domainCode, int targetId, FixedScalar multiplier) =>
        Data.AddCharge(ageSlot, domainCode, targetId, multiplier);

    public int OpenAgeSlot(int trackCode) => State.OpenAgeSlots[trackCode];

    public int OpenAgeId(int trackCode)
    {
        var slot = State.OpenAgeSlots[trackCode];
        return slot == 0 ? 0 : Data.AgeId(slot);
    }

    public int OpenedTick(int trackCode) => State.OpenedTicks[trackCode];

    internal void NoteRelationshipCreated(int relationshipKindId)
    {
        if ((uint)relationshipKindId < (uint)State.ActiveRelationshipCountsByKind.Length)
        {
            State.ActiveRelationshipCountsByKind[relationshipKindId]++;
        }
    }

    internal void NoteRelationshipEnded(int relationshipKindId)
    {
        if ((uint)relationshipKindId >= (uint)State.ActiveRelationshipCountsByKind.Length)
        {
            return;
        }

        if (State.ActiveRelationshipCountsByKind[relationshipKindId] > 0)
        {
            State.ActiveRelationshipCountsByKind[relationshipKindId]--;
        }

        State.EndedRelationshipCountsByKind[relationshipKindId]++;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        if (Data.AgeCount == 0)
        {
            return;
        }

        SampleFacts(simulation, tick);
        CloseEligibleAges(simulation, tick);
        OpenEligibleAges(simulation, tick);
        PushCharges(simulation);
    }

    private void SampleFacts(Simulation simulation, int tick)
    {
        var mass = simulation.World.TotalPhysicalCultureRawMass;
        if (State.PreviousMassSampleTick >= 0 && State.PreviousTotalCultureMass > 0)
        {
            State.MassGrowthPermille =
                1000 * (mass - State.PreviousTotalCultureMass) / State.PreviousTotalCultureMass;
        }

        State.PreviousTotalCultureMass = mass;
        State.PreviousMassSampleTick = tick;
    }

    private void CloseEligibleAges(Simulation simulation, int tick)
    {
        for (var track = 0; track < WorldBeatTrackCodes.Count; track++)
        {
            var slot = State.OpenAgeSlots[track];
            if (slot == 0 ||
                tick - State.OpenedTicks[track] < Data.MinOpenTicks(slot) ||
                !AnyCloseConditionHolds(simulation, slot, tick))
            {
                continue;
            }

            State.OpenAgeSlots[track] = 0;
            State.LastClosedTicks[slot] = tick;
            simulation.EmitWorldBeatChanged(tick, track, Data.AgeId(slot), WorldBeatPhaseCodes.Closed);
        }
    }

    private void OpenEligibleAges(Simulation simulation, int tick)
    {
        for (var track = 0; track < WorldBeatTrackCodes.Count; track++)
        {
            var best = BestEligibleAge(simulation, track, tick);
            if (best == 0)
            {
                continue;
            }

            var current = State.OpenAgeSlots[track];
            if (current == best)
            {
                continue;
            }

            // A higher-priority eligible age preempts a lower-priority
            // incumbent (once the incumbent has met its minimum span), so a
            // low-priority fallback like the Still Ice never traps the track.
            if (current != 0 &&
                (Data.Priority(best) <= Data.Priority(current) ||
                 tick - State.OpenedTicks[track] < Data.MinOpenTicks(current)))
            {
                continue;
            }

            if (current != 0)
            {
                State.LastClosedTicks[current] = tick;
                simulation.EmitWorldBeatChanged(tick, track, Data.AgeId(current), WorldBeatPhaseCodes.Closed);
            }

            State.OpenAgeSlots[track] = best;
            State.OpenedTicks[track] = tick;
            simulation.EmitWorldBeatChanged(tick, track, Data.AgeId(best), WorldBeatPhaseCodes.Opened);
        }
    }

    private int BestEligibleAge(Simulation simulation, int track, int tick)
    {
        var best = 0;
        for (var slot = 1; slot <= Data.AgeCount; slot++)
        {
            if (Data.TrackCode(slot) != track ||
                (best != 0 && Data.Priority(slot) <= Data.Priority(best)) ||
                tick - State.LastClosedTicks[slot] < Data.CooldownTicks(slot) ||
                !AllOpenConditionsHold(simulation, slot, tick))
            {
                continue;
            }

            best = slot;
        }

        return best;
    }

    private bool AllOpenConditionsHold(Simulation simulation, int ageSlot, int tick)
    {
        var start = Data.OpenConditionStart(ageSlot);
        var count = Data.OpenConditionCount(ageSlot);
        for (var condition = start; condition < start + count; condition++)
        {
            if (!ConditionHolds(simulation, condition, tick))
            {
                return false;
            }
        }

        return true;
    }

    private bool AnyCloseConditionHolds(Simulation simulation, int ageSlot, int tick)
    {
        var start = Data.CloseConditionStart(ageSlot);
        var count = Data.CloseConditionCount(ageSlot);
        for (var condition = start; condition < start + count; condition++)
        {
            if (ConditionHolds(simulation, condition, tick))
            {
                return true;
            }
        }

        return false;
    }

    private bool ConditionHolds(Simulation simulation, int condition, int tick)
    {
        var value = FactValue(simulation, Data.ConditionFactCode(condition), Data.ConditionParamId(condition), tick);
        var threshold = Data.ConditionValue(condition);
        return Data.ConditionComparatorCode(condition) == WorldBeatComparatorCodes.AtLeast
            ? value >= threshold
            : value < threshold;
    }

    private FixedScalar FactValue(Simulation simulation, int factCode, int paramId, int tick)
    {
        return factCode switch
        {
            WorldBeatFactCodes.TicksSinceStart => tick,
            WorldBeatFactCodes.ActiveEntitiesOfKind => KernelQuery.ActiveEntityCountOfKind(simulation.World, paramId),
            WorldBeatFactCodes.LivingRelationshipsOfKind => LivingRelationships(paramId),
            WorldBeatFactCodes.CulledRelationshipsOfKind => CulledRelationships(paramId),
            WorldBeatFactCodes.TotalCultureRawMass => State.PreviousTotalCultureMass,
            WorldBeatFactCodes.CultureMassGrowthPermille => State.MassGrowthPermille,
            WorldBeatFactCodes.LegendaryResolutions => simulation.Chronicles.LegendaryResolutionCount,
            _ => 0,
        };
    }

    private int LivingRelationships(int relationshipKindId) =>
        relationshipKindId > 0 && relationshipKindId < State.ActiveRelationshipCountsByKind.Length
            ? State.ActiveRelationshipCountsByKind[relationshipKindId]
            : 0;

    private int CulledRelationships(int relationshipKindId) =>
        relationshipKindId > 0 && relationshipKindId < State.EndedRelationshipCountsByKind.Length
            ? State.EndedRelationshipCountsByKind[relationshipKindId]
            : 0;
}
