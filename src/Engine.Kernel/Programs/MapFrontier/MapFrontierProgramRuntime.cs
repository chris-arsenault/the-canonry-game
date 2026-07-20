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
        var capacity = _candidateQ.Length;
        var count = 0;
        var cultureCount = world.CultureHighWatermark;
        var startCultureId = FirstCultureId(State.NextPhysicalCultureId, cultureCount);
        for (var offset = 0; offset < cultureCount && count < capacity; offset++)
        {
            var cultureId = ((startCultureId - 1 + offset) % cultureCount) + 1;
            count = CollectThawCandidates(simulation, world, cultureId, count, capacity);
        }

        State.NextPhysicalCultureId = NextCultureId(startCultureId, cultureCount);

        var thawCount = global::System.Math.Min(budget, count);
        OrderCandidatesByGeography(simulation, count, thawCount);
        for (var i = 0; i < thawCount; i++)
        {
            ThawCell(simulation, i, tick);
        }
    }

    // The retreating ice follows the land: candidates over budget are
    // dropped from the coldest, most edge-hostile ground first, so thaw
    // advances along valleys, coasts, and warm refuges instead of as a
    // uniform rim.
    private void OrderCandidatesByGeography(Simulation simulation, int count, int thawCount)
    {
        for (var slot = 0; slot < thawCount; slot++)
        {
            var bestIndex = slot;
            var bestScore = CandidateGeographyScore(simulation, bestIndex);
            for (var i = slot + 1; i < count; i++)
            {
                var score = CandidateGeographyScore(simulation, i);
                if (score > bestScore)
                {
                    bestIndex = i;
                    bestScore = score;
                }
            }

            if (bestIndex != slot)
            {
                (_candidateQ[slot], _candidateQ[bestIndex]) = (_candidateQ[bestIndex], _candidateQ[slot]);
                (_candidateR[slot], _candidateR[bestIndex]) = (_candidateR[bestIndex], _candidateR[slot]);
            }
        }
    }

    private int CandidateGeographyScore(Simulation simulation, int index)
    {
        var q = _candidateQ[index];
        var r = _candidateR[index];
        var geography = simulation.KernelData.PhysicalGeography;
        var warmth = PhysicalHexFieldCodes.BandAt(
            geography.FieldWord(q, r),
            PhysicalHexFieldCodes.WarmthShift);
        var crossingWord = geography.CrossingWord(q, r);
        var hostility = 0;
        for (var direction = 0; direction < PhysicalCrossingCodes.DirectionCount; direction++)
        {
            hostility += PhysicalCrossingCodes.ClassAt(crossingWord, direction) == PhysicalCrossingCodes.Open ? 0 : 1;
        }

        return (warmth * 4) - hostility;
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

    // Only Deep Ice cover marks a cell as un-thawed frontier. Other covers
    // (sea ice over water) are ordinary transient state owned by their own
    // mechanics, not ground the frontier still has to open.
    private bool CellNeedsThaw(Simulation simulation, WorldState world, int q, int r) =>
        simulation.KernelData.PhysicalGeography.TryGetCell(q, r, out _, out _, out _) &&
        (!world.CellIsKnown(PlaneCodes.Physical, q, r) ||
         world.CellTerrainCoverId(PlaneCodes.Physical, q, r) == Data.FrozenTerrainId);

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
            simulation.ApplyInitialTerrainCover(q, r, tick);
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
