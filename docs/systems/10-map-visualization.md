# System 10: Map Visualization

Map visualization makes the simulation observable. It reads simulation state and
writes nothing back.

Map presentation follows the durable UI rules in
[ui-principles.md](../design/ui-principles.md): projection state owns versions
and dirty sets, while Godot render components own node mutation.

## Current Visual Responsibilities

- Render terrain and region coverage.
- Render physical entity positions and movement.
- Render semantic position in the bounded semantic plane.
- Render legacy/entity-attribution influence overlays when the projection has
  event data for them.
- Render cultural influence and boundary overlays from kernel boundary
  projection data.
- Render activity and signal overlays when useful for debugging or play.
- Show active entities by default.
- Keep historical/dead entities available for explicit history views rather
  than default hot-path map rendering.
- Keep the tableau inspectable without covering the entity list or hiding the
  map.

## Overlay Priorities

The map should help answer these questions quickly:

- Where are entities actually spending time?
- Which areas are empty or underused?
- Which cultures dominate, contest, or barely trace a cell?
- Where are cultural interiors, borders, isolated pockets, contested cells, and
  nexuses?
- Are culture fields blob-like enough to read, or degenerating into thin
  disconnected spider legs?
- Which regions, settlements, and relationships explain a generator or gambit
  becoming available?

## Physical Generation Views

The immutable startup geography projection exposes the complete latent physical
disk without making it live simulation state. Normal play clips that projection
to materialized, unmodified hexes; explicit generation inspection may reveal the
latent field. Distant zoom renders one aggregate value per hex, while close zoom
renders all 24 physical microtriangles.

Close zoom uses triangle-scale rivers, peaks, and significant anomalies only.
It does not superimpose aggregate coast/ridge/cliff/pass/fracture masks over the
triangle field. At distant zoom, each shared aggregate edge has one
deterministic drawing owner so reciprocal masks do not produce doubled seams.
Terrain, final-surface, and natural-surface views preserve structural oceans,
lakes, rivers, and deltas beneath ice and magic surfaces. They also apply
restrained elevation shading at aggregate and triangle LODs, keeping relief
legible without replacing surface identity.

The physical-map draw policy, colors, feature geometry, and LOD behavior live
in a presentation-only painter. The Godot map node is a canvas adapter for that
painter; it does not contain a second rendering implementation. The standalone
world-review harness adapts the same painter to an offscreen raster canvas, so
its output exercises the map users actually see without launching Godot.

### Read-only 3D inspection

The top-bar `Explore 3D` button opens a modal quality-control view of the same
immutable physical geography projection. Entry remembers the current plane and playback state,
pauses the kernel driver, hides the complete two-dimensional map and entity
subtrees, and activates an orthographic oblique camera. Exit restores the prior
plane and resumes only when playback was running before entry. Simulation map
overlays, entity markers, trails, auras, interventions, and semantic rendering
are deliberately absent.

The inspector reconstructs one deterministic displaced surface from the fine
elevation sample on every microtriangle. A world-space vertex key averages all
incident samples before chunks are built, so hex and chunk seams agree. Each
microtriangle then retains its exact sample at a centroid vertex and fans to the
shared corners, recovering local relief without cracks. Per-face vertices retain
categorical view colors while shared reconstructed normals make lighting
continuous. Water is a separate translucent surface whose displayed height is
the median floor-plus-depth sample for each projected water body.
River thalwegs, water-area outlines, and significant feature marks are separate
terrain-following line meshes. None of this interpolation changes the projected
triangle values shown by the geography-only tooltip.

The existing generation-view selector controls both renderers. Contextual 3D
controls add orbit, pan, zoom, vertical exaggeration, microtriangle wireframe,
and water visibility; the existing geography-feature toggle also controls 3D
feature marks. The calibrated `1x` scale matches the original inspector's `4x`
appearance, with a `0.5x` to `2x` range. Ground is double-sided so steep faces
do not appear as holes from oblique inspection angles. Ray picking resolves the
nearest reconstructed face back to its stable hex and triangle index without
adding physics bodies or command paths.

Available views cover effective terrain, final surface, natural surface,
material, elevation, temperature, moisture, geothermal heat, ocean currents
with direction vectors, wave exposure, rain/snow, sea-ice suitability, landform,
hydrology, ice, sediment thickness, cumulative fluvial/glacial/coastal erosion,
deposition, maximum ice thickness/flux, filled channel/floodplain/delta
geometry, fractures, tectonic provinces, crust type, tectonic boundary
type and strain, fire, aurora, pressure, composite magic, and cumulative magic
history colored by first-active epoch. These
views visualize projected facts; they never classify terrain or feed values
back to worldgen or the simulation.

For worldgen review, `tools/WorldGenerator capture` accepts a canonical `.world`
from the repository `worlds/` directory and writes deterministic whole-world
surface/elevation/hydrology/river views, warm/cold/major-river detail views, and
a JSON manifest under `.artifacts/captures/seed<seed>/`. The output location is
fixed and cannot be redirected. Capture is a normal command-line process and
does not require a desktop, virtual display, Godot process, or imported Godot
resources. PNGs are review evidence rather than golden-test inputs.

```text
dotnet run --project tools/WorldGenerator -c Release -- capture \
  --world=worlds/ice-remembers-seed-42.world
```

## Non-Goals

- Visualization must not compute simulation state.
- The UI should not depend on domain strings that the runtime does not expose.
- Map liveliness should not be inferred from generator fire counts alone.

## Debug/Design Views

Useful overlays for tuning:

- legacy/entity-attribution physical influence
- legacy/entity-attribution semantic influence
- cultural boundary kind and strength band
- cultural component id and size
- recent movement trails
- recent verb commitments
- map activity heat
- map support/threat/crowding signals
- generator target availability
