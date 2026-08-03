# System 23: Tags And Status State Machines

Tags are discrete per-entity status bits (max 31, domain-declared catalog
compiled to ids). They are the entity-centric complement to the map-centric
fields: where culture mass and signals answer "what is this place like,"
tags answer "what state is this entity in." Relationship strength remains
the only continuous numeric; every state machine is built from tags.

## Writers

- **Threshold rules with hysteresis** (`RelationshipTagRuleConfig`): an
  entity gains a tag while the summed strength of its relationships of a
  kind crosses a set threshold, and loses it below the clear threshold.
  Example: a faction is `fervent` while its outgoing devotion flow is hot.
- **Jurisdiction-filtered rules**: the same rule shape with an
  `InsideCoverageOwnerKind`/`Subtype`/`Plane` filter — the tag holds only
  while the entity stands inside matching named coverage. This is how
  violation works: it is a *state over existing primitives*, never an
  interpreted event. `oathbreaker` = holding war edges while inside accord
  coverage; `profaner` = devotion flow inside taboo ground. Leaving the
  jurisdiction clears the tag.
- **Direct sets from generators** (`SetBindingTagEffect` /
  `ClearBindingTagEffect` on any resolved ref including `$target`): the
  event-stepped ladders. Raids set `hunted`; a persistence generator steps
  `hunted` to `terrified` while predation continues; a fade generator steps
  back down when it stops; a fallen hero sets `bereaved`.

## Readers

- Generator target and binding requirements (`RequiredTags`/`WithoutTags`,
  compiled to bitmasks) — escalation and response content is tag-gated:
  counter-offensives need `terrified` + `bereaved`, the Accord enforces on
  `oathbreaker`, wardens rise on `terrified`.
- Effect rules and future gambit preconditions follow the same pattern.

## Telemetry

`EntityTagChanged` is a primitive kernel event, replayed on snapshot. The
`entity_tag_events` telemetry export was deleted 2026-08-03 (no reader).

## Design rules

State machines are discrete; do not introduce scalar stocks (see the
no-stock/flow principle). Ladders compose because states persist — prefer
persistent-state composition over coincidence of threshold crossings when
gating rare events.
