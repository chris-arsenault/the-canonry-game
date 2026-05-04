# Entity Kind Registry

The registry is domain-owned. It defines the concrete kind vocabulary compiled
into the engine for a run.

## Active Registry

| Kind | Category | Subtypes | Health | Placeholder Traits |
| --- | --- | --- | --- | --- |
| `region` | Spatial | `face`, `shelf`, `depths`, `reach`, `sea`, `scar` | `stability` | `region_sheltering_ground`, `region_brittle_seams` |
| `settlement` | Spatial | `rookery`, `colony`, `holdfast`, `sanctuary`, `harbor`, `reefhold` | `temp_hp` | `settlement_provident_hearth`, `settlement_overextended_halls` |
| `point_of_interest` | Spatial | `vent`, `peak`, `bloom`, `shoal` | `presence` | `point_of_interest_generous_bounty`, `point_of_interest_treacherous_approach` |
| `faction` | Agency | `order`, `compact`, `cell`, `choir` | `cohesion` | `faction_steady_cadre`, `faction_fractious_council` |
| `penguin` | Agency | `warden`, `broker`, `witness`, `exile` | `vitality` | `penguin_ranging_heart`, `penguin_hearthbound` |
| `orca` | Agency | `matriarch`, `bull`, `scout`, `elder` | `endurance` | `orca_sure_wake`, `orca_blood_frenzy` |
| `monster` | Agency | `leviathan`, `wraith`, `beast`, `revenant` | `containment` | `monster_well_bound`, `monster_hungry_stirring` |
| `artifact` | Asset | `relic`, `instrument`, `weapon`, `tome`, `seal` | `condition` | `artifact_restorative_aura`, `artifact_volatile_resonance` |
| `ability` | Asset | `technique`, `rite`, `craft` | `clarity` | `ability_practiced_form`, `ability_brittle_lineage` |
| `transport` | Asset | `trade`, `battleship`, `scout`, `citadel` | `integrity` | `transport_proven_make`, `transport_hidden_fault` |
| `occurrence` | Agency | `war`, `trade_conference`, `plague`, `festival` | `momentum` | `occurrence_gathering_force`, `occurrence_grinding_attrition` |

Health is the only mutable scalar attribute in the active registry. Traits are
mostly static per-entity differentiators assigned when an entity is created.
Trait operations are declared on each trait and compiled into generic health,
low-altitude, or gambit modifiers; runtime code must not special-case trait IDs.

## Categories

Categories remain engine-level because they support generic validation:

- Spatial: place-like entities and coverage owners.
- Agency: entities capable of social relationships and high-altitude action.
- Asset: named objects that can move, be held, or become symbolically important.

Concrete kind names remain domain-level.

## Relationship Vocabulary

Current Ice Remembers relationships include:

- `controlled_by`, `claimed_by`, `home_base`
- `allied_with`, `rival_of`, `at_war_with`, `betrayed_by`
- `led_by`, `member_of`, `taught_by`, `practiced_by`
- `holds`, `anchored`, `created_by`, `native_to`, `part_of`
- `memorializes`, `trade_route`, `founded_from`
- `conduit_of`, `ward_against`, `corrupted_by`, `cleansed_by`

Relationship kinds should be reusable world vocabulary, not one-off generator
memory. Relationship strength carries magnitude; provenance and event history
carry how an edge was produced or changed.

## Registry Rule

All concrete Ice Remembers kinds are domain-registered and compile to IDs.
Runtime code must not reintroduce engine enums for concrete kinds. The active
monster model uses `containment` health and binding/hunger traits; it does not
have a separate sealed/unbound/risen status field.
