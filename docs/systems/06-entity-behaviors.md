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
- growing owned influence;
- consolidating owned influence;
- reducing targetable foreign influence;
- rare coverage expansion;
- rare anchor relocation.

All active entity kinds can participate. Mobile entities make the map live by
moving. Static or semantic-primary entities make the map live by moving on the
semantic plane and by projecting influence over cells or semantic coordinates.

The domain config supplies kind, plane, operation, target mode, timing, base
weight, opportunity bonus, relationship fitness, relationship-strength delta,
influence parameters, and targeting filters.
`Engine.Gameplay` validates the authored shape and compiles it into primitive
kernel program data. `Engine.Kernel` executes only ids, opcodes, numeric
weights, and fixed arrays.

## Authored Taxonomy

The Ice Remembers domain currently authors 98 low-altitude verbs:

- Physical movement, 22 verbs: penguin exploration/visiting/fishing/relic
  following; artifact circulation/bearer/shrine/scar/sea movement; orca hunt,
  patrol, and pod calls; monster stirring/heat/fissure motion; ability and
  point-of-interest surfacing/yielding; transport sailing, port, patrol, and
  captain-following.
- Semantic movement, 18 verbs: settlement deliberation/gossip; faction
  preaching/quarreling; region communing/dreaming; orca deed-name motion;
  point-of-interest resonance/calling/echoing; ability bearer/region/lineage
  movement; monster semantic reach; transport charter motion; occurrence
  word-spreading and faction-drawing.
- Relationship strength mutation, 13 verbs: factions strengthen or fray
  alliances, factions inflame or cool rivalries, settlements tend trade
  routes, abilities reinforce practice, let practice fade, or charge conduits,
  orcas reinforce pod ties, points of interest loosen claims, monsters deepen
  corruption, transports overstrain conduits, and occurrences exhaust wars.
  These mutate only existing relationship strength; they do not create
  relationship topology.
- Semantic influence, 28 verbs: entities grow, consolidate, or reduce semantic
  influence around themselves, related targets, or foreign influence.
- Physical influence, 15 ordinary verbs plus 2 rare structural verbs: entities
  grow, consolidate, or reduce physical influence; settlements can expand
  coverage; factions can relocate their physical seat.

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
- influence shortfall or foreign influence presence;
- generator and gambit affordance rows compiled into the verb data;
- terrain, entity-kind, relationship, and foreign-influence filters.

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
through low-altitude behavior, such as moving to a locality, growing influence,
or changing strength on an existing relationship. Other preconditions are only
created by generators or gambits, and that breakpoint is part of the layer
model.

Low-altitude verbs may mutate existing relationship strength, but they do not
create or remove relationship topology. A relationship-existence precondition
therefore must not compile into a relationship-creation affordance. Low-altitude
verbs also do not create or change terrain types, so terrain creation,
conversion, or count constraints must not compile into terrain-modification
affordances. If a precondition cannot be moved toward by an existing
low-altitude verb shape, it should create no low-altitude affordance pressure;
the pressure must come from the high-altitude content that can actually create
that state.

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

## Non-Goals

Entity behavior should not reintroduce the removed process deck under a new
name. Do not model low altitude as scalar deficit planning or recovery-host
chasing. Traits, relationship strength, and locality may shape behavior, but
they should not become a treadmill that consumes the simulation.
