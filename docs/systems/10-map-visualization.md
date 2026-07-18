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
Final- and natural-surface views preserve an ocean or lake underlay beneath ice
and magic surfaces, keeping frozen water geographically legible.

Available views cover effective terrain, final surface, natural surface,
material, elevation, temperature, moisture, geothermal heat, landform,
hydrology, ice, fractures, tectonic provinces, crust type, tectonic boundary
type and strain, fire, aurora, pressure, and composite magic. These
views visualize projected facts; they never classify terrain or feed values
back to worldgen or the simulation.

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
