namespace Engine.Kernel;

/// <summary>
/// The Chronicle Director: casts archetypes over live entities, then watches.
/// Each chronicle carries an unordered pool of authored beats. Beats open on
/// world or heat conditions, charge the world while open (gambit bias and
/// chronicle-granted gambits), and strike when the cast actually acts or the
/// recognized fact occurs. Strikes raise heat; heat decays in quiet. Terminal
/// beats end the chronicle by recognizing world facts; a cold, quiet
/// chronicle fades from the telling. The director never writes world state.
/// </summary>
internal sealed partial class ChronicleProgramRuntime : IKernelProgramRuntime<ChronicleProgramData, ChronicleProgramState>
{
    private const float HeatDecayFactor = 0.99f;
    private const float AmbientPairHeat = 0.25f;
    private const float MaxHeat = 12f;
    private const float InitialHeat = 1f;
    private const float FadeHeatThreshold = 0.5f;
    private const int FadeQuietTicks = 1250;
    private const int FadeGraceTicks = 1500;
    private const float HeatChargeSlope = 0.15f;
    private const float MaxHeatChargeMultiplier = 4f;
    private const int SuppressionTicks = 2000;
    private const float ConvergenceHeatFloor = 4f;
    private const float LegendaryHeatThreshold = 8f;
    private const float ConvergencePullSlope = 0.3f;
    private const float ConvergencePullMax = 2f;

    private readonly int _chronicleCapacity;

    public ChronicleProgramRuntime(SimulationOptions options)
    {
        _chronicleCapacity = options.ChronicleCapacity;
        Data = new ChronicleProgramData();
        Data.Allocate(options.ChronicleArchetypeCapacity, options.ChronicleItemCapacity);
        State = new ChronicleProgramState(options.ChronicleCapacity, options.ChronicleArchetypeCapacity);
    }

    public ChronicleProgramData Data { get; }

    public ChronicleProgramState State { get; }

    public void Tick(Simulation simulation, int tick)
    {
        var world = simulation.World;
        ProcessPendingExecutions(simulation, world, tick);
        for (var chronicle = 1; chronicle <= State.HighWatermark; chronicle++)
        {
            if (!State.Active[chronicle])
            {
                continue;
            }

            State.Heat[chronicle] *= HeatDecayFactor;
            StepChronicle(simulation, world, chronicle, tick);
        }

        for (var archetype = 1; archetype <= Data.ArchetypeCount; archetype++)
        {
            TryCast(simulation, world, archetype, tick);
        }
    }

    /// <summary>
    /// Recorded by the gambit program after every execution; consumed on the
    /// director's next pulse for ambient heat and strike detection.
    /// </summary>
    public void NoteExecution(int actorId, int targetId, int gambitId, bool succeeded) =>
        NotePending(ChroniclePendingKindCodes.GambitExecution, actorId, targetId, gambitId, succeeded);

    /// <summary>
    /// Recorded by the generator program after every fire with a selected
    /// target; the world turning its weight on a cast member is a beat.
    /// </summary>
    public void NoteGeneratorFire(int targetId, int generatorId) =>
        NotePending(ChroniclePendingKindCodes.GeneratorFire, 0, targetId, generatorId, succeeded: true);

    private void NotePending(int kind, int actorId, int targetId, int producerId, bool succeeded)
    {
        if (State.PendingCount >= ChronicleProgramState.PendingExecutionCapacity)
        {
            return;
        }

        State.PendingKinds[State.PendingCount] = kind;
        State.PendingActorIds[State.PendingCount] = actorId;
        State.PendingTargetIds[State.PendingCount] = targetId;
        State.PendingProducerIds[State.PendingCount] = producerId;
        State.PendingSucceeded[State.PendingCount] = succeeded ? 1 : 0;
        State.PendingCount++;
    }

    /// <summary>
    /// Direction: extra score for a gambit whose actor and target match a
    /// charge of an open beat, plus the convergence pull - a hot chronicle
    /// raises the weight of any outsider's action against its cast, so power
    /// is drawn toward running stories. Read by the gambit program during
    /// scoring. Heat-scaled charges grow with the chronicle's heat -
    /// convergence pushes stories toward their endings without decreeing
    /// them.
    /// </summary>
    public float ScoreBias(int actorId, int targetId, int gambitId)
    {
        var total = 0f;
        for (var chronicle = 1; chronicle <= State.HighWatermark; chronicle++)
        {
            if (!State.Active[chronicle])
            {
                continue;
            }

            total += ChronicleBias(chronicle, actorId, targetId, gambitId);
            total += ConvergencePull(chronicle, actorId, targetId);
        }

        return total;
    }

