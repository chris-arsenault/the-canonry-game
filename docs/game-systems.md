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
physical generation blueprint also exposes 24 canonical microtriangles for
every runtime surface cell. That grouping is a seed-to-kernel projection, not
the generation topology: outside the kernel, one padded global equilateral
microtriangle lattice—with uniform unit-distance adjacency and no parent-cell
knowledge—partitions moving continental and oceanic crust, derives typed boundaries and strain,
grows distributed continental assemblies, and carries continuous crust
composition, age, thickness, load, and material history through conservative
source-side plate transport. Connected plate-pair systems spend finite
deformation budgets on broad collision roots, subduction belts, rifts, and
transform relief instead of independently raising each boundary triangle.
Those budgets construct both flexed roots and narrower resistant summit rock.
Crustal support measures deformation from a composition-specific foundation,
then adds thermal subsidence and explicit loads; regional flexure converts that
support into the bedrock datum without erasing local relief. Ocean coverage
calibrates one sea datum rather than chasing later terrain edits. Margin
evolution leaves ordinary landward relief intact and adaptively grades shelves
and continental rises into the ambient basin; convergent margins retain rare
trenches, and marine fill is limited to previously exported sediment.
Convergence between unlike transported continental assemblies leaves material
sutures whose pair identity and shortening strength move with the crust; those
inherited systems preserve major interior ranges after active boundaries move.
It applies sparse impacts and moves crust across
stable mantle roots to construct seamount chains, islands, cones, calderas, and
lava plains before climate and hydrology. It then erodes valleys, labels connected land and ocean, solves
bathymetry and drainage, derives reduced basin currents and wave exposure,
builds one shared atmospheric/upwind field for moisture, rain, and snow, and evolves a persistent bedrock and
sediment surface through fluvial, coastal, and glacial erosion and deposition
across four configurable geological epochs. Bedrock removal lowers the surface
and records unloading; a later support/flexure pass produces regional rebound.
A mass-conserving mass-wasting pass gives each unstable source one bounded
outgoing budget, and a post-incision pass lets gorge walls respond to rivers
cutting toward a discharge-conditioned equilibrium grade.
Incremental ice mass and flux leave cumulative glacial history from which
present and inherited landforms are classified. Outlet-rooted drainage and
basin-relative tributary retention keep classified rivers continuous through
lakes. Priority fill supplies only routing sills and outlet ranks. A separate
lake stage balances catchment and direct water input against evaporation and
infiltration, permits partial endorheic pools below the sill, and records
overflow for outlet incision only when the supported stage spills. The
authoritative drainage spine then expands across low-relief banks into
discharge-scaled channel triangles, adjacent cross-valley floodplains, and
sediment-rich delta fans without changing the hex-scale runtime. Ice Remembers
interleaves asymmetric fire, aurora, and pressure passes with those epochs,
retains cumulative magic exposure and first-active epochs separately from the current fields, and
converts pressure-driven coastal stress to domain-neutral physical forcing.
Natural surfaces include frozen
ocean forms such as pack ice, sea ice, ice shelves, and polynyas; anomaly
surfaces remain sparse overrides while their flags and intensity fields are
retained independently. Ordinary cold land is not collapsed into generic ice:
polar barrens, wet tundra, cold steppe, scree, river valleys, tundra, ridges,
snowfields, and glaciers remain distinct outcomes. Every physical and domain
generation pass declares its inputs and outputs. Causal controls live in
tunable profiles, while ocean, ice, landform, basin, and flow-direction
distributions are non-blocking diagnostics rather than acceptance ranges.
Structural invariants and fixed-seed Release renders decide acceptance.
Ordinary surface views retain structural water and restrained
relief shading at both aggregate and microtriangle zoom levels.

