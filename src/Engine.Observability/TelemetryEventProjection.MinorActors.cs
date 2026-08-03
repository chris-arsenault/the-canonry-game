namespace Engine.Observability;

using Engine.Eventing;

internal sealed partial class TelemetryEventProjection
{
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
}
