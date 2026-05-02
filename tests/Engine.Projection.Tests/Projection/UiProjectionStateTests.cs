namespace Engine.Projection.Tests.Projection;

using Engine.Eventing;
using Engine.Projection;

public sealed class UiProjectionStateTests
{
    [Fact]
    public void Projection_UsesKernelPlaneCodesForEntityMovement()
    {
        var log = new KernelEventLog(capacity: 16);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 10,
            SourceCodeId: 40,
            KindId: 2,
            SubtypeId: 3,
            CultureId: 4,
            PhysicalQ: 0,
            PhysicalR: 0,
            SemanticQ: 0,
            SemanticR: 0,
            SemanticS: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new EntityMovedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 10,
            SourceCodeId: 40,
            PlaneCode: PlaneCodes.Physical,
            FromQ: 0,
            FromR: 0,
            FromS: 0,
            ToQ: 1,
            ToR: 0,
            ToS: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new EntityMovedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            EntityId: 10,
            SourceCodeId: 40,
            PlaneCode: PlaneCodes.Semantic,
            FromQ: 0,
            FromR: 0,
            FromS: 0,
            ToQ: 0,
            ToR: 1,
            ToS: -1))));

        pump.DrainAvailable();

        var entity = projection.Entities[10];
        Assert.Equal(1, entity.PhysicalQ);
        Assert.Equal(0, entity.PhysicalR);
        Assert.Equal(0, entity.SemanticQ);
        Assert.Equal(1, entity.SemanticR);
        Assert.Equal(-1, entity.SemanticS);
    }

    [Fact]
    public void Projection_UsesKernelStatusCodesForEntityStatusChanges()
    {
        var log = new KernelEventLog(capacity: 16);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 10,
            SourceCodeId: 40,
            KindId: 2,
            SubtypeId: 3,
            CultureId: 4,
            PhysicalQ: 0,
            PhysicalR: 0,
            SemanticQ: 0,
            SemanticR: 0,
            SemanticS: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new EntityStatusChangedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 10,
            SourceCodeId: 40,
            KindId: 2,
            SubtypeId: 3,
            PreviousStatusCode: EntityStatusCodes.Active,
            CurrentStatusCode: EntityStatusCodes.Historical))));
        Assert.True(log.TryWrite(KernelEvent.From(new EntityStatusChangedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            EntityId: 10,
            SourceCodeId: 40,
            KindId: 2,
            SubtypeId: 3,
            PreviousStatusCode: EntityStatusCodes.Historical,
            CurrentStatusCode: EntityStatusCodes.Active))));

        pump.DrainAvailable();

        var entity = projection.Entities[10];
        Assert.Equal(EntityStatusCodes.Historical, entity.PreviousStatusCode);
        Assert.Equal(EntityStatusCodes.Active, entity.StatusCode);
    }

    [Fact]
    public void Projection_ReplaysPrimitiveEntityAndMutationEventsIntoUiState()
    {
        var log = new KernelEventLog(capacity: 16);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        WritePrimitiveEntityAndMutationEvents(log);

        pump.DrainAvailable();

        var entity = projection.Entities[10];
        Assert.Equal(2, entity.KindId);
        Assert.Equal(3, entity.SubtypeId);
        Assert.Equal(4, entity.CultureId);
        Assert.Equal(10, entity.NameSeed);
        Assert.Equal(EntityStatusCodes.Active, entity.StatusCode);
        Assert.Equal(7, entity.PhysicalQ);
        Assert.Equal(8, entity.PhysicalR);
        Assert.Equal(14.5f, entity.Attributes[30].NewValue);
        Assert.Contains(70, entity.Traits);
        Assert.Equal(50, entity.LastGambitExecution?.GambitId);
        Assert.True(entity.WasCreatedFromEvent);
        Assert.True(projection.IsComplete);
    }

    [Fact]
    public void Projection_ReplaysRelationshipResourceGeneratorAndDecisionEvents()
    {
        var log = new KernelEventLog(capacity: 16);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        WriteRelationshipResourceGeneratorAndDecisionEvents(log);

        pump.DrainAvailable();

        Assert.Equal(5f, projection.Resources[new UiResourceKey(7, 2, 3)].NewValue);
        var relationship = projection.Relationships[new UiRelationshipKey(20, 21, 10)];
        Assert.True(relationship.IsActive);
        Assert.Equal(2, relationship.FactCount);
        Assert.Equal(1, projection.Generators[40].TotalFired);
        Assert.Equal(40, projection.GeneratorFires[0].GeneratorId);
        Assert.Equal(60, projection.Entities[20].LastGambitExecution?.GambitId);
    }

    private static void WritePrimitiveEntityAndMutationEvents(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 10,
            SourceCodeId: 40,
            KindId: 2,
            SubtypeId: 3,
            CultureId: 4,
            PhysicalQ: 5,
            PhysicalR: 6,
            SemanticQ: 1,
            SemanticR: -1,
            SemanticS: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new EntityMovedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 10,
            SourceCodeId: 40,
            PlaneCode: PlaneCodes.Physical,
            FromQ: 5,
            FromR: 6,
            FromS: 0,
            ToQ: 7,
            ToR: 8,
            ToS: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new AttributeChangedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            EntityId: 10,
            AttributeId: 30,
            SourceCodeId: 40,
            OldValue: 12f,
            NewValue: 14.5f))));
        Assert.True(log.TryWrite(KernelEvent.From(new GambitExecutedKernelEvent(
            Tick: 4,
            WorldVersion: 4,
            ActorId: 10,
            ActorKindId: 2,
            GambitId: 50,
            AffectedEntityId0: 11,
            AffectedEntityId1: 0,
            AffectedEntityCount: 1,
            State: KernelEventState.Succeeded))));
        Assert.True(log.TryWrite(KernelEvent.From(new TraitAssignedKernelEvent(
            Tick: 5,
            WorldVersion: 5,
            EntityId: 10,
            TraitId: 70,
            SourceCodeId: 40))));
    }

    private static void WriteRelationshipResourceGeneratorAndDecisionEvents(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new ResourceChangedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            ResourceId: 7,
            Q: 2,
            R: 3,
            SourceCodeId: 8,
            OldValue: 1f,
            NewValue: 5f))));
        Assert.True(log.TryWrite(KernelEvent.From(new RelationshipChangedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            ChangeCodeId: 9,
            SourceCodeId: 8,
            RelationshipKindId: 10,
            PolarityCode: RelationshipPolarityCodes.Negative,
            SourceEntityId: 20,
            TargetEntityId: 21,
            FactId0: 30,
            FactId1: 31,
            FactId2: 0,
            FactId3: 0,
            FactCount: 2,
            PreviousStrength: 0f,
            NewStrength: 0.75f))));
        Assert.True(log.TryWrite(KernelEvent.From(new GeneratorEvaluatedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            GeneratorId: 40,
            StatusCode: GeneratorStatusCodes.Eligible,
            SelectedTargetEntityId: 20,
            CandidateCount: 6,
            FailedFireCount: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new GeneratorFiredKernelEvent(
            Tick: 4,
            WorldVersion: 4,
            GeneratorId: 40,
            TargetEntityId: 20,
            CreatedEntityCount: 2,
            CreatedRelationshipCount: 1))));
        Assert.True(log.TryWrite(KernelEvent.From(new GambitExecutedKernelEvent(
            Tick: 5,
            WorldVersion: 5,
            ActorId: 20,
            ActorKindId: 2,
            GambitId: 60,
            AffectedEntityId0: 61,
            AffectedEntityId1: 0,
            AffectedEntityCount: 1,
            State: KernelEventState.Succeeded))));
    }

    [Fact]
    public void Projection_MarksStateIncompleteWhenStreamDropsEvents()
    {
        var log = new KernelEventLog(capacity: 1);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 1,
            SourceCodeId: 8,
            KindId: 1,
            SubtypeId: 1,
            CultureId: 1,
            PhysicalQ: 0,
            PhysicalR: 0,
            SemanticQ: 0,
            SemanticR: 0,
            SemanticS: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 2,
            SourceCodeId: 8,
            KindId: 1,
            SubtypeId: 1,
            CultureId: 1,
            PhysicalQ: 0,
            PhysicalR: 0,
            SemanticQ: 0,
            SemanticR: 0,
            SemanticS: 0))));

        pump.DrainAvailable();

        Assert.False(projection.IsComplete);
        Assert.Equal(1, projection.DroppedEvents);
        Assert.False(projection.Entities.ContainsKey(1));
        Assert.True(projection.Entities.ContainsKey(2));
    }

    [Fact]
    public void Projection_ListsKnownFutureMigrationItems()
    {
        Assert.Contains(
            UiProjectionState.FutureMigrationItems,
            item => item.Area == "initial attributes");
        Assert.Contains(
            UiProjectionState.FutureMigrationItems,
            item => item.Area == "map terrain and coverage");
    }
}
