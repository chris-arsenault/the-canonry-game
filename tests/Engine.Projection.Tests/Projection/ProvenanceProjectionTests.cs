namespace Engine.Projection.Tests.Projection;

using Engine.Eventing;
using Engine.Projection;

public sealed class ProvenanceProjectionTests
{
    [Fact]
    public void ProvenanceProjection_IndexesOutputsAndRelationshipTriggerTraversal()
    {
        var log = new KernelEventLog(capacity: 8);
        var provenance = new ProvenanceProjection();
        using var pump = new KernelEventProjectionPump(log, KernelEventCursorStart.EarliestAvailable, provenance);

        WriteCreatedRelationship(log, invocationId: 1, relationshipKindId: 7);
        WriteInvocation(log, 1, ProvenanceProducerKindCodes.Gambit, 40, 10, 20, 0, ProvenanceTriggerKindCodes.Entity, 20, 1);
        WriteInvocation(log, 2, ProvenanceProducerKindCodes.Generator, 50, 20, 0, 1, ProvenanceTriggerKindCodes.Relationship, 30, 0);

        pump.DrainAvailable();

        Assert.True(provenance.TryGetInvocation(2, out var invocation));
        Assert.Equal(1, invocation.ParentInvocationId);
        Assert.True(provenance.TryGetParentInvocation(2, out var parent));
        Assert.Equal(40, parent.ProducerId);
        Assert.Single(provenance.OutputsOfInvocation(1));
        Assert.True(provenance.TryGetRelationshipCreatedByInvocation(30, out var createdBy));
        Assert.Equal(1, createdBy);
        Assert.True(provenance.TryGetRelationshipTrigger(2, out var trigger));
        Assert.Equal(30, trigger.RelationshipId);
        Assert.Equal(7, trigger.RelationshipKindId);
        Assert.Equal(10, trigger.SourceEntityId);
        Assert.Equal(20, trigger.TargetEntityId);
        Assert.Equal(1, trigger.CreatedByInvocationId);
    }

    private static void WriteCreatedRelationship(
        KernelEventLog log,
        int invocationId,
        int relationshipKindId,
        int sourceId = 10,
        int targetId = 20,
        int relationshipId = 30)
    {
        Assert.True(log.TryWrite(WithProvenance(
            KernelEvent.From(new RelationshipChangedKernelEvent(
                Tick: 1, WorldVersion: 3, ChangeCodeId: 9, SourceCodeId: 1,
                RelationshipKindId: relationshipKindId, PolarityCode: RelationshipPolarityCodes.Positive,
                SourceEntityId: sourceId, TargetEntityId: targetId, RelationshipId: relationshipId,
                PreviousStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0f),
                NewStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0.78f),
                ActionId: 0, IsActive: 1)),
            invocationId)));
    }

    private static void WriteInvocation(
        KernelEventLog log,
        int invocationId,
        int producerKind,
        int producerId,
        int primary,
        int secondary,
        int parent,
        int triggerKind,
        int triggerId,
        int outputCount)
    {
        Assert.True(log.TryWrite(WithProvenance(
            KernelEvent.From(new ProvenanceInvocationRecordedKernelEvent(
                Tick: invocationId, WorldVersion: invocationId + 10, InvocationId: invocationId,
                ProducerKindCode: producerKind, ProducerId: producerId, PrimaryEntityId: primary,
                SecondaryEntityId: secondary, ParentInvocationId: parent, TriggerKindCode: triggerKind,
                TriggerId: triggerId, OutputCount: outputCount)),
            invocationId)));
    }

    private static KernelEvent WithProvenance(KernelEvent kernelEvent, int invocationId) =>
        kernelEvent.WithExecutionContext(programId: 0, sourceCodeId: 1, provenanceInvocationId: invocationId);
}
