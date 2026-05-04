namespace Engine.Kernel;

internal readonly record struct MovementMoveResult(
    bool Moved,
    float StepCost,
    int FromQ,
    int FromR,
    int ToQ,
    int ToR,
    int CauseCode);

internal readonly record struct MovementSemanticMoveResult(
    bool Moved,
    float StepCost,
    int FromQ,
    int FromR,
    int FromS,
    int ToQ,
    int ToR,
    int ToS,
    int CauseCode);
