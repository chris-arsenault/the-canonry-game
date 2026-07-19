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
flow accumulation, river order, ocean-current direction/strength/heat, wave
exposure, rainfall, snowfall, sea-ice suitability, bedrock elevation, sediment
thickness, cumulative erosion/deposition (including glacial deposition),
maximum glaciation, impact excavation/rim/ejecta history, channel strength
and order, floodplain intensity, delta intensity, mantle-plume roots, current
volcanic activity, cumulative volcanic construction, volcanic age, and permanent
feature flags. Physical materials
such as bedrock, scree, tundra soil, glacial ice, marine sediment, reef stone,
and geothermal crust are not gameplay terrain IDs. Stable within-hex and
across-edge adjacency makes ridges, coasts, drainage, and rivers continuous
across hex boundaries instead of independently rolled decorations.

Generation elevation is deterministic signed fixed point with 1,024 subunits
per authored elevation unit. Bedrock plus sediment defines the irregular
physical floor. Actual ocean or lake surface, water depth, the conditioned
drainage surface, and outletward routing rank are separate facts: priority
flooding may establish a flat lake surface without flattening its submerged
floor, and routing may cross a conditioned flat without pretending the terrain
itself is level. The kernel receives only the rounded hex-scale terrain result;
the fine fields remain in startup worldgen, projection, and inspection data.

Generation is ordered over one padded graph. Irregular moving crust provinces
establish continental assemblies and oceanic crust. Relative motion types their
boundaries as convergent, divergent, or transform; continuous strain then drives
mountain systems, rifts, fractures, and mostly submarine island arcs. Rare
intraplate mantle roots remain fixed while crust motion advances their surface
expression across epochs, constructing age-progressive seamount and island
chains plus occasional continental cones, calderas, and lava plains. Those
landforms supply geothermal provenance but do not originate in fire magic.
Four authored but configurable epochs—crust assembly, wet orogeny,
glacial maximum, and retreat/present—reuse these same physical layers with
different tectonic, hydrologic, glacial, coastal, temperature, and sea-level
forcing. Fluvial stream power incrementally removes sediment and bedrock,
routes the produced sediment downstream, deposits it in low-energy basins and
mouths, and accounts for material exported from the modeled field.
Before those epochs, the first volcanic constructs and sparse domain-neutral
impacts establish relief; impacts excavate basins, lift and
fracture rims, distribute conserved ejecta, and retain short-lived heat only
for sufficiently large strikes. Later volcanic, fluvial, coastal, glacial,
sea-level, and hydrological passes continue to act on and reshape those forms.
Connected-component and boundary-flood passes identify landmasses,
continental shelves, bathymetry, and the exterior ocean. Priority-flood
drainage then creates coherent lakes, accumulated runoff, tributaries, and
routing rank without changing the physical floor. Legal routes descend the
fine drainage surface or advance outletward across an exactly conditioned
flat; inherited valleys guide only physically compatible choices. Coordinate
hashes break only exact final ties. Basin-relative thresholds retain
tributaries, and classified channels remain continuous through exorheic lakes
to terminal water or an explicitly configured crop outlet. That one-triangle
routing spine remains authoritative while a separate channel layer widens
major flows perpendicular to local flow tangents from discharge, order,
relief, and basin membership. Low-gradient cross-valley margins become
floodplains; sediment delivery and sheltered shelf energy produce seaward
delta fans. These coverage fields do not rewrite downstream routing. A cold
derived climate supplies multiple separated warmth pockets
across ocean, shore, inland, and geothermal peak sites; the startup heart
overlaps one pocket but does not own the world's only warmth.
Currents follow broad curl forcing while responding to basin depth and coast
normals. They transport geothermal heat, moderate selected habitats, expose
coasts to waves, and break up sea-ice suitability. Coherent atmospheric paths
retain orographic moisture shadows and split precipitation into rain and snow;
only precipitation and melt on dry ground create local runoff. Standing ocean
and lake triangles contribute no local water merely by existing. Equal drainage
choices use an outlet-facing geometric preference, never triangle storage order.
Ice mass accumulates and ablates incrementally; surface, basal, geothermal, and
retreat melt feed proglacial drainage. Its retained flow graph drives separate
glacial erosion and terminus deposition passes. U-valleys, cirques, fjords,
moraines, and outwash are then classified from cumulative erosion, glacial
deposition, and maximum historical flux rather than stamped into elevation.
Ancestral river valleys, inherited ridges, and impact basins/rims remain
classifiable after their active process has waned. Wave/current exposure and
shelf geometry likewise drive a separate coastal erosion/deposition pass.
Surface-changing work is operator-split within an epoch: fluvial and sediment
passes share the beginning-of-epoch climate/drainage snapshot, glacial work is
followed by one post-glacial reconciliation, and coastal work shares that
post-glacial ocean/current snapshot. Derived facts are invalidated at those
batch boundaries and fully reconciled again before the next epoch. A branching fracture network
finishes the physical inputs. The padded
result is cropped to the configured latent disk, so the configured edge is not
automatically a coast or watershed.

