# System 4: Map / Spatial

The map is the physical substrate and one half of placement. The other half is
the semantic plane.

## Physical And Semantic Maps

The physical map is a hex grid with Ice Remembers terrain vocabulary: shelf,
wastes, tundra, ice, hot springs, ocean, reef, scar, and related variants.
Procedural generation creates terrain-aware seeds rather than full-cover
placeholder partitions. It deterministically generates a latent field through
radius 128 while initially materializing only the radius-18 heart.

The semantic plane is an analogous map model, not a mirror of the physical
map. It has the same plane-aware storage, movement, culture influence, road,
coverage, superregion, and continent surfaces, but its coordinates locate a
conceptual position rather than a physical one. Physical `(6, 5)` and semantic
`(6, 5)` have no direct implied relationship.

Its two free axes (`q`, `r`; `s = -q - r` is the derived cube coordinate) are
**neutral directions** — like compass bearings, they carry no meaning of their
own and impose no scale or good/evil polarity. Meaning lives in the terrain at a
coordinate and the entities standing on it, not in the axis values; the
simulation reads only the distance between coordinates (source and destination),
never any absolute "similarity" measure. The plane is a growing map like the
physical one — born as a heart and pushed outward through play — see
[the growing map](./26-growing-map.md).

Semantic terrain is first-class Ice Remembers terrain: lineage, yoke, witness,
silence, severance, breakaway, beacon spire, taboo ridge, cave pocket, open
field, and related authored media. Semantic terrain changes movement cost,
enterability, culture influence propagation, claim viability, and road
formation just as physical terrain does, with semantic interpretation instead
of physical material.

## Physical Microgeography And Effective Terrain

Every latent physical surface hex has 24 canonically indexed microtriangles.
Each triangle stores a physical material, landform, elevation, stable climate
and geothermal values, water kind/depth/body, drainage basin, downstream edge,
flow accumulation, river order, and permanent feature flags. Physical materials
such as bedrock, scree, tundra soil, glacial ice, marine sediment, reef stone,
and geothermal crust are not gameplay terrain IDs. Stable within-hex and
across-edge adjacency makes ridges, coasts, drainage, and rivers continuous
across hex boundaries instead of independently rolled decorations.

Generation is ordered over one padded graph: macro sites and plate-boundary
influence shape elevation; boundary flooding identifies the connected ocean;
climate and geothermal normals follow; a deterministic priority flood creates
acyclic drainage, coherent lakes, accumulated runoff, and tributary order; and
elevation topology classifies plains, valleys, ridges, peaks, and passes. The
padded result is cropped to the configured latent disk, so the configured edge
is not automatically a coast or watershed.

The current simulation remains hex-scale. Worldgen derives one authoritative
summary per physical hex: base terrain, dominant material, elevation range and
relief, roughness, climate normals, land/ocean/lake/ice/river coverage, peaks,
and reciprocal river/coast/ridge/cliff/pass edge masks. Base terrain is chosen
by scored Ice Remembers rules over both aggregate and minority features, not a
dominant-material count cascade. Movement, culture, placement, and map signals
currently consume the effective terrain id from that summary boundary; using
the richer physical summary is a follow-up. They do not independently
reinterpret the 24 triangles.

Continents and superregions are connected growth partitions over the generated
geography. Their traversal costs prefer their authored maritime, land, cold,
coastal, or highland character. Subdivision assignment never edits elevation,
water, material, landform, or terrain to manufacture a boundary.

Physical terrain has two runtime layers. Durable base terrain is the generated
summary or a later permanent transformation. Transient cover, currently Deep
Ice, wins when present. Removing cover exposes whatever durable terrain is then
underneath, including a transformation made while the cell was frozen. The
semantic plane has a varied deterministic per-hex blueprint but no
microtriangles or terrain-cover cycle.

Microtriangle rendering, inspection, runtime triangle edits, and triangle-scale
movement or placement remain follow-up systems; installing the geography
substrate does not move ordinary simulation below the hex scale.

Named regions are physical coverage. There is no separate concept of anonymous
partition plus local named coverage. A region is the named geographic coverage.
The semantic plane has named coverage too: canons (laws, taboos, accords,
doctrines) own jurisdictions of idea-space the same way regions own ground —
see [canons and jurisdictions](./24-canons-and-jurisdictions.md).

