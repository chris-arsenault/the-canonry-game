namespace Engine.Host.Tests;

using Engine.Domain.Compilation;

using IceRemembers;

public sealed class GameplayCompilationBoundaryTests
{
    [Fact]
    public void IceRemembersGeneratorAffordancesCoverTargetSecondaryAndProducedKinds()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var affordances = compiled.Programs.LowAltitudeVerbs.GeneratorAffordances;
        var factionId = compiled.Ids.KindIdOf(IceRemembersKinds.Faction).Value;
        var regionId = compiled.Ids.KindIdOf(IceRemembersKinds.Region).Value;
        var settlementId = compiled.Ids.KindIdOf(IceRemembersKinds.Settlement).Value;
        var artifactId = compiled.Ids.KindIdOf(IceRemembersKinds.Artifact).Value;

        Assert.Contains(affordances, rule =>
            rule.ActorKindId == factionId &&
            rule.CandidateKindId == regionId &&
            rule.BindingKindId == regionId);
        Assert.Contains(affordances, rule =>
            rule.ActorKindId == factionId &&
            rule.CandidateKindId == artifactId &&
            rule.BindingKindId == artifactId);
        Assert.Contains(affordances, rule =>
            rule.ActorKindId == regionId &&
            rule.CandidateKindId == artifactId &&
            rule.BindingKindId == artifactId);
        Assert.Contains(affordances, rule =>
            rule.ActorKindId == settlementId &&
            rule.CandidateKindId == settlementId &&
            rule.BindingKindId == settlementId);
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
