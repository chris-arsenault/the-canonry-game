# Monte Carlo Observability Completion Plan

This document defines the full observability work required to satisfy the
semantic parity ledger in
[monte-carlo-parity-ledger.md](./monte-carlo-parity-ledger.md).

It is intentionally broader than the immediate tuning needs. It describes the
complete target system for Monte Carlo analysis so future work does not confuse
table names, legacy parity, trace rows, and actual production data.

## Target Architecture

Monte Carlo observability should have three layers:

1. Primitive kernel events
   - The event log remains the source of truth for mutations and causal traces.
   - Runtime systems emit primitive IDs, not strings.
   - Observers consume the event stream; they do not write directly into kernel
     logs or bypass the event architecture.

2. Snapshot telemetry layer
   - A dedicated observability layer samples read-only kernel state at configured
     tick boundaries.
   - Snapshotting is not a fallback inside analyzers and not ad hoc code in the
     Monte Carlo CLI.
   - Snapshot providers own full-state observations that cannot be reconstructed
     from deltas alone, such as resource totals, pressure values, full coverage
     state, and configured health summaries.

3. Analysis table layer
   - Analysis outputs are DuckDB/Parquet tables only.
   - Analysis tables are query products derived from primitive event tables and
     snapshot tables.
   - CSV files are not part of the solution.
   - Human-readable summaries may exist, but structured analysis artifacts must
     be parquet-backed.

The important architectural rule is that observability domains should not be
replicated just because new traces are needed. Each domain gets one canonical
trace/snapshot contract:

- planner/process behavior belongs to the planner trace family
- map and coverage behavior belongs to the map trace/snapshot family
- entity state and attributes belong to the entity trace/snapshot family
- resource behavior belongs to the resource trace/snapshot family
- pressure behavior belongs to the pressure trace/snapshot family
- generator behavior belongs to the generator trace family
- relationship behavior belongs to the relationship trace/snapshot family
- lifecycle behavior belongs to the lifecycle trace family

Compatibility tables may exist as views, but they should not become parallel
systems with overlapping ownership.

## Domain Trace Families

The target system is organized by domain, not by legacy table names. Every
semantic row should have exactly one owning trace/snapshot family.

| Domain | Canonical Raw / Projection Family | Owns |
|---|---|---|
| Entity | Entity events and entity snapshots | Entity creation/status, physical/semantic movement, current attributes, entity-level stock dynamics. |
| Planner / Process | Planner trace family | Goals, candidates, scores, selected actions, process executions, support chains, locality bundles. |
| Map / Coverage | Map trace/snapshot family | Cell creation/change, terrain state, coverage ownership, coverage size, terrain mix, named territory development. |
| Resource | Resource trace/snapshot family | Resource deltas, current stock totals, per-resource bands, terrain/resource aggregates. |
| Pressure | Pressure trace/snapshot family | Pressure deltas, pressure values, source contributions, scope ownership. |
| Generator | Generator trace family | Generator pulses, eligibility, blockers, selected target, produced entities/relationships/cell changes, placement decisions. |
| Relationship | Relationship events and snapshots | Relationship creation/culling/strength changes plus active relationship strength distribution. |
| Lifecycle | Lifecycle events | Status transitions, deaths, lifecycle reasons where available. |

## Planner / Process Trace Family

Planner and process behavior should be one domain family because process
selection is a planner outcome. The current system has overlapping concepts:

- `planner_goals`
- `planner_trace_goals`
- `planner_trace_scores`
- `planner_trace_steps`
- `process_candidates`
- generic `trace_events`

The target system should treat `trace_events` as the raw primitive payload and
project one semantic planner/process trace family from it.

### Planner / Process Tables

The semantic planner/process family should contain:

| Table | Purpose |
|---|---|
| `planner_traces` | One row per plan/evaluation, including entity, kind, plan id, status, reason, candidate count, selected goal/action, total score, start/end sequence, and dropped event count. |
| `planner_goals` | One row per generated goal, including desired effect scope/value/kind, target entity, urgency, parent goal, depth, actionable state, blocker reference, and support-rule source where applicable. |
| `planner_candidates` | One row per candidate action/provider considered for a goal, including process/action id, provider/target id, candidate index, parent candidate index, effect scope/value/kind, raw score, adjusted score, reward/base score, precondition status, diagnostic/status code, cooldown state, locality id, and selected flag. |
| `planner_scores` | Score contribution rows for goal/candidate scoring passes, including reason code, score delta, and running total. |
| `planner_steps` | Committed plan steps, including process/action id, target entity, relationship kind if relevant, step role, and order. |
| `planner_localities` | Locality bundle candidates, including plane, coordinates, anchor target, bundle score, served goal/effect counts, distance, committed/current flags, and selected flag. |
| `process_executions` | Executed process rows projected from the same family, including process id, target, result status, affected entities, and executed step metadata. |

