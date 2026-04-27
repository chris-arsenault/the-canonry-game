namespace Engine.Observability.Tests.Eventing;

using Engine.Eventing;
using Engine.Eventing.Schema;

public sealed class KernelEventSchemaRegistryTests
{
    [Fact]
    public void Registry_ContainsPlannerTraceEvents()
    {
        var schema = KernelEventSchemaRegistry.Get(KernelEventType.GoalScored);

        Assert.Equal("GoalScored", schema.Name);
        Assert.Contains(
            schema.Slots,
            slot => slot.Name == "plan_id" &&
                    slot.PrimitiveKind == KernelEventPrimitiveKind.Int32 &&
                    slot.SemanticKind == KernelEventSemanticKind.PlanId);
        Assert.Contains(
            schema.Slots,
            slot => slot.Name == "running_total" &&
                    slot.PrimitiveKind == KernelEventPrimitiveKind.Float32 &&
                    slot.SemanticKind == KernelEventSemanticKind.Score);
    }

    [Fact]
    public void Registry_ContainsExistingKernelEvents()
    {
        var schema = KernelEventSchemaRegistry.Get(KernelEventType.RelationshipChanged);

        Assert.Equal("RelationshipChanged", schema.Name);
        Assert.Contains(
            schema.Slots,
            slot => slot.Name == "relationship_kind_id" &&
                    slot.SemanticKind == KernelEventSemanticKind.RelationshipKindId);
        Assert.Contains(
            schema.Slots,
            slot => slot.Name == "source_code_id" &&
                    slot.SemanticKind == KernelEventSemanticKind.SourceCodeId);
        Assert.Contains(
            schema.Slots,
            slot => slot.Name == "new_strength" &&
                    slot.PrimitiveKind == KernelEventPrimitiveKind.Float32);
    }
}
