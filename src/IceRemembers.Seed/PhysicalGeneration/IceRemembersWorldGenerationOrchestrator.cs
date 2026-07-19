namespace IceRemembers.Seed;

using Engine.WorldGen;

using IceRemembers;

internal static class IceRemembersWorldGenerationOrchestrator
{
    public static IceRemembersWorldGenerationResult Generate(
        int radius,
        int seed,
        PhysicalGeographyGenerationProfile physicalProfile,
        IceRemembersMicrogeographyGenerationProfile microgeographyProfile,
        IReadOnlyList<IceRemembersWorldGenerationEpoch> epochs)
    {
        ArgumentNullException.ThrowIfNull(physicalProfile);
        ArgumentNullException.ThrowIfNull(microgeographyProfile);
        ArgumentNullException.ThrowIfNull(epochs);
        ValidateEpochs(epochs);
        microgeographyProfile.Validate();

        var physical = PhysicalGeographyEvolutionSession.Begin(radius, seed, physicalProfile);
        var magic = new IceRemembersMicrogeographyMap(physical.Map);
        var magicPipeline = new IceRemembersMicrogeographyPipeline(microgeographyProfile, []);
        for (var epochIndex = 0; epochIndex < epochs.Count; epochIndex++)
        {
            ApplyEpoch(
                physical,
                magicPipeline,
                magic,
                seed,
                epochs[epochIndex],
                epochIndex,
                epochs.Count);
        }

        var field = physical.Complete();
        ApplyFinalState(
            magicPipeline,
            magic,
            seed,
            epochs[^1],
            epochs.Count - 1,
            epochs.Count);
        return new IceRemembersWorldGenerationResult(field, magic);
    }

    private static void ApplyEpoch(
        PhysicalGeographyEvolutionSession physical,
        IceRemembersMicrogeographyPipeline pipeline,
        IceRemembersMicrogeographyMap magic,
        int seed,
        IceRemembersWorldGenerationEpoch epoch,
        int epochIndex,
        int epochCount)
    {
        physical.BeginEpoch(epoch.Physical, epochCount);
        physical.ApplyGeology();
        physical.ApplyClimate();
        ApplyMagic(pipeline, new FireFieldLayer(), magic, seed, epoch, epochIndex, epochCount);
        ApplyMagic(
            pipeline,
            new FireClimateFeedbackLayer(),
            magic,
            seed,
            epoch,
            epochIndex,
            epochCount);
        physical.ApplyHydrology();
        physical.ApplyGlaciation();
        physical.ApplyPostGlacialReconciliation();
        ApplyMagic(pipeline, new AuroraFieldLayer(), magic, seed, epoch, epochIndex, epochCount);
        ApplyMagic(pipeline, new PressureFieldLayer(), magic, seed, epoch, epochIndex, epochCount);
        ApplyMagic(pipeline, new MagicHistoryLayer(), magic, seed, epoch, epochIndex, epochCount);
        ApplyMagic(
            pipeline,
            new PressureCoastFeedbackLayer(),
            magic,
            seed,
            epoch,
            epochIndex,
            epochCount);
        physical.MarkDomainForcingComplete();
        physical.ApplyCoastalEvolution();
        physical.CompleteEpoch();
    }

    private static void ApplyFinalState(
        IceRemembersMicrogeographyPipeline pipeline,
        IceRemembersMicrogeographyMap map,
        int seed,
        IceRemembersWorldGenerationEpoch epoch,
        int epochIndex,
        int epochCount)
    {
        IIceRemembersMicrogeographyLayer[] layers =
        [
            new FireFieldLayer(),
            new FireClimateFeedbackLayer(),
            new AuroraFieldLayer(),
            new PressureFieldLayer(),
            new NaturalSurfaceLayer(),
            new FireFeatureLayer(),
            new AuroraFeatureLayer(),
            new PressureFeatureLayer(),
            new MagicInteractionLayer(),
            new FinalSurfaceLayer(),
            new IceRemembersMicrogeographyValidationLayer(),
        ];
        foreach (var layer in layers)
        {
            ApplyMagic(pipeline, layer, map, seed, epoch, epochIndex, epochCount);
        }
    }

    private static void ApplyMagic(
        IceRemembersMicrogeographyPipeline pipeline,
        IIceRemembersMicrogeographyLayer layer,
        IceRemembersMicrogeographyMap map,
        int seed,
        IceRemembersWorldGenerationEpoch epoch,
        int epochIndex,
        int epochCount) =>
        pipeline.ApplyLayer(layer, map, seed, epoch, epochIndex, epochCount);

    private static void ValidateEpochs(IReadOnlyList<IceRemembersWorldGenerationEpoch> epochs)
    {
        if (epochs.Count == 0 || epochs.Count > int.MaxValue / 1000)
        {
            throw new InvalidOperationException("World generation requires a bounded epoch schedule.");
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var epoch in epochs)
        {
            epoch?.Validate();
            if (epoch is null || !names.Add(epoch.Physical.StableName))
            {
                throw new InvalidOperationException("World generation epoch names must be unique.");
            }
        }
    }
}

internal sealed record IceRemembersWorldGenerationResult(
    PhysicalGeographyField Physical,
    IceRemembersMicrogeographyMap Magic);
