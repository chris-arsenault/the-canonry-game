namespace Engine.Observability;

using Engine.Constants;
using Engine.Eventing;
using Engine.Projection;

internal sealed class PlannerTraceProjection : KernelEventProjector
{
    private readonly int _runId;
    private readonly TelemetryBatch _batch;
    private readonly int _maxOpenTraces;
    private readonly Dictionary<PlannerTraceKey, PlannerTraceBuilder> _openTraces = [];
    private long _droppedEventCount;
    private int _lastObservedTick;

    public PlannerTraceProjection(
        int runId,
        TelemetryBatch batch,
        int maxOpenTraces = 4096)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxOpenTraces);

        _runId = runId;
        _batch = batch;
        _maxOpenTraces = maxOpenTraces;
    }

    public int OpenTraceCount => _openTraces.Count;

    public override void OnEventsDropped(long droppedEventCount, long nextSequence)
    {
        _droppedEventCount += droppedEventCount;
        FlushAllOpen("truncated", reasonCodeId: 0, endTick: _lastObservedTick, endSequence: nextSequence);
    }

    protected override void OnPlanStarted(long sequence, in PlanStartedKernelEvent evt)
    {
        _lastObservedTick = Math.Max(_lastObservedTick, evt.Tick);
        var key = PlannerTraceKey.From(evt.Tick, evt.EntityId, evt.PlanId);
        if (_openTraces.TryGetValue(key, out var existing))
        {
            Flush(existing, "truncated", evt.ReasonCodeId, evt.Tick, sequence);
        }

        _openTraces[key] = new PlannerTraceBuilder(
            key,
            evt.Tick,
            sequence,
            evt.CandidateCount,
            evt.ReasonCodeId);
        EvictIfNeeded(sequence);
    }

    protected override void OnGoalConsidered(long sequence, in GoalConsideredKernelEvent evt)
    {
        _lastObservedTick = Math.Max(_lastObservedTick, evt.Tick);
        var trace = GetOrCreateTrace(evt.Tick, evt.EntityId, evt.PlanId, sequence);
        trace.Goals[(evt.GoalId, evt.CandidateIndex)] = new GoalBuilder(
            evt.Tick,
            evt.GoalId,
            evt.CandidateIndex,
            evt.PreliminaryScore);
    }

    protected override void OnGoalScored(long sequence, in GoalScoredKernelEvent evt)
    {
        _lastObservedTick = Math.Max(_lastObservedTick, evt.Tick);
        var trace = GetOrCreateTrace(evt.Tick, evt.EntityId, evt.PlanId, sequence);
        trace.Scores.Add(new PlannerTraceScoreRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            evt.PlanId,
            evt.GoalId,
            evt.ReasonCodeId,
            evt.ScoringPassIndex,
            evt.ScoreDelta,
            evt.RunningTotal));
    }

    protected override void OnGoalSelected(long sequence, in GoalSelectedKernelEvent evt)
    {
        _lastObservedTick = Math.Max(_lastObservedTick, evt.Tick);
        var trace = GetOrCreateTrace(evt.Tick, evt.EntityId, evt.PlanId, sequence);
        trace.SelectedGoalId = evt.GoalId;
        trace.FinalGoalScore = evt.FinalScore;
    }

    protected override void OnPlanStepEmitted(long sequence, in PlanStepEmittedKernelEvent evt)
    {
        _lastObservedTick = Math.Max(_lastObservedTick, evt.Tick);
        var trace = GetOrCreateTrace(evt.Tick, evt.EntityId, evt.PlanId, sequence);
        trace.Steps.Add(new PlannerTraceStepRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            evt.PlanId,
            evt.GoalId,
            evt.StepIndex,
            evt.ActionId,
            evt.TargetEntityId,
            evt.RelationshipKindId));
    }

    protected override void OnPlanCommitted(long sequence, in PlanCommittedKernelEvent evt)
    {
        _lastObservedTick = Math.Max(_lastObservedTick, evt.Tick);
        var trace = GetOrCreateTrace(evt.Tick, evt.EntityId, evt.PlanId, sequence);
        trace.StepCount = evt.StepCount;
        trace.TotalScore = evt.TotalScore;
        Flush(trace, "committed", evt.ReasonCodeId, evt.Tick, sequence);
    }

    protected override void OnPlanAbandoned(long sequence, in PlanAbandonedKernelEvent evt)
    {
        _lastObservedTick = Math.Max(_lastObservedTick, evt.Tick);
        var trace = GetOrCreateTrace(evt.Tick, evt.EntityId, evt.PlanId, sequence);
        Flush(trace, "abandoned", evt.ReasonCodeId, evt.Tick, sequence);
    }

    private PlannerTraceBuilder GetOrCreateTrace(int tick, int entityId, int planId, long sequence)
    {
        var key = PlannerTraceKey.From(tick, entityId, planId);
        if (_openTraces.TryGetValue(key, out var trace))
        {
            return trace;
        }

        trace = new PlannerTraceBuilder(key, tick, sequence, candidateCount: 0, reasonCodeId: 0);
        _openTraces[key] = trace;
        EvictIfNeeded(sequence);
        return trace;
    }

    private void FlushAllOpen(string status, int reasonCodeId, int endTick, long endSequence)
    {
        foreach (var trace in _openTraces.Values.ToArray())
        {
            Flush(trace, status, reasonCodeId, endTick, endSequence);
        }
    }

    private void Flush(
        PlannerTraceBuilder trace,
        string status,
        int reasonCodeId,
        int endTick,
        long endSequence)
    {
        _openTraces.Remove(trace.Key);
        var effectiveReasonCode = reasonCodeId == 0 ? trace.StartReasonCodeId : reasonCodeId;
        var stepCount = trace.StepCount ?? trace.Steps.Count;
        _batch.PlannerTraces.Add(new PlannerTraceRow(
            _runId,
            trace.StartTick,
            endTick,
            trace.Key.EntityId,
            trace.Key.PlanId,
            PlannerTraceStatusCodes.FromStatus(status),
            trace.CandidateCount,
            stepCount,
            effectiveReasonCode,
            trace.TotalScore,
            trace.StartSequence,
            endSequence,
            _droppedEventCount));

        foreach (var goal in trace.Goals.Values.OrderBy(goal => goal.CandidateIndex))
        {
            var wasSelected = trace.SelectedGoalId == goal.GoalId;
            _batch.PlannerTraceGoals.Add(new PlannerTraceGoalRow(
                _runId,
                goal.Tick,
                trace.Key.EntityId,
                trace.Key.PlanId,
                goal.GoalId,
                goal.CandidateIndex,
                goal.PreliminaryScore,
                wasSelected,
                wasSelected ? trace.FinalGoalScore : null));
        }

        foreach (var score in trace.Scores)
        {
            _batch.PlannerTraceScores.Add(score);
        }

        foreach (var step in trace.Steps.OrderBy(step => step.StepIndex))
        {
            _batch.PlannerTraceSteps.Add(step);
        }
    }

    private void EvictIfNeeded(long sequence)
    {
        while (_openTraces.Count > _maxOpenTraces)
        {
            var oldest = _openTraces.Values.MinBy(trace => trace.StartSequence);
            if (oldest is null)
            {
                return;
            }

            Flush(oldest, "truncated", reasonCodeId: 0, endTick: _lastObservedTick, endSequence: sequence);
        }
    }

    private readonly record struct PlannerTraceKey(int StartTick, int EntityId, int PlanId)
    {
        public static PlannerTraceKey From(int tick, int entityId, int planId) =>
            new(tick, entityId, planId);
    }

    private sealed class PlannerTraceBuilder(
        PlannerTraceKey key,
        int startTick,
        long startSequence,
        int candidateCount,
        int reasonCodeId)
    {
        public PlannerTraceKey Key { get; } = key;
        public int StartTick { get; } = startTick;
        public long StartSequence { get; } = startSequence;
        public int CandidateCount { get; } = candidateCount;
        public int StartReasonCodeId { get; } = reasonCodeId;
        public Dictionary<(int GoalId, int CandidateIndex), GoalBuilder> Goals { get; } = [];
        public List<PlannerTraceScoreRow> Scores { get; } = [];
        public List<PlannerTraceStepRow> Steps { get; } = [];
        public int? SelectedGoalId { get; set; }
        public float? FinalGoalScore { get; set; }
        public int? StepCount { get; set; }
        public float? TotalScore { get; set; }
    }

    private sealed record GoalBuilder(
        int Tick,
        int GoalId,
        int CandidateIndex,
        float PreliminaryScore);
}
