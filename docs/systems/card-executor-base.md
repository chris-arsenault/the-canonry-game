# Card Executor Base

Shared architecture for all three card executors (Generator, System, Action). Each executor extends this base with type-specific behavior.

---

## Purpose

Card executors are where the player's cards meet the simulation. When a card is in the tableau, its executor evaluates preconditions against world state each tick (at the card's configured cadence), and when preconditions are met, applies the card's defined effects. This is the mid-altitude layer: cards operate on the aggregate conditions produced by low-altitude physics.

---

## Design Decisions

### Card definitions are declarative (DSL, not code)

Card effects, preconditions, targeting, and mutations are defined in a domain-specific language or structured data format (JSON-like or custom DSL). NOT hard-coded in the game engine.

**Why:** Cards are the core of the game. The system must support:
- Balatro-style card modification (players modify preconditions, targeting, effects on individual cards during a run)
- Future player-created cards (extensibility without code changes)
- Rapid iteration during design (change card behavior without recompiling)
- Composability (card modifications layer on top of base card definitions)

The DSL must express: preconditions (world state queries), targeting (entity selection), mutations (effects on the entity graph, attributes, relationships, pressures), and meta (cooldowns, cadence, costs).

The canonry's JSON-based approach is the starting point. Lessons from the canonry:
- Nested AND/OR conditions work but get verbose for complex cases
- Graph path assertions (2-hop relationship queries) are essential
- Variable binding ($actor, $target, $previousOwner) enables flexible targeting
- The system should degrade gracefully when conditions can't be met (see preconditions below)

### Preconditions compose with AND/OR logic

Preconditions are composable boolean expressions over world state. A card's preconditions are a tree of AND/OR nodes with leaf conditions that evaluate specific state:

**Leaf condition types:**
- **Pressure threshold:** "military tension in region X exceeds Y"
- **Attribute value:** "entity's stability is below Y" / "entity's government label is 'authoritarian-theocratic'"
- **Relationship existence:** "entity has a rival_of relationship with strength above Y"
- **Relationship absence:** "entity has NO allied_with relationships"
- **Entity count:** "there are at least N entities of kind K in region R"
- **Spatial condition:** "there exists a habitable cell within N distance of entity X with no settlement"
- **Semantic plane position:** "entity's ideology is in the [label] region" (via reducer)
- **Disposition threshold:** "disposition between entity A and entity B is below Y" (via semantic plane distance)
- **Tag check:** "entity has tag T with value V"
- **Temporal condition:** "at least N ticks since this card last fired"
- **Target availability:** "a valid target exists for this card's targeting rules" (critical -- target finding is a precondition, not a separate step)

**Composition:**
```
AND(
  pressure("military_tension", region=$target_region, min=60),
  OR(
    attribute($actor, "stability", max=30),
    relationship_exists($actor, "rival_of", min_strength=0.5)
  ),
  target_available(kind="location", filters=[...])
)
```

Preconditions must be generally composable because Balatro-style card modifications may ADD or REMOVE precondition nodes from a card's precondition tree. A card enhancement that "removes the pressure requirement" deletes a subtree from the precondition.

### Target finding is a precondition

Following the user's direction: one of the preconditions is the ability to succeed. If a card requires a target (most do), finding a valid target is part of precondition evaluation. If no valid target exists, the card's preconditions are NOT met and it does not fire.

**Targeting rules** (defined per card):
- **Entity selection:** by kind, by subtype, by attribute range, by relationship to actor, by spatial proximity, by region
- **Filters:** additional constraints narrowing the candidate set
- **Pick strategy:** how to choose among valid candidates (random, highest attribute, nearest, etc.)
- **Variable binding:** the selected target is bound to a variable ($target) usable in effects

This means the precondition evaluation and target finding are a single atomic step. The card either finds everything it needs (conditions met + target found) or it doesn't fire.

### Cards track execution state

Each card in the tableau maintains runtime state:

- **Last fired:** in-universe tick when this card last executed. Visible to the player.
- **Cooldown:** minimum ticks between firings. Defined per card. Prevents rapid repeat firing.
- **Precondition status:** which preconditions are currently met and which are not. Visible to the player as a diagnostic. The player can see "this generator isn't firing because there's no valid target location" vs "this generator isn't firing because military tension is too low."
- **Achievement progress:** progress toward the card's deck modification conditions (e.g., "generated 3/5 entities"). Visible to the player.

