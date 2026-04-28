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
    public bool TraceEnabled { get; init; }
    public int Seed { get; init; } = 1;
}
