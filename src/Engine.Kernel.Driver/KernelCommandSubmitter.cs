namespace Engine.Kernel.Driver;

public sealed class KernelCommandSubmitter : ICommandSubmitter
{
    private readonly KernelCommandRing _commands;
    private readonly KernelCommandCompletionTable _completionTable;

    public KernelCommandSubmitter(KernelCommandRing commands, KernelCommandCompletionTable completionTable)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(completionTable);

        _commands = commands;
        _completionTable = completionTable;
    }

    public bool TryEnqueue(in KernelCommand command, out CommandHandle handle)
    {
        if (!_commands.TryEnqueue(in command, out var commandId))
        {
            handle = default;
            return false;
        }

        _completionTable.Track(commandId);
        handle = new CommandHandle(_completionTable, commandId);
        return true;
    }
}
