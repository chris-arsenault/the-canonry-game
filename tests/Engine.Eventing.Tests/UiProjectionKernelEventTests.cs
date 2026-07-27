namespace Engine.Eventing.Tests;

using Engine.Eventing;
using Engine.Projection;

public sealed class UiProjectionKernelEventTests
{
    [Fact]
    public void ProjectionReadsCurrentTickAndGeneratorEnabledStateFromKernelEvents()
    {
        var log = new KernelEventLog(capacity: 8);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new GeneratorEnabledChangedKernelEvent(
            Tick: 0,
            WorldVersion: 7,
            GeneratorId: 91,
            Enabled: 1))));
        Assert.True(log.TryWrite(KernelEvent.From(
            new WatermarkAdvancedKernelEvent(
            Tick: 3,
            WorldVersion: 11,
            LowWatermark: 3,
            HighWatermark: 4,
            OwnerCount: 64,
            PublicationRevision: 5))));
        Assert.True(log.TryWrite(KernelEvent.From(new GeneratorEnabledChangedKernelEvent(
            Tick: 4,
            WorldVersion: 12,
            GeneratorId: 91,
            Enabled: 0))));

        pump.DrainAvailable();

        Assert.Equal(3, projection.CurrentTick);
        Assert.Equal(12, projection.WorldVersion);
        Assert.False(projection.Generators[91].IsEnabled);
        Assert.Equal(4, projection.Generators[91].LastEnabledChanged?.Tick);
    }
}
