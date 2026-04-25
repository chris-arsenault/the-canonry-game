namespace Engine.Kernel;

public sealed class TickScheduler
{
    private readonly int[] _phases;
    private readonly int[] _cadences;
    private readonly global::System.Action<int>?[] _callbacks;
    private readonly bool[] _active;
    private readonly int _maxPhase;
    private int _highWatermark;

    public TickScheduler(int capacity, int maxPhase)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _phases = new int[capacity];
        _cadences = new int[capacity];
        _callbacks = new global::System.Action<int>[capacity];
        _active = new bool[capacity];
        _maxPhase = maxPhase;
    }

    public int CurrentTick { get; private set; }

    public int Register(int phase, int cadence, global::System.Action<int> callback)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(phase);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThan(phase, _maxPhase);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cadence);
        global::System.ArgumentNullException.ThrowIfNull(callback);

        var slot = FindSlot();
        _phases[slot] = phase;
        _cadences[slot] = cadence;
        _callbacks[slot] = callback;
        _active[slot] = true;
        if (slot >= _highWatermark)
        {
            _highWatermark = slot + 1;
        }

        return slot;
    }

    public void Unregister(int handle)
    {
        if ((uint)handle >= (uint)_active.Length)
        {
            return;
        }

        _active[handle] = false;
        _callbacks[handle] = null;
    }

    public bool WouldRun(int handle, int tick)
    {
        if ((uint)handle >= (uint)_active.Length || !_active[handle])
        {
            return false;
        }

        return tick % _cadences[handle] == 0;
    }

    public bool Tick()
    {
        CurrentTick++;
        for (var phase = 0; phase <= _maxPhase; phase++)
        {
            ExecutePhase(phase);
        }

        return true;
    }

    private int FindSlot()
    {
        for (var i = 0; i < _active.Length; i++)
        {
            if (!_active[i])
            {
                return i;
            }
        }

        throw new global::System.InvalidOperationException();
    }

    private void ExecutePhase(int phase)
    {
        for (var i = 0; i < _highWatermark; i++)
        {
            if (!_active[i] || _phases[i] != phase || CurrentTick % _cadences[i] != 0)
            {
                continue;
            }

            var callback = _callbacks[i];
            if (callback is null)
            {
                throw new global::System.InvalidOperationException();
            }

            callback(CurrentTick);
        }
    }
}