## The Vertical Axis

Physical cells carry an integer z coordinate in `[-3, +3]`. World generation
and startup seeding only create surface cells at `z = 0`; every non-surface
cell is *forged* at runtime by an entity action (`ForgeCellVertical`): Aurora
settlements raise spire tiers upward, Nightshelf settlements and factions dig
tunnel galleries downward. Forging extends the actor's own column one layer at
a time and, when the column is full, creeps the first layer into an adjacent
known column, so spire clusters and gallery networks spread instead of
stalling.

Forged cells are real cells: they hold terrain (`spire_tier`,
`tunnel_gallery`), accept culture mass, and are enterable through vertical
wander steps. Deliberate surface simplifications: roads, travel structures,
culture boundary components, and route planning operate on the `z = 0`
surface only, and forged-cell culture mass feeds the culture power ledgers
directly rather than the boundary component model. The semantic plane has no
vertical axis yet.

## Coverage

Map coverage is a first-class map abstraction:

- Regions own connected terrain-aware coverage.
- Settlements can own local built coverage.
- New regions may be created or split by generators.
- Coverage creation should fail gracefully with explicit rejection data rather
  than crashing the host.
- New coverage can optionally change terrain when the narrative requires it,
  such as a scar. Such changes update durable base terrain even when transient
  cover currently hides it.

## Culture Influence

Culture influence is a culture-owned tile layer on both planes. The durable
value is raw culture mass per `(culture, plane, cell)`, capped at `2000`.
Dominance share is recalculated from the raw culture totals at a cell, so two
cultures cannot both own 100% of the same cell. The raw scale lets a cell that
has been reinforced for hundreds of ticks differ from a fresh peripheral claim.

Current strength bands are:

| Band | Raw mass |
| --- | ---: |
| trace | `5+` |
| claimed | `30+` |
| controlled | `100+` |
| entrenched | `300+` |
| nexus | `700+` |

The `CultureBoundaries` kernel program derives interiors, borders, contested
cells, isolated pockets, holes, and connected components from this tile layer.
Consumers should query the boundary program or projection output rather than
recomputing their own component math.

Current culture influence sources:

- low-altitude influence verbs from named entities;
- minor actor culture expansion and cleanup verbs;
- culture slot multipliers that make slot occupants stronger influence
  producers;
- authored startup state.

Cleanup reduces all non-own cultures' raw mass at the targeted cell. It does
not mutate the dominance-share cache directly.

Entity-level influence production is retained as attribution and scoring
evidence for systems such as culture slots. It is not the durable territorial
influence layer.

Coverage ownership is separate and rarer than culture influence. Only one
coverage owner can own a cell, and generator placement/coverage rules still
use named coverage where the narrative requires a concrete region or settlement.

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

Travel structures are a separate map surface from terrain and roads. They are
not entities and do not convert the underlying cell terrain. Domains declare
travel-structure types, their plane, and movement-profile costs. A cell may
then carry a travel-structure id; movement uses the cheaper enterable surface
between terrain and structure, so a structure can make otherwise blocked cells
crossable for matching movement profiles without changing terrain semantics.

In Ice Remembers, `ice_causeway` and `deep_current` are the initial travel
structures. The authored `settlement_raises_causeway` gambit builds an
`ice_causeway` line between mutually connected settlements with an existing
trade route. This is the first implementation of bridges/currents/water-tubes
as travel surfaces rather than terrain rewrites or special road cases.

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
radius, occupancy, semantic bounds, culture-boundary gates, and coverage
constraints are applied. Normal or wilderness placement should not drop a new
entity into another culture's entrenched/nexus field; hostile-intrusion
placement is the explicit escape hatch for content that means to do that.
Generator placement failure rejects the fire instead of consuming generator
pulse budget.

## Quality Metrics

Map quality is evaluated by utilization, not by movement volume alone:

- distinct active cells over time
- activity entropy and top-cell share
- largest strict empty hex radius
- bounding-box fill
- terrain diversity among newly materialized cells and across the live map
- durable base-terrain preservation across cover/thaw cycles
- culture component count and discontinuity
- per-entity roam radius and unique cells visited
