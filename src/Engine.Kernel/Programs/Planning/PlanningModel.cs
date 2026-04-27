namespace Engine.Kernel;

public readonly record struct PlanningRequest(
    int PlannerKindCode,
    int ActionFamilyCode,
    int ActorEntityId,
    int ActorKindId,
    int Tick,
    float SelectionRoll);

public readonly record struct PlanningActionHandle(
    int ActionFamilyCode,
    int ActionSlot);

public readonly record struct PlanningPlanHandle(
    int Slot);

public readonly record struct PlanningDecision(
    int StatusCode,
    PlanningPlanHandle Plan,
    PlanningActionHandle ImmediateAction,
    int ImmediateTargetEntityId,
    int CandidateCount,
    float Score);

public readonly record struct PlanningPlanHeader(
    int PlannerKindCode,
    int ActionFamilyCode,
    int ActorEntityId,
    int ActorKindId,
    int ObjectiveEffectKey,
    int StepStart,
    int StepCount,
    int ImmediateStepIndex,
    int RootStepIndex,
    int RootTargetEntityId,
    int LocalityKey,
    int CandidateCount,
    float Score,
    int StatusCode);

public readonly record struct PlanningPlanStep(
    PlanningActionHandle Action,
    int TargetEntityId,
    int SatisfiesEffectKey,
    int ParentStepIndex,
    int StepRoleCode,
    float Score);
