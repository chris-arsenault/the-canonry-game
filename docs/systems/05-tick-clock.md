# System 5: Tick Clock

The tick clock is the deterministic heartbeat of the simulation. All world
mutation happens inside `Simulation.AdvanceTicks`.

## Core Rules

There is one simulation clock. Systems do not own separate clocks; they register
a cadence and run when the current tick matches that cadence.

Within a tick, program order is strict. Earlier same-tick writes are visible to
later programs. Direct locality queries read current bounded world state and
are not separate scheduled phases.

Speed control changes how quickly ticks are requested by the host or driver. It
does not change the deterministic tick sequence.

## Current Program Order

```text
Movement
LowAltitudeVerbs
CultureFieldDecay
Relationships
Effects
Gambits
ChronicleDirector
Generators
WorldBeats
MapFrontier
CultureSlots
Knowledge
MinorActors
```

Important dependency notes:

- `Movement` owns route execution and road/travel-surface state.
- `LowAltitudeVerbs` reads direct current locality and culture-locality facts at
  each candidate it evaluates.
- `CultureFieldDecay` changes durable culture mass on its slower cadence.
- `Relationships` can use same-tick position, culture influence, contact
  memory, and relationship maintenance config.
- `Effects` reads direct current locality and relationship/culture evidence,
  combines all attribute deltas with district healing and lifecycle drain, then
  moves dead entities to historical status.
- `Gambits` and `Generators` see the low-altitude state created before them in
  the same tick.
- `MinorActors` stores unnamed individual workers and runs bounded actor-only
  substeps inside the world pulse.

## Cadence

Every registered program has a cadence in world pulses. A program with cadence
`1` runs every pulse. A program with cadence `5` keeps its registration order
but only runs on eligible pulses.

Cadence is a performance and pacing tool, not a separate simulation phase.

Current default cadence:

- ordinary movement, low-altitude verbs, relationships, effects/lifecycle,
  gambits, chronicles, and generators: every world pulse;
- culture-field decay: every five world pulses;
- `CultureSlots` and `Knowledge`: every ten world pulses;
- world beats and map frontier: every twenty-five world pulses;
- `MinorActors`: five actor-only substeps per world pulse.

There is no raw/main conversion. A configured generator pulse width of `25`
spans exactly `25` world pulses. Changing minor-actor substeps changes labor
resolution only; it does not retune graph pacing or the calendar.

## Calendar Mapping

The in-world meaning of one tick is not fixed to a day/week/year mapping.
Simulation quality is judged by behavior over ticks: visible motion, graph
change, pacing, and long-run stability.
