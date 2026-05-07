namespace Engine.Host.Tests;

using Engine.Domain.Compilation;

using IceRemembers;

public sealed class GameplayCompilationBoundaryTests
{
    [Fact]
    public void IceRemembersAffordanceCompilerDoesNotExposeHighAltitudeOnlyPrerequisites()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var affordances = compiled.Programs.LowAltitudeVerbs.Affordances;
        var allowedShapeCodes = AllowedAffordanceShapeCodes();
        var settlementId = compiled.Ids.KindIdOf(IceRemembersKinds.Settlement).Value;
        var routesId = compiled.Ids.GeneratorIdOf("routes_draw_readers").Value;
        var tradeRouteGambitId = compiled.Ids.GambitIdOf(IceRemembersGambitId.SettlementOpensTradeRoute.ToGambitId()).Value;
        var peaceGambitId = compiled.Ids.GambitIdOf(IceRemembersGambitId.FactionSuesForPeace.ToGambitId()).Value;
        var atWarId = compiled.Ids.RelationshipKindIdOf(IceRemembersRelationship.AtWarWith.ToDomainName()).Value;
        var tradeRouteId = compiled.Ids.RelationshipKindIdOf(IceRemembersRelationship.TradeRoute.ToDomainName()).Value;

        Assert.All(affordances, rule => Assert.Contains(rule.ShapeCode, allowedShapeCodes));
        Assert.DoesNotContain(affordances, rule =>
            rule.ShapeCode == LowAltitudeAffordanceShapeCodes.PresenceNearEntity &&
            rule.SourceKindCode == AffordanceSourceKindCodes.Generator);
        Assert.DoesNotContain(affordances, rule =>
            rule.ShapeCode == LowAltitudeAffordanceShapeCodes.RelationshipStrengthChange &&
            rule.SourceKindCode == AffordanceSourceKindCodes.Gambit &&
            rule.SourceId == tradeRouteGambitId &&
            rule.RelationshipKindId == tradeRouteId);
        Assert.DoesNotContain(affordances, rule =>
            rule.ShapeCode == LowAltitudeAffordanceShapeCodes.RelationshipStrengthChange &&
            rule.SourceKindCode == AffordanceSourceKindCodes.Gambit &&
            rule.SourceId == peaceGambitId &&
            rule.RelationshipKindId == atWarId);
        Assert.Contains(affordances, rule =>
            rule.ShapeCode == LowAltitudeAffordanceShapeCodes.RelationshipStrengthChange &&
            rule.SourceKindCode == AffordanceSourceKindCodes.Generator &&
            rule.SourceId == routesId &&
            rule.ActorKindId == settlementId &&
            rule.RelationshipKindId == tradeRouteId &&
            rule.RelationshipDirectionCode == RelationshipMatchDirectionCodes.Outgoing &&
            rule.PolarityCode == AffordancePolarityCodes.Strengthen);
    }

    [Fact]
    public void IceRemembersAffordanceCompilerKeepsActionableInfluencePressure()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var affordances = compiled.Programs.LowAltitudeVerbs.Affordances;
        var factionId = compiled.Ids.KindIdOf(IceRemembersKinds.Faction).Value;
        var warGambitId = compiled.Ids.GambitIdOf(IceRemembersGambitId.FactionDeclaresTerritorialWar.ToGambitId()).Value;

        Assert.Contains(affordances, rule =>
            rule.ShapeCode == LowAltitudeAffordanceShapeCodes.InfluenceChange &&
            rule.SourceKindCode == AffordanceSourceKindCodes.Gambit &&
            rule.SourceId == warGambitId &&
            rule.ActorKindId == factionId &&
            rule.PlaneCode == PlaneCodes.Physical &&
            rule.OperationCode == ProgramOpcodeCodes.AddOwnedCellScalar &&
            rule.PolarityCode == AffordancePolarityCodes.Increase);
    }

    [Fact]
    public void LowAltitudeAffordanceShapeCodesDoNotDefineHighAltitudeOnlyShapes()
    {
        var shapeNames = AffordanceShapeNames();

        Assert.DoesNotContain("PresenceNearRelationshipTarget", shapeNames);
        Assert.DoesNotContain("TerrainPresence", shapeNames);
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

    private static HashSet<int> AllowedAffordanceShapeCodes() =>
        AffordanceShapeFields()
            .Select(field => (int)field.GetRawConstantValue()!)
            .ToHashSet();

    private static HashSet<string> AffordanceShapeNames() =>
        AffordanceShapeFields()
            .Select(field => field.Name)
            .ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<System.Reflection.FieldInfo> AffordanceShapeFields() =>
        typeof(LowAltitudeAffordanceShapeCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly);
}
