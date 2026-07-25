# Pre-Parallelization Simulation Redesign Plan

Status: Phases 1-5 complete on 2026-07-24. Completed phase results describe the
current implementation; Phase 6 and later remain unimplemented directions.

Phase 1 code basis: `main` at `7344037` on 2026-07-24.

Durable plan: `7a9bf1a1-5633-4ee9-884b-411fc7db7758`.

Primary inputs:

- `docs/kernel/kernel-parallelization-brief.md`
- `.sulion-paste/paste-2026-07-24_17-34-35-182Z.txt`
- `docs/kernel/simulation-kernel-discipline.md`
- the current kernel, gameplay compiler, Ice Remembers content, projection,
  telemetry, and snapshot implementations
- the cumulative design corrections recorded below

## Purpose

The simulation is too slow at long horizons, and the first parallel experiment
flattened around `1.2x` because it parallelized evaluation while leaving
mutation, indexing, eventing, provenance, and most aggregation on a serial
commit path.

The next attempt must not begin by distributing the current kernel unchanged.
The current kernel is an implementation, not a preservation specification.
Several expensive programs and lifetime stores exist only to preserve narrow
behaviors or observer contracts that may be simplified or removed.

This plan therefore has three goals, in order:

1. remove simulation work and state that do not justify their cost;
2. rebuild the surviving serial simulation around active-only storage,
   field-specific fixed-point values, explicit epochs, and typed deltas; and
3. choose no parallel architecture, subsystem offload, a bulk-synchronous
   phase graph, or spatial shard actors from evidence gathered on that smaller
   model.

History retention is one preparatory workstream inside this redesign. It is not
the organizing purpose of the work.

## Cumulative Corrections And Durable Constraints

The following corrections are cumulative. A later phase must not silently
reintroduce an earlier rejected assumption.

1. Do not begin with a 25k, 100k, or multi-hour Monte Carlo run. Initial
   evidence is limited to existing runs, static consumer tracing, and a short
   bounded Release diagnostic. Long registered runs are validation after the
   kernel is materially faster.
2. Kernel code, program boundaries, tick ordering, and intermediate state may
   be replaced completely. Only the broad shape of the simulation is required.
3. Storage and fixed-point conversion are explicit workstreams before choosing
   a parallel architecture. They are not optional details hidden under a
   generic "parallel substrate" phase.
4. Rewriting minor actors into aggregate labor is not a prerequisite. The
   individual unnamed-cohort model remains a separable design decision.
5. Lifetime arrays must be justified by a current query or simulation need.
   Retired state is not retained merely because a current behavior happens to
   consult it.
6. Artifact reawakening does not preserve an artifact occurrence. Installing
   an artifact ends that occurrence. Reawakening creates a new artifact from
   an authored district template and creates new relationships.
7. Gameplay changes that preserve the broad role of a mechanic while removing
   state, work, or synchronization are desirable, not compatibility failures.
8. Same-seed repeatability under the new model is the intended determinism
   invariant. Exact legacy event history, RNG consumption, IDs, and
   same-pulse outcomes are not.

## Proposal Contract

### Owning layers

- `Engine.Kernel` owns active simulation state, bounded scratch, deterministic
  pulse execution, conflict resolution, and the primitive event boundary.
- `Engine.Gameplay` and `Engine.Domain` own reusable authored vocabulary and
  compilation into the new primitive representation.
- `IceRemembers` owns gameplay substitutions where an existing behavior is too
  expensive or requires unnecessary history.
- `Engine.Projection` owns user-facing active views and compact narrative
  records.
- `Engine.Observability` owns sampled and streamed analysis history outside the
  simulation state.
- `Engine.Host` owns configuration, optional subscribers, and persistence
  coordination outside the hot simulation path.

### Observed failure

The kernel repeatedly scans lifetime high-water ranges, rebuilds global derived
tables, mutates open-addressed structures in hot paths, records history inside
simulation storage, advances a global version and provenance stream for small
scalar writes, and relies on immediate ordered mutation. These choices create
both serial cost and synchronization boundaries.

The first parallel branch retained most of those boundaries and therefore
could not scale beyond the parallel evaluation fraction.

### Causal hypothesis

If the simulation:

- stores only concurrently active state;
- replaces polling and global materialization with direct or incremental facts;
- uses dense layouts and preindexed authored rules;
- batches additive changes in deterministic fixed-point deltas;
- separates logical occurrence identity from reusable storage slots;
- publishes semantic events instead of recording every scalar mutation; and
- proves order-independent resolution on one thread;

then the serial kernel should become substantially faster and the remaining
work should expose honest entity-, relationship-, or destination-cell
parallelism. If the serial result is already sufficient, no in-run
parallelization is required.

### What this proposal does not preserve

- exact legacy output or event order;
- old entity or relationship IDs;
- retired entity or relationship rows in kernel state;
- exact artifact identity across reawakening;
- the current raw/main tick split;
- the current list or ordering of kernel programs;
- current float rounding;
- current per-mutation `WorldVersion` semantics;
- old binary snapshot compatibility;
- general historical entity browsing after a reload;
- a cold history archive or synchronous history hydration path.

### What this proposal preserves

- named entities are created, act, move, relate, age, and leave the world;
- active relationships influence ordinary and strategic behavior;
- cultures occupy, reinforce, contest, and lose territory on both maps;
- unnamed minor cohorts perform visible local labor;
- gambits remain entity-authored high-stakes actions;
- generators remain high-altitude sources of new named entities and major
  developments;
- chronicles and world beats recognize and steer emergent stories and regimes;
- maps grow and retain meaningful terrain, coverage, roads, and cultural
  patterns;
- the simulation remains bounded, deterministic under the new model,
  allocation-free on hot paths, and domain-agnostic inside `Engine.Kernel`;
- the application remains designed for an approximately 4 GB working set, with
  up to 8 GB acceptable for a simpler durable layout.

## Re-Review Method

The review began with the design and user corrections, then traced each claimed
contract to its actual runtime and external consumers.

For every subsystem the disposition is one of:

- keep the model unchanged;
- tune the current algorithm;
- preserve the role while replacing the implementation;
- merge it into another phase;
- move it out of the kernel; or
- remove it.

The review does not infer that a field must survive because a current test,
projection, or narrow gameplay operation reads it. The consumer is also subject
to redesign.

## Subsystem Dispositions

