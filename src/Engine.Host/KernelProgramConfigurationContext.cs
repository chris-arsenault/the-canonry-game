namespace Engine.Host;

public sealed class KernelProgramConfigurationContext
{
    private readonly Dictionary<int, bool> _enabledByProgram = [];

    public void Enable(int programId) =>
        _enabledByProgram[programId] = true;

    public void Disable(int programId) =>
        _enabledByProgram[programId] = false;

    internal bool IsEnabled(int programId, bool defaultValue) =>
        _enabledByProgram.TryGetValue(programId, out var enabled)
            ? enabled
            : defaultValue;

    internal IEnumerable<KeyValuePair<int, bool>> Overrides => _enabledByProgram;
}
