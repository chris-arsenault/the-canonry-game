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
- Render influence overlays by entity and plane.
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
- Which settlements, factions, or artifacts are exerting influence?
- Are influence fields blob-like enough to read, or degenerating into thin
  disconnected spider legs?
- Which regions, settlements, and relationships explain a generator or gambit
  becoming available?

## Non-Goals

- Visualization must not compute simulation state.
- The UI should not depend on domain strings that the runtime does not expose.
- Map liveliness should not be inferred from generator fire counts alone.

## Debug/Design Views

Useful overlays for tuning:

- physical influence by entity
- semantic influence by entity
- recent movement trails
- recent verb commitments
- map activity heat
- map support/threat/crowding signals
- generator target availability