### Relationship To Existing Tables

The target is not to maintain two parallel systems.

| Existing Table | Target Treatment |
|---|---|
| `trace_events` | Keep as raw primitive event table. It is not the main analysis contract. |
| `planner_trace_goals` | Fold into or replace with `planner_goals` once it carries the full goal fields. |
| `planner_trace_scores` | Rename/project as `planner_scores`. |
| `planner_trace_steps` | Rename/project as `planner_steps`. |
| `process_candidates` | Replace with `planner_candidates` or make it a compatibility view over `planner_candidates`. |
| `locality_opportunities` | Replace with `planner_localities` or make it a compatibility view over `planner_localities`. |

The end state should let process and planner analysis answer questions from the
same trace family:

- which goals existed
- which actions were considered
- why candidates failed or lost
- which candidate won
- which locality bundle was selected
- which committed steps executed

## Map / Coverage Trace Family

Map and coverage behavior should not be split into unrelated map tables. The
map/coverage family owns both cell-level mutation history and sampled current
coverage state.

| Table | Purpose |
|---|---|
| `map_cell_events` | Cell creation/change events: coordinates, previous/current terrain, previous/current owner, source code. |
| `map_snapshots` | Terrain aggregate snapshots: terrain id, cell count, covered cell count, active entity count. |
| `coverage_snapshots` | One row per coverage owner at snapshot tick: owner entity id, cell count, bounding box/spread, active entities inside coverage. |
| `coverage_terrain_snapshots` | Terrain mix per coverage owner: owner entity id, terrain id, cell count. |
| `placement_events` | Generator or system placement decisions, if required: anchor, placement mode, selected coordinates, plane, terrain/coverage context, and rejection reason where available. |

`placement_events` belongs here only if it describes spatial placement. Generator
analysis may join to it, but generator observability should not duplicate map
placement state.

## Entity Trace / Snapshot Family

Entity observability owns identity, status, movement, and attribute state.

| Table | Purpose |
|---|---|
| `entity_events` | Entity creation and status changes. |
| `movement_events` | Physical and semantic moves. |
| `attribute_events` | Attribute deltas by source. |
| `entity_snapshots` | Current entity status, kind/subtype/culture, and coordinates. |
| `entity_attribute_snapshots` | Current entity attribute values. |
| `stock_dynamics` | Analysis table derived from entity attribute events/snapshots: variance, reversals, dwell, pinning, and normalized range. |

`stock_dynamics` is an analysis product over the entity family, not a new runtime
domain.

## Resource Trace / Snapshot Family

Resource observability owns both resource deltas and full stock state.

| Table | Purpose |
|---|---|
| `resource_events` | Resource changes by cell/source. |
| `resource_snapshots` | Resource total, average, min, max by resource at snapshot tick. |
| `resource_terrain_snapshots` | Optional resource totals by terrain. |

Resource snapshots require the snapshot telemetry layer because deltas cannot
reconstruct untouched initial stock reliably.

## Pressure Trace / Snapshot Family

Pressure observability should be one pressure domain, not scattered across
processes or resources.

| Table | Purpose |
|---|---|
| `pressure_events` | Pressure deltas or set events by scope and source. |
| `pressure_snapshots` | Current pressure values by pressure id and scope target. |
| `pressure_source_snapshots` | Optional source contribution aggregates when pressure internals need explanation. |

## Generator Trace Family

Generator observability owns generator eligibility, selection, blockers, and
causal attribution. It should reference entity/map/relationship outputs rather
than duplicate those domains.

| Table | Purpose |
|---|---|
| `generator_events` | Pulses and fires: generator id, status, blockers, candidate count, selected target, created counts. |
| `generator_outputs` | Explicit cause/correlation rows linking a generator fire to created entities, relationships, and map cell changes. |
| `generator_state_snapshots` | Enabled state, run count, cadence/cooldown state. |

Generated entities, relationships, and map cells remain owned by their own
domains. `generator_outputs` is the attribution join table that connects those
domain rows to the generator cause.

## Relationship Trace / Snapshot Family

Relationship observability owns relationship topology and strength.

| Table | Purpose |
|---|---|
| `relationship_events` | Relationship creation, culling, fact changes, and strength changes. |
| `relationship_snapshots` | Active relationship counts and strength bands by relationship kind. |

## Lifecycle Trace Family

Lifecycle observability owns entity lifecycle transitions.

| Table | Purpose |
|---|---|
| `lifecycle_events` | Deaths and historical transitions. |
| `lifecycle_reason_events` | Optional reason/cause detail if lifecycle tuning requires attribution. |

