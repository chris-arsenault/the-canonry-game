# Monte Carlo Parity Ledger

This document tracks Monte Carlo analysis readiness after the primitive-kernel
and event-sourced telemetry migration.

The point is to avoid confusing table existence with useful data. Each row
identifies the semantic feature, whether legacy Monte Carlo had it, what the
current production path provides, and whether it matters for the current tuning
experiments.

## Semantic Parity

| Semantic Feature | Legacy MC Had It? | Current State | Verdict | Current Experiment Relevance |
|---|---:|---|---|---|
| Process execution counts | Yes | Populated from `process_events`. | Complete enough. | Required now. |
| Process candidate diagnostics | Yes | `process_candidates` exists but production never writes it; only tests do. | Deficient. | Required now for H1. |
| Process candidate raw trace | No exact legacy equivalent | `trace_events` likely contains enough when `--trace` is enabled. | Query/projection missing; not proven data missing. | Required now for H1. |
| Planner goal rows | Yes | `planner_goals` exists but production never writes it. | Table parity deficient. | Maybe required, depending on whether trace replacement is enough. |
| Planner trace/scoring | No exact legacy equivalent | `planner_trace_*` is populated from primitive plan events. | New replacement; partially overlaps legacy goals. | Useful now. |
| Goal desired effect/target/actionable/parent | Yes in legacy `PlannerGoalRow` | Not in `planner_trace_goals`; likely partially in `trace_events`. | Needs trace-derived replacement or table removal. | Required only if process candidate trace is insufficient. |
| Attribute events | Yes | Populated from primitive `AttributeChanged`. | Complete enough. | Required now. |
| Entity attribute snapshots | Yes | Populated in `entity_attribute_snapshots`. | Complete enough. | Required now. |
| Stock fluctuation analysis | No, analyzer-level metric | Raw data exists; parquet analysis table missing. | Query gap. | Required now for H2. |
| Movement events | Yes | Populated from primitive movement events. | Complete enough. | Required now. |
| Map aggregate snapshots | Yes | `map_snapshots` populated from event-projected cells. | Parity restored. | Required now. |
| Tick coverage counts | Yes | `tick_snapshots.coverage_count`, `covered_cells`, and `uncovered_cells` populated from event-projected cells. | Parity restored. | Required now. |
| Map cell mutation history | No | `map_cell_events` populated. | New capability. | Useful, not required now. |
| Map placement causality | No | Not present. | Not a regression; not required now. | Later only. |
| Resource events | Yes | Populated from primitive resource deltas. | Complete for deltas. | Future only. |
| Resource snapshots | Yes | `resource_snapshots` exists as a retained output surface. | Deferred. | Not part of current tuning analysis. |
| Pressure snapshots/internals | No clear legacy equivalent | Not present. | Not a regression. | Later unless pressure ablation changes outcomes. |
| Generator pulse/eval counts | Yes | Populated. | Complete enough. | Required now. |
| Generated entity attribution | Yes, explicit in legacy fired message | Current `generated_entities` inferred by same-tick matching. | Weaker than legacy, but usable for counts. | Good enough now. |
| Relationship events/snapshots | Yes | Populated. | Complete enough. | Useful now. |
| Lifecycle/death events | Yes | Populated. | Complete enough. | Required now. |
| Kind health summaries | Yes, read configured health attribute | Current `kind_snapshots` writes health as `0`. | Deficient vs legacy. | Do not use; use attributes directly. |
| Locality opportunities | No | Table exists but production never writes it. | Not legacy regression. | Not required now. |
| CSV analysis artifacts | N/A | Analyzer writes CSV. | Architecture/output gap. | Must fix before calling analysis final. |

## Required Gaps For Current Experiments

| Gap | Type | Why It Matters | First Fix |
|---|---|---|---|
| Process candidate diagnostics | Real deficiency or missing trace projection | H1 depends on knowing why non-movement verbs lose. | Reconstruct a `process_candidates`-equivalent parquet table from `trace_events`; only add kernel events if trace is insufficient. |
| Stock fluctuation metrics | Query gap | H2 needs per-entity variance, reversal frequency, mid-band dwell, and pinning, not aggregate bands. | Add a parquet analysis table over `attribute_events` and/or `entity_attribute_snapshots`. |
| Parquet-only analysis output | Architecture/output gap | CSVs should not be part of the solution. | Replace CSV artifact writer with DuckDB/parquet analysis tables. |
| Planner goal parity | Ambiguous replacement | Legacy `planner_goals` is empty, but `planner_trace_*` and `trace_events` may replace it. | Do not claim complete; validate after process-candidate reconstruction. |

## Current Interpretation

Map coverage is not currently a gap for the current work. Aggregate map coverage
and terrain occupancy were restored through event-projected cells, `CellChanged`,
`map_cell_events`, `map_snapshots`, and tick coverage counts.

Planner goals are not cleanly complete. The old `planner_goals` table is empty.
The new planner trace system is a different representation that may be enough
for current tuning questions, but it needs validation against the actual
questions rather than table-name parity.

The highest-priority correction is process-candidate analysis. Do not run a long
Monte Carlo tuning baseline and call H1 answered until process candidate
diagnostics are populated from real runtime data or proven reconstructible from
`trace_events`.
