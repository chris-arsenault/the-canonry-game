# Planner

The current planning system is a bounded, inspectable goal-tree planner built directly on `ProcessDefinition`.

It exists to support emergent multi-step behavior without hand-authored process chains.

---

## Core Intent

The planner is designed around four principles:

1. **Planning and execution share one contract.**
   `ProcessDefinition` is the authority for both.
2. **Blockers are explicit.**
   Failed preconditions emit typed blocker refs, not vague strings.
3. **Multi-step behavior is discovered, not scripted.**
   If one process blocks on a fact another process can satisfy, the planner can chain them.
4. **The system must stay inspectable.**
   Blocked parents, actionable leaves, traces, and diagnostics are all part of the runtime model.

---

## Main Data Structures

### `ProcessDefinition`

The planner reads these fields:

- `GoalEffects`
- `ProviderSelectors`
- `Precondition`
- `RewardExpression`
- `ScoreExpression`
- `SupportRules`

It does not infer planning semantics from raw writes alone.

### `AttributeRef`

Planner facts are typed refs, not strings.

Important scopes:

- `Self`
- `Target`
- `Relational`
- `Structural`
- `Resource`

Examples:

- `Self.Settlement.residents`
- `Target.Settlement.civic_ideology`
- `Relational.Settlement.located_at.matched`
- `Structural.Npc.roam`
- `Resource.food`

### `PreconditionResult`

Failed preconditions return:

- `Passed`
- `Diagnostic`
- `Blockers`

The blocker list is what allows sub-goals to be generated truthfully.

### `PlannerSupportRule`

Support rules model the case where a terminal action is legal but weak.

Example:

- `Grow` is already legal
- current local food is below a desired minimum
- `Irrigate` improves local food
- planner may prefer `Irrigate` first if that produces a stronger actionable leaf

---

## Planning Flow

```text
Deficits + Standing Drives
          |
          v
   desired effects
          |
          v
find processes with matching GoalEffects
          |
          v
enumerate targets/providers from ProviderSelectors
          |
          v
weight each candidate with RewardExpression
          |
          v
evaluate preconditions
   |                    |
   | pass               | fail
   v                    v
score direct leaf   emit typed blockers
   |                    |
   |                    v
   |             find serving processes
   |             for each blocker
   |                    |
   |                    v
   |               recurse (bounded)
   |____________________|
          |
          v
compare direct leaf vs support path
          |
          v
keep best actionable leaf under each root
          |
          v
retain top root plans on entity
```

---

## Execution Flow

The evaluator only competes actionable leaves.

```text
entity tick
  -> regenerate goals if stale/invalid
  -> gather actionable leaves
  -> choose a goal in urgency-weighted order
  -> evaluate serving processes for that goal and target
  -> execute one winning process
  -> if a sub-goal fired or a branch stalled, force replanning
```

This is why blocked parent goals remain in the tree: they explain what the entity is ultimately trying to do, while the leaf explains what it can do now.

---

## Current Planning Patterns

### 1. Blocker Resolution

```text
Want: lower npc warmth
  -> Eat serves warmth
  -> Eat blocked on Relational.located_at.matched
  -> Move To Target serves that relational blocker
  -> NPC moves, replans, then can Eat
```

### 2. Support Planning

```text
Want: increase settlement residents
  -> Grow serves residents
  -> Grow is legal, but local food is weak
  -> support rule proposes better local food
  -> Irrigate serves ambient food
  -> Irrigate wins if its support leaf is stronger than immediate Grow
```

This second pattern matters because the resource model is intentionally about ambient possibility space, not only hard blockers.

---

## Root Selection Rules

The planner does not keep every possible branch.

Current limits:

- max primary roots per entity: `3`
- max generated goals per entity: `12`
- max planning depth: `2`

The current planner keeps one committed root per desired effect.

That means:

- one warmth plan, not three simultaneous warmth destination plans
- one roam plan, not several equivalent roam destinations
- replanning can replace the committed root, but only when the old plan disappears or a challenger is materially better

---

## Why The Planner Keeps One Committed Root Per Effect

Without this rule, one need can fragment into several simultaneously competing destination plans.

Example problem:

```text
warmth
  -> Eat at target A
  -> Eat at target B
  -> Eat at target C

All three roots survive,
so the entity keeps reconsidering where to satisfy the same need.
```

Committing to one root per effect makes entities finish a plan more often, even if it is not globally optimal, which is preferable to visible dithering.

---

## Resource Interaction

The planner treats ambient resources as world-state inputs, not actor-owned inventory.

That matters in two ways:

1. ambient resources can act as thresholds or blockers
2. ambient resources can change the utility of a downstream process

Examples:

- ambient food slows NPC warmth drift
- `Eat` still requires a named settlement
- `Grow` depends on ambient food and ambient population
- `Irrigate` improves ambient food, which strengthens later growth

This is why resource-related planning cannot be reduced to simple stockpile checks.

---

## Validation and Observability

The planner is defended in three ways:

### Wiring validation

Startup validation checks:

- declared `GoalEffects` map to real writes when appropriate
- planner blockers have serving processes
- support effects have serving processes
- goal-effect chains can reach an actionable leaf

### Entity traces

Each entity keeps:

- goal-generation trace
- evaluation trace

These are the main tools for investigating surprising behavior in the live sim.

### UI exposure

The player-facing UI stays compact, but copied debug output includes the deeper planner trace.

---

## Current Limits

These are known, intentional limits of the present system:

- target-presence is still modeled through a relation-backed blocker, not yet a fully generalized presence abstraction
- decision availability is not yet fully wired to tableau state at runtime
- support planning exists, but only the first concrete use case is implemented
- planning depth is intentionally shallow to keep behavior inspectable

Those are future enhancement points, not hidden bugs.
