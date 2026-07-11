namespace Engine.Kernel;

internal sealed partial class GeneratorProgramRuntime
{
    private void TickCadenceGenerators(Simulation simulation, int tick)
    {
        if (tick <= 0)
        {
            return;
        }

        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            if (_cadenceTicks[slot] <= 0 || !_enabled[slot])
            {
                continue;
            }

            TickCadenceGenerator(simulation, slot, tick);
        }
    }

    private void TickCadenceGenerator(Simulation simulation, int slot, int tick)
    {
        var scaledCadence = global::System.Math.Max(1, Tuning.ScaleMainTicks(_cadenceTicks[slot]));
        if (_nextCadenceFireTicks[slot] == 0)
        {
            // First sighting: desynchronize timelines across generators.
            _nextCadenceFireTicks[slot] = tick + 1 + simulation.NextInt(scaledCadence);
            return;
        }

        if (tick < _nextCadenceFireTicks[slot])
        {
            return;
        }

        _nextCadenceFireTicks[slot] = tick + global::System.Math.Max(1, scaledCadence + CadenceJitter(simulation, slot));
        for (var other = 1; other <= _generatorCount; other++)
        {
            ResetPulseScratch(other);
        }

        EvaluateGenerator(simulation, slot);
        if (_pulseStatusCodes[slot] == GeneratorStatusCodes.Eligible)
        {
            _ = TryFireEligibleGenerator(simulation, slot, tick);
        }
    }

    private int CadenceJitter(Simulation simulation, int slot)
    {
        var jitter = Tuning.ScaleMainTicks(_cadenceJitterTicks[slot]);
        return jitter <= 0 ? 0 : simulation.NextInt((2 * jitter) + 1) - jitter;
    }

    private void ResetPulseScratch(int slot)
    {
        _pulseStatusCodes[slot] = GeneratorStatusCodes.NotEvaluated;
        _pulseCandidateCounts[slot] = 0;
        _pulseSelectionWeights[slot] = 0;
        _pulseSelectedTargetIds[slot] = 0;
        _pulseFailedFireCounts[slot] = 0;
    }
}
