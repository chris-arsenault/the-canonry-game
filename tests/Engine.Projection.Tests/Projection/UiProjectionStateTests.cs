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
            SemanticS: 0,
            NameSeed: 10))));
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
            ToS: 0,
            FromZ: 0,
            ToZ: 0))));
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
            ToS: -1,
            FromZ: 0,
            ToZ: 0))));

        pump.DrainAvailable();

        var entity = projection.Entities[10];
        Assert.Equal(1, entity.PhysicalQ);
        Assert.Equal(0, entity.PhysicalR);
        Assert.Equal(0, entity.SemanticQ);
        Assert.Equal(1, entity.SemanticR);
        Assert.Equal(-1, entity.SemanticS);
    }

    [Fact]
    public void Projection_RetirementIsFinalAndLaterMutationEventsDoNotRecreateEntity()
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
            SemanticS: 0,
            NameSeed: 10))));
        Assert.True(log.TryWrite(KernelEvent.From(new EntityStatusChangedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 10,
            SourceCodeId: 40,
            KindId: 2,
            SubtypeId: 3,
            PreviousStatusCode: EntityStatusCodes.Active,
            CurrentStatusCode: EntityStatusCodes.Historical))));
        Assert.True(log.TryWrite(KernelEvent.From(new AttributeChangedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            EntityId: 10,
            SourceCodeId: 40,
            AttributeId: 5,
            OldValueUnits: KernelFixedPoint.AttributeUnits(1f),
            NewValueUnits: KernelFixedPoint.AttributeUnits(2f)))));

        pump.DrainAvailable();

        Assert.DoesNotContain(10, projection.Entities.Keys);
        Assert.Equal(0, projection.ActiveEntityCount);
    }

    [Fact]
    public void Projection_UpdatesEntityCultureChanges()
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
            SemanticS: 0,
            NameSeed: 10))));
        Assert.True(log.TryWrite(KernelEvent.From(new EntityCultureChangedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 10,
            SourceCodeId: 40,
            KindId: 2,
            SubtypeId: 3,
            PreviousCultureId: 4,
            CurrentCultureId: 5))));

        pump.DrainAvailable();

        var entity = projection.Entities[10];
        Assert.Equal(5, entity.CultureId);
        Assert.Equal(2, entity.LastUpdatedTick);
        Assert.Equal(2, entity.WorldVersion);
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
    public void Projection_ReplaysRelationshipGeneratorAndDecisionEvents()
    {
        var log = new KernelEventLog(capacity: 16);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        WriteRelationshipGeneratorAndDecisionEvents(log);

        pump.DrainAvailable();

        var relationship = projection.Relationships[new UiRelationshipKey(20, 21, 10)];
        Assert.True(relationship.IsActive);
        Assert.Equal(1, projection.Generators[40].TotalFired);
        Assert.Equal(40, projection.GeneratorFires[0].GeneratorId);
        Assert.Equal(60, projection.Entities[20].LastGambitExecution?.GambitId);
    }

    [Fact]
    public void Projection_ReplaysPersistentMinorActorLifecycleAndWork()
    {
        var log = new KernelEventLog(capacity: 16);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(MinorActorCreatedEvent()));
        Assert.True(log.TryWrite(MinorActorAssignedEvent()));
        Assert.True(log.TryWrite(MinorActorMovedEvent()));
        Assert.True(log.TryWrite(MinorActorWorkedEvent()));
        WriteWatermarkAdvanced(log, tick: 7, worldVersion: 7);

        pump.DrainAvailable();

        var actor = Assert.Single(projection.MinorActors).Value;
        var activity = Assert.Single(projection.MinorActorWork);
        Assert.Equal(4, projection.MinorActorVersion);
        Assert.Equal(7, projection.CurrentTick);
        Assert.Equal(-2, actor.Q);
        Assert.Equal(MinorActorPostureCodes.Working, actor.PostureCode);
        Assert.Equal(12, activity.CultureId);
        Assert.Equal(MinorActorActionCodes.BuildCulture, activity.ActionCode);
        Assert.Equal(PlaneCodes.Physical, activity.PlaneCode);
        Assert.Equal(-2, activity.Q);
        Assert.Equal(1.5f, activity.Delta);

        Assert.True(log.TryWrite(MinorActorRetiredEvent()));
        pump.DrainAvailable();

        Assert.Empty(projection.MinorActors);
        Assert.Equal(5, projection.MinorActorVersion);
    }

    private static KernelEvent MinorActorCreatedEvent() =>
        KernelEvent.From(new MinorActorCreatedKernelEvent(
            Tick: 7,
            WorldVersion: 8,
            ActorOccurrence: 9,
            SourceEntityId: 10,
            SourceRuleId: 11,
            BindingRelationshipId: 0,
            CultureId: 12,
            RoleCode: MinorActorRoleCodes.CulturalFront,
            PlaneCode: PlaneCodes.Physical,
            Q: -3,
            R: 3,
            PostureCode: MinorActorPostureCodes.Traveling,
            StrengthUnits: FixedScalar.FromSingle(3.25f).Units));

    private static KernelEvent MinorActorAssignedEvent() =>
        KernelEvent.From(new MinorActorAssignedKernelEvent(
            Tick: 7,
            WorldVersion: 8,
            ActorOccurrence: 9,
            SourceEntityId: 10,
            RoleCode: MinorActorRoleCodes.CulturalFront,
            PlaneCode: PlaneCodes.Physical,
            PreviousTargetQ: -3,
            PreviousTargetR: 3,
            TargetQ: -2,
            TargetR: 3,
            TargetingCode: MinorActorTargetingCodes.ExtendBorder,
            PostureCode: MinorActorPostureCodes.Traveling,
            NextReplanTick: 19,
            CommitmentUntilTick: 15,
            StrengthUnits: FixedScalar.FromSingle(3.25f).Units));

    private static KernelEvent MinorActorMovedEvent() =>
        KernelEvent.From(new MinorActorMovedKernelEvent(
            Tick: 7,
            WorldVersion: 9,
            ActorOccurrence: 9,
            SourceEntityId: 10,
            CultureId: 12,
            RoleCode: MinorActorRoleCodes.CulturalFront,
            PlaneCode: PlaneCodes.Physical,
            FromQ: -3,
            FromR: 3,
            ToQ: -2,
            ToR: 3,
            TargetQ: -2,
            TargetR: 3,
            PostureCode: MinorActorPostureCodes.Working,
            StrengthUnits: FixedScalar.FromSingle(3.25f).Units));

    private static KernelEvent MinorActorWorkedEvent() =>
        KernelEvent.From(new MinorActorWorkedKernelEvent(
            Tick: 7,
            WorldVersion: 10,
            ActorOccurrence: 9,
            SourceEntityId: 10,
            SourceRuleId: 11,
            SourceWorkTier: 2,
            EffectiveRadius: 5,
            CultureId: 12,
            RoleCode: MinorActorRoleCodes.CulturalFront,
            ActionCode: MinorActorActionCodes.BuildCulture,
            TargetingCode: MinorActorTargetingCodes.ExtendBorder,
            PlaneCode: PlaneCodes.Physical,
            Q: -2,
            R: 3,
            CellKindCode: CultureFieldCellKindCodes.Border,
            LocalRawMassUnits: FixedScalar.FromSingle(14.5f).Units,
            ConnectedRawMassUnits: FixedScalar.FromSingle(21.5f).Units,
            StrengthUnits: FixedScalar.FromSingle(3.25f).Units,
            DeltaUnits: FixedScalar.FromSingle(1.5f).Units));

    private static KernelEvent MinorActorRetiredEvent() =>
        KernelEvent.From(new MinorActorRetiredKernelEvent(
            Tick: 8,
            WorldVersion: 11,
            ActorOccurrence: 9,
            SourceEntityId: 10,
            SourceRuleId: 11,
            BindingRelationshipId: 0,
            CultureId: 12,
            RoleCode: MinorActorRoleCodes.CulturalFront,
            PlaneCode: PlaneCodes.Physical,
            Q: -2,
            R: 3,
            ReasonCode: MinorActorRetirementReasonCodes.NoAssignment,
            PostureCode: MinorActorPostureCodes.Working,
            StrengthUnits: FixedScalar.FromSingle(3.25f).Units));

    [Fact]
    public void Projection_AttributesGeneratedEntitiesByProvenanceInvocation()
    {
        var log = new KernelEventLog(capacity: 16);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        WriteGeneratedEntity(log, invocationId: 17, tick: 4, entityId: 30, q: 0, r: 0);
        WriteGeneratorFire(log, invocationId: 17, tick: 6, generatorId: 90, targetEntityId: 12);
        WriteGeneratorInvocation(log, invocationId: 17, tick: 6, generatorId: 90, targetEntityId: 12);
        WriteGeneratorFire(log, invocationId: 18, tick: 5, generatorId: 91, targetEntityId: 13);
        WriteGeneratedEntity(log, invocationId: 18, tick: 7, entityId: 31, q: 1, r: 0);
        WriteGeneratorInvocation(log, invocationId: 18, tick: 7, generatorId: 91, targetEntityId: 13);

        pump.DrainAvailable();

        Assert.Equal(90, projection.Entities[30].CreatedByGeneratorId);
        Assert.Equal(6, projection.Entities[30].CreatedByGeneratorTick);
        Assert.Equal(12, projection.Entities[30].CreatedByGeneratorTargetEntityId);
        Assert.Equal([30], projection.GeneratorFires[0].CreatedEntityIds);
        Assert.Equal(91, projection.Entities[31].CreatedByGeneratorId);
        Assert.Equal([31], projection.GeneratorFires[1].CreatedEntityIds);
    }

    private static void WriteGeneratedEntity(
        KernelEventLog log,
        int invocationId,
        int tick,
        int entityId,
        int q,
        int r)
    {
        Assert.True(log.TryWrite(WithProvenance(
            KernelEvent.From(new EntityCreatedKernelEvent(
                Tick: tick,
                WorldVersion: tick,
                EntityId: entityId,
                SourceCodeId: 40,
                KindId: 2,
                SubtypeId: 3,
                CultureId: 4,
                PhysicalQ: q,
                PhysicalR: r,
                SemanticQ: q,
                SemanticR: r,
                SemanticS: -q - r,
                NameSeed: entityId)),
            invocationId)));
    }

    private static void WriteGeneratorFire(
        KernelEventLog log,
        int invocationId,
        int tick,
        int generatorId,
        int targetEntityId)
    {
        Assert.True(log.TryWrite(WithProvenance(
            KernelEvent.From(new GeneratorFiredKernelEvent(
                Tick: tick,
                WorldVersion: tick,
                GeneratorId: generatorId,
                TargetEntityId: targetEntityId,
                CreatedEntityCount: 1,
                CreatedRelationshipCount: 0)),
            invocationId)));
    }

    private static void WriteGeneratorInvocation(
        KernelEventLog log,
        int invocationId,
        int tick,
        int generatorId,
        int targetEntityId)
    {
        Assert.True(log.TryWrite(WithProvenance(
            KernelEvent.From(new ProvenanceInvocationRecordedKernelEvent(
                Tick: tick,
                WorldVersion: tick,
                InvocationId: invocationId,
                ProducerKindCode: ProvenanceProducerKindCodes.Generator,
                ProducerId: generatorId,
                PrimaryEntityId: targetEntityId,
                SecondaryEntityId: 0,
                ParentInvocationId: 0,
                TriggerKindCode: 0,
                TriggerId: 0,
                OutputCount: 1)),
            invocationId)));
    }

    [Fact]
    public void Projection_SplitsUiVersionsByConsumerSurface()
    {
        var log = new KernelEventLog(capacity: 16);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        WriteVersionTestEntityCreated(log);
        pump.DrainAvailable();

        Assert.Equal(1, projection.ActiveEntityCount);
        Assert.Equal(1, projection.TopBarVersion);
        Assert.Equal(1, projection.EntityMapVersion);
        Assert.Equal(1, projection.EntityBrowserVersion);
        AssertBrowserInvalidation(projection, structureVersion: 1, rowVersion: 1, 10);
        Assert.Equal(1, projection.EntityDetailVersion);

        var browserVersion = projection.EntityBrowserVersion;
        var browserStructureVersion = projection.EntityBrowserStructureVersion;
        var browserRowVersion = projection.EntityBrowserRowVersion;
        projection.ClearDirtyEntityBrowserRows();
        WriteVersionTestEntityMoved(log);
        pump.DrainAvailable();

        Assert.Equal(browserVersion, projection.EntityBrowserVersion);
        AssertBrowserInvalidation(projection, browserStructureVersion, browserRowVersion);
        Assert.Equal(2, projection.EntityMapVersion);
        Assert.Equal(2, projection.EntityDetailVersion);

        WriteVersionTestRelationshipChanged(log);
        pump.DrainAvailable();

        Assert.Equal(browserVersion, projection.EntityBrowserVersion);
        Assert.Equal(1, projection.RelationshipVersion);
        Assert.Equal(3, projection.EntityDetailVersion);

        WriteVersionTestGambitExecuted(log);
        pump.DrainAvailable();

        browserVersion++;
        browserRowVersion++;
        Assert.Equal(browserVersion, projection.EntityBrowserVersion);
        AssertBrowserInvalidation(projection, browserStructureVersion, browserRowVersion, 10);
        Assert.Equal(4, projection.EntityDetailVersion);

        WriteVersionTestWatermarkAdvanced(log);
        pump.DrainAvailable();

        Assert.Equal(browserVersion, projection.EntityBrowserVersion);
        Assert.Equal(2, projection.TopBarVersion);
    }

    [Fact]
    public void Projection_IndexesActiveOccupantsAndDirtyEntityHexes()
    {
        var log = new KernelEventLog(capacity: 16);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        WriteEntityCreated(log, entityId: 10, physicalQ: 0, physicalR: 0, semanticQ: 1, semanticR: -1);
        pump.DrainAvailable();

        Assert.Equal([10], projection.ActiveOccupants(PlaneCodes.Physical, new HexCoord(0, 0)).ToArray());
        Assert.Equal([10], projection.ActiveOccupants(PlaneCodes.Semantic, new HexCoord(1, -1)).ToArray());
        projection.ClearDirtyEntityHexes(PlaneCodes.Physical);

        WriteEntityMoved(log, entityId: 10, fromQ: 0, fromR: 0, toQ: 1, toR: 0);
        pump.DrainAvailable();

        Assert.Empty(projection.ActiveOccupants(PlaneCodes.Physical, new HexCoord(0, 0)));
        Assert.Equal([10], projection.ActiveOccupants(PlaneCodes.Physical, new HexCoord(1, 0)).ToArray());
        Assert.Equal([new HexCoord(0, 0), new HexCoord(1, 0)], projection.DirtyEntityHexes(PlaneCodes.Physical).OrderBy(coord => coord.Q).ThenBy(coord => coord.R).ToArray());

        WriteEntityStatusChanged(log, entityId: 10, EntityStatusCodes.Active, EntityStatusCodes.Historical);
        pump.DrainAvailable();

        Assert.Empty(projection.ActiveOccupants(PlaneCodes.Physical, new HexCoord(1, 0)));
        Assert.Empty(projection.ActiveOccupants(PlaneCodes.Semantic, new HexCoord(1, -1)));
    }

    [Fact]
    public void Projection_IndexesActiveRelationshipsOnly()
    {
        var log = new KernelEventLog(capacity: 16);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        WriteEntityCreated(log, entityId: 10, physicalQ: 0, physicalR: 0, semanticQ: 0, semanticR: 0);
        WriteEntityCreated(log, entityId: 11, physicalQ: 1, physicalR: 0, semanticQ: 1, semanticR: -1);
        WriteRelationship(log, sourceEntityId: 10, targetEntityId: 11, strength: 1f);
        pump.DrainAvailable();

        Assert.Equal([11], projection.ActiveOutgoingRelationships(10).Select(rel => rel.Key.TargetEntityId).ToArray());
        Assert.Equal([10], projection.ActiveIncomingRelationships(11).Select(rel => rel.Key.SourceEntityId).ToArray());

        WriteEntityStatusChanged(log, entityId: 11, EntityStatusCodes.Active, EntityStatusCodes.Historical);
        pump.DrainAvailable();

        Assert.Empty(projection.ActiveOutgoingRelationships(10));
        Assert.Empty(projection.ActiveIncomingRelationships(11));
    }

    private static void WriteVersionTestEntityCreated(KernelEventLog log)
    {
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
            SemanticS: 0,
            NameSeed: 10))));
    }

    private static void WriteVersionTestEntityMoved(KernelEventLog log)
    {
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
            ToS: 0,
            FromZ: 0,
            ToZ: 0))));
    }

    private static void WriteVersionTestRelationshipChanged(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new RelationshipChangedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            ChangeCodeId: 9,
            SourceCodeId: 8,
            RelationshipKindId: 10,
            PolarityCode: RelationshipPolarityCodes.Positive,
            SourceEntityId: 10,
            TargetEntityId: 11,
            RelationshipId: 12,
            PreviousStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0f),
            NewStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(1f),
            ActionId: 0,
            IsActive: 1))));
    }

    private static void AssertBrowserInvalidation(
        UiProjectionState projection,
        long structureVersion,
        long rowVersion,
        params int[] dirtyEntityIds)
    {
        Assert.Equal(structureVersion, projection.EntityBrowserStructureVersion);
        Assert.Equal(rowVersion, projection.EntityBrowserRowVersion);
        Assert.Equal(dirtyEntityIds, projection.DirtyEntityBrowserRows.ToArray());
    }

    private static void WriteVersionTestGambitExecuted(KernelEventLog log)
    {
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
    }

    private static void WriteVersionTestWatermarkAdvanced(
        KernelEventLog log)
    {
        WriteWatermarkAdvanced(log, tick: 5, worldVersion: 5);
    }

    private static void WriteWatermarkAdvanced(
        KernelEventLog log,
        int tick,
        long worldVersion)
    {
        Assert.True(log.TryWrite(KernelEvent.From(
            new WatermarkAdvancedKernelEvent(
            Tick: tick,
            WorldVersion: worldVersion,
            LowWatermark: tick,
            HighWatermark: tick,
            OwnerCount: 64,
            PublicationRevision: tick))));
    }

    private static void WriteEntityCreated(
        KernelEventLog log,
        int entityId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 1,
            WorldVersion: entityId,
            EntityId: entityId,
            SourceCodeId: 40,
            KindId: 2,
            SubtypeId: 3,
            CultureId: 4,
            PhysicalQ: physicalQ,
            PhysicalR: physicalR,
            SemanticQ: semanticQ,
            SemanticR: semanticR,
            SemanticS: -semanticQ - semanticR,
            NameSeed: entityId))));
    }

    private static void WriteEntityMoved(KernelEventLog log, int entityId, int fromQ, int fromR, int toQ, int toR)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new EntityMovedKernelEvent(
            Tick: 2,
            WorldVersion: 20,
            EntityId: entityId,
            SourceCodeId: 40,
            PlaneCode: PlaneCodes.Physical,
            FromQ: fromQ,
            FromR: fromR,
            FromS: -fromQ - fromR,
            ToQ: toQ,
            ToR: toR,
            ToS: -toQ - toR,
            FromZ: 0,
            ToZ: 0))));
    }

    private static void WriteEntityStatusChanged(
        KernelEventLog log,
        int entityId,
        int previousStatus,
        int currentStatus)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new EntityStatusChangedKernelEvent(
            Tick: 3,
            WorldVersion: 30 + entityId,
            EntityId: entityId,
            SourceCodeId: 40,
            KindId: 2,
            SubtypeId: 3,
            PreviousStatusCode: previousStatus,
            CurrentStatusCode: currentStatus))));
    }

    private static void WriteRelationship(
        KernelEventLog log,
        int sourceEntityId,
        int targetEntityId,
        float strength)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new RelationshipChangedKernelEvent(
            Tick: 2,
            WorldVersion: 40,
            ChangeCodeId: 9,
            SourceCodeId: 8,
            RelationshipKindId: 10,
            PolarityCode: RelationshipPolarityCodes.Positive,
            SourceEntityId: sourceEntityId,
            TargetEntityId: targetEntityId,
            RelationshipId: 12,
            PreviousStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0f),
            NewStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(strength),
            ActionId: 0,
            IsActive: strength > 0 ? 1 : 0))));
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
            SemanticS: 0,
            NameSeed: 10))));
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
            ToS: 0,
            FromZ: 0,
            ToZ: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new AttributeChangedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            EntityId: 10,
            AttributeId: 30,
            SourceCodeId: 40,
            OldValueUnits: KernelFixedPoint.AttributeUnits(12f),
            NewValueUnits: KernelFixedPoint.AttributeUnits(14.5f)))));
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

    private static void WriteRelationshipGeneratorAndDecisionEvents(KernelEventLog log)
    {
        WriteEntityCreated(log, entityId: 20, physicalQ: 0, physicalR: 0, semanticQ: 0, semanticR: 0);
        WriteEntityCreated(log, entityId: 21, physicalQ: 1, physicalR: 0, semanticQ: 1, semanticR: -1);
        Assert.True(log.TryWrite(KernelEvent.From(new RelationshipChangedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            ChangeCodeId: 9,
            SourceCodeId: 8,
            RelationshipKindId: 10,
            PolarityCode: RelationshipPolarityCodes.Negative,
            SourceEntityId: 20,
            TargetEntityId: 21,
            RelationshipId: 22,
            PreviousStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0f),
            NewStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0.75f),
            ActionId: 0,
            IsActive: 1))));
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
            SemanticS: 0,
            NameSeed: 1))));
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
            SemanticS: 0,
            NameSeed: 2))));

        pump.DrainAvailable();

        Assert.False(projection.IsComplete);
        Assert.Equal(1, projection.DroppedEvents);
        Assert.False(projection.Entities.ContainsKey(1));
        Assert.True(projection.Entities.ContainsKey(2));
    }

    private static KernelEvent WithProvenance(KernelEvent kernelEvent, int invocationId) =>
        kernelEvent.WithExecutionContext(
            programId: 0,
            sourceCodeId: 1,
            provenanceInvocationId: invocationId);

}
