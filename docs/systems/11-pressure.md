# System 11: Pressure

Pressure is a macro aggregation layer. It smooths world state into broad values
that future cards, generators, or UI can read.

## Current Role

Pressures are not the primary low-altitude driver. Low-altitude verbs,
relationships, effects, upkeep, and map signals should behave sensibly without
pressure values controlling them.

Pressure remains useful as:

- a human-readable summary of regional or entity-level stress;
- a future generator/gambit/card precondition;
- an observability surface for macro trends.

## Model

Each pressure definition has:

- a scope;
- a baseline;
- a homeostasis rate;
- a smoothing factor.

The pressure runtime stores and updates pressure values by id. The current
domain can define pressure names and parameters without making pressure the
center of the simulation.

## Boundary

Low altitude should not read pressure as a causal input by default. Pressure is
derived signal first. If a future design wants pressure to become causal, that
should be explicit gameplay configuration rather than hidden behavior.
