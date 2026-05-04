# System 19: Narrative Domain

A narrative domain is a mechanical content package. It defines what world the
simulation is running, not just what names appear in the UI.

## Domain Responsibilities

The domain owns:

- kinds and subtypes;
- health attributes and traits;
- cultures and naming;
- relationship kinds;
- terrain and map-generation terms;
- low-altitude gameplay config;
- gambits;
- generators;
- narrative presentation data.

The engine owns the generic runtime. It does not know Ice Remembers lore.

## Gameplay Boundary

Reusable mechanics live in `Engine.Gameplay`: low-altitude verb shapes, map
signals, effects, map control, validation, and compile-down to primitive kernel
data.

The domain chooses and configures those mechanics. The kernel executes the
compiled representation.

## The Ice Remembers

The first domain is The Ice Remembers: Aurora Berg, penguins, orcas, cultures,
the Long Dark, ice-memory, artifacts, factions, settlements, regions, and the
dead god under the sea.

The simulation history generated during play does not need to match the lore
documents exactly. The lore sets tone, vocabulary, and ground truth; the run
creates its own history.
