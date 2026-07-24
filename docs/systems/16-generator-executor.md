# System 16: Generator Executor

Generators are high-altitude event bundles. They create new named entities and
relationships when the world state satisfies their preconditions.

## Purpose

The world is richer than the named entities the sim tracks. A generator fires
when background world activity becomes important enough to name. "Create" means
the entity enters the named simulation, not necessarily that it was born from
nothing.

Without generators, lifecycle and relationship drift eventually wind the world
down. With generators, the player-selected generator set determines what kinds
of history the world can produce.

## Bundle Shape

A generator can:

- select one or more existing targets;
- resolve or create target-like produced entities when configured to do so;
- create named entities;
- create relationships among targets and produced entities;
- apply additional effects.

Generated entities should usually form a connected bundle. Every generator
should introduce relationships that the rest of the graph understands.

Each produced entity declares its culture source: either `InheritCultureFrom`
(an entity ref, usually `$target`) or an authored fixed `Culture`. Fixed
culture enables cross-cultural generators — one culture's activity creating
another culture's entities, such as the Nightshelf shadow economy growing
under Aurora trade hubs. Cross-cultural outputs typically pair with
`HostileIntrusion` culture placement so the own-culture support gate does not
reject placement inside the host culture's territory.

## Placement

Generators place entities on the physical and semantic planes. Placement may be
near a target, across the map, inside a region, or based on other authored
placement strategies.

Placement uses the same plane-aware kernel placement model for physical and
semantic coordinates. Area placement searches an authored radius band around a
point; locality placement searches inside the target locality. The old
semantic/physical shortcuts are not separate placement systems.

Beyond area and locality, two placements read the map's own structure.
**Frontier** placement lands the produced entity on known free ground just
past its culture's leading edge, so it becomes the anchor the next push
starts from — the founding ratchet. **FeatureAnchor** placement lands it on
the nearest revealed, unoccupied anchor of a persisted geological feature
(river systems, lakes, peaks, craters), filtered by an authored feature
kind; the entity's occupancy of the anchor is the claim, so exhausted
features fall out of candidacy on their own.

Produced entities can author preferred and allowed terrain per plane. Allowed
terrain is a hard filter; preferred terrain biases scored candidates after
locality, radius, occupancy, semantic bounds, and coverage constraints pass.
Physical placement also scores discrete site flags (summit, thermal, anomaly,
polynya, crater), signed warmth/fire/aurora/pressure band affinities over the
latent facts, and the kind and culture placement taste axes.
For physical regions and coverage, placement must respect terrain and named
coverage rules. For semantic placement, entities are placed within the bounded
semantic plane against semantic terrain.

If no legal placement candidate exists, the generator fire fails cleanly and
does not consume pulse budget. This keeps invalid placement from suppressing
later valid fires inside the same pulse window.

Pulse scheduling is provincial. Every revealed physical superregion
registers as a province with its own pulse clock; the tempo comes from the
province's settledness band, derived from covered cells within it (dormant
ground has no clock, frontier runs slow, settled faster, heartland at full
tempo — width multipliers and thresholds are tuning options). While a
province's pulse selects and fires, only targets standing inside that
superregion are eligible; secondary bindings stay unrestricted, so
cross-province relationships still form. Cadence generators keep their own
world-wide clocks outside the provincial pool. World creation throughput is
therefore the sum of awake provincial clocks and grows as territory is
revealed and settles.

A fire is atomic over its produced bundle. The executor first plans every
produced entity — chance roll, query binding, subtype, culture, placement
coordinates, and coverage cells — without mutating world state, then commits
the creations only after the whole bundle resolves. A failed fire therefore
leaves nothing behind: no entities, no events, no historical placeholders.
During planning, later produced entities see earlier planned ones through
pending references: their planned cells count as occupied, their planned
coverage counts as claimed, and they can serve as placement anchors and
culture sources.

Generator culture placement uses the kernel culture-boundary model. Normal
placement prefers same-culture support and avoids another culture's
entrenched/nexus fields. Hostile intrusion is an explicit placement mode for
content that should appear in a foreign cultural field; it is not the default
for wilderness or ordinary across-map placement.

Culture placement intent is authored per produced entity. The default
`PreferInheritedCulture` is a soft same-culture bias. `CultureInterior`,
`CultureFrontier`, and `CultureRoute` express stronger geography preferences
against the same culture-boundary field; `Wilderness` prefers low-culture cells;
`HostileIntrusion` bypasses the normal foreign-entrenchment rejection. These
intents affect both final placement scoring and the weighted selection of
eligible generator targets, so culturally supported loci are more likely to
receive generator activity before placement begins.

Region coverage created by generators uses the same culture placement intent as
the generated owner. Coverage seed and patch selection still respect terrain and
coverage rules, but supported same-culture cells are preferred and foreign
entrenched fields are rejected unless the produced entity explicitly uses
hostile intrusion.

## Pulse Budget

Generators run on the main graph cadence, not every minor-actor tick. Fire
budget is reset by pulse windows. The default tuning is `PulseWidthTicks = 25`
main ticks and `FiresPerPulse = 1`. With the current
`MinorActorMainTickRatio = 5`, the default pulse width spans `125` simulation
ticks. A generator can fire on any eligible generator tick inside the current
pulse window while budget remains, instead of only on an exact modulo boundary.

When multiple generators are eligible, selection is weighted by authored pulse
weight. Fire budget limits how many successful fires can occur in one window;
it does not increase the low-altitude tick frequency.

Generators with `CadenceTicks > 0` are timeline generators: they leave the
shared pulse pool entirely and fire on their own jittered cadence
(`CadenceTicks ± CadenceJitterTicks`), outside the pulse budget. Target
eligibility still applies — a cadence firing with no eligible target passes
quietly. Timelines give each culture an authored drumbeat that does not scale
with its population, without steering outcomes toward equality.

## Preconditions

Generator preconditions should be satisfiable from the seed or from other
generators. If a precondition requires a relationship or kind that no seed or
generator can create, the generator is dead content.

Generators should avoid self-propagating infinitely through the same target.
Repeat control should be expressed as a causal limit: the generator must spend,
relieve, transform, or lock ordinary world state that made the fire eligible.
See [System 20: Generator Causal Limits](./20-generator-causal-limits.md).

If a precondition needs a numeric threshold, use relationship strength as the
default. For example, require an artifact to have a strong `anchored` or
`holds` relationship before it can cleanse a scar, rather than inventing an
artifact `power` attribute. Health thresholds remain valid for survival,
damage, decay, and repair. Locality-based queries are acceptable when nearby
map state, semantic position, or regional containment is the real gate.

Generator target gates, relationship gates, terrain gates, and produced entity
binding queries use the shared kernel query path.
Generator-specific code still owns pulse budgeting, target choice, production
orchestration, placement, and reference binding order.

Secondary binding queries may require the candidate to be in the same physical
culture component as the target. This is stricter than same-culture matching and
is intended for local patrons, ports, hosts, or callers whose support should be
physically grounded in the same cultural mass.

## Effects

Generator-created entities, relationships, status changes, coverage changes,
and cleanup effects must execute through the shared kernel effect path. If a
generator needs a new world mutation, add the `Simulation` mutation and shared
effect wrapper before exposing it to authored generator data.

## Boundary

Low altitude creates conditions. Gambits create high-stakes entity actions and
relationship changes. Generators create new named entities and major event
bundles.
