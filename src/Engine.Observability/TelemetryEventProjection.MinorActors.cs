namespace Engine.Observability;

using Engine.Constants;
using Engine.Eventing;

internal sealed partial class TelemetryEventProjection
{
    protected override void OnMinorActorCreated(long sequence, in MinorActorCreatedKernelEvent evt)
    {
        _batch.MinorActorLifecycleEvents.Add(new MinorActorLifecycleEventRow(
            _runId,
            evt.Tick,
            (int)KernelEventType.MinorActorCreated,
            evt.ActorOccurrence,
            evt.SourceEntityId,
            evt.SourceRuleId,
            evt.BindingRelationshipId,
            evt.CultureId,
            evt.RoleCode,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.Q,
            evt.R,
            evt.Q,
            evt.R,
            TargetingCode: 0,
            evt.PostureCode,
            ReasonCode: 0,
            NextReplanTick: 0,
            CommitmentUntilTick: 0,
            FixedScalar.FromUnits(evt.StrengthUnits).ToSingle()));
        MarkProjected(sequence);
    }

    protected override void OnMinorActorAssigned(long sequence, in MinorActorAssignedKernelEvent evt)
    {
        _batch.MinorActorLifecycleEvents.Add(new MinorActorLifecycleEventRow(
            _runId,
            evt.Tick,
            (int)KernelEventType.MinorActorAssigned,
            evt.ActorOccurrence,
            evt.SourceEntityId,
            SourceRuleId: 0,
            BindingRelationshipId: 0,
            CultureId: 0,
            evt.RoleCode,
            evt.PlaneCode,
            evt.PreviousTargetQ,
            evt.PreviousTargetR,
            Q: 0,
            R: 0,
            evt.TargetQ,
            evt.TargetR,
            evt.TargetingCode,
            evt.PostureCode,
            ReasonCode: 0,
            evt.NextReplanTick,
            evt.CommitmentUntilTick,
            FixedScalar.FromUnits(evt.StrengthUnits).ToSingle()));
        MarkProjected(sequence);
    }

    protected override void OnMinorActorMoved(long sequence, in MinorActorMovedKernelEvent evt)
    {
        _batch.MinorActorLifecycleEvents.Add(new MinorActorLifecycleEventRow(
            _runId,
            evt.Tick,
            (int)KernelEventType.MinorActorMoved,
            evt.ActorOccurrence,
            evt.SourceEntityId,
            SourceRuleId: 0,
            BindingRelationshipId: 0,
            evt.CultureId,
            evt.RoleCode,
            evt.PlaneCode,
            evt.FromQ,
            evt.FromR,
            evt.ToQ,
            evt.ToR,
            evt.TargetQ,
            evt.TargetR,
            TargetingCode: 0,
            evt.PostureCode,
            ReasonCode: 0,
            NextReplanTick: 0,
            CommitmentUntilTick: 0,
            FixedScalar.FromUnits(evt.StrengthUnits).ToSingle()));
        MarkProjected(sequence);
    }

    protected override void OnMinorActorWorked(long sequence, in MinorActorWorkedKernelEvent evt)
    {
        _batch.MinorActorWorkEvents.Add(new MinorActorWorkEventRow(
            _runId,
            evt.Tick,
            evt.ActorOccurrence,
            evt.SourceEntityId,
            evt.SourceRuleId,
            evt.SourceWorkTier,
            evt.EffectiveRadius,
            evt.CultureId,
            evt.RoleCode,
            evt.ActionCode,
            evt.TargetingCode,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.CellKindCode,
            FixedScalar.FromUnits(evt.LocalRawMassUnits).ToSingle(),
            FixedScalar.FromUnits(evt.ConnectedRawMassUnits).ToSingle(),
            FixedScalar.FromUnits(evt.StrengthUnits).ToSingle(),
            FixedScalar.FromUnits(evt.DeltaUnits).ToSingle()));
        MarkProjected(sequence);
    }

    protected override void OnMinorActorEncountered(
        long sequence,
        in MinorActorEncounteredKernelEvent evt)
    {
        _batch.MinorActorEncounterEvents.Add(new MinorActorEncounterEventRow(
            _runId,
            evt.Tick,
            evt.ActorOccurrence,
            evt.OtherActorOccurrence,
            evt.ActorCultureId,
            evt.OtherCultureId,
            evt.ActorRoleCode,
            evt.OtherRoleCode,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.EncounterCode,
            evt.ActorPostureCode,
            evt.OtherPostureCode,
            FixedScalar.FromUnits(evt.ActorStrengthUnits).ToSingle(),
            FixedScalar.FromUnits(evt.OtherStrengthUnits).ToSingle(),
            FixedScalar.FromUnits(evt.EffectUnits).ToSingle()));
        MarkProjected(sequence);
    }

    protected override void OnMinorActorRetired(long sequence, in MinorActorRetiredKernelEvent evt)
    {
        _batch.MinorActorLifecycleEvents.Add(new MinorActorLifecycleEventRow(
            _runId,
            evt.Tick,
            (int)KernelEventType.MinorActorRetired,
            evt.ActorOccurrence,
            evt.SourceEntityId,
            evt.SourceRuleId,
            evt.BindingRelationshipId,
            evt.CultureId,
            evt.RoleCode,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.Q,
            evt.R,
            TargetQ: 0,
            TargetR: 0,
            TargetingCode: 0,
            evt.PostureCode,
            evt.ReasonCode,
            NextReplanTick: 0,
            CommitmentUntilTick: 0,
            FixedScalar.FromUnits(evt.StrengthUnits).ToSingle()));
        MarkProjected(sequence);
    }
}
