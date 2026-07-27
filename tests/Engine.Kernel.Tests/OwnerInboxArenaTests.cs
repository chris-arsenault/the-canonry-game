namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class OwnerInboxArenaTests
{
    [Fact]
    public void SealProducesBoundedOwnerContiguousRanges()
    {
        var inbox = new OwnerInboxArena<int>(
            capacity: 5,
            workerCount: 2,
            ownerCount: 3);

        inbox.Add(workerIndex: 1, ownerIndex: 2, value: 20);
        inbox.Add(workerIndex: 0, ownerIndex: 0, value: 1);
        inbox.Add(workerIndex: 1, ownerIndex: 1, value: 10);
        inbox.Add(workerIndex: 0, ownerIndex: 2, value: 21);
        inbox.Add(workerIndex: 1, ownerIndex: 0, value: 2);

        Assert.Equal(5, inbox.Seal());
        Assert.Equal(0, inbox.OwnerStart(0));
        Assert.Equal(2, inbox.OwnerLimit(0));
        Assert.Equal(2, inbox.OwnerStart(1));
        Assert.Equal(3, inbox.OwnerLimit(1));
        Assert.Equal(3, inbox.OwnerStart(2));
        Assert.Equal(5, inbox.OwnerLimit(2));
        Assert.Equal([1, 2], inbox.Values[0..2]);
        Assert.Equal(10, inbox.Values[2]);
        Assert.Equal([20, 21], inbox.Values[3..5]);
    }

    [Fact]
    public void SealRejectsLogicalCapacityOverflow()
    {
        var inbox = new OwnerInboxArena<int>(
            capacity: 2,
            workerCount: 1,
            ownerCount: 2);

        inbox.Add(workerIndex: 0, ownerIndex: 1, value: 1);
        inbox.Add(workerIndex: 0, ownerIndex: 1, value: 2);
        inbox.Add(workerIndex: 0, ownerIndex: 1, value: 3);

        Assert.Throws<InvalidOperationException>(() => inbox.Seal());
    }

    [Fact]
    public void ClearReusesAllChannelsWithoutRetainingMessages()
    {
        var inbox = new OwnerInboxArena<int>(
            capacity: 2,
            workerCount: 2,
            ownerCount: 2);
        inbox.Add(workerIndex: 1, ownerIndex: 1, value: 8);
        Assert.Equal(1, inbox.Seal());

        inbox.Clear();
        inbox.Add(workerIndex: 0, ownerIndex: 0, value: 3);

        Assert.Equal(1, inbox.Count);
        Assert.Equal(1, inbox.Seal());
        Assert.Equal(0, inbox.OwnerStart(0));
        Assert.Equal(1, inbox.OwnerLimit(0));
        Assert.Equal(3, inbox.Values[0]);
        Assert.Equal(1, inbox.OwnerStart(1));
        Assert.Equal(1, inbox.OwnerLimit(1));
    }
}
