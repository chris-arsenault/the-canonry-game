namespace Engine.Tests.Observability;

using Engine.Domain.IceRemembers;
using Engine.Eventing;
using Engine.Observability;
using Engine.Projection;
using Engine.Simulation;

public class SimulationTelemetryRecorderTests
{
    [Fact]
    public void Recorder_CapturesStructuredTelemetryAcrossSimulationLayers()
    {
        var sim = SimulationRunner.CreateFromDomain(IceRemembersDefinition.Create(), new Random(7100));
        using var recorder = new SimulationTelemetryRecorder(
            sim,
            "test-run",
            seed: 7100,
            entitySnapshotCadence: 10);

        for (var i = 0; i < 50; i++)
        {
            sim.Tick();
            recorder.OnTickEnded();
        }

        Assert.NotEmpty(recorder.Batch.Runs);
        Assert.True(recorder.Batch.TickSnapshots.Count >= 51);
        Assert.NotEmpty(recorder.Batch.KindSnapshots);
        Assert.NotEmpty(recorder.Batch.EntitySnapshots);
        Assert.NotEmpty(recorder.Batch.MapSnapshots);
        Assert.NotEmpty(recorder.Batch.ResourceSnapshots);

        Assert.NotEmpty(recorder.Batch.ProcessEvents);
        Assert.NotEmpty(recorder.Batch.ProcessCandidates);
        Assert.NotEmpty(recorder.Batch.LocalityOpportunities);
        Assert.NotEmpty(recorder.Batch.PlannerGoals);
        Assert.NotEmpty(recorder.Batch.AttributeEvents);
        Assert.NotEmpty(recorder.Batch.MovementEvents);
        Assert.NotEmpty(recorder.Batch.RelationshipEvents);
        Assert.NotEmpty(recorder.Batch.RelationshipSnapshots);

        Assert.Contains(recorder.Batch.GeneratorEvents, row => row.EventType == "pulse");
        Assert.Equal(
            recorder.Batch.GeneratorEvents
                .Where(row => row.EventType == "fired")
                .Sum(row => row.CreatedEntityCount),
            recorder.Batch.GeneratedEntities.Count);
        Assert.Contains(recorder.Batch.AttributeEvents, row => row.Source == "tick_snapshot");
        Assert.Contains(recorder.Batch.MovementEvents, row => row.Plane == "semantic");
    }

    [Fact]
    public void ProjectionPump_CursorCanBeDisposed()
    {
        var log = new KernelEventLog(capacity: 1, KernelEventOverflowPolicy.RejectWrite);
        var projection = new UiProjectionState();
        var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new DegeneracyDetectedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            DetectorId: 2,
            SeverityCode: 3,
            ReasonCodeId: 4))));
        Assert.False(log.TryWrite(KernelEvent.From(new DegeneracyDetectedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            DetectorId: 2,
            SeverityCode: 3,
            ReasonCodeId: 5))));

        pump.Dispose();

        Assert.True(log.TryWrite(KernelEvent.From(new DegeneracyDetectedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            DetectorId: 2,
            SeverityCode: 3,
            ReasonCodeId: 5))));
    }

    [Fact]
    public void PerformanceRecorder_CapturesTickAndSystemTimings()
    {
        var sim = SimulationRunner.CreateFromDomain(IceRemembersDefinition.Create(), new Random(7101));
        using var recorder = new SimulationPerformanceRecorder(
            sim,
            "perf-run",
            requestedTicks: 5,
            seed: 7101,
            tickSampleCadence: 1,
            systemSampleCadence: 1);

        for (var i = 0; i < 5; i++)
        {
            sim.Tick();
        }

        recorder.Complete();

        Assert.Single(recorder.Batch.RunPerformance);
        Assert.Equal(5, recorder.Batch.TickPerformance.Count);
        Assert.NotEmpty(recorder.Batch.SystemPerformance);
        Assert.Contains(recorder.Batch.SystemPerformance, row => row.SystemName == "processes");
        Assert.All(recorder.Batch.TickPerformance, row => Assert.True(row.ElapsedMilliseconds >= 0d));
    }

    [Fact]
    public void Recorder_CanDisableEntitySnapshotsForPerfRuns()
    {
        var sim = SimulationRunner.CreateFromDomain(IceRemembersDefinition.Create(), new Random(7102));
        using var recorder = new SimulationTelemetryRecorder(
            sim,
            "no-entity-snapshots",
            seed: 7102,
            entitySnapshotCadence: 0);

        for (var i = 0; i < 5; i++)
        {
            sim.Tick();
            recorder.OnTickEnded();
        }

        Assert.Empty(recorder.Batch.EntitySnapshots);
        Assert.NotEmpty(recorder.Batch.TickSnapshots);
    }
}
