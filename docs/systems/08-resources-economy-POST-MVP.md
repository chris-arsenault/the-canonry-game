# System 8: Resources / Economy

Production, consumption, and flow of resources across the map.

---

## Purpose

Resources are the material substrate of the simulation. Settlements produce and consume them. Populations depend on them. Scarcity and abundance create the economic conditions that drive entity behavior, feed into pressure, and trigger card preconditions.

---

## Design Decisions

### Resources are properties of the map graph, not the entity graph

This is a critical architectural distinction. Resources belong to map cells and regions, not to entities. A settlement doesn't "have" 200 food -- the map cells the settlement occupies have food production, and the region has food availability. Entities interact with resources through their spatial position: a settlement consumes resources from the cells it occupies, a trade route moves resources between connected cells.

This means:
- Resource production is determined by terrain, fertility, and what's physically on the map
- Resource consumption is determined by what entities occupy which cells
- Resource surplus/deficit is a property of map regions, visible as a map overlay
- Entities read resource state from their location, they don't carry resources internally

**Why this matters:** it keeps the entity graph clean (entities are identity + attributes + relationships) and makes resources inherently spatial. A settlement's food situation is a question about its geographic position, not an entity attribute. Moving a population to a different region changes their resource access automatically.

### 3-6 distinct resource types

Resources are specific and distinct, not abstracted into a single "prosperity" value. Each resource type has its own production sources, consumption patterns, and strategic implications.

The specific resource types are TBD (deferred to domain/narrative design), but the architecture supports 3-6 types at MVP. Likely candidates shaped by the narrative domain:
- A food resource (sustenance -- whatever this world eats)
- A material resource (construction, tools, infrastructure)
- A strategic resource (military, power, rare materials)
- Possibly: a cultural resource (knowledge, art, religious artifacts), a trade good (luxury, surplus for exchange), an energy resource (fuel, magic, labor)

Each resource type has:
- **Production sources:** which terrain types and developments produce it, at what rate
- **Consumption patterns:** which entities consume it, at what rate, and what happens when supply is insufficient
- **Strategic role:** what having surplus or deficit means for the simulation (food deficit = population decline, material surplus = development potential)

### Flow model for MVP, stockpile post-MVP

**MVP: flow rates.** Each map cell has per-resource production and consumption rates. The net flow (production minus consumption) is computed per tick. Positive net = surplus available for neighboring cells or trade. Negative net = deficit drawing from neighbors.

Flow is the simpler model and produces immediately legible state: "this region produces more food than it consumes" or "this region is in food deficit." The player can see flow rates and net values as map overlays.

**Post-MVP: stockpiles.** Cells accumulate surplus as stockpiled reserves. Deficit draws down stockpiles before triggering scarcity effects. This creates richer dynamics: a settlement can survive a drought by drawing down stored food, but prolonged deficit eventually exhausts stores and triggers crisis. Stockpiles also enable strategic gameplay: building reserves during good times to survive bad times.

### Resource flow is partially low-altitude, partially card-gated

**Always-on (low-altitude):** 
- Per-cell production based on terrain, fertility, and development
- Per-cell consumption based on occupying entity population
- Internal flow within a nation's territory: resources move between a nation's settlements through connected cells automatically. A nation's settlements share resources without cards -- this is the basic infrastructure of nationhood.

**Card-gated (mid-altitude):**
- Inter-nation trade: resources flowing between settlements of different nations requires a trade-related card
- Trade route establishment: creating formal trade connections between distant settlements is card territory
- Taxation and redistribution policy: how surplus is allocated (military vs. development vs. reserves) is card territory
- Resource exploitation methods: whether a region's resources are harvested sustainably or strip-mined is card territory
- Scarcity response: what happens when deficit hits (famine, rationing, migration, conflict over resources) is card territory

The line: **intra-national resource sharing is physics (always on). Inter-national resource exchange and policy decisions about resource use are card territory.**

---

## Resource Flow Mechanics

### Production

