namespace Engine.Tests.Observability;

using Engine.Domain.IceRemembers;
using Engine.Messages;
using Engine.Observability;
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
        Assert.NotEmpty(recorder.Batch.PlannerGoals);
        Assert.NotEmpty(recorder.Batch.AttributeEvents);
        Assert.NotEmpty(recorder.Batch.MovementEvents);
        Assert.NotEmpty(recorder.Batch.RelationshipEvents);
        Assert.NotEmpty(recorder.Batch.RelationshipSnapshots);

        Assert.Contains(recorder.Batch.GeneratorEvents, row => row.EventType == "pulse");
        Assert.Contains(recorder.Batch.GeneratorEvents, row => row.EventType == "fired");
        Assert.NotEmpty(recorder.Batch.GeneratedEntities);
        Assert.Contains(recorder.Batch.AttributeEvents, row => row.Source == "tick_snapshot");
        Assert.Contains(recorder.Batch.MovementEvents, row => row.Plane == "semantic");
    }

    [Fact]
    public void MessageBus_SubscriptionCanBeDisposed()
    {
        var bus = new MessageBus();
        var count = 0;
        var subscription = bus.Subscribe(_ => count++);

        bus.Publish(new DegeneracyWarningMessage(1, "test", Engine.Degeneracy.Severity.Warning, "first"));
        subscription.Dispose();
        bus.Publish(new DegeneracyWarningMessage(2, "test", Engine.Degeneracy.Severity.Warning, "second"));

        Assert.Equal(1, count);
    }
}
