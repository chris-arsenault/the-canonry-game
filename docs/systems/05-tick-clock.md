# System 5: Tick Clock

The tick clock is the deterministic heartbeat of the simulation. All world
mutation happens inside `Simulation.AdvanceTicks`.

## Core Rules

There is one simulation clock. Systems do not own separate clocks; they register
a cadence and run when the current tick matches that cadence.

Within a tick, program order is strict. Earlier same-tick writes are visible to
later programs. Programs that intentionally read previous-tick derived signals
must declare that dependency explicitly.

Speed control changes how quickly ticks are requested by the host or driver. It
does not change the deterministic tick sequence.

## Current Program Order

```text
LowAltitudeVerbs
MapActivity
MapSignals
Relationships
Effects
Upkeep
Gambits
Generators
```

Important dependency notes:

- `LowAltitudeVerbs` reads previous-tick map activity and map signals.
- `MapActivity` records the current physical and semantic presence state.
- `MapSignals` reads same-tick map activity and writes support, threat,
  crowding, capacity, and attractor fields.
- `Relationships` can use same-tick position, influence, contact memory, and
  relationship maintenance config.
- `Effects` reads same-tick map signals and relationship/influence evidence.
- `Upkeep` applies lifecycle drain and moves dead entities to historical status.
- `Gambits` and `Generators` see the low-altitude state created before them in
  the same tick.

## Cadence

Every registered program has a cadence. A program with cadence `1` runs every
tick. A program with cadence `5` keeps its registration order but only runs on
eligible ticks.

Cadence is a performance and pacing tool, not a separate simulation phase.

## Calendar Mapping

The in-world meaning of one tick is not fixed to a day/week/year mapping.
Simulation quality is judged by behavior over ticks: visible motion, graph
change, pacing, and long-run stability.
