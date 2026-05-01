# System 3: Attributes

Attributes are per-entity scalar state. In the current low-altitude rewrite,
most attributes are intentionally static; health is the primary active stock.

## Purpose

Attributes provide targetable, visible entity variation. They are not an
implicit planner, hidden resource economy, or old process deck substrate.

## Current Attribute Set

| Kind | Health | Other Attributes |
| --- | --- | --- |
| `region` | `stability` | `holding`, `pristiness` |
| `settlement` | `temp_hp` | `dominion`, `shelter` |
| `faction` | `cohesion` | `reach`, `provisions` |
| `penguin` | `vitality` | `echo`, `warmth` |
| `artifact` | `condition` | `power`, `resonance` |

All current attributes are bounded 0-100 scalars. Seed defaults are randomized
so entities begin with individual variation.

## Current Dynamics

- Upkeep and effects modify health.
- Lifecycle reads health and age to transition entities out of active play.
- Generators and gambits may use attributes as preconditions or effects.
- Low-altitude verbs currently use configured weights, map state, relationships,
  and influence more than direct attribute demand.
- Resources and pressures are retained as future observability/design surfaces,
  but they are not the active driver of low-altitude action selection.

## Design Rules

- High value should mean "more of the positive thing." If the concept is bad,
  name the inverse. Example: `pristiness`, not `corruption`.
- Do not add attributes just to restore the old recovery treadmill.
- If an attribute does not affect targeting, lifecycle, relationship strength,
  generator/gambit conditions, UI, or future card design, cut it.
- Cultural display names are UI flavor only. Mechanical implementation remains
  the same attribute ID.
