# System 17: System Executor

System cards are a future mid-altitude layer. They are not the current
low-altitude physics runtime.

## Purpose

System cards should define broad world forces: diffusion, isolation, scarcity,
weather, rituals, trade patterns, or other slow balancing mechanisms.
They are intended to operate over scopes rather than single actors.

## Boundary

Low altitude is always on and runs without card permission.

Gambits are entity-authored high-stakes actions.

Generators create new named entities and relationship bundles.

System cards sit between those layers. They should read accumulated state and
apply broad effects over time, but they should not replace low-altitude physics
or become hidden directors.

## Deferred Status

No concrete system-card executor is being tuned in the current low-altitude
rewrite. When system cards return, they should use the same kernel discipline:
domain-authored config, gameplay validation where reusable, primitive compiled
kernel data, and event-backed observability.
