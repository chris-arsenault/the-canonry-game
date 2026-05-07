# Generators

Generators are the high-altitude layer of the simulation: world events that
intermittently bring **new named entities** into being. They are creation, not
an aggregate release valve. The deck of generators the player runs is a
statement about what kinds of worlds are allowed to come into being.

This document is the canonical generator design: system rules, authoring
invariants, and the current catalog. Implementation is in
`src/Engine.Kernel/Programs/Generators/` and the IceRemembers domain
content is in `src/IceRemembers/IceRemembersGeneratorDefinition.cs`.

## What generators are

A generator is a **world event that spawns a pre-wired package of entities
inside a pulse-window budget**. The runtime evaluates generators every tick and
fires an applicable generator while the current pulse window still has fire
budget. Generators are the mechanism by which the simulation introduces new
actors, factions, settlements, and artifacts over time, rather than starting
with a fixed census and only losing pieces.

A generator is not just "spawn a penguin" — it spawns **a bundle** (entities +
relationships + placement), because the interesting narrative units are
relational: a settlement with a founding leader who belongs to a faction,
not three lonely pieces.

### Simulation altitudes

* **Low altitude** — continuous simulation of named entities already in the
world: movement, influence, map signals, relationship maintenance, health,
aging, and death.
* **Mid altitude** — systematic change in the graph: wars hardening,
alliances fraying, corruption spreading, sanctity stabilizing.
* **High altitude** — generators. Major world events that intermittently
bring new named entities into being.

The world is much richer than the named entities we track. Generators are
how that richer implied world occasionally condenses into new named
things. A named entity is not necessarily being born — it is becoming
important enough to enter the simulation.

## Anatomy of a generator

Each generator names:

* **A target kind** — what kind of existing entity it looks at to decide
"am I applicable here right now?" (e.g. Region, Settlement, Faction).
* **One or more dynamic preconditions** — relationships, relationship
strength, subtype filters, locality, health, traits, or other current-world
conditions that gate applicability. Static non-health attribute dependence is
intentionally not part of the active model.
* **A produce list** — entities that get spawned, each with a role and
optional create-chance.
* **A relationship list** — how produced entities wire to each other and
to the target (and optionally to a secondary existing target).
* **A pulse weight** — relative likelihood this generator wins the pulse
lottery.
* **(Optional) Non-creation consequences** — additional effects on existing
entities or world state when those effects are part of the event's
meaning.
* **A `CausalLimit` declaration** — one plain-language sentence naming the
world condition that enables repeat firing and the spend, transform, or lock
that prevents immediate repetition.

Placement is automatic but terrain-aware. Produced entities receive independent
physical and semantic coordinates. Each plane can use locality placement or an
area search around an anchor point, and each produced entity can author
preferred and allowed terrain. Allowed terrain is a hard filter; preferred
terrain biases scored candidates after locality, radius, occupancy, semantic
bounds, and coverage constraints pass.

## Pulse model

* The generator runtime evaluates every tick.
* A pulse window resets fire budget. Default tuning is
`PulseWidthTicks = 25` and `FiresPerPulse = 1`.
* A generator may fire on any tick inside the current pulse window while
fire budget remains.
* Applicable = on the tableau AND has at least one target entity whose
gates pass.
* Within available fire budget, one eligible generator candidate is selected
weighted-random by `PulseWeight` — NOT by how many targets it has. Otherwise a
generator targeting settlements would crowd out one targeting factions just
because there are more settlements.
* If a selected generator cannot legally place its produced entities, that fire
fails cleanly and does not consume the pulse window's fire budget.
* Tableau-gating is the whole point: remove the generator's card → it
stops being applicable, even if world state would otherwise trigger it.
This is "your deck defines the world's nature" in action.

## Hard invariants (build-time enforcement)

Authoring violations fail the build, not the runtime.

1. **Every generator has at least one target gate.** No "fires randomly
out of nothing" — the world must have a reason. The active catalog uses
relationship, terrain, and ordinary world-state gates rather than non-health
attribute thresholds.
2. **Target gates match target kind.** You can't target Regions and gate
off an unrelated Settlement-only condition.
3. **The generated graph stays connected to the existing world.** The
target exists to guarantee that a generator fires into the current
world rather than producing an isolated mini-graph.
4. **No orphan produces** (with a carve-out for Artifacts). Every produced
entity must be referenced by at least one relationship. If it isn't,
you're dropping a loose entity into the world with no story attached.
5. **Optional produces can't be anchors.** If `CreateChance < 1.0`, the
entity can be referenced by at most 2 relationships. Otherwise
"skipping" that entity silently orphans chains of others.
6. **No cyclic culture inheritance.** `InheritCultureFrom = "$other"` can't
form a cycle — somebody has to be the root.
7. **Culture must resolve to a real value.** No silent fallback. If a
produce declares `InheritCultureFrom = "$target"` and the target has
no culture, the fire hard-fails. ("none" counts as real for Regions,
which legitimately have no culture.)
8. **Repeated firing is limited causally.** A generator must spend, relieve,
transform, or lock the world condition that made it eligible. Adding a
second target is not enough unless firing changes that target or the original
cause. Each generator declares this as `CausalLimit`. See
[generator causal limits](../systems/20-generator-causal-limits.md).
9. **Generators create a connected bundle of new named entities.**
Relationship mutation on existing entities is allowed but secondary.
If the main thing the content does is mutate existing graph state, it
is probably not a generator — it's an action.
10. **The starter generator set must be satisfiable from seed state.** A
generator cannot depend on relationships that do not exist in the
seed unless some earlier generator in the active set can create them.

