namespace Engine.Kernel.Driver;

public readonly record struct SetGeneratorEnabledCommand(
    int IssueVersion,
    int GeneratorId,
    bool Enabled,
    int SubmitterId = 0,
    int SubmitterSequence = 0);
