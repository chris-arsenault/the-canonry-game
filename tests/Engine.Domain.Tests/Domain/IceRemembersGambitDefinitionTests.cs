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

        Assert.Equal(4, gambitIds.Count);
        Assert.Contains(IceRemembersGambitId.FactionBetraysOath.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.FactionDeclaresWar.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.PenguinSlaysRival.ToGambitId(), gambitIds);
        Assert.Contains(IceRemembersGambitId.PenguinWieldsArtifact.ToGambitId(), gambitIds);
    }

    [Fact]
    public void IceRemembers_GambitsCompileIntoEnabledKernelProgram()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());

        Assert.True(compiled.Programs.Gambits.EnabledByDefault);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.PenguinWieldsArtifact.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.FactionDeclaresWar.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.FactionBetraysOath.ToGambitId()).Value);
        Assert.Contains(compiled.Programs.Gambits.Gambits, gambit =>
            gambit.GambitId == compiled.Ids.GambitIdOf(IceRemembersGambitId.PenguinSlaysRival.ToGambitId()).Value);
    }

    [Fact]
    public void IceRemembers_GambitsUseRelationshipAndTargetHistoricalEffects()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());

        Assert.Contains(compiled.Programs.Gambits.Gambits.SelectMany(gambit => gambit.Steps), step =>
            step.Operation == BehaviorProgramOperation.CreateEndpointRelationship);
        Assert.Contains(compiled.Programs.Gambits.Gambits.SelectMany(gambit => gambit.Steps), step =>
            step.Operation == BehaviorProgramOperation.SetEndpointRelationshipStrength);
        Assert.Contains(compiled.Programs.Gambits.Gambits.SelectMany(gambit => gambit.Steps), step =>
            step.Operation == BehaviorProgramOperation.SetHistorical &&
            step.B == (int)BehaviorProgramEndpoint.Target);
    }

    [Fact]
    public void IceRemembers_GambitsPreserveTargetKindContracts()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var penguinKindId = compiled.Ids.KindIdOf(IceRemembersKinds.Penguin).Value;
        var artifactKindId = compiled.Ids.KindIdOf(IceRemembersKinds.Artifact).Value;
        var factionKindId = compiled.Ids.KindIdOf(IceRemembersKinds.Faction).Value;

        var wield = Gambit(compiled, IceRemembersGambitId.PenguinWieldsArtifact);
        Assert.Equal(penguinKindId, wield.KindId);
        Assert.Equal(artifactKindId, wield.TargetKindId);

        var war = Gambit(compiled, IceRemembersGambitId.FactionDeclaresWar);
        Assert.Equal(factionKindId, war.KindId);
        Assert.Equal(factionKindId, war.TargetKindId);

        var slay = Gambit(compiled, IceRemembersGambitId.PenguinSlaysRival);
        Assert.Equal(penguinKindId, slay.KindId);
        Assert.Equal(penguinKindId, slay.TargetKindId);
    }

    private static CompiledGambitRule Gambit(CompiledDomain compiled, IceRemembersGambitId gambitId)
    {
        var compiledId = compiled.Ids.GambitIdOf(gambitId.ToGambitId()).Value;
        return Assert.Single(compiled.Programs.Gambits.Gambits, gambit => gambit.GambitId == compiledId);
    }
}