| Subsystem | Current cost or accidental contract | Disposition |
| --- | --- | --- |
| Entity lifetime | Retired entities occupy direct arrays forever and keep IDs meaningful to chronicles, projection, and artifact reactivation. | Replace with active slots and generation-safe occurrence handles. Retirement is final. |
| Relationships | Culled edges remain allocated as a historical graph; high-water scans and history-facing assumptions prevent slot reuse. | Active edges only. End incident edges on retirement. Preserve a cumulative fact only when a current mechanic needs one. |
| Artifact districts | Districts store a historical artifact ID so a gambit can reactivate the same entity. Inactive districts also consume lifetime slots. | Store an authored reawakening template, host, cell, and active bonus. Reawakening consumes the district and materializes a new artifact with new edges. |
| Gambit action history | Every entity reserves a 60-entry action ring for an implicit ten-main-tick score penalty and a generic precondition unused by Ice Remembers. | Remove the ring and implicit recency penalty. Use authored cooldown expiry. A future recency requirement gets a direct last-tick field. |
| Gambit cooldowns | Open-addressed `(entity, gambit)` keys accumulate for the run lifetime. | Direct active-slot expiry rows or kind-local rule spans, cleared on slot reuse. |
| Provenance | Fourteen invocation arrays and eight output arrays grow and copy for history mostly reconstructed by event projections already. | Keep a current invocation frame and compact causal token on active state. Stream completed semantic provenance; do not retain completed output rows in the kernel. |
| UI history | `UiProjectionState` retains historical entities and relationships because IDs remain globally meaningful. | Active entity/relationship views only. Chronicle and journal entries capture immutable rendered names and compact facts at event time. |
| Telemetry history | Historical detail is useful for analysis but does not define simulation behavior. | Stream or batch to parquet and release resident state. Headless runs do not construct unused UI projections. |
| MapActivity | Clears occupancy and scans every active entity on each main pulse to recreate presence. | Remove as a tick program. Occupancy comes from the spatial index; visits and heat update on movement and meaningful actions. |
| MapSignals | Clears tables, scatters from entities and relationships, gathers per entity, and scatters attractors. Few systems consume it. | Remove as a tick program. Replace consumers with direct terrain, occupancy, culture, active-edge, and local-target facts. |
| Culture boundaries | Lazy component caches rebuild global topology and make apparently read-only queries mutate shared cache state. | Replace runtime consumers with a local culture descriptor derived from own/foreign raw mass, share, and one/two-ring support. Compute component and hole metrics offline. |
| Culture field | Durable territorial state is central, but sparse hash layers, per-write rollups, invalidation, events, and float reductions are expensive. | Keep the model. Move to dense `CellId × culture` fixed-point slabs, batched deltas, and epoch-time derived winner/share fields. |
| Movement and verbs | Separate passes, global/high-water candidate scans, mutable shared scratch, and immediate visibility couple work. | Keep committed ordinary behavior and movement, but preindex rules and candidates, use active/spatial indexes, and resolve intents through deltas. Exact scoring and same-loop visibility may change. |
| Relationship maintenance | Active-edge drift is useful; follow movement and immediate cull/index mutation broaden the pass. | Keep strength drift. Move relationship-driven movement into entity intent or an explicit movement delta. Resolve culls after strength merge. |
| Effects and upkeep | Both scan active entities and apply health/attribute changes before a separate death check. Districts add another scan. | Merge into one entity-resolution phase. Accumulate contributions, apply once, then retire. Maintain host/cell district modifiers incrementally. |
| Minor actors | Per-cohort state is stored in hash tables; source emission scans entities and rules; actors query expensive culture components. | Keep individual cohorts. Move state to direct active slots, preindex emitters, use due lists and local culture descriptors, and emit typed culture/road/health deltas. Aggregate labor remains optional later. |
| Raw/main clocks | A five-to-one clock exists chiefly to give cohorts more frequent low-impact work and forces tuning conversions across systems. | Treat as disposable. Prefer one logical pulse with scaled cohort rates. If actor substeps are useful, keep them behind an actor-only scheduler rather than a second world clock. |
| Culture slots and knowledge | Rare derived systems rescan eligible state on cadence even when relevant inputs have not changed. | Retain their gameplay role but use dirty revisions, preindexed candidates, and due recomputation. |
| World beats | Scans lifetime relationships to count living and culled facts. | Maintain active counts and explicit cumulative transition counters. World-beat evaluation stays rare and small. |
| Generators and chronicles | Broad structural mutation and global story recognition are relatively rare but use global queries and provenance history. | Keep their roles in a deterministic structural lane. Replace historical queries with current facts, counters, bounded suppression, or compact causal tokens. |
| Map frontier | Rare structural map work is legitimate, but current activity dependency and broad scanning may be avoidable. | Keep map growth. Read direct occupancy/action facts and retain bounded cursor-based work. |
| Events and world version | Small scalar mutations synchronously append provenance/events and advance a global total-order version. | Publish semantic action, structural, retirement, and phase-completion events. Use pulse/phase revisions for state visibility. Sample dense numeric state rather than event every cell delta. |

## Retention Policy

Every proposed historical value must fit one of four categories:

1. **Active simulation state**: required to decide the next pulse. It remains in
   kernel storage only while active.
2. **Bounded recency state**: a cooldown, suppression window, or last-tick fact
   with an explicit maximum horizon.
3. **Cumulative simulation fact**: a small counter or regime statistic whose
   meaning is authored directly, such as wars ended. It is not a hidden copy
   of the historical graph.
4. **External record**: narrative, telemetry, debugging, or presentation
   history consumed outside the kernel.

If a requirement does not fit one of these categories, its gameplay contract
must be clarified before storage is added.

There will be no general cold entity graph, relationship archive, page
hydration protocol, or history query API in this plan.

## Artifact Installation And Reawakening

Artifact reawakening is the concrete example of replacing gameplay to unify
storage.

### Installation

1. Resolve the district bonus and a compiled `ReawakeningTemplateId`.
2. Add or update the host/cell's active district modifier.
3. Create an active district row containing:
   - host occurrence handle;
   - physical `CellId`;
   - bonus kind and fixed-point magnitude;
   - reawakening template ID.
4. Emit a semantic installation event carrying the retiring artifact's
   occurrence handle for the external record.
5. End every active relationship incident to the artifact.
6. Retire the artifact occurrence and recycle its runtime slot.

The district does not store the source artifact ID, traits, attributes,
relationships, or full provenance.

### Reawakening

1. The settlement gambit selects and consumes an active district.
2. Remove the district's active modifier and recycle the district slot.
3. Materialize a new artifact occurrence from the compiled template at the
   district cell.
4. Create the template's authored active relationship bundle.
5. Emit a semantic reawakening event linking the new occurrence to the
   consumed district and installation record for presentation only.

The new artifact may share a subtype or cultural theme with the installed
artifact, but it does not inherit runtime identity or old relationships.

If the host settlement retires first, its active districts end with it. The
terrain/coverage consequence may remain, but no detached district bonus or
sleeping artifact record survives.

## Work Elimination Design

### Replace MapActivity

- Maintain per-cell active occupancy through the existing movement/spatial
  index.
- Increment visits and action heat only when an entity arrives or performs a
  meaningful local action.
- Represent decay from `lastActionTick` rather than rewriting heat every pulse.
- Let map frontier check occupancy directly.
- Let UI and telemetry sample occupancy and heat from the compact cell state.

This intentionally stops treating an unmoving entity as a fresh visit on every
main pulse.

### Replace MapSignals

The current five fields become direct facts at the consumers:

- support: own-culture mass, shelter/coverage, and relevant positive active
  relationships;
- threat: foreign culture, local hostile occurrences, and relevant negative
  active relationships;
- crowding: spatial occupancy count;
- capacity: terrain and durable cell modifiers;
- attractor: computed during candidate scoring from actual target properties.

Effects and targeting need not share one globally materialized approximation.
Each consumes the smallest facts that express its authored intent.

### Replace Runtime Culture Components

The local descriptor contains only values cheap to compute or publish from the
dense field:

- own raw mass and share;
- strongest foreign raw mass and share;
- total raw mass;
- supported adjacent and ring-two counts;
- adjacent and ring-two raw totals;
- headroom and an authored local strength band.