## Runtime preconditions

* Target entity must be `Active` (not Historical).
* Target subtype must match if filter present.
* Required target relationships must exist.
* Required relationship strength thresholds must pass when authored.
* `MaxRunsPerSim` not exceeded.
* Placement constraints must have at least one valid physical and semantic
  candidate for every produced entity.

Relationship strength should be the default numeric gate. For example, a
generator or gambit around cleansing should require a strong relevant
relationship such as `anchored`, `holds`, or `cleansed\_by`, not an invented
`power` or `pristiness` scalar. Health thresholds and locality queries remain
valid when they describe the real condition.

## Soft warnings (heuristics, not blockers)

Authoring guidance that we think is right but won't fail a build over.

1. **There is an optimal connectedness, and it is mid.** Too few
connections lock targets into the same repeated patterns. Too many
make graph targeting feel effectively random because everything
touches everything.
2. **Secondary attachment is allowed, but should earn its keep.** A
generator may connect to more than one existing entity when that makes
the event more specific, more legible, or more grounded. Bad when it
only increases edge count.
3. **Subtype coverage matters at the catalog level.** The generator
catalog should exercise entity subtypes, not only broad kinds. If the
catalog ignores subtypes, subtype choice collapses into flavor text.
4. **Cross-kind creation is usually more interesting than same-kind
echo.** A world event around one kind should often cause other kinds
to become named.

## Authoring principles

1. **Generators are narratively interesting, not just graph-interesting.**
A good generator represents something that happens in the world:
schism, war outbreak, relic corruption, pilgrimage, a hero sacrificing
themselves to end a war. If the best description is only topological,
the generator is not finished.
2. **Generators are event-shaped, not census-shaped.** They should not
default to agency bundles just because agency relationships are the
easiest things to wire. Place-centered, artifact-centered,
lineage-centered, and historical-state events should be first-class.
3. **Generators may leave consequences beyond what they create.** Some
events deserve additional effects on existing world state — a
corruption that scars the surrounding region, a sanctification that
cleanses an institution. These are part of the event's meaning, not
a separate hidden action system.

## Subtypes

Current subtype families per kind. The rule: a subtype represents a real
authored role in the world, not a narrow one-off flavor label, and the set
supports the active generator/gambit catalog.

|Kind|Current subtypes|Rationale|
|-|-|-|
|`Penguin`|`Warden`, `Broker`, `Witness`, `Exile`|Guardians, negotiators, memory-keepers, and displaced figures.|
|`Faction`|`Order`, `Compact`, `Cell`, `Choir`|Durable institutions, survival compacts, small operational groups, and devotional organizations.|
|`Settlement`|`Rookery`, `Colony`, `Holdfast`, `Sanctuary`, `Harbor`, `Reefhold`|Direct size levels (`Rookery`/`Colony`) plus durable role-based forms: hardened survival, sacred refuge, trade-facing edge, intentionally built offshore.|
|`Artifact`|`Relic`, `Instrument`, `Weapon`, `Tome`, `Seal`|Sacred inheritances, operational instruments, weapon-objects, dangerous written artifacts, containment/closure objects.|
|`Region`|`Face`, `Shelf`, `Depths`, `Reach`, `Sea`, `Scar`|Structural position in/around the Berg: exposed outer face, held ice, deep interior strata, opened margin, open water, wounded ground.|
|`Orca`|`Matriarch`, `Bull`, `Scout`, `Elder`|Wake-Singer leadership, force, reconnaissance, and old-memory roles.|
|`PointOfInterest`|`Vent`, `Peak`, `Bloom`, `Shoal`|Named natural features smaller than regions and not built like settlements.|
|`Ability`|`Technique`, `Rite`, `Craft`|Combat techniques, magical rites, and technological practices.|
|`Monster`|`Leviathan`, `Wraith`, `Beast`, `Revenant`|Different forms of bound or emerging peril; sealed/unbound/risen remains status, not subtype.|

### Coverage read

* Strong: `Settlement`, `Artifact`, `Region` — the active set exercises
`Holdfast`, `Reefhold`, `Scar`, `Sea`, `Relic`, `Instrument`, and `Seal`.
* Weaker: `Penguin` and `Faction` coverage is intentionally partial in the
current catalog.

Stable subtype families to design against matter more than forcing full
generator coverage from a small catalog.

## Catalog: current generators

The active Ice Remembers catalog currently contains five relationship-driven
generators:

* `ice\_settles\_accounts`
* `what\_the\_spring\_remembers`
* `war\_breaks\_the\_land`
* `routes\_draw\_readers`
* `what\_the\_artifact\_knows`

These start from graph state the simulation already produces, such as
`holds`, `at\_war\_with`, `trade\_route`, and `cleansed\_by`, then create
cross-kind bundles. Target subtype filters are broad in the active catalog
because subtype coverage is still sparse.
