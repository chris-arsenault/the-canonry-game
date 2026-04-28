namespace Engine.Observability.Tests.Observability;

using Engine.Host;
using Engine.Observability;

using IceRemembers.Seed;

public sealed class ObservabilitySmokeTests
{
    [Fact]
    public void RunLogger_EmitsPerTickBlocks_ForShortRun()
    {
        var logPath = Path.Combine(
            Path.GetTempPath(),
            $"canonry-runlogger-kernel-smoke-{Guid.NewGuid():N}.log");

        try
        {
            using var runtime = CreateRuntime(seed: 42);
            var host = runtime.Host;
            using var logger = new RunLogger(logPath, host.Events, host.Symbols);

            for (var i = 0; i < 5; i++)
            {
                var advanced = host.Time.AdvanceSingleStep(1);
                Assert.Equal(1, advanced);
                logger.OnTickEnded(host.CurrentTick);
            }

            var lines = File.ReadAllLines(logPath);
            Assert.Contains(lines, line => line.StartsWith("# The Canonry kernel event log", StringComparison.Ordinal));
            Assert.Contains(lines, line => line.StartsWith("=== t=1 ===", StringComparison.Ordinal));
            Assert.Contains(lines, line => line.StartsWith("=== t=5 ===", StringComparison.Ordinal));
            Assert.Contains(lines, line => line.Contains(" -- t=5:", StringComparison.Ordinal));
        }
        finally
        {
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }
    }

    [Fact]
    public void RunLogger_EmitsGeneratorLines_ForShortRun()
    {
        var logPath = Path.Combine(
            Path.GetTempPath(),
            $"canonry-generator-runlogger-kernel-smoke-{Guid.NewGuid():N}.log");

        try
        {
            using var runtime = CreateRuntime(seed: 42);
            var host = runtime.Host;
            using var logger = new RunLogger(logPath, host.Events, host.Symbols);

            for (var i = 0; i < 150; i++)
            {
                var advanced = host.Time.AdvanceSingleStep(1);
                Assert.Equal(1, advanced);
                logger.OnTickEnded(host.CurrentTick);
            }

            var lines = File.ReadAllLines(logPath);
            Assert.Contains(
                lines,
                line => line.StartsWith("  gen ", StringComparison.Ordinal));
        }
        finally
        {
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }
    }

    [Fact]
    public void TelemetryRecorder_CapturesStructuredTelemetryAcrossKernelEvents()
    {
        using var runtime = CreateRuntime(seed: 7100);
        var host = runtime.Host;
        using var recorder = new KernelTelemetryRecorder(
            host.Events,
            "test-run",
            host.Symbols,
            seed: 7100);

        recorder.RecordSnapshots(host.CurrentTick);
        for (var i = 0; i < 50; i++)
        {
            var advanced = host.Time.AdvanceSingleStep(1);
            Assert.Equal(1, advanced);
            recorder.RecordSnapshots(host.CurrentTick, includeEntities: host.CurrentTick % 10 == 0);
        }

        recorder.DrainAvailable();

        Assert.NotEmpty(recorder.Batch.Runs);
        Assert.NotEmpty(recorder.Batch.TickSnapshots);
        Assert.NotEmpty(recorder.Batch.KindSnapshots);
        Assert.NotEmpty(recorder.Batch.EntitySnapshots);
        Assert.NotEmpty(recorder.Batch.AttributeEvents);
        Assert.NotEmpty(recorder.Batch.MovementEvents);
        Assert.NotEmpty(recorder.Batch.RelationshipEvents);
    }

    [Fact]
    public void PerformanceRecorder_CapturesTickTimings()
    {
        using var runtime = CreateRuntime(seed: 7101);
        var host = runtime.Host;
        using var recorder = new KernelPerformanceRecorder(
            host.Events,
            "perf-run",
            requestedTicks: 5,
            seed: 7101);

        for (var i = 0; i < 5; i++)
        {
            var started = DateTime.UtcNow;
            var advanced = host.Time.AdvanceSingleStep(1);
            Assert.Equal(1, advanced);
            recorder.RecordTick(host.CurrentTick, DateTime.UtcNow - started);
        }

        recorder.Complete(processedTicks: 5);

        Assert.Single(recorder.Batch.RunPerformance);
        Assert.Equal(5, recorder.Batch.TickPerformance.Count);
    }

    private static KernelRuntime CreateRuntime(int seed) =>
        IceRemembersRuntime.CreateRuntimeFactory().Start(new KernelStartRequest(
            IceRemembersRuntime.DomainName,
            StartupSeedPolicy.Requested(seed),
            KernelFunctionProfile.ManualStep));
}
