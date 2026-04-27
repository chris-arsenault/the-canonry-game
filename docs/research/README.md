# Research Corpus

Background research that informed the design. Reference material — not
canonical design — kept here so the reasoning is visible without
re-deriving it. Designs that crystallized out of this research live in
[`docs/design/`](../design/).

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

Process / behavior / planning research. Crystallized into the
[entities and relationships design](../design/entities.md), the
[loops](../design/loops.md), and the per-system docs in
[`docs/systems/`](../systems/).

- [`simulation/formal-system-design.md`](simulation/formal-system-design.md)
- [`simulation/formal-system-design-addon.md`](simulation/formal-system-design-addon.md)
- [`simulation/entity-behavior-models.md`](simulation/entity-behavior-models.md)
- [`simulation/needs-and-enabler-processes.md`](simulation/needs-and-enabler-processes.md)

## Graph

Graph storage and library evaluation. Crystallized into the QuikGraph
choice referenced in [`docs/architecture.md`](../architecture.md).

- [`graph/graph-database-evaluation.md`](graph/graph-database-evaluation.md)
- [`graph/graph-library-evaluation.md`](graph/graph-library-evaluation.md)

## Pacing

Tempo, attention, and lose-condition research. Crystallized into design
principles 6 (contemplative tempo) and 7 (degenerate states), and the
[anti-degeneracy design](../design/anti-degeneracy.md).

- [`pacing/tempo-and-attention.md`](pacing/tempo-and-attention.md)
- [`pacing/lose-conditions-and-metrics.md`](pacing/lose-conditions-and-metrics.md)

## Modes

- [`modes/favored-nation-mode.md`](modes/favored-nation-mode.md) — patron
  god mode. Post-MVP per
  [`docs/design/post-mvp-backlog.md`](../design/post-mvp-backlog.md).
