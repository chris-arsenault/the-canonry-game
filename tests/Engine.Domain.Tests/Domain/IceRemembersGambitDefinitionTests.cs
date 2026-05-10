namespace Engine.Domain.Tests.Domain;

using Engine.Domain.Compilation;
using Engine.Domain.Compilation.KernelPrograms;

using IceRemembers;

public sealed class IceRemembersGambitDefinitionTests
{
    [Fact]
    public void IceRemembers_DefinesConcreteGambits()
    {
        var config = IceRemembersDefinition.Create();
        var gambitIds = config.Gambits.Select(gambit => gambit.Id).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(5, gambitIds.Count);
        Assert.Contains(IceRemembersGambitId.ArtifactCleansesScar.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.FactionDeclaresTerritorialWar.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.FactionSuesForPeace.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.PenguinWieldsArtifact.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.SettlementOpensTradeRoute.ToGambitId(), gambitIds);
    }

    [Fact]
    public void IceRemembers_GambitsCompileIntoEnabledKernelProgram()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());

        Assert.True(compiled.Programs.Gambits.EnabledByDefault);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.PenguinWieldsArtifact.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.FactionDeclaresTerritorialWar.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.SettlementOpensTradeRoute.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.ArtifactCleansesScar.ToGambitId()).Value);
    }

    [Fact]
    public void IceRemembers_GambitsStartWithTuningBaselineSuccessChance()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());

        Assert.All(compiled.Programs.Gambits.Gambits, gambit => Assert.Equal(0.25f, gambit.SuccessChance));
    }

    [Fact]
    public void IceRemembers_GambitScoresAreSituationDependent()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var situationCodes = new[]
        {
            BehaviorProgramExpression.SelfAttribute,
            BehaviorProgramExpression.TargetAttribute,
        };

        Assert.All(compiled.Programs.Gambits.Gambits, gambit =>
        {
            Assert.Contains(gambit.ScoreExpression, expression => situationCodes.Contains(expression.Code));
            Assert.Contains(gambit.ScoreExpression, expression => expression.Code != BehaviorProgramExpression.Literal);
        });
    }

    [Fact]
    public void IceRemembers_GambitsUseRelationshipAndMovementEffects()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());

        Assert.Contains(compiled.Programs.Gambits.Gambits.SelectMany(gambit => gambit.Steps), step =>
            step.Operation == BehaviorProgramOperation.CreateEndpointRelationship);
        Assert.Contains(compiled.Programs.Gambits.Gambits.SelectMany(gambit => gambit.Steps), step =>
            step.Operation == BehaviorProgramOperation.MoveTargetToSelfPhysical);
        Assert.DoesNotContain(compiled.Programs.Gambits.Gambits.SelectMany(gambit => gambit.Steps), step =>
            step.Operation == BehaviorProgramOperation.SetHistorical);
    }

    [Fact]
    public void SettlementOpensTradeRouteStartsWithWeakTradeRoute()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var trade = Gambit(compiled, IceRemembersGambitId.SettlementOpensTradeRoute);
        var tradeRouteId = compiled.Ids.RelationshipKindIdOf(IceRemembersRelationship.TradeRoute.ToDomainName()).Value;
        var routeSteps = trade.Steps
            .Where(step =>
                step.Operation == BehaviorProgramOperation.CreateEndpointRelationship &&
                step.A == tradeRouteId)
            .ToArray();

        Assert.Equal(2, routeSteps.Length);
        Assert.All(routeSteps, step =>
        {
            Assert.Equal(IceRemembersRelationshipStrengths.InitialTradeRoute, step.X);
            Assert.True(step.X > 0.05f);
            Assert.True(step.X < 0.70f);
        });
    }

    [Fact]
    public void IceRemembers_CleansingScarTransformsTargetCoverageTerrain()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var cleanse = Gambit(compiled, IceRemembersGambitId.ArtifactCleansesScar);
        var hotSpringsId = compiled.Ids.TerrainIdOf(IceRemembersTerrain.HotSprings.ToDomainName()).Value;

        Assert.Contains(cleanse.Steps, step =>
            step.Operation == BehaviorProgramOperation.SetTargetCoverageTerrain &&
            step.A == hotSpringsId);
    }

    [Fact]
    public void IceRemembers_GambitsPreserveTargetKindContracts()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var penguinKindId = compiled.Ids.KindIdOf(IceRemembersKinds.Penguin).Value;
        var artifactKindId = compiled.Ids.KindIdOf(IceRemembersKinds.Artifact).Value;
        var factionKindId = compiled.Ids.KindIdOf(IceRemembersKinds.Faction).Value;
        var settlementKindId = compiled.Ids.KindIdOf(IceRemembersKinds.Settlement).Value;
        var regionKindId = compiled.Ids.KindIdOf(IceRemembersKinds.Region).Value;

        var wield = Gambit(compiled, IceRemembersGambitId.PenguinWieldsArtifact);
        Assert.Equal(penguinKindId, wield.KindId);
        Assert.Equal(artifactKindId, wield.TargetKindId);

        var war = Gambit(compiled, IceRemembersGambitId.FactionDeclaresTerritorialWar);
        Assert.Equal(factionKindId, war.KindId);
        Assert.Equal(factionKindId, war.TargetKindId);

        var trade = Gambit(compiled, IceRemembersGambitId.SettlementOpensTradeRoute);
        Assert.Equal(settlementKindId, trade.KindId);
        Assert.Equal(settlementKindId, trade.TargetKindId);

        var cleanse = Gambit(compiled, IceRemembersGambitId.ArtifactCleansesScar);
        Assert.Equal(artifactKindId, cleanse.KindId);
        Assert.Equal(regionKindId, cleanse.TargetKindId);
    }

    [Fact]
    public void IceRemembers_TerritorialWarRequiresContestedInfluence()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var war = Gambit(compiled, IceRemembersGambitId.FactionDeclaresTerritorialWar);

        Assert.Contains(war.Preconditions, precondition =>
            precondition.Code == BehaviorProgramPrecondition.ContestedInfluence);
    }

    [Fact]
    public void IceRemembers_PenguinWieldRequiresOneToOneHolds()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var wield = Gambit(compiled, IceRemembersGambitId.PenguinWieldsArtifact);
        var holdsId = compiled.Ids.RelationshipKindIdOf(IceRemembersRelationship.Holds.ToDomainName()).Value;

        Assert.Contains(wield.Preconditions, precondition =>
            precondition.Code == BehaviorProgramPrecondition.EndpointRelationshipCountBelow &&
            precondition.A == holdsId &&
            precondition.B == (int)BehaviorProgramEndpoint.Self &&
            precondition.C == 1);
        Assert.Contains(wield.Preconditions, precondition =>
            precondition.Code == BehaviorProgramPrecondition.EndpointRelationshipCountBelow &&
            precondition.A == holdsId &&
            precondition.B == (int)BehaviorProgramEndpoint.Target &&
            precondition.C == 1);
    }

    [Fact]
    public void IceRemembers_HoldsRelationshipPhysicallyCarriesTarget()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var holdsId = compiled.Ids.RelationshipKindIdOf(IceRemembersRelationship.Holds.ToDomainName()).Value;

        var holds = Assert.Single(compiled.Programs.Relationships.Relationships, relationship =>
            relationship.RelationshipKindId == holdsId);
        Assert.True(holds.TargetFollowsSourcePhysical);
    }

    private static CompiledGambitRule Gambit(CompiledDomain compiled, IceRemembersGambitId gambitId)
    {
        var compiledId = compiled.Ids.GambitIdOf(gambitId.ToGambitId()).Value;
        return Assert.Single(compiled.Programs.Gambits.Gambits, gambit => gambit.GambitId == compiledId);
    }
}
