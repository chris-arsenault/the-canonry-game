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
    private readonly int[] _livingByRelationshipKind;
    private readonly int[] _culledByRelationshipKind;

    public WorldBeatsProgramRuntime(SimulationOptions options)
    {
        global::System.ArgumentNullException.ThrowIfNull(options);
        Data = new WorldBeatsProgramData();
        Data.Allocate(options.WorldBeatAgeCapacity, options.WorldBeatItemCapacity);
        State = new WorldBeatsProgramState(options.WorldBeatAgeCapacity);
        _livingByRelationshipKind = new int[options.RelationshipKindIdCapacity + 1];
        _culledByRelationshipKind = new int[options.RelationshipKindIdCapacity + 1];
    }

    public WorldBeatsProgramData Data { get; }

    public WorldBeatsProgramState State { get; }

    public int AddAge(int ageId, int trackCode, int priority, int minOpenTicks, int cooldownTicks) =>
        Data.AddAge(ageId, trackCode, priority, minOpenTicks, cooldownTicks);

    public void AddOpenCondition(int ageSlot, int factCode, int comparatorCode, float value, int paramId) =>
        Data.AddOpenCondition(ageSlot, factCode, comparatorCode, value, paramId);

    public void AddCloseCondition(int ageSlot, int factCode, int comparatorCode, float value, int paramId) =>
        Data.AddCloseCondition(ageSlot, factCode, comparatorCode, value, paramId);

    public void AddCharge(int ageSlot, int domainCode, int targetId, float multiplier) =>
        Data.AddCharge(ageSlot, domainCode, targetId, multiplier);

    public int OpenAgeSlot(int trackCode) => State.OpenAgeSlots[trackCode];

    public int OpenAgeId(int trackCode)
    {
        var slot = State.OpenAgeSlots[trackCode];
        return slot == 0 ? 0 : Data.AgeId(slot);
    }

    public int OpenedTick(int trackCode) => State.OpenedTicks[trackCode];

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
        var world = simulation.World;
        for (var kind = 0; kind < _livingByRelationshipKind.Length; kind++)
        {
            _livingByRelationshipKind[kind] = 0;
            _culledByRelationshipKind[kind] = 0;
        }

        for (var relationshipId = 1; relationshipId <= world.RelationshipHighWatermark; relationshipId++)
        {
            var kindId = world.RelationshipKindId(relationshipId);
            if (kindId <= 0 || kindId >= _livingByRelationshipKind.Length)
            {
                continue;
            }

            if (world.RelationshipIsActive(relationshipId))
            {
                _livingByRelationshipKind[kindId]++;
            }
            else
            {
                _culledByRelationshipKind[kindId]++;
            }
        }

        var mass = TotalCultureRawMass(world);
        if (State.PreviousMassSampleTick >= 0 && State.PreviousTotalCultureMass > 0f)
        {
            State.MassGrowthPermille =
                1000f * (mass - State.PreviousTotalCultureMass) / State.PreviousTotalCultureMass;
        }

        State.PreviousTotalCultureMass = mass;
        State.PreviousMassSampleTick = tick;
    }

    private static float TotalCultureRawMass(WorldState world)
    {
        var total = 0f;
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
        {
            var cursor = world.RawCultureInfluenceCellsOf(cultureId, PlaneCodes.Physical);
            while (world.MoveNextRawCultureInfluenceCell(ref cursor, out _, out _, out _, out var raw))
            {
                total += raw;
            }
        }

        return total;
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
            if (State.OpenAgeSlots[track] != 0)
            {
                continue;
            }

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

            if (best == 0)
            {
                continue;
            }

            State.OpenAgeSlots[track] = best;
            State.OpenedTicks[track] = tick;
            simulation.EmitWorldBeatChanged(tick, track, Data.AgeId(best), WorldBeatPhaseCodes.Opened);
        }
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

    private float FactValue(Simulation simulation, int factCode, int paramId, int tick)
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
            _ => 0f,
        };
    }

    private int LivingRelationships(int relationshipKindId) =>
        relationshipKindId > 0 && relationshipKindId < _livingByRelationshipKind.Length
            ? _livingByRelationshipKind[relationshipKindId]
            : 0;

    private int CulledRelationships(int relationshipKindId) =>
        relationshipKindId > 0 && relationshipKindId < _culledByRelationshipKind.Length
            ? _culledByRelationshipKind[relationshipKindId]
            : 0;
}
