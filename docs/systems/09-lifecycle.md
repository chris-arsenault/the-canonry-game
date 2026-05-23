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

When that attribute reaches zero, the entity transitions from active to
historical status. Historical entities remain in the graph for reference and
relationship history, but they no longer participate in low-altitude programs.

## Upkeep

The upkeep program applies kind-specific lifecycle drain every tick. The drain
uses the entity's age, configured lifespan target, and tuning options.

Other low-altitude systems can counteract or worsen this drain through effects
and minor actor labor. Support, own-culture influence, culture slot occupancy,
home relationships, and favorable map signals can extend life. Threat, hostile
culture influence, crowding load, and hostile relationships can shorten it.

Minor actor lifecycle maintenance is the active counterpart to passive decay:
workers emitted by a source entity can spend local cultural support to restore
part of the source's age-related drain. Work tiers increase the number of
workers a source can support and how far those workers can operate.

## Design Bias

Death should be a normal source of churn, not only the result of a failed
recovery action. The world should create enough historical material for
generators and gambits to read without forcing every entity into a constant
maintenance treadmill.

Regions are special. They have lifecycle state, but they should not become a
primary actor in every low-altitude interaction.

## Tuning

The aging curve is a tuning parameter. The runtime supports aging-model tuning,
and defaults are judged from Monte Carlo evidence rather than from a status
document.
