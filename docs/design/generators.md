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

A generator is not just "spawn an NPC" — it spawns **a bundle** (entities +
relationships + placement), because the interesting narrative units are
relational: a settlement with a founding leader who belongs to a faction,
not three lonely pieces.

### Simulation altitudes

- **Low altitude** — continuous simulation of named entities already in the
  world: movement, resource conversion, local process execution,
  relationship updates.
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
- **One or more triggers** — attribute thresholds on the target that gate
  applicability (Low / High / Critical / Any), e.g. *"Pristiness is High."*
- **A produce list** — entities that get spawned, each with a role and
  optional create-chance.
- **A relationship list** — how produced entities wire to each other and
  to the trigger target (and optionally to a secondary existing target).
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
  trigger passes.
- **One winner fires per pulse**, weighted-random by `PulseWeight` — NOT
  by how many targets it has. (Otherwise a generator targeting settlements
  would crowd out one targeting factions just because there are more
  settlements.)
- Tableau-gating is the whole point: remove the generator's card → it
  stops being applicable, even if world state would otherwise trigger it.
  This is "your deck defines the world's nature" in action.

## Hard invariants (build-time enforcement)

Authoring violations fail the build, not the runtime.

1. **Every generator has at least one trigger.** No "fires randomly out of
   nothing" — the world must have a reason.
2. **Trigger kind matches target kind.** You can't target Regions and
   trigger off a Settlement attribute.
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
- Trigger attribute value crosses threshold (Low ≤ Concern,
  High ≥ Comfortable, etc.).
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

## Subtype proposal

First-pass subtype families per kind. Not encoded schema yet — design
proposal stable enough to author against. The rule: a subtype represents a
real authored role in the world, not a narrow one-off flavor label, and
the set is broad enough to support future generators.

| Kind | Proposed subtypes | Rationale |
| --- | --- | --- |
| `Penguin` | `Outcast`, `Commander`, `Ruler`, `Mystic` | Founder/outcast energy, fortress commanders, civic leaders, sanctuary/catastrophe spiritual figures. |
| `Faction` | `ExileBand`, `CivicOrder`, `FrontierHost`, `Cult` | Displaced polities, institutional urban orders, militarized frontier powers, sacred/dangerous organizations. |
| `Settlement` | `Rookery`, `Colony`, `Holdfast`, `Sanctuary`, `Harbor`, `Reefhold` | Direct size levels (`Rookery`/`Colony`) plus durable role-based forms: hardened survival, sacred refuge, trade-facing edge, intentionally built offshore. |
| `Artifact` | `Relic`, `Instrument`, `Weapon`, `Tome`, `Seal` | Sacred inheritances, operational instruments, weapon-objects, dangerous written artifacts, containment/closure objects. |
| `Region` | `Face`, `Shelf`, `Depths`, `Reach`, `Sea`, `Scar` | Structural position in/around the Berg: exposed outer face, held ice, deep interior strata, opened margin, open water, wounded ground. |

### Coverage read

- Strong: `Faction`, `Settlement`, `Artifact` — each subtype has a clear
  generator path.
- Weaker: `Penguin.Mystic` is only lightly grounded so far;
  `Region.Heartland` (if added) is not yet exercised by starter
  generators.

That's acceptable for a first pass. Stable subtype families to design
against matters more than full generator coverage right now.

### Open subtype questions

- Should `Penguin.Ruler` and `Penguin.Commander` collapse into a broader
  `Leader`, or keep the distinction because civic and martial emergence
  feel different?
- `Choir` covers explicitly devotional faction work; `Order` remains the
  broader disciplined form — keep both?
- Do we need a `Region.Heartland` even without current generator support,
  because world-shape balance needs a stable-core category from the
  beginning?

## Catalog: current generators

Five starter generators, one per target kind, plus two coverage-focused
expansions for subkind variety. All satisfiable from seed state.

### Aggregate creation balance (5-generator starter)

- **Guaranteed kinds:** Penguin × 2, Faction × 4, Settlement × 3,
  Region × 1, Artifact × 3.
- **Optional kinds:** +1 Artifact, +1 Penguin.
- **Guaranteed relationships:** `ControlledBy`, `MemberOf`, `HomeBase`,
  `Holds`, `ClaimedBy`, `Anchored` or `NativeTo`, `CorruptedBy`.
- **Optional relationships:** `RivalOf`, `Memorializes`, `PartOf`.

### `penguin_calls_the_unhuddled` (target: Penguin)

A high-echo exile survives long enough that the cold-lost gather around
them, becoming a named refuge and compact.

