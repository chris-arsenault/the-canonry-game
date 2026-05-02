# Generators

Generators are the high-altitude layer of the simulation: world events that
intermittently bring **new named entities** into being. They are not pressure
release; they are creation. The deck of generators the player runs is a
statement about what kinds of worlds are allowed to come into being.

This document is the canonical generator design — system rules, authoring
invariants, current catalog, and open questions. Implementation is in
`src/Engine.Kernel/Programs/Generators/` and the IceRemembers domain
content is in `src/IceRemembers/IceRemembersGeneratorDefinition.cs`.

## What generators are

A generator is a **world event that spawns a pre-wired package of entities
on a pulse**. Each pulse, the system picks one applicable generator and
fires it. They are the mechanism by which the simulation introduces new
actors, factions, settlements, and artifacts over time, rather than
starting with a fixed census and only losing pieces.

A generator is not just "spawn a penguin" — it spawns **a bundle** (entities +
relationships + placement), because the interesting narrative units are
relational: a settlement with a founding leader who belongs to a faction,
not three lonely pieces.

### Simulation altitudes

- **Low altitude** — continuous simulation of named entities already in the
  world: movement, influence, map signals, relationship maintenance, health,
  aging, and death.
- **Mid altitude** — systematic change in the graph: wars hardening,
  alliances fraying, corruption spreading, sanctity stabilizing.
- **High altitude** — generators. Major world events that intermittently
  bring new named entities into being.

The world is much richer than the named entities we track. Generators are
how that richer implied world occasionally condenses into new named
things. A named entity is not necessarily being born — it is becoming
important enough to enter the simulation.

## Anatomy of a generator

Each generator names:

- **A target kind** — what kind of existing entity it looks at to decide
  "am I applicable here right now?" (e.g. Region, Settlement, Faction).
- **One or more dynamic preconditions** — relationships, facts,
  saturation limits, subtype filters, or other current-world conditions
  that gate applicability. Static attribute dependence is intentionally
  avoided in the active catalog.
- **A produce list** — entities that get spawned, each with a role and
  optional create-chance.
- **A relationship list** — how produced entities wire to each other and
  to the target (and optionally to a secondary existing target).
- **A pulse weight** — relative likelihood this generator wins the pulse
  lottery.
- **(Optional) Non-creation consequences** — additional effects on existing
  entities or world state when those effects are part of the event's
  meaning.

Placement is automatic: produced entities go *on* the target hex, *near*
it, or — with `AllowWilderness` — on a free hex within the target region.
Both physical and semantic hex coordinates are assigned.

## Pulse model

- Every 25 ticks the system asks: *which generators are applicable
  somewhere right now?*
- Applicable = on the tableau AND has at least one target entity whose
  gates pass.
- **One winner fires per pulse**, weighted-random by `PulseWeight` — NOT
  by how many targets it has. (Otherwise a generator targeting settlements
  would crowd out one targeting factions just because there are more
  settlements.)
- Tableau-gating is the whole point: remove the generator's card → it
  stops being applicable, even if world state would otherwise trigger it.
  This is "your deck defines the world's nature" in action.

## Hard invariants (build-time enforcement)

Authoring violations fail the build, not the runtime.

1. **Every generator has at least one target gate.** No "fires randomly
   out of nothing" — the world must have a reason. The active catalog uses
   relationship and saturation gates rather than attribute thresholds.
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
8. **Saturation limits honored.** A target declaring `MaxCount = 3` on a
   `contains` relationship stops being applicable once the target has 3
   out-relationships of that kind. Pressure valve against unbounded
   spawn.
9. **Generators create a connected bundle of new named entities.**
   Relationship mutation on existing entities is allowed but secondary.
   If the main thing the content does is mutate existing graph state, it
   is probably not a generator — it's an action.
10. **The starter generator set must be satisfiable from seed state.** A
    generator cannot depend on relationships that do not exist in the
    seed unless some earlier generator in the active set can create them.

## Runtime preconditions (per pulse)

- Target entity must be `Active` (not Historical).
- Target subtype must match if filter present.
- Required target relationships must exist.
- Saturation cap not hit.
- `MaxRunsPerSim` not exceeded.
- For `AllowWilderness`: region must have a free physical hex nearby.

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
is broad enough to support future generators.

