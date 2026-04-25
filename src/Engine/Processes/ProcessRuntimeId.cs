namespace Engine.Processes;

/// <summary>
/// Stable primitive process identity used by the simulation kernel after domain
/// content has been registered.
/// </summary>
public readonly record struct ProcessRuntimeId(int Value)
{
    public static readonly ProcessRuntimeId None = new(0);

    public bool IsNone => Value == 0;

    public static ProcessRuntimeId FromStableName(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        unchecked
        {
            const uint offset = 2166136261u;
            const uint prime = 16777619u;
            var hash = offset;
            foreach (var ch in value)
            {
                hash ^= ch;
                hash *= prime;
            }

            var id = (int)(hash & 0x7fffffffu);
            return new ProcessRuntimeId(id == 0 ? 1 : id);
        }
    }
}
