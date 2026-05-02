namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class EpochStampedArrayTests
{
    [Fact]
    public void EpochStampedArray_ReturnsFallbackUntilIndexIsSetInCurrentEpoch()
    {
        var values = new EpochStampedArray<int>(capacity: 3);

        values.BeginEpoch();
        values.Set(1, 42);

        Assert.Equal(3, values.Length);
        Assert.Equal(-1, values.GetOrDefault(0, fallback: -1));
        Assert.Equal(42, values.GetOrDefault(1, fallback: -1));

        values.BeginEpoch();

        Assert.Equal(-1, values.GetOrDefault(1, fallback: -1));
    }

    [Fact]
    public void EpochStampedArray_OverwritesOnlyTheSelectedIndex()
    {
        var values = new EpochStampedArray<string>(capacity: 2);

        values.BeginEpoch();
        values.Set(0, "first");
        values.Set(1, "second");
        values.Set(0, "updated");

        Assert.Equal("updated", values.GetOrDefault(0, fallback: "missing"));
        Assert.Equal("second", values.GetOrDefault(1, fallback: "missing"));
    }

    [Fact]
    public void EpochStampedArray_RejectsNonPositiveCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EpochStampedArray<int>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EpochStampedArray<int>(-1));
    }

    [Fact]
    public void EpochStampSet_TracksMembershipOnlyWithinCurrentEpoch()
    {
        var set = new EpochStampSet(capacity: 3);

        set.BeginEpoch();
        set.Add(2);

        Assert.Equal(3, set.Length);
        Assert.False(set.Contains(1));
        Assert.True(set.Contains(2));

        set.BeginEpoch();

        Assert.False(set.Contains(2));
    }

    [Fact]
    public void EpochStampSet_RemoveOnlyClearsCurrentEpochMembership()
    {
        var set = new EpochStampSet(capacity: 2);

        set.BeginEpoch();
        set.Add(1);
        set.Remove(1);

        Assert.False(set.Contains(1));

        set.Add(1);
        set.BeginEpoch();
        set.Remove(1);

        Assert.False(set.Contains(1));
    }

    [Fact]
    public void EpochStampSet_RejectsNonPositiveCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EpochStampSet(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EpochStampSet(-1));
    }
}