| Kind | Proposed subtypes | Rationale |
| --- | --- | --- |
| `Penguin` | `Warden`, `Broker`, `Witness`, `Exile` | Guardians, negotiators, memory-keepers, and displaced figures. |
| `Faction` | `Order`, `Compact`, `Cell`, `Choir` | Durable institutions, survival compacts, small operational groups, and devotional organizations. |
| `Settlement` | `Rookery`, `Colony`, `Holdfast`, `Sanctuary`, `Harbor`, `Reefhold` | Direct size levels (`Rookery`/`Colony`) plus durable role-based forms: hardened survival, sacred refuge, trade-facing edge, intentionally built offshore. |
| `Artifact` | `Relic`, `Instrument`, `Weapon`, `Tome`, `Seal` | Sacred inheritances, operational instruments, weapon-objects, dangerous written artifacts, containment/closure objects. |
| `Region` | `Face`, `Shelf`, `Depths`, `Reach`, `Sea`, `Scar` | Structural position in/around the Berg: exposed outer face, held ice, deep interior strata, opened margin, open water, wounded ground. |

### Coverage read

- Strong: `Settlement`, `Artifact`, `Region` — the active set exercises
  `Holdfast`, `Reefhold`, `Scar`, `Sea`, `Relic`, `Instrument`, and `Seal`.
- Weaker: `Penguin` and `Faction` coverage is intentionally partial while
  generators are being rebuilt around the new simulation dynamics.

That's acceptable for a first pass. Stable subtype families to design
against matters more than full generator coverage right now.

### Open subtype questions

- `Choir` covers explicitly devotional faction work; `Order` remains the
  broader disciplined form — keep both?
- Do `Penguin.Broker` and `Penguin.Witness` need more generator support,
  or should they emerge primarily through gambits and relationship drift?

## Catalog: current generators

There are currently **no active Ice Remembers generators**. The previous
catalog was deleted because its event shapes were not grounded enough in
the rebuilt simulation layer.

The next catalog pass should start from the gambit-created relationships
that the simulation already produces, such as `Holds`, `AtWarWith`,
`TradeRoute`, and `CleansedBy`, then define four reviewed generators with
cross-kind production. Target subkind filters should stay broad until the
catalog is large enough for subtype coverage to be meaningful.

## Open design questions (system-level)

- How should secondary targeting be represented in authoring data without
  weakening the "single target anchors the event" rule?
- Which of the hard invariants are true schema constraints versus
  runtime validation versus lint?
- What measure of graph connectedness is authorable enough to be useful
  without becoming fake precision?
- What catalog-level subtype coverage do we want: broad parity across
  kinds, or explicit spotlighting of selected subtypes?
- How should non-creation effects be represented so they remain legible
  as generator outcomes rather than becoming a second hidden action
  system?

## Authoring backlog

Recommendations for future passes, separated from blocking design work
so the backlog stays explicit.

**Catalog content.**
- The active catalog is empty while the first four simulation-facing
  generators are redesigned.
- Generator gates should come from actual simulation dynamics, especially
  gambit-created relationships, not from static attribute thresholds.
- A healthy catalog has generators that **collapse** state as well as
  create state —
  famine, exile, dissolution of factions into penguins. Spawn-only
  generators tend toward demographic explosion and are the main reason
  sims get boring after hour 2.

**Invariant follow-ups.**
- An "orphan artifact" rule. Artifacts are currently exempt from the
  orphan-produces check because we haven't decided what anchors them.
  Options: artifacts must be `stored_at` something, or `owned_by` a
  faction, or have any named-entity relationship.
- A "no out-of-domain cultures" rule. A generator could currently
  produce an entity with culture `none` (via region inheritance).
  That's semantically odd for penguins/factions; require Penguin/Faction
  produces to resolve to a culture in the `Cultures` list.
- A cap on `Produces.Count`. A generator producing 10 entities is
  almost certainly a bug or a quest, not a generator. 3–4 is probably
  the right soft cap.
- Require a `Description` field. Currently optional; in a catalog of 30
  generators we'll want authored intent readable at a glance.

**Workflow.**
- Generators should be attached to cards in a visible way so the player
  understands "this card is what produces heroes." The card is the
  contract; the generator is the mechanism. Make sure the card face
  communicates what the generator does in world terms.
