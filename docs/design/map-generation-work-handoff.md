# Map Generation Work Handoff

Date: 2026-07-19

Purpose: give a fresh reviewer a high-level record of the map-generation work,
the feedback that shaped it, the changes already made, and the remaining user
concerns. This document intentionally does not diagnose the current river
directionality concern, inspect its implementation, or recommend a solution.

## Starting point

This work began as part of the growing-map and ebb-and-flow feature. The first
reported failures were that newly revealed physical and semantic hexes were
uniform, Deep Ice covered large portions of the world, and thawing replaced the
underlying geography with one terrain type. The result was maps that converged
on Hot Springs in the physical plane and Open Field in the semantic plane.

The initial review separated two concerns:

- Frontier radius and entity-density work belonged together and was assigned
  to another agent.
- Durable generated geography, terrain preservation under transient ice,
  post-initialization terrain variety, and map presentation remained in this
  tranche.

Deep Ice was consequently treated as transient cover over durable generated
terrain rather than as a destructive terrain replacement.

The user also corrected the performance target early. World generation did not
need to be optimized below 1 GiB. Approximately 4 GiB is the preferred target,
and up to 8 GiB is acceptable. The user required that guidance to live in
`AGENTS.md`, and it shaped the later performance work.

## Introduction of microtriangles

The user proposed dividing every hex into 24 triangles so geography could
express rivers, plains, ridges, peaks, elevation, coasts, and other fine
features. The simulation was to remain hex-scale. A hex's terrain would be a
deterministic summary of its triangles, and the kernel would never simulate or
store the individual triangles.

This was accepted as a seed/worldgen and presentation concern rather than a
kernel concern. The work was split conceptually into:

1. Constructing latent physical geography before simulation.
2. Aggregating it into the existing hex-scale terrain contract.
3. Rendering it for inspection.
4. Deferring richer gameplay use of generated geographic facts to a later
   gameplay-mirroring tranche.

The user repeatedly emphasized that the additional detail must not expand the
simulation hot path or introduce microtriangle knowledge into the kernel.

## First generation and UI passes

The first microtriangle implementation established the 24-triangle topology,
physical fields, aggregation, startup serialization, and UI inspection. Startup
then became visibly unresponsive, so work expanded to include:

- A startup/loading surface rather than a frozen window.
- Background world construction and render-thread binding.
- Profiling of first-tick and UI lag using traces rather than guesses.
- Reusable pregenerated `.world` files for development.
- A world selector in the UI.
- Ordinary application-relative filesystem discovery for worlds. The earlier
  Godot-specific `res://` path approach was removed at the user's request.

The user initially asked for four pregenerated worlds. These remain local,
gitignored development artifacts rather than checked-in content.

## Rebuilding the generation model

Early screenshots showed that the initial generator was not acceptable. At the
macro scale it interpreted "ice bound with pockets of warmth" too literally:
most of the map was undifferentiated ice with a few concentrated warm bands or
hotspots. Continents were central blobs separated by overly broad oceans. At
the micro scale some ridges and plains were visible, but rivers, inland water,
and cold-region detail were poor.

The intended setting was clarified during this phase:

- This is not an intrinsically frozen planet.
- Before the Collapse, the world was roughly Earth-temperature.
- The apocalyptic war, death of the god, and resulting magical imbalance
  caused an artificial ice age.
- The Deep Freeze ended roughly twenty years before game year zero.
- Grandmother Deep, Shadowbroker Nyla, High Auditor Selka, and other ancient
  seed entities predate the Collapse.
- The "Before Times" and pre-Collapse period are the same era.
- Serial-numbered relics are advanced war weapons used in the apocalyptic war,
  not beings from a separate historical period.
- Pre-Collapse megaliths may eventually be seeded and transformed by geological
  processes into relic sites, but that was explicitly deferred until ordinary
  geography worked.

The initial heart is one warmth zone, not the only warmth zone. Warmth may occur
in oceans, coasts, inland basins, and high verdant hot springs. Cold climate is
supposed to contain varied geography rather than be represented as literal ice
everywhere.

## Layer architecture feedback

The user required every generation stage to be an independently testable layer
with explicit dependencies and a map-in/map-out style contract. Continental
construction, tectonics, climate, hydrology, glaciation, erosion, magic, and
classification were separated so defects could be corrected in one process
without replacing the entire generator.

Coverage expectations such as ocean and land-ice amounts were made configurable
targets rather than hard-coded invariants. Structural contracts—valid topology,
reciprocal adjacency, legal flow, and data-shape validity—remained invariants.

