# Resources (MVP)

Two-resource system: gold and soup. Properties of the map graph that flow between locations.

---

## Purpose

Resources provide the material dimension that makes settlements, nations, and NPCs feel grounded. Without resources, population growth and settlement prosperity are abstract numbers. With resources, there's a tangible reason settlements grow (they have food), nations compete (they want wealth), and NPCs move (they seek sustenance).

Resources are properties of the MAP GRAPH, not the entity graph. They exist at locations and flow between connected locations. Entities interact with resources through their spatial position.

---

## The Two MVP Resources

### Soup

The primary sustenance resource. Settlements produce soup from regional fertility. Populations consume soup. Soup deficit causes population decline. Soup surplus attracts population.

Soup is THE food resource for the Ice Remembers domain. It is not an abstraction -- it is literally soup. All food is soup. This is a ground truth of the domain.

- **Produced by:** settlements, based on regional fertility and population (labor)
- **Consumed by:** population at settlements (each population unit consumes N soup per tick)
- **Surplus effect:** attracts population inflow, increases settlement prosperity attribute
- **Deficit effect:** causes population decline, decreases prosperity, drives NPC hunger process

### Gold

The economic resource. Produced by trade, extracted from resources, accumulated through commerce. Gold enables construction, military strength, and political power.

- **Produced by:** settlements with trade connections, settlements on resource-rich terrain, domain-specific production rules
- **Consumed by:** military maintenance, development projects, faction operations (TBD specifics)
- **Surplus effect:** increases development rate, enables card effects that require gold, attracts factions
- **Deficit effect:** decreases military strength, slows development, faction instability

---

## Design Decisions

### Resources are map-graph properties

Resources exist at map cells and flow along connections between settlements. They are NOT entity attributes. A settlement doesn't "have 200 soup" as an attribute -- the map cells the settlement occupies have a soup production rate, and the local soup supply is computed from production, consumption, and flow.

This means:
- Resource visualization is a MAP overlay (soup production heatmap, gold flow lines)
- Resource state is spatial, not entity-bound
- Moving a population to a different location changes their resource access automatically

### Flow model

Resources flow between connected settlements within the same nation. Surplus at one settlement flows toward deficit at another. Flow rate depends on:
- Distance (further = slower, more loss)
- Terrain between (roads speed flow, mountains slow it)
- Connection quality (trade routes improve flow)

Inter-nation flow requires trade-related cards (post-MVP for full implementation, but basic flow within a nation is MVP).

### Resources feed into existing systems

Resources don't create their own behavioral layer. They feed into existing systems:
- **Settlement behaviors:** population growth/decline driven by soup surplus/deficit
- **Nation behaviors:** economic pressure driven by gold surplus/deficit across controlled settlements
- **NPC behaviors:** hunger process driven by local soup availability
- **Pressure:** resource surplus/deficit feeds into economic stress pressure computation
- **Card preconditions:** cards can check resource levels ("fire when soup deficit in region exceeds N")

### Two resources is the MVP floor

Gold and soup are the minimum that creates interesting economic dynamics:
- Settlements must produce soup or decline (survival pressure)
- Settlements must generate gold or stagnate (development pressure)
- A settlement rich in soup but poor in gold has different strategic value than the reverse
- Trade (when cards enable it) creates interdependence: a gold-rich settlement trades with a soup-rich one

Post-MVP resources (materials, strategic resources, luxury goods) can be added by the domain without architectural changes.

---

## Interactions with Other Systems

| System | How Resources Interacts |
|--------|------------------------|
| Map/Spatial (#4) | Resources are map-graph properties. Terrain determines base production. Connectivity determines flow paths. |
| Entity Behaviors (#6) | Settlement growth driven by soup. NPC hunger driven by local soup. Nation pressure driven by gold. |
| Pressure (#11) | Resource surplus/deficit feeds into economic stress pressure. |
| Card Executors (#16-18) | Cards can check resource levels as preconditions. Cards can modify resource production/flow. Trade cards enable inter-nation resource flow. |
| Lifecycle (#9) | Population decline from sustained soup deficit accelerates settlement aging toward ruin. |
| Narrative Domain (#19) | Resource types, production rules, and consumption rates are domain-defined. "Soup" is an Ice Remembers domain choice. Another domain might call its sustenance resource something else. |
| Event Log | "Famine in Frosthollow: soup deficit for 10 ticks" |

---

## Open Questions (Deferred)

- **Exact production/consumption rates.** How much soup a settlement produces per population per tick. How much gold. Needs playtesting.
- **Flow mechanics.** How resource flow between settlements is computed. Whether flow is instant (deficit immediately draws from surplus) or gradual (flow rate per tick). Gradual is more realistic but adds complexity.
- **Gold consumption specifics.** What exactly consumes gold. Military? Development? Faction operations? The consumption model determines what creates gold pressure.
- **Map cell vs settlement level.** Do resources track per-cell or per-settlement? Per-cell is more granular but more expensive. Per-settlement is simpler.
- **Stockpiles.** Whether settlements accumulate surplus as stockpiles (buffering against future deficit). MVP might skip stockpiles and use pure flow rates. Stockpiles add interesting dynamics (saving for winter) but also complexity.
