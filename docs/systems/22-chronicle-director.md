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
  executed between roles, a gambit by a
  non-cast entity on a role (optionally filtered to one gambit — "an
  instrument is brought to bear"), a role executing a named gambit against
  anyone (deeds in the world, not just against the cast), a generator firing
  with a role as its selected target ("the war is named"), or a recognized
  state fact evaluated on the director pulse — including heat falling (lull
  beats) and attributes recovering (second-wind beats). Charge-only beats
  never strike. Openings are evaluated at cast time, so Always-open beats
  charge and accept strikes from the first pulse. Execution strikes require
  the gambit to succeed unless the beat is authored `AcceptFailure`, which is
  reserved for outcome-neutral narratives ("blow answers blow") — a beat's
  sentence must never claim a deed the world refused.
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

Convergence has two mechanisms. **Entrance beats**: archetypes declare open
roles (kind-constrained cast slots that start empty), and entrance beats fill
them — the outside party of the strike joins the cast, whether an outsider
acting on a role (a second hunter answering blood with blood) or the outside
target of a role's deed (the faction a challenger swears into its cause). An
entrance without a qualified entrant is no strike. Joined roles carry their
own charges, so the tale's pull grows with its cast. **The convergence pull**:
past a heat floor, every outsider's gambit against a chronicle's cast gains
heat-scaled score — power is drawn toward running stories as a uniform rule
of the world, which is also what feeds the entrance beats. (The map-signal
attractor field was considered and rejected for the pull: nothing consumes
it at runtime, so gambit scoring — which every action already passes
through — is the honest force.)

**Remembrance** is the world's memory answering the chronicle system, and it
flows entirely through generators — the director still writes nothing.
Generator targets can be gated on chronicle facts: `ChronicleHeatMin` (the
target is cast in a running chronicle at or above the heat) and
`ChronicleLegendaryWithinTicks` (the target recently resolved out of a
chronicle whose final heat reached the legendary threshold; the suppression
ring records final heat for this). Ice Remembers authors two: *The Ice Takes
Notice* raises a witness-stone (memorial artifact) for the living center of
a blazing story, and *The Legend Enters the Canon* births a **saga** — a new
canon subtype — claiming semantic ground around a legend whose tale resolved
at legendary heat, memorializing them inside the culture's law and lore.

## Cross-story beats and stances

Shared beats are written against stances ("protagonist", "adversary") in the
domain's `ChronicleSharedBeats` library. An archetype opts in with a stance
map (`Stances`); the library applies only to archetypes with a non-empty
stance map, and a shared beat applies only when every stance it references
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
`Chronicles.NoteExecution`, the generator program reports every targeted fire
through `Chronicles.NoteGeneratorFire` (both land in a pending ring consumed
on the next director pulse), and the gambit program reads
`ScoreBias`/`GrantsGambit` during candidate evaluation.
Casting is unchanged from R5: generator-grade queries, provenance-recency
primary selection, global and per-archetype concurrency caps.

Telemetry: `ChronicleChanged` events carry Cast / BeatStruck / Resolved, with
beat and outcome codes in reason-code space (`chronicle_events` table). The
UI projection accumulates durable per-tale beats for the Chronicle Journal,
split into the telling (authored struck-beat narratives) and the record
(observed events, kept only when they involve two cast members or the world
acting on a cast member — a cast member's routine solo deeds elsewhere are
ordinary life, not the story).
