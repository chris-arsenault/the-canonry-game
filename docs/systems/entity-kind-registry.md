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
- `led_by`, `member_of`
- `holds`, `anchored`, `created_by`, `native_to`, `part_of`
- `memorializes`, `trade_route`, `founded_from`
- `conduit_of`, `ward_against`, `corrupted_by`, `cleansed_by`

Relationship facts carry generator/gambit-specific meaning without creating
throwaway relationship kinds.

Relationship facts are edge annotations, not entity traits. Use traits for
static personality or artifact-effect identity; use facts when the meaning
belongs to a specific relationship event or relationship history.

## Future Registry Additions

All initially deferred kinds (`orca`, `point_of_interest`, `ability`,
`monster`, `transport`, `occurrence`) are now registered. New kinds added in
the future should follow the same path: extend the domain schema and kind
registry, then compile to IDs. Do not reintroduce engine enums for concrete
kinds.

Monster status (sealed/unbound/risen) remains deferred. The kind is registered
with `containment` health and placeholder binding/hunger traits; the status
field and the gambits/generators that move a monster between statuses are
future work.