Minor-actor border extension, bridging, and hole repair become local-shape
heuristics. Generator placement requiring the "same component" becomes
supported adjacency, a bounded local route test, or another authored local
condition. Exact connected-component identity is not preserved.

Telemetry can compute global components, holes, fragmentation, and territory
shape from sampled dense fields with DuckDB or Python.

### Reduce Observer Work

- Do not create UI projections in headless Monte Carlo runs.
- Do not retain completed provenance in the kernel and then reconstruct it
  again in projections.
- Emit structural and narrative events losslessly.
- Sample high-volume numeric fields on the configured telemetry cadence.
- Capture display names and narrative text when journal events are formed so
  retired entity records can be released.

## Low-Altitude Execution Direction

This plan keeps ordinary entity behavior and individual minor cohorts, but not
their present scheduling structure.

The intended serial shape is:

1. iterate dense active entity slots;
2. read kind-local compiled verb/effect/lifecycle/gambit spans;
3. use spatial, kind, culture, and active-adjacency indexes for bounded
   candidate selection;
4. produce movement, culture, road, relationship, attribute, and structural
   intents without immediate world mutation;
5. run due minor cohorts against the same frozen local field and produce the
   same delta vocabulary;
6. resolve destination-keyed changes;
7. apply lifecycle retirement after additive health resolution.

The current committed-intent model may remain if it still provides useful
movement continuity. Exact dwell, TTL, action cadence, weighting, and
same-pulse visibility may be retuned after the clock and field changes.

The aggregate-minor-actor alternative remains a separate later experiment. No
phase in this plan depends on it.

## Fixed-Point Direction

Fixed-point precision is field-specific, but the kernel boundary is not.
`Engine.Kernel` contains no `System.Single` or `System.Double` runtime state,
signatures, arithmetic, tables, event compatibility views, or snapshot fields.
All values compiled for kernel execution cross the compiler boundary as
integer-backed types.

### Convert

- raw culture mass and culture deltas;
- relationship strength and maintenance deltas;
- health and persistent attributes that receive additive deltas;
- road quality and road work;
- district and other persistent local modifiers;
- local candidate weights, probabilities, scores, ratios, heat, and path costs;
- every other numeric value stored or calculated by the kernel.

### Floating point outside the kernel

- authored JSON/C# configuration before compilation;
- diagnostic calculations outside the kernel;
- presentation values derived from fixed-point state.

### Required contract for every converted field

- authored and observed range;
- scale and representable precision;
- zero, minimum, maximum, and cull behavior;
- rounding rule at the compilation and API boundaries;
- intermediate width for multiplication and reduction;
- checked, saturating, or rejecting overflow behavior;
- deterministic clamp point;
- serialization representation;
- conversion used by telemetry and projection.

The fixed-point phase must produce this matrix before the storage rewrite
lands. It must use short probes or existing telemetry to establish ranges; it
must not guess a universal scale.

## Active-Only Storage Direction

### Entity storage

- fixed-capacity direct structure-of-arrays keyed by internal slot;
- free-slot stack;
- dense active-slot vector so iteration never scans retired high-water rows;
- generation-safe external occurrence handle so slot reuse cannot alias an old
  event or command;
- kind and culture active lists or spans;
- direct tag bits and compiled attribute/trait layout;
- per-slot program state cleared on retirement.

The occurrence handle should encode slot and generation so an active lookup
does not require a lifetime ID map.

### Relationship storage

- fixed-capacity direct active relationship rows;
- free-slot stack and dense active relationship vector;
- generation-safe relationship occurrence handle for events;
- source and target entity occurrence handles validated on use;
- active outgoing/incoming adjacency indexed by endpoint and kind;
- deterministic create deduplication by endpoint/kind key;
- immediate slot recycling after end events are published.

There is no allocated-but-inactive relationship row.

### District storage

- active rows only;
- host occurrence handle, cell ID, bonus, and reawakening template;
- free-slot stack;
- host-to-district active index;
- incrementally maintained host/cell modifier totals.

### Cell and culture storage

- stable dense `CellId` for each plane and known coordinate;
- direct terrain, cover, coverage, road, occupancy, visit, and modifier slabs;
- direct fixed-point culture raw mass by `(plane, culture, cell)`;
- active/dirty cell vectors for work that should skip zero cells;
- epoch-published total, winner, share, and local descriptor data where
  precomputation is cheaper than repeated direct calculation.

### Program definitions and state

- compile kind-to-rule and relationship-kind-to-rule contiguous spans;
- use due queues or next-due arrays for cooldowns, emissions, rare
  recomputation, and actor action times;
- remove lifetime action history;
- move named-entity work tier out of minor-actor cohort state;
- retain bounded recent-target and chronicle suppression state only where its
  authored behavior survives.

### Snapshot

The new snapshot format contains:

- active world state;
- active districts, relationships, cohorts, chronicles, and regimes;
- durable map and culture state;
- program definition/state required to continue;
- RNG/keyed-random seed and pulse;
- generation counters needed to prevent handle aliasing.

It does not contain retired entities, ended relationships, completed
provenance output, UI history, or compatibility shims for the old format.

## Serial Epoch And Delta Model

Threads are deliberately absent from this phase.

### Candidate pulse structure

1. **Admit**: validate commands and structural requests against the published
   pulse boundary.
2. **Evaluate entities**: read a frozen active epoch and produce ordinary
   movement/action intents.
3. **Evaluate local labor**: run due individual cohorts and produce culture,
   road, and health deltas.
4. **Resolve local state**: destination-key and deterministically merge cell,
   movement, attribute, and relationship-strength changes.
5. **Resolve lifecycle**: apply combined health, retire occurrences, and end
   incident relationships and hosted districts.
6. **Resolve strategic work**: evaluate due gambits, chronicles, generators,
   world beats, slots, knowledge, and frontier work in a bounded structural
   lane using the published phase state.
7. **Publish**: update indexes, publish the next epoch, and emit semantic
   events and a pulse-completed marker.

The exact number of phases is a design output of this work, not a requirement
to preserve the current seventeen-program order.

### Conflict rules

- additive fixed-point values sum in a defined intermediate width;
- competing absolute writes use an authored priority class followed by a
  stable logical source key;
- each occurrence may accept at most one movement result per phase;
- relationship creates deduplicate by canonical endpoint/kind key;
- relationship end wins over later strength updates in the same resolution;
- lifecycle retirement occurs after all accepted health deltas for the phase;
- structural allocations follow a deterministic sorted request order;
- semantic events follow logical phase and result order, not worker execution
  order.

### Randomness

Random draws are keyed by stable logical inputs such as:

`(seed, pulse, phase, occurrence handle, rule ID, purpose, attempt)`

Adding a rejected action or changing worker count must not shift unrelated
random streams.

### Gate

Before introducing a second execution thread, the one-thread resolver must
produce identical published state when:

- evaluation order is shuffled;
- delta buffer order is shuffled;
- logical shard count changes;
- equivalent work is split into different chunk sizes.

This is shuffle-invariance, not legacy equivalence.

## Phased Plan

### Phase 1: Define The Broad Outcome Contract

Owner: cross-layer design and analysis.

Status: complete on 2026-07-24.

#### Accepted outcome envelope

