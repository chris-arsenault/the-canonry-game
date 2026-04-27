namespace Engine.Eventing.Schema;

using Engine.Eventing;

internal sealed record KernelEventSlotSchema(
    int Slot,
    string Name,
    KernelEventPrimitiveKind PrimitiveKind,
    KernelEventSemanticKind SemanticKind);

internal sealed record KernelEventSchema(
    KernelEventType EventType,
    string Name,
    int SchemaMinor,
    IReadOnlyList<KernelEventSlotSchema> Slots);

internal sealed class KernelEventSchemaValidator : IKernelEventSchemaValidator
{
    public static KernelEventSchemaValidator Instance { get; } = new();

    private KernelEventSchemaValidator()
    {
    }

    public bool IsKnown(KernelEventType eventType) => KernelEventSchemaRegistry.TryGet(eventType, out _);

    public bool IsValid(in KernelEvent kernelEvent) => IsKnown(kernelEvent.EventType);
}

internal static class KernelEventSchemaRegistry
{
    private static readonly IReadOnlyList<KernelEventSchema> RegisteredSchemas = BuildSchemas();

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

    private static KernelEventSchema[] BuildSchemas()
    {
        var descriptors = KernelEventLayoutDescriptors.All;
        var schemas = new KernelEventSchema[descriptors.Length];
        for (var i = 0; i < descriptors.Length; i++)
        {
            schemas[i] = BuildSchema(descriptors[i]);
        }

        return schemas;
    }

    private static KernelEventSchema BuildSchema(KernelEventLayoutDescriptor descriptor)
    {
        var slots = new KernelEventSlotSchema[descriptor.Slots.Count];
        for (var i = 0; i < descriptor.Slots.Count; i++)
        {
            var slot = descriptor.Slots[i];
            slots[i] = new KernelEventSlotSchema(
                slot.Slot,
                slot.Name,
                slot.PrimitiveKind,
                slot.SemanticKind);
        }

        EnsureSlotBounds(descriptor.EventType, slots);
        return new KernelEventSchema(descriptor.EventType, descriptor.Name, descriptor.SchemaMinor, slots);
    }

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
