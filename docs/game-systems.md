# Game Systems

The Canonry Game is a deterministic simulation about named entities, their
relationships, and the map they inhabit. Systems are grouped by what they own:
runtime state, low-altitude physics, high-altitude events, player-facing cards,
and observability.

## Core Runtime State

The runtime world is a graph of named entities. Each entity has a kind, subtype,
culture, status, health, traits, discrete status tags, physical coordinates,
semantic coordinates, and relationships. Relationships are typed, directional or symmetric, polar,
and strength-bearing.

The map has physical and semantic planes. The physical plane is a hex grid with
terrain, named coverage, and an integer z axis in `[-3, +3]` whose non-surface
cells are forged only by entity actions (Aurora spire tiers up, Nightshelf
tunnel galleries down). Regions and settlements are not a separate
abstraction from map coverage: they are named coverage over physical space. The
physical generation blueprint also divides every surface hex into 24 canonical
microtriangles with material, elevation, and hydrology. Those triangles produce
one definitive hex summary for the current hex-scale simulation and are the
substrate for later fine-grained rendering and gameplay. The semantic plane is
an analogous bounded axial q/r map with `s = -q-r`, its own varied terrain,
culture influence, roads, and placement. It is not a coordinate copy of the
physical plane and does not use microtriangles.

Domain content is authored in `DomainConfig` and Ice Remembers-specific files.
Reusable gameplay vocabulary lives in `Engine.Gameplay` and compiles down into
primitive kernel program data. The kernel executes only ids, numeric codes, and
fixed-capacity arrays.

## Runtime Altitudes

Low altitude is always-on per-tick physics. It keeps the world visibly alive,
ages entities, drifts existing relationship strength, and writes map signals
that later systems can read. It does not create named entities or declare major
story outcomes.

Minor actors are the low-salience labor layer. They are unnamed cohorts stored
inside a kernel program, not graph entities. They act more frequently than the
main graph cadence and do bounded local work for their source entity and
culture.

Gambits are high-stakes entity actions. A gambit has an actor, target, intrinsic
success criteria, and effects such as creating or changing relationships. A
gambit may use shared behavior bytecode, shared query predicates, and shared
effect execution, but it is not the removed low-altitude process system or the
removed generic planning program.

Generators are intermittent high-altitude events. They create named entities
and relationship bundles when their preconditions are met. Generator selection
should be shaped by the state created by low altitude and gambits.

Both map planes grow from radius-18 hearts into deterministic latent geography
bounded at radius 128. Physical cells open where culture presses the frontier
during the Age of the Thaw; semantic cells open continuously where idea-mass
presses. The Long Dark can cover weak frontier-grown physical cells with
impassable Deep Ice, but thaw removes the cover and reveals the current durable
base terrain rather than replacing it. Reach across the map is priced by travel
cost: a `WithinTravelReach` gate line-samples road-discounted terrain cost, so
frozen frontier blocks reach and roads extend it. See
[systems/26-growing-map.md](./systems/26-growing-map.md).

World beats are the macro-chronicle: authored Ages on three regime tracks
(Ice, Peoples, Memory) recognized from macro world state - population, wars
living and ended, culture mass and its growth, canons, legendary resolutions.
An open age charges ordinary authored content (generator weights, verb
weights, gambit scores, the decay tide); the Age of Striving arms campaigns,
whose labor columns are the world's mass-projection at contested fronts. See
[systems/25-world-beats.md](./systems/25-world-beats.md).

Knowledge is claimed spatially rather than researched through a queue. The 27
authored technologies occupy tiered semantic-map sites; current culture claims
activate explicit grants over existing verbs, generators, gambits, movement,
relationships, map signals, culture fields, minor actors, and frontier rules.
Claims and grants disappear if culture loses the site. See
[systems/27-knowledge.md](./systems/27-knowledge.md).

Beats are the fourth authorship class, owned by the Chronicle Director. A beat
is a dramatic potential attached to a cast chronicle: it opens on world or
heat conditions, charges gambits between the cast while open, and strikes when
the cast actually acts or a recognized fact occurs. Terminal beats end
chronicles by recognizing world facts; the director never edits the world.
See [systems/22-chronicle-director.md](./systems/22-chronicle-director.md).

The player-facing card/tableau layer controls which generators and gambits are
active. Cards determine what kinds of worlds can emerge; they should not be
mistaken for the low-altitude physics itself. Two one-shot intervention cards
(consecrate a hex for its dominant culture, favor an entity's influence
production) route through kernel commands under a dedicated `player` source
code, so player actions stay attributable in events and telemetry.

See [simulation-principles.md](./systems/simulation-principles.md) for the
quality goals used to judge whether these layers are working together.

## Low-Altitude Programs

The low-altitude rewrite replaced the old process deck with first-class kernel
programs:

- `LowAltitudeVerbs`: multi-tick movement, semantic movement, relationship
  strength mutation, culture influence growth/reduction, coverage expansion,
  anchor relocation, and work-tier growth.
- `Movement`: route planning, movement execution, road quality, and travel
  structures.
- `MapActivity`: physical and semantic presence heat, visit counts, and
  occupancy.
- `MapSignals`: terrain, entity, relationship, activity, crowding, support,
  threat, and attractor fields.
- `CultureBoundaries`: derived culture components, interiors, borders,
  contested cells, isolated pockets, holes, and strength bands.
