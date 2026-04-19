# Generator System — gameplay overview

## What generators do
A generator is a **world event that spawns a pre-wired package of entities on a pulse** (every 25 ticks). Each pulse, the system picks *one* applicable generator and fires it. Think of them as the "things that happen to your world" layer — the mechanism by which the simulation introduces new actors, factions, and artifacts over time, rather than starting with a fixed census and only losing pieces.

A generator isn't just "spawn an NPC" — it spawns **a bundle** (entities + relationships + placement), because the interesting narrative units are relational: a settlement with a founding leader who belongs to a faction, not three lonely pieces.

## The shape of a generator
Each generator names:

- **A target kind** — what kind of existing entity it looks at to decide "am I applicable here right now?" (e.g. Region, Settlement, Faction)
- **One or more triggers** — attribute thresholds on the target that gate applicability (Low / High / Critical / Any), e.g. *"Pristineness is High"*
- **A produce list** — the entities that get spawned, each with a role and optional create-chance
- **A relationship list** — how the produced entities wire to each other and to the trigger target
- **A pulse weight** — relative likelihood this generator wins the pulse lottery

Placement of produced entities is automatic: either *on* the target hex, *near* it, or — with `AllowWilderness` — on a free hex within the target region. Both physical and semantic hexes get assigned.

## Pulse model
- Every 25 ticks the game asks: *"which generators are applicable somewhere right now?"*
- Applicable = on the tableau AND has at least one target entity whose trigger passes.
- **One winner fires per pulse**, weighted-random by the generator's `PulseWeight` — NOT by how many targets it has. (Otherwise a generator targeting settlements would crowd out one targeting factions just because there are more settlements.)
- Tableau-gating matters: remove the generator's card → it stops being applicable, even if the world state would otherwise trigger it. This is the "your deck defines the world's nature" thesis in action.

## Invariants the authoring layer enforces
These fail the build if violated — they're not runtime checks, they're schema rules for anyone authoring a generator.

1. **Every generator has at least one trigger.** No "fires randomly out of nothing" — the world must have a reason.
2. **Trigger kind matches target kind.** You can't target Regions and trigger off a Settlement attribute.
3. **No orphan produces** (with a carve-out for Artifacts). Every produced entity must be referenced by at least one relationship — otherwise you're dropping a loose entity into the world with no story attached.
4. **Optional produces can't be anchors.** If `CreateChance < 1.0`, the entity can be referenced by at most 2 relationships. Otherwise "skipping" that entity silently orphans chains of others.
5. **No cyclic culture inheritance.** `InheritCultureFrom = "$other"` can't form a cycle — somebody has to be the root.
6. **Culture must resolve to a real value.** No silent fallback to a default — if a produce declares `InheritCultureFrom = "$target"` and the target has no culture, the fire hard-fails. ("none" counts as a real value for Regions, which legitimately have no culture.)
7. **Saturation limits honored.** If a generator's target declares `MaxCount = 3` on the `contains` relationship, it stops being applicable once the target has 3 out-relationships of that kind — pressure valve against unbounded spawn.

## Preconditions the runtime applies at pulse time
- Target entity must be `Active` (not Historical).
- Target subtype must match if filter present.
- Trigger attribute value crosses threshold (Low ≤ Concern, High ≥ Comfortable, etc.).
- Saturation cap not hit.
- `MaxRunsPerSim` not exceeded.
- For `AllowWilderness`: region must have a free physical hex nearby.

## Currently authored generators

| Id | Target | Trigger | Produces | Weight |
|---|---|---|---|---|
| `colony_founding` | Region (wilderness) | Pristineness High | village + warrior-leader + founding faction, plus `contains`/`located_at`/`controlled_by`/`loyal_to` wiring | 2 |
| `hero_emergence` | Settlement | Sanitation Low | hero NPC, optionally (25%) a weapon artifact `stored_at` the hero | 3 |
| `faction_splinter` | Faction | Cohesion High | new faction + warrior leader, both `rival_of` the parent | 1 |
| `artifact_discovery` | Region (wilderness) | Pristineness High | instrument artifact (no wiring) | 1 |

Read in aggregate: **wilderness with high pristineness attracts colonies and artifacts; strained settlements produce heroes; overly cohesive factions tear themselves apart.** That's the authored worldview right now.

## Recommendations (for when you do the real design pass)

**Gameplay-shaped:**

1. **The current set is scaffolding, not a designed world.** Four generators is too few to feel like a living world over a 4-hour session. Expect to author 15–30 before it breathes. The bones are fine; the content isn't.
2. **The triggers are too on-the-nose.** "Sanitation Low → hero appears" is a direct translation of one attribute to one outcome. Interesting generators read sideways: e.g. *"a settlement with high sanitation AND a neighboring rival faction produces a diplomat."* Multi-attribute triggers would give the semantic model more to chew on.
3. **Every current generator is additive.** None remove or transform entities. A healthy catalog has generators that *collapse* state — famine, exile, dissolution of factions into NPCs. Spawn-only generators tend toward demographic explosion and are the main reason sims get boring after hour 2.
4. **`hero_emergence` with CreateChance=0.25 on weapon is fine, but the weapon is orphaned culturally** — it inherits from `$hero`, who inherits from `$target` (the settlement). When the hero isn't created (well, in this case hero is always created, but in other future generators)... watch for this. Consider whether optional produces should inherit from `$target` directly to survive anchor loss.
5. **No generator has relationships pointing back to the world that *mutate* existing entities.** Every relationship is new-entity-to-new-entity or new-entity-to-target. That means generators feel like "stuff gets added" rather than "the world reacts." Consider authoring generators that grant a new buff/effect to the target (e.g. a heroic emergence bumps the settlement's morale for N ticks).

**Invariant-shaped (for the parked #147 discussion):**

6. **You'll want an "orphan artifact" rule.** Right now artifacts are exempt from the orphan-produces check because you haven't decided what anchors them. Options: *artifacts must be `stored_at` something*, or *artifacts must be `owned_by` a faction*, or *artifacts must have any named-entity relationship*. Pick one.
7. **Consider a "no out-of-domain cultures" rule.** Currently a generator could produce an entity with culture "none" (via region inheritance). That's semantically odd for NPCs/factions — "none" is fine for the land, less fine for a person. May want to require that `Npc`/`Faction` produces resolve to a culture in the `Cultures` list.
8. **Consider a cap on `Produces.Count`.** A generator producing 10 entities is almost certainly a bug or a quest, not a generator. 3–4 is probably the right soft cap.
9. **Consider requiring a `Description` field.** Currently `Description` is optional; in a catalog of 30 generators you'll want the authored intent readable at a glance.

**Authoring-workflow-shaped:**

10. **Generators should be attached to cards in a visible way** so the player understands "this card is what produces heroes." Right now the tableau wires them up but the player-facing framing of "your deck is why these things happen" is the whole thesis — make sure the card face communicates what the generator *does* in world terms, not just "Hero Emergence." The card is the contract, the generator is the mechanism.
