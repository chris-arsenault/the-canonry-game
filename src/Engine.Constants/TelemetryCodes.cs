namespace Engine.Constants;

public static class TelemetrySymbolKinds
{
    public const int Run = 1001;
    public const int Domain = 1002;
    public const int MapGenerationPhase = 1003;
    public const int MapGenerationDescription = 1004;
    public const int MapGenerationReason = 1005;
    public const int PerformancePhase = 1006;
    public const int PerformanceSystem = 1007;
    public const int PerformanceExportStep = 1008;
    public const int ProcessProvider = 1009;
    public const int ProcessPreconditionDiagnostic = 1010;
    public const int EntityStatus = 1011;
    public const int Plane = 1012;
    public const int RelationshipPolarity = 1013;
    public const int GeneratorStatus = 1014;
    public const int PlannerTraceStatus = 1015;
    public const int KernelEventType = 1016;
}

public static class PlannerTraceStatusCodes
{
    public const int Committed = 1;
    public const int Abandoned = 2;
    public const int Truncated = 3;

    public static int FromStatus(string status) => status switch
    {
        "committed" => Committed,
        "abandoned" => Abandoned,
        "truncated" => Truncated,
        _ => 0,
    };
}
