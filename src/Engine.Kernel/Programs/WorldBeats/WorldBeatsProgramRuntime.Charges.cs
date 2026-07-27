namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class WorldBeatsProgramRuntime
{
    /// <summary>
    /// Pushes the open ages' rate charges into the target programs. The push
    /// is idempotent and runs on every scheduled invocation: targets are reset to neutral, then
    /// every open age's charges apply multiplicatively. Target programs own
    /// their own modulation arrays, so hot-path reads stay O(1) and a
    /// snapshot reload self-heals on the next run.
    /// </summary>
    private void PushCharges(Simulation simulation)
    {
        simulation.ResetWorldBeatCharges();

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
            simulation.ApplyWorldBeatCharge(
                Data.ChargeDomainCode(charge),
                targetId,
                multiplier);
        }
    }
}
