namespace Engine.Kernel.Schema;

using System.Diagnostics.CodeAnalysis;

using Engine.Kernel;

[SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification = "Primitive command schemas intentionally name the concrete slot storage width.")]
internal enum KernelCommandPrimitiveKind
{
    None = 0,
    Int32 = 1,
    Float32 = 2,
}

internal enum KernelCommandSemanticKind
{
    None = 0,
    RawScalar = 1,
    CommandId = 2,
    CommandType = 3,
    IssueVersion = 4,
    SubmitterId = 5,
    SubmitterSequence = 6,
    EntityId = 7,
    ActionId = 8,
    GeneratorId = 9,
    DetectorId = 11,
    AttributeId = 12,
    RelationshipId = 14,
    RelationshipKindId = 15,
    TagKeyId = 16,
    TagValueId = 17,
    StatusCode = 18,
    ReasonCodeId = 19,
    Coordinate = 20,
    Enabled = 21,
}

internal sealed record KernelCommandSlotSchema(
    int Slot,
    string Name,
    KernelCommandPrimitiveKind PrimitiveKind,
    KernelCommandSemanticKind SemanticKind);

internal sealed record KernelCommandSchema(
    int CommandType,
    string Name,
    int SchemaMinor,
    IReadOnlyList<KernelCommandSlotSchema> Slots);

internal static class KernelCommandSchemaRegistry
{
    private static readonly IReadOnlyList<KernelCommandSchema> RegisteredSchemas =
    [
        Command(KernelCommandTypeCodes.NoOp, "NoOp", 0),
        Command(KernelCommandTypeCodes.SetGeneratorEnabled, "SetGeneratorEnabled", 0,
            Int(0, "generator_id", KernelCommandSemanticKind.GeneratorId),
            Int(1, "enabled", KernelCommandSemanticKind.Enabled)),
    ];

    private static readonly Dictionary<int, KernelCommandSchema> SchemasByType =
        RegisteredSchemas.ToDictionary(schema => schema.CommandType);

    public static IReadOnlyList<KernelCommandSchema> All => RegisteredSchemas;

    public static bool TryGet(int commandType, out KernelCommandSchema schema) =>
        SchemasByType.TryGetValue(commandType, out schema!);

    public static KernelCommandSchema Get(int commandType)
    {
        if (TryGet(commandType, out var schema))
        {
            return schema;
        }

        throw new KeyNotFoundException($"No kernel command schema registered for {commandType}.");
    }

    private static KernelCommandSchema Command(
        int commandType,
        string name,
        int schemaMinor,
        params KernelCommandSlotSchema[] slots)
    {
        EnsureSlotBounds(commandType, slots);
        return new KernelCommandSchema(commandType, name, schemaMinor, slots);
    }

    private static KernelCommandSlotSchema Int(
        int slot,
        string name,
        KernelCommandSemanticKind semanticKind) =>
        new(slot, name, KernelCommandPrimitiveKind.Int32, semanticKind);

    private static void EnsureSlotBounds(int commandType, IReadOnlyList<KernelCommandSlotSchema> slots)
    {
        var intSlots = 0;
        var floatSlots = 0;
        foreach (var slot in slots)
        {
            var maxSlot = slot.PrimitiveKind == KernelCommandPrimitiveKind.Float32
                ? KernelCommand.FloatSlotCount - 1
                : KernelCommand.IntSlotCount - 1;
            if (slot.Slot < 0 || slot.Slot > maxSlot)
            {
                throw new InvalidOperationException(
                    $"{commandType} declares {slot.PrimitiveKind} slot {slot.Slot}, outside 0..{maxSlot}.");
            }

            var slotMask = 1 << slot.Slot;
            if (slot.PrimitiveKind == KernelCommandPrimitiveKind.Float32)
            {
                if ((floatSlots & slotMask) != 0)
                {
                    throw new InvalidOperationException($"{commandType} declares duplicate float slot {slot.Slot}.");
                }

                floatSlots |= slotMask;
            }
            else
            {
                if ((intSlots & slotMask) != 0)
                {
                    throw new InvalidOperationException($"{commandType} declares duplicate int slot {slot.Slot}.");
                }

                intSlots |= slotMask;
            }
        }
    }
}
