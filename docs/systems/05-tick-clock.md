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
- `MinorActors` stores unnamed individual workers and advances once at the end
  of every tick.

## Cadence

Every registered program has a cadence in ticks. A program with cadence `1`
runs every tick. A program with cadence `5` keeps its registration order but
only runs on eligible ticks.

Cadence is a performance and pacing tool, not a separate simulation phase.

Current default cadence:

- ordinary movement, low-altitude verbs, relationships, effects/lifecycle,
  gambits, chronicles, and generators: every five ticks;
- culture-field decay: every twenty-five ticks;
- `CultureSlots` and `Knowledge`: every fifty ticks;
- world beats and map frontier: every one hundred twenty-five ticks;
- `MinorActors`: every tick.

There is no raw/main clock conversion and there are no sub-ticks. The
`MainProgramCadenceTicks` setting is an ordinary program cadence. Authored
durations owned by main-cadence systems are normalized once into global tick
deadlines when installed; runtime state and snapshot data store those global
tick deadlines. Minor-actor TTL, emission, and action cadence are already
global tick values and are never scaled through the main cadence.

## Calendar Mapping

The in-world meaning of one tick is not fixed to a day/week/year mapping.
Simulation quality is judged by behavior over ticks: visible motion, graph
change, pacing, and long-run stability.
