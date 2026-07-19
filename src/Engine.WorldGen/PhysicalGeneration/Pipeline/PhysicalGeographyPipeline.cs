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
        var oceanDistance = new OceanDistanceLayer();
        var bathymetry = new BathymetryLayer();
        var landmasses = new LandmassLayer();
        var currents = new OceanCurrentLayer();
        var warmth = new WarmthSourceLayer();
        var temperature = new TemperatureLayer();
        var atmosphericFlow = new AtmosphericFlowLayer();
        var moisture = new MoistureLayer();
        var precipitation = new PrecipitationLayer();
        var runoff = new RunoffLayer();
        var lakes = new LakeBasinLayer();
        var flow = new FlowRoutingLayer();
        var rivers = new RiverNetworkLayer();
        var landIce = new LandIceLayer();
        var iceFlow = new IceFlowLayer();
        var layers = new List<IPhysicalGeographyLayer>
        {
            new TectonicProvinceLayer(),
            new ContinentalDomainLayer(),
            new TectonicUpliftLayer(),
            new LandscapeInitializationLayer(),
            ocean,
            new FluvialErosionLayer(),
            ocean,
            new FractureNetworkLayer(),
            new GeothermalLayer(),
        };
        AddClimateAndDrainage(
            layers, ocean, oceanDistance, bathymetry, landmasses, currents, warmth, temperature,
            atmosphericFlow, moisture, precipitation, runoff, lakes, flow, rivers);
        layers.Add(new SedimentTransportLayer());
        AddClimateAndDrainage(
            layers, ocean, oceanDistance, bathymetry, landmasses, currents, warmth, temperature,
            atmosphericFlow, moisture, precipitation, runoff, lakes, flow, rivers);
        layers.Add(new CoastalErosionLayer());
        AddClimateAndDrainage(
            layers, ocean, oceanDistance, bathymetry, landmasses, currents, warmth, temperature,
            atmosphericFlow, moisture, precipitation, runoff, lakes, flow, rivers);
        layers.Add(landIce);
        layers.Add(iceFlow);
        layers.Add(new GlacialErosionLayer());
        layers.Add(new GlacialDepositionLayer());
        AddClimateAndDrainage(
            layers, ocean, oceanDistance, bathymetry, landmasses, currents, warmth, temperature,
            atmosphericFlow, moisture, precipitation, runoff, lakes, flow, rivers);
        layers.Add(landIce);
        layers.Add(iceFlow);
        layers.Add(new GlacialLandformLayer());
        layers.Add(new SeaIceLayer());
        layers.Add(new LandformClassificationLayer());
        layers.Add(new RiverChannelGeometryLayer());
        layers.Add(new FloodplainLayer());
        layers.Add(new DeltaFormationLayer());
        layers.Add(new PhysicalGeographyValidationLayer());
        return new PhysicalGeographyPipeline(profile, layers);
    }

    private static void AddClimateAndDrainage(
        List<IPhysicalGeographyLayer> layers,
        IPhysicalGeographyLayer ocean,
        IPhysicalGeographyLayer oceanDistance,
        IPhysicalGeographyLayer bathymetry,
        IPhysicalGeographyLayer landmasses,
        IPhysicalGeographyLayer currents,
        IPhysicalGeographyLayer warmth,
        IPhysicalGeographyLayer temperature,
        IPhysicalGeographyLayer atmosphericFlow,
        IPhysicalGeographyLayer moisture,
        IPhysicalGeographyLayer precipitation,
        IPhysicalGeographyLayer runoff,
        IPhysicalGeographyLayer lakes,
        IPhysicalGeographyLayer flow,
        IPhysicalGeographyLayer rivers)
    {
        layers.Add(ocean);
        layers.Add(oceanDistance);
        layers.Add(bathymetry);
        layers.Add(landmasses);
        layers.Add(currents);
        layers.Add(warmth);
        layers.Add(temperature);
        layers.Add(atmosphericFlow);
        layers.Add(moisture);
        layers.Add(precipitation);
        layers.Add(runoff);
        layers.Add(lakes);
        layers.Add(flow);
        layers.Add(rivers);
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
        int rootSeed,
        PhysicalGeographyEpochDefinition? epoch = null,
        int epochIndex = -1,
        int epochCount = 0)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(map);
        var missing = layer.Contract.Requires & ~map.AvailableFacts;
        if (missing != PhysicalGeographyFacts.None)
        {
            throw new InvalidOperationException(
                $"Physical generation layer '{layer.Contract.StableName}' is missing {missing}.");
        }

        var context = new PhysicalGeographyGenerationContext(
            rootSeed,
            _profile,
            layer.Contract,
            epoch,
            epochIndex,
            epochCount);
        layer.Apply(map, context);
        layer.Validate(map, context);
        map.AvailableFacts &= ~layer.Contract.Invalidates;
        map.AvailableFacts |= layer.Contract.Produces;
        return map;
    }
}
