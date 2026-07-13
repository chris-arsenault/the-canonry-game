namespace Engine.Domain.Tests.Domain;

using Engine.Constants;
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

        // 24 strategic gambits + 12 grant gambits + one work-tier gambit per kind.
        Assert.Equal(36 + IceRemembersEntities.All.Count, gambitIds.Count);
        Assert.Contains(IceRemembersGambitId.AbilitySharesPractice.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.ArtifactBindsMonster.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.ArtifactCleansesScar.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.FactionDeclaresTerritorialWar.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.FactionClaimsPointOfInterest.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.FactionJoinsOccurrence.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.FactionSuesForPeace.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.OrcaClaimsHuntingGround.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.PenguinWieldsArtifact.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.SettlementOpensTradeRoute.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.SettlementRaisesCauseway.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.TransportOpensConduit.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.FactionChartersSettlement.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.TransportSurveysRegion.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.OrcaLaysDeepCurrent.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.TransportWardsMonster.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.MonsterHauntsTransport.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.OrcaRaidsTradeRoute.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.MonsterStealsArtifact.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.PenguinSlaysMonster.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.PenguinSlaysOrca.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.SettlementDeepensTheHolding.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.FactionDeepensTheHolding.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.TransportDeepensTheHolding.ToGambitId(), gambitIds);
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
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.SettlementRaisesCauseway.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.ArtifactCleansesScar.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.AbilitySharesPractice.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.FactionClaimsPointOfInterest.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.FactionJoinsOccurrence.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.OrcaClaimsHuntingGround.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.TransportOpensConduit.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.ArtifactBindsMonster.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.FactionChartersSettlement.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.TransportSurveysRegion.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.OrcaLaysDeepCurrent.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.TransportWardsMonster.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.MonsterHauntsTransport.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.OrcaRaidsTradeRoute.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.MonsterStealsArtifact.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.PenguinSlaysMonster.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.PenguinSlaysOrca.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.SettlementDeepensTheHolding.ToGambitId()).Value);
    }

    [Fact]
    public void IceRemembers_GambitsStartWithTuningBaselineSuccessChance()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());

        var workTierGambitIds = new HashSet<int>(
            Enum.GetValues<IceRemembersGambitId>()
                .Where(id => id.ToGambitId().EndsWith("_deepens_the_holding", StringComparison.Ordinal))
                .Select(id => compiled.Ids.GambitIdOf(id.ToGambitId()).Value));
        var grantGambitIds = new HashSet<int>(
            IceRemembersDefinition.GrantGambitIds
                .Select(id => compiled.Ids.GambitIdOf(id.ToGambitId()).Value));

        Assert.All(compiled.Programs.Gambits.Gambits, gambit =>
        {
            if (grantGambitIds.Contains(gambit.GambitId))
            {
                // Grant gambits carry authored reliability: renewal and other
                // granted acts are dependable by design, not baseline-tuned.
                Assert.InRange(gambit.SuccessChance, 0.5f, 0.85f);
                return;
            }

            var expected = workTierGambitIds.Contains(gambit.GambitId) ? 0.75f : 0.25f;
            Assert.Equal(expected, gambit.SuccessChance);
        });
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

        var slayMonsterId = compiled.Ids.GambitIdOf(IceRemembersGambitId.PenguinSlaysMonster.ToGambitId()).Value;
        var slayMonster = Assert.Single(compiled.Programs.Gambits.Gambits, gambit => gambit.GambitId == slayMonsterId);
        Assert.Contains(slayMonster.Steps, step =>
            step.Operation == BehaviorProgramOperation.SetHistorical);
        var slayOrcaId = compiled.Ids.GambitIdOf(IceRemembersGambitId.PenguinSlaysOrca.ToGambitId()).Value;
        var slayOrca = Assert.Single(compiled.Programs.Gambits.Gambits, gambit => gambit.GambitId == slayOrcaId);
        Assert.Contains(slayOrca.Steps, step =>
            step.Operation == BehaviorProgramOperation.SetHistorical);
        Assert.DoesNotContain(
            compiled.Programs.Gambits.Gambits
                .Where(gambit => gambit.GambitId != slayMonsterId && gambit.GambitId != slayOrcaId)
                .SelectMany(gambit => gambit.Steps),
            step => step.Operation == BehaviorProgramOperation.SetHistorical);
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
    public void SettlementOpensTradeRouteRequiresNearbySemanticTarget()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var trade = Gambit(compiled, IceRemembersGambitId.SettlementOpensTradeRoute);

        Assert.Contains(trade.Preconditions, precondition =>
            precondition.Code == BehaviorProgramPrecondition.PlaneDistance &&
            precondition.A == 12 &&
            precondition.B == PlaneCodes.Semantic);
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
    public void IceRemembers_SettlementCausewayBuildsPhysicalTravelStructure()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var causeway = Gambit(compiled, IceRemembersGambitId.SettlementRaisesCauseway);
        var causewayId = compiled.Ids.TravelStructureIdOf(IceRemembersTravelStructure.IceCauseway.ToDomainName()).Value;

        Assert.Contains(causeway.Steps, step =>
            step.Operation == BehaviorProgramOperation.BuildTravelStructureLine &&
            step.A == causewayId &&
            step.B == PlaneCodes.Physical &&
            step.C == 24);
    }

    [Fact]
    public void IceRemembers_OrcaCurrentBuildsPhysicalDeepCurrent()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var current = Gambit(compiled, IceRemembersGambitId.OrcaLaysDeepCurrent);
        var currentId = compiled.Ids.TravelStructureIdOf(IceRemembersTravelStructure.DeepCurrent.ToDomainName()).Value;

        Assert.Contains(current.Steps, step =>
            step.Operation == BehaviorProgramOperation.BuildTravelStructureLine &&
            step.A == currentId &&
            step.B == PlaneCodes.Physical &&
            step.C == 16);

        var controlledById = compiled.Ids.RelationshipKindIdOf(IceRemembersRelationship.ControlledBy.ToDomainName()).Value;
        Assert.Contains(current.Steps, step =>
            step.Operation == BehaviorProgramOperation.CreateEndpointRelationship &&
            step.A == controlledById &&
            step.B == (int)BehaviorProgramEndpoint.Target &&
            step.C == (int)BehaviorProgramEndpoint.Self);
    }

    [Fact]
    public void IceRemembers_WorkTierGambitsUseSelfCultureMassOnNonMobilePlane()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var settlement = Gambit(compiled, IceRemembersGambitId.SettlementDeepensTheHolding);
        var transport = Gambit(compiled, IceRemembersGambitId.TransportDeepensTheHolding);

        Assert.Equal(BehaviorProgramTargetMode.Self, settlement.TargetMode);
        Assert.Contains(settlement.Preconditions, precondition =>
            precondition.Code == BehaviorProgramPrecondition.SelfCultureMass &&
            precondition.A == PlaneCodes.Physical);
        Assert.DoesNotContain(settlement.Preconditions, precondition =>
            precondition.Code == BehaviorProgramPrecondition.SelfWorkTierBelow);
        Assert.Contains(settlement.Steps, step =>
            step.Operation == BehaviorProgramOperation.IncreaseWorkTier &&
            step.A == 0);
        Assert.Contains(transport.Preconditions, precondition =>
            precondition.Code == BehaviorProgramPrecondition.SelfCultureMass &&
            precondition.A == PlaneCodes.Semantic);
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

        var causeway = Gambit(compiled, IceRemembersGambitId.SettlementRaisesCauseway);
        Assert.Equal(settlementKindId, causeway.KindId);
        Assert.Equal(settlementKindId, causeway.TargetKindId);

        var cleanse = Gambit(compiled, IceRemembersGambitId.ArtifactCleansesScar);
        Assert.Equal(artifactKindId, cleanse.KindId);
        Assert.Equal(regionKindId, cleanse.TargetKindId);
    }

    [Fact]
    public void IceRemembers_SecondWaveGambitsPreserveTargetKindContracts()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());

        AssertGambitKinds(compiled, IceRemembersGambitId.FactionClaimsPointOfInterest, IceRemembersKinds.Faction, IceRemembersKinds.PointOfInterest);
        AssertGambitKinds(compiled, IceRemembersGambitId.OrcaClaimsHuntingGround, IceRemembersKinds.Orca, IceRemembersKinds.PointOfInterest);
        AssertGambitKinds(compiled, IceRemembersGambitId.AbilitySharesPractice, IceRemembersKinds.Ability, IceRemembersKinds.Penguin);
        AssertGambitKinds(compiled, IceRemembersGambitId.TransportOpensConduit, IceRemembersKinds.Transport, IceRemembersKinds.PointOfInterest);
        AssertGambitKinds(compiled, IceRemembersGambitId.FactionJoinsOccurrence, IceRemembersKinds.Faction, IceRemembersKinds.Occurrence);
        AssertGambitKinds(compiled, IceRemembersGambitId.ArtifactBindsMonster, IceRemembersKinds.Artifact, IceRemembersKinds.Monster);
        AssertGambitKinds(compiled, IceRemembersGambitId.FactionChartersSettlement, IceRemembersKinds.Faction, IceRemembersKinds.Settlement);
        AssertGambitKinds(compiled, IceRemembersGambitId.TransportSurveysRegion, IceRemembersKinds.Transport, IceRemembersKinds.Region);
        AssertGambitKinds(compiled, IceRemembersGambitId.OrcaLaysDeepCurrent, IceRemembersKinds.Orca, IceRemembersKinds.PointOfInterest);
        AssertGambitKinds(compiled, IceRemembersGambitId.TransportWardsMonster, IceRemembersKinds.Transport, IceRemembersKinds.Monster);
        AssertGambitKinds(compiled, IceRemembersGambitId.MonsterHauntsTransport, IceRemembersKinds.Monster, IceRemembersKinds.Transport);
        AssertGambitKinds(compiled, IceRemembersGambitId.OrcaRaidsTradeRoute, IceRemembersKinds.Orca, IceRemembersKinds.Penguin);
        AssertGambitKinds(compiled, IceRemembersGambitId.MonsterStealsArtifact, IceRemembersKinds.Monster, IceRemembersKinds.Artifact);
        AssertGambitKinds(compiled, IceRemembersGambitId.PenguinSlaysMonster, IceRemembersKinds.Penguin, IceRemembersKinds.Monster);
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

    private static void AssertGambitKinds(
        CompiledDomain compiled,
        IceRemembersGambitId gambitId,
        Engine.Kinds.KindId actorKind,
        Engine.Kinds.KindId targetKind)
    {
        var gambit = Gambit(compiled, gambitId);
        Assert.Equal(compiled.Ids.KindIdOf(actorKind).Value, gambit.KindId);
        Assert.Equal(compiled.Ids.KindIdOf(targetKind).Value, gambit.TargetKindId);
    }
}
