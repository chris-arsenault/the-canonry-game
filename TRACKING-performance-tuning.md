# Performance Tuning Backlog

Performance-shaped follow-ups that aren't blocking correctness or design
intent but that we expect to need attention as the simulation scales or as
new callers add load.

This is a backlog, not an active task list. An item moves into a proposal
only when Monte Carlo evidence shows tick budget, run wall-clock, or memory
has moved past tolerance.

## 1. Hierarchical route planning for low-altitude movement

Status: promoted to proposal.

Origin: deferred Phase 2 of the unified movement API rework documented under
the road-network fix. Phase 1 routes low-altitude verb stepping through the
existing A* planner, which currently runs a fresh search every step. The
prior A* caller (gambit movement) is rare; low-altitude movement runs every
`IntentStepIntervalTicks` for every moving entity in the world, so per-step
replanning may push the kernel's per-tick budget.

Updated direction: do not treat per-entity full-path caching as the final
form. Low-altitude movement can choose world-spanning entity-kind and
relationship targets, and the intended map is materially larger than the
current profile map. Prefer hierarchical route planning with segment-level
route caches, discrete movement profiles, and cadence-based road
reconciliation.

Detailed proposal: `TRACKING-pathfinding-hierarchy.md`.

Trigger: continue work once dotnet-trace hotspot analysis shows movement
pathfinding remains non-trivial after the current loop-shape optimizations.

Out of scope for this item:
- Changes to verb / gambit / generator authoring.