There were two important course corrections from the user:

- A proposed broad rewrite was rejected. Existing sound layers were to be
  reused and improved, while genuinely incompatible algorithms could be
  replaced and new discrete stages such as glaciation could be added.
- The subsequent response overcorrected toward doing too little. The user
  required a balanced evolution: retain working processes, substantially
  improve weak ones, and add missing processes when warranted.

This led to durable project guidance that feedback must be synthesized with the
whole conversation rather than applied as a swing to the opposite extreme.

## Multi-epoch geography

The generation model evolved into a configurable multi-epoch process. The user
suggested repeated uplift, glaciation, hydrology, and magic so that geography
would record history rather than look like independently painted noise. The
adopted direction reused existing layers across epochs and added missing
processes instead of restarting from scratch.

Work in this phase included:

- Moving crust provinces and typed tectonic boundaries.
- Repeated uplift, erosion, climate, drainage, river, and ice passes.
- Glacial erosion and deposition.
- Coastal evolution and ocean processes.
- Sparse meteor impacts whose basins, rims, fractures, and heat are modified by
  later epochs. The user specifically liked the resulting craters.
- Persistent mantle-plume volcanism producing seamounts, island chains,
  continental cones, calderas, and lava plains that later processes can reshape.
- Fire, aurora, and pressure magic passes interleaved with physical evolution.
- A larger microtriangle material, surface, landform, water, anomaly, and magic
  vocabulary so the final world is not reduced to a few terrain labels.

Vegetation categories such as forest, desert, swamp, and marsh were considered.
They were not added in this tranche because the immediate problem was physical
structure and magical geography, and their eventual gameplay meaning was not
yet established.

## Macro-geography revisions

Several screenshot reviews drove repeated improvements to continent and ocean
construction. Early continent seeds were visibly centralized and regularly
spaced. Coastlines and ocean basins read as blobs and bands rather than evolved
geography. Later revisions distributed tectonic activity across the map,
introduced more irregular boundaries, increased large-scale land/ocean
separation, and allowed impacts, volcanism, erosion, ice, and magic to add
character away from the initial heart.

The user's later assessment was that the macro result had become substantially
better and was probably good enough for the current tranche. The newer maps had
multiple continents and oceans, interesting activity outside the center, useful
craters, and real micro-scale detail across most of the field. The user still
noted that further macro improvement remained possible.

## Quality-gate correction

Percentage-based generation goals were initially used heavily while tuning
ocean, ice, river, and terrain coverage. The user pointed out that every visibly
failed map had nevertheless passed those percentages. They were providing a
false sense of correctness.

The response was to retain configurable targets where they are useful for
generation control and broad diagnostics, but stop treating them as acceptance
proof. Human review of rendered maps became the primary evidence for geography
quality. Tests remain responsible for durable structural and behavioral
contracts, not for declaring a map aesthetically successful.

## High-fidelity elevation and hydrology

The user questioned whether hydrology was operating on enough elevation
fidelity and whether lake floors and water surfaces were being conflated. This
led to a dedicated hydrology tranche with the following completed changes:

- Generation elevation now uses deterministic fixed point with 1,024 subunits
  per authored elevation unit.
- Fractional uplift, erosion, deposition, terrain floor, water surface, and
  conditioned drainage values survive through the epoch process.
- Rounded elevation remains available only as compatibility output.
- Physical floor, actual ocean/lake surface, water depth, conditioned drainage
  surface, and outletward routing rank are distinct facts.
- Lakes can have flat water surfaces over irregular floors.
- Glaciers can cross frozen inland water, carve grounded submerged floors, and
  leave sills or moraine dams. Floating ice does not carve the floor.
- Lake state is recomputed after glacial landscape edits.
- Drainage conditioning, flow guidance, final routing, river classification,
  channel geometry, floodplains, and deltas are discrete stages.
- Narrow rivers have an authoritative connected spine, while high-order or
  high-discharge rivers can occupy broader triangle coverage.
- River validation follows downstream paths to ocean, lake, endorheic water, or
  the configured world boundary.

The goal was to fix both de-quantization and routing together rather than tune
ties on top of an artificially flat elevation field.

## River feedback across iterations

Rivers have remained the least satisfactory part of the generated maps through
every screenshot cycle.

The user reported, at different stages:

- Rivers placed repeatedly on the same hex border.
- Nearly universal north/south orientation.
- Few tributaries, confluences, deltas, lakes, or genuinely wide channels.
- Small rivers that appeared to stop before reaching water.
- Blocky or chaotic filled river geometry at close zoom.
- Later maps with better macro-scale networks but persistent visible breaks and
  strong directionality.

Changes already made in response included moving river work fully onto the
microtriangle graph, separating the flow spine from width/floodplain/delta
coverage, retaining tributaries through confluences, completing downstream
channel paths, routing through lake entry and outflow, removing earlier
origin/handedness-based choices, and rendering the structural spine on surface
and diagnostic views.

The newest captures show the authoritative spine in surface, hydrology, and
river-geometry views. Despite the completed structural work, the user remains
strongly dissatisfied with the visually straight north/south rivers. This is
the current concern for the next reviewer. No diagnosis or proposed correction
is included here by design.

## Cold-region and magic legibility

The user consistently found warm hotspots more interesting than cold regions,
especially broad Aurora Ice fields. Conversely, the warm regions could become
too noisy and chaotic at micro scale.

Later work preserved natural relief and structural water beneath final ice and
magic surfaces, strengthened relief shading, and grouped magic features through
causal multi-epoch history rather than independent per-triangle thresholds.
Cold areas now contain more visible mountains, valleys, craters, coastlines,
water, and material variation. Warm regions still tend to be more visually
active, but the discrepancy is smaller than in the early maps.

## Performance and test-process feedback

World generation and UI startup were profiled with trace tools. Optimization
focused on measured hotspots in drainage conditioning, routing geometry,
fixed-point ordering, queue behavior, and redundant storage/work.

The final recorded radius-128 seed-42 generation took 39.59 seconds, followed by
2.05 seconds to write its approximately 372 MiB `.world` file. A separate
profiled run took 48.68 seconds and peaked near 4.3 GiB. Final CI's heavy seed
tests peaked around 5 GiB, inside the accepted memory envelope.

The user also corrected the verification cadence. Heavy integration tests were
consuming most milestone time without finding defects. During implementation,
unit tests and targeted new integration contracts were used; the full suite was
deferred until all milestones were complete. The final `make ci` passed with no
warnings or errors.

## Screenshot automation and shared rendering

The user rejected an attempted Godot viewport/headless-display approach and
asked for the actual map display implementation to be factored so it could be
called both by Godot and by an external harness.

The final design has one presentation painter for map colors, triangle and hex
geometry, feature overlays, river spines, and level-of-detail decisions. The
live Godot node and the external capture utility are canvas adapters around that
same painter. The capture utility loads a normal `.world` through ordinary file
semantics and renders PNGs without starting Godot or requiring a display
server.

The final seed-42 evidence set contains ten 2560x1440 views: whole-world surface,
elevation, hydrology, and river geometry; warm and cold surface details; cold
elevation; and three major-river detail views. It rendered in 11.24 seconds.
Generated captures and `.world` files are intentionally ignored artifacts.

The last shared-renderer pass also corrected two presentation defects found by
review: elevation diagnostics had been saturating most land to white, and the
hydrology/river-geometry views omitted the narrow structural river overlay.

## Boundaries and deferred work

The following work was deliberately outside this tranche:

- Frontier-radius behavior and entity-density changes, owned by another agent.
- Microtriangle state or behavior inside the simulation kernel.
- Triangle-scale entity movement, placement, or runtime mutation.
- Gameplay use of richer generated geography beyond the current aggregate
  terrain contract.
- A final expanded hex taxonomy for primary terrain plus significant gameplay
  features.
- Vegetation and biome simulation.
- Pre-Collapse megalith/relic geological history.
- Full tectonic or fluid simulation and dynamic ice-sheet physics.

Generators and gambits that permanently change terrain were raised as a future
runtime-contract concern. No broad removal or replacement was undertaken in
this map-generation tranche.

## Current state for handoff

All implemented map-generation, hydrology, volcanism, projection, UI, capture,
startup, and documentation changes were committed and pushed to `main` in
commit `b0ea8d3` and its preceding map-generation milestones. The working tree
was clean at that handoff.

The high-level result is materially better than the starting point: it has
multiple continents and oceans, multi-epoch physical history, varied cold and
warm geography, inland water, craters, volcanism, glacial features, explicit
high-fidelity floor/water/drainage state, and inspectable microtriangle detail.

The unresolved user-facing issue to inspect independently is the visual quality
and strong north/south directionality of rivers. The user wants the next agent
to form its own diagnosis from the generation process and evidence, without
inheriting a proposed cause from this handoff.
