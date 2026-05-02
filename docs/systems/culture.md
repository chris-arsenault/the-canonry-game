# Culture

A static, domain-defined classification that describes innate tendencies, naming conventions, and identity for entities.

---

## What Culture Is

Culture is a non-spatially-bound, overarching construct that defines an entity's identity foundation. It is:

- **Static per entity.** An entity's culture is set at creation and does not change (at least not in MVP). A penguin born into one culture remains that culture unless a later explicit system says otherwise.
- **Static per domain.** The list of available cultures is defined by the narrative domain, similar to how entity kinds are registered by the domain. Cultures are not created, destroyed, or merged during a run. They exist as a fixed taxonomy.
- **Not equivalent to morphology, location, or faction.** Wake-Singers are not the orca kind; they are a culture mostly associated with orcas. Penguins can share Wake-Singer culture, and orcas can defect from it. Culture and body/kind are orthogonal.
- **An identity tendency.** Each culture may define baseline tendencies on
  semantic planes, trait weighting, relationship maintenance, and naming.
  Culture Y might tend toward ritual conservatism; Culture Z might tend toward
  risk-taking or secrecy. These tendencies affect:
  - **Start conditions:** when an entity is created with a given culture, its
    initial semantic plane positions and trait probabilities may be influenced
    by cultural tendencies
  - **Ongoing modifiers:** cultural affiliation may provide future persistent
    effects on relationship maintenance, targeting, or map influence. These are
    not the active low-altitude driver today.
- **The determinant of entity names.** Culture is the key input to the naming system. A penguin of Culture Y gets a Culture Y name. A settlement founded by Culture Y entities gets a Culture Y place name.

## What Culture Is Not

- **Not an entity kind.** Culture does not exist as an entity in the graph.
  There is no "Culture Y" node with health, traits, relationships, and
  behaviors.
- **Not spatial.** Culture is not tied to regions or cells. It is a property of
  entities regardless of where they are. Cultural influence on the map, if
  implemented, is computed from entities present there, not from culture
  entities.
- **Not dynamic.** The culture list doesn't change during a run. No new cultures emerge, no cultures go extinct. Cultural dynamics (influence spread, friction between cultures) operate on the ENTITIES that have culture, not on culture itself.
- **Not a card target.** Cards don't target "Culture Y." They target entities
  with culture, trait, relationship, or locality filters. "Entities of culture
  Y" is a filter on entity queries, not a reference to a culture entity.

## How Culture Interacts with Systems

| System | How Culture Interacts |
|--------|----------------------|
| Entity Graph (#1) | Culture is a property on entity records (like kind and subtype). Not a separate entity. |
| Health And Traits (#3) | Culture may influence trait probabilities, naming, and future non-health modifiers. |
| Entity Behaviors (#6) | Cultural tendencies can influence low-altitude verb weights, initial semantic position, relationship maintenance, and gambit targeting. |
| Relationships (#7) | Shared culture is a factor in relationship strength modification (same culture trends positive). Cultural difference feeds into disposition calculation via semantic plane distance. |
| Map Signals (#4) | Cultural distribution can feed future locality and semantic-map evidence. |
| Narrative Domain (#19) | Culture list and per-culture definitions (tendencies, naming rules, modifiers) are defined by the domain. |
| Map Visualization (#10) | Cultural distribution can be visualized as an overlay, computed from entities at each location. |
| Generator Executor (#16) | New entities inherit culture from context (location, parent entity, domain rules). |

## Culture in the Domain Package

Each narrative domain defines its culture list. The Ice Remembers currently uses:

- `aurora_stack`
- `nightshelf`
- `wake_singers`

Each culture definition specifies:

- **Name and identity:** the culture's name, descriptive flavor
- **Tendencies:** default positions on semantic planes, possible trait weights,
  and relationship-maintenance biases. "This culture tends toward
  collectivism, martial discipline, and spiritual practice."
- **Ongoing modifiers:** possible future persistent effects on entities of this
  culture. These are not the active low-altitude driver today.
- **Naming rules:** patterns, phonemes, syllable structures, title conventions for generating entity names. Simplified from the canonry's Name Forge.
- **Ground truths:** culture-specific facts that affect generation and description ("this culture builds with ice, not wood" affects settlement descriptions and potentially subtype availability).

## Open Questions

- **Can culture change post-MVP?** Cultural conversion, assimilation, or identity shift could be interesting future mechanics. For MVP, culture is immutable.
- **Multi-cultural entities.** Can an entity have more than one culture? For MVP, one culture per entity is simpler.
- **Culture count per domain.** How many cultures is right for interesting dynamics? Too few = no cultural friction. Too many = diluted identity.
- **Naming system depth.** How sophisticated the per-culture naming rules need to be. The canonry's Name Forge is complex; a simplified version may suffice for MVP.
