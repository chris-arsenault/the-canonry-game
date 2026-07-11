namespace Engine.Kernel.Driver;

public readonly record struct SetGambitEnabledCommand(
    int IssueVersion,
    int GambitId,
    bool Enabled,
    int SubmitterId = 0,
    int SubmitterSequence = 0);
