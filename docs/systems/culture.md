# Culture

A static, domain-defined classification that describes innate tendencies, naming conventions, and identity for entities.

---

## What Culture Is

Culture is a non-spatially-bound, overarching construct that defines an entity's identity foundation. It is:

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
- **Not spatial.** Culture is not tied to regions or cells. It is a property of
  entities regardless of where they are. Cultural influence on the map, if
  implemented, is computed from entities present there, not from culture
  entities.
- **Not a dynamic taxonomy.** The culture list doesn't change during a run. No
  new cultures emerge, no cultures go extinct. Cultural dynamics (influence
  spread, friction between cultures, and authored place claims) operate on the
  ENTITIES that have culture, not on culture itself.
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
| Map Visualization (#10) | Cultural distribution can be visualized as an overlay, computed from entities at each location. |
| Generator Executor (#16) | New entities inherit culture from context (location, parent entity, domain rules). |

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
