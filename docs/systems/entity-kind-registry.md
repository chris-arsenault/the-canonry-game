# Entity Kind Registry

The registry is domain-owned. It defines the concrete kind vocabulary compiled
into the engine for a run.

## Active Registry

| Kind | Category | Subtypes | Health |
| --- | --- | --- | --- |
| `region` | Spatial | `face`, `shelf`, `depths`, `reach`, `sea`, `scar` | `stability` |
| `settlement` | Spatial | `rookery`, `colony`, `holdfast`, `sanctuary`, `harbor`, `reefhold` | `temp_hp` |
| `faction` | Agency | `order`, `compact`, `cell`, `choir` | `cohesion` |
| `penguin` | Agency | `warden`, `broker`, `witness`, `exile` | `vitality` |
| `artifact` | Asset | `relic`, `instrument`, `weapon`, `tome`, `seal` | `condition` |

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

## Future Registry Additions

Likely future kinds include orca, transport, monster, point of interest, and
occurrence. Add them by extending the domain schema and kind registry, then
compiling to IDs. Do not reintroduce engine enums for concrete kinds.
