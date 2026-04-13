# System 7: Relationships

Fine-grained, always-on dynamics between entities.

---

## Purpose

Relationships are the connective tissue of the entity graph. They encode who is connected to whom, how, and how strongly. Low-altitude systems continuously modify relationship strength based on proximity, shared traits, competition, and other factors. Relationships are the primary mechanism through which entities affect each other (via the effect transfer pattern from the attributes system).

---

## Design Decisions

### Relationships exist only between specific pairs

Relationships are structural links between specific entity instances, created by cards (generators, actions, system effects). There are no implicit or kind-level default relationships. Two nations that have never interacted have NO relationship -- not even a neutral one.

This means graph queries like "find all entities related to X within 2 hops" return meaningful results -- the graph's topology reflects actual connections, not a fully-connected mesh of default attitudes.

**Implication:** "First Contact" or "Establish Diplomacy" type cards are necessary to create initial relationships between entities that encounter each other. Until a card creates the link, the entities are strangers.

### Relationships vs. Dispositions

There are two distinct concepts that must not be conflated:

**Relationships (this system):** structural links in the entity graph. Created by cards. Have a kind, strength, and history. "Nation A is allied_with Nation B, strength 0.7." These are the first-class connections that other systems traverse, that carry effects, and that the player sees in entity detail panels.

**Dispositions (derived from semantic planes):** continuous-variable attitudes between entities that exist without a structural relationship. Two nations that have never formally interacted still have a computable disposition toward each other -- derived primarily from **Euclidean distance on their semantic planes.**

Entities close together on semantic planes (similar ideology, similar government type, similar economic model) have higher dispositions. Entities far apart have lower dispositions. Semantic distance naturally produces alignment or friction without any explicit "attitude" tracking.

Additional modifiers may layer on top of semantic distance:
- Geographic proximity (nearby entities have more salient dispositions)
- Shared threats (entities threatened by the same third party may have boosted dispositions regardless of semantic distance)
- Historical relationships (entities that were previously allied may retain residual positive disposition even after the alliance is culled)

