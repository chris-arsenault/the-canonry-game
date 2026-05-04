# Entity Activity Unification Tracking

This document tracks the current design findings around unifying entity verbs,
gambits, movement, relationships, and generator affordances. It is not an
implementation plan yet. Use it to keep the model honest before changing code.

## Target Direction

An entity should have one coherent current activity, not independent ambient
verbs and decisive gambits that can issue contradictory effects in the same
tick.

Activities should be able to include phases such as:

- approach a physical or semantic locality;
- perform the meaningful operation;
- dwell or maintain presence;
- release, expire, or get interrupted.

Low-altitude verbs and gambits are candidates to become different severities of
the same activity vocabulary:

- ambient activities: roam, tend, patrol, influence, trade, practice;
- decisive activities: betray, declare war, cleanse, found, transform, create
  or remove important topology.

Generators should remain world systems, but they should publish pressure or
affordances that activities can satisfy.

## Current Findings

### Movement Program

The movement program is not an autonomous movement decider.

It owns traversal services and travel-surface state:

- terrain enterability;
- physical and semantic travel cost;
- physical and semantic movement mutation calls;
- road quality, road decay, and road snapshot/event output.

Its tick does not choose movement. Low-altitude verbs, gambits, relationship
attachment behavior, and other callers request movement through it.

### Low-Altitude Verbs

Low-altitude verbs are ambient multi-tick intents.

Current selection is weighted semi-random:

- scan verbs for the entity kind;
- build one candidate per eligible verb;
- weight the candidate from authored weights, exploration, staleness, heat,
  attractors, crowding, generator affordance, relationship affinity, and trait
  modifiers;
- choose one candidate by weighted random selection;
- keep the intent until it expires, reaches/dwells, or becomes invalid.

Low-altitude has no shared action budget with gambits. It can move or mutate an
entity in the same tick that a gambit also acts on that entity.

### Relationship-Strength Verbs

Relationship-strength verbs target existing relationships and apply small
signed deltas. They do not create relationship topology.

Current relationship affinity weighting favors stronger matching relationships:

```text
relationship affinity contribution = relationship strength * affinity weight
```

That does not produce the desired emergent behavior where entities prioritize
repairing low-strength positive relationships. A unified activity model needs a
relationship-fitness primitive such as strength deficit, surplus, desired
strength, or live generator/gambit pressure.

### Gambits

Gambits are decisive entity actions with scoring, reward, preconditions,
cooldowns, success chance, effects, and shallow planning support.

They currently execute separately from low-altitude verbs. A gambit can move or
mutate the same entity that already moved or mutated through low-altitude in
the same tick.

If low-altitude says "move east" and a gambit later says "move west", the
movement program validates both movement requests independently. The final
position is whichever successful movement mutation runs last.

### Generators

Generators are cadence-based world systems. They select eligible generators by
pulse weight and eligible targets by candidate selection. They are not entity
activities.

Generator affordances currently flow into low-altitude as static compiled
kind-pair hints derived from generator definitions. That is useful, but it is
not live pressure. It does not say which generator is currently blocked, which
relationship is under a threshold, or which local opportunity is close to
unlocking.

### Relationships

Relationships own graph state, strength, culling, and maintenance drift.

Maintenance can read distance, co-presence, contact memory, culture, and
influence evidence. This is appropriate for passive graph-state drift.

There is also a specific relationship-kind behavior:

- relationship kinds may set `TargetFollowsSourcePhysical`;
- `holds` currently uses this;
- the relationship program moves the target to the source's physical position
  with movement cause `Follow`.

This `holds` behavior is correct game behavior: held or possessed things should
physically follow their holder. In the unified model, this should be accounted
for as attachment or possession resolution, not as ordinary relationship
agency. A held object should not be treated as choosing an activity that
competes with its holder's movement.

### Telemetry

Relationship events now carry `action_id`, so relationship-strength mutations
can be attributed directly to the acting low-altitude verb or other action
context. This makes it possible to evaluate whether relationship modifiers are
chosen, executed, and materially changing strength without relying on analyzer
inference.

## Open Design Requirements

- Define an entity activity lane or activity budget so ambient and decisive
  actions do not issue contradictory agency in the same tick.
- Preserve correct attachment behavior such as `holds`, but classify it as a
  constraint or attachment resolution, not as an activity chosen by the held
  entity.
- Let decisive activities interrupt or supersede ambient activities explicitly.
- Give relationship-strength activity selection a reusable fitness primitive:
  desired strength, deficit, surplus, or pressure from blocked downstream
  opportunities.
- Convert generator affordance from static kind-pair hints into live opportunity
  pressure where possible.
- Keep low-altitude from becoming the removed process deck or a scalar recovery
  treadmill.
- Keep gambits from absorbing ordinary wandering, tending, and map-life work.

## Questions To Resolve

- What exactly counts as an entity's current activity?
- Which effects consume the activity lane, and which are passive consequences?
- Can an activity perform movement and an operation on the same tick, or should
  that require explicit phase/budget rules?
- How should interruption work when a high-urgency gambit competes with an
  ambient activity?
- How should attached entities, held artifacts, transported passengers, and
  similar dependent entities be represented?
- What is the smallest reusable relationship-fitness vocabulary that covers
  allied, rival, trade, practice, ward, corruption, war, and similar strengths?
