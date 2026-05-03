# Entities and Relationships

Current entity and relationship source of truth lives in code. This document is
the design narrative around that schema, not a duplicate registry.

## Source Of Truth

- Schema IDs: `src/IceRemembers/IceRemembersSchema.cs`
- Kind definitions and relationships: `src/IceRemembers/IceRemembersDefinition.cs`
- Low-altitude verbs: `src/IceRemembers/IceRemembersLowAltitudeVerbs.cs`
- Generators: `src/IceRemembers/IceRemembersGeneratorDefinition.cs`
- Gambits: `src/IceRemembers/IceRemembersGambitDefinition.cs`

## Current Kinds

| Kind | Category | Health | Placeholder Traits | Primary Plane |
| --- | --- | --- | --- | --- |
| `region` | Spatial | `stability` | `region_sheltering_ground`, `region_brittle_seams` | Semantic |
| `settlement` | Spatial | `temp_hp` | `settlement_provident_hearth`, `settlement_overextended_halls` | Semantic |
| `point_of_interest` | Spatial | `presence` | `point_of_interest_generous_bounty`, `point_of_interest_treacherous_approach` | Semantic |
| `faction` | Agency | `cohesion` | `faction_steady_cadre`, `faction_fractious_council` | Semantic |
| `penguin` | Agency | `vitality` | `penguin_ranging_heart`, `penguin_hearthbound` | Physical |
| `orca` | Agency | `endurance` | `orca_sure_wake`, `orca_blood_frenzy` | Physical |
| `monster` | Agency | `containment` | `monster_well_bound`, `monster_hungry_stirring` | Physical |
| `artifact` | Asset | `condition` | `artifact_restorative_aura`, `artifact_volatile_resonance` | Physical |
| `ability` | Asset | `clarity` | `ability_practiced_form`, `ability_brittle_lineage` | Semantic |
| `transport` | Asset | `integrity` | `transport_proven_make`, `transport_hidden_fault` | Physical |

Culture is orthogonal to morphology. Wake-Singers are not "the orca kind";
they are a culture largely associated with orcas, with possible penguin
defectors or adherents.

Health is the only mutable per-kind scalar. Traits are mostly static
per-entity differentiators assigned at creation. Trait behavior is authored as
operation clauses on the trait itself; runtime systems interpret generic
operation primitives rather than checking individual trait names.

## Relationship Role

Relationships are graph topology. They define who is connected, what kind of
connection exists, and how strong it is. Relationship facts add event-specific
meaning to real edges without inventing disposable relationship kinds.

Current relationship families:

- place and control: `controlled_by`, `claimed_by`, `home_base`, `anchored`
- agency bonds: `allied_with`, `rival_of`, `at_war_with`, `betrayed_by`,
  `led_by`, `member_of`
- asset bonds: `holds`, `created_by`, `native_to`, `part_of`, `conduit_of`,
  `ward_against`
- memory and aftermath: `memorializes`, `corrupted_by`, `cleansed_by`,
  `founded_from`, `trade_route`

## Low-Altitude Role

Low-altitude verbs do not implement the removed process ring. They create map
life:

- mobile entities commit to short movement verbs
- entities dwell and roam rather than synchronize perfectly
- settlements and factions exert influence instead of walking physically
- all primary entities can exert semantic influence
- relationships strengthen or decay from proximity, influence, culture, and
  contact memory

## High-Altitude Role

Generators add named entities. Gambits change significant relationships,
status, health, or other authored state. The relationship catalog should
support both without creating graph edges that no other system understands.
When a numeric gate is needed, relationship strength should be the first
choice. Event meaning and lineage should usually be ordinary relationship
kinds or provenance; any remaining relationship facts are rare boolean edge
qualifiers, not static entity personality.
