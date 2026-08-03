# Research Corpus

Background research that informed the design. Reference material — not
canonical design — kept here so the reasoning is visible without
re-deriving it. Some research files reference superseded process/planner
models; treat them as historical inputs, not current architecture.

## Top-level

- [`requirements.md`](requirements.md) — original requirements summary
  and the design space we positioned the game inside (CK-meets-Victoria-
  meets-DF-Legends).
- [`tech-stack.md`](tech-stack.md) — engine and language evaluation that
  led to Godot 4 + C#.
- [`exemplars.md`](exemplars.md) — deep mechanical analysis of the games
  whose patterns we drew on (RimWorld storyteller, DF, etc.).

## Cards

Card-system research. Crystallized into the generators / systems / actions
model in [`docs/design/principles.md`](../design/principles.md) and the
[generators design](../design/generators.md).

- [`cards/balatro-card-model.md`](cards/balatro-card-model.md)
- [`cards/card-mechanics-research.md`](cards/card-mechanics-research.md)
- [`cards/card-system-proposal.md`](cards/card-system-proposal.md)

## Simulation

Process / behavior / planning research. The current runtime shape is captured
in [`docs/systems/06-entity-behaviors.md`](../systems/06-entity-behaviors.md)
and [`docs/systems/simulation-principles.md`](../systems/simulation-principles.md).

- [`simulation/formal-system-design.md`](simulation/formal-system-design.md)
- [`simulation/formal-system-design-addon.md`](simulation/formal-system-design-addon.md)
- [`simulation/entity-behavior-models.md`](simulation/entity-behavior-models.md)

## Graph

Graph storage and library evaluation. Crystallized into the QuikGraph
choice referenced in [`docs/architecture.md`](../architecture.md).

- [`graph/graph-database-evaluation.md`](graph/graph-database-evaluation.md)
- [`graph/graph-library-evaluation.md`](graph/graph-library-evaluation.md)

## Pacing

Tempo, attention, and lose-condition research. Crystallized into design
principles 6 (contemplative tempo) and 7 (degenerate states).

- [`pacing/tempo-and-attention.md`](pacing/tempo-and-attention.md)
- [`pacing/lose-conditions-and-metrics.md`](pacing/lose-conditions-and-metrics.md)

## Modes

- [`modes/favored-nation-mode.md`](modes/favored-nation-mode.md) — patron
  god mode. Deferred; see the deferred-ideas section of
  [`docs/backlog.md`](../backlog.md).
