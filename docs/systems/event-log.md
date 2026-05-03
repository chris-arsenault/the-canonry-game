# Event Log System

The event log is the player-facing projection of kernel events. It is also a
debugging aid, but it is not the telemetry store.

## Purpose

The player needs to see what changed without reading raw trace output. The event
log should surface important changes in short, linked entries while detailed
analysis remains in parquet telemetry.

## Event Sources

- Low-altitude verbs: movement commitments, influence changes, coverage growth,
  headquarters moves when surfaced.
- Relationship maintenance: significant relationship strength changes or culls.
- Effects/upkeep/lifecycle: health changes, aging, death, collapse, historical
  transitions.
- Gambits: declarations, betrayals, wielding, killing, relationship inversions.
- Generators: created entity and relationship bundles, plus provenance links.
- Degeneracy: map generation or runtime degenerate-state messages.

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
