# Generator Design Work

Working document for actual generator design. This is separate from `docs/GENERATOR_DESIGN_START.md`, which should remain a session-transfer document rather than the active design spec.

## Current framing

A generator is a world event that fires on a pulse. Its default form is a bundle: entities, relationships, and placement, attached to an existing target in the world. Generators do not exist to make the graph denser for its own sake. They exist to author things that happen in the world.

Creation is not the whole story. A generator may also apply additional effects to existing entities or world state when those effects are part of the event's meaning.

## Simulation altitudes

This simulation operates across multiple altitudes.

- **Low altitude** is the continuous simulation of named entities already in the world: movement, resource conversion, local process execution, and relationship updates.
- **Mid altitude** is systematic change in the graph: wars hardening, alliances fraying, corruption spreading, sanctity stabilizing, and other patterned shifts that emerge from the lower-level simulation.
- **High altitude** is the domain of generators: major world events that intermittently bring new named entities into being.

The world is much richer than the named entities we track, but the simulation only models the named layer directly. Generators are how that richer implied world occasionally condenses into new named things. A named entity is not necessarily being born. It is becoming important enough to enter the simulation.

This means the overall system is intentionally hybrid:

- partially procedural, because the named world continues simulating between generations
- partially generative, because intermittent high-altitude events introduce new named entities and fresh graph structure

The tableau determines which generators are active. That means the player's tableau is not just a modifier on an existing world; it is a statement about what kinds of worlds are allowed to come into being.

## Rule types

Not every statement about generators should live in the same enforcement bucket. Some should become hard schema/runtime constraints. Some should be warning-level heuristics. Some are authoring principles that help us judge whether a generator is good, but should not fail a build.

### Hard invariants

1. **The generated graph stays connected to the existing world.** The target exists to guarantee that a generator fires into the current world rather than producing an isolated mini-graph.
2. **Every generator has at least one real world anchor.** In the default case that anchor is the target. If we add explicit secondary targeting later, those attachments still support the same event rather than replacing world attachment.
3. **A generator creates a connected bundle of new named entities.** Relationship mutation on existing entities is allowed, but it is secondary. If the main thing the content does is mutate existing graph state, it is probably not a generator.
4. **The starter generator set must be satisfiable from seed state.** A generator cannot depend on relationships that do not exist in the seed unless some earlier generator in the active set can create them. We should eventually encode this as a validation invariant over the generator catalog.

### Soft warnings

1. **There is an optimal connectedness, and it is mid.** Too few connections lock targets into the same repeated patterns. Too many connections make graph targeting feel effectively random because everything important touches everything else. Good generators create a shape the simulation can still read.
2. **Secondary attachment is allowed, but should earn its keep.** A generator may connect to more than one existing entity when that makes the event more specific, more legible, or more grounded. It is bad when it only increases edge count.
3. **Subtype coverage matters at the catalog level.** The generator catalog should exercise entity subtypes, not only broad kinds. If the catalog ignores subtypes, subtype choice collapses into flavor text instead of becoming authored content surface.
4. **Cross-kind creation is usually more interesting than same-kind echo.** A generator is generally stronger when the created bundle is not just "more of the target kind." A world event around one kind should often cause other kinds to become named.

### Authoring principles

1. **Generators are narratively interesting, not just graph-interesting.** A good generator represents something that happens in the world: a faction schism, a war outbreak, a relic's corruption, a pilgrimage, a hero sacrificing themselves to end a war. If the best description is only topological, the generator is not finished.
2. **Generators are event-shaped, not census-shaped.** They should not default to agency bundles just because agency relationships are the easiest things to wire. The relationship pool exists so place-centered, artifact-centered, lineage-centered, and historical-state events can be first-class content.
3. **Generators may leave consequences beyond what they create.** A generator can carry additional effects besides creating entities and relationships when those effects are part of the event's meaning. The world should be allowed to react, not just accumulate pieces.

## Open design questions

- How should secondary targeting be represented in authoring data without weakening the "single target anchors the event" rule?
- Which of the hard invariants above are true schema constraints versus runtime validation versus lint?
- What measure of graph connectedness is authorable enough to be useful without becoming fake precision?
- What catalog-level subtype coverage do we actually want: broad parity across kinds, or explicit spotlighting of selected subtypes?
- How should non-creation effects be represented so they remain legible as generator outcomes rather than becoming a second hidden action system?