## Snapshot Telemetry Layer

Some observability cannot be reconstructed from mutation events alone. A new
snapshot telemetry layer should own that responsibility.

The snapshot layer should be a first-class observability component, separate
from kernel mutation events and separate from analysis queries.

### Snapshot Layer Responsibilities

| Snapshot Provider | Rows Produced | Purpose |
|---|---|---|
| Entity snapshot provider | `entity_snapshots`, `entity_attribute_snapshots` | Entity status, kind/subtype/culture, physical and semantic position, current attribute values. |
| Kind snapshot provider | `kind_snapshots` | Active/historical counts plus configured health-attribute average/min/max. This replaces current zero health summaries. |
| Relationship snapshot provider | `relationship_snapshots` | Active relationship counts and strength bands by relationship kind. |
| Map coverage snapshot provider | `map_snapshots`, `coverage_snapshots`, `coverage_terrain_snapshots` | Terrain totals, named coverage totals, owner coverage size, covered/uncovered counts, terrain mix by coverage owner. |
| Resource snapshot provider | `resource_snapshots`, optionally `resource_terrain_snapshots` | Resource totals/average/min/max by resource, with terrain grouping if needed. This restores legacy resource snapshot parity. |
| Pressure snapshot provider | `pressure_snapshots`, `pressure_source_snapshots` | Pressure values by scope/entity/coverage plus aggregate input/source contributions. |
| Generator snapshot provider | `generator_state_snapshots` | Enabled state, run count, cooldown/cadence state, and other generator state needed to explain availability. |
| Experiment metadata provider | `runs`, `experiment_variants` | Seed, experiment id, config hash, git SHA when available, trace enabled flag, snapshot cadences, active variant flags. |

Snapshot providers should write primitive IDs and rely on symbol tables for
names. They should not serialize display strings into analysis rows.

### Snapshot Cadence

Snapshots should support independent cadences:

- entity snapshots
- resource snapshots
- pressure snapshots
- map/coverage snapshots
- generator state snapshots

The run metadata must record these cadences so analysis can interpret missing or
sparse rows correctly.

## Feature Completion Matrix

This matrix maps every feature in the parity ledger to the target source of
truth and required completion work.

| Semantic Feature | Target Source Of Truth | Target Output | Required Work |
|---|---|---|---|
| Process execution counts | Planner/process trace family | `process_executions` | Keep current event projection, but ensure execution rows are not mixed with planning rows ambiguously. |
| Process candidate diagnostics | Planner/process trace family | `planner_candidates`; optional `process_candidates` compatibility view | Project candidate rows from trace events or add missing primitive fields to trace emission if current trace cannot represent them. |
| Process candidate raw trace | Primitive `Trace` events | `trace_events` | Keep as raw data only; do not use as the main semantic analysis table. |
| Planner goal rows | Planner/process trace family | `planner_goals` | Replace empty legacy table with trace-derived goal rows carrying desired effect, target, urgency, parent, depth, actionable state, blocker, and support source. |
| Planner trace/scoring | Planner/process trace family | `planner_traces`, `planner_scores`, `planner_steps` | Consolidate current planner trace tables into the planner/process trace family. |
| Goal desired effect/target/actionable/parent | Planner/process trace family | `planner_goals` | Ensure trace payloads or projections contain all legacy goal semantics. |
| Attribute events | Entity trace/snapshot family | `attribute_events` | Keep current projection. |
| Entity attribute snapshots | Entity trace/snapshot family | `entity_attribute_snapshots` | Keep current snapshot rows, owned by snapshot layer. |
| Stock fluctuation analysis | Entity trace/snapshot family plus analysis layer | `stock_dynamics` | Add parquet analysis table for variance, reversal frequency, dwell time, pinning, and normalized range traversed. |
| Movement events | Entity trace/snapshot family | `movement_events` | Keep current projection. |
| Map aggregate snapshots | Map/coverage trace/snapshot family | `map_snapshots` | Keep current aggregate rows under snapshot provider ownership. |
| Tick coverage counts | Map/coverage trace/snapshot family | `tick_snapshots` | Keep current coverage counts, but make source ownership explicit. |
| Map cell mutation history | Map/coverage trace/snapshot family | `map_cell_events` | Keep current projection. |
| Map placement causality | Map/coverage trace/snapshot family | `placement_events` | Add only if generator analysis needs placement explanation; do not mix with aggregate map snapshots. |
| Resource events | Resource trace/snapshot family | `resource_events` | Keep current delta projection. |
| Resource snapshots | Resource trace/snapshot family | `resource_snapshots` | Add resource snapshot provider; deltas alone are insufficient. |
| Resource ablation outcome comparison | Analysis table layer over process/map/lifecycle/generator outputs | Experiment comparison tables | Use variant metadata and existing outcome tables; do not require resource internals for outcome comparison. |
| Pressure snapshots/internals | Pressure trace/snapshot family | `pressure_snapshots`, `pressure_events`, `pressure_source_snapshots` | Add pressure snapshot provider and source/delta rows if pressure internals are part of analysis. |
| Generator pulse/eval counts | Generator trace family | `generator_events` | Keep current projection. |
| Generated entity attribution | Generator trace family plus entity/relationship/map domains | `generator_outputs` joined to domain tables | Replace same-tick inference with explicit cause id or created output references in generator emission. |
| Relationship events/snapshots | Relationship trace/snapshot family | `relationship_events`, `relationship_snapshots` | Keep current projection/snapshot rows. |
| Lifecycle/death events | Lifecycle trace family | `lifecycle_events` | Keep current projection; add cause codes only if lifecycle analysis needs them. |
| Kind health summaries | Entity snapshot provider | `kind_snapshots` | Compute from configured health attribute instead of writing zero placeholders. |
| Locality opportunities | Planner/process trace family | `planner_localities`; optional `locality_opportunities` compatibility view | Project locality rows from the planner/process trace family; do not maintain a separate locality-only system. |
| CSV analysis artifacts | Analysis table layer | Parquet analysis tables | Remove CSV artifacts; write analysis outputs through DuckDB/Parquet. |

