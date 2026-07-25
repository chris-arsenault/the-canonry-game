namespace Engine.Kernel;

internal readonly record struct MovementMoveResult(
    bool Moved,
    FixedScalar StepCost,
    int PlaneCode,
    int FromQ,
    int FromR,
    int FromS,
    int ToQ,
    int ToR,
    int ToS,
    int CauseCode);
