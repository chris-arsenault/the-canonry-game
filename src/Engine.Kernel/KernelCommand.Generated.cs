namespace Engine.Kernel;

internal readonly partial record struct KernelCommand
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

    public static KernelCommand From(in SetGambitEnabledKernelCommand command) =>
        new(
            KernelCommandTypeCodes.SetGambitEnabled,
            command.IssueVersion,
            command.SubmitterId,
            command.SubmitterSequence,
            command.GambitId,
            command.Enabled);

    public SetGambitEnabledKernelCommand AsSetGambitEnabled()
    {
        EnsureType(KernelCommandTypeCodes.SetGambitEnabled);
        return new(IssueVersion, _a, _b, SubmitterId, SubmitterSequence);
    }

    public static KernelCommand From(in BlessCellCultureKernelCommand command) =>
        new(
            KernelCommandTypeCodes.BlessCellCulture,
            command.IssueVersion,
            command.SubmitterId,
            command.SubmitterSequence,
            command.CultureId,
            command.PlaneCode,
            command.Q,
            command.R,
            command.Radius,
            x: command.Amount);

    public BlessCellCultureKernelCommand AsBlessCellCulture()
    {
        EnsureType(KernelCommandTypeCodes.BlessCellCulture);
        return new(IssueVersion, _a, _b, _c, _d, _e, _x, SubmitterId, SubmitterSequence);
    }

    public static KernelCommand From(in FavorEntityKernelCommand command) =>
        new(
            KernelCommandTypeCodes.FavorEntity,
            command.IssueVersion,
            command.SubmitterId,
            command.SubmitterSequence,
            command.EntityId,
            command.DurationTicks,
            x: command.MultiplierDelta);

    public FavorEntityKernelCommand AsFavorEntity()
    {
        EnsureType(KernelCommandTypeCodes.FavorEntity);
        return new(IssueVersion, _a, _b, _x, SubmitterId, SubmitterSequence);
    }

    private void EnsureType(int expected)
    {
        if (CommandType != expected)
        {
            throw new global::System.InvalidOperationException();
        }
    }
}
