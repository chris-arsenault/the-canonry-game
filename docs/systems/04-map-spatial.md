# System 4: Map / Spatial

The map is the physical substrate and one half of placement. The other half is
the semantic plane.

## Physical And Semantic Maps

The physical map is a hex grid with Ice Remembers terrain vocabulary: shelf,
wastes, tundra, ice, hot springs, ocean, reef, scar, and related variants.
Procedural generation creates terrain-aware seeds rather than full-cover
placeholder partitions.

The semantic plane is an analogous map model, not a mirror of the physical
map. It has the same plane-aware storage, movement, influence, road, coverage,
superregion, and continent surfaces, but its coordinates and terrain mean
conceptual position rather than physical location. Physical `(6, 5)` and
semantic `(6, 5)` have no direct implied relationship.

Semantic terrain is first-class Ice Remembers terrain: lineage, yoke, witness,
silence, severance, breakaway, beacon spire, taboo ridge, cave pocket, open
field, and related authored media. Semantic terrain changes movement cost,
enterability, influence propagation, claim viability, and road formation just
as physical terrain does, with semantic interpretation instead of physical
material.

Named regions are physical coverage. There is no separate concept of anonymous
partition plus local named coverage. A region is the named geographic coverage.

## Coverage

Map coverage is a first-class map abstraction:

- Regions own connected terrain-aware coverage.
- Settlements can own local built coverage.
- New regions may be created or split by generators.
- Coverage creation should fail gracefully with explicit rejection data rather
  than crashing the host.
- New coverage can optionally change terrain when the narrative requires it,
  such as a scar.

## Influence

Influence is a per-entity, per-cell scalar layer. Multiple entities can influence
the same cell. Only one coverage owner can own a cell.

Current physical influence uses:

- `GrowInfluence`: extend local influence outward.
- `ConsolidateInfluence`: strengthen existing influence.
- `ReduceInfluence`: weaken foreign influence that is hostile or unaffiliated.
- `ExpandCoverage`: rarely turn strong settlement influence into owned coverage.
- `MoveHeadquarters`: rarely move a faction's physical anchor.

Semantic influence mirrors this on the semantic plane for ideas, doctrine,
memory, and affinity.

## Movement Routing

Movement is terrain-aware on both physical and semantic planes. Entities route
through a small set of definitive movement profiles rather than arbitrary
per-kind pathing rules. A kind can define a default profile, and subtype or
trait rules can refine it; culture does not override movement.

The movement planner uses hierarchical route planning over deterministic hex
segments. It routes through 10-hex and 5-hex segment structure before refining
to local cell steps near the actor and destination. Segment route cache entries
are keyed by plane, movement profile, segment size, source segment, destination
segment, and pathing epochs. The cache is segment-level, not per-entity full
path state.

Roads are derived from travel. For route-cache purposes, road quality is
reconciled on cadence into coarse marks such as none, low, and high. Small
quality changes inside the same mark do not invalidate cached segment routes;
only topology changes or reconciled road-mark changes advance route epochs.

Direct full-grid A* is not kept as a legacy fallback in the normal movement
path. Degenerate movement should fail explicitly rather than silently switching
back to the old whole-map search shape.

## Placement

Generators place entities independently on the physical and semantic planes:

- in a locality;
- in an authored area around a point, including near, on, or far/across shapes;
- by resolving an existing target or creating one if allowed.

Physical and semantic placement are independent. A splinter faction may be near
physically but distant semantically; a colony may be far physically but close
semantically.

Placement is terrain-aware on both planes. Authored allowed terrain is a hard
filter. Authored preferred terrain biases scored placement after locality,
radius, occupancy, semantic bounds, and coverage constraints are applied.
Generator placement failure rejects the fire instead of consuming generator
pulse budget.

## Quality Metrics

Map quality is evaluated by utilization, not by movement volume alone:

- distinct active cells over time
- activity entropy and top-cell share
- largest strict empty hex radius
- bounding-box fill
- influence component count and discontinuity
- per-entity roam radius and unique cells visited
