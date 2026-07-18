namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// The growing map: the physical world is born as a small thawed heart and
/// opens only where history presses. Each pulse, when the Ice track's thaw
/// age is open, culture mass at the current frontier thaws the ground beyond
/// it - new cells materialized, region inherited from the pressing cell.
/// When the reclaim age (the Long Dark) is open, frontier-grown rim ground
/// that no culture holds and no road crosses receives an impassable deep-ice
/// cover. Thaw removes that cover and reveals the durable terrain beneath.
/// Extent is earned and can ebb; it is never allocated up front.
/// </summary>
internal sealed partial class MapFrontierProgramRuntime :
    IKernelProgramRuntime<MapFrontierProgramData, MapFrontierProgramState>
{
    private static readonly int[] NeighborDQ = [1, 1, 0, -1, -1, 0];
    private static readonly int[] NeighborDR = [0, -1, -1, 0, 1, 1];

    private readonly int[] _candidateQ;
    private readonly int[] _candidateR;

    public MapFrontierProgramRuntime(SimulationOptions options)
    {
        global::System.ArgumentNullException.ThrowIfNull(options);
        Data = new MapFrontierProgramData(options.MapGenerationRadiusCapacity);
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
        var worldAge = simulation.WorldBeats.OpenAgeId(WorldBeatTrackCodes.World);
        if (Data.IsThawAge(worldAge))
        {
            ThawFrontier(simulation, tick);
        }

        if (Data.IsRefreezeAge(worldAge))
        {
            ReclaimRim(simulation, tick);
        }

        // Idea-space grows every tick where culture presses it - no age gate.
        if (Data.SemanticGrowthEnabled)
        {
            GrowSemanticFrontier(simulation);
        }
    }

    private static int HexDistanceFromOrigin(int q, int r) =>
        (global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2;

    private void ThawFrontier(Simulation simulation, int tick)
    {
        var world = simulation.World;
        var budget = global::System.Math.Min(Data.ThawBudgetPerPulse, _candidateQ.Length);
        var count = 0;
        var cultureCount = world.CultureHighWatermark;
        var startCultureId = FirstCultureId(State.NextPhysicalCultureId, cultureCount);
        for (var offset = 0; offset < cultureCount && count < budget; offset++)
        {
            var cultureId = ((startCultureId - 1 + offset) % cultureCount) + 1;
            count = CollectThawCandidates(simulation, world, cultureId, count, budget);
        }

        State.NextPhysicalCultureId = NextCultureId(startCultureId, cultureCount);

        for (var i = 0; i < count; i++)
        {
            ThawCell(simulation, i, tick);
        }
    }

    private int CollectThawCandidates(
        Simulation simulation,
        WorldState world,
        int cultureId,
        int count,
        int budget)
    {
        var threshold = Data.FrontierMassThreshold * simulation.Knowledge.Modifier(
            cultureId,
            KnowledgeGrantDomainCodes.FrontierThawThreshold);
        var cursor = world.RawCultureInfluenceCellsOf(cultureId, PlaneCodes.Physical);
        while (count < budget &&
               world.MoveNextRawCultureInfluenceCell(ref cursor, out var q, out var r, out _, out var raw))
        {
            if (raw < threshold || HexDistanceFromOrigin(q, r) >= Data.MaxRadius)
            {
                continue;
            }

            count = CollectThawNeighbors(simulation, world, q, r, count, budget);
        }

        return count;
    }

    private int CollectThawNeighbors(
        Simulation simulation,
        WorldState world,
        int q,
        int r,
        int count,
        int budget)
    {
        for (var direction = 0; direction < 6 && count < budget; direction++)
        {
            var nq = q + NeighborDQ[direction];
            var nr = r + NeighborDR[direction];
            if (HexDistanceFromOrigin(nq, nr) > Data.MaxRadius ||
                !CellNeedsThaw(simulation, world, nq, nr) ||
                AlreadyQueued(nq, nr, count))
            {
                continue;
            }

            _candidateQ[count] = nq;
            _candidateR[count] = nr;
            count++;
        }

        return count;
    }

    private bool CellNeedsThaw(Simulation simulation, WorldState world, int q, int r) =>
        simulation.KernelData.PhysicalGeography.TryGetCell(q, r, out _, out _, out _) &&
        (!world.CellIsKnown(PlaneCodes.Physical, q, r) ||
         world.CellTerrainCoverId(PlaneCodes.Physical, q, r) != 0);

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

    private void ThawCell(Simulation simulation, int index, int tick)
    {
        var q = _candidateQ[index];
        var r = _candidateR[index];
        if (!simulation.KernelData.PhysicalGeography.TryGetCell(
                q,
                r,
                out var terrainId,
                out var superregionId,
                out var continentId))
        {
            throw new global::System.InvalidOperationException();
        }

        var wasKnown = simulation.World.CellIsKnown(PlaneCodes.Physical, q, r);
        if (wasKnown)
        {
            simulation.ClearTerrainCover(q, r, tick);
            State.TotalThawed++;
        }
        else
        {
            simulation.CreateCell(
                PlaneCodes.Physical,
                q,
                r,
                terrainId,
                0,
                superregionId,
                continentId);
        }

        if (!wasKnown)
        {
            State.RecordThawed(q, r);
        }
    }

    private static int FirstCultureId(int nextCultureId, int cultureCount) =>
        cultureCount <= 0 || nextCultureId <= 0 || nextCultureId > cultureCount ? 1 : nextCultureId;

    private static int NextCultureId(int currentCultureId, int cultureCount) =>
        cultureCount <= 0 || currentCultureId >= cultureCount ? 1 : currentCultureId + 1;
}
