# System 9: Lifecycle

Lifecycle is the low-altitude program that makes entities age, decay, and leave
the active simulation.

## Health Attribute

Every kind declares one health-like attribute:

- regions use stability;
- settlements use temporary health;
- factions use cohesion;
- penguins use vitality;
- artifacts use condition.
- orcas use endurance;
- points of interest use presence;
- transports use integrity;
- occurrences use momentum;
- abilities use clarity;
- monsters use containment.

When that attribute reaches zero, the entity occurrence retires. Retirement
ends all incident relationships, removes districts hosted by the entity,
clears active-slot cooldown state, records compact chronicle role-retired facts,
and then emits the historical status transition. Later simulation rules do not
query the retired occurrence.

## Entity Resolution

The effects program owns a single active-entity resolution pass. It selects
only rules compiled for the entity kind, accumulates attribute effects,
district healing, and kind-specific lifecycle drain, applies each affected
attribute once, and retires the entity if its lifecycle attribute reaches
zero. The drain uses the entity's age, configured lifespan target, and tuning
options.

Other low-altitude systems can counteract or worsen this drain through effects
and minor actor labor. Support, own-culture influence, culture slot occupancy,
home relationships, and favorable locality can extend life. Threat, hostile
culture influence, crowding load, and hostile relationships can shorten it.

Minor actor lifecycle maintenance is the active counterpart to passive decay:
workers emitted by a source entity can spend local cultural support to restore
part of the source's age-related drain. Work tiers increase the number of
workers a source can support and how far those workers can operate.

## Design Bias

Death should be a normal source of churn, not only the result of a failed
recovery action. It should create enough event history for narrative and
analysis without forcing the live simulation to carry dead rows as gameplay
inputs.

Regions are special. They have lifecycle state, but they should not become a
primary actor in every low-altitude interaction.

## Tuning

The aging curve is a tuning parameter. The runtime supports aging-model tuning,
and defaults are judged from Monte Carlo evidence rather than from a status
document.