The preserved/disposable contract and subsystem dispositions earlier in this
document are the acceptance criteria. In particular:

- preserve the broad world loop: active named entities and minor cohorts act,
  move, relate, shape culture and maps, age, retire, and are replenished by
  gambits and generators;
- preserve the distinct gameplay roles of low-altitude verbs, gambits,
  generators, chronicles, world beats, lifecycle, relationships, maps, and
  individual minor cohorts;
- preserve bounded deterministic execution under the new model and semantic
  events sufficient for the active UI, narrative record, and analysis;
- allow different IDs, event order, pulse timing, RNG consumption, numeric
  rounding, exact counts, and exact same-pulse outcomes;
- allow gameplay substitutions that remove a historical dependency or global
  materialization while retaining the mechanic's broad role;
- do not preserve retired entity rows, ended relationship rows, inactive
  districts, completed provenance rows, same-instance artifact reawakening,
  the action-history ring, old binary snapshots, or a general history query
  API;
- keep individual minor actors as the default model, while allowing their
  storage, clock, scoring inputs, and execution organization to change.

These criteria judge the redesigned simulation against its own repeatable
contract, not against exact legacy output.

#### Short-run behavior checks

The routine check after each implementation phase is one registered Release
run of one seed for 1,000 ticks, sized to detect a dead or disconnected
simulation rather than to prove long-horizon balance. It checks:

- active named entities remain and both creation and retirement paths are
  reachable;
- physical and semantic movement occur across more than one cell;
- more than one culture retains nonzero influence and culture writes remain
  finite and bounded;
- minor actors perform labor and produce at least one authored culture, road,
  or lifecycle consequence;
- relationships are active and strength/create/end transitions occur;
- gambit attempts and generator evaluation continue;
- chronicle, world-beat, district, and map-frontier programs remain installed
  and can progress when their authored conditions occur;
- no capacity, snapshot, graph/index, determinism, or dropped-event invariant
  fails.

Zero fires from an intentionally rare gambit, generator, chronicle, or
reawakening in a 1,000-tick run is not by itself a failure. Long-horizon shape
is judged only after the kernel is materially faster.

#### Initial evidence and diagnostic bound

Existing registered evidence is reused rather than repeated:

- `stages-3x100k-20260724b` provides the current 100,000-tick broad-shape and
  lifetime-capacity reference. Its three seeds completed at 15.8, 20.1, and
  39.4 ticks/s, with entity density identified as a major cost multiplier.
- `stages-perf-probe-1x100k-20260724` provides the existing seed-9100
  simulation-only point: 17.1 ticks/s.
- `stages-r2b-100k-20260724` provides later density, stage-shape, relationship,
  cooldown, district, and culture-mass evidence without requiring another
  long run in this phase.

The only new Phase 1 run is:

- run ID: `preparallel-phase1-1x1000-20260724`;
- seed 9100, one run, 1,000 ticks;
- Release and simulation-only through `make mc`, with a fresh build;
- one 20-second sampled CPU trace attached after the simulation process starts;
- no second seed, no full telemetry export, no escalation to 10,000/25,000/
  100,000 ticks, and no rerun merely for confidence.

The short trace is directional. It may identify obvious dominant program or
mutation families, but it does not establish 100,000-tick percentages because
the world is smaller at tick 1,000.

#### Performance target and named horizon

The user-facing target is a three-seed 100,000-tick evidence batch that finishes
in at most 30 minutes when the three independent seeds are fanned across the
reference development host. The corresponding single-run gate is:

- fixed seed 9100 at tick 100,000;
- Release, simulation-only, same configuration and reference host;
- at least 60 ticks/s end to end;
- no more than 8 GB application working set;
- no capacity failure;
- repeated same-seed runs publish identical new-model state.

This is a result target, not a promise that in-run parallelism is required. If
the reduced serial kernel meets it, Phase 8 selects no parallel strategy.
Otherwise a parallel strategy must beat that reduced one-worker model
end-to-end; worker-loop speedup alone does not pass.

Phase 4 subsequently removed the five-to-one raw/main clock. In the current
clock, the same accepted evidence horizon is `20,000` world pulses and the same
throughput gate is `12` world pulses/s: one current pulse performs the main
simulation work formerly scheduled every fifth raw tick and also runs five
minor-actor substeps. A `100,000`-world-pulse run is now a separate five-times
longer product horizon, not the old performance gate under a renamed counter.

#### Phase 1 result

`preparallel-phase1-1x1000-20260724` completed successfully with a fresh
Release build:

- seed 9100 advanced 1,000 ticks in 4.520506 seconds, or 221.2 ticks/s;
- the registry entry is complete and records the result;
- the whole command took about 196 seconds because fresh build and startup
  world generation dominated this deliberately short run;
- observed resident memory peaked near 5.7 GiB during world generation, inside
  the 8 GiB ceiling but above the approximate 4 GiB working-set preference.

The 20-second sampled trace
`.artifacts/profiles/preparallel-phase1-1x1000-20260724.nettrace` attached to
the correct Release process, but the tick loop lasted less than five seconds
and had not started during the sample. The trace is therefore a startup/worldgen
trace, dominated in managed frames by physical-geography work such as plate
advection, flexure, noise, and drainage. It is not used to claim kernel phase
percentages, and Phase 1 does not rerun a longer workload to compensate.

The short result and existing long-horizon result are intentionally very
different: 221.2 ticks/s at the small tick-1,000 world versus 17.1 ticks/s in
the existing seed-9100 tick-100,000 simulation-only probe. This confirms that
the named late horizon matters and that a fast startup slice cannot be used as
the performance verdict.

For work-volume context, ad hoc DuckDB queries against the registered
`stages-r2b-100k-20260724-s9102` run were appended to that run's registry
statistics. At tick 100,000 it had:

- 970 active and 1,249 historical named entities;
- 2,957 active relationships and 1,285 covered cells;
- 49,185,361 minor-actor event rows;
- 19,179,660 relationship event rows;
- 1,630,513 movement event rows;
- 490,968 generator event rows and 47,213 gambit event rows.

These are work-volume indicators, not CPU attribution. They show why minor
labor, relationship processing/eventing, movement, and generator evaluation
deserve direct scrutiny, while also showing that rare world-beat and chronicle
work should not be treated as a primary bulk lane.

The most recent reusable late-run CPU trace predates the current source
(`perf-20260713-eb96908-1x100k-sim-after25k`) and is treated as directional
only. Its visible managed work was led by culture-boundary neighborhood
queries, low-altitude candidate construction, minor-actor target construction,
gambit target/precondition evaluation, movement/path search, and relationship
index mutation. That matches the current-code consumer review, but fresh CPU
percentages wait until the serial model is smaller.

Phase 1 therefore selects no parallel strategy. Its evidence supports the
existing sequence: remove history dependencies and unnecessary materialized
work, simplify the surviving serial loops, define fixed-point/storage
contracts, and only then collect a representative late-run profile.

Files changed in Phase 1:

- this document only.

Exit gate:

- the preservation envelope, gameplay substitutions, diagnostic bound, and
  success criteria are recorded;
- the bounded diagnostic completes and its run ID and trace findings are
  recorded here;
- no long Monte Carlo run has been started.

Will not change:

- runtime code or authored content.

### Phase 2: Break Accidental History Dependencies

