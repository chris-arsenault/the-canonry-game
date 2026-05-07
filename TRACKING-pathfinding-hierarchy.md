# Hierarchical Route Planning Proposal

This proposal replaces the earlier per-entity path cache idea as the preferred
long-term shape for movement performance.

## Context

Low-altitude movement currently routes committed movement intents through
`MovementProgramRuntime.Navigate`, which performs a fresh A* search over the
cell grid for each step. That is workable on the current profile map, but the
intended simulation map is expected to be 10x to 100x larger. Entity-kind and
relationship-target movement can choose targets across the world, so optimizing
for only local movement is the wrong model.

The goal is to avoid pathfinding over the whole cell corpus for repeated
long-distance movement while preserving deterministic kernel behavior and
keeping allocations out of the kernel path.

## Proposal

Introduce hierarchical route planning over stable world subdivisions.

- Partition each plane into deterministic route regions, initially at 10-hex
  and 5-hex levels.
- Build route nodes and portals between adjacent regions.
- Route at the macro level first, then refine through local 5-hex and 1-hex
  cell searches near the actor and destination.
- Cache route results at the segment/region level, not as full per-entity path
  copies.
- Keep per-entity state limited to a small route commitment cursor or short
  local segment, if needed, rather than making per-entity path caching the core
  optimization.

The intended query shape is:

```text
actor cell -> local 5-hex portal -> 10-hex regional route
    -> destination 5-hex portal -> destination cell
```

## Movement Profiles

Actors should route through a small set of definitive movement profiles instead
of arbitrary per-kind movement breadth.

Examples:

- `solo_physical_no_water`: penguins and artifacts without swimming or vessel
  semantics.
- `solo_physical_with_water`: penguins with a swim trait and ship-like
  transports.
- Additional profiles only when they have materially different pathing rules.

This should drive a refactor away from broad per-kind movement cost variation
and toward discrete mobility profiles. Entity kinds can still express gameplay
identity, but pathfinding should see a smaller, stable set of route-capability
classes. This reduces cache key cardinality and makes segment route reuse
practical.

The route cache key should use movement profile rather than raw entity kind
where possible:

```text
plane + movement profile + source segment + destination segment + route epochs
```

## Road Reconciliation

Road updates should not invalidate route caches on every travel step. For cache
purposes, reconcile roads on a cadence and expose coarse road marks.

Suggested starting cadences:

- Fast road reconciliation: every 10 ticks.
- Slower/global reconciliation: every 25 ticks.

Route cache invalidation should happen only when a relevant segment or edge
crosses a coarse mark boundary, such as:

```text
none -> low -> high
```

Within a mark, road quality can continue to affect exact local movement cost,
but macro route caches should not churn on small quality changes or continuous
decay. This lets roads reinforce transport corridors without turning road
growth into constant cache invalidation.

## Performance Expectations

Full-grid A* scales with the reachable cell area and currently pays repeated
work every movement step. At larger map sizes, this becomes the wrong
computational shape, especially for world-spanning entity-kind and
relationship-target movement.

Hierarchical routing should reduce each query to:

- small local searches near the actor and destination,
- a much smaller macro graph search,
- cached segment or segment-pair route lookups for common corridors.

This should improve asymptotic behavior and make route work reusable across
actors that share movement profiles and regional travel corridors.

## Implementation Complexity

This is a navigation subsystem, not a small cache.

Required pieces:

- deterministic hex-to-region mapping for 5-hex and 10-hex levels,
- stable portal selection between neighboring regions,
- per-profile passability and cost summaries,
- macro graph construction,
- local path refinement and path stitching,
- segment route cache storage with fixed-capacity kernel data structures,
- pathing-specific terrain/materialization epochs,
- road reconciliation epochs and coarse road marks,
- snapshot/replay rules for cached route state,
- direct A* fallback for unsupported or degenerate cases.

`WorldVersion` is too coarse for route invalidation because entity movement
advances it. The planner needs pathing-specific epochs that change only when
route-relevant topology, terrain, or reconciled road marks change.

## Suggested Phases

1. Define movement profiles and route all movement cost/passability queries
   through them.
2. Build static 5-hex and 10-hex region indexing and portal discovery.
3. Add a terrain/topology-aware hierarchical planner with direct A* fallback.
4. Add fixed-capacity segment route caches keyed by plane, movement profile,
   source segment, destination segment, and pathing epochs.
5. Add road reconciliation marks and road-aware cache invalidation.
6. Replace direct per-step full-grid A* calls from low-altitude movement with
   the hierarchical planner.

## Non-Goals

- Do not implement a broad per-entity full-path cache as the final form.
- Do not make route cache invalidation depend on generic world version changes.
- Do not add allocation-heavy route structures to the kernel path.
- Do not introduce many movement profiles unless they create meaningfully
  different routing decisions.
