# Region

A geographic area defined by natural boundaries. Mountains, rivers, and coastlines divide the world into regions. Regions are the spatial unit for pressure computation and provide the terrain that determines what settlements can produce.

## Subtypes

- **Plains** — fertile, habitable, low movement cost
- **Mountain** — mineral-rich, defensible, high movement cost
- **Coastal** — access to water routes, moderate fertility
- **Tundra** — harsh, low fertility, sparse population

## Key Attributes

| Attribute | Description |
|-----------|-------------|
| Fertility | Agricultural productivity. Determines sustenance production rate for settlements in this region. |
| Resource Richness | Abundance of extractable resources (minerals, timber, game). |
| Habitability | How viable the region is for settlement. Derived from terrain and fertility. |

## Processes

Regions are mostly passive — they provide the stage. But they do have slow, long-term processes:

### Seasonal Shift
Fertility and habitability fluctuate with the seasonal cycle. Growing seasons increase fertility; harsh seasons decrease it.
- **Reads:** current tick (season calculation), base fertility
- **Writes:** fertility (cyclical modification around baseline)
- **Note:** seasonal effects propagate to all settlements in the region via sustenance production

### Develop Features
Over long time scales, regions slowly change. Resource deposits are discovered. Fertility shifts from sustained cultivation or erosion.
- **Reads:** resource richness, settlement count in region, population in region
- **Writes:** resource richness (small increase with sustained habitation)
- **Precondition:** population present in region for extended period

## Decisions (Card-Gated)

Regions are permanent entities — they have no health metric and cannot die. But cards can trigger significant regional changes:

*See the card encyclopedia for region-targeting decisions like Develop Mystical Properties, Terraform, and Attract Wildlife (post-MVP).*

## Lifecycle

Regions are **permanent**. They have no health metric and no lifecycle drain. They exist as long as the simulation runs. The terrain may shift through processes and decisions, but the region itself persists.

## Role in the Simulation

Regions are the primary scope for:
- **Pressure computation** — military tension, economic stress, cultural friction, and demographic pressure are all calculated per region
- **Resource production** — terrain determines what settlements can produce
- **Adjacency** — region borders drive diplomatic tension between nations
- **Cultural dynamics** — cultural friction is computed from the diversity of cultures present in a region
