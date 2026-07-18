namespace IceRemembers.Seed.Tests;

using Engine.WorldGen;

using IceRemembers;

internal static class IceRemembersMicrogeographyLayerTestHarness
{
    public const int Radius = 12;
    public const int Seed = 77123;

    public static PhysicalGeographyGenerationProfile PhysicalProfile { get; } = new()
    {
        Padding = 6,
        ContinentalCenterCount = 3,
        OceanBasinCenterCount = 4,
        ContinentalRadius = 18d,
        ContinentalWarpScale = 14d,
        ContinentalWarpStrength = 5d,
        RidgeAxisCount = 4,
        RidgeHalfLength = 16d,
        RidgeWidth = 4d,
        FluvialErosionIterations = 2,
        IslandTriangleThreshold = 100,
        LakeDepthThreshold = 3,
        MinimumLakeTriangles = 2,
        EndorheicLakeFraction = 0d,
        RiverAccumulationThreshold = 700,
        MinimumRiverReachTriangles = 2,
        TemperatureBaseline = 160,
        WarmthPocketCount = 6,
        WarmthPocketMinRadius = 4d,
        WarmthPocketMaxRadius = 8d,
        HeartWarmthRadius = 8d,
        OceanModerationDistance = 24,
        OceanMoistureDistance = 28,
        FractureBranchCount = 10,
        GlacierFluxThreshold = 400,
        QualityTargets = new PhysicalGeographyQualityTargets
        {
            MinimumEvaluationRadius = 32,
        },
    };

    public static IceRemembersMicrogeographyGenerationProfile Profile { get; } = new()
    {
        MaximumFireCores = 24,
        StandingPressureRayCount = 72,
        CoastalPressureSourceCount = 4,
        AncientWakeSourceCount = 4,
        QualityTargets = new IceRemembersMicrogeographyQualityTargets
        {
            MinimumEvaluationRadius = 32,
            FireCoreCount = new CountTarget(1, 24),
        },
    };

    public static LayerResult GenerateThrough<TLayer>()
        where TLayer : class, IIceRemembersMicrogeographyLayer
    {
        var pipeline = IceRemembersMicrogeographyPipeline.CreateDefault(Profile);
        var map = NewMap();
        foreach (var layer in pipeline.Layers)
        {
            map = pipeline.ApplyLayer(layer, map, Seed);
            if (layer is TLayer)
            {
                return Result(map, layer, pipeline);
            }
        }

        throw new InvalidOperationException($"Layer {typeof(TLayer).Name} is not in the default pipeline.");
    }

    public static LayerResult GenerateBefore<TLayer>()
        where TLayer : class, IIceRemembersMicrogeographyLayer
    {
        var pipeline = IceRemembersMicrogeographyPipeline.CreateDefault(Profile);
        var map = NewMap();
        foreach (var layer in pipeline.Layers)
        {
            if (layer is TLayer)
            {
                return Result(map, layer, pipeline);
            }

            map = pipeline.ApplyLayer(layer, map, Seed);
        }

        throw new InvalidOperationException($"Layer {typeof(TLayer).Name} is not in the default pipeline.");
    }

    public static IceRemembersMicrogeographyMap GenerateComplete(
        IceRemembersMicrogeographyGenerationProfile? profile = null)
    {
        var selectedProfile = profile ?? Profile;
        return IceRemembersMicrogeographyPipeline.CreateDefault(selectedProfile)
            .Generate(NewPhysical(), Seed);
    }

    private static IceRemembersMicrogeographyMap NewMap() => new(NewPhysical());

    private static PhysicalGeographyField NewPhysical() =>
        PhysicalGeographyGenerator.Generate(Radius, Seed, PhysicalProfile);

    private static LayerResult Result(
        IceRemembersMicrogeographyMap map,
        IIceRemembersMicrogeographyLayer layer,
        IceRemembersMicrogeographyPipeline pipeline) =>
        new(
            map,
            layer,
            pipeline,
            new IceRemembersMicrogeographyGenerationContext(Seed, Profile, layer.Contract));

    internal sealed record LayerResult(
        IceRemembersMicrogeographyMap Map,
        IIceRemembersMicrogeographyLayer Layer,
        IceRemembersMicrogeographyPipeline Pipeline,
        IceRemembersMicrogeographyGenerationContext Context);
}
