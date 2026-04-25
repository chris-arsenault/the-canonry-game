namespace Engine.Tests.Eventing;

using Engine.Eventing;

public sealed class KernelEventConsumerWorkerTests
{
    [Fact]
    public async Task WorkersMaintainIndependentAsyncReadHeads()
    {
        var stream = new KernelEventLog(capacity: 8);
        var firstConsumer = new RecordingKernelEventConsumer();
        var secondConsumer = new RecordingKernelEventConsumer();

        var firstWorker = new KernelEventConsumerWorker(
            stream,
            firstConsumer,
            KernelEventCursorStart.Latest,
            batchSize: 2,
            idleDelay: TimeSpan.FromMilliseconds(1));
        var secondWorker = new KernelEventConsumerWorker(
            stream,
            secondConsumer,
            KernelEventCursorStart.Latest,
            batchSize: 2,
            idleDelay: TimeSpan.FromMilliseconds(1));

        try
        {
            firstWorker.Start();
            secondWorker.Start();

            for (var i = 0; i < 3; i++)
            {
                Assert.True(stream.TryWrite(KernelEvent.From(new GoalGeneratedKernelEvent(
                    Tick: i + 1,
                    WorldVersion: i + 10,
                    EntityId: 1,
                    KindId: 2,
                    GoalCount: i + 3,
                    ActionableGoalCount: i,
                    State: KernelEventState.Succeeded))));
            }

            await WaitForCount(firstConsumer, 3).ConfigureAwait(true);
            await WaitForCount(secondConsumer, 3).ConfigureAwait(true);

            Assert.Equal([0L, 1L, 2L], firstConsumer.Sequences);
            Assert.Equal([0L, 1L, 2L], secondConsumer.Sequences);
        }
        finally
        {
            await firstWorker.DisposeAsync().ConfigureAwait(true);
            await secondWorker.DisposeAsync().ConfigureAwait(true);
        }
    }

    private static async Task WaitForCount(RecordingKernelEventConsumer consumer, int expectedCount)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);
        while (DateTime.UtcNow < deadline)
        {
            if (consumer.Count >= expectedCount)
            {
                return;
            }

            await Task.Delay(10).ConfigureAwait(true);
        }

        throw new TimeoutException($"Timed out waiting for {expectedCount} kernel events.");
    }

    private sealed class RecordingKernelEventConsumer : IKernelEventConsumer
    {
        private readonly object _gate = new();
        private readonly List<SequencedKernelEvent> _events = [];

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _events.Count;
                }
            }
        }

        public IReadOnlyList<long> Sequences
        {
            get
            {
                lock (_gate)
                {
                    return _events.Select(kernelEvent => kernelEvent.Sequence).ToArray();
                }
            }
        }

        public ValueTask ConsumeAsync(SequencedKernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _events.Add(kernelEvent);
            }

            return ValueTask.CompletedTask;
        }
    }
}