## Analysis Output Contract

The structured analysis product should be a directory of parquet tables plus a
DuckDB database used to build them. The analysis layer should produce tables such
as:

| Table | Purpose |
|---|---|
| `analysis_overview` | Run counts, ticks, active/historical entities, generator totals, final spread. |
| `analysis_readiness` | One row per semantic feature indicating populated row counts, missing sources, and whether analysis columns are trustworthy. |
| `generator_balance` | Pulse, eligibility, rejection, fire, and creation counts by generator. |
| `generated_subtype_coverage` | Created kind/subtype coverage by generator. |
| `process_pressure` | Process concentration, candidate counts, selection rates, scores, precondition/cooldown diagnostics. |
| `planner_pressure` | Plan status, goal counts, candidate counts, selected goal/action, score distributions. |
| `stock_dynamics` | Per-entity and aggregate stock variance, reversals, dwell bands, pinning duration, range traversed. |
| `map_spread` | Occupied cells, spread, anisotropy, bounding area, occupancy within bounds. |
| `coverage_development` | Named coverage counts, sizes, terrain mix, growth/shrink over time. |
| `terrain_occupancy` | Entity and coverage occupancy by terrain. |
| `resource_pressure` | Resource totals and stock bands from snapshots. |
| `pressure_dynamics` | Pressure values and source contributions over time. |
| `lifecycle` | Deaths, historical transitions, active/historical counts by kind. |
| `experiment_comparison` | Cross-variant comparison keyed by experiment metadata. |

CSV files should not be generated. If a human summary is needed, it can be
derived from parquet tables, but it should not be the structured analysis
artifact.

## Completion Rules

The observability system should not be considered complete because a table
exists. It should be considered complete for a semantic feature only when:

1. Production runtime data populates the table.
2. Tests prove the production path, not only synthetic table writing.
3. The table has stable primitive IDs and uses symbol joins for names.
4. Analysis tables use the populated source without zero-filling missing facts.
5. `analysis_readiness` reports the feature as populated and trustworthy.

## Validation Plan

Validation should include:

| Validation | Purpose |
|---|---|
| Production path tests | Assert that runtime execution populates planner candidates, planner goals, resource snapshots, pressure snapshots, map snapshots, and generated entity attribution where applicable. |
| Short trace Monte Carlo run | Assert nonzero rows for expected trace-derived tables under `--trace`. |
| Snapshot cadence test | Assert snapshot providers emit at configured cadences and record cadences in run metadata. |
| Parquet-only test | Assert analysis output contains parquet/DuckDB artifacts and no CSV files. |
| No-string row contract | Assert analysis row types use primitive IDs except symbol tables and explicit human summaries. |
| Readiness tests | Assert missing required source data produces an `analysis_readiness` failure row rather than zero-filled metrics. |

## End State

The end state is a single coherent Monte Carlo observability system:

- primitive events for mutations and causal traces
- a dedicated snapshot layer for full-state observations
- one canonical trace/snapshot family per domain
- parquet-only raw and analysis outputs
- readiness tables that identify missing or untrustworthy inputs

At that point the analysis can describe both outcomes and causes without
depending on legacy live-object access, duplicated domain traces, synthetic-only
rows, or CSV side artifacts.
