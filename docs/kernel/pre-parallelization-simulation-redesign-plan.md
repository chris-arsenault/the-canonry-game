# Pre-Parallelization Simulation Redesign Plan

Status: Phases 1-10 complete. Phase 10 was implemented and validated on
`feature/unified-tick-minor-actors`. Phases 2-4 were reopened and remediated on
2026-07-25 after a non-fixed-point completion audit. Phase 8 corrected the
clock and lifecycle contracts after the Phase 7 performance remediation.
Completed phase results and their remediation addenda describe the current
implementation. Phase 9 selected a typed-owner bulk-synchronous candidate.
Phase 10 removed the differential minor-actor cadence and replaced emitted
short-lived workers with persistent discrete task cohorts before parallel
execution begins.

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
   same-tick outcomes are not.
9. The kernel-to-consumer boundary is the shared primitive record assembly,
   not a live kernel snapshot or object reference. UI views, archives, parquet,
   and narrative retention own their materialization and cadence outside the
   kernel. A future view store or durable-watermark handshake is separable
   consumer infrastructure, not a reason to retain observer history in kernel
   state.
10. The simulation has one clock and one public unit: ticks. Phases are
    ownership boundaries inside a tick, not clocks. No second-clock revision,
    sub-tick counter, or bundled advance unit is retained. Phase 10 supersedes
    the earlier five-tick main-program cadence: one new tick is one complete
    ordinary simulation opportunity, and minor actors also receive at most one
    movement/interaction/work opportunity in that tick.
11. Lifecycle configuration expresses target ticks and curve shape. The kernel
    derives interval drain from a normalized cumulative health-budget curve;
    authored content does not store tiny base-rate or age-coefficient values.

## Proposal Contract

### Owning layers

- `Engine.Kernel` owns active simulation state, bounded scratch, deterministic
  tick execution, conflict resolution, and the primitive event boundary.
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
| MapActivity | Clears occupancy and scans every active entity on each main-program tick to recreate presence. | Remove as a tick program. Occupancy comes from the spatial index; visits and heat update on movement and meaningful actions. |
| MapSignals | Clears tables, scatters from entities and relationships, gathers per entity, and scatters attractors. Few systems consume it. | Remove as a tick program. Replace consumers with direct terrain, occupancy, culture, active-edge, and local-target facts. |
| Culture boundaries | Lazy component caches rebuild global topology and make apparently read-only queries mutate shared cache state. | Replace runtime consumers with a local culture descriptor derived from own/foreign raw mass, share, and one/two-ring support. Compute component and hole metrics offline. |
| Culture field | Durable territorial state is central, but sparse hash layers, per-write rollups, invalidation, events, and float reductions are expensive. | Keep the model. Move to dense `CellId × culture` fixed-point slabs, batched deltas, and epoch-time derived winner/share fields. |
| Movement and verbs | Separate passes, global/high-water candidate scans, mutable shared scratch, and immediate visibility couple work. | Keep committed ordinary behavior and movement, but preindex rules and candidates, use active/spatial indexes, and resolve intents through deltas. Exact scoring and same-loop visibility may change. |
| Relationship maintenance | Active-edge drift is useful; follow movement and immediate cull/index mutation broaden the pass. | Keep strength drift. Move relationship-driven movement into entity intent or an explicit movement delta. Resolve culls after strength merge. |
| Effects and upkeep | Both scan active entities and apply health/attribute changes before a separate death check. Districts add another scan. | Merge into one entity-resolution phase. Accumulate contributions, apply once, then retire. Maintain host/cell district modifiers incrementally. |
| Minor actors | Per-cohort state is stored in hash tables; source emission scans entities and rules; actors query expensive culture components. | Keep individual cohorts. Move state to direct active slots, preindex emitters, use due lists and local culture descriptors, and emit typed culture/road/health deltas. Aggregate labor remains optional later. |
| Raw/main clocks | A five-to-one clock exists chiefly to give cohorts more frequent low-impact work and forces tuning conversions across systems. | Replace it with one tick clock. Run minor actors every tick and ordinary main programs on an explicit five-tick cadence. |
| Culture slots and knowledge | Rare derived systems rescan eligible state on cadence even when relevant inputs have not changed. | Retain their gameplay role but use dirty revisions, preindexed candidates, and due recomputation. |
| World beats | Scans lifetime relationships to count living and culled facts. | Maintain active counts and explicit cumulative transition counters. World-beat evaluation stays rare and small. |
| Generators and chronicles | Broad structural mutation and global story recognition are relatively rare but use global queries and provenance history. | Keep their roles in a deterministic structural lane. Replace historical queries with current facts, counters, bounded suppression, or compact causal tokens. |
| Map frontier | Rare structural map work is legitimate, but current activity dependency and broad scanning may be avoidable. | Keep map growth. Read direct occupancy/action facts and retain bounded cursor-based work. |
| Events and world version | Small scalar mutations synchronously append provenance/events and advance a global total-order version. | Publish semantic action, structural, retirement, and phase-completion events. Use the event tick and phase revision for state visibility. Sample dense numeric state rather than event every cell delta. |

## Retention Policy

Every proposed historical value must fit one of four categories:

1. **Active simulation state**: required to decide the next tick. It remains in
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
- Represent decay from `lastActionTick` rather than rewriting heat every tick.
- Let map frontier check occupancy directly.
- Let UI and telemetry sample occupancy and heat from the compact cell state.

This intentionally stops treating an unmoving entity as a fresh visit on every
main-program cadence.

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
same-tick visibility may be retuned after the clock and field changes.

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
- RNG/keyed-random seed and current tick;
- generation counters needed to prevent handle aliasing.

It does not contain retired entities, ended relationships, completed
provenance output, UI history, or compatibility shims for the old format.

## Serial Epoch And Delta Model

Threads are deliberately absent from this phase.

### Candidate tick structure

1. **Admit**: validate commands and structural requests against the published
   tick boundary.
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
   events and a tick-completed marker.

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

`(seed, tick, phase, occurrence handle, rule ID, purpose, attempt)`

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

Status: reopened and remediated on 2026-07-25 after the non-fixed-point
completion audit.

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
- allow different IDs, event order, tick timing, RNG consumption, numeric
  rounding, exact counts, and exact same-tick outcomes;
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
the reduced serial kernel meets it, Phase 9 selects no parallel strategy.
Otherwise a parallel strategy must beat that reduced one-worker model
end-to-end; worker-loop speedup alone does not pass.

Phase 8 restored the original one-tick unit while retaining the cheaper main
program cadence. The accepted evidence horizon is therefore again `100,000`
ticks and the gate is again `60` ticks/s. Phase 7 performance runs used a
temporary bundled advance containing five actor runs and one main-program run;
their counts and rates must be multiplied by five before comparing equivalent
simulation work with the current tick clock.

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

#### Phase 2 completion-audit addendum

The 2026-07-25 audit found that the kernel-side retirement changes are sound,
but the external history boundary is not complete:

- culture-locality birth telemetry and entity snapshots still scan the entity
  lifetime high-water mark and read retained retired rows;
- UI and telemetry infer generator attribution by correlating independent events
  on the same tick instead of consuming an explicit creation cause;
- entity creation does not carry the stable name seed and initial state needed
  to form durable external records without later kernel-row reconstruction;
- relationship removal is inferred from strength rather than represented by an
  explicit lifecycle fact; and
- `ProvenanceProjection` retains lifetime invocation/output/relationship maps
  even when only compact active causality and already-rendered journal facts are
  still useful.

Remediation reopens Phase 2. It will capture creation-time identity, locality,
cause, and relationship-lifecycle facts at the primitive event boundary; make
active snapshots iterate active occurrences only; form durable names and
narrative facts at event ingestion; and compact or release raw projection
history once no active consumer needs it.

Additional intended files:

- `src/Engine.Eventing/KernelEvent*.cs`
- `src/Engine.Host/KernelHost.CultureLocality.cs`
- `src/Engine.Host/KernelRuntimeFactory.cs`
- `src/Engine.Projection/UiProjectionState.cs`
- `src/Engine.Projection/ProvenanceProjection.cs`
- `src/Engine.Projection/ProjectionSymbolCatalog.cs`
- `src/Engine.Observability/TelemetryEventProjection*.cs`
- `src/Engine.UI/GameRuntimeChronicle.cs`
- directly affected event/projection behavior tests

The reopened exit gate additionally requires that no production telemetry or
projection path reconstruct creation facts from retired kernel rows, same-tick
event ordering, or an unbounded raw provenance archive.

The separate display/projection discussion that had remained in
`.sulion-paste` has now been resolved into this contract. `Engine.Eventing` is
the plain-record assembly shared by producer and consumers;
`Engine.Projection` does not reference `Engine.Kernel`. The completed Phase 2
remediation therefore keeps active causality and a bounded recent window in the
live projection, while any future hot view store, keyframes, or archive tier
remain consumer-side work rather than a kernel compatibility layer.

#### Phase 2 completion-audit remediation

- `EntityCreated` now carries a required stable name seed and creation-time
  physical/semantic locality facts. Birth telemetry consumes that event
  directly, and current entity sampling iterates the active list.
- `RelationshipChanged` carries explicit active/end state. Projection and
  telemetry no longer infer relationship lifetime from strength.
- Generator-created entities are joined to generator invocations through the
  event execution-context token and the invocation completion record; the
  same-tick inference dictionaries and future-migration placeholders are gone.
- The live provenance projection retains structural creation outputs, active
  causal parent chains, and a bounded recent window. References are released
  when their active entity or relationship ends.
- Obsolete provenance growth capacity codes, growth reasons, and the
  `KernelStorageGrown` event family were deleted with the storage they once
  described.

### Phase 3: Remove Materialized Global And Observer Work

Status: reopened and remediated on 2026-07-25 after the non-fixed-point
completion audit.

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

#### Phase 3 completion-audit addendum

The 2026-07-25 audit found that the deleted global programs stayed deleted, but
several observer costs were narrowed or relocated instead of removed:

- scalar entity, relationship, attribute, position, terrain, and coverage
  mutations still advance versions and call a provenance-output adapter whose
  detailed arguments are discarded;
- physical map changes and snapshots emit both the plane-aware map event and a
  legacy physical-only compatibility event;
- culture component and hole topology is allocated and calculated synchronously
  in the Monte Carlo host at sample time rather than derived offline from the
  sampled cell field; and
- the stable repository instructions still describe deleted MapActivity,
  MapSignals, CultureBoundaries, and separate upkeep programs as current.

Remediation reopens Phase 3. It will replace the remaining scalar provenance
adapter with semantic action/structural events and bounded sampled revisions;
collapse map state to one plane-aware event contract; export the raw sampled
culture field without synchronous topology analysis; move component/hole
derivation to the existing DuckDB/Python analysis boundary; and update stable
architecture instructions to describe the surviving runtime.

Additional intended files:

- `src/Engine.Kernel/WorldState.cs`
- `src/Engine.Kernel/WorldState.Attributes.cs`
- `src/Engine.Kernel/WorldState.EntityPosition.cs`
- `src/Engine.Kernel/WorldState.MapLayers.cs`
- `src/Engine.Kernel/WorldState.Provenance.cs`
- `src/Engine.Kernel/Persistence/WorldStateSnapshot.cs`
- `src/Engine.Eventing/KernelEvent*.cs`
- `src/Engine.Host/KernelHost.CultureLocality.cs`
- `src/Engine.Projection/HexMapProjection.cs`
- `src/Engine.Projection/UiProjectionState.cs`
- `src/Engine.Observability/TelemetryEventProjection*.cs`
- `tools/MonteCarlo/**` and its DuckDB/Python analysis
- `AGENTS.md`, `docs/architecture.md`, and `docs/game-systems.md`
- directly affected event, projection, and analysis contract tests

The reopened exit gate additionally requires one canonical map event family, no
discard-only provenance mapper, and no topology allocation or traversal on the
simulation/telemetry capture path.

#### Phase 3 completion-audit remediation

- The discard-only scalar provenance adapter and its callers are deleted.
  World revisions advance at most once per tick instead of once per
  scalar mutation; semantic events and structural provenance remain explicit.
- `MapCellStateChanged` is the only map-cell lifecycle event. The physical-only
  compatibility event family, duplicate emission, projection dispatch, and
  tests were removed.
- The host captures raw culture-field cells only. Component derivation moved to
  the Monte Carlo DuckDB analysis query, so capture does not allocate or walk
  culture components and holes.
- Stable architecture and simulation documents now describe direct locality,
  unified effects/lifecycle resolution, the deleted graph/runtime programs, and
  explicit future mutation ownership rather than treating one writer as a
  permanent constraint.

### Phase 4: Simplify Low-Altitude Execution

Status: reopened and remediated on 2026-07-25 after the non-fixed-point
completion audit.

Owner: `Engine.Kernel` runtime plus authored rule compilation in
`Engine.Gameplay`/`Engine.Domain`.

Work:

- preindex rules by entity or relationship kind;
- use active/spatial/adjacency indexes instead of lifetime scans;
- merge effects and upkeep into one resolution model;
- move relationship-follow movement into explicit entity movement intent;
- incrementally maintain district modifiers;
- replace raw/main clocks with one tick clock, minor actors every tick, and
  explicit main-program cadence;
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
  ticks; minor actors run once per tick and ordinary main programs run on their
  explicit cadence.
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

#### Phase 4 completion-audit addendum

The 2026-07-25 audit found several incomplete hot-path conversions and retained
compatibility mechanisms:

- minor-actor source-kind and source-rule indexes are populated but the runtime
  still scans all emitter rules and all verbs;
- knowledge clears and rebuilds all culture state on cadence, rescanning every
  site and every grant, while culture slots reevaluate every slot and compatible
  entity instead of using dirty/due work;
- follow movement repeatedly searches incoming relationship kinds from the
  active-entity loop instead of consuming a direct follow source or a
  relationship-driven intent pass;
- locality scoring recomputes overlapping support, threat, capacity, and
  attractor neighborhoods within the same candidate decision;
- low-altitude verb selection silently ignores compatible rules after a
  hard-coded 64-entry scratch limit, while chronicle strike capture silently
  drops executions after a hard-coded 256-entry pending limit;
- the unused no-verb minor-actor action path preserves the retired behavior
  model even though production minor actors are verb-authored;
- stage-rule lookup still scans the global stage catalog in entity hot paths;
  named-entity stage state is misleadingly owned by minor-actor state;
- the unused object `WorldGraph`, historical entity/relationship model, graph
  events, QuikGraph dependency, and their legacy tests remain beside the kernel;
  and
- old tracked `TestResults` files remain after test output moved to
  `.artifacts/test-logs`.

Remediation reopens Phase 4. It will consume compiled indexes end to end; add
explicit dirty/due invalidation for knowledge and culture slots; make follow
ownership directly iterable; share composite locality samples inside a
decision; derive bounded scratch capacities from validated compiled maxima or
fail installation explicitly; require verb-authored minor actors; preindex
stage rules; move named-entity stage data to an appropriate entity program
owner; remove the dead graph runtime and dependency while retaining the live
primitive IDs; and delete obsolete generated test output.

