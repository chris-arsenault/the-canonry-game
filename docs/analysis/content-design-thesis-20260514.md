# Next Generator / Gambit / Verb Round

This note captures the design goals and hypotheses from the recent Monte Carlo
work. It is not a tracker and it is not a work queue. The purpose is to preserve
the current content-design thesis before the next authoring round for Ice
Remembers generators, gambits, and low-altitude verbs.

Evidence comes from recent commits, the Monte Carlo registry database in
`.artifacts/monte-carlo/registry.duckdb`, the deleted follow-up tracker in git
history, and `the-road-problem.md`.

## Core Thesis

The runtime is now strong enough to expose the next problem: the authored
catalog is too narrow and too unevenly connected. The next round should mostly
author better causal ladders, not add more selection machinery.

Low altitude should make the world alive and prepare existing state. Gambits
should create significant graph topology. Generators should condense that state
into new named entities and relationship bundles. The catalog should make those
layers reinforce each other repeatedly over long runs.

## Design Goals

High-altitude content should be broader, earlier, and more causal. The active
catalog entered this round with only five generators and six gambits. It now has
thirty-one generators and twenty gambits, with monster, orca-transport,
orca-route-war, stolen-artifact hunt, haunted-transport, named-war,
trade-conference, cleansing, conduit-network, reefhold-then-depths, colony, and
ward-order loops tied to documented causal rows rather than one-off targets.
Earlier evidence runs showed that once trade routes entered the world,
`routes_draw_readers` could dominate while `ice_settles_accounts`,
`what_the_spring_remembers`, and `what_the_artifact_knows` stayed at zero. The
current tuned baseline addresses that specific absence, but the lesson remains:
distribution problems usually point to content-graph shape, not just cadence.

Early generators should be minor world-condensation events rather than more
pulse relaxation. The pulse-window runtime now lets generators fire any tick
inside a window, and early fire coverage improved in
`low-altitude-placement-validation-10x500`. The remaining goal is authored
startup stability: small, low-impact events that can usually find a real cause
in the seed state and create useful named entities or provenance hooks.

Gambits should create topology that generators can later consume. Low-altitude
verbs may move entities, project influence, and change strength on existing
relationships, but they should not create relationship topology. A generator or
gambit precondition that requires a new relationship is satisfied by another
gambit or generator, not by a low-altitude affordance.

Gambit preconditions should use hard gates only for facts that must be true.
Preferences such as distance, culture, settlement viability, influence amount,
or route shape should usually be scores. The trade-route bootstrap was fixed by
removing brittle distance and shared-influence gates and replacing them with
scoring pressure. That lesson should guide the next catalog work.

Relationship strength should be a real dynamic quantity over long simulations.
It should have visible sources and sinks, and ordinary relationship-strength
verbs should matter against passive maintenance. Strength is not a replacement
for relationship topology: low altitude can maintain or weaken existing edges,
but only gambits and generators should create or remove meaningful graph edges.

Affordances should exist only where a low-altitude verb can actually help.
Movement affordances point at locations or entities that can be approached.
Influence affordances point at places where influence can be grown or reduced.
Relationship-strength affordances point at existing relationships whose
strength can be changed. Relationship-creation and terrain-modification
pressure should not compile into low-altitude affordances because low altitude
cannot perform those actions.

Semantic roads need repeated convergence, not just more semantic movement. The
semantic plane now has first-class terrain and semantic movement volume is no
longer the obvious gap. The road problem is that movement traces smear into weak
blobs or collapse into tiny entity-adjacent patches instead of forming shared
anchor-to-anchor corridors.

Physical and semantic placement should remain terrain-aware content pressure.
Generated entity placement now lands on authored preferred physical terrain at
high rates, which means placement profiles are a useful authoring lever. Future
content should treat placement and terrain preference as part of the meaning of
the entity bundle, not as incidental spawn coordinates.

