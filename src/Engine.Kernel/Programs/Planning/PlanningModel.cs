namespace Engine.Kernel;

internal readonly record struct PlanningRequest(
    int PlannerKindCode,
    int ActionFamilyCode,
    int ActorEntityId,
    int ActorKindId,
    int Tick,
    float SelectionRoll);

internal readonly record struct PlanningActionHandle(
    int ActionFamilyCode,
    int ActionSlot);

internal readonly record struct PlanningPlanHandle(
    int Slot);

internal readonly record struct PlanningDecision(
    int StatusCode,
    PlanningPlanHandle Plan,
    PlanningActionHandle ImmediateAction,
    int ImmediateTargetEntityId,
    int CandidateCount,
    float Score);

internal readonly record struct PlanningPlanHeader(
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

internal readonly record struct PlanningPlanStep(
    PlanningActionHandle Action,
    int TargetEntityId,
    int SatisfiesEffectKey,
    int ParentStepIndex,
    int StepRoleCode,
    float Score);