- **Trigger:** `Echo` High; warmth pressure or social marginality;
  instability around the target's situation.
- **Creates:** Settlement (sanctuary/refuge), Faction (exile compact),
  Penguin (first follower).
- **Relationships:** refuge `ControlledBy` new faction; target `MemberOf`
  new faction; target `HomeBase` refuge; lieutenant `MemberOf` new
  faction; lieutenant `HomeBase` refuge.
- **Why starter:** introduces `HomeBase` and `MemberOf` into the world
  rather than depending on them. Cross-kind: target is Penguin, center of
  gravity is Settlement+Faction.
- **Open questions:** preferred Penguin subtype (exile / witness / other)
  after subtype pass; default faction skew (religious / political /
  survivalist); refuge subtype (sanctuary / camp / frontier hamlet);
  optional `ClaimedBy` conflict hook now or later.

### `faction_seals_the_edge` (target: Faction)

A cohesive faction pushes fire-cores, aurora light, or offerings to the
edge of its reach and seals a threatened boundary with a holdfast,
warden, and warding seal.

- **Trigger:** `Cohesion` High; `Reach` High or growing territorial
  pressure; edge pressure / external threat / internal need to make reach
  spatially explicit.
- **Optional secondary targets:** existing Region that can plausibly
  receive a frontier settlement; existing Faction whose pressure /
  rivalry / shadow helps explain the stronghold.
- **Creates:** Settlement (holdfast), Penguin (edge warden), Artifact
  (seal/wardstone/banner).
- **Relationships:** holdfast `ControlledBy` target faction; commander
  `MemberOf` target faction; commander `HomeBase` holdfast; warden
  `Holds` artifact; artifact `Anchored` to holdfast; artifact `ClaimedBy`
  target faction; optional `RivalOf` toward existing faction; optional
  stronger version: holdfast `ClaimedBy` rival faction.
- **Why starter:** strengthens artifact creation without making the
  whole event artifact-dependent. Cross-kind: target is Faction, center
  of gravity is Settlement+Penguin+Artifact.
- **Open questions:** holdfast expression (keep / watchpost / citadel /
  cliff-cut bastion); artifact default (political insignia / military
  standard / ritual ward); whether `Holds` and `Anchored` are mutually
  exclusive variants or the banner moves between commander and holdfast.

### `settlement_names_the_office` (target: Settlement)

A settlement with strong dominion makes authority explicit by naming an
office, seating a leader, and binding the role to a seal.

- **Trigger:** `Dominion` High; `Shelter` High or general civic
  stability; pressure toward formalization, named authority, or
  institutional consolidation.
- **Optional secondary target:** existing Region whose broader importance
  the settlement is trying to answer or dominate.
- **Creates:** Faction (named office / civic order), Penguin (office
  holder), Artifact (office seal / tome / regalia).
- **Relationships:** target settlement `ControlledBy` new faction; leader
  `MemberOf` new faction; leader `HomeBase` target settlement; leader
  `Holds` artifact; artifact `Anchored` to target settlement; artifact
  `ClaimedBy` new faction.
- **Why starter:** stronger institutional event than simple
  daughter-colony formation. Cross-kind: target is Settlement, center of
  gravity is Faction+Penguin+Artifact.
- **Open questions:** faction subtype preference (`order`/`compact`/
  `choir`); leader subtype (magistrate / war-leader / speaker / prince /
  founder-ruler); artifact default (seal / charter / crown / baton /
  banner / civic relic); whether `ControlledBy` is guaranteed or whether
  some variants produce the order without full political control.

### `region_opens_the_old_holding` (target: Region)

A pristine or unstable region becomes permeable: deep ice, old shadow, or
sealed memory opens into a sanctuary/holding-place, and a custodial order
forms around it.

- **Trigger:** latent significance, remoteness, holding, or instability
  in the region; `Pristiness` or other region-state pressure that makes
  revelation plausible.
- **Creates:** Settlement (sanctuary / old holding), Faction
  (custodians), Artifact (relic / seal); optional Penguin (keeper).
- **Relationships:** settlement `ControlledBy` new faction; artifact
  `Anchored` to settlement; artifact `NativeTo` target region; artifact
  `ClaimedBy` new faction; optional penguin `MemberOf`/`HomeBase`;
  optional historical `Memorializes`; optional `CleansedBy` or
  `CorruptedBy` for moral-polarity variants.
- **Why starter:** the landscape itself causes the event, rather than
  using the region as a generic backdrop for social emergence.
  Cross-kind: target is Region, center of gravity is
  Settlement+Faction+Artifact.
