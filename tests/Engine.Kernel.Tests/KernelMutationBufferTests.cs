namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class KernelMutationBufferTests
{
    [Fact]
    public void WorkerSegmentsPreserveLocalOrderWithoutSharingWriteSlots()
    {
        var buffer = new KernelMutationBuffer(workerCount: 2, capacityPerWorker: 2);
        var first = new KernelMutation(KernelMutationCodes.ModifyAttribute, 11, 3, 0, 0.5f);
        var second = new KernelMutation(KernelMutationCodes.SetEntityTag, 12, 4, 1, 0f);

        Assert.True(buffer.TryAppend(1, first));
        Assert.True(buffer.TryAppend(0, second));

        Assert.Equal(second, buffer.At(0, 0));
        Assert.Equal(first, buffer.At(1, 0));
        Assert.Equal(1, buffer.Count(0));
        Assert.Equal(1, buffer.Count(1));
    }

    [Fact]
    public void OverflowIsStickyUntilResetAndDoesNotOverwriteBufferedMutations()
    {
        var buffer = new KernelMutationBuffer(workerCount: 1, capacityPerWorker: 1);
        var accepted = new KernelMutation(KernelMutationCodes.ModifyAttribute, 7, 2, 0, -1f);
        var rejected = new KernelMutation(KernelMutationCodes.SetEntityTag, 8, 5, 1, 0f);

        Assert.True(buffer.TryAppend(0, accepted));
        Assert.False(buffer.TryAppend(0, rejected));
        Assert.True(buffer.Overflowed(0));
        Assert.Equal(accepted, buffer.At(0, 0));

        buffer.Reset();

        Assert.False(buffer.Overflowed(0));
        Assert.Equal(0, buffer.Count(0));
    }
}
