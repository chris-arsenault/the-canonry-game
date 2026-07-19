namespace IceRemembers.Seed;

using Engine.WorldGen;

using IceRemembers;

internal sealed class IceRemembersMicrogeographyPipeline
{
    private readonly IceRemembersMicrogeographyGenerationProfile _profile;
    private readonly IReadOnlyList<IIceRemembersMicrogeographyLayer> _layers;

    internal IceRemembersMicrogeographyPipeline(
        IceRemembersMicrogeographyGenerationProfile profile,
        IReadOnlyList<IIceRemembersMicrogeographyLayer> layers)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _layers = layers ?? throw new ArgumentNullException(nameof(layers));
    }

    public static IceRemembersMicrogeographyPipeline CreateDefault(
        IceRemembersMicrogeographyGenerationProfile profile) =>
        new(
            profile,
            [
                new NaturalSurfaceLayer(),
                new FireFieldLayer(),
                new FireClimateFeedbackLayer(),
                new AuroraFieldLayer(),
                new PressureFieldLayer(),
                new FireFeatureLayer(),
                new AuroraFeatureLayer(),
                new PressureFeatureLayer(),
                new MagicInteractionLayer(),
                new FinalSurfaceLayer(),
                new IceRemembersMicrogeographyValidationLayer(),
            ]);

    internal IReadOnlyList<IIceRemembersMicrogeographyLayer> Layers => _layers;

    public IceRemembersMicrogeographyMap Generate(PhysicalGeographyField physical, int seed)
    {
        ArgumentNullException.ThrowIfNull(physical);
        _profile.Validate();
        var map = new IceRemembersMicrogeographyMap(physical);
        foreach (var layer in _layers)
        {
            map = ApplyLayer(layer, map, seed);
        }

        return map;
    }

    internal IceRemembersMicrogeographyMap ApplyLayer(
        IIceRemembersMicrogeographyLayer layer,
        IceRemembersMicrogeographyMap map,
        int rootSeed,
        IceRemembersWorldGenerationEpoch? epoch = null,
        int epochIndex = -1,
        int epochCount = 0)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(map);
        var missing = layer.Contract.Requires & ~map.AvailableFacts;
        if (missing != IceRemembersMicrogeographyFacts.None)
        {
            throw new InvalidOperationException(
                $"Ice Remembers microgeography layer '{layer.Contract.StableName}' is missing {missing}.");
        }

        var context = new IceRemembersMicrogeographyGenerationContext(
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
