namespace Engine.Kernel;

public readonly partial record struct KernelCommand
{
    public static KernelCommand From(in NoOpKernelCommand command) =>
        new(
            KernelCommandTypeCodes.NoOp,
            command.IssueVersion,
            command.SubmitterId,
            command.SubmitterSequence);

    public static KernelCommand From(in SetGeneratorEnabledKernelCommand command) =>
        new(
            KernelCommandTypeCodes.SetGeneratorEnabled,
            command.IssueVersion,
            command.SubmitterId,
            command.SubmitterSequence,
            command.GeneratorId,
            command.Enabled);

    public NoOpKernelCommand AsNoOp()
    {
        EnsureType(KernelCommandTypeCodes.NoOp);
        return new(IssueVersion, SubmitterId, SubmitterSequence);
    }

    public SetGeneratorEnabledKernelCommand AsSetGeneratorEnabled()
    {
        EnsureType(KernelCommandTypeCodes.SetGeneratorEnabled);
        return new(IssueVersion, _a, _b, SubmitterId, SubmitterSequence);
    }

    private void EnsureType(int expected)
    {
        if (CommandType != expected)
        {
            throw new global::System.InvalidOperationException();
        }
    }
}
