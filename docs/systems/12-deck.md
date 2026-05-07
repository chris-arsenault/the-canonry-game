# System 12: Cards And Tableau

Cards are the player-facing wrappers over world-shaping capabilities. The
current runtime exposes generator and gambit cards through the tableau. Cards
are not the low-altitude runtime.

## Purpose

Cards let players choose what kind of world can emerge. Active cards feed the
tableau; the tableau gates generators and gambits.

## Current Relationship To Runtime

- Generators already model high-altitude entity creation bundles.
- Gambits already model significant entity actions against targets.
- Low-altitude verbs are domain gameplay config, not player cards.
- Process cards and standing drives are removed and should not be restored under
  deck terminology.
- Generator and gambit card structure should be derived from compiled domain
  definitions and the definition graph, not hardcoded UI metadata or symbol
  strings.

## Card Families

| Card Family | Runtime Layer | Typical Effect |
| --- | --- | --- |
| Generator | High altitude | Create a named bundle of connected entities. |
| Gambit | Mid/high altitude | Create, invert, or strengthen significant relationships; kill, wield, betray, declare war. |
| System | Inactive | Not part of the current runtime. |

## Deck Principles

- Deck composition is the player's theory of the world.
- Different active generator/gambit sets should produce different histories.
- Cards should be narratively meaningful, not only graph-interesting.
- The deck should not become a hidden planner or resource economy.
- Per-card modifiers are future card-instance state. They should layer on top
  of immutable compiled definitions and only enter the kernel when they change
  runtime-observable behavior.
