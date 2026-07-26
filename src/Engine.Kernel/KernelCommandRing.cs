namespace Engine.Kernel;

internal sealed class KernelCommandRing
{
    private readonly KernelCommand[] _commands;
    private readonly object _gate = new();
    private int _head;
    private int _tail;
    private int _count;
    private int _nextCommandId;
    private long _rejectedWriteCount;

    public KernelCommandRing(int capacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(capacity);
        _commands = new KernelCommand[capacity];
    }

    public int Capacity => _commands.Length;

    public int Count
    {
        get => global::System.Threading.Volatile.Read(ref _count);
    }

    public long RejectedWriteCount => global::System.Threading.Volatile.Read(ref _rejectedWriteCount);

    public bool TryEnqueue(in KernelCommand command, out int commandId)
    {
        lock (_gate)
        {
            if (global::System.Threading.Volatile.Read(ref _count) == _commands.Length)
            {
                _rejectedWriteCount++;
                commandId = 0;
                return false;
            }

            commandId = ++_nextCommandId;
            _commands[_tail] = command.WithCommandId(commandId);
            _tail++;
            if (_tail == _commands.Length)
            {
                _tail = 0;
            }

            global::System.Threading.Interlocked.Increment(ref _count);
            return true;
        }
    }

    public bool TryRead(out KernelCommand command)
    {
        if (global::System.Threading.Volatile.Read(ref _count) == 0)
        {
            command = default;
            return false;
        }

        command = _commands[_head];
        _commands[_head] = default;
        _head++;
        if (_head == _commands.Length)
        {
            _head = 0;
        }

        global::System.Threading.Interlocked.Decrement(ref _count);
        return true;
    }
}
