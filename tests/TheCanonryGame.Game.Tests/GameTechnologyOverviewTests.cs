namespace TheCanonryGame.Game.Tests;

using Engine.Constants;
using Engine.Gameplay;
using Engine.Host;
using Engine.Projection;

using TheCanonryGame.Game.Runtime;

public sealed class GameTechnologyOverviewTests
{
    [Fact]
    public void Build_PreservesKernelTotalAndResolvesCultureAndTargetLabels()
    {
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Culture, 1, "test_culture", "Test Culture");
        var snapshot = new KnowledgeOverviewSnapshot(
            Revision: 7,
            Cultures:
            [
                new KnowledgeCultureSnapshot(
                    CultureId: 1,
                    ClaimedTechs: 0,
                    MaxTier: -1,
                    Claims: [],
                    Modifiers:
                    [
                        new KnowledgeModifierSnapshot(
                            KnowledgeGrantDomain.CarryingCapacity,
                            TargetId: PlaneCodes.Physical,
                            TargetStableName: null,
                            AuthoredTargetCode: PlaneCodes.Physical,
                            Multiplier: 1.46f),
                    ]),
            ]);

        var overview = GameTechnologyOverviewReadModel.Build(snapshot, symbols);

        var culture = Assert.Single(overview.Cultures);
        var bonus = Assert.Single(culture.Bonuses);
        Assert.Equal(7, overview.Version);
        Assert.Equal("test_culture", culture.StableName);
        Assert.Equal("Test Culture", culture.DisplayName);
        Assert.Equal("Physical carrying capacity", bonus.Label);
        Assert.Equal(1.46f, bonus.Multiplier);
    }
}
