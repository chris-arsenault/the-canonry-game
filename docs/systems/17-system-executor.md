# System 17: System Executor

System cards are not active runtime. The current mid/high-altitude runtime is
generators plus gambits.

## Purpose

This document preserves the boundary for inactive system cards: they must not
be confused with low-altitude physics, gambits, or generators.

## Boundary

Low altitude is always on and runs without card permission.

Gambits are entity-authored high-stakes actions.

Generators create new named entities and relationship bundles.

There is no concrete system-card executor in the active runtime. Any system-card
implementation must use the same kernel discipline: domain-authored config,
gameplay validation where reusable, primitive compiled kernel data, and
event-backed observability.
