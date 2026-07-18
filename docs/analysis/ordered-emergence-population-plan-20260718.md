# Ordered Emergence: Population, Density, Movement, and Retirement Plan

Status: investigation-backed proposal, not approved for implementation.

This plan continues `entity-population-growth-20260718.md` using direct code
inspection, registered Monte Carlo evidence, and the design corrections made in
the accompanying review. It does not impose a fixed cast size. Its goal is to
make population, territory, movement, clustering, and retirement form a
coherent emergent system on both the physical and semantic planes.

No simulation or UI change is implemented by this document.

## Design Goal

Impose order on a chaotic simulation through authored incentives, competing
pressures, and feedback loops rather than hard placement rules or population
caps. The resulting world should remain emergent:

- growing physical and semantic territory supports a growing cast;
- mortal populations churn while growing modestly with usable territory;
- artifacts, regions, POIs, settlements, factions, canons, and other durable
  things can pass out of the zeitgeist without ceasing to exist historically;
- cities develop clusters and factions develop holdings;
- clusters remain productive rather than becoming inert piles;
- explorers make sustained progress, reinforce a frontier, or follow a route
  instead of spending most of their movement oscillating locally;
- retired entities may leave permanent effects on hosts or landscape;
- historical identity and provenance remain available for a later, optional
  reactivation mechanic.

The primary user-facing failure is clutter and illegibility. This plan owns
simulation structure and balance only. Raw UI rendering performance is a
separate effort.

## Revised Failure Statement

Unbounded population growth is not inherently a defect. The defect is that
population growth, usable-map growth, spatial distribution, movement purpose,
and retirement are weakly coupled.

The current simulation produces two related failures:

1. Active stock grows without maintaining an intentional relationship to the
   usable physical and semantic maps.
2. Much of the resulting activity is churn without legible progress: entities
   remain concentrated around old areas, repeat local relationship work, or
   traverse many steps while producing little net displacement.

Some clustering is desirable. Uniform distribution would be as wrong as the
current pile-ups. The target is a hub-and-frontier shape: mature cities,
faction holdings, routes, contested borders, and comparatively sparse areas
between them. Clusters should have an identifiable reason to exist and should
generate outward consequences.

## Current Evidence

The current registered evidence run is
`knowledge-real-grants-postrepair` (seed 4200, 15,000 ticks). Material ad hoc
findings have been recorded on that run with the Monte Carlo registry CLI.

### Map growth and density

Active entities grow from 44 to 202 over the run.

| Measure | Tick 0 | Tick 15,000 |
|---|---:|---:|
| Physical known cells | 1,027 | 1,139 |
| Physical covered cells | 440 | 525 |
| Semantic known cells | 1,027 | 1,401 |
| Semantic covered cells | 56 | 110 |

At tick 15,000:

- the physical plane has 202 active entities on 139 occupied coordinates,
  averaging 1.453 per occupied coordinate, with a maximum stack of 6;
- the semantic plane has 202 active entities on 186 occupied coordinates,
  averaging 1.086 per occupied coordinate, with a maximum stack of 2;
- 84 active entities never changed physical coordinates during their observed
  lifetime;
- 135 never changed semantic coordinates;
- 34 changed neither coordinate.

Exact-coordinate stacking is therefore currently more severe physically, but
the semantic failure is still real: active stock, semantic-map utilization,
and semantic relevance do not advance together.

### Activity without progress

During the final 5,000 ticks, 199 of 202 final-active entities participated in
at least one gameplay movement, gameplay-authored relationship mutation, or
gambit event. The cast is not literally dormant in runtime terms. The problem
is that much of its work is repetitive, local, or strategically inconclusive.

Physical immediate-step reversal rates are:

| Kind | Moves | Immediate reversals |
|---|---:|---:|
| Artifact | 87,446 | 49.7% |
| Transport | 17,357 | 67.4% |
| Penguin | 33,149 | 30.9% |

Median net-displacement/path-length efficiency over moving 1,000-tick windows
is 2.8% for artifacts, 0% for transports, and 8.8% for penguins.

This matches the movement implementation. `Explore` selects a known,
traversable coordinate using distance, recency, route, and saturation weights;
it does not directly value an unknown frontier sector or a contested cultural
front. After reaching an intent destination, the actor performs uniformly
random adjacent movement inside the dwell locality. That local roam produces
visible backtracking by construction.

Only 7.0% of physical movement and 2.9% of semantic movement entered cells
added after startup. Expansion cells received 4.9% of physical and 9.2% of
semantic absolute culture-influence change. These are not final balance
targets, but they show that map expansion and actor purpose are not yet a
single loop.

### Cluster shape