Influence verbs should be purposeful rather than numerous. The current catalog
contains many influence verbs, and the mechanism itself is directionally
correct, but near-duplicate influence verbs can drown out movement,
relationship maintenance, and setup behavior. Influence should matter because
specific content reads it.

## Content Hypotheses

Minor early generators will stabilize the first simulation pulses better than
more runtime cadence changes. The likely shape is small events with broad seed
targets, causal repeat guards, and modest outputs: a named witness appearing
near a meaningful place, a small claimed feature, a low-grade practice lineage,
or a local aftermath entity that gives later gambits something to read.

The next generator set should create several causal ladders instead of a few
isolated set pieces. A useful ladder has a gambit or existing seed fact that
creates topology, low-altitude verbs that maintain or erode strength and
locality, and a generator that spends or transforms the condition into new
named entities. This shape avoids both inert generators and generators that fire
repeatedly without changing their cause.

Semantic road formation will improve only if semantic movement has stable
destinations. Physical movement already has partial settlement convergence.
Semantic movement should converge around durable semantic anchors where the
domain supports it, especially penguins, artifacts, abilities, points of
interest, and other entities that can plausibly become conceptual landmarks.
The goal is shared travel through semantic space, not artificial road-building.

Trade-route content should aim for network shape through scoring and outputs.
The trade-route bootstrap is no longer blocked, but route topology can still
become too permissive. Future trade content should bias toward nearby links,
same-culture links, intermediate harbors, brokers, transports, orders, and
route-spend/reinvestment loops so the end state resembles a trade network
instead of disconnected long lines.

War, cleansing, spring, and memorial content need broader upstream and
downstream paths. `artifact_cleanses_scar` and `what_the_spring_remembers`
showed that terms matter: `cleansed_by` is durable provenance and
`memorializes` is the repeat guard. The next content should preserve that
meaning while giving the chain more ways to occur and more consequences after
it occurs.

Artifact and ability content should create more possession, practice, conduit,
ward, and memory topology. `what_the_artifact_knows` depends on held artifacts
and absence of a home base, but recent MC did not exercise it. That suggests the
catalog needs stronger paths into and out of `holds`, `practiced_by`,
`conduit_of`, `ward_against`, and related relationship-strength verbs.

Second-wave kinds need actual high-altitude hooks. Orcas, points of interest,
abilities, transports, occurrences, and monsters should not exist only as
low-altitude activity. Monsters now have a spawning path and a binding gambit,
so the next useful question is whether their ward and corruption edges produce
interesting aftermath. The design goal is not to force every kind into every
system, but each kind should have at least one real causal role in the
generator/gambit catalog before its low-altitude activity is judged.

Influence should become a selective precondition, not a universal behavior tax.
When a gambit or generator really cares about territorial pressure, influence is
appropriate. When the desired shape is contact, memory, possession, lineage, or
trade viability, relationship strength, locality, culture, and route topology
are often better signals.

Terrain requirements in high-altitude content should mean locality context, not
terrain creation affordances. `war_breaks_the_land` can read nearby wastes and
then transform terrain as a generator effect. A low-altitude verb cannot satisfy
that by deciding to create wastes. Content should preserve this layer boundary.

`anchored` should remain a physical or material binding term, not a synonym for
"near" or "relevant". Gambits should prefer explicit locality preconditions
when locality is the actual requirement. Generators may create `anchored` when
an artifact, settlement, region, or other entity is materially bound to a
place.

## Authoring Implications

Generator authoring should favor event-shaped bundles: named entities,
relationships, placement, and a causal limit that spends, transforms, or locks
the condition that enabled the event. A generator whose best description is
"make another entity of kind X" is probably unfinished.

Gambit authoring should be strict about layer semantics. If an actor is doing a
meaningful action that changes relationship topology, state, or terrain, it is
a gambit. If the content only needs ordinary movement, influence, or strength
maintenance, it belongs in low altitude. If a preference is probabilistic or
topological, it should normally be a score term rather than a hard gate.

