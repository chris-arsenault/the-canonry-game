# System 11: Map Visualization

Dynamic visual representation of simulation state on the map.

---

## Purpose

The map must be visually dynamic, and that dynamism must correlate to the game state. The player spends significant time observing the map (SimCity/Cities Skylines attention pattern). If the map is static or disconnected from what's happening in the simulation, the contemplative tempo fails -- there's nothing to learn from looking.

This system is NOT a simulation system. It does not compute state that feeds back into other systems. It reads state from low-altitude systems and renders it spatially. It is the visual layer that makes the simulation observable.

---

## Design Decisions

### Visualize low-altitude physics, not bulk flow

The original concept of "bulk flow" (animated population movement, goods flowing along routes) assumed a resource system that is now post-MVP. Rather than simulating aggregate flow phenomena, the MVP map visualization reads existing low-altitude system outputs and renders them on the map.

What makes the map dynamic:
- **Entity positions change.** Heroes move (entity behavior system). Settlements grow or shrink (lifecycle + entity behavior). Nations' controlled territory shifts (card-driven relationship changes visualized spatially).
- **Relationship state is visible spatially.** Alliance/rivalry lines between nations. Trade connections between settlements. Factional presence within cities. These are relationship system data rendered on the map.
- **Attribute gradients are visible.** Settlement prosperity as size/glow. National stability as border solidity. Cultural influence as color gradients. Pressure values as heatmap overlays. These are attribute data rendered spatially.
- **Seasons change.** Terrain appearance shifts cyclically (greener in growing season, browner in dry season, snow in winter). This is a simple periodic visual modifier on terrain rendering.
- **Status transitions are visible.** A settlement going from active to historical (ruins appearing). A nation fragmenting (borders dissolving). An artifact being destroyed (marker disappearing).

### Map overlays as attention lenses

Following the Cities Skylines pattern, the map supports multiple overlay modes that recolor the map surface to show different data layers. The player toggles between overlays to study different aspects:

- **Political:** which nation controls which territory. Color-coded by nation.
- **Cultural:** which culture is dominant in which region. Color gradients showing influence.
- **Pressure:** heatmap of a selected pressure type (military tension, cultural friction, etc.)
- **Entity density:** where entities are concentrated. Settlement sizes, hero locations, artifact positions.
- **Relationship network:** lines between entities showing active relationships (alliances, rivalries, trade). Color/thickness by strength.
- **Terrain:** base terrain view with biome coloring, resource markers.

Only one overlay active at a time (overlays recolor the entire map, they don't stack).

### The map teaches the player to see

Per the contemplative tempo research: the game must continuously teach the player how to see. Map visualization supports this by:
- Making spatial patterns visible that aren't obvious from entity detail panels (two nations' cultural influence overlapping at a border = tension the player can SEE)
- Showing trends over time (an overlay that was green last era is now yellow -- things are getting worse)
- Surfacing the spatial consequences of card decisions (placing "Cultural Diffusion" and watching the culture overlay shift over subsequent ticks)

---

## What This System Renders

### Entity Markers

Visual representations of entities at their map positions:
- Settlements: sized by population, styled by subtype (village/city/fortress)
- Heroes and mobile entities: icons at their current cell, showing movement paths
- Artifacts: markers at their current location (held by entity or at a site)
- Nations: territory fill (colored region showing controlled cells)
- Factions: influence indicators within settlements they're present in

### Relationship Lines

Visual connections between related entities:
- Alliance lines between nations (color-coded, thickness by strength)
- Rivalry indicators (different color/style)
- Trade connections between settlements (when trade cards are active, post-MVP)
- Control lines from nation capitals to settlements (showing political reach)

### Attribute Overlays

Map-wide color gradients derived from entity and pressure state:
- Settlement prosperity gradient
- Cultural influence heatmaps
- Pressure heatmaps (per pressure type)
- Lifecycle health indicators (entities in decline shown dimmer/redder)

### Temporal Indicators

Visual changes that show time passing:
- Seasonal terrain variation (cyclic appearance change)
- Entity aging (visual indicators of old vs. young entities)
- Historical markers (ruins where settlements once stood, faded territory of former nations)

---

## Interactions with Other Systems

| System | What Map Visualization Reads |
|--------|------------------------------|
| Entity Graph (#1) | Entity positions, relationships, statuses |
| Attributes (#3) | Entity attribute values for sizing, coloring, overlay computation |
| Map/Spatial (#4) | Terrain, regions, cell data for base map rendering |
| Pressure (#11) | Pressure values per region for heatmap overlays |
| Entity Behaviors (#6) | Hero movement paths, entity position changes |
| Relationships (#7) | Active relationships for network line rendering |
| Lifecycle (#9) | Entity health metrics for decline indicators |
| Tick Clock (#5) | Current tick for seasonal cycle computation |

This system ONLY reads. It writes nothing back to the simulation.

---

## Open Questions (Deferred)

- **Visual style.** Art direction, sprite vs. procedural rendering, level of detail. Depends on engine choice (Godot) and art pipeline.
- **Overlay performance.** How expensive are full-map overlays to recompute? Per tick? On demand? Cached?
- **Animation.** How much animation is MVP? Hero movement as smooth interpolation vs. teleporting between cells. Seasonal transitions as smooth blends vs. discrete switches.
- **Information density balance.** How much can be shown simultaneously without overwhelming the player? The overlay toggle model helps, but entity markers + relationship lines + territory fill might already be dense.
- **Zoom levels.** How does visualization change at different zoom levels? Zoomed out: territory colors, pressure heatmaps, large-scale patterns. Zoomed in: individual entity markers, settlement details, local relationships.
- **Bulk flow visualization post-MVP.** When the resource system is added, animated goods/population flow along routes becomes valuable. Architecturally, this system should be extensible to support it.
