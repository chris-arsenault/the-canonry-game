# Culture

A static, domain-defined classification that describes innate tendencies, naming conventions, and identity for entities.

---

## What Culture Is

Culture is an overarching construct that defines an entity's identity
foundation and can also accumulate as culture map influence. It is:

- **Normally static per entity.** An entity's culture is set at creation. Most
  living/social entities keep that culture for their lifetime. Authored domain
  rules may explicitly change the culture of claimable world-place entities
  such as regions and points of interest.
- **Static per domain.** The list of available cultures is defined by the narrative domain, similar to how entity kinds are registered by the domain. Cultures are not created, destroyed, or merged during a run. They exist as a fixed taxonomy.
- **Not equivalent to morphology, location, or faction.** Wake-Singers are not the orca kind; they are a culture mostly associated with orcas. Penguins can share Wake-Singer culture, and orcas can defect from it. Culture and body/kind are orthogonal.
- **An identity tendency.** Each culture can define baseline tendencies on
  semantic planes, trait weighting, relationship maintenance, and naming.
  Culture Y might tend toward ritual conservatism; Culture Z might tend toward
  risk-taking or secrecy. These tendencies affect:
  - **Start conditions:** when an entity is created with a given culture, its
    initial semantic plane positions and trait probabilities may be influenced
    by cultural tendencies
  - **Ongoing modifiers:** cultural affiliation feeds relationship
    maintenance and can be used by authored targeting rules.
- **The determinant of entity names.** Culture is the key input to the naming system. A penguin of Culture Y gets a Culture Y name. A settlement founded by Culture Y entities gets a Culture Y place name.

## What Culture Is Not

- **Not an entity kind.** Culture does not exist as an entity in the graph.
  There is no "Culture Y" node with health, traits, relationships, and
  behaviors.
- **Not a map entity.** Culture is not represented by a graph node. Cultural
  influence is map-owned tile state derived from actions and authored startup
  state, then classified by the kernel boundary program.
- **Not a dynamic taxonomy.** The culture list doesn't change during a run. No
  new cultures emerge, no cultures go extinct. Cultural dynamics operate
  through entities and map tile layers keyed by culture id; culture itself does
  not have graph state or lifecycle.
- **Not a card target.** Cards don't target "Culture Y." They target entities
  with culture, trait, relationship, or locality filters. "Entities of culture
  Y" is a filter on entity queries, not a reference to a culture entity.

## How Culture Interacts with Systems

