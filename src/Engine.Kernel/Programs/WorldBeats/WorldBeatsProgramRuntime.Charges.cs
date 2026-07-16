namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class WorldBeatsProgramRuntime
{
    /// <summary>
    /// Pushes the open ages' rate charges into the target programs. The push
    /// is idempotent and runs every pulse: targets are reset to neutral, then
    /// every open age's charges apply multiplicatively. Target programs own
    /// their own modulation arrays, so hot-path reads stay O(1) and a
    /// snapshot reload self-heals on the next pulse.
    /// </summary>
    private void PushCharges(Simulation simulation)
    {
        simulation.LowAltitudeVerbs.ResetAgeWeightMultipliers();
        simulation.Generators.ResetAgePulseWeightMultipliers();
        simulation.Gambits.ResetAgeScoreMultipliers();
        simulation.Gambits.ResetAgeSuccessMultipliers();
        simulation.CultureFieldDecay.SetContestTide(1f);
        simulation.CultureFieldDecay.SetGlobalTide(1f);
        simulation.Movement.SetAgeStepRateMultiplier(1f);

        for (var track = 0; track < WorldBeatTrackCodes.Count; track++)
        {
            var slot = State.OpenAgeSlots[track];
            if (slot == 0)
            {
                continue;
            }

            ApplyAgeCharges(simulation, slot);
        }
    }

    private void ApplyAgeCharges(Simulation simulation, int ageSlot)
    {
        var start = Data.ChargeStart(ageSlot);
        var count = Data.ChargeCount(ageSlot);
        for (var charge = start; charge < start + count; charge++)
        {
            var targetId = Data.ChargeTargetId(charge);
            var multiplier = Data.ChargeMultiplier(charge);
            switch (Data.ChargeDomainCode(charge))
            {
                case WorldBeatChargeDomainCodes.GeneratorPulseWeight:
                    simulation.Generators.ApplyAgePulseWeightMultiplier(targetId, multiplier);
                    break;
                case WorldBeatChargeDomainCodes.VerbWeight:
                    simulation.LowAltitudeVerbs.ApplyAgeWeightMultiplier(targetId, multiplier);
                    break;
                case WorldBeatChargeDomainCodes.GambitScore:
                    simulation.Gambits.ApplyAgeScoreMultiplier(targetId, multiplier);
                    break;
                case WorldBeatChargeDomainCodes.ContestDecayTide:
                    simulation.CultureFieldDecay.ScaleContestTide(multiplier);
                    break;
                case WorldBeatChargeDomainCodes.GlobalDecayTide:
                    simulation.CultureFieldDecay.ScaleGlobalTide(multiplier);
                    break;
                case WorldBeatChargeDomainCodes.GambitSuccess:
                    simulation.Gambits.ApplyAgeSuccessMultiplier(targetId, multiplier);
                    break;
                case WorldBeatChargeDomainCodes.MovementStepRate:
                    simulation.Movement.ScaleAgeStepRateMultiplier(multiplier);
                    break;
                default:
                    break;
            }
        }
    }
}