Each map cell has a per-resource production rate determined by:
- **Terrain type:** fertile plains produce food, forested hills produce materials, mineral deposits produce strategic resources
- **Regional fertility modifier:** seasonal and climate effects (from the seasonal cycle in bulk flow)
- **Development level:** settlements improve production rates in their cells through infrastructure
- **Population labor:** cells with population assigned produce more than empty cells

Production is computed per tick based on these factors. It is deterministic given the cell's current state.

### Consumption

Consumption is driven by entities occupying cells:
- **Population:** each population unit consumes food and materials at a base rate
- **Development maintenance:** developed cells consume materials to maintain infrastructure
- **Entity-specific:** certain entity kinds or subtypes may consume specific resources (military units consume strategic resources, cultural institutions consume cultural resources)

### Net Flow

Per cell, per resource: net = production - consumption.
- Positive net: surplus. Available to neighboring cells within the same nation.
- Negative net: deficit. Draws from neighboring cells within the same nation.
- Zero: balanced. No surplus, no deficit.

### Internal Distribution

Within a nation's territory, surplus flows toward deficit along connected cells. This is automatic (low-altitude) and represents the basic logistical infrastructure of a political entity. The flow rate may be modified by:
- Distance from surplus to deficit (longer distances = slower flow, more loss)
- Terrain between (mountains slow flow, rivers and roads speed it)
- Settlement development along the route (developed waypoints improve flow)

### Inter-National Flow (Card-Gated)

Resources do NOT flow between nations automatically. Two neighboring nations each sitting on food surplus have no resource exchange unless a trade-related card enables it. This means:
- One nation can be in surplus while its neighbor starves, and the surplus doesn't help
- Trade cards create the inter-national flow that makes economic interdependence possible
- Cutting off trade (removing a trade card) creates real economic consequences for dependent nations

---

## Resource Visualization

Resources should be visible on the map as overlays:
- **Per-resource production heatmap:** color gradient showing where each resource is produced
- **Net flow overlay:** green (surplus) to red (deficit) showing regional economic health
- **Flow lines:** visual indicators of resource movement between settlements (especially for card-enabled trade routes)
- **Region detail panel:** exact production/consumption/net values for inspected regions

---

## Interactions with Other Systems

| System | How Resources Interacts |
|--------|-------------------------|
| Map/Spatial (#4) | Resources are properties of map cells. Terrain determines base production. Connectivity determines flow paths. |
| Entity Behaviors (#6) | Entity impulses read resource availability at their location. Hunger-driven movement responds to food availability. Settlement growth responds to resource surplus. |
| Pressure (#11) | Resource surplus/deficit feeds into economic stress pressure. Sustained regional deficit increases pressure. |
| Relationships (#7) | Trade relationships may modify inter-national flow rates (when trade cards are active). Resource competition between neighboring entities feeds into relationship strength modification. |
| Bulk Flow (#11) | Goods flow along trade routes is a bulk flow phenomenon. Resource flow and bulk flow may share computation or be the same system. |
| Lifecycle (#9) | Population decline from sustained food deficit. Settlement development decline from sustained material deficit. |
| Card Executors (#16-18) | Trade cards enable inter-national flow. Resource policy cards modify distribution. Scarcity response cards define what happens during deficit. |

---

## Open Questions (Deferred)

- **Specific resource types.** What the 3-6 resources actually are. Depends on the narrative domain (what does this world produce and consume?).
- **Stockpile mechanics.** How stockpiles accumulate and deplete. Maximum stockpile capacity per cell. How stockpile depletion triggers differ from flow deficit. Post-MVP.
- **Internal distribution efficiency.** How lossy is intra-national resource flow? Does distance always cost? Are there infrastructure investments (roads, canals) that improve it?
- **Resource depletion.** Can map cells run out of resources? Mineral deposits being exhausted, soil fertility declining from overuse. Post-MVP but affects map change mechanics.
- **Resource discovery.** Can new resource deposits be discovered during a run? A card that enables prospecting, revealing previously hidden resources.
- **Population as a resource.** Is population itself a resource type (labor), or is it purely an entity attribute? Population drives both production and consumption, so it's qualitatively different from other resources.
- **Resource flow vs. bulk flow.** Whether these are the same computation or separate systems with a defined interface. The mechanics are very similar (quantities moving along map connections based on differential).
