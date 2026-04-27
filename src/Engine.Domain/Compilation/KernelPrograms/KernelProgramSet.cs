namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class KernelProgramSet
{
    public required UpkeepProgram Upkeep { get; init; }
    public required ProcessProgram Processes { get; init; }
    public required GambitProgram Gambits { get; init; }
    public required RelationshipProgram Relationships { get; init; }
    public required PressureProgram Pressures { get; init; }
    public required GeneratorProgram Generators { get; init; }
    public required DegeneracyProgram Degeneracy { get; init; }
}
