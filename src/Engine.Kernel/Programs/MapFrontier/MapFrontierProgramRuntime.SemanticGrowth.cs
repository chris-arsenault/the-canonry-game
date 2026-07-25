namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MapFrontierProgramRuntime
{

    /// <summary>
    /// Idea-space grows where thought concentrates. Each tick, every cell a
    /// culture holds with idea-mass at or above the threshold opens its unknown
    /// semantic neighbours from the latent idea-space terrain field. Growth is
    /// continuous and ungated by any age because ideas have no ice to wait on.
    /// It is bounded by the semantic max radius and authored per-pulse budget.
    /// </summary>
    private void GrowSemanticFrontier(Simulation simulation)
    {
        var world = simulation.World;
        var budget = global::System.Math.Min(Data.SemanticBudgetPerPulse, _candidateQ.Length);
        var count = 0;
        var cultureCount = world.CultureHighWatermark;
        var startCultureId = FirstCultureId(State.NextSemanticCultureId, cultureCount);
        for (var offset = 0; offset < cultureCount && count < budget; offset++)
        {
            var cultureId = ((startCultureId - 1 + offset) % cultureCount) + 1;
            count = CollectSemanticCandidates(world, cultureId, count, budget);
        }

        State.NextSemanticCultureId = NextCultureId(startCultureId, cultureCount);

        for (var i = 0; i < count; i++)
        {
            if (!Data.TryGetSemanticBlueprintCell(
                    _candidateQ[i],
                    _candidateR[i],
                    out var terrainId,
                    out var superregionId,
                    out var continentId))
            {
                throw new global::System.InvalidOperationException();
            }

            simulation.CreateCell(
                PlaneCodes.Semantic,
                _candidateQ[i],
                _candidateR[i],
                terrainId,
                0,
                superregionId,
                continentId);
            State.TotalSemanticOpened++;
        }
    }

    private int CollectSemanticCandidates(WorldState world, int cultureId, int count, int budget)
    {
        var cursor = world.RawCultureInfluenceCellsOf(cultureId, PlaneCodes.Semantic);
        while (count < budget &&
               world.MoveNextRawCultureInfluenceCell(ref cursor, out var q, out var r, out _, out var raw))
        {
            if (raw < Data.SemanticFrontierMassThreshold || HexDistanceFromOrigin(q, r) >= Data.SemanticMaxRadius)
            {
                continue;
            }

            count = CollectSemanticNeighbors(world, q, r, count, budget);
        }

        return count;
    }

    private int CollectSemanticNeighbors(WorldState world, int q, int r, int count, int budget)
    {
        for (var direction = 0; direction < 6 && count < budget; direction++)
        {
            var nq = q + NeighborDQ[direction];
            var nr = r + NeighborDR[direction];
            if (HexDistanceFromOrigin(nq, nr) > Data.SemanticMaxRadius ||
                world.CellIsKnown(PlaneCodes.Semantic, nq, nr) ||
                !Data.TryGetSemanticBlueprintCell(nq, nr, out _, out _, out _) ||
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

    /// <summary>Configures continuous semantic growth from the installed terrain field.</summary>
    public void SetSemanticConfiguration(
        bool enabled,
        int maxRadius,
        FixedScalar frontierMassThreshold,
        int budgetPerPulse)
    {
        ValidateRadiusAndBudget(maxRadius, budgetPerPulse);
        Data.SemanticGrowthEnabled = enabled;
        Data.SemanticMaxRadius = maxRadius;
        Data.SemanticFrontierMassThreshold = frontierMassThreshold;
        Data.SemanticBudgetPerPulse = budgetPerPulse;
    }
}
