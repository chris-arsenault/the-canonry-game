namespace Engine.Projection;

using Engine.Constants;
using Engine.Eventing;

public sealed partial class UiProjectionState
{
    protected override void OnMinorActorCreated(long sequence, in MinorActorCreatedKernelEvent evt)
    {
        _minorActors[evt.ActorOccurrence] = new UiMinorActorProjection(
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
            FixedScalar.FromUnits(evt.StrengthUnits).ToSingle(),
            evt.Tick,
            evt.Tick,
            evt.WorldVersion);
        MinorActorVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnMinorActorAssigned(long sequence, in MinorActorAssignedKernelEvent evt)
    {
        if (_minorActors.TryGetValue(evt.ActorOccurrence, out var actor))
        {
            _minorActors[evt.ActorOccurrence] = actor with
            {
                TargetQ = evt.TargetQ,
                TargetR = evt.TargetR,
                TargetingCode = evt.TargetingCode,
                PostureCode = evt.PostureCode,
                Strength = FixedScalar.FromUnits(evt.StrengthUnits).ToSingle(),
                LastUpdatedTick = evt.Tick,
                WorldVersion = evt.WorldVersion,
            };
            MinorActorVersion++;
        }

        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnMinorActorMoved(long sequence, in MinorActorMovedKernelEvent evt)
    {
        if (_minorActors.TryGetValue(evt.ActorOccurrence, out var actor))
        {
            _minorActors[evt.ActorOccurrence] = actor with
            {
                PreviousQ = evt.FromQ,
                PreviousR = evt.FromR,
                Q = evt.ToQ,
                R = evt.ToR,
                TargetQ = evt.TargetQ,
                TargetR = evt.TargetR,
                PostureCode = evt.PostureCode,
                Strength = FixedScalar.FromUnits(evt.StrengthUnits).ToSingle(),
                LastUpdatedTick = evt.Tick,
                WorldVersion = evt.WorldVersion,
            };
            MinorActorVersion++;
        }

        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnMinorActorWorked(long sequence, in MinorActorWorkedKernelEvent evt)
    {
        var browserChanged = false;
        if (_entities.TryGetValue(evt.SourceEntityId, out var source) &&
            source.WorkTier != evt.SourceWorkTier)
        {
            source.WorkTier = evt.SourceWorkTier;
            browserChanged = true;
        }

        if (_minorActors.TryGetValue(evt.ActorOccurrence, out var actor))
        {
            _minorActors[evt.ActorOccurrence] = actor with
            {
                Q = evt.Q,
                R = evt.R,
                TargetingCode = evt.TargetingCode,
                PostureCode = MinorActorPostureCodes.Working,
                Strength = FixedScalar.FromUnits(evt.StrengthUnits).ToSingle(),
                LastUpdatedTick = evt.Tick,
                WorldVersion = evt.WorldVersion,
            };
        }

        _minorActorWork.Add(new UiMinorActorWorkProjection(
            sequence,
            evt.Tick,
            evt.ActorOccurrence,
            evt.SourceEntityId,
            evt.SourceRuleId,
            evt.SourceWorkTier,
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
            FixedScalar.FromUnits(evt.DeltaUnits).ToSingle(),
            evt.WorldVersion));

        if (_minorActorWork.Count > MinorActorWorkCapacity)
        {
            _minorActorWork.RemoveRange(0, _minorActorWork.Count - MinorActorWorkCapacity);
        }

        MinorActorVersion++;
        if (browserChanged)
        {
            MarkEntityBrowserStructureChanged(evt.SourceEntityId);
            EntityDetailVersion++;
        }

        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnMinorActorEncountered(
        long sequence,
        in MinorActorEncounteredKernelEvent evt)
    {
        UpdateMinorActorAfterEncounter(
            evt.ActorOccurrence,
            evt.ActorPostureCode,
            evt.ActorStrengthUnits,
            evt.Tick,
            evt.WorldVersion);
        UpdateMinorActorAfterEncounter(
            evt.OtherActorOccurrence,
            evt.OtherPostureCode,
            evt.OtherStrengthUnits,
            evt.Tick,
            evt.WorldVersion);
        MinorActorVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnMinorActorRetired(long sequence, in MinorActorRetiredKernelEvent evt)
    {
        if (_minorActors.Remove(evt.ActorOccurrence))
        {
            MinorActorVersion++;
        }

        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    private void UpdateMinorActorAfterEncounter(
        int actorOccurrence,
        int postureCode,
        long strengthUnits,
        int tick,
        long worldVersion)
    {
        if (_minorActors.TryGetValue(actorOccurrence, out var actor))
        {
            _minorActors[actorOccurrence] = actor with
            {
                PostureCode = postureCode,
                Strength = FixedScalar.FromUnits(strengthUnits).ToSingle(),
                LastUpdatedTick = tick,
                WorldVersion = worldVersion,
            };
        }
    }
}

public sealed record UiMinorActorProjection(
    int ActorOccurrence,
    int SourceEntityId,
    int SourceRuleId,
    int BindingRelationshipId,
    int CultureId,
    int RoleCode,
    int PlaneCode,
    int PreviousQ,
    int PreviousR,
    int Q,
    int R,
    int TargetQ,
    int TargetR,
    int TargetingCode,
    int PostureCode,
    float Strength,
    int CreatedTick,
    int LastUpdatedTick,
    long WorldVersion);

public sealed record UiMinorActorWorkProjection(
    long Sequence,
    int Tick,
    int ActorOccurrence,
    int SourceEntityId,
    int SourceRuleId,
    int SourceWorkTier,
    int CultureId,
    int RoleCode,
    int ActionCode,
    int TargetingCode,
    int PlaneCode,
    int Q,
    int R,
    int CellKindCode,
    float LocalRawMass,
    float ConnectedRawMass,
    float Strength,
    float Delta,
    long WorldVersion);