Low-altitude verb authoring should emphasize distinct simulation roles. A verb
should be kept because it creates movement convergence, existing-relationship
strength movement, meaningful influence shape, or another readable signal that
high altitude can use. Similar influence verbs across many kinds should earn
their place by producing different outcomes.

Placement authoring should describe natural habitat and semantic fit for both
seed and generated entities. Physical and semantic maps are analogous, not
coordinate copies: the same q/r on each plane has no direct meaning. Content
should use each plane's terrain definitions as local context for where an
entity naturally resides.

Affordance authoring should follow domain demand rather than a coverage quota.
It is acceptable for a shape such as influence or terrain to be quiet when no
active content genuinely asks low altitude to satisfy it. The goal is not equal
coverage by affordance type; the goal is for real generator and gambit needs to
produce actionable low-altitude pressure.

## Evidence Snapshot

`low-altitude-placement-validation-10x500` showed the current engine can carry
affordance pressure and terrain-aware placement: `26.80%` selected affordance
rows, `21.25%` opportunity-bucket selections, `485` selected
relationship-strength rows, `0` selected influence rows, `0` selected terrain
rows, `180` generator fires, and about `92%` preferred physical placement for
generator outputs.

`semantic-radius-mapradius-10x500` showed the semantic plane radius issue was
fixed and semantic movement was active, but semantic roads were still weak:
semantic max edge travel was `19` versus physical `106`, semantic terrain-target
movement intents selected were `0` versus physical `261`, and semantic light
road cells averaged only `0.2`.

`movement-cost-experiment-10x500` showed road-speed and movement-cost tuning
alone did not create a road network. Meaningful road cells were tiny local
patches, semantic `quality > 25` road nodes averaged `0`, and physical high
quality was dominated by repeated local edge travel rather than shared
highways.

`semantic-placement-telemetry-10x2500-v2` showed long-run high-altitude output
was narrow: `routes_draw_readers` fired `879` times, `war_breaks_the_land`
fired `101` times, while `ice_settles_accounts`,
`what_the_spring_remembers`, and `what_the_artifact_knows` fired `0` times.
That is the clearest evidence that the next round needs broader causal content.

`trade-route-weighted-10x500-20260505` showed the value of replacing brittle
hard gates with scores: the settlement trade gambit produced hundreds of
attempts and dozens of successes after physical distance and shared-influence
requirements were removed as preconditions.

`authoring-distribution-tune-10x5000-20260514` is the current balance baseline:
10/10 runs reached 200 generator fires with `map_degenerate=0`. Generator fires
spanned `17` to `160` per generator (`9.41x`), gambit successes spanned `53` to
`507` per gambit (`9.57x`), and `corruption_wakes_monsters` fired `33` times.
The intertwined loops are active at long-run scale: `orca_route_wars_get_names`
fired `37` times, `stolen_artifacts_draw_hunters` fired `160` times,
`penguin_slays_monster` succeeded `107` times, and
`monster_slayings_become_festivals` fired `102` times.

## Non-Goals

Do not revive the removed process deck, generic low-altitude planner, or
relationship-creation affordances.

Do not treat lifecycle collapse as a standalone tuning target before improving
generator and gambit activity. More meaningful creation and topology is the
expected first fix.

Do not force equal affordance coverage across relationship target, relationship
strength, influence, terrain, and presence. Coverage should follow actual
domain content.

Do not solve semantic roads by directly painting roads or by making semantic
terrain mirror entities and relationships. Roads should remain movement-derived;
semantic terrain should remain a mostly static conceptual backdrop.

Do not loosen terms until they lose meaning. `cleansed_by`, `memorializes`,
`anchored`, `holds`, `trade_route`, and `at_war_with` should stay semantically
specific because the simulation depends on those edges being readable.
