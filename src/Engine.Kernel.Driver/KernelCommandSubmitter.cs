namespace Engine.Kernel.Driver;

using Engine.Constants;

internal sealed class KernelCommandSubmitter : ICommandSubmitter
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

    public bool TrySetGeneratorEnabled(in SetGeneratorEnabledCommand command, out CommandHandle handle)
    {
        var kernelCommand = KernelCommand.From(new SetGeneratorEnabledKernelCommand(
            command.IssueVersion,
            command.GeneratorId,
            command.Enabled ? 1 : 0,
            command.SubmitterId,
            command.SubmitterSequence));
        return TryEnqueue(in kernelCommand, out handle);
    }

    public bool TrySetGambitEnabled(in SetGambitEnabledCommand command, out CommandHandle handle)
    {
        var kernelCommand = KernelCommand.From(new SetGambitEnabledKernelCommand(
            command.IssueVersion,
            command.GambitId,
            command.Enabled ? 1 : 0,
            command.SubmitterId,
            command.SubmitterSequence));
        return TryEnqueue(in kernelCommand, out handle);
    }

    public bool TryBlessCellCulture(in BlessCellCultureCommand command, out CommandHandle handle)
    {
        var kernelCommand = KernelCommand.From(new BlessCellCultureKernelCommand(
            command.IssueVersion,
            command.CultureId,
            command.PlaneCode,
            command.Q,
            command.R,
            command.Radius,
            FixedScalar.FromSingle(command.Amount),
            command.SubmitterId,
            command.SubmitterSequence));
        return TryEnqueue(in kernelCommand, out handle);
    }

    public bool TryFavorEntity(in FavorEntityCommand command, out CommandHandle handle)
    {
        var kernelCommand = KernelCommand.From(new FavorEntityKernelCommand(
            command.IssueVersion,
            command.EntityId,
            command.DurationTicks,
            FixedScalar.FromSingle(command.MultiplierDelta),
            command.SubmitterId,
            command.SubmitterSequence));
        return TryEnqueue(in kernelCommand, out handle);
    }

    internal bool TryEnqueue(in KernelCommand command, out CommandHandle handle)
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
