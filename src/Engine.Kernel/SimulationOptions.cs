using Engine.Eventing;

namespace Engine.Kernel;

internal sealed record SimulationOptions
{
    public int EntityCapacity { get; init; } = 4096;
    public int RelationshipCapacity { get; init; } = 8192;
    public int AttributeCapacity { get; init; } = 65536;
    public int ResourceCapacity { get; init; } = 8192;
    public int MapCellCapacity { get; init; } = 2048;
    public int SystemCapacity { get; init; } = 32;
    public int EventCapacity { get; init; } = 65536;
    public int EventCursorCapacity { get; init; } = 32;
    public IKernelEventSchemaValidator? EventSchemaValidator { get; init; }
    public int CommandCapacity { get; init; } = 1024;
    public int ProcessCapacity { get; init; } = 512;
    public int ProcessStepCapacity { get; init; } = 4096;
    public int ProcessCooldownCapacity { get; init; } = 8192;
    public int ProcessObjectiveCapacity { get; init; } = 1024;
    public int ProcessProviderSelectorCapacity { get; init; } = 1024;
    public int ProcessSupportRuleCapacity { get; init; } = 1024;
    public int ProcessCommitmentCapacity { get; init; } = 8192;
    public int PlanningOverrideCapacity { get; init; } = 512;
    public int GambitCapacity { get; init; } = 256;
    public int GambitStepCapacity { get; init; } = 2048;
    public int GambitCooldownCapacity { get; init; } = 4096;
    public int GambitObjectiveCapacity { get; init; } = 512;
    public int GambitProviderSelectorCapacity { get; init; } = 512;
    public int GambitSupportRuleCapacity { get; init; } = 512;
    public int GambitCommitmentCapacity { get; init; } = 4096;
    public int GeneratorCapacity { get; init; } = 256;
    public int GeneratorItemCapacity { get; init; } = 4096;
    public int PressureCapacity { get; init; } = 128;
    public int PressureValueCapacity { get; init; } = 8192;
    public int DegeneracyDetectorCapacity { get; init; } = 32;
    public int DegeneracyStateCapacity { get; init; } = 8192;
    public int LifecycleRuleCapacity { get; init; } = 256;
    public int EntityAttributeCapacity { get; init; } = 512;
    public int UpkeepRuleCapacity { get; init; } = 512;
    public int AttributeIdCapacity { get; init; } = 1024;
    public int KindIdCapacity { get; init; } = 256;
    public int TerrainIdCapacity { get; init; } = 128;
    public int RelationshipRuleCapacity { get; init; } = 256;
    public int RelationshipKindIdCapacity { get; init; } = 256;
    public int RelationshipGraphDeltaMergeThreshold { get; init; } = 1024;
    public KernelRuntimeCodes RuntimeCodes { get; init; } = KernelRuntimeCodes.Default;
    public KernelTuningOptions KernelTuning { get; init; } = new();
    public bool TraceEnabled { get; init; }
    public int Seed { get; init; } = 1;
}

internal sealed record KernelTuningOptions
{
    public KernelProgramTuningOptions Programs { get; init; } = new();
}

internal sealed record KernelProgramTuningOptions
{
    public ProcessProgramTuningOptions Processes { get; init; } = new();
    public PlanningProgramTuningOptions Planning { get; init; } = new();
    public PressureProgramTuningOptions Pressures { get; init; } = new();
    public UpkeepProgramTuningOptions Upkeep { get; init; } = new();
    public GeneratorProgramTuningOptions Generators { get; init; } = new();
}

internal sealed record PlanningProgramTuningOptions
{
    public int DefaultPlannerKindCode { get; init; } = PlannerKindCodes.Goap;
    public int OpportunityTargetCapacity { get; init; } = 2;
    public PlanningKindPlannerOverride[] KindPlannerOverrides { get; init; } = [];
    public PlanningEntityPlannerOverride[] EntityPlannerOverrides { get; init; } = [];
}

internal readonly record struct PlanningKindPlannerOverride(int KindId, int PlannerKindCode);

internal readonly record struct PlanningEntityPlannerOverride(int EntityId, int PlannerKindCode);

internal sealed record ProcessProgramTuningOptions
{
    public int ResourceSupplyModelCode { get; init; } = ProcessResourceSupplyModelCodes.AmbientHex;
    public int WorkCommitmentCode { get; init; } = ProcessWorkCommitmentCodes.Off;
}

internal sealed record PressureProgramTuningOptions
{
    public bool Enabled { get; init; }
}

internal sealed record UpkeepProgramTuningOptions
{
    public int AgingModelCode { get; init; } = UpkeepAgingModelCodes.Linear;
}

internal sealed record GeneratorProgramTuningOptions
{
    public bool RetryFireTargets { get; init; } = true;
}