Physical material and surface vocabularies are separate from gameplay terrain.
Scored authored rules turn each arranged 24-triangle aggregate into one base
terrain for the current hex-scale simulation. Only that resolved base terrain
and its superregion/continent ids enter the kernel; triangle details and the
processes that created them remain in the immutable startup blueprint,
projection, cache, and UI. The physical map renders those external facts
through a zoom-dependent, chunk-cached geography layer: aggregate fields at
distant zoom and all 24 triangles plus connected physical features at close
zoom. Normal play shows only materialized geography; an explicit inspection
mode can reveal the complete latent generation field for diagnosis. The
close-zoom renderer uses triangle features only, while distant aggregate edge
masks have a single owner per shared edge. Surface views composite water beneath
sea ice and other ocean surfaces so frozen seas still read as seas. The
generation controls also provide a read-only 3D quality-control mode. Entering
it pauses playback, hides the simulation map and entity layers, and displays the
complete latent microtriangle field as displaced chunk meshes with actual
lighting, water surfaces, structural river lines, feature markers, wireframe,
and adjustable vertical exaggeration. It preserves the selected generation
view and restores the prior map plane and playback state on exit; it does not
make simulation play or entity interaction three-dimensional. The
semantic plane is an analogous bounded axial q/r map with `s = -q-r`, its own
varied terrain, culture influence, roads, and placement. It is not a coordinate
copy of the physical plane and does not use microtriangles.

Domain content is authored in `DomainConfig` and Ice Remembers-specific files.
Reusable gameplay vocabulary lives in `Engine.Gameplay` and compiles down into
primitive kernel program data. The kernel executes only ids, numeric codes, and
fixed-capacity arrays.

Shared additive state is deterministic fixed point in the kernel. Culture mass,
relationship strength, persistent attributes and health, road quality/work, and
persistent local modifiers each have an independent scale suited to their
range. Candidate scoring, probabilities, heat, ratios, and path costs use the
generic signed `FixedScalar` representation. Authored values may remain
ordinary floats before compilation, while presentation may convert after the
query/event boundary; compiled records and kernel execution are integer-backed.
Hot scoring shapes use bounded fixed-point rational or polynomial operations.
The kernel does not retain generic exponential, logarithm, or power
compatibility functions for those shapes.

Named entities and relationships use reusable internal slots. Any identity that
crosses an event, command, provenance, chronicle, gambit, district, or snapshot
boundary is a generation-safe occurrence handle, so retiring and later reusing
a slot cannot reconnect old behavior to a new occurrence. Active vectors and
incremental graph/spatial indexes drive iteration; retirement clears the
occurrence-owned direct rows.

Runtime map coordinates have stable dense cell IDs. Terrain, occupancy,
coverage, roads, travel memory, culture totals, and culture influence use direct
cell-backed storage with active/dirty indexes. Culture influence rows disappear
from active work when their mass reaches zero. Snapshot format `53` stores only
live state, handle-generation metadata, and the phase revision. It
rebuilds derived indexes, free lists, due queues, and route caches after load
and does not retain sequential RNG state.

Each tick has explicit command-admission, local-view preparation,
frozen local evaluation, deterministic local resolution, strategic, and
publication phases. Low-altitude verbs, relationship maintenance, effects,
culture decay, and minor actors observe the frozen active set and contribute
typed movement, attribute, culture, relationship, tag, cell, and lifecycle
deltas. Destination-owned reduction makes their result independent of
evaluation order and logical shard count. Gambits, generators, chronicles,
world beats, culture slots, knowledge, and map frontier remain strategic
programs on the same tick scheduler. Gambits and generators evaluate through
program-scoped frozen intent epochs: gambit actors and generator definitions
can be evaluated by the fixed worker set, and their entity, relationship, cell,
travel-structure, and event outputs resolve through typed destination owners.
The coordinator reserves structural IDs deterministically and reconciles
cross-owner graph, spatial, active-index, and district structures. The other
strategic programs remain coordinator-owned.

## Runtime Altitudes

Low altitude is always-on per-tick physics. It keeps the world visibly alive,
ages entities, drifts existing relationship strength, and reads direct locality
facts where actors work. It does not create named entities or declare major
story outcomes.

Minor actors are the low-salience labor layer. They are unnamed cohorts stored
inside a kernel program, not graph entities. Every cohort belongs to an active
named source and executes one of that source rule's authored verbs; it retires
when its source retires. Direct source-kind, source-rule, active-slot, and due
indexes keep their frequent local culture, road, and lifecycle work bounded.

Gambits are high-stakes entity actions. A gambit has an actor, target, intrinsic
success criteria, and effects such as creating or changing relationships. A
gambit may use shared behavior bytecode, shared query predicates, and shared
effect execution, but it is not the removed low-altitude process system or the
removed generic planning program.

The authored boolean precondition tree compiles to one bounded short-circuit
branch plan. Candidate-independent actor values are computed once, and a
relationship predicate required on every successful branch supplies a direct
relationship-index target cursor. This changes evaluation order, not authored
eligibility, scoring, or weighted selection probability.

