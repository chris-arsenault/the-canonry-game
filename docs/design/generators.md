# Generators

Generators are the high-altitude layer of the simulation: world events that
intermittently bring **new named entities** into being. They are creation, not
an aggregate release valve. The deck of generators the player runs is a
statement about what kinds of worlds are allowed to come into being.

This document is the generator design: system rules and authoring invariants.
The active catalog is source code, not a parallel table here. Implementation is in
`src/Engine.Kernel/Programs/Generators/` and the IceRemembers domain
content is in `src/IceRemembers/IceRemembersGeneratorDefinition.cs`.

## What generators are

A generator is a **world event that spawns a pre-wired package of entities
inside a fire-window budget**. The runtime evaluates generators on the main
program cadence and fires an applicable generator while the current window
still has fire budget. Generators are the mechanism by which the simulation
introduces new actors, factions, settlements, and artifacts over time, rather
than starting with a fixed census and only losing pieces.

A generator is not just "spawn a penguin" — it spawns **a bundle** (entities +
relationships + placement), because the interesting narrative units are
relational: a settlement with a founding leader who belongs to a faction,
not three lonely pieces.

### Simulation altitudes

* **Low altitude** — continuous simulation of named entities already in the
world: movement, culture influence, minor actor labor, map signals,
relationship maintenance, health, aging, and death.
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
* **A fire weight** — relative likelihood this generator wins selection within
the current window.
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

## Fire-window model

* The generator runtime evaluates on the main-program cadence.
* A provincial fire window resets fire budget. Default tuning is
`FireWindowTicks = 50` and `FiresPerWindow = 1`; runtime deadlines use the
single global tick clock.
* A generator may fire on any eligible scheduled tick inside the current
window while fire budget remains.
* Applicable = on the tableau AND has at least one target entity whose
gates pass.
* Within available fire budget, one eligible generator candidate is selected
weighted-random by `FireWeight` — NOT by how many targets it has. Otherwise a
generator targeting settlements would crowd out one targeting factions just
because there are more settlements.
* If a selected generator cannot legally place its produced entities, that fire
fails cleanly and does not consume the fire window's budget.
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
|`Transport`|`Trade`, `Battleship`, `Scout`, `Citadel`|Named mobile assets: route carriers, escalation vessels, reconnaissance craft, and fortified moving ground.|
|`Occurrence`|`War`, `TradeConference`, `Plague`, `Festival`|Named active events with momentum: conflict, negotiation, loss, and collective attention while they are still happening.|

### Coverage read

* Strong produced coverage: `Artifact`, `Monster`, `Occurrence`, and
`Transport` all have multiple generated subtypes in active generators.
* Corrected second-wave coverage: `Orca`, `PointOfInterest`, `Ability`,
`Monster`, `Transport`, and `Occurrence` are not only targets; each kind is now
produced by at least one generator.
* Third-pass coverage: `Settlement`, `Region`, and `Faction` now have generated
rows for `colony`, `reefhold`, `depths`, `choir`, and `order`. These rows are
tied to cleansing, conduit, and ward topology rather than broad kind targeting.
`cell` and `compact` remain lighter because they still mostly enter as optional
outputs on older rows.
* Weaker by design for now: `Penguin` remains a frequent witness/actor payload;
new passes should avoid using penguins as the default answer to every new cause.

Stable subtype families matter, but the catalog must prove that a subtype can
enter the world through a causal loop. Kind participation alone is not coverage.

## Catalog: current generators

The active Ice Remembers catalog currently contains twenty-six relationship-driven
generators:

* `ice\_settles\_accounts`
* `what\_the\_spring\_remembers`
* `war\_breaks\_the\_land`
* `routes\_draw\_readers`
* `what\_the\_artifact\_knows`
* `unclaimed\_places\_draw\_wardens`
* `practiced\_forms\_find\_tomes`
* `charters\_find\_harbors`
* `vigils\_leave\_relics`
* `corruption\_wakes\_monsters`
* `wars\_get\_names`
* `trade\_routes\_convene\_negotiation`
* `claimed\_waters\_draw\_bulls`
* `conduits\_map\_hidden\_peaks`
* `mapped\_peaks\_birth\_techniques`
* `controlled\_depths\_wake\_leviathans`
* `overclaimed\_blooms\_draw\_beasts`
* `corrupted\_monsters\_raise\_revenants`
* `war\_occurrences\_launch\_scouts`
* `trade\_conferences\_raise\_citadels`
* `conferences\_become\_festivals`
* `cleansed\_springs\_found\_sanctuaries`
* `sanctuaries\_anchor\_colonies`
* `conduit\_points\_found\_reefholds`
* `deep\_conduits\_name\_depths`
* `warded\_monsters\_found\_orders`

These start from graph state the simulation already produces, such as
`holds`, `at\_war\_with`, `trade\_route`, `cleansed\_by`, `claimed\_by`,
`practiced\_by`, `native\_to`, `corrupted\_by`, `ward\_against`, and
`memorializes`, then create cross-kind bundles. Target subtype filters and
strong relationship gates are used where they carry mechanical meaning: scars
wake wraiths, controlled blooms and shoals wake leviathans, mapped shore-peaks
and vents produce techniques or conduit settlements, named occurrences launch
transports or festivals, cleansed regions found sanctuaries, conduit points
first found reefholds and then reefhold-founded conduit points name depths, and
warded monsters produce containment orders.