But the primary signal is semantic distance. This is elegant because:
- It's already tracked (entities have semantic plane positions from the attribute system)
- It evolves naturally (as an entity's ideology drifts, its dispositions to all other entities shift automatically)
- It requires no per-pair storage (computed on demand from existing attribute data)
- It produces intuitive results (similar nations get along, different ones don't)

Dispositions serve as the input conditions for relationship-creating cards. "Establish Alliance" might require a disposition above a certain threshold. "Declare Rivalry" might require a disposition below a certain threshold. The disposition is the low-altitude condition; the relationship is the card-created outcome.

### Relationship strength is 0 to 1 with defined polarity

Each relationship kind has an inherent polarity:
- **Positive kinds:** allied_with, trades_with, mentor_of, protects. Higher strength = stronger positive connection.
- **Negative kinds:** rival_of, enemy_of, oppresses. Higher strength = stronger negative connection.
- **Neutral kinds:** controls, member_of, located_in, founded_by. Strength represents degree of connection, not sentiment.

Strength ranges from 0 (barely exists) to 1 (absolute). There is no negative strength. A deteriorating alliance has decreasing strength, approaching 0. It does not flip to negative.

This means:
- "allied_with, strength 0.9" = strong alliance
- "allied_with, strength 0.2" = weak, fragile alliance
- "rival_of, strength 0.8" = bitter rivalry
- "rival_of, strength 0.1" = mild tension, barely rivals

### Relationships do not transform

An allied_with relationship never becomes a rival_of relationship. They are different relationship kinds with different semantics. If an alliance collapses, the allied_with relationship is culled (see below) and a card may separately create a rival_of relationship. The history shows: "these two were once allies, then the alliance ended, then they became rivals" -- three distinct events, not a continuous transformation.

### Relationship culling at low strength

When a relationship's strength drops to zero (or below a configured threshold), it is culled. Culling does NOT delete the relationship -- it sets an end date and transitions it to historical status. The graph retains the record for queries:

- **"Is this alliance active now?"** -- checks for active allied_with relationship
- **"Were they ever allied?"** -- checks for any allied_with relationship (active or historical)
- **"How many times have they allied and broken up?"** -- counts allied_with relationships with end dates

This means relationships accumulate history. A pair of nations that have repeatedly allied and broken up has multiple historical allied_with records, each with start and end dates. This history feeds into disposition computation (entities with a history of betrayal have lower dispositions) and is visible to the player.

### Low-altitude systems modify strength, not topology

Low-altitude game systems (proximity, shared culture, competing claims, trade) continuously modify the STRENGTH of existing relationships. They do NOT create or destroy relationships. Topology changes (new relationships, culled relationships) happen through:
- **Card execution:** generators create entities with pre-connected relationships. Action cards create new relationships. System cards may create or modify relationships.
- **Automatic culling:** when strength drops below threshold, the system sets an end date.

This maintains the design principle: low-altitude physics creates conditions, cards (and culling) determine structural outcomes.

---

## Relationship Properties

Each relationship instance has:
- **Source entity:** the entity this relationship originates from
- **Target entity:** the entity this relationship points to
- **Kind:** the relationship type (allied_with, controls, member_of, etc.)
- **Strength:** 0 to 1, modified by low-altitude systems each tick
- **Start date:** tick when the relationship was created
- **End date:** null if active, set to tick when culled
- **Status:** active or historical
- **Metadata:** optional additional data (creation context, originating card, etc.)

## Relationship Kind Properties

Each relationship kind definition specifies:
- **Polarity:** positive, negative, or neutral
- **Symmetry:** symmetric (A allied_with B implies B allied_with A) or asymmetric (A controls B does not imply B controls A)
- **Exclusivity:** exclusive (a settlement can only be controlled_by one nation) or non-exclusive (a settlement can be influenced_by many cultures)
- **Cardinality constraints:** min/max per entity (a hero must be located_at exactly one settlement)
- **Valid source/target kinds:** which entity kinds can participate and in which role
- **Cull threshold:** strength below which the relationship is automatically culled (may vary by kind -- an alliance might cull at 0.1, a rivalry at 0.05)
- **Effect definitions:** what attribute effects this relationship transfers to the target entity (following the Foundry VTT transfer pattern)

---

## Strength Modification (Low-Altitude)

Each tick, active relationship strength is modified by factors computed from entity state and world conditions:

**Factors that increase strength:**
- Proximity (entities that are near each other)
- Shared culture (entities of the same culture)
- Active trade (entities connected by trade routes with goods flowing)
- Mutual threats (entities sharing a common enemy -- for positive relationships)
- Sustained interaction (relationships that have existed longer tend to strengthen)

**Factors that decrease strength:**
- Distance (entities far apart)
- Cultural difference (entities of different cultures)
- Competing claims (entities with overlapping territorial interests)
- Resource competition (entities competing for the same scarce resources)
- Neglect (relationships without reinforcing factors gradually weaken)

The specific factors and their coefficients are per-relationship-kind. An allied_with relationship strengthens from trade and weakens from competing claims. A controls relationship strengthens from military presence and weakens from distance to capital.

---

## Interactions with Other Systems

| System | How Relationships Interacts |
|--------|----------------------------|
| Entity Graph (#1) | Relationships ARE graph edges. This system manages their strength and lifecycle. |
| Entity Kinds (#2) | Kind constrains which relationship types are valid between which entities. |
| Attributes (#3) | Relationships carry effect definitions that transfer attribute modifiers to related entities (the effect transfer pattern). |
| Map/Spatial (#4) | Proximity and distance feed into strength modification factors. |
| Pressure (#11) | Relationship counts and types within a region feed into pressure computation. |
| Entity Behaviors (#6) | Reads relationship values for impulse evaluation. Relationship-derived effects modify entity effective attributes. |
| Lifecycle (#9) | When an entity transitions to historical status, its relationships may be culled or preserved. |
| Card Executors (#16-18) | Cards create new relationships (generators, actions). Cards may modify relationship strength directly (system effects). |

---

## Open Questions (Deferred)

- **Disposition modifiers beyond semantic distance.** Semantic plane distance is the primary signal. What additional modifiers layer on (proximity, shared threats, historical relationships), and how are they weighted relative to the semantic distance?
- **Relationship history queries.** How historical relationship data is indexed for efficient querying (e.g., "how many times have these two nations been rivals?").
- **Effect type categories.** Mapping relationship kinds to effect types for the Pathfinder 2e-style stacking rules (political, cultural, factional, artifact, environmental). Whether the mapping is 1:1 or more nuanced.
- **Multi-hop relationship semantics.** Whether the system supports queries like "friend of my enemy is my enemy" -- derived dispositions from relationship topology. Could be powerful but expensive.
- **Relationship visibility to the player.** How relationships are surfaced in the UI. Whether the player sees all relationships or only high-strength ones. Whether relationship history is browsable.
