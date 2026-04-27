# Nation

A political entity controlling settlements and territory. Nations are the primary macro-level agents in the simulation — their processes drive border tension, resource distribution, and political stability.

## Subtypes

- **Monarchy** — centralized rule, high legitimacy, fragile succession
- **Republic** — distributed governance, stable but slow to act
- **Tribal** — decentralized, strong cultural cohesion, limited infrastructure

## Key Attributes

| Attribute | Description |
|-----------|-------------|
| Stability | Resistance to internal disruption. Low stability invites faction revolt and succession crisis. |
| Military Strength | Capacity for armed conflict and territorial defense. |
| Cohesion | Internal unity. The nation's health metric — reaches zero and the nation dissolves. |
| Legitimacy | Recognized right to rule. Low legitimacy increases instability. |

## Processes

### Redistribute Resources
Balance sustenance and gold between controlled settlements. Surplus from prosperous settlements flows to struggling ones.
- **Reads:** controlled settlements' sustenance and gold levels
- **Writes:** sustenance/gold redistribution across settlements
- **Cross-entity:** modifies resource layers at multiple settlement locations

### Patrol Borders
Reduce tension along borders with neighboring nations. Costs population (soldiers on patrol).
- **Reads:** border tension (from rival proximity), military strength
- **Writes:** stability (increases), population at border settlements (small cost)
- **Precondition:** has at least one border with another nation

### Collect Taxes
Extract gold from controlled settlements to fund national activities. Increases national treasury at the cost of settlement prosperity.
- **Reads:** controlled settlements' prosperity and population
- **Writes:** gold (increases at nation level), settlement prosperity (decreases slightly)
- **Cross-entity:** reduces prosperity at controlled settlements

### Rally Morale
Invest gold into boosting national cohesion and legitimacy. The nation spends economic resources to maintain political unity.
- **Reads:** gold, cohesion, legitimacy
- **Writes:** cohesion (increases), legitimacy (increases), gold (decreases)
- **Precondition:** gold above minimum threshold

## Decisions (Card-Gated)

*See the card encyclopedia for nation-targeting decisions like Expand Culture, Forge Alliance, Declare War, and Consolidate Territory.*

## What Sustains a Nation

Cohesion is the nation's health metric. It is sustained by:
- Prosperous settlements (economic health)
- Rally morale process (spending gold)
- Low border tension (peaceful neighbors)
- Cultural homogeneity among settlements

It is threatened by:
- Lifecycle aging drain (old nations accumulate bureaucratic entropy)
- Consolidation strain (more settlements = more administrative distance)
- Cultural diversity within controlled settlements (friction)
- Rival nations along borders (tension)
- Decisions that consume population and gold
