# Minor Actor Labor Guardrail Plan

## Contract

Minor actors are high-frequency, low-salience labor cohorts. They are not graph
entities, not narrative event sources, and not just a scorer that teleports work
to a best cell.

The system must model local workers that start near a named source, move or
operate through nearby culture/roads/fronts, and apply repeated small work along
that path.

## Owning Layers

- `Engine.Kernel`: minor actor runtime state, pulse cadence, local movement, and
  primitive telemetry.
- `IceRemembers`: authored emitter cadence, action cadence, active counts, and
  verb weights/deltas.
- Tests: runtime behavior tests that fail if minor actors become only a scoring
  layer again.

## Failure To Fix

- Minor actors currently apply work every 8-10 pulses while the program runs at
  one pulse per kernel tick, so they act much less often than major low-altitude
  verbs.
- Verb selection scans around the source and then sets the actor position to the
  selected cell, so a worker can effectively teleport work to a scored target
  instead of moving through local cells.
- The current implementation therefore behaves like a low-volume target scoring
  layer, not a labor metabolism.

## Required Implementation Checks

- Workers must be able to act 1-10x more often than major entities through
  program substeps and authored action cadence.
- Culture/road verbs must choose from the actor's local neighborhood, bounded by
  the source's operating radius, so work progresses through adjacent cells.
- Applying a selected verb may move the cohort by at most one local step.
- A far better cultural target must not receive work until a worker reaches it
  through local movement.
- Existing graph entity counts must not increase from minor actors.
- Telemetry must still report selected minor actor verb, cell kind, local mass,
  connected mass, score, and delta.

## Things Not To Do

- Do not solve this by only increasing `WorkDelta`.
- Do not put minor actors into the entity graph.
- Do not add narrative/chronicled worker events.
- Do not hard-code cultural centers or slot-owner emission radius.
- Do not replace the kernel-local behavior with only `CultureInfluenceTargetScorer`
  changes.

## Verification

- Add failing runtime tests before implementation.
- Use repo-level verification commands only: `make build`, then `make test`.
- If Monte Carlo evidence is needed, use the registered MC workflow and cite the
  run id.
