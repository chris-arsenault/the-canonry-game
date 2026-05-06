namespace Engine.Host.Tests;

using Engine.Domain.Compilation;

using IceRemembers;

public sealed class GameplayCompilationBoundaryTests
{
    [Fact]
    public void IceRemembersGeneratorAffordancesComeFromEligibilitySignals()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var affordances = compiled.Programs.LowAltitudeVerbs.Affordances;
        var factionId = compiled.Ids.KindIdOf(IceRemembersKinds.Faction).Value;
        var regionId = compiled.Ids.KindIdOf(IceRemembersKinds.Region).Value;
        var settlementId = compiled.Ids.KindIdOf(IceRemembersKinds.Settlement).Value;
        var cleansedById = compiled.Ids.RelationshipKindIdOf(IceRemembersRelationship.CleansedBy.ToDomainName()).Value;
        var tradeRouteId = compiled.Ids.RelationshipKindIdOf(IceRemembersRelationship.TradeRoute.ToDomainName()).Value;
        var wastesId = compiled.Ids.TerrainIdOf(IceRemembersTerrain.Wastes.ToDomainName()).Value;

        var generatorAffordances = affordances
            .Where(rule => rule.SourceKindCode == AffordanceSourceKindCodes.Generator)
            .ToArray();

        Assert.DoesNotContain(generatorAffordances, rule =>
            rule.ShapeCode == LowAltitudeAffordanceShapeCodes.PresenceNearEntity);
        Assert.Contains(generatorAffordances, rule =>
            rule.ShapeCode == LowAltitudeAffordanceShapeCodes.PresenceNearRelationshipTarget &&
            rule.ActorKindId == regionId &&
            rule.RelationshipKindId == cleansedById &&
            rule.RelationshipDirectionCode == RelationshipMatchDirectionCodes.Incoming);
        Assert.Contains(generatorAffordances, rule =>
            rule.ShapeCode == LowAltitudeAffordanceShapeCodes.RelationshipStrengthChange &&
            rule.ActorKindId == settlementId &&
            rule.RelationshipKindId == tradeRouteId &&
            rule.RelationshipDirectionCode == RelationshipMatchDirectionCodes.Outgoing &&
            rule.PolarityCode == AffordancePolarityCodes.Strengthen);
        Assert.Contains(generatorAffordances, rule =>
            rule.ShapeCode == LowAltitudeAffordanceShapeCodes.TerrainPresence &&
            rule.SourceKindCode == AffordanceSourceKindCodes.Generator &&
            rule.ActorKindId == factionId &&
            rule.TerrainId == wastesId);
    }

    [Fact]
    public void IceRemembersPenguinsCanReduceInfluenceOnPhysicalAndSemanticPlanes()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var penguinId = compiled.Ids.KindIdOf(IceRemembersKinds.Penguin).Value;
        var physicalVerbId = compiled.Ids.VerbIdOf(IceRemembersVerbId.PenguinScatterClaimMarks.ToVerbId()).Value;
        var semanticVerbId = compiled.Ids.VerbIdOf(IceRemembersVerbId.PenguinBreakChorus.ToVerbId()).Value;

        Assert.Contains(compiled.Programs.LowAltitudeVerbs.Verbs, verb =>
            verb.KindId == penguinId &&
            verb.VerbId == physicalVerbId &&
            verb.PlaneCode == PlaneCodes.Physical &&
            verb.OperationCode == ProgramOpcodeCodes.SubtractOtherCellScalar &&
            verb.TargetModeCode == TargetSelectorCodes.OtherMapValue);
        Assert.Contains(compiled.Programs.LowAltitudeVerbs.Verbs, verb =>
            verb.KindId == penguinId &&
            verb.VerbId == semanticVerbId &&
            verb.PlaneCode == PlaneCodes.Semantic &&
            verb.OperationCode == ProgramOpcodeCodes.SubtractOtherCellScalar &&
            verb.TargetModeCode == TargetSelectorCodes.OtherMapValue);
    }

    [Fact]
    public void IceRemembersReduceInfluenceVerbsUseHostileOrOverextendedTargeting()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var reducers = compiled.Programs.LowAltitudeVerbs.Verbs
            .Where(verb => verb.OperationCode == ProgramOpcodeCodes.SubtractOtherCellScalar)
            .ToArray();

        Assert.NotEmpty(reducers);
        Assert.All(reducers, verb =>
        {
            Assert.True(verb.ForeignInfluenceFilterEnabled);
            Assert.True(verb.ForeignInfluenceBlockPositiveRelationships);
            Assert.True(verb.ForeignInfluenceAllowNegativeRelationships);
            Assert.Equal(0.05f, verb.ForeignInfluenceMinimumRelationshipStrength);
            Assert.Equal(50f, verb.ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove);
        });
    }
}
