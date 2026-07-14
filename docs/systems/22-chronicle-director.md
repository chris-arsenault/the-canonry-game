# Chronicle Director — the beat engine

The Chronicle Director casts story archetypes over live entities and then
watches. It is a recognition system with one steering rule, not a plot
executor: the simulation's philosophy is rules-then-consequences, and the
director never writes world state.

## Authorship classes

Beats are the fourth authorship class beside low-altitude verbs, gambits, and
generators. A beat is an authored dramatic *potential* that a running
chronicle makes real:

- **Opening condition** — a predicate over cast/world state or heat. Openings
  latch until struck. Openings may reference world state and heat, never
  another beat's strike; heat is the only narrative memory. This is the
  discipline that keeps an unordered pool from collapsing back into a
  scripted act order.
- **Charges** — what the open beat arms: role-pair gambit score bonuses
  (optionally heat-scaled) and chronicle-granted gambits (gambits flagged
  `ChronicleGranted` are inert unless an open beat grants them to the acting
  entity's role).
- **Strike condition** — what marks the beat as having happened: a gambit
  executed between roles (any outcome; failures are beats too), a gambit by a
  non-cast entity on a role, or a recognized state fact evaluated on the
  director pulse. Charge-only beats never strike.
- **Terminal flag** — striking a terminal beat ends the chronicle with its
  outcome code. Terminals only recognize world facts (deaths, edges culled by
  real gambits or relationship drift); every ending must be reachable through
  ordinary world actuators. Where an ending had no actuator, one is authored
  as a gambit (e.g. `penguin_forsakes_the_hunt`), never as a director write.

## Heat, convergence, fade

Each chronicle carries a heat scalar: strikes add authored heat, any cast-pair
gambit execution adds ambient heat, and heat decays each director pulse.
Heat-scaled charges grow with heat — convergence pushes stories toward their
endings without decreeing them. A chronicle whose heat stays below the fade
threshold past the grace window, with no strikes inside the quiet window,
resolves with the archetype's authored fade outcome. Fades are endings, and
some are authored as good ones ("the road endures quietly").

Planned layers: **convergence** (C2 — map-signal attraction at the story's
locus, entrance beats that grow the cast) and **remembrance** (C3 — strikes
strengthen memory edges; legendary resolutions arm canon birth).

## Cross-story beats and stances

Shared beats are written against stances ("protagonist", "adversary") in the
domain's `ChronicleSharedBeats` library. An archetype opts in with a stance
map (`Stances`); a shared beat applies only when every stance it references
is mapped. `ChronicleBeatSet.EffectiveBeats` assembles the per-archetype pool
(shared beats rewritten to role refs, narratives included, then archetype
beats) and is the single assembly point used by both the domain compiler and
game-layer presentation.

## Runtime shape

`Engine.Kernel/Programs/Chronicles` holds flat definition arrays (archetypes,
roles, beats, charges, grants) and fixed per-chronicle state: heat, open and
struck bitmasks (max 64 beats per archetype), role entity ids, and a
suppression ring so resolved cast members are not immediately recast as
primaries. The gambit program reports every execution through
`Chronicles.NoteExecution` (a pending ring consumed on the next director
pulse) and reads `ScoreBias`/`GrantsGambit` during candidate evaluation.
Casting is unchanged from R5: generator-grade queries, provenance-recency
primary selection, global and per-archetype concurrency caps.

Telemetry: `ChronicleChanged` events carry Cast / BeatStruck / Resolved, with
beat and outcome codes in reason-code space (`chronicle_events` table). The
UI projection accumulates durable per-tale beats — authored struck beats plus
observed cast events — for the Chronicle Journal.
