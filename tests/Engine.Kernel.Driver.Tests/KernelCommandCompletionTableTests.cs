namespace Engine.Kernel.Driver.Tests;

using Engine.Kernel.Driver;

public sealed class KernelCommandCompletionTableTests
{
    [Fact]
    public async Task WaitAsyncCompletesWhenTrackedCommandResultArrives()
    {
        var table = new KernelCommandCompletionTable(capacity: 2);
        var result = new KernelCommandResult(3, CommandType: 99, Applied: true, ReasonCodeId: 0);

        table.Track(result.CommandId);
        var wait = table.WaitAsync(result.CommandId);

        Assert.False(wait.IsCompleted);

        table.Complete(result);

        Assert.Equal(result, await wait.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(true));
        Assert.True(table.TryGetResult(result.CommandId, out var stored));
        Assert.Equal(result, stored);
    }

    [Fact]
    public async Task WaitAsyncReturnsCompletedTaskWhenResultAlreadyArrived()
    {
        var table = new KernelCommandCompletionTable(capacity: 2);
        var result = new KernelCommandResult(2, CommandType: 7, Applied: false, ReasonCodeId: 11);

        table.Track(result.CommandId);
        table.Complete(result);

        var waited = await table.WaitAsync(result.CommandId).WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(true);

        Assert.Equal(result, waited);
    }

    [Fact]
    public async Task WaitAsyncFaultsForUntrackedOrOverwrittenCommandId()
    {
        var table = new KernelCommandCompletionTable(capacity: 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => table.WaitAsync(1));

        table.Track(1);
        table.Track(2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => table.WaitAsync(1));
    }

    [Fact]
    public void TrackReusingSlotClearsPreviousResult()
    {
        var table = new KernelCommandCompletionTable(capacity: 1);
        var first = new KernelCommandResult(1, CommandType: 7, Applied: true, ReasonCodeId: 0);

        table.Track(first.CommandId);
        table.Complete(first);
        Assert.True(table.TryGetResult(first.CommandId, out _));

        table.Track(2);

        Assert.False(table.TryGetResult(first.CommandId, out _));
        Assert.False(table.TryGetResult(2, out _));
    }

    [Fact]
    public async Task DefaultCommandHandleHasNoResultAndCannotWait()
    {
        var handle = default(CommandHandle);

        Assert.False(handle.TryGetResult(out _));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handle.WaitAsync().ConfigureAwait(true));
    }
}
