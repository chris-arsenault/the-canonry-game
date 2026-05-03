namespace TheCanonryGame.Game.Tests;

using Engine.Domain.Compilation;
using Engine.Kernel.Driver;
using Engine.Projection;

using IceRemembers;

using TheCanonryGame.Game.Cards;
using TheCanonryGame.Game.Tableau;

public sealed class GameDomainKnowledgeTests
{
    [Fact]
    public void FromCompiledDomain_ExposesGeneratorCardOutputsAndRelationships()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var generatorId = compiled.Ids.GeneratorIdOf("war_makes_wardens").Value;

        var knowledge = GameDomainKnowledge.FromCompiledDomain(compiled, new ProjectionSymbolCatalog());

        var generator = knowledge.GeneratorsById[generatorId];
        Assert.Equal(GameCapabilityKind.Generator, generator.Kind);
        Assert.Equal("war_makes_wardens", generator.StableName);
        Assert.Equal("War Makes Wardens", generator.DisplayName);
        Assert.Equal("penguin", generator.PrimaryKindStableName);
        Assert.Collection(
            generator.Produces,
            produced =>
            {
                Assert.Equal("$warden", produced.Ref);
                Assert.Equal("penguin", produced.KindStableName);
                Assert.Equal("warden", produced.SubtypeStableName);
                Assert.Equal("Warden", produced.DisplayLabel);
                Assert.False(produced.IsOptional);
            },
            produced =>
            {
                Assert.Equal("$holdfast", produced.Ref);
                Assert.Equal("settlement", produced.KindStableName);
                Assert.Equal("holdfast", produced.SubtypeStableName);
                Assert.Equal("Holdfast", produced.DisplayLabel);
                Assert.False(produced.IsOptional);
            },
            produced =>
            {
                Assert.Equal("$weapon", produced.Ref);
                Assert.Equal("artifact", produced.KindStableName);
                Assert.Equal("weapon", produced.SubtypeStableName);
                Assert.Equal("Weapon", produced.DisplayLabel);
                Assert.True(produced.IsOptional);
            });
        Assert.Collection(
            generator.RelationshipChanges,
            relationship => Assert.Equal("member_of", relationship.RelationshipStableName),
            relationship => Assert.Equal("home_base", relationship.RelationshipStableName),
            relationship => Assert.Equal("controlled_by", relationship.RelationshipStableName),
            relationship => Assert.Equal("native_to", relationship.RelationshipStableName));
    }

    [Fact]
    public void FromCompiledDomain_ExposesGambitEffectsAndDecisionModifiers()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var gambitId = compiled.Ids.GambitIdOf("artifact_cleanses_scar").Value;

        var knowledge = GameDomainKnowledge.FromCompiledDomain(compiled, new ProjectionSymbolCatalog());

        var gambit = knowledge.GambitsById[gambitId];
        Assert.Equal(GameCapabilityKind.Gambit, gambit.Kind);
        Assert.Equal("artifact_cleanses_scar", gambit.StableName);
        Assert.Equal("Cleanse a Scar", gambit.DisplayName);
        Assert.Equal("artifact", gambit.PrimaryKindStableName);
        Assert.Empty(gambit.Produces);

        var relationship = Assert.Single(gambit.RelationshipChanges);
        Assert.Equal(CardRelationshipChangeKind.Create, relationship.ChangeKind);
        Assert.Equal("$self", relationship.SourceRef);
        Assert.Equal("$target", relationship.TargetRef);
        Assert.Equal("cleansed_by", relationship.RelationshipStableName);
        Assert.Equal(0.78f, relationship.Strength);

        var terrain = Assert.Single(gambit.TerrainTransforms);
        Assert.Equal("hot_springs", terrain.TerrainStableName);
        Assert.Equal("Hot Springs", terrain.TerrainDisplayName);

        Assert.Equal(2, gambit.DecisionModifiers.Count);
        var modifier = Assert.Single(gambit.DecisionModifiers, modifier =>
            modifier.TraitStableName == "artifact_restorative_aura");
        Assert.Equal("artifact_restorative_aura", modifier.TraitStableName);
        Assert.Equal(0.10f, modifier.ScoreDelta);
        Assert.Equal(0.10f, modifier.SuccessChanceDelta);
    }

    [Fact]
    public void TableauCardsReceiveCompiledDomainKnowledge()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var generatorId = compiled.Ids.GeneratorIdOf("war_makes_wardens").Value;
        var gambitId = compiled.Ids.GambitIdOf("artifact_cleanses_scar").Value;
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Generator, generatorId, "war_makes_wardens", "War Makes Wardens")
            .RegisterSymbol(ProjectionSymbolKind.Gambit, gambitId, "artifact_cleanses_scar", "Cleanse a Scar");
        var knowledge = GameDomainKnowledge.FromCompiledDomain(compiled, symbols);
        var projection = new UiProjectionState();

        var tableau = new GameRuntimeTableau(
            projection,
            symbols,
            new GameIntentPort(new NoopCommandSubmitter(), () => projection.WorldVersion),
            knowledge);

        var generatorCard = tableau.Cards.Single(card => card.Definition.GeneratorId == generatorId);
        Assert.Equal("penguin", generatorCard.Definition.Tags.Single(tag => tag == "penguin"));
        Assert.Equal(3, generatorCard.Definition.Produces.Count);
        Assert.Equal(4, generatorCard.Definition.RelationshipChanges.Count);
        Assert.Contains(generatorCard.Definition.Produces, produced =>
            produced.SubtypeStableName == "weapon" &&
            produced.IsOptional);

        var gambitCard = tableau.Cards.Single(card => card.Definition.GambitId == gambitId);
        Assert.Contains("artifact", gambitCard.Definition.Tags);
        Assert.Empty(gambitCard.Definition.Produces);
        Assert.Contains(gambitCard.Definition.RelationshipChanges, relationship =>
            relationship.RelationshipStableName == "cleansed_by");
        Assert.Contains(gambitCard.Definition.TerrainTransforms, transform =>
            transform.TerrainStableName == "hot_springs");
        Assert.Contains(gambitCard.Definition.DecisionModifiers, modifier =>
            modifier.TraitStableName == "artifact_restorative_aura");
    }

    private sealed class NoopCommandSubmitter : ICommandSubmitter
    {
        public bool TrySetGeneratorEnabled(in SetGeneratorEnabledCommand command, out CommandHandle handle)
        {
            _ = command;
            handle = default;
            return true;
        }
    }
}