    private float ConvergencePull(int chronicle, int actorId, int targetId)
    {
        var heat = State.Heat[chronicle];
        if (heat < ConvergenceHeatFloor ||
            !CastContains(chronicle, targetId) ||
            CastContains(chronicle, actorId))
        {
            return 0f;
        }

        var pull = (heat - ConvergenceHeatFloor) * ConvergencePullSlope;
        return pull > ConvergencePullMax ? ConvergencePullMax : pull;
    }

    private const float PursuitHeatFloor = 2.5f;

    /// <summary>
    /// The tale draws its cast together: the costar this entity should
    /// travel toward, from its hottest sufficiently hot chronicle. Cast
    /// members converge on the primary; the primary is drawn to whichever
    /// costar carries the story to it. Read by the verb program's
    /// chronicle-pursuit target selector.
    /// </summary>
    public int PursuitTargetFor(int entityId, out float heat)
    {
        heat = 0f;
        var target = 0;
        for (var chronicle = 1; chronicle <= State.HighWatermark; chronicle++)
        {
            if (!State.Active[chronicle] ||
                State.Heat[chronicle] < PursuitHeatFloor ||
                State.Heat[chronicle] <= heat ||
                !CastContains(chronicle, entityId))
            {
                continue;
            }

            var costar = PursuitCostar(chronicle, entityId);
            if (costar > 0)
            {
                heat = State.Heat[chronicle];
                target = costar;
            }
        }

        return target;
    }

    private int PursuitCostar(int chronicle, int entityId)
    {
        var primary = RoleEntity(chronicle, 0);
        if (primary != entityId)
        {
            return primary;
        }

        for (var role = 1; role < ChronicleProgramData.MaxRolesPerArchetype; role++)
        {
            var costar = RoleEntity(chronicle, role);
            if (costar > 0)
            {
                return costar;
            }
        }

        return 0;
    }

    /// <summary>
    /// The hottest running chronicle this entity is cast in; the world's
    /// remembrance machinery (generators) reads this to notice story-bearers.
    /// </summary>
    public float HeatFor(int entityId)
    {
        var best = 0f;
        for (var chronicle = 1; chronicle <= State.HighWatermark; chronicle++)
        {
            if (State.Active[chronicle] &&
                CastContains(chronicle, entityId) &&
                State.Heat[chronicle] > best)
            {
                best = State.Heat[chronicle];
            }
        }

        return best;
    }

