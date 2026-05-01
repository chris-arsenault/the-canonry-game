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

| Kind | Category | Health | Other Attributes | Primary Plane |
| --- | --- | --- | --- | --- |
| `region` | Spatial | `stability` | `holding`, `pristiness` | Semantic |
| `settlement` | Spatial | `temp_hp` | `dominion`, `shelter` | Semantic |
| `faction` | Agency | `cohesion` | `reach`, `provisions` | Semantic |
| `penguin` | Agency | `vitality` | `echo`, `warmth` | Physical |
| `artifact` | Asset | `condition` | `power`, `resonance` | Physical |

Culture is orthogonal to morphology. Wake-Singers are not "the orca kind";
they are a culture largely associated with orcas, with possible penguin
defectors or adherents.

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
status, or attributes. The relationship catalog should support both without
creating graph edges that no other system understands.
