namespace Engine.WorldGen.Tests.Map;

internal static class PhysicalGeographyLayerTestHarness
{
    public const int ContentTriangleCount = 7_944;
    public const int Seed = 9173;

    public static PhysicalGeographyGenerationProfile Profile { get; } = new()
    {
        BoundaryPaddingSteps = 36,
        TectonicProvinceCount = 6,
        ContinentalAssemblyCount = 2,
        ContinentalTransitionWidth = 8d,
        ContinentalWarpScale = 12d,
        ContinentalWarpStrength = 4d,
        RidgeWidth = 4d,
        MantlePlumeCount = 2,
        MantlePlumeRadius = 4d,
        VolcanicConeRadius = 2d,
        MantlePlumeMinimumSpacing = 8d,
        FluvialErosionIterations = 2,
        IslandTriangleThreshold = 100,
        LakeDepthThreshold = 3,
        MinimumLakeTriangles = 2,
        RunoffMinimumLossPerMille = 0,
        RunoffTemperatureLossPerMille = 0,
        LakeEvaporationMinimum = 1,
        LakeEvaporationTemperatureStrength = 0,
        LakeInfiltrationLoss = 0,
        RiverMinimumBasinDischarge = 1_000,
        RiverAccumulationThreshold = 700,
        RiverMinimumAccumulationThreshold = 180,
        RiverBasinHeadwaterFraction = 90,
        MinimumRiverReachTriangles = 2,
        TemperatureBaseline = 150,
        WarmthPocketCount = 6,
        WarmthPocketMinRadius = 4d,
        WarmthPocketMaxRadius = 8d,
        HeartWarmthRadius = 8d,
        OceanModerationDistance = 24,
        OceanMoistureDistance = 28,
        FractureBranchCount = 10,
        GlacierFluxThreshold = 400,
    };

    public static LayerResult GenerateThrough<TLayer>()
        where TLayer : class, IPhysicalGeographyLayer
    {
        Profile.Validate();
        var pipeline = PhysicalGeographyPipeline.CreateDefault(Profile);
        var map = new PhysicalGeographyField(PhysicalGeographyGrid.CreateWithPadding(
            ContentTriangleCount,
            Profile.BoundaryPaddingSteps));
        foreach (var layer in pipeline.Layers)
        {
            map = pipeline.ApplyLayer(layer, map, Seed);
            if (layer is TLayer)
            {
                return new LayerResult(
                    map,
                    layer,
                    new PhysicalGeographyGenerationContext(Seed, Profile, layer.Contract));
            }
        }

        throw new InvalidOperationException($"Default pipeline does not contain {typeof(TLayer).Name}.");
    }

    public static LayerResult GenerateComplete()
    {
        var pipeline = PhysicalGeographyPipeline.CreateDefault(Profile);
        var map = pipeline.Generate(ContentTriangleCount, Seed);
        var layer = pipeline.Layers[^1];
        return new LayerResult(
            map,
            layer,
            new PhysicalGeographyGenerationContext(Seed, Profile, layer.Contract));
    }

    internal sealed record LayerResult(
        PhysicalGeographyField Map,
        IPhysicalGeographyLayer Layer,
        PhysicalGeographyGenerationContext Context);
}
