# System 6: Entity Behaviors

The behavior stack that turns domain-defined processes into per-tick action.

---

## Purpose

Every active entity goes through the same engine flow:

1. low-altitude physics updates passive state
2. the planner turns needs and standing drives into bounded goal trees
3. the evaluator selects one viable process and executes its mutations
4. decisions use the same definition model, but runtime card-gating is still a deferred integration step

There is no per-kind behavioral code in the engine. Kinds differ by data only: attributes, standing drives, and registered `ProcessDefinition`s.

---

## Current Stack

### 1. Physics

Always-on systems update state before any process selection:

- lifecycle drain
- ambient location-resource drift
- maintenance tick effects
- relationship updates
- ambient resource preservation

These are not scored or selected. They define conditions.

### 2. Planner

The planner is definition-backed and multi-step.

- attribute deficits create demand
- standing drives create low-urgency ambient demand
- processes advertise what they can satisfy through `GoalEffects`
- blocked processes emit typed blocker refs through `PreconditionResult`
- support rules can prefer an enabling action even when the terminal action is already legal but weak

See [planner.md](./planner.md) for the full model and diagrams.

### 3. Processes

The evaluator only considers actionable leaf goals. For each tick it:

1. checks entity-wide cooldown
2. regenerates goals if needed
3. tries actionable goals in weighted order
4. evaluates serving processes against that goal and target
5. scores viable candidates
6. executes the winning process
7. forces replanning when a sub-goal fires or a branch stalls

### 4. Decisions

Decisions already share the same definition structure as processes, but the runtime tableau-driven registration path is still backlog work. The engine supports separate decision execution; full card availability wiring is intentionally deferred.

---

## ProcessDefinition as the Runtime Contract

The engine now treats `ProcessDefinition` as the behavioral authority for both planning and execution.

Important fields:

- `GoalEffects`: planner facts this process can satisfy
- `ProviderSelectors`: how to enumerate external targets/providers
- `Reads` / `Writes`: execution wiring
- `Precondition`: composable gating logic
- `ScoreExpression`: immediate execution utility
- `RewardExpression`: planning-time provider/target weighting
- `SupportRules`: ways to improve a downstream action rather than merely unblock it
- `Mutations`: ordered effects applied on execution

This means planning and execution no longer drift apart into separate semantic models.

---

## Current Planner Shape

The planner is not a generic GOAP search over arbitrary world states. It is a bounded, inspectable goal-tree system built from process contracts.

- max primary roots per entity: `3`
- max planning depth: `2`
- only actionable leaves compete for execution
- blocked parents remain visible for inspectability
- the planner keeps one committed root per desired effect, so one need does not fragment into several competing target branches on every replan

The first support-planning path is already live:

```text
Settlement wants residents
  -> Grow is legal but weak because local food is poor
  -> support rule proposes Irrigate
  -> Irrigate improves ambient food
  -> Grow becomes stronger later
```

---

## Current NPC Pattern

The current NPC loop is intentionally simple but structurally aligned:

- ambient food slows hunger growth by location
- `Eat` is the strong negative-hunger action
- `Eat` must happen at a named settlement
- `Move To Target` satisfies the relational blocker for getting to that settlement
- `Wander` is the low-urgency fallback when no stronger motive dominates

This is a substitute loop, not the final NPC design. The important property is that movement is discovered as an enabler, not hardcoded as `eat -> move -> eat`.

Process ideation for the next wave of behavior should follow the interface-first loop rules in [process_design.md](./process_design.md).

---

## Diagnostics and Validation

The behavior system is designed to be inspectable.

- goal generation writes a trace onto the entity
- evaluation writes a per-tick candidate/execution trace
- copied UI payloads include the deeper planner/evaluator trace
- wiring validation checks planner blockers, support effects, and reachable goal-effect chains

This is part of the architecture, not optional debug sugar.

---

## Known Deferred Work

- generalized target-presence abstraction beyond the current relation-backed movement blocker
- full runtime card-driven decision registration
- richer indirect utility-improvement planning beyond the first support-rule use case