### Effects (mutations) are declarative

When a card fires, it applies a set of mutations to the world state. Mutations are defined in the same DSL as preconditions.

**Mutation types:**
- **Create entity:** spawn a new entity of specified kind/subtype with initial attributes, at a specified location. Bind to a variable for subsequent mutations.
- **Create relationship:** establish a new relationship between two entities (potentially using variables bound during targeting or entity creation).
- **Modify attribute:** change an entity's attribute value (delta or absolute set).
- **Modify relationship strength:** change an existing relationship's strength.
- **Remove relationship:** cull a relationship (set end date, transition to historical).
- **Modify pressure:** directly adjust a pressure value (delta).
- **Set tag:** add or modify a tag on an entity.
- **Set goal tag:** place a goal tag on an entity or location (for entity behavior system).
- **Modify status:** transition an entity's status (active -> historical).
- **Modify subtype:** change an entity's subtype.

Mutations execute in defined order within a single card firing. A generator that creates an entity and then creates relationships TO that entity needs ordered execution (create first, then relate).

### Card modifiability (Balatro pattern)

Card definitions must support runtime modification. During deck modification events, the player may alter a specific card:

- **Precondition modification:** add, remove, or change precondition nodes in the card's precondition tree
- **Targeting modification:** change the targeting rules (wider selection, different pick strategy, additional filters)
- **Effect modification:** add, amplify, or alter mutations (a generator that creates 1 entity could be modified to create 2; a system that modifies an attribute by +5 could be amplified to +8)
- **Cost modification:** change the card's mana cost
- **Cooldown modification:** change the minimum ticks between firings
- **Tag modification:** add or change the card's tags (affecting synergy with other cards and persistent modifiers)

This means card definitions are not static templates -- they are mutable data structures that can be altered per-instance during a run. The base definition comes from the archetype deck. Modifications layer on top.

---

## Card Lifecycle in the Tableau

1. **Placed:** player moves card from hand to tableau. Card registers with the appropriate executor. Mana is consumed.
2. **Waiting:** card is in the tableau but preconditions are not met. Precondition diagnostic is visible to the player. Card checks preconditions each tick (at its configured cadence).
3. **Firing:** preconditions are met (including target availability). Card executes its mutations. Last-fired timestamp is set. Cooldown begins. Achievement progress increments.
4. **Cooling down:** card has fired recently. Even if preconditions are met, it won't fire again until the cooldown expires.
5. **Removed:** player removes card from tableau. Card deregisters from executor. Mana is freed. Card goes to discard pile. Effects already applied persist in the entity graph.

---

## Interactions with Other Systems

| System | How Card Executor Base Interacts |
|--------|----------------------------------|
| Entity Graph (#1) | Reads for precondition evaluation. Writes mutations (new entities, relationships, status changes). |
| Entity Kinds (#2) | Card targeting and creation reference kind definitions for validation. |
| Attributes (#3) | Reads for precondition evaluation (attribute values, semantic plane labels). Writes attribute modifications. |
| Map/Spatial (#4) | Reads for spatial preconditions and targeting (find valid locations). |
| Pressure (#11) | Reads for pressure threshold preconditions. Writes direct pressure modifications. |
| Relationships (#7) | Reads for relationship preconditions. Writes new relationships and strength modifications. |
| Tableau/Mana (#13) | Cards register/deregister when placed/removed. |
| Tick Clock (#5) | Executes in phase 4 of tick order (after low-altitude and pressure). Cadence determines which ticks this card evaluates on. |

---

## Open Questions (Deferred)

- **DSL design.** The specific syntax and capabilities of the card definition language. Whether to extend the canonry's JSON approach or design a custom DSL. Whether the DSL is player-facing (for player-created cards) or developer-facing (with a UI builder for players).
- **Modification representation.** How card modifications are stored (as diffs on the base definition? as a separate modification layer? as a rewritten full definition?). Whether modifications compose (can you modify a modification?).
- **Execution ordering across cards.** When multiple cards fire in the same tick, what order do they execute? Does it matter? Can one card's mutations affect another card's preconditions within the same tick? (Current design says no -- strict tick phase ordering means all cards evaluate preconditions against the same state snapshot.)
- **Performance of precondition evaluation.** Complex precondition trees evaluated against large entity graphs every tick could be expensive. May need optimization (dirty-flagging, caching, spatial indexing for entity queries).
