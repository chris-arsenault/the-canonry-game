namespace TheCanonryGame.Game.Tests;

using Engine.Eventing;
using Engine.Kernel.Driver;
using Engine.Projection;

using TheCanonryGame.Game.Cards;
using TheCanonryGame.Game.Tableau;

public sealed class GameRuntimeTableauTests
{
    [Fact]
    public void Constructor_BuildsSortedCardsFromProjectionSymbolsAndCopiesMetadata()
    {
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, 20, "routes_draw_readers", "Routes Draw Readers", "Draws readers.")
            .RegisterSymbol(ProjectionSymbolKind.Generator, 10, "ice_settles_accounts", "Ice Settles Accounts", "Settles artifacts.")
            .RegisterSymbol(ProjectionSymbolKind.Gambit, 30, "penguin_wields_artifact", "Penguin Wields Artifact", "A penguin uses an artifact.")
            .RegisterSymbol(ProjectionSymbolKind.Kind, 40, "settlement", "Settlement");

        var tableau = CreateTableau(new UiProjectionState(), symbols);

        Assert.Collection(
            tableau.Cards,
            card =>
            {
                Assert.Equal("generator:ice_settles_accounts", card.Definition.Id);
                Assert.Equal(CardType.Generator, card.Definition.Type);
                Assert.Equal(10, card.Definition.GeneratorId);
                Assert.Equal("Settles artifacts.", card.Definition.Description);
                Assert.Contains("generator", card.Definition.Tags);
                Assert.Contains("artifact", card.Definition.Tags);
                Assert.Contains("kind:artifact", card.Definition.Tags);
            },
            card =>
            {
                Assert.Equal("generator:routes_draw_readers", card.Definition.Id);
                Assert.Equal(CardType.Generator, card.Definition.Type);
                Assert.Equal(20, card.Definition.GeneratorId);
                Assert.Contains("settlement", card.Definition.Tags);
                Assert.Contains("kind:settlement", card.Definition.Tags);
            },
            card =>
            {
                Assert.Equal("gambit:penguin_wields_artifact", card.Definition.Id);
                Assert.Equal(CardType.Action, card.Definition.Type);
                Assert.Equal(30, card.Definition.GambitId);
                Assert.Equal("A penguin uses an artifact.", card.Definition.Description);
                Assert.Contains("gambit", card.Definition.Tags);
                Assert.Contains("penguin", card.Definition.Tags);
                Assert.Contains("kind:penguin", card.Definition.Tags);
            });
        Assert.Equal(3, tableau.EnabledCardCount);
    }

    [Theory]
    [InlineData("ice_settles_accounts", "artifact")]
    [InlineData("what_the_spring_remembers", "region")]
    [InlineData("war_breaks_the_land", "region")]
    [InlineData("routes_draw_readers", "settlement")]
    [InlineData("what_the_artifact_knows", "settlement")]
    [InlineData("unknown_generator", null)]
    public void Constructor_AddsGeneratorKindHintTagsFromStableNames(string stableName, string? expectedKindHint)
    {
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, 1, stableName, "Generator");

        var tableau = CreateTableau(new UiProjectionState(), symbols);

        AssertKindHintTags(CardForGenerator(tableau, 1), "generator", expectedKindHint);
    }

    [Theory]
    [InlineData("penguin_wields_artifact", "penguin")]
    [InlineData("faction_declares_war", "faction")]
    [InlineData("settlement_sends_scouts", "settlement")]
    [InlineData("artifact_wakes", "artifact")]
    [InlineData("region_remembers", "region")]
    [InlineData("oracle_speaks", null)]
    [InlineData("_headless", null)]
    public void Constructor_AddsGambitKindHintTagsFromActorStableNamePrefix(string stableName, string? expectedKindHint)
    {
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Gambit, 1, stableName, "Gambit");

        var tableau = CreateTableau(new UiProjectionState(), symbols);

        AssertKindHintTags(CardForGambit(tableau, 1), "gambit", expectedKindHint);
    }

    [Fact]
    public void Constructor_UsesEmptyDescriptionWhenSymbolDescriptionIsMissing()
    {
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, 1, "unknown_generator", "Unknown Generator")
            .RegisterSymbol(ProjectionSymbolKind.Gambit, 2, "oracle_speaks", "Oracle Speaks");

        var tableau = CreateTableau(new UiProjectionState(), symbols);

        Assert.Equal(string.Empty, CardForGenerator(tableau, 1).Definition.Description);
        Assert.Equal(string.Empty, CardForGambit(tableau, 2).Definition.Description);
    }

    [Fact]
    public void GetCardState_ReflectsProjectedGeneratorAndGambitEvents()
    {
        var projection = new UiProjectionState();
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, 11, "routes_draw_readers", "Routes Draw Readers")
            .RegisterSymbol(ProjectionSymbolKind.Gambit, 21, "faction_declares_war", "Faction Declares War");
        var tableau = CreateTableau(projection, symbols);

        Project(projection, 1, KernelEvent.From(new GeneratorEnabledChangedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            GeneratorId: 11,
            Enabled: 1)));
        Project(projection, 2, KernelEvent.From(new GeneratorFiredKernelEvent(
            Tick: 5,
            WorldVersion: 5,
            GeneratorId: 11,
            TargetEntityId: 101,
            CreatedEntityCount: 2,
            CreatedRelationshipCount: 1)));
        Project(projection, 3, KernelEvent.From(new GeneratorEnabledChangedKernelEvent(
            Tick: 6,
            WorldVersion: 6,
            GeneratorId: 11,
            Enabled: 0)));
        Project(projection, 4, KernelEvent.From(new GambitExecutedKernelEvent(
            Tick: 8,
            WorldVersion: 8,
            ActorId: 101,
            ActorKindId: 4,
            GambitId: 21,
            AffectedEntityId0: 0,
            AffectedEntityId1: 0,
            AffectedEntityCount: 0,
            State: KernelEventState.Failed)));
        Project(projection, 5, KernelEvent.From(new GambitExecutedKernelEvent(
            Tick: 9,
            WorldVersion: 9,
            ActorId: 101,
            ActorKindId: 4,
            GambitId: 21,
            AffectedEntityId0: 102,
            AffectedEntityId1: 0,
            AffectedEntityCount: 1,
            State: KernelEventState.Succeeded)));

        var generatorState = tableau.GetCardState(CardForGenerator(tableau, 11));
        Assert.True(generatorState.IsKnown);
        Assert.False(generatorState.IsEnabled);
        Assert.Equal(1, generatorState.TotalFired);
        Assert.Equal(5, generatorState.LastFireTick);

        var gambitState = tableau.GetCardState(CardForGambit(tableau, 21));
        Assert.True(gambitState.IsKnown);
        Assert.True(gambitState.IsEnabled);
        Assert.Equal(1, gambitState.TotalFired);
        Assert.Equal(9, gambitState.LastFireTick);
        Assert.Equal(1, tableau.EnabledCardCount);
    }

    [Fact]
    public void GetCardState_TreatsUnseenGeneratorCardsAsKnownToTableauButEnabledByDefault()
    {
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, 11, "routes_draw_readers", "Routes Draw Readers");
        var tableau = CreateTableau(new UiProjectionState(), symbols);

        var state = tableau.GetCardState(CardForGenerator(tableau, 11));

        Assert.False(state.IsKnown);
        Assert.True(state.IsEnabled);
        Assert.Equal(11, state.GeneratorId);
        Assert.Equal(0, state.TotalFired);
        Assert.Null(state.LastFireTick);
        Assert.Equal(1, tableau.EnabledCardCount);
    }

    [Fact]
    public void GetCardState_TreatsUnseenGambitCardsAsEnabledActionsWithoutSuccessHistory()
    {
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Gambit, 21, "faction_declares_war", "Faction Declares War");
        var tableau = CreateTableau(new UiProjectionState(), symbols);

        var state = tableau.GetCardState(CardForGambit(tableau, 21));

        Assert.False(state.IsKnown);
        Assert.True(state.IsEnabled);
        Assert.Equal(0, state.GeneratorId);
        Assert.Equal(0, state.TotalFired);
        Assert.Null(state.LastFireTick);
        Assert.Equal(1, tableau.EnabledCardCount);
    }

    [Fact]
    public void GetCardState_TreatsSystemCardsAsInactiveAndNotToggleable()
    {
        var commands = new RecordingCommandSubmitter();
        var tableau = CreateTableau(new UiProjectionState(), new ProjectionSymbolCatalog(), commands);
        var card = new CardInstance(new CardDefinition("system:ledger", "Ledger", CardType.System), placedAtTick: 10);

        var state = tableau.GetCardState(card);
        var requested = tableau.RequestSetEnabled(card, enabled: true);

        Assert.False(state.IsKnown);
        Assert.False(state.IsEnabled);
        Assert.Equal(0, state.GeneratorId);
        Assert.Equal(0, state.TotalFired);
        Assert.Null(state.LastFireTick);
        Assert.False(requested);
        Assert.Empty(commands.Commands);
    }

    [Fact]
    public void RequestToggle_SubmitsInverseGeneratorStateAndRefusesGambitCards()
    {
        var projection = new UiProjectionState();
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, 31, "war_breaks_the_land", "War Breaks The Land")
            .RegisterSymbol(ProjectionSymbolKind.Gambit, 51, "settlement_sends_scouts", "Settlement Sends Scouts");
        var commands = new RecordingCommandSubmitter();
        var tableau = CreateTableau(projection, symbols, commands);

        Project(projection, 1, KernelEvent.From(new GeneratorEnabledChangedKernelEvent(
            Tick: 12,
            WorldVersion: 123,
            GeneratorId: 31,
            Enabled: 1)));

        var generatorCard = CardForGenerator(tableau, 31);
        var gambitCard = CardForGambit(tableau, 51);

        Assert.True(tableau.RequestToggle(generatorCard));
        Assert.False(tableau.RequestToggle(gambitCard));
        Assert.Collection(
            commands.Commands,
            command =>
            {
                Assert.Equal(123, command.IssueVersion);
                Assert.Equal(31, command.GeneratorId);
                Assert.False(command.Enabled);
                Assert.Equal(1, command.SubmitterSequence);
            });
    }

    [Fact]
    public void RequestToggle_EnablesKnownDisabledGenerator()
    {
        var projection = new UiProjectionState();
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, 31, "war_breaks_the_land", "War Breaks The Land");
        var commands = new RecordingCommandSubmitter();
        var tableau = CreateTableau(projection, symbols, commands);

        Project(projection, 1, KernelEvent.From(new GeneratorEnabledChangedKernelEvent(
            Tick: 12,
            WorldVersion: 123,
            GeneratorId: 31,
            Enabled: 0)));

        Assert.True(tableau.RequestToggle(CardForGenerator(tableau, 31)));
        Assert.Collection(
            commands.Commands,
            command =>
            {
                Assert.Equal(31, command.GeneratorId);
                Assert.True(command.Enabled);
            });
    }

    [Fact]
    public void RequestToggle_DisablesUnseenGeneratorBecauseTableauDefaultsItToEnabled()
    {
        var projection = new UiProjectionState();
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, 31, "war_breaks_the_land", "War Breaks The Land");
        var commands = new RecordingCommandSubmitter();
        var tableau = CreateTableau(projection, symbols, commands);

        Assert.True(tableau.RequestToggle(CardForGenerator(tableau, 31)));
        Assert.Collection(
            commands.Commands,
            command =>
            {
                Assert.Equal(31, command.GeneratorId);
                Assert.False(command.Enabled);
            });
    }

    [Fact]
    public void RequestSetEnabled_ReturnsSubmitFailureAfterSubmittingGeneratorCommand()
    {
        var projection = new UiProjectionState();
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, 31, "war_breaks_the_land", "War Breaks The Land");
        var commands = new RecordingCommandSubmitter { NextResult = false };
        var tableau = CreateTableau(projection, symbols, commands);

        Project(projection, 1, KernelEvent.From(new GeneratorEnabledChangedKernelEvent(
            Tick: 12,
            WorldVersion: 123,
            GeneratorId: 31,
            Enabled: 0)));

        var result = tableau.RequestSetEnabled(CardForGenerator(tableau, 31), enabled: true);

        Assert.False(result);
        Assert.Collection(
            commands.Commands,
            command =>
            {
                Assert.Equal(123, command.IssueVersion);
                Assert.Equal(31, command.GeneratorId);
                Assert.True(command.Enabled);
            });
    }

    [Fact]
    public void RecentGeneratorFires_ReturnsLastCountWithResolvedNamesInChronologicalOrder()
    {
        var projection = new UiProjectionState();
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, 1, "alpha_generator", "Alpha Generator")
            .RegisterSymbol(ProjectionSymbolKind.Generator, 2, "beta_generator", "Beta Generator");
        var tableau = CreateTableau(projection, symbols);

        ProjectGeneratorFire(projection, sequence: 1, tick: 10, generatorId: 1);
        ProjectGeneratorFire(projection, sequence: 2, tick: 11, generatorId: 2);
        ProjectGeneratorFire(projection, sequence: 3, tick: 12, generatorId: 3);

        var recent = tableau.RecentGeneratorFires(2);

        Assert.Equal(3, tableau.TotalGeneratorFires);
        Assert.Collection(
            recent,
            fire =>
            {
                Assert.Equal("Beta Generator", fire.GeneratorName);
                Assert.Equal(11, fire.Tick);
            },
            fire =>
            {
                Assert.Equal("Generator 3", fire.GeneratorName);
                Assert.Equal(12, fire.Tick);
            });
    }

    [Fact]
    public void RecentGeneratorFires_ReturnsAllAvailableFiresWhenCountExceedsHistory()
    {
        var projection = new UiProjectionState();
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, 1, "alpha_generator", "Alpha Generator");
        var tableau = CreateTableau(projection, symbols);

        ProjectGeneratorFire(projection, sequence: 1, tick: 10, generatorId: 1);

        var recent = tableau.RecentGeneratorFires(5);

        Assert.Collection(
            recent,
            fire =>
            {
                Assert.Equal("Alpha Generator", fire.GeneratorName);
                Assert.Equal(10, fire.Tick);
            });
    }

    [Fact]
    public void RecentGeneratorFires_ReturnsEmptyListWhenCountIsZero()
    {
        var projection = new UiProjectionState();
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, 1, "alpha_generator", "Alpha Generator");
        var tableau = CreateTableau(projection, symbols);

        ProjectGeneratorFire(projection, sequence: 1, tick: 10, generatorId: 1);

        Assert.Empty(tableau.RecentGeneratorFires(0));
    }

    [Fact]
    public void RecentGeneratorFires_RejectsNegativeCount()
    {
        var tableau = CreateTableau(new UiProjectionState(), new ProjectionSymbolCatalog());

        Assert.Throws<ArgumentOutOfRangeException>(() => tableau.RecentGeneratorFires(-1));
    }

    private static void AssertKindHintTags(CardInstance card, string requiredTypeTag, string? expectedKindHint)
    {
        Assert.Contains(requiredTypeTag, card.Definition.Tags);
        if (expectedKindHint is null)
        {
            Assert.Single(card.Definition.Tags);
            return;
        }

        Assert.Contains(expectedKindHint, card.Definition.Tags);
        Assert.Contains($"kind:{expectedKindHint}", card.Definition.Tags);
        Assert.Equal(3, card.Definition.Tags.Count);
    }

    private static GameRuntimeTableau CreateTableau(
        UiProjectionState projection,
        ProjectionSymbolCatalog symbols,
        RecordingCommandSubmitter? commands = null)
    {
        commands ??= new RecordingCommandSubmitter();
        var intents = new GameIntentPort(commands, () => projection.WorldVersion);
        return new GameRuntimeTableau(projection, symbols, intents);
    }

    private static CardInstance CardForGenerator(GameRuntimeTableau tableau, int generatorId) =>
        tableau.Cards.Single(card => card.Definition.GeneratorId == generatorId);

    private static CardInstance CardForGambit(GameRuntimeTableau tableau, int gambitId) =>
        tableau.Cards.Single(card => card.Definition.GambitId == gambitId);

    private static void ProjectGeneratorFire(
        UiProjectionState projection,
        long sequence,
        int tick,
        int generatorId)
    {
        Project(projection, sequence, KernelEvent.From(new GeneratorFiredKernelEvent(
            Tick: tick,
            WorldVersion: tick,
            GeneratorId: generatorId,
            TargetEntityId: 100 + generatorId,
            CreatedEntityCount: 1,
            CreatedRelationshipCount: 0)));
    }

    private static void Project(UiProjectionState projection, long sequence, KernelEvent kernelEvent) =>
        projection.Project(new SequencedKernelEvent(sequence, kernelEvent));

    private sealed class RecordingCommandSubmitter : ICommandSubmitter
    {
        public List<SetGeneratorEnabledCommand> Commands { get; } = [];

        public bool NextResult { get; init; } = true;

        public bool TrySetGeneratorEnabled(in SetGeneratorEnabledCommand command, out CommandHandle handle)
        {
            Commands.Add(command);
            handle = default;
            return NextResult;
        }
    }
}
