namespace Engine.Eventing.Schema;

using System.Diagnostics.CodeAnalysis;

using Engine.Eventing;

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
    ProcessId = 15,
    GoalId = 16,
    PlanId = 17,
    CardId = 18,
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
}

public sealed record KernelEventSlotSchema(
    int Slot,
    string Name,
    KernelEventPrimitiveKind PrimitiveKind,
    KernelEventSemanticKind SemanticKind);

public sealed record KernelEventSchema(
    KernelEventType EventType,
    string Name,
    int SchemaMinor,
    IReadOnlyList<KernelEventSlotSchema> Slots);

public sealed class KernelEventSchemaValidator : IKernelEventSchemaValidator
{
    public static KernelEventSchemaValidator Instance { get; } = new();

    private KernelEventSchemaValidator()
    {
    }

    public bool IsKnown(KernelEventType eventType) => KernelEventSchemaRegistry.TryGet(eventType, out _);

    public bool IsValid(in KernelEvent kernelEvent) => IsKnown(kernelEvent.EventType);
}

public static class KernelEventSchemaRegistry
{
    private static readonly IReadOnlyList<KernelEventSchema> RegisteredSchemas =
    [
        Event(KernelEventType.EntityCreated, "EntityCreated", 0,
            Int(0, "entity_id", KernelEventSemanticKind.EntityId),
            Int(1, "kind_id", KernelEventSemanticKind.KindId),
            Int(2, "subtype_id", KernelEventSemanticKind.SubtypeId),
            Int(3, "culture_id", KernelEventSemanticKind.CultureId),
            Int(4, "physical_q", KernelEventSemanticKind.Coordinate),
            Int(5, "physical_r", KernelEventSemanticKind.Coordinate),
            Int(6, "semantic_q", KernelEventSemanticKind.Coordinate),
            Int(7, "semantic_r", KernelEventSemanticKind.Coordinate),
            Int(8, "semantic_s", KernelEventSemanticKind.Coordinate)),
        Event(KernelEventType.EntityStatusChanged, "EntityStatusChanged", 0,
            Int(0, "entity_id", KernelEventSemanticKind.EntityId),
            Int(1, "kind_id", KernelEventSemanticKind.KindId),
            Int(2, "subtype_id", KernelEventSemanticKind.SubtypeId),
            Int(3, "previous_status_code", KernelEventSemanticKind.StatusCode),
            Int(4, "current_status_code", KernelEventSemanticKind.StatusCode)),
        Event(KernelEventType.EntityMoved, "EntityMoved", 0,
            Int(0, "entity_id", KernelEventSemanticKind.EntityId),
            Int(1, "plane_code", KernelEventSemanticKind.PlaneCode),
            Int(2, "from_q", KernelEventSemanticKind.Coordinate),
            Int(3, "from_r", KernelEventSemanticKind.Coordinate),
            Int(4, "from_s", KernelEventSemanticKind.Coordinate),
            Int(5, "to_q", KernelEventSemanticKind.Coordinate),
            Int(6, "to_r", KernelEventSemanticKind.Coordinate),
            Int(7, "to_s", KernelEventSemanticKind.Coordinate)),
        Event(KernelEventType.AttributeChanged, "AttributeChanged", 0,
            Int(0, "entity_id", KernelEventSemanticKind.EntityId),
            Int(1, "attribute_id", KernelEventSemanticKind.AttributeId),
            Int(2, "source_code_id", KernelEventSemanticKind.SourceCodeId),
            Float(0, "old_value", KernelEventSemanticKind.RawScalar),
            Float(1, "new_value", KernelEventSemanticKind.RawScalar)),
        Event(KernelEventType.ResourceChanged, "ResourceChanged", 0,
            Int(0, "resource_id", KernelEventSemanticKind.ResourceId),
            Int(1, "q", KernelEventSemanticKind.Coordinate),
            Int(2, "r", KernelEventSemanticKind.Coordinate),
            Int(3, "source_code_id", KernelEventSemanticKind.SourceCodeId),
            Float(0, "old_value", KernelEventSemanticKind.RawScalar),
            Float(1, "new_value", KernelEventSemanticKind.RawScalar)),
        Event(KernelEventType.RelationshipChanged, "RelationshipChanged", 0,
            Int(0, "change_code_id", KernelEventSemanticKind.EventCodeId),
            Int(1, "relationship_kind_id", KernelEventSemanticKind.RelationshipKindId),
            Int(2, "polarity_code", KernelEventSemanticKind.PolarityCode),
            Int(3, "source_entity_id", KernelEventSemanticKind.EntityId),
            Int(4, "target_entity_id", KernelEventSemanticKind.EntityId),
            Int(5, "fact_id_0", KernelEventSemanticKind.RelationshipFactId),
            Int(6, "fact_id_1", KernelEventSemanticKind.RelationshipFactId),
            Int(7, "fact_id_2", KernelEventSemanticKind.RelationshipFactId),
            Int(8, "fact_id_3", KernelEventSemanticKind.RelationshipFactId),
            Int(9, "fact_count", KernelEventSemanticKind.Count),
            Float(0, "previous_strength", KernelEventSemanticKind.Strength),
            Float(1, "new_strength", KernelEventSemanticKind.Strength)),
        Event(KernelEventType.ProcessEvaluated, "ProcessEvaluated", 0,
            Int(0, "entity_id", KernelEventSemanticKind.EntityId),
            Int(1, "kind_id", KernelEventSemanticKind.KindId),
            Int(2, "selected_goal_id", KernelEventSemanticKind.GoalId),
            Int(3, "executed_process_id", KernelEventSemanticKind.ProcessId),
            Int(4, "candidate_count", KernelEventSemanticKind.Count),
            Int(5, "state", KernelEventSemanticKind.StatusCode)),
        Event(KernelEventType.ProcessExecuted, "ProcessExecuted", 0,
            Int(0, "actor_id", KernelEventSemanticKind.EntityId),
            Int(1, "actor_kind_id", KernelEventSemanticKind.KindId),
            Int(2, "process_id", KernelEventSemanticKind.ProcessId),
            Int(3, "affected_entity_id_0", KernelEventSemanticKind.EntityId),
            Int(4, "affected_entity_id_1", KernelEventSemanticKind.EntityId),
            Int(5, "affected_entity_count", KernelEventSemanticKind.Count),
            Int(6, "state", KernelEventSemanticKind.StatusCode)),
        Event(KernelEventType.GoalGenerated, "GoalGenerated", 0,
            Int(0, "entity_id", KernelEventSemanticKind.EntityId),
            Int(1, "kind_id", KernelEventSemanticKind.KindId),
            Int(2, "goal_count", KernelEventSemanticKind.Count),
            Int(3, "actionable_goal_count", KernelEventSemanticKind.Count),
            Int(4, "state", KernelEventSemanticKind.StatusCode)),
        Event(KernelEventType.DegeneracyDetected, "DegeneracyDetected", 0,
            Int(0, "detector_id", KernelEventSemanticKind.DetectorId),
            Int(1, "severity_code", KernelEventSemanticKind.StatusCode),
            Int(2, "reason_code_id", KernelEventSemanticKind.ReasonCodeId)),
        Event(KernelEventType.GeneratorEvaluated, "GeneratorEvaluated", 0,
            Int(0, "generator_id", KernelEventSemanticKind.GeneratorId),
            Int(1, "status_code", KernelEventSemanticKind.StatusCode),
            Int(2, "selected_target_entity_id", KernelEventSemanticKind.EntityId),
            Int(3, "candidate_count", KernelEventSemanticKind.Count),
            Int(4, "failed_fire_count", KernelEventSemanticKind.Count)),
        Event(KernelEventType.GeneratorFired, "GeneratorFired", 0,
            Int(0, "generator_id", KernelEventSemanticKind.GeneratorId),
            Int(1, "target_entity_id", KernelEventSemanticKind.EntityId),
            Int(2, "created_entity_count", KernelEventSemanticKind.Count),
            Int(3, "created_relationship_count", KernelEventSemanticKind.Count)),
        Event(KernelEventType.DecisionExecuted, "DecisionExecuted", 0,
            Int(0, "actor_id", KernelEventSemanticKind.EntityId),
            Int(1, "actor_kind_id", KernelEventSemanticKind.KindId),
            Int(2, "decision_id", KernelEventSemanticKind.ProcessId),
            Int(3, "card_id", KernelEventSemanticKind.CardId),
            Int(4, "state", KernelEventSemanticKind.StatusCode)),
        Event(KernelEventType.CommandApplied, "CommandApplied", 0,
            Int(0, "command_id", KernelEventSemanticKind.CommandId),
            Int(1, "command_type", KernelEventSemanticKind.CommandType),
            Int(2, "issue_version", KernelEventSemanticKind.RawScalar)),
        Event(KernelEventType.CommandRejected, "CommandRejected", 0,
            Int(0, "command_id", KernelEventSemanticKind.CommandId),
            Int(1, "command_type", KernelEventSemanticKind.CommandType),
            Int(2, "issue_version", KernelEventSemanticKind.RawScalar),
            Int(3, "reason_code_id", KernelEventSemanticKind.ReasonCodeId)),
        Event(KernelEventType.TickCompleted, "TickCompleted", 0),
        Event(KernelEventType.GeneratorEnabledChanged, "GeneratorEnabledChanged", 0,
            Int(0, "generator_id", KernelEventSemanticKind.GeneratorId),
            Int(1, "enabled", KernelEventSemanticKind.Enabled)),
        Event(KernelEventType.CellCreated, "CellCreated", 0,
            Int(0, "q", KernelEventSemanticKind.Coordinate),
            Int(1, "r", KernelEventSemanticKind.Coordinate),
            Int(2, "terrain_id", KernelEventSemanticKind.TerrainId),
            Int(3, "owner_entity_id", KernelEventSemanticKind.EntityId)),
        Event(KernelEventType.PlanStarted, "PlanStarted", 0,
            Int(0, "plan_id", KernelEventSemanticKind.PlanId),
            Int(1, "entity_id", KernelEventSemanticKind.EntityId),
            Int(2, "reason_code_id", KernelEventSemanticKind.ReasonCodeId),
            Int(3, "candidate_count", KernelEventSemanticKind.Count)),
        Event(KernelEventType.GoalConsidered, "GoalConsidered", 0,
            Int(0, "plan_id", KernelEventSemanticKind.PlanId),
            Int(1, "entity_id", KernelEventSemanticKind.EntityId),
            Int(2, "goal_id", KernelEventSemanticKind.GoalId),
            Int(3, "candidate_index", KernelEventSemanticKind.Index),
            Float(0, "preliminary_score", KernelEventSemanticKind.Score)),
        Event(KernelEventType.GoalScored, "GoalScored", 0,
            Int(0, "plan_id", KernelEventSemanticKind.PlanId),
            Int(1, "entity_id", KernelEventSemanticKind.EntityId),
            Int(2, "goal_id", KernelEventSemanticKind.GoalId),
            Int(3, "reason_code_id", KernelEventSemanticKind.ReasonCodeId),
            Int(4, "scoring_pass_index", KernelEventSemanticKind.Index),
            Float(0, "score_delta", KernelEventSemanticKind.Delta),
            Float(1, "running_total", KernelEventSemanticKind.Score)),
        Event(KernelEventType.GoalSelected, "GoalSelected", 0,
            Int(0, "plan_id", KernelEventSemanticKind.PlanId),
            Int(1, "entity_id", KernelEventSemanticKind.EntityId),
            Int(2, "goal_id", KernelEventSemanticKind.GoalId),
            Float(0, "final_score", KernelEventSemanticKind.Score)),
        Event(KernelEventType.PlanStepEmitted, "PlanStepEmitted", 0,
            Int(0, "plan_id", KernelEventSemanticKind.PlanId),
            Int(1, "entity_id", KernelEventSemanticKind.EntityId),
            Int(2, "goal_id", KernelEventSemanticKind.GoalId),
            Int(3, "step_index", KernelEventSemanticKind.Index),
            Int(4, "process_id", KernelEventSemanticKind.ProcessId),
            Int(5, "target_entity_id", KernelEventSemanticKind.EntityId),
            Int(6, "relationship_kind_id", KernelEventSemanticKind.RelationshipKindId)),
        Event(KernelEventType.PlanCommitted, "PlanCommitted", 0,
            Int(0, "plan_id", KernelEventSemanticKind.PlanId),
            Int(1, "entity_id", KernelEventSemanticKind.EntityId),
            Int(2, "step_count", KernelEventSemanticKind.Count),
            Int(3, "reason_code_id", KernelEventSemanticKind.ReasonCodeId),
            Float(0, "total_score", KernelEventSemanticKind.Score)),
        Event(KernelEventType.PlanAbandoned, "PlanAbandoned", 0,
            Int(0, "plan_id", KernelEventSemanticKind.PlanId),
            Int(1, "entity_id", KernelEventSemanticKind.EntityId),
            Int(2, "reason_code_id", KernelEventSemanticKind.ReasonCodeId)),
    ];