## First-pass starter generators

Goal: start with one generator per target kind so the initial set exercises the generator system and tableau system broadly. Each generator should create a real connected bundle of new named entities, not just mutate relationships on existing ones. These are workshop candidates, not final semantic definitions.

### 1. `penguin_gathers_exiles`

Moved to its own file: [penguin-gathers-exiles.md](/home/dev/repos/the-canonry-game/docs/generators/penguin-gathers-exiles.md).

### 2. `faction_seals_the_edge`

Moved to its own file: [faction-seals-the-edge.md](/home/dev/repos/the-canonry-game/docs/generators/faction-seals-the-edge.md).

### 3. `settlement_names_the_office`

Moved to its own file: [settlement-names-the-office.md](/home/dev/repos/the-canonry-game/docs/generators/settlement-names-the-office.md).

### 4. `region_opens_the_old_holding`

Moved to its own file: [region-opens-the-old-holding.md](/home/dev/repos/the-canonry-game/docs/generators/region-opens-the-old-holding.md).

### 5. `artifact_cracks_the_berg`

Moved to its own file: [artifact-cracks-the-berg.md](/home/dev/repos/the-canonry-game/docs/generators/artifact-cracks-the-berg.md).

### 6. `sea_raises_the_reefhold`

Moved to its own file: [sea-raises-the-reefhold.md](/home/dev/repos/the-canonry-game/docs/generators/sea-raises-the-reefhold.md).

### 7. `the_shelf_yields_a_tome`

Moved to its own file: [the-shelf-yields-a-tome.md](/home/dev/repos/the-canonry-game/docs/generators/the-shelf-yields-a-tome.md).

## Starter generator creation summary

This table tracks only what the generators create, not the prerequisite state they depend on. It is meant to make bias visible while the set is still small.

| Generator | Created kinds / subkinds | Created relationships |
|---|---|---|
| `penguin_gathers_exiles` | `Settlement` refuge, `Faction` exile band/commune/sect, `Penguin` lieutenant/first follower | `ControlledBy`, `MemberOf`, `HomeBase` |
| `faction_seals_the_edge` | `Settlement` fortress, `Penguin` commander, `Artifact` banner/wardstone/regalia | `ControlledBy`, `MemberOf`, `HomeBase`, `Holds`, `Anchored`, `ClaimedBy`; optional `RivalOf`, optional fortress `ClaimedBy` |
| `settlement_names_the_office` | `Faction` civic order/magistracy/ruling house, `Penguin` strong leader/magistrate/founder-ruler, `Artifact` seal/charter/regalia | `ControlledBy`, `MemberOf`, `HomeBase`, `Holds`, `Anchored`, `ClaimedBy` |
| `region_opens_the_old_holding` | `Settlement` sanctuary/dig-site/shrine-town, `Faction` custodians/excavators/pilgrims, `Artifact` relic/seal/idol; optional `Penguin` custodian/discoverer/hierophant | `ControlledBy`, `Anchored`, `NativeTo`, `ClaimedBy`; optional `MemberOf`, optional `HomeBase`, optional `Memorializes`, optional `CleansedBy` or `CorruptedBy` |
| `artifact_cracks_the_berg` | `Region` scar/breach, `Faction` choir/order/cell; optional splinter `Artifact` | `CorruptedBy`, `ControlledBy`, `Anchored`; optional `PartOf`, optional `NativeTo`, optional `RivalOf` |

## Aggregate creation balance

Across the current first-pass set:

- Guaranteed created kinds:
  - `Penguin`: 2
  - `Faction`: 4
  - `Settlement`: 3
  - `Region`: 1
  - `Artifact`: 3
- Optional created kinds:
  - `Artifact`: +1
  - `Penguin`: +1
- Guaranteed created relationships:
  - `ControlledBy`
  - `MemberOf`
  - `HomeBase`
  - `Holds`
  - `ClaimedBy`
  - `Anchored` or `NativeTo`
  - `CorruptedBy`
- Optional created relationships:
  - `RivalOf`
  - `Memorializes`
  - `PartOf`

