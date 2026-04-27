namespace Engine.Observability.Tests.Observability;

using Engine.Eventing;
using Engine.Constants;
using Engine.Observability;
using Engine.Projection;

public sealed class KernelTelemetryRecorderTests
{
    [Fact]
    public void Recorder_ProjectsPrimitiveRowsFromEventLogWithSymbolCatalog()
    {
        var log = new KernelEventLog(capacity: 64);
        var symbols = new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Kind, 1, "settlement", "Settlement")
            .RegisterSymbol(ProjectionSymbolKind.Subtype, 2, "village", "Village")
            .RegisterSymbol(ProjectionSymbolKind.Culture, 3, "north", "North")
            .RegisterSymbol(ProjectionSymbolKind.Attribute, 4, "health", "Health")
            .RegisterSymbol(ProjectionSymbolKind.SourceCode, 5, "process", "Process")
            .RegisterSymbol(ProjectionSymbolKind.Resource, 6, "food", "Food")
            .RegisterSymbol(ProjectionSymbolKind.EventCode, 7, "relationship_added", "Relationship Added")
            .RegisterSymbol(ProjectionSymbolKind.RelationshipKind, 8, "allied_with", "Allied With")
            .RegisterSymbol(ProjectionSymbolKind.RelationshipFact, 9, "nearby", "Nearby")
            .RegisterSymbol(ProjectionSymbolKind.Generator, 10, "found_settlement", "Found Settlement")
            .RegisterSymbol(ProjectionSymbolKind.Process, 11, "forage", "Forage")
            .RegisterEntityName(100, "Hearthhome")
            .RegisterEntityName(101, "New Hall")
            .RegisterEntityName(200, "Waystone");
        using var recorder = new KernelTelemetryRecorder(log, "run-1", symbols, seed: 42, domain: "test");

