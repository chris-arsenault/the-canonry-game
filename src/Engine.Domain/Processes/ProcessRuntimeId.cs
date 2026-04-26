namespace Engine.Processes;

/// <summary>
/// Stable primitive process identity used by the simulation kernel after domain
/// content has been registered.
/// </summary>
public readonly record struct ProcessRuntimeId(int Value)
{
    public static readonly ProcessRuntimeId None = new(0);

    public bool IsNone => Value == 0;
}
