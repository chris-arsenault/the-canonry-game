# System 22: Locality Queries

Locality is the direct, bounded view of what a location supports right now.
There is no `MapActivity` or `MapSignals` tick program and no second
materialized spatial world.

## Current facts

- **Occupancy** counts active entities at the queried cell.
- **Crowding** compares bounded local occupancy with current capacity.
- **Capacity** combines terrain/geography, nearby kind rules, owned coverage,
  culture maturity, and knowledge modifiers.
- **Support and threat** combine nearby kind rules, incident active
  relationships, current culture pressure, and owned coverage.
- **Attractor mass** combines authored kind mass, current attributes, coverage,
  culture maturity, chronicle heat, support, and threat.

Both physical and semantic consumers use the same direct-query model. Authored
weights live in `LocalityConfig` and compile to primitive `LocalityRules`.

## Consumer rule

Consumers ask only at the cell or entity they are already evaluating.
Low-altitude verbs, effects, generator placement, frontier work, culture slots,
and minor actors do not request a whole-map refresh.

Historical visit heat and previous-tick signal lag are intentionally absent.
If a mechanic needs durable memory, it must own a specific durable fact such as
culture mass, road quality, coverage, relationship strength, or chronicle
heat—not recreate a generic global activity field.
