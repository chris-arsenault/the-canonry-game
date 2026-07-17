namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MapFrontierProgramRuntime
{
    // Fraction of the frontier threshold echoed into each newly opened semantic
    // cell (below 1 so the rim still needs a little labour to become a source -
    // no instant cascade to the edge).
    private const float SemanticFrontierSeedFraction = 0.72f;


    /// <summary>
    /// Idea-space grows where thought concentrates. Each tick, every cell a
    /// culture holds with idea-mass at or above the threshold opens its unknown
    /// semantic neighbours as new open-field cells - continuous and ungated by
    /// any age, because ideas have no ice to wait on. Superregion and continent
    /// are inherited from the pressing cell; new ground earns its meaning as
    /// culture and entities reach it. Bounded by the semantic max radius and the
    /// shared per-pulse candidate budget.
    /// </summary>
    private void GrowSemanticFrontier(Simulation simulation, int tick)
    {
        var world = simulation.World;
        var count = 0;
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark && count < _candidateQ.Length; cultureId++)
        {
            count = CollectSemanticCandidates(world, cultureId, count);
        }

        // A newly opened cell inherits a head-start of the culture that pressed
        // it open - a fraction of the frontier threshold. Without this the fresh
        // rim decays from nothing faster than heart-bound labour can feed it, and
        // the frontier plateaus a few rings out; with it, the culture carries its
        // own edge outward and idea-space keeps climbing toward the higher tiers.
        var seedMass = SemanticFrontierSeedFraction * Data.SemanticFrontierMassThreshold;
        for (var i = 0; i < count; i++)
        {
            simulation.CreateCell(
                PlaneCodes.Semantic,
                _candidateQ[i],
                _candidateR[i],
                Data.SemanticOpenTerrainId,
                _candidateOwner[i],
                _candidateSuperregion[i],
                _candidateContinent[i]);
            simulation.ModifyCellCultureInfluenceRaw(
                _candidateCulture[i],
                PlaneCodes.Semantic,
                _candidateQ[i],
                _candidateR[i],
                seedMass,
                tick);
            State.TotalSemanticOpened++;
        }
    }

    private int CollectSemanticCandidates(WorldState world, int cultureId, int count)
    {
        var cursor = world.RawCultureInfluenceCellsOf(cultureId, PlaneCodes.Semantic);
        while (count < _candidateQ.Length &&
               world.MoveNextRawCultureInfluenceCell(ref cursor, out var q, out var r, out _, out var raw))
        {
            if (raw < Data.SemanticFrontierMassThreshold || HexDistanceFromOrigin(q, r) >= Data.SemanticMaxRadius)
            {
                continue;
            }

            count = CollectSemanticNeighbors(world, cultureId, q, r, count);
        }

        return count;
    }

    private int CollectSemanticNeighbors(WorldState world, int cultureId, int q, int r, int count)
    {
        for (var direction = 0; direction < 6 && count < _candidateQ.Length; direction++)
        {
            var nq = q + NeighborDQ[direction];
            var nr = r + NeighborDR[direction];
            if (HexDistanceFromOrigin(nq, nr) > Data.SemanticMaxRadius ||
                world.CellIsKnown(PlaneCodes.Semantic, nq, nr) ||
                AlreadyQueued(nq, nr, count))
            {
                continue;
            }

            _candidateQ[count] = nq;
            _candidateR[count] = nr;
            _candidateSuperregion[count] = world.CellSuperregionId(PlaneCodes.Semantic, q, r);
            _candidateContinent[count] = world.CellContinentId(PlaneCodes.Semantic, q, r);
            _candidateOwner[count] = 0;
            _candidateCulture[count] = cultureId;
            count++;
        }

        return count;
    }

    /// <summary>Configures continuous semantic growth; a zero open-terrain id disables it.</summary>
    public void SetSemanticConfiguration(int openTerrainId, int maxRadius, float frontierMassThreshold)
    {
        Data.SemanticOpenTerrainId = openTerrainId;
        Data.SemanticMaxRadius = maxRadius;
        Data.SemanticFrontierMassThreshold = frontierMassThreshold;
    }
}
