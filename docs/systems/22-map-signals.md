# System 22: Map Signals

Map signals are per-tick spatial summaries of where entities act, computed by
the `MapSignals` kernel program during the low-altitude physics rewrite. They
answer "what does this location feel like right now" without any consumer
having to traverse the entity graph.

## The five fields

- **Support** — friendly presence pressure: nearby same-culture activity,
  allies, sheltering structures.
- **Threat** — hostile activity pressure: nearby negative-polarity
  relationships and hostile entities acting.
- **Crowding** — how many actors are pressing on the same cells.
- **Capacity** — how much activity a location can sustain (terrain- and
  crowding-derived).
- **Attractors** — pull for movement and placement toward interesting
  locations.

Both planes have all five fields. The domain tunes the weights in
`CreateMapSignals()` (`MapSignalsConfig`).

## Ordering and consumers

`MapSignals` reads same-tick map activity and writes its fields; consumers
read on a declared lag so same-tick ordering cannot create feedback loops:

- `LowAltitudeVerbs` reads the *previous* tick's signals in verb weighting
  (an entity under threat weighs flight and defensive verbs differently).
- `Effects` reads same-tick signals through the `MapSupportAtLocation`,
  `MapThreatAtLocation`, and crowding effect conditions (attribute drains and
  shelter bonuses keyed on local pressure).

Nothing else consumes signals today. Generator targeting, placement, and the
culture systems do not read them. Before adding a new consumer, prefer the
question "should this read the culture field instead?" — culture mass is the
durable spatial memory; signals are the fast-decaying present tense.
