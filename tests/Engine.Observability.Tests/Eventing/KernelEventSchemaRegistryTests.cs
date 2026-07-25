namespace Engine.Observability.Tests.Eventing;

using Engine.Eventing;
using Engine.Eventing.Schema;

public sealed class KernelEventSchemaRegistryTests
{
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
            slot => slot.Name == "relationship_id" &&
                    slot.SemanticKind == KernelEventSemanticKind.RelationshipId);
        Assert.Contains(
            schema.Slots,
            slot => slot.Name == "action_id" &&
                    slot.SemanticKind == KernelEventSemanticKind.ActionId);
        Assert.Contains(
            schema.Slots,
            slot => slot.Name == "source_code_id" &&
                    slot.SemanticKind == KernelEventSemanticKind.SourceCodeId);
        Assert.Contains(
            schema.Slots,
            slot => slot.Name == "new_strength_units" &&
                    slot.PrimitiveKind == KernelEventPrimitiveKind.Int32 &&
                    slot.SemanticKind == KernelEventSemanticKind.Strength);
    }
}
