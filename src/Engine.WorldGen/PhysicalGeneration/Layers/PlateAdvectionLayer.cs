namespace Engine.WorldGen;

using Engine.Constants;

/// <summary>
/// Moves crust, and everything crust remembers, one epoch's worth of plate
/// drift.
///
/// This is what makes epochs a mixer rather than a sequence of repaints. A
/// basin that sat in plate interior arrives at a convergent margin and is
/// uplifted with its sediment fill intact. Seafloor that accumulated pressure
/// history in the ocean rides into a collision, rises above sea level, and is
/// now land that still remembers being ocean. A volcanic chain built over a
/// mantle plume travels away from that plume and can later be thrust up as
/// peaks — the same magma source, two eras, two landforms.
///
/// Each epoch is still a static snapshot: plates do not move during an epoch,
/// they move between epochs, so nothing downstream has to solve a moving
/// boundary problem. Only persistent history advects; every derived fact
/// (climate, drainage, ice, classifications) is rebuilt afterwards anyway.
///
/// Sampling is a world-space gather. Where plates converge, several targets
/// pull from the same source and crust thickens; where they diverge, a target
/// finds nothing and is fresh crust with no history — which is exactly what a
/// spreading centre should produce.
/// </summary>
internal sealed class PlateAdvectionLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "plate-advection",
        0,
        PhysicalGeographyFacts.TectonicProvinces | PhysicalGeographyFacts.LandscapeHistory,
        PhysicalGeographyFacts.LandscapeHistory,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.TectonicMargins |
        PhysicalGeographyFacts.ContinentalDomains |
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.Landmasses |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        // Drift decays on its own curve: the assembling world reorganises at
        // continent scale, later epochs only adjust.
        var decay = Math.Pow(
            context.Profile.PlateDriftDecay / 1000d,
            Math.Max(0, context.EpochIndex));
        var worldExtent = map.Grid.Radius * Math.Sqrt(3d);
        var step = worldExtent * (context.Profile.PlateDriftPerEpoch / 1000d) * decay;
        map.AdvectionSourceIndex.Fill(-1);
        if (step <= 0d)
        {
            return;
        }

        var sources = ResolveSources(map, step);
        AdvectHistory(map, sources);
        map.AccumulatedPlateDrift += step;
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        LandscapeSurface.Validate(map);
        foreach (var source in map.AdvectionSourceIndex)
        {
            if (source >= map.Grid.TriangleCount)
            {
                throw new InvalidOperationException("Plate advection produced an invalid source index.");
            }
        }
    }

    /// <summary>
    /// For each triangle, which triangle's crust arrives here. The motion used
    /// is that of the province currently occupying the target, which is a fair
    /// approximation while a single epoch's displacement stays small relative
    /// to province size.
    /// </summary>
    private static int[] ResolveSources(PhysicalGeographyField map, double step)
    {
        var sources = new int[map.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < sources.Length; triangleIndex++)
        {
            var motionX = map.CrustMotionX[triangleIndex] / 1000d;
            var motionY = map.CrustMotionY[triangleIndex] / 1000d;
            var sourceX = map.Grid.TriangleX(triangleIndex) - (motionX * step);
            var sourceY = map.Grid.TriangleY(triangleIndex) - (motionY * step);
            if (!map.Grid.TryFindNearestTriangle(sourceX, sourceY, out var source))
            {
                sources[triangleIndex] = -1;
                continue;
            }

            // A gather always finds *some* source, so divergence would silently
            // duplicate crust instead of creating it. Where the crust would
            // have to come from the far side of a spreading boundary, none
            // exists yet: this is new ocean floor.
            sources[triangleIndex] =
                map.TectonicBoundaryCodes[triangleIndex] == PhysicalTectonicBoundaryCodes.Divergent &&
                map.CrustProvinceIds[source] != map.CrustProvinceIds[triangleIndex]
                    ? -1
                    : source;
        }

        return sources;
    }

    private static void AdvectHistory(PhysicalGeographyField map, int[] sources)
    {
        var before = HistorySnapshot.Capture(map);
        for (var triangleIndex = 0; triangleIndex < sources.Length; triangleIndex++)
        {
            var source = sources[triangleIndex];
            map.AdvectionSourceIndex[triangleIndex] = source;
            if (source < 0)
            {
                // New crust at a spreading centre: no inherited history.
                ResetToFreshCrust(map, triangleIndex);
                continue;
            }

            before.CopyInto(map, source, triangleIndex);
            LandscapeSurface.Rebuild(map, triangleIndex);
        }
    }

    /// <summary>
    /// The persistent history a piece of crust carries with it. Derived facts
    /// are absent by design: climate, drainage, ice, and every classification
    /// are rebuilt after the plates move.
    /// </summary>
    private sealed record HistorySnapshot(
        long[] Bedrock,
        long[] Sediment,
        long[] SedimentSupply,
        long[] Fluvial,
        long[] GlacialErosion,
        long[] GlacialDeposition,
        long[] Coastal,
        long[] Deposition,
        long[] Excavation,
        long[] Rim,
        long[] Ejecta,
        long[] Volcanic,
        int[] Tectonic,
        int[] Geothermal,
        int[] Wave,
        int[] MaximumIce,
        int[] MaximumFlux,
        int[] MaximumGrounded,
        int[] VolcanicEpoch,
        int[] ImpactHeat,
        int[] CoastalForcing,
        int[] FeatureFlags)
    {
        public static HistorySnapshot Capture(PhysicalGeographyField map) => new(
            map.BedrockElevation.ToArray(),
            map.SedimentThickness.ToArray(),
            map.SedimentSupply.ToArray(),
            map.CumulativeFluvialErosion.ToArray(),
            map.CumulativeGlacialErosion.ToArray(),
            map.CumulativeGlacialDeposition.ToArray(),
            map.CumulativeCoastalErosion.ToArray(),
            map.CumulativeDeposition.ToArray(),
            map.ImpactExcavation.ToArray(),
            map.ImpactRimUplift.ToArray(),
            map.ImpactEjectaThickness.ToArray(),
            map.VolcanicConstructionHistory.ToArray(),
            map.TectonicActivityHistory.ToArray(),
            map.GeothermalHistory.ToArray(),
            map.WaveExposureHistory.ToArray(),
            map.MaximumIceThickness.ToArray(),
            map.MaximumIceFlux.ToArray(),
            map.MaximumGroundedIceFlux.ToArray(),
            map.VolcanicLastActiveEpoch.ToArray(),
            map.ImpactHeat.ToArray(),
            map.ExternalCoastalForcingHistory.ToArray(),
            map.FeatureFlags.ToArray());

        public void CopyInto(PhysicalGeographyField map, int source, int target)
        {
            map.BedrockElevation[target] = Bedrock[source];
            map.SedimentThickness[target] = Sediment[source];
            map.SedimentSupply[target] = SedimentSupply[source];
            map.CumulativeFluvialErosion[target] = Fluvial[source];
            map.CumulativeGlacialErosion[target] = GlacialErosion[source];
            map.CumulativeGlacialDeposition[target] = GlacialDeposition[source];
            map.CumulativeCoastalErosion[target] = Coastal[source];
            map.CumulativeDeposition[target] = Deposition[source];
            map.ImpactExcavation[target] = Excavation[source];
            map.ImpactRimUplift[target] = Rim[source];
            map.ImpactEjectaThickness[target] = Ejecta[source];
            map.VolcanicConstructionHistory[target] = Volcanic[source];
            map.TectonicActivityHistory[target] = Tectonic[source];
            map.GeothermalHistory[target] = Geothermal[source];
            map.WaveExposureHistory[target] = Wave[source];
            map.MaximumIceThickness[target] = MaximumIce[source];
            map.MaximumIceFlux[target] = MaximumFlux[source];
            map.MaximumGroundedIceFlux[target] = MaximumGrounded[source];
            map.VolcanicLastActiveEpoch[target] = VolcanicEpoch[source];
            map.ImpactHeat[target] = ImpactHeat[source];
            map.ExternalCoastalForcingHistory[target] = CoastalForcing[source];

            // Cones, calderas, and vents describe the crust, so they travel
            // with it; transient flags such as ice state are rebuilt after.
            map.FeatureFlags[target] = FeatureFlags[source];
        }
    }

    private static void ResetToFreshCrust(PhysicalGeographyField map, int triangleIndex)
    {
        map.SedimentThickness[triangleIndex] = 0;
        map.SedimentSupply[triangleIndex] = 0;
        map.CumulativeFluvialErosion[triangleIndex] = 0;
        map.CumulativeGlacialErosion[triangleIndex] = 0;
        map.CumulativeGlacialDeposition[triangleIndex] = 0;
        map.CumulativeCoastalErosion[triangleIndex] = 0;
        map.CumulativeDeposition[triangleIndex] = 0;
        map.TectonicActivityHistory[triangleIndex] = 0;
        map.GeothermalHistory[triangleIndex] = 0;
        map.WaveExposureHistory[triangleIndex] = 0;
        map.MaximumIceThickness[triangleIndex] = 0;
        map.MaximumIceFlux[triangleIndex] = 0;
        map.MaximumGroundedIceFlux[triangleIndex] = 0;
        map.VolcanicConstructionHistory[triangleIndex] = 0;
        map.VolcanicLastActiveEpoch[triangleIndex] = -1;
        map.ImpactExcavation[triangleIndex] = 0;
        map.ImpactRimUplift[triangleIndex] = 0;
        map.ImpactEjectaThickness[triangleIndex] = 0;
        map.ImpactHeat[triangleIndex] = 0;
        map.ExternalCoastalForcingHistory[triangleIndex] = 0;
        map.FeatureFlags[triangleIndex] = 0;
        LandscapeSurface.Rebuild(map, triangleIndex);
    }
}
