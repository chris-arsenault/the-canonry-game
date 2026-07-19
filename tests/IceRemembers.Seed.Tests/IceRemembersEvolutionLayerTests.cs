namespace IceRemembers.Seed.Tests;

using Engine.WorldGen;

using IceRemembers;

public sealed class IceRemembersEvolutionLayerTests
{
    [Theory]
    [InlineData(42)]
    [InlineData(9173)]
    [InlineData(21031)]
    public void ProductionEpochSchedule_SatisfiesTargetsAcrossFixedQualityCorpus(int seed)
    {
        var result = IceRemembersWorldGenerationOrchestrator.Generate(
            32,
            seed,
            IceRemembersPhysicalGeography.GenerationProfile,
            IceRemembersPhysicalGeography.MicrogeographyProfile,
            IceRemembersPhysicalGeography.WorldGenerationEpochs);

        Assert.Equal(IceRemembersPhysicalGeography.WorldGenerationEpochs.Count, result.Physical.CompletedEpochCount);
        Assert.True(result.Magic.AvailableFacts.HasFlag(IceRemembersMicrogeographyFacts.Validated));
    }

    [Fact]
    public void Orchestrator_UsesConfiguredEpochCountAndKeepsCumulativeMagicHistory()
    {
        var epochs = new[]
        {
            Epoch("first", 420, 180, 360),
            Epoch("second", 1000, 850, 1000),
        };
        var result = IceRemembersWorldGenerationOrchestrator.Generate(
            6,
            IceRemembersMicrogeographyLayerTestHarness.Seed,
            IceRemembersMicrogeographyLayerTestHarness.PhysicalProfile,
            IceRemembersMicrogeographyLayerTestHarness.Profile,
            epochs);

        Assert.Equal(epochs.Length, result.Physical.CompletedEpochCount);
        Assert.Equal(epochs.Length, result.Magic.CompletedEpochCount);
        Assert.True(result.Magic.AvailableFacts.HasFlag(IceRemembersMicrogeographyFacts.Validated));
        AssertHistoryIsCumulative(result.Magic.FireHistory, epochs.Length);
        AssertHistoryIsCumulative(result.Magic.AuroraHistory, epochs.Length);
        AssertHistoryIsCumulative(result.Magic.PressureHistory, epochs.Length);
        AssertFirstEpochsMatchHistory(
            result.Magic.FireHistory,
            result.Magic.FireFirstEpoch,
            epochs.Length);
        AssertFirstEpochsMatchHistory(
            result.Magic.AuroraHistory,
            result.Magic.AuroraFirstEpoch,
            epochs.Length);
        AssertFirstEpochsMatchHistory(
            result.Magic.PressureHistory,
            result.Magic.PressureFirstEpoch,
            epochs.Length);
        Assert.NotSame(result.Magic.FireHistory, result.Magic.FireIntensity);
        Assert.Contains(result.Magic.PressureCoastalHistory, value => value > 0);
        AssertFirstEpochsMatchHistory(
            result.Magic.PressureCoastalHistory,
            result.Magic.PressureCoastalFirstEpoch,
            epochs.Length);
    }

    [Fact]
    public void MagicHistoryAndPressureCoastFeedback_RejectMalformedState()
    {
        var result = IceRemembersMicrogeographyLayerTestHarness.GenerateThrough<PressureFieldLayer>();
        var epoch = Epoch("history", 1000, 1000, 1000);
        var history = new MagicHistoryLayer();
        result.Pipeline.ApplyLayer(history, result.Map, 77123, epoch, 0, 1);
        Assert.Equal(1, result.Map.CompletedEpochCount);

        var historicFire = Array.FindIndex(result.Map.FireHistory, static value => value > 0);
        Assert.True(historicFire >= 0);
        result.Map.FireFirstEpoch[historicFire] = -1;
        var historyContext = new IceRemembersMicrogeographyGenerationContext(
            77123,
            IceRemembersMicrogeographyLayerTestHarness.Profile,
            history.Contract,
            epoch,
            0,
            1);
        Assert.Throws<InvalidOperationException>(() => history.Validate(result.Map, historyContext));
        result.Map.FireFirstEpoch[historicFire] = 0;

        var feedback = new PressureCoastFeedbackLayer();
        result.Pipeline.ApplyLayer(feedback, result.Map, 77123, epoch, 0, 1);
        var ocean = Enumerable.Range(0, result.Map.Physical.Grid.TriangleCount)
            .First(index => result.Map.Physical.WaterKindCodes[index] ==
                Engine.Constants.PhysicalWaterKindCodes.Ocean);
        result.Map.Physical.ExternalCoastalForcing[ocean] = 1;
        var feedbackContext = new IceRemembersMicrogeographyGenerationContext(
            77123,
            IceRemembersMicrogeographyLayerTestHarness.Profile,
            feedback.Contract,
            epoch,
            0,
            1);
        Assert.Throws<InvalidOperationException>(() => feedback.Validate(result.Map, feedbackContext));
    }

    [Fact]
    public void EpochSchedule_RejectsDuplicateNamesAndOutOfRangeMagicStrength()
    {
        var duplicate = Epoch("same", 1000, 1000, 1000);
        Assert.Throws<InvalidOperationException>(() =>
            IceRemembersWorldGenerationOrchestrator.Generate(
                4,
                1,
                IceRemembersMicrogeographyLayerTestHarness.PhysicalProfile,
                IceRemembersMicrogeographyLayerTestHarness.Profile,
                [duplicate, duplicate]));
        Assert.Throws<InvalidOperationException>(() =>
            (duplicate with { FireStrength = 1001 }).Validate());
    }

    private static IceRemembersWorldGenerationEpoch Epoch(
        string name,
        int fire,
        int aurora,
        int pressure) => new()
        {
            Physical = new PhysicalGeographyEpochDefinition
            {
                StableName = name,
                TectonicStrength = 600,
                HydrologyStrength = 700,
                GlaciationStrength = 600,
                CoastalStrength = 700,
            },
            FireStrength = fire,
            AuroraStrength = aurora,
            PressureStrength = pressure,
        };

    private static void AssertHistoryIsCumulative(int[] history, int epochCount)
    {
        Assert.Contains(history, value => value > 0);
        Assert.All(history, value => Assert.InRange(value, 0, epochCount * 1000));
    }

    private static void AssertFirstEpochsMatchHistory(
        ReadOnlySpan<int> history,
        ReadOnlySpan<int> firstEpochs,
        int epochCount)
    {
        var found = false;
        for (var index = 0; index < history.Length; index++)
        {
            Assert.Equal(history[index] == 0, firstEpochs[index] == -1);
            Assert.InRange(firstEpochs[index], -1, epochCount - 1);
            found |= firstEpochs[index] >= 0;
        }

        Assert.True(found);
    }
}
