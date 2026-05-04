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

When that attribute reaches zero, the entity transitions from active to
historical status. Historical entities remain in the graph for reference and
relationship history, but they no longer participate in low-altitude programs.

## Upkeep

The upkeep program applies kind-specific lifecycle drain every tick. The drain
uses the entity's age, configured lifespan target, and tuning options.

Other low-altitude systems can counteract or worsen this drain through effects.
Support, allied influence, home relationships, and favorable map signals can
extend life. Threat, hostile influence, crowding load, and hostile
relationships can shorten it.

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
