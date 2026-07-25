# Event Log System

The event log is the player-facing projection of kernel events. It is also a
debugging aid, but it is not the telemetry store.

## Purpose

The player needs to see what changed without reading raw trace output. The event
log should surface important changes in short, linked entries while detailed
analysis remains in parquet telemetry.

## Event Sources

- Low-altitude verbs: movement commitments, culture influence changes, coverage
  growth, work-tier growth, and headquarters moves when surfaced.
- Relationship maintenance: significant relationship strength changes or culls.
- Unified effects/lifecycle resolution: health changes, aging, death, collapse,
  and historical transitions.
- Gambits: declarations, betrayals, wielding, killing, relationship inversions.
- Generators: created entity and relationship bundles, plus provenance links.

## Entry Structure

Each entry should include:

- tick
- event type
- source program
- involved entity IDs
- short text generated outside the kernel from symbol tables
- optional map focus position

## Boundary

Kernel events are primitive and no-string. Human-readable event text is produced
by projection code after joining against symbol tables.

Provenance is streamed rather than retained as completed kernel tables.
Mutation events carry their invocation token; the completion event carries the
producer, parent, trigger evidence, and output count. The provenance projection
indexes that stream without naming domain chains. Curated chronicle chains are
authored in domain config, compiled to runtime IDs, and interpreted by the game
layer against that external read model.
