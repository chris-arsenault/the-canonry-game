namespace IceRemembers.Seed;

/// <summary>
/// Carries magic history with the crust when plates drift between epochs.
///
/// This is how ocean-born magic ends up on land. Pressure is generated in the
/// ocean because that is where the Dead God lies, and seafloor accumulates its
/// record. When that plate converges and the crust is thrust above sea level,
/// it arrives as land still carrying the pressure it accumulated as ocean
/// floor — an inland pressure relic is former seabed, not magic leaking
/// ashore. Fire and aurora records travel the same way, so a range can carry
/// the memory of firecores that burned in it before it was a range.
///
/// `Engine.WorldGen` publishes the per-triangle source mapping without knowing
/// what magic is; this layer applies it to the fields the domain owns.
/// </summary>
internal sealed class MagicAdvectionLayer : IIceRemembersMicrogeographyLayer
{
    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "magic-advection",
        0,
        IceRemembersMicrogeographyFacts.None,
        IceRemembersMicrogeographyFacts.MagicHistory,
        IceRemembersMicrogeographyFacts.FinalSurfaces |
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = context;
        var sources = map.Physical.AdvectionSourceIndex;
        var fireHistory = (int[])map.FireHistory.Clone();
        var auroraHistory = (int[])map.AuroraHistory.Clone();
        var pressureHistory = (int[])map.PressureHistory.Clone();
        var pressureCoastal = (int[])map.PressureCoastalHistory.Clone();
        var fireFirst = (int[])map.FireFirstEpoch.Clone();
        var auroraFirst = (int[])map.AuroraFirstEpoch.Clone();
        var pressureFirst = (int[])map.PressureFirstEpoch.Clone();
        var pressureCoastalFirst = (int[])map.PressureCoastalFirstEpoch.Clone();

        for (var triangleIndex = 0; triangleIndex < sources.Length; triangleIndex++)
        {
            var source = sources[triangleIndex];
            if (source < 0)
            {
                // Crust newly created at a spreading centre remembers nothing.
                map.FireHistory[triangleIndex] = 0;
                map.AuroraHistory[triangleIndex] = 0;
                map.PressureHistory[triangleIndex] = 0;
                map.PressureCoastalHistory[triangleIndex] = 0;
                map.FireFirstEpoch[triangleIndex] = -1;
                map.AuroraFirstEpoch[triangleIndex] = -1;
                map.PressureFirstEpoch[triangleIndex] = -1;
                map.PressureCoastalFirstEpoch[triangleIndex] = -1;
                continue;
            }

            map.FireHistory[triangleIndex] = fireHistory[source];
            map.AuroraHistory[triangleIndex] = auroraHistory[source];
            map.PressureHistory[triangleIndex] = pressureHistory[source];
            map.PressureCoastalHistory[triangleIndex] = pressureCoastal[source];
            map.FireFirstEpoch[triangleIndex] = fireFirst[source];
            map.AuroraFirstEpoch[triangleIndex] = auroraFirst[source];
            map.PressureFirstEpoch[triangleIndex] = pressureFirst[source];
            map.PressureCoastalFirstEpoch[triangleIndex] = pressureCoastalFirst[source];
        }
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        var maximumHistory = checked(Math.Max(1, map.CompletedEpochCount) * 1000);
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            if (map.FireHistory[triangleIndex] < 0 ||
                map.AuroraHistory[triangleIndex] < 0 ||
                map.PressureHistory[triangleIndex] < 0 ||
                map.PressureCoastalHistory[triangleIndex] < 0 ||
                map.PressureCoastalHistory[triangleIndex] > maximumHistory ||
                map.FireFirstEpoch[triangleIndex] >= context.EpochCount ||
                map.PressureFirstEpoch[triangleIndex] >= context.EpochCount)
            {
                throw new InvalidOperationException("Advected magic history is invalid.");
            }
        }
    }
}
