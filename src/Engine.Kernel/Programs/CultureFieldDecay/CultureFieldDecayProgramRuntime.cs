namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// Applies slow global decay to raw culture influence so the field reflects the
/// balance of current production against loss instead of the integral of all
/// history. Unsustained trails and satellites fade; sustained centers persist.
/// </summary>
internal sealed class CultureFieldDecayProgramRuntime : ISimulationSystem
{
    private readonly int _decayPerRunUnits;
    private readonly int _minimumDecayPerRunUnits;
    private readonly int _cullFloorRawMassUnits;
    private readonly int _contestDecayMultiplierUnits;
    private readonly int _contestFloorRawMassUnits;
    private int _contestTideUnits = KernelFixedPoint.RatioScale;
    private int _globalTideUnits = KernelFixedPoint.RatioScale;
    private readonly int[] _scratchQ;
    private readonly int[] _scratchR;
    private readonly int[] _scratchZ;
    private readonly int[] _scratchRaw;

    internal CultureFieldDecayProgramRuntime(CultureFieldTuningOptions tuning, int cellCapacity)
    {
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        NumericGuard.ThrowIfNegativeOrZero(cellCapacity);
        _decayPerRunUnits = KernelFixedPoint.RatioUnits(tuning.DecayPerRun);
        _minimumDecayPerRunUnits = KernelFixedPoint.CultureMassUnits(tuning.MinimumDecayPerRun);
        _cullFloorRawMassUnits = KernelFixedPoint.CultureMassUnits(tuning.CullFloorRawMass);
        _contestDecayMultiplierUnits = KernelFixedPoint.RatioUnits(tuning.ContestDecayMultiplier);
        _contestFloorRawMassUnits = KernelFixedPoint.CultureMassUnits(tuning.ContestFloorRawMass);
        _scratchQ = new int[cellCapacity];
        _scratchR = new int[cellCapacity];
        _scratchZ = new int[cellCapacity];
        _scratchRaw = new int[cellCapacity];
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        if (_decayPerRunUnits <= 0 && _minimumDecayPerRunUnits <= 0)
        {
            return;
        }

        var world = simulation.World;
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
        {
            DecayPlane(simulation, cultureId, PlaneCodes.Physical, tick);
            DecayPlane(simulation, cultureId, PlaneCodes.Semantic, tick);
        }
    }

    private void DecayPlane(Simulation simulation, int cultureId, int planeCode, int tick)
    {
        var persistence = simulation.Knowledge.Modifier(
            cultureId,
            KnowledgeGrantDomainCodes.CulturePersistence,
            planeCode);
        var tideResistance = simulation.Knowledge.Modifier(
            cultureId,
            KnowledgeGrantDomainCodes.DecayTideResistance,
            planeCode);
        var persistenceUnits = KernelFixedPoint.RatioUnits(persistence);
        var tideResistanceUnits = KernelFixedPoint.RatioUnits(tideResistance);
        var effectiveGlobalTideUnits = _globalTideUnits > KernelFixedPoint.RatioScale
            ? KernelFixedPoint.RatioScale + KernelFixedPoint.MultiplyByRatio(
                _globalTideUnits - KernelFixedPoint.RatioScale,
                tideResistanceUnits,
                0,
                KernelFixedPoint.RatioMaximumUnits)
            : _globalTideUnits;
        var count = SnapshotPlaneCells(simulation.World, cultureId, planeCode);
        ApplyDecay(
            simulation,
            cultureId,
            planeCode,
            tick,
            count,
            persistenceUnits,
            effectiveGlobalTideUnits);
    }

    private int SnapshotPlaneCells(WorldState world, int cultureId, int planeCode)
    {
        // Two-phase: snapshot cells first because applying decay mutates the
        // spatial index the cursor walks. Cells beyond scratch capacity skip
        // this run and decay on a later one.
        var count = 0;
        var cursor = world.RawCultureInfluenceCellsOf(cultureId, planeCode);
        while (count < _scratchQ.Length &&
               world.MoveNextRawCultureInfluenceCellUnits(
                   ref cursor,
                   out var q,
                   out var r,
                   out var z,
                   out var raw))
        {
            if (raw <= 0)
            {
                continue;
            }

            _scratchQ[count] = q;
            _scratchR[count] = r;
            _scratchZ[count] = z;
            _scratchRaw[count] = raw;
            count++;
        }

        return count;
    }

