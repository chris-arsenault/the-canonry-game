# System 5: Tick Clock

The tick clock is the authored heartbeat of the simulation. All world mutation
happens inside `Simulation.AdvanceTicks`.

## Core Rules

There is one gameplay clock. Ticks do not contain pulses, subticks, or
worker-specific time scales. Systems register a cadence and receive one
opportunity when an owning partition reaches an eligible local tick.

Logical owners may be at different ticks inside the configured lead window.
Local authoritative writes are exact. Cross-owner writes travel as bounded
typed messages and may become visible before or after the receiver's next local
evaluation. Registration order and same-tick cross-program visibility are not
gameplay contracts.

Speed control changes how quickly ticks are requested by the host or driver. It
does not change authored durations or cadences. Physical worker count changes
owner assignment, not logical ownership.

## Current Program Set

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

Important ownership notes:

- entity owners hold position, attributes, tags, lifecycle state, and
  entity-keyed program rows;
- relationship owners hold edge facts, strength, and relationship-keyed program
  rows;
- cell owners hold terrain, coverage values, culture mass, and road work;
- structural indexes are projections maintained by their structural owner;
- gambits, generators, chronicles, world beats, map frontier, culture slots,
  knowledge, and minor actors run on stable program owners; and
- program owners read published remote values and emit ordinary typed owner
  messages rather than relying on registration order.

## Cadence

Every registered program has a cadence in ticks. A program with cadence `1`
runs on every local tick. A program with cadence `5` runs when its owning
partition reaches a tick divisible by five.

Cadence is a performance and pacing tool, not a separate simulation phase.

Current default cadence:

- ordinary movement, low-altitude verbs, relationships, effects/lifecycle,
  gambits, chronicles, generators, and minor actors: every tick;
- culture-field decay: every five ticks;
- `CultureSlots` and `Knowledge`: every ten ticks;
- world beats and map frontier: every twenty-five ticks.

There is no raw/main clock conversion, configurable main cadence, or sub-tick.
One tick is one complete ordinary simulation opportunity. Authored durations
are direct tick values; runtime state and snapshot data store those same units
without a cadence mapper.

## Calendar Mapping

The canonical calibration is 50 ticks per in-world year. Simulation quality is
still judged by behavior over ticks: visible motion, graph change, pacing, and
long-run stability.
