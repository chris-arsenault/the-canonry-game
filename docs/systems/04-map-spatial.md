# System 4: Map / Spatial

The map is the physical substrate and one half of placement. The other half is
the semantic plane.

## Physical Map

The physical map is a hex grid with Ice Remembers terrain vocabulary: shelf,
wastes, tundra, ice, hot springs, ocean, reef, scar, and related variants.
Procedural generation should create terrain-aware seeds rather than full-cover
placeholder partitions.

Named regions are physical coverage. There is no separate concept of anonymous
partition plus local named coverage. A region is the named geographic coverage.

## Coverage

Map coverage is a first-class map abstraction:

- Regions own connected terrain-aware coverage.
- Settlements can own local built coverage.
- New regions may be created or split by generators.
- Coverage creation should fail gracefully through degeneracy reporting rather
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

## Placement

Generators can place entities:

- near a target
- across the map
- in a target region
- by resolving an existing target or creating one if allowed

Physical and semantic placement are independent. A splinter faction may be near
physically but distant semantically; a colony may be far physically but close
semantically.

## Quality Metrics

Map quality is evaluated by utilization, not by movement volume alone:

- distinct active cells over time
- activity entropy and top-cell share
- largest strict empty hex radius
- bounding-box fill
- influence component count and discontinuity
- per-entity roam radius and unique cells visited
