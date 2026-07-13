namespace Engine.Kernel;

internal readonly record struct MovementMovePlan(
    bool HasStep,
    bool Unreachable,
    float StepCost,
    int PlaneCode,
    int FromQ,
    int FromR,
    int FromZ,
    int FromS,
    int ToQ,
    int ToR,
    int ToZ,
    int ToS,
    int CauseCode);
