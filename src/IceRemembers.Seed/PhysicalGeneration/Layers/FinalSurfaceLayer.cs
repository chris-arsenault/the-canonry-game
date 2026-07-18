namespace IceRemembers.Seed;

using IceRemembers;

internal sealed class FinalSurfaceLayer : IIceRemembersMicrogeographyLayer
{
    private static readonly (IceRemembersPhysicalFeature Feature, IceRemembersPhysicalSurface Surface)[]
        Precedence =
        [
            (IceRemembersPhysicalFeature.PressureAuroraBreach, IceRemembersPhysicalSurface.PressureAuroraBreach),
            (IceRemembersPhysicalFeature.SteamFracture, IceRemembersPhysicalSurface.SteamFracture),
            (IceRemembersPhysicalFeature.ThermalWarRim, IceRemembersPhysicalSurface.ThermalWarRim),
            (IceRemembersPhysicalFeature.PressureGodLocus, IceRemembersPhysicalSurface.PressureGodLocus),
            (IceRemembersPhysicalFeature.FirecoreVent, IceRemembersPhysicalSurface.FirecoreVent),
            (IceRemembersPhysicalFeature.LightWell, IceRemembersPhysicalSurface.LightWell),
            (IceRemembersPhysicalFeature.HotSpring, IceRemembersPhysicalSurface.HotSpring),
            (IceRemembersPhysicalFeature.CrushZone, IceRemembersPhysicalSurface.CrushZone),
            (IceRemembersPhysicalFeature.WakeScar, IceRemembersPhysicalSurface.WakeScar),
            (IceRemembersPhysicalFeature.RogueImpact, IceRemembersPhysicalSurface.RogueImpact),
            (IceRemembersPhysicalFeature.FrostPrism, IceRemembersPhysicalSurface.FrostPrism),
            (IceRemembersPhysicalFeature.AuroraField, IceRemembersPhysicalSurface.AuroraField),
            (IceRemembersPhysicalFeature.InterferenceLattice, IceRemembersPhysicalSurface.InterferenceLattice),
            (IceRemembersPhysicalFeature.ThermalVein, IceRemembersPhysicalSurface.ThermalVein),
            (IceRemembersPhysicalFeature.BasaltFlat, IceRemembersPhysicalSurface.BasaltFlat),
            (IceRemembersPhysicalFeature.MeltLine, IceRemembersPhysicalSurface.MeltLine),
            (IceRemembersPhysicalFeature.ThawedHalo, IceRemembersPhysicalSurface.ThawedHalo),
        ];

    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "final-surfaces",
        0,
        IceRemembersMicrogeographyFacts.NaturalSurfaces |
        IceRemembersMicrogeographyFacts.Interactions,
        IceRemembersMicrogeographyFacts.FinalSurfaces,
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            map.SurfaceIndexes[triangleIndex] = (int)Classify(
                map.BaseSurfaceIndexes[triangleIndex],
                map.SignificantFeatureFlags[triangleIndex]);
        }
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            if ((uint)map.SurfaceIndexes[triangleIndex] >=
                    (uint)IceRemembersPhysicalGeography.SurfacePalette.Count ||
                map.SurfaceIndexes[triangleIndex] != (int)Classify(
                    map.BaseSurfaceIndexes[triangleIndex],
                    map.SignificantFeatureFlags[triangleIndex]))
            {
                throw new InvalidOperationException("Final surface classification is invalid.");
            }
        }
    }

    private static IceRemembersPhysicalSurface Classify(int baseSurfaceIndex, ulong flags)
    {
        foreach (var candidate in Precedence)
        {
            if (IceRemembersFeatureMasks.Has(flags, candidate.Feature))
            {
                return candidate.Surface;
            }
        }

        return (IceRemembersPhysicalSurface)baseSurfaceIndex;
    }
}
