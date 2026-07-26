namespace Engine.Kernel.Tests;

using Engine.Constants;
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
                    MainProgramCadenceTicks = mainTickRatio,
                },
            },
        });
    }

    /// <summary>
    /// Materializes a disk of semantic cells so movement, placement, and
    /// influence tests have known idea-ground to act on. The semantic plane is
    /// now a growing map (cells are known only once materialized), so unit tests
    /// that exercise semantic mechanics must seed the region they operate in -
    /// the same way the real seed materializes its idea-space heart.
    /// </summary>
    public static void SeedSemanticCells(Simulation simulation, int radius) =>
        SeedSemanticCells(simulation, 0, 0, radius);

    public static void SeedSemanticCells(Simulation simulation, int centerQ, int centerR, int radius, int terrainId = 1)
    {
        System.ArgumentNullException.ThrowIfNull(simulation);
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minR = System.Math.Max(-radius, -dq - radius);
            var maxR = System.Math.Min(radius, -dq + radius);
            for (var dr = minR; dr <= maxR; dr++)
            {
                simulation.CreateCell(PlaneCodes.Semantic, centerQ + dq, centerR + dr, terrainId, ownerEntityId: 0);
            }
        }
    }
}
