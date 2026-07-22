namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// The growing map: the physical world is born as a small revealed heart and
/// opens where entities actually go. Each pulse, every active entity reveals
/// the latent ground around its position - fog of war retreats at entity
/// speed, one-way, untied from culture mass and from the ice. The ice is a
/// separate breathing layer: during freeze ages cold pressure lays impassable
/// deep-ice cover over weakly-held frontier ground, and during thaw ages the
/// cover melts back; it closes passages and shrinks range but never
/// un-reveals a cell. Extent is earned and permanent; passability ebbs.
/// </summary>
internal sealed partial class MapFrontierProgramRuntime :
    IKernelProgramRuntime<MapFrontierProgramData, MapFrontierProgramState>
{
    private static readonly int[] NeighborDQ = [1, 1, 0, -1, -1, 0];
    private static readonly int[] NeighborDR = [0, -1, -1, 0, 1, 1];

    // Entities visited per reveal pulse: bounds pulse cost on large worlds
    // while the rotating cursor still reaches every entity within a few
    // pulses.
    private const int RevealEntityVisitBudget = 96;

    private readonly int[] _candidateQ;
    private readonly int[] _candidateR;

    public MapFrontierProgramRuntime(SimulationOptions options)
    {
        global::System.ArgumentNullException.ThrowIfNull(options);
        Data = new MapFrontierProgramData(options.MapGenerationRadiusCapacity, options.KindIdCapacity);
        State = new MapFrontierProgramState(options.MapFrontierThawRingCapacity);
        var budget = options.MapFrontierCandidateCapacity;
        _candidateQ = new int[budget];
        _candidateR = new int[budget];
    }

    public MapFrontierProgramData Data { get; }

    public MapFrontierProgramState State { get; }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);

        // Reveal is not age-gated: explorers walk in every age.
        RevealAroundEntities(simulation, tick);

        var worldAge = simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.World);
        if (Data.IsThawAge(worldAge))
        {
            RetreatIce(simulation, tick);
            PulseSeaIce(simulation, tick, freeze: false);
        }

        if (Data.IsRefreezeAge(worldAge))
        {
            AdvanceIce(simulation, tick);
            PulseSeaIce(simulation, tick, freeze: true);
        }

        // Idea-space grows every tick where culture presses it - no age gate.
        if (Data.SemanticGrowthEnabled)
        {
            GrowSemanticFrontier(simulation);
        }
    }

    private static int HexDistanceFromOrigin(int q, int r) =>
        (global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2;

    /// <summary>
    /// Presence reveals: each visited entity materializes the latent cells
    /// within its kind's reveal radius. Static kinds hold a lit hearth;
    /// ranging kinds carry the light with them.
    /// </summary>
    private void RevealAroundEntities(Simulation simulation, int tick)
    {
        var world = simulation.World;
        var budget = Data.RevealBudgetPerPulse;
        if (budget <= 0)
        {
            return;
        }

        var highWatermark = world.EntityHighWatermark;
        if (highWatermark <= 0)
        {
            return;
        }

        var revealed = 0;
        var visited = 0;
        var entityId = NextRevealEntityId(highWatermark);
        while (visited < RevealEntityVisitBudget && visited < highWatermark && revealed < budget)
        {
            if (world.EntityIsActive(entityId))
            {
                revealed += RevealAroundEntity(simulation, world, entityId, budget - revealed, tick);
            }

            visited++;
            entityId = entityId >= highWatermark ? 1 : entityId + 1;
        }

        State.NextRevealEntityId = entityId;
    }

    private int NextRevealEntityId(int highWatermark)
    {
        var entityId = State.NextRevealEntityId;
        return entityId <= 0 || entityId > highWatermark ? 1 : entityId;
    }

    private int RevealAroundEntity(Simulation simulation, WorldState world, int entityId, int budget, int tick)
    {
        var radius = Data.RevealRadiusOf(world.EntityKindId(entityId));
        var q = world.EntityPhysicalQ(entityId);
        var r = world.EntityPhysicalR(entityId);
        var revealed = 0;
        for (var dq = -radius; dq <= radius && revealed < budget; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr && revealed < budget; dr++)
            {
                if (TryRevealCell(simulation, world, q + dq, r + dr, tick))
                {
                    revealed++;
                }
            }
        }

        return revealed;
    }

    // Reveal is one-way materialization of latent geography. Known cells -
    // including ice-covered ones - are never touched here; covers belong to
    // the ice layer.
    private bool TryRevealCell(Simulation simulation, WorldState world, int q, int r, int tick)
    {
        if (HexDistanceFromOrigin(q, r) > Data.MaxRadius ||
            world.CellIsKnown(PlaneCodes.Physical, q, r) ||
            !simulation.KernelData.PhysicalGeography.TryGetCell(
                q,
                r,
                out var terrainId,
                out var superregionId,
                out var continentId))
        {
            return false;
        }

        simulation.CreateCell(
            PlaneCodes.Physical,
            q,
            r,
            terrainId,
            0,
            superregionId,
            continentId);
        simulation.ApplyInitialTerrainCover(q, r, tick);
        State.RecordThawed(q, r);
        return true;
    }

    private bool AlreadyQueued(int q, int r, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (_candidateQ[i] == q && _candidateR[i] == r)
            {
                return true;
            }
        }

        return false;
    }

    private void ValidateRadiusAndBudget(int radius, int budgetPerPulse)
    {
        if (radius <= 0 ||
            (Data.GenerationRadiusCapacity > 0 && radius > Data.GenerationRadiusCapacity))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(radius));
        }

        if (budgetPerPulse <= 0 || budgetPerPulse > _candidateQ.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(budgetPerPulse));
        }
    }

    private static int FirstCultureId(int nextCultureId, int cultureCount) =>
        cultureCount <= 0 || nextCultureId <= 0 || nextCultureId > cultureCount ? 1 : nextCultureId;

    private static int NextCultureId(int currentCultureId, int cultureCount) =>
        cultureCount <= 0 || currentCultureId >= cultureCount ? 1 : currentCultureId + 1;
}