    private void ApplyDecay(
        Simulation simulation,
        int cultureId,
        int planeCode,
        int tick,
        int count,
        int persistenceUnits,
        int effectiveGlobalTideUnits)
    {
        var world = simulation.World;
        for (var i = 0; i < count; i++)
        {
            var raw = _scratchRaw[i];
            var contestUnits = ContestMultiplierUnits(
                world,
                cultureId,
                planeCode,
                _scratchQ[i],
                _scratchR[i],
                _scratchZ[i],
                raw);
            var decay = DecayUnits(
                raw,
                contestUnits,
                effectiveGlobalTideUnits,
                persistenceUnits);
            if (raw - decay < _cullFloorRawMassUnits)
            {
                decay = raw;
            }

            _ = simulation.ModifyCellCultureInfluenceRawUnits(
                cultureId,
                planeCode,
                _scratchQ[i],
                _scratchR[i],
                _scratchZ[i],
                -decay,
                tick);
        }
    }

    private int DecayUnits(
        int raw,
        int contestUnits,
        int effectiveGlobalTideUnits,
        int persistenceUnits)
    {
        var decay = global::System.Math.Max(
            KernelFixedPoint.MultiplyByRatio(
                raw,
                _decayPerRunUnits,
                0,
                WorldState.CultureFullRawMassUnits),
            _minimumDecayPerRunUnits);
        decay = KernelFixedPoint.MultiplyByRatio(
            decay,
            contestUnits,
            0,
            WorldState.CultureFullRawMassUnits);
        decay = KernelFixedPoint.MultiplyByRatio(
            decay,
            effectiveGlobalTideUnits,
            0,
            WorldState.CultureFullRawMassUnits);
        return KernelFixedPoint.MultiplyByRatio(
            decay,
            persistenceUnits,
            0,
            WorldState.CultureFullRawMassUnits);
    }

    /// <summary>
    /// The standing tide: where a stronger rival culture holds the same
    /// cell, the locally weaker side decays faster - fronts form where
    /// cultures touch, and boundaries can visibly move.
    /// </summary>
    /// <summary>Age charges: the world-beat program scales the tides per open age each run.</summary>
    public void SetContestTide(FixedScalar tide) => _contestTideUnits = KernelFixedPoint.RatioUnits(tide);

    public void ScaleContestTide(FixedScalar multiplier) =>
        _contestTideUnits = KernelFixedPoint.MultiplyByRatio(
            _contestTideUnits,
            KernelFixedPoint.RatioUnits(multiplier),
            0,
            KernelFixedPoint.RatioMaximumUnits);

    public void SetGlobalTide(FixedScalar tide) => _globalTideUnits = KernelFixedPoint.RatioUnits(tide);

    public void ScaleGlobalTide(FixedScalar multiplier) =>
        _globalTideUnits = KernelFixedPoint.MultiplyByRatio(
            _globalTideUnits,
            KernelFixedPoint.RatioUnits(multiplier),
            0,
            KernelFixedPoint.RatioMaximumUnits);

    private int ContestMultiplierUnits(WorldState world, int cultureId, int planeCode, int q, int r, int z, int raw)
    {
        var contested = KernelFixedPoint.MultiplyByRatio(
            _contestDecayMultiplierUnits,
            _contestTideUnits,
            0,
            KernelFixedPoint.RatioMaximumUnits);
        if (contested <= KernelFixedPoint.RatioScale || raw <= 0)
        {
            return KernelFixedPoint.RatioScale;
        }

        for (var other = 1; other <= world.CultureHighWatermark; other++)
        {
            if (other == cultureId)
            {
                continue;
            }

            var rival = world.GetCellCultureInfluenceRawUnits(other, planeCode, q, r, z);
            if (rival > raw && rival >= _contestFloorRawMassUnits)
            {
                return contested;
            }
        }

        return KernelFixedPoint.RatioScale;
    }
}
