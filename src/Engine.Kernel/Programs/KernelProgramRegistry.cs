namespace Engine.Kernel.Programs;

public sealed partial class KernelProgramRegistry
{
    private readonly int[] _programIds;
    private readonly int[] _systemHandles;
    private readonly bool[] _enabled;
    private int _count;

    public KernelProgramRegistry(int capacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _programIds = new int[capacity];
        _systemHandles = new int[capacity];
        _enabled = new bool[capacity];
    }

    public int Count => _count;

    public void Register(int programId, int systemHandle)
    {
        ValidatePositive(programId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(systemHandle);
        if (_count >= _programIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _programIds[_count] = programId;
        _systemHandles[_count] = systemHandle;
        _enabled[_count] = true;
        _count++;
    }

    public void SetEnabled(Simulation simulation, int programId, bool enabled)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        ValidatePositive(programId);
        var matched = false;
        for (var i = 0; i < _count; i++)
        {
            if (_programIds[i] != programId)
            {
                continue;
            }

            simulation.SetSystemActive(_systemHandles[i], enabled);
            _enabled[i] = enabled;
            matched = true;
        }

        if (!matched)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    public bool IsEnabled(int programId)
    {
        ValidatePositive(programId);
        var matched = false;
        var enabled = false;
        for (var i = 0; i < _count; i++)
        {
            if (_programIds[i] != programId)
            {
                continue;
            }

            matched = true;
            enabled |= _enabled[i];
        }

        if (!matched)
        {
            throw new global::System.InvalidOperationException();
        }

        return enabled;
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
