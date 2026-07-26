namespace TheCanonryGame.Game.Tests;

using Engine.Projection;

using TheCanonryGame.Game.Cards;

// Slow: starts the full game runtime; excluded from the fast loop.
[Trait("Category", "Slow")]
public sealed class CanonryGameRuntimeTests
{
    [Fact]
    public void Start_WithRequestedSeedWiresReadModelsSeedRunLogAndTableauFromRuntimeSymbols()
    {
        var runLogPath = TempRunLogPath();

        try
        {
            using var runtime = CanonryGameRuntimeFactory.Start(GameSeedPolicy.Requested(42), runLogPath);
            runtime.Controls.Pause();

            const int interventionCardCount = 2;
            var expectedCardCount = interventionCardCount + runtime.ReadModels.Symbols.Symbols.Values.Count(symbol =>
                symbol.Kind is ProjectionSymbolKind.Generator or ProjectionSymbolKind.Gambit);
            var generatorCard = runtime.Tableau.Cards.First(card => card.Definition.GeneratorId > 0);

            Assert.Equal(42, runtime.Seed.BaseSeed);
            Assert.True(runtime.Seed.Attempt >= 0);
            Assert.Equal(runLogPath, runtime.RunLogPath);
            Assert.NotNull(runtime.ReadModels.Map);
            Assert.NotNull(runtime.ReadModels.Entities);
            Assert.NotNull(runtime.ReadModels.Chronicle);
            Assert.NotNull(runtime.ReadModels.Symbols);
            Assert.Equal(expectedCardCount, runtime.Tableau.Cards.Count);
            Assert.All(runtime.Tableau.Cards, card =>
                Assert.True(
                    card.Definition.Type is CardType.Generator or CardType.Action or CardType.Intervention,
                    $"Unexpected tableau card type {card.Definition.Type} for {card.Definition.Id}."));
            Assert.True(runtime.Intents.RequestGeneratorEnabled(generatorCard.Definition.GeneratorId, enabled: false));
        }
        finally
        {
            TryDelete(runLogPath);
        }
    }

    [Fact]
    public void Update_PumpsRuntimeUntilDisposedAndThenRejectsFurtherUpdates()
    {
        var runLogPath = TempRunLogPath();
        var runtime = CanonryGameRuntimeFactory.Start(GameSeedPolicy.Requested(43), runLogPath);

        try
        {
            runtime.Controls.Pause();

            var advance = runtime.Update();

            Assert.True(advance.CurrentTick >= advance.PreviousTick);
            Assert.Equal(Math.Max(0, advance.CurrentTick - advance.PreviousTick), advance.TicksProcessed);

            runtime.Dispose();
            runtime.Dispose();

            Assert.Throws<ObjectDisposedException>(() => runtime.Update());
        }
        finally
        {
            runtime.Dispose();
            TryDelete(runLogPath);
        }
    }

    [Fact]
    public void Start_WithoutRunLogPathDisablesInteractiveRunLogging()
    {
        using var runtime = CanonryGameRuntimeFactory.Start(GameSeedPolicy.Requested(44));
        runtime.Controls.Pause();

        Assert.Null(runtime.RunLogPath);
    }

    private static string TempRunLogPath() =>
        Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.log");

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
