# Performance Tuning Backlog

Performance-shaped follow-ups that aren't blocking correctness or design
intent but that we expect to need attention as the simulation scales or as
new callers add load.

This is a backlog, not an active task list. An item moves into a proposal
only when Monte Carlo evidence shows tick budget, run wall-clock, or memory
has moved past tolerance.

## 1. Per-entity path cache for low-altitude movement

Status: open, not yet evidenced.

Origin: deferred Phase 2 of the unified movement API rework documented under
the road-network fix. Phase 1 routes low-altitude verb stepping through the
existing A* planner, which currently runs a fresh search every step. The
prior A* caller (gambit movement) is rare; low-altitude movement runs every
`IntentStepIntervalTicks` for every moving entity in the world, so per-step
replanning may push the kernel's per-tick budget.

Hypothesis: cache the planner's outbound cell sequence on per-entity state.
Replan only when:

- Intent target changes (new verb begins, target invalidated, dwell ends).
- Entity drifted off the cached path.
- Terrain on a cached cell changed, or `CanEnter` for the actor flipped.
- Road quality on a cached cell changed enough to alter the planner's choice
  (cheap proxy: epoch counter on relevant cells).
- Defensive replan cooldown expires.

Trigger: open the proposal once Phase 1 lands and MC tick-budget telemetry
shows replanning is non-trivial. If MC shows no measurable per-tick cost
increase from the Phase 1 wiring, this stays open indefinitely.

Out of scope for this item:
- Changes to the A* implementation itself.
- Changes to road quality, decay, or `RoadAdjustedTravelCost`.
- Changes to verb / gambit / generator authoring.
