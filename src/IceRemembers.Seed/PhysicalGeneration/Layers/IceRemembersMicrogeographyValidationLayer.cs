namespace IceRemembers.Seed;

using Engine.WorldGen;

using IceRemembers;

internal sealed class IceRemembersMicrogeographyValidationLayer : IIceRemembersMicrogeographyLayer
{
    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "microgeography-structural-validation",
        0,
        IceRemembersMicrogeographyFacts.FinalSurfaces,
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = map;
        _ = context;
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = context;
        var surfaceCount = IceRemembersPhysicalGeography.SurfacePalette.Count;
        var allFeatures = (1UL << IceRemembersPhysicalGeography.FeaturePalette.Count) - 1UL;
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            if (map.BaseSurfaceIndexes[triangleIndex] is < (int)IceRemembersPhysicalSurface.Tundra or
                    > (int)IceRemembersPhysicalSurface.Trench ||
                (uint)map.SurfaceIndexes[triangleIndex] >= (uint)surfaceCount ||
                (map.SignificantFeatureFlags[triangleIndex] & ~allFeatures) != 0)
            {
                throw new InvalidOperationException(
                    "Ice Remembers microgeography contains an invalid code.");
            }
        }
    }
}
