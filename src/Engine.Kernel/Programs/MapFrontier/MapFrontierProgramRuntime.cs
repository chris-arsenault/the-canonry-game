namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// The growing map: the physical world is born as a small thawed heart and
/// opens only where history presses. Each pulse, when the Ice track's thaw
/// age is open, culture mass at the current frontier thaws the ground beyond
/// it - new cells materialized, region inherited from the pressing cell.
/// When the reclaim age (the Long Dark) is open, rim ground that no culture
/// holds and no road crosses is taken back as impassable deep ice. Extent is
/// earned and can ebb; it is never allocated up front.
/// </summary>
internal sealed partial class MapFrontierProgramRuntime :
    IKernelProgramRuntime<MapFrontierProgramData, MapFrontierProgramState>
{
    private static readonly int[] NeighborDQ = [1, 1, 0, -1, -1, 0];
    private static readonly int[] NeighborDR = [0, -1, -1, 0, 1, 1];

    private readonly int[] _candidateQ;
    private readonly int[] _candidateR;
    private readonly int[] _candidateSuperregion;
    private readonly int[] _candidateContinent;
    private readonly int[] _candidateOwner;
    private readonly int[] _candidateCulture;

    public MapFrontierProgramRuntime(SimulationOptions options)
    {
        global::System.ArgumentNullException.ThrowIfNull(options);
        Data = new MapFrontierProgramData();
        State = new MapFrontierProgramState(options.MapFrontierThawRingCapacity);
        var budget = global::System.Math.Max(1, Data.ThawBudgetPerPulse);
        _candidateQ = new int[budget];
        _candidateR = new int[budget];
        _candidateSuperregion = new int[budget];
        _candidateContinent = new int[budget];
        _candidateOwner = new int[budget];
        _candidateCulture = new int[budget];
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
        if (Data.SemanticOpenTerrainId != 0)
        {
            GrowSemanticFrontier(simulation, tick);
        }
    }

    private static int HexDistanceFromOrigin(int q, int r) =>
        (global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2;

    private void ThawFrontier(Simulation simulation, int tick)
    {
        var world = simulation.World;
        var budget = _candidateQ.Length;
        var count = 0;
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark && count < budget; cultureId++)
        {
            count = CollectThawCandidates(world, cultureId, count);
        }

        for (var i = 0; i < count; i++)
        {
            ThawCell(simulation, i, tick);
        }
    }

    private int CollectThawCandidates(WorldState world, int cultureId, int count)
    {
        var cursor = world.RawCultureInfluenceCellsOf(cultureId, PlaneCodes.Physical);
        while (count < _candidateQ.Length &&
               world.MoveNextRawCultureInfluenceCell(ref cursor, out var q, out var r, out _, out var raw))
        {
            if (raw < Data.FrontierMassThreshold || HexDistanceFromOrigin(q, r) >= Data.MaxRadius)
            {
                continue;
            }

            count = CollectThawNeighbors(world, q, r, count);
        }

        return count;
    }

    private int CollectThawNeighbors(WorldState world, int q, int r, int count)
    {
        for (var direction = 0; direction < 6 && count < _candidateQ.Length; direction++)
        {
            var nq = q + NeighborDQ[direction];
            var nr = r + NeighborDR[direction];
            if (HexDistanceFromOrigin(nq, nr) > Data.MaxRadius || !CellNeedsThaw(world, nq, nr) || AlreadyQueued(nq, nr, count))
            {
                continue;
            }

            _candidateQ[count] = nq;
            _candidateR[count] = nr;
            _candidateSuperregion[count] = world.CellSuperregionId(PlaneCodes.Physical, q, r);
            _candidateContinent[count] = world.CellContinentId(PlaneCodes.Physical, q, r);
            _candidateOwner[count] = 0;
            count++;
        }

        return count;
    }

    private bool CellNeedsThaw(WorldState world, int q, int r) =>
        !world.CellIsKnown(PlaneCodes.Physical, q, r) ||
        world.CellTerrainId(PlaneCodes.Physical, q, r) == Data.FrozenTerrainId;

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
        if (simulation.World.CellIsKnown(PlaneCodes.Physical, q, r))
        {
            simulation.SetCellTerrain(PlaneCodes.Physical, q, r, Data.ThawTerrainId, tick);
        }
        else
        {
            simulation.CreateCell(
                PlaneCodes.Physical,
                q,
                r,
                Data.ThawTerrainId,
                _candidateOwner[index],
                _candidateSuperregion[index],
                _candidateContinent[index]);
        }

        State.RecordThawed(q, r);
    }
}