Status: complete on 2026-07-24.

Owner: `Engine.Kernel`, `Engine.Domain`, `Engine.Gameplay`, `IceRemembers`,
projection, and observability at their existing boundaries.

Work:

- implement final occurrence retirement and incident-edge ending semantics;
- replace same-instance artifact reawakening;
- replace chronicle historical-entity dereferences with compact role-retired
  facts;
- replace world-beat culled-edge scans with explicit counters;
- remove gambit action history and lifetime cooldown behavior;
- stream/compact provenance and active UI records;
- define active-only snapshot semantics.

Intended files:

- `src/IceRemembers/IceRemembersGambits.Retirement.cs`
- `src/Engine.Domain/Behaviors/Mutations.cs`
- `src/Engine.Domain/Compilation/DomainCompiler.cs`
- `src/Engine.Gameplay/GameplayDefinition.cs`
- `src/Engine.Host/KernelProgramInstaller.cs`
- `src/Engine.Kernel/WorldState.cs`
- `src/Engine.Kernel/WorldState.Districts.cs`
- `src/Engine.Kernel/WorldState.Provenance.cs`
- `src/Engine.Kernel/WorldState.RelationshipIndexes.cs`
- `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.cs`
- `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.Evaluation.cs`
- `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.Execution.cs`
- `src/Engine.Kernel/Programs/Gambits/GambitProgramState.cs`
- `src/Engine.Kernel/Programs/Chronicles/ChronicleProgramRuntime.Beats.cs`
- `src/Engine.Kernel/Programs/Chronicles/ChronicleProgramState.cs`
- `src/Engine.Kernel/Programs/WorldBeats/WorldBeatsProgramRuntime.cs`
- `src/Engine.Kernel/Persistence/WorldStateSnapshot.cs`
- `src/Engine.Kernel/Persistence/SimulationSnapshot.cs`
- `src/Engine.Projection/UiProjectionState.cs`
- `src/Engine.Projection/ProvenanceProjection.cs`
- `src/Engine.Observability/TelemetryEventProjection.cs`
- directly affected contract tests only

Exit gate:

- no production kernel query requires a retired entity, ended relationship,
  inactive district, completed provenance output, or action-history row.

Will not change:

- individual minor-actor behavior;
- map activity/signals/boundaries yet;
- threading.

#### Phase 2 result

- Retirement is final at the simulation mutation boundary. It records compact
  chronicle role-retired facts, ends and immediately releases incident
  relationships, removes hosted districts, and clears the occurrence's
  gambit-cooldown row.
- Relationship rows are active-only and reusable. World-beat living and ended
  relationship facts are maintained on create/end rather than reconstructed
  by scanning a lifetime edge archive.
- Artifact districts are dense active templates. Installation preserves only
  the authored kind/subtype/culture, host, cell, bonus, and future relationship
  rule. Reawakening consumes the template and creates a fresh artifact
  occurrence with a fresh relationship; it never revives the retired source,
  and the new occurrence cannot act until the following tick.
- Chronicles retain compact role-retired bits and relationship start ticks
  instead of querying completed provenance or dereferencing retired cast
  members for pursuit and beat conditions.
- Gambit lifetime action history, implicit recency decay, generic
  history-count preconditions, and sparse lifetime cooldown tables are gone.
  Current cooldowns are direct bounded expiry arrays and are cleared on
  retirement.
- Completed provenance invocation/output tables and their growable-storage
  machinery are gone. Active invocation frames count outputs while primitive
  mutation events and one completion record stream the durable facts to the
  external provenance projection.
- The live UI and snapshot materialization boundary expose active entities and
  relationships only. Retired entity slots are not yet recycled inside the
  kernel snapshot because generation-safe occurrence handles are the Phase 6
  prerequisite; this is an explicit storage boundary, not a retained gameplay
  dependency.
- The behavioral contracts cover final incident-edge retirement and reuse,
  fresh artifact reawakening, compact chronicle retirement facts, incremental
  world-beat counts, streamed provenance, and active-only snapshot/projection
  output.

### Phase 3: Remove Materialized Global And Observer Work

Status: complete on 2026-07-24.

Owner: `Engine.Kernel` for simulation facts, host/projection/observability for
external consumers, and Ice Remembers for simplified authored conditions.

Work:

- replace and delete the MapActivity and MapSignals programs;
- replace runtime culture component queries with local descriptors;
- move topology metrics to analysis;
- disable unused headless projections;
- replace per-scalar provenance/version/event work with semantic action and
  sampled state outputs;
- maintain rare global facts incrementally.

Intended files:

- `src/Engine.Kernel/Programs/MapActivity/*`
- `src/Engine.Kernel/Programs/MapSignals/*`
- `src/Engine.Kernel/Programs/CultureBoundaries/*`
- `src/Engine.Kernel/Simulation.cs`
- `src/Engine.Kernel/Simulation.CultureInfluence.cs`
- `src/Engine.Kernel/Simulation.MapCapacity.cs`
- `src/Engine.Kernel/Simulation.MapTerrain.cs`
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.Candidates.cs`
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.CultureInfluenceTargets.cs`
- `src/Engine.Kernel/Programs/Effects/EffectsProgramRuntime.cs`
- `src/Engine.Kernel/Programs/MinorActors/MinorActorProgramRuntime.cs`
- `src/Engine.Kernel/Programs/MinorActors/MinorActorProgramRuntime.Campaign.cs`
- `src/Engine.Kernel/Programs/MinorActors/MinorActorProgramRuntime.CultureField.cs`
- `src/Engine.Kernel/Programs/MinorActors/MinorActorProgramRuntime.Scoring.cs`
- `src/Engine.Kernel/Programs/Generators/GeneratorProgramRuntime.Helpers.cs`
- `src/Engine.Kernel/Programs/Generators/GeneratorProgramRuntime.PlacementTerrain.cs`
- `src/Engine.Kernel/Programs/MapFrontier/MapFrontierProgramRuntime.Ice.cs`
- `src/Engine.Gameplay/MapSignalsConfig.cs`
- `src/Engine.Host/KernelConfigurationContext.cs`
- `src/Engine.Host/KernelHost.CultureBoundaries.cs`
- `src/Engine.Host/KernelProgramInstaller.cs`
- `src/Engine.Host/KernelRuntimeFactory.cs`
- `src/Engine.Projection/UiProjectionState.cs`
- `src/Engine.Observability/TelemetryEventProjection*.cs`
- directly affected behavioral and architecture tests

Exit gate:

- removed programs have no runtime registration, state, snapshot, installer,
  or production consumer;
- broad map/culture/lifecycle behavior remains present through the replacement
  facts;
- headless runs construct only requested subscribers.

Will not change:

- map/worldgen terrain generation;
- the individual cohort model;
- parallel execution.

#### Phase 3 result

- `MapActivity`, `MapSignals`, and `CultureBoundaries` are no longer tick
  programs. Their registrations, state arrays, snapshots, dependency channels,
  installers, program codes, and runtime configuration capacities are deleted.
- Authored map-signal configuration is now domain-agnostic `Locality`
  configuration. Consumers calculate current occupancy, terrain capacity,
  crowding, nearby kind support/threat, relationship support/threat, and
  attractor mass directly at the bounded point where they act. Historical
  activity heat is intentionally gone.
