namespace Engine.Kernel.Driver.Tests;

using Engine.Kernel;
using Engine.Kernel.Schema;

public sealed class KernelCommandSchemaTests
{
    [Fact]
    public void RegistryContainsSupportedCommandTypes()
    {
        var noOp = KernelCommandSchemaRegistry.Get(KernelCommandTypeCodes.NoOp);
        var generatorToggle = KernelCommandSchemaRegistry.Get(KernelCommandTypeCodes.SetGeneratorEnabled);

        Assert.Equal(KernelCommandTypeCodes.NoOp, noOp.CommandType);
        Assert.Equal("NoOp", noOp.Name);
        Assert.Equal(KernelCommandTypeCodes.SetGeneratorEnabled, generatorToggle.CommandType);
        Assert.Equal("SetGeneratorEnabled", generatorToggle.Name);
    }

    [Fact]
    public void SchemasUseDistinctSlotsInsidePrimitiveStorageBounds()
    {
        foreach (var schema in KernelCommandSchemaRegistry.All)
        {
            foreach (var slot in schema.Slots)
            {
                var maxSlot = slot.PrimitiveKind == KernelCommandPrimitiveKind.Float32
                    ? KernelCommand.FloatSlotCount - 1
                    : KernelCommand.IntSlotCount - 1;
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
    public void NoOpCommandRoundTripsThroughGeneratedPackAndView()
    {
        var command = new NoOpKernelCommand(IssueVersion: 42, SubmitterId: 7, SubmitterSequence: 9);

        var packed = KernelCommand.From(command);
        var unpacked = packed.AsNoOp();

        Assert.Equal(KernelCommandTypeCodes.NoOp, packed.CommandType);
        Assert.Equal(command, unpacked);
    }

    [Fact]
    public void SetGeneratorEnabledCommandRoundTripsThroughGeneratedPackAndView()
    {
        var command = new SetGeneratorEnabledKernelCommand(
            IssueVersion: 42,
            GeneratorId: 91,
            Enabled: 1,
            SubmitterId: 7,
            SubmitterSequence: 9);

        var packed = KernelCommand.From(command);
        var unpacked = packed.AsSetGeneratorEnabled();

        Assert.Equal(KernelCommandTypeCodes.SetGeneratorEnabled, packed.CommandType);
        Assert.Equal(command, unpacked);
    }
}
