namespace Engine.Host.Tests;

using Engine.Domain.Compilation;
using Engine.Domain.Compilation.KernelPrograms;

using IceRemembers;

public sealed class DomainCompilerObjectiveTests
{
    [Fact]
    public void Compile_RegistersEveryIceRemembersAttributeAsAPlannerObjective()
    {
        var config = IceRemembersDefinition.Create();
        var compiled = DomainCompiler.Compile(config);

        var expectedObjectives = config.Kinds
            .SelectMany(kind =>
                kind.Value.Attributes.Select(attribute => (
                    KindId: compiled.Ids.KindIdOf(kind.Key).Value,
                    AttributeId: compiled.Ids.AttributeIdOf(attribute.Name).Value)))
            .ToHashSet();

        var actualObjectives = compiled.Programs.Processes.Objectives
            .Where(objective => objective.EffectScope == ProcessProgramEffectScope.SelfAttribute)
            .Select(objective => (objective.KindId, objective.AttributeId))
            .ToHashSet();

        Assert.All(expectedObjectives, objective => Assert.Contains(objective, actualObjectives));
    }
}