Additional intended files:

- `src/Engine.Kernel/Programs/MinorActors/*`
- `src/Engine.Kernel/Programs/Knowledge/*`
- `src/Engine.Kernel/Programs/CultureSlots/*`
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/*`
- `src/Engine.Kernel/Programs/Relationships/*`
- `src/Engine.Kernel/Programs/Chronicles/*`
- `src/Engine.Kernel/Programs/Effects/*`
- `src/Engine.Kernel/LocalityQuery.cs`
- `src/Engine.Kernel/WorldState.Stages.cs`
- `src/Engine.Domain/Compilation/KernelPrograms/*`
- `src/Engine.Domain/World/*` and live primitive-ID replacements
- `src/Engine.Domain/Engine.Domain.csproj`
- `tests/Engine.Domain.Tests/Graph/*`
- `.gitignore` and tracked `tests/**/TestResults/*`
- directly affected compiler, runtime, overflow, and architecture tests

The reopened exit gate additionally requires that hot paths consume only
compatible indexed work, dirty/due systems do not rebuild clean cultures or
slots, capacity overflow is explicit rather than silent, and no retired runtime
model survives only for compatibility.

#### Phase 4 completion-audit remediation

- Minor-actor emission consumes its source-kind rule index and verb selection
  consumes the source-rule verb index. Source-less and verb-less cohorts are
  invalid, and active cohorts retire with their named source.
- Knowledge recomputes only cultures whose relevant world revision changed.
  Culture slots reevaluate changed slots and separately due tenure expirations
  rather than rescanning every slot/entity pair on cadence.
- Follow movement reads a direct target-to-follow-relationship index maintained
  with relationship creation and retirement. Locality candidates reuse
  composite pressure and crowding samples instead of repeating overlapping
  neighborhood queries.
- Low-altitude candidate scratch is sized from validated compiled rule
  capacity. Chronicle pending strike storage is sized from configured entity
  and generator capacity, and overflow fails explicitly rather than silently
  truncating at 64 or 256.
- Stage rules are indexed by entity kind. Named-entity work tiers moved from
  minor-actor state into `WorldState`; obsolete non-catalog stage entries were
  removed from Ice Remembers.
- The unused object graph, historical entity/relationship classes, graph
  events, QuikGraph dependency, and legacy graph tests are deleted. Host
  relationship sampling now also consumes the active relationship list.
- Generated `tests/**/TestResults` artifacts are ignored and removed. A
  misleading test-only “culture labor influence” alias that merely forwarded
  to raw culture mass was also deleted.

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

That sample advanced 1,000 temporary bundled advances in `16.006344` seconds,
or `62.5` bundled advances/s. Its old tick label must not be compared directly
with Phase 1's `221.2` actual ticks/s: each bundled advance performed five
minor-actor runs and one main-program run, equivalent to five current ticks.
The sample therefore represents `312.4` current-tick equivalents/s while
reaching a world five times farther into simulation. It was captured from a
partial conversion and does not establish the performance of the completed
fixed-point implementation. The earlier inference that it cleared the completed
Phase 5 implementation was wrong; the 2026-07-25 remediation addendum below
replaces that performance conclusion.

The full-conversion remediation was accepted with repository-wide `make ci`
(1,068 fast tests) and the registered Release smoke run
`preparallel-phase5-full-fixedpoint-1x250-20260724`. Seed `9100` advanced 250
temporary bundled advances in `10.7629472` simulation seconds (`23.2278`
bundled advances/s), produced 379,257 telemetry rows and 16 generator fires,
and reported `map_degenerate=0`. This short run proves executable integration
and supplies a post-remediation sanity point; it is deliberately not a
late-horizon performance or balance claim.

#### Phase 5 performance remediation addendum (2026-07-25)

The complete conversion had translated two hot gameplay curves directly into
generic fixed-point transcendental functions: saturation used a fourteen-term
Taylor `Exp`, while spread headroom used `Pow` through iterative `Log` and
`Exp`. Every term performed general `FixedScalar` multiply/divide with `Int128`
intermediates. This preserved a narrow mathematical formula at the expense of
the simulation and violated the broader simplification allowance of this plan.

Detached, pre-rendered-world checkpoints established the regression without
rerunning world generation:

- final Phase 5 commit `56215b3`:
  `preparallel-checkpoint-phase5-1x1000-20260725`, seed 9100, 1,000 temporary
  bundled advances, `24.4142` bundled advances/s;
- Phase 6 commit `0bcb494`:
  `preparallel-checkpoint-phase6-1x1000-20260725`, same workload,
  `25.4895` bundled advances/s;
- Phase 7 before remediation:
  `preparallel-remediation-phase7-before-1x1000-20260725`, same workload,
  `27.7275` bundled advances/s.

These historical bundled advances were one `AdvanceTicks` iteration containing
one pass of the named simulation systems plus five minor-actor labor runs. They
are not a current simulation unit. The checkpoint proves that Phase 7 did not
create the main slowdown; final Phase 5 already had it.

The remediation replaces saturation with the monotonic rational curve
`x / (x + scale)` and the bounded `[0, 1]` five-fourths headroom curve with
`x * (3 + x) / 4`. These preserve the required broad ordering and endpoint
shape without preserving the exact old formula. The now-unused generic
`FixedScalar.Exp`, `Pow`, and `Log` implementations are deleted, not retained
behind adapters. Bounded fixed-point ratio, multiply, and divide operations use
an exact signed-64-bit path and fall back to `Int128` only when the product
cannot fit; midpoint-away rounding and saturation remain unchanged.

| Field family | Authored and current runtime range | Runtime representation and precision | Reduction, clamp, and overflow | Persistence, events, and presentation |
| --- | --- | --- | --- | --- |
| Raw culture mass and deltas | Current per-cell state is `[0, 2000]`; authored thresholds reach `300`, initial sources reach `35`, district emission reaches `2`, and erosion reaches `25` per application. The generic conversion accepts signed deltas through `+/-1,000,000`. Zero cells are removed by the sparse layer. | `int32`, scale `1,000`, precision `0.001`; total physical culture is `int64` units. | Each contribution is quantized before merge. Adds use an `int64` intermediate and saturate at the per-cell `[0, 2,000,000]` unit clamp. Ratio products use `int64` and deterministic midpoint-away rounding. The world total is checked rather than wrapped. | Snapshot 49 stores integer cell values and the integer total. Culture state-change events expose only integer units; projection and telemetry divide at their own boundary. |
| Relationship strength and deltas | Strength is `[0, 1]`; authored conditions observed through `0.85`, with current deltas approximately `[-0.32, +0.04]`. Zero or a compiled cull threshold ends an active relationship. | `int32`, scale `1,000,000`, precision `0.000001`. | Maintenance and effect contributions are quantized independently, summed with saturating `int64` addition, then clamped to `[0, 1,000,000]`. Cull is tested after the deterministic merge. | Snapshot 49 and relationship events store integer strength units. Projection and telemetry explicitly divide outside the runtime boundary. |
| Persistent attributes, including health | The shared contract is `[-10,000, +10,000]`; Ice Remembers health is authored as `[0, 100]` with default `90`, and current effects include both small continuous changes and discrete changes up to roughly `30`. | `int32`, scale `100,000`, precision `0.00001`. | Every effect contribution is quantized before accumulation. Adds use `int64` and saturate at the shared numeric envelope, followed by the compiled per-attribute min/max clamp. Invalid authored bounds are rejected. | Snapshot 49 and attribute events store integer units. Domain definitions may remain readable floats before compilation; projection and telemetry divide outside the runtime boundary. |
| Road quality and work | Quality is `[0, 100]`; active checkpoints are `25` and `75`, ordinary authored work is around `1`, minor-actor work reaches about `1.6`, and the current per-main-cadence decay ratio is `0.9995`. | `int32`, scale `1,000`, precision `0.001`; decay ratios use scale `1,000,000`. | Work uses saturating `int64` addition followed by `[0, 100,000]` clamp. Lazy decay uses integer exponentiation by squaring and `int64` ratio products with midpoint-away rounding; checkpoint floors are integer units. | Movement-state schema 7 and snapshot 49 store integer quality and floor tables. Road events store integer units; path scoring remains fixed point and off-kernel consumers convert explicitly. |
| Persistent local modifiers | District healing uses attribute units; district culture emission uses culture units. Entity influence/favor deltas use scale `100,000` and a shared `+/-10,000` envelope. Knowledge and decay multipliers use scale `1,000,000` and a shared `+/-100` envelope; current derived influence is clamped to `[0.1, 3]`. | `int32`, using the destination field's scale or the explicit modifier/ratio scale. This avoids a misleading universal modifier precision. | District totals and entity modifier deltas use saturating `int64` addition. Multiplier composition uses `int64` products and deterministic midpoint-away division. Semantic clamps occur after the integer merge. | Snapshot 49 stores the integer tables and arrays. Authored district bonuses and multipliers may stay floats before compilation; off-kernel consumers divide by the selected scale. |
| Dimensionless runtime scalars | Probabilities, candidate weights, scores, ratios, heat, path costs, affinities, and other non-stock values use the ranges required by their owning rule. | Signed `int64` `FixedScalar`, scale `1,000,000`, precision `0.000001`; positive infinity has an explicit sentinel. | Adds saturate. Ratio, multiply, and divide use exact `int64` operations when the intermediate fits and an `Int128` fallback otherwise; division rounds midpoint values away from zero. Owning rules apply their semantic clamps. Generic transcendental functions are not part of the runtime scalar contract. | Compiled program records, primitive scalar tables, snapshot fields, command slots, and event long slots carry integer units. Only authored and off-kernel presentation APIs convert to floating point. |

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

#### Phase 6 implementation result

- Entity and relationship storage are reusable fixed-capacity slot pools with
  dense active vectors. Kind, culture, incident-edge, follow, physical, and
  semantic indexes are maintained incrementally as active occurrences enter,
  move, and retire.
- Entity and relationship occurrence handles encode slot plus generation.
  Primitive events, commands, provenance frames, chronicle pending state,
  district hosts, gambit execution state, and frozen relationship adjacency
  carry occurrence handles anywhere an identity can outlive an immediate
  mutation. Reusing a slot therefore cannot make a stale reference name the
  replacement occurrence.
- Entity attributes and traits are direct per-slot slabs. Gambit cooldowns,
  culture slots, low-altitude intent state, relationship contact/follow state,
  and minor-actor source/rule state use direct owner-local rows cleared on
  occurrence retirement.
- Map coordinates are registered once as stable dense cell IDs. Terrain,
  cover, region identity, occupancy, coverage, roads, travel memory, culture
  totals, and `(culture, cell)` influence use direct cell storage plus dense
  active or dirty indexes. Zero culture rows, ended coverage, and inactive
  road/travel rows release their active work entries without discarding the
  durable map coordinate.
- Districts are active reusable rows with direct host linkage and incremental
  host/cell modifiers. Minor actors remain individual reusable cohorts and
  continue to use the due heap introduced in Phase 4.
- Relationship creation deduplicates the directed
  `(source occurrence, target occurrence, kind)` topology. Ending an edge
  removes it from active and adjacency indexes before its slot can be reused.
- Snapshot format `51` is an active-state replacement, not a compatibility
  reader. It writes active entities, relationships, districts, cohorts,
  chronicles, culture slots, map layers, sparse program-owned live rows, and
  generation metadata required to reject stale handles after reload. It omits
  retired entity rows, ended relationship rows, completed provenance history,
  expired cooldown rows, and derived route caches; indexes, free lists, due
  queues, and derived caches are rebuilt on load.
- Contract coverage exercises entity and relationship churn, stale-handle
  rejection before and after reload, frozen-adjacency reuse safety,
  relationship deduplication, active-only snapshots, reclaimed culture rows,
  and derived-cache rebuilding. Final `make ci` passed on 2026-07-25 with zero
  analyzer warnings, formatting and domain-boundary checks, and all 1,042 fast
  tests.

### Phase 7: Prove The Serial Epoch And Delta Model

Owner: `Engine.Kernel`; the driver remains a one-writer caller.

Work:

- add frozen tick views, typed delta buffers, deterministic resolution, keyed
  randomness, semantic event publication, and phase revisions;
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

#### Phase 7 implementation result

- `Simulation` now executes six explicit one-writer phases: command admission,
  local-view preparation, local evaluation, deterministic local resolution,
  strategic work, and publication. Local evaluation reads a frozen active-set
  view and writes nine typed delta families; resolution owns mutation,
  collision policy, grouped fixed-point reductions, and structural changes.
- All surviving kernel program randomness is stateless and keyed by seed,
  tick, program/action identity, occurrence handle, and purpose. The mutable
  random stream and its snapshot payload were removed instead of retained
  behind an adapter.
- Events are buffered and deterministically ordered at semantic phase
  boundaries. Phase 7 initially published a redundant second-clock revision
  beside the phase revision; Phase 8 removed it and retains the event tick plus
  phase revision. Kernel snapshot format 53 persists the current execution
  state without reviving the deleted random-stream state.
- Contract coverage proves keyed calls are evaluation-order independent,
  reduction occurs before clamping, reversed evaluation and delta insertion
  produce the same state, and one versus seven logical shards converge to the
  same published state. Retirement regressions also prove gambit and generator
  completion events retain captured occurrence handles rather than resolving a
  freed entity slot after an effect commits.
- Registered Release profile
  `preparallel-phase7-serial-epoch-prerendered-1x1000-20260725` loaded the
  versioned seed-4200 world and completed 1,000 former bundled advances in
  40.814 seconds (24.50 bundled advances/second, equivalent work to 122.5
  current ticks/second). Normalized against the sampled `AdvanceTicks` subtree,
  the local epoch accounts for 82.0%. Its identifiable children are minor
  actors at 51.3% of advance work, low-altitude verbs at 22.8%, effects at 4.2%,
  and deterministic delta resolution at 1.6%. The remaining named strategic
  and publication subtrees are led by gambit entity evaluation at 8.9%,
  generators at 7.5%, and phase event publication at 2.8%. Optimized-stack
  attribution makes these a directional phase map rather than an additive
  accounting identity or a parallel scaling claim; Phase 9 owns that decision.
- The profile also showed the concrete serial regression: generic fixed-point
  `Exp` alone consumed 13.8% of the `AdvanceTicks` subtree, fixed-point division
  another 7.2%, and repeated `CultureLocality.CellAt`/neighborhood work 28.8%.
  The remediation described in the Phase 5 addendum removes the transcendental
  calls. `CultureLocality` now has a direct-mapped, fixed-capacity,
  revision-stamped cell cache, so overlapping actor and verb reads reuse the
  same radius-two result until culture mutation advances the revision. Keyed
  random tuples are folded before one integer avalanche, scalar draws map bits
  directly to fixed units, and minor-actor selection carries the winning
  priority instead of recalculating it. These remain deterministic,
  allocation-free runtime operations and do not add shared mutable topology.
- Three registered Release, simulation-only seed-9100 repeats against the same
  pre-rendered world measured `62.9625`, `72.3497`, and `70.8329` former
  bundled advances/second, equivalent to `314.8125`, `361.7485`, and
  `354.1645` current ticks/second:
  `preparallel-remediation-fastmath-cache-1x1000-20260725`,
  `preparallel-remediation-fastmath-cache-repeat1-1x1000-20260725`, and
  `preparallel-remediation-fastmath-cache-repeat2-1x1000-20260725`. The median
  is `70.8329`, 2.55 times the exact pre-remediation checkpoint and above the
  partial-Phase-5 no-regression floor in every repeat.
- Relationship-index mutation was only 4.5% of the pre-remediation
  `AdvanceTicks` profile. Because the serial gate now passes, the proposed
  phase-batched relationship-index rebuild is not implemented. Reopen it only
  if a post-change profile at a representative horizon makes it material; do
  not add that complexity preemptively.
- The profile exposed that fresh world generation, rather than tick execution,
  dominated short-run wall time. Monte Carlo now accepts a validated
  pregenerated `--world` for single-run work. `make mc-smoke` and
  `make profile-kernel` use that path, while `make mc` retains fresh/distinct
  world generation for calibration batches. Registered smoke
  `phase7-prerendered-smoke-20260725` loaded seed 4200 and completed its 1,000
  former bundled advances at 26.89/second without the multi-minute
  world-generation prelude. The earlier fresh-generation trace
  `preparallel-phase7-serial-epoch-1x1000-20260725d` remains the evidence for
  that startup diagnosis: simulation occupied only 4.50% of its total sampled
  thread time.
- `make test` now rebuilds before executing while retaining its full log and
  concise failure summary; the former `--no-build` path could report success
  from stale test assemblies. Final `make ci` passed on 2026-07-25 with zero
  analyzer warnings, clean formatting and domain boundaries, and all 1,047 fast
  tests.

### Phase 8: Correct Tick And Lifecycle Contracts

Owner: `Engine.Kernel` for scheduling, runtime lifecycle math, snapshots, and
events; `Engine.Domain` for the lifecycle compilation contract; `Engine.Host`
for tuning input; `IceRemembers` for lifespan bands; stable system and
architecture documentation for the public model.

#### Observed failures

Phase 7's first performance remediation bundled one main-program pass and five
minor-actor runs behind one `AdvanceTicks` iteration. That made the API tick a
second calendar unit, introduced minor-actor substeps and redundant clock
revisions, and made the new throughput labels incomparable with the earlier tick
measurements. It also changed the effective cadence of every duration whose
authored value had been tuned against main-program opportunities.

The fixed-point lifecycle conversion stored very small base-rate and
age-coefficient values. Those values were not meaningful authored facts; they
were the expanded coefficients of a curve whose slope is proportional to
`1 / target²`. The representation obscured the intended lifespan, was
precision-sensitive, and encouraged tuning around numeric artifacts.

The full-domain lifecycle assertions had two distinct problems:

- their exact content expectations had become stale as accepted stage design
  moved settlements to Standard, artifacts to Enduring, and points of interest
  to Monumental while regions remained WorldAnchor; and
- they asserted only authored floating-point coefficients, so they could not
  catch a compiler/runtime conversion that preserved the source config while
  corrupting the executable curve.

Those tests are categorized `Slow`, so normal `make test` and `make ci` do not
execute them. Work that changes the full domain therefore also requires the
repository's full-domain verification path.

#### Causal correction

Keep the Phase 7 phase graph, frozen tick view, typed deltas, destination-owned
reduction, occurrence handles, and keyed randomness. Replace only the
incompatible clock and lifecycle representations:

1. `AdvanceTicks(1)` advances one tick.
2. Minor actors run once per tick and store only tick deadlines.
3. Main movement, verbs, relationships, effects, gambits, chronicles, and
   generators run every five ticks by default.
4. Slower culture, world-beat, frontier, slot, and knowledge programs retain
   explicit tick cadences.
5. Main-program commitments, cooldowns, generator cadence/jitter, fire windows,
   recency windows, and tenure are normalized once to tick deadlines at their
   registration boundary. Minor-actor TTL/action/emission values are already
   tick values and are not multiplied. Chronicle and world-age durations were
   already absolute calendar ticks and remain unchanged.
6. Road decay counts elapsed main-program cadences rather than exponentiating
   its per-main-run ratio once for every tick.
7. Lifecycle config stores `TargetTicks` and `FlatDrainShare`. Compilation
   derives the nominal health budget from `health.default - health.min`.
8. For `x = age / effectiveTargetTicks`, the default cumulative drain is
   `healthBudget * (flatShare*x + (1-flatShare)*x²)`. The late-life option uses
   half the flat share plus an `x³` age term. Each effects run applies the
   cumulative difference since the previous main-program boundary; stage
   multipliers stretch the target and variance scales that interval drain.

This is a mixed evolution rather than a rollback. Phase ownership and
deterministic reduction remain because their contracts are sound. The bundled
clock and expanded lifecycle coefficients are removed because they are
causally wrong. Individual minor actors remain unchanged as a gameplay model;
only their scheduling unit changes.

#### Content disposition

The accepted Ice Remembers base targets are actual ticks at 250 ticks per
in-world year:

| Band | Target ticks | Current examples |
|---|---:|---|
| Brief | 10,000 | penguins |
| Standard | 17,500 | settlements, orcas |
| Enduring | 22,500 | artifacts, monsters |
| Generational | 50,000 | factions, abilities |
| Monumental | 100,000 | points of interest |
| WorldAnchor | no passive drain | regions |

Artifact retirement remains final. A later reawakening creates a new occurrence
and new relationships; no historical relationship set is retained to support
that case.

#### Intended files

- scheduler and execution: `src/Engine.Kernel/Simulation*.cs`,
  `src/Engine.Kernel/Execution/*.cs`, `SimulationOptions.cs`;
- minor actors and persistence:
  `src/Engine.Kernel/Programs/MinorActors/*.cs`,
  `src/Engine.Kernel/Persistence/*.cs`;
- affected cadence owners: low-altitude verbs, gambits, generators, culture
  slots, movement road decay, culture field, world beats, and map frontier;
- lifecycle contract:
  `src/Engine.Domain/Config/DomainConfig.cs`,
  `src/Engine.Domain/Compilation/DomainCompiler.Lifecycle.cs`,
  `src/Engine.Domain/Compilation/KernelPrograms/LifecycleProgram.cs`,
  `src/Engine.Kernel/Programs/Effects/*`, and the Ice Remembers entity
  definition;
- host configuration, kernel event/projection contracts, affected durable
  tests, and the stable architecture/system documents.

#### Explicit non-goals

- no parallel workers or new parallel strategy;
- no aggregate minor-actor rewrite;
- no attempt to reproduce exact pre-Phase-7 event order or RNG consumption;
- no content-stage reversal;
- no deletion of lifecycle content assertions merely because their old
  expected values were stale;
- no long Monte Carlo confidence run.

#### Implemented result

- The redundant second-clock revisions, frozen-view/delta legacy names,
  minor-actor substeps, and the minor-actor internal secondary counter are
  removed. Phase events carry the tick plus the monotonic phase revision.
- Generator burst policy is named a fire window, not a second clock.
- Snapshot format `53`, effects data schema `7`, and minor-actor data/state
  schemas `5`/`6` intentionally reject the replaced layouts.
- The lifecycle content assertions now encode the accepted bands and
  WorldAnchor boundary. A separate compiler contract compares every authored
  lifecycle against its compiled target, health budget, and curve share.
- The obsolete floating-point `LinearAging` loader and model are deleted rather
  than retained as a mapper beside the target-based fixed-point contract.
- Startup seed entity placement no longer expands the live map as a side
  effect. Runtime creation and movement still reveal on arrival, while the
  startup seed's explicit cells remain the complete initial map contract.
- The six exact slow-test failures exposed by the lifecycle/content audit now
  pass: lifecycle compilation, canonical kind coverage, TOML cadence loading,
  startup projection hydration, generic seed ordering, and stochastic distance
  preference. Their stale content-mirroring assumptions were removed without
  weakening the lifecycle compiler or canonical-kind contracts. Running every
  unrelated slow test is not an exit gate for this phase.
- The tick correction initially exposed a Monte Carlo measurement-path
  regression: simulation-only runs crossed the host/driver/kernel boundary once
  per tick, stopping the driver and constructing 5,000 stepped drivers for the
  standard smoke. Registered pre-fix runs
  `phase8-tick-contract-smoke-20260725-1121z` and
  `phase8-tick-contract-smoke-repeat-20260725-1122z` measured `300.9017` and
  `287.5904` ticks/second. The runner now advances in progress-window batches,
  preserving exact tick totals and output boundaries without putting that
  tooling overhead in the kernel measurement. Registered run
  `phase8-tick-contract-batched-smoke-20260725-1127z` completed 5,000 actual
  ticks against the pre-rendered seed-9100 world in `15.1570` seconds at
  `329.8812` ticks/second, above the `312.5` no-regression floor.
- Final `make ci` passed all 1,047 fast tests on 2026-07-25 with zero build
  warnings or errors.

### Phase 9: Gate Parallel Strategies

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

#### Phase 9 result

The gate selects a **typed-owner bulk-synchronous hybrid** as the only parallel
candidate with enough measured promise to implement. It combines:

- stable-ID or dynamically chunked evaluation for entities, minor actors, and
  relationships;
- exclusive destination owners by storage type during reduction: entity,
  relationship, and cell/map owners are separate domains;
- spatial partitioning only for genuinely cell-owned map and culture state;
- persistent workers and bounded worker-local intent/event storage;
- deterministic reduction and publication at phase boundaries inside the one
  tick clock.

This is BSP plus limited spatial destination ownership. It is not a persistent
geographic-actor simulation. It also does not make subsystem offload a second
kernel scheduler. Projection, telemetry, export, and analysis remain
independently paced consumers outside simulation-only timing.

No threads were added in this phase. The speedups below are Amdahl sensitivity
models grounded in the measured phase profile, work distribution, and
cross-shard traffic. They are not presented as observed parallel speedups.

#### Evidence provenance and serial position

Three bounded, registered Release runs used the pre-rendered seed-9100 world:

- `phase9-serial-1x10000-20260725a`: one 10,000-tick simulation-only sampled
  trace. Trace overhead reduced throughput, so this run is used only for
  normalized CPU attribution. Its reports are
  `.artifacts/profiles/phase9-serial-1x10000-20260725a.inclusive180.txt` and
  `.artifacts/profiles/phase9-serial-1x10000-20260725a.exclusive120.txt`.
- `phase9-serial-untraced-1x10000-20260725a`: one untraced simulation-only run,
  completing at `203.9967` ticks/s overall. Ticks 1-5,000 ran at `330.4`
  ticks/s and ticks 5,001-10,000 at `147.5` ticks/s.
- `phase9-locality-1x10000-20260725a`: one telemetry run used for work counts,
  interaction distance, candidate shard load, and cross-shard traffic. It
  produced 3,559,566 primitive rows and ended with 232 active entities and 627
  covered cells.

Important profile, performance, and SQL-derived locality findings are stored in
the corresponding registry entries with `append-stats`. The Makefile now
provides `mc-append-stats` so this required registry operation does not need a
hand-rolled Monte Carlo invocation.

The current 10,000-tick serial run is substantially faster than the older
100,000-tick references, but it does not prove the 60 ticks/s gate at tick
100,000. The declining 5,000-tick windows show why a short-run rate cannot be
extrapolated as a pass. Per the accepted diagnostic bound, Phase 9 does not pay
for a new multi-hour serial 100,000-tick baseline. The stop gate therefore has
not passed, and the long evidence run remains deferred until a candidate first
passes the short gates below.

#### Midgame evidence addendum

The user clarified that the serial redesign has already delivered roughly 2x
and that the remaining parallel speedup target is **5-10x**, not 15-30x. A
longer same-seed measurement was authorized to test whether the 10,000-tick
strategy analysis survives midgame growth.

`phase9-midgame-serial-1x25000-20260725a` ran 25,000 ticks simulation-only
against the same pre-rendered seed-9100 world. It completed in 372.063 seconds
at 67.193 ticks/s overall, but the overall result conceals a severe decline:

| Tick window | Simulation-only ticks/s | Milliseconds/tick |
| --- | ---: | ---: |
| 1-5,000 | 302.3 | 3.308 |
| 5,001-10,000 | 134.0 | 7.463 |
| 10,001-15,000 | 65.7 | 15.221 |
| 15,001-20,000 | 44.0 | 22.727 |
| 20,001-25,000 | 38.9 | 25.707 |

The final window retains only 12.87% of the first window's throughput. The
25,000-tick overall rate being above 60 does not pass the 100,000-tick target:
the current instantaneous midgame rate is already below it.

`phase9-midgame-locality-1x25000-20260725a` repeated the horizon with telemetry.
It completed at 60.241 ticks/s overall and wrote 14,340,766 primitive rows.
Telemetry overhead makes it unsuitable as the performance baseline, but it
identifies the growing work:

| Tick | Active entities | Active relationships | Covered cells | Entity density |
| ---: | ---: | ---: | ---: | ---: |
| 5,000 | 123 | 254 | 570 | 0.216 |
| 10,000 | 232 | 562 | 627 | 0.370 |
| 15,000 | 343 | 872 | 685 | 0.501 |
| 20,000 | 406 | 1,144 | 742 | 0.547 |
| 25,000 | 470 | 1,329 | 785 | 0.599 |

The footprint grows slowly while entities and relationships accumulate inside
it. Per-tick event work follows that accumulation:

| Tick window | Minor-actor events/tick | Relationship events/tick | Generator target evaluations/tick |
| --- | ---: | ---: | ---: |
| 1-5,000 | 87.20 | 18.50 | 101.89 |
| 5,001-10,000 | 189.29 | 43.44 | 38.17 |
| 10,001-15,000 | 307.85 | 71.37 | 33.50 |
| 15,001-20,000 | 401.78 | 95.19 | 40.79 |
| 20,001-25,000 | 475.10 | 116.32 | 49.40 |

Minor-actor work increases 5.45x and relationship mutation work 6.29x from the
first to final window. Generator target evaluation falls by about half, so
generator scans do not explain the throughput collapse. Average cost per active
entity rises from 0.0396 ms in the first window to 0.0587 ms in the final
window, showing that entity count alone is incomplete: denser relationship,
culture-locality, and candidate work adds roughly 48% per-entity cost. That
per-entity cost does flatten between the fourth and fifth windows, but total
active work is still growing.

The midgame ownership measurements preserve the Phase 9 ranking:

| Final-window 16-way partition | Load efficiency |
| --- | ---: |
| Minor work by stable actor ID | 0.985 |
| Generator targets by stable entity ID | 0.841 |
| Relationships by canonical-key hash | 0.793 |
| Active entities by physical geography at tick 25,000 | 0.344 |
| Active entities by semantic geography at tick 25,000 | 0.475 |

| Final-window cross-shard traffic at 16 geographic shards | Physical | Semantic |
| --- | ---: | ---: |
| Minor-actor selected action | 27.522% | 26.414% |
| Relationship mutation | 65.887% | 71.531% |
| Gambit actor-target pair | 36.390% | 75.235% |

Minor work remains local, but stable-ID partitioning is far better balanced
than geography. Relationship work becomes both more important and remains
predominantly nonlocal. This strengthens, rather than weakens, the choice of
typed relationship owners inside BSP. Persistent geographic actors remain
rejected.

The important correction is to the confidence attached to the 10,000-tick CPU
percentages. The Amdahl table below remains a sensitivity model, but midgame
contains substantially more relationship and dense-locality work than the
profiled horizon. The first parallel candidate must report late-window phase
timings and speedup, not merely whole-run or early-window throughput. The
strategy choice holds; its exact 8-11x modeled result is headroom around the
accepted 5-10x target, not a promise.

#### Midgame serial-collapse diagnosis

Parallel implementation is paused at this gate. The selected typed-owner BSP
topology remains the parallelization decision, but parallelizing the current
strategic candidate scan would preserve and distribute accidental work. The
serial algorithm must be reduced first and the phase model reprofiled.

`phase9-midgame-cpu-1x25000-20260725a` is the registered Release,
simulation-only sampled-thread trace for this diagnosis. It uses the same
pre-rendered seed-9100 world as the untraced and locality runs. Trace overhead
reduced the overall rate to 63.031 ticks/s, but its fixed windows reproduced the
same collapse:

| Tick window | Traced ticks/s |
| --- | ---: |
| 1-5,000 | 279.5 |
| 5,001-10,000 | 126.0 |
| 10,001-15,000 | 59.5 |
| 15,001-20,000 | 41.6 |
| 20,001-25,000 | 37.1 |

Most samples therefore come from the slow midgame rather than startup. The
trace attributed 32.75% of process samples to `Simulation.AdvanceTicks`;
unrelated socket/runtime samples were outside that root. Normalized within
`AdvanceTicks`, the important entries are:

| Measured entry | Share of `AdvanceTicks` |
| --- | ---: |
| Gambit entity evaluation | 37.649% |
| Gambit preconditions | 33.130% |
| Terrain-integrated `TravelReachCost` | 21.221% |
| Minor-actor selected-verb path | 22.504% |
| Low-altitude verbs | 22.046% |
| Culture-locality cell computation | 9.924% |
| Relationship-index insertion | 4.336% |
| Typed-delta resolution | 3.237% |
| Event publication | 2.260% |
| Generators | 2.351% |
| Generic `Int128` fixed-point division | 0.305% |

Program entries can nest and must not be added. In particular,
`TravelReachCost` is inside gambit preconditions. The attribution rejects
fixed-point representation, typed-delta resolution, event publication,
relationship storage, and generator scoring as primary explanations for the
collapse. Undoing the fixed-point, active-only storage, or serial epoch work
would spend substantial effort outside the causal path.

The causal path is:

1. every due gambit tick walks every active entity;
2. every actor walks every authored gambit to find matching actor kinds;
3. every matching kind-targeted gambit walks every active entity of its target
   kind;
4. composed preconditions evaluate every postfix predicate without
   short-circuiting; and
5. `WithinTravelReach` samples every hex on the actor-target line and invokes
   terrain, road, thaw, and movement-cost logic at every sample.

Cheap relationship, culture, attribute, and target-state predicates therefore
do not prevent the terrain integral from running when they have already made a
candidate impossible. Eight Ice Remembers gambits use this predicate. Four of
them also require an existing `at_war_with` or `trade_route` relationship.
Their current target scan examines an upper-bound 11,509 actor/target pairs at
tick 25,000. The world contains only 47 active `at_war_with` and 83 active
`trade_route` relationships. Enumerating those existing relationship indexes
would reduce that relationship-gated candidate source to at most 224
rule/relationship candidates, over 50x fewer, before other kind and strength
filters.

The eight travel-gated gambits together have the following upper-bound
actor-kind by target-kind pair count. This is a structural proxy rather than an
executed-candidate counter because actor cooldown and target filters can reject
work before the precondition body:

| Tick | Travel-gambit pair proxy |
| ---: | ---: |
| 5,000 | 1,177 |
| 10,000 | 5,008 |
| 15,000 | 12,769 |
| 20,000 | 16,502 |
| 25,000 | 19,533 |

That candidate surface grows 16.596x while wall time per tick grows 7.77x.
Active entities grow 3.82x over the same measured endpoints. The combination
of linear entity/cohort work and superlinear gambit pair work explains why the
earlier short gate looked healthy and the midgame did not.

The selector has a second evaluation pass after drawing a weighted gambit, but
it is not the current primary cost: `SelectGambitSlot` accounts for only about
0.49% of `AdvanceTicks` because most actors find no positive candidate. It
should be removed as part of a complete cleanup, not presented as the cause of
the collapse.

Two historical comparisons bound the interpretation:

- The Phase 7 remediation's 70.83 ticks/s median was a 1,000-tick startup
  gate. It established that the fixed-point hot-math regression was repaired;
  it never established midgame throughput.
- The immediate pre-redesign `stages-3x100k-20260724b` seed-9100 run had 475
  active entities at tick 25,000, versus 470 now. Current lifecycle retention
  did not create a 10x population regression. The current kind mix does raise
  the travel-gambit pair proxy from 14,090 to 19,533, a 38.6% increase that
  amplifies the bad algorithm but does not explain it alone.
- The older 2026-05-24 50,000-tick profile completed at 214.42 ticks/s after
  its boundary-cache fix. It used a different seed and substantially older
  content/runtime, so it is not a same-workload benchmark. It is still useful
  CPU-shape evidence: gambit preconditions occupied 2.347% of
  `AdvanceTicks`, versus 33.130% now, and the terrain-integrated travel
  predicate did not yet exist.

`TravelReachCost` was introduced in commit `d2c5e48` before the
pre-parallelization phases. Phase 5 converted its arithmetic to fixed point but
did not create its line-sampling algorithm. The current trace finds the generic
fixed-point operators far below the full travel subtree, so converting this
path back to floating point would preserve the wrong complexity and is
rejected.

Important profile and SQL-derived values above are stored through
`append-stats` on `phase9-midgame-cpu-1x25000-20260725a` and
`phase9-midgame-locality-1x25000-20260725a`.

#### Mechanical Batch 0 result and replacement hot path

The first behavior-preserving Batch 0 implementation is measured by
`phase9-gambit-mechanical-1x25000-20260725a` and the matching sampled trace
`phase9-gambit-mechanical-cpu-1x25000-20260725a`. Both load the same
pre-rendered seed-9100 world. The untraced run completed at `95.089` ticks/s
overall, versus `67.193` before the fix: a `1.42x` whole-run improvement. Its
fixed windows were `313.4`, `149.7`, `69.4`, `75.9`, and `66.2` ticks/s. The
late window is `1.70x` faster than the prior `38.9` ticks/s late window, but
still decays too far for the optimization target. Whole-run rates and fixed
windows are deliberately not divided into each other.

The matching trace moved gambit entity evaluation from `37.649%` to `10.546%`
of `AdvanceTicks`. Its normalized hot entries are now:

| Measured entry | Share of `AdvanceTicks` |
| --- | ---: |
| Minor-actor selected-verb path | 32.155% |
| Low-altitude verbs | 31.576% |
| Culture-locality cell computation | 14.782% |
| Gambit entity evaluation | 10.546% |
| Effects | 7.681% |
| Relationship delta insertion | 6.492% |
| Movement reachability | 6.218% |
| Generators | 3.536% |
| Event publication | 3.170% |

Program entries nest and must not be added. The result validates actor-kind
buckets, actor-gate hoisting, ordered conjunction short-circuiting,
and relationship-index target filtering. It also identifies four further
mechanical fixes:

1. retain every positive weighted gambit candidate from the evaluation pass
   so selection never reevaluates the chosen gambit;
2. resolve a minor actor's source lifecycle need once per actor/tick rather
   than once per candidate cell;
3. invalidate cached culture locality only in the changed cell's radius-two
   dependency neighborhood rather than invalidating the whole map; and
4. replace globally shifting relationship-delta CSR suffixes with bounded
   sorted per-row delta chains.

The authored `WithinTravelReach` question is no longer a credible large
performance lever. The replacement trace attributes only `6.218%` of
`AdvanceTicks` to `MovementProgramRuntime.CanReach`, and that subtree includes
non-gambit callers. Removing every remaining gambit travel predicate therefore
has a theoretical gain below `1.07x` on this trace. The eight authored uses
remain a gameplay-simplicity design decision, not a prerequisite that may be
smuggled into mechanical performance work.

#### Mechanical Batch 1 result and remaining growth cost

The four follow-up fixes above are implemented and measured by
`phase9-serial-storage-mechanical-1x25000-20260725a` and the matching sampled
trace `phase9-serial-storage-mechanical-cpu-1x25000-20260725a`. Both use the
same pre-rendered seed-9100 world and one-tick contract as the preceding runs.

| Version | Overall ticks/s | 1-5k | 5-10k | 10-15k | 15-20k | 20-25k |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Before serial remediation | 67.193 | 302.3 | 134.0 | 65.7 | 44.0 | 38.9 |
| Gambit mechanical batch | 95.089 | 313.4 | 149.7 | 69.4 | 75.9 | 66.2 |
| Storage/locality mechanical batch | 123.487 | 425.6 | 204.0 | 117.5 | 88.1 | 74.8 |

The implemented mechanical subset is therefore `1.84x` faster over the whole
25,000 ticks and `1.92x` faster in the final window. The final measured
implementation still slows by `5.69x` from its first window to its last,
improved from the previous `7.77x` collapse but not eliminated.

The matching trace completed at `112.692` ticks/s overall with windows
`371.3`, `185.5`, `108.3`, `83.3`, and `66.4`. `AdvanceTicks` was `25.13%` of
process samples; normalized within that root, its important entries are:

| Measured entry | Share of `AdvanceTicks` |
| --- | ---: |
| Low-altitude verbs | 34.02% |
| Minor-actor selected-verb path | 33.98% |
| Culture-locality cell computation | 20.53% |
| Effects | 8.67% |
| Movement reachability | 7.24% |
| Typed-delta resolution | 6.17% |
| Gambit entity evaluation | 5.93% |
| Event publication | 3.90% |
| Generators | 0.56% |
| Relationship delta insertion | below 0.20% |

These entries nest and must not be added. In particular, culture locality and
movement are called from the program paths above them. The relationship
insertion subtree fell from `6.492%` to below the trace resolution threshold,
and gambits fell again from `10.546%` after the exact one-pass candidate
completion. Culture locality's larger *share* does not imply an absolute
regression: the untraced workload became faster while surrounding work was
removed.

The unchanged deterministic seed-9100 workload still grows from 123 to 470
active entities and from 87.20 to 475.10 minor-actor events per tick between
the first and final windows. Minor-actor work therefore grows `5.45x`, close
to the corrected runtime's `5.69x` milliseconds-per-tick growth. The remaining
decline is no longer explained by the removed quadratic gambit scan or global
relationship-delta shifting. It is predominantly the cost of executing more
active named entities and more active worker cohorts in a denser map, with
each tick's reduced culture mutations invalidating exactly the locality
descriptors whose inputs changed before the next tick reads them.

This evidence preserves the Phase 9 strategy decision. Stable-ID evaluation
is well balanced for the now-dominant entity and minor-actor work, while typed
cell owners are required to reduce culture deltas without shared mutation.
Subsystem offload remains insufficient: even perfectly removing the complete
`20.53%` culture-locality subtree would cap its gain at `1.26x`, and locality
is a same-tick gameplay input rather than an independently publishable
consumer. Persistent geographic actors remain rejected for the measured load
imbalance and cross-shard traffic. The selected implementation remains the
typed-owner bulk-synchronous hybrid, with evaluation-only BSP as an
intermediate implementation stage rather than the endpoint.

The same trace showed an empty `KernelCommandRing.TryRead` monitor acquisition
at `3.42%` of `AdvanceTicks`. A volatile empty fast path was tested under
`phase9-command-admission-mechanical-1x25000-20260725a`, but that run reached
only `113.451` ticks/s overall and `65.7` in the final window. Host variation
is larger than the proposed micro-optimization, so the slower result is not
attributed to the extra branch. The change was nevertheless reverted because
it demonstrated no end-to-end gain. The validated `123.487`/`74.8` run remains
the corrected serial baseline.

Final verification passed `make ci` on 2026-07-25: the Release analyzer build
had zero warnings or errors, both format gates passed, all 1,047 tests passed,
and the engine domain-contamination check passed.

#### Gameplay-changing performance options not implemented

The corrected profile separates four possible gameplay changes from the
mechanical remediation. None is implemented by this phase. Each needs its own
causal design and behavioral evidence:

| Option | Owning layer | Performance promise | Gameplay decision required |
| --- | --- | --- | --- |
| Consolidate equivalent minor actors into source/purpose cohorts with explicit worker strength | `Engine.Kernel` minor-actor model plus `Engine.Gameplay` authored vocabulary | Highest isolated worker lever. The current selected-verb subtree is 33.98% of `AdvanceTicks`; perfect removal caps at 1.51x, while removing half to three quarters of that work models only 1.20-1.34x before overhead. | Decide whether individual worker position, expiry, stochastic action variance, and per-worker telemetry are valuable. This rewrite is separable from typed-owner BSP and remains optional. |
| Increase minor-actor action cadence and compensate work per action | Ice Remembers minor-actor tuning, with no storage rewrite required | Can reduce the same worker-selection surface roughly in proportion to the cadence change; a twofold reduction in that subtree has a 1.20x Amdahl ceiling. | Accept slower response to newly contested cells, chunkier culture/maintenance changes, and different overshoot/variance even if long-run expected work is preserved. |
| Keep low-altitude intents longer and replan only on explicit invalidators | `Engine.Gameplay` behavior contract and `Engine.Kernel` low-altitude runtime | The low-altitude subtree is 34.02%; reducing selection work by half has a similar 1.20x ceiling. | Define which target, relationship, terrain, culture, or chronicle changes invalidate a committed intent. Reusing an older target is a temporal gameplay change, not an exact cache. |
| Bound active named-entity density by transforming/reusing eligible entities or by budgeting creation against map footprint | Ice Remembers generators and lifecycles | The only gameplay option that can reduce several dominant paths together: low-altitude verbs, effects, gambits, relationship opportunities, and source-emitted minor actors. It therefore has materially more promise than deleting a single predicate, but no honest multiplier exists until a target density is designed and measured. | Decide the desired named cast density, which generator outcomes may transform or awaken existing entities, and whether creation pressure should expand the map, retire old actors, or wait. This changes the emergent world population and must not be smuggled in as engine cleanup. |

Two tempting content changes are explicitly below the significance threshold.
Removing all eight gambit travel predicates has a measured ceiling below
`1.07x`, and relationship drift/culling is only `0.64%` of `AdvanceTicks`
after the storage fix. They may still be simplified for clearer gameplay, but
they are not performance projects.

#### Phase 9 gambit Batch 0 closure

The remaining mechanical Batch 0 work is complete. Terrain reach remains
separately classified as optional gameplay design; the other three original
items now have one end-to-end implementation rather than partial optimized and
fallback paths:

| Original item | Current state | Remaining contract |
| --- | --- | --- |
| Remove terrain-integrated gambit reach end to end | **Reclassified as optional gameplay work.** All eight authored callers and the end-to-end runtime remain unchanged. | Current evidence gives removal of all eight predicates a ceiling below `1.07x`; that does not justify changing authored semantics. Revisit only as an independently motivated gameplay design, not as a parallelization prerequisite. |
| Source relationship-driven targets from relationship indexes | **Complete.** A mandatory relationship descriptor now selects the existing frozen-plus-delta cursor, and the runtime evaluates its endpoints directly in ascending relationship ID order. | The membership stamp and secondary global entity scan are deleted. Preserve direct sparse enumeration when adding workers. |
| Hoist actor/target-only gates and short-circuit conjunctions | **Complete.** Every supported boolean tree compiles into leaf instructions with true/false destinations. Actor values have a separate feasibility branch and direct value-to-leaf index. | The postfix stack, composition opcodes, shape detection, specialized/fallback evaluator split, and blocker plumbing are deleted. Preserve the single representation. |
| Remove the secondary candidate-selection pass | **Complete.** Positive `(gambit, target, weight)` candidates are retained and selection does not reevaluate the chosen gambit. | No remaining work. Preserve this contract while completing the other three items. |

##### Terrain-reach reclassification

Terrain-integrated gambit reach remains intact. Its eight callers, compiled
predicate, query binding, and terrain-line implementation are not part of the
mechanical Batch 0 completion. The earlier replacement table was an unaccepted
semantic-cleanup proposal and has been removed rather than retained as a stale
current design.

The completed mechanical work enumerates mandatory relationship targets
directly and compiles every gambit boolean expression into one bounded
short-circuit branch plan. Authored eligibility, scores, success chances,
mutations, cooldowns, and weighted selection probabilities remain unchanged.
Direct index order may change exact seeded target choice while remaining
deterministic; preserving historical global-entity traversal was not a
simulation contract and would have preserved the scaling defect.

##### Approved mechanical Batch 0 design

**Owning layers.** `Engine.Domain` lowers the authored precondition tree into
the only compiled gambit precondition representation. `Engine.Host` installs
that representation without interpreting it. `Engine.Kernel` executes the
bounded plan and sources candidates from existing indexes.

**Observed failures.**

1. A relationship-constrained gambit walks the correct relationship cursor,
   writes matching endpoints into `GambitVisited`, then scans the complete
   active-kind or active-entity order and probes the stamp. Candidate work
   therefore still scales with population rather than relationship degree.
2. Nested `And`, `Or`, and `Not` expressions fall back to a postfix stack that
   evaluates every leaf. Actor-only leaves are repeated for every target, the
   fallback cannot publish a safe sparse relationship source, and the kernel
   retains shape detection plus three evaluators for one authored contract.

**Causal design.**

- Compile every gambit precondition tree into leaf instructions with explicit
  true and false branch destinations. `And` and `Or` become branch wiring;
  `Not` swaps destinations while compiling. Composition is not represented by
  runtime opcodes.
- Compile actor-scoped leaf values once per actor and gambit into bounded
  scratch storage. An optimistic actor-feasibility plan may reject an actor
  before target enumeration only when no target-dependent branch could make
  the authored expression true.
- Order pure predicates by bounded cost where boolean commutativity permits:
  actor state, target state, relationships, constant-time spatial predicates,
  then terrain-integrated reach. Execute with an iterative program counter,
  no recursion, allocation, or boolean stack.
- Analyze the same authored tree for a positive relationship-to-target
  predicate required on every successful branch. Publish its direction, kind,
  and optional minimum strength with the compiled gambit. If no single
  descriptor is universally required, use the normal target-kind/all-entity
  source; do not add speculative union-index machinery.
- For a published relationship source, enumerate the existing frozen-plus-delta
  cursor directly. That cursor is already merged in ascending relationship ID
  order, and the graph already guarantees one active relationship per
  `(source, target, kind)`, so no membership or deduplication stamp is needed.
  Apply active endpoint, target-kind, provider-selector, and remaining
  precondition checks inline.
- Use ascending relationship ID as the deterministic weighted-candidate order.
  This can change exact seeded target choice relative to the historical
  global-entity traversal, but it does not change the eligible set, score,
  success chance, or weighted probability of any candidate.
- Delete the postfix composition opcodes and application helper, runtime shape
  detection, simple/flat/complex evaluator split, generic fallback,
  `GambitVisited`, membership collection, secondary entity scan, and unused
  blocker-output plumbing. Do not retain a compatibility representation.

**Exact intended files.**

- Compiled representation:
  `src/Engine.Domain/Compilation/DomainCompiler.cs`,
  `src/Engine.Domain/Compilation/DomainCompiler.Gambits.cs`,
  `src/Engine.Domain/Compilation/KernelPrograms/BehaviorProgram.cs`, and
  `src/Engine.Domain/Compilation/KernelPrograms/GambitProgram.cs`.
- Installation:
  `src/Engine.Host/KernelProgramInstaller.cs` and
  `src/Engine.Host/KernelConfigurationContext.cs`.
- Primitive codes and query evaluation:
  `src/Engine.Constants/KernelCodes.cs` and
  `src/Engine.Kernel/KernelQuery.cs`.
- Runtime data, scratch, registration, evaluation, and targeting:
  `src/Engine.Kernel/Programs/Gambits/GambitProgramData.cs`,
  `src/Engine.Kernel/Programs/Gambits/GambitProgramScratch.cs`,
  `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.cs`,
  `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.Evaluation.cs`,
  `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.PreconditionPlans.cs`,
  and
  `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.RelationshipTargeting.cs`.
- Durable contract coverage belongs in the existing domain compiler, gambit
  runtime, and relationship-index test surfaces. Temporary differential
  scaffolding, if used during the rewrite, must be deleted before landing.

**What does not change.** No Ice Remembers definition changes. Terrain reach,
movement, roads, relationship topology, target modes, provider selectors,
scores, success chances, cooldowns, mutations, event ordering, the one-tick
clock, and the selected future parallel ownership model remain unchanged.

**Implementation result.** `Engine.Domain` now emits only the branch-plan
representation. `Engine.Host` installs that representation without a mapping
layer. `Engine.Kernel` uses terminal actor-plan fast paths, direct actor-value
indexes, iterative candidate branches, and direct relationship cursors. The
runtime no longer contains `GambitVisited`, a boolean precondition stack,
composition codes, precondition shape detection, a generic complex fallback,
or a secondary relationship-membership entity scan. Durable tests cover the
nested boolean truth table, actor metadata, mandatory relationship-source
metadata, and runtime exclusion of weak/unrelated relationship endpoints.

**Replacement evidence.** Three registered same-world runs use the existing
Release-generated seed-9100 world and the same 25,000-tick simulation-only
shape as the corrected serial baseline:

- `phase9-gambit-branchplan-1x25000-20260726a` completed at `122.101` ticks/s
  overall with windows `390.9`, `184.5`, `114.9`, `90.5`, and `75.7`. Against
  the prior `123.487`/`74.8` baseline this is `-1.1%` overall and `+1.2%` in
  the final window: effectively neutral at whole-run scale while preserving
  the late-window rate.
- `phase9-gambit-branchplan-1x25000-20260726c`, after replacing the remaining
  actor-leaf discovery scan with direct indexes, completed at `108.283`
  ticks/s with windows `358.0`, `183.7`, `102.8`, `78.7`, and `64.5`.
  Back-to-back identical-shape runs slowed progressively across all windows,
  so this lower wall rate is retained as run-variance evidence rather than
  attributed to an actor lookup that strictly removes work.
- The matching final-code sampled trace
  `phase9-gambit-branchplan-cpu-1x25000-20260726a` puts
  `Simulation.AdvanceTicks` at `32.51%` and gambit `TickEntity` at `0.67%` of
  process samples. Gambits therefore occupy `2.06%` inside `AdvanceTicks`,
  down from the corrected baseline trace's `5.93%`: a `65.2%` reduction in
  the targeted subsystem. The former composed-precondition fallback is absent.

The CPU evidence establishes that Batch 0 reduced, rather than displaced or
increased, gambit work. The host-level repeats do not justify replacing the
stable `123.487`/`74.8` corrected serial reference with either extreme; future
worker-count measurements need paired/repeated runs when host drift is this
large. All material values above are stored on their registry entries through
`append-stats`.

#### Required serial remediation and parallel prerequisites

The owning layers and dispositions are:

1. **Ice Remembers authored rules — keep unchanged.** The eight
   `WithinTravelReach` uses are no longer a meaningful performance gate after
   sparse target filtering and short-circuiting. Any future simplification is
   an independent gameplay design and is not required before parallel work.
2. **Engine.Domain compiled gambit vocabulary — completed branch plan.** The
   authored boolean vocabulary compiles into bounded short-circuit structure.
   There is no second authored representation or compatibility mapper.
3. **Engine.Kernel gambit runtime — completed candidate replacement.**
   Actor-kind indexing, actor-gate hoisting, direct relationship-index
   enumeration, bounded branch execution, and one-pass weighted selection are
   all complete. The order-preserving membership bridge and generic postfix
   fallback are deleted.
4. **Engine.Kernel query/map runtime — keep unchanged.** The authored
   terrain-reach callers remain, so their query and map implementation remain.
   Do not alter them as part of the mechanical candidate-enumeration and
   precondition-plan work.
5. **Parallel execution — keep selected.** The reduced serial model has now
   been reprofiled. The ownership decision is unchanged: balanced entity and
   minor-actor evaluation plus typed entity, relationship, and cell reduction
   remains the only candidate with credible 5-10x headroom. The existing
   Amdahl table remains a sensitivity model rather than an observed promise;
   implementation must report final-window speedup against the corrected
   `74.8` ticks/s baseline.

The implemented mechanical file set is:

- `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.cs`,
  `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.Evaluation.cs`,
  `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.PreconditionPlans.cs`,
  `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.RelationshipTargeting.cs`,
  `src/Engine.Kernel/Programs/Gambits/GambitProgramData.cs`, and
  `src/Engine.Kernel/Programs/Gambits/GambitProgramScratch.cs`;
- `src/Engine.Kernel/Programs/MinorActors/MinorActorProgramRuntime.cs`,
  `src/Engine.Kernel/Programs/MinorActors/MinorActorProgramRuntime.Scoring.cs`,
  and
  `src/Engine.Kernel/Programs/MinorActors/MinorActorProgramRuntime.CultureField.cs`;
- `src/Engine.Kernel/CultureLocality/CultureLocality.cs`,
  `src/Engine.Kernel/Simulation.CultureInfluence.cs`, and the exact mutation
  call sites that publish culture changes;
- `src/Engine.Kernel/RelationshipGraphCursor.cs`,
  `src/Engine.Kernel/WorldState.cs`, and
  `src/Engine.Kernel/WorldState.RelationshipIndexes.cs`; and
- this plan plus the stable system documents whose contracts actually change.

No Ice Remembers authored gambit was changed. A future travel-semantics design
does not receive a file manifest until its gameplay disposition is chosen.

This remediation did not change the one-tick clock, five-tick main-program
cadence, lifecycle curves, generator frequency, active-only storage,
fixed-point boundary, serial epoch/delta model, minor-actor design, or add
parallel workers. Content balance remains a separate diagnosis; it will not be
tuned to make a performance number pass.

The corrected serial gate is a registered same-world 25,000-tick run with
fixed 5,000-tick windows and a matching CPU trace. A 1,000- or 10,000-tick
overall rate cannot close the gate. The accidental quadratic candidate,
global delta-shift, relationship membership scan, and complex-expression
fallback costs are gone. The remaining measured growth follows active
entity/cohort work closely. That is the workload the selected worker-count
sweep must accelerate rather than another reason to defer it indefinitely.

#### Preliminary 10,000-tick CPU shape

The sampled trace attributed 30.75% of process samples to
`Simulation.AdvanceTicks`; unrelated unresolved runtime/socket samples were
outside that root. Normalizing within `AdvanceTicks` gives:

| Measured root | Share of `AdvanceTicks` |
| --- | ---: |
| Local epoch | 69.724% |
| Strategic and publish phases | 25.789% |
| Admission and preparation | 4.455% |
| Delta resolution, nested in the local epoch | 3.642% |
| Event publication, nested across phases | 3.057% |

Program entries identify the ownership work inside those phase roots:

| Measured program entry | Share of `AdvanceTicks` |
| --- | ---: |
| Minor-actor selected-verb path | 29.626% |
| Low-altitude verbs | 25.561% |
| Gambits | 18.537% |
| Effects | 6.992% |
| Generators | 5.659% |
| Culture-locality cell computation | 13.041% |

The program entries can nest and are not summed as independent phase shares.
They show that the remaining cost is entity/cohort evaluation followed by
destination mutation, not a detachable derived builder. Culture-locality
queries occur inside the hot gameplay programs and require the same published
tick state; treating their full 13.041% as independently removable is already
an unrealistically generous subsystem-offload bound.

The trace also leaves a hard warning for any high-speedup claim: command
admission, delta resolution, event sorting/publication, structural graph/index
mutation, and barriers must fit inside the final serial fraction. Evaluation
workers alone cannot do that.

#### Work count, locality, and load

The telemetry run contained:

- 1,382,463 minor-actor events, including 1,310,483 selected actions;
- 50,996 movements;
- 309,787 relationship mutations;
- 1,642 gambit attempts; and
- 700,278 generator target evaluations.

Minor-actor target choice is local: its combined physical/semantic median
distance was 3 cells, p90 was 4, and 95.954% was within radius 4. Movement was
even more local. Relationships and gambits were not:

| Interaction | Within radius 4, physical | Within radius 4, semantic |
| --- | ---: | ---: |
| Minor-actor selected action | 95.954% combined | 95.954% combined |
| Relationship mutation | 22.586% | 30.710% |
| Gambit actor-target pair | 50.426% | 29.598% |

Rectangular geographic partitions were evaluated at 4, 8, and 16 shards over
both planes. At 16 shards, the measured percentage of operations whose source
and destination land in different shards was:

| Interaction | Physical | Semantic |
| --- | ---: | ---: |
| Movement | 11.001% | 13.753% |
| Minor-actor selected action | 29.917% | 33.244% |
| Relationship mutation | 74.347% | 68.631% |
| Gambit actor-target pair | 46.163% | 67.540% |

Local reads inside a halo do not remove those destination writes. A persistent
spatial actor would still need messages or convergence rules for most
relationship work and much of gambit work.

Load is a second independent rejection. Efficiency here is ideal mean load
divided by the busiest shard's load, so `1.0` is perfectly balanced:

| 16-shard workload | Physical | Semantic |
| --- | ---: | ---: |
| Active entities at tick 10,000 | 0.279 | 0.302 |
| Minor-actor work by source cell | 0.275 | 0.226 |

Even with zero messaging and fully parallel work, those efficiencies cap the
dominant spatial portion at only 3.6-4.8 effective workers. Empty or lightly
loaded edge shards make adding more spatial actors counterproductive.

Stable-ID work partitioning is materially better:

| 16-way work partition | Load efficiency |
| --- | ---: |
| Minor-actor events by actor ID | 0.958 |
| Generator targets by entity ID | 0.743 |
| Relationship mutations by simple endpoint hash | 0.591 |

Minor actors and entity evaluation therefore suit ID ranges. Generator targets
have enough items for dynamic bounded chunks. Relationships require canonical
relationship-key ownership or dynamic chunks rather than a naive endpoint hash.
This mixed result is the reason to use typed owners instead of forcing every
store through one geography.

#### Strategy comparison

| Candidate | Measurement result | Disposition |
| --- | --- | --- |
| Stop at serial | The 10,000-tick run is fast, but no current 100,000-tick gate exists and late-window cost is still growing. | Do not claim the long target is met. Defer the long run until a candidate clears short gates. |
| Surviving subsystem offload | The only plausible hot builder, culture locality, is a same-tick query nested in gameplay. Perfectly removing all 13.041% would yield only 1.15x; the remaining independently derived knowledge work is about 0.3%. | Reject as the primary kernel strategy. Keep consumer-side async work outside the kernel. |
| Evaluation-only BSP | Entity/cohort work is large and ID-balanced, but admission, reduction, eventing, graph/index mutation, and strategic application remain serial. | Better than the historical worker experiment, but reject as the final design. It is an implementation stage, not an endpoint. |
| Persistent spatial actors | Minor labor is local, but load efficiency is poor and relationship/gambit traffic is predominantly cross-shard across two different planes. | Reject. Adding global lanes to repair it converges on the typed-owner BSP hybrid anyway. |
| BSP plus geographic actors for all state | Spatial messages and skew remain while barriers are also added. | Reject. It combines both costs without a unified owner. |
| BSP plus typed destination owners, with spatial ownership only for cells | Evaluation follows balanced stable IDs/dynamic chunks; mutation follows the actual entity, relationship, or cell storage owner. | **Select.** This is the highest-promise candidate. |
| Typed-owner BSP plus subsystem offload | UI, telemetry, export, and analysis are already excluded from simulation-only timing; no remaining kernel builder justifies snapshot traffic. | Treat external consumers as orthogonal, not as an additional simulation speedup claim. |

#### Speedup and barrier sensitivity

The reference host has 36 physical cores and 72 hardware threads across two
NUMA nodes. The following is a sensitivity model, not a benchmark. It applies
the measured serial phase shape to three explicit assumptions:

- evaluation-only BSP: 81.5% parallel fraction;
- typed-owner hybrid: 95% parallel fraction after destination reduction and
  structural work move to owners;
- typed-owner stretch: 98% parallel fraction after nearly all admission,
  event, index, and commit work is removed from the coordinator.

Assumed efficiency declines at higher worker counts to include queue, memory,
barrier, and dual-socket costs.

| Model | 4 workers | 8 workers | 16 workers | 32 workers |
| --- | ---: | ---: | ---: | ---: |
| Evaluation-only BSP | 2.43x | 3.28x | 4.02x | 4.52x |
| Typed-owner hybrid, 5% serial | 3.33x | 5.50x | 8.05x | 10.82x |
| Typed-owner stretch, 2% serial | 3.60x | 6.41x | 10.36x | 15.69x |

At 32 workers and 70% parallel efficiency, a 15x result permits only 2.305%
serial work. On all 36 physical cores at 85% efficiency, a 30x result permits
only 0.068% serial work. The current evidence therefore supports:

- the accepted 5-10x target as appropriate for a complete typed-owner
  implementation;
- 8-11x as modeled headroom, subject to direct midgame measurement;
- 15x only as a stretch after the coordinator, event merge, graph/index
  maintenance, and empty admission path together fall below about 2.3%; and
- no credible 30x claim on this host from in-run parallelism alone.

Thirty-fold improvement would require further algorithmic work removal,
coarser independent simulation batching, or different hardware; it is not an
honest acceptance target for this scheduler candidate.

The untraced run took 3.03 ms/tick in its early window and 6.78 ms/tick in its
late window. For a conservative average of 3.2 worker rendezvous per tick, a
50-microsecond rendezvous costs 5.29% of the early window and 2.36% of the late
window. A persistent worker set is mandatory. Per-program jobs or transient
tasks are rejected. The candidate must skip a rendezvous when no strategic
program is due and must measure, not assume, its barrier and queue cost.

#### Selected ownership model

Parallelism follows mutable storage, not one universal shard key:

1. **Evaluation owners** receive stable entity/minor-actor/relationship ranges
   or bounded dynamic chunks from the frozen tick view. They read the published
   view and append primitive intents. They do not mutate live world state.
2. **Entity owners** resolve entity-position, attribute, tag, lifecycle, and
   entity-program-state destinations keyed by occurrence handle.
3. **Relationship owners** resolve relationship strength/state keyed by
   relationship occurrence or canonical relationship key.
4. **Cell owners** resolve terrain, culture, and other map-cell state keyed by
   plane and cell ID. This is the deliberately limited spatial part of the
   hybrid.
5. Cross-store structural effects are routed as typed bounded messages. No
   owner reaches into another owner's array or index.
6. Worker-local event segments merge by the existing deterministic event key
   after state application. Append order and worker count are not observable.
7. Strategic programs use the same evaluate/route/own pattern on ticks where
   they are due. Generators and gambits do not retain a separate direct-mutation
   escape hatch.

Movement and relationship topology expose the important storage problem.
Movement changes an entity position and spatial membership; relationship
creation/end changes a relationship slot and endpoint adjacency. The
implementation must either route each component to its exclusive typed owner
or replace the coupled mutable index with a deterministically rebuilt owner
index. A coordinator that applies all index changes would recreate the
historical serial commit bottleneck and fails this proposal.

#### Implementation batches and exact file manifest

Parallel execution is not authorized by this completed measurement phase. The
midgame diagnosis above adds a required Batch 0 before the selected scheduler
work. If approved, implement in the following order. Each batch must leave
one-worker behavior usable and must not preserve an obsolete serial path behind
a mapper.

**Batch 0: remove accidental strategic candidate work**

- **Complete:** apply the kernel-runtime dispositions in the midgame
  serial-collapse diagnosis without changing authored gameplay.
- **Complete:** route relationship-gated gambits directly from existing
  relationship indexes and delete the membership/global-scan bridge.
- **Complete:** compile actor feasibility and every candidate boolean tree
  into bounded cost-ordered short-circuit branches.
- **Complete:** remove the second weighted-selection evaluation pass.
- **Pending gameplay design:** treat terrain-integrated gambit reach as a
  separate authored design. If that design removes every caller, delete the
  predicate end to end; do not replace it with a compatibility callback or
  cached all-pairs matrix.
- **Complete:** pass a same-world 25,000-tick fixed-window gate and collect a
  replacement sampled-thread trace before adding worker infrastructure.

**Batch A: owner-ready storage and coordinator budget**

- **Complete:** introduce the host-only worker-count option in
  `src/Engine.Kernel/SimulationOptions.cs`; execution topology is not gameplay
  state and must not become a second clock.
- **Complete:** remove `LogicalShardCount` as a persisted pseudo-simulation
  property from
  `src/Engine.Kernel/Persistence/KernelSnapshot.cs` once worker-count
  invariance replaces its serial traversal use.
- **Complete:** measure and remove the empty command-admission cost in
  `src/Engine.Kernel/Simulation.Commands.cs`; do not optimize it from trace
  attribution alone.
- **Complete:** separate coupled entity-position/spatial-membership ownership in
  `src/Engine.Kernel/WorldState.EntityPosition.cs`,
  `src/Engine.Kernel/SpatialLayerIndex.cs`, and
  `src/Engine.Kernel/MapCellRegistry.cs`.
- **Complete:** separate relationship-slot and endpoint-adjacency ownership in
  `src/Engine.Kernel/WorldState.RelationshipIndexes.cs`.
- **Complete:** preserve bounded capacities and occurrence validation. Do not
  add locks
  around existing multi-owner indexes as the final storage design.

Batch A landed without worker threads or gameplay changes. Entity position
remains entity-slot state while cell membership has a bounded dedicated owner.
Relationship slots remain direct relationship state while incident adjacency,
frozen CSR, delta chains, and graph compaction have a dedicated endpoint-index
owner. Hot graph queries read the endpoint-owned arrays directly; the discarded
facade version regressed the 5,000-tick screen and was not retained. Snapshot
format `55` omits both logical shard and worker topology and resolves them from
the loading host. The command ring retains serialized multi-producer admission
but uses the simulation thread as its single lock-free reader.

Registered Release, pre-rendered-world evidence used seed 4200 for 5,000
simulation-only ticks. Baseline runs
`parallel-batch-a-before-1x5000-20260726a` and
`parallel-batch-a-before-repeat-1x5000-20260726a` averaged `15.620 ms/tick`.
Exact-final runs `parallel-batch-a-after-final-1x5000-20260726c` and
`parallel-batch-a-after-final-repeat-1x5000-20260726a` ranged from `15.742` to
`14.956 ms/tick` and averaged `15.349 ms/tick`, a `1.73%` improvement
(`1.018x`). The range shows meaningful host noise, so Batch A is accepted as
no-regression groundwork rather than claimed as a material speedup. The
registered traces `parallel-batch-a-command-before-1x1000-20260726a` and
`parallel-batch-a-command-after-1x1000-20260726a` respectively attributed
`1.68%` of sampled CPU to the locked empty command read and no named samples to
`KernelCommandRing.TryRead` after the single-reader change.

**Batch B: persistent evaluation workers**

- **Complete:** add the fixed-lifetime worker set and owner/chunk descriptors under
  `src/Engine.Kernel/Execution/`.
- **Complete:** change `src/Engine.Kernel/Execution/FrozenTickView.cs` to publish immutable
  range views.
- **Complete:** replace the single shared append cursor in
  `src/Engine.Kernel/Execution/TickDeltaBuffer.cs` with preallocated bounded
  worker append segments or block arenas whose total capacity remains explicit.
  Full-capacity buffer duplication per worker is prohibited.
- **Complete:** add range evaluation to:
  `Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs`,
  `Programs/MinorActors/MinorActorProgramRuntime.cs`,
  `Programs/Effects/EffectsProgramRuntime.cs`, and
  `Programs/Relationships/RelationshipProgramRuntime.cs`.
- **Complete:** keep the current single-thread entry through the same range path at worker
  count one; do not retain separate serial gameplay implementations.

Batch B adds one fixed worker set per simulation. Background workers start
lazily, persist for the host lifetime, and synchronize only at program
boundaries. Every worker receives an immutable contiguous range from the frozen
entity or relationship view plus worker-local scratch and execution context.
Effects and relationship maintenance may evaluate concurrent ranges.
Low-altitude verbs may do so when trace capture is disabled; trace event
partitioning remains Batch C. Minor actors use the same range contract at worker
count one, but concurrent actor creation, active/cell indexes, encounters, and
event mutation also remain Batch C. The coordinator still performs delta
reduction and all cross-owner world mutation.

Delta append arenas reserve 64-record blocks without contending on a shared
cursor per record. They retain the original logical capacity and add only
bounded physical tail room for partially filled concurrent blocks; compaction
rejects actual output above the declared capacity. Worker count one uses direct
worker-zero scratch, context, range dispatch, and delta append storage inside
the same infrastructure. This fast lane is not a separate gameplay evaluator.

The first Batch B timing pair exposed exactly why the worker-one gate is
mandatory. Registered runs `parallel-batch-b-after-1x5000-20260726a` and
`parallel-batch-b-after-repeat-1x5000-20260726a` averaged
`17.225 ms/tick`, a `12.2%` regression from the Batch A exact-final mean,
because worker one still paid thread-local and block-arena bookkeeping in hot
paths. Those runs are rejected evidence. After direct worker-zero plumbing,
registered exact-final runs
`parallel-batch-b-after-fastpath-1x5000-20260726a` and
`parallel-batch-b-after-fastpath-repeat-1x5000-20260726a` measured `14.887`
and `15.110 ms/tick`, averaging `14.999 ms/tick`. That is a `2.28%`
improvement (`1.023x`) over Batch A's `15.349 ms/tick` mean. Temporary
four-worker behavioral scaffolds passed for effects/relationships and
low-altitude movement and were removed; Batch E still owns the durable
worker-count invariance matrix and public host/tool wiring.

**Batch C: typed-owner reduction and publication**

- **Complete:** replace the serial shard loops in
  `src/Engine.Kernel/Execution/TickDeltaBuffer.cs` with destination-key routing
  and disjoint owner reductions.
- **Complete:** drive the barriers and phase visibility from
  `src/Engine.Kernel/Execution/Simulation.Execution.cs`.
- **Complete:** partition event appends and deterministically merge them in
  `src/Engine.Kernel/Execution/KernelEventPublisher.cs`.
- **Complete:** route entity, relationship, and cell mutations through the owner-ready
  `WorldState` and index files from Batch A. No global apply queue is accepted.

Batch C generalizes the persistent evaluation worker set into one kernel worker
set that also executes owner ranges. Attribute, tag, movement-coordinate, and
influence-credit groups route to entity owners; strength groups route to
relationship owners; terrain/coverage values and all cultures at a cell route
to cell owners. The coordinator retains only explicit structural stages:
spatial detach/attach, coverage reverse-index and topology reconciliation,
culture-row and locality reconciliation, relationship culling, and lifecycle
retirement. Event writers use bounded worker arenas and publish a deterministic
sorted merge after the phase barrier.

Worker count one uses the same typed records and owner-ready storage APIs with
direct coordinator execution. It does not retain the removed gameplay mutators
as adapters. A first implementation nevertheless regressed badly because the
owner preparation/reconciliation passes and multi-worker revision publication
leaked into topology one. The direct one-worker orchestration removed the extra
movement, cell, and culture passes. Direct revision reads at worker one removed
the remaining read-heavy locality penalty while multi-worker revisions remain
atomically published.

Registered seed-4200, pre-rendered-world, 5,000-tick measurements preserve the
failed evidence. The initial pair
`parallel-batch-c-after-owner-reduction-1x5000-20260726a` and
`parallel-batch-c-after-owner-reduction-repeat-1x5000-20260726a` averaged
`18.892 ms/tick`, a rejected `25.96%` regression from Batch B. The first clean
exact-final run,
`parallel-batch-c-revision-read-fastpath-1x5000-20260726a`, measured
`15.531 ms/tick`. That is within the earlier Batch A host spread but is `3.55%`
slower than Batch B's `14.999 ms/tick` paired mean, so it is not evidence that
the one-worker performance gate has passed. Two attempted repeats are registered
as rejected because competing Roslyn/MSBuild work appeared after launch.

The internal four-worker screen
`parallel-batch-c-owner-reduction-4w-1x5000-20260726a` measured
`23.908 ms/tick` and is rejected. Fine-grained owner dispatch, multi-worker
arena bookkeeping, atomic revision publication, and barrier frequency still
outweigh the available concurrent work. Batch C's ownership conversion is
implemented and behavior-validated; Batch E must not expose worker selection
until it has coalesced those regions and passed both the worker-one regression
gate and the required end-to-end scaling gates.

**Batch D: strategic ownership**

- **Complete:** convert gambit evaluation/application in
  `Programs/Gambits/GambitProgramRuntime.cs` and its execution partials to
  bounded typed intents.
- **Complete:** convert generator scoring, placement, and structural application in
  `Programs/Generators/GeneratorProgramRuntime*.cs` to the same ownership
  boundary.
- **Complete:** allocate structural IDs in deterministic owner blocks, then route graph,
  position, relationship, and event records to their typed owners.
- **Complete:** apply the same pattern to any other measured strategic program that still
  mutates shared state directly. Do not leave a direct-mutation compatibility
  lane merely because it is less frequent.

Batch D gives each converted strategic program a program-scoped frozen intent
epoch while preserving strategic registration order. Gambit actors evaluate in
frozen entity ranges with worker-local scratch. Generator definitions score in
disjoint worker ranges; selected fires plan produced entities, placement,
relationships, and effects without live world mutation.

Strategic application uses bounded typed arenas for absolute and additive
entity state, relationship state, movement, cell terrain/coverage,
travel structures, forge requests, lifecycle/status changes, districts, and
structural creation. The coordinator reserves entity and relationship slots in
deterministic blocks and prepares cells. Owners write disjoint entity,
relationship, cell, travel-structure, provenance, and primitive event records.
The coordinator then reconciles active vectors, kind/culture indexes, spatial
membership, endpoint adjacency, coverage reverse indexes, map topology,
district pools, and lifecycle retirement.

District reawakening no longer bypasses the structural path: consuming the
district produces reserved entity and relationship records that enter the same
owner stages as generator and ordinary gambit creation. Generator and gambit
runtime entrypoints require their strategic intent epoch, so no less-frequent
direct application path remains. The full repository `make test` pass is green.

**Batch E: host/tool wiring and evidence**

- **Complete:** coalesce evaluation and owner-reduction regions so configured multi-worker
  execution no longer pays one dispatch/barrier per small destination family.
  Reprofile partition arenas, atomic revision publication, event merge, and
  coordinator reconciliation before exposing worker selection.
- **Complete:** wire worker count through
  `src/Engine.Host/KernelStartRequest.cs`,
  `src/Engine.Host/KernelRuntimeFactory.cs`,
  `tools/MonteCarlo/MonteCarloOptions.cs`, and
  `tools/MonteCarlo/MonteCarloCli.cs`.
- **Complete:** add canonical Makefile arguments for registered worker-count
  comparisons.
- **Complete:** extend the durable epoch invariance tests in
  `tests/Engine.Kernel.Tests/SerialEpochInvarianceTests.cs` to compare worker
  counts rather than adding source-text or implementation-shape tests.
- **Complete:** update `docs/architecture.md`,
  `docs/kernel/simulation-kernel-discipline.md`, and
  `docs/methodology/performance-testing.md` with the implemented topology and
  measured evidence.

Batch E is mechanically complete, but its performance candidate is rejected.
Configured multi-worker local evaluation uses one dispatch for the due range
programs. Owner application uses five dependency-preserving batches—entity
creation, spatial state, primary state, dependent secondary state, and final
relationship/tag/entity state—instead of one dispatch per destination family.
Culture revisions and aggregate physical culture mass reduce through
worker-local counters and publish on the coordinator rather than contending on
shared atomics. Worker one retains direct registration-order dispatch through
the same range and owner implementations because cross-program coalescing made
the 5,000-tick reference workload slower and changed the timing of
`CompleteRanges`.

`KernelStartRequest`, the host factory, Monte Carlo options/CLI, and the
canonical Makefile targets now carry an explicit positive worker count. One
remains the default. `WorkerCountsPublishIdenticalStateAndEvents` compares full
kernel snapshots, including primitive events, at 1, 2, 4, 8, and 16 workers.

The first registered 1,000-tick pre-rendered-world screen measured:

- `parallel-batch-e-screen-1w-1x1000-20260726a`: `7.849 ms/tick`;
- `parallel-batch-e-screen-2w-1x1000-20260726a`: `11.555 ms/tick`,
  `0.679x` worker-one throughput;
- `parallel-batch-e-screen-4w-1x1000-20260726a`: `13.105 ms/tick`,
  `0.599x`; and
- `parallel-batch-e-screen-8w-1x1000-20260726a`: `14.744 ms/tick`,
  `0.532x`.

Owner batching and worker-local revision/mass publication improved the
eight-worker result to `13.071 ms/tick` in
`parallel-batch-e-atomic-free-screen-8w-1x1000-20260726a`, but that is still
only about `0.60x` the initial worker-one throughput. The final direct
worker-one short screen,
`parallel-batch-e-worker1-final-screen-1x1000-20260726a`, measured
`8.304 ms/tick`; the difference is inside neither an accepted paired 5,000-tick
gate nor a basis for claiming a regression fix. The attempted multi-worker
arena-padding experiment measured `13.783 ms/tick` and was removed rather than
retained as speculative complexity.

The post-coalescing eight-worker trace
`parallel-batch-e-coalesced-profile-8w-1x1000-20260726a` attributed only
`0.70%` inclusive sampled thread time to owner batches, `0.81%` to delta
resolution, `0.39%` to deterministic sorting, and `1.05%` to event publication.
Worker sleeping/waking and coordinator joins dominated instead:
`KernelWorker.Run` was `41.66%` exclusive, the two `Monitor.Wait` forms were
`10.14%` and `5.28%` exclusive, `WaitForWorkers` was `3.95%` inclusive, and
`KernelWorker.Start` was `1.17%` exclusive. Observed eight-worker RSS was about
`460 MB`, far below the 8 GB ceiling.

The initial coalesced worker-one 5,000-tick pair averaged `15.260 ms/tick`,
`13.8%` slower than the Batch D `13.409 ms/tick` mean. Restoring direct
worker-one registration-order dispatch improved the next run to
`14.770 ms/tick`, but that single run still missed the prior mean by `10.1%`
under a busier host. The worker-one acceptance gate therefore remains failed,
not waived or reclassified as noise.

Per the stop rule, no 16-worker, 10,000-tick, 25,000-tick, or 100,000-tick
escalation was run. The typed-owner/hybrid groundwork remains useful and
deterministic, but the current fine-grained synchronous worker scheduler is not
an accepted performance strategy. The next performance design must reopen
dispatch granularity and the measured serial/coordinator fraction before adding
workers.

After Batch 0, the parallel scheduler manifest deliberately excludes further
Ice Remembers tuning, projection semantics, public tick units, and the
one-tick/five-tick cadence contract. Phase 9 found both an accidental serial
candidate algorithm and a separate execution/storage ownership problem.
Correcting the former does not reverse the selected solution for the latter.

#### Candidate acceptance and stop rules

Use the pre-rendered world for every short comparison. First pass the corrected
serial 25,000-tick fixed-window gate. Then run identical current-source Release
workloads at worker counts 1, 2, 4, 8, and 16; try 32 only when 16 workers still
add useful throughput.

- The new one-worker path must match or beat the current serial path. A slower
  one-worker scheduler is not accepted as harmless overhead.
- Same seed and commands must produce identical published state at worker
  counts 1, 2, 4, 8, and 16. Worker completion order must not affect IDs,
  reductions, events, or state.
- Eight workers must reach at least 4.5x and 16 workers at least 7x end to end
  on the early 10,000-tick simulation-only screen. Missing either threshold
  reopens the measured serial fraction before adding more workers. Passing it
  does not close the performance gate.
- Barrier, queue, route, merge, and coordinator time must be reported
  separately. No worker-loop-only speedup is accepted.
- Peak application working set stays within 8 GB. Worker-local buffers may not
  multiply global capacities by worker count.
- The short behavior gate checks active entities, map use, minor labor,
  relationship churn, gambit/generator activity, lifecycle churn, dropped
  records, and capacity failures.
- After the early worker screen, repeat the registered 25,000-tick
  fixed-window comparison before the three-seed 100,000-tick final evidence.
  Stop escalation immediately on a failed late-window performance,
  determinism, memory, or broad-shape gate.

Final `make ci` passed all 1,047 fast tests on 2026-07-25 with zero build
warnings or errors and a passing domain-boundary check.

### Phase 10: Unify Tick Cadence And Rebuild Minor Actors As Persistent Tasks

Status: implemented and validated on 2026-07-26.

This phase is preparatory work before the Phase 9 typed-owner parallel
scheduler. It deliberately removes work from the dominant minor-actor path and
makes each surviving actor a durable, visually legible unit of local causality.
It does not reopen the rejected persistent-geographic-shard scheduler: minor
actors are simulation objects partitionable by stable actor ID, not owners of
arbitrary world state.

#### Owning layers

- `Engine.Kernel` owns the one-tick scheduler, persistent actor slots,
  generation-safe actor identity, bounded assignment/encounter state, movement,
  work intents, deterministic interaction resolution, and snapshots.
- `Engine.Domain` and `Engine.Gameplay` own the authored vocabulary compiled
  into front, road, and campaign task rules. Emission cadence, worker TTL, and
  duplicate-worker caps do not survive as compatibility fields.
- `IceRemembers` owns which named sources can sustain each task, task strength,
  reach/stride, commitment, replanning, and authored chaotic interactions.
- `Engine.Eventing` publishes primitive actor-created, assigned, moved, worked,
  encountered, and retired facts.
- `Engine.Projection` and the UI turn those facts into persistent map motion
  and activity. They do not decide actor behavior.
- `Engine.Observability` and Monte Carlo analysis measure actor population,
  assignment churn, work mix, travel, encounters, culture shape, and cost.

#### Observed failure

Minor actors are the only program family that receives an opportunity on every
current tick. Ordinary movement, verbs, relationships, effects, gambits,
chronicles, and generators run every five ticks. Four ticks out of five
therefore exist primarily to service short-lived workers, even though the
public simulation model has one clock.

The current actor runtime compounds that scheduling tax:

- named sources repeatedly emit expiring workers;
- stages manufacture multiple near-identical workers through a worker-count
  bonus;
- each due worker repeatedly scores local verb/cell combinations;
- 81.44% of measured selected work reinforces established home mass already
  held by stage ambient emission and districts;
- lifecycle support produces only 0.023% of selected actor delta despite
  consuming 2.86% of selections; and
- projection receives transient action events rather than a persistent moving
  front, road crew, or campaign column.

The final-code sampled trace
`phase9-gambit-branchplan-cpu-1x25000-20260726a` attributes 33.99% of
`AdvanceTicks` to minor-actor selected-verb work. Perfectly deleting that
subtree would cap equivalent-work whole-simulation speedup at about `1.52x`.
This phase therefore does not promise a `3x` CPU gain by itself. Its larger
user-facing benefit is that one public tick advances the whole simulation
instead of spending four intervening ticks on worker-only activity.

#### Causal hypothesis

If one tick represents the current complete main-program opportunity, ordinary
programs and minor actors can all run at cadence one without substeps or a
second clock. Calendar and slower-system durations can be reauthored directly
in the new tick unit.

If minor actors persist while a real front, road task, or campaign exists,
assignment selection moves from every actor action to explicit creation or
replanning boundaries. Replacing duplicate stage workers with actor strength
reduces active actor count without making culture act autonomously. Removing
routine core reinforcement lets actors concentrate on visible border, route,
and campaign changes while stage emission and districts retain their already
approved interior-holding role.

The resulting actor loop should scale with active meaningful tasks rather than
source count multiplied by stage bonus multiplied by emission/expiry churn.
Its stable actor IDs and destination-keyed culture/road effects remain suitable
for the Phase 9 evaluation-worker and cell-owner topology.

#### Dispositions

| Existing portion | Disposition | Reason |
| --- | --- | --- |
| One public tick and phase revisions | Keep | They correctly prevent hidden clocks and preserve published deterministic boundaries. |
| Five-tick main-program cadence | Replace | It exists to create worker-only opportunities and makes the public tick a poor unit of perceived progress. |
| Fixed-point fields, typed deltas, keyed randomness, occurrence handles | Keep | Their contracts are sound and are prerequisites for deterministic owner reduction. |
| Minor-actor direct reusable slots and dense active iteration | Extend | The storage shape is appropriate, but actor occurrence identity and persistent assignment fields are missing. |
| Emission deadlines, TTL, per-source duplicate count, action due heap | Remove | Persistent source/task actors do not need recurring emission or expiry churn. |
| Stage worker bonus | Correct | Interpret it as task strength/work capacity, not automatic duplicate cohort count. Additional detachments require a separate future authored property. |
| Culture core-reinforcement treadmill | Remove | Stage ambient emission and districts now own interior holding. |
| Cultural border/hole/contest work | Replace internally | Preserve it as discrete moving cultural-front actors with persistent direction and assignment. |
| Road maintenance | Extend | Preserve discrete crews, but bind them to actual used/degrading route work instead of repeated generic local selection. |
| Campaign columns | Extend | Preserve one discrete column per active campaign relationship and give opposing/local actors explicit encounters. |
| Default lifecycle maintenance | Remove | Its measured effect is negligible. A future relief column must be an explicit role with visible consequences. |
| Per-action weighted random cell scoring | Replace | Replan against bounded opportunities only when a task starts, invalidates, or reaches a commitment deadline. |
| Minor-actor action telemetry | Replace | Publish persistent lifecycle, movement, work, and encounter facts rather than reconstructing an actor from unrelated action motes. |

#### Unified tick contract

One Phase 10 tick is the old complete main-program opportunity, equivalent to
five Phase 8/9 ticks for calendar and throughput comparisons.

- Movement, low-altitude verbs, relationships, effects, gambits, chronicles,
  generators, and minor actors register at cadence `1`.
- Culture decay changes from `25` old ticks to `5` new ticks.
- Culture slots and knowledge change from `50` old ticks to `10` new ticks.
- World beats and map frontier change from `125` old ticks to `25` new ticks.
- The canonical calendar changes from `250` ticks/year to `50` ticks/year.
- Lifecycle targets and every authored absolute-calendar tick value are divided
  by five at their owning definition.
- Values authored in main-program opportunities keep their numeric value.
  `ScaleMainProgramDuration` and `MainProgramCadenceTicks` are deleted; there
  is no runtime compatibility mapper.
- Minor actors receive one bounded movement/interaction/work decision per
  tick. A configured macro stride may publish one endpoint several cells away,
  but it does not execute hidden intermediate time steps.

Raw new ticks/second must not be compared to Phase 9 raw ticks/second. Evidence
reports new ticks/second, world-years/second, and old-tick-equivalents/second.
Equivalent-work regression is not accepted merely because the public world
appears to advance faster.

#### Persistent actor state

Each live minor actor is an unnamed task cohort with:

- a generation-safe actor occurrence handle;
- a generation-safe source entity occurrence handle;
- culture, plane, and purpose (`CulturalFront`, `RoadCrew`, or
  `CampaignColumn`);
- current and previous cell;
- assignment kind and target cell;
- strength and remaining per-tick work charge;
- commitment and next-replan tick;
- optional active campaign relationship handle or road-segment binding; and
- a bounded posture code such as advancing, working, blocked, withdrawing, or
  disrupted.

There is normally at most one actor per active source/purpose rule. A stage
worker bonus increases its strength and effective work charge. It does not
create additional actors. Actor slots remain fixed capacity, dense, reusable,
and cleared on retirement. An actor survives until its source or bound task
becomes invalid; completing one assignment returns it to bounded replanning
rather than expiring and requiring re-emission.

#### Role behavior

**Cultural fronts**

- Exist only when a reachable border, hole, bridge, or contested opportunity
  exists on the rule's labor plane.
- Carry culture from their named source; culture never expands without a
  discrete actor or another already-authored named-entity effect.
- Move toward a persistent assignment, repair a local hole, bridge nearby
  supported mass, extend a supported border, or contest foreign mass.
- Do not repeatedly reinforce established interior mass. Ambient stage
  emission and districts own that function.
- Replan when the target becomes invalid, the local culture band materially
  changes, the actor is displaced, or its bounded commitment expires.

**Road crews**

- Bind to current travel demand and a degrading road segment or bounded route
  span.
- Travel to the task and apply road work once per tick.
- May be delayed, diverted, escorted, or disrupted by local actor encounters.
- Return to replanning after the segment is healthy or route demand disappears.

**Campaign columns**

- Bind one-to-one with an active authored campaign relationship such as
  `wages_campaign_against`.
- Advance from friendly support toward the target frontier, project culture,
  and reduce hostile local control according to authored work strength.
- Withdraw or retire when the relationship ends or either endpoint is no
  longer active.
- Interact with opposing columns, cultural fronts, and road crews locally
  rather than through a global all-pairs pass.

#### Tick execution and chaos

For every active actor in stable occurrence order:

1. validate its source, binding, and assignment against the published tick
   view;
2. replan only if invalid, unassigned, displaced, or due;
3. publish one macro movement endpoint toward the assignment;
4. discover encounters through a direct actor-by-cell index using only
   co-location and adjacent cells;
5. resolve authored local interactions through keyed randomness and bounded
   typed intents; and
6. spend the remaining work charge once on its assigned cell.

Authored chaotic interactions may include:

- allied temporary strength joins;
- a high-strength front splitting its work across two adjacent opportunities
  without creating a permanent extra actor;
- hostile front or campaign clashes that damage strength or displace one side;
- campaign disruption or diversion of a road crew;
- road-crew support or escort of a friendly column;
- overextension causing a retreat/replan posture;
- competing friendly tasks redirecting the lower-priority actor; and
- a rare opportunistic adjacent retarget.

Eligibility is local and primitive: cell/adjacency, purpose, culture, posture,
local band, and optional campaign/road binding. Randomness is keyed by tick,
actor occurrence handles, interaction code, and attempt so traversal order and
future worker count do not change outcomes. There is no actor all-pairs scan,
general graph identity, or hidden population model.

#### Presentation contract

Primitive events cover actor creation, assignment, movement, work, encounter,
posture change, and retirement. Projection stores active actor views keyed by
actor occurrence handle and interpolates from previous to current cell.
Cultural fronts, road crews, and campaign columns have distinct map
presentation; strength changes density/intensity without multiplying actor
objects. UI interpolation is visual only and never feeds simulation state.

#### Implementation batches

**Batch A: tick and calendar foundation**

- Remove `MainProgramCadenceTicks` and `ScaleMainProgramDuration`.
- Register ordinary programs at cadence one and normalize the slower scheduler
  cadences.
- Reauthor canonical calendar, lifecycle targets, absolute cooldowns/windows,
  minor-actor rule timing, persistence, host tuning, and tests in the new unit.
- Keep phase ownership and `TickCompleted`; do not add a replacement clock or
  compatibility view.

Intended files:

- `src/Engine.Kernel/Simulation.cs`,
  `src/Engine.Kernel/SimulationOptions.cs`, and
  `src/Engine.Kernel/Execution/*`;
- affected cadence/duration owners under
  `src/Engine.Kernel/Programs/{LowAltitudeVerbs,Movement,Gambits,Generators,Chronicles,CultureField,CultureSlots,Knowledge,WorldBeats,MapFrontier,Effects}`;
- `src/Engine.Kernel/Persistence/KernelSnapshot.cs`;
- host tuning/configuration, Monte Carlo progress/calendar reporting, Ice
  Remembers lifecycle and duration definitions, and durable cadence/lifecycle
  contract tests.

**Batch B: persistent actor vocabulary and storage**

- Replace emitter/TTL/cap fields with task strength, stride, commitment,
  replan, and interaction fields through authoring, compiler, installer, kernel
  data, and snapshots.
- Add actor occurrence generations, assignment/binding/posture state, and the
  actor-by-cell index.
- Replace stage duplicate-worker bonuses with strength contribution.
- Remove the due heap if all actors act once per tick; retain only direct
  replan deadlines.

Intended files:

- `src/Engine.Domain/Config/DomainConfig.cs`;
- `src/Engine.Domain/Compilation/DomainCompiler.Sets.cs` and
  `src/Engine.Domain/Compilation/KernelPrograms/MinorActorProgram.cs`;
- `src/Engine.Gameplay` minor-actor vocabulary;
- `src/Engine.Host/KernelProgramInstaller*.cs`;
- `src/Engine.Kernel/Programs/MinorActors/*`;
- `src/Engine.Kernel/WorldState.Stages.cs`; and
- actor compilation, capacity, stale-handle, churn, and snapshot contract
  tests.

**Batch C: assignments, roles, and encounters**

- Implement bounded opportunity discovery and persistent replanning.
- Implement cultural-front, road-crew, and campaign-column movement/work.
- Remove core reinforcement and default lifecycle maintenance end to end.
- Implement the local actor-cell encounter pass and authored interaction table.
- Preserve culture, road, campaign, and source ownership through typed deltas.

Intended files:

- `src/Engine.Kernel/Programs/MinorActors/*`;
- `src/IceRemembers/IceRemembersMinorActors.cs`;
- campaign relationship and road-demand query/index owners;
- culture locality and movement/path helpers used by bounded assignment; and
- durable behavior tests for source ownership, task persistence, role work,
  local encounters, and deterministic replanning.

**Batch D: event, projection, persistence, and stable documentation**

- Publish and project persistent actor lifecycle/motion/work/encounter facts.
- Replace transient-only minor-actor map motes with active role projections.
- Replace snapshot formats directly; do not read old actor or cadence layouts.
- Update stable architecture, game-system, minor-actor, entity-stage, and
  simulation-principle documents to the implemented model.

Intended files:

- `src/Engine.Eventing/KernelEvent*.cs`;
- `src/Engine.Kernel/Simulation.Events.cs`;
- `src/Engine.Projection/MinorActorFlowProjection.cs`,
  `src/Engine.Projection/UiProjectionState.cs`, and relevant map activity
  layers;
- `src/Engine.Observability/TelemetryEventProjection*.cs`;
- kernel and simulation snapshot files;
- `docs/architecture.md`, `docs/game-systems.md`,
  `docs/systems/21-minor-actors.md`, `docs/design/entity-stages.md`, and
  `docs/systems/simulation-principles.md`.

**Batch E: verification and performance gate**

- Run the durable repository behavior/determinism suite and final `make ci`.
- Use one registered pre-rendered-world Release smoke to validate live
  simulation shape without paying world-generation cost.
- Compare a same-world 25,000-old-tick-equivalent workload with the corrected
  Phase 9 reference. Report new ticks, old-tick equivalents, and world-years.
- Append actor count, replan rate, assignment mix, encounter rate, work mix,
  culture shape, road work, campaign work, phase time, and memory findings to
  the MC registry.
- Reprofile before starting Phase 9 Batch A. Update the typed-owner work
  partition from the new active actor population rather than assuming the old
  0.985 actor-ID balance still applies.

#### Implementation result

The feature branch `feature/unified-tick-minor-actors` implements Batches A-D
as a direct replacement:

- ordinary programs now receive one opportunity per tick, slower cadences and
  the 50-tick/year calendar are reauthored in the new unit, and the main
  cadence field/scaler/snapshot value are gone;
- full-world and Monte Carlo horizons that represented elapsed simulation time
  use one-fifth their former count, while kernel tests that already forced
  cadence one retain their behavior-opportunity counts;
- minor-actor emitter rules, TTL, caps, verb registration, due heap,
  per-source count rows, lifecycle-maintenance work, core reinforcement, and
  old snapshot reader are removed;
- persistent actor state uses generation-safe actor/source/relationship
  occurrences, dense active slots, direct source/rule and campaign indexes,
  direct actor-cell links, persistent assignments, and strength rather than
  duplicate workers;
- cultural fronts, road crews, campaign columns, macro movement, bounded
  replanning, and same/adjacent-cell encounters are live;
- primitive creation, assignment, movement, work, encounter, and retirement
  events replace `MinorActorVerbExecuted`; projection retains active role
  actors plus bounded work history; telemetry exports separate lifecycle,
  work, and encounter tables; and
- stable tick, architecture, game-system, entity-stage, simulation-principle,
  and minor-actor documents describe the implemented model.

The final source state passed `make ci`: warning-as-error Release build,
whitespace/style verification, all 1,031 tests, and the engine domain-boundary
check completed with zero failures.

#### Registered Phase 10 evidence

All evidence below loads the existing Release-generated seed-9100 world rather
than regenerating geography.

`phase10-persistent-actors-smoke-1x1000-20260726a`:

- 1,000 new ticks = 5,000 old-tick equivalents;
- `107.146` new ticks/s = `535.729` old-tick equivalents/s; and
- `1.259x` versus the Phase 9 first-window `425.6` old ticks/s. This is the
  deliberately short regression gate, not the final speedup claim.

`phase10-persistent-actors-midgame-1x5000-20260726a`:

- 5,000 new ticks = 25,000 old-tick equivalents;
- `49.629` new ticks/s = `248.144` old-tick equivalents/s overall;
- `2.009x` versus the corrected Phase 9 whole-run `123.487` old ticks/s;
- the final window reached `31.7` new ticks/s = `158.5` old-tick
  equivalents/s, `2.119x` versus Phase 9's `74.8`; and
- window throughput still declines sharply (`102.1`, `86.4`, `46.2`, `38.2`,
  `31.7` new ticks/s). The unified actor model improves but does not remove
  the previously diagnosed growth-sensitive costs outside the actor runtime.

`phase10-persistent-actors-cpu-1x1000-20260726a`:

- the profiled run sustained `101.637` new ticks/s;
- `TickActiveActors` occupied `1.75%` of the full trace while
  `AdvanceTicks` occupied `22.43%`, approximately `7.8%` of sampled advance
  time; and
- deleting all remaining actor execution would therefore have only about a
  `1.085x` upper bound on the new kernel. Reaching `3x` versus Phase 9 cannot
  come from more minor-actor semantic removal; low-altitude evaluation,
  effects/locality, event publication, generators, and later-world growth own
  the remaining work.

`phase10-persistent-actors-behavior-1x200-20260726a`:

- 57 actors were created and remained active: 53 cultural fronts and four road
  crews; no campaign relationship existed in this short sample;
- 960 assignments, 276 movement facts, 9,969 work facts, and 359 encounters
  were published;
- encounters comprised 283 friendly supports and 76 hostile clashes;
- work covered supported extensions, bridge gaps, contests, and roads; and
- `map_degenerate=0`. The one-column-per-active-campaign contract is covered
  by the kernel campaign behavior test.

Phase 10 therefore clears the user's no-less-than-`2x` equivalent-work floor,
but it is not represented as a `3x` actor optimization. The next material
speedup belongs to the already-selected broader typed-owner parallel boundary
or to independently evidenced non-actor serial reductions.

#### Acceptance and stop rules

- One `AdvanceTicks(1)` call advances exactly one public tick and no subsystem
  executes an internal time substep.
- Ordinary programs and minor actors receive at most one opportunity per tick;
  slower programs use explicit integer tick cadences.
- There is no main-cadence tuning field, duration scaling helper, old/new tick
  adapter, actor emission deadline, actor TTL, or compatibility snapshot reader.
- Culture expansion remains traceable to a named entity, persistent minor
  actor, district, or another explicit authored source; it does not become an
  autonomous field process.
- Fronts, road crews, and campaign columns remain discrete, spatial, persistent,
  and visually distinguishable.
- Routine core reinforcement and default lifecycle labor are absent.
- Same seed and commands produce identical published state under reversed
  evaluation and supported logical-shard counts.
- Actor work has no global all-pairs pass and no hot-path allocation.
- Equivalent-work one-worker performance may not regress from the corrected
  Phase 9 baseline. A perceived calendar acceleration does not excuse a CPU
  regression.
- The actor rewrite is not claimed as a `3x` CPU optimization. Its measured
  whole-simulation gain and the remaining typed-owner headroom are reported
  honestly before parallel implementation resumes.
- Application working set remains within the accepted 8 GB ceiling.

#### Explicit non-goals

- no OS worker threads in this phase;
- no aggregate or autonomous culture simulation;
- no graph-entity promotion for minor actors;
- no generic economy, supply, or population model;
- no restoration of pulses, substeps, steps, bundled advances, or a second
  calendar clock;
- no legacy snapshot, cadence, or minor-actor compatibility mapper;
- no long 100,000-old-tick-equivalent run before the short and midgame gates
  pass; and
- no authored terrain-reach or unrelated semantic cleanup.

## Strategy Consequences After The Measurement

### Subsystem offload

Subsystem offload is rejected as the primary kernel strategy. Optional
projection, telemetry preparation, UI frame construction, and batch analysis
remain consumer work. A future expensive derived builder may still be offloaded
if it independently passes the snapshot/publication cost gate, but it is not
part of the selected parallel candidate.

### Bulk-synchronous phase graph

BSP is selected, but only with typed destination ownership rather than the
historical evaluation-worker/serial-commit split. The serial epoch/delta phase
already establishes:

- fixed-point reductions;
- destination ownership;
- conflict resolution;
- keyed randomness;
- structural allocation;
- event ordering;
- phase visibility.

Adding workers is a scheduling and storage-ownership change rather than a
second simulation model. Strategic direct mutation and multi-owner indexes must
still be converted; they are not exempt serial lanes.

### Spatial shard actors

Persistent geographic actors are rejected by measured skew and nonlocal
relationship/gambit traffic. Spatial ownership survives only for cell/map
destinations inside the typed-owner BSP design. There is no reason to build a
logical geographic-actor prototype before the selected candidate.

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