Of the coordinates that are stacked at tick 15,000, only 30.2% of physical
coordinates and 50.0% of semantic coordinates were also stacked at tick
14,000. Exact physical piles are comparatively volatile; semantic pairs are
more persistent. Broader region-scale clustering still needs a dedicated
radius/component analysis because exact-coordinate persistence does not fully
measure a city, holding, route, or front.

### Evidence limits

The earlier registered 100,000-tick run is useful structural evidence for
retained stock, retirement paths, and long-horizon accumulation, but it
predates the current growing-map implementation and is not a current density
baseline. It must not be used as if its map/population ratio were produced by
the current runtime.

The current 15,000-tick run has no low-altitude selection trace. Movement
events demonstrate reversal and poor progress, but do not attribute each move
to a particular verb, target mode, travel phase, or dwell phase. That
attribution requires a trace-enabled confirmation run.

## Target Population Model

Population should be in a dynamic density equilibrium, not a fixed-count
equilibrium.

For each kind `k`, define a relevant usable-territory measure `U_k(t)` from
the plane or planes that kind actually occupies. It may combine known cells,
covered cells, culture-supported cells, connected components, holdings, and
settled/frontier structure. Raw map-cell count alone is not sufficient.

The intended long-horizon relationship is:

`dN_k / dt ≈ rho_k * dU_k / dt`

with continuing creation and retirement on both sides of that net change.
`rho_k` is authored per kind or subtype; it is not one global density.

Consequences:

- penguins, orcas, transports, occurrences, and other mortal kinds should
  exhibit real churn and modest positive net growth while usable territory
  grows;
- their old absolute equilibrium is not the final target;
- artifacts, regions, POIs, settlements, factions, and canons may have much
  longer relevance lifetimes, but they still need ways to become historical;
- active stock may plateau only when the relevant usable map plateaus;
- bootstrap can legitimately fill faster than the long-run rate;
- the historical graph keeps growing unless a separate archival policy is
  later designed.

No exact density band is ratified yet. It must be derived from multi-seed,
current-runtime evidence and UI/gameplay legibility, not copied from one run.

## Four Coupled Knobs

These knobs are complementary. No one knob should be expected to impose the
desired order alone.

### K1. Territory-coupled creation and churn

Creation and retirement pressure should respond softly to relevant usable
territory and mature local structure.

Candidate mechanisms:

- make appropriate generator opportunities depend on settlement, holding,
  culture-support, route, frontier, or component structure rather than only a
  global pulse;
- let successful map expansion create new opportunity without guaranteeing a
  birth quota;
- let stagnant or overcrowded components reduce creation fitness or increase
  retirement pressure;
- preserve local causal limits and authored fiction;
- tune each kind independently so mortal populations grow modestly while
  durable kinds grow more slowly and turn over through relevance lifecycle.

Primary owner: `src/IceRemembers/IceRemembersGeneratorDefinition*.cs` and
`src/IceRemembers/Entities/*Entity.cs`.

Diagnostic engine surfaces:
`src/Engine.Kernel/Programs/Generators/` and
`src/Engine.Kernel/Programs/Upkeep/UpkeepProgramRuntime.cs`.

Engine runtime changes are warranted only if usable-territory feedback cannot
be expressed as reusable gameplay vocabulary or authored gates.

### K2. Purposeful movement and commitment

Movement should choose a purpose and retain it long enough to alter the world.
Different actors need different spatial jobs:

- explorers select and continue into a frontier sector until they discover,
  cover, influence, or connect something;
- factions and their agents reinforce contested cultural boundaries or extend
  a coherent holding;
- transports connect holdings, ports, routes, and frontier supply rather than
  oscillating around a reached destination;
- artifact movement follows custody, circulation, pilgrimage, theft, return,
  or installation arcs;
- semantic movers reinforce or contest idea-space fronts instead of merely
  spraying across known coordinates.

The first pass should tune authored verbs, weights, commitment lengths, dwell,
and competing opportunities in
`src/IceRemembers/IceRemembersLowAltitudeVerbs.cs`.

If the investigation confirms that frontier-sector and contested-front
targeting cannot be expressed, add domain-agnostic targeting vocabulary in:

- `src/Engine.Gameplay/LowAltitudeVerbConfig.cs`
- `src/Engine.Gameplay/LowAltitudeVerbShapes.cs`
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramData.cs`
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.Candidates.cs`
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.Weighting.cs`
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.MovementShape.cs`

The target selector should remain probabilistic. It supplies weighted
opportunities and persistent intent, not a forced assignment or scripted
route.

### K3. Productive clustering and carrying capacity