- **Open questions:** sanctuary subtype (shrine-town / excavation camp /
  cloister / vault-settlement / hidden refuge); faction default
  (custodians / excavators / pilgrims / opportunists); artifact mood
  (sacred / historical / dangerous); whether old holdings usually carry
  both `Anchored` and `NativeTo` or pick one in variants.

### `artifact_cracks_the_berg` (target: Artifact, secondary target: Region)

A high-power or high-resonance artifact breaks the ice's holding. A true
scar-region is named into being, with a choir / cell / order rising to
occupy or worship the rupture.

- **Trigger:** `Resonance` High or `Power` High; a vulnerable or
  symbolically loaded `Region` as secondary target; pressure for rupture,
  overflow, corruption, or catastrophic emergence.
- **Creates:** Region (scar — a true new named region), Faction (choir /
  order / cell); optional Artifact (splinter / broken relic).
- **Relationships:** new region `CorruptedBy` target artifact; new region
  `ControlledBy` new faction; target artifact `Anchored` to new region;
  optional splinter `PartOf` target artifact; optional splinter
  `NativeTo` new region; optional new faction `RivalOf` controller of
  surrounding parent region.
- **Non-creation consequences:** secondary target region loses integrity
  as the new region tears out of it; local safety, pristiness, and
  symbolic stability are degraded around the rupture.
- **Why starter:** one unmistakably catastrophic, high-altitude event.
  Makes artifacts capable of reshaping the named map, not just modifying
  holders or sites. Strongest cross-kind result of any starter.
- **Open questions:** whether the target artifact also becomes `NativeTo`
  the scar or `Anchored` carries the stronger rupture meaning; whether
  the new faction is always present or some future variant creates only
  the region; splinter subtype (shard / echo / idol-fragment / cursed
  tool); whether the parent region needs an explicit relationship to the
  new region later.

### Coverage expansions

Two coverage-focused additions targeting weak subkinds. Target kind is
intentionally flexible until the next semantic pass.

**`sea_raises_the_reefhold`** — a dangerous water-edge becomes newly
legible as a named sea, and a built reefhold rises to meet it. Pushes
the catalog toward oceanic / non-scar regions / smaller faction forms.
Creates `Settlement.Reefhold` + `Region.Sea`, optional
`Faction.Cell` + `Artifact.Instrument`. Improves coverage on
`reefhold` / `sea` / `cell` / `instrument`.

**`the_shelf_yields_a_tome`** — a named shelf becomes legible through the
recovery of a tome, optionally stabilizing into a rookery and scholar.
Adds a quieter, scholarly, archival emergence the catalog otherwise
lacks. Creates `Artifact.Tome` + `Region.Shelf`, optional
`Settlement.Rookery` + `Penguin.Scholar`. Improves coverage on
`tome` / `shelf` / `rookery` / `scholar` and expands artifact vocabulary
toward text and memory rather than only warding and relics.

## Adjustment notes

Visible from current coverage stats; not final decisions.

1. **Settlement overproduction reduced.** Replacing the colony generator
   with the civic-order generator dropped guaranteed Settlement creation
   from 4 to 3 and improved artifact coverage.
2. **Add optional Penguin creation** in `region_opens_the_old_holding`
   (custodian / discoverer / hierophant) and `artifact_cracks_the_berg`
   (prophet / war-chief / first witness). The starter set currently
   guarantees only 2 Penguins, both fairly role-functional.
3. **Pull in more existing-Region secondary targets** in
   `faction_seals_the_edge` and `settlement_names_the_office` to ground
   them spatially without forcing daughter-settlement creation.

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
- The current set is scaffolding, not a designed world. Four-to-seven
  generators is too few to feel like a living world over a 4-hour
  session. Expect 15–30 before it breathes.
- Triggers are too on-the-nose. Multi-attribute triggers
  ("settlement with high shelter AND a neighboring rival faction
  produces a diplomat") would give the semantic model more to chew on.
- All current generators are additive. None remove or transform
  entities. A healthy catalog has generators that **collapse** state —
  famine, exile, dissolution of factions into NPCs. Spawn-only
  generators tend toward demographic explosion and are the main reason
  sims get boring after hour 2.

**Invariant follow-ups.**
- An "orphan artifact" rule. Artifacts are currently exempt from the
  orphan-produces check because we haven't decided what anchors them.
  Options: artifacts must be `stored_at` something, or `owned_by` a
  faction, or have any named-entity relationship.
- A "no out-of-domain cultures" rule. A generator could currently
  produce an entity with culture `none` (via region inheritance).
  That's semantically odd for NPCs/factions; require Npc/Faction
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
