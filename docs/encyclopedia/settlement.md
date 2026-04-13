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
| Population | Number of inhabitants. The settlement's health metric — reaches zero and the settlement becomes a ruin. |
| Development | Infrastructure level. Increases gold production and resilience. |
| Prosperity | Economic health. Drives population attraction and NPC satisfaction. |
| Defense | Military fortification. Affects vulnerability to external threats. |

## Processes

These are the mundane, always-available activities a settlement performs each tick. One is selected via weighted random.

### Harvest
Produce sustenance from the surrounding terrain. Higher fertility and more population labor yield more food.
- **Reads:** map cell fertility, population (labor force)
- **Writes:** settlement prosperity (increases with surplus), resource layer sustenance production
- **Cross-entity:** feeds the sustenance resource layer that NPCs and population depend on

### Repair
Invest population labor into maintaining and improving infrastructure.
- **Reads:** population, development
- **Writes:** development (increases), population (small cost — labor diverted)
- **Precondition:** development below maximum, population above minimum labor threshold

### Attract Migrants
When prosperous, a settlement pulls population from the surrounding region.
- **Reads:** prosperity, nearby settlement prosperity (differential)
- **Writes:** population layer (increases at this cell), population layer at source (decreases)
- **Precondition:** prosperity above regional average

### Store Food
Buffer sustenance surplus against future deficit. Builds stockpile reserves.
- **Reads:** sustenance net (surplus available), current stockpile
- **Writes:** sustenance stockpile (increases)
- **Precondition:** sustenance net is positive (surplus exists)

### Grow
Transition to the next settlement subtype when population thresholds are met.
- **Reads:** population, current subtype
- **Writes:** subtype (village → town, town → city)
- **Precondition:** population exceeds threshold for next subtype
- **Emits:** SubtypeChangedMessage — this is a notable event

## Decisions (Card-Gated)

These require specific cards in the player's tableau.

*Decisions are defined per card. See the card encyclopedia for settlement-targeting decisions like Expand Borders, Fortify, Establish Trade Route, and Spread Culture.*

## What Sustains a Settlement

Population is the settlement's health metric. It is sustained by:
- Sustenance surplus (food production exceeds consumption)
- Prosperity attracting migrants
- NPC presence boosting economic activity

It is threatened by:
- Sustenance deficit (famine)
- Lifecycle aging drain (old settlements require more maintenance)
- Population consumed by decisions (expansion, military, labor)
- Loss of controlling nation (stability loss)
