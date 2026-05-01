# System 12: Deck

The deck is the future player-facing selection of world-shaping cards. It is not
currently the low-altitude runtime.

## Purpose

The deck should let players choose what kind of world can emerge. Active cards
feed the tableau, and the tableau enables generators, gambits, and eventually
larger system cards.

## Current Relationship To Runtime

- Generators already model high-altitude entity creation bundles.
- Gambits already model significant entity actions against targets.
- Low-altitude verbs are domain gameplay config, not player cards.
- Process cards and standing drives are removed and should not be restored under
  deck terminology.

## Intended Card Families

| Card Family | Runtime Layer | Typical Effect |
| --- | --- | --- |
| Generator | High altitude | Create a named bundle of connected entities. |
| Gambit | Mid/high altitude | Create, invert, or strengthen significant relationships; kill, wield, betray, declare war. |
| System | Future macro layer | Adjust large-scale conditions or enable/disable broad rules without creating routine low-altitude actions. |

## Deck Principles

- Deck composition is the player's theory of the world.
- Different active generator/gambit sets should produce different histories.
- Cards should be narratively meaningful, not only graph-interesting.
- The deck should not become a hidden planner or resource economy.

## Deferred

Draw cadence, hand size, upgrades, archetypes, and meta-progression remain
design work. They should be implemented only after the simulation substrate is
stable enough to make deck choices meaningful.
