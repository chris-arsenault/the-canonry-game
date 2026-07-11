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
    private readonly int[] _scratchQ;
    private readonly int[] _scratchR;
    private readonly float[] _scratchRaw;

    internal CultureFieldDecayProgramRuntime(CultureFieldTuningOptions tuning, int cellCapacity)
    {
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCapacity);
        _decayPerPulse = tuning.DecayPerPulse;
        _minimumDecayPerPulse = tuning.MinimumDecayPerPulse;
        _cullFloorRawMass = tuning.CullFloorRawMass;
        _scratchQ = new int[cellCapacity];
        _scratchR = new int[cellCapacity];
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
               world.MoveNextRawCultureInfluenceCell(ref cursor, out var q, out var r, out var raw))
        {
            if (raw <= 0f)
            {
                continue;
            }

            _scratchQ[count] = q;
            _scratchR[count] = r;
            _scratchRaw[count] = raw;
            count++;
        }

        for (var i = 0; i < count; i++)
        {
            var raw = _scratchRaw[i];
            var decay = global::System.MathF.Max(raw * _decayPerPulse, _minimumDecayPerPulse);
            if (raw - decay < _cullFloorRawMass)
            {
                decay = raw;
            }

            _ = simulation.ModifyCellCultureInfluenceRaw(
                cultureId,
                planeCode,
                _scratchQ[i],
                _scratchR[i],
                -decay,
                tick);
        }
    }
}
