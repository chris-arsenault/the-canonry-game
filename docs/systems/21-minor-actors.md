# System 21: Minor Actors

Minor actors are persistent, low-salience worker cohorts. They make cultural
fronts, roads, and campaigns spatially visible without adding more named
entities to the graph.

## Contract

Minor actors are:

- unnamed fixed-capacity kernel actors with generation-safe occurrence handles;
- created from an active named source and owned by that source;
- persistent across ticks and assignments;
- culture-bearing through their source entity;
- spatially indexed by current map cell;
- represented as cultural fronts, road crews, or campaign columns; and
- visible as active map actors as well as through primitive telemetry.

Minor actors are not:

- graph entities or relationship endpoints;
- culture slot occupants;
- generator, gambit, or chronicle targets;
- an aggregate culture field that expands on its own;
- a hidden population or economy simulation; or
- emitted disposable workers with TTLs and per-source caps.

## Tick and Storage

The simulation advances one complete ordinary opportunity at a time.
`MinorActors`, movement, relationships, effects, gambits, chronicles, and
generators each run once per tick. There is no worker-only cadence, second
clock, or internal substep.

Actor state uses direct fixed-capacity arrays. The live set is dense and stable
by actor occurrence, retired slots are reusable, and a direct cell-linked index
supports local encounters. Source/rule and campaign-relationship indexes
enforce one ordinary actor per source rule and one campaign column per active
campaign relationship. Snapshots persist only active actor state and rebuild
free, source, relationship, and cell indexes directly; no emitter, deadline,
TTL, or legacy snapshot shape remains.

Each actor stores its source and optional relationship occurrence, role,
culture, plane, current and previous cell, target, targeting mode, posture,
strength, per-tick work charge, commitment deadline, and next replan tick.
Stage worker bonuses and culture-slot leverage increase actor strength. They do
not multiply actor count.

## Assignment Lifecycle

An eligible source creates an actor only when a valid task exists. The actor
keeps that assignment until it becomes invalid or both its replan and
commitment deadlines are due. It moves by one authored macro stride, resolves
same-cell and adjacent-cell encounters, and spends its work charge at the
assigned cell. It does not run hidden intermediate time steps.

Actors retire when:

- their source occurrence is invalid or no longer matches the source rule;
- a campaign relationship occurrence ends or changes;
- no valid assignment remains; or
- the source entity retires.

Completing or invalidating one task normally triggers bounded reassignment.
There is no ordinary per-tick spawn/expire churn.

## Roles

### Cultural fronts

Every current entity kind can author a front on the plane complementary to its
primary mobility. A front chooses a supported border extension, a bridge or
hole repair, or a contested cell within the source radius. It carries culture
from its named source and can reduce foreign mass before adding its own.

Cultural fronts do not reinforce already controlled interior cells. Ambient
stage emission and districts own stable interior support. Culture therefore
does not spread through an autonomous field process, but the worker layer also
does not recreate the former interior-maintenance treadmill.

### Road crews

Transport sources can author a physical road crew. It selects a used road cell
whose quality has degraded, travels there, and reinforces it according to
current work strength and knowledge modifiers. When the segment is healthy or
demand disappears, it replans or retires.

### Campaign columns

Faction campaign rules bind one column to each active authored campaign
relationship. A column advances from friendly support toward the target
frontier, contests foreign culture, and projects source-owned culture. It
retires when the relationship or either endpoint becomes invalid.

## Local Encounters

Actors inspect only their current and adjacent cells through the actor-cell
index. A keyed random decision can produce:

- friendly support that raises both actors' available work;
- a hostile clash that reduces both actors' strength; or
- stronger campaign disruption when either actor is a campaign column.

An exhausted actor becomes disrupted, drops its assignment, and must recover
and replan. Each unordered pair is resolved once. There is no global all-pairs
pass and no traversal-order random stream.

## Events, Projection, and Telemetry

Primitive kernel events publish actor creation, assignment, movement, work,
encounter, and retirement. Projection keeps active actors keyed by occurrence
handle and a separate bounded trail of recent work. Cultural fronts, road
crews, and campaign columns use distinct map markers; recent work remains a
short visual effect rather than the source of actor identity.

Monte Carlo output exports `minor_actor_work_events` (role, targeting,
boundary kind, strength, applied delta). The lifecycle and encounter
telemetry lanes were deleted 2026-08-03 (no reader); the kernel events
remain for UI projections.

Analysis should lead with work by role and boundary kind, cultural shape,
and road and campaign outcomes.
