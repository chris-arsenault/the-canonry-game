namespace Engine.Kernel.Tests;

using Engine.Kernel;

internal static class KernelTestSimulation
{
    public static Simulation Create(SimulationOptions? options = null, int mainTickRatio = 1)
    {
        options ??= new SimulationOptions();
        return new Simulation(options with
        {
            KernelTuning = options.KernelTuning with
            {
                Programs = options.KernelTuning.Programs with
                {
                    MinorActorMainTickRatio = mainTickRatio,
                },
            },
        });
    }
}
