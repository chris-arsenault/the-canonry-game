namespace Engine.Kernel;

public static class PlannerKindCodes
{
    public const int Goap = 1;
    public const int Htn = 2;
}

public static class ActionFamilyCodes
{
    public const int Process = 1;
    public const int Gambit = 2;
}

public static class PlanningStatusCodes
{
    public const int None = 0;
    public const int Succeeded = 1;
    public const int NoCandidates = 2;
    public const int NoWeight = 3;
    public const int UnsupportedPlanner = 4;
    public const int UnsupportedActionFamily = 5;
}

public static class PlanningStepRoleCodes
{
    public const int Immediate = 1;
    public const int Root = 2;
    public const int Blocker = 3;
    public const int Support = 4;
    public const int Decomposition = 5;
}
