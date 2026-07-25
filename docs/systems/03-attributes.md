# System 3: Health And Traits

The active entity model has one mutable scalar per kind: health. Static
per-entity differentiation lives in traits.

## Health

Health is the low-level simulation stock that keeps an entity active. It is a
bounded 0-100 scalar, seeded with slight variation, modified by upkeep and
effects, and read by lifecycle.

| Kind | Health Attribute |
| --- | --- |
| `region` | `stability` |
| `settlement` | `temp_hp` |
| `point_of_interest` | `presence` |
| `faction` | `cohesion` |
| `penguin` | `vitality` |
| `orca` | `endurance` |
| `monster` | `containment` |
| `artifact` | `condition` |
| `ability` | `clarity` |
| `transport` | `integrity` |

The removed internal/external attribute pair should not be reintroduced as a
generic numeric personality model. If a second scalar seems necessary, first
ask whether it is really a trait, a relationship strength, a locality query, a
direct world fact, or authored entity state that should stay explicit.

## Traits

Traits are mostly static per-entity differentiators assigned when an entity is
created. A kind declares its trait catalog; each entity independently receives
zero or more of those traits according to the authored assignment chances.

Traits carry:

- stable ID and display name;
- polarity (`positive` or `negative`);
- effect summary;
- assignment chance;
- zero or more declarative operations.

Trait effects are explicit and data-authored. A trait does not create hidden
generic math by itself, and runtime systems should not special-case trait names.
Instead, a trait owns operation clauses that compile into shared query, effect,
or decision-modifier primitives.

Current operation primitives:

- `TraitAttributeDeltaOperationConfig`: per-tick health delta gated by map
  signal, relationship distance, related culture influence, and optional
  target/related trait bearer. These rules execute through the shared effect
  path.
- `TraitLowAltitudeWeightOperationConfig`: modifies low-altitude verb weights,
  opportunity bonus, or relationship fitness weight. These rules execute
  through `DecisionModifier`.
- `TraitGambitModifierOperationConfig`: modifies gambit score and success
  chance when the actor or target bears the trait. These rules execute through
  `DecisionModifier`.

Adding a normal new trait should mean editing the trait config: id, display
text, assignment chance, and operation clauses. Engine code changes are reserved
for adding a new reusable operation primitive, not for adding a new trait ID.

## Current Trait Catalog

The current catalog has one positive and one negative trait per kind. These are
domain-authored anchors for active trait mechanics.

| Kind | Positive Trait | Negative Trait |
| --- | --- | --- |
| `region` | `region_sheltering_ground` | `region_brittle_seams` |
| `settlement` | `settlement_provident_hearth` | `settlement_overextended_halls` |
| `point_of_interest` | `point_of_interest_generous_bounty` | `point_of_interest_treacherous_approach` |
| `faction` | `faction_steady_cadre` | `faction_fractious_council` |
| `penguin` | `penguin_ranging_heart` | `penguin_hearthbound` |
| `orca` | `orca_sure_wake` | `orca_blood_frenzy` |
| `monster` | `monster_well_bound` | `monster_hungry_stirring` |
| `artifact` | `artifact_restorative_aura` | `artifact_volatile_resonance` |
| `ability` | `ability_practiced_form` | `ability_brittle_lineage` |
| `transport` | `transport_proven_make` | `transport_hidden_fault` |

The initial operations are intentionally conservative:

- place-like traits mostly adjust their own health from local support, threat,
  or crowding;
- penguin traits adjust low-altitude exploration versus home-base movement;
- faction traits adjust cohesion plus faction gambit score/success;
- artifact traits grant health effects through `holds`/`anchored` relationships
  and modify cleanse-scar gambits;
- ability, monster, orca, and transport traits currently express health effects
  through relationship or local map-signal clauses.

## Authoring Rules

- Use health for survival, decay, repair, damage, and lifecycle.
- Use traits for mostly static personality, temperament, affinity, artifact
  effect identity, or other "this entity is different" facts.
- Trait operation authoring should use existing operation primitives whenever
  possible. If the desired behavior cannot be expressed, add one reusable
  primitive in the shared query/effect/decision layer and then keep individual
  traits data-only.
- Use relationship strength as the default numeric gate for generators and
  gambits. Example: an artifact cleanses a scar because it has a strong
  `anchored` or `holds` relationship, not because it has `power >= 45`.
- Locality-based queries are acceptable when spatial or semantic context is the
  meaningful gate.
- Do not add attributes solely to feed generator thresholds, gambit odds, or
  old process-style action demand.
- Cultural display names are UI flavor only. Mechanical implementation remains
  the same health attribute or trait ID.
