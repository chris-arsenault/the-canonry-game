namespace Engine.Kernel.Driver;

internal sealed class KernelCommandCompletionTable
{
    private readonly Entry[] _entries;
    private readonly object _gate = new();

    public KernelCommandCompletionTable(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _entries = new Entry[capacity];
        for (var i = 0; i < _entries.Length; i++)
        {
            _entries[i] = new Entry();
        }
    }

    public void Track(int commandId)
    {
        var entry = _entries[Slot(commandId)];
        lock (_gate)
        {
            entry.CommandId = commandId;
            entry.HasResult = false;
            entry.Result = default;
            entry.Completion = null;
        }
    }

    public bool TryGetResult(int commandId, out KernelCommandResult result)
    {
        var entry = _entries[Slot(commandId)];
        lock (_gate)
        {
            if (entry.CommandId == commandId && entry.HasResult)
            {
                result = entry.Result;
                return true;
            }
        }

        result = default;
        return false;
    }

    public Task<KernelCommandResult> WaitAsync(int commandId)
    {
        var entry = _entries[Slot(commandId)];
        lock (_gate)
        {
            if (entry.CommandId == commandId && entry.HasResult)
            {
                return Task.FromResult(entry.Result);
            }

            if (entry.CommandId != commandId)
            {
                return Task.FromException<KernelCommandResult>(new InvalidOperationException());
            }

            entry.Completion ??= new TaskCompletionSource<KernelCommandResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            return entry.Completion.Task;
        }
    }

    public void Complete(in KernelCommandResult result)
    {
        TaskCompletionSource<KernelCommandResult>? completion;
        var entry = _entries[Slot(result.CommandId)];
        lock (_gate)
        {
            if (entry.CommandId != result.CommandId)
            {
                return;
            }

            entry.Result = result;
            entry.HasResult = true;
            completion = entry.Completion;
        }

        completion?.TrySetResult(result);
    }

    private int Slot(int commandId) => (int)((uint)commandId % (uint)_entries.Length);

    private sealed class Entry
    {
        public int CommandId { get; set; }

        public bool HasResult { get; set; }

        public KernelCommandResult Result { get; set; }

        public TaskCompletionSource<KernelCommandResult>? Completion { get; set; }
    }
}
