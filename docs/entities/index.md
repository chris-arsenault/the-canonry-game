# Entity Index

Current active entity kinds and their current attributes, as defined in
[TestDomain.cs](/home/tsonu/src/the-canonry-game/src/Engine/Domain/TestDomain.cs).

This is an inventory page, not a design-spec page.

## Region

- `regional_safety` — scalar — Projection
- `geologic_stability` — scalar — Integrity
- `mystic_resonance` — 2D — Alignment

Health attribute:
- none

## Settlement

- `residents` — scalar — Integrity
- `security` — scalar — Capacity
- `prestige` — scalar — Projection
- `civic_ideology` — 2D — Alignment

Health attribute:
- `residents`

## Faction

- `cohesion` — scalar — Integrity
- `support` — scalar — Capacity
- `influence` — scalar — Projection
- `civic_ideology` — 2D — Alignment

Health attribute:
- `cohesion`

## Npc

- `vitality` — scalar — Integrity
- `satiety` — scalar — Need
- `ambition` — scalar — Drive
- `civic_ideology` — 2D — Alignment

Health attribute:
- `vitality`

## Artifact

- `condition` — scalar — Integrity
- `power` — scalar — Capacity / Projection
- `containment` — scalar — Drive
- `mystic_resonance` — 2D — Alignment

Health attribute:
- `condition`
