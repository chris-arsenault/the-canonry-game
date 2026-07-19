namespace IceRemembers.Seed.Tests;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;

public sealed class IceRemembersNaturalAndFireLayerTests
{
    [Fact]
    public void NaturalSurfaces_ExpressColdClimateLandformsAndRejectInvalidSurface()
    {
        var result = IceRemembersMicrogeographyLayerTestHarness.GenerateThrough<NaturalSurfaceLayer>();
        var candidates = Enumerable.Range(0, result.Map.Physical.Grid.TriangleCount)
            .Where(index => result.Map.Physical.WaterKindCodes[index] == PhysicalWaterKindCodes.None)
            .Take(8)
            .ToArray();
        Assert.Equal(8, candidates.Length);

        ConfigureLand(result.Map, candidates[0], temperature: 205, moisture: 90, slope: 5);
        ConfigureLand(result.Map, candidates[1], temperature: 285, moisture: 760, slope: 5);
        ConfigureLand(result.Map, candidates[2], temperature: 280, moisture: 300, slope: 5);
        ConfigureLand(result.Map, candidates[3], temperature: 280, moisture: 300, slope: 100);
        ConfigureLand(result.Map, candidates[4], temperature: 280, moisture: 300, slope: 5);
        result.Map.Physical.RiverOrders[candidates[4]] = 2;
        ConfigureLand(result.Map, candidates[5], temperature: 280, moisture: 300, slope: 5);
        result.Map.Physical.RiverChannelStrength[candidates[5]] = 700;
        result.Map.Physical.RiverChannelOrders[candidates[5]] = 3;
        ConfigureLand(result.Map, candidates[6], temperature: 280, moisture: 300, slope: 5);
        result.Map.Physical.FloodplainIntensity[candidates[6]] = 600;
        ConfigureLand(result.Map, candidates[7], temperature: 280, moisture: 300, slope: 5);
        result.Map.Physical.DeltaIntensity[candidates[7]] = 800;
        result.Map.Physical.RiverChannelStrength[candidates[7]] = 800;
        result.Map.Physical.RiverChannelOrders[candidates[7]] = 3;

        result.Layer.Apply(result.Map, result.Context);
        Assert.Equal((int)IceRemembersPhysicalSurface.PolarBarrens, result.Map.BaseSurfaceIndexes[candidates[0]]);
        Assert.Equal((int)IceRemembersPhysicalSurface.WetTundra, result.Map.BaseSurfaceIndexes[candidates[1]]);
        Assert.Equal((int)IceRemembersPhysicalSurface.ColdSteppe, result.Map.BaseSurfaceIndexes[candidates[2]]);
        Assert.Equal((int)IceRemembersPhysicalSurface.ScreeField, result.Map.BaseSurfaceIndexes[candidates[3]]);
        Assert.Equal((int)IceRemembersPhysicalSurface.RiverValley, result.Map.BaseSurfaceIndexes[candidates[4]]);
        Assert.Equal((int)IceRemembersPhysicalSurface.RiverChannel, result.Map.BaseSurfaceIndexes[candidates[5]]);
        Assert.Equal((int)IceRemembersPhysicalSurface.Floodplain, result.Map.BaseSurfaceIndexes[candidates[6]]);
        Assert.Equal((int)IceRemembersPhysicalSurface.Delta, result.Map.BaseSurfaceIndexes[candidates[7]]);

        result.Map.BaseSurfaceIndexes[candidates[0]] = IceRemembersPhysicalGeography.SurfacePalette.Count;
        Assert.Throws<InvalidOperationException>(() => result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void GeneratedFireField_FollowsFracturesFromBoundedCoresAndRejectsInvalidIntensity()
    {
        var result = IceRemembersMicrogeographyLayerTestHarness.GenerateThrough<FireFieldLayer>();
        var cores = Enumerable.Range(0, result.Map.Physical.Grid.TriangleCount)
            .Where(index => (result.Map.Physical.FeatureFlags[index] &
                PhysicalGeographyFeatureCodes.FireCore) != 0)
            .ToArray();

        Assert.NotEmpty(cores);
        Assert.True(cores.Length <= result.Context.Profile.MaximumFireCores);
        Assert.All(cores, index => Assert.True(result.Map.FireIntensity[index] > 0));
        Assert.Contains(result.Map.FireIntensity, intensity => intensity is > 0 and < 700);

        result.Map.FireIntensity[0] = -1;
        Assert.Throws<InvalidOperationException>(() => result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void ClimateFeedbackFromFire_HeatsOrMeltsBurningTerrainAndRejectsInvalidClimate()
    {
        var result = IceRemembersMicrogeographyLayerTestHarness.GenerateBefore<FireClimateFeedbackLayer>();
        var temperatures = result.Map.Physical.Temperature.ToArray();
        var ice = result.Map.Physical.IceThickness.ToArray();
        result.Pipeline.ApplyLayer(result.Layer, result.Map, IceRemembersMicrogeographyLayerTestHarness.Seed);

        Assert.Contains(
            Enumerable.Range(0, result.Map.Physical.Grid.TriangleCount),
            index => result.Map.FireIntensity[index] > 0 &&
                (result.Map.Physical.Temperature[index] > temperatures[index] ||
                    result.Map.Physical.IceThickness[index] < ice[index]));

        result.Map.Physical.Temperature[0] = 1001;
        Assert.Throws<InvalidOperationException>(() => result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void Pipeline_RejectsAFieldLayerWhenItsDeclaredDependencyIsMissing()
    {
        var result = IceRemembersMicrogeographyLayerTestHarness.GenerateBefore<NaturalSurfaceLayer>();
        var fire = new FireFieldLayer();
        Assert.Throws<InvalidOperationException>(() =>
            result.Pipeline.ApplyLayer(
                fire,
                result.Map,
                IceRemembersMicrogeographyLayerTestHarness.Seed));
    }

    private static void ConfigureLand(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int temperature,
        int moisture,
        int slope)
    {
        map.Physical.WaterKindCodes[triangleIndex] = PhysicalWaterKindCodes.None;
        map.Physical.BathymetricZoneCodes[triangleIndex] = PhysicalBathymetricZoneCodes.Land;
        map.Physical.LandformCodes[triangleIndex] = PhysicalLandformCodes.Plain;
        map.Physical.IceThickness[triangleIndex] = 0;
        map.Physical.RiverOrders[triangleIndex] = 0;
        map.Physical.RiverChannelStrength[triangleIndex] = 0;
        map.Physical.RiverChannelOrders[triangleIndex] = 0;
        map.Physical.FloodplainIntensity[triangleIndex] = 0;
        map.Physical.DeltaIntensity[triangleIndex] = 0;
        map.Physical.Temperature[triangleIndex] = temperature;
        map.Physical.Moisture[triangleIndex] = moisture;
        map.Physical.Slope[triangleIndex] = slope;
    }
}
