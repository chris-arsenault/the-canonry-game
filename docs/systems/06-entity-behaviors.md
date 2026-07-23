# System 6: Entity Behaviors

Entity behavior is now split into low-altitude verbs and gambits. The removed
process deck and standing-drive GOAP model are not part of the current runtime.

## Low-Altitude Verbs

Low-altitude verbs are recurring entity intents. They are multi-tick
commitments, not one-tick scalar recovery actions.

Current verb families:

- movement on the physical plane;
- movement on the semantic plane;
- relationship strength mutation on existing relationships;
- growing owned culture influence;
- consolidating owned culture influence;
- reducing targetable foreign culture influence;
- rare coverage expansion;
- rare anchor relocation.

All active entity kinds can participate. Mobile entities make the map live by
moving. Static or semantic-primary entities make the map live by moving on the
semantic plane, projecting culture influence, or emitting minor labor.

The domain config supplies kind, plane, operation, target mode, timing, base
weight, opportunity bonus, relationship fitness, relationship-strength delta,
culture influence parameters, and targeting filters.
`Engine.Gameplay` validates the authored shape and compiles it into primitive
kernel program data. `Engine.Kernel` executes only ids, opcodes, numeric
weights, and fixed arrays.

## Authored Taxonomy

The Ice Remembers domain authors low-altitude verbs across movement,
relationship strength, culture influence, coverage, relocation, and work-tier
growth. Counts are intentionally not part of the durable design contract; when
checking exact coverage, inspect the authored catalog in
`src/IceRemembers/IceRemembersLowAltitudeVerbs.cs`.

Movement verbs create physical and semantic travel around entity-local,
relationship-local, terrain, and culture targets. Relationship-strength verbs
mutate only existing relationship strength; they do not create relationship
topology. Culture influence verbs grow, consolidate, spread, or clean up the
actor's culture field on the relevant plane. Structural verbs remain rare:
settlements can expand coverage, factions can relocate their physical seat, and
entities inside a supported culture mass can deepen their work tier.

## Targeting

Low-altitude targeting is weighted semi-random. Cell-target modes scan a
bounded neighborhood around the actor instead of sampling a few uniform random
map coordinates. Entity-kind and relationship-target modes enumerate eligible
entities or existing relationship edges.

Candidate scores can consider:

- authored base verb weight;
- mode-specific distance preference;
- previous-tick cell saturation;
- recent-target cooldown;
- relationship fitness against authored deficit or surplus thresholds;
- culture-boundary state, connected mass, headroom, and foreign culture
  presence;
- generator and gambit affordance rows compiled into the verb data;
- terrain, entity-kind, relationship, and foreign-culture influence filters.

Affordances are target-level pressure, not a separate action type. Matching
affordance rows are combined with diminishing returns, multiply the candidate
score through an opportunity bonus, and also feed an opportunity bucket. Each
selection rolls between routine and opportunity buckets using kernel tuning, so
affordance-led behavior can be measured directly instead of inferred from
action names.

Trait-authored low-altitude score changes use the shared decision-modifier
path. Traits may adjust base verb weight, opportunity bonus, and relationship
fitness weight. New weighting inputs should be reusable primitives, not
trait-name or kind-name special cases inside the low-altitude runtime.

## Affordance Causal Boundary

Generator and gambit affordances may only point at things low-altitude verbs
can actually do. Some generator and gambit preconditions are intentionally met
through low-altitude behavior, such as moving to a locality, growing culture
influence, or changing strength on an existing relationship. Other
preconditions are only created by generators or gambits, and that breakpoint is
part of the layer model.

Low-altitude verbs may mutate existing relationship strength, but they do not
create or remove relationship topology. A relationship-existence precondition
therefore must not compile into a relationship-creation affordance. Low-altitude
verbs also do not create or change terrain types, so terrain creation,
conversion, or count constraints must not compile into terrain-modification
affordances.

Preconditions can read the ground directly. `SelfHexFactThreshold` and
`TargetHexFactThreshold` band any latent geography fact (temperature,
geothermal, elevation, river coverage, aurora, and the rest) at the actor's
or target's physical hex, authored on each fact's natural scale and
quantized at compile time with the same functions worldgen used to write
the fact. `SelfOnTerrainGroup` gates on the terrain boundary group
underfoot (warm_land, frozen_land, maritime, ...) rather than per-terrain
lists, so content stays valid as terrains join or leave a group. These are
read-only gates: they create no affordance pressure, because no behavior
can move an entity's ground temperature. If a precondition cannot be moved toward by an existing
low-altitude verb shape, it should create no low-altitude affordance pressure;
the pressure must come from the high-altitude content that can actually create
that state.

The goal is not perfect local optimality. The goal is map life: entities should
commit to plausible medium-term behavior, wander and dwell around targets, and
produce activity that later systems can read.

## Culture Influence

The low-altitude influence verbs now write culture tile influence, not a
durable per-entity territorial layer. The acting entity's culture determines
which culture receives raw mass. Targeting uses the kernel culture-boundary
model so actions prefer connected mass, borders, holes, and contested cells
instead of sampling unrelated cells.

Reducing foreign influence removes raw mass from every non-own culture at the
targeted cell. This keeps contested cells resolvable without mutating a
derived share cache.

Entity influence production is still tracked as attribution: a slot occupant or
long-lived entity can be credited for producing culture influence, and that
history can feed slot scoring or telemetry.

## Minor Actors

Minor actors are not graph entities, but they are part of behavior. They are
unnamed worker cohorts emitted by active entities. They run at the simulation
tick cadence, move or operate through local neighborhoods, and execute a small
verb set for culture reinforcement, border extension, gap bridging, foreign
trace cleanup, road maintenance, and source lifecycle support.

See [21 Minor Actors](./21-minor-actors.md).

## Gambits

Gambits are higher-stakes entity actions. They use shared query/effect
primitives for preconditions, scoring, and ordered effects, but they do not
build durable plans.

A gambit has:

- an actor kind;
- a target mode;
- preconditions;
- score expression;
- provider selectors where the target is external;
- success chance;
- effects such as creating, strengthening, weakening, or reversing
  relationships.

Gambits should represent meaningful world actions: wielding a relic, declaring
war, betraying an ally, or warding a dangerous place. They should be rarer than
low-altitude verbs and should create graph state that generators can later use.

Gambit preconditions use shared query predicates, and gambit mutations use the
shared effect executor. Trait-authored score and success modifiers use the
shared decision-modifier path. Gambit-specific code owns direct target choice,
score/reward evaluation, cooldowns, success rolls, and ordered operation
sequencing.

Low-altitude verbs and gambits do not yet share one activity lane or action
budget. The same entity can perform ordinary low-altitude work and a gambit in
the same tick, and multiple movement sources can request movement through the
movement program independently. Treat same-tick contradictions as analysis
targets and future activity-lane design work, not as evidence that the removed
process deck should return.

## Non-Goals

Entity behavior should not reintroduce the removed process deck under a new
name. Do not model low altitude as scalar deficit planning or recovery-host
chasing. Traits, relationship strength, and locality may shape behavior, but
they should not become a treadmill that consumes the simulation.