Clusters should be attractive while they are useful and dispersive when they
become stagnant.

The feedback shape should be approximately:

1. settlements, holdings, routes, support, and relationships attract relevant
   actors;
2. co-location improves coordination, safety, culture, trade, or gambit
   opportunity;
3. benefits diminish as local capacity fills;
4. excess actors receive increasing incentives to depart, found, extend a
   route/front, retire into a host, or lose relevance;
5. a mature cluster leaves durable structure rather than an indefinitely
   growing foreground cast.

Existing map crowding, capacity, support, threat, attraction, culture slots,
and saturation are inputs to this loop. The task is to tune and connect them
to behavior and lifecycle, not merely increase a universal damage number.
Strong hubs should be able to support more than weak sites. A cluster of six
may be correct for a major city and wrong for an empty ice hex.

Primary authored surfaces:

- `src/IceRemembers/IceRemembersDefinition.cs`
- `src/IceRemembers/Entities/*Entity.cs`
- `src/IceRemembers/IceRemembersLowAltitudeVerbs.cs`
- `src/IceRemembers/IceRemembersGambits*.cs`

Diagnostic engine surfaces:

- `src/Engine.Kernel/Programs/MapSignals/`
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.Weighting.cs`
- `src/Engine.Kernel/Programs/Effects/EffectsProgramRuntime.cs`

### K4. Relevance lifecycle and authored retirement

Lifecycle means leaving the foreground zeitgeist, not necessarily physical
death or destruction. It is valid for artifacts, regions, POIs, settlements,
factions, canons, gambits, occurrences, and mortal actors.

Use a mixture of:

- passive relevance decay where fiction supports gradual fading;
- carrying-pressure effects where an overcrowded or unsupported entity loses
  relevance;
- gambit outcomes that resolve wars, monsters, factions, settlements,
  artifacts, and other long-running situations;
- generator consequences that transform a completed pattern into history;
- voluntary self-retirement that turns an entity into durable host or
  landscape state.

This is not a command to apply one lifecycle band to every member of a kind.
Subtypes and circumstances should deliberately mix long-lived anchors,
ordinary relevance lifetimes, and authored endings.

Primary surfaces:

- `src/IceRemembers/Entities/IceRemembersEntityDefinition.cs`
- `src/IceRemembers/Entities/*Entity.cs`
- `src/IceRemembers/IceRemembersGambits*.cs`
- `src/IceRemembers/IceRemembersGeneratorDefinition*.cs`
- `src/IceRemembers/IceRemembersDefinition.cs`

## Legacy Retirement Gambits

Dynamic meta-entities are not part of this plan. A smaller and more coherent
mechanic is an entity-authored legacy retirement gambit.

Example: an artifact that has been held by a settlement or faction for a
meaningful period installs itself permanently in a museum, shrine, archive,
ward, standard, or foundation.

The atomic result is:

1. validate a mature host relationship, locality, condition, and narrative
   reason;
2. end or transform the artifact's active custody/circulation relationships;
3. materialize a permanent gameplay consequence on the host or landscape;
4. preserve an archival relationship from the retired artifact to its host;
5. transition the artifact itself to historical status;
6. remove it from ordinary UI, targeting, map signals, and hot graph queries;
7. retain its identity and provenance for history-facing queries and a later
   optional return mechanic.

Current hot-graph behavior is not a design restriction. Today, relationships
with a historical endpoint are archival and do not emit active effects. If a
permanent museum benefit is desired, the implementation must add or materialize
the durable state required to provide it. The model can be changed wherever
necessary.

Two representations should be evaluated:

### Fungible legacy

If the effect is an accumulated resource such as prestige, capacity,
authority, memory, or protection, materialize it as explicit durable
host-owned state and retain the artifact-host relationship only for history.
This is the smaller design.

### Typed installed legacy

If multiple installed entities must retain distinct mechanical identities,
stack independently, be queried by subtype, or later be reactivated, add a
fixed-capacity installed-legacy/attachment component. It should record at
least:

- historical source entity ID;
- active host entity ID or landscape coordinate/coverage owner;
- registered legacy kind ID;
- magnitude or authored parameters;
- installation tick and active/removed state.

This is not a meta-entity. It is durable state owned by the host or landscape,
with the original entity preserved historically.

If approved, the likely reusable implementation surfaces are:

- `src/Engine.Domain/Behaviors/Mutations.cs`
- `src/Engine.Domain/Compilation/DomainCompiler.cs`
- `src/Engine.Domain/Compilation/KernelPrograms/GambitProgram.cs`
- `src/Engine.Kernel/Programs/Gambits/GambitProgramData.cs`
- `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.Execution.cs`
- `src/Engine.Kernel/WorldState.cs`
- `src/Engine.Kernel/Persistence/KernelSnapshot.cs`

The Ice Remembers authoring would live in a focused partial file:

- `src/IceRemembers/IceRemembersGambits.Retirement.cs` (new)
- `src/IceRemembers/IceRemembersGambits.cs`
- `src/IceRemembers/IceRemembersSchema.cs`
- relevant `src/IceRemembers/Entities/*Entity.cs` and
  `src/IceRemembers/IceRemembersDefinition.cs` only when they own the durable
  host benefit.

An explicit reusable self-historical mutation should be authored rather than
depending on an implicit endpoint fallback. Retirement plus legacy
materialization must execute atomically so failure cannot leave half-installed
state.

The first artifact retirement gambits should be narrow and causal, not a
universal random sink. Candidate gates include sustained custody or anchoring,
host maturity, co-location, artifact condition/age, completed chronicle or war
context, and the absence of a prior installation.

Later reactivation is not required by this plan. The data shape must avoid
making it impossible: historical identity, host provenance, and the installed
benefit must be reversible or transformable by an explicit history-facing
mechanic.

## Evidence and Tuning Sequence

### Phase 1: Ratify measures before changing behavior

Run current-code, multi-seed evidence at 25k, 50k, and at least 100k ticks.
Include low-altitude trace on a representative subset. Record:

- active, created, retired, and historical counts by kind and subtype;
- usable-map denominators by plane and kind;
- creation and retirement rates per unit of map growth;
- exact-stack and radius/component cluster distributions;
- cluster age, membership turnover, and cluster productivity;
- movement reversal, path efficiency, destination novelty, frontier entry,
  route adherence, and contested-front reinforcement;
- generator surviving entity-ticks and self-reopening loops;
- gambit offer, selection, success, resolution, and retirement rates;
- crowding/support exposure followed by departure, founding, or retirement.

Every evidence run must use the Monte Carlo registry and a unique run ID.
Material SQL findings must be appended to the registry.

### Phase 2: Tune existing authored knobs

Before adding runtime vocabulary, test whether a balanced combination of
authored verb weights/commitments, generator conditions, lifecycle bands,
health effects, gambit rates, and map signals produces the desired shape.

Compare matched seeds against Phase 1. Do not accept a lower active count if it
also destroys map utilization, mortal growth, clustering, or narrative churn.

### Phase 3: Add missing reusable vocabulary

Only after attribution demonstrates the gap, add the smallest domain-agnostic
vocabulary needed for:

- frontier-sector or contested-front movement targeting;
- explicit self-retirement;
- atomic durable legacy materialization;
- history-facing reactivation later, only if separately approved.

Before editing any kernel runtime file, read and follow
`docs/kernel/simulation-kernel-discipline.md`.

### Phase 4: Add Ice Remembers retirement content

Author a deliberate mix of endings and transformations, beginning with a
small set of high-volume or high-clutter patterns. Validate that these gambits
make clusters more meaningful rather than merely hiding entities.

### Phase 5: Long-horizon validation

Use matched multi-seed runs to check the entire coupled outcome. The success
criterion is ordered emergence, not simply fewer entities.

## Acceptance Shape

Exact numeric bands remain to be ratified, but a successful design must show:

- active stock grows with relevant usable territory on both planes;
- mortal stock has ongoing turnover plus modest positive growth during map
  expansion;
- long-lived kinds have observable, fictionally coherent retirement paths;
- physical and semantic density stop diverging from their usable maps;
- large clusters are mostly associated with settlements, holdings, routes,
  fronts, or named events;
- mature clusters generate outward movement, new structure, transformation,
  or retirement;
- artifacts and transports show materially lower immediate reversal and
  higher progress per path length;
- frontier and contested-boundary activity rises without scripted allocation;
- historical identity is preserved;
- the UI receives a more legible foreground cast without becoming the source
  of simulation truth.

## What Will Not Change Under This Plan

- No arbitrary global active-entity cap.
- No uniform-distribution rule or blanket anti-clustering penalty.
- No forced city, holding, route, or frontier assignments.
- No deletion of historical entities as a density fix.
- No dynamic meta-entity/grouping system.
- No revival of the removed planning/process deck as the default
  low-altitude behavior model.
- No UI performance implementation in this simulation plan.
- No assumption that the existing runtime vocabulary limits what can be
  designed; engine changes remain available when the desired reusable mechanic
  genuinely belongs there.

## Approval Boundary

This document is the proposal required before simulation implementation. No
code, tuning, tests, telemetry schema, or stable architecture documentation
should change until the intended first implementation slice and its exact
files are approved.
