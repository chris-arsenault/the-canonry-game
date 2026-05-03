namespace Engine.Eventing;

using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class KernelEventLayoutAttribute : Attribute
{
    public KernelEventLayoutAttribute(KernelEventType eventType, int schemaMinor)
    {
        EventType = eventType;
        SchemaMinor = schemaMinor;
    }

    public KernelEventType EventType { get; }

    public int SchemaMinor { get; }
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class KernelEventIntSlotAttribute : Attribute
{
    public KernelEventIntSlotAttribute(int slot, string name, KernelEventSemanticKind semanticKind)
    {
        Slot = slot;
        Name = name;
        SemanticKind = semanticKind;
    }

    public int Slot { get; }

    public string Name { get; }

    public KernelEventSemanticKind SemanticKind { get; }
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class KernelEventFloatSlotAttribute : Attribute
{
    public KernelEventFloatSlotAttribute(int slot, string name, KernelEventSemanticKind semanticKind)
    {
        Slot = slot;
        Name = name;
        SemanticKind = semanticKind;
    }

    public int Slot { get; }

    public string Name { get; }

    public KernelEventSemanticKind SemanticKind { get; }
}

[SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification = "Primitive event schemas intentionally name the concrete slot storage width.")]
public enum KernelEventPrimitiveKind
{
    None = 0,
    Int32 = 1,
    Float32 = 2,
}

[SuppressMessage(
    "Design",
    "CA1027:Mark enums with FlagsAttribute",
    Justification = "Semantic kinds are stable identifiers, not bit flags.")]
public enum KernelEventSemanticKind
{
    None = 0,
    RawScalar = 1,
    EntityId = 2,
    KindId = 3,
    SubtypeId = 4,
    CultureId = 5,
    AttributeId = 6,
    ResourceId = 7,
    RelationshipKindId = 8,
    RelationshipFactId = 9,
    EventCodeId = 10,
    ReasonCodeId = 11,
    SourceCodeId = 12,
    DetectorId = 13,
    GeneratorId = 14,
    BehaviorId = 15,
    GoalId = 16,
    PlanId = 17,
    GambitId = 18,
    Tick = 19,
    Coordinate = 20,
    Count = 21,
    Index = 22,
    StatusCode = 23,
    PlaneCode = 24,
    PolarityCode = 25,
    Strength = 26,
    Score = 27,
    Delta = 28,
    CommandId = 29,
    CommandType = 30,
    Enabled = 31,
    TerrainId = 32,
    NameSeed = 33,
    ResourceAmount = 34,
    TraceSubjectCode = 35,
    ActionFamilyCode = 36,
    ActionId = 37,
    EffectScopeCode = 38,
    EffectValueId = 39,
    EffectKindId = 40,
    SuperregionId = 43,
    ContinentId = 44,
    TraitId = 45,
    ProvenanceInvocationId = 46,
    ProvenanceProducerKindCode = 47,
    ProvenanceTriggerKindCode = 48,
    ProvenanceProducerId = 49,
    StorageCode = 50,
    Capacity = 51,
    Threshold = 52,
}

public sealed record KernelEventLayoutSlotDescriptor(
    int Slot,
    string Name,
    KernelEventPrimitiveKind PrimitiveKind,
    KernelEventSemanticKind SemanticKind,
    string PropertyName);

public sealed record KernelEventLayoutDescriptor(
    KernelEventType EventType,
    string Name,
    string PayloadTypeName,
    int SchemaMinor,
    IReadOnlyList<KernelEventLayoutSlotDescriptor> Slots);