    /// <summary>
    /// True when an open beat of a running chronicle grants this gambit to
    /// this entity's role. Chronicle-granted gambits are otherwise inert.
    /// </summary>
    public bool GrantsGambit(int actorId, int gambitId)
    {
        for (var chronicle = 1; chronicle <= State.HighWatermark; chronicle++)
        {
            if (State.Active[chronicle] && ChronicleGrants(chronicle, actorId, gambitId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True when the entity was cast in a chronicle that resolved within the
    /// window at legendary heat - the hook remembrance generators fire on.
    /// </summary>
    /// <summary>Cumulative count of tales that resolved at legendary heat - the world's macro memory fact.</summary>
    public int LegendaryResolutionCount => State.LegendaryResolutionCount;

    public bool LegendaryResolvedWithin(int entityId, int currentTick, int windowTicks)
    {
        for (var slot = 0; slot < ChronicleProgramState.SuppressionRingCapacity; slot++)
        {
            if (State.RecentCastEntityIds[slot] == entityId &&
                currentTick - State.RecentCastTicks[slot] <= windowTicks &&
                State.RecentCastHeat[slot] >= LegendaryHeatThreshold)
            {
                return true;
            }
        }

        return false;
    }

    private float ChronicleBias(int chronicle, int actorId, int targetId, int gambitId)
    {
        var total = 0f;
        var archetype = State.ArchetypeSlots[chronicle];
        var beatStart = Data.ArchetypeBeatStarts[archetype];
        var beatEnd = beatStart + Data.ArchetypeBeatCounts[archetype];
        for (var beat = beatStart; beat < beatEnd; beat++)
        {
            if (!BeatIsOpen(chronicle, beat - beatStart))
            {
                continue;
            }

            total += BeatChargeBias(chronicle, beat, actorId, targetId, gambitId);
        }

        return total;
    }

    private float BeatChargeBias(int chronicle, int beat, int actorId, int targetId, int gambitId) =>
        BeatChargeSum(chronicle, beat, actorId, targetId, gambitId, Data.ChargeScoreBonuses, heatScale: true);

    /// <summary>
    /// The story favors the committed: charges also raise the success chance
    /// of the deeds they push, so a charged blow lands more often than an
    /// uncharged one. Read by the gambit program's success roll. Success
    /// favor is never heat-scaled - it is authored, bounded encouragement.
    /// </summary>
    public float SuccessBonus(int actorId, int targetId, int gambitId)
    {
        var total = 0f;
        for (var chronicle = 1; chronicle <= State.HighWatermark; chronicle++)
        {
            if (!State.Active[chronicle])
            {
                continue;
            }

            var archetype = State.ArchetypeSlots[chronicle];
            var beatStart = Data.ArchetypeBeatStarts[archetype];
            var beatEnd = beatStart + Data.ArchetypeBeatCounts[archetype];
            for (var beat = beatStart; beat < beatEnd; beat++)
            {
                if (BeatIsOpen(chronicle, beat - beatStart))
                {
                    total += BeatChargeSum(chronicle, beat, actorId, targetId, gambitId, Data.ChargeSuccessBonuses, heatScale: false);
                }
            }
        }

        return total;
    }

    private float BeatChargeSum(
        int chronicle,
        int beat,
        int actorId,
        int targetId,
        int gambitId,
        float[] bonuses,
        bool heatScale)
    {
        var total = 0f;
        var chargeStart = Data.BeatChargeStarts[beat];
        var chargeEnd = chargeStart + Data.BeatChargeCounts[beat];
        for (var charge = chargeStart; charge < chargeEnd; charge++)
        {
            if (RoleEntity(chronicle, Data.ChargeActorRoles[charge]) != actorId ||
                RoleEntity(chronicle, Data.ChargeTargetRoles[charge]) != targetId)
            {
                continue;
            }

            if (Data.ChargeGambitIds[charge] != 0 && Data.ChargeGambitIds[charge] != gambitId)
            {
                continue;
            }

            var bonus = bonuses[charge];
            if (heatScale && Data.ChargeHeatScaled[charge] != 0)
            {
                var multiplier = 1f + (State.Heat[chronicle] * HeatChargeSlope);
                bonus *= multiplier < MaxHeatChargeMultiplier ? multiplier : MaxHeatChargeMultiplier;
            }

            total += bonus;
        }

        return total;
    }

    private bool ChronicleGrants(int chronicle, int actorId, int gambitId)
    {
        var archetype = State.ArchetypeSlots[chronicle];
        var beatStart = Data.ArchetypeBeatStarts[archetype];
        var beatEnd = beatStart + Data.ArchetypeBeatCounts[archetype];
        for (var beat = beatStart; beat < beatEnd; beat++)
        {
            if (!BeatIsOpen(chronicle, beat - beatStart))
            {
                continue;
            }

            var grantStart = Data.BeatGrantStarts[beat];
            var grantEnd = grantStart + Data.BeatGrantCounts[beat];
            for (var grant = grantStart; grant < grantEnd; grant++)
            {
                if (Data.GrantGambitIds[grant] == gambitId &&
                    RoleEntity(chronicle, Data.GrantRoles[grant]) == actorId)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool BeatIsOpen(int chronicle, int beatOrdinal) =>
        (State.OpenMasks[chronicle] & (1L << beatOrdinal)) != 0;

    private bool BeatIsStruck(int chronicle, int beatOrdinal) =>
        (State.StruckMasks[chronicle] & (1L << beatOrdinal)) != 0;

    private int RoleEntity(int chronicle, int role) =>
        State.RoleEntityIds[(chronicle * ChronicleProgramData.MaxRolesPerArchetype) + role];

    private void SetRoleEntity(int chronicle, int role, int entityId) =>
        State.RoleEntityIds[(chronicle * ChronicleProgramData.MaxRolesPerArchetype) + role] = entityId;

    private bool CastContains(int chronicle, int entityId)
    {
        if (entityId <= 0)
        {
            return false;
        }

        for (var role = 0; role < ChronicleProgramData.MaxRolesPerArchetype; role++)
        {
            if (RoleEntity(chronicle, role) == entityId)
            {
                return true;
            }
        }

        return false;
    }

    private void EmitChronicleChanged(
        Simulation simulation,
        int chronicle,
        int eventTypeCode,
        int beatOrdinal,
        int outcomeCodeId,
        int tick)
    {
        simulation.EmitChronicleChanged(
            tick,
            State.ChronicleSequences[chronicle],
            Data.ArchetypeIds[State.ArchetypeSlots[chronicle]],
            eventTypeCode,
            beatOrdinal,
            outcomeCodeId,
            RoleEntity(chronicle, 0),
            RoleEntity(chronicle, 1),
            RoleEntity(chronicle, 2),
            RoleEntity(chronicle, 3),
            State.Heat[chronicle]);
    }
}
