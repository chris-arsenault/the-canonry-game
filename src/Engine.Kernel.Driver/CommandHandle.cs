namespace Engine.Kernel.Driver;

public readonly record struct CommandHandle
{
    private readonly KernelCommandCompletionTable? _completionTable;

    internal CommandHandle(KernelCommandCompletionTable completionTable, int commandId)
    {
        _completionTable = completionTable;
        CommandId = commandId;
    }

    public int CommandId { get; }

    public bool TryGetResult(out KernelCommandResult result)
    {
        if (_completionTable is null)
        {
            result = default;
            return false;
        }

        return _completionTable.TryGetResult(CommandId, out result);
    }

    public ValueTask<KernelCommandResult> WaitAsync(CancellationToken cancellationToken = default)
    {
        if (_completionTable is null)
        {
            return ValueTask.FromException<KernelCommandResult>(new InvalidOperationException());
        }

        return new ValueTask<KernelCommandResult>(_completionTable.WaitAsync(CommandId).WaitAsync(cancellationToken));
    }
}