- `CultureFieldDecay`: slow global decay of raw culture mass so territory
  reflects current production against loss instead of accumulated history.
- `Relationships`: existing relationship strength drift and culling.
- `Effects`: health modifiers derived from relationship,
  culture influence, distance, support, threat, and crowding evidence.
- `Upkeep`: lifecycle drain and historical-status transition on death.
- `MinorActors`: unnamed worker cohorts that maintain culture, roads, and
  source lifecycle without adding graph nodes.
- `Knowledge`: slow claim recomputation and culture-scoped modifiers derived
  from authored semantic-map sites.

The current default tick order is:

```text
Movement
LowAltitudeVerbs
MapActivity
MapSignals
CultureFieldDecay
CultureBoundaries
Relationships
Effects
Upkeep
Gambits
ChronicleDirector
Generators
WorldBeats
MapFrontier
CultureSlots
Knowledge
MinorActors
```

`LowAltitudeVerbs` intentionally reads previous-tick map activity and map
signals so same-tick ordering does not create feedback loops. `MapSignals`,
`CultureBoundaries`, `Relationships`, and `Effects` read same-tick state after
movement and activity have been materialized.

By default, minor actors run every simulation tick. The main graph programs run
every `MinorActorMainTickRatio` ticks, currently `5`; generator pulse widths
and main-cadence ages are scaled by the same ratio.

## High-Altitude Systems

Gambits and generators are the primary graph-shaping systems.

Gambits represent entities doing meaningful things: wielding an artifact,
declaring war, betraying an ally, making a ward, or attempting a dangerous
ritual. They normally change relationship topology or polarity rather than
creating new entities.

Generators represent major world events where new named entities come into
being. "Created" means named by the simulation, not necessarily born from
nothing. Generators can also create relationships and apply secondary effects.

Both systems are data-defined through the domain and compiled into primitive
kernel program data. Their gates and mutations use shared kernel query and
effect primitives; pulse budgeting, target selection, scoring, and production
order remain caller policy.

## Observability

The kernel emits primitive events into a bounded event log. Projections,
run logs, UI state, Monte Carlo telemetry, and parquet exports are consumers of
that event stream. The kernel is not allowed to call UI, file I/O, console I/O,
or string-heavy observability directly.

Culture power is measured on three asymmetric ledgers (Witness, Shadow,
Wake) computed host-side from world state. Balance is kept by theme-emergent
mechanisms — parasitic cross-cultural generators, devotion and predation
economies, prey-side responses, local carrying capacity (crowding strain
discounted by own-culture territory) — never by scoreboard controllers.
Ledgers are exported as
`culture_power_snapshots`, and shown in the nations rows; see
[culture.md](./systems/culture.md).

Knowledge telemetry has two levels: `knowledge_snapshots` reports aggregate
claim count and maximum tier, while `knowledge_claim_snapshots` identifies the
site index each culture actually holds. `knowledge_site_symbols` resolves those
indices to stable technology names.

The in-game **Technology (K)** overview uses the same live kernel claim state
and derived modifier totals to show each culture's controlled technologies and
combined bonuses; it does not infer effects from telemetry counts.

Monte Carlo analysis should be performed against parquet output with SQL,
DuckDB, or Python. The stable simulation-quality metrics are described in
[simulation-principles.md](./systems/simulation-principles.md).

## Presentation

The Godot UI presents projection/read-model state. It must not compute
simulation truth, mutate runtime state, or redraw every panel just because a
kernel tick advanced. Current UI presentation principles are recorded in
[ui-principles.md](./design/ui-principles.md).

## System Index

- [01 Entity Graph](./systems/01-entity-graph.md)
- [02 Entity Kinds](./systems/02-entity-kinds.md)
- [03 Health And Traits](./systems/03-attributes.md)
- [04 Map Spatial](./systems/04-map-spatial.md)
- [05 Tick Clock](./systems/05-tick-clock.md)
- [06 Entity Behaviors](./systems/06-entity-behaviors.md)
- [07 Relationships](./systems/07-relationships.md)
- [08 Removed Resources / Economy](./systems/08-removed-resources-economy.md)
- [09 Lifecycle](./systems/09-lifecycle.md)
- [10 Map Visualization](./systems/10-map-visualization.md)
- [11 Removed Macro Aggregate](./systems/11-removed-macro-aggregate.md)
- [12 Cards And Tableau](./systems/12-deck.md)
- [13 Tableau Mana](./systems/13-tableau-mana.md)
- [15 Domain Definition Graph](./systems/15-domain-definition-graph.md)
- [16 Generator Executor](./systems/16-generator-executor.md)
- [17 System Executor](./systems/17-system-executor.md)
- [18 Action Executor](./systems/18-action-executor.md)
- [19 Narrative Domain](./systems/19-narrative-domain.md)
- [20 Generator Causal Limits](./systems/20-generator-causal-limits.md)
- [21 Minor Actors](./systems/21-minor-actors.md)
- [22 Map Signals](./systems/22-map-signals.md)
- [23 Tags And Status State Machines](./systems/23-tags.md)
- [24 Canons And Jurisdictions](./systems/24-canons-and-jurisdictions.md)
- [25 World Beats](./systems/25-world-beats.md)
- [26 Growing Map](./systems/26-growing-map.md)
- [27 Knowledge and Technology](./systems/27-knowledge.md)
- [UI Principles](./design/ui-principles.md)
