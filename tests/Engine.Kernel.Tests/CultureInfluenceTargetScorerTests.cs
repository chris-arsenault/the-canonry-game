namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class CultureInfluenceTargetScorerTests
{
    [Fact]
    public void Spread_StrongAttachedComponentDominatesMinorIslandEvenWithLessHeadroom()
    {
        var strongFrontier = ScoreSpread(
            ownShare: 20f,
            ownRaw: 0f,
            attachedMass: 20f,
            adjacentSupportedCount: 3,
            attachedComponentCount: 1);
        var islandFrontier = ScoreSpread(
            ownShare: 0f,
            ownRaw: 0f,
            attachedMass: 1f,
            adjacentSupportedCount: 1,
            attachedComponentCount: 1);

        Assert.True(strongFrontier > islandFrontier * 20f);
    }

    [Fact]
    public void Spread_BridgeBetweenComponentsBeatsEquivalentSingleComponentFrontier()
    {
        var bridge = ScoreSpread(
            ownShare: 0f,
            ownRaw: 0f,
            attachedMass: 12f,
            adjacentSupportedCount: 2,
            attachedComponentCount: 2);
        var ordinaryFrontier = ScoreSpread(
            ownShare: 0f,
            ownRaw: 0f,
            attachedMass: 12f,
            adjacentSupportedCount: 1,
            attachedComponentCount: 1);

        Assert.True(bridge > ordinaryFrontier * 1.5f);
    }

    [Fact]
    public void Spread_SaturatedInteriorLosesToFrontier()
    {
        var frontier = ScoreSpread(
            ownShare: 0f,
            ownRaw: 0f,
            attachedMass: 20f,
            adjacentSupportedCount: 3,
            attachedComponentCount: 1);
        var saturatedInterior = ScoreSpread(
            ownShare: 100f,
            ownRaw: 100f,
            entityInfluence: 100f,
            attachedMass: 20f,
            adjacentSupportedCount: 6,
            attachedComponentCount: 1);

        Assert.True(frontier > 0f);
        Assert.Equal(0f, saturatedInterior);
    }

    [Fact]
    public void Spread_ForeignDominatedCellsLoseToCleanSupportedFrontier()
    {
        var cleanFrontier = Score(
            LowAltitudeInfluenceTargetingCodes.Spread,
            ownShare: 0f,
            ownRaw: 0f,
            entityInfluence: 0f,
            foreignShare: 0f,
            foreignRaw: 0f,
            attachedMass: 20f,
            adjacentSupportedCount: 3,
            attachedComponentCount: 1);
        var foreignDominatedFrontier = Score(
            LowAltitudeInfluenceTargetingCodes.Spread,
            ownShare: 5f,
            ownRaw: 3f,
            entityInfluence: 0f,
            foreignShare: 90f,
            foreignRaw: 60f,
            attachedMass: 20f,
            adjacentSupportedCount: 3,
            attachedComponentCount: 1);

        Assert.True(cleanFrontier > foreignDominatedFrontier * 2f);
    }

    [Fact]
    public void Spread_AllowsEntityInfluenceGrowthInsideCultureSaturatedCell()
    {
        var saturatedForCulture = ScoreSpread(
            ownShare: 100f,
            ownRaw: 55f,
            entityInfluence: 35f,
            attachedMass: 2f,
            adjacentSupportedCount: 1,
            attachedComponentCount: 1);

        Assert.True(saturatedForCulture > 0f);
    }

    [Fact]
    public void Reinforce_UnderNexusSupportedCellScoresButNexusCellDoesNot()
    {
        var underNexus = Score(
            LowAltitudeInfluenceTargetingCodes.Reinforce,
            ownShare: 100f,
            ownRaw: 30f,
            entityInfluence: 30f,
            foreignShare: 0f,
            attachedMass: 20f,
            adjacentSupportedCount: 6,
            attachedComponentCount: 1);
        var atNexus = Score(
            LowAltitudeInfluenceTargetingCodes.Reinforce,
            ownShare: 100f,
            ownRaw: 1000f,
            entityInfluence: 90f,
            foreignShare: 0f,
            attachedMass: 20f,
            adjacentSupportedCount: 6,
            attachedComponentCount: 1);

        Assert.True(underNexus > 0f);
        Assert.Equal(0f, atNexus);
    }

    [Fact]
    public void Spread_AllowsWeakSelfBootstrapButKeepsItBelowComponentFrontier()
    {
        var bootstrap = ScoreSpread(
            ownShare: 33f,
            ownRaw: 10f,
            attachedMass: 0f,
            adjacentSupportedCount: 0,
            attachedComponentCount: 0);
        var frontier = ScoreSpread(
            ownShare: 0f,
            ownRaw: 0f,
            attachedMass: 20f,
            adjacentSupportedCount: 3,
            attachedComponentCount: 1);

        Assert.True(bootstrap > 0f);
        Assert.True(frontier > bootstrap * 100f);
    }

    [Fact]
    public void Contest_RequiresOwnComponentAttachment()
    {
        var unsupportedForeignPocket = Score(
            LowAltitudeInfluenceTargetingCodes.Contest,
            ownShare: 0f,
            ownRaw: 0f,
            entityInfluence: 0f,
            foreignShare: 100f,
            attachedMass: 0f,
            adjacentSupportedCount: 0,
            attachedComponentCount: 0);
        var border = Score(
            LowAltitudeInfluenceTargetingCodes.Contest,
            ownShare: 20f,
            ownRaw: 12f,
            entityInfluence: 0f,
            foreignShare: 100f,
            attachedMass: 8f,
            adjacentSupportedCount: 2,
            attachedComponentCount: 1);

        Assert.Equal(0f, unsupportedForeignPocket);
        Assert.True(border > 0f);
    }

    [Fact]
    public void Contest_StrongOwnMassBeatsWeakAttachmentWithHigherForeignShare()
    {
        var supportedMass = Score(
            LowAltitudeInfluenceTargetingCodes.Contest,
            ownShare: 75f,
            ownRaw: 30f,
            entityInfluence: 30f,
            foreignShare: 20f,
            attachedMass: 18f,
            adjacentSupportedCount: 4,
            attachedComponentCount: 1);
        var weakAttachment = Score(
            LowAltitudeInfluenceTargetingCodes.Contest,
            ownShare: 10f,
            ownRaw: 3f,
            entityInfluence: 0f,
            foreignShare: 100f,
            attachedMass: 1f,
            adjacentSupportedCount: 1,
            attachedComponentCount: 1);

        Assert.True(supportedMass > weakAttachment);
    }

    [Fact]
    public void Contest_MeaningfulForeignRawInsideDominantMassBeatsLowShareDust()
    {
        var meaningfulTrace = Score(
            LowAltitudeInfluenceTargetingCodes.Contest,
            ownShare: 85f,
            ownRaw: 100f,
            entityInfluence: 40f,
            foreignShare: 18f,
            foreignRaw: 20f,
            attachedMass: 24f,
            adjacentSupportedCount: 6,
            attachedComponentCount: 1);
        var lowShareDust = Score(
            LowAltitudeInfluenceTargetingCodes.Contest,
            ownShare: 88f,
            ownRaw: 100f,
            entityInfluence: 40f,
            foreignShare: 8f,
            foreignRaw: 2f,
            attachedMass: 24f,
            adjacentSupportedCount: 6,
            attachedComponentCount: 1);

        Assert.True(meaningfulTrace > lowShareDust * 4f);
    }

    [Fact]
    public void ComponentScratch_AttachmentUsesWholeConnectedComponents()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 77 });
        simulation.World.SetMapRadius(4);
        var entityId = simulation.CreateEntity(kindId: 1, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        using (simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0))
        {
            simulation.SetCellInfluence(entityId, PlaneCodes.Physical, 0, 0, 30f, tick: 0);
            simulation.SetCellInfluence(entityId, PlaneCodes.Physical, 1, 0, 30f, tick: 0);
            simulation.SetCellInfluence(entityId, PlaneCodes.Physical, 1, -1, 30f, tick: 0);
            simulation.SetCellInfluence(entityId, PlaneCodes.Physical, 0, -1, 30f, tick: 0);
        }

        var scratch = new CultureInfluenceComponentScratch();
        scratch.Build(simulation.World, cultureId: 1, PlaneCodes.Physical);

        var mainAttachment = scratch.AttachmentAt(0, 1);

        Assert.True(mainAttachment.ComponentMass >= 4f);
        Assert.True(mainAttachment.AdjacentSupportedCount >= 2);
        Assert.Equal(1, mainAttachment.ComponentCount);
    }

    private static float ScoreSpread(
        float ownShare,
        float ownRaw,
        float attachedMass,
        int adjacentSupportedCount,
        int attachedComponentCount) =>
        ScoreSpread(ownShare, ownRaw, 0f, attachedMass, adjacentSupportedCount, attachedComponentCount);

    private static float ScoreSpread(
        float ownShare,
        float ownRaw,
        float entityInfluence,
        float attachedMass,
        int adjacentSupportedCount,
        int attachedComponentCount) =>
        Score(
            LowAltitudeInfluenceTargetingCodes.Spread,
            ownShare,
            ownRaw,
            entityInfluence,
            foreignShare: 0f,
            foreignRaw: 0f,
            attachedMass,
            adjacentSupportedCount,
            attachedComponentCount);

    private static float Score(
        int targetingCode,
        float ownShare,
        float ownRaw,
        float entityInfluence,
        float foreignShare,
        float attachedMass,
        int adjacentSupportedCount,
        int attachedComponentCount) =>
        Score(
            targetingCode,
            ownShare,
            ownRaw,
            entityInfluence,
            foreignShare,
            foreignShare,
            attachedMass,
            adjacentSupportedCount,
            attachedComponentCount);

    private static float Score(
        int targetingCode,
        float ownShare,
        float ownRaw,
        float entityInfluence,
        float foreignShare,
        float foreignRaw,
        float attachedMass,
        int adjacentSupportedCount,
        int attachedComponentCount)
    {
        _ = entityInfluence;
        var input = new CultureInfluenceTargetScoreInput(
            targetingCode,
            ownShare,
            ownRaw,
            foreignShare,
            foreignRaw,
            attachedMass,
            adjacentSupportedCount,
            attachedComponentCount);
        return CultureInfluenceTargetScorer.Score(input);
    }
}
