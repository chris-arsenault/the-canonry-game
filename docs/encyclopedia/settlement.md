# Settlement

A constructed location. Cities, towns, villages, fortresses, outposts. Built by people, sustained by resources, shaped by the decisions of their controlling nation.

## Subtypes

Settlements evolve through subtypes as they grow:
- **Village** — a small founding settlement
- **Town** — grown past the village threshold
- **City** — a major population center
- **Fortress** — a militarily fortified settlement
- **Ruin** — an abandoned settlement (historical)

## Key Attributes

| Attribute | Description |
|-----------|-------------|
| Population | Number of inhabitants. Health metric — reaches zero and the settlement becomes a ruin. Starts low, unbounded max. Lifecycle drain increases with age. |
| Sustenance | Food stockpile. Produced by stockpiling, consumed by converting to population growth. |
| Fame | Reputation and influence. Increased by deeds. Required to attract population from other settlements. |
| Ideology | Political/cultural leaning. Settlements with similar ideologies attract each other's population more easily. |

## Processes

These are the mundane, always-available activities a settlement performs each tick. One is selected via weighted random.

### Stockpile Sustenance
Produce sustenance from surrounding terrain and labor.
- **Reads:** terrain fertility (TODO), population (labor force)
- **Writes:** sustenance (increases)
- **Notes:** currently flat rate, will scale with terrain fertility when hex cell data is accessible to processes

### Convert Sustenance to Population
Convert food surplus into population growth.
- **Reads:** sustenance
- **Writes:** population (increases), sustenance (decreases)
- **Precondition:** sustenance above threshold

### Attract Population
Pull population from a nearby settlement. Requires fame advantage and proximity.
- **Reads:** fame (self vs target), ideology (similarity), distance to target
- **Writes:** population (increases on self, decreases on target)
- **Precondition:** fame > target fame, target within range
- **Modifiers:** similar ideology increases attraction rate, dissimilar decreases it
- **Range:** base range is short. Extended by trading posts (see below).
- **Target selection:** nearby settlements, weighted by fame differential and ideology similarity

### Build Trading Post
Extend the settlement's interaction range. A supporting process — does not directly satisfy a need but enables other processes (attract population, trade) to reach further.
- **Reads:** population (labor cost), sustenance (material cost)
- **Writes:** range attribute or creates a trading post entity/relationship
- **Notes:** analogous to movement for NPCs — an enabler process that creates conditions for other processes. See docs/systems/enabler-processes.md.

### Harvest Resources
Collect specific resources from a nearby point of interest (POI).
- **Reads:** POI type and yield, distance to POI
- **Writes:** relevant resource attribute (gold, materials, etc.)
- **Precondition:** POI within range

### Build Fortification
Invest population labor into military defenses.
- **Reads:** population (labor cost)
- **Writes:** defense attribute (increases)

### Create Farm
Establish agricultural infrastructure to increase sustenance production.
- **Reads:** population (labor cost), terrain fertility
- **Writes:** sustenance production rate (permanent modifier via tick effect)
- **Notes:** creates a lasting tick effect rather than a one-time delta

### Grow (Subtype Transition)
Transition to the next settlement subtype when population thresholds are met.
- **Reads:** population, current subtype
- **Writes:** subtype (village → town, town → city)
- **Precondition:** population exceeds threshold for next subtype

## Decisions (Card-Gated)

These require specific cards in the player's tableau.

*Decisions are defined per card. See the card encyclopedia for settlement-targeting decisions like Expand Borders, Fortify, Establish Trade Route, and Spread Culture.*

## What Sustains a Settlement

Population is the settlement's health metric. It is sustained by:
- Sustenance surplus (converting food to population growth)
- Fame attracting migrants from nearby settlements
- Ideological similarity with neighbors

It is threatened by:
- Sustenance deficit (no food to convert)
- Lifecycle aging drain (old settlements require more maintenance to sustain population)
- Population consumed by building/labor processes
- Loss of controlling nation (stability loss)
- Rival settlements with higher fame stealing population
