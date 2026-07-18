namespace Engine.WorldGen;

internal sealed class PhysicalGeographyPipeline
{
    private readonly PhysicalGeographyGenerationProfile _profile;
    private readonly IReadOnlyList<IPhysicalGeographyLayer> _layers;

    internal PhysicalGeographyPipeline(
        PhysicalGeographyGenerationProfile profile,
        IReadOnlyList<IPhysicalGeographyLayer> layers)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(layers);
        _profile = profile;
        _layers = layers;
    }

    public static PhysicalGeographyPipeline CreateDefault(PhysicalGeographyGenerationProfile profile)
    {
        var ocean = new OceanConnectivityLayer();
        var bathymetry = new BathymetryLayer();
        var landmasses = new LandmassLayer();
        var warmth = new WarmthSourceLayer();
        var temperature = new TemperatureLayer();
        var moisture = new MoistureLayer();
        var runoff = new RunoffLayer();
        var lakes = new LakeBasinLayer();
        var flow = new FlowRoutingLayer();
        var rivers = new RiverNetworkLayer();
        var landIce = new LandIceLayer();
        var iceFlow = new IceFlowLayer();
        return new PhysicalGeographyPipeline(
            profile,
            [
                new TectonicProvinceLayer(),
                new ContinentalDomainLayer(),
                new TectonicUpliftLayer(),
                ocean,
                new FluvialErosionLayer(),
                ocean,
                bathymetry,
                landmasses,
                new FractureNetworkLayer(),
                new GeothermalLayer(),
                warmth,
                temperature,
                moisture,
                runoff,
                lakes,
                flow,
                rivers,
                landIce,
                iceFlow,
                new GlacialLandformLayer(),
                ocean,
                bathymetry,
                landmasses,
                warmth,
                temperature,
                moisture,
                runoff,
                lakes,
                flow,
                rivers,
                landIce,
                iceFlow,
                new SeaIceLayer(),
                new LandformClassificationLayer(),
                new PhysicalGeographyValidationLayer(),
            ]);
    }

    internal IReadOnlyList<IPhysicalGeographyLayer> Layers => _layers;

    public PhysicalGeographyField Generate(int radius, int seed)
    {
        _profile.Validate();
        var map = new PhysicalGeographyField(new PhysicalGeographyGrid(radius, _profile.Padding));
        foreach (var layer in _layers)
        {
            map = ApplyLayer(layer, map, seed);
        }

        return map;
    }

    internal PhysicalGeographyField ApplyLayer(
        IPhysicalGeographyLayer layer,
        PhysicalGeographyField map,
        int rootSeed)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(map);
        var missing = layer.Contract.Requires & ~map.AvailableFacts;
        if (missing != PhysicalGeographyFacts.None)
        {
            throw new InvalidOperationException(
                $"Physical generation layer '{layer.Contract.StableName}' is missing {missing}.");
        }

        var context = new PhysicalGeographyGenerationContext(rootSeed, _profile, layer.Contract);
        layer.Apply(map, context);
        layer.Validate(map, context);
        map.AvailableFacts &= ~layer.Contract.Invalidates;
        map.AvailableFacts |= layer.Contract.Produces;
        return map;
    }
}
