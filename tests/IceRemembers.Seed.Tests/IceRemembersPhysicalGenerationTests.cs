namespace IceRemembers.Seed.Tests;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;

public sealed class IceRemembersPhysicalGenerationTests
{
    [Theory]
    [InlineData(42)]
    [InlineData(9173)]
    [InlineData(21031)]
    public void ProductionProfile_SatisfiesConfiguredTargetsAndUsesExpandedNaturalSurfaces(int seed)
    {
        var field = PhysicalGeographyGenerator.Generate(
            IceRemembersSeedBuilder.GenerationRadius,
            seed,
            IceRemembersPhysicalGeography.GenerationProfile);
        var microgeography = IceRemembersMicrogeographyGenerator.Generate(
            field,
            seed,
            IceRemembersPhysicalGeography.MicrogeographyProfile);

        Assert.Equal(IceRemembersSeedBuilder.GenerationRadius, field.Grid.Radius);
        Assert.True(microgeography.AvailableFacts.HasFlag(IceRemembersMicrogeographyFacts.Validated));

        var naturalSurfaces = new HashSet<int>();
        for (var configuredIndex = 0; configuredIndex < field.Grid.ConfiguredHexCount; configuredIndex++)
        {
            var hexIndex = field.Grid.ConfiguredHexIndexAt(configuredIndex);
            for (var localTriangle = 0;
                localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
                localTriangle++)
            {
                naturalSurfaces.Add(microgeography.BaseSurfaceIndexes[
                    field.Grid.TriangleIndex(hexIndex, localTriangle)]);
            }
        }

        Assert.Contains((int)IceRemembersPhysicalSurface.PolarBarrens, naturalSurfaces);
        Assert.Contains((int)IceRemembersPhysicalSurface.WetTundra, naturalSurfaces);
        Assert.Contains((int)IceRemembersPhysicalSurface.ColdSteppe, naturalSurfaces);
        Assert.Contains((int)IceRemembersPhysicalSurface.ScreeField, naturalSurfaces);
        Assert.Contains((int)IceRemembersPhysicalSurface.RiverValley, naturalSurfaces);
    }
}
