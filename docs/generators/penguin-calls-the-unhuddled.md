# Penguin Calls the Unhuddled

Status: first finalized workshop candidate

## Purpose

This generator is part of the first-pass starter set: one generator per target kind, each creating a real connected bundle of new named entities. It is intended to be satisfiable from seed state and to introduce key social relationships into the world rather than assuming they already exist.

## Summary

- Target kind: `Penguin`
- Narrative event: A high-echo exile survives long enough that the cold-lost gather around them, becoming a named refuge and compact.

## Trigger shape

Keep this broad for now. Exact trigger logic should wait until the rest of the starter generators are shaped and we can check seed satisfiability across the whole set.

- `Echo` High
- warmth pressure or social marginality
- instability around the target's current situation

## Created entities

- `Settlement` sanctuary/refuge
- `Faction` exile compact
- `Penguin` first follower

## Created relationships

- refuge `ControlledBy` new faction
- target penguin `MemberOf` new faction
- target penguin `HomeBase` refuge
- lieutenant `MemberOf` new faction
- lieutenant `HomeBase` refuge

## Non-creation consequences

- the target penguin is promoted from local actor to founder-level named significance
- nearby social pressure can be redirected into the new refuge

## Why this belongs in the starter set

- It is generator-shaped rather than action-shaped: the center of gravity is a new connected bundle.
- It is satisfiable from seed state: it does not require prior `HomeBase` or `MemberOf` on the target.
- It introduces `HomeBase` and `MemberOf` into the world rather than depending on them.
- It follows the cross-kind rule well: target kind is `Penguin`, but the created center of gravity is `Settlement` + `Faction`.

## Open questions

- What penguin subtype should this prefer after the next subtype pass: exile, witness, or something more specific?
- Should the new faction skew religious, political, or survivalist by default?
- Should the refuge settlement subtype be sanctuary-like, camp-like, or frontier hamlet-like?
- Does this generator want an optional conflict hook such as `ClaimedBy`, or should that wait for later generators to create pressure on the refuge?
