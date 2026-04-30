# Low-Altitude Physics Plan

## Goal Boundary

Low-altitude physics is the per-tick state-evolution layer. It should not create new named entities, introduce new relationship kinds, or produce major narrative events. Those remain the job of generators and future gambits.

Low altitude should provide the live substrate that generators and gambits read:

- entities move or visibly affect the map,
- entities age and die,
- existing relationship strengths drift,
- accumulated map/entity/relationship signals make later high-altitude events feel earned.

## Program 1: Movement / Presence

Purpose:

- Make entities physically or semantically active each tick.
- Spread activity away from the seed zone without requiring high-altitude events.
- Avoid planner behavior where entities repeatedly collapse into a single recovery nexus.

Non-goals:

- Do not create relationships.
- Do not create entities.
- Do not encode story beats.

Expected inputs:

- entity kind and primary plane,
- current physical/semantic coordinates,
- terrain and coverage,
- existing relationships,
- recent map activity,
- optional home/anchor/relationship pulls.

Expected outputs:

- physical or semantic coordinate mutations,
- movement/presence telemetry,
- map activity written by Program 2.

Initial design bias:

- Treat motion as physics/forces over possible nearby positions, not as GOAP over scalar deficits.
- Keep entity as the action unit.
- Let domain configure tendencies, but keep kernel execution generic.

## Program 2: Map Activity Fields

Purpose:

- Make the map visibly live even when no named entity or relationship is created.
- Accumulate evidence that generators/gambits can later query.
- Support map-utilization metrics directly.

Non-goals:

- Do not act as the primary resource economy.
- Do not become another hidden planner.

Expected inputs:

- entity positions,
- movement events,
- coverage/region map,
- terrain,
- tick.

Expected outputs:

- activity heat,
- recent visit counters or decay fields,
- long-absence/unvisited evidence,
- crowding or density fields,
- telemetry/snapshots for Monte Carlo analysis.

Initial design bias:

- Prefer a small set of general-purpose fields over many domain-specific counters.
- Values should decay or roll forward so the map can show both recent activity and long-term use.

## Program 3: Aging / Death

Purpose:

- Make entity turnover a normal low-altitude phenomenon.
- Decouple death from the current scalar-stock recovery loop.
- Produce enough death and churn that generators have meaningful historical material.

Non-goals:

- Do not make death primarily the result of failing a recovery action.
- Do not require each kind to have an attribute economy before it can die.

Expected inputs:

- age,
- kind/subkind lifecycle tuning,
- exposure/crowding/isolation signals,
- relationship or location modifiers where configured.

Expected outputs:

- health/death-risk updates,
- historical status mutations,
- lifecycle/death telemetry.

Initial design bias:

- Death risk should include nonlinear aging.
- Low risk should still occasionally matter over long runs.
- Exposure/crowding/isolation should modulate risk without becoming the whole system.

## Program 4: Relationship Strength Drift

Purpose:

- Make existing relationships evolve from actual world behavior.
- Preserve graph continuity without requiring high-altitude events to maintain every edge.
- Let generators read relationship strength/trends as preconditions.

Non-goals:

- Do not create new relationship kinds.
- Do not make relationship creation a low-altitude default.

Expected inputs:

- existing active relationships,
- source/target position and plane distance,
- shared/different culture,
- co-presence/activity overlap,
- relationship kind tuning,
- relationship facts.

Expected outputs:

- relationship strength deltas,
- culling when strength falls below threshold,
- relationship drift telemetry.

Initial design bias:

- This already exists in a simple form in `RelationshipProgramRuntime`.
- Extend it toward behavior-driven drift instead of replacing it wholesale.

## Architectural Shape

Existing kernel shape:

- `Simulation` registers first-class kernel programs with cadence and source-code scope.
- Current order is `Upkeep`, `Relationships`, `Processes`, `Gambits`, `Pressures`, `Generators`, `Degeneracy`.
- Programs have `Data`, `State`, optional `Scratch`, a runtime, snapshot support, and installation from compiled domain config.

Recommended implementation shape:

- Keep low-altitude physics as first-class kernel programs, not hidden inside process/gambit planning.
- Keep generator and map/coverage systems intact.
- Convert or replace current scalar/process behavior gradually behind config flags.

Likely program responsibilities:

- `MovementProgram`: generic per-kind/per-plane movement physics and optional domain-tuned motion tendencies.
- `MapActivityProgram`: activity/visit/crowding fields and snapshots.
- `UpkeepProgram`: retain or evolve into lifecycle/death risk; avoid making scalar drift the center of simulation.
- `RelationshipProgram`: extend current relationship drift with behavior/map/activity inputs.

Important sequencing:

- Movement should run before map activity if activity records where entities moved this tick.
- Map activity should run before generators if generator preconditions can read recent/accumulated activity.
- Relationship drift can run after movement/activity so it can use co-presence and distance.
- Death/upkeep should run before generators if generators are allowed to react to deaths in the same tick; otherwise one-tick delay is acceptable but should be explicit.

Proposed eventual order:

```text
Movement
MapActivity
RelationshipDrift
Lifecycle/Upkeep
Processes/Gambits (while legacy exists)
Pressures (if retained)
Generators
Degeneracy
```

Migration principle:

- Do not remove old process/planner behavior in the same step as introducing low-altitude physics.
- Add one program at a time with telemetry and Monte Carlo checks.
- Disable or demote legacy behavior only after replacement metrics show equal or better behavior on:
  - map utilization,
  - individual entity roaming variance,
  - visible per-tick activity,
  - death/aging churn,
  - relationship strength dynamics.