Initial read: this set now behaves much more like an actual generator seed catalog. It creates real bundles, covers all five kinds in created output, and follows the "cross-kind emergence is more interesting" rule much better than the previous draft. Replacing the colony generator with the civic-order generator reduces settlement overproduction and improves artifact and institution coverage in the guaranteed set.

## Compact coverage table

| Generator | Target kind | Guaranteed created kinds | Optional created kinds | Existing secondary target | Key created relationships |
|---|---|---|---|---|---|
| `penguin_gathers_exiles` | `Penguin` | `Settlement`, `Faction`, `Penguin` | None currently | None currently | `ControlledBy`, `MemberOf`, `HomeBase` |
| `faction_seals_the_edge` | `Faction` | `Settlement`, `Penguin`, `Artifact` | None currently | optional existing `Region`, optional existing `Faction` | `ControlledBy`, `MemberOf`, `HomeBase`, `Holds`, `Anchored`, `ClaimedBy`; optional `RivalOf` |
| `settlement_names_the_office` | `Settlement` | `Faction`, `Penguin`, `Artifact` | None currently | optional existing `Region` | `ControlledBy`, `MemberOf`, `HomeBase`, `Holds`, `Anchored`, `ClaimedBy` |
| `region_opens_the_old_holding` | `Region` | `Settlement`, `Faction`, `Artifact` | optional `Penguin` | None currently | `ControlledBy`, `Anchored`, `NativeTo`, `ClaimedBy`; optional `MemberOf`, optional `HomeBase` |
| `artifact_cracks_the_berg` | `Artifact` | `Region`, `Faction` | optional `Artifact` splinter | required existing `Region` | `CorruptedBy`, `ControlledBy`, `Anchored`; optional `PartOf`, optional `NativeTo` |

## Adjustment notes for next pass

These are not final decisions yet. They are the current pressure points visible from the coverage stats.

### 1. Settlement overproduction has been reduced

Replacing the colony generator with the civic-order generator drops guaranteed `Settlement` creation from `4` to `3` and improves the starter set's artifact coverage.

### 2. Add some optional Penguin creation

The set currently guarantees only `Penguin: 2`, and both are still fairly role-functional: lieutenant / commander / strong leader. A bit more optional Penguin creation would help variety.

Good places to add optional Penguin creation:

- `region_opens_the_old_holding`
  - optional `Penguin` custodian / discoverer / hierophant
  - This makes the sanctuary less institution-only and gives it an immediate human face.

- `artifact_cracks_the_berg`
  - optional `Penguin` prophet / war-chief / first witness
  - This would help the artifact catastrophe produce a more legible human face without making the Penguin mandatory.

Less urgent:

- `faction_seals_the_edge`
  - already guarantees one Penguin

### 3. Pull in additional existing-region secondary targets

There is room to use existing `Region` entities as secondary targets more aggressively, especially where they can produce relationships on existing world state rather than only on the created bundle.

Good candidates:

- `faction_seals_the_edge`
  - existing secondary target: `Region`
  - possible created relationship on existing world:
    - new fortress `ClaimedBy` controller of the surrounding region, if relevant later
    - artifact `Anchored` into that frontier region's new strongpoint
  - structural value:
    - makes the faction event less abstractly political and more spatially grounded
  - additional optional secondary target: existing `Faction`
  - possible created relationship on existing world:
    - `RivalOf` between the target faction and the existing faction
  - structural value:
    - lets the stronghold form in response to a specific peer pressure rather than only abstract frontier tension

- `settlement_names_the_office`
  - existing secondary target: `Region`
  - possible created relationship on existing world:
    - the new civic order's authority can be framed as an answer to a broader regional situation
    - the created regalia could later be `Anchored` into a frontier or ceremonial site within that region
  - structural value:
    - keeps the settlement generator spatially aware without forcing it back into daughter-settlement creation

- `artifact_cracks_the_berg`
  - already requires existing `Region` as secondary target
  - the main remaining design opportunity is whether the parent region should eventually carry an explicit relationship to the new region, if we add such a relationship family later

Less useful:

- `penguin_gathers_exiles`
  - can stay seed-simple for now

- `region_opens_the_old_holding`
  - already uses the target region as the landscape cause; a second existing region is probably unnecessary

## First-pass subtype proposal