    private static readonly Dictionary<KernelEventType, KernelEventSchema> SchemasByType =
        RegisteredSchemas.ToDictionary(schema => schema.EventType);

    public static IReadOnlyList<KernelEventSchema> All => RegisteredSchemas;

    public static IKernelEventSchemaValidator Validator => KernelEventSchemaValidator.Instance;

    public static bool TryGet(KernelEventType eventType, out KernelEventSchema schema) =>
        SchemasByType.TryGetValue(eventType, out schema!);

    public static KernelEventSchema Get(KernelEventType eventType)
    {
        if (TryGet(eventType, out var schema))
        {
            return schema;
        }

        throw new KeyNotFoundException($"No kernel event schema registered for {eventType}.");
    }

    private static KernelEventSchema Event(
        KernelEventType eventType,
        string name,
        int schemaMinor,
        params KernelEventSlotSchema[] slots)
    {
        EnsureSlotBounds(eventType, slots);
        return new KernelEventSchema(eventType, name, schemaMinor, slots);
    }

    private static KernelEventSlotSchema Int(
        int slot,
        string name,
        KernelEventSemanticKind semanticKind) =>
        new(slot, name, KernelEventPrimitiveKind.Int32, semanticKind);

    private static KernelEventSlotSchema Float(
        int slot,
        string name,
        KernelEventSemanticKind semanticKind) =>
        new(slot, name, KernelEventPrimitiveKind.Float32, semanticKind);

    private static void EnsureSlotBounds(KernelEventType eventType, IReadOnlyList<KernelEventSlotSchema> slots)
    {
        var intSlots = 0;
        var floatSlots = 0;
        foreach (var slot in slots)
        {
            var maxSlot = slot.PrimitiveKind == KernelEventPrimitiveKind.Float32
                ? KernelEvent.FloatSlotCount - 1
                : KernelEvent.IntSlotCount - 1;
            if (slot.Slot < 0 || slot.Slot > maxSlot)
            {
                throw new InvalidOperationException(
                    $"{eventType} declares {slot.PrimitiveKind} slot {slot.Slot}, outside 0..{maxSlot}.");
            }

            var slotMask = 1 << slot.Slot;
            if (slot.PrimitiveKind == KernelEventPrimitiveKind.Float32)
            {
                if ((floatSlots & slotMask) != 0)
                {
                    throw new InvalidOperationException($"{eventType} declares duplicate float slot {slot.Slot}.");
                }

                floatSlots |= slotMask;
            }
            else
            {
                if ((intSlots & slotMask) != 0)
                {
                    throw new InvalidOperationException($"{eventType} declares duplicate int slot {slot.Slot}.");
                }

                intSlots |= slotMask;
            }
        }
    }
}