- Culture consumers use a radius-two `CultureLocality` descriptor containing
  current own and foreign mass/share, adjacent and ring-two support, connected
  local mass, strength, and a local interior-hole heuristic. Runtime connected
  components, component identity, largest-component gravity, and read-triggered
  cache rebuilds are gone.
- Generator "same component" binding is now a bounded same-culture route
  requirement. It requires locally supported endpoints within a fixed physical
  distance; it does not preserve arbitrary historical connectivity.
- Component and hole summaries are computed only at the host telemetry sampling
  boundary. The UI consumes dirty radius-two locality refreshes, while ordinary
  headless runs keep the existing opt-in consumer registration and construct no
  UI projections.
- Raw culture mutation no longer advances the world version, records scalar
  provenance outputs, or emits one `CellCultureInfluenceChanged` event per
  culture/cell mutation. Semantic action events remain, and Monte Carlo exports
  sampled `culture_field_cell_snapshots` instead.
- The world maintains total physical culture raw mass incrementally for the
  rare global world-beat query rather than scanning every culture cell at each
  sample.
- Snapshot format `46` removes the deleted program state and obsolete capacity
  knobs. Behavioral contracts now cover direct locality queries, local culture
  descriptors, bounded dirty refresh, sampled telemetry, and the single-cell
  anchor needed by ordinary influence work.

### Phase 4: Simplify Low-Altitude Execution

Owner: `Engine.Kernel` runtime plus authored rule compilation in
`Engine.Gameplay`/`Engine.Domain`.

Work:

- preindex rules by entity or relationship kind;
- use active/spatial/adjacency indexes instead of lifetime scans;
- merge effects and upkeep into one resolution model;
- move relationship-follow movement into explicit entity movement intent;
- incrementally maintain district modifiers;
- replace raw/main clocks with one pulse or an actor-only substep scheduler;
- move cohort state from hash tables to direct active slots and due lists
  without aggregating cohorts.

Intended files:

