namespace Engine.Domain.Compilation.KernelPrograms;

using Engine.Gameplay;

public sealed class KernelProgramSet
{
    public required LowAltitudeVerbProgram LowAltitudeVerbs { get; init; }
    public required LocalityRules Locality { get; init; }
    public required EffectsProgram Effects { get; init; }
    public required LifecycleProgram Lifecycle { get; init; }
    public required GambitProgram Gambits { get; init; }
    public required RelationshipProgram Relationships { get; init; }
    public required GeneratorProgram Generators { get; init; }
    public required CultureSlotProgram CultureSlots { get; init; }
    public required MinorActorProgram MinorActors { get; init; }
    public required ChronicleProgram Chronicles { get; init; }
    public required WorldBeatProgram WorldBeats { get; init; }
    public required MapFrontierProgram MapFrontier { get; init; }
}
