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
        var cadence = global::System.Math.Max(1, _cadenceTicks[slot]);
        if (_nextCadenceFireTicks[slot] == 0)
        {
            // First sighting: desynchronize timelines across generators.
            _nextCadenceFireTicks[slot] = tick + 1 + simulation.RandomInt(
                cadence,
                slot,
                _generatorIds[slot],
                KernelRandomPurposeCodes.GeneratorCadenceStart,
                attempt: tick);
            return;
        }

        if (tick < _nextCadenceFireTicks[slot])
        {
            return;
        }

        _nextCadenceFireTicks[slot] = tick + global::System.Math.Max(1, cadence + CadenceJitter(simulation, slot));
        for (var other = 1; other <= _generatorCount; other++)
        {
            ResetFireScratch(other);
        }

        EvaluateGenerator(simulation, slot);
        if (_fireStatusCodes[slot] == GeneratorStatusCodes.Eligible)
        {
            _ = TryFireEligibleGenerator(simulation, slot, tick);
        }
    }

    private int CadenceJitter(Simulation simulation, int slot)
    {
        var jitter = _cadenceJitterTicks[slot];
        return jitter <= 0
            ? 0
            : simulation.RandomInt(
                (2 * jitter) + 1,
                slot,
                _generatorIds[slot],
                KernelRandomPurposeCodes.GeneratorCadenceJitter,
                attempt: _nextCadenceFireTicks[slot]) - jitter;
    }

    private void ResetFireScratch(int slot)
    {
        _fireStatusCodes[slot] = GeneratorStatusCodes.NotEvaluated;
        _fireCandidateCounts[slot] = 0;
        _fireSelectionWeights[slot] = 0;
        _fireSelectedTargetOccurrenceHandles[slot] = 0;
        _fireFailedCounts[slot] = 0;
    }
}
