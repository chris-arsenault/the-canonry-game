namespace Engine.Eventing.Tests;

using Engine.Eventing;
using Engine.Eventing.Schema;

public sealed class KernelEventSchemaRegistryTests
{
    [Fact]
    public void RegistryCoversEveryKernelEventTypeExceptNone()
    {
        var registeredTypes = KernelEventSchemaRegistry.All
            .Select(schema => schema.EventType)
            .OrderBy(type => (int)type)
            .ToArray();
        var definedTypes = Enum.GetValues<KernelEventType>()
            .Where(type => type != KernelEventType.None)
            .OrderBy(type => (int)type)
            .ToArray();

        Assert.Equal(definedTypes, registeredTypes);
    }

    [Fact]
    public void SchemasUseDistinctSlotsInsidePrimitiveStorageBounds()
    {
        foreach (var schema in KernelEventSchemaRegistry.All)
        {
            foreach (var slot in schema.Slots)
            {
                var maxSlot = slot.PrimitiveKind == KernelEventPrimitiveKind.Float32
                    ? KernelEvent.FloatSlotCount - 1
                    : KernelEvent.IntSlotCount - 1;
                Assert.InRange(slot.Slot, 0, maxSlot);
            }

            var duplicateSlots = schema.Slots
                .GroupBy(slot => (slot.PrimitiveKind, slot.Slot))
                .Where(group => group.Count() > 1)
                .ToArray();
            Assert.Empty(duplicateSlots);
        }
    }

    [Fact]
    public void PlannerEventsReserveIntSlotZeroForPlanId()
    {
        var plannerTypes = Enum.GetValues<KernelEventType>()
            .Where(type => type is >= KernelEventType.PlanStarted and <= KernelEventType.PlanAbandoned);

        foreach (var eventType in plannerTypes)
        {
            var schema = KernelEventSchemaRegistry.Get(eventType);
            var slot = Assert.Single(schema.Slots, candidate =>
                candidate.PrimitiveKind == KernelEventPrimitiveKind.Int32 &&
                candidate.Slot == 0);

            Assert.Equal(KernelEventSemanticKind.PlanId, slot.SemanticKind);
        }
    }
}
