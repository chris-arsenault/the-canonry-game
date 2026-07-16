namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// Applies slow global decay to raw culture influence so the field reflects the
/// balance of current production against loss instead of the integral of all
/// history. Unsustained trails and satellites fade; sustained centers persist.
/// </summary>
internal sealed class CultureFieldDecayProgramRuntime : ISimulationSystem
{
    private readonly float _decayPerPulse;
    private readonly float _minimumDecayPerPulse;
    private readonly float _cullFloorRawMass;
    private readonly float _contestDecayMultiplier;
    private readonly float _contestFloorRawMass;
    private float _contestTide = 1f;
    private float _globalTide = 1f;
    private readonly int[] _scratchQ;
    private readonly int[] _scratchR;
    private readonly int[] _scratchZ;
    private readonly float[] _scratchRaw;

    internal CultureFieldDecayProgramRuntime(CultureFieldTuningOptions tuning, int cellCapacity)
    {
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCapacity);
        _decayPerPulse = tuning.DecayPerPulse;
        _minimumDecayPerPulse = tuning.MinimumDecayPerPulse;
        _cullFloorRawMass = tuning.CullFloorRawMass;
        _contestDecayMultiplier = tuning.ContestDecayMultiplier;
        _contestFloorRawMass = tuning.ContestFloorRawMass;
        _scratchQ = new int[cellCapacity];
        _scratchR = new int[cellCapacity];
        _scratchZ = new int[cellCapacity];
        _scratchRaw = new float[cellCapacity];
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        if (_decayPerPulse <= 0f && _minimumDecayPerPulse <= 0f)
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
        var world = simulation.World;
        // Two-phase: snapshot cells first because applying decay mutates the
        // spatial index the cursor walks. Cells beyond scratch capacity skip
        // this pulse and decay on a later one.
        var count = 0;
        var cursor = world.RawCultureInfluenceCellsOf(cultureId, planeCode);
        while (count < _scratchQ.Length &&
               world.MoveNextRawCultureInfluenceCell(ref cursor, out var q, out var r, out var z, out var raw))
        {
            if (raw <= 0f)
            {
                continue;
            }

            _scratchQ[count] = q;
            _scratchR[count] = r;
            _scratchZ[count] = z;
            _scratchRaw[count] = raw;
            count++;
        }

        for (var i = 0; i < count; i++)
        {
            var raw = _scratchRaw[i];
            var contest = ContestMultiplier(world, cultureId, planeCode, _scratchQ[i], _scratchR[i], _scratchZ[i], raw);
            var decay = global::System.MathF.Max(raw * _decayPerPulse, _minimumDecayPerPulse) * contest * _globalTide;
            if (raw - decay < _cullFloorRawMass)
            {
                decay = raw;
            }

            _ = simulation.ModifyCellCultureInfluenceRaw(
                cultureId,
                planeCode,
                _scratchQ[i],
                _scratchR[i],
                _scratchZ[i],
                -decay,
                tick);
        }
    }

    /// <summary>
    /// The standing tide: where a stronger rival culture holds the same
    /// cell, the locally weaker side decays faster - fronts form where
    /// cultures touch, and boundaries can visibly move.
    /// </summary>
    /// <summary>Age charges: the world-beat program scales the tides per open age each pulse.</summary>
    public void SetContestTide(float tide) => _contestTide = tide;

    public void ScaleContestTide(float multiplier) => _contestTide *= multiplier;

    public void SetGlobalTide(float tide) => _globalTide = tide;

    public void ScaleGlobalTide(float multiplier) => _globalTide *= multiplier;

    private float ContestMultiplier(WorldState world, int cultureId, int planeCode, int q, int r, int z, float raw)
    {
        var contested = _contestDecayMultiplier * _contestTide;
        if (contested <= 1f || raw <= 0f)
        {
            return 1f;
        }

        for (var other = 1; other <= world.CultureHighWatermark; other++)
        {
            if (other == cultureId)
            {
                continue;
            }

            var rival = world.GetCellCultureInfluenceRaw(other, planeCode, q, r, z);
            if (rival > raw && rival >= _contestFloorRawMass)
            {
                return contested;
            }
        }

        return 1f;
    }
}