This is a working proposal for subtype families by kind. It is grounded in the starter generators, but it is not limited to them. Full generator coverage is not expected yet. The goal here is to choose 3-4 durable subtype buckets per kind that are broad enough to support multiple generators, actions, and future content.

### Design rule

- A subtype should represent a real authored role in the world, not just a narrow one-off flavor label.
- A subtype set should be broad enough to support future generators we have not written yet.
- The first pass should prefer fewer, stronger subtype families over many thin ones.

### Proposed subtype families

| Kind | Proposed subtypes | Why these |
|---|---|---|
| `Penguin` | `Outcast`, `Commander`, `Ruler`, `Mystic` | Covers `penguin_gathers_exiles` founder/outcast energy, fortress commanders, civic leaders, and sanctuary/catastrophe spiritual figures without overfitting to one generator. |
| `Faction` | `ExileBand`, `CivicOrder`, `FrontierHost`, `Cult` | Maps cleanly onto the current starter set while staying broad: displaced polities, institutional urban orders, militarized frontier powers, and sacred/dangerous organizations. |
| `Settlement` | `Rookery`, `Colony`, `Holdfast`, `Sanctuary`, `Harbor`, `Reefhold` | Gives us direct size levels plus durable role-based forms: ordinary habitation, major organized settlement, hardened survival node, sacred/refuge settlement, trade-facing edge settlement, and intentionally built offshore/deep-water settlement. |
| `Artifact` | `Relic`, `Instrument`, `Weapon`, `Tome`, `Seal` | Matches the object's actual vocabulary in The Ice Remembers: sacred inheritances, operational instruments, weapon-objects, dangerous written artifacts, and containment or closure objects. |
| `Region` | `Face`, `Shelf`, `Depths`, `Reach`, `Sea`, `Scar` | Classifies regions by structural position in and around the Berg: exposed outer face, held ice, deep interior strata, opened margin, open water, and wounded or breached ground. |

### Mapping from current starter generators

This is not a coverage requirement. It is just a sanity check that the proposed subtypes are already useful.

| Generator | Implied subtype usage |
|---|---|
| `penguin_gathers_exiles` | `Penguin.Outcast`, `Settlement.Refuge`, `Faction.ExileBand` |
| `faction_seals_the_edge` | `Faction.FrontierHost`, `Settlement.Holdfast`, `Penguin.Commander`, `Artifact.Regalia` or `Artifact.Ward` |
| `settlement_names_the_office` | `Settlement.Colony`, `Faction.CivicOrder`, `Penguin.Ruler`, `Artifact.Regalia` |
| `region_opens_the_old_holding` | `Region.Wilderness` or `Region.Frontier`, `Settlement.Sanctuary`, `Faction.Choir`, `Artifact.Relic`, optional `Penguin.Mystic` |
| `artifact_cracks_the_berg` | `Artifact.Relic` or `Artifact.Shard`, `Region.Scar`, `Faction.Choir`; optional splinter `Artifact.Shard` |

### Coverage read

The current subtype proposal is strongest on:

- `Faction`: each proposed subtype already has a clear generator path
- `Settlement`: all four subtype families already have an obvious place in the starter set
- `Artifact`: the current generators already justify `Regalia`, `Relic`, and `Shard`, with `Ward` supported by the holdfast pattern

The current subtype proposal is weakest on:

- `Penguin`: `Mystic` is only lightly grounded so far
- `Region`: `Heartland` is not yet strongly exercised by the starter generators

That is acceptable for a first pass. We do not need full generator coverage yet. We only need subtype families that are stable enough to design against.

### Why this set is better than narrower labels

- It avoids overfitting to one generator's exact wording, like `Hierophant` or `ShrineTown`, as first-class subtype categories.
- It gives each kind a mix of civic, frontier, sacred, and catastrophic identity where appropriate.
- It leaves room for future generators to express different world moods without forcing a subtype redesign.

### Open questions

- Should `Penguin.Ruler` and `Penguin.Commander` collapse into a broader `Leader`, or is the distinction worth keeping because civic and martial emergence feel different?
- `Choir` covers the explicitly devotional faction slot; `Order` remains the broader disciplined form.
- `Settlement` now has direct size levels in `Rookery` and `Colony`, so the generic `Town` bucket is no longer needed.
- Should `Region.Heartland` remain even without current generator support, because world-shape balance needs a stable-core category from the beginning?
