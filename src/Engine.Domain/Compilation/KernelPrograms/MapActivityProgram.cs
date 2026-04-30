namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class MapActivityProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public float PresenceHeat { get; init; } = 1f;
}

