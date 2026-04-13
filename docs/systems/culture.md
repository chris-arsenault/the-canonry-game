# Culture

A static, domain-defined classification that describes innate tendencies, naming conventions, and identity for entities.

---

## What Culture Is

Culture is a non-spatially-bound, overarching construct that defines an entity's identity foundation. It is:

- **Static per entity.** An entity's culture is set at creation and does not change (at least not in MVP). A hero born into Culture Y remains Culture Y.
- **Static per domain.** The list of available cultures is defined by the narrative domain, similar to how entity kinds are defined by the engine. Cultures are not created, destroyed, or merged during a run. They exist as a fixed taxonomy.
- **Not equivalent to nation, location, or faction.** A nation may be predominantly one culture but contain members of others. A location may host multiple cultures. A faction may draw from several cultures. Culture and political/organizational structures are independent axes.
- **An innate attribute tendency.** Each culture defines baseline tendencies on semantic planes and scalar attributes. Culture Y might tend toward ideological liberalism, Culture Z toward conservatism. These tendencies affect:
  - **Start conditions:** when an entity is created with a given culture, its initial attribute values and semantic plane positions are influenced by cultural tendencies
  - **Ongoing modifiers:** cultural affiliation may provide ongoing attribute effects (small, persistent nudges toward the cultural baseline -- not overriding other influences, but providing a gravitational pull)
- **The determinant of entity names.** Culture is the key input to the naming system. A hero of Culture Y gets a Culture Y name. A location founded by Culture Y entities gets a Culture Y place name. The naming system is a simplified version of the canonry's Name Forge, driven by per-culture naming rules defined in the domain package.

## What Culture Is Not

- **Not an entity kind.** Culture does not exist as an entity in the graph. There is no "Culture Y" node with attributes, relationships, and behaviors.
- **Not spatial.** Culture is not tied to regions or cells. It is a property of entities regardless of where they are. (Cultural INFLUENCE on the map -- which regions are culturally dominated -- is computed from the cultural attributes of entities present there, not from culture entities.)
- **Not dynamic.** The culture list doesn't change during a run. No new cultures emerge, no cultures go extinct. Cultural dynamics (influence spread, friction between cultures) operate on the ENTITIES that have culture, not on culture itself.
- **Not a card target.** Cards don't target "Culture Y." They target entities with cultural attributes matching certain criteria ("entities of culture Y" is a filter on entity queries, not a reference to a culture entity).

## How Culture Interacts with Systems

| System | How Culture Interacts |
|--------|----------------------|
| Entity Graph (#1) | Culture is a property on entity records (like kind and subtype). Not a separate entity. |
| Attributes (#3) | Culture defines initial attribute tendencies and may provide ongoing modifiers. |
| Entity Behaviors (#6) | Cultural tendencies influence impulse scoring weights. Entities of different cultures evaluate the same impulses with slightly different priorities. |
| Relationships (#7) | Shared culture is a factor in relationship strength modification (same culture trends positive). Cultural difference feeds into disposition calculation via semantic plane distance. |
| Pressure (#11) | Cultural distribution across a region feeds into cultural friction pressure. |
| Narrative Domain (#19) | Culture list and per-culture definitions (tendencies, naming rules, modifiers) are defined by the domain. |
| Map Visualization (#10) | Cultural distribution can be visualized as an overlay, computed from the culture attributes of entities at each location. |
| Generator Executor (#16) | New entities inherit culture from context (location, parent entity, domain rules). |

## Culture in the Domain Package

Each narrative domain defines its culture list. For The Ice Remembers, this might include 4-8 cultures (TBD during domain definition). Each culture definition specifies:

- **Name and identity:** the culture's name, descriptive flavor
- **Attribute tendencies:** default positions on semantic planes, baseline scalar modifiers. "This culture tends toward collectivism, martial discipline, and spiritual practice."
- **Ongoing modifiers:** small persistent effects on entities of this culture (e.g., +1 to cohesion, -1 to ambition). Using the Pathfinder 2e-style typed bonus system, these would be "cultural type" effects.
- **Naming rules:** patterns, phonemes, syllable structures, title conventions for generating entity names. Simplified from the canonry's Name Forge.
- **Ground truths:** culture-specific facts that affect generation and description ("this culture builds with ice, not wood" affects settlement descriptions and potentially subtype availability).

## Open Questions

- **Can culture change post-MVP?** Cultural conversion, assimilation, or identity shift could be interesting future mechanics. For MVP, culture is immutable.
- **Multi-cultural entities.** Can an entity have more than one culture (a mixed-heritage hero, a cosmopolitan settlement)? For MVP, probably one culture per entity. Multi-cultural as post-MVP.
- **Culture count per domain.** How many cultures is right for interesting dynamics? Too few = no cultural friction. Too many = diluted identity. 4-8 feels right but needs playtesting.
- **Naming system depth.** How sophisticated the per-culture naming rules need to be. The canonry's Name Forge is complex; a simplified version may suffice for MVP.