| System | How Culture Interacts |
|--------|----------------------|
| Entity Graph (#1) | Culture is a property on entity records (like kind and subtype). Not a separate entity. |
| Health And Traits (#3) | Culture may influence trait probabilities and naming. |
| Entity Behaviors (#6) | Cultural tendencies can influence low-altitude verb weights, initial semantic position, relationship maintenance, and gambit targeting. |
| Relationships (#7) | Shared culture is a factor in relationship strength modification (same culture trends positive). Cultural difference feeds into disposition calculation via semantic plane distance. |
| Map Signals (#4) | Cultural distribution is available as locality and semantic-map evidence. |
| Narrative Domain (#19) | Culture list and per-culture definitions (tendencies, naming rules, modifiers) are defined by the domain. |
| Map Visualization (#10) | Cultural distribution can be visualized as an overlay using kernel boundary classifications. |
| Generator Executor (#16) | New entities inherit culture from context (location, parent entity, domain rules). |

## Cultural Influence And Boundaries

Cultural influence is stored as a kernel tile layer, not as per-entity influence
and not as authored culture objects. The `CultureBoundaries` kernel program
derives the shared boundary model from that layer:

- raw culture mass, capped at `2000` per culture/cell/plane;
- dominance share, recalculated from all raw culture mass at the cell;
- per-culture connected components on the physical and semantic planes;
- cell kind: none, interior, border, isolated, or contested;
- strength band: none, trace, claimed, controlled, entrenched, or nexus;
- component size and raw mass;
- strongest foreign culture/share at the cell;
- interior holes as derived component gaps.

Consumers should query the kernel boundary program or a projection populated
from it. Minor actors, low-altitude influence verbs, generator placement,
culture slots, MC telemetry, and UI overlays should not reimplement their own
boundary/component formulas.

The current strength thresholds are:

| Band | Raw mass |
| --- | ---: |
| trace | `5+` |
| claimed | `30+` |
| controlled | `100+` |
| entrenched | `300+` |
| nexus | `700+` |

The raw mass cap is `2000`, giving peaks roughly 3x the nexus threshold of
dynamic range.

Cleanup actions reduce raw mass for all non-own cultures at the target cell.
They never write the derived share layer directly.

Entity-level influence production is still tracked as attribution and scoring
evidence, especially for culture slots. It is not the durable territorial layer.

### Decay, Remittances, And Concentration

Raw culture mass decays. The `CultureFieldDecay` kernel program applies a
multiplicative decay per pulse (default `0.5%` every 5 main ticks) with a
minimum decay floor and a cull threshold that removes near-zero cells. The
field therefore reflects the balance of current production against loss:
unsustained wander-trails and satellites fade, while cells near sustained
sources persist. Decay parameters live under
`kernelTuning.programs.cultureField` in the host startup config.

Mobile production is anchored. Every entity has a per-plane culture home
anchor, set at creation and moved when a relationship kind flagged
`IsCultureAnchor` is created (Ice Remembers flags `HomeBase`). Positive
influence produced farther than the remittance distance from the anchor sends
a remittance share (default 50%) back to the anchor cell, so roaming entities
feed their culture's center instead of painting unbounded trails.

Concentration is self-reinforcing. Reinforce-style influence targeting keeps a
positive floor near the cap and gains a gravity bonus inside the culture's
largest connected component. Minor actor sources standing in an own-culture
nexus cell emit stronger work. Generator outputs using `CultureInterior` or
`CultureRoute` placement require existing own-culture support at the candidate
cell and are rejected in unsupported void, which prevents satellite-founding
from fragmenting territory.

## Culture Power Ledgers

Landmass is not the only definition of cultural power. The host computes three
asymmetric power ledgers per culture; each is a 0-100% share of a world total,
and each culture is designed to lead its own axis in a healthy run:

- **Witness (Aurora's axis)** — share of world physical-plane raw culture
  mass at `z >= 0`, with each cell weighted `1 + 0.25 * z`. Tunnel mass
  (`z < 0`) is excluded: unwitnessed presence does not count.
- **Shadow (Nightshelf's axis)** — mean of three component shares: world
  semantic-plane raw mass; hidden physical mass at `z < 0`; and custody, the
  summed strength of custody relationships (`holds`, `anchored`,
  `practiced_by`, `conduit_of`) touching living custody-kind entities
  (artifacts, abilities) of the culture.
- **Blood (Wake-Singers' axis)** — mean of two component shares: claims, the
  summed strength of `claimed_by` relationships whose claimant belongs to the
  culture; and war presence, the summed strength of `at_war_with`
  relationships with at least one endpoint in the culture.

Components whose world total is zero are dropped from their axis mean. The
domain declares the vocabulary in `DomainConfig.CulturePower` (custody kinds,
custody relationships, claim and war relationship names); the host resolves
names to runtime ids through the compiled domain, so the engine stays
domain-agnostic. Ledgers are exported to the `culture_power_snapshots`
telemetry table on the Monte Carlo snapshot cadence and rendered per culture
in the nations rows of the in-game legend panel.

## Cultural Slots, Work Tiers, And Labor

The kernel has a static cultural slot program. The domain registers the slot
vocabulary, allowed entity kinds, polarity (`positive`, `neutral`, or
`negative`), scoring signals, and benefits. A slot is occupied by a specific
entity only when that entity scores well enough and survives hysteresis against
the incumbent. Slots are not faction-only: settlements, factions, regions,
points of interest, artifacts, penguins, transports, occurrences, abilities, or
other authored kinds may be eligible depending on the slot definition.

Slot occupation matters mechanically:

- it contributes to entity health/effects;
- it raises culture influence production through slot benefits;
- it raises the causal-spend limit for generator targets;
- it increases minor actor emission/effective work through the same multiplier.

Entities inside a supported cultural mass can use the domain-authored
`Deepen the Holding` gambit to increase work tier. Work tier has no authored
maximum in the current Ice Remembers config. Each tier increases the worker cap
and operating radius for that source entity.

Minor actors are unnamed worker cohorts stored in the kernel, not graph
entities. They are emitted by active named entities and perform local culture
labor: reinforcing home mass, extending connected borders, bridging holes,
cleaning foreign traces, maintaining roads, and sustaining the source's
lifecycle. This gives culture a low-salience metabolic layer without adding
more named entities for the player to track.

## Culture in the Domain Package

Each narrative domain defines its culture list. The Ice Remembers currently uses:

- `aurora_stack`
- `nightshelf`
- `wake_singers`
- `the_ice`

`the_ice` is the authored wilderness/substrate culture for unclaimed regions
and points of interest. It is not the compiler fallback culture `none`.
Ice-held places can be claimed by authored actions from Aurora Stack,
Nightshelf, or Wake-Singers entities. The first claim changes the place's
culture away from `the_ice`; current claim actions do not allow one authored
culture to steal a place from another authored culture.

Each culture definition specifies:

- **Name and identity:** the culture's name, descriptive flavor
- **Tendencies:** default positions on semantic planes, possible trait weights,
  and relationship-maintenance biases. "This culture tends toward
  collectivism, martial discipline, and spiritual practice."
- **Ongoing modifiers:** relationship-maintenance and targeting tendencies
  expressed through active gameplay configuration.
- **Naming rules:** patterns, phonemes, syllable structures, title conventions for generating entity names. Simplified from the canonry's Name Forge.
- **Ground truths:** culture-specific facts that affect generation and
  description ("this culture builds with ice, not wood" affects settlement
  descriptions and subtype availability).

## Current Constraints

- Culture is immutable after creation except where an explicit domain-authored
  mutation changes the culture of a claimable place.
- Each entity has exactly one culture.
- Culture count is a domain authoring choice; Ice Remembers currently has
  three social cultures plus the authored `the_ice` substrate.
- Naming is deterministic and procedural, using the culture as an input rather
  than runtime generated prose.