        Assert.True(log.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 100,
            SourceCodeId: 5,
            KindId: 1,
            SubtypeId: 2,
            CultureId: 3,
            PhysicalQ: 0,
            PhysicalR: 0,
            SemanticQ: 0,
            SemanticR: 1,
            SemanticS: -1))));
        Assert.True(log.TryWrite(KernelEvent.From(new AttributeChangedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            EntityId: 100,
            AttributeId: 4,
            SourceCodeId: 5,
            OldValue: 5f,
            NewValue: 7f))));
        Assert.True(log.TryWrite(KernelEvent.From(new EntityMovedKernelEvent(
            Tick: 2,
            WorldVersion: 3,
            EntityId: 100,
            SourceCodeId: 5,
            PlaneCode: PlaneCodes.Physical,
            FromQ: 0,
            FromR: 0,
            FromS: 0,
            ToQ: 2,
            ToR: -1,
            ToS: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new RelationshipChangedKernelEvent(
            Tick: 2,
            WorldVersion: 4,
            ChangeCodeId: 7,
            SourceCodeId: 5,
            RelationshipKindId: 8,
            PolarityCode: RelationshipPolarityCodes.Neutral,
            SourceEntityId: 100,
            TargetEntityId: 200,
            FactId0: 9,
            FactId1: 0,
            FactId2: 0,
            FactId3: 0,
            FactCount: 1,
            PreviousStrength: 0f,
            NewStrength: 0.75f))));
        Assert.True(log.TryWrite(KernelEvent.From(new ProcessEvaluatedKernelEvent(
            Tick: 2,
            WorldVersion: 5,
            EntityId: 100,
            KindId: 1,
            SelectedGoalId: 30,
            ExecutedProcessId: 11,
            CandidateCount: 4,
            State: KernelEventState.Succeeded))));
        Assert.True(log.TryWrite(KernelEvent.From(new GeneratorFiredKernelEvent(
            Tick: 3,
            WorldVersion: 6,
            GeneratorId: 10,
            TargetEntityId: 200,
            CreatedEntityCount: 1,
            CreatedRelationshipCount: 2))));
        Assert.True(log.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 3,
            WorldVersion: 7,
            EntityId: 101,
            SourceCodeId: 5,
            KindId: 1,
            SubtypeId: 2,
            CultureId: 3,
            PhysicalQ: 3,
            PhysicalR: -1,
            SemanticQ: 1,
            SemanticR: 0,
            SemanticS: -1))));

        recorder.RecordSnapshots(tick: 3);

        Assert.Equal(42, Assert.Single(recorder.Batch.Runs).RunId);

        var created = Assert.Single(recorder.Batch.EntityEvents, row => row.EntityId == 100);
        Assert.Equal((int)KernelEventType.EntityCreated, created.EventTypeCode);
        Assert.Equal(5, created.SourceCodeId);
        Assert.Equal(1, created.KindId);
        Assert.Equal(2, created.SubtypeId);
        Assert.Equal(3, created.CultureId);
        Assert.Equal(EntityStatusCodes.Active, created.CurrentStatusCode);
        Assert.Contains(recorder.Batch.EntityNameSymbols, row =>
            row.EntityId == 100 &&
            row.DisplayName == "Hearthhome");

        var attribute = Assert.Single(recorder.Batch.AttributeEvents);
        Assert.Equal(4, attribute.AttributeId);
        Assert.Equal(2f, attribute.Delta);
        Assert.Equal(5, attribute.SourceCodeId);

        var movement = Assert.Single(recorder.Batch.MovementEvents);
        Assert.Equal(5, movement.SourceCodeId);
        Assert.Equal(PlaneCodes.Physical, movement.PlaneCode);
        Assert.Equal(2, movement.Distance);

        var relationship = Assert.Single(recorder.Batch.RelationshipEvents);
        Assert.Equal(7, relationship.ChangeCodeId);
        Assert.Equal(5, relationship.SourceCodeId);
        Assert.Equal(8, relationship.RelationshipKindId);
        Assert.Equal(RelationshipPolarityCodes.Neutral, relationship.PolarityCode);
        Assert.Equal(9, relationship.FactId0);
        Assert.Equal(1, relationship.FactCount);

        var process = Assert.Single(recorder.Batch.ProcessEvents);
        Assert.Equal(11, process.ExecutedProcessId);
        Assert.Equal(4, process.CandidateCount);

        var generator = Assert.Single(recorder.Batch.GeneratorEvents);
        Assert.Equal(10, generator.GeneratorId);
        Assert.Equal(1, generator.CreatedEntityCount);

        var generated = Assert.Single(recorder.Batch.GeneratedEntities);
        Assert.Equal(101, generated.EntityId);
        Assert.Equal(10, generated.GeneratorId);

        var tick = Assert.Single(recorder.Batch.TickSnapshots);
        Assert.Equal(2, tick.ActiveEntities);
        Assert.Equal(1, tick.TotalGeneratorFires);
        Assert.Equal(1, tick.ActiveRelationships);
    }

    [Fact]
    public void PerformanceRecorder_CountsPrimitiveEventsByTick()
    {
        var log = new KernelEventLog(capacity: 32);
        using var recorder = new KernelPerformanceRecorder(log, "run-1", requestedTicks: 5);

        Assert.True(log.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 100,
            SourceCodeId: 5,
            KindId: 1,
            SubtypeId: 2,
            CultureId: 3,
            PhysicalQ: 0,
            PhysicalR: 0,
            SemanticQ: 0,
            SemanticR: 0,
            SemanticS: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new ProcessEvaluatedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            EntityId: 100,
            KindId: 1,
            SelectedGoalId: 0,
            ExecutedProcessId: 11,
            CandidateCount: 3,
            State: KernelEventState.Succeeded))));
        Assert.True(log.TryWrite(KernelEvent.From(new GeneratorFiredKernelEvent(
            Tick: 1,
            WorldVersion: 3,
            GeneratorId: 10,
            TargetEntityId: 0,
            CreatedEntityCount: 0,
            CreatedRelationshipCount: 0))));

        recorder.RecordTick(1, TimeSpan.FromMilliseconds(4));
        var tick = Assert.Single(recorder.Batch.TickPerformance);

        Assert.Equal(1, tick.ActiveEntities);
        Assert.Equal(1, tick.ProcessEvents);
        Assert.Equal(3, tick.ProcessCandidates);
        Assert.Equal(1, tick.GeneratorEvents);
        Assert.Equal(3, tick.TotalMessages);

        var completed = recorder.Complete(processedTicks: 1);
        var run = Assert.Single(completed.RunPerformance);
        Assert.Equal(5, run.RequestedTicks);
        Assert.Equal(1, run.ProcessedTicks);
    }
}