Ice Remembers interleaves three worldgen-only magic processes with the physical
epochs. Fire follows fracture conductance and writes generic heat forcing;
aurora routes into cold ice reservoirs; pressure rays propagate and reflect
through ocean geometry from one unnamed dead-god locus plus ancient impacts and
wakes. Pressure-derived coastal stress becomes a generic physical forcing field,
so the domain layer selects where magic acts while physical worldgen retains
erosion, deposition, and sediment accounting. Cumulative fire, aurora, pressure,
and coastal-pressure histories and their first-active epoch indexes remain
separate from the final current fields.
Interaction and final-surface classification run only after final physical
reconciliation, retaining rare compound anomalies without erasing the underlying
natural surface. Frozen oceans classify into pack ice, sea ice, ice shelf, and
polynya surfaces, making the setting icebound while leaving distinct warm-water
habitats. Cold land separately classifies polar barrens, wet tundra, cold
steppe, scree fields, river valleys, ordinary tundra, ridges, snowfields, and
glaciers. Physical and domain passes are independent dependency-declaring
processes; distribution goals such as ocean, land-ice, major-landmass size and
spacing, island share, coast density, and hotspot distribution are tunable
generation controls or non-blocking diagnostics rather than hard-coded
acceptance proportions. Final generation validation enforces structural
correctness; rendered fixed-seed worlds remain the acceptance surface for
geographic quality. These processes and every triangle field remain external
startup data; the dead-god locus is a generation pressure source, not a graph
entity.

The current simulation remains hex-scale. Worldgen derives one authoritative
external summary per physical hex: base terrain, primary surface, significant
features, dominant material, relief, roughness, climate, land/water/ice/river
coverage, peaks, magic intensity ranges, and reciprocal
river/coast/ridge/cliff/pass/fracture edge masks. Base terrain is chosen by
scored Ice Remembers rules over both aggregate and minority features, not a
dominant-material count cascade. The kernel installs only that resolved base
terrain plus the hex's superregion and continent ids. Movement, culture,
placement, and map signals consume the effective hex terrain; gameplay use of
the richer summary is a follow-up. Kernel systems do not store or independently
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

The UI receives an immutable projection of the complete generated physical
field at startup. Normal map presentation still uses the event-backed live map
as its fog boundary. At close zoom it draws the 24 triangles of each revealed,
unmodified hex; at distant zoom an explicit worldgen-inspection mode draws
aggregate latent hexes. Terrain, final and natural surface, material,
elevation, temperature, moisture, geothermal, currents, wave exposure,
precipitation, sea-ice suitability, landform, hydrology, ice, sediment,
cumulative erosion, maximum glaciation, and river geometry,
fracture, fire, aurora, pressure, current composite magic, and cumulative
magic-history/age views expose how the
synthesized terrain was chosen. Chunk culling keeps triangle drawing limited to
the visible field. Close zoom draws triangle-scale features without overlaying
aggregate full-hex masks; distant shared feature edges are drawn once by a
deterministic owner. Surface views blend structural oceans, lakes, rivers, and
deltas beneath ice and magic surfaces, then apply restrained relief shading at
both aggregate and triangle LODs. The tooltip exposes both hex summaries and
individual triangle facts.

Runtime triangle edits and triangle-scale movement or placement remain
follow-up systems; rendering the geography does not move ordinary simulation
below the hex scale.

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