- `src/Engine.Kernel/Simulation.cs`
- `src/Engine.Kernel/SimulationOptions.cs`
- `src/Engine.Kernel/Programs/Traversal/*`
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/*`
- `src/Engine.Kernel/Programs/Relationships/*`
- `src/Engine.Kernel/Programs/Effects/*`
- `src/Engine.Kernel/Programs/Upkeep/*`
- `src/Engine.Kernel/Programs/MinorActors/*`
- `src/Engine.Kernel/Programs/CultureSlots/*`
- `src/Engine.Kernel/Programs/Knowledge/*`
- `src/Engine.Domain/Compilation/KernelPrograms/*`
- `src/Engine.Host/KernelProgramInstaller*.cs`
- directly affected behavioral tests

Exit gate:

- hot loops iterate active work and compatible rules only;
- no duplicate effects/upkeep scan;
- cohort aggregation has not been introduced;
- behavior checks show continued ordinary movement, culture/road labor,
  relationship drift, and lifecycle churn.

Will not change:

- strategic parallel architecture;
- authored generator/chronicle catalogs except conditions invalidated by
  removed runtime facts.

#### Phase 4 result

- `WorldState` now maintains active entity and active relationship lists.
  Retiring an entity removes it from active kind, culture, and spatial indexes;
  retiring either relationship endpoint still ends and releases each incident
  edge.
- Low-altitude verb, movement-profile, relationship-maintenance, effect,
  lifecycle, minor-actor emitter, and minor-actor verb rules are indexed by the
  kind or source rule that can use them. Hot paths no longer filter unrelated
  global rule tables for every active occurrence.
- Follow-configured relationships expose entity movement intent. The
  relationship pass owns contact memory, strength drift, and culling only;
  target placement flows through the ordinary movement boundary.
- Effects and upkeep are one active-entity resolution pass. Compatible
  attribute deltas, district healing, and lifecycle drain accumulate before
  each affected attribute is written once, followed by lifecycle retirement.
  The separate upkeep runtime, snapshot, program registration, and enable flag
  are deleted.
- District installation/removal incrementally maintains direct per-host
  healing totals and compact per-culture/cell emission totals. The effects pass
  consumes those totals rather than rescanning every district for every entity.
- The raw/main clock split is deleted. Authored cadences and durations are
  world pulses; minor actors alone take a configurable number of bounded
  substeps inside a pulse.
- Minor actors remain individual. Direct reusable slots replace hash-table
  actor fields, source/rule state is direct-indexed, active slots are dense, and
  a deterministic due heap visits only actors whose action or expiration is
  due.
- Snapshot format `47` records the new active indexes, rule indexes, direct
  minor-actor state, unified tuning clock, and merged entity-resolution data.
- Repo test targets now persist complete output under
  `.artifacts/test-logs/` and print a bounded failure summary without losing the
  original `dotnet test` exit code.

Phase 4 deliberately does not introduce fixed-point arithmetic,
generation-safe occurrence handles, delta epochs, cohort aggregation, or
parallel execution.

### Phase 5: Define Fixed-Point Numeric Contracts

Owner: kernel data contract and domain compiler boundary.

Work:

- audit ranges from definitions and short runtime samples;
- write the field matrix described above;
- implement compile-time conversion helpers and deterministic arithmetic;
- convert one field family at a time on the serial runtime;
- verify invariants and expected bounded rounding differences.

Intended files:

- `src/Engine.Kernel/WorldState*.cs`
- `src/Engine.Kernel/Programs/**/*.cs` for surviving numeric owners
- `src/Engine.Domain/Compilation/*.cs`
- `src/Engine.Host/KernelProgramInstaller*.cs`
- kernel snapshot and primitive event payload files carrying converted values
- projection and telemetry conversion boundaries
- numeric invariant tests

Exit gate:

- every shared accumulator has a documented integer representation and
  deterministic reduction rule;
- the `Engine.Kernel` and primitive event assemblies expose no floating-point
  member signatures;
- compiled domain and gameplay runtime records are integer-backed before
  installation;
- no silent overflow;
- authored content remains readable and does not expose kernel scaling units.

Will not change:

- threading;
- candidate weighting and selection semantics except for bounded quantization.

#### Phase 5 result

Status: complete on 2026-07-24.

The serial kernel is now fully fixed point. It has no `float`, `double`,
`MathF`, floating-point table, or floating-point member signature. Durable
semantic stocks use field-specific integer units; dimensionless probabilities,
weights, scores, ratios, heat, and path costs use the signed `FixedScalar`
representation. Authored content may remain ordinary floating point before
compilation. `Engine.Domain` and `Engine.Gameplay` quantize every compiled
runtime record before the host installs it, and presentation/telemetry perform
explicit conversion after values leave the event or query boundary. Conversion
uses round-to-nearest with midpoint values away from zero. Non-finite and
out-of-contract authored values are rejected.

The range audit used the compiled definitions, kernel limits, tests, and the
bounded Release sample
`preparallel-phase5-fixedpoint-1x1000-20260724`. It did not require a long Monte
Carlo run. That earlier run measured the first field-family conversion and is
retained only as historical directional evidence; it is not the acceptance
evidence for the completed floating-point boundary removal.

That sample advanced 1,000 current world pulses in `16.006344` seconds, or
`62.5` world pulses/s. Its tick label must not be compared directly with Phase
1's `221.2` raw ticks/s: 1,000 current pulses perform the work of 5,000 former
raw ticks, including 5,000 minor-actor substeps. That is `312.4` former-raw-tick
equivalents/s while reaching a world five times farther into simulation. The
short result is directional evidence that the completed simplification and
fixed-point work did not introduce the apparent `3.5x` regression suggested by
the unconverted tick labels; Phase 8 still owns late-horizon profiling.

The full-conversion remediation was accepted with repository-wide `make ci`
(1,068 fast tests) and the registered Release smoke run
`preparallel-phase5-full-fixedpoint-1x250-20260724`. Seed `9100` advanced 250
world pulses in `10.7629472` simulation seconds (`23.2278` pulses/s), produced
379,257 telemetry rows and 16 generator fires, and reported
`map_degenerate=0`. This short run proves executable integration and supplies a
post-remediation sanity point; it is deliberately not a late-horizon
performance or balance claim.

| Field family | Authored and current runtime range | Runtime representation and precision | Reduction, clamp, and overflow | Persistence, events, and presentation |
| --- | --- | --- | --- | --- |
| Raw culture mass and deltas | Current per-cell state is `[0, 2000]`; authored thresholds reach `300`, initial sources reach `35`, district emission reaches `2`, and erosion reaches `25` per application. The generic conversion accepts signed deltas through `+/-1,000,000`. Zero cells are removed by the sparse layer. | `int32`, scale `1,000`, precision `0.001`; total physical culture is `int64` units. | Each contribution is quantized before merge. Adds use an `int64` intermediate and saturate at the per-cell `[0, 2,000,000]` unit clamp. Ratio products use `int64` and deterministic midpoint-away rounding. The world total is checked rather than wrapped. | Snapshot 49 stores integer cell values and the integer total. Culture state-change events expose only integer units; projection and telemetry divide at their own boundary. |
| Relationship strength and deltas | Strength is `[0, 1]`; authored conditions observed through `0.85`, with current deltas approximately `[-0.32, +0.04]`. Zero or a compiled cull threshold ends an active relationship. | `int32`, scale `1,000,000`, precision `0.000001`. | Maintenance and effect contributions are quantized independently, summed with saturating `int64` addition, then clamped to `[0, 1,000,000]`. Cull is tested after the deterministic merge. | Snapshot 49 and relationship events store integer strength units. Projection and telemetry explicitly divide outside the runtime boundary. |
| Persistent attributes, including health | The shared contract is `[-10,000, +10,000]`; Ice Remembers health is authored as `[0, 100]` with default `90`, and current effects include both small continuous changes and discrete changes up to roughly `30`. | `int32`, scale `100,000`, precision `0.00001`. | Every effect contribution is quantized before accumulation. Adds use `int64` and saturate at the shared numeric envelope, followed by the compiled per-attribute min/max clamp. Invalid authored bounds are rejected. | Snapshot 49 and attribute events store integer units. Domain definitions may remain readable floats before compilation; projection and telemetry divide outside the runtime boundary. |
| Road quality and work | Quality is `[0, 100]`; active checkpoints are `25` and `75`, ordinary authored work is around `1`, minor-actor work reaches about `1.6`, and the current per-pulse decay ratio is `0.9995`. | `int32`, scale `1,000`, precision `0.001`; decay ratios use scale `1,000,000`. | Work uses saturating `int64` addition followed by `[0, 100,000]` clamp. Lazy decay uses integer exponentiation by squaring and `int64` ratio products with midpoint-away rounding; checkpoint floors are integer units. | Movement-state schema 7 and snapshot 49 store integer quality and floor tables. Road events store integer units; path scoring remains fixed point and off-kernel consumers convert explicitly. |
| Persistent local modifiers | District healing uses attribute units; district culture emission uses culture units. Entity influence/favor deltas use scale `100,000` and a shared `+/-10,000` envelope. Knowledge and decay multipliers use scale `1,000,000` and a shared `+/-100` envelope; current derived influence is clamped to `[0.1, 3]`. | `int32`, using the destination field's scale or the explicit modifier/ratio scale. This avoids a misleading universal modifier precision. | District totals and entity modifier deltas use saturating `int64` addition. Multiplier composition uses `int64` products and deterministic midpoint-away division. Semantic clamps occur after the integer merge. | Snapshot 49 stores the integer tables and arrays. Authored district bonuses and multipliers may stay floats before compilation; off-kernel consumers divide by the selected scale. |
| Dimensionless runtime scalars | Probabilities, candidate weights, scores, ratios, heat, path costs, affinities, and other non-stock values use the ranges required by their owning rule. | Signed `int64` `FixedScalar`, scale `1,000,000`, precision `0.000001`; positive infinity has an explicit sentinel. | Adds saturate, multiply and divide use `Int128` intermediates, and division rounds midpoint values away from zero. Owning rules apply their semantic clamps. | Compiled program records, primitive scalar tables, snapshot fields, command slots, and event long slots carry integer units. Only authored and off-kernel presentation APIs convert to floating point. |

The converted families are:

- world attributes and health;
- active relationship strength, direct deltas, and maintenance reductions;
- raw culture layers, influence production, decay, anchors, and physical totals;
- road quality, work, floors, and lazy decay;
- district aggregates, culture/favor deltas, and knowledge multipliers.

The primitive state-change event boundary now carries the integer units for
attributes, relationships, raw culture, roads, and generic scalars. It exposes
no typed floating-point compatibility properties. Consumers convert explicit
unit fields. Snapshot format `49` intentionally replaces the prior numeric
layout and the partial-conversion format without a legacy compatibility layer.

Floating point remains only before the compiler boundary in authored
configuration and after the kernel/event boundary in diagnostics, analysis, and
presentation. Candidate weights and probabilities, terrain/path scoring,
culture-slot candidate scores, minor-actor input rates, and chronicle selection
heat are fixed point while compiled or executing.

Phase 5 deliberately does not introduce generation-safe occurrence handles,
delta epochs, active-only pool storage, candidate-weight changes, or parallel
execution.

### Phase 6: Rebuild Active-Only Dense Storage

Owner: `Engine.Kernel`.

Work:

- implement active entity, relationship, district, and cohort pools;
- implement generation-safe occurrence handles;
- implement dense cells/cultures and active indexes;
- replace ID-keyed hash tables with direct arrays or bounded owner-local sparse
  structures;
- compile rule spans and due queues;
- replace snapshots with the active-state format.

Intended files:

- `src/Engine.Kernel/WorldState.cs`
- `src/Engine.Kernel/WorldState.*.cs`
- `src/Engine.Kernel/MapLayerSet.cs`
- `src/Engine.Kernel/SpatialLayerIndex.cs`
- `src/Engine.Kernel/PrimitiveIntTable.cs`
- `src/Engine.Kernel/PrimitiveFloatTable.cs`
- surviving `src/Engine.Kernel/Programs/**/*ProgramState.cs`
- surviving `src/Engine.Kernel/Programs/**/*ProgramSnapshot.cs`
- `src/Engine.Kernel/Persistence/KernelSnapshot.cs`
- `src/Engine.Kernel/Persistence/KernelSnapshotBinary.cs`
- `src/Engine.Kernel/Persistence/SimulationSnapshot.cs`
- `src/Engine.Kernel/Persistence/WorldStateSnapshot.cs`
- active-storage, index, handle, and snapshot invariant tests

Exit gate:

- elapsed time alone cannot exhaust entity, relationship, district, cooldown,
  action-history, or provenance capacity;
- hot iteration cost tracks active counts;
- stale occurrence handles cannot address reused slots;
- active graph and spatial indexes remain valid through churn and reload.

Will not change:

- parallel execution;
- minor actors into an aggregate model.

### Phase 7: Prove The Serial Epoch And Delta Model

Owner: `Engine.Kernel`; the driver remains a one-writer caller.

Work:

- add frozen pulse views, typed delta buffers, deterministic resolution, keyed
  randomness, semantic event publication, and pulse/phase revisions;
- route surviving programs through the new model on one thread;
- add shuffle and logical-shard invariance tests;
- make snapshots and projections consume published boundaries.

Intended files:

- `src/Engine.Kernel/Simulation.cs`
- `src/Engine.Kernel/ISimulationSystem.cs`
- `src/Engine.Kernel/RandomSource.cs`
- `src/Engine.Kernel/Simulation.Events.cs`
- surviving `src/Engine.Kernel/Programs/**/*ProgramRuntime*.cs`
- `src/Engine.Eventing/KernelEventLog.cs`
- `src/Engine.Kernel/Persistence/*`
- new epoch/delta/resolution primitives under `src/Engine.Kernel/Execution/`
- projection and telemetry event consumers
- determinism, shuffle, graph, snapshot, and event contract tests

Exit gate:

- shuffled evaluation and delta order produce identical published state;
- one logical shard and many logical shards produce identical convergence
  state on one thread;
- the serial result has a current short Release profile and phase cost map.

Will not change:

- OS threads or worker pools;
- the chosen parallel architecture.

### Phase 8: Gate Parallel Strategies

Owner: architecture decision followed by a separately approved implementation
proposal.

Work:

- compare reduced serial performance with the accepted target;
- measure surviving phase cost, work-item counts, locality, cross-shard message
  volume, and candidate shard load;
- decide among stopping, subsystem offload, BSP, and spatial actors;
- write the exact implementation plan and file manifest for the selected
  strategy before adding threads.

Exit gates:

- **Stop** when the reduced serial kernel meets the target.
- **Subsystem offload** only when a surviving derived builder plus snapshot
  publication has a measured net benefit. Removed activity/signal/boundary
  systems are not recreated merely to offload them.
- **BSP** when entity/relationship/destination-cell phases dominate, work per
  phase amortizes barriers, and the serial delta model already owns conflicts.
- **Spatial actors** only when a single-threaded logical-shard prototype shows
  that the overwhelming majority of interactions fit a practical local/halo
  radius, shard load is acceptable or repartitioning is designed, and every
  cross-shard read has a defensible lag or convergence rule.

The phase does not have an implementation file set until one gate passes.
Creating that file set is part of the required follow-up proposal.

Long 25k/100k registered Monte Carlo runs occur after a candidate meets its
short performance, determinism, and behavior gates.

## Strategy Consequences After The Re-Review

### Subsystem offload

The original strategy centered on offloading MapActivity, MapSignals,
knowledge, and culture boundaries. This redesign removes the first three
runtime materializations or makes them incremental/local, and reduces
knowledge to rare dirty work.

Subsystem offload is therefore no longer the default first implementation.
Its likely remaining uses are optional projection, telemetry preparation, UI
frame construction, or a surviving expensive derived analysis proven by the
new profile.

### Bulk-synchronous phase graph

BSP remains the most direct continuation if the reduced kernel still needs
parallel execution. The serial epoch/delta phase establishes its difficult
semantics before worker/barrier code:

- fixed-point reductions;
- destination ownership;
- conflict resolution;
- keyed randomness;
- structural allocation;
- event ordering;
- phase visibility.

Adding workers is then a scheduling and storage-ownership change rather than a
second simulation model.

### Spatial shard actors

Spatial actors remain a higher-risk option, not a presumed end state. Removing
global target scans and component queries improves their viability, but it
does not prove sufficient locality.

Before threads, the kernel must run multiple logical spatial shards
round-robin on one thread with:

- explicit ownership and migration;
- halo/local read APIs;
- cross-shard messages;
- declared lag;
- `ConvergeAt`;
- snapshot and command routing;
- locality and load counters.

Failure at this logical-shard stage rejects the strategy without paying the
concurrency cost.

## Evidence And Verification

### Initial evidence

- no new long Monte Carlo baseline;
- reuse current registered evidence for broad world shape, with provenance
  treated as potentially stale where the code has moved;
- one short Release simulation-only warmup and sampled trace, bounded before
  execution;
- bounded counters for active entities, relationships, cohorts, dirty cells,
  rule candidates, deltas, and interaction distance;
- no permanent telemetry unless it is part of an approved product/tooling
  surface.

### Per-phase verification

- durable behavioral and invariant tests only;
- `make test` for normal repository verification;
- `make ci` for final verification of an approved implementation phase;
- no parallel test execution;
- no low-value content-mirroring or source-text ban tests;
- snapshot, handle-generation, graph, and determinism invariants where those
  contracts change.

### Final evidence

After the serial redesign or selected parallel strategy is materially faster:

- registered Release Monte Carlo runs with unique human-readable run IDs;
- no `--no-build`;
- important SQL findings appended through the Monte Carlo CLI;
- repeatability across repeated runs and worker/logical-shard counts where
  applicable;
- phase throughput and end-to-end simulation-only throughput;
- behavior checks for map use, culture shape, cohort labor, lifecycle churn,
  relationship dynamics, gambits, generators, chronicles, and map growth;
- memory and capacity behavior through the accepted long horizon.

## Success Criteria

The plan succeeds only if:

1. the kernel's cost scales primarily with active work, not elapsed history;
2. unnecessary global programs and observer work are absent rather than merely
   parallel;
3. durable additive state has explicit deterministic numeric contracts;
4. the serial model is order-independent at published boundaries;
5. per-cohort minor actors remain possible without dictating the rest of the
   architecture;
6. the broad simulation remains alive and recognizable under the accepted
   quality checks;
7. long-run capacity failures are concurrency-bound and validated explicitly,
   not hidden lifetime totals;
8. the chosen parallel strategy, if any, demonstrates net end-to-end benefit
   over the same reduced one-worker model.

## Decision Status

Accepted for subsequent planning and implementation:

1. the preservation/disposable contract in this document;
2. final retirement and new-instance artifact reawakening;
3. removal of general historical entity browsing and old snapshot
   compatibility;
4. individual minor actors as the retained default, with their storage,
   scoring inputs, and clock allowed to change;
5. the completed Phase 1 diagnostic bound;
6. the 60 ticks/s seed-9100 tick-100,000 performance gate and three-seed
   30-minute batch target;
7. same-seed repeatability under the new model rather than legacy equivalence.

Still decided at the owning implementation phase:

1. the exact direct/local replacements made before deleting MapActivity,
   MapSignals, and runtime connected-component culture identity;
2. field-specific fixed-point formats and overflow/rounding rules;
3. the final active-slot capacities and occurrence-handle widths;
4. whether the reduced serial result needs any parallel strategy.

Authorization remains phase-specific. Completing Phase 1 does not itself
authorize runtime changes in Phase 2 or later.
