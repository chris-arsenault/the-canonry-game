# System 6: Entity Behaviors

Entity behavior is now split into low-altitude verbs and gambits. The removed
process deck and standing-drive GOAP model are not part of the current runtime.

## Low-Altitude Verbs

Low-altitude verbs are recurring entity intents. They are multi-tick
commitments, not one-tick scalar recovery actions.

Current verb families:

- movement on the physical plane;
- movement on the semantic plane;
- growing owned influence;
- consolidating owned influence;
- reducing targetable foreign influence;
- rare coverage expansion;
- rare anchor relocation.

All active entity kinds can participate. Mobile entities make the map live by
moving. Static or semantic-primary entities make the map live by moving on the
semantic plane and by projecting influence over cells or semantic coordinates.

The domain config supplies kind, plane, operation, target mode, timing,
weights, relationship affinity, influence parameters, and targeting filters.
`Engine.Gameplay` validates the authored shape and compiles it into primitive
kernel program data. `Engine.Kernel` executes only ids, opcodes, numeric
weights, and fixed arrays.

## Targeting

Low-altitude targeting is weighted semi-random. Candidate scores can consider:

- exploration and staleness;
- recent activity and hotspots;
- crowding;
- relationship affinity;
- generator affordance pairs compiled into the verb data;
- terrain or entity-kind filters;
- foreign influence filters.

Trait-authored low-altitude score changes use the shared decision-modifier
path. New weighting inputs should be reusable primitives, not trait-name or
kind-name special cases inside the low-altitude runtime.

The goal is not perfect local optimality. The goal is map life: entities should
commit to plausible medium-term behavior, wander and dwell around targets, and
produce activity that later systems can read.

## Map Influence

Influence is non-exclusive. Multiple entities can influence the same cell or
semantic point. Physical coverage ownership is separate and rare; influence can
grow without claiming ownership.

Settlements and factions use influence to make static entities visible on the
map. Penguins and artifacts can exert semantic influence even when their primary
movement is physical.

## Gambits

Gambits are higher-stakes entity actions. They use the behavior bytecode and
planner support that remains in the engine, but they are not low-altitude
processes.

A gambit has:

- an actor kind;
- a target mode;
- preconditions;
- optional objective/scoring data;
- success chance;
- effects such as creating, strengthening, weakening, or reversing
  relationships.

Gambits should represent meaningful world actions: wielding a relic, declaring
war, betraying an ally, or warding a dangerous place. They should be rarer than
low-altitude verbs and should create graph state that generators can later use.

Gambit preconditions use shared query predicates, and gambit mutations use the
shared effect executor. Trait-authored score and success modifiers use the
shared decision-modifier path. Gambit-specific code owns scheduling, planning,
target choice, score expressions, and ordered operation sequencing.

## Non-Goals

Entity behavior should not reintroduce the removed process deck under a new
name. Do not model low altitude as scalar deficit planning or recovery-host
chasing. Traits, relationship strength, and locality may shape behavior, but
they should not become a treadmill that consumes the simulation.
