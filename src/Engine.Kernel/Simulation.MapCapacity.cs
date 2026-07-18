namespace Engine.Kernel;

internal sealed partial class Simulation
{
    private static void ValidateMapCapacities(SimulationOptions options)
    {
        if (options.MapGenerationRadiusCapacity < 0 ||
            options.MapFrontierCandidateCapacity <= 0 ||
            options.MapFrontierThawRingCapacity <= 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(options));
        }

        if (options.MapGenerationRadiusCapacity == 0)
        {
            return;
        }

        var radius = (long)options.MapGenerationRadiusCapacity;
        var planeCellCount = checked(1L + (3L * radius * (radius + 1L)));
        var twoPlaneCellCount = checked(planeCellCount * 2L);
        if (HasInsufficientMapCapacity(options, planeCellCount, twoPlaneCellCount))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(options));
        }
    }

    private static bool HasInsufficientMapCapacity(
        SimulationOptions options,
        long planeCellCount,
        long twoPlaneCellCount) =>
        options.MapCellCapacity < twoPlaneCellCount ||
            options.MapInfluenceCapacity < twoPlaneCellCount ||
            options.MovementCellCapacity < twoPlaneCellCount ||
            options.MapActivityCellCapacity < twoPlaneCellCount ||
            options.MapSignalCellCapacity < twoPlaneCellCount ||
            options.MapFrontierThawRingCapacity < planeCellCount;
}
