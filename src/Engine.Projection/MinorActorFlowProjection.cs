namespace Engine.Projection;

using Engine.Eventing;

public readonly record struct MinorActorViewPoint(
    int ActorOccurrence,
    int Tick,
    int PlaneCode,
    int PreviousQ,
    int PreviousR,
    int Q,
    int R,
    int TargetQ,
    int TargetR,
    int CultureId,
    int RoleCode,
    int TargetingCode,
    int PostureCode,
    float Strength);

public readonly record struct MinorActorWorkPoint(
    int Tick,
    int ActorOccurrence,
    int PlaneCode,
    int Q,
    int R,
    int CultureId,
    int RoleCode,
    int ActionCode,
    float Delta);

/// <summary>
/// Active unnamed cohorts plus a short visual trail of completed work. Actor
/// lifecycle events own the active view; work history is bounded decoration.
/// </summary>
public sealed class MinorActorFlowProjection : KernelEventProjector
{
    private const int WorkCapacity = 768;

    private readonly Dictionary<int, MinorActorViewPoint> _active = [];
    private readonly MinorActorWorkPoint[] _work = new MinorActorWorkPoint[WorkCapacity];
    private int _nextWork;
    private int _workCount;

    public long Version { get; private set; }

    public int CurrentTick { get; private set; }

    public int ActiveCount => _active.Count;

    public int CopyActive(MinorActorViewPoint[] buffer, int planeCode)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var copied = 0;
        foreach (var actor in _active.Values)
        {
            if (copied >= buffer.Length)
            {
                break;
            }

            if (actor.PlaneCode == planeCode)
            {
                buffer[copied++] = actor;
            }
        }

        return copied;
    }

    public int CopyRecentWork(MinorActorWorkPoint[] buffer, int planeCode, int minimumTick)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var copied = 0;
        for (var i = 0; i < _workCount && copied < buffer.Length; i++)
        {
            var work = _work[(_nextWork - 1 - i + WorkCapacity) % WorkCapacity];
            if (work.Tick < minimumTick)
            {
                break;
            }

            if (work.PlaneCode == planeCode)
            {
                buffer[copied++] = work;
            }
        }

        return copied;
    }

    protected override void OnMinorActorCreated(long sequence, in MinorActorCreatedKernelEvent evt)
    {
        _active[evt.ActorOccurrence] = new MinorActorViewPoint(
            evt.ActorOccurrence,
            evt.Tick,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.Q,
            evt.R,
            evt.Q,
            evt.R,
            evt.CultureId,
            evt.RoleCode,
            TargetingCode: 0,
            evt.PostureCode,
            Strength(evt.StrengthUnits));
        Version++;
    }

    protected override void OnMinorActorAssigned(long sequence, in MinorActorAssignedKernelEvent evt)
    {
        if (_active.TryGetValue(evt.ActorOccurrence, out var actor))
        {
            _active[evt.ActorOccurrence] = actor with
            {
                Tick = evt.Tick,
                TargetQ = evt.TargetQ,
                TargetR = evt.TargetR,
                TargetingCode = evt.TargetingCode,
                PostureCode = evt.PostureCode,
                Strength = Strength(evt.StrengthUnits),
            };
            Version++;
        }
    }

    protected override void OnMinorActorMoved(long sequence, in MinorActorMovedKernelEvent evt)
    {
        if (_active.TryGetValue(evt.ActorOccurrence, out var actor))
        {
            _active[evt.ActorOccurrence] = actor with
            {
                Tick = evt.Tick,
                PreviousQ = evt.FromQ,
                PreviousR = evt.FromR,
                Q = evt.ToQ,
                R = evt.ToR,
                TargetQ = evt.TargetQ,
                TargetR = evt.TargetR,
                PostureCode = evt.PostureCode,
                Strength = Strength(evt.StrengthUnits),
            };
            Version++;
        }
    }

    protected override void OnMinorActorWorked(long sequence, in MinorActorWorkedKernelEvent evt)
    {
        if (_active.TryGetValue(evt.ActorOccurrence, out var actor))
        {
            _active[evt.ActorOccurrence] = actor with
            {
                Tick = evt.Tick,
                Q = evt.Q,
                R = evt.R,
                TargetingCode = evt.TargetingCode,
                PostureCode = Engine.Constants.MinorActorPostureCodes.Working,
                Strength = Strength(evt.StrengthUnits),
            };
        }

        _work[_nextWork] = new MinorActorWorkPoint(
            evt.Tick,
            evt.ActorOccurrence,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.CultureId,
            evt.RoleCode,
            evt.ActionCode,
            Strength(evt.DeltaUnits));
        _nextWork = (_nextWork + 1) % WorkCapacity;
        if (_workCount < WorkCapacity)
        {
            _workCount++;
        }

        Version++;
    }

    protected override void OnMinorActorEncountered(
        long sequence,
        in MinorActorEncounteredKernelEvent evt)
    {
        UpdateEncounteredActor(
            evt.ActorOccurrence,
            evt.Tick,
            evt.ActorPostureCode,
            evt.ActorStrengthUnits);
        UpdateEncounteredActor(
            evt.OtherActorOccurrence,
            evt.Tick,
            evt.OtherPostureCode,
            evt.OtherStrengthUnits);
        Version++;
    }

    protected override void OnMinorActorRetired(long sequence, in MinorActorRetiredKernelEvent evt)
    {
        if (_active.Remove(evt.ActorOccurrence))
        {
            Version++;
        }
    }

    protected override void OnWatermarkAdvanced(
        long sequence,
        in WatermarkAdvancedKernelEvent evt) =>
        CurrentTick = evt.LowWatermark;

    private void UpdateEncounteredActor(
        int actorOccurrence,
        int tick,
        int postureCode,
        long strengthUnits)
    {
        if (_active.TryGetValue(actorOccurrence, out var actor))
        {
            _active[actorOccurrence] = actor with
            {
                Tick = tick,
                PostureCode = postureCode,
                Strength = Strength(strengthUnits),
            };
        }
    }

    private static float Strength(long units) =>
        Engine.Constants.FixedScalar.FromUnits(units).ToSingle();
}
