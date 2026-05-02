namespace Engine.Domain.Compilation.KernelPrograms;

using Engine.Gameplay;

public sealed class KernelProgramSet
{
    public required LowAltitudeVerbProgram LowAltitudeVerbs { get; init; }
    public required MapActivityProgram MapActivity { get; init; }
    public required MapSignalsProgram MapSignals { get; init; }
    public required EffectsProgram Effects { get; init; }
    public required UpkeepProgram Upkeep { get; init; }
    public required GambitProgram Gambits { get; init; }
    public required RelationshipProgram Relationships { get; init; }
    public required GeneratorProgram Generators { get; init; }
    public required DegeneracyProgram Degeneracy { get; init; }
}
