# System 4: Map / Spatial

The 2D physical world. Geography that matters to the simulation.

---

## Purpose

The map is the physical substrate of the simulation. It determines where things are, what's near what, what terrain separates or connects entities, and what natural resources are available where. Geography drives low-altitude simulation through its attributes -- terrain type, elevation, fertility, resources are all queryable properties that other systems use.

---

## Design Decisions

### Discrete grid with small units

The map is a discrete hex grid (or square -- TBD) with small cell size. Think Civilization scale, not Catan scale. Individual cells are small enough that entities like cities can occupy multiple cells. A mountain range is many cells. A river follows a path across cells.

Small cell size gives the simulation fine spatial granularity for:
- Entities spanning multiple cells (a city grows to occupy more cells as population increases)
- Natural features following realistic geography (rivers flow, coastlines curve, mountain ranges extend)
- Movement and pathfinding at meaningful resolution
- Bulk flow computed across cells with natural geographic barriers

For testing and MVP, the grid can start smaller (fewer cells). The architecture should support scaling up.

### Regions are first-class geographic areas

A region is a connected subset of map cells defined by geographic boundaries -- mountain ranges, rivers, coastlines, or other natural features. Every map cell belongs to exactly one region. Regions are generated during map creation (procedurally, following natural geographic boundaries) and are static during a run.

Regions are the scope unit for pressure computation: "military tension in the Northern Highlands" is a pressure value computed across all entities within that region's cells. Regions have no inherent relationship to political boundaries -- a single nation may control parts of multiple regions, and a single region may be contested by multiple nations.

Regions may also carry their own attributes (average fertility, dominant terrain type, total resource count) computed from their constituent cells. These are convenience aggregations, not independent state.

### The map is a spatial graph with queryable attributes

Each cell in the grid is a node with attributes:
- **Terrain type:** plains, mountain, forest, desert, water, coastal, swamp, tundra, etc.
- **Elevation:** height value affecting climate, water flow, movement
- **Fertility:** base agricultural productivity
- **Resources:** what natural resources are present (minerals, timber, game, fish, etc.)
- **Climate/biome:** derived from elevation, latitude, proximity to water
- **Habitability:** derived from terrain, climate, resources -- how viable is settlement here

Cells are connected to their neighbors (6 for hex, 4/8 for square), forming a spatial graph. This graph supports queries like:
- "Find all mountain cells adjacent to river cells" (terrain adjacency queries)
- "Find the shortest path between cell A and cell B avoiding water" (pathfinding with terrain costs)
- "Find all cells within N distance of this city" (radius queries)
- "Find all connected cells of terrain type forest" (flood-fill for natural features)

### Geography is attributes, not special cases

Geographic effects on the simulation are expressed through cell attributes, not through hard-coded geographic rules. A mountain cell has high elevation, low fertility, high movement cost, and potential mineral resources. These attributes feed into other systems naturally:
- Low fertility means settlements here produce less food (Resources system reads fertility)
- High movement cost means entities take longer to cross (Entity Behavior system reads movement cost)
- Mountain cells between two nations reduce the rate of relationship change (Relationships system reads terrain barriers)
- Rivers connecting cells increase trade flow between connected settlements (Bulk Flow system reads river connectivity)

This means adding new geographic effects is a matter of adding attributes to cells and having systems read them, not writing special-case geography code.

### Map is static at MVP, changeable eventually

For MVP, the map is generated once and does not change during a simulation run. Terrain, resources, elevation -- all fixed.

Post-MVP, the map should support change: deforestation (forest cells become plains), flooding (low cells become water), volcanic eruption (new land, destroyed settlements), resource depletion (minerals run out), and construction (roads, canals, bridges modifying connectivity and movement cost). These changes would be driven by card effects or by long-running low-altitude systems.

### Procedural generation is MVP

The map is procedurally generated at the start of each run. Generation parameters (map size, continental vs. archipelago, climate distribution, resource density) are run configuration options. Seed-based reproducibility allows replaying the same map.

Generation must produce maps with:
- Meaningful geographic variety (not uniform terrain)
- Natural strategic positions (chokepoints, defensible positions, fertile valleys, trade-route-friendly coastlines)
- Resource distribution that creates trade necessity (no single region has everything)
- Barriers and connectors that create natural political boundaries

---

## Cell Data Model

Each cell has:
- **Grid position:** coordinates in the hex/square grid
- **Terrain type:** enumerated (plains, mountain, forest, desert, water, coastal, etc.)
- **Elevation:** continuous value
- **Fertility:** continuous value (how much food this cell can produce)
- **Resources:** set of resource types present (may be empty)
- **Habitability:** derived from terrain + climate + resources
- **Movement cost:** derived from terrain + elevation (how expensive it is to cross)
- **Connectivity:** links to neighbor cells, potentially with connection type (road, river, sea lane) added during simulation

Derived values (habitability, movement cost) are computed from base attributes and cached. They don't change unless base attributes change (which is post-MVP).

---

## Multi-Cell Entities

Entities can occupy more than one cell:
- A city starts on one cell and expands to adjacent cells as population grows
- A mountain range spans many cells
- A forest is a connected set of forest-type cells
- A nation's territory is the set of all cells it controls (through its settlements and claimed land)

The entity's position in the Entity Graph is its "anchor" cell (primary location). Its spatial extent is computed from the map -- which cells it occupies. The map system tracks which entities occupy which cells.

---

## Interactions with Other Systems

| System | How Map/Spatial Interacts |
|--------|--------------------------|
| Entity Graph (#1) | Entities have coordinates linking them to map cells. Map tracks which entities occupy which cells. |
| Attributes (#3) | Cell attributes (fertility, resources) feed into entity attribute calculations. |
| Entity Behaviors (#6) | Movement uses pathfinding across the grid. Terrain affects goal evaluation (fertile cells attract settlement). |
| Relationships (#7) | Geographic barriers (mountains, water) affect relationship change rates between entities on opposite sides. |
| Resources (#8) | Cell fertility and resources determine settlement production capacity. |
| Map Visualization (#10) | Reads map cell data for rendering. Overlays visualize spatial state derived from entities and pressures. |
| Pressure (#11) | Pressure is computed per-region (groups of cells), influenced by geographic factors. |
| Generator Executor (#16) | Generators check spatial preconditions (available habitable cells, distance from existing entities). |

---

## Open Questions (Deferred)

- **Hex vs. square grid.** Hex has more natural adjacency (6 neighbors vs 4/8) and better distance uniformity. Square is simpler to implement and render. Hex is likely better for this game but TBD.
- **Grid scale.** How many cells in a typical map. Affects performance, visual density, and simulation granularity. Start small for testing, scale up.
- **River and road representation.** Are rivers/roads cell attributes, edge properties between cells, or separate overlay data? Edge properties (this border between cell A and cell B is a river) may be more natural than cell properties (this cell has a river).
- **Procedural generation algorithm.** Specific approach (noise-based, fractal, plate tectonics simulation, etc.) is TBD. Design requirement is output quality (meaningful geography), not algorithm choice.
- **Map change mechanics.** How terrain modification works post-MVP. Which systems can modify terrain. How cascading effects (deforestation reduces fertility, which reduces population, which reduces the nation's power) propagate.