Gambit repetition is controlled by the actor's current per-gambit and
whole-actor expiry ticks. The runtime does not retain lifetime action history or
apply an implicit recency penalty. Retirement clears the occurrence's cooldown
row.

Generators are intermittent high-altitude events. They create named entities
and relationship bundles when their preconditions are met. Generator selection
should be shaped by the state created by low altitude and gambits.

Both map planes grow from radius-18 hearts into deterministic latent geography
bounded at radius 128. Physical cells open where entities actually arrive:
reveal is one-way and entity-driven (per-kind radii on movement and
materialization), with the culture field acting as home-base support rather
than the reveal mechanism; semantic cells open continuously where idea-mass
presses. Ice is a separate dynamic cover layer over frontier-grown ground:
freeze ages advance impassable Deep Ice where cold pressure beats local
resistance (mass, roads, knowledge), and thaw ages retreat it, exposing the
durable base terrain rather than replacing it. Reach across the map is priced by travel
cost: a `WithinTravelReach` gate line-samples road-discounted terrain cost, so
frozen frontier blocks reach and roads extend it. See
[systems/26-growing-map.md](./systems/26-growing-map.md).

World beats are the macro-chronicle: authored Ages on three regime tracks
(Ice, Peoples, Memory) recognized from macro world state - population, wars
living and ended, culture mass and its growth, canons, legendary resolutions.
Living and ended relationship totals are updated at relationship creation and
retirement, so recognizing an Age does not rescan a lifetime relationship
archive.
An open age charges ordinary authored content (generator weights, verb
weights, gambit scores, the decay tide); the Age of Striving arms campaigns,
whose labor columns are the world's mass-projection at contested fronts. See
[systems/25-world-beats.md](./systems/25-world-beats.md).

Knowledge is claimed spatially rather than researched through a queue. The 27
authored technologies occupy tiered semantic-map sites; current culture claims
activate explicit grants over existing verbs, generators, gambits, movement,
relationships, locality, culture fields, minor actors, and frontier rules.
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
- `Locality`: direct current terrain capacity, occupancy, crowding, support,
  threat, and attractor queries at the bounded point of use.
- `CultureLocality`: direct radius-two culture mass, share, foreign pressure,
  local connection, cell-kind, hole, and strength facts, cached with exact
  radius-two invalidation when source culture mass changes.
- `CultureFieldDecay`: slow global decay of raw culture mass so territory
  reflects current production against loss instead of accumulated history.
- `Relationships`: existing relationship strength drift and culling over
  frozen sorted adjacency plus bounded sorted per-row live deltas.
- `Effects`: one per-entity resolution stage for relationship and locality
  effects, district healing, lifecycle drain, and retirement.
- `MinorActors`: persistent unnamed cultural fronts, road crews, and campaign
  columns with bounded assignments, macro movement, local encounters, and
  source-owned culture or road work without graph nodes.
- `Knowledge`: culture-scoped claims and modifiers derived from authored
  semantic-map sites, recomputed only when that culture's relevant state
  revision changes.

The current default tick order is:

```text
Movement
LowAltitudeVerbs
CultureFieldDecay
Relationships
Effects
Gambits
ChronicleDirector
Generators
WorldBeats
MapFrontier
CultureSlots
Knowledge
MinorActors
```

Locality facts are calculated from current bounded state when a consumer acts;
there is no map-activity or map-signal tick phase. Culture classification is
also local and read-only. Only scheduled mutating programs participate in tick
ordering.

Every `AdvanceTicks` iteration advances one tick. Movement, low-altitude verbs,
relationships, effects, gambits, chronicles, generators, and minor actors each
receive one opportunity in that tick. Program-specific durations are direct
values on the same clock. There are no sub-ticks, cadence mappers, or secondary
calendar counters.

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
effect primitives; fire-window budgeting, target selection, scoring, and production
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
- [22 Locality Queries](./systems/22-map-signals.md)
- [23 Tags And Status State Machines](./systems/23-tags.md)
- [24 Canons And Jurisdictions](./systems/24-canons-and-jurisdictions.md)
- [25 World Beats](./systems/25-world-beats.md)
- [26 Growing Map](./systems/26-growing-map.md)
- [27 Knowledge and Technology](./systems/27-knowledge.md)
- [UI Principles](./design/ui-principles.md)
