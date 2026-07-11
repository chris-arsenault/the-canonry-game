namespace Engine.Kernel.Driver;

public readonly record struct BlessCellCultureCommand(
    int IssueVersion,
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    int Radius,
    float Amount,
    int SubmitterId = 0,
    int SubmitterSequence = 0);

public readonly record struct FavorEntityCommand(
    int IssueVersion,
    int EntityId,
    int DurationTicks,
    float MultiplierDelta,
    int SubmitterId = 0,
    int SubmitterSequence = 0);
